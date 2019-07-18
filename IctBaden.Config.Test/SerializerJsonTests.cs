using System;
using System.IO;
using IctBaden.Config.Namespace;
using Newtonsoft.Json;
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
            var root = ConfigurationNamespaceJsonSerializer.Load(new StringReader(_testSettingsJson));
            Assert.NotNull(root);
        }

        [Fact]
        public void LoadInvalid()
        {
            var invalid = _testSettingsJson.Replace("\"Id\": \"Sequence\"", "\"Id\": \"Sequence");
            var caught = false;
            try
            {
                var root = ConfigurationNamespaceJsonSerializer.Load(new StringReader(invalid));
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

            var root = ConfigurationNamespaceXmlSerializer.Load(new StringReader(_testSettingsXml));
            using (var wrt = new StreamWriter(SchemaName))
            {
                ConfigurationNamespaceJsonSerializer.Save(root, wrt);
            }

            Assert.True(File.Exists(SchemaName));
            var json = File.ReadAllText(SchemaName);
            Assert.StartsWith("{", json);
        }

        [Fact]
        public void FromXmlAndFromJsonShouldBeEqual()
        {
            var fromXml = ConfigurationNamespaceXmlSerializer.Load(new StringReader(_testSettingsXml));
            var fromJson = ConfigurationNamespaceJsonSerializer.Load(new StringReader(_testSettingsJson));

            var jsonFromXml = JsonConvert.SerializeObject(fromXml);
            var jsonFromJson = JsonConvert.SerializeObject(fromJson);
            Assert.Equal(jsonFromXml, jsonFromJson);
        }

    }
}
