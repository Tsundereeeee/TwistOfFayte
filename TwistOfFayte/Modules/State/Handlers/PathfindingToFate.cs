using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Ocelot.Gameplay;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.PathfindingToFate)]
public class PathfindingToFate : StateHandler<State, StateModule>
{
    private Vector3? Destination = null;

    private bool IsPathingToNpc = false;

    private bool IsComplete = false;

    public override void Enter(StateModule module)
    {
        Destination = null;
        IsPathingToNpc = false;
        IsComplete = false;
        
        Destination = FateHelper.SelectedFate?.GetDestination();
    }

    public override State? Handle(StateModule module)
    {
        if (FateHelper.SelectedFate == null || Destination == null)
        {
            module.Debug("Fate ended, returning to idle");
            return State.Idle;
        }

        if (IsComplete || FateHelper.CurrentFate?.IsSelected() == true)
        {
            return FateHelper.SelectedFate.State == FateState.Preparation ? State.StartingFate : State.ParticipatingInFate;
        }

        // Path to starting NPC if needed
        if (FateHelper.SelectedFate.IsInPreparation() && !IsPathingToNpc && TargetHelper.Friendlies.Any())
        {
            var npc = TargetHelper.Friendlies.First();
            IsPathingToNpc = true;

            module.Debug($"Re-pathing to starting npc {Destination:f2}");
            
            Prowler.Prowl(new (npc) {
                ShouldFly = true,
                PreProcessor = prowl => {
                    if (prowl.GameObject == null)
                    {
                        Destination =  prowl.OriginalDestination;
                        return (prowl.OriginalStart, prowl.OriginalDestination);    
                    }

                    Destination = prowl.OriginalDestination.GetPointFromPlayer(prowl.GameObject.HitboxRadius, prowl.GameObject.HitboxRadius + 2f);
                    
                    return (prowl.OriginalStart, Destination.Value);
                },
                PostProcessor = prowl => prowl.OriginalNodes.ContinueFrom(Player.Position).Smooth(),
                Watcher = prowl => Player.DistanceTo(prowl.Destination) <= 5f,
                OnComplete = (_, _) => IsComplete = true,
            });
        }

        var distance = Player.DistanceTo(Destination!.Value);
        if (distance <= 5f) // @todo: make configurable?
        {
            module.Debug($"Close enough to Destination {Destination:f2} ({distance}/5.0)");
            module.VNavmesh.Stop();
            return FateHelper.SelectedFate.State == FateState.Preparation ? State.StartingFate : State.ParticipatingInFate;
        }

        if (Destination.HasValue && !Prowler.IsRunning)
        {
            Prowler.Prowl(new Prowl(Destination.Value) {
                ShouldFly = true,
                PostProcessor = prowl => prowl.OriginalNodes.Smooth(),
                Watcher = prowl => Player.DistanceTo(prowl.Destination) <= 5f,
                OnComplete = (_, _) => IsComplete = true,
            });
        }

        if (!Player.Mounted && !Player.Mounting && EzThrottler.Throttle("Mount", 2500))
        {
            module.Debug("Ensuring player is mounted");
            Plugin.Chain.Submit(Actions.MountRoulette.GetCastChain());
        }

        return null;
    }
}
