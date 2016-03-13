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

        private EmpireData data;

        public SaveRaceScreen(RaceDesignScreen screen, EmpireData data) : base(SLMode.Save, data.Traits.Name, "Save Race", "Saved Race already exists.  Overwrite?")
        {
            this.screen = screen;
            this.data = data;
        }

        public override void DoSave()
        {
            data.Traits.Name = this.EnterNameArea.Text;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            XmlSerializer Serializer = new XmlSerializer(typeof(EmpireData));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Races/", data.Traits.Name, ".xml"));
            Serializer.Serialize(WriteFileStream, data);
            //WriteFileStream.Close();
            WriteFileStream.Dispose();
            this.ExitScreen();
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            EmpireData data = e.item as EmpireData;

            fh.FileName = data.Traits.Name;
            fh.Info = String.Concat( "Original Race: ", data.PortraitName );
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
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
                    XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
                    EmpireData data = (EmpireData)serializer1.Deserialize(file);
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
            this.data = e.item as EmpireData;
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as EmpireData).Traits.Name;
        }

        protected override bool CheckOverWrite()
        {
            foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text == (entry.item as EmpireData).Traits.Name)
                {
                    return false;
                }
            }

            return true;
        }
    }
}