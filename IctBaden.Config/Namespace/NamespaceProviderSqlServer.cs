using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IctBaden.Config.Unit;
using IctBaden.Framework.Types;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Config.Namespace;

public class NamespaceProviderSqlServer : NamespaceProvider
{
    // table
    // parentId    unitId    value
    private readonly string _tableName = "CfgData";
    private bool _tableExists;

    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly SqlConnection _connection;
    private string _lastError;

    private const string CreateTable = 
        """
        CREATE TABLE [dbo].[CfgData](
        	            [ParentId] [nvarchar](50) NOT NULL,
        	            [UnitId] [nvarchar](50) NOT NULL,
        	            [Value] [nvarchar](max) NULL,
                     CONSTRAINT [PK_CfgData] PRIMARY KEY CLUSTERED 
                        (
        	                [ParentId] ASC,
        	                [UnitId] ASC
                        )
                     WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
        """;
        
    public NamespaceProviderSqlServer(ILogger logger, string connectionString)
    {
        _logger = logger;
        _lastError = string.Empty;
        _connectionString = connectionString;
        _connection = new SqlConnection(connectionString);
    }

    public override string GetPersistenceInfo()
    {
        var info = _connectionString;
        if (!string.IsNullOrEmpty(_lastError))
        {
            info += ": " + _lastError;
        }
        return info;
    }

    public override bool Connect()
    {
        SignalWaiting(true);

        try
        {
            lock (_connection)
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"NamespaceProviderSqlServer: Connect FAILED: {ex.Message}");
            _lastError = ex.Message;
        }

        SignalWaiting(false);
        return _connection.State == ConnectionState.Open;
    }

    public override bool IsReadOnly()
    {
        
        if (_connection.State != ConnectionState.Open) return true;

        try
        {
            lock (_connection)
            {
                SqlCommand cmd;
                using var command = cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT DATABASEPROPERTYEX('{_connection.Database}', 'Updateability') AS Status;";
                using var rdr = cmd.ExecuteReader();
                if (!rdr.Read())
                {
                    return true;
                }
                if (rdr[0] is bool updatable)
                {
                    return !updatable;
                }

                return true;
            }
        }
        catch (SqlException)
        {
            // ignore
        }
        return true;
    }

    private void CreateCfgDataTableIfNotExists()
    {
        if (_connection.State != ConnectionState.Open) return;

        SqlCommand cmd;
        try
        {
            lock (_connection)
            {
                using var command = cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {_tableName}";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Close();
                }

                _tableExists = true;
                return;
            }
        }
        catch (SqlException)
        {
            // ignore and create table
        }

        try
        {
            lock (_connection)
            {
                using var command = cmd = _connection.CreateCommand();
                cmd.CommandText = CreateTable
                    .Replace("CfgData]", _tableName + "]");

                var count = cmd.ExecuteNonQuery();
                if (count != 0)
                {
                    _tableExists = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"NamespaceProviderSqlServer: Create DB: {ex.Message}");
        }
    }

    private string? GetValue(string parentId, string unitId)
    {
        lock (_connection)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT Value FROM {_tableName} WHERE ParentId=@pid AND UnitId=@uid";
            cmd.Parameters.Add(new SqlParameter("@pid", parentId));
            cmd.Parameters.Add(new SqlParameter("@uid", unitId));

            using var rdr = cmd.ExecuteReader();
            try
            {
                if (!rdr.Read())
                {
                    return null;
                }
                if (rdr[0] is string value)
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"NamespaceProviderSqlServer: GetValue: {ex.Message}");
            }
            finally
            {
                rdr.Close();
            }
            return null;
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
            
        var parentId = unit.Id;
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
                newFolder.Description = unit.Session?.Folder.DisplayName;
                children.Add(newFolder);
            }
            else
            {
                var type = unit.Session?.Namespace.GetUnitById(childClass);
                if (type is { IsEmpty: false })
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

    public override T? GetValue<T>(ConfigurationUnit unit, T defaultValue) where T : default
    {
        if (!Connect())
            return defaultValue;

        var value = GetValue(unit.Parent?.Id ?? string.Empty, unit.Id);
        return value != null
            ? UniversalConverter.ConvertTo<T>(value)
            : defaultValue;
    }

    public override void SetValue<T>(ConfigurationUnit unit, T newValue)
    {
        if (!Connect())
            return;

        try
        {
            lock (_connection)
            {
                using var cmd1 = _connection.CreateCommand();
                cmd1.CommandText = $"UPDATE {_tableName} SET Value=@val WHERE ParentId=@pid AND UnitId=@uid";
                cmd1.Parameters.Add(new SqlParameter("@pid", unit.Parent?.Id));
                cmd1.Parameters.Add(new SqlParameter("@uid", unit.Id));
                cmd1.Parameters.Add(new SqlParameter("@val", $"{newValue}"));
                var result = cmd1.ExecuteNonQuery();

                if (result != 0) return;
                using var cmd2 = _connection.CreateCommand();
                cmd2.CommandText = $"INSERT INTO {_tableName} (ParentId, UnitId, Value) VALUES (@pid, @uid, @val)";
                cmd2.Parameters.Add(new SqlParameter("@pid", unit.Parent?.Id));
                cmd2.Parameters.Add(new SqlParameter("@uid", unit.Id));
                cmd2.Parameters.Add(new SqlParameter("@val", $"{newValue}"));
                cmd2.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"NamespaceProviderSqlServer: SetValue: {ex.Message}");
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

        if (unit.Parent != null)
        {
            var containerChildren = ConfigurationUnit.GetProperty(unit.Parent, "Children");
            containerChildren.SetValue(ConfigurationUnit.GetUnitListIdList(unit.Parent.Children));
        }

        var itemDisplayName = ConfigurationUnit.GetProperty(unit, "DisplayName");
        itemDisplayName.SetValue(unit.DisplayName);
    }

    public override void RemoveUserUnit(ConfigurationUnit unit)
    {
        if (!Connect())
            return;
        if (unit.Parent == null)
            return;
            
        var containerChildren = ConfigurationUnit.GetProperty(unit.Parent, "Children");
        var newChildren = unit.Parent.Children.Where(c => c.Id != unit.Id);
        containerChildren.SetValue(ConfigurationUnit.GetUnitListIdList(newChildren));
    }

    public override void DeleteUserUnit(ConfigurationUnit unit)
    {
        if (!Connect())
            return;

        try
        {
            lock (_connection)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM {_tableName} WHERE ParentId=@pid";
                cmd.Parameters.Add(new SqlParameter("@pid", (unit.Parent?.Class != null) ? unit.Parent.Id : unit.Id));
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"NamespaceProviderSqlServer: DeleteUserUnit: {ex.Message}");
        }
    }

    public override List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
    {
        var list = new List<SelectionValue>();

        if (!Connect())
            return list;

        _logger.LogCritical($"NamespaceProviderSqlServer: GetSelectionValues: NOT IMPLEMENTED");
        throw new NotImplementedException();
    }

}