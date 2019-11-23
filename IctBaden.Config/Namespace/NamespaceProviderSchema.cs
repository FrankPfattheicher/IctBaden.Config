using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IctBaden.Config.Unit;
using IctBaden.Framework.Types;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderSchema : NamespaceProvider
    {
        private readonly string _schemaFileName;
        private readonly ConfigurationUnit _schemaNamespace;

        public NamespaceProviderSchema(string specification)
        {
            _schemaFileName = specification;
            using (var rdr = new StreamReader(_schemaFileName))
            {
                _schemaNamespace = ConfigurationNamespaceXmlSerializer.Load(rdr);
                rdr.Close();
            }
        }

        public override string GetPersistenceInfo()
        {
            return _schemaFileName;
        }

        private void Save()
        {
            using (var wrt = new StreamWriter(_schemaFileName))
            {
                ConfigurationNamespaceXmlSerializer.Save(_schemaNamespace, wrt);
            }
        }

        private static string GetDisplayImage(ConfigurationUnit unit)
        {
            if (unit.IsItem || unit.IsFolder)
                return "bullet_square_glass_blue";
            if (unit.IsTemplate)
                return "bullet_square_glass_yellow";
            if (unit.IsProperty)
                return "bullet_ball_glass_blue";
            return "unknown";
        }

        private static string GetToolTip(ConfigurationUnit unit)
        {
            if (unit.IsItem)
                return "Item";
            if (unit.IsFolder)
                return "Folder";
            if (unit.IsTemplate)
                return "Template";
            if (unit.IsProperty)
                return "Property";
            return null;
        }

        private ConfigurationUnit CreateSchemaProperty(string name, TypeCode dataType = TypeCode.String, SelectionType selection = SelectionType.Edit)
        {
            var prop = new ConfigurationUnit
              {
                  Id = name,
                  DisplayName = name,
                  DisplayImage = "fas fa-circle",
                  DataType = dataType,
                  Selection = selection
              };
            prop.SetUserId(prop.Id);
            return prop;
        }

        private ConfigurationUnit CreateSchemaTemplate(string name)
        {
            var template = new ConfigurationUnit
            {
                Id = name,
                DisplayName = name,
                DataType = TypeCode.Empty
            };
            //template.SetUserId(template.Id);
            return template;
        }

        private ConfigurationUnit CreateSchemaUnit(ConfigurationUnit unit, ConfigurationUnit parent)
        {
            var schemaUnit = unit.Clone(unit.DisplayName, false);
            schemaUnit.DataType = TypeCode.Object;
            schemaUnit.DisplayImage = GetDisplayImage(unit);
            schemaUnit.ToolTip = GetToolTip(unit);
            schemaUnit.Description = null;
            schemaUnit.Parent = parent;
            schemaUnit.NamespaceProvider = null;
            schemaUnit.SetUserId(unit.Id);

            if (unit.CanCreateItem)
            {
                schemaUnit.AddChild(CreateSchemaTemplate("Template"));
                schemaUnit.Selection = SelectionType.ParentFlat;
            }
            if (!unit.IsProperty)
            {
                schemaUnit.AddChild(CreateSchemaTemplate("Item"));
                schemaUnit.AddChild(CreateSchemaTemplate("Property"));
                schemaUnit.Selection = SelectionType.ParentFlat;
            }

            //schemaUnit.AddChild(CreateSchemaProperty("Id"));
            schemaUnit.AddChild(CreateSchemaProperty("Class"));
            schemaUnit.AddChild(CreateSchemaProperty("Category"));
            schemaUnit.AddChild(CreateSchemaProperty("SortOrder", TypeCode.Int32));
            schemaUnit.AddChild(CreateSchemaProperty("DisplayName"));
            schemaUnit.AddChild(CreateSchemaProperty("DisplayImage"));
            schemaUnit.AddChild(CreateSchemaProperty("Description", TypeCode.String, SelectionType.EditHtml));
            schemaUnit.AddChild(CreateSchemaProperty("ToolTip"));

            var dataType = CreateSchemaProperty("DataType", TypeCode.String, SelectionType.ListOnly);
            dataType.ValueList.AddRange(Enum.GetNames(typeof(TypeCode)).ToList().Select(en => new SelectionValue { Value = en, DisplayText = en }));
            schemaUnit.AddChild(dataType);

            schemaUnit.AddChild(CreateSchemaProperty("DefaultValue"));
            schemaUnit.AddChild(CreateSchemaProperty("DefaultValueDisplayText"));
            schemaUnit.AddChild(CreateSchemaProperty("Unit"));

            var inputType = CreateSchemaProperty("Input", TypeCode.String, SelectionType.ListOnly);
            inputType.ValueList.AddRange(Enum.GetNames(typeof(InputType)).ToList().Select(en => new SelectionValue { Value = en, DisplayText = en }));
            schemaUnit.AddChild(inputType);

            var selectionType = CreateSchemaProperty("Selection", TypeCode.String, SelectionType.ListOnly);
            selectionType.ValueList.AddRange(Enum.GetNames(typeof(SelectionType)).ToList().Select(en => new SelectionValue { Value = en, DisplayText = en }));
            schemaUnit.AddChild(selectionType);

            schemaUnit.AddChild(CreateSchemaProperty("ValueList"));
            schemaUnit.AddChild(CreateSchemaProperty("ValueSource"));
            schemaUnit.AddChild(CreateSchemaProperty("ValidationRule"));
            schemaUnit.AddChild(CreateSchemaProperty("UserLevel", TypeCode.Int32));

            var namespaceProvider = CreateSchemaProperty("NamespaceProviderInternal");
            namespaceProvider.DisplayName = "NamespaceProvider";
            schemaUnit.AddChild(namespaceProvider);

            foreach (var prop in schemaUnit.Children)
            {
                prop.Parent = schemaUnit;
            }
            return schemaUnit;
        }

        private ConfigurationUnit GetSchemaUnit(ConfigurationUnit unit)
        {
            var namespaceId = unit.Id;
            if (!string.IsNullOrEmpty(unit.Parent.Id))
            {
                namespaceId = unit.Parent.Id + "/" + namespaceId;
            }
            var namespaceUnit = _schemaNamespace.GetUnitById(namespaceId);
            return namespaceUnit;
        }

        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            var parent = (unit.Id == string.Empty) ? _schemaNamespace : GetSchemaUnit(unit);
            return parent.Children.Select(source => CreateSchemaUnit(source, parent)).ToList();
        }

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            var namespaceUnit = GetSchemaUnit(unit.Parent);
            var propertyInfo = namespaceUnit.GetType().GetProperties().FirstOrDefault(prop => prop.Name == unit.Id);
            if (propertyInfo == null)
                return defaultValue;
            var propValue = propertyInfo.GetValue(namespaceUnit, new object[] { });
            if (propValue is SelectionValueCollection)
                propValue = propValue.ToString();
            return UniversalConverter.ConvertTo<T>(propValue);
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            var namespaceUnit = GetSchemaUnit(unit.Parent);
            var propertyInfo = namespaceUnit.GetType().GetProperties().FirstOrDefault(prop => prop.Name == unit.Id);
            if (propertyInfo == null)
                return;
            try
            {
                if (propertyInfo.PropertyType == typeof(SelectionValueCollection))
                {
                    propertyInfo.SetValue(namespaceUnit, (SelectionValueCollection)newValue.ToString(), new object[] { });
                }
                // ReSharper disable RedundantAssignment
                else if (propertyInfo.PropertyType == typeof(SelectionType))
                {
                    Enum.TryParse(newValue.ToString(), out SelectionType selectionType);
                    propertyInfo.SetValue(namespaceUnit, selectionType, new object[] { });
                }
                else if (propertyInfo.PropertyType == typeof(InputType))
                {
                    Enum.TryParse(newValue.ToString(), out InputType inputType);
                    propertyInfo.SetValue(namespaceUnit, inputType, new object[] { });
                }
                else if (propertyInfo.PropertyType == typeof(TypeCode))
                {
                    Enum.TryParse(newValue.ToString(), out TypeCode typeCode);
                    propertyInfo.SetValue(namespaceUnit, typeCode, new object[] { });
                }
                // ReSharper restore RedundantAssignment
                else
                {
                    propertyInfo.SetValue(namespaceUnit, Convert.ChangeType(newValue, propertyInfo.PropertyType), new object[] { });
                }

                Save();
            }
            catch (FormatException)
            {
            }
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            switch (unit.Description)
            {
                case "Template":
                    unit.DataType = TypeCode.Empty;
                    unit.Selection = SelectionType.None;
                    break;
                case "Item":
                    unit.DataType = TypeCode.Object;
                    unit.Selection = SelectionType.ParentFlat;
                    unit.DisplayImage = "far fa-square";
                    break;
                case "Property":
                    unit.DataType = TypeCode.String;
                    unit.Selection = SelectionType.Edit;
                    break;
            }

            var namespaceUnit = GetSchemaUnit(unit.Parent);

            var newUnit = unit.Clone(unit.DisplayName.Substring(1), false);
            newUnit.Id = newUnit.DisplayName;
            newUnit.Description = null;
            newUnit.NamespaceProviderInternal = null;

            namespaceUnit.AddChild(newUnit);
            Save();

            var newSchemaUnit = CreateSchemaUnit(newUnit, namespaceUnit);
            ConfigurationUnit.Clone(newSchemaUnit, unit, true);
            unit.SetPropertyValue("Selection", newUnit.Selection);
        }

        public override void RemoveUserUnit(ConfigurationUnit unit)
        {
            var namespaceUnit = GetSchemaUnit(unit);
            if (namespaceUnit.IsEmpty || (namespaceUnit.Parent == null))
                return;
            namespaceUnit.Parent.Children.Remove(namespaceUnit);
            Save();
        }
    }
}