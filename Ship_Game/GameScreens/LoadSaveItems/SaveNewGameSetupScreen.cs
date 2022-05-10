using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game
{
    public sealed class SaveNewGameSetupScreen : GenericLoadSaveScreen
    {
        readonly SetupSave SS;

        public SaveNewGameSetupScreen(RaceDesignScreen screen,
            GameDifficulty gameDifficulty,
            RaceDesignScreen.StarsAbundance numStars,
            GalSize galaxySize,
            int gamePacing,
            ExtraRemnantPresence extraRemnant,
            int numOpponents,
            RaceDesignScreen.GameMode mode) 
            : base(screen, SLMode.Save, "New Saved Setup", "Save Setup", "Saved Setups", 
                                        "Saved Setup already exists.  Overwrite?")
        {
            Path = Dir.StarDriveAppData + "/Saved Setups/";
            SS = new SetupSave(gameDifficulty, numStars, galaxySize, gamePacing, extraRemnant, numOpponents, mode);
        }

        public override void DoSave()
        {
            SS.Name = EnterNameArea.Text;
            XmlSerializer Serializer = new XmlSerializer(typeof(SetupSave));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(Path, EnterNameArea.Text, ".xml"));
            Serializer.Serialize(WriteFileStream, SS);
            WriteFileStream.Dispose();
            ExitScreen();
        }

        protected override void InitSaveList()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path))
            {
                try
                {
                    SetupSave data = fileInfo.Deserialize<SetupSave>();
                    if (string.IsNullOrEmpty(data.Name))
                    {
                        data.Name = fileInfo.NameNoExt();
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
                        extraInfo = (data.ModName != "" ? "Mod: " + data.ModName : "Default");
                    }
                    saves.Add(new FileData(fileInfo, data, data.Name, info, extraInfo, null, Color.White));
                }
                catch
                {
                }
            }
            
            AddItemsToSaveSL(saves.OrderBy(data => data.FileName));
        }
    }
}