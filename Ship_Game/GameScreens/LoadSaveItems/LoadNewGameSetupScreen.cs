using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
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
            if (SelectedFile is { Data: SetupSave setupSave })
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
                    var save = YamlParser.Deserialize<SetupSave>(file);
                    if (save.Name.IsEmpty() || save.Version < 308)
                        continue;

                    if (GlobalStats.HasMod)
                    {
                        if (save.ModName != GlobalStats.ActiveMod.ModName)
                            continue;
                    }
                    else if (save.ModName.NotEmpty()) // we have no mod, but this is for some mod
                        continue;

                    string info = save.Date;
                    string extraInfo = save.ModName != "" ? "Mod: "+save.ModName : "Default";
                    saves.Add(new(file, save, save.Name, info, extraInfo, null, Color.White));
                }
                catch
                {
                }
            }

            saves.Sort(data => data.FileName);
            AddItemsToSaveSL(saves);
        }
    }
}