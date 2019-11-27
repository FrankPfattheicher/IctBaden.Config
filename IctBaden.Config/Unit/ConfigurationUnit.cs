﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using IctBaden.Config.Session;
using IctBaden.Framework.AppUtils;
using IctBaden.Framework.PropertyProvider;
using IctBaden.Framework.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IctBaden.Config.Unit
{
    //[ContentProperty("Children")]
    [XmlRoot(Namespace = "clr-namespace:IctBaden.Config.Unit;assembly=IctBaden.Config")]
    public class ConfigurationUnit
    {
        public event Action<ConfigurationUnit> Changed;
        
        // basic data
        [XmlAttribute]
        public string Id { get; set; }

        [XmlIgnore][JsonIgnore]
        public string ContextId
        {
            get
            {
                var unit = Parent;
                while ((unit != null) && !unit.IsUserUnit)
                    unit = unit.Parent;
                return ((unit != null) && unit.IsUserUnit) ? unit.Id : null;
            }
        }
        [XmlIgnore][JsonIgnore]
        public string FullId
        {
            get
            {
                if (IsUserUnit || (ContextId == null))
                    return Id;
                return ContextId + "/" + Id;
            }
        }
        [XmlAttribute]
        public string Class { get; set; }

        // description
        [XmlAttribute]
        public string Category { get; set; }
        [XmlAttribute, DefaultValue(0)]
        public int SortOrder { get; set; }
        [XmlAttribute]
        public string DisplayName { get; set; }
        [XmlAttribute]
        public string DisplayNameSingular { get; set; }
        [XmlAttribute]
        public string DisplayImage { get; set; }

        private string _description;
        [XmlAttribute]
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// This property is for XAML compatible embedded property deserialization only.
        /// </summary>
        [XmlElement("ConfigurationUnit.Description")]
        [JsonIgnore]
        public string ConfigurationUnitDescription
        {
            get => null;
            set => _description = value;
        }

        [XmlAttribute]
        public string ToolTip { get; set; }

        // value
        [XmlAttribute]
        [JsonConverter(typeof(StringEnumConverter))]
        public TypeCode DataType { get; set; }
        [XmlAttribute]
        public string DefaultValue { get; set; }
        [XmlAttribute]
        public string DefaultValueDisplayText { get; set; }
        [XmlAttribute]
        public string Unit { get; set; }
        [XmlAttribute, DefaultValue(InputType.Optional)]
        [JsonConverter(typeof(StringEnumConverter))]
        public InputType Input { get; set; }

        private SelectionType _selection;
        [XmlAttribute, DefaultValue(SelectionType.Edit)]
        [JsonConverter(typeof(StringEnumConverter))]
        public SelectionType Selection
        {
            set => _selection = value;
            get => (IsProperty || IsParent) ? _selection : SelectionType.None;
        }

        private SelectionValueCollection ValueListInternal { get; set; }

        [XmlArray("ConfigurationUnit.ValueList")]
        [XmlArrayItem("SelectionValue")]
        public SelectionValueCollection ValueList
        {
            set => ValueListInternal = value;
            get
            {
                if (ValueListInternal.Count == 0)
                {
                    ValueListInternal = SpecialValueList;
                }
                return ValueListInternal;
            }
        }
        [XmlIgnore][JsonIgnore]
        public bool ValueListSpecified => (ValueList.Count > 0);

        [XmlAttribute]
        public string ValueSource { get; set; }
        [XmlAttribute]
        public string ValidationRule { get; set; }
        [XmlAttribute, DefaultValue(0)]
        public int UserLevel { get; set; }

        public IEnumerable<ConfigurationUnit> GetUnitList(string idList)
        {
            return GetUnitList(Parent.BaseUnitForUserUnits, idList);
        }

        public IEnumerable<ConfigurationUnit> GetUnitList(ConfigurationUnit baseUnit, string idList)
        {
            if (string.IsNullOrEmpty(idList))
                return new List<ConfigurationUnit>();

            var ids = idList.Split(';');
            return from id in ids let unit = baseUnit.GetUnitById(id) where (unit != null) && !unit.IsEmpty select unit;
        }

        public static string GetUnitListIdList(IEnumerable<ConfigurationUnit> units)
        {
            var ids = from i in units
                      where (i.DataType != TypeCode.Empty)
                      select i.FullId;
            return string.Join(";", ids.ToArray());
        }
        public static string GetUnitListDisplayText(IEnumerable<ConfigurationUnit> units)
        {
            var names = from i in units
                        where (i.DataType != TypeCode.Empty)
                        select i.DisplayName;
            return string.Join("; ", names.ToArray());
        }

        // hierarchy
        [XmlAttribute("NamespaceProvider")]
        public string NamespaceProviderInternal { get; set; }
        [XmlIgnore][JsonIgnore]
        public string NamespaceProvider
        {
            get
            {
                if (!String.IsNullOrEmpty(NamespaceProviderInternal))
                    return NamespaceProviderInternal;
                return Parent?.NamespaceProvider;
            }
            set => NamespaceProviderInternal = value;
        }
        [XmlIgnore][JsonIgnore]
        public ConfigurationUnit Parent;
        [XmlIgnore][JsonIgnore]
        public ConfigurationUnit ParentClass
        {
            get
            {
                if (Parent == null)
                    return null;
                return (Parent.Class != null) ? Parent : Parent.ParentClass;
            }
        }
        [XmlIgnore][JsonIgnore]
        public string BrowserName => (IsSchemaItem && !string.IsNullOrEmpty(Id)) ? Id : DisplayName;

        [XmlIgnore][JsonIgnore]
        public ConfigurationUnit Template
        {
            get
            {
                return Parent?.Children.FirstOrDefault(t => t.IsTemplate && (t.Id == Class));
            }
        }
        [XmlIgnore][JsonIgnore]
        public bool HasTemplates
        {
            get
            {
                return Children.Any(t => t.IsTemplate);
            }
        }
        [XmlIgnore][JsonIgnore]
        public IEnumerable<ConfigurationUnit> Templates
        {
            get
            {
                if (HasTemplates)
                    return Children.Where(t => t.IsTemplate).Select(t => t);
                return Parent?.Templates;
            }
        }

        private List<ConfigurationUnit> _children;
        protected bool ChildrenLoaded;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [XmlElement("ConfigurationUnit")]
        public List<ConfigurationUnit> Children
        {
            get
            {
                if (!IsTemplate && !IsProperty && (Session != null) && (NamespaceProvider != null) && !ChildrenLoaded)
                {
                    ChildrenLoaded = true;
                    AddChildren(Session.GetChildren(this));
                }
                return (Session == null) ? _children : _children.Where(ch => ch.UserLevel <= Session.CurrentUserLevel).ToList();
            }
            set => _children = value;
        }



        public void AddChild(ConfigurationUnit newChild)
        {
            AddChild(newChild, false);
        }
        public void AddChild(ConfigurationUnit newChild, bool inFront)
        {
            newChild.Parent = this;
            if (inFront)
            {
                _children.Insert(0, newChild);
            }
            else
            {
                _children.Add(newChild);
            }
        }

        public void AddChildren(IEnumerable<ConfigurationUnit> newChildren)
        {
            AddChildren(newChildren, false);
        }
        public void AddChildren(IEnumerable<ConfigurationUnit> newChildren, bool inFront)
        {
            foreach (var unit in newChildren)
            {
                AddChild(unit, inFront);
            }
        }

        private void AddUserChild(ConfigurationUnit userChild)
        {
            AddChild(userChild);
            Session.AddUserUnit(userChild);
            Changed?.Invoke(this);
        }

        private bool RemoveUserChild(ConfigurationUnit userChild)
        {
            if (!_children.Remove(userChild))
            {
                return false;
            }
            Changed?.Invoke(this);
            return true;
        }

        [XmlIgnore][JsonIgnore]
        public IEnumerable<ConfigurationUnit> ChildItems => from unit in Children where unit.IsItem || unit.IsFolder orderby unit.BrowserName select unit;

        [XmlIgnore][JsonIgnore]
        public bool HasProperties => Properties.Any();

        [XmlIgnore][JsonIgnore]
        public IEnumerable<ConfigurationUnit> Properties => from unit in Children where unit.IsProperty select unit;

        // information helpers
        [XmlIgnore][JsonIgnore]
        public bool IsUserUnit { get; private set; }
        [XmlIgnore][JsonIgnore]
        public bool IsRoot => Parent is ConfigurationSessionUnit;

        [XmlIgnore][JsonIgnore]
        public bool IsParent => (_selection == SelectionType.ParentFlat) || (_selection == SelectionType.ParentHierarchical);

        [XmlIgnore][JsonIgnore]
        public bool IsFolder => (Class == null) && IsParent && !IsTemplate;

        [XmlIgnore][JsonIgnore]
        public bool IsTemplate => (DataType == TypeCode.Empty) && !IsRoot;

        [XmlIgnore][JsonIgnore]
        public bool IsItem => (DataType == TypeCode.Object) && !IsFolder;

        [XmlIgnore][JsonIgnore]
        public bool IsProperty => (DataType != TypeCode.Empty) && (DataType != TypeCode.Object);

        [XmlIgnore][JsonIgnore]
        public bool IsEmpty => DataType == TypeCode.Empty && String.IsNullOrEmpty(Id);

        [XmlIgnore][JsonIgnore]
        public bool IsMandatory => Input == InputType.Mandatory;

        [XmlIgnore][JsonIgnore]
        public bool IsReadonly => Input == InputType.Readonly;

        [XmlIgnore][JsonIgnore]
        public bool IsSchemaItem => (NamespaceProvider != null) && NamespaceProvider.StartsWith("schema:");

        private bool _isExpanded;
        [XmlIgnore][JsonIgnore]
        public bool IsExpanded
        {
            get => _isExpanded || IsRoot;
            set => _isExpanded = value;
        }

        [XmlIgnore][JsonIgnore]
        public bool IsSelected { get; set; }


        public virtual ConfigurationSession Session => Parent?.Session;

        public ConfigurationUnit()
        {
            _children = new List<ConfigurationUnit>();
            DataType = TypeCode.Empty;
            Input = InputType.Optional;
            Selection = SelectionType.Edit;
            ValueListInternal = new SelectionValueCollection();
            UserLevel = 0;
            SortOrder = 0;
        }

        public void SetUserId(string userId)
        {
            IsUserUnit = true;
            Id = userId;
        }

        public void Rename(string newName)
        {
            if (IsSchemaItem)
            {
                var idProp = GetProperty(this, "Id");
                idProp.SetValue(newName);

                Id = newName;
                Changed?.Invoke(this);
                return;
            }

            DisplayName = newName;
            var cfgProp = GetProperty(this, "DisplayName");
            cfgProp.SetValue(newName);
            Changed?.Invoke(this);

            if (Template == null)
                return;

            var nameProp = Properties.FirstOrDefault(prop => prop.Id == Template.DisplayName);
            if (nameProp != null)
            {
                Session.SetValue(nameProp, newName);
            }
        }

        public ConfigurationUnit ChangeClass(ConfigurationUnit typeUnit)
        {
            var me = this;
            var mySettings = me.GetProperties();

            if (!Delete())
                return null;

            var newMe = Parent.CreateItem(me.Id, typeUnit, me.DisplayName);
            newMe.SetProperties(mySettings);
            Changed?.Invoke(this);
            return newMe;
        }

        public static void Clone(ConfigurationUnit source, ConfigurationUnit destination, bool cloneChildren)
        {
            destination.NamespaceProviderInternal = source.NamespaceProviderInternal;
            destination.Id = source.Id;
            destination.IsUserUnit = source.IsUserUnit;
            destination.DataType = source.DataType;
            destination.Category = source.Category;
            destination.DisplayName = source.DisplayName;
            destination.DisplayImage = source.DisplayImage;
            destination.Description = source.Description;
            destination.ToolTip = source.ToolTip;
            destination.DefaultValue = source.DefaultValue;
            destination.Unit = source.Unit;
            destination.Input = source.Input;
            destination.Selection = source.Selection;
            destination.ValueList = source.ValueList;
            destination.ValueSource = source.ValueSource;
            destination.ValidationRule = source.ValidationRule;
            destination.UserLevel = source.UserLevel;

            if (cloneChildren)
            {
                destination._children.Clear();
                foreach (var child in source._children)
                {
                    var newChild = child.Clone(child.DisplayName, true);
                    destination.AddChild(newChild);
                }
            }
        }

        public ConfigurationUnit Clone(string displayName, bool cloneChildren)
        {
            var newUnit = new ConfigurationUnit();
            Clone(this, newUnit, cloneChildren);
            newUnit.DisplayName = displayName;
            return newUnit;
        }

        [XmlIgnore][JsonIgnore]
        public bool CanCreateFolder => (_selection == SelectionType.ParentHierarchical) && !IsSchemaItem;

        public ConfigurationUnit CreateFolder(string displayName)
        {
            var newFolder = Session.Folder.Clone(displayName, false);
            newFolder.SetUserId(Session.GetNewUserId(this));
            newFolder.Description = Session.Folder.DisplayName;
            AddUserChild(newFolder);
            return newFolder;
        }

        [XmlIgnore][JsonIgnore]
        public bool CanCreateItem => IsParent && !string.IsNullOrEmpty(NamespaceProvider);

        public ConfigurationUnit CreateItem(ConfigurationUnit type, string displayName)
        {
            return CreateItem(Session.GetNewUserId(this), type, displayName);
        }
        private ConfigurationUnit CreateItem(string id, ConfigurationUnit type, string displayName)
        {
            var newItem = type.Clone("~" + displayName, true);
            newItem.SetUserId(id);
            newItem.Parent = this;
            newItem.DataType = TypeCode.Object;
            newItem.Description = type.DisplayName;
            newItem.Class = type.Id;
            newItem.DataType = TypeCode.Object;
            AddUserChild(newItem);
            newItem.Rename(displayName);
            return newItem;
        }

        [XmlIgnore][JsonIgnore]
        public bool CanDelete => IsUserUnit;

        public T GetValue<T>()
        {
            return GetValue(UniversalConverter.ConvertTo<T>(DefaultValue));
        }
        public T GetValue<T>(T defaultValue)
        {
            if (Session == null) return defaultValue;
            
            var value = Session.GetValue(this, defaultValue);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        [XmlIgnore][JsonIgnore]
        public string ValueDisplayText
        {
            get
            {
                var text = GetValue<string>();

                switch (DataType)
                {
                    case TypeCode.DateTime:
                        text = text.Replace(" 00:00:00", "");
                        break;
                }

                return text;
            }
        }

        public void SetValue<T>(T value)
        {
            if ((Parent.Template == null) || (Id != Parent.Template.DisplayName))
            {
                Session.SetValue(this, value);
            }
            else
            {
                Parent.Rename(value.ToString());
            }
        }

        public bool Delete()
        {
            foreach (var child in Children.ToArray())
            {
                child.Delete();
            }
            Debug.Assert(Parent != null);
            Parent.RemoveUserChild(this);
            Session.RemoveUserUnit(this);
            return true;
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(DisplayName)
                ? DisplayName
                : "ID=" + FullId;
        }

        public ConfigurationUnit GetUnitById(string id)
        {
            if (Id == id)
                return this;

            var path = id.Split('/').ToList();
            foreach (var child in Children)
            {
                var unit = child.GetUnitById(id);
                if (!unit.IsEmpty)
                    return unit;

                if ((path.Count == 0) || (child.Id != path[0]))
                    continue;

                return child.GetUnitById(string.Join("/", path.Skip(1).ToArray()));
            }
            return new ConfigurationUnit();
        }

        public ConfigurationUnit GetUnitByName(string name, bool includeFolders = false)
        {
            if ((includeFolders || !IsFolder) && (DisplayName == name))
                return this;

            foreach (var child in Children.Where(child => (!child.IsFolder && !child.IsTemplate && !child.IsProperty)))
            {
                var unit = child.GetUnitByName(name, includeFolders);
                if (!unit.IsEmpty)
                    return unit;
            }

            var path = name.Split('/').ToList();
            foreach (var child in Children.Where(child => child.IsFolder && !child.IsTemplate))
            {
                var unit = child.GetUnitByName(name, includeFolders);
                if (!unit.IsEmpty)
                    return unit;

                if ((path.Count == 0) || (child.DisplayName != path[0]))
                    continue;

                return child.GetUnitByName(string.Join("/", path.Skip(1).ToArray()), includeFolders);
            }
            return new ConfigurationUnit();
        }

        public ConfigurationUnit GetNewUnitBase()
        {
            var newUnit = Children.FirstOrDefault(child => child.IsTemplate);
            return (newUnit != null) ? this : Parent.GetNewUnitBase();
        }

        [XmlIgnore][JsonIgnore]
        public ConfigurationUnit BaseUnitForReferenceUnits
        {
            get
            {
                var source = Session.Namespace.GetUnitById(ValueSource);
                if (source == null)
                {
                    throw new ArgumentException("Invalid schema: ValueSource=" + ValueSource);
                }
                while (source.IsTemplate)
                {
                    source = source.Parent;
                }
                return source;
            }
        }

        [XmlIgnore][JsonIgnore]
        public ConfigurationUnit BaseUnitForUserUnits
        {
            get
            {
                var baseUnit = this;
                while (!baseUnit.HasTemplates && (baseUnit.Parent != null))
                    baseUnit = baseUnit.Parent;
                return baseUnit;
            }
        }
        [XmlIgnore][JsonIgnore]
        public IEnumerable<ConfigurationUnit> NewUnitTemplates => from unit in BaseUnitForUserUnits.Children where unit.IsTemplate select unit;

        public List<ConfigurationUnit> GetUserUnits(string unitClass)
        {
            var userUnits = new List<ConfigurationUnit>();
            foreach (var child in Children)
            {
                if (child.IsFolder)
                {
                    userUnits.AddRange(child.GetUserUnits(unitClass));
                }
                else if (child.IsUserUnit)
                {
                    if (string.IsNullOrEmpty(unitClass) || (child.Class == unitClass))
                    {
                        userUnits.Add(child);
                    }
                    else
                    {
                        userUnits.AddRange(child.GetUserUnits(unitClass));
                    }
                }
            }
            return userUnits;
        }

        public IPropertyProvider GetPropertyProvider()
        {
            return new ConfigurationUnitPropertyProvider(this);
        }

        public PropertyBag GetProperties()
        {
            var bag = new PropertyBag();
            foreach (var child in Children)
            {
                bag.Set(child.Id, GetPropertyValue<object>(child.Id));
            }
            return bag;
        }

        public void SetProperties(PropertyBag properties)
        {
            foreach (var property in properties)
            {
                SetPropertyValue(property.Key, property.Value);
            }
        }

        public static ConfigurationUnit GetProperty(ConfigurationUnit item, string name)
        {
            var property = item.Clone(null, false);
            property.SetUserId(name);
            property.Parent = item;
            return property;
        }

        public T GetPropertyValue<T>(string property)
        {
            var prop = GetUnitById(property);
            return (prop.IsEmpty) ? default : GetPropertyValue(property, UniversalConverter.ConvertTo<T>(prop.DefaultValue));
        }
        public T GetPropertyValue<T>(string property, T defaultValue)
        {
            var prop = GetUnitById(property);
            return (prop.IsEmpty) ? defaultValue : prop.GetValue(defaultValue);
        }

        public void SetPropertyValue<T>(string property, T newValue)
        {
            var prop = GetUnitById(property);
            if (prop.IsEmpty)
                return;
            var cfgProp = GetProperty(this, property);
            cfgProp.SetValue(newValue);
        }

        [XmlIgnore][JsonIgnore]
        private SelectionValueCollection SpecialValueList
        {
            get
            {
                var specialList = new SelectionValueCollection();

                if ((Input == InputType.Optional) && !string.IsNullOrEmpty(DefaultValueDisplayText))
                {
                    specialList.Add(new SelectionValue { DisplayText = DefaultValueDisplayText, Value = null });
                }

                switch (_selection)
                {
                    case SelectionType.AvailableComPorts:
                        if(SystemInfo.Platform == Platform.Windows)
                        {
                            specialList.AddRange(SerialPort.GetPortNames()
                                .Select(port => new SelectionValue {DisplayText = port, Value = port}));
                        }
                        else if (SystemInfo.Platform == Platform.Linux)
                        {
                            //TODO: add available serial ports
                        }
                        break;
                    
                    case SelectionType.AvailableTtsEngines:
                        //TODO: IctBaden.Speech
                        //    var speechSynthesizer = new SpeechSynthesizer();
                        //    specialList.AddRange(from voice in speechSynthesizer.GetInstalledVoices()
                        //                         order by voice.VoiceInfo.Name
                        //                         select new SelectionValue { DisplayText = voice.VoiceInfo.Name + " (" + voice.VoiceInfo.Culture + ")", Value = voice.VoiceInfo.Name });
                        break;
                    case SelectionType.ListOnly:
                        if (!string.IsNullOrEmpty(ValueSource) && (Session != null))
                        {
                            specialList.AddRange(Session.GetSelectionValues(this));
                        }
                        break;
                }

                return specialList;
            }
        }

        public List<string> GetNamespaceProviderList()
        {
            var list = new List<string>();
            EnumNamespaceProviderList(ref list);
            return list;
        }
        private void EnumNamespaceProviderList(ref List<string> list)
        {
            if (!string.IsNullOrEmpty(NamespaceProviderInternal) && !list.Contains(NamespaceProviderInternal))
            {
                list.Add(NamespaceProviderInternal);
            }

            foreach (var child in _children)
            {
                child.EnumNamespaceProviderList(ref list);
            }
        }

    }

}
