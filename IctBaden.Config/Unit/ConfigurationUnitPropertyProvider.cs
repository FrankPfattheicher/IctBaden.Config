﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IctBaden.Framework.PropertyProvider;

namespace IctBaden.Config.Unit
{
    public class ConfigurationUnitPropertyProvider : IPropertyProvider
    {
        private readonly ConfigurationUnit unit;

        public ConfigurationUnitPropertyProvider(ConfigurationUnit cfgUnit)
        {
            unit = cfgUnit;
        }

        #region IPropertyProvider Members

        public List<T> GetAll<T>()
        {
            return (from property in unit.Children where property.IsProperty && (property.GetType() == typeof(T)) select property.GetValue<T>()).ToList();
        }

        public T Get<T>(string key)
        {
            var prop = unit.GetUnitById(key);
            return prop.GetValue<T>();
        }

        public T Get<T>(string key, T defaultValue)
        {
            var prop = unit.GetUnitById(key);
            return prop.IsEmpty ? defaultValue : prop.GetValue(defaultValue);
        }

        public void Set<T>(string key, T newValue)
        {
            var prop = ConfigurationUnit.GetProperty(unit, key);
            prop.SetValue(newValue);
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            return !unit.GetUnitById(key).IsEmpty;
        }

        #endregion

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
