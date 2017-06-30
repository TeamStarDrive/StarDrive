﻿using System.IO;
using System.Linq;

namespace Ship_Game
{
    public sealed class LoadRaceScreen : GenericLoadSaveScreen
    {
        private RaceDesignScreen screen;

        public LoadRaceScreen(RaceDesignScreen screen) : base(screen, SLMode.Load, "", "Load Saved Race", "Saved Races")
        {
            this.screen = screen;
            Path = Dir.ApplicationData + "/StarDrive/Saved Races/";
        }

        protected override void Load()
        {
            if (selectedFile != null)
            {
                screen.SetCustomEmpireData((selectedFile.Data as RaceSave)?.Traits);
            }
            else
            {
                GameAudio.PlaySfxAsync("UI_Misc20");
            }
            this.ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path))
            {
                try
                {
                    RaceSave data = fileInfo.Deserialize<RaceSave>();
                    if (string.IsNullOrEmpty(data.Name) || data.Version < 308)
                        continue;

                    if (GlobalStats.ActiveMod != null)
                    {
                        if (data.ModPath != GlobalStats.ActiveMod.ModName)
                            continue;
                    }
                    else if (!string.IsNullOrEmpty(data.ModPath))
                        continue;

                    string info = "Race Name: " + data.Traits.Name;
                    string extraInfo = (data.ModName != "" ? "Mod: " + data.ModName : "Default");
                    saves.Add(new FileData(fileInfo, data, data.Name, info, extraInfo));
                }
                catch
                {
                }
            }

            var sortedList = from data in saves orderby data.FileName ascending select data;
            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
        }
    }
}