using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Ocelot;
using Ocelot.Chain;
using Ocelot.Prowler;
using Ocelot.Windows;
using TwistOfFayte.Data;
using TwistOfFayte.Modules.Currency;
using TwistOfFayte.Modules.State;
using TwistOfFayte.Modules.State.Handlers;
using TwistOfFayte.Modules.State.Handlers.FateAi;
using TwistOfFayte.Modules.Tracker;

namespace TwistOfFayte.Windows;

[OcelotMainWindow]
public class MainWindow(Plugin _plugin, Config pluginConfig) : OcelotMainWindow(_plugin, pluginConfig)
{
    public override void PostInitialize()
    {
        base.PostInitialize();

        TitleBarButtons.Add(new DynamicTitleBarButton((button, m) => {
            if (m != ImGuiMouseButton.Left)
            {
                return;
            }

            var module = Plugin.Modules.GetModule<StateModule>();
            module.IsRunning ^= true;
            module.VNavmesh.Stop();
            module.StateMachine.Reset();
            Prowler.Abort();
            ChainManager.AbortAll();

            button.Icon = module.IsRunning ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
        }) {
            Icon = FontAwesomeIcon.Play,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip(Plugin.Modules.GetModule<StateModule>().IsRunning ? I18N.T("generic.stop") : I18N.T("generic.start")),
        });
    }

    protected override unsafe void Render(RenderContext context)
    {
        var module = Plugin.Modules.GetModule<StateModule>();
        if (!module.HasRequiredIPCs)
        {
            foreach (var name in module.MissingIPCs)
            {
                OcelotUI.Error(I18N.T("windows.main.missing_ipc.label", new Dictionary<string, string> {
                    ["plugin"] = name,
                }));
            }

            return;
        }

        RenderState(context);
        OcelotUI.VSpace();
        ImGui.Separator();
        OcelotUI.VSpace();

        RenderActiveFates(context);
        OcelotUI.VSpace();
        ImGui.Separator();
        OcelotUI.VSpace();

        RenderGemstones(context);
        OcelotUI.VSpace();
        ImGui.Separator();
        OcelotUI.VSpace();

        foreach (var mob in TargetHelper.NotInCombat)
        {
            OcelotUI.LabelledValue("Name", mob.Name);
            OcelotUI.Indent(() => {
                OcelotUI.LabelledValue("Position", mob.Position.ToString("f2"));
                OcelotUI.LabelledValue("Target", mob.TargetObject != null);
                if (mob.TargetObject == null)
                {
                    return;
                }

                OcelotUI.Indent(() => {
                    var target = (BattleChara*)mob.TargetObject.Address;

                    OcelotUI.LabelledValue("Is Character", target->IsCharacter());

                    var job = (Job)target->ClassJob;
                    OcelotUI.LabelledValue("Is Tank", job.IsTank());
                });
            });
        }
    }

    private void RenderState(RenderContext _)
    {
        if (OcelotUI.LabelledValue("Fate", FateHelper.SelectedFate == null ? "No Fate" : FateHelper.SelectedFate.Name) == UIState.Hovered)
        {
            if (FateHelper.SelectedFate != null)
            {
                ImGui.SetTooltip(FateHelper.SelectedFate.State.ToString());
            }
        }

        var state = Plugin.Modules.GetModule<StateModule>();
        var key = state.StateMachine.State.GetKey();
        OcelotUI.LabelledValue("State", state.T($"state.{key}.label"));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(state.T($"state.{key}.tooltip"));
        }

        if (state.StateMachine.TryGetCurrentHandler<ParticipatingInFate>(out var handler))
        {
            OcelotUI.LabelledValue("Sub-state", handler.StateMachine.State.ToString());

            if (handler.StateMachine.TryGetCurrentHandler<GatherMobs>(out var subhandler)) { }
        }

        var isPathfinding = state.VNavmesh.IsPathfinding();
        var isRunning = state.VNavmesh.IsRunning();

        OcelotUI.LabelledValue("Vnavmesh state", isPathfinding ? "Pathfinding" : isRunning ? "Running" : "Idle");
    }

    private void RenderActiveFates(RenderContext _)
    {
        if (Plugin.Modules.GetModule<TrackerModule>().Fates.Count <= 0)
        {
            ImGui.Text("No Fates");
        }

        foreach (var fate in Plugin.Modules.GetModule<TrackerModule>().Fates.Values)
        {
            var progress = fate.Progress / 100f;
            var progressText = $"{progress * 100f:0}%%";
            var estimate = fate.ProgressTracker.EstimateTimeToCompletion();
            var fateProgressText = progressText;
            if (estimate != null && pluginConfig.UiConfig.ShowTimeEstimate)
            {
                fateProgressText = $"{fateProgressText} | {estimate.Value:mm\\:ss}";
            }

            var estimatedEnemies = fate.ProgressTracker.EstimateEnemiesRemaining();
            if (estimatedEnemies > 0 && pluginConfig.UiConfig.ShowObjectiveEstimate && fate.Type != FateType.Boss)
            {
                fateProgressText = $"{fateProgressText} | {estimatedEnemies}";
            }

            var left = new UIString();
            if (pluginConfig.UiConfig.ShowFateTypeIcon)
            {
                left.AddIcon(fate.IconId);
            }


            if (fate.IsBonus && pluginConfig.UiConfig.ShowBonusFateIcon)
            {
                left.AddIcon(60934);
            }

            var color = OcelotColor.Text;
            if (fate.IsSelected() && pluginConfig.UiConfig.HighlightSelectedFate)
            {
                color = OcelotColor.Blue;
            }

            if (fate.IsBlacklisted(_plugin))
            {
                color = OcelotColor.Disabled;
            }

            left.Add(fate.Name, color);
            if (fate.State == FateState.Preparation && pluginConfig.UiConfig.ShowPreparingFateIcon)
            {
                left.AddIcon(61397);
            }

            var right = new UIString().Add(fateProgressText);
            if (OcelotUI.LeftRightText(left, right) == UIState.LeftHovered)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Type: {fate.Type}");
                sb.AppendLine($"State: {fate.State}");
                sb.AppendLine($"Score: {fate.Score:f2}");
                foreach (var source in fate.Score.Sources)
                {
                    sb.AppendLine($" - {source.Key}: {source.Value:f2}");
                }


                ImGui.SetTooltip(sb.ToString());
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                {
                    _plugin.Config.FateBlacklist.Toggle(fate.Id);
                    _plugin.Config.Save();
                }
            }


            if (OcelotUI.ProgressBar(progress) == UIState.Hovered)
            {
                ImGui.SetTooltip(progressText);
            }

            OcelotUI.VSpace();
        }
    }

    private void RenderGemstones(RenderContext context)
    {
        Plugin.Modules.GetModule<CurrencyModule>().BicolorGemstones.Item.Render(context);
        // OcelotUI.LabelledValue("Bicolor Gemstones", $"{Items.BicolorGemstones.Count()}/1500");

        var delta = Plugin.Modules.GetModule<CurrencyModule>().BicolorGemstones.GetGainPerHour();
        OcelotUI.LabelledValue("Bicolor Gemstones per hour", delta);
    }
}
