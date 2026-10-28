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
    [StarData] public string ModPath;
    [StarData] public int Version;
    [StarData] public UniverseParams Settings;
    // the salvage ops box was unlocked as it was saved, so its tick is the player's; otherwise a new game takes salvage ops from the Globals flag
    [StarData] public bool SalvageOpsChosen;

    public SetupSave()
    {
    }

    public SetupSave(UniverseParams settings)
    {
        ModName = GlobalStats.ModName; // default ""
        ModPath = GlobalStats.ModPath; // default ""
        Version = SavedGame.SaveGameVersion;
        Settings = settings;
        SalvageOpsChosen = GlobalStats.Defaults.EnableSalvageOps;

        DateTime now = DateTime.Now;
        string date = now.ToString("M/d/yyyy");
        string time = now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat);
        Date = date + " " + time;
    }

    // the settings a new game starts from: the Globals flag decides salvage ops unless the box was the player's and the flag is on
    public UniverseParams SettingsToStart()
    {
        if (Settings != null && (!SalvageOpsChosen || !GlobalStats.Defaults.EnableSalvageOps))
            Settings.DisableSalvageOps = !GlobalStats.Defaults.EnableSalvageOps;
        return Settings;
    }
}
