using System;
using IctBaden.Config.Namespace;
using Microsoft.Extensions.Logging;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Config.Test;

public class NamespaceTests
{
    private readonly ILogger _logger = Framework.Logging.Logger.DefaultFactory.CreateLogger("Test");
        
    [Fact]
    public void NoSpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "");
        Assert.Null(provider);
    }

    [Fact]
    public void InvalidSpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "file:");
        Assert.Null(provider);
    }

    [Fact]
    public void InvalidSchema()
    {
        var caught = false;
        try
        {
            var provider = NamespaceProviderFactory.Create(_logger, "nothing://spec");
            Assert.Null(provider);
            Assert.Fail("Missing ArgumentException");
        }
        catch (ArgumentException)
        {
            caught = true;
        }

        Assert.True(caught);
    }

    [Fact]
    public void SqlServerSpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "sql://Data Source=localhost\\SQLEXPRESS;Integrated Security=SSPI;");
        Assert.NotNull(provider);
        Assert.Equal(typeof(NamespaceProviderSqlServer), provider.GetType());
    }

    [Fact]
    public void MongoDbSpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "mongo://mongodb:27017");
        Assert.NotNull(provider);
        Assert.Equal(typeof(NamespaceProviderMongoDb), provider.GetType());
    }

    [Fact]
    public void ProfileSpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "file://Test.ini");
        Assert.NotNull(provider);
        Assert.Equal(typeof(NamespaceProviderProfile), provider.GetType());
    }

    [Fact]
    public void MemorySpecification()
    {
        var provider = NamespaceProviderFactory.Create(_logger, "memory://");
        Assert.NotNull(provider);
        Assert.Equal(typeof(NamespaceProviderMemory), provider.GetType());
    }
}