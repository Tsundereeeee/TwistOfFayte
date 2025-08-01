using Ocelot.Config.Attributes;
using Ocelot.Modules;

namespace TwistOfFayte.Modules.Selector;

public class SelectorConfig : ModuleConfig
{
    [FloatRange(6f, 20f)]
    public float CostPerYalm { get; set; } = 20f;

    [IntRange(5, 10)]
    public int TimeToTeleport { get; set; } = 6;

    [FloatRange(-1024f, 1024f)]
    public float BonusFateModifier { get; set; } = 512f;

    [FloatRange(-1024f, 1024f)]
    public float UnstartedFateModifier { get; set; } = 128f;

    [FloatRange(0f, 60f)]
    public float TimeRequiredToConsiderFate { get; set; } = 30f;

    [FloatRange(0f, 2f)]
    public float InProgressFateModifier { get; set; } = 2f;

    [Checkbox]
    public bool IgnoreMobFates { get; set; } = false;

    [FloatRange(-512f, 512f)]
    public float MobFateModifier { get; set; } = 0f;

    [Checkbox]
    public bool IgnoreBossFates { get; set; } = false;

    [FloatRange(-512f, 512f)]
    public float BossFateModifier { get; set; } = 0f;

    [Checkbox]
    public bool IgnoreCollectFates { get; set; } = false;

    [FloatRange(-512f, 512f)]
    public float CollectFateModifier { get; set; } = 0f;

    [Checkbox]
    public bool IgnoreDefendFates { get; set; } = false;

    [FloatRange(-512f, 512f)]
    public float DefendFateModifier { get; set; } = 0f;

    [Checkbox]
    public bool IgnoreEscortFates { get; set; } = true;

    [FloatRange(-512f, 512f)]
    public float EscortFateModifier { get; set; } = 0f;
}
