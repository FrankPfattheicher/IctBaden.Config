using System;
using System.IO;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using Newtonsoft.Json;

namespace IctBaden.Config.Namespace
{
    public class ConfigurationNamespaceJsonSerializer : IConfigurationNamespaceSerializer
    {
        private readonly ConfigurationSession _session;

        public ConfigurationNamespaceJsonSerializer(ConfigurationSession session)
        {
            _session = session;
        }
        public ConfigurationUnit Load(TextReader reader)
        {
            ConfigurationUnit root;
            try
            {
                var json = reader.ReadToEnd();
                root = JsonConvert.DeserializeObject<ConfigurationUnit>(json);
                _session.ResolveUnitTypesAndParents(root);
            }
            catch (Exception ex)
            {
                throw new FormatException("Invalid JSON schema definition: " + ex.Message);
            }
            return root;
        }

        // ReSharper disable once UnusedMember.Global
        public void Save(ConfigurationUnit rootUnit, TextWriter writer)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(rootUnit, settings);
            writer.Write(json);
        }

    }
}
