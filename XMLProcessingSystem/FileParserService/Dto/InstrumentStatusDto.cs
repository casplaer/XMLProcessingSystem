using System.Xml.Serialization;

namespace FileParserService.Dto
{
    [XmlRoot("InstrumentStatus")]
    public class InstrumentStatusDto
    {
        [XmlElement("PackageID")]
        public string PackageID { get; set; }

        [XmlElement("DeviceStatus")]
        public List<DeviceStatusDto> Devices { get; set; } = new();
    }
}
