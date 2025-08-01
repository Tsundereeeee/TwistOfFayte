using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.Entrance)]
public class Entrance(StateModule module) : ScoreEntranceState<FateAiState, StateModule>(module);
