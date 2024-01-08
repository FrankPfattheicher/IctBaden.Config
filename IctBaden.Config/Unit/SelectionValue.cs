using System.Xml.Serialization;

namespace IctBaden.Config.Unit
{
    public class SelectionValue
    {
        [XmlAttribute]
        public string? Value { get; set; }
        [XmlAttribute]
        public string? DisplayText { get; set; }

        public override string ToString()
        {
            return DisplayText ?? string.Empty;
        }
    }
}
