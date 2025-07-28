using ECommons.DalamudServices;
using ECommons.Throttlers;
using Ocelot.States;
using TwistOfFayte.Modules.General;

namespace TwistOfFayte.Modules.State.Handlers;

[StateAttribute<State>(State.ExtractMateria)]
public class ExtractMateria : StateHandler<State, StateModule>
{
    public override State? Handle(StateModule module)
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
            // Svc.Log.Info("Firing off Extractor!");
            MateriaHelper.ExtractEquipped();
            return null;
        }

        Svc.Log.Info("Chain is running: " + (Plugin.Chain.IsRunning ? "Yes" : "No"));

        return State.Idle;
    }
}
