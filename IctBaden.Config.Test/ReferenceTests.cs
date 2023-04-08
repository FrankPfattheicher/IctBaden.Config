using System;
using System.IO;
using System.Linq;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using Xunit;

namespace IctBaden.Config.Test
{
    public class ReferenceTests
    {
        private readonly ConfigurationSession _session = new ConfigurationSession();

        public ReferenceTests()
        {
            var unitsJson = TestResources.LoadResourceString("ConfigTestUnits.json");
            var serializer = new ConfigurationNamespaceJsonSerializer(_session);
            var root = serializer.Load(new StringReader(unitsJson));
            _session.Namespace.AddChildren(root.Children); 
        }

        [Fact]
        public void TargetTemplatesShouldBeTwo()
        {
            var unit = _session.Namespace.GetUnitById("Targets");
            var templates = unit.Templates;
            Assert.NotNull(templates);
            Assert.Equal(2, templates.Count());
        }
        
        [Fact]
        public void FilterTemplatesShouldBeOne()
        {
            var unit = _session.Namespace.GetUnitById("MessageFilters");
            var templates = unit.Templates;
            Assert.NotNull(templates);
            Assert.Single(templates);
        }

        [Fact]
        public void UnitTemplatesTargetFilterShouldBeThree()
        {
            var unit = new ConfigurationSessionUnit(_session)
            {
                Selection = SelectionType.Reference,
                ValueSourceUnitIds = "Targets;MessageFilters"
            };
            var templates = unit.BaseUnitsForReferenceUnits
                .SelectMany(bu => bu.Templates ?? Array.Empty<ConfigurationUnit>());
            Assert.Equal(3, templates.Count());
        }
        
    }
}