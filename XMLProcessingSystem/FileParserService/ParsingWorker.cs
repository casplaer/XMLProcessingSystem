using FileParserService.Common.Helpers;
using FileParserService.Dto;
using FileParserService.Settings;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using Polly;
using RabbitMQ.Client.Exceptions;

namespace FileParserService
{
    public class ParsingWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly string _queueName;
        private readonly string _inputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "input");
        private readonly XmlSerializer _serializer;
        private readonly ILogger<ParsingWorker> _logger;
        private readonly IAsyncPolicy _publishRetryPolicy;

        public ParsingWorker(
            IOptions<RabbitMQSetting> rabbitMqSetting,
            IConnection connection,
            XmlSerializer serializer,
            ILogger<ParsingWorker> logger)
        {
            _connection = connection;
            _queueName = rabbitMqSetting.Value.QueueName ?? "modules";
            _serializer = serializer;
            _logger = logger;

            _publishRetryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<AlreadyClosedException>()
                .Or<OperationInterruptedException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromMilliseconds(Math.Min(2000, Math.Pow(2, attempt) * 100)) +
                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
                    onRetry: (ex, delay, attempt) =>
                    {
                        _logger.LogWarning(ex, "Publish retry {Attempt} in {Delay}.", attempt, delay);
                    });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var setupChannel = await _connection.CreateChannelAsync();
            await setupChannel.QueueDeclareAsync(queue: _queueName,
                                                 durable: true,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var xmlFiles = Directory.GetFiles(_inputDirectory, searchPattern: "*.xml");
                    if (xmlFiles.Length == 0)
                    {
                        _logger.LogInformation("No XML files found. Waiting...");
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation($"Found {xmlFiles.Length} XML files to process.");

                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };
                    await Parallel.ForEachAsync(xmlFiles, parallelOptions, async (filePath, ct) =>
                    {
                        await ProcessFileAsync(filePath, ct);
                    });

                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during file processing cycle.");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task ProcessFileAsync(string filePath, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation($"Starting processing of file: {filePath}.");

                var fileBytes = await File.ReadAllBytesAsync(filePath, ct);

                using var ms = new MemoryStream(fileBytes);
                var parsedInstrumentStatus = _serializer.Deserialize(ms) as InstrumentStatusDto
                    ?? throw new SerializationException("Provided XML does not match InstrumentStatusDto schema.");

                var validationResult = InstrumentStatusValidator.ValidateInstrumentStatusDto(parsedInstrumentStatus);

                if (!validationResult.IsValid)
                {
                    throw new SerializationException(validationResult.Message);
                }
                _logger.LogInformation("Successfully validated PackageID: {PackageID}", parsedInstrumentStatus.PackageID);

                _logger.LogInformation($"Processing package {parsedInstrumentStatus.PackageID} from file {filePath}.");

                foreach (var device in parsedInstrumentStatus.Devices)
                {
                    _logger.LogInformation($"Processing device {device.ModuleCategoryId} {device.IndexWithinRole}.");

                    if (string.IsNullOrEmpty(device.RapidControlStatusXml))
                    {
                        _logger.LogWarning($"RapidControlStatusXml property for device {device.ModuleCategoryId} {device.IndexWithinRole} was empty.");
                        continue;
                    }

                    var rapidControlDoc = XDocument.Parse(device.RapidControlStatusXml);

                    var moduleStateElement = rapidControlDoc.Descendants("ModuleState").FirstOrDefault();

                    if (moduleStateElement != null)
                    {
                        _logger.LogInformation($"Previous value of ModuleState for {device.ModuleCategoryId} {device.IndexWithinRole}: {moduleStateElement.Value}.");

                        var states = new[] { "Online", "Run", "NotReady", "Offline" };

                        var newValue = states[Random.Shared.Next(states.Length)];

                        moduleStateElement.Value = newValue;

                        _logger.LogInformation($"New value of ModuleState for {device.ModuleCategoryId} {device.IndexWithinRole}: {newValue}.");
                    }

                    device.RapidControlStatusXml = rapidControlDoc.ToString();
                }

                var instrumentStatusJson = JsonSerializer.SerializeToUtf8Bytes(parsedInstrumentStatus);

                await _publishRetryPolicy.ExecuteAsync(async () =>
                {
                    await using var channel = await _connection.CreateChannelAsync();
                    await channel.BasicPublishAsync(exchange: "", routingKey: _queueName, body: instrumentStatusJson);
                });

                _logger.LogInformation($"Published processed data from {filePath} to RabbitMQ queue {_queueName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file {filePath}.");
            }
        }
    }
}
