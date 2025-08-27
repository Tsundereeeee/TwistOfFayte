using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Ocelot;
using Ocelot.Chain;
using Ocelot.Prowler;
using Ocelot.Ui;
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
                OcelotUi.Error(I18N.T("windows.main.missing_ipc.label", new Dictionary<string, string> {
                    ["plugin"] = name,
                }));
            }

            return;
        }

        RenderState(context);
        OcelotUi.VSpace();
        ImGui.Separator();
        OcelotUi.VSpace();

        RenderActiveFates(context);
        OcelotUi.VSpace();
        ImGui.Separator();
        OcelotUi.VSpace();

        Plugin.Modules.GetModule<CurrencyModule>().BicolorGemstones.Render(context);
        OcelotUi.VSpace();
        ImGui.Separator();
        OcelotUi.VSpace();
    }

    private void RenderState(RenderContext _)
    {
        if (OcelotUi.LabelledValue("Fate", FateHelper.SelectedFate == null ? "No Fate" : FateHelper.SelectedFate.Name) == UiState.Hovered)
        {
            if (FateHelper.SelectedFate != null)
            {
                ImGui.SetTooltip(FateHelper.SelectedFate.State.ToString());
            }
        }

        var state = Plugin.Modules.GetModule<StateModule>();
        var key = state.StateMachine.State.GetKey();
        OcelotUi.LabelledValue("State", state.T($"state.{key}.label"));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(state.T($"state.{key}.tooltip"));
        }

        if (state.StateMachine.TryGetCurrentHandler<ParticipatingInFate>(out var handler))
        {
            OcelotUi.LabelledValue("Sub-state", handler.StateMachine.State.ToString());
        }

        var isPathfinding = state.VNavmesh.IsPathfinding();
        var isRunning = state.VNavmesh.IsRunning();

        if (state.PluginConfig.DebugConfig.ShowPathfindingState)
        {
            OcelotUi.LabelledValue("Vnavmesh state", isPathfinding ? "Pathfinding" : isRunning ? "Running" : "Idle");
            OcelotUi.LabelledValue("Prowler state", Prowler.Current == null ? "Idle" : Prowler.Current.State.ToString());
        }
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
            var progressText = $"{progress * 100f:0}%";
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

            var left = new UiString();
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

            if (fate.Type == FateType.Collect)
            {
                left.Add($"({fate.GetCurrentHandInInInventory()})");
            }

            var right = new UiString().Add(fateProgressText);
            if (OcelotUi.LeftRightText(left, right) == UiState.LeftHovered)
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


            if (OcelotUi.ProgressBar(progress) == UiState.Hovered)
            {
                ImGui.SetTooltip(progressText);
            }

            OcelotUi.VSpace();
        }
    }
}
