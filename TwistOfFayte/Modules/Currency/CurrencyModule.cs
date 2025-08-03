using Ocelot.Gameplay;
using Ocelot.Modules;
using Items = TwistOfFayte.Gameplay.Items;

namespace TwistOfFayte.Modules.Currency;

[OcelotModule]
public class CurrencyModule(Plugin plugin, Config config) : Module(plugin, config)
{
    private ushort territoryId = 0;

    public readonly ItemTracker BicolorGemstones = new(Items.BicolorGemstones);

    public override void Update(UpdateContext context)
    {
        BicolorGemstones.Update();
    }

    public override void OnTerritoryChanged(ushort id)
    {
        if (territoryId == 0)
        {
            BicolorGemstones.Reset();
        }

        territoryId = id;
    }
}
