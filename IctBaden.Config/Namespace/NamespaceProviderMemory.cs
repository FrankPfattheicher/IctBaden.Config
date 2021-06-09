using System;
using System.Collections.Generic;
using System.Linq;
using IctBaden.Config.Unit;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderMemory : NamespaceProvider
    {
        public string Name { get; private set; }

        //                          section            key     value
        //                          unit.Parent.FullId unit.Id
        private readonly Dictionary<string, Dictionary<string, object>> _data;

        public NamespaceProviderMemory(string specification)
        {
            Name = specification;
            _data = new Dictionary<string, Dictionary<string, object>>();
        }

        public override bool Connect()
        {
            return true;
        }

        public override string GetPersistenceInfo()
        {
            return "InMemory";
        }

        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            //Children=2DFA8F569C574CB787C9ACE8587A7749;C6D9E7D346234B55B3179750C81E8989;12DF7A3A144446DFB472FC28C9742C3E
            var children = new List<ConfigurationUnit>();
            if (unit.FullId == null || !_data.ContainsKey(unit.FullId))
                return children;

            var keys = _data[unit.FullId];
            if (!keys.ContainsKey("Children"))
                return children;

            var childIds = keys["Children"].ToString().Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var childId in childIds)
            {
                var childSection = _data[childId];
                var childDisplayName = childSection["DisplayName"].ToString();
                var childClass = childSection["Class"].ToString();
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
                        newItem.Parent = unit;
                        newItem.Class = childClass;
                        newItem.Description = type.DisplayName;
                        newItem.DataType = TypeCode.Object;
                        children.Add(newItem);
                    }
                }
            }

            return children;
        }

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            if (!_data.ContainsKey(unit.Parent.FullId))
                return defaultValue;

            var keys = _data[unit.Parent.FullId];
            if (!keys.ContainsKey(unit.Id))
                return defaultValue;

            return (T) Convert.ChangeType(keys[unit.Id], typeof(T));
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            if (!_data.ContainsKey(unit.Parent.FullId))
            {
                _data.Add(unit.Parent.FullId, new Dictionary<string, object>());
            }

            var keys = _data[unit.Parent.FullId];
            if (!keys.ContainsKey(unit.Id))
            {
                keys.Add(unit.Id, newValue);
                return;
            }

            keys[unit.Id] = newValue;
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            if (unit.Class == null)
                return;

            if (!_data.ContainsKey(unit.Parent.FullId))
            {
                _data.Add(unit.Parent.FullId, new Dictionary<string, object>());
            }

            if (!_data.ContainsKey(unit.FullId))
            {
                _data.Add(unit.FullId, new Dictionary<string, object>());
            }

            var parentUnit = _data[unit.Parent.FullId];
            parentUnit["Children"] = ConfigurationUnit.GetUnitListIdList(unit.Parent.Children);
            var newUnit = _data[unit.FullId];
            newUnit["Class"] = unit.Class;
            newUnit["DisplayName"] = unit.DisplayName;
        }

        public override void RemoveUserUnit(ConfigurationUnit unit)
        {
            if (!_data.ContainsKey(unit.Parent.FullId))
                return;
            var parentUnit = _data[unit.Parent.FullId];
            var newChildren = unit.Parent.Children.Where(c => c.Id != unit.Id);
            parentUnit["Children"] = ConfigurationUnit.GetUnitListIdList(newChildren);
        }

        public override void DeleteUserUnit(ConfigurationUnit unit)
        {
            if (!_data.ContainsKey(unit.Parent.FullId))
                return;
            _data.Remove(unit.Parent.FullId);
        }
    }
}