using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Ocelot.Modules;
using TwistOfFayte.Modules.Tracker;
using TwistOfFayte.Zone;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace TwistOfFayte;

public class Fate : IEquatable<Fate>
{
    public readonly ushort Id;

    public readonly Vector3 Position;

    public readonly float Radius;

    public readonly string Name;

    public readonly bool IsBonus;

    public readonly int MaxLevel;

    public readonly FateProgress ProgressTracker = new();

    public float Score { get; private set; } = float.MinValue;

    private unsafe Fate(FateContext* context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Id = context->FateId;
        Position = context->Location;
        Radius = context->Radius;
        Name = context->Name.ToString();
        IsBonus = context->IsBonus;
        MaxLevel = context->MaxLevel;

        if (Position == Vector3.Zero || Position == Vector3.NaN)
        {
            throw new ArgumentException("Fate position was invalid");
        }
    }

    public Fate(IFate fate)
    {
        Id = fate.FateId;
        Position = fate.Position;
        Radius = fate.Radius;
        Name = fate.Name.ToString();
        IsBonus = fate.HasBonus;
        MaxLevel = fate.MaxLevel;

        if (Position == Vector3.Zero || Position == Vector3.NaN)
        {
            throw new ArgumentException("Fate position was invalid");
        }
    }

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

        var config = module.SelectorModule.Config;

        var score = 4096f;

        var aetheryteDistance = ZoneHelper.GetAetherytes()
            .Select(a => Vector3.Distance(Position, a.Position))
            .Order()
            .FirstOrDefault(float.MaxValue);

        var playerDistance = Vector3.Distance(Position, Player.Position);

        var distance = Math.Min(aetheryteDistance, playerDistance);
        score -= distance;

        var teleportRequired = aetheryteDistance < playerDistance;
        if (teleportRequired)
        {
            score -= config.TimeToTeleport * config.CostPerYalm;
        }

        if (IsBonus)
        {
            score += config.BonusFateModifier;
        }

        var estimate = ProgressTracker.EstimateTimeToCompletion();
        if (estimate == null)
        {
            score += config.UnstartedFateModifier;
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
                Score = float.MinValue;
                return;
            }

            score += (timeLeft - timeToReach) * config.InProgressFateModifier;
        }

        Score = score;
    }


    public Vector3 GetDestination()
    {
        var toFate = Position - Player.Position;
        var direction = Vector3.Normalize(toFate);

        var angle = (float)(Random.Shared.NextDouble() * MathF.PI / 3 - MathF.PI / 6);
        var sin = MathF.Sin(angle);
        var cos = MathF.Cos(angle);

        var rotatedDirection = new Vector3(direction.X * cos - direction.Z * sin, 0, direction.X * sin + direction.Z * cos);
        var distance = (float)(Radius * (0.3 + Random.Shared.NextDouble() * 0.4));

        return Position - rotatedDirection * distance;
    }

    public bool NeedSync()
    {
        return MaxLevel < Player.Level && !Player.IsLevelSynced;
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
