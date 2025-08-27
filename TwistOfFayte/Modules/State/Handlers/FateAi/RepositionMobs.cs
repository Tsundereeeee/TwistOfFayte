using System.Linq;
using System.Numerics;
using ECommons.GameHelpers;
using Ocelot.Extensions;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.RepositionMobs)]
public class RepositionMobs(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;
    
    public override float GetScore()
    {
        return float.MinValue;
    }

    public override bool Handle()
    {
        return true;
    }
}
