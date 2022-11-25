using System;
using System.IO;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.LoadGame;
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
            Path = SavedGame.DefaultSaveGameFolder;
        }

        public LoadSaveScreen(MainMenuScreen screen)
            : base(screen, SLMode.Load, "", Localizer.Token(GameText.LoadSavedGame), "Saved Games", true)
        {
            Screen = screen;
            Path = SavedGame.DefaultSaveGameFolder;
        }

        protected override void Load()
        {
            if (SelectedFile != null)
            {
                // if caller was UniverseScreen, this will Unload the previous universe
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
            if (SelectedFile == null)
            {
                GameAudio.NegativeClick();
                return;
            }

            string savedFileName = ExportSave(SelectedFile);

            string message = $"The selected save was exported to your desktop as {savedFileName}";
            int messageWidth = ((int)Fonts.Arial12Bold.MeasureString(savedFileName).X + 20).UpperBound(400);
            ScreenManager.AddScreen(new MessageBoxScreen(this, message, MessageBoxButtons.Ok, messageWidth));
        }

        string ExportSave(FileData save)
        {
            Log.FlushAllLogs();

            string fileName = save.FileName;
            var dirInfo = new DirectoryInfo(Path + "/" + fileName);
            dirInfo.Create();
            string tmpDir = dirInfo.FullName;

            save.FileLink.CopyTo($"{tmpDir}/{save.FileName}{save.FileLink.Extension}", overwrite:true);

            // also add both logfiles
            if (File.Exists("blackbox.log"))
                File.Copy("blackbox.log", $"{tmpDir}/blackbox.log", overwrite:true);
            if (File.Exists("blackbox.old.log"))
                File.Copy("blackbox.old.log", $"{tmpDir}/blackbox.old.log", overwrite:true);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outZip = $"{GetDebugVers()}_{fileName}.zip";
            HelperFunctions.CompressDir(dirInfo, $"{desktop}/{outZip}");
            dirInfo.Delete(true);

            return outZip;
        }

        static string GetDebugVers()
        {
            string blackBox = GlobalStats.ExtendedVersionNoHash
                                         .Replace(":", "")
                                         .Replace(" ", "_")
                                         .Replace("/", "_");

            string modTitle = "";
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ModName;
                string version = GlobalStats.ActiveModInfo.Version;
                if (version.NotEmpty() && !title.Contains(version))
                    modTitle = title + "-" + version;

                modTitle = modTitle.Replace(":", "").Replace(" ", "_");
                return $"{blackBox}_{modTitle}";
            }
            return blackBox;
        }

        // Set list of files to show
        protected override void InitSaveList()
        {
            var saves = new Array<FileData>();
            var saveFiles = Dir.GetFiles(Path, "sav");
            foreach (FileInfo saveFile in saveFiles)
            {
                try
                {
                    HeaderData header = LoadGame.PeekHeader(saveFile);

                    // GlobalStats.ModName is "" if no active mods
                    if (header != null && // null if saveFile is not a valid binary save
                        header.Version == SavedGame.SaveGameVersion &&
                        header.ModName == GlobalStats.ModName)
                    {
                        saves.Add(FileData.FromSaveHeader(saveFile, header));
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"Error parsing SaveGame header {saveFile.Name}: {e.Message}");
                }
            }

            AddItemsToSaveSL(saves.OrderByDescending(header => (header.Data as HeaderData)?.Time));
        }

    }
}
