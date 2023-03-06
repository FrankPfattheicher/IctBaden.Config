using System;
using System.Collections.Generic;
using System.Linq;

namespace IctBaden.Config.Unit
{
    public class SelectionValueCollection : List<SelectionValue>
    {
        //TODO: aggregate instead of derivate
        private readonly List<SelectionValue> _values;

        public SelectionValueCollection()
        {
            _values = this;
        }

        public SelectionValue? this[string value] => 
            _values.FirstOrDefault(v => v.Value == value);

        public static explicit operator SelectionValueCollection(string selValStr)
        {
            var elements = selValStr.Split(new[] { ";" }, StringSplitOptions.None);
            var coll = new SelectionValueCollection();
            for (var ix = 0; ix < (elements.Length - 1); ix += 2)
            {
                coll.Add(new SelectionValue { Value = elements[ix], DisplayText = elements[ix + 1] });
            }
            return coll;
        }

        public new int Count => _values.Count;

        public static explicit operator string(SelectionValueCollection selValCol)
        {
            return selValCol.ToString();
        }

        public override string ToString()
        {
            var text = new List<string>();

            foreach (var selectionValue in _values)
            {
                if (selectionValue.Value != null) text.Add(selectionValue.Value);
                if (selectionValue.DisplayText != null) text.Add(selectionValue.DisplayText);
            }

            return string.Join(";", text.ToArray());
        }

        internal new void Add(SelectionValue selectionValue)
        {
            _values.Add(selectionValue);
        }

        internal new void AddRange(IEnumerable<SelectionValue> selectionValues)
        {
            foreach (var selectionValue in selectionValues)
            {
                if(_values.Any(sv => sv.Value == selectionValue.Value)) continue;
                Add(selectionValue);
            }
        }
        
    }
}
