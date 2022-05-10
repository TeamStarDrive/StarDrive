using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.NewGame
{
    /// <summary>
    /// Helper class for creating a New universe with Empires
    /// </summary>
    public class UniverseGenerator
    {
        readonly int NumSystems;
        readonly Array<Vector2> ClaimedSpots = new Array<Vector2>();
        readonly RaceDesignScreen.GameMode Mode;
        readonly Empire Player;
        readonly GameDifficulty Difficulty;
        readonly int NumOpponents;
        UniverseScreen us;
        UniverseState UState;

        public class Params
        {
            public EmpireData PlayerData;
            public RaceDesignScreen.GameMode Mode;
            public GalSize UniverseSize;
            public int NumSystems;
            public int NumOpponents;
            public float StarNumModifier;
            public float Pace;
            public GameDifficulty Difficulty;
        }

        public UniverseGenerator(Params p)
        {
            foreach (Artifact art in ResourceManager.ArtifactsDict.Values)
                art.Discovered = false;

            Difficulty = p.Difficulty;
            Mode = p.Mode;
            NumOpponents = p.NumOpponents;
            NumSystems = p.NumSystems;
            EmpireManager.Clear();
            ResourceManager.LoadEncounters();

            float uSize;
            switch (p.UniverseSize)
            {
                default:
                case GalSize.Tiny: uSize = 2_000_000; break;
                case GalSize.Small: uSize = 4_000_000; break;
                case GalSize.Medium: uSize = 6_000_000; break;
                case GalSize.Large: uSize = 9_000_000; break;
                case GalSize.Huge: uSize = 12_000_000; break;
                case GalSize.Epic: uSize = 15_000_000; break;
                case GalSize.TrulyEpic: uSize = 20_000_000; break;
            }

            us = new UniverseScreen(uSize);
            UState = us.UState;
            UState.FTLModifier = GlobalStats.FTLInSystemModifier;
            UState.EnemyFTLModifier = GlobalStats.EnemyFTLInSystemModifier;
            UState.GravityWells = GlobalStats.PlanetaryGravityWells;
            UState.FTLInNeutralSystems = GlobalStats.WarpInSystem;
            UState.Difficulty = p.Difficulty;
            UState.GalaxySize = p.UniverseSize;
            UState.BackgroundSeed = new Random().Next();

            GlobalStats.DisableInhibitionWarning = UState.Difficulty > GameDifficulty.Hard;
            CurrentGame.StartNew(UState, p.Pace, p.StarNumModifier, GlobalStats.ExtraPlanets, NumOpponents + 1); // +1 is the player empire
            Player = new Empire(UState)
            {
                EmpireColor = p.PlayerData.Traits.Color,
                data = p.PlayerData,
                isPlayer = true,
            };

            UState.AddEmpire(Player); // this binds Player to the universe

            Player.Initialize();
            Player.data.CurrentAutoScout = Player.data.ScoutShip;
            Player.data.CurrentAutoColony = Player.data.ColonyShip;
            Player.data.CurrentAutoFreighter = Player.data.FreighterShip;
            Player.data.CurrentConstructor = Player.data.ConstructorShip;

            StatTracker.Reset();
        }

        public readonly ProgressCounter Progress = new ProgressCounter();

        public TaskResult<UniverseScreen> GenerateAsync()
        {
            return Parallel.Run(Generate);
        }

        /// <summary>
        /// Generates a new UniverseScreen with UniverseState.
        ///
        /// After completion you have to LoadContent via
        /// ScreenManager.AddScreenAndLoadContent(us)
        /// or manually via us.LoadContent()
        /// </summary>
        public UniverseScreen Generate()
        {
            Progress.Start(0.65f, 0.35f);
            GenerateInitialSystemData(Progress.NextStep());
            FinalizeSolarSystems();
            FinalizeEmpires(Progress.NextStep());
            Progress.Finish();

            Planet homePlanet = Player.GetPlanets()[0];
            us.CamPos = new Vector3d(homePlanet.Center.X, homePlanet.Center.Y, 5000);

            Log.Info(ConsoleColor.Blue, $"  GenerateInitialSystemData elapsed: {Progress[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"  FinalizeEmpires           elapsed: {Progress[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.DarkRed, $"TOTAL GenerateSystems       elapsed: {Progress.ElapsedMillis}ms");
            return us;
        }

        void FinalizeEmpires(ProgressCounter step)
        {
            step.Start(UState.Empires.Count);
            foreach (Empire empire in UState.Empires)
            {
                step.Advance();
                if (empire.isFaction)
                    continue;

                IReadOnlyList<Planet> planets = empire.GetPlanets();
                for (int planetId = 0; planetId < planets.Count; planetId++)
                {
                    Planet planet = planets[planetId];
                    planet.MineralRichness += GlobalStats.StartingPlanetRichness;
                    planet.ParentSystem.Explorable.SetExploredBy(empire);
                    planet.SetExploredBy(empire);

                    foreach (Planet p in planet.ParentSystem.PlanetList)
                    {
                        p.SetExploredBy(empire);
                    }

                    if (planet.ParentSystem.OwnerList.Count == 0)
                    {
                        planet.ParentSystem.OwnerList.Add(empire);
                        foreach (Planet planet2 in planet.ParentSystem.PlanetList)
                            planet2.SetExploredBy(empire);
                    }
                }
            }

            foreach (Empire e in UState.Empires)
            {
                if (e.isFaction)
                    continue;

                e.InitFleetEmpireStrMultiplier();
                Planet homeWorld = e.GetPlanets()[0];
                SolarSystem[] closestSystems = UState.Systems.Sorted(system => homeWorld.Center.SqDist(system.Position));

                // Home system is always set to be explored
                int numExplored = (e.data.Traits.BonusExplored + 1).UpperBound(UState.Systems.Count);

                for (int i = 0; i < numExplored; ++i)
                {
                    SolarSystem ss = closestSystems[i];
                    ss.Explorable.SetExploredBy(e);
                    foreach (Planet planet in ss.PlanetList)
                        planet.SetExploredBy(e);

                    ss.UpdateFullyExploredBy(e);
                }
            }

            EmpireHullBonuses.RefreshBonuses();
        }

        void FinalizeSolarSystems()
        {
            // once the map is generated, update all planet positions:
            foreach (SolarSystem system in UState.Systems)
            {
                system.FiveClosestSystems = UState.GetFiveClosestSystems(system);
                foreach (Planet planet in system.PlanetList)
                {
                    planet.UpdatePositionOnly();
                }
            }
        }

        void GenerateInitialSystemData(ProgressCounter step)
        {
            // expected times of each step
            step.StartAbsolute(0.2f, 0.04f, 0.42f, 0.425f);

            CreateOpponents(step.NextStep());
            Empire.InitializeRelationships(UState.Empires, Difficulty);
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep()); // 40ms
            LoadEmpireStartingSystems(step.NextStep()); // 420ms
            GenerateRandomSystems(step.NextStep());    // 425ms

            switch (Mode)
            {
                case RaceDesignScreen.GameMode.Corners: GenerateCornersGameMode(); break;
                case RaceDesignScreen.GameMode.BigClusters: GenerateBigClusters(); break;
                case RaceDesignScreen.GameMode.SmallClusters: GenerateSmallClusters(); break;
                default: GenerateRandomMap(); break;
            }

            step.Finish();
            Log.Info(ConsoleColor.Blue, $"    ## CreateOpponents           elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## LoadEmpireStartingSystems elapsed: {step[2].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## GenerateRandomSystems     elapsed: {step[3].ElapsedMillis}ms");
        }

        void CreateOpponents(ProgressCounter step)
        {
            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != Player.data.ArchetypeName);

            // create a randomly shuffled list of opponents
            var opponents = new Array<IEmpireData>(majorRaces);
            opponents.Shuffle();
            opponents.Resize(Math.Min(opponents.Count, NumOpponents)); // truncate

            step.Start(opponents.Count + ResourceManager.MinorRaces.Count);

            foreach (IEmpireData readOnlyData in opponents)
            {
                Empire e = UState.CreateEmpire(readOnlyData, isPlayer: false);
                RacialTrait t = e.data.Traits;

                e.data.FlatMoneyBonus += e.DifficultyModifiers.FlatMoneyBonus;
                t.ProductionMod += e.DifficultyModifiers.ProductionMod;
                t.ResearchMod += e.DifficultyModifiers.ResearchMod;
                t.TaxMod += e.DifficultyModifiers.TaxMod;
                t.ModHpModifier += e.DifficultyModifiers.ModHpModifier;
                t.ShipCostMod += e.DifficultyModifiers.ShipCostMod;
                t.ResearchTaxMultiplier = e.DifficultyModifiers.ResearchTaxMultiplier; // the "=" here is intended

                step.Advance();
            }

            foreach (IEmpireData readOnlyData in ResourceManager.MinorRaces)
            {
                UState.CreateEmpire(readOnlyData, isPlayer: false);
                step.Advance();
            }
        }

        void LoadEmpireStartingSystems(ProgressCounter step)
        {
            step.Start(UState.Empires.Count);
            foreach (Empire e in UState.Empires)
            {
                step.Advance();
                if (e.isFaction)
                    continue;

                SolarSystem sys;
                SolarSystemData systemData = ResourceManager.LoadSolarSystemData(e.data.Traits.HomeSystemName);
                if (systemData == null)
                {
                    sys = new SolarSystem(UState);
                    sys.GenerateStartingSystem(UState, e.data.Traits.HomeSystemName, 1f, e);
                }
                else
                {
                    sys = SolarSystem.GenerateSystemFromData(UState, systemData, e);
                }

                if (e.GetOwnedSystems().Count == 0)
                {
                    Log.Error($"Failed to create starting system for {e}");
                }

                UState.AddSolarSystem(sys);
            }
        }

        void GenerateRandomSystems(ProgressCounter step)
        {
            step.Start(NumSystems);
            int systemCount = 0;
            foreach (SolarSystemData systemData in ResourceManager.LoadRandomSolarSystems())
            {
                if (systemCount > NumSystems)
                    break;
                var solarSystem = SolarSystem.GenerateSystemFromData(UState, systemData, null);
                solarSystem.DontStartNearPlayer = true; // Added by Gretman
                UState.AddSolarSystem(solarSystem);
                systemCount++;
                step.Advance();
            }

            var nameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
            for (; systemCount < NumSystems; ++systemCount)
            {
                var solarSystem2 = new SolarSystem(UState);
                solarSystem2.GenerateRandomSystem(UState, nameGenerator.NextName, 1f);
                UState.AddSolarSystem(solarSystem2);
                step.Advance();
            }
        }

        void SolarSystemSpacing(IReadOnlyList<SolarSystem> solarSystems)
        {
            foreach (SolarSystem solarSystem2 in solarSystems)
            {
                float spacing = 350000f;
                if (solarSystem2.IsStartingSystem)
                    continue; // We created starting systems before

                if (solarSystem2.DontStartNearPlayer)
                    spacing = UState.Size / (2f - 1f / (UState.Empires.Count - 1));

                solarSystem2.Position = GenerateRandomSysPos(spacing);
            }
        }

        void GenerateCornersGameMode()
        {
            short whichCorner = StartingPositionCorners();

            foreach (SolarSystem system in UState.Systems)
            {
                // This will distribute all the rest of the planets evenly
                if (!system.IsStartingSystem && !system.DontStartNearPlayer)
                {
                    system.Position = GenerateRandomCorners(whichCorner);
                    whichCorner += 1;   // Only change which corner if a system is actually created
                    if (whichCorner > 3)
                        whichCorner = 0;
                }
            }
        }

        short StartingPositionCorners()
        {
            float universeSize = UState.Size;
            short whichcorner = (short)RandomMath.Float(0, 4); //So the player doesnt always end up in the same corner;
            foreach (SolarSystem solarSystem2 in UState.Systems)
            {
                if (solarSystem2.IsStartingSystem || solarSystem2.DontStartNearPlayer)
                {
                    if (solarSystem2.IsStartingSystem)
                    {
                        //Corner Values
                        //0 = Top Left
                        //1 = Top Right
                        //2 = Bottom Left
                        //3 = Bottom Right

                        //Put the 4 Home Planets into their corners, nessled nicely back a bit
                        float RandomoffsetX =
                            RandomMath.Float(0, 19) / 100; //Do want some variance in location, but still in the back
                        float RandomoffsetY = RandomMath.Float(0, 19) / 100;
                        float MinOffset = 0.04f; //Minimum Offset
                        //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                        float CornerOffset = 0.75f; //Additional Offset for being in corner
                        //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                        switch (whichcorner)
                        {
                            case 0:
                                solarSystem2.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 1:
                                solarSystem2.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 2:
                                solarSystem2.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 3:
                                solarSystem2.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                        }
                    }
                    else
                        solarSystem2.Position =
                            GenerateRandomCorners(
                                whichcorner); //This will distribute the extra planets from "/SolarSystems/Random" evenly

                    whichcorner += 1;
                    if (whichcorner > 3) whichcorner = 0;
                }
            }

            return whichcorner;
        }

        Vector2 GenerateRandomSysPos(float spacing)
        {
            float safetyBreak = 1f;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = RandomMath.Vector2D(UState.Size - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        void GenerateRandomMap()
        {
            // FB - we are using the sector creation only for starting systems here. the rest will be created randomly
            (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors((NumOpponents + 1).LowerBound(9));
            Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.1f);
            GenerateClustersStartingSystems(sectors);
            SolarSystemSpacing(UState.Systems);
        }

        void GenerateBigClusters()
        {
            // Divides the galaxy to several sectors and populates each sector with stars
            (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors(NumOpponents + 1);
            Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.25f);
            GenerateClustersStartingSystems(sectors);
            GenerateClusterSystems(sectors);
        }

        void GenerateSmallClusters()
        {
            // Divides the galaxy to many sectors and populates each sector with stars
            int numSectorsPerAxis = GetNumSectorsPerAxis(NumSystems, NumOpponents + 1);
            float offsetMultiplier = 0.28f / numSectorsPerAxis.UpperBound(4);
            float deviation = 0.05f * numSectorsPerAxis.UpperBound(4);
            Array<Sector> sectors = GenerateSectors(numSectorsPerAxis, numSectorsPerAxis, deviation, offsetMultiplier);
            GenerateClustersStartingSystems(sectors, numSectorsPerAxis - 1);
            GenerateClusterSystems(sectors);
        }

        (int NumHorizontalSectors, int NumVerticalSectors) GetNumSectors(int numEmpires)
        {
            int numHorizontalSectors = 2;
            int numVerticalSectors = 2;

            if (numEmpires > 9) // 4x4 sectors - probably not applicable (limited empires to 8 by default)
            {
                numHorizontalSectors = 4;
                numVerticalSectors = 4;
            }
            else if (numEmpires > 6) // 3x3 sectors
            {
                numHorizontalSectors = 3;
                numVerticalSectors = 3;
            }
            else if (numEmpires > 4) // 3x2 sectors
            {
                numHorizontalSectors = 3;
            }

            return (NumHorizontalSectors: numHorizontalSectors, NumVerticalSectors: numVerticalSectors);
        }

        // This will divide number of stars by number of empires to get the number of wanted sectors.
        // Then it will use square root to get the number of sector per axis
        int GetNumSectorsPerAxis(int numSystems, int numEmpires)
        {
            int numSectors = numSystems / numEmpires.LowerBound(4); // each sector will have stars as ~player num, minimum of 4
            int numSectorsPerAxis = (int)Math.Sqrt(numSectors) + 1;

            return numSectorsPerAxis.LowerBound(numEmpires / 2);
        }

        Array<Sector> GenerateSectors(int numHorizontalSectors, int numVerticalSectors, float deviation, float offsetMultiplier = 0.1f)
        {
            Array<Sector> sectors = new Array<Sector>();
            for (int h = 1; h <= numHorizontalSectors; ++h)
            {
                for (int v = 1; v <= numVerticalSectors; ++v)
                {
                    Sector sector = new Sector(UState.Size, numHorizontalSectors, numVerticalSectors,
                        h, v, deviation, offsetMultiplier);

                    sectors.Add(sector);
                }
            }

            return sectors;
        }

        void GenerateClustersStartingSystems(Array<Sector> sectors, int trySpacingNum = 1)
        {
            Array<Sector> claimedSectors = new Array<Sector>();
            var startingSystems = UState.Systems.Filter(s => s.IsStartingSystem);
            if (sectors.Count < startingSystems.Length)
                Log.Error($"Sectors ({sectors.Count}) < starting Systems ({startingSystems.Length})");

            SolarSystem firstSystem = startingSystems[0];
            Sector initialSector = sectors.RandItem();
            firstSystem.Position = GenerateSystemInCluster(initialSector, 350000f);
            claimedSectors.Add(initialSector);

            for (int i = 1; i < startingSystems.Length; i++) // starting with 2nd (i = 1) item since the first one was added above
            {
                SolarSystem system = startingSystems[i];
                var remainingSectors = sectors.Filter(s => !claimedSectors.Contains(s));
                int spacing = trySpacingNum;
                var potentialSectors = remainingSectors.Filter(s => IsSuitableSector(s, claimedSectors, spacing));

                while (potentialSectors.Length == 0)
                {
                    spacing--;
                    if (spacing < 0)
                        Log.Error("GenerateClustersStartingSystems: Could not find suitable sectors to add starting system");

                    potentialSectors = remainingSectors.Filter(s => IsSuitableSector(s, claimedSectors, spacing));
                }

                Sector nextSector = potentialSectors.RandItem();
                system.Position = GenerateSystemInCluster(nextSector, 350000f);
                claimedSectors.Add(nextSector);
            }

            // Local Method
            bool SpaceBetweenMoreThan(int space, Sector a, Sector b)
            {
                return Math.Abs(a.X - b.X) > space || Math.Abs(a.Y - b.Y) > space;
            }

            // Local Method
            bool IsSuitableSector(Sector sector, Array<Sector> list, int space)
            {
                foreach (Sector s in list)
                {
                    if (!SpaceBetweenMoreThan(space, sector, s))
                        return false;
                }

                return true;
            }
        }

        void GenerateClusterSystems(Array<Sector> sectors)
        {
            int i = 0;
            foreach (SolarSystem system in UState.Systems.Filter(s => !s.IsStartingSystem)
                     .SortedDescending(s => s.AverageValueForEmpires(UState.Empires)))
            {
                Sector currentSector = sectors[i]; // distribute systems evenly per sector, based on value
                system.Position = GenerateSystemInCluster(currentSector, 300000f);
                i = i < sectors.Count - 1 ? i + 1 : 0; // always cycle within the array
            }
        }

        Vector2 GenerateSystemInCluster(Sector sector, float spacing)
        {
            float safetyBreak = 1;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = sector.RandomPosInSector;
                safetyBreak *= 0.99f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        struct Sector
        {
            private readonly float RightX;
            private readonly Vector2 Center;
            public readonly int X;
            public readonly int Y;

            public Sector(float universeSize, int horizontalSectors, int verticalSectors, int horizontalNum, int verticalNum,
                          float deviation, float offsetMultiplier) : this()
            {
                X = horizontalNum;
                Y = verticalNum;
                float xSection = universeSize / horizontalSectors;
                float ySection = universeSize / verticalSectors;
                float offset = universeSize * offsetMultiplier;

                // raw center is the center of the sector before generating offset (for gaps)
                Vector2 rawCenter = new Vector2(-universeSize + xSection * (-1 + horizontalNum * 2),
                                             -universeSize + ySection * (-1 + verticalNum * 2));

                // Some deviation in the center of the cluster
                rawCenter = rawCenter.GenerateRandomPointInsideCircle(universeSize * deviation);

                float leftX = (rawCenter.X - xSection).LowerBound(-universeSize);
                RightX = (rawCenter.X + xSection).UpperBound(universeSize);
                float topY = (rawCenter.Y - ySection).LowerBound(-universeSize) + offset;
                float botY = (rawCenter.Y + ySection).UpperBound(universeSize) - offset;

                // creating some gaps between clusters
                GenerateOffset(universeSize, offset, ref leftX, ref RightX);
                GenerateOffset(universeSize, offset, ref topY, ref botY);

                // This is the true Center, after all offsets are applied with borders
                Center = new Vector2((leftX + RightX) / 2, (topY + botY) / 2);
            }

            // Offset from borders. Less offset if near one or 2 edges
            void GenerateOffset(float size, float offset, ref float leftOrTop, ref float rightOrBot)
            {
                if (leftOrTop.AlmostEqual(-size))
                {
                    leftOrTop += offset * 0.1f;
                    rightOrBot -= offset * 1.9f;
                }
                else if (rightOrBot.AlmostEqual(size))
                {
                    leftOrTop += offset * 1.9f;
                    rightOrBot -= offset * 0.1f;
                }
                else
                {
                    leftOrTop += offset;
                    rightOrBot -= offset;
                }
            }

            public Vector2 RandomPosInSector => Center.GenerateRandomPointInsideCircle(RightX - Center.X);
        }

        Vector2 GenerateRandomCorners(short corner) //Added by Gretman for Corners Game type
        {
            //Corner Values
            //0 = Top Left
            //1 = Top Right
            //2 = Bottom Left
            //3 = Bottom Right
            float uSize = UState.Size;
            float SizeX = uSize * 2;     //Allow for new negative coordinates
            float SizeY = uSize * 2;

            double CornerSizeX = SizeX * 0.4;    //20% of map per corner
            double CornerSizeY = SizeY * 0.4;

            double offsetX = 100000;
            double offsetY = 100000;
            if (corner == 1 || corner == 3)
                offsetX = SizeX * 0.6 - 100000;    //This creates a Huge blank "Neutral Zone" between corner areas
            if (corner == 2 || corner == 3)
                offsetY = SizeY * 0.6 - 100000;

            Vector2 sysPos;
            long noinfiniteloop = 0;
            do
            {
                sysPos = new Vector2(RandomMath.Float(-uSize + (float)offsetX, -uSize + (float)(CornerSizeX + offsetX)),
                                        RandomMath.Float(-uSize + (float)offsetY, -uSize + (float)(CornerSizeY + offsetY)));
                noinfiniteloop += 1000;
            }
            //Decrease the acceptable proximity slightly each attempt, so there wont be an infinite loop here on 'tiny' + 'SuperPacked' maps
            while (!SystemPosOK(sysPos, 400000 - noinfiniteloop));
            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            float uSize = UState.Size;
            float num1 = (float)(2f / numOfStars * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow(uSize - 0.0850000008940697 * uSize, index / (float)numOfStars);
                float num3 = index * num1 + rotation;
                float x = RadMath.Cos(num3) * num2;
                float y = RadMath.Sin(num3) * num2;
                Vector2 sysPos = new Vector2(RandomMath.Float(-10000f, 10000f) * index, (float)(RandomMath.Float(-10000f, 10000f) * (double)index / 4.0));
                sysPos = new Vector2(x, y) + sysPos;
                if (SystemPosOK(sysPos))
                {
                    ClaimedSpots.Add(sysPos);
                }
                else
                {
                    double halfSize = uSize / 2.0;
                    // extra padding to avoid suns existing at the edge of the universe
                    double padding = 0.085 * uSize;
                    float min = (float)(-halfSize + padding);
                    float max = (float)(+halfSize - padding);
                    while (!SystemPosOK(sysPos))
                    {
                        sysPos.X = RandomMath.Float(min, max);
                        sysPos.Y = RandomMath.Float(min, max);
                    }

                    ClaimedSpots.Add(sysPos);
                }
            }
        }

        bool IsInUniverseBounds(Vector2 sysPos)
        {
            float uSize = UState.Size;
            return -uSize < sysPos.X && sysPos.X < uSize
                && -uSize < sysPos.Y && sysPos.Y < uSize;
        }

        bool SystemPosOK(Vector2 sysPos)
        {
            return SystemPosOK(sysPos, 300000f);
        }

        bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            if (!IsInUniverseBounds(sysPos))
                return false;

            for (int i = 0; i < ClaimedSpots.Count; ++i)
            {
                Vector2 claimed = ClaimedSpots[i];
                if (sysPos.InRadius(claimed, spacing))
                    return false;
            }
            return true;
        }

    }
}
