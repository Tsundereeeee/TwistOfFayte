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
            return Enemies
                .Where(npc => {
                    if (npc.IsTargetingPlayer())
                    {
                        return false;
                    }

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

                    return !job.HasTankStanceOn();
                });
        }
    }

    public static IEnumerable<IBattleNpc> ForlornMaidens {
        get => Enemies.Where(o => o.NameId == PriorityMob.ForlornMaiden.GetDataId());
    }

    public static IEnumerable<IBattleNpc> TheForlorns {
        get => Enemies.Where(o => o.NameId == PriorityMob.TheForlorn.GetDataId());
    }

    public static unsafe IEnumerable<IBattleNpc> Friendlies {
        // Danke Croizat
        get => Npcs.Where(o => !o.IsHostile() && o.Struct()->NamePlateIconId == 60093);
    }

    public static unsafe IEnumerable<IBattleNpc> HandIn {
        get => Npcs.Where(o => !o.IsHostile() && o.Struct()->NamePlateIconId == 60732);
    }

    public static void Update(Fate fate)
    {
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
