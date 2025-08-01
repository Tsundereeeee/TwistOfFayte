using System.Numerics;
using Dalamud.Interface.Colors;
using Ocelot.Config.Attributes;
using Ocelot.Modules;

namespace TwistOfFayte.Modules.Debug;

public class DebugConfig : ModuleConfig
{
    [Checkbox]
    public bool RenderOutsideOfFate { get; set; } = false;

    [Checkbox]
    public bool RenderDistanceToEngagedEnemies { get; set; } = false;

    [Color4]
    public Vector4 RenderDistanceToEngagedEnemiesColor { get; set; } = ImGuiColors.DalamudRed;

    [Checkbox]
    public bool RenderDistanceToNotEngagedEnemies { get; set; } = false;

    [Color4]
    public Vector4 RenderDistanceToNotEngagedEnemiesColor { get; set; } = ImGuiColors.HealerGreen;

    [Checkbox]
    public bool RenderAoeRadiusAroundPlayer { get; set; } = false;

    [Color4]
    public Vector4 RenderAoeRadiusAroundPlayerColor { get; set; } = ImGuiColors.DalamudYellow;

    [Checkbox]
    public bool RenderLineToEngagedEnemiesOutOfAoeRadius { get; set; } = false;

    [Color4]
    public Vector4 RenderLineToEngagedEnemiesOutOfAoeRadiusColor { get; set; } = ImGuiColors.DPSRed;

    [Checkbox]
    public bool RenderLineToNonEngagedEnemies { get; set; } = false;

    [Color4]
    public Vector4 RenderLineToNonEngagedEnemiesColor { get; set; } = ImGuiColors.ParsedBlue;

    [Checkbox]
    public bool RenderMobHitboxRadius { get; set; } = false;

    [Color4]
    public Vector4 RenderEngagedMobHitboxRadiusColor { get; set; } = ImGuiColors.DalamudViolet;

    [Color4]
    public Vector4 RenderNotEngagedMobHitboxRadiusColor { get; set; } = ImGuiColors.ParsedPurple;
}
