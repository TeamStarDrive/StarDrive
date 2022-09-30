using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Binary;

namespace Ship_Game
{
    [StarDataType]
    public sealed class HeaderData
    {
        [StarData] public int Version;
        [StarData] public string SaveName;
        [StarData] public string StarDate;
        [StarData] public string PlayerName;
        [StarData] public string RealDate;
        [StarData] public string ModName = "";
        [StarData] public DateTime Time;
    }

    public sealed class SavedGame
    {
        // Every time the savegame layout changes significantly,
        // this version needs to be bumped to avoid loading crashes
        public const int SaveGameVersion = 14;

        public bool Verbose;

        public readonly UniverseState State;
        public FileInfo SaveFile;

        public static bool IsSaving  => GetIsSaving();
        public static bool NotSaving => !IsSaving;
        public static string DefaultSaveGameFolder => Dir.StarDriveAppData + "/Saved Games/";

        static TaskResult SaveTask;

        public SavedGame(UniverseScreen screen)
        {
            // clean up and submit objects before saving
            State = screen.UState;
        }

        static bool GetIsSaving()
        {
            if (SaveTask == null)
                return false;
            if (!SaveTask.IsComplete)
                return true;

            SaveTask = null; // avoids some nasty memory leak issues
            return false;
        }

        public void Save(string saveAs, bool async)
        {
            string destFolder = DefaultSaveGameFolder;
            SaveFile = new FileInfo($"{destFolder}{saveAs}.sav");

            if (!async)
            {
                SaveUniverseData(State, SaveFile);
            }
            else
            {
                // All of this data can be serialized in parallel,
                // because we already built `SaveData` object, which no longer depends on UniverseScreen
                SaveTask = Parallel.Run(() =>
                {
                    SaveUniverseData(State, SaveFile);
                });
            }
        }

        void SaveUniverseData(UniverseState state, FileInfo saveFile)
        {
            var t = new PerfTimer();

            DateTime now = DateTime.Now;

            var header = new HeaderData
            {
                Version    = SaveGameVersion,
                SaveName   = SaveFile.NameNoExt(),
                StarDate   = state.StarDate.ToString("#.0"),
                PlayerName = state.Player.data.Traits.Name,
                RealDate   = now.ToString("M/d/yyyy") + " " + now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat),
                ModName    = GlobalStats.ModName,
                Time       = now,
            };

            // an annoying edge case, someone has created a folder with the same name
            if (Directory.Exists(saveFile.FullName))
                Directory.Delete(saveFile.FullName, recursive: true);

            using (var writer = new Writer(new FileStream(saveFile.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096)))
            {
                BinarySerializer.SerializeMultiType(writer, new object[] { header, state }, Verbose);
            }

            SaveTask = null;
            Log.Info($"Binary Total Save elapsed: {t.Elapsed:0.00}s ({saveFile.Length / (1024.0 * 1024.0):0.0}MB)");

            HelperFunctions.CollectMemory();
        }

        public static UniverseState Deserialize(FileInfo saveFile, bool verbose)
        {
            var t = new PerfTimer();

            using var reader = new Reader(saveFile.OpenRead());
            var results = BinarySerializer.DeserializeMultiType(reader, new[]
            {
                typeof(HeaderData),
                typeof(UniverseState)
            }, verbose);

            var usData = (UniverseState)results[1];
            Log.Info($"Binary Total Load elapsed: {t.Elapsed:0.0}s  ");
            return usData;
        }
    }
}
