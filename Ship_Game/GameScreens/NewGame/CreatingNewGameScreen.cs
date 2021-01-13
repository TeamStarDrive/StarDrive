using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.NewGame;

namespace Ship_Game
{
    public sealed class CreatingNewGameScreen : GameScreen
    {
        readonly int NumSystems;
        readonly Array<Vector2> ClaimedSpots = new Array<Vector2>();
        readonly RaceDesignScreen.GameMode Mode;
        readonly Vector2 GalacticCenter;
        readonly UniverseData Data;
        readonly Empire Player;
        readonly UniverseData.GameDifficulty Difficulty;
        readonly int NumOpponents;
        readonly MainMenuScreen MainMenu;
        Texture2D LoadingScreenTexture;
        string AdviceText;
        TaskResult BackgroundTask;
        UniverseScreen us;

        public CreatingNewGameScreen(Empire player, GalSize universeSize, int numSystems, 
                float starNumModifier, int numOpponents, RaceDesignScreen.GameMode mode, 
                float pace, UniverseData.GameDifficulty difficulty, MainMenuScreen mainMenu) : base(null)
        {
            CanEscapeFromScreen = false;
            MainMenu = mainMenu;
            foreach (Artifact art in ResourceManager.ArtifactsDict.Values)
                art.Discovered = false;

            Difficulty = difficulty;
            Mode = mode;
            NumOpponents = numOpponents;
            NumSystems   = numSystems;
            EmpireManager.Clear();
            ResourceManager.LoadEncounters();

            Data = new UniverseData
            {
                FTLSpeedModifier      = GlobalStats.FTLInSystemModifier,
                EnemyFTLSpeedModifier = GlobalStats.EnemyFTLInSystemModifier,
                GravityWells          = GlobalStats.PlanetaryGravityWells,
                FTLinNeutralSystem    = GlobalStats.WarpInSystem,
                difficulty            = difficulty,
                GalaxySize            = universeSize
            };

            CurrentGame.StartNew(Data, pace, starNumModifier, GlobalStats.ExtraPlanets, NumOpponents + 1); // +1 is the player empire
            Player          = player;
            player.isPlayer = true;

            player.Initialize();
            player.data.CurrentAutoScout     = player.data.ScoutShip;
            player.data.CurrentAutoColony    = player.data.ColonyShip;
            player.data.CurrentAutoFreighter = player.data.FreighterShip;
            player.data.CurrentConstructor   = player.data.ConstructorShip;


            /* FB - left here so we can see the legacy numbers. size is coming as a parameter in the declaration.
            bool corners = Mode == RaceDesignScreen.GameMode.Corners;
            int size;
            switch (universeSize)
            {
                default:
                case GalSize.Tiny:   size =                16;  Data.Size = new Vector2(1750000); break;
                case GalSize.Small:  size = corners ? 32 : 30;  Data.Size = new Vector2(3500000); break;
                case GalSize.Medium: size = corners ? 48 : 45;  Data.Size = new Vector2(5500000); break;
                case GalSize.Large:  size = corners ? 64 : 70;  Data.Size = new Vector2(9000000); break;
                case GalSize.Huge:   size = corners ? 80 : 92;  Data.Size = new Vector2(12500000); break;
                case GalSize.Epic:   size = corners ? 112: 115; Data.Size = new Vector2(17500000); break;
                // case GalSize.TrulyEpic:   size = corners ? 144: 160; Data.Size = new Vector2(33554423); break;
            }*/

            switch (universeSize)
            {
                default:
                case GalSize.Tiny:      Data.Size = new Vector2(2000000);  break;
                case GalSize.Small:     Data.Size = new Vector2(4000000);  break;
                case GalSize.Medium:    Data.Size = new Vector2(6000000);  break;
                case GalSize.Large:     Data.Size = new Vector2(9000000);  break;
                case GalSize.Huge:      Data.Size = new Vector2(12000000); break;
                case GalSize.Epic:      Data.Size = new Vector2(15000000); break;
                case GalSize.TrulyEpic: Data.Size = new Vector2(20000000); break;
            }

           //Log.Info($"Empire.ProjectorRadius = {Empire.ProjectorRadius}");

           Data.EmpireList.Add(player);
            EmpireManager.Add(player);
            GalacticCenter = new Vector2(0f, 0f);  // Gretman (for new negative Map dimensions)
            StatTracker.Reset();
        }

        public override void LoadContent()
        {
            ScreenManager.ClearScene();
            LoadingScreenTexture = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            AdviceText = Fonts.Arial12Bold.ParseText(ResourceManager.LoadRandomAdvice(), 500f);

            BackgroundTask = Parallel.Run(GenerateSystems);
            base.LoadContent();
        }

        void FinalizeEmpires(ProgressCounter step)
        {
            step.Start(Data.EmpireList.Count);
            for (int empireId = 0; empireId < Data.EmpireList.Count; empireId++)
            {
                step.Advance();
                Empire empire = Data.EmpireList[empireId];
                if (empire.isFaction)
                    continue;

                IReadOnlyList<Planet> planets = empire.GetPlanets();
                for (int planetId = 0; planetId < planets.Count; planetId++)
                {
                    Planet planet = planets[planetId];
                    planet.MineralRichness += GlobalStats.StartingPlanetRichness;
                    planet.ParentSystem.SetExploredBy(empire);
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

                    if (planet.HasSpacePort)
                    {
                        planet.Station = new SpaceStation(planet);
                        planet.Station.LoadContent(ScreenManager, empire);
                    }
                }
            }

            foreach (Empire e in Data.EmpireList)
            {
                if (e.isFaction)
                    continue;

                e.InitFleetEmpireStrMultiplier();
                if (e.data.Traits.BonusExplored <= 0)
                    continue;
                
                Planet homeWorld             = e.GetPlanets()[0];
                SolarSystem[] closestSystems = Data.SolarSystemsList.Sorted(system => homeWorld.Center.Distance(system.Position));
                int numExplored              = Data.SolarSystemsList.Count >= 20 ? e.data.Traits.BonusExplored : Data.SolarSystemsList.Count;

                for (int i = 0; i < numExplored; ++i)
                {
                    SolarSystem ss = closestSystems[i];
                    ss.SetExploredBy(e);
                    foreach (Planet planet in ss.PlanetList)
                    {
                        planet.SetExploredBy(e);
                    }
                }
            }
        }

        void FinalizeSolarSystems()
        {
            foreach (SolarSystem solarSystem1 in Data.SolarSystemsList)
            {
                var list = new Array<SysDisPair>();
                foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
                {
                    if (solarSystem1 != solarSystem2)
                    {
                        float num1 = solarSystem1.Position.Distance(solarSystem2.Position);
                        if (list.Count < 5)
                        {
                            list.Add(new SysDisPair
                            {
                                System = solarSystem2,
                                Distance = num1
                            });
                        }
                        else
                        {
                            int index1 = 0;
                            float num2 = 0.0f;
                            for (int index2 = 0; index2 < 5; ++index2)
                            {
                                if (list[index2].Distance > num2)
                                {
                                    index1 = index2;
                                    num2 = list[index2].Distance;
                                }
                            }
                            if (num1 < num2)
                                list[index1] = new SysDisPair
                                {
                                    System = solarSystem2,
                                    Distance = num1
                                };
                        }
                    }
                }
                foreach (SysDisPair sysDisPair in list)
                    solarSystem1.FiveClosestSystems.Add(sysDisPair.System);
            }

        }

        void SubmitSceneObjects(ProgressCounter step)
        {
            step.Start(Data.SolarSystemsList.Count);
            for (int i = 0; i < Data.SolarSystemsList.Count; ++i)
            {
                step.Advance();
                SolarSystem wipSystem = Data.SolarSystemsList[i];
                
                foreach (Planet planet in wipSystem.PlanetList)
                {
                    planet.ParentSystem = wipSystem;
                    planet.Center += wipSystem.Position;
                    planet.InitializePlanetMesh(this);
                }
                foreach (Asteroid asteroid in wipSystem.AsteroidsList)
                {
                    asteroid.Position += wipSystem.Position;
                }
                foreach (Ship ship in wipSystem.ShipList)
                {
                    ship.Position = ship.loyalty.GetPlanets()[0].Center + new Vector2(6000f, 2000f);
                    ship.InitializeShip();
                }
            }
            step.Finish();
        }

        void GenerateInitialSystemData(ProgressCounter step)
        {
            step.Start(0.15f, 0.20f, 0.30f, 0.35f); // proportions for each step

            CreateOpponents(step.NextStep());
            Empire.InitializeRelationships(Data.EmpireList, Difficulty);
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep()); // 240ms
            LoadEmpireStartingSystems(step.NextStep()); // 420ms
            GenerateRandomSystems(step.NextStep());    // 425ms

            switch (Mode)
            {
                case RaceDesignScreen.GameMode.Corners:       GenerateCornersGameMode();                 break;
                case RaceDesignScreen.GameMode.BigClusters:   GenerateBigClusters();                     break;
                case RaceDesignScreen.GameMode.SmallClusters: GenerateSmallClusters();                   break;
                default:                                      SolarSystemSpacing(Data.SolarSystemsList); break; // 2ms
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
                Empire e = Data.CreateEmpire(readOnlyData);
                RacialTrait t = e.data.Traits;

                e.data.FlatMoneyBonus  += e.DifficultyModifiers.FlatMoneyBonus;
                t.ProductionMod        += e.DifficultyModifiers.ProductionMod;
                t.ResearchMod          += e.DifficultyModifiers.ResearchMod;
                t.TaxMod               += e.DifficultyModifiers.TaxMod;
                t.ModHpModifier        += e.DifficultyModifiers.ModHpModifier;
                t.ShipCostMod          += e.DifficultyModifiers.ShipCostMod;
                t.ResearchTaxMultiplier = e.DifficultyModifiers.ResearchTaxMultiplier; // the "=" here is intended

                step.Advance();
            }
            
            foreach (IEmpireData readOnlyData in ResourceManager.MinorRaces)
            {
                Data.CreateEmpire(readOnlyData);
                step.Advance();
            }
        }

        void LoadEmpireStartingSystems(ProgressCounter step)
        {
            step.Start(Data.EmpireList.Count);
            foreach (Empire e in Data.EmpireList)
            {
                step.Advance();
                if (e.isFaction)
                    continue;
                SolarSystem sys;
                SolarSystemData systemData = ResourceManager.LoadSolarSystemData(e.data.Traits.HomeSystemName);
                if (systemData == null)
                {
                    sys = new SolarSystem();
                    sys.GenerateStartingSystem(e.data.Traits.HomeSystemName, 1f, e);
                }
                else sys = SolarSystem.GenerateSystemFromData(systemData, e);

                sys.isStartingSystem = true;
                Data.SolarSystemsList.Add(sys);
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
                var solarSystem = SolarSystem.GenerateSystemFromData(systemData, null);
                solarSystem.DontStartNearPlayer = true; // Added by Gretman
                Data.SolarSystemsList.Add(solarSystem);
                systemCount++;
                step.Advance();
            }

            var nameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
            for (; systemCount < NumSystems; ++systemCount)
            {
                var solarSystem2 = new SolarSystem();
                solarSystem2.GenerateRandomSystem(nameGenerator.NextName, 1f);
                Data.SolarSystemsList.Add(solarSystem2);
                step.Advance();
            }
        }

        void SolarSystemSpacing(Array<SolarSystem> solarSystems)
        {
            foreach (SolarSystem solarSystem2 in solarSystems)
            {
                float spacing = 350000f;
                if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                    spacing = Data.Size.X / (2f - 1f / (Data.EmpireList.Count - 1));
                solarSystem2.Position = GenerateRandomSysPos(spacing);
            }
        }

        void GenerateCornersGameMode()
        {
            short whichCorner = StartingPositionCorners();

            foreach (SolarSystem system in Data.SolarSystemsList)
            {
                // This will distribute all the rest of the planets evenly
                if (!system.isStartingSystem && !system.DontStartNearPlayer)
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
            short whichcorner = (short) RandomMath.RandomBetween(0, 4); //So the player doesnt always end up in the same corner;
            foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
            {
                if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                {
                    if (solarSystem2.isStartingSystem)
                    {
                        //Corner Values
                        //0 = Top Left
                        //1 = Top Right
                        //2 = Bottom Left
                        //3 = Bottom Right

                        //Put the 4 Home Planets into their corners, nessled nicely back a bit
                        float RandomoffsetX =
                            RandomMath.RandomBetween(0, 19) / 100; //Do want some variance in location, but still in the back
                        float RandomoffsetY = RandomMath.RandomBetween(0, 19) / 100;
                        float MinOffset = 0.04f; //Minimum Offset
                        //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                        float CornerOffset = 0.75f; //Additional Offset for being in corner
                        //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                        switch (whichcorner)
                        {
                            case 0:
                                solarSystem2.Position = new Vector2(
                                    (-Data.Size.X + (Data.Size.X * (MinOffset + RandomoffsetX))),
                                    (-Data.Size.Y + (Data.Size.Y * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 1:
                                solarSystem2.Position = new Vector2(
                                    (Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (-Data.Size.Y + (Data.Size.Y * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 2:
                                solarSystem2.Position = new Vector2(
                                    (-Data.Size.X + (Data.Size.X * (MinOffset + RandomoffsetX))),
                                    (Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(solarSystem2.Position);
                                break;
                            case 3:
                                solarSystem2.Position = new Vector2(
                                    (Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
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

        readonly ProgressCounter Progress = new ProgressCounter();

        void GenerateSystems()
        {
            Progress.Start(0.5f, 0.3f, 0.2f);
            GenerateInitialSystemData(Progress.NextStep());
            SubmitSceneObjects(Progress.NextStep());
            FinalizeSolarSystems();
            FinalizeEmpires(Progress.NextStep());
            Progress.Finish();

            Log.Info(ConsoleColor.Blue,    $"  GenerateInitialSystemData elapsed: {Progress[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue,    $"  SubmitSceneObjects        elapsed: {Progress[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue,    $"  FinalizeEmpires           elapsed: {Progress[2].ElapsedMillis}ms");
            Log.Info(ConsoleColor.DarkRed, $"TOTAL GenerateSystems       elapsed: {Progress.ElapsedMillis}ms");
        }

        Vector2 GenerateRandomSysPos(float spacing)
        {
            float safteyBreak = 1;
            Vector2 sysPos;
            do {
                spacing *= safteyBreak;
                sysPos = RandomMath.Vector2D(Data.Size.X - 100000f);
                safteyBreak *= .97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        void GenerateBigClusters()
        {
            // Divides the galaxy to several sectors and populates each sector with starts
            (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors(NumOpponents + 1);
            Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.1f);
            GenerateClustersStartingSystems(sectors);
            GenerateClusterSystems(sectors);
        }

        void GenerateSmallClusters()
        {
            // Divides the galaxy to many sectors and populates each sector with stars
            int numSectorsPerAxis  = GetNumSectorsPerAxis(NumSystems, NumOpponents + 1);
            float offsetMultiplier = 0.2f / numSectorsPerAxis.UpperBound(4);
            float deviation        = 0.02f * numSectorsPerAxis.UpperBound(4);
            Array<Sector> sectors  = GenerateSectors(numSectorsPerAxis, numSectorsPerAxis, deviation, offsetMultiplier);
            GenerateClustersStartingSystems(sectors);
            GenerateClusterSystems(sectors);
        }

        (int NumHorizontalSectors, int NumVerticalSectors) GetNumSectors(int numEmpires)
        {
            int numHorizontalSectors = 2;
            int numVerticalSectors   = 2;

            if (numEmpires > 9) // 4x4 sectors - probably not applicable (limited empires to 8 by default)
            {
                numHorizontalSectors = 4;
                numVerticalSectors   = 4;
            }
            else if (numEmpires > 6) // 3x3 sectors
            {
                numHorizontalSectors = 3;
                numVerticalSectors   = 3;
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
            int numSectors        = numSystems / numEmpires.LowerBound(4); // each sector will have stars as ~player num, minimum of 4
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
                    Sector sector = new Sector(Data.Size, numHorizontalSectors, numVerticalSectors, 
                        h, v, deviation, offsetMultiplier);

                    sectors.Add(sector);
                }
            }

            return sectors;
        }

        void GenerateClustersStartingSystems(Array<Sector> sectors)
        {
            Array<Sector> startingSectors = new Array<Sector>();
            foreach (SolarSystem system in Data.SolarSystemsList.Filter(s => s.isStartingSystem))
            {
                Sector startingSector = sectors.RandItem();
                while (startingSectors.Contains(startingSector))
                    startingSector = sectors.RandItem();

                startingSectors.Add(startingSector);
                system.Position = GenerateSystemInCluster(startingSector, 350000f);
            }
        }

        void GenerateClusterSystems(Array<Sector> sectors)
        {
            int i = 0;
            foreach (SolarSystem system in Data.SolarSystemsList.Filter(s => !s.isStartingSystem)
                     .SortedDescending(s => s.AverageValueForEmpires(Data.EmpireList)))
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
                spacing     *= safetyBreak;
                sysPos       = sector.RandomPosInSector;
                safetyBreak *= 0.99f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        struct Sector
        {
            private readonly float LeftX;
            private readonly float RightX;
            private readonly float TopY;
            private readonly float BotY;
            private readonly Vector2 Center;

            public Sector(Vector2 universeSize, int horizontalSectors, int verticalSectors, int horizontalNum, int verticalNum, 
                          float deviation, float offsetMultiplier) : this()
            {
                float xSection = universeSize.X / horizontalSectors;
                float ySection = universeSize.Y / verticalSectors;
                float offset   = universeSize.X * offsetMultiplier; 

                // raw center is the center of the sector before generating offset (for gaps)
                Vector2 rawCenter = new Vector2(-universeSize.X + xSection * (-1 + horizontalNum*2), 
                                             -universeSize.Y + ySection * (-1 + verticalNum*2));

                // Some deviation in the center of the cluster
                rawCenter = rawCenter.GenerateRandomPointInsideCircle(universeSize.X * deviation);

                LeftX  = (rawCenter.X - xSection).LowerBound(-universeSize.X);
                RightX = (rawCenter.X + xSection).UpperBound(universeSize.X);
                TopY   = (rawCenter.Y - ySection).LowerBound(-universeSize.Y) + offset;
                BotY   = (rawCenter.Y + ySection).UpperBound(universeSize.Y) - offset;

                // creating some gaps between clusters
                GenerateOffset(universeSize.X, offset,ref LeftX, ref RightX);
                GenerateOffset(universeSize.Y, offset, ref TopY, ref BotY);

                // This is the true Center, after all offsets are applied with borders
                Center = new Vector2((LeftX + RightX) / 2, (TopY + BotY) / 2);
            }
            
            // Offset from borders. Less offset if near one or 2 edges
            void GenerateOffset(float size, float offset, ref float leftOrTop, ref float rightOrBot)
            {
                if (leftOrTop.AlmostEqual(-size))
                {
                    leftOrTop  += offset * 0.1f;
                    rightOrBot -= offset * 1.9f;
                }
                else if (rightOrBot.AlmostEqual(size))
                {
                    leftOrTop  += offset * 1.9f;
                    rightOrBot -= offset * 0.1f;
                }
                else
                {
                    leftOrTop  += offset;
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

            float SizeX = Data.Size.X * 2;     //Allow for new negative coordinates
            float SizeY = Data.Size.Y * 2;

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
                sysPos = new Vector2(   RandomMath.RandomBetween(-Data.Size.X + (float)offsetX, -Data.Size.X + (float)(CornerSizeX + offsetX)),
                                        RandomMath.RandomBetween(-Data.Size.Y + (float)offsetY, -Data.Size.Y + (float)(CornerSizeY + offsetY)));
                noinfiniteloop += 1000;
            } 
            //Decrease the acceptable proximity slightly each attempt, so there wont be an infinite loop here on 'tiny' + 'SuperPacked' maps
            while (!SystemPosOK(sysPos, 400000 - noinfiniteloop));
            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            Random random = new Random();
            Vector2 vector2 = GalacticCenter;
            float num1 = (float)(2f / numOfStars * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow(Data.Size.X - 0.0850000008940697 * Data.Size.X, index / (float)numOfStars);
                float num3 = index * num1 + rotation;
                float x = vector2.X + RadMath.Cos(num3) * num2;
                float y = vector2.Y + RadMath.Sin(num3) * num2;
                Vector2 sysPos = new Vector2(RandomMath.RandomBetween(-10000f, 10000f) * index, (float)(RandomMath.RandomBetween(-10000f, 10000f) * (double)index / 4.0));
                sysPos = new Vector2(x, y) + sysPos;
                if (SystemPosOK(sysPos))
                {
                    ClaimedSpots.Add(sysPos);
                }
                else
                {
                    while (!SystemPosOK(sysPos))
                    {
                        sysPos.X = GalacticCenter.X + RandomMath.RandomBetween((float)(-(double)Data.Size.X / 2.0 + 0.0850000008940697 * Data.Size.X), (float)(Data.Size.X / 2.0 - 0.0850000008940697 * Data.Size.X));
                        sysPos.Y = GalacticCenter.Y + RandomMath.RandomBetween((float)(-(double)Data.Size.X / 2.0 + 0.0850000008940697 * Data.Size.X), (float)(Data.Size.X / 2.0 - 0.0850000008940697 * Data.Size.X));
                    }
                    ClaimedSpots.Add(sysPos);
                }
            }
        }

        bool IsInUniverseBounds(Vector2 sysPos)
        {
            return -Data.Size.X < sysPos.X && sysPos.X < Data.Size.X
                && -Data.Size.Y < sysPos.Y && sysPos.Y < Data.Size.Y;
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

        public override bool HandleInput(InputState input)
        {
            if (BackgroundTask?.IsComplete != true || !input.InGameSelect)
                return false;

            GameAudio.StopGenericMusic(immediate: false);
            Planet homePlanet = Player.GetPlanets()[0];
            us = new UniverseScreen(Data, Player)
            {
                ScreenManager = ScreenManager,
                CamPos = new Vector3(homePlanet.Center.X, homePlanet.Center.Y, 5000f),
            };

            EmpireShipBonuses.RefreshBonuses();

            ScreenManager.AddScreen(us);

            Log.Info("CreatingNewGameScreen.Objects.Update(0.01)");
            us.Objects.Update(new FixedSimTime(0.01f));

            ScreenManager.Music.Stop();
            ScreenManager.RemoveScreen(MainMenu);
 
            ExitScreen();
            return true;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);

            if (BackgroundTask?.IsComplete == false)
            {
                // heavily throttle main draw thread, so the worker thread can turbo
                Thread.Sleep(33);
                if (IsDisposed) // just in case we tried to ALT+F4 during loading
                    return;
            }

            batch.Begin();
            int width  = ScreenWidth;
            int height = ScreenHeight;
            if (LoadingScreenTexture != null)
                batch.Draw(LoadingScreenTexture, new Rectangle(width / 2 - 960, height / 2 - 540, 1920, 1080), Color.White);
            
            var r = new Rectangle(width / 2 - 150, height - 25, 300, 25);
            new ProgressBar(r) { Max = 100f, Progress = Progress.Percent * 100f }.Draw(batch);

            var position = new Vector2(ScreenCenter.X - 250f, (float)(r.Y - Fonts.Arial12Bold.MeasureString(AdviceText).Y - 5.0));
            batch.DrawString(Fonts.Arial12Bold, AdviceText, position, Color.White);
            
            if (BackgroundTask?.IsComplete == true)
            {
                position.Y = (float)(position.Y - Fonts.Pirulen16.LineSpacing - 10.0);
                string token = Localizer.Token(2108);
                position.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(token).X / 2f;

                batch.DrawString(Fonts.Pirulen16, token, position, CurrentFlashColor);
            }

            batch.End();
        }

        protected override void Destroy()
        {
            LoadingScreenTexture?.Dispose(ref LoadingScreenTexture);                
            base.Destroy();
        }

        struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
