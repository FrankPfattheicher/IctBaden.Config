using System;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Schema
{
  public class SchemaFromSql
  {
    public static ConfigurationUnit Create(string dbConnectionString, string tableName)
    {
      var root = new ConfigurationUnit
      {
        NamespaceProvider = "sql://" + dbConnectionString,
        Id = tableName,
        DisplayName = tableName,
        DataType = TypeCode.Object,
        Description = tableName,
        DisplayImage = "far fa-square"
      };

      return root;
    }
  }
}