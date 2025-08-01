using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.ChangingZone)]
public class ChangingZone(StateModule module) : StateHandler<State, StateModule>(module)
{
    public override State? Handle()
    {
        return null;
    }
}
