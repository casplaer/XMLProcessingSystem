using System.Xml.Serialization;

namespace FileParserService.Dto
{
    public class DeviceStatusDto
    {
        [XmlElement("ModuleCategoryID")]
        public string? ModuleCategoryId { get; set; }

        [XmlElement("IndexWithinRole")]
        public int? IndexWithinRole { get; set; }

        [XmlElement("RapidControlStatus")]
        public string? RapidControlStatusXml { get; set; }
    }
}
