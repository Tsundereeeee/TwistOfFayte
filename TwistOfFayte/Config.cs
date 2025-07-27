using System;
using ECommons.DalamudServices;
using Ocelot;
using TwistOfFayte.Modules.Selector;
using TwistOfFayte.Modules.Tracker;
using TwistOfFayte.Modules.Ui;

namespace TwistOfFayte;

[Serializable]
public class Config : IOcelotConfig
{
    public int Version { get; set; } = 1;

    public SelectorConfig SelectorConfig { get; set; } = new();

    public TrackerConfig TrackerConfig { get; set; } = new();

    public UiConfig UiConfig { get; set; } = new();

    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }
}
