namespace TwistOfFayte.Modules.State;

public enum State
{
    Idle,

    InCombat,

    PathfindingToFate,

    StartingFate,

    ParticipatingInFate,

    // Traversal
    ChangingInstance,

    ChangingZone,

    //
    RepairGear,

    ExtractMateria,

    SpendGemstones,
}

public static class StateEx
{
    public static string GetKey(this State state)
    {
        return state.ToString().ToSnakeCase();
    }
}
