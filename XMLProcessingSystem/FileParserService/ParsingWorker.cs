using FileParserService.Dto;
using RabbitMQ.Client;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FileParserService
{
    public class ParsingWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly string _queueName = "modules";
        private readonly string _inputDirectory = Directory.GetCurrentDirectory();
        private readonly XmlSerializer _serializer;
        private readonly ILogger<ParsingWorker> _logger;

        public ParsingWorker(
            IConnection connection, 
            XmlSerializer serializer,
            ILogger<ParsingWorker> logger)
        {
            _connection = connection;
            _serializer = serializer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var setupChannel = await _connection.CreateChannelAsync();
            await setupChannel.QueueDeclareAsync(queue: _queueName, 
                                                 durable: true, 
                                                 exclusive: false, 
                                                 autoDelete: false);

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
                        var rnd = new Random();

                        moduleStateElement.Value = states[rnd.Next(states.Length)];

                        _logger.LogInformation($"New value of ModuleState for {device.ModuleCategoryId} {device.IndexWithinRole}: {moduleStateElement.Value}.");
                    }

                    device.RapidControlStatusXml = rapidControlDoc.ToString();
                }

                var instrumentStatusJson = JsonSerializer.Serialize(parsedInstrumentStatus);

                await using var channel = await _connection.CreateChannelAsync();
                var body = Encoding.UTF8.GetBytes(instrumentStatusJson);

                await channel.BasicPublishAsync(exchange: "", routingKey: _queueName, body: body);

                _logger.LogInformation($"Published processed data from {filePath} to RabbitMQ queue {_queueName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file {filePath}.");
            }
        }
    }
}
