using System;
using IctBaden.Config.Session;

namespace IctBaden.Config.Unit;

public class ConfigurationSessionUnit : ConfigurationUnit
{
    public override ConfigurationSession? Session { get; }

    public ConfigurationSessionUnit(ConfigurationSession? session)
    {
        Session = session;
        DataType = TypeCode.Object;
        ChildrenLoaded = true;
    }
}