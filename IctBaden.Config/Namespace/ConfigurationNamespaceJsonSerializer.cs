using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var json = "";
            try
            {
                json = reader.ReadToEnd();
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
