using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using SDUtils;

namespace Ship_Game
{
    public sealed class SaveGameScreen : GenericLoadSaveScreen
    {
        readonly UniverseScreen Screen;

        public SaveGameScreen(UniverseScreen screen) 
            : base(screen, SLMode.Save, screen.PlayerLoyalty + ", Star Date " + screen.StarDateString, "Save Game", "Saved Games", "Saved Game already exists.  Overwrite?")
        {
            Screen = screen;
            Path = SavedGame.DefaultSaveGameFolder;
        }

        public override void DoSave()
        {
            // Save must run on the empire thread to ensure thread safety
            RunOnEmpireThread(() =>
            {
                Screen.Save(EnterNameArea.Text);
            });
            ExitScreen();
        }

        protected override void DeleteFile()
        {
            try
            {
                // find header of save file
                var headerToDel = new FileInfo(Path+"Headers/"+FileToDelete.FileLink.NameNoExt());
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile();
        }

        // Set list of files to show
        protected override void InitSaveList()
        {
            var ser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            var saves = new Array<FileData>();
            foreach (FileInfo saveHeaderFile in Dir.GetFiles(Path + "Headers", "json"))
            {
                try
                {
                    HeaderData data;
                    using (var reader = new JsonTextReader(new StreamReader(saveHeaderFile.FullName)))
                        data = ser.Deserialize<HeaderData>(reader);

                    if (string.IsNullOrEmpty(data.SaveName))
                        continue;

                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.ZipExt);
                    if (!data.FI.Exists)
                    {
                        Log.Warning($"Missing save payload: {data.FI.FullName}");
                        continue;
                    }

                    string info = $"{data.PlayerName} StarDate {data.StarDate}";
                    string extraInfo = data.RealDate;

                    IEmpireData empire = ResourceManager.AllRaces.FirstOrDefault(e => e.Name == data.PlayerName)
                                      ?? ResourceManager.AllRaces[0];
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo,
                                           empire.Traits.FlagIcon, empire.Traits.Color));
                }
                catch (Exception e)
                {
                    Log.Warning($"Error parsing SaveHeader {saveHeaderFile.Name}: {e.Message}");
                }
            }

            AddItemsToSaveSL(saves.OrderByDescending(header => (header.Data as HeaderData)?.Time));
        }
    }
}