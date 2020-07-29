using System;
using IctBaden.Config.Namespace;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Config.Test
{
    public class NamespaceTests
    {
        [Fact]
        public void NoSpecification()
        {
            var provider = NamespaceProviderFactory.Create("");
            Assert.Null(provider);
        }

        [Fact]
        public void InvalidSpecification()
        {
            var provider = NamespaceProviderFactory.Create("file:");
            Assert.Null(provider);
        }

        [Fact]
        public void InvalidSchema()
        {
            var caught = false;
            try
            {
                var provider = NamespaceProviderFactory.Create("nothing://spec");
                Assert.Null(provider);
                Assert.True(false, "Missing ArgumentException");
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
            var provider = NamespaceProviderFactory.Create("sql://Data Source=localhost\\SQLEXPRESS;Integrated Security=SSPI;");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderSqlServer), provider.GetType());
        }

        [Fact]
        public void MongoDbSpecification()
        {
            var provider = NamespaceProviderFactory.Create("mongo://mongodb:27017");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderMongoDb), provider.GetType());
        }

        [Fact]
        public void ProfileSpecification()
        {
            var provider = NamespaceProviderFactory.Create("file://Test.ini");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderProfile), provider.GetType());
        }

        [Fact]
        public void MemorySpecification()
        {
            var provider = NamespaceProviderFactory.Create("memory://");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderMemory), provider.GetType());
        }
    }
}