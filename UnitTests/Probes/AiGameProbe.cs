using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Ship_Game.Universe;
using ResourceManager = Ship_Game.ResourceManager;

namespace UnitTests.Probes;

/// <summary>
/// AI-only game probe: the AI empires play saved galaxies on their own with the player locked out, and every AI empire's
/// planets, wars, losses and fleets are written to TSV files. It does nothing unless PROBE_MODE is set, so the normal
/// test run passes over it. AI_Prompts/ai-only-game-probe.txt explains how to run it and compare two builds or mods.
/// Folders are best given as absolute paths: a relative one is taken from the worktree's game folder.
///
/// PROBE_MODE=generate: PROBE_SAVES (folder), PROBE_FIRST (1), PROBE_COUNT (1), PROBE_PLAYER (United), PROBE_AIS (5),
///   PROBE_GALAXY (Medium), PROBE_DIFFICULTY (Brutal)
/// PROBE_MODE=run: PROBE_ARM (base, new, ...: starts every file name), PROBE_OUT (folder), PROBE_SAVES (folder) or
///   PROBE_SAVE (file), PROBE_ONLY (galaxy names, comma separated), PROBE_TURNS (1500), PROBE_FPS (20 sim steps per
///   game second), PROBE_SAMPLE (10 turns), PROBE_LIMIT (900 wall seconds per game, 0 for none), PROBE_REPEAT (1)
///   Files: summary.tsv, samples.tsv, progress.tsv, errors.tsv and warnings.tsv (each distinct message and its count)
/// Both: PROBE_MOD (folder under game/Mods; unset for the base game), PROBE_THREADS (4)
/// </summary>
[TestClass]
public class AiGameProbe : StarDriveTest
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static string Env(string name, string fallback)
    {
        string v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(v) ? fallback : v;
    }

    static int EnvInt(string name, int fallback)
        => int.TryParse(Env(name, ""), NumberStyles.Integer, Inv, out int v) ? v : fallback;

    static string F(double v) => v.ToString("0.###", Inv);

    [TestMethod]
    public void Probe()
    {
        string mode = Env("PROBE_MODE", "");
        if (mode.Length == 0)
            return;

        int parallelism = GlobalStats.MaxParallelism;
        bool restrict = GlobalStats.RestrictAIPlayerInteraction;
        string mod = Env("PROBE_MOD", "");
        try
        {
            GlobalStats.MaxParallelism = EnvInt("PROBE_THREADS", 4);
            GlobalStats.RestrictAIPlayerInteraction = true;
            ModEntry modEntry = mod.Length > 0 ? new ModEntry(GamePlayGlobals.Deserialize(new FileInfo($"Mods/{mod}/Globals.yaml"))) : null;
            LoadAllGameData(modEntry);
            if (modEntry != null && (!GlobalStats.HasMod || GlobalStats.ModName != modEntry.Mod.Name))
                throw new($"the mod '{mod}' did not load, the base game loaded instead");
            if (mode == "generate")
                Generate();
            else if (mode == "run")
                RunGames();
            else
                throw new($"unknown PROBE_MODE '{mode}'");
        }
        finally
        {
            GlobalStats.MaxParallelism = parallelism;
            GlobalStats.RestrictAIPlayerInteraction = restrict;
            if (mod.Length > 0)
            {
                GlobalStats.SetActiveModNoSave(null);
                ResourceManager.InitContentDir();
            }
        }
    }

    void Generate()
    {
        string dir = Env("PROBE_SAVES", "");
        if (dir.Length == 0) throw new("PROBE_SAVES not set");
        Directory.CreateDirectory(dir);
        string playerRace = Env("PROBE_PLAYER", "United");
        IEmpireData race = ResourceManager.FindEmpire(playerRace) ?? throw new($"no race named '{playerRace}' in this content");
        int first = EnvInt("PROBE_FIRST", 1);
        int count = EnvInt("PROBE_COUNT", 1);
        for (int i = first; i < first + count; ++i)
        {
            UniverseParams p = UniverseParams.NewGame();
            EmpireData player = race.CreateInstance();
            player.DiplomaticPersonality = new DTrait();
            p.PlayerData = player;
            p.NumOpponents = EnvInt("PROBE_AIS", 5);
            p.GalaxySize = Enum.Parse<GalSize>(Env("PROBE_GALAXY", "Medium"));
            p.Difficulty = Enum.Parse<GameDifficulty>(Env("PROBE_DIFFICULTY", "Brutal"));
            (p.NumSystems, p.StarsModifier) = RaceDesignScreen.GetNumStars(p.StarsCount, p.GalaxySize, p.NumOpponents);

            CreateCustomUniverse(p);
            Universe.CreateSimThread = false;
            Universe.LoadContent();
            UState.Objects.Update(new FixedSimTime(0.01f));

            string name = $"galaxy_{i:00}";
            SavedGame save = Universe.Save("AiGameProbe." + name, throwOnError: true);
            File.Move(save.SaveFile.FullName, Path.Combine(dir, name + ".sav"), overwrite: true);

            var races = new StringBuilder();
            foreach (Empire e in UState.MajorEmpires)
                races.Append(e.isPlayer ? "player:" : "").Append(e.Name).Append(';');
            File.AppendAllText(Path.Combine(dir, "galaxies.tsv"),
                $"{name}\t{p.GalaxySize}\t{p.NumSystems}\t{p.Difficulty}\t{p.Pace}\t{races}\r\n");
            Dispose(true);
        }
    }

    sealed class RunSettings
    {
        public string Arm, OutDir;
        public int Turns, Fps, SampleEvery, LimitSeconds;
        public string PathOf(string kind) => Path.Combine(OutDir, $"{Arm}_{kind}");
    }

    void RunGames()
    {
        var run = new RunSettings
        {
            Arm = Env("PROBE_ARM", "arm"),
            OutDir = Env("PROBE_OUT", ""),
            Turns = EnvInt("PROBE_TURNS", 1500),
            Fps = EnvInt("PROBE_FPS", 20),
            SampleEvery = EnvInt("PROBE_SAMPLE", 10),
            LimitSeconds = EnvInt("PROBE_LIMIT", 900),
        };
        if (run.OutDir.Length == 0) throw new("PROBE_OUT not set");
        Directory.CreateDirectory(run.OutDir);

        var saves = new List<string>();
        string single = Env("PROBE_SAVE", "");
        string folder = Env("PROBE_SAVES", "");
        if (single.Length > 0)
        {
            saves.Add(single);
        }
        else
        {
            if (folder.Length == 0) throw new("neither PROBE_SAVES nor PROBE_SAVE is set");
            saves.AddRange(Directory.GetFiles(folder, "galaxy_*.sav"));
            saves.Sort(StringComparer.Ordinal);
            string only = Env("PROBE_ONLY", "");
            if (only.Length > 0)
            {
                var names = new HashSet<string>(only.Split(','));
                saves = saves.FindAll(s => names.Contains(Path.GetFileNameWithoutExtension(s)));
            }
        }
        if (saves.Count == 0) throw new($"no galaxy_*.sav saves to play in '{folder}'");

        int repeat = EnvInt("PROBE_REPEAT", 1);
        foreach (string save in saves)
        {
            for (int r = 1; r <= repeat; ++r)
            {
                string galaxy = GalaxyName(save, repeat > 1 ? r : 0);
                try
                {
                    RunGame(run, save, galaxy);
                }
                catch (Exception e)
                {
                    Append(run.PathOf("summary.tsv"), GameTracker.SummaryHeader, $"{run.Arm}\t{galaxy}\tcrashed");
                    Append(run.PathOf("errors.tsv"), MessagesHeader, $"{run.Arm}\t{galaxy}\t1\tprobe: {e.GetType().Name}: {OneLine(e.Message)}");
                }
            }
        }
    }

    static string GalaxyName(string savePath, int repeat)
        => Path.GetFileNameWithoutExtension(savePath) + (repeat > 0 ? $"#{repeat}" : "");

    static string OneLine(string text) => text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    const string MessagesHeader = "arm\tgalaxy\tcount\tmessage";

    // a log line without its time stamp
    static string MessageOf(string line)
    {
        line = line.TrimEnd('\r');
        int at = line.IndexOf("ms: ", StringComparison.Ordinal);
        return at >= 0 && at < 16 ? line.Substring(at + 4) : line;
    }

    static void WriteMessages(string path, string arm, string galaxy, Dictionary<string, int> counts)
    {
        bool isNew = !File.Exists(path);
        using var w = new StreamWriter(path, append: true, new UTF8Encoding(false));
        if (isNew)
            w.Write(MessagesHeader + "\r\n");
        foreach (KeyValuePair<string, int> kv in counts)
            w.Write($"{arm}\t{galaxy}\t{kv.Value}\t{OneLine(kv.Key)}\r\n");
    }

    static void Append(string path, string header, string line)
    {
        bool isNew = !File.Exists(path);
        using var w = new StreamWriter(path, append: true, new UTF8Encoding(false));
        if (isNew)
            w.Write(header + "\r\n");
        w.Write(line + "\r\n");
    }

    void RunGame(RunSettings run, string savePath, string galaxy)
    {
        long logStart = LogLength();
        var wall = Stopwatch.StartNew();
        UniverseScreen us = new LoadGame(new FileInfo(savePath)).Load(noErrorDialogs: true, startSimThread: false);
        if (us == null)
        {
            Append(run.PathOf("summary.tsv"), GameTracker.SummaryHeader, $"{run.Arm}\t{galaxy}\tload_failed");
            return;
        }

        try
        {
            UniverseState u = us.UState;
            u.CanShowDiplomacyScreen = false;
            var tracker = new GameTracker(u);
            var step = new FixedSimTime(1f / run.Fps);
            float start = u.StarDate;
            int lastTurn = 0;
            string stop = "turns";
            tracker.OnTurn(0);
            tracker.WriteSamples(run, galaxy, 0, u.StarDate, wall.Elapsed.TotalSeconds);

            while (lastTurn < run.Turns)
            {
                u.Paused = false;
                us.SingleSimulationStep(step);
                us.LoadAskedHullParts(); // the hull parts a drawn frame reads
                int turn = (int)Math.Round((u.StarDate - start) * 10f);
                if (turn != lastTurn)
                {
                    lastTurn = turn;
                    tracker.OnTurn(turn);
                    if (turn % run.SampleEvery == 0)
                        tracker.WriteSamples(run, galaxy, turn, u.StarDate, wall.Elapsed.TotalSeconds);
                    if (turn % 50 == 0)
                        Append(run.PathOf("progress.tsv"), "arm\tgalaxy\tturn\twall_s\tships_in_universe\tai_warships",
                            $"{run.Arm}\t{galaxy}\t{turn}\t{F(wall.Elapsed.TotalSeconds)}\t{u.Objects.NumShips}\t{tracker.TotalWarships}");
                }
                if (run.LimitSeconds > 0 && wall.Elapsed.TotalSeconds > run.LimitSeconds)
                {
                    stop = "time";
                    break;
                }
            }

            int errors = 0, warnings = 0;
            var errorCounts = new Dictionary<string, int>();
            var warningCounts = new Dictionary<string, int>();
            foreach (string line in LoggedSince(logStart).Split('\n'))
            {
                string text = MessageOf(line);
                Dictionary<string, int> counts = null;
                if (text.Contains("(!) Error:") || text.Contains("(!) Exception:"))
                {
                    ++errors;
                    counts = errorCounts;
                }
                else if (text.Contains("Warning: "))
                {
                    ++warnings;
                    counts = warningCounts;
                }
                if (counts != null)
                    counts[text] = counts.TryGetValue(text, out int n) ? n + 1 : 1;
            }
            WriteMessages(run.PathOf("errors.tsv"), run.Arm, galaxy, errorCounts);
            WriteMessages(run.PathOf("warnings.tsv"), run.Arm, galaxy, warningCounts);
            tracker.WriteSummary(run, galaxy, stop, lastTurn, wall.Elapsed.TotalSeconds, errors, warnings);
        }
        finally
        {
            us.ExitScreen();
            us.Dispose();
            StarDriveTestContext.FlushGraphics();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    sealed class EmpireStats
    {
        public Empire E;
        public int DefeatedTurn = -1, TurnsAlive;
        public int Lost, Captured, Won, LostTo, Colonized, LostOther, Wars, TurnsAtWar, SectionsLost;
        public double LostStr, StrTask, StrIdle, StrPool, TasksWaiting, Fleets, Kept, Cap, IdleKept;

        public int Warships, NowFleets, NowFleetsOnTask, NowKept, NowCap, NowIdleKept, NowTasks, NowWaiting, NowWars, NowMissingSections;
        public float Str, NowStrTask, NowStrIdle, NowStrPool;

        public void ClearNow()
        {
            Warships = NowFleets = NowFleetsOnTask = NowKept = NowCap = NowIdleKept = NowTasks = NowWaiting = NowWars = NowMissingSections = 0;
            Str = NowStrTask = NowStrIdle = NowStrPool = 0f;
        }
    }

    sealed class GameTracker
    {
        static readonly FieldInfo HasExploded = typeof(Ship).GetField("HasExploded", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo RolledDerelict = typeof(Ship).GetField("RolledDerelict", BindingFlags.Instance | BindingFlags.NonPublic);
        // read by reflection, so the probe builds on commits where the AI keeps no fleets
        static readonly MethodInfo KeptFleetCap = typeof(Empire).GetMethod("KeptFleetCap", Type.EmptyTypes);
        static readonly PropertyInfo FleetKept = typeof(Fleet).GetProperty("Kept");

        static readonly HashSet<string> FleetSizedTasks = new()
        {
            "StrikeForce", "AssaultPlanet", "GlassPlanet", "StageFleet", "ReclaimPlanet", "DefendPostInvasion",
            "ClearAreaOfEnemies", "DefendVsRemnants", "AssaultPirateBase", "InhibitorInvestigate",
        };

        readonly UniverseState U;
        readonly EmpireStats[] Stats;
        readonly Dictionary<Ship, (EmpireStats Owner, float Strength)> Ships = new();
        readonly Dictionary<Ship, uint> MissingSections = new(); // each warship's hull sections off at the last turn, a bit each
        readonly Dictionary<Planet, Empire> PlanetOwners = new();
        readonly HashSet<(int, int)> AtWar = new();
        bool Started;
        int FirstWarTurn = -1;
        public int TotalWarships;

        public GameTracker(UniverseState u)
        {
            U = u;
            var stats = new List<EmpireStats>();
            foreach (Empire e in u.MajorEmpires)
                if (!e.isPlayer)
                    stats.Add(new EmpireStats { E = e });
            Stats = stats.ToArray();
            if (HasExploded == null || RolledDerelict == null)
                throw new("Ship death fields not found");
        }

        EmpireStats StatsOf(Empire e)
        {
            foreach (EmpireStats s in Stats)
                if (s.E == e)
                    return s;
            return null;
        }

        static bool IsWarship(Ship s)
            => !s.IsFreighter && !s.IsPlatformOrStation && !s.IsHomeDefense && !s.IsConstructor && !s.IsSalvageShip
               && !s.IsHangarShip && s.DesignRoleType is RoleType.Warship or RoleType.WarSupport or RoleType.Troop;

        static bool DiedInCombat(Ship s)
            => s.Dying || (bool)HasExploded.GetValue(s) || (bool)RolledDerelict.GetValue(s);

        public void OnTurn(int turn)
        {
            foreach (EmpireStats s in Stats)
                s.ClearNow();
            CountShipsGone();
            CountPlanetChanges();
            CountWars(turn);

            TotalWarships = 0;
            foreach (EmpireStats s in Stats)
            {
                if (s.E.IsDefeated)
                {
                    if (s.DefeatedTurn < 0)
                        s.DefeatedTurn = turn;
                    continue;
                }
                ++s.TurnsAlive;
                ScanShips(s);
                ScanFleetsAndTasks(s);
                TotalWarships += s.Warships;
                s.StrTask += s.NowStrTask;
                s.StrIdle += s.NowStrIdle;
                s.StrPool += s.NowStrPool;
                s.TasksWaiting += s.NowWaiting;
                s.Fleets += s.NowFleets;
                s.Kept += s.NowKept;
                s.Cap += s.NowCap;
                s.IdleKept += s.NowIdleKept;
                if (s.NowWars > 0)
                    ++s.TurnsAtWar;
            }
            Started = true;
        }

        void CountShipsGone()
        {
            var gone = new List<Ship>();
            foreach (KeyValuePair<Ship, (EmpireStats Owner, float Strength)> kv in Ships)
            {
                Ship ship = kv.Key;
                EmpireStats owner = kv.Value.Owner;
                if (ship.Active && ship.Loyalty == owner.E)
                    continue;
                gone.Add(ship);
                if (ship.Active)
                {
                    ++owner.Captured;
                    owner.LostStr += kv.Value.Strength;
                }
                else if (DiedInCombat(ship))
                {
                    ++owner.Lost;
                    owner.LostStr += kv.Value.Strength;
                }
            }
            foreach (Ship ship in gone)
            {
                Ships.Remove(ship);
                MissingSections.Remove(ship);
            }
        }

        // the hull sections a ship shows less than whole, lost or coming back, a bit each; the core never goes
        static uint SectionsOff(Ship ship)
        {
            float[] shown = ship.PartShown;
            uint off = 0u;
            if (shown != null)
                for (int part = 1; part < shown.Length && part < 32; ++part)
                    if (shown[part] < 1f)
                        off |= 1u << part;
            return off;
        }

        void CountPlanetChanges()
        {
            foreach (Planet p in U.Planets)
            {
                Empire now = p.Owner;
                PlanetOwners.TryGetValue(p, out Empire before);
                if (now == before)
                    continue;
                PlanetOwners[p] = now;
                if (!Started)
                    continue;
                EmpireStats from = before != null ? StatsOf(before) : null;
                EmpireStats to = now != null ? StatsOf(now) : null;
                if (before == null)
                {
                    if (to != null) ++to.Colonized;
                }
                else if (now == null)
                {
                    if (from != null) ++from.LostOther;
                }
                else
                {
                    if (from != null) ++from.LostTo;
                    if (to != null) ++to.Won;
                }
            }
        }

        void CountWars(int turn)
        {
            for (int i = 0; i < Stats.Length; ++i)
            {
                for (int j = i + 1; j < Stats.Length; ++j)
                {
                    EmpireStats a = Stats[i], b = Stats[j];
                    bool war = !a.E.IsDefeated && !b.E.IsDefeated && a.E.IsAtWarWith(b.E);
                    if (war)
                    {
                        ++a.NowWars;
                        ++b.NowWars;
                        if (AtWar.Add((i, j)) && Started)
                        {
                            ++a.Wars;
                            ++b.Wars;
                            if (FirstWarTurn < 0)
                                FirstWarTurn = turn;
                        }
                    }
                    else
                    {
                        AtWar.Remove((i, j));
                    }
                }
            }
        }

        void ScanShips(EmpireStats s)
        {
            IReadOnlyList<Ship> ships = s.E.OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                if (ship == null || !ship.Active || !IsWarship(ship))
                    continue;
                float str = ship.GetStrength();
                bool known = Ships.ContainsKey(ship);
                Ships[ship] = (s, str);
                ++s.Warships;
                s.Str += str;
                uint off = SectionsOff(ship);
                // a ship new to the empire, captured or recovered, brings the sections it lacks: none of them lost by this empire
                uint offBefore = !known ? off : MissingSections.TryGetValue(ship, out uint had) ? had : 0u;
                s.SectionsLost += System.Numerics.BitOperations.PopCount(off & ~offBefore);
                if (off != 0u)
                {
                    MissingSections[ship] = off;
                    ++s.NowMissingSections;
                }
                else
                {
                    MissingSections.Remove(ship);
                }
                Fleet f = ship.Fleet;
                if (f == null) s.NowStrPool += str;
                else if (f.FleetTask != null) s.NowStrTask += str;
                else s.NowStrIdle += str;
            }
        }

        void ScanFleetsAndTasks(EmpireStats s)
        {
            foreach (Fleet f in s.E.ActiveFleets)
            {
                if (f.Ships.Count == 0)
                    continue;
                ++s.NowFleets;
                if (f.FleetTask != null)
                    ++s.NowFleetsOnTask;
                if (FleetKept != null && (bool)FleetKept.GetValue(f))
                {
                    ++s.NowKept;
                    if (f.FleetTask == null)
                        ++s.NowIdleKept;
                }
            }
            if (KeptFleetCap != null)
                s.NowCap = (int)KeptFleetCap.Invoke(s.E, null);

            IReadOnlyList<MilitaryTask> tasks = s.E.AI.GetTasks();
            for (int i = 0; i < tasks.Count; ++i)
            {
                MilitaryTask task = tasks[i];
                ++s.NowTasks;
                if (task.Fleet == null && FleetSizedTasks.Contains(task.Type.ToString()))
                    ++s.NowWaiting;
            }
        }

        public const string SampleHeader =
            "arm\tgalaxy\tturn\tstardate\twall_s\tempire\talive\tplanets\tpop\tmoney\tincome\tscore\twarships\tstr" +
            "\tstr_task\tstr_idle\tstr_pool\tfleets\tfleets_task\tkept\tcap\tidle_kept\ttasks\ttasks_waiting\twars_now" +
            "\tlost\tlost_str\tsections_lost\tships_missing_sections\tcaptured\twon\tlost_to\tcolonized\tlost_other\twars\tturns_at_war";

        public void WriteSamples(RunSettings run, string galaxy, int turn, float starDate, double wallSeconds)
        {
            foreach (EmpireStats s in Stats)
            {
                Empire e = s.E;
                bool alive = !e.IsDefeated;
                Append(run.PathOf("samples.tsv"), SampleHeader,
                    $"{run.Arm}\t{galaxy}\t{turn}\t{F(starDate)}\t{F(wallSeconds)}\t{e.Name}\t{(alive ? 1 : 0)}" +
                    $"\t{e.NumPlanets}\t{F(e.TotalPopBillion)}\t{F(e.Money)}\t{F(e.NetIncome)}\t{e.TotalScore}" +
                    $"\t{s.Warships}\t{F(s.Str)}\t{F(s.NowStrTask)}\t{F(s.NowStrIdle)}\t{F(s.NowStrPool)}" +
                    $"\t{s.NowFleets}\t{s.NowFleetsOnTask}\t{s.NowKept}\t{s.NowCap}\t{s.NowIdleKept}\t{s.NowTasks}\t{s.NowWaiting}" +
                    $"\t{s.NowWars}\t{s.Lost}\t{F(s.LostStr)}\t{s.SectionsLost}\t{s.NowMissingSections}" +
                    $"\t{s.Captured}\t{s.Won}\t{s.LostTo}\t{s.Colonized}\t{s.LostOther}" +
                    $"\t{s.Wars}\t{s.TurnsAtWar}");
            }
        }

        public const string SummaryHeader =
            "arm\tgalaxy\tstop\tturns\twall_s\terrors\twarnings\tfirst_war\tempire\tdefeated_turn\tplanets\tpop\tmoney\tscore\tstr" +
            "\tlost\tlost_str\tsections_lost\tcaptured\twon\tlost_to\tcolonized\tlost_other\twars\tturns_at_war" +
            "\tshare_task\tshare_idle\tshare_pool\tavg_waiting\tavg_fleets\tavg_kept\tavg_cap\tidle_kept_share";

        public void WriteSummary(RunSettings run, string galaxy, string stop, int turns, double wallSeconds, int errors, int warnings)
        {
            foreach (EmpireStats s in Stats)
            {
                Empire e = s.E;
                double strTurns = s.StrTask + s.StrIdle + s.StrPool;
                double alive = Math.Max(1, s.TurnsAlive);
                Append(run.PathOf("summary.tsv"), SummaryHeader,
                    $"{run.Arm}\t{galaxy}\t{stop}\t{turns}\t{F(wallSeconds)}\t{errors}\t{warnings}\t{FirstWarTurn}\t{e.Name}\t{s.DefeatedTurn}" +
                    $"\t{e.NumPlanets}\t{F(e.TotalPopBillion)}\t{F(e.Money)}\t{e.TotalScore}\t{F(s.Str)}" +
                    $"\t{s.Lost}\t{F(s.LostStr)}\t{s.SectionsLost}\t{s.Captured}\t{s.Won}\t{s.LostTo}\t{s.Colonized}\t{s.LostOther}\t{s.Wars}\t{s.TurnsAtWar}" +
                    $"\t{F(strTurns > 0 ? s.StrTask / strTurns : 0)}\t{F(strTurns > 0 ? s.StrIdle / strTurns : 0)}\t{F(strTurns > 0 ? s.StrPool / strTurns : 0)}" +
                    $"\t{F(s.TasksWaiting / alive)}\t{F(s.Fleets / alive)}\t{F(s.Kept / alive)}\t{F(s.Cap / alive)}" +
                    $"\t{F(s.Kept > 0 ? s.IdleKept / s.Kept : 0)}");
            }
        }
    }
}
