using System;
using IctBaden.Config.Session;

namespace IctBaden.Config.Unit
{
  public class ConfigurationSessionUnit : ConfigurationUnit
  {
    private ConfigurationSession session;

    public override ConfigurationSession Session
    { get { return session; } }

    public ConfigurationSessionUnit(ConfigurationSession session)
    {
      this.session = session;
      DataType = TypeCode.Object;
      ChildrenLoaded = true;
    }
  }
}