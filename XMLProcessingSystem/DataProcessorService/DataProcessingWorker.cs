using DataProcessorService.Data;
using DataProcessorService.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Polly;
using Polly.Retry;
using System.Data.Common;

namespace DataProcessorService
{
    public class DataProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly string _queueName;
        private readonly ILogger<DataProcessingWorker> _logger;

        private readonly SemaphoreSlim _parallelism = new(10);
        private readonly AsyncRetryPolicy _dbRetryPolicy;

        public DataProcessingWorker(
            IOptions<RabbitMQSetting> rabbitMqSetting,
            IServiceProvider serviceProvider,
            IConnection connection,
            ILogger<DataProcessingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _queueName = rabbitMqSetting.Value.QueueName ?? "modules";
            _connection = connection;
            _logger = logger;

            _dbRetryPolicy = Policy
                .Handle<DbUpdateException>()
                .Or<DbException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromMilliseconds(Math.Min(2000, Math.Pow(2, attempt) * 100)) +
                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
                    onRetry: (ex, delay, attempt) =>
                    {
                        _logger.LogWarning(ex, "Transient DB error on attempt {Attempt}. Retrying in {Delay}.", attempt, delay);
                    });
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

                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Received empty message, rejecting.");
                    await setupChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await _parallelism.WaitAsync(stoppingToken);
                try
                {
                    _logger.LogInformation(" [x] Processing message...");

                    await _dbRetryPolicy.ExecuteAsync(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        await SaveModuleDataAsync(message, dbContext);
                    });

                    _logger.LogInformation(" [x] Message processed successfully!");

                    await setupChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " [x] Error processing message");
                    await setupChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
                finally
                {
                    _parallelism.Release();
                }
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
            var packageId = doc.RootElement.GetProperty("PackageID").GetString();

            _logger.LogInformation($"Processing package {packageId}");

            foreach (var device in devices)
            {
                var moduleCategoryId = device.GetProperty("ModuleCategoryId").GetString();

                if (moduleCategoryId == null)
                {
                    _logger.LogWarning($"No ModuleCategoryID.");

                    continue;
                }

                int? indexWithinRole = device.GetProperty("IndexWithinRole").GetInt16();
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

                    var moduleEntity = await dbContext.Modules
                                                      .Where(m => m.ModuleCategoryID == moduleCategoryId &&
                                                                  m.IndexWithinRole == indexWithinRole &&
                                                                  m.PackageID == packageId)
                                                      .FirstOrDefaultAsync();

                    if (moduleEntity == null)
                    {
                        _logger.LogInformation($"No Module with ID {moduleCategoryId} found. Creating new entity.");

                        moduleEntity ??= new Module()
                        {
                            ModuleCategoryID = moduleCategoryId,
                            ModuleState = moduleState,
                            IndexWithinRole = indexWithinRole,
                            PackageID = packageId
                        };
                        
                        dbContext.Modules.Add(moduleEntity);

                        _logger.LogInformation($"Successfully created {moduleCategoryId} {indexWithinRole} {moduleState} db entity.");
                    }
                    else
                    {
                        _logger.LogInformation($"Module with ID {moduleCategoryId} found. Updating module state.");

                        moduleEntity.ModuleState = moduleState;

                        dbContext.Update(moduleEntity);

                        _logger.LogInformation($"Successfully updated module state of module {moduleCategoryId} {indexWithinRole} to {moduleState}.");
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    