using FileParserService.Dto;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FileParserService
{
    public class ParsingWorker : BackgroundService
    {
        private readonly ILogger<ParsingWorker> _logger;

        public ParsingWorker(ILogger<ParsingWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            XmlSerializer serializer = new(typeof(InstrumentStatusDto));

            while (!stoppingToken.IsCancellationRequested)
            {
                var fileBytes = File.ReadAllBytes("status.xml");

                var ms = new MemoryStream(fileBytes);

                var parsedInstrumentStatus = serializer.Deserialize(ms) as InstrumentStatusDto
                        ?? throw new SerializationException("Provided XML does not match InstrumentStatusDto schema.");

                _logger.LogInformation($"Processing package {parsedInstrumentStatus.PackageID}.");

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

                        var states = new[]{ "Online", "Run", "NotReady", "Offline" };
                        var rnd = new Random();

                        moduleStateElement.Value = states[rnd.Next(states.Length)];

                        _logger.LogInformation($"New value of ModuleState for {device.ModuleCategoryId} {device.IndexWithinRole}: {moduleStateElement.Value}.");
                    }

                    device.RapidControlStatusXml = rapidControlDoc.ToString();
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
