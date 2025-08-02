using Ocelot.Modules;

namespace TwistOfFayte.Modules.General;

// [OcelotModule(ConfigOrder = 0)]
public class GeneralModule(Plugin plugin, Config config) : Module(plugin, config)
{
    public override GeneralConfig Config {
        get => PluginConfig.GeneralConfig;
    }
}
