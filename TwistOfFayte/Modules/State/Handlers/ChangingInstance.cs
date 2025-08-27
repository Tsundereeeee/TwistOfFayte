using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using Ocelot.Chain;
using Ocelot.Chain.ChainEx;
using Ocelot.IPC;
using Ocelot.States;
using TwistOfFayte.Zone;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.ChangingInstance)]
public class ChangingInstance(StateModule module) : StateHandler<State, StateModule>(module)
{
    private bool isComplete;
    
    public override void Enter()
    {
        isComplete = false;
        var instances = Module.Lifestream.GetNumberOfInstances();
        if (instances < 2)
        {
            isComplete = true;
            return;
        }
        Svc.Log.Info($"SwitchingInstance - Instances count: {instances}");
        var nextInstance = Module.Lifestream.GetCurrentInstance() + 1;
        if (nextInstance > instances)
        {
            nextInstance = 1;
        }
        Svc.Log.Info($"SwitchingInstance - Switching {Module.Lifestream.GetCurrentInstance()}->{nextInstance}");
        Plugin.Chain.Submit(() => {
            return Chain.Create($"ChangingInstance")
                .WaitUntilNotCondition(ConditionFlag.InCombat, 5000)
                .WaitGcd()
                .Then(_ => ZoneHelper.GetAetherytes().First().Teleport())
                .WaitToCycleCondition(ConditionFlag.BetweenAreas, 7500)
                .BreakIf(() => !Module.Lifestream.CanChangeInstance())
                .Then(_ => Module.Lifestream.ChangeInstance(nextInstance))
                .WaitToCycleCondition(ConditionFlag.BetweenAreas, 7500)
                .Then(_ => isComplete = true);
        });
    }

    public override State? Handle()
    {
        if (isComplete && !Plugin.Chain.IsRunning)
        {
            return State.Idle;
        }
        return null;
    }
}
