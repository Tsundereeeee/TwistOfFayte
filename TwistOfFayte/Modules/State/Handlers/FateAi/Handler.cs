using Ocelot.ScoreBased;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

public abstract class Handler(StateModule module) : ScoreStateHandler<FateAiState, StateModule>(module);
