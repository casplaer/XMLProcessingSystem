using DataProcessorService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DataProcessorService
{
    public class DataProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly string _queueName = "modules";
        private readonly ILogger<DataProcessingWorker> _logger;

        private readonly SemaphoreSlim _parallelism = new(10);

        public DataProcessingWorker(
            IServiceProvider serviceProvider,
            IConnection connection,
            ILogger<DataProcessingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _connection = connection;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataProcessingWorker started.");

            using var setupChannel = await _connection.CreateChannelAsync();
            await setupChannel.QueueDeclareAsync(queue: _queueName,
                                                 durable: true,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 cancellationToken: stoppingToken);

            _logger.LogInformation("Modules queue declared. Waiting for messages.");

            var consumer = new AsyncEventingBasicConsumer(setupChannel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (message != null)
                {
                    await _parallelism.WaitAsync();

                    _ = Task.Run(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        try
                        {
                            _logger.LogInformation(" [x] Processing message...");
                            await SaveModuleDataAsync(message, dbContext);
                            await setupChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message");
                            await setupChannel.BasicNackAsync(ea.DeliveryTag, false, true);
                        }
                        finally
                        {
                            _parallelism.Release();
                        }
                    });
                }

                await setupChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await setupChannel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task SaveModuleDataAsync(string message, AppDbContext dbContext)
        {
            using var doc = JsonDocument.Parse(message);
            var devices = doc.RootElement.GetProperty("Devices").EnumerateArray();

            foreach (var device in devices)
            {
                var moduleCategoryId = device.GetProperty("ModuleCategoryId").GetString();

                if (moduleCategoryId == null)
                {
                    _logger.LogWarning($"No ModuleCategoryID.");

                    continue;
                }

                var indexWithinRole = device.GetProperty("IndexWithinRole").GetInt16();
                var rapidControlStatusXml = device.GetProperty("RapidControlStatusXml").GetString();

                if (!string.IsNullOrEmpty(rapidControlStatusXml))
                {
                    var xml = XDocument.Parse(rapidControlStatusXml);
                    var moduleState = xml.Root?.Element("ModuleState")?.Value;

                    if (moduleState == null)
                    {
                        _logger.LogWarning($"ModuleState not found for {moduleCategoryId} {indexWithinRole}");
                        continue;
                    }

                    var newModuleEntity = new Module()
                    {
                        ModuleCategoryID = moduleCategoryId,
                        ModuleState = moduleState
                    };

                    dbContext.Modules.Add(newModuleEntity);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
