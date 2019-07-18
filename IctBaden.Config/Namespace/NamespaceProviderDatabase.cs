using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using IctBaden.Config.Unit;
using IctBaden.Framework.Types;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderDatabase : NamespaceProvider
    {
        private readonly SqlConnection _connection;
        private string _lastError;

        public NamespaceProviderDatabase(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        private bool Connect()
        {
            SignalWaiting(true);

            try
            {
                if (_connection.State == ConnectionState.Broken)
                {
                    _connection.Close();
                }
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _lastError = ex.Message;
            }

            SignalWaiting(false);
            return _connection.State == ConnectionState.Open;
        }

        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            var children = new List<ConfigurationUnit>();

            var template = unit.Children.FirstOrDefault(ch => ch.IsTemplate);
            if (template == null)
                return children;

            var table = template.Id;
            var idColumn = template.DefaultValue;
            var displayColumn = template.DisplayName;
            if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(idColumn) || string.IsNullOrEmpty(displayColumn))
            {
                return children;
            }

            if (!Connect())
            {
                children.Add(new ConfigurationUnit { DataType = TypeCode.Object, DisplayName = _lastError, DisplayImage = "error" });
                return children;
            }

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM " + table;

            if (!string.IsNullOrEmpty(template.ValueSource))
            {
                // ReSharper disable once StringLiteralTypo
                cmd.Parameters.Add(new SqlParameter("@parentid", template.ParentClass.Id));
                var where = " WHERE " + template.ValueSource + "=?";
                var join = " INNER JOIN " + template.ParentClass.Class + " ON " + table + "." + template.ValueSource + " = " + template.ParentClass.Class + "." + template.ParentClass.DefaultValue;
                cmd.CommandText += join + where;
            }

            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var id = rdr[idColumn].ToString();
                var name = rdr[displayColumn].ToString();

                var newItem = template.Clone(name, true);
                newItem.SetUserId(id);
                newItem.Parent = unit;
                newItem.Class = table;
                newItem.ValueSource = idColumn;
                newItem.DataType = TypeCode.Object;
                children.Add(newItem);
            }
            return children;
        }

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            if (!Connect())
                return defaultValue;

            var table = unit.Parent.Class;
            var column = unit.Parent.ValueSource;
            if ((table == null) || (column == null))
                return defaultValue;

            var cmd = _connection.CreateCommand();
            cmd.Parameters.Add(new SqlParameter("@id", unit.Parent.Id));
            cmd.CommandText = "SELECT * FROM " + table + " WHERE " + column + "=?";
            var rdr = cmd.ExecuteReader();
            if (!rdr.Read())
                return defaultValue;

            var value = rdr[unit.Id];
            return UniversalConverter.ConvertTo<T>(value);
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            if (!Connect())
                return;

            var table = unit.Parent.Class;
            var idColumn = unit.Parent.ValueSource;
            if ((table == null) || (idColumn == null))
                return;

            var valueColumn = unit.Id;
            var template = unit.Parent.Parent?.Children.FirstOrDefault(ch => ch.IsTemplate);
            if (template != null && (valueColumn == "DisplayName"))
            {
                valueColumn = template.DisplayName;
            }

            var cmd = _connection.CreateCommand();
            // ReSharper disable once StringLiteralTypo
            cmd.Parameters.Add(new SqlParameter("@newval", newValue));
            cmd.Parameters.Add(new SqlParameter("@id", unit.Parent.Id));
            cmd.CommandText = "UPDATE " + table + " SET " + valueColumn + "=? WHERE " + idColumn + "=?";
            cmd.ExecuteNonQuery();
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            if ((unit.Class == null) || !Connect())
                return;

            var template = unit.Parent.Children.FirstOrDefault(ch => ch.IsTemplate);
            if (template == null)
                return;

            var table = unit.Class;
            var idColumn = template.DefaultValue;

            var columnProperties = unit.Properties.Where(p => p.Input == InputType.Mandatory).Select(p => p).ToList();
            if (columnProperties.Count == 0)
            {
                columnProperties.Add(unit.Properties.First());
            }
            var namePlaceholders = columnProperties.Select(p => p.Id).ToList();
            var paramPlaceholders = new List<string>();

            var cmd = _connection.CreateCommand();

            foreach (var property in columnProperties)
            {
                cmd.Parameters.Add(new SqlParameter("@" + property.Id, property.DefaultValue ?? string.Empty));
                paramPlaceholders.Add("?");
            }

            if (!string.IsNullOrEmpty(template.ValueSource))
            {
                namePlaceholders.Add(template.ValueSource);
                cmd.Parameters.Add(new SqlParameter("@" + template.ValueSource, unit.ParentClass.Id));
                paramPlaceholders.Add("?");
            }

            var columnNames = string.Join(", ", namePlaceholders.ToArray());
            var columnParams = string.Join(", ", paramPlaceholders.ToArray());

            cmd.CommandText = "INSERT INTO " + table + " ( " + columnNames + " ) VALUES ( " + columnParams + " ); SELECT SCOPE_IDENTITY()";
            var rdr = cmd.ExecuteReader();
            if (!rdr.Read())
                return;

            unit.Id = rdr[0].ToString();
            unit.ValueSource = idColumn;
        }

        public override void RemoveUserUnit(ConfigurationUnit unit)
        {
            if ((unit.Class == null) || !Connect())
                return;

            var table = unit.Parent.Class ?? unit.Class;
            var column = unit.Parent.ValueSource ?? unit.ValueSource;
            var cmd = _connection.CreateCommand();
            cmd.Parameters.Add(new SqlParameter("@id", (unit.Parent.Class != null) ? unit.Parent.Id : unit.Id));
            cmd.CommandText = "DELETE FROM " + table + " WHERE " + column + "=?";
            cmd.ExecuteNonQuery();
        }

        public override List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            var list = new List<SelectionValue>();

            if (!Connect())
                return list;

            var cmd = _connection.CreateCommand();
            cmd.CommandText = unit.ValueSource;

            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var value = rdr[0].ToString();
                var displayText = rdr[1].ToString();

                list.Add(new SelectionValue { Value = value, DisplayText = displayText });
            }
            return list;
        }

    }
}