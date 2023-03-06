using System;
using System.IO;
using System.Text.Json;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using Xunit;

namespace IctBaden.Config.Test
{
    public class SerializerJsonTests
    {
        private readonly string _testSettingsXml = TestResources.LoadResourceString("test_settings.xaml");
        private readonly string _testSettingsJson = TestResources.LoadResourceString("test_settings.json");
        private const string SchemaName = "SettingsJson.schema";

        [Fact]
        public void Load()
        {
            var session = new ConfigurationSession();
            var serializer = new ConfigurationNamespaceJsonSerializer(session);
            var root = serializer.Load(new StringReader(_testSettingsJson));
            Assert.NotNull(root);
        }

        [Fact]
        public void LoadInvalid()
        {
            var invalid = _testSettingsJson.Replace("\"Id\": \"Sequence\"", "\"Id\": \"Sequence");
            var caught = false;
            try
            {
                var session = new ConfigurationSession();
                var serializer = new ConfigurationNamespaceJsonSerializer(session);
                var root = serializer.Load(new StringReader(invalid));
                Assert.Null(root);
                Assert.True(false, "Missing FormatException");
            }
            catch (FormatException)
            {
                caught = true;
            }
            Assert.True(caught);
        }

        [Fact]
        public void Save()
        {
            if (File.Exists(SchemaName))
            {
                File.Delete(SchemaName);
            }

            var session = new ConfigurationSession();
            var jsonSerializer = new ConfigurationNamespaceJsonSerializer(session);
            var xmlSerializer = new ConfigurationNamespaceXamlSerializer(session);
            var root = xmlSerializer.Load(new StringReader(_testSettingsXml));
            using (var wrt = new StreamWriter(SchemaName))
            {
                jsonSerializer.Save(root, wrt);
            }

            Assert.True(File.Exists(SchemaName));
            var json = File.ReadAllText(SchemaName);
            Assert.StartsWith("{", json);
        }

        [Fact]
        public void FromXmlAndFromJsonShouldBeEqual()
        {
            var session = new ConfigurationSession();
            var jsonSerializer = new ConfigurationNamespaceJsonSerializer(session);
            var xmlSerializer = new ConfigurationNamespaceXamlSerializer(session);
            var fromXml = xmlSerializer.Load(new StringReader(_testSettingsXml));
            var fromJson = jsonSerializer.Load(new StringReader(_testSettingsJson));

            var settings = new JsonSerializerOptions {  WriteIndented = true };
            var jsonFromXml = JsonSerializer.Serialize(fromXml, settings);
            var jsonFromJson = JsonSerializer.Serialize(fromJson, settings);
            Assert.Equal(jsonFromXml, jsonFromJson);
        }

    }
}
