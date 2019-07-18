using System;
using System.Collections.Generic;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace
{
  public abstract class NamespaceProvider
  {
    public virtual string GetNewUserId()
    {
      return Guid.NewGuid().ToString("N").ToUpper();
    }

    public abstract IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit);

    public abstract T GetValue<T>(ConfigurationUnit unit, T defaultValue);
    public abstract void SetValue<T>(ConfigurationUnit unit, T newValue);

    public abstract void AddUserUnit(ConfigurationUnit unit);
    public abstract void RemoveUserUnit(ConfigurationUnit unit);

    public virtual List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
    {
      return new List<SelectionValue>();
    }

    protected void SignalWaiting(bool isWaiting)
    {
      var handler = Waiting;
      if(handler == null)
        return;
      handler(isWaiting);
    }

    public event Action<bool> Waiting;
  }
}