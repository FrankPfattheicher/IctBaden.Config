using System.IO;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using Xunit;

namespace IctBaden.Config.Test;

public class ReadConfigTests
{
    private readonly ConfigurationSession _session = new ConfigurationSession();
    private readonly string _unitsJson;

    public ReadConfigTests()
    {
        _unitsJson = TestResources.LoadResourceString("ConfigSettings.json");
        Assert.NotNull(_unitsJson);
    }

    [Fact]
    public void JsonDefinitionShouldBeLoadable()
    {
        var serializer = new ConfigurationNamespaceJsonSerializer(_session);
        var root = serializer.Load(new StringReader(_unitsJson));
        
        Assert.Equal(SelectionType.None, root.Selection);
        Assert.Equal(1, root.Children.Count);
    }
    
    
}