using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace
{
    public class ConfigurationNamespaceXmlSerializer : IConfigurationNamespaceSerializer
    {
        private readonly ConfigurationSession _session;

        public ConfigurationNamespaceXmlSerializer(ConfigurationSession session)
        {
            _session = session;
        }
        public ConfigurationUnit Load(TextReader reader)
        {
            ConfigurationUnit root;
            try
            {
                using (var rdr = XmlReader.Create(reader))
                {
                    root = (ConfigurationUnit)new XmlSerializer(typeof(ConfigurationUnit)).Deserialize(rdr);
                    rdr.Close();
                }
                _session.ResolveUnitTypesAndParents(root);
            }
            catch (Exception e)
            {
                throw new FormatException("Invalid XML schema definition: " + e.Message);
            }
            return root;
        }

        public void Save(ConfigurationUnit rootUnit, TextWriter writer)
        {
            var serializer = new XmlSerializer(typeof(ConfigurationUnit));
            serializer.Serialize(writer, rootUnit);
        }

    }
}
