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

        private RaceSave RS;

        public LoadRaceScreen(RaceDesignScreen screen) : base(SLMode.Load, "", "Load Saved Race", "")
        {
            this.screen = screen;
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            RaceSave data = e.item as RaceSave;

            fh.FileName = data.Name;
            fh.Info = String.Concat("Original Race: ", data.Traits.ShipType);
            fh.ExtraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
        }

        protected override void Load()
        {
            if (this.RS != null)
            {
                this.screen.SetCustomEmpireData(this.RS.Traits);
            }
            else
            {
                AudioManager.PlayCue("UI_Misc20");
            }
            this.ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<RaceSave> saves = new List<RaceSave>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Races/"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    //EmpireData data = (EmpireData)ResourceManager.HeaderSerializer.Deserialize(file);
                    XmlSerializer serializer1 = new XmlSerializer(typeof(RaceSave));
                    RaceSave data = (RaceSave)serializer1.Deserialize(file);
                    if (GlobalStats.ActiveMod != null)
                    {
                        if (data.ModPath != GlobalStats.ActiveMod.ModPath)
                        {
                            //file.Close();
                            file.Dispose();
                            continue;
                        }
                    }
                    else if (!string.IsNullOrEmpty(data.ModPath))
                    {
                        //file.Close();
                        file.Dispose();
                        continue;
                    }
                    saves.Add(data);
                    //file.Close();
                    file.Dispose();
                }
                catch
                {
                    //file.Close();
                    file.Dispose();
                }
                //Label0:
                //  continue;
            }
            IOrderedEnumerable<RaceSave> sortedList =
                from data in saves
                orderby data.Name descending
                select data;
            foreach (RaceSave data in sortedList)
            {
                this.SavesSL.AddItem(data);
            }
        }

        protected override void SwitchFile(ScrollList.Entry e)
        {
            this.RS = (e.item as RaceSave);
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as RaceSave).Name;
        }
    }
}