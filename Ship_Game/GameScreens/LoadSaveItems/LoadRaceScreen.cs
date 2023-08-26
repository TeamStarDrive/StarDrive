using System.IO;
using System.Linq;
using SDUtils;
using Ship_Game.Audio;

namespace Ship_Game;

public sealed class LoadRaceScreen : GenericLoadSaveScreen
{
    readonly RaceDesignScreen Screen;

    public LoadRaceScreen(RaceDesignScreen screen) : base(screen, SLMode.Load, "", "Load Saved Race", "Saved Races")
    {
        Screen = screen;
        Path = Dir.StarDriveAppData + "/Saved Races/";
    }

    protected override void Load()
    {
        if (SelectedFile is { Enabled: true, Data: RaceSave raceSave })
        {
            Screen.SetCustomEmpireData(raceSave.Traits);
        }
        else
        {
            GameAudio.NegativeClick();
        }
        ExitScreen();
    }

    protected override void InitSaveList()
    {
        var saves = new Array<FileData>();
        foreach (FileInfo file in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                RaceSave data = SaveRaceScreen.Load(file);
                if (data.Name.IsEmpty() || data.Version < SavedGame.SaveGameVersion)
                    continue;

                string info = "Race Name: " + data.Traits.Name;
                string extraInfo = data.ModName.NotEmpty() ? $"Mod: {data.ModName}" : "Vanilla";
                string tooltip = file.Name;
                saves.Add(new(file, data, data.Name, info, extraInfo, tooltip, data.Traits.FlagIcon, data.Traits.Color)
                {
                    Enabled = GlobalStats.IsValidForCurrentMod(data.ModName)
                });
            }
            catch
            {
            }
        }
        AddItemsToSaveSL(saves.OrderBy(data => data.FileName));
    }
}
