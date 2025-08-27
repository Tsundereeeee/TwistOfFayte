using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using Ocelot.Chain;
using Ocelot.Chain.ChainEx;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.GatherMobs)]
public class GatherMobs(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;

    private const float AoeRange = 4.5f;

    // The Npc(s) to pull this run
    private readonly List<IBattleNpc> targets = [];

    private bool isComplete = false;

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
        isComplete = false;
        Prowler.Abort();
        targets.Clear();

        var mobs = TargetHelper.NotInCombat.ToList();
        module.Debug($"[GatherMobs] Found {mobs.Count} non-combat mobs.");

        if (mobs.Count == 0)
        {
            module.Debug("[GatherMobs] No mobs available to target.");
            return;
        }

        Svc.Commands.ProcessCommand("/bmr ar set Full Auto");
        var fallback = mobs.FirstOrDefault();
        if (fallback != null)
        {
            targets.Add(fallback);
            module.Debug($"[GatherMobs] No valid cluster. Fallback to closest mob: {fallback.NameId} at {fallback.Position}");
        }

        var target = targets.FirstOrDefault();
        if (target == null)
        {
            return;
        }
        Svc.Targets.Target = target;
        Plugin.Chain.Submit(() => {
            return Chain.Create($"GatherMobs")
                .Wait(2500)
                .BreakIf(() => Svc.Targets.Target == null || Svc.Targets.Target.IsTargetingPlayer())
                .Then(_ => Prowler.Prowl(new Prowl(target.Position) {
                    ShouldFly = _ => false,
                    ShouldMount = _ => false,
                    Watcher = _ => Svc.Targets.Target == null || Svc.Targets.Target.IsTargetingPlayer(),
                    OnComplete = (_, _) => isComplete = true,
                    OnCancel = (_, _) => isComplete = true,
                }))
                .OnCancel(() => isComplete = true);
        });
    }

    public override bool Handle()
    {
        return targets.Count == 0 || isComplete || (!Prowler.IsRunning && !Plugin.Chain.IsRunning);
    }
}
