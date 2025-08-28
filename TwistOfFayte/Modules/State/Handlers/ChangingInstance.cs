using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation.NeoTaskManager;
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
    private int targetInstanceId;
    
    public override void Enter()
    {
        isComplete = false;
        targetInstanceId = 0;
        var instances = Module.Lifestream.GetNumberOfInstances();
        if (instances < 2)
        {
            return;
        }
        Svc.Log.Info($"SwitchingInstance - Instances count: {instances}");
        targetInstanceId = Module.Lifestream.GetCurrentInstance() + 1;
        if (targetInstanceId > instances)
        {
            targetInstanceId = 1;
        }
    }

    public override State? Handle()
    {
        if (Plugin.Chain.IsRunning)
        {
            return null;
        }
        if (isComplete || targetInstanceId < 1)
        {
            return State.Idle;
        }
        Svc.Log.Info($"SwitchingInstance - Switching {Module.Lifestream.GetCurrentInstance()}->{targetInstanceId}");
        Plugin.Chain.Submit(() => {
            return Chain.Create($"ChangingInstance")
                .WaitUntilNotCondition(ConditionFlag.InCombat, 15000)
                .WaitGcd()
                .Then(_ => ZoneHelper.GetAetherytes().First().Teleport())
                .Wait(1000)
                .WaitUntilNotCondition(ConditionFlag.Casting)
                .Wait(1000)
                .WaitUntilNotCondition(ConditionFlag.BetweenAreas, 10000)
                .BreakIf(() => !Module.Lifestream.CanChangeInstance())
                .Then(_ => Module.Lifestream.ChangeInstance(targetInstanceId))
                .Wait(1000)
                .WaitUntilNotCondition(ConditionFlag.BetweenAreas, 10000)
                .Then(_ => isComplete = true)
                .OnCancel(() => isComplete = true);
        });
        return null;
    }
}
