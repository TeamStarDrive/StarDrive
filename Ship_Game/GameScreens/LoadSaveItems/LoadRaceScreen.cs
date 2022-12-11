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
        foreach (FileInfo fileInfo in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                RaceSave data = SaveRaceScreen.Load(fileInfo);
                if (data.Name.IsEmpty())
                    continue;

                string info = "Race Name: " + data.Traits.Name;
                string extraInfo = data.ModName.NotEmpty() ? $"Mod: {data.ModName}" : "Vanilla";
                saves.Add(new(fileInfo, data, data.Name, info, extraInfo, data.Traits.FlagIcon, data.Traits.Color)
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
