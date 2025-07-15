using System;
using ECommons.DalamudServices;
using Ocelot;

namespace TwistOfFayte;

[Serializable]
public class Config : IOcelotConfig
{
    public int Version { get; set; } = 1;

    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }
}
