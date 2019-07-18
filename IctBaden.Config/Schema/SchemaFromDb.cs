using System;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Schema
{
  public class SchemaFromDb
  {
    public static ConfigurationUnit Create(string dbConnectionString, string tableName)
    {
      var root = new ConfigurationUnit
      {
        NamespaceProvider = "db://" + dbConnectionString,
        Id = tableName,
        DisplayName = tableName,
        DataType = TypeCode.Object,
        Description = tableName,
        DisplayImage = "element"
      };

      return root;
    }
  }
}