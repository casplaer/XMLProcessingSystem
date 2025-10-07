using FileParserService.Common.Models;
using FileParserService.Dto;
using System.Xml;
using System.Xml.Linq;

namespace FileParserService.Common.Helpers
{
    public static class InstrumentStatusValidator
    {
        public static ValidationResult ValidateInstrumentStatusDto(InstrumentStatusDto instrumentStatusDto)
        {
            var errors = new List<string>();

            if (instrumentStatusDto == null)
            {
                errors.Add("InstrumentStatusDto is null.");
            }
            else
            {
                if (string.IsNullOrEmpty(instrumentStatusDto.PackageID))
                {
                    errors.Add("Missing required PackageID element.");
                }
                if (instrumentStatusDto.Devices == null || !instrumentStatusDto.Devices.Any())
                {
                    errors.Add("Devices collection is empty or missing.");
                }
                else
                {
                    foreach (var device in instrumentStatusDto.Devices)
                    {

                        if (string.IsNullOrEmpty(device.ModuleCategoryId)) errors.Add("A device is missing ModuleCategoryId.");
                        if (device.IndexWithinRole == null) errors.Add("A device is missing IndexWithinRole.");
                        if (string.IsNullOrEmpty(device.RapidControlStatusXml)) errors.Add("A device is missing RapidControlStatusXml.");
                        else
                        {
                            try
                            {
                                if (!XDocument.Parse(device.RapidControlStatusXml).Descendants("ModuleState").Any())
                                {
                                    errors.Add("A device is missing ModuleState in RapidControlStatusXml.");
                                }
                            }
                            catch (XmlException ex)
                            {
                                errors.Add($"Invalid RapidControlStatusXml format: {ex.Message}");
                            }
                        }

                    }
                }
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }
    }
}
