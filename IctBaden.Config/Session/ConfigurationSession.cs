using System;
using System.Collections.Generic;
using System.ComponentModel;
using IctBaden.Config.Namespace;
using IctBaden.Config.Unit;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Config.Session
{
    public class ConfigurationSession : INotifyPropertyChanged
    {
        private Dictionary<string, NamespaceProvider> _namespaceProviders;
        public ConfigurationSessionUnit Namespace { get; private set; }
        public string CurrentUser { get; private set; }
        public int CurrentUserLevel { get; private set; }

        private ConfigurationUnit _folder;
        public ConfigurationUnit Folder => _folder 
                                           ?? (_folder = new ConfigurationUnit
                                           {
                                               Id = "Folder", 
                                               DisplayName = "Ordner",
                                               DisplayImage = "fa fa-folder",
                                               DataType = TypeCode.Object,
                                               Selection = SelectionType.ParentHierarchical
                                           });

        // ReSharper disable EventNeverInvoked
        public event Action<bool> Waiting = _ => { };
        // ReSharper restore EventNeverInvoked

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigurationSession()
        {
            _namespaceProviders = new Dictionary<string, NamespaceProvider>();
            Namespace = new ConfigurationSessionUnit(this);
            CurrentUserLevel = 99;
        }

        public ConfigurationSession Clone()
        {
            return new ConfigurationSession { _namespaceProviders = _namespaceProviders, Namespace = Namespace };
        }

        public void RegisterNamespaceProvider(string name, NamespaceProvider namespaceProvider)
        {
            if (_namespaceProviders.ContainsKey(name))
            {
                _namespaceProviders[name] = namespaceProvider;
            }
            else
            {
                _namespaceProviders.Add(name, namespaceProvider);
            }

            namespaceProvider.Waiting += isWaiting => Waiting(isWaiting);
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

        public NamespaceProvider GetNamespaceProvider(string namespaceProvider)
        {
            if ((namespaceProvider == null) || !_namespaceProviders.ContainsKey(namespaceProvider))
                return null;
            return _namespaceProviders[namespaceProvider];
        }

        public IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            //if(string.IsNullOrEmpty(currentUser))
            //  throw new UnauthorizedAccessException("No user logged in");

            var provider = GetNamespaceProvider(unit.NamespaceProvider);
            var children = (provider == null) ? new List<ConfigurationUnit>() : provider.GetChildren(unit);
            return children;
        }

        private void NotifyPropertyChanged(ConfigurationUnit unit)
        {
            PropertyChanged?.Invoke(unit, new PropertyChangedEventArgs(unit.Id));
        }

        public T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            var provider = GetNamespaceProvider(unit.Parent.NamespaceProvider);
            if (provider == null)
                return defaultValue;

            var value = provider.GetValue(unit, defaultValue);
            return value;
        }

        public void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            var provider = GetNamespaceProvider(unit.Parent.NamespaceProvider);
            if (provider == null)
                return;

            provider.SetValue(unit, newValue);
            NotifyPropertyChanged(unit);
        }

        public void AddUserUnit(ConfigurationUnit unit)
        {
            var provider = GetNamespaceProvider(unit.NamespaceProvider);
            if (provider == null)
                return;

            provider.AddUserUnit(unit);
            NotifyPropertyChanged(unit);
        }

        public void RemoveUserUnit(ConfigurationUnit unit)
        {
            var provider = GetNamespaceProvider(unit.NamespaceProvider);
            if (provider == null)
                return;

            provider.RemoveUserUnit(unit);
            NotifyPropertyChanged(unit);
        }

        public List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            var provider = GetNamespaceProvider(unit.NamespaceProvider);
            return (provider == null) ? new List<SelectionValue>() : provider.GetSelectionValues(unit);
        }

        public string GetNewUserId(ConfigurationUnit unit)
        {
            var provider = GetNamespaceProvider(unit.NamespaceProvider);
            return (provider == null) ? Guid.NewGuid().ToString("N").ToUpper() : provider.GetNewUserId();
        }


    }
}
