using System;
using IctBaden.Config.Unit;
using Microsoft.Extensions.Primitives;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace IctBaden.Config.Configuration;

public class UnitReloadToken : IChangeToken
{
    // ReSharper disable once NotAccessedField.Local
    private readonly ConfigurationUnit? _unit;

    public UnitReloadToken(ConfigurationUnit? cfgUnit)
    {
        _unit = cfgUnit;
    }
        
    public IDisposable RegisterChangeCallback(Action<object> callback, object? state)
    {
        throw new NotImplementedException();
    }

    public bool HasChanged { get; }
    public bool ActiveChangeCallbacks { get; }
}