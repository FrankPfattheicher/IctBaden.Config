using System;
using System.IO;
using IctBaden.Config.Unit;
using Newtonsoft.Json;

namespace IctBaden.Config.Namespace
{
    public static class ConfigurationNamespaceJsonSerializer
    {
        public static ConfigurationUnit Load(TextReader reader)
        {
            ConfigurationUnit root;
            try
            {
                var json = reader.ReadToEnd();
                root = JsonConvert.DeserializeObject<ConfigurationUnit>(json);
                if (root != null)
                    ResolveParents(root);
            }
            catch (Exception e)
            {
                throw new FormatException("Invalid JSON schema definition: " + e.Message);
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
