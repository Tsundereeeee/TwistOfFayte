using System;
using ECommons.DalamudServices;
using Ocelot;
using TwistOfFayte.Modules.Target;

namespace TwistOfFayte;

[Serializable]
public class Config : IOcelotConfig
{
    public int Version { get; set; } = 1;

    public TargetConfig TargetConfig { get; set; } = new();

    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }
}
