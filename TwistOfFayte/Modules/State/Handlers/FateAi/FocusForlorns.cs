using System.Linq;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Ocelot.Gameplay.Rotation;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.FocusForlorns)]
public class FocusForlorns(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;

    private IRotationPlugin rotation = null!;

    public override float GetScore()
    {
        return TargetHelper.ForlornMaidens.Any() || TargetHelper.TheForlorns.Any() ? float.MaxValue : float.MinValue;
    }

    public override void Enter()
    {
        Svc.Commands.ProcessCommand("/bmr ar set Full Auto");
        rotation = RotationHelper.GetPlugin(module);
        rotation.DisableAoe();
    }

    public override void Exit()
    {
        rotation.Dispose();
        Prowler.Abort();
    }

    public override bool Handle()
    {
        if (TargetHelper.ForlornMaidens.Any())
        {
            Svc.Targets.Target = TargetHelper.ForlornMaidens.First();
        }

        if (TargetHelper.TheForlorns.Any())
        {
            Svc.Targets.Target = TargetHelper.TheForlorns.First();
        }
        
        return !TargetHelper.ForlornMaidens.Any() && !TargetHelper.TheForlorns.Any();
    }
}
