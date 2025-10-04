using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace DataProcessorService
{
    public class DataProcessingWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly string _queueName = "modules";
        private readonly ILogger<DataProcessingWorker> _logger;

        public DataProcessingWorker(
            IConnection connection, 
            ILogger<DataProcessingWorker> logger)
        {
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

                _logger.LogInformation($" [x] Message recieved.");


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
    }
}
