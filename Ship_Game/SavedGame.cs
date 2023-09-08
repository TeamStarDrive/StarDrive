using System;
using System.Globalization;
using System.IO;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
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
        public const int SaveGameVersion = 17;

        public bool Verbose;

        public readonly UniverseState State;
        public FileInfo SaveFile;

        public static string DefaultSaveGameFolder => Dir.StarDriveAppData + "/Saved Games/";

        public SavedGame(UniverseScreen screen)
        {
            // clean up and submit objects before saving
            State = screen.UState;
        }

        public void Save(string saveAs)
        {
            SaveFile = new($"{DefaultSaveGameFolder}{saveAs}.sav");
            SaveUniverseData(State, SaveFile, collectMemory: false);
        }

        public TaskResult SaveAsync(string saveAs, Action<Exception> finished)
        {
            SaveFile = new($"{DefaultSaveGameFolder}{saveAs}.sav");

            // All of this data can be serialized in parallel,
            // because we already built `SaveData` object, which no longer depends on UniverseScreen
            var saveTask = Parallel.Run(() =>
            {
                try
                {
                    SaveUniverseData(State, SaveFile, collectMemory:false);
                    finished(null);
                }
                catch (Exception e)
                {
                    finished(e);
                }
            });
            return saveTask;
        }

        void SaveUniverseData(UniverseState state, FileInfo saveFile, bool collectMemory)
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

            Log.Write($"Binary Total Save elapsed: {t.Elapsed:0.00}s ({saveFile.Length / (1024.0 * 1024.0):0.0}MB)");
            if (collectMemory)
                HelperFunctions.CollectMemory();

            Log.ConfigureStatsReporter(saveFile.FullName);
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
