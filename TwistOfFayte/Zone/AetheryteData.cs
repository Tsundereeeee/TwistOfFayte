using System.Numerics;
using Lumina.Excel.Sheets;
using Map = ECommons.GameHelpers.Map;

namespace TwistOfFayte.Zone;

public class AetheryteData(Aetheryte data)
{
    public readonly Aetheryte Data = data;

    public Vector3 Position {
        get => Map.AetherytePosition(Data.RowId);
    }
}
