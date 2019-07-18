using IctBaden.Config.Unit;
using IctBaden.Framework.Types;

namespace IctBaden.Config.Property
{
  public class ConfigurationProperty<T>
  {
    private readonly ConfigurationUnit property;

    public ConfigurationProperty(ConfigurationUnit parent, string name)
      : this(parent, name, default(T))
    {
    }
    public ConfigurationProperty(ConfigurationUnit parent, string name, T defaultValue)
    {
      property = ConfigurationUnit.GetProperty(parent, name);
      property.DefaultValue = UniversalConverter.ConvertTo<string>(defaultValue);
    }

    public T Value
    {
      get
      {
        return property.GetValue<T>();
      }
      set
      {
        property.SetValue(value);
      }
    }
  }

}
