using System;
using System.Collections.Generic;
using System.Linq;

namespace TwistOfFayte;

public class FateProgress
{
    private const int MaxSamples = 100;

    public List<ProgressSample> samples { get; } = new();

    public int Count {
        get => samples.Count;
    }

    public float Latest {
        get => samples[^1].Progress;
    }

    public void Add(float progress)
    {
        if (samples.Count >= MaxSamples)
        {
            samples.RemoveAt(0);
        }

        samples.Add(new ProgressSample(progress, DateTimeOffset.UtcNow));
    }

    public TimeSpan? EstimateTimeToCompletion()
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples.First();
        var last = samples.Last();

        var deltaProgress = last.Progress - first.Progress;
        var deltaSeconds = (last.Timestamp - first.Timestamp).TotalSeconds;

        if (deltaProgress <= 0 || deltaSeconds <= 0)
        {
            return null;
        }

        var remainingProgress = 100f - last.Progress;
        var ratePerSecond = deltaProgress / deltaSeconds;
        var estimatedSecondsRemaining = remainingProgress / ratePerSecond;

        return TimeSpan.FromSeconds(estimatedSecondsRemaining);
    }

    public float GetStride()
    {
        List<float> strides = [];

        for (var i = 1; i < samples.Count; i++)
        {
            var delta = samples[i].Progress - samples[i - 1].Progress;
            if (delta > 0)
            {
                strides.Add(delta);
            }
        }

        return strides.Count <= 0 ? 0f : strides.Average();
    }

    public int EstimateEnemiesRemaining()
    {
        var stride = GetStride();

        if (stride <= 0f)
        {
            return 0;
        }

        var remainingProgress = 100f - Latest;
        var estimatedEnemiesLeft = (int)Math.Ceiling(remainingProgress / stride);

        return estimatedEnemiesLeft;
    }

    public record ProgressSample(float Progress, DateTimeOffset Timestamp);
}
