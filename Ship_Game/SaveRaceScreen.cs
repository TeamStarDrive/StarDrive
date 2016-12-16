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

        public SaveRaceScreen(RaceDesignScreen screen, RacialTrait data) : base(SLMode.Save, data.Name, "Save Race", "Saved Races", "Saved Race already exists.  Overwrite?")
        {
            this.screen = screen;
            //this.selectedFile = new FileData(null, new RaceSave(data), this.TitleText);            // save some extra info for filtering purposes
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Races/");
            this.RS = new RaceSave(data);
        }

        public override void DoSave()
        {
            this.RS.Name = this.EnterNameArea.Text;
            XmlSerializer Serializer = new XmlSerializer(typeof(RaceSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(this.Path, this.EnterNameArea.Text, ".xml"));
            Serializer.Serialize(WriteFileStream, this.RS);
            WriteFileStream.Dispose();
            this.ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            List<FileData> saves = new List<FileData>();
            FileInfo[] filesFromDirectory = Dir.GetFiles(this.Path);
            foreach (FileInfo fileInfo in filesFromDirectory)
            {
                try
                {
                    RaceSave data = fileInfo.Deserialize<RaceSave>();

                    if (string.IsNullOrEmpty(data.Name))
                    {
                        data.Name = fileInfo.NameNoExt();
                        data.Version = 0;
                    }

                    string info;
                    string extraInfo;
                    if (data.Version < 308)     // Version checking
                    {
                        info = "Invalid Race File";
                        extraInfo = "";
                    } else {
                        info = String.Concat("Race Name: ", data.Traits.Name);
                        extraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
                    }
                    saves.Add(new FileData(fileInfo, data, data.Name, info, extraInfo));
                }
                catch
                {
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