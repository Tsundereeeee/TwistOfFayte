using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Ocelot;
using Ocelot.Prowler;
using Ocelot.Windows;
using TwistOfFayte.Gameplay;
using TwistOfFayte.Modules.Currency;
using TwistOfFayte.Modules.State;
using TwistOfFayte.Modules.Tracker;

namespace TwistOfFayte.Windows;

[OcelotMainWindow]
public class MainWindow(Plugin _plugin, Config _config) : OcelotMainWindow(_plugin, _config)
{
    public override void PostInitialize()
    {
        base.PostInitialize();

        TitleBarButtons.Add(new DynamicTitleBarButton((button, m) => {
            if (m != ImGuiMouseButton.Left)
            {
                return;
            }

            var module = plugin.Modules.GetModule<StateModule>();
            module.IsRunning ^= true;
            module.VNavmesh.Stop();
            module.StateMachine.Reset(module);

            button.Icon = module.IsRunning ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
        }) {
            Icon = FontAwesomeIcon.Play,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip(I18N.T("generic.start.label")),
        });
    }

    protected override unsafe void Render(RenderContext context)
    {
        var module = plugin.Modules.GetModule<StateModule>();
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

        foreach (var npc in TargetHelper.Friendlies)
        {
            if (Svc.Targets.Target?.EntityId == npc.EntityId)
            {
                ImGui.TextUnformatted("*");
                ImGui.SameLine();
            }
            
            OcelotUI.LabelledValue("Name", npc.Name.ToString());
            OcelotUI.Indent(() => { 
                OcelotUI.LabelledValue("Name Id", npc.NameId);
                OcelotUI.LabelledValue("Position", npc.Position.ToString("f2"));
                OcelotUI.LabelledValue("Has Behaviour", (Svc.Data.GetExcelSheet<BNpcBase>().GetRow(npc.NameId).Behavior.RowId != 0));

                var obj = npc.GameObject();

                if (obj->IsCharacter())
                {
                    var character = (Character*)obj;
                    // OcelotUI.LabelledValue("Icon", character->Level);
                    
                    OcelotUI.LabelledValue("ModelScale", character->ModelScale);
                    OcelotUI.LabelledValue("Health", character->Health);
                    OcelotUI.LabelledValue("MaxHealth", character->MaxHealth);
                    OcelotUI.LabelledValue("Mana", character->Mana);
                    OcelotUI.LabelledValue("MaxMana", character->MaxMana);
                    OcelotUI.LabelledValue("GatheringPoints", character->GatheringPoints);
                    OcelotUI.LabelledValue("MaxGatheringPoints", character->MaxGatheringPoints);
                    OcelotUI.LabelledValue("CraftingPoints", character->CraftingPoints);
                    OcelotUI.LabelledValue("MaxCraftingPoints", character->MaxCraftingPoints);
                    OcelotUI.LabelledValue("TransformationId", character->TransformationId);
                    OcelotUI.LabelledValue("TitleId", character->TitleId);
                    OcelotUI.LabelledValue("StatusLoopVfxId", character->StatusLoopVfxId);
                    OcelotUI.LabelledValue("ClassJob", character->ClassJob);
                    OcelotUI.LabelledValue("Level", character->Level);
                    OcelotUI.LabelledValue("Icon", character->Icon);
                    OcelotUI.LabelledValue("SEPack", character->SEPack);
                    OcelotUI.LabelledValue("ShieldValue", character->ShieldValue);
                    OcelotUI.LabelledValue("Map", character->Map);
                    OcelotUI.LabelledValue("OnlineStatus", character->OnlineStatus);
                    OcelotUI.LabelledValue("Battalion", character->Battalion);
                    
                    OcelotUI.LabelledValue("Flags", character->Flags);
                    OcelotUI.LabelledValue("CombatTagType", character->CombatTagType);
                    OcelotUI.LabelledValue("CombatTaggerId", character->CombatTaggerId);
    
                }
                else
                {
                    OcelotUI.Title("Not a cahracter...");
                }

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

        var state = plugin.Modules.GetModule<StateModule>();
        var key = state.StateMachine.State.GetKey();
        OcelotUI.LabelledValue("State", state.T($"state.{key}.label"));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(state.T($"state.{key}.tooltip"));
        }
    }

    private void RenderActiveFates(RenderContext _)
    {
        if (plugin.Modules.GetModule<TrackerModule>().Fates.Count <= 0)
        {
            ImGui.Text("No Fates");
        }

        foreach (var fate in plugin.Modules.GetModule<TrackerModule>().Fates.Values)
        {
            var progress = fate.Progress / 100f;
            var progressText = $"{progress * 100f:0}%%";
            var estimate = fate.ProgressTracker.EstimateTimeToCompletion();
            var fateProgressText = progressText;
            if (estimate != null && _config.UiConfig.ShowTimeEstimate)
            {
                fateProgressText = $"{fateProgressText} | {estimate.Value:mm\\:ss}";
            }

            var estimatedEnemies = fate.ProgressTracker.EstimateEnemiesRemaining();
            if (estimatedEnemies > 0 && _config.UiConfig.ShowObjectiveEstimate)
            {
                fateProgressText = $"{fateProgressText} | {estimatedEnemies}";
            }

            var left = new UIString();
            if (fate.IsBonus && _config.UiConfig.ShowBonusFateIcon)
            {
                left.AddIcon(60934);
            }

            var color = OcelotColor.Text;
            if (fate == FateHelper.SelectedFate && _config.UiConfig.HighlightSelectedFate)
            {
                color = OcelotColor.Blue;
            }

            left.Add(fate.Name, color);
            if (fate.State == FateState.Preparation && _config.UiConfig.ShowPreparingFateIcon)
            {
                left.AddIcon(61397);
            }

            var right = new UIString().Add(fateProgressText);
            if (OcelotUI.LeftRightText(left, right) == UIState.LeftHovered)
            {
                ImGui.SetTooltip($"{fate.State} (Score: {fate.Score:f2})");
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
        OcelotUI.LabelledValue("Bicolor Gemstones", $"{Items.BicolorGemstones.Count()}/1500");

        var delta = plugin.Modules.GetModule<CurrencyModule>().BicolorGemstones.GetGainPerHour();
        OcelotUI.LabelledValue("Bicolor Gemstones per hour", delta);
    }
}
