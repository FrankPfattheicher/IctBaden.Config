using System.Collections.Generic;
using IctBaden.Config.Unit;

namespace IctBaden.Config.ValueLists
{
    public class InMemoryValueListProvider : IValueListProvider
    {
        private readonly List<SelectionValue> _values;
        
        public InMemoryValueListProvider(List<SelectionValue> selectionValues)
        {
            _values = selectionValues;
        }

        public List<SelectionValue> GetSelectionValues() => _values;
        
    }
}