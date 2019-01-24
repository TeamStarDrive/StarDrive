using System;
using System.IO;
using System.Linq;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class LoadSaveScreen : GenericLoadSaveScreen
    {
        private UniverseScreen screen;
        private MainMenuScreen mmscreen;

        public LoadSaveScreen(UniverseScreen screen) : base(screen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
        {
            this.screen = screen;
            Path = Dir.StarDriveAppData +  "/Saved Games/";
        }
        public LoadSaveScreen(MainMenuScreen mmscreen) : base(mmscreen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
        {
            this.mmscreen = mmscreen;
            Path = Dir.StarDriveAppData + "/Saved Games/";
        }
        public LoadSaveScreen(GameScreen screen) : base(screen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
        {
            Path = Dir.StarDriveAppData + "/Saved Games/";
        }
        protected override void DeleteFile(object sender, EventArgs e)
        {
            try
            {
                // find header of save file
                var headerToDel = new FileInfo(Path + "Headers/" + fileToDel.Name.Substring(0, fileToDel.Name.LastIndexOf('.')));
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile(sender, e);
        }

        protected override void Load()
        {
            if (selectedFile != null)
            {
                screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(selectedFile.FileLink));
                mmscreen?.ExitScreen();
            }
            else
            {
                GameAudio.NegativeClick();
            }
            ExitScreen();
        }

        protected override void InitSaveList()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo saveHeaderFile in Dir.GetFiles(Path + "Headers", "xml"))
            {
                try
                {
                    HeaderData data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(saveHeaderFile);
                    if (data.SaveGameVersion != SavedGame.SaveGameVersion)
                        continue;
                    if (string.IsNullOrEmpty(data.SaveName))
                        continue;

                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.NewZipExt);

                    if (!data.FI.Exists)
                    {
                        Log.Info($"Savegame missing payload: {data.FI.FullName}");
                        continue;
                    }
                    
                    if (GlobalStats.HasMod)
                    {
                        // check mod and check version of save file since format changed
                        if (data.Version > 0 && data.ModPath != GlobalStats.ActiveMod.ModName ||
                            data.Version == 0 && data.ModName != GlobalStats.ActiveMod.ModName)
                            continue;
                    }
                    else if (data.Version > 0 && !string.IsNullOrEmpty(data.ModPath) ||
                             data.Version == 0 && !string.IsNullOrEmpty(data.ModName))
                        continue; // skip non-mod savegames

                    string info = data.PlayerName + " StarDate " + data.StarDate + " (sav)"; ;

                    string extraInfo = data.RealDate;
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo));
                }
                catch
                {
                }
            }

            var sortedList = from header in saves
                             orderby (header.Data as HeaderData)?.Time 
                             descending select header;

            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddSubItem(data.FileLink);
        }

    }
}