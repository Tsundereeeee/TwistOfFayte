using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;

namespace TwistOfFayte;

public static class IBattleNpcListEx
{
    public static IBattleNpc? Closest(this IEnumerable<IBattleNpc> enemies)
    {
        return enemies.FirstOrDefault();
    }

    public static IBattleNpc? Furthest(this IEnumerable<IBattleNpc> enemies)
    {
        return enemies.FirstOrDefault();
    }

    public static IBattleNpc? Centroid(this IEnumerable<IBattleNpc> enemies)
    {
        var list = enemies.ToList();

        var sum = Vector3.Zero;
        foreach (var npc in list)
        {
            sum += npc.Position;
        }

        var centroid = sum / list.Count;

        return list
            .OrderBy(npc => Vector3.DistanceSquared(npc.Position, centroid))
            .FirstOrDefault();
    }
}

public static class IGameObjectEx
{
    public static bool HasTarget(this IGameObject obj)
    {
        return obj.TargetObject != null;
    }

    public static bool IsTargetingPlayer(this IGameObject obj)
    {
        return obj.TargetObject?.Address == Player.Object.Address;
    }
}

public static class IBattleNpcEx
{
    public static unsafe bool IsFateTarget(this IBattleNpc npc, Fate fate)
    {
        var battleChara = (BattleChara*)npc.Address;

        return battleChara->FateId == fate.Id;
    }

    public static bool IsInCombat(this IBattleNpc npc)
    {
        return TargetHelper.InCombat.Contains(npc);
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(
            str.Select((ch, i) =>
                i > 0 && char.IsUpper(ch) ? "_" + char.ToLower(ch) : char.ToLower(ch).ToString()
            )
        );
    }
}

public static class PathExtensions
{
    public static List<Vector3> ContinueFrom(this List<Vector3> points, Vector3 point)
    {
        if (points.Count == 0)
        {
            return [];
        }

        var bestIndex = 0;
        var bestDistance = float.MaxValue;

        for (var i = 0; i < points.Count; i++)
        {
            var totalDistance = Vector3.Distance(point, points[i]);

            for (var j = i; j < points.Count - 1; j++)
            {
                totalDistance += Vector3.Distance(points[j], points[j + 1]);
            }

            if (totalDistance < bestDistance)
            {
                bestDistance = totalDistance;
                bestIndex = i;
            }
        }

        return points.GetRange(bestIndex, points.Count - bestIndex);
    }


    public static List<Vector3> Smooth(this List<Vector3> points, float pointsPerUnit = 0.25f, int minSegments = 2)
    {
        if (points.Count < 2)
        {
            return points;
        }

        points = points.Distinct().ToList();
        var smoothed = new List<Vector3> { points[0] };

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : p2;

            const float t0 = 0.0f;
            var t1 = GetT(t0, p0, p1);
            var t2 = GetT(t1, p1, p2);
            var t3 = GetT(t2, p2, p3);

            var segmentLength = Vector3.Distance(p1, p2);
            var segments = Math.Max(minSegments, (int)(segmentLength * pointsPerUnit));

            for (var j = 0; j <= segments; j++)
            {
                var t = t1 + (t2 - t1) * (j / (float)segments);
                var A1 = Lerp(p0, p1, (t1 - t) / (t1 - t0), (t - t0) / (t1 - t0));
                var A2 = Lerp(p1, p2, (t2 - t) / (t2 - t1), (t - t1) / (t2 - t1));
                var A3 = Lerp(p2, p3, (t3 - t) / (t3 - t2), (t - t2) / (t3 - t2));

                var B1 = Lerp(A1, A2, (t2 - t) / (t2 - t0), (t - t0) / (t2 - t0));
                var B2 = Lerp(A2, A3, (t3 - t) / (t3 - t1), (t - t1) / (t3 - t1));

                var C = Lerp(B1, B2, (t2 - t) / (t2 - t1), (t - t1) / (t2 - t1));

                smoothed.Add(C);
            }
        }

        smoothed.Add(points[^1]);

        return smoothed
            .Where(v => !float.IsNaN(v.X) && !float.IsNaN(v.Y) && !float.IsNaN(v.Z))
            .ToList();
    }

    private static float GetT(float t, Vector3 p0, Vector3 p1)
    {
        return MathF.Pow(Vector3.Distance(p0, p1), 0.5f) + t;
    }

    private static Vector3 Lerp(Vector3 a, Vector3 b, float w1, float w2)
    {
        return a * w1 + b * w2;
    }

    public static Vector3 Center(this List<Vector3> points)
    {
        {
            if (points.Count == 0)
            {
                return Vector3.Zero;
            }

            var sum = Vector3.Zero;
            foreach (var point in points)
            {
                sum += point;
            }

            return sum / points.Count;
        }
    }
}

public static class JobExtensions
{
    private static ClassJob? GetData(this Job job)
    {
        return Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault((uint)job);
    }

    public static bool IsMelee(this Job job)
    {
        var data = job.GetData();
        if (data == null)
        {
            return true;
        }

        // 0 = crafter/gatherer, 1 = tank, 2 = melee
        return data.Value.Role <= 2;
    }

    public static float GetRange(this Job job)
    {
        return job.IsMelee() ? 3.5f : 25f;
    }

    public static bool IsTank(this Job job)
    {
        var data = job.GetData();
        if (data == null)
        {
            return false;
        }

        return data.Value.Role == 1;
    }

    public static bool HasTankStanceOn(this Job job)
    {
        if (!job.IsTank())
        {
            return true;
        }

        var id = job.GetData()?.RowId switch {
            1 or 19 => 79,
            3 or 21 => 91,
            32 => 743,
            37 => 1833,
            _ => 0,
        };

        return Player.Status.Any(s => s.StatusId == id);
    }
}

public static class NodeEx
{
    public static Vector3 GetPointFromPlayer(this Vector3 origin, float max, float min = 0f)
    {
        return origin.GetPointFrom(Player.Position, max, min);
    }

    public static Vector3 GetPointFrom(this Vector3 origin, Vector3 from, float max, float min = 0f)
    {
        if (min < 0f || min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "min must be between 0 and max");
        }

        var direction = Vector3.Normalize(origin - from);

        var angle = (float)(Random.Shared.NextDouble() * MathF.PI / 3 - MathF.PI / 6);
        var sin = MathF.Sin(angle);
        var cos = MathF.Cos(angle);

        var rotatedDirection = new Vector3(direction.X * cos - direction.Z * sin, 0, direction.X * sin + direction.Z * cos);
        var distance = (float)(min + Random.Shared.NextDouble() * (max - min));

        return origin - rotatedDirection * distance;
    }
}

public static class ListEx
{
    public static void Toggle<T>(this List<T> list, T item)
    {
        if (list.Remove(item))
        {
            return;
        }

        list.Add(item);
    }
}
