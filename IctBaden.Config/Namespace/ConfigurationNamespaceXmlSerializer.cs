using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace
{
    public static class ConfigurationNamespaceXmlSerializer
    {
        public static ConfigurationUnit Load(TextReader reader)
        {
            ConfigurationUnit root;
            try
            {
                using (var rdr = XmlReader.Create(reader))
                {
                    root = (ConfigurationUnit)new XmlSerializer(typeof(ConfigurationUnit)).Deserialize(rdr);
                    rdr.Close();
                }
                if (root != null)
                    ResolveParents(root);
            }
            catch (Exception e)
            {
                throw new FormatException("Invalid XML schema definition: " + e.Message);
            }
            return root;
        }

        private static void ResolveParents(ConfigurationUnit item)
        {
            foreach (var i in item.Children)
            {
                i.Parent = item;
                ResolveParents(i);
            }
        }

        public static void Save(ConfigurationUnit rootUnit, TextWriter writer)
        {
            var serializer = new XmlSerializer(typeof(ConfigurationUnit));
            serializer.Serialize(writer, rootUnit);
        }

    }
}
