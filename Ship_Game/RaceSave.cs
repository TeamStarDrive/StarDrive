using Ship_Game.Data.Serialization;
using System;
using System.Configuration;

namespace Ship_Game;

[StarDataType]
public sealed class RaceSave
{
    [StarData] public string Name;
    [StarData] public string ModName = "";
    [StarData] public string ModPath = "";
    [StarData] public int Version;
    [StarData] public RacialTrait Traits;

    [StarDataConstructor] public RaceSave() { }

    public RaceSave(RacialTrait traits)
    {
        Name = traits.Name;
        if (GlobalStats.HasMod)
        {
            ModName = GlobalStats.ModName;
            ModPath = GlobalStats.ModPath;
        }
        Version = SavedGame.SaveGameVersion;
        Traits = traits;
    }
}
