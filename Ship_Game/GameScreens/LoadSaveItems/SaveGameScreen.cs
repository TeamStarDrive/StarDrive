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
            var savedGame = new SavedGame(Screen, EnterNameArea.Text);
            ExitScreen();
        }

        protected override void DeleteFile()
        {
            try
            {
                FileInfo headerToDel = new FileInfo(string.Concat(Path, "Headers/", fileToDel.Name.Substring(0, fileToDel.Name.LastIndexOf('.'))));       // find header of save file
                //Log.Info(headerToDel.FullName);
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
                    HeaderData data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(fileInfo);

                    if (string.IsNullOrEmpty(data.SaveName))
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
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo));
                }
                catch
                {
                }
            }
            var sortedList = from header in saves orderby (header.Data as HeaderData)?.Time descending select header;
            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddSubItem(data.FileLink);
        }
    }
}