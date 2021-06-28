using System.Collections.Generic;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using IctBaden.Config.ValueLists;
using Xunit;

namespace IctBaden.Config.Test
{
    public class SelectionValueTests
    {
        private readonly ConfigurationSession _session = new ConfigurationSession();

        public SelectionValueTests()
        {
            var unit = new ConfigurationUnit
            {
                Id = "test",
                Selection = SelectionType.ListOnly,
                ValueSourceId = "testValues"
            };
            
            _session.Namespace.AddChild(unit);
            _session.RegisterValueListProvider("testValues", new InMemoryValueListProvider(new List<SelectionValue>
            {
                new SelectionValue { Value = "111", DisplayText = "Eins Eins Eins" },
                new SelectionValue { Value = "222", DisplayText = "Zwei Zwei Zwei" }
            }));
        }

        [Fact]
        public void RegisteredSelectionValueListShouldBeResolved()
        {
            var unit = _session.Namespace.GetUnitById("test");

            var valueList = unit.ValueList;
            Assert.Equal(2, valueList.Count);
        }
        
    }
}