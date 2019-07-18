using System;
using System.IO;
using IctBaden.Config.Namespace;
using Xunit;

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
        public void DbSpecification()
        {
            var provider = NamespaceProviderFactory.Create("db://Data Source=localhost\\SQLEXPRESS;Integrated Security=SSPI;");
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderDatabase), provider.GetType());
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

        private static readonly string SchemaName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test.schema");

        [Fact]
        public void SchemaSpecification()
        {
            var provider = NamespaceProviderFactory.Create("schema://" + SchemaName);
            Assert.NotNull(provider);
            Assert.Equal(typeof(NamespaceProviderSchema), provider.GetType());
        }

    }
}