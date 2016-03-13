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

        private EmpireData activeRace;

        public LoadRaceScreen(RaceDesignScreen screen) : base(SLMode.Load, "", "Load Saved Race", "")
        {
            this.screen = screen;
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            EmpireData data = e.item as EmpireData;

            fh.FileName = data.Traits.Name;
            fh.Info = String.Concat("Original Race: ", data.PortraitName);
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
        }

        protected override void Load()
        {
            if (this.activeRace != null)
            {
                this.screen.SetCustomEmpireData(this.activeRace);
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
            List<EmpireData> saves = new List<EmpireData>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Races/"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    //EmpireData data = (EmpireData)ResourceManager.HeaderSerializer.Deserialize(file);
                    XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
                    EmpireData data = (EmpireData)serializer1.Deserialize(file);
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
            IOrderedEnumerable<EmpireData> sortedList =
                from data in saves
                orderby data.Traits.Name descending
                select data;
            foreach (EmpireData data in sortedList)
            {
                this.SavesSL.AddItem(data);
            }
        }

        protected override void SwitchFile(ScrollList.Entry e)
        {
            this.activeRace = (e.item as EmpireData);
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as EmpireData).Traits.Name;
        }
    }
}