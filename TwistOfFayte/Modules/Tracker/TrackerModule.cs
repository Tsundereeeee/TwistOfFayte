using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;
using Ocelot.Modules;
using TwistOfFayte.Data;

namespace TwistOfFayte.Modules.Tracker;

// [OcelotModule]
public class TrackerModule(Plugin plugin, Config config) : Module(plugin, config)
{
    private readonly Dictionary<uint, Fate> _fates = [];

    public IReadOnlyDictionary<uint, Fate> Fates {
        get => _fates.OrderByDescending(f => f.Value.Score.Value).ToDictionary().AsReadOnly();
    }

    public override void Update(UpdateContext context)
    {
        var currentFates = Svc.Fates
            .Where(f => f.State is FateState.Preparation or FateState.Running)
            .Where(f => f.Position != Vector3.Zero && f.Position != Vector3.NaN)
            .Select(f => new Fate(f))
            .Where(f =>  f.Type switch {
                FateType.Mobs => !PluginConfig.SelectorConfig.IgnoreMobFates,
                FateType.Boss => !PluginConfig.SelectorConfig.IgnoreBossFates,
                FateType.Collect => !PluginConfig.SelectorConfig.IgnoreCollectFates,
                FateType.Defend => !PluginConfig.SelectorConfig.IgnoreDefendFates,
                FateType.Escort => !PluginConfig.SelectorConfig.IgnoreEscortFates,
                _ => true,
            }).ToDictionary(f => f.Id, f => f);

        foreach (var (id, fate) in currentFates)
        {
            if (_fates.TryAdd(id, fate))
            {
                // On Fate Spawned
            }
        }

        var despawned = _fates.Keys.Except(currentFates.Keys).ToList();
        foreach (var id in despawned)
        {
            if (_fates.Remove(id))
            {
                // On Fate Removed
            }
        }

        foreach (var fate in _fates.Values)
        {
            fate.Update(context.ForModule(this));
        }
    }
}
