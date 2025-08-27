using System;
using System.Linq;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Ocelot.Chain;
using Ocelot.Chain.ChainEx;
using Ocelot.Extensions;
using Ocelot.Gameplay;
using Ocelot.Prowler;
using Ocelot.States;
using TwistOfFayte.Data;

namespace TwistOfFayte.Modules.State.Handlers.FateAi;

[State<FateAiState>(FateAiState.HandIn)]
public class HandIn(StateModule module) : Handler(module)
{
    private readonly StateModule module = module;

    private bool isComplete = false;

    private static ChainQueue ChainQueue {
        get => ChainManager.Get("FateAi.HandIn.ChainQueue");
    }

    public override float GetScore()
    {
        var fate = FateHelper.SelectedFate;
        if (fate == null || fate.Type != FateType.Collect || fate.State == FateState.WaitingForEnd)
        {
            return 0f;
        }

        var count = fate.GetCurrentHandInInInventory();
        var target = 5f;
        var estimated = fate.ProgressTracker.EstimateEnemiesRemaining();
        var remaining = estimated + 2;
        if (estimated > 0 && remaining < target)
        {
            target = remaining;
        }
        return count >= target ? 100f + count : 0f;
    }

    public override unsafe void Enter()
    {
        isComplete = false;
        Prowler.Abort();

        if (!TargetHelper.HandIn.Any() || FateHelper.SelectedFate == null || FateHelper.SelectedFate.State == FateState.WaitingForEnd)
        {
            isComplete = true;
            return;
        }

        IGameObject? handIn = TargetHelper.HandIn.First();
        if (TargetHelper.InCombat.Any() && Player.Position.DistanceTo2D(handIn.Position) - handIn.HitboxRadius <= 5f)
        {
            isComplete = true;
            return;
        }

        Prowler.Prowl(new Prowl(handIn.GetPointOnHitboxFromPlayer(2f)) {
            ShouldFly = _ => false,
            ShouldMount = _ => false,
            ShouldSprint = _ => true,
            PostProcessor = prowl => prowl.Nodes = prowl.Nodes.Smooth(),
            OnComplete = (_, _) => ChainQueue.Submit(chain => chain
                .Then(_ => Svc.Commands.ProcessCommand("/bmr ar set Full Auto"))
                .Then(_ => !TargetHelper.InCombat.Any())
                .Then(_ => Svc.Commands.ProcessCommand("/bmr ar disable"))
                .Then(_ => Actions.TryUnmount())
                .Then(_ => !Player.Mounted)
                .Wait(1000)
                .Then(_ => Svc.Targets.Target = handIn)
                .BreakIf(() => Svc.Targets.Target == null || Player.DistanceTo(Svc.Targets.Target) > 5f)
                .Then(_ => !Player.IsBusy)
                .Then(_ => TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.Struct(), false) != 0)
                .Then(_ => TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.Struct(), false) != 0)
                .Then(_ => TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.Struct(), false) != 0)
                .WaitForAddonReady("Talk")
                .Then(_ => {
                    if (!EzThrottler.Throttle("HandIn.InteractWithNpc.Talk", 100))
                    {
                        return false;
                    }

                    var addonPtr = Svc.GameGui.GetAddonByName("Talk");
                    if (addonPtr == IntPtr.Zero)
                    {
                        return true;
                    }

                    var addon = (AtkUnitBase*)addonPtr.Address;
                    if (!GenericHelpers.IsAddonReady(addon))
                    {
                        return true;
                    }

                    Svc.Log.Info("Clicking through talk");
                    new AddonMaster.Talk(addonPtr).Click();
                    return false;
                })
                .Then(_ => {
                    // wait for request to be ready
                    var addonPtr = Svc.GameGui.GetAddonByName("Request");
                    Svc.Log.Info("Getting request addon...");
                    if (addonPtr != IntPtr.Zero)
                    {
                        Svc.Log.Info("Pressing Hand over");
                        var addon = (AtkUnitBase*)addonPtr.Address;
                        new AddonMaster.Request(addon).HandOver();
                        Svc.Log.Info("Done");
                        return true;
                    }

                    return FateHelper.CurrentFate?.GetCurrentHandInInInventory() <= 0;
                })
                .WaitForAddonReady("Talk")
                .Then(_ => {
                    if (!EzThrottler.Throttle("HandIn.InteractWithNpc.Talk", 100))
                    {
                        return false;
                    }

                    var addonPtr = Svc.GameGui.GetAddonByName("Talk");
                    if (addonPtr == IntPtr.Zero)
                    {
                        return true;
                    }

                    var addon = (AtkUnitBase*)addonPtr.Address;
                    if (!GenericHelpers.IsAddonReady(addon))
                    {
                        return true;
                    }

                    Svc.Log.Info("Clicking through talk");
                    new AddonMaster.Talk(addonPtr).Click();
                    return false;
                })
                .Then(_ => isComplete = true)
                .OnCancel(() => isComplete = true)
            ),
            OnCancel = (_, _) => isComplete = true,
        });
    }
    
    public override bool Handle()
    {
        return isComplete;
    }
}
