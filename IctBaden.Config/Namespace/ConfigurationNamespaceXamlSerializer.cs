using System;
using System.IO;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using Portable.Xaml;

namespace IctBaden.Config.Namespace
{
    public class ConfigurationNamespaceXamlSerializer : IConfigurationNamespaceSerializer
    {
        private readonly ConfigurationSession _session;

        public ConfigurationNamespaceXamlSerializer(ConfigurationSession session)
        {
            _session = session;
        }
        public ConfigurationUnit Load(TextReader reader)
        {
            ConfigurationUnit root;
            try
            {
                root = (ConfigurationUnit) XamlServices.Load(reader);
                _session.ResolveUnitTypesAndParents(root);
            }
            catch (Exception e)
            {
                throw new FormatException("Invalid XAML schema definition: " + e.Message);
            }
            return root;
        }

        public void Save(ConfigurationUnit rootUnit, TextWriter writer)
        {
            XamlServices.Save(writer, rootUnit);
        }

    }
}
