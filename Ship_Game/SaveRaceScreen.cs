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
    public sealed class SaveRaceScreen : GenericLoadSaveScreen, IDisposable
    {
        private RaceDesignScreen screen;

        private RaceSave RS;

        public SaveRaceScreen(RaceDesignScreen screen, RacialTrait data) : base(SLMode.Save, data.Name, "Save Race", "Saved Race already exists.  Overwrite?")
        {
            this.screen = screen;
            this.RS = new RaceSave(data);            // save some extra info for filtering purposes
        }

        public override void DoSave()
        {
            this.RS.Name = this.EnterNameArea.Text;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            XmlSerializer Serializer = new XmlSerializer(typeof(RaceSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Races/", this.RS.Name, ".xml"));
            Serializer.Serialize(WriteFileStream, this.RS);
            WriteFileStream.Dispose();
            //WriteFileStream.Close();
            this.ExitScreen();
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            RaceSave data = e.item as RaceSave;

            fh.FileName = data.Name;
            fh.Info = String.Concat( "Original Race: ", data.Traits.ShipType );
            fh.ExtraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
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
                    XmlSerializer serializer1 = new XmlSerializer(typeof(RaceSave));
                    RaceSave data = (RaceSave)serializer1.Deserialize(file);
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
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as RaceSave).Name;
        }

        protected override bool CheckOverWrite()
        {
            foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text == (entry.item as RaceSave).Name)
                {
                    return false;
                }
            }

            return true;
        }
    }
}