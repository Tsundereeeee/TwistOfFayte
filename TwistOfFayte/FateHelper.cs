using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Action = System.Action;

namespace TwistOfFayte;

public static class FateHelper
{
    public static event Action? OnLeaveFate;

    public static Fate? SelectedFate { get; private set; } = null;

    public static Fate? CurrentFate { get; private set; } = null;

    public static unsafe bool IsInFate()
    {
        return FateManager.Instance()->CurrentFate != null;
    }

    public static void SetCurrentFate(Fate? fate)
    {
        CurrentFate = fate;
    }

    public static void SetSelectedFate(Fate? fate)
    {
        SelectedFate = fate;
    }

    public static bool IsInSelectedFate()
    {
        return IsInFate() && SelectedFate == CurrentFate;
    }

    public static void LeaveFate()
    {
        OnLeaveFate?.Invoke();
    }
}
