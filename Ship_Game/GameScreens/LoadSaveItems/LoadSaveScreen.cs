using System;
using System.IO;
using System.Linq;
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
            : base(screen, SLMode.Load, "", Localizer.Token(GameText.LoadSavedGame), "Saved Games", showSaveExport:true)
        {
            Screen = screen;
            Path = SavedGame.DefaultSaveGameFolder;
        }

        public LoadSaveScreen(MainMenuScreen screen)
            : base(screen, SLMode.Load, "", Localizer.Token(GameText.LoadSavedGame), "Saved Games", showSaveExport:true)
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
