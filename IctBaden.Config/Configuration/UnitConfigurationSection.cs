using System.Collections.Generic;
using System.Linq;
using IctBaden.Config.Unit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace IctBaden.Config.Configuration
{
    public class UnitConfigurationSection : IConfigurationSection
    {
        private readonly ConfigurationUnit? _unit;

        public UnitConfigurationSection(ConfigurationUnit? cfgUnit)
        {
            _unit = cfgUnit;
        }
        
        public IConfigurationSection? GetSection(string key)
        {
            var child = _unit?.GetUnitById(key);
            return child == null 
                ? null 
                : new UnitConfigurationSection(child);
        }

        public IEnumerable<IConfigurationSection>? GetChildren() => _unit?.Children
                .Select(ch => new UnitConfigurationSection(ch));

        public IChangeToken GetReloadToken()
        {
            return new UnitReloadToken(_unit);
        }

        public string this[string key]
        {
            get => _unit?.GetPropertyValue<string>(key) ?? string.Empty;
            set => _unit?.SetPropertyValue(key, value);
        }

        public string Key => _unit?.Id ?? string.Empty;
        public string Path => _unit?.FullId ?? string.Empty;
        public string Value 
        { 
            get =>_unit?.GetValue<string>() ?? string.Empty;
            set => _unit?.SetValue(value);
        }
        
    }
}