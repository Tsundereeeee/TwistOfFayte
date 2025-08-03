using System;
using Ocelot.ScoreBased;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

public abstract class Handler(StateModule module) : ScoreStateHandler<FateAiState, StateModule>(module)
{
    protected DateTime EnterTime = DateTime.Now;

    protected TimeSpan TimeInState {
        get => DateTime.Now - EnterTime;
    }
}
