using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Ocelot.Gameplay;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.PathfindingToFate)]
public class PathfindingToFate : StateHandler<State, StateModule>
{
    private Vector3? Destination = null;

    private bool IsPathingToNpc = false;

    private Path? Path = null;

    public override void Enter(StateModule module)
    {
        Destination = null;
        IsPathingToNpc = false;
        Path = null;
    }

    public override State? Handle(StateModule module)
    {
        if (FateHelper.SelectedFate == null)
        {
            module.Debug("Fate ended, returning to idle");
            return State.Idle;
        }

        if (Destination == null)
        {
            Destination = FateHelper.SelectedFate.GetDestination();
            module.Debug($"Selected point in fate radius '{Destination:f2}'");
            // var floor = module.VNavmesh.FindPointOnFloor(Destination!.Value, false, 10);
            // if (floor.HasValue && floor != Vector3.NaN)
            // {
            //     module.Debug($"Setting destination to point on floor. From '{Destination:f2}' to '{floor:f2}'");
            //     Destination = floor.Value;
            // }
        }

        // Path to starting NPC if needed
        if (Destination.HasValue && FateHelper.SelectedFate.State == FateState.Preparation && !IsPathingToNpc && TargetHelper.Friendlies.Any())
        {
            var position = TargetHelper.Friendlies.First().Position;
            if (position != Vector3.NaN)
            {
                Destination = position;
                IsPathingToNpc = true;
                module.VNavmesh.Stop();

                module.Debug($"Re-pathing to starting npc {Destination:f2}");

                Path?.Dispose();
                Path = null;
            }
        }

        var distance = Player.DistanceTo(Destination!.Value);
        if (distance <= 5f) // @todo: make configurable?
        {
            module.Debug($"Close enough to Destination {Destination:f2} ({distance}/5.0)");
            module.VNavmesh.Stop();
            return FateHelper.SelectedFate.State == FateState.Preparation ? State.StartingFate : State.ParticipatingInFate;
        }

        if (Destination.HasValue)
        {
            if (Path?.IsDone == true)
            {
                Path = null;
            }

            if (Path == null)
            {
                module.Debug($"Generating new path to {Destination.Value:f2}");
                Path = Path.Fly(Destination.Value, module.VNavmesh, path => path.Smooth());
            }
        }

        if (!Player.Mounted && !Player.Mounting && EzThrottler.Throttle("Mount", 2500))
        {
            module.Debug("Ensuring player is mounted");
            Plugin.Chain.Submit(Actions.MountRoulette.GetCastChain());
        }

        return null;
    }
}
