using ECommons.GameHelpers;
using Ocelot.Extensions;
using Ocelot.Prowler;
using Ocelot.States;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.MaintainFateZone)]
public class MaintainFateZone(StateModule module) : Handler(module)
{
    private bool isComplete = false;

    public override float GetScore()
    {
        var fate = FateHelper.CurrentFate;
        if (fate == null)
        {
            return float.MaxValue;
        }

        var distance = Player.Position.DistanceTo2D(fate.Position);
        var radius = fate.Radius;

        if (distance > radius)
        {
            return 100f;
        }

        var normalized = distance / radius;
        return normalized >= 0.9f ? 100f : 0f;
    }

    public override void Enter()
    {
        isComplete = false;
        Prowler.Abort();

        if (FateHelper.CurrentFate == null || Player.DistanceTo(FateHelper.CurrentFate.Position) <= FateHelper.CurrentFate.Radius * 0.9f)
        {
            isComplete = true;
            return;
        }

        Prowler.Prowl(new Prowl(FateHelper.CurrentFate.Position.GetPointFromPlayer(5f, 2f)) {
            ShouldFly = _ => false,
            ShouldMount = _ => false,
            PostProcessor = prowl => prowl.Nodes = prowl.Nodes.Smooth(),
            Watcher = prowl => Player.DistanceTo(prowl.Destination) <= 1f,
            OnComplete = (_, _) => isComplete = true,
            OnCancel = (_, _) => isComplete = true,
        });
    }

    public override bool Handle()
    {
        return FateHelper.CurrentFate == null || isComplete || !Prowler.IsRunning;
    }
}
