using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class SaveSetupScreen : GenericLoadSaveScreen, IDisposable
    {
        private RaceDesignScreen screen;

        private SetupSave SS;

        public SaveSetupScreen(RaceDesignScreen screen, UniverseData.GameDifficulty gameDifficulty, RaceDesignScreen.StarNum StarEnum, RaceDesignScreen.GalSize Galaxysize, int Pacing, RaceDesignScreen.ExtraRemnantPresence ExtraRemnant, int numOpponents, RaceDesignScreen.GameMode mode) : base(SLMode.Save, "New Saved Setup", "Save Setup", "Saved Setup already exists.  Overwrite?")
        {
            this.screen = screen;
            this.SS = new SetupSave(gameDifficulty, StarEnum, Galaxysize, Pacing, ExtraRemnant, numOpponents, mode);            // save some extra info for filtering purposes
        }

        public override void DoSave()
        {
            this.SS.Name = this.EnterNameArea.Text;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            XmlSerializer Serializer = new XmlSerializer(typeof(SetupSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Setups/", this.SS.Name, ".xml"));
            Serializer.Serialize(WriteFileStream, this.SS);
            WriteFileStream.Dispose();
            //WriteFileStream.Close();
            this.ExitScreen();
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            SetupSave data = e.item as SetupSave;

            fh.FileName = data.Name;
            fh.Info = data.Date;
            fh.ExtraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<SetupSave> saves = new List<SetupSave>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Setups/"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    XmlSerializer serializer1 = new XmlSerializer(typeof(SetupSave));
                    SetupSave data = (SetupSave)serializer1.Deserialize(file);
                    saves.Add(data);
                    file.Dispose();
                }
                catch
                {
                    file.Dispose();
                }
                //Label0:
                //  continue;
            }
            IOrderedEnumerable<SetupSave> sortedList =
                from data in saves
                orderby data.Name descending
                select data;
            foreach (SetupSave data in sortedList)
            {
                this.SavesSL.AddItem(data);
            }
        }

        protected override void SwitchFile(ScrollList.Entry e)
        {
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as SetupSave).Name;
        }

        protected override bool CheckOverWrite()
        {
            foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text == (entry.item as SetupSave).Name)
                {
                    return false;
                }
            }

            return true;
        }
    }
}