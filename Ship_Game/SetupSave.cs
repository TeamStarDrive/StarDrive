using Ship_Game.Universe;
using System;
using System.Globalization;
using System.IO;
using SDUtils;
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
    // the chosen opponents, kept only in the last setup, each with the color and flag picked for it
    [StarData] public Array<OpponentPick> Opponents;

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

[StarDataType]
public sealed class OpponentPick
{
    [StarData] public string Race; // the race's ArchetypeName
    [StarData] public bool LookPicked;
    [StarData] public int Color; // packed
    [StarData] public int Flag;
}

// the setup of the last game started, one per mod, which the Game Setup screen's Use Last Setup box loads
public static class LastSetup
{
    static string FilePath => $"{Dir.StarDriveAppData}/Last Setups/{(GlobalStats.ModName.NotEmpty() ? GlobalStats.ModName : "Vanilla")}.yaml";

    public static SetupSave Create(UniverseParams settings, string setupName)
    {
        var save = new SetupSave(settings) { Name = setupName ?? "", Opponents = new() };
        foreach (IEmpireData race in settings.SelectedOpponents)
        {
            var pick = new OpponentPick { Race = race.ArchetypeName };
            if (settings.OpponentLooks.TryGetValue(race, out (Microsoft.Xna.Framework.Color Color, int Flag) look))
            {
                pick.LookPicked = true;
                pick.Color = unchecked((int)look.Color.PackedValue);
                pick.Flag = look.Flag;
            }
            save.Opponents.Add(pick);
        }
        return save;
    }

    // the chosen opponents and their looks, skipping races the mod no longer has, the player's own, repeats, and a look whose flag is gone
    public static void RestoreOpponents(UniverseParams p, SetupSave last, IEmpireData player, int numFlags)
    {
        if (last.Opponents == null)
            return;
        foreach (OpponentPick pick in last.Opponents)
        {
            IEmpireData race = null;
            foreach (IEmpireData major in ResourceManager.MajorRaces)
                if (major.ArchetypeName == pick.Race)
                    race = major;
            if (race == null || race.ArchetypeName == player.ArchetypeName || p.SelectedOpponents.Contains(race))
                continue;
            p.SelectedOpponents.Add(race);
            if (pick.LookPicked && pick.Flag >= 0 && pick.Flag < numFlags)
                p.PickOpponentLook(race, new Microsoft.Xna.Framework.Color(unchecked((uint)pick.Color)), pick.Flag);
        }
    }

    public static void Save(UniverseParams settings, string setupName)
    {
        SetupSave save = Create(settings, setupName);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            SaveNewGameSetupScreen.Save(FilePath, save);
        }
        catch (Exception e)
        {
            Log.Warning($"The last setup could not be saved: {e.Message}");
        }
    }

    // null when there is none for the current mod
    public static SetupSave Load()
    {
        var file = new FileInfo(FilePath);
        if (!file.Exists)
            return null;
        try
        {
            SetupSave save = SaveNewGameSetupScreen.Load(file);
            return save?.Settings != null && GlobalStats.IsValidForCurrentMod(save.ModName) ? save : null;
        }
        catch (Exception e)
        {
            Log.Warning($"The last setup could not be loaded: {e.Message}");
            return null;
        }
    }
}
