using System;
using System.Collections.Generic;
using System.Linq;

namespace IctBaden.Config.Unit
{
    public class SelectionValueCollection : List<SelectionValue>
    {
        public SelectionValue this[string value]
        {
            get { return this.FirstOrDefault(v => v.Value == value); }
        }

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

        public static explicit operator string(SelectionValueCollection selValCol)
        {
            return selValCol.ToString();
        }

        public override string ToString()
        {
            var text = new List<string>();

            foreach (var selectionValue in this)
            {
                text.Add(selectionValue.Value);
                text.Add(selectionValue.DisplayText);
            }

            return string.Join(";", text.ToArray());
        }
    }
}
