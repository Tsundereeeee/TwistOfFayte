using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.ChangingZone)]
public class ChangingZone : StateHandler<State, StateModule>
{
    public override State? Handle(StateModule module)
    {
        return null;
    }
}
