using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using IctBaden.Config.Namespace;
using IctBaden.Config.Unit;
using IctBaden.Config.ValueLists;
using Microsoft.Extensions.Logging;

// ReSharper disable StringLiteralTypo

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Config.Session;

public class ConfigurationSession
{
    private ILogger? _logger;
    private readonly Dictionary<string, NamespaceProvider> _namespaceProviders;
    private readonly Dictionary<string, IValueListProvider> _valueListProviders;
    public ConfigurationSessionUnit UnitTypes { get; private set; }
    public ConfigurationSessionUnit Namespace { get; private set; }
    public string CurrentUser { get; private set; }
    public int CurrentUserLevel { get; private set; }

    private ConfigurationUnit? _folder;

    /// <summary>
    /// This is the folder unit used to group
    /// user items into hierarchical folders.
    /// If there is a unit in the current namespace
    /// with Id="Folder", that is used.
    /// Otherwise a default unit is returned. 
    /// </summary>
    public ConfigurationUnit Folder
    {
        get
        {
            _folder = Namespace.GetUnitById("Folder");
            if (_folder != null)
            {
                _folder.DataType = TypeCode.Object;
                _folder.Selection = SelectionType.ParentHierarchical;
                return _folder;
            }

            _folder = new ConfigurationUnit
            {
                Id = "Folder",
                DisplayName = "Ordner",
                DisplayImage = "fa fa-folder",
                Description = "Ordner zur Organisation von Elementen",
                DataType = TypeCode.Object,
                Selection = SelectionType.ParentHierarchical
            };
            return _folder;
        }
    }

    // ReSharper disable EventNeverInvoked
    public event Action<bool> Waiting = _ => { };
    // ReSharper restore EventNeverInvoked

    public event Action<ConfigurationUnit> Changed = _ => { };
    [Obsolete("Use Changed event instead")]
    public event PropertyChangedEventHandler PropertyChanged = (_, _) => { };

    public ConfigurationSession() : this(null) // no logging
    {
    }
            
    public ConfigurationSession(ILogger? logger)
    {
        _logger = logger;
        _namespaceProviders = new Dictionary<string, NamespaceProvider>();
        _valueListProviders = new Dictionary<string, IValueListProvider>();
        UnitTypes = new ConfigurationSessionUnit(this);
        Namespace = new ConfigurationSessionUnit(this);
        CurrentUser = string.Empty; 
        CurrentUserLevel = 99;
    }

    public ConfigurationSession Clone()
    {
        var clone = new ConfigurationSession
        {
            _logger = _logger,
            UnitTypes = UnitTypes, 
            Namespace = Namespace,
            CurrentUserLevel = CurrentUserLevel
        };
        foreach (var keyValue in _namespaceProviders)
        {
            clone._namespaceProviders.Add(keyValue.Key, keyValue.Value);
        }
        foreach (var keyValue in _valueListProviders)
        {
            clone._valueListProviders.Add(keyValue.Key, keyValue.Value);
        }
        return clone;
    }

        
    internal void ResolveUnitTypesAndParents(ConfigurationUnit item)
    {
        if (!string.IsNullOrEmpty(item.UnitTypeId))
        {
            var unitType = UnitTypes.GetUnitById(item.UnitTypeId);
            if (!unitType.IsEmpty)
            {
                var typeItem = unitType.Clone(unitType.DisplayName, true);
                typeItem.Id = item.Id;
                typeItem.Parent = item.Parent;
                if(unitType.Category != null) typeItem.Category = unitType.Category;
                typeItem.DisplayName = unitType.DisplayName;
                if(unitType.Description != null) typeItem.Description = unitType.Description;

                if (item.Parent != null)
                {
                    item.Parent.Children = item.Parent.Children
                        .Select(c => c == item ? typeItem : c)
                        .ToList();
                }
            }
        }

        foreach (var i in item.Children)
        {
            i.Parent = item;
            ResolveUnitTypesAndParents(i);
        }
    }


    public void RegisterNamespaceProvider(string name, NamespaceProvider namespaceProvider)
    {
        _namespaceProviders[name] = namespaceProvider;
        namespaceProvider.Waiting += isWaiting => Waiting(isWaiting);
    }

    public void RegisterValueListProvider(string name, IValueListProvider valueListProvider)
    {
        _valueListProviders[name] = valueListProvider;
    }
        
    // ReSharper disable once UnusedParameter.Global
    public bool Login(string user, string password)
    {
        CurrentUser = user;
        return false;
    }

    public void SetUserLevel(int level)
    {
        CurrentUserLevel = level;
    }

    public NamespaceProvider? GetNamespaceProvider(string? namespaceProvider)
    {
        if ((namespaceProvider == null) || !_namespaceProviders.ContainsKey(namespaceProvider))
            return null;
        return _namespaceProviders[namespaceProvider];
    }

    public IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
    {
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        var children = (provider == null)
            ? new List<ConfigurationUnit>()
            : provider.GetChildren(unit);
        return children;
    }

    private void SignalConfigurationUnitChanged(ConfigurationUnit unit)
    {
        Changed.Invoke(unit);
#pragma warning disable 618
        PropertyChanged.Invoke(unit, new PropertyChangedEventArgs(unit.Id));
#pragma warning restore 618
    }

    public T? GetValue<T>(ConfigurationUnit unit, T defaultValue)
    {
        var provider = GetNamespaceProvider(unit.Parent?.NamespaceProvider);
        if (provider == null)
            return defaultValue;

        var value = provider.GetValue(unit, defaultValue);
        return value;
    }

    public void SetValue<T>(ConfigurationUnit unit, T newValue)
    {
        var provider = GetNamespaceProvider(unit.Parent?.NamespaceProvider);
        if (provider == null)
            return;

        provider.SetValue(unit, newValue);
        SignalConfigurationUnitChanged(unit);
    }

    public void AddUserUnit(ConfigurationUnit unit)
    {
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        if (provider == null)
            return;

        provider.AddUserUnit(unit);
        SignalConfigurationUnitChanged(unit);
    }

    public void RemoveUserUnit(ConfigurationUnit unit)
    {
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        if (provider == null)
            return;

        provider.RemoveUserUnit(unit);
        SignalConfigurationUnitChanged(unit);
    }

    public void DeleteUserUnit(ConfigurationUnit unit)
    {
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        if (provider == null)
            return;

        provider.DeleteUserUnit(unit);
        SignalConfigurationUnitChanged(unit);
    }

    public List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
    {
        var source = unit.ValueSourceId!;
        if (_valueListProviders.TryGetValue(source, out var listProvider))
        {
            return listProvider.GetSelectionValues();
        }
            
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        return (provider == null) ? new List<SelectionValue>() : provider.GetSelectionValues(unit);
    }

    public string GetNewUserId(ConfigurationUnit unit)
    {
        var provider = GetNamespaceProvider(unit.NamespaceProvider);
        return (provider == null) ? Guid.NewGuid().ToString("N").ToUpper() : provider.GetNewUserId();
    }
}