using System;
using System.IO;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using Xunit;

namespace IctBaden.Config.Test;

public class SerializerXmlTests
{
    private readonly string _testSettings = TestResources.LoadResourceString("test_settings.xaml");
    private const string SchemaName = "SettingsXml.schema";

    [Fact]
    public void Load()
    {
        var session = new ConfigurationSession();
        var serializer = new ConfigurationNamespaceXamlSerializer(session);
        var root = serializer.Load(new StringReader(_testSettings));
        Assert.NotNull(root);
    }

    [Fact]
    public void DescriptionShouldBeLoadedFromAttributeAndEmbeddedElement()
    {
        var session = new ConfigurationSession();
        var serializer = new ConfigurationNamespaceXamlSerializer(session);
        var root = serializer.Load(new StringReader(_testSettings));
        Assert.Equal("DescriptionAsAttribute", root.Children.ToArray()[0].Description);
        Assert.Equal("DescriptionAsEmbedded", root.Children.ToArray()[1].Description);
    }

    [Fact]
    public void LoadInvalid()
    {
        var invalid = _testSettings.Replace("Id=\"Sequence\"", "Id=\"Sequence");
        var caught = false;
        try
        {
            var session = new ConfigurationSession();
            var serializer = new ConfigurationNamespaceXamlSerializer(session);
            var root = serializer.Load(new StringReader(invalid));
            Assert.Null(root);
            Assert.Fail("Missing FormatException");
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
        var serializer = new ConfigurationNamespaceXamlSerializer(session);
        var root = serializer.Load(new StringReader(_testSettings));
        using (var wrt = new StreamWriter(SchemaName))
        {
            serializer.Save(root, wrt);
        }

        Assert.True(File.Exists(SchemaName));
        var xml = File.ReadAllText(SchemaName);
        Assert.StartsWith("<ConfigurationUnit ", xml);
    }
}