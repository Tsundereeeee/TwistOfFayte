using Ocelot.Modules;
using Ocelot.Windows;

namespace TwistOfFayte.Modules.Target;

[OcelotModule]
public class TargetModule(Plugin plugin, Config config) : Module<Plugin, Config>(plugin, config)
{
    public override TargetConfig Config {
        get => PluginConfig.TargetConfig;
    }

    private Panel panel = new();

    public override bool RenderMainUi(RenderContext context)
    {
        panel.Render(context.ForModule(this));
        return true;
    }
}
