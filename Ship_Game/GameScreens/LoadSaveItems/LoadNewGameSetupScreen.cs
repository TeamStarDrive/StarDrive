using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;

namespace Ship_Game;

public sealed class LoadNewGameSetupScreen : GenericLoadSaveScreen
{
    readonly RaceDesignScreen Screen;

    public LoadNewGameSetupScreen(RaceDesignScreen screen)
        : base(screen, SLMode.Load, "", "Load Saved Setup", "Saved Setups")
    {
        Screen = screen;
        Path = Dir.StarDriveAppData + "/Saved Setups/";
    }

    protected override void Load()
    {
        if (SelectedFile is { Enabled: true, Data: SetupSave setupSave })
        {
            Screen.SetCustomSetup(setupSave.Settings);
        }
        else
        {
            GameAudio.NegativeClick();
        }
        ExitScreen();
    }

    protected override void InitSaveList() // Set list of files to show
    {
        var saves = new Array<FileData>();
        foreach (FileInfo file in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                SetupSave data = SaveNewGameSetupScreen.Load(file);
                if (data.Name.IsEmpty())
                    continue;

                string info = data.Date;
                string extraInfo = data.ModName.NotEmpty() ? $"Mod: {data.ModName}" : "Vanilla";
                string tooltip = file.Name;
                saves.Add(new(file, data, data.Name, info, extraInfo, tooltip, null, Color.White)
                {
                    Enabled = GlobalStats.IsValidForCurrentMod(data.ModName)
                });
            }
            catch
            {
            }
        }

        saves.Sort(data => data.FileName);
        AddItemsToSaveSL(saves);
    }
}