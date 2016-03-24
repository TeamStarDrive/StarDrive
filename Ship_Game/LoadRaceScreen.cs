using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class LoadRaceScreen : GenericLoadSaveScreen, IDisposable
    {
        private RaceDesignScreen screen;

        public LoadRaceScreen(RaceDesignScreen screen) : base(SLMode.Load, "", "Load Saved Race", "Saved Races")
        {
            this.screen = screen;
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Races/");
        }

        protected override void Load()
        {
            if (this.selectedFile != null)
            {
                this.screen.SetCustomEmpireData((this.selectedFile.Data as RaceSave).Traits);
            }
            else
            {
                AudioManager.PlayCue("UI_Misc20");
            }
            this.ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            List<FileData> saves = new List<FileData>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(this.Path);
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    XmlSerializer serializer1 = new XmlSerializer(typeof(RaceSave));
                    RaceSave data = (RaceSave)serializer1.Deserialize(file);
                    if (string.IsNullOrEmpty(data.Name) || data.Version < 308)
                    {
                        file.Dispose();
                        continue;
                    }

                    if (GlobalStats.ActiveMod != null)
                    {
                        if (data.ModPath != GlobalStats.ActiveMod.ModPath)
                        {
                            file.Dispose();
                            continue;
                        }
                    }
                    else if (!string.IsNullOrEmpty(data.ModPath))
                    {
                        file.Dispose();
                        continue;
                    }

                    string info = String.Concat("Race Name: ", data.Traits.Name);
                    string extraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
                    saves.Add(new FileData(filesFromDirectory[i], data, data.Name, info, extraInfo));
                    file.Dispose();
                }
                catch
                {
                    file.Dispose();
                }
            }
            IOrderedEnumerable<FileData> sortedList =
                from data in saves
                orderby data.FileName ascending
                select data;
            foreach (FileData data in sortedList)
            {
                this.SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
            }
        }
    }
}