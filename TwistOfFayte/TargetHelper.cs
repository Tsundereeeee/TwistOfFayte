using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ObjectLifeTracker;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using TwistOfFayte.Data;

namespace TwistOfFayte;

public static class TargetHelper
{
    public static float MeleeDistance { get; private set; } = 3.5f;

    public static float RangedDistance { get; private set; } = 25f;

    private static IGameObject? LastTarget = null;

    public static event Action<IGameObject?>? OnTargetChanged;

    public static IEnumerable<IBattleNpc> Npcs { get; private set; } = [];

    public static IEnumerable<IBattleNpc> Enemies {
        get => Npcs.Where(o => o.IsHostile());
    }

    public static IEnumerable<IBattleNpc> InCombat {
        get => Enemies.Where(o => o.IsTargetingPlayer());
    }

    public static IEnumerable<IBattleNpc> InCombatOutOfRange {
        get => InCombat.Where(mob => Player.DistanceTo(mob) > 5f + mob.HitboxRadius);
    }

    public static unsafe IEnumerable<IBattleNpc> NotInCombat {
        get {
            var notInCombat = Enemies
                .Where(npc => {
                    if (npc.GetLifeTimeSeconds() < 2)
                    {
                        return false;
                    }

                    if (npc.TargetObject == null)
                    {
                        return true;
                    }

                    var target = (BattleChara*)npc.TargetObject.Address;
                    if (!target->IsCharacter())
                    {
                        return true;
                    }

                    var job = (Job)target->ClassJob;
                    if (!job.IsTank())
                    {
                        return true;
                    }


                    return true;
                });

            return Enemies.Where(o => !o.HasTarget());
        }
    }

    public static IEnumerable<IBattleNpc> ForlornMaidens {
        get => Enemies.Where(o => o.NameId == PriorityMob.ForlornMaiden.GetDataId());
    }

    public static IEnumerable<IBattleNpc> TheForlorns {
        get => Enemies.Where(o => o.NameId == PriorityMob.TheForlorn.GetDataId());
    }

    public static IEnumerable<IBattleNpc> Friendlies {
        get => Npcs.Where(o => !o.IsHostile());
    }

    public static void Update(Fate fate)
    {
        if (LastTarget?.EntityId != Svc.Targets.Target?.EntityId)
        {
            OnTargetChanged?.Invoke(LastTarget);
        }

        LastTarget = Svc.Targets.Target;

        Npcs = Svc.Objects.OfType<IBattleNpc>()
            .Where(o => o is {
                IsDead: false,
                IsTargetable: true,
            })
            .Where(o => o.IsFateTarget(fate))
            .OrderBy(Player.DistanceTo);
    }

    public static void Clear()
    {
        Npcs = [];
    }
}
