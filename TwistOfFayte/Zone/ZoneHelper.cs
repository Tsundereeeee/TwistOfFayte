using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.Sheets;

namespace TwistOfFayte.Zone;

public static class ZoneHelper
{
    private static ushort ZoneId = 0;

    private static IEnumerable<AetheryteData> Aetherytes = [];

    public static IEnumerable<AetheryteData> GetAetherytes()
    {
        if (!IsCurrentZoneCached())
        {
            GenerateZoneData();
        }

        return Aetherytes;
    }

    private static bool IsCurrentZoneCached()
    {
        return ZoneId == Svc.ClientState.TerritoryType;
    }

    private static unsafe void GenerateZoneData()
    {
        ZoneId = Svc.ClientState.TerritoryType;
        Aetherytes = [];

        var layout = LayoutWorld.Instance()->ActiveLayout;
        if (layout == null)
        {
            return;
        }

        if (!layout->InstancesByType.TryGetValue(InstanceType.Aetheryte, out var map, false))
        {
            return;
        }

        foreach (ILayoutInstance* instance in map.Value->Values)
        {
            Svc.Log.Warning(instance->Id.InstanceKey.ToString());
            Svc.Log.Warning("    " + instance->Flags1.ToString());
            Svc.Log.Warning("    " + instance->Flags2.ToString());
            Svc.Log.Warning("    " + instance->Flags3.ToString());

            var transform = instance->GetTransformImpl();
            var position = transform->Translation;
        }

        Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>()
            .Where(a => a.Territory.RowId == Svc.ClientState.TerritoryType)
            .Where(a => a.IsAetheryte)
            .Select(a => new AetheryteData(a));
    }
}
