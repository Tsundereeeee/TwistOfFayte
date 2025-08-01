using Lumina.Excel.Sheets;
using Ocelot.Config.Attributes;
using Ocelot.Modules;

namespace TwistOfFayte.Modules.General;

public class GeneralConfig : ModuleConfig
{
    [ExcelSheet(typeof(Mount), nameof(MountProvider))]
    [Searchable]
    public uint Mount { get; set; } = 1;

    [Checkbox]
    public bool MountRoulette { get; set; } = false;

    [Checkbox]
    public bool ShouldExtractMateria { get; set; } = false;

    [Checkbox]
    public bool ShouldRepairGear { get; set; } = false;

    [Checkbox]
    public bool SyncLevel { get; set; } = true;

    [Checkbox]
    public bool MaintainStance { get; set; } = true;

    [Checkbox]
    public bool ShouldTeleport { get; set; } = true;
}
