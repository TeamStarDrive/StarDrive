using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using SDUtils;
using Ship_Game.GameScreens.LoadGame;

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

        // Set list of files to show
        protected override void InitSaveList()
        {
            var ser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            var saveFiles = Dir.GetFiles(Path, "sav");
            var saves = new Array<FileData>();
            foreach (FileInfo saveFile in saveFiles)
            {
                try
                {
                    HeaderData header = LoadGame.PeekHeader(saveFile);
                    saves.Add(FileData.FromSaveHeader(saveFile, header));
                }
                catch (Exception e)
                {
                    Log.Warning($"Error parsing SaveHeader {saveFile.Name}: {e.Message}");
                }
            }

            AddItemsToSaveSL(saves.OrderByDescending(header => (header.Data as HeaderData)?.Time));
        }
    }
}