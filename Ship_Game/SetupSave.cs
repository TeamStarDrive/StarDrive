using Ship_Game.Universe;
using System;
using System.Globalization;
using Ship_Game.Data.Serialization;

namespace Ship_Game;

[StarDataType]
public sealed class SetupSave
{
    [StarData] public string Name = "";
    [StarData] public string Date;
    [StarData] public string ModName;
    [StarData] public int Version;
    [StarData] public UniverseParams Settings;

    public SetupSave()
    {
    }

    public SetupSave(UniverseParams settings)
    {
        ModName = GlobalStats.ModName; // default ""
        Version = SavedGame.SaveGameVersion;
        Settings = settings;

        DateTime now = DateTime.Now;
        string date = now.ToString("M/d/yyyy");
        string time = now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat);
        Date = date + " " + time;
    }
}
