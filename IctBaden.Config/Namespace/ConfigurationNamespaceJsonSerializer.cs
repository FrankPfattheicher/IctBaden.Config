using System;
using System.IO;
using System.Text.Json;
// ReSharper disable once RedundantUsingDirective
using System.Text.Json.Serialization;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace;

public class ConfigurationNamespaceJsonSerializer(ConfigurationSession session) : IConfigurationNamespaceSerializer
{
    public ConfigurationUnit Load(TextReader reader)
    {
        ConfigurationUnit root;
        try
        {
            var json = reader.ReadToEnd();
            root = JsonSerializer.Deserialize<ConfigurationUnit>(json) ?? new ConfigurationUnit();
            session.ResolveUnitTypesAndParents(root);
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
        var settings = new JsonSerializerOptions
        {
            WriteIndented = true,
#if NET6_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
#else
            IgnoreNullValues = true
#endif
        };
        var json = JsonSerializer.Serialize(rootUnit, settings);
        writer.Write(json);
    }

}