using System;
using System.IO;
using System.Linq;

namespace Ship_Game
{
    public sealed class SaveGameScreen : GenericLoadSaveScreen
    {
        readonly UniverseScreen Screen;

        public SaveGameScreen(UniverseScreen screen) 
            : base(screen, SLMode.Save, screen.PlayerLoyalty + ", Star Date " + screen.StarDateString, "Save Game", "Saved Games", "Saved Game already exists.  Overwrite?")
        {
            Screen = screen;
            Path = Dir.StarDriveAppData + "/Saved Games/";
        }

        public override void DoSave()
        {
            // Save must run on the empire thread to ensure thread safety
            RunOnEmpireThread(() =>
            {
                var savedGame = new SavedGame(Screen, EnterNameArea.Text);
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

        protected override void InitSaveList()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path + "Headers", "xml"))
            {
                try
                {
                    var data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(fileInfo);

                    if (data.SaveName.IsEmpty())
                    {
                        data.SaveName = fileInfo.NameNoExt(); // set name before it's used
                        data.Version = 0;
                    }

                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.OldZipExt);
                    if (!data.FI.Exists)
                        data.FI = new FileInfo(Path + data.SaveName + SavedGame.NewZipExt);
                    if (!data.FI.Exists)
                    {
                        Log.Warning($"Missing save payload {data.FI.FullName}");
                        continue;
                    }

                    string info = $"{data.PlayerName} StarDate {data.StarDate}";
                    string extraInfo = data.RealDate;
                    IEmpireData empire = ResourceManager.AllRaces.FirstOrDefault(e => e.Name == data.PlayerName)
                                      ?? ResourceManager.AllRaces[0];
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo, empire.Traits.FlagIcon, empire.Traits.Color));
                }
                catch
                {
                }
            }

            AddItemsToSaveSL(saves.OrderByDescending(header => (header.Data as HeaderData)?.Time));
        }
    }
}