using System;
using System.Collections.Generic;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace
{
    public abstract class NamespaceProvider
    {
        public abstract string GetPersistenceInfo();
        public abstract bool Connect();

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        public virtual string GetNewUserId()
        {
            return Guid.NewGuid().ToString("N").ToUpper();
        }

        public abstract IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit);

        public abstract T GetValue<T>(ConfigurationUnit unit, T defaultValue);
        public abstract void SetValue<T>(ConfigurationUnit unit, T newValue);

        public abstract void AddUserUnit(ConfigurationUnit unit);
        public abstract void RemoveUserUnit(ConfigurationUnit unit);

        /// <summary>
        /// Finally delete item data in repository
        /// </summary>
        /// <param name="unit"></param>
        public abstract void DeleteUserUnit(ConfigurationUnit unit);
        

        public virtual List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            return new List<SelectionValue>();
        }

        protected void SignalWaiting(bool isWaiting)
        {
            Waiting?.Invoke(isWaiting);
        }

        public event Action<bool> Waiting;
    }
}