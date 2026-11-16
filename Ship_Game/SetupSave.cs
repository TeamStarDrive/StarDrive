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
    // the same for the hull plates box
    [StarData] public bool HullPlatesChosen;
    // the opponents' hull color box started off in a new game as it was saved, so its tick is the player's; otherwise a new game starts it off
    [StarData] public bool OpponentHullColorsChosen;

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
        HullPlatesChosen = GlobalStats.Defaults.EnableHullPlates;
        OpponentHullColorsChosen = true;

        DateTime now = DateTime.Now;
        string date = now.ToString("M/d/yyyy");
        string time = now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat);
        Date = date + " " + time;
    }

    // the settings a new game starts from: each Globals flag decides its feature unless the box was the player's and the flag is on, and the opponents' hull color box starts off unless it was the player's
    public UniverseParams SettingsToStart()
    {
        if (Settings != null && (!SalvageOpsChosen || !GlobalStats.Defaults.EnableSalvageOps))
            Settings.DisableSalvageOps = !GlobalStats.Defaults.EnableSalvageOps;
        if (Settings != null && (!HullPlatesChosen || !GlobalStats.Defaults.EnableHullPlates))
            Settings.DisableHullPlates = !GlobalStats.Defaults.EnableHullPlates;
        if (Settings != null && !OpponentHullColorsChosen)
            Settings.OpponentHullColors = false;
        return Settings;
    }
}
