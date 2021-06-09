using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IctBaden.Config.Unit;
using IctBaden.Framework.IniFile;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderProfile : NamespaceProvider
    {
        private readonly Profile _profile;

        public NamespaceProviderProfile(string profileName)
        {
            _profile = new Profile(profileName);
        }

        public override bool Connect()
        {
            if (File.Exists(_profile.FileName)) return true;
            
            File.WriteAllText(_profile.FileName, "");
            return _profile.Load();
        }

        public override string GetPersistenceInfo()
        {
            return _profile.FileName;
        }

        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            //Children=2DFA8F569C574CB787C9ACE8587A7749;C6D9E7D346234B55B3179750C81E8989;12DF7A3A144446DFB472FC28C9742C3E
            var childIds = _profile[unit.FullId].Get("Children", string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var children = new List<ConfigurationUnit>();
            foreach (var childId in childIds)
            {
                var childSection = _profile[childId];
                var childDisplayName = childSection.Get<string>("DisplayName");
                var childClass = childSection.Get<string>("Class");
                if (string.IsNullOrEmpty(childClass))
                {
                    // folder
                    var newFolder = unit.Clone(childDisplayName, false);
                    newFolder.SetUserId(childId);
                    newFolder.Description = unit.Session.Folder.DisplayName;
                    children.Add(newFolder);
                }
                else
                {
                    var type = unit.Session.Namespace.GetUnitById(childClass);
                    if (!type.IsEmpty)
                    {
                        var newItem = type.Clone(childDisplayName, true);
                        newItem.SetUserId(childId);
                        newItem.Class = childClass;
                        newItem.Description = type.DisplayName;
                        newItem.DataType = TypeCode.Object;
                        children.Add(newItem);
                    }
                }
            }

            return children;
        }

        public override List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            var sourceUnits = unit.ValueSourceUnitIds
                .Split(';');
            var sections = sourceUnits
                .Select(su => _profile[su])
                .ToList();
            return sections
                .SelectMany(sect => sect.Keys.Select(key => new SelectionValue {DisplayText = key.Name, Value = key.StringValue}))
                .ToList();
        }

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            var section = _profile[unit.Parent.FullId];
            if (section.IsUnnamedGlobalSection)
                section = _profile[ProfileSection.UnnamedGlobalSectionName];
            return section.Get(unit.Id, defaultValue);
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            var section = _profile[unit.Parent.FullId];
            if (section.IsUnnamedGlobalSection)
                section = _profile[ProfileSection.UnnamedGlobalSectionName];

            var newValueString = newValue?.ToString();
            if (newValue == null)
            {
                if (unit.DefaultValue == null)
                {
                    if (section.Contains(unit.Id))
                    {
                        section.Remove(unit.Id);
                        _profile.Save();
                    }
                }
                else
                {
                    section[unit.Id].ObjectValue = null;
                    _profile.Save();
                }
            }
            else if (newValueString == unit.DefaultValue)
            {
                if (section.Contains(unit.Id))
                {
                    section.Remove(unit.Id);
                    _profile.Save();
                }
            }
            else if (newValueString != section[unit.Id].StringValue)
            {
                section[unit.Id].StringValue = newValueString;
                _profile.Save();
            }
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            if (unit.Class != null)
            {
                var itemClass = ConfigurationUnit.GetProperty(unit, "Class");
                itemClass.SetValue(unit.Class);
            }

            var containerChildren = ConfigurationUnit.GetProperty(unit.Parent, "Children");
            containerChildren.SetValue(ConfigurationUnit.GetUnitListIdList(unit.Parent.Children));
            var itemDisplayName = ConfigurationUnit.GetProperty(unit, "DisplayName");
            itemDisplayName.SetValue(unit.DisplayName);
        }

        public override void RemoveUserUnit(ConfigurationUnit unit)
        {
            var containerChildren = ConfigurationUnit.GetProperty(unit.Parent, "Children");
            var newChildren = unit.Parent.Children.Where(c => c.Id != unit.Id);
            containerChildren.SetValue(ConfigurationUnit.GetUnitListIdList(newChildren));
        }

        public override void DeleteUserUnit(ConfigurationUnit unit)
        {
            _profile[unit.Id].Remove();
            _profile.Save();
        }
    }
}