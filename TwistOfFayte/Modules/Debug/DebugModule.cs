using System.Linq;
using ECommons.GameHelpers;
using Ocelot.Modules;
using Ocelot.Windows;

namespace TwistOfFayte.Modules.Debug;

// [OcelotModule(ConfigOrder = int.MaxValue)]
public class DebugModule(Plugin plugin, Config config) : Module(plugin, config)
{
    public override DebugConfig Config {
        get => PluginConfig.DebugConfig;
    }

    public override void Render(RenderContext context)
    {
        if (!Config.RenderOutsideOfFate && !FateHelper.IsInFate())
        {
            return;
        }

        if (Config.RenderDistanceToEngagedEnemies && TargetHelper.InCombat.Any())
        {
            var maxDistance = TargetHelper.InCombat
                .Select(mob => Player.DistanceTo(mob) - mob.HitboxRadius)
                .Max();

            context.DrawCircle(Player.Position, maxDistance, Config.RenderDistanceToEngagedEnemiesColor);
        }

        if (Config.RenderDistanceToNotEngagedEnemies && TargetHelper.NotInCombat.Any())
        {
            var maxDistance = TargetHelper.NotInCombat
                .Select(mob => Player.DistanceTo(mob) - mob.HitboxRadius)
                .Max();

            context.DrawCircle(Player.Position, maxDistance, Config.RenderDistanceToNotEngagedEnemiesColor);
        }

        if (Config.RenderAoeRadiusAroundPlayer)
        {
            context.DrawCircle(Player.Position, 5f, Config.RenderAoeRadiusAroundPlayerColor);
        }

        if (Config.RenderLineToEngagedEnemiesOutOfAoeRadius)
        {
            foreach (var mob in TargetHelper.InCombat.Where(mob => Player.DistanceTo(mob) > 5f + mob.HitboxRadius))
            {
                context.DrawLine(mob.Position, Config.RenderLineToEngagedEnemiesOutOfAoeRadiusColor);
            }
        }

        if (Config.RenderLineToNonEngagedEnemies)
        {
            foreach (var mob in TargetHelper.NotInCombat)
            {
                context.DrawLine(mob.Position, Config.RenderLineToNonEngagedEnemiesColor);
            }
        }

        if (Config.RenderMobHitboxRadius)
        {
            foreach (var mob in TargetHelper.InCombat)
            {
                context.DrawCircle(mob.Position, mob.HitboxRadius, Config.RenderEngagedMobHitboxRadiusColor);
            }

            foreach (var mob in TargetHelper.NotInCombat)
            {
                context.DrawCircle(mob.Position, mob.HitboxRadius, Config.RenderNotEngagedMobHitboxRadiusColor);
            }
        }
    }
}
