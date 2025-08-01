using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Ocelot.Chain.ChainEx;
using Ocelot.Gameplay;
using Ocelot.Prowler;
using Ocelot.ScoreBased;
using Ocelot.States;
using TwistOfFayte.Modules.State.Handlers.FateAi;

namespace TwistOfFayte.Modules.State.Handlers;

[State<State>(State.ParticipatingInFate)]
public class ParticipatingInFate(StateModule module) : StateHandler<State, StateModule>(module)
{
    public readonly ScoreStateMachine<FateAiState, StateModule> StateMachine = new(FateAiState.Entrance, module);

    public override void Enter()
    {
        Svc.Targets.Target = null;
        StateMachine.Reset();
    }

    public override void Exit()
    {
        Prowler.Abort();
    }

    public override unsafe State? Handle()
    {
        if (Player.Mounted)
        {
            Actions.TryUnmount();
        }

        if (!FateHelper.IsInFate())
        {
            return State.Idle;
        }

        if (Module.PluginConfig.GeneralConfig.SyncLevel && EzThrottler.Throttle("Fate Sync") && FateHelper.CurrentFate?.NeedSync() == true)
        {
            FateManager.Instance()->LevelSync();
        }

        if (Module.PluginConfig.GeneralConfig.MaintainStance && EzThrottler.Throttle("Fate Stance", 2500) && TankHelper.Current is { } tank)
        {
            Plugin.Chain.Submit(chain => chain
                .BreakIf(() => tank.HasStanceOn())
                .Then(tank.TurnStanceOn())
            );
        }

        StateMachine.Update();

        return null;
    }
}
