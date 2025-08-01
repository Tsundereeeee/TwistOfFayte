using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Map = ECommons.GameHelpers.Map;

namespace TwistOfFayte.Zone;

public class AetheryteData(Aetheryte data)
{
    public readonly Aetheryte Data = data;

    public Vector3 Position {
        get => Map.AetherytePosition(Data.RowId);
    }

    public readonly uint Id = data.RowId;

    public unsafe void Teleport()
    {
        Telepo.Instance()->Teleport(Id, 0);
    }
}
