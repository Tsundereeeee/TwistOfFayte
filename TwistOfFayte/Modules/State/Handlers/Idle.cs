using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using Ocelot.States;
using TwistOfFayte.Modules.General;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.Idle)]
public class Idle : StateHandler<State, StateModule>
{
    public override State? Handle(StateModule module)
    {
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            return State.InCombat;
        }

        if (MateriaHelper.CanExtract())
        {
            return State.ExtractMateria;
        }

        if (FateHelper.SelectedFate != null)
        {
            return State.PathfindingToFate;
        }

        return null;
    }
}
