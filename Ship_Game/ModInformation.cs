using Ship_Game.Data.Serialization;

namespace Ship_Game;

[StarDataType]
public sealed class ModInformation
{
    // ex: "Mods/ExampleMod/" sanitized path
    public string Path = "";

    // the unique name id of the mod
    [StarData] public string Name = "";

    [StarData] public string Description = "";
    [StarData] public string IconPath = "";
    [StarData] public string Author = "";

    [StarData] public string Version;
    [StarData] public string SupportedBlackBoxVersions;
    [StarData] public int FormatVersion; // make sure this mode can be loaded regarding data format

    // TRUE by default, but if set false, no vanilla designs will be loaded
    // from StarDrive/Content/ShipDesigns and the mod is responsible to provide all required designs
    [StarData] public bool UseVanillaShips = true;
    [StarData] public bool UseVanillaTechs = true;
    [StarData] public bool UseVanillaWeapons = true;
    [StarData] public bool UseVanillaModules = true;
    [StarData] public bool UseVanillaBuildings = true; // This also includes random stuff - as it might contain vanilla buildings
    [StarData] public bool UseVanillaRaces = true;

    // TODO: DEPRECATED FLAGS, KEEPING THEM FOR A FEW VERSIONS FOR COMPATIBILITY WITH MODS
    [StarData] public bool ClearVanillaTechs;
    [StarData] public bool ClearVanillaWeapons;
    [StarData] public bool DisableDefaultRaces;

    public bool ModFormatSupported => FormatVersion == ModEntry.FormatVersion;
}

