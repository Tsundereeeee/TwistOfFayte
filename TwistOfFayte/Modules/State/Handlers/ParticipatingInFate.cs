using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Ocelot.Gameplay;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.ParticipatingInFate)]
public class ParticipatingInFate : StateHandler<State, StateModule>
{
    private Path? Path = null;

    private IGameObject? LastTarget = null;

    private float? TargetOffset = null;

    public override void Enter(StateModule module)
    {
        Svc.Targets.Target = null;
        module.TargetModule.ShouldTarget = true;
        TargetHelper.OnTargetChanged += OnTargetChanged;
    }

    public override void Exit(StateModule module)
    {
        module.TargetModule.ShouldTarget = false;
        TargetHelper.OnTargetChanged -= OnTargetChanged;
        ClearPath();
    }

    private void OnTargetChanged(IGameObject? target)
    {
        LastTarget = target;
        TargetOffset = null;
        ClearPath();
    }

    private void ClearPath()
    {
        Path?.Dispose();
        Path = null;
    }

    public override unsafe State? Handle(StateModule module)
    {
        if (Player.Mounted)
        {
            Actions.TryUnmount();
        }

        if (!FateHelper.IsInFate())
        {
            return State.Idle;
        }

        if (Path?.IsDone == true)
        {
            Path?.Dispose();
            Path = null;
        }

        if (FateHelper.CurrentFate?.NeedSync() == true && EzThrottler.Throttle("Fate Sync"))
        {
            FateManager.Instance()->LevelSync();
        }

        var target = Svc.Targets.Target;
        if (target != null && target.EntityId != LastTarget?.EntityId && !target.IsTargetingPlayer())
        {
            var isMelee = Player.Job.IsMelee();
            var baseDistance = isMelee ? TargetHelper.MeleeDistance : TargetHelper.RangedDistance;

            TargetOffset ??= isMelee ? (float)(Random.Shared.NextDouble() * 0.5 - 0.25) : (float)(Random.Shared.NextDouble() * 3.0 - 1.5);

            var desiredDistance = Math.Clamp(baseDistance + TargetOffset.Value, 1.0f, baseDistance);
            var distance = Player.DistanceTo(target) - target.HitboxRadius;

            if (distance >= desiredDistance && !module.VNavmesh.IsRunning() && Path == null)
            {
                var dir = Vector3.Normalize(target.Position - Player.Position);
                var destination = target.Position - dir * (target.HitboxRadius + desiredDistance);
                module.VNavmesh.Stop();
                Path = Path.Walk(destination, module.VNavmesh, path => path.Count <= 0 ? path : path.Smooth())
                    .WithWatcher(_ => {
                        if (target.IsTargetingPlayer())
                        {
                            module.VNavmesh.Stop();
                        }
                    });
            }
        }

        // If we're in large-scale combat, and not moving, go to the center of action
        if (!TargetHelper.NotInCombat.Any()
            && TargetHelper.InCombat.Count() >= 2
            && !module.VNavmesh.IsRunning()
            && Path == null)
        {
            var destination = TargetHelper.InCombat.Select(t => t.Position).ToList().Center();
            if (Player.DistanceTo(destination) >= 1.5f)
            {
                module.VNavmesh.Stop();
                Path = Path.Walk(destination, module.VNavmesh, path => path.Count <= 0 ? path : path.Smooth());
            }
        }

        return null;
    }
}
