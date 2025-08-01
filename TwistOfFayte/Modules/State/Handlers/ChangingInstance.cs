using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.ChangingInstance)]
public class ChangingInstance(StateModule module) : StateHandler<State, StateModule>(module)
{
    public override State? Handle()
    {
        return null;
    }
}
