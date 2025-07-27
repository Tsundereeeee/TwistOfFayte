using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.ChangingInstance)]
public class ChangingInstance : StateHandler<State, StateModule>
{
    public override State? Handle(StateModule module)
    {
        return null;
    }
}
