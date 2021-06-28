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
            var provider = NamespaceProviderFactory.Create(null, "");
            Assert.Null(provider);
        }

        [Fact]
        public void InvalidSpecification()
        {
            var provider = NamespaceProviderFactory.Create(null, "file:");
            Assert.Null(provider);
        }

        [Fact]
        public void InvalidSchema()
        {
            var caught = false;
            try
            {
                var provider = NamespaceProviderFactory.Create(null, "nothing://spec");
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
            var provider = NamespaceProviderFactory.Create(null, "sql://Data Source=localhost\\SQLEXPRESS;Integrated Security=SSPI;");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderSqlServer), provider.GetType());
        }

        [Fact]
        public void MongoDbSpecification()
        {
            var provider = NamespaceProviderFactory.Create(null, "mongo://mongodb:27017");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderMongoDb), provider.GetType());
        }

        [Fact]
        public void ProfileSpecification()
        {
            var provider = NamespaceProviderFactory.Create(null, "file://Test.ini");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderProfile), provider.GetType());
        }

        [Fact]
        public void MemorySpecification()
        {
            var provider = NamespaceProviderFactory.Create(null, "memory://");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderMemory), provider.GetType());
        }
    }
}