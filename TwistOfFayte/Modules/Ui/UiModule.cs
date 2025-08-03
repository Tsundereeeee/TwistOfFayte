using Ocelot.Modules;

namespace TwistOfFayte.Modules.Ui;

[OcelotModule(ConfigOrder = 3)]
public class UiModule(Plugin plugin, Config config) : Module(plugin, config)
{
    public override UiConfig Config {
        get => PluginConfig.UiConfig;
    }
}
