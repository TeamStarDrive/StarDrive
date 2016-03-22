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

        public SaveRaceScreen(RaceDesignScreen screen, RacialTrait data) : base(SLMode.Save, data.Name, "Save Race", "Saved Race already exists.  Overwrite?")
        {
            this.screen = screen;
            this.selectedFile = new FileData(new RaceSave(data), this.TitleText);            // save some extra info for filtering purposes
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Races/");
        }

        public override void DoSave()
        {
            this.selectedFile.FileName = this.EnterNameArea.Text;
            (this.selectedFile.Data as RaceSave).Name = this.EnterNameArea.Text;
            XmlSerializer Serializer = new XmlSerializer(typeof(RaceSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(this.Path, this.selectedFile.FileName, ".xml"));
            Serializer.Serialize(WriteFileStream, this.selectedFile.Data as RaceSave);
            WriteFileStream.Dispose();
            //WriteFileStream.Close();
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

                    if (string.IsNullOrEmpty(data.Name))
                    {
                        data.Name = filesFromDirectory[i].Name;
                        data.Name = data.Name.Substring(0, data.Name.LastIndexOf('.'));
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