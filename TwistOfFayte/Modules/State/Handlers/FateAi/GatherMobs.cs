using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Ocelot.Chain;
using Ocelot.Chain.ChainEx;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.GatherMobs)]
public class GatherMobs(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;

    public override float GetScore()
    {
        if (FateHelper.CurrentFate == null)
        {
            return 0f;
        }

        var max = module.PluginConfig.TargetConfig.MaxMobsToFight;
        if (max == 0)
        {
            max = int.MaxValue;
        }

        var mobsLeft = FateHelper.CurrentFate?.ProgressTracker.EstimateEnemiesRemaining();
        if (mobsLeft > 0 && TargetHelper.InCombat.Count() >= mobsLeft)
        {
            return 0f;
        }

        return TargetHelper.InCombat.Count() < max ? 99f : 0f;
    }

    public override void Enter()
    {
        if (Prowler.IsRunning || Plugin.Chain.IsRunning)
        {
            Prowler.Abort();
        }
        Svc.Commands.ProcessCommand("/bmr ar set Full Auto");
    }

    public override bool Handle()
    {
        if (Prowler.IsRunning || Plugin.Chain.IsRunning || TargetHelper.InCombat.Any())
        {
            Svc.Targets.Target ??= TargetHelper.InCombat.FirstOrDefault();
            return true;
        }
        var mobs = TargetHelper.NotInCombat.ToList();
        if (mobs.Count == 0)
        {
            var fatePosition = FateHelper.CurrentFate.Position;
            if (Player.DistanceTo(fatePosition) > 2f)
            {
                Prowler.Prowl(new Prowl(fatePosition) {
                    ShouldFly = _ => false,
                    ShouldMount = _ => false,
                    Watcher = _ => TargetHelper.InCombat.Any()
                });
            }
            return false;
        }
        var target = mobs.FirstOrDefault();
        if (target == null)
        {
            return false;
        }
        Svc.Targets.Target = target;
        Plugin.Chain.Submit(() => {
            return Chain.Create($"GatherMobs")
                .Wait(2500)
                .BreakIf(() => Svc.Targets.Target == null || Svc.Targets.Target.IsTargetingPlayer())
                .Then(_ => Prowler.Prowl(new Prowl(target.Position) {
                    ShouldFly = _ => false,
                    ShouldMount = _ => false,
                    Watcher = _ => Svc.Targets.Target == null || Svc.Targets.Target.IsTargetingPlayer()
                }));
        });
        return false;
    }
}
