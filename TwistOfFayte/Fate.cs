using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel.Sheets;
using Ocelot.Modules;
using TwistOfFayte.Data;
using TwistOfFayte.Modules.State.Handlers;
using TwistOfFayte.Modules.Tracker;
using TwistOfFayte.Zone;
using FateState = Dalamud.Game.ClientState.Fates.FateState;
using FateData = Lumina.Excel.Sheets.Fate;

namespace TwistOfFayte;

public class Fate : IEquatable<Fate>
{
    public readonly uint Id;

    public readonly Vector3 Position;

    public readonly float Radius;

    public readonly string Name;

    public readonly bool IsBonus;

    public readonly int MaxLevel;

    public readonly uint IconId;

    public readonly FateType Type;

    public readonly FateProgress ProgressTracker = new();

    public readonly Score Score = new();

    public readonly FateData GameData;

    public readonly EventItem? EventItem;

    private unsafe Fate(FateContext* context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Id = context->FateId;
        Position = context->Location;
        Radius = context->Radius;
        Name = context->Name.ToString();
        IsBonus = context->IsBonus;
        MaxLevel = context->MaxLevel;
        IconId = context->IconId;
        Type = Enum.IsDefined(typeof(FateType), context->IconId) ? (FateType)context->IconId : FateType.Unknown;

        GameData = Svc.Data.GetExcelSheet<FateData>().GetRow(Id);
        if (GameData.EventItem.IsValid)
        {
            EventItem = GameData.EventItem.Value;
        }

        if (Position == Vector3.Zero || Position == Vector3.NaN)
        {
            throw new ArgumentException("Fate position was invalid");
        }
    }

    public unsafe Fate(IFate fate) : this((FateContext*)fate.Address) { }

    public static unsafe Fate Current()
    {
        if (!FateHelper.IsInFate())
        {
            throw new Exception("There is no current fate.");
        }

        return new Fate(FateManager.Instance()->CurrentFate);
    }

    private IFate? Data {
        get => Svc.Fates.FirstOrDefault(fate => fate.FateId == Id);
    }

    public bool IsActive {
        get => Data is { State: FateState.Preparation } or { State: FateState.Running };
    }

    public FateState State {
        get => Data?.State ?? FateState.Ended;
    }

    public byte Progress {
        get => Data?.Progress ?? 0;
    }

    public void Update(UpdateContext context)
    {
        UpdateScore(context);

        if (Progress <= 0)
        {
            return;
        }

        if (ProgressTracker.Count == 0 || ProgressTracker.Latest != Progress)
        {
            ProgressTracker.Add(Progress);
        }
    }


    public void UpdateScore(UpdateContext context)
    {
        if (!context.IsForModule<TrackerModule>(out var module))
        {
            return;
        }

        Score.Clear();
        if (context.Plugin is Plugin plugin && IsBlacklisted(plugin))
        {
            return;
        }

        var config = module.SelectorModule.Config;

        if (IsCurrent())
        {
            Score.Add("Current", 1024f);
            return;
        }

        var typeScore = Type switch {
            FateType.Mobs => module.PluginConfig.SelectorConfig.MobFateModifier,
            FateType.Boss => module.PluginConfig.SelectorConfig.BossFateModifier,
            FateType.Collect => module.PluginConfig.SelectorConfig.CollectFateModifier,
            FateType.Defend => module.PluginConfig.SelectorConfig.DefendFateModifier,
            FateType.Escort => module.PluginConfig.SelectorConfig.EscortFateModifier,
            _ => 0f,
        };

        if (typeScore != 0f)
        {
            Score.Add("Type", typeScore);
        }

        var aetheryteDistance = ZoneHelper.GetAetherytes()
            .Select(a => Vector3.Distance(Position, a.Position))
            .Order()
            .FirstOrDefault(float.MaxValue);

        var playerDistance = Vector3.Distance(Position, Player.Position);

        var distance = Math.Min(aetheryteDistance, playerDistance);
        if (!module.PluginConfig.GeneralConfig.ShouldTeleport)
        {
            distance = playerDistance;
        }

        Score.Add("Distance", (2048 - distance) / 25f);

        var teleportRequired = aetheryteDistance < playerDistance && module.PluginConfig.GeneralConfig.ShouldTeleport;
        if (teleportRequired)
        {
            Score.Add("Teleport Time", -(config.TimeToTeleport * config.CostPerYalm));
        }

        if (IsBonus)
        {
            Score.Add("Bonus Modifier", config.BonusFateModifier);
        }

        var estimate = ProgressTracker.EstimateTimeToCompletion();
        if (estimate == null)
        {
            Score.Add("Unstarted Modifier", config.UnstartedFateModifier);
        }
        else
        {
            var timeLeft = (float)estimate.Value.TotalSeconds;
            var timeToReach = distance / config.CostPerYalm;
            if (teleportRequired)
            {
                timeToReach += config.TimeToTeleport;
            }

            // Less than about 30 seconds left when we would arrive
            if (timeToReach > timeLeft - config.TimeRequiredToConsiderFate)
            {
                Score.Clear();
                return;
            }

            Score.Add("In Progress Modifier", (timeLeft - timeToReach) * config.InProgressFateModifier);
        }
    }


    public Vector3 GetDestination()
    {
        return Position.GetPointFromPlayer(Radius);
    }

    public bool NeedSync()
    {
        return MaxLevel < Player.Level && !Player.IsLevelSynced;
    }

    public bool IsInPreparation()
    {
        return State == FateState.Preparation;
    }

    public bool IsSelected()
    {
        return this == FateHelper.SelectedFate;
    }

    public bool IsCurrent()
    {
        return this == FateHelper.CurrentFate;
    }

    public bool IsBlacklisted(Plugin plugin)
    {
        return plugin.Config.FateBlacklist.Contains(Id);
    }

    public bool ShouldTeleport()
    {
        var aetheryteDistance = ZoneHelper.GetAetherytes()
                                    .Select(a => Vector3.Distance(Position, a.Position))
                                    .Order()
                                    .FirstOrDefault(float.MaxValue) + 120f;

        var playerDistance = Vector3.Distance(Position, Player.Position);

        return aetheryteDistance < playerDistance;
    }

    public void Teleport()
    {
        var aetheryte = ZoneHelper.GetAetherytes()
            .OrderBy(a => Vector3.Distance(Position, a.Position))
            .First();

        aetheryte.Teleport();
    }

    public int GetCurrentHandInInInventory()
    {
        return EventItem?.Count() ?? 0;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Fate);
    }

    public bool Equals(Fate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Fate? left, Fate? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Fate? left, Fate? right)
    {
        return !Equals(left, right);
    }
}
