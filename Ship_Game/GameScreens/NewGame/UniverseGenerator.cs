using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Utils;
using System;
using System.Collections.Generic;
using Vector2 = SDGraphics.Vector2;

#pragma warning disable CA1001

namespace Ship_Game.GameScreens.NewGame
{
    /// <summary>
    /// Helper class for creating a New universe with Empires
    /// </summary>
    public sealed class UniverseGenerator
    {
        private readonly int NumSystems;
        private readonly Array<Vector2> ClaimedSpots = new();
        private readonly RaceDesignScreen.GameMode Mode;
        private readonly GameDifficulty Difficulty;
        private readonly int NumOpponents;

        private readonly Empire Player;
        private readonly Array<IEmpireData> FoeList;
        private readonly UniverseScreen us;
        private readonly UniverseState UState;

        readonly Array<SystemPlaceHolder> Systems = new();

        public readonly RandomBase Random;

        public UniverseGenerator(UniverseParams p)
        {
            // TODO: allow players to enter their own universe seed
            Random = new SeededRandom();

            foreach (Artifact art in ResourceManager.ArtifactsDict.Values)
                art.Discovered = false;

            Difficulty = p.Difficulty;
            Mode = p.Mode;
            NumOpponents = p.NumOpponents;
            NumSystems = p.NumSystems;
            FoeList = p.SelectedFoes;
            ResourceManager.LoadEncounters();

            float uSize;
            switch (p.GalaxySize)
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


            us = new UniverseScreen(p, uSize);
            UState = us.UState;
            UState.BackgroundSeed = new Random().Next();

            UState.P.DisableInhibitionWarning = p.Difficulty > GameDifficulty.Hard;

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
            Player.data.CurrentResearchStation = Player.data.ResearchStation;
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
            FinalizeEmpires(Progress.NextStep());
            Progress.Finish();

            Planet homePlanet = Player.GetPlanets()[0];
            us.CamPos = new Vector3d(homePlanet.Position.X, homePlanet.Position.Y, 5000);

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
                if (empire.IsFaction)
                    continue;

                IReadOnlyList<Planet> planets = empire.GetPlanets();
                for (int planetId = 0; planetId < planets.Count; planetId++)
                {
                    Planet planet = planets[planetId];
                    planet.MineralRichness += UState.P.StartingPlanetRichnessBonus;
                    planet.System.SetExploredBy(empire);
                    planet.SetExploredBy(empire);

                    foreach (Planet p in planet.System.PlanetList)
                    {
                        p.SetExploredBy(empire);
                    }

                    if (planet.System.OwnerList.Count == 0)
                    {
                        planet.System.OwnerList.Add(empire);
                        foreach (Planet planet2 in planet.System.PlanetList)
                            planet2.SetExploredBy(empire);
                    }
                }
            }

            foreach (Empire e in UState.Empires)
            {
                e.InitFleetEmpireStrMultiplier();
                if (e.IsFaction)
                    continue;

                Planet homeWorld = e.GetPlanets()[0];
                SolarSystem[] closestSystems = UState.Systems.Sorted(system => homeWorld.Position.SqDist(system.Position));

                // Home system is always set to be explored
                int numExplored = (e.data.Traits.BonusExplored + 1).UpperBound(UState.Systems.Count);

                for (int i = 0; i < numExplored; ++i)
                {
                    SolarSystem ss = closestSystems[i];
                    ss.SetExploredBy(e);
                    foreach (Planet planet in ss.PlanetList)
                        planet.SetExploredBy(e);

                    ss.UpdateFullyExploredBy(e);
                }
            }

            EmpireHullBonuses.RefreshBonuses(UState);
        }

        class SystemPlaceHolder
        {
            public string SystemName;
            public SolarSystemData Data;
            public Empire Owner;
            public Vector2 Position;
            public bool DontStartNearPlayer;
            public bool IsStartingSystem => Owner != null;
        }

        void GenerateInitialSystemData(ProgressCounter step)
        {
            // expected times of each step
            step.StartAbsolute(0.228f, 0.007f, 0.043f, 0.008f, 0.376f);

            CreateOpponents(step.NextStep()); // 228ms
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep()); // 7ms
            CreateSystemPlaceHolders(step.NextStep()); // 43ms
            CreateSystemPositions(step.NextStep()); // 8ms
            GenerateSystems(step.NextStep()); // 376ms

            step.Finish();
            Log.Info(ConsoleColor.Blue, $"    ## CreateOpponents           elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## CreateSystemPlaceHolders  elapsed: {step[2].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## CreateSystemPositions     elapsed: {step[3].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## GenerateSystems           elapsed: {step[4].ElapsedMillis}ms");
        }

        void CreateOpponents(ProgressCounter step)
        {
            //IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
            //                    data => data.ArchetypeName != Player.data.ArchetypeName);

            // create a randomly shuffled list of opponents
            //var opponents = new Array<IEmpireData>(majorRaces);
            //opponents.Shuffle();
            //opponents.Resize(Math.Min(opponents.Count, NumOpponents)); // truncate

            step.Start(FoeList.Count + ResourceManager.MinorRaces.Count);

            foreach (IEmpireData readOnlyData in FoeList)
            {
                Empire e = UState.CreateEmpire(readOnlyData, isPlayer: false, difficulty: Difficulty);
                RacialTrait t = e.data.Traits;
                e.data.FlatMoneyBonus += e.DifficultyModifiers.FlatMoneyBonus;
                t.ShipCostMod += e.DifficultyModifiers.ShipCostMod;

                if (e.DifficultyModifiers.ProductionMod.NotZero())
                    t.ProductionMod = (1 + t.ProductionMod) * e.DifficultyModifiers.ProductionMod;
                if (e.DifficultyModifiers.ResearchMod.NotZero())
                    t.ResearchMod = (1 + t.ResearchMod) * e.DifficultyModifiers.ResearchMod;
                if (e.DifficultyModifiers.TaxMod.NotZero())
                    t.TaxMod = (1 + t.TaxMod) * e.DifficultyModifiers.TaxMod;

                t.ModHpModifier += e.DifficultyModifiers.ModHpModifier;

                t.ResearchTaxMultiplier = e.DifficultyModifiers.ResearchTaxMultiplier; // the "=" here is intended

                step.Advance();
            }

            foreach (IEmpireData readOnlyData in ResourceManager.MinorRaces)
            {
                UState.CreateEmpire(readOnlyData, isPlayer: false, difficulty: Difficulty);
                step.Advance();
            }

            UState.CalcInitialSettings();
        }

        void CreateSystemPlaceHolders(ProgressCounter step)
        {
            Empire[] majorEmpires = UState.Empires.Filter(e => !e.IsFaction);
            
            step.Start(NumSystems + majorEmpires.Length);

            foreach (Empire e in majorEmpires)
            {
                Systems.Add(new SystemPlaceHolder
                {
                    Owner = e,
                    Data = ResourceManager.LoadSolarSystemData(e.data.Traits.HomeSystemName), // SystemData can be null
                    SystemName = e.data.Traits.HomeSystemName,
                });
                step.Advance();
            }

            int systemCount = 0;
            foreach (SolarSystemData systemData in ResourceManager.LoadRandomSolarSystems())
            {
                if (systemCount > NumSystems)
                    break;
                ++systemCount;
                Systems.Add(new SystemPlaceHolder { DontStartNearPlayer = true, Data = systemData });
                step.Advance();
            }

            if (systemCount < NumSystems)
            {
                var nameGenerator = ResourceManager.GetNameGenerator("NameGenerators/names.txt");
                for (; systemCount < NumSystems; ++systemCount)
                {
                    Systems.Add(new SystemPlaceHolder { SystemName = nameGenerator.NextName });
                    step.Advance();
                }
            }
        }

        void CreateSystemPositions(ProgressCounter step)
        {
            step.Start(Systems.Count);
            switch (Mode)
            {
                case RaceDesignScreen.GameMode.Corners:       GenerateCornersGameMode(step);  break;
                case RaceDesignScreen.GameMode.BigClusters:   GenerateBigClusters(step);      break;
                case RaceDesignScreen.GameMode.SmallClusters: GenerateSmallClusters(step);    break;
                case RaceDesignScreen.GameMode.Ring:          GenerateRingMap(step);          break;
                case RaceDesignScreen.GameMode.Sandbox:       GenerateRandomMap(step, false); break;
                default:                                      GenerateRandomMap(step, true);  break;
            }
        }

        void GenerateSystems(ProgressCounter step)
        {
            step.Start(Systems.Count);
            float exoticPlanetMultiplier = (100f / Systems.Count).UpperBound(1);
            foreach (SystemPlaceHolder placeHolder in Systems)
            {
                Empire e = placeHolder.Owner;
                var sys = new SolarSystem(UState, placeHolder.Position);

                if (placeHolder.Data != null)
                    sys.GenerateFromData(UState, Random, placeHolder.Data, e, exoticPlanetMultiplier);
                else
                    sys.GenerateRandomSystem(UState, Random, placeHolder.SystemName, e, exoticPlanetMultiplier);

                if (e != null && e.GetOwnedSystems().Count == 0)
                {
                    Log.Error($"Failed to create starting system for {e}");
                }

                UState.AddSolarSystem(sys);
                step.Advance();
            }

            // once all systems are generated, init FiveClosestSystems for all
            foreach (SolarSystem system in UState.Systems)
            {
                system.FiveClosestSystems = UState.GetFiveClosestSystems(system);
            }

            UState.MineablePlanets.Sort(p => -p.Mining.Richness);

            step.Finish();
        }

        void SolarSystemSpacingRing(ProgressCounter step)
        {
            foreach (SystemPlaceHolder sys in Systems)
            {
                float spacing = 350000f;
                if (sys.DontStartNearPlayer)
                    spacing = UState.Size / (2f - 1f / (UState.Empires.Count - 1));

                sys.Position = GenerateRandomSysPosInRing(spacing);
                step.Advance();
            }
        }

        void SolarSystemSpacing(ProgressCounter step, bool randomStartingPos)
        {
            foreach (SystemPlaceHolder sys in Systems)
            {
                float spacing = 350000f;
                if (sys.IsStartingSystem && !randomStartingPos)
                    continue; // We created starting systems before

                if (sys.DontStartNearPlayer)
                    spacing = UState.Size / (2f - 1f / (UState.Empires.Count - 1));

                sys.Position = GenerateRandomSysPos(spacing);
                step.Advance();
            }
        }

        void GenerateCornersGameMode(ProgressCounter step)
        {
            int whichCorner = StartingPositionCorners(step);

            foreach (SystemPlaceHolder sys in Systems)
            {
                // This will distribute all the rest of the planets evenly
                if (!sys.IsStartingSystem && !sys.DontStartNearPlayer)
                {
                    sys.Position = GenerateRandomCorners(whichCorner);
                    step.Advance();
                    NextCorner(ref whichCorner);
                }
            }
        }

        static void NextCorner(ref int whichCorner)
        {
            if (++whichCorner > 3)
                whichCorner = 0;
        }

        int StartingPositionCorners(ProgressCounter step)
        {
            float universeSize = UState.Size;
            int whichCorner = Random.Int(0, 3); //So the player doesnt always end up in the same corner;
            foreach (SystemPlaceHolder sys in Systems)
            {
                if (sys.IsStartingSystem || sys.DontStartNearPlayer)
                {
                    if (sys.IsStartingSystem)
                    {
                        //Corner Values
                        //0 = Top Left
                        //1 = Top Right
                        //2 = Bottom Left
                        //3 = Bottom Right

                        //Put the 4 Home Planets into their corners, nessled nicely back a bit
                        float RandomoffsetX = Random.Float(0, 19) / 100; //Do want some variance in location, but still in the back
                        float RandomoffsetY = Random.Float(0, 19) / 100;
                        float MinOffset = 0.04f; //Minimum Offset
                        //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                        float CornerOffset = 0.75f; //Additional Offset for being in corner
                        //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                        switch (whichCorner)
                        {
                            case 0:
                                sys.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 1:
                                sys.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 2:
                                sys.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 3:
                                sys.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            default: throw new IndexOutOfRangeException(nameof(whichCorner));
                        }
                    }
                    else
                    {
                        //This will distribute the extra planets from "/SolarSystems/Random" evenly
                        sys.Position = GenerateRandomCorners(whichCorner);
                    }
                    step.Advance();
                    NextCorner(ref whichCorner);
                }
            }

            return whichCorner;
        }

        Vector2 GenerateRandomSysPosInRing(float spacing)
        {
            float safetyBreak = 1f;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = Random.RandomPointInRing(UState.Size * 0.75f, UState.Size - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        Vector2 GenerateRandomSysPos(float spacing)
        {
            float safetyBreak = 1f;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = Random.Vector2D(UState.Size - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        void GenerateRandomMap(ProgressCounter step, bool randomStartingPos)
        {
            if (!randomStartingPos)
            {
                // FB - we are using the sector creation only for starting systems here. the rest will be created randomly
                (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors((NumOpponents + 1).LowerBound(9));
                Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.1f);
                GenerateClustersStartingSystems(step, sectors);
            }

            SolarSystemSpacing(step, randomStartingPos);
        }

        void GenerateRingMap(ProgressCounter step)
        {
            SolarSystemSpacingRing(step);
        }

        void GenerateBigClusters(ProgressCounter step)
        {
            // Divides the galaxy to several sectors and populates each sector with stars
            (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors(NumOpponents + 1);
            Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.25f);
            GenerateClustersStartingSystems(step, sectors);
            GenerateClusterSystems(step, sectors);
        }

        void GenerateSmallClusters(ProgressCounter step)
        {
            // Divides the galaxy to many sectors and populates each sector with stars
            int numSectorsPerAxis = GetNumSectorsPerAxis(NumSystems, NumOpponents + 1);
            float offsetMultiplier = 0.28f / numSectorsPerAxis.UpperBound(4);
            float deviation = 0.05f * numSectorsPerAxis.UpperBound(4);
            Array<Sector> sectors = GenerateSectors(numSectorsPerAxis, numSectorsPerAxis, deviation, offsetMultiplier);
            GenerateClustersStartingSystems(step, sectors, numSectorsPerAxis - 1);
            GenerateClusterSystems(step, sectors);
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
            var sectors = new Array<Sector>();
            for (int h = 1; h <= numHorizontalSectors; ++h)
            {
                for (int v = 1; v <= numVerticalSectors; ++v)
                {
                    sectors.Add(new Sector(Random, UState.Size, numHorizontalSectors, numVerticalSectors,
                                           h, v, deviation, offsetMultiplier));
                }
            }

            return sectors;
        }

        void GenerateClustersStartingSystems(ProgressCounter step, Array<Sector> sectors, int trySpacingNum = 1)
        {
            var claimedSectors = new Array<Sector>();
            var startingSystems = Systems.Filter(s => s.IsStartingSystem);
            if (sectors.Count < startingSystems.Length)
                Log.Error($"Sectors ({sectors.Count}) < starting Systems ({startingSystems.Length})");

            SystemPlaceHolder firstSystem = startingSystems[0];
            Sector initialSector = Random.Item(sectors);
            firstSystem.Position = GenerateSystemInCluster(initialSector, 350000f);
            step.Advance();
            claimedSectors.Add(initialSector);

            for (int i = 1; i < startingSystems.Length; i++) // starting with 2nd (i = 1) item since the first one was added above
            {
                SystemPlaceHolder system = startingSystems[i];
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

                Sector nextSector = Random.Item(potentialSectors);
                system.Position = GenerateSystemInCluster(nextSector, 350000f);
                step.Advance();
                claimedSectors.Add(nextSector);
            }

            bool SpaceBetweenMoreThan(int space, Sector a, Sector b)
            {
                return Math.Abs(a.X - b.X) > space || Math.Abs(a.Y - b.Y) > space;
            }

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

        void GenerateClusterSystems(ProgressCounter step, Array<Sector> sectors)
        {
            int i = 0;
            foreach (SystemPlaceHolder sys in Systems.Filter(s => !s.IsStartingSystem))
            {
                Sector currentSector = sectors[i];
                sys.Position = GenerateSystemInCluster(currentSector, 300000f);
                step.Advance();
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
                sysPos = sector.GetRandomPosInSector(Random);
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

            public Sector(RandomBase random, float universeSize, 
                          int horizontalSectors, int verticalSectors, int horizontalNum, int verticalNum,
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
                rawCenter = rawCenter.GenerateRandomPointInsideCircle(universeSize * deviation, random);

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

            public Vector2 GetRandomPosInSector(RandomBase random) => Center.GenerateRandomPointInsideCircle(RightX - Center.X, random);
        }

        Vector2 GenerateRandomCorners(int corner) //Added by Gretman for Corners Game type
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
                sysPos = new Vector2(Random.Float(-uSize + (float)offsetX, -uSize + (float)(CornerSizeX + offsetX)),
                                     Random.Float(-uSize + (float)offsetY, -uSize + (float)(CornerSizeY + offsetY)));
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
                Vector2 sysPos = new Vector2(Random.Float(-10000f, 10000f) * index,
                                     (float)(Random.Float(-10000f, 10000f) * (double)index / 4.0));
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
                        sysPos.X = Random.Float(min, max);
                        sysPos.Y = Random.Float(min, max);
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
