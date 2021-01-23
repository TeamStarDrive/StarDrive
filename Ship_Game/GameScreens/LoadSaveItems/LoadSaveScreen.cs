using System;
using System.IO;
using System.Linq;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class LoadSaveScreen : GenericLoadSaveScreen
    {
        UniverseScreen screen;
        MainMenuScreen mmscreen;

        public LoadSaveScreen(UniverseScreen screen) : base(screen, SLMode.Load, "", Localizer.Token(6), "Saved Games", true)
        {
            this.screen = screen;
            Path = Dir.StarDriveAppData +  "/Saved Games/";
        }
        public LoadSaveScreen(MainMenuScreen mmscreen) : base(mmscreen, SLMode.Load, "", Localizer.Token(6), "Saved Games", true)
        {
            this.mmscreen = mmscreen;
            Path = Dir.StarDriveAppData + "/Saved Games/";
        }
        public LoadSaveScreen(GameScreen screen) : base(screen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
        {
            Path = Dir.StarDriveAppData + "/Saved Games/";
        }
        protected override void DeleteFile()
        {
            try
            {
                // find header of save file
                var headerToDel = new FileInfo(Path + "Headers/"+FileToDelete.FileLink.NameNoExt());
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile();
        }

        protected override void Load()
        {
            if (SelectedFile != null)
            {
                screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(SelectedFile.FileLink));
                mmscreen?.ExitScreen();
            }
            else
            {
                GameAudio.NegativeClick();
            }
            ExitScreen();
        }

        protected override void ExportSave()
        {
            if (SelectedFile != null)
            {
                string fileName = SelectedFile.FileName;
                var dirInfo     = new DirectoryInfo(Path + "/" + fileName);
                dirInfo.Create();
                SelectedFile.FileLink.CopyTo(dirInfo.FullName + "/" + SelectedFile.FileLink.Name, true);
                var header = new FileInfo(Path + "/Headers/" + fileName + ".xml");
                var fog    = new FileInfo(Path + "/Fog Maps/" + fileName + "fog.png");

                string error = "";
                if (!header.Exists)
                    error = "header.xml file does not exist.";

                if (!fog.Exists)
                    error = $"{error}. Fog map does not exist.";

                if (error.NotEmpty())
                {
                    error = $"{error} For {fileName}";
                    ScreenManager.AddScreen(new MessageBoxScreen(this, error, MessageBoxButtons.Ok));
                }
                else
                {
                    header.CopyTo(dirInfo.FullName + "/" + header.Name, true);
                    fog.CopyTo(dirInfo.FullName + "/" + fog.Name, true);
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string savedFileName = $"{GetDebugVers()}{dirInfo.Name}.zip";
                    HelperFunctions.CompressDir(dirInfo, path + "/" + savedFileName);
                    dirInfo.Delete(true);
                    string message = $"The selected save was exported to your desktop as {savedFileName}";
                    int messageWidth = ((int)Fonts.Arial12Bold.MeasureString(savedFileName).X + 20).UpperBound(400);
                    ScreenManager.AddScreen(new MessageBoxScreen(this, message, MessageBoxButtons.Ok, messageWidth));
                    return;
                }
            }

            GameAudio.NegativeClick();
        }

        string GetDebugVers()
        {
            string blackBox = GlobalStats.ExtendedVersionNoHash
                                         .Replace(":", "")
                                         .Replace(" ", "_")
                                         .Replace("/", "_");

            string modTitle = "";
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ActiveModInfo.ModName;
                string version = GlobalStats.ActiveModInfo.Version;
                if (version.NotEmpty() && !title.Contains(version))
                    modTitle = title + "-" + version;

                modTitle = modTitle.Replace(":", "").Replace(" ", "_");
            }

            return $"{blackBox}_{modTitle}_"; 
        }

        protected override void InitSaveList()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo saveHeaderFile in Dir.GetFiles(Path + "Headers", "xml"))
            {
                try
                {
                    var data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(saveHeaderFile);
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