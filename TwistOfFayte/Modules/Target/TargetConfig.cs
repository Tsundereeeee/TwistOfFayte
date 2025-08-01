using Ocelot.Config.Attributes;
using Ocelot.Modules;

namespace TwistOfFayte.Modules.Target;

public class TargetConfig : ModuleConfig
{
    [IntRange(0, 16)]
    public int MaxMobsToFight { get; set; } = 0;
}
