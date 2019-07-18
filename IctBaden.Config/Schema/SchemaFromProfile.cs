using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IctBaden.Config.Unit;
using IctBaden.Framework.IniFile;

namespace IctBaden.Config.Schema
{
  public class SchemaFromProfile
  {
    public static ConfigurationUnit Create(string fileName)
    {
      var iniFile = new Profile(fileName);

      var root = new ConfigurationUnit
      {
        NamespaceProvider = "file://" + fileName,
        Id = Path.GetFileNameWithoutExtension(fileName),
        DisplayName = Path.GetFileName(fileName),
        DataType = TypeCode.Object,
        Description = fileName,
        DisplayImage = "IniFile"
      };

      foreach (var section in iniFile.Sections)
      {
        var sectionUnit = new ConfigurationUnit
        {
          Parent = root,
          Id = section.Name,
          DisplayName = section.Name,
          DataType = TypeCode.Object,
          DisplayImage = "IniSection"
        };
        root.Children.Add(sectionUnit);

        foreach (var keyUnit in section.Keys.Select(key => new ConfigurationUnit
        {
          Parent = sectionUnit,
          Id = key.Name,
          DisplayName = key.Name,
          DataType = DetectTypeFromValue(key.StringValue)
        }))
        {
          sectionUnit.Children.Add(keyUnit);
        }
      }

      return root;
    }

    private static TypeCode DetectTypeFromValue(string stringValue)
    {
      var looksLikeDouble = new Regex(@"^[+-]?[0-9]*\.[0-9]+$");
      if (looksLikeDouble.IsMatch(stringValue))
        return TypeCode.Double;

      var looksLikeInteger = new Regex(@"^[+-]?[0-9]+$");
      if (looksLikeInteger.IsMatch(stringValue))
        return TypeCode.Int64;
      
      return TypeCode.String;
    }
  }
}