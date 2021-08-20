using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class LoadSaveScreen : GenericLoadSaveScreen
    {
        GameScreen Screen;

        public LoadSaveScreen(UniverseScreen screen)
            : base(screen, SLMode.Load, "", Localizer.Token(GameText.LoadSavedGame), "Saved Games", true)
        {
            Screen = screen;
            Path = Dir.StarDriveAppData +  "/Saved Games/";
        }

        public LoadSaveScreen(MainMenuScreen screen)
            : base(screen, SLMode.Load, "", Localizer.Token(GameText.LoadSavedGame), "Saved Games", true)
        {
            Screen = screen;
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
                Screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(SelectedFile.FileLink));
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
                var dirInfo = new DirectoryInfo(Path + "/" + fileName);
                dirInfo.Create();
                SelectedFile.FileLink.CopyTo(dirInfo.FullName + "/" + SelectedFile.FileLink.Name, true);
                var header = new FileInfo(Path + "/Headers/" + fileName + ".json");

                if (!header.Exists)
                {
                    string err = $"Header does not exist: 'Headers/{fileName}.json' for '{fileName}'";
                    ScreenManager.AddScreen(new MessageBoxScreen(this, err, MessageBoxButtons.Ok));
                }
                else
                {
                    header.CopyTo(dirInfo.FullName + "/" + header.Name, true);
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

                    if (data.SaveGameVersion != SavedGame.SaveGameVersion || string.IsNullOrEmpty(data.SaveName))
                        continue;

                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.ZipExt);
                    if (!data.FI.Exists)
                    {
                        Log.Warning($"Missing save payload: {data.FI.FullName}");
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
                    {
                        continue; // skip non-mod savegames
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
