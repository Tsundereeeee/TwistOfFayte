using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.Idle)]
public class Idle : StateHandler<State, StateModule>
{
    public override void Enter(StateModule module) { }

    public override State? Handle(StateModule module)
    {
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            return State.InCombat;
        }

        if (FateHelper.SelectedFate != null)
        {
            return State.PathfindingToFate;
        }

        return null;
    }
}
