using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.GatherMobs)]
public class GatherMobs(StateModule module, FateAiStateMachine stateMachine) : Handler(module, stateMachine)
{
    private const float AoeRange = 4.5f;

    // The Npc(s) to pull this run
    private readonly List<IBattleNpc> targets = [];

    private bool isComplete = false;

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

        if (mobs.Count == 1)
        {
            var single = mobs.First();
            targets.Add(single);
            module.Debug($"[GatherMobs] Only one mob available: {single.NameId} at {single.Position}");
        }
        else
        {
            Vector3? bestCenter = null;
            var bestCount = 0;
            List<IBattleNpc> bestCluster = [];

            foreach (var mob in mobs)
            {
                var center = mob.Position;
                List<IBattleNpc> cluster = [];

                foreach (var other in mobs)
                {
                    var combinedHitbox = other.HitboxRadius + AoeRange;
                    var dist = Vector3.Distance(center, other.Position);

                    if (dist <= combinedHitbox)
                    {
                        cluster.Add(other);
                    }
                }

                module.Debug($"[GatherMobs] Cluster check: mob {mob.NameId} at {center} has {cluster.Count} in range");

                if (cluster.Count > bestCount)
                {
                    bestCount = cluster.Count;
                    bestCluster = cluster;
                    bestCenter = center;
                }
            }

            if (bestCluster.Count > 0)
            {
                targets.AddRange(bestCluster);
                module.Debug($"[GatherMobs] Best AoE cluster: {bestCluster.Count} mobs centered around {bestCenter}");
            }
            else
            {
                var fallback = mobs.FirstOrDefault();
                if (fallback != null)
                {
                    targets.Add(fallback);
                    module.Debug($"[GatherMobs] No valid cluster. Fallback to closest mob: {fallback.NameId} at {fallback.Position}");
                }
            }
        }


        if (targets.Count == 1 && !Prowler.IsRunning)
        {
            var target = targets.First();
            Svc.Targets.Target = target;

            var radius = target.HitboxRadius;
            var destination = target.Position.GetPointFromPlayer(radius + 2f, radius);

            module.Debug($"[GatherMobs] Prowling to single mob {target.NameId} at {target.Position}, destination: {destination}");

            Prowler.Prowl(new Prowl(destination) {
                ShouldFly = _ => false,
                ShouldMount = _ => false,
                PostProcessor = prowl => prowl.Nodes = prowl.Nodes.Smooth(),
                Watcher = prowl => {
                    var playerDist = Player.DistanceTo(prowl.Destination);
                    var targetDist = Vector3.Distance(target.Position, prowl.Destination);
                    var attackRange = Player.Job.GetRange();

                    if (playerDist <= 1f)
                    {
                        module.Debug($"[GatherMobs] Reached single-target position (distance {playerDist:0.00}).");
                        return true;
                    }

                    if (target.IsTargetingPlayer())
                    {
                        module.Debug($"[GatherMobs] Mob {target.NameId} has aggroed on player.");
                        return true;
                    }

                    if (targetDist > attackRange + target.HitboxRadius)
                    {
                        module.Debug($"[GatherMobs] Mob {target.NameId} moved too far (distance {targetDist:0.00}, range {attackRange}).");
                        return true;
                    }

                    return false;
                },
                OnComplete = (_, _) => isComplete = true,
                OnCancel = (_, _) => isComplete = true,
            });
        }

        if (targets.Count > 1 && !Prowler.IsRunning)
        {
            var point = Vector3.Zero;
            foreach (var mob in targets)
            {
                point += mob.Position;
            }

            point /= targets.Count;
            var destination = point.GetPointFromPlayer(0.25f);

            module.Debug($"[GatherMobs] Prowling to AoE cluster center at {point}, destination: {destination}");

            Prowler.Prowl(new Prowl(destination) {
                ShouldFly = _ => false,
                ShouldMount = _ => false,
                PostProcessor = prowl => prowl.Nodes = prowl.Nodes.Smooth(),
                Watcher = prowl => {
                    var playerDist = Player.DistanceTo(prowl.Destination);
                    if (playerDist <= 0.25f)
                    {
                        module.Debug($"[GatherMobs] Reached AoE cluster point (distance {playerDist:0.00}).");
                        return true;
                    }

                    var stillInRange = targets.Count(mob =>
                        Vector3.Distance(mob.Position, prowl.Destination) <= AoeRange + mob.HitboxRadius);

                    module.Debug($"[GatherMobs] Cluster integrity check: {stillInRange}/{targets.Count} still in AoE range.");

                    return stillInRange > 1;
                },
                OnComplete = (_, _) => isComplete = true,
                OnCancel = (_, _) => isComplete = true,
            });
        }
    }

    public override FateAiState? Handle()
    {
        return targets.Count == 0 || isComplete || !Prowler.IsRunning ? StateMachine.MakeChoice() : null;
    }
}
