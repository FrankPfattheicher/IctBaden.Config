using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using IctBaden.Config.Unit;
using IctBaden.Framework.Types;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderSqlServer : NamespaceProvider
    {
        // table
        // parentId    unitId    value
        private readonly string _tableName = "CfgData";
        private bool _tableExists;

        private readonly string _connectionString;
        private readonly SqlConnection _connection;
        private string _lastError;

        private const string CreateTable = 
            @"CREATE TABLE [dbo].[CfgData](
	            [ParentId] [nvarchar](50) NOT NULL,
	            [UnitId] [nvarchar](50) NOT NULL,
	            [Value] [nvarchar](max) NULL,
             CONSTRAINT [PK_CfgData] PRIMARY KEY CLUSTERED 
                (
	                [ParentId] ASC,
	                [UnitId] ASC
                )
             WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]";
        
        public NamespaceProviderSqlServer(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(connectionString);
        }

        public override string GetPersistenceInfo()
        {
            return _connectionString;
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

                if (!_tableExists)
                {
                    CreateCfgDataTableIfNotExists();
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

        private void CreateCfgDataTableIfNotExists()
        {
            if (_connection.State != ConnectionState.Open) return;

            SqlCommand cmd;
            try
            {
                using(cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM {_tableName}";
                    using (var rdr = cmd.ExecuteReader())
                    {
                        rdr.Close();
                    }
                }
                _tableExists = true;
            }
            catch (SqlException)
            {
                // ignore and create table
            }
            
            using(cmd = _connection.CreateCommand())
            {
                cmd.CommandText = CreateTable
                    .Replace("CfgData]", _tableName + "]");

                var count = cmd.ExecuteNonQuery();
                if (count != 0)
                {
                    _tableExists = true;
                }
            }
        }

        private string GetValue(string parentId, string unitId)
        {
            using(var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT Value FROM {_tableName} WHERE ParentId=@pid AND UnitId=@uid";
                cmd.Parameters.Add(new SqlParameter("@pid", parentId));
                cmd.Parameters.Add(new SqlParameter("@uid", unitId));

                using (var rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read()) return null;
                    if (!(rdr[0] is string value)) return null;
                    return value;
                }
            }
        }
        
        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            var children = new List<ConfigurationUnit>();

            if (!Connect())
            {
                children.Add(new ConfigurationUnit { DataType = TypeCode.Object, DisplayName = _lastError, DisplayImage = "error" });
                return children;
            }
            
            var parentId = unit.Id ?? "";
            var childIdList = GetValue(parentId, "Children");
            if (string.IsNullOrEmpty(childIdList)) return children;

            var childIds = childIdList.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var childId in childIds)
            {
                var childDisplayName = GetValue(childId,"DisplayName");
                if (string.IsNullOrEmpty(childDisplayName)) continue;

                var childClass = GetValue(childId, "Class");
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

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            if (!Connect())
                return defaultValue;

            var value = GetValue(unit.Parent.Id ?? "", unit.Id);
            return value != null
                ? UniversalConverter.ConvertTo<T>(value)
                : defaultValue;
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            if (!Connect())
                return;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {_tableName} SET Value=@val WHERE ParentId=@pid AND UnitId=@uid";
                cmd.Parameters.Add(new SqlParameter("@pid", unit.Parent.Id));
                cmd.Parameters.Add(new SqlParameter("@uid", unit.Id));
                cmd.Parameters.Add(new SqlParameter("@val", newValue));
                var result = cmd.ExecuteNonQuery();

                if (result != 0) return;
            }
            using(var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {_tableName} (ParentId, UnitId, Value) VALUES (@pid, @uid, @val)";
                cmd.Parameters.Add(new SqlParameter("@pid", unit.Parent.Id));
                cmd.Parameters.Add(new SqlParameter("@uid", unit.Id));
                cmd.Parameters.Add(new SqlParameter("@val", newValue));
                cmd.ExecuteNonQuery();
            }
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            if (!Connect())
                return;

            if(unit.Class != null)
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
            if ((unit.Class == null) || !Connect())
                return;

            using(var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {_tableName} WHERE ParentId=@pid";
                cmd.Parameters.Add(new SqlParameter("@pid", (unit.Parent.Class != null) ? unit.Parent.Id : unit.Id));
                cmd.ExecuteNonQuery();
            }
        }

        public override List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            var list = new List<SelectionValue>();

            if (!Connect())
                return list;

            throw new NotImplementedException();
        }

    }
}