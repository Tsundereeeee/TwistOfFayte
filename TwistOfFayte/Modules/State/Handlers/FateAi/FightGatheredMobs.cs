using System;
using System.Linq;
using System.Numerics;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.FightGatheredMobs)]
public class FightGatheredMobs(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;

    private bool isComplete = false;

    private DateTime enterTime = DateTime.Now;

    private bool IsComplete {
        get => (isComplete || !TargetHelper.InCombat.Any() || !Prowler.IsRunning) && DateTime.Now.Subtract(enterTime).TotalSeconds > 1;
    }

    public override float GetScore()
    {
        var inCombat = TargetHelper.InCombat.ToList();

        if (inCombat.Count == 0)
        {
            return 0f;
        }

        if (!TargetHelper.NotInCombat.Any())
        {
            return 100f;
        }

        var max = module.PluginConfig.TargetConfig.MaxMobsToFight;
        if (max == 0)
        {
            max = int.MaxValue;
        }

        return TargetHelper.InCombat.Count() >= max ? 100f : 0f;
    }

    public override void Enter()
    {
        if (!TargetHelper.InCombat.Any())
        {
            isComplete = true;
            return;
        }
        
        Svc.Commands.ProcessCommand("/bmr ar set Full Auto");
        
        isComplete = false;
        enterTime = DateTime.Now;
        Prowler.Abort();
        
        var mob = TargetHelper.InCombat.First();
        Svc.Targets.Target ??= mob;
        
        isComplete = true;
    }

    public override bool Handle()
    {
        return IsComplete;
    }
}
