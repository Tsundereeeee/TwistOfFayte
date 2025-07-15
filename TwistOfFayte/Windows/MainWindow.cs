using Ocelot.Windows;

namespace TwistOfFayte.Windows;

[OcelotMainWindow]
public class MainWindow(Plugin _plugin, Config _config) : OcelotMainWindow(_plugin, _config)
{
    protected override void Render(RenderContext context)
    {
        plugin.Modules.RenderMainUi(context);
    }
}
