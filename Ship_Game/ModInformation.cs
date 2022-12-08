using Ship_Game.Data.Serialization;

namespace Ship_Game;

[StarDataType]
public sealed class ModInformation
{
    [StarData] public string Name = "";
    [StarData] public string Description = "";
    [StarData] public string IconPath = "";
    [StarData] public string Author = "";

    [StarData] public string Version;
    [StarData] public string SupportedBlackBoxVersions;

    // TRUE by default, but if set false, no vanilla designs will be loaded
    // from StarDrive/Content/ShipDesigns and the mod is responsible to provide all required designs
    [StarData] public bool UseVanillaShips = true;
    [StarData] public bool ClearVanillaTechs;
    [StarData] public bool ClearVanillaWeapons;
    [StarData] public bool DisableDefaultRaces;
}
