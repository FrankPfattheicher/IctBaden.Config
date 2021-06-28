using System.Collections.Generic;
using IctBaden.Config.Unit;

namespace IctBaden.Config.ValueLists
{
    public interface IValueListProvider
    {
        List<SelectionValue> GetSelectionValues();
    }
}