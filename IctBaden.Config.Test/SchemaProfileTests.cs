using System;
using System.IO;
using IctBaden.Config.Schema;
using Xunit;

namespace IctBaden.Config.Test
{
    public class SchemaProfileTests
    {
        private static readonly string ProfileName = 
            Path.Combine(Path.GetDirectoryName(typeof(SchemaProfileTests).Assembly.Location)!, "SchemaProfile.cfg");

        [Fact]
        public void Create()
        {
            var schema = SchemaFromProfile.Create(ProfileName);

            Assert.NotNull(schema);
            Assert.Equal(ProfileName, schema.Description);
            Assert.Equal(3, schema.Children.Count);

            Assert.Equal("ProfileSection1", schema.Children[1].Id);
            Assert.Equal("ProfileSection2", schema.Children[2].Id);

            var profileSection1 = schema.Children[1];
            Assert.Equal(3, profileSection1.Children.Count);

            Assert.Equal(TypeCode.Int64, profileSection1.Children[0].DataType);
            Assert.Equal(TypeCode.String, profileSection1.Children[1].DataType);
            Assert.Equal(TypeCode.String, profileSection1.Children[2].DataType);

            var profileSection2 = schema.Children[2];
            Assert.Single(profileSection2.Children);

            Assert.Equal(TypeCode.Double, profileSection2.Children[0].DataType);
        }
    }
}