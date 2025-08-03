using ECommons.Throttlers;
using Ocelot.States;
using TwistOfFayte.Modules.General;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.ExtractMateria)]
public class ExtractMateria(StateModule module) : StateHandler<State, StateModule>(module)
{
    public override State? Handle()
    {
        if (Plugin.Chain.IsRunning)
        {
            return null;
        }

        if (!EzThrottler.Throttle("ExtractMateriaState.Check", 100))
        {
            return null;
        }

        if (MateriaHelper.CanExtract() && !Plugin.Chain.IsRunning)
        {
            MateriaHelper.ExtractEquipped();
            return null;
        }

        return State.Idle;
    }
}
