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

        public SaveSetupScreen(RaceDesignScreen screen, UniverseData.GameDifficulty gameDifficulty, RaceDesignScreen.StarNum StarEnum, RaceDesignScreen.GalSize Galaxysize, int Pacing, RaceDesignScreen.ExtraRemnantPresence ExtraRemnant, int numOpponents, RaceDesignScreen.GameMode mode) : base(SLMode.Save, "New Saved Setup", "Save Setup", "Saved Setup already exists.  Overwrite?")
        {
            this.screen = screen;
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Setups/");
            this.selectedFile = new FileData(new SetupSave(gameDifficulty, StarEnum, Galaxysize, Pacing, ExtraRemnant, numOpponents, mode), this.TitleText);            // save some extra info for filtering purposes
        }

        public override void DoSave()
        {
            this.selectedFile.FileName = this.EnterNameArea.Text;
            (this.selectedFile.Data as SetupSave).Name = this.EnterNameArea.Text;
            XmlSerializer Serializer = new XmlSerializer(typeof(SetupSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(this.Path, this.selectedFile.FileName, ".xml"));
            Serializer.Serialize(WriteFileStream, this.selectedFile.Data as SetupSave);
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
                    XmlSerializer serializer1 = new XmlSerializer(typeof(SetupSave));
                    SetupSave data = (SetupSave)serializer1.Deserialize(file);
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
                        info = "Invalid Setup File";
                        extraInfo = "";
                    }
                    else
                    {
                        info = data.Date;
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