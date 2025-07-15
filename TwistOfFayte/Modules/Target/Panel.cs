using Ocelot;
using Ocelot.Windows;

namespace TwistOfFayte.Modules.Target;

public class Panel
{
    public void Render(RenderContext context)
    {
        OcelotUI.Title("Target:");
        OcelotUI.Indent(() => {
            // Content here
        });
    }
}
