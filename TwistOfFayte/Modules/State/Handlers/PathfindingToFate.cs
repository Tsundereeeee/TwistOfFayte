using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Ocelot.Gameplay;
using Ocelot.Prowler;
using Ocelot.States;
using SharpDX.DirectWrite;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.PathfindingToFate)]
public class PathfindingToFate(StateModule module) : StateHandler<State, StateModule>(module)
{
    private readonly StateModule module = module;

    private bool isComplete = false;

    public override void Enter()
    {
        isComplete = false;
        Prowler.Abort();

        if (FateHelper.SelectedFate == null)
        {
            isComplete = true;
            return;
        }

        Prowler.Prowl(new Prowl(FateHelper.SelectedFate.GetDestination())
        {
            ShouldFly = prowl => prowl.EuclideanDistance >= 30f,
            ShouldMount = prowl => prowl.PathLength >= 30f,
            Mount = Module.PluginConfig.GeneralConfig.MountRoulette ? 0 : Module.PluginConfig.GeneralConfig.Mount,
            PostProcessor = prowl => prowl.Nodes = prowl.Nodes.Smooth(),
            Watcher = prowl => {
                if (Player.DistanceTo(prowl.Destination) <= 5f || !FateHelper.SelectedFate.IsActive)
                {
                    return true;
                }

                if (prowl.GameObject != null)
                {
                    return false;
                }

                if (TargetHelper.Friendlies.Any())
                {
                    module.Debug("Redirecting to npc");
                    prowl.Redirect(TargetHelper.Friendlies.First());
                }


                return false;
            },
            OnComplete = (_, _) => isComplete = true,
        });
    }

    public override State? Handle()
    {
        if (isComplete)
        {
            if (FateHelper.SelectedFate != null)
            {
                return FateHelper.SelectedFate.State == FateState.Preparation ? State.StartingFate : State.ParticipatingInFate;
            }

            return State.Idle;
        }

        return null;
    }
}
