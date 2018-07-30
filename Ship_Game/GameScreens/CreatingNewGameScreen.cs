// Type: Ship_Game.CreatingNewGameScreen
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Ship_Game.Ships;


namespace Ship_Game
{
    public sealed class CreatingNewGameScreen : GameScreen
    {
        private float Scale = 1f;
        private int NumSystems = 50;
        private bool firstRun = true;
        private AutoResetEvent WorkerBeginEvent = new AutoResetEvent(false);
        private ManualResetEvent WorkerCompletedEvent = new ManualResetEvent(true);
        private Array<Vector2> stars = new Array<Vector2>();
        private Array<Vector2> ClaimedSpots = new Array<Vector2>();
        private RaceDesignScreen.GameMode Mode;
        private Vector2 GalacticCenter;
        private UniverseData Data;
        private string EmpireToRemoveName;
        private Empire PlayerEmpire;
        private UniverseData.GameDifficulty Difficulty;
        private int NumOpponents;
        private MainMenuScreen mmscreen;
        private DiplomaticTraits DTraits;
        private Texture2D LoadingScreenTexture;
        private SolarSystem PlayerSystem;
        private string text;
        private Effect ThrusterEffect;
        private int counter;
        private Ship playerShip;
        private Thread WorkerThread;
        private UniverseScreen us;
        private bool AllSystemsGenerated;
        private float PercentLoaded;
        private int systemToMake;
        

        public CreatingNewGameScreen(Empire empire, string universeSize, 
                float starNumModifier, string empireToRemoveName, 
                int numOpponents, RaceDesignScreen.GameMode gamemode, 
                int gameScale, UniverseData.GameDifficulty difficulty, MainMenuScreen mmscreen) : base(mmscreen)
        {
            GlobalStats.RemnantArmageddon = false;
            GlobalStats.RemnantKills = 0;
            this.mmscreen = mmscreen;
            foreach (var art in ResourceManager.ArtifactsDict)
                art.Value.Discovered = false;

            RandomEventManager.ActiveEvent = null;
            Difficulty = difficulty;
            if      (gameScale == 5) Scale = 8;
            else if (gameScale == 6) Scale = 16;
            else                     Scale = gameScale;

            Mode = gamemode;
            NumOpponents = numOpponents;
            EmpireToRemoveName = empireToRemoveName;
            EmpireManager.Clear();

            DTraits = ResourceManager.DiplomaticTraits;
            ResourceManager.LoadEncounters();
            PlayerEmpire = empire;
            empire.Initialize();
            empire.data.CurrentAutoScout     = empire.data.ScoutShip;
            empire.data.CurrentAutoColony    = empire.data.ColonyShip;
            empire.data.CurrentAutoFreighter = empire.data.FreighterShip;
            empire.data.CurrentConstructor   = empire.data.ConstructorShip;
            Data = new UniverseData
            {
                FTLSpeedModifier      = GlobalStats.FTLInSystemModifier,
                EnemyFTLSpeedModifier = GlobalStats.EnemyFTLInSystemModifier,                    
                GravityWells          = GlobalStats.PlanetaryGravityWells,
                FTLinNeutralSystem    = GlobalStats.WarpInSystem
            };

            bool corners = Mode == RaceDesignScreen.GameMode.Corners;
            int size;
            switch (universeSize)
            {
                default: /*"Tiny"*/ size = 16;                 Data.Size = new Vector2(1750000); break;
                case "Small":       size = corners ? 32 : 30;  Data.Size = new Vector2(3500000); break;
                case "Medium":      size = corners ? 48 : 45;  Data.Size = new Vector2(5500000); break;
                case "Large":       size = corners ? 64 : 70;  Data.Size = new Vector2(9000000); break;
                case "Huge":        size = corners ? 80 : 92;  Data.Size = new Vector2(13500000); break;
                case "Epic":        size = corners ? 112: 115; Data.Size = new Vector2(20000000); break;
                case "TrulyEpic":   size = corners ? 144: 160; Data.Size = new Vector2(33554423); break;
            }

            NumSystems = (int)(size * starNumModifier);           
            //Log.Info($"Empire.ProjectorRadius = {Empire.ProjectorRadius}");
            
            UniverseData.UniverseWidth = Data.Size.X * 2;
            Data.Size *= Scale;
            Data.EmpireList.Add(empire);
            EmpireManager.Add(empire);
            GalacticCenter = new Vector2(0f, 0f);  // Gretman (for new negative Map dimensions)
            StatTracker.SnapshotsDict.Clear();
        }

        private void SaveRace(Empire empire)
        {
            using (TextWriter textWriter = new StreamWriter("Content/Races/test.xml"))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(EmpireData));
                xmlSerializer.Serialize(textWriter, new EmpireData { Traits = empire.data.Traits });
            }
        }

        public override void LoadContent()
        {
            ScreenManager.ClearScene();
            LoadingScreenTexture = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            string adviceString  = ResourceManager.LoadRandomAdvice();
            text = HelperFunctions.ParseText(Fonts.Arial12Bold, adviceString, 500f);

            WorkerThread = new Thread(Worker) { IsBackground = true };
            WorkerThread.Start();
            base.LoadContent();
        }

        private void FinalizeEmpires()
        {
            foreach (Empire empire in Data.EmpireList)
            {
                if (empire.isFaction)
                    continue;

                foreach (Planet planet in empire.GetPlanets())
                {
                    planet.MineralRichness += GlobalStats.StartingPlanetRichness;
                    planet.ParentSystem.SetExploredBy(empire);
                    planet.SetExploredBy(empire);

                    foreach (Planet p in planet.ParentSystem.PlanetList)
                        p.SetExploredBy(empire);

                    if (planet.ParentSystem.OwnerList.Count == 0)
                    {
                        planet.ParentSystem.OwnerList.Add(empire);
                        foreach (Planet planet2 in planet.ParentSystem.PlanetList)
                            planet2.SetExploredBy(empire);
                    }
                    if (planet.HasShipyard)
                    {
                        SpaceStation spaceStation = new SpaceStation { planet = planet };
                        planet.Station = spaceStation;
                        spaceStation.ParentSystem = planet.ParentSystem;
                        spaceStation.LoadContent(ScreenManager);
                    }

                    string colonyShip = empire.data.DefaultColonyShip;
                    if (GlobalStats.HardcoreRuleset) colonyShip += " STL";

                    Ship ship1 = Ship.CreateShipAt(colonyShip, empire, planet, new Vector2(-2000, -2000), true);
                    Data.MasterShipList.Add(ship1);

                    string startingScout = empire.data.StartingScout;
                    if (GlobalStats.HardcoreRuleset) startingScout += " STL";

                    Ship ship2 = Ship.CreateShipAt(startingScout, empire, planet, new Vector2(-2500, -2000), true);
                    Data.MasterShipList.Add(ship2);

                    if (empire == PlayerEmpire)
                    {
                        string starterShip = empire.data.Traits.Prototype == 0 ? empire.data.StartingShip : empire.data.PrototypeShip;

                        playerShip = Ship.CreateShipAt(starterShip, empire, planet, new Vector2(350f, 0.0f), true);
                        playerShip.SensorRange = 100000f; // @todo What is this range hack?

                        if (GlobalStats.ActiveModInfo == null || playerShip.VanityName == "")
                            playerShip.VanityName = "Perseverance";

                        Data.MasterShipList.Add(playerShip);

                        // Doctor: I think commenting this should completely stop all the recognition of the starter ship being the 'controlled' ship for the pie menu.
                        Data.playerShip = playerShip;

                        planet.GovernorOn = false;
                        planet.colonyType = Planet.ColonyType.Colony;
                    }
                    else
                    {
                        string starterShip = empire.data.StartingShip;
                        if (GlobalStats.HardcoreRuleset) starterShip += " STL";
                        starterShip = empire.data.Traits.Prototype == 0 ? starterShip : empire.data.PrototypeShip;

                        Ship ship3 = Ship.CreateShipAt(starterShip, empire, planet, new Vector2(-2500, -2000), true);
                        Data.MasterShipList.Add(ship3);

                        //empire.AddShip(ship3);
                        //empire.GetForcePool().Add(ship3);
                    }
                }
            }
            foreach (Empire empire in Data.EmpireList)
            {
                if (empire.isFaction || empire.data.Traits.BonusExplored <= 0)
                    continue;

                var planet0 = empire.GetPlanets()[0];
                var solarSystems = Data.SolarSystemsList;
                var orderedEnumerable = solarSystems.OrderBy(system => Vector2.Distance(planet0.Center, system.Position));
                int numSystemsExplored = solarSystems.Count >= 20 ? empire.data.Traits.BonusExplored : solarSystems.Count;
                for (int i = 0; i < numSystemsExplored; ++i)
                {
                    var system = orderedEnumerable.ElementAt(i);
                    system.SetExploredBy(empire);
                    foreach (Planet planet in system.PlanetList)
                        planet.SetExploredBy(empire);
                }
            }
        }

        private void FinalizeSolarSystems()
        {
            foreach (SolarSystem solarSystem1 in Data.SolarSystemsList)
            {
                var list = new Array<SysDisPair>();
                foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
                {
                    if (solarSystem1 != solarSystem2)
                    {
                        float num1 = Vector2.Distance(solarSystem1.Position, solarSystem2.Position);
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

        private void SubmitSceneObjectsForRendering()
        {
            SolarSystem wipSystem = Data.SolarSystemsList[systemToMake];

            PercentLoaded = (counter + systemToMake) / (float)(Data.SolarSystemsList.Count * 2);

            foreach (Planet planet in wipSystem.PlanetList)
            {
                planet.ParentSystem = wipSystem;
                planet.Center += wipSystem.Position;
                planet.InitializePlanetMesh(this);
            }
            foreach (Asteroid asteroid in wipSystem.AsteroidsList)
            {
                asteroid.Position3D.X += wipSystem.Position.X;
                asteroid.Position3D.Y += wipSystem.Position.Y;
                asteroid.Initialize();
                AddObject(asteroid.So);
            }
            foreach (Moon moon in wipSystem.MoonList)
            {
                moon.Initialize();
                AddObject(moon.So);
            }
            foreach (Ship ship in wipSystem.ShipList)
            {
                ship.Position = ship.loyalty.GetPlanets()[0].Center + new Vector2(6000f, 2000f);
                ship.InitializeShip();
            }
        }

        private void GenerateInitialSystemData()
        {
            var removalCollection = new BatchRemovalCollection<EmpireData>();
            foreach (EmpireData empireData in ResourceManager.Empires)
            {
                if (empireData.Traits.Name != EmpireToRemoveName && empireData.Faction == 0 && !empireData.MinorRace)
                    removalCollection.Add(empireData);
            }
            int num = removalCollection.Count - NumOpponents;
            float spaceSaved = GC.GetTotalMemory(true);
            for (int opponents = 0; opponents < num; ++opponents)
            {
                //Intentionally using too high of a value here, because of the truncated decimal. -Gretman
                int index = RandomMath.InRange(removalCollection.Count);

                Log.Info($"Race excluded from game: {removalCollection[index].PortraitName}  (Index {index} of {removalCollection.Count-1})");
                removalCollection.RemoveAt(index);
            }

            Log.Info($"Memory purged: {spaceSaved - GC.GetTotalMemory(true)}");

            foreach (EmpireData data in removalCollection)
            {
                Empire empireFromEmpireData = CreateEmpireFromEmpireData(data);
                Data.EmpireList.Add(empireFromEmpireData);
                var traits = empireFromEmpireData.data.Traits;
                switch (Difficulty)
                {
                    case UniverseData.GameDifficulty.Easy:
                        traits.ProductionMod -= 0.25f;
                        traits.ResearchMod   -= 0.25f;
                        traits.TaxMod        -= 0.25f;
                        traits.ModHpModifier -= 0.25f;
                        break;
                    case UniverseData.GameDifficulty.Hard:
                        empireFromEmpireData.data.FlatMoneyBonus += 10;
                        traits.ProductionMod += 0.5f;
                        traits.ResearchMod   += 0.75f;
                        traits.TaxMod        += 0.5f;
                        traits.ShipCostMod   -= 0.2f;
                        break;
                    case UniverseData.GameDifficulty.Brutal:
                        empireFromEmpireData.data.FlatMoneyBonus += 20; // cheaty cheat
                        traits.ProductionMod += 1.0f;
                        traits.ResearchMod    = 1.33f;
                        traits.TaxMod        += 1.0f;
                        traits.ShipCostMod   -= 0.5f;
                        break;
                }
                EmpireManager.Add(empireFromEmpireData);
            }

            foreach (EmpireData data in ResourceManager.Empires)
            {
                if (data.Faction == 0 && !data.MinorRace)
                    continue;
                Empire empireFromEmpireData = CreateEmpireFromEmpireData(data);
                Data.EmpireList.Add(empireFromEmpireData);
                EmpireManager.Add(empireFromEmpireData);
            }
            
            foreach (Empire empire in Data.EmpireList)
            {
                foreach (Empire e in Data.EmpireList)
                {
                    if (empire == e)
                        continue;
                    Relationship r = new Relationship(e.data.Traits.Name);
                    empire.AddRelationships(e, r);
                    if (PlayerEmpire != e)
                        continue;
                    if (Difficulty <= UniverseData.GameDifficulty.Normal) continue;
                        float angerMod = (int)Difficulty * (90 - empire.data.DiplomaticPersonality.Trustworthiness);
                    r.Anger_DiplomaticConflict = angerMod;
                    r.Anger_MilitaryConflict = 1;
                }
            }
            ResourceManager.MarkShipDesignsUnlockable();

            Log.Info($"Memory purged: {spaceSaved - GC.GetTotalMemory(true)}");

            foreach (Empire empire in Data.EmpireList)
            {
                if (empire.isFaction)
                    continue;
                SolarSystem solarSystem;
                SolarSystemData systemData = ResourceManager.LoadSolarSystemData(empire.data.Traits.HomeSystemName);
                if (systemData == null)
                {
                    solarSystem = new SolarSystem();
                    solarSystem.GenerateStartingSystem(empire.data.Traits.HomeSystemName, Data, Scale, empire);
                }
                else solarSystem = SolarSystem.GenerateSystemFromData(systemData, empire);
                solarSystem.isStartingSystem = true;
                Data.SolarSystemsList.Add(solarSystem);
                if (empire == PlayerEmpire)
                    PlayerSystem = solarSystem;
            }
            int systemCount = 0;
            foreach (var systemData in ResourceManager.LoadRandomSolarSystems())
            {
                if (systemCount > NumSystems)
                    break;
                var solarSystem = SolarSystem.GenerateSystemFromData(systemData, null);
                solarSystem.DontStartNearPlayer = true; // Added by Gretman
                Data.SolarSystemsList.Add(solarSystem);
                systemCount++;
            }
            var markovNameGenerator = new MarkovNameGenerator( File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
            var solarSystem1 = new SolarSystem();
            solarSystem1.GenerateCorsairSystem(markovNameGenerator.NextName);
            solarSystem1.DontStartNearPlayer = true;
            Data.SolarSystemsList.Add(solarSystem1);
            for (; systemCount < NumSystems; ++systemCount)
            {
                var solarSystem2 = new SolarSystem();
                solarSystem2.GenerateRandomSystem(markovNameGenerator.NextName, this.Data, this.Scale);
                Data.SolarSystemsList.Add(solarSystem2);
                ++counter;
                PercentLoaded = counter / (float)(NumSystems * 2);
            }

            // This section added by Gretman
            if (Mode != RaceDesignScreen.GameMode.Corners)            
                SoloarSystemSpacing(Data.SolarSystemsList);            
            else
            {
                short whichcorner = StartingPositionCorners();

                foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
                {
                    //This will distribute all the rest of the planets evenly
                    if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                        continue;
                    solarSystem2.Position = GenerateRandomCorners(whichcorner);
                    whichcorner += 1;   //Only change which corner if a system is actually created
                    if (whichcorner > 3) whichcorner = 0;
                }
            }// Done breaking stuff -- Gretman

            ThrusterEffect = TransientContent.Load<Effect>("Effects/Thrust");
        }

        private void SoloarSystemSpacing(Array<SolarSystem> solarSystems)
        {
            foreach (SolarSystem solarSystem2 in solarSystems)
            {
                float spacing = 350000f;
                if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                    spacing = Data.Size.X / (2f - 1f / (Data.EmpireList.Count - 1));
                solarSystem2.Position = GenerateRandomSysPos(spacing);
            }
        }

        private short StartingPositionCorners()
        {
            short whichcorner = (short) RandomMath.RandomBetween(0, 4); //So the player doesnt always end up in the same corner;
            foreach (SolarSystem solarSystem2 in this.Data.SolarSystemsList)
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

        private void Worker()
        {
            while (!AllSystemsGenerated)
            {
                if (firstRun)
                {
                    GenerateInitialSystemData();
                    firstRun = false;
                }

                SubmitSceneObjectsForRendering();

                ++systemToMake;

                if (systemToMake == Data.SolarSystemsList.Count)
                {
                    FinalizeSolarSystems();
                    FinalizeEmpires();
                    AllSystemsGenerated = true;
                }
            }
        }

        public Vector2 GenerateRandomSysPos(float spacing)
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

        public Vector2 GenerateRandomCorners(short corner) //Added by Gretman for Corners Game type
        {
            //Corner Values
            //0 = Top Left
            //1 = Top Right
            //2 = Bottom Left
            //3 = Bottom Right

            float SizeX = this.Data.Size.X * 2;     //Allow for new negative coordinates
            float SizeY = this.Data.Size.Y * 2;

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
                sysPos = new Vector2(   RandomMath.RandomBetween(-this.Data.Size.X + (float)offsetX, -this.Data.Size.X + (float)(CornerSizeX + offsetX)),
                                        RandomMath.RandomBetween(-this.Data.Size.Y + (float)offsetY, -this.Data.Size.Y + (float)(CornerSizeY + offsetY)));
                noinfiniteloop += 1000;
            } 
            //Decrease the acceptable proximity slightly each attempt, so there wont be an infinite loop here on 'tiny' + 'SuperPacked' maps
            while (!this.SystemPosOK(sysPos, 400000 - noinfiniteloop));
            this.ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            Random random = new Random();
            Vector2 vector2 = this.GalacticCenter;
            float num1 = (float)((double)(2f / (float)numOfStars) * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow((double)this.Data.Size.X - 0.0850000008940697 * (double)this.Data.Size.X, (double)((float)index / (float)numOfStars));
                float num3 = (float)index * num1 + rotation;
                float x = vector2.X + (float)Math.Cos((double)num3) * num2;
                float y = vector2.Y + (float)Math.Sin((double)num3) * num2;
                Vector2 sysPos = new Vector2(RandomMath.RandomBetween(-10000f, 10000f) * (float)index, (float)((double)RandomMath.RandomBetween(-10000f, 10000f) * (double)index / 4.0));
                sysPos = new Vector2(x, y) + sysPos;
                if (this.SystemPosOK(sysPos))
                {
                    this.stars.Add(sysPos);
                    this.ClaimedSpots.Add(sysPos);
                }
                else
                {
                    while (!this.SystemPosOK(sysPos))
                    {
                        sysPos.X = this.GalacticCenter.X + RandomMath.RandomBetween((float)(-(double)this.Data.Size.X / 2.0 + 0.0850000008940697 * (double)this.Data.Size.X), (float)((double)this.Data.Size.X / 2.0 - 0.0850000008940697 * (double)this.Data.Size.X));
                        sysPos.Y = this.GalacticCenter.Y + RandomMath.RandomBetween((float)(-(double)this.Data.Size.X / 2.0 + 0.0850000008940697 * (double)this.Data.Size.X), (float)((double)this.Data.Size.X / 2.0 - 0.0850000008940697 * (double)this.Data.Size.X));
                    }
                    this.stars.Add(sysPos);
                    this.ClaimedSpots.Add(sysPos);
                }
            }
        }

        private bool SystemPosOK(Vector2 sysPos)
        {
            foreach (Vector2 vector2 in ClaimedSpots)
            {   //Updated to make use of the negative map values -Gretman
                if (Vector2.Distance(vector2, sysPos) < 300000.0 * Scale 
                    || sysPos.X >  Data.Size.X || sysPos.Y >  Data.Size.Y 
                    || sysPos.X < -Data.Size.X || sysPos.Y < -Data.Size.Y)
                    return false;
            }
            return true;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            foreach (Vector2 vector2 in ClaimedSpots)
            {
                if (Vector2.Distance(vector2, sysPos) < spacing 
                    || sysPos.X >  Data.Size.X || sysPos.Y >  Data.Size.Y 
                    || sysPos.X < -Data.Size.X || sysPos.Y < -Data.Size.Y)
                    return false;
            }
            return true;
        }

        public static EmpireData CopyEmpireData(EmpireData data)
        {
            EmpireData empireData = new EmpireData
            {
                ArmorPiercingBonus        = data.ArmorPiercingBonus,
                BaseReproductiveRate      = data.BaseReproductiveRate,
                BonusFighterLevels        = data.BonusFighterLevels,
                CounterIntelligenceBudget = 0.0f,
                DefaultColonyShip         = data.DefaultColonyShip,
                DefaultSmallTransport     = data.DefaultSmallTransport,
                DefaultTroopShip          = data.DefaultTroopShip
            };
            if (string.IsNullOrEmpty(empireData.DefaultTroopShip))
            {
                empireData.DefaultTroopShip = empireData.PortraitName + " " + "Troop";
            }
            empireData.DefaultConstructor                     = data.DefaultConstructor;
            empireData.DefaultShipyard                        = data.DefaultShipyard;
            empireData.DiplomacyDialogPath                    = data.DiplomacyDialogPath;
            empireData.DiplomaticPersonality                  = data.DiplomaticPersonality;
            empireData.EconomicPersonality                    = data.EconomicPersonality;
            empireData.EmpireFertilityBonus                   = data.EmpireFertilityBonus;
            empireData.EmpireWideProductionPercentageModifier = data.EmpireWideProductionPercentageModifier;
            empireData.ExcludedDTraits                        = data.ExcludedDTraits;
            empireData.ExcludedETraits                        = data.ExcludedETraits;
            empireData.ExplosiveRadiusReduction               = data.ExplosiveRadiusReduction;
            empireData.FlatMoneyBonus                         = 0.0f;
            empireData.FTLModifier                            = data.FTLModifier;
            empireData.FTLPowerDrainModifier                  = data.FTLPowerDrainModifier;
            empireData.FuelCellModifier                       = data.FuelCellModifier;
            empireData.Inhibitors                             = data.Inhibitors;
            empireData.MassModifier                           = data.MassModifier;
            //Doctor: Armour Mass Mod
            empireData.ArmourMassModifier         = data.ArmourMassModifier;
            empireData.MissileDodgeChance         = data.MissileDodgeChance;
            empireData.MissileHPModifier          = data.MissileHPModifier;
            empireData.OrdnanceEffectivenessBonus = data.OrdnanceEffectivenessBonus;
            empireData.Privatization              = data.Privatization;
            empireData.SensorModifier             = data.SensorModifier;
            empireData.SpyModifier                = data.SpyModifier;
            empireData.SpoolTimeModifier          = data.SpoolTimeModifier;
            empireData.StartingScout              = data.StartingScout;
            empireData.StartingShip               = data.StartingShip;
            empireData.SubLightModifier           = data.SubLightModifier;
            empireData.TaxRate                    = data.TaxRate;
            empireData.TroopDescriptionIndex      = data.TroopDescriptionIndex;
            empireData.TroopNameIndex             = data.TroopNameIndex;
            empireData.PowerFlowMod               = data.PowerFlowMod;
            empireData.ShieldPowerMod             = data.ShieldPowerMod;
            //Doctor: Civilian Maint Mod
            empireData.CivMaintMod = data.CivMaintMod;

            empireData.Traits = new RacialTrait
            {
                Aquatic              = data.Traits.Aquatic,
                Assimilators         = data.Traits.Assimilators,
                B                    = 128f,
                Blind                = data.Traits.Blind,
                BonusExplored        = data.Traits.BonusExplored,
                Burrowers            = data.Traits.Burrowers,
                ConsumptionModifier  = data.Traits.ConsumptionModifier,
                Cybernetic           = data.Traits.Cybernetic,
                DiplomacyMod         = data.Traits.DiplomacyMod,
                DodgeMod             = data.Traits.DodgeMod,
                EnergyDamageMod      = data.Traits.EnergyDamageMod,
                FlagIndex            = data.Traits.FlagIndex,
                G                    = 128f,
                GenericMaxPopMod     = data.Traits.GenericMaxPopMod,
                GroundCombatModifier = data.Traits.GroundCombatModifier,
                InBordersSpeedBonus  = data.Traits.InBordersSpeedBonus,
                MaintMod             = data.Traits.MaintMod,
                Mercantile           = data.Traits.Mercantile,
                Militaristic         = data.Traits.Militaristic,
                Miners               = data.Traits.Miners,
                ModHpModifier        = data.Traits.ModHpModifier,
                PassengerModifier    = data.Traits.PassengerModifier,
                ProductionMod        = data.Traits.ProductionMod,
                R                    = 128f,
                RepairMod            = data.Traits.RepairMod,
                ReproductionMod      = data.Traits.ReproductionMod,
                PopGrowthMax         = data.Traits.PopGrowthMax,
                PopGrowthMin         = data.Traits.PopGrowthMin,
                ResearchMod          = data.Traits.ResearchMod,
                ShipCostMod          = data.Traits.ShipCostMod,
                ShipType             = data.Traits.ShipType,
                Singular             = data.RebelSing,
                Plural               = data.RebelPlur,
                Spiritual            = data.Traits.Spiritual,
                SpyMultiplier        = data.Traits.SpyMultiplier,
                TaxMod               = data.Traits.TaxMod
            };
            empireData.TurnsBelowZero = 0;
            return empireData;
        }

        private Empire CreateEmpireFromEmpireData(EmpireData data)
        {
            var empire = new Empire();
            Log.Info($"Creating Empire {data.PortraitName}");
            if (data.Faction == 1)
                empire.isFaction = true;
            int index1 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.DiplomaticTraitsList.Count);
            data.DiplomaticPersonality = this.DTraits.DiplomaticTraitsList[index1];
            while (!CheckPersonality(data))
            {
                int index2 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.DiplomaticTraitsList.Count);
                data.DiplomaticPersonality = this.DTraits.DiplomaticTraitsList[index2];
            }
            int index3 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.EconomicTraitsList.Count);
            data.EconomicPersonality = this.DTraits.EconomicTraitsList[index3];
            while (!CheckEPersonality(data))
            {
                int index2 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.EconomicTraitsList.Count);
                data.EconomicPersonality = this.DTraits.EconomicTraitsList[index2];
            }
            empire.data = data;
            //Added by McShooterz: set values for alternate race file structure
            data.Traits.LoadTraitConstraints();
            empire.dd = ResourceManager.DDDict[data.DiplomacyDialogPath];
            empire.data.SpyModifier = data.Traits.SpyMultiplier;
            empire.data.Traits.Spiritual = data.Traits.Spiritual;
            data.Traits.PassengerModifier += data.Traits.PassengerBonus;
            empire.PortraitName = data.PortraitName;
            empire.data.Traits = data.Traits;
            empire.EmpireColor = new Color((byte)data.Traits.R, (byte)data.Traits.G, (byte)data.Traits.B);
            empire.Initialize();
            return empire;
        }

        private static bool CheckPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedDTraits)
            {
                if (str == data.DiplomaticPersonality.Name)
                    return false;
            }
            return true;
        }

        private static bool CheckEPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedETraits)
            {
                if (str == data.EconomicPersonality.Name)
                    return false;
            }
            return true;
        }

        public override bool HandleInput(InputState input)
        {
            if (!AllSystemsGenerated || !input.InGameSelect)
                return false;

            GameAudio.StopGenericMusic(immediate: false);
            
            us = new UniverseScreen(Data)
            {
                player         = PlayerEmpire,
                CamPos = new Vector3(-playerShip.Center.X, playerShip.Center.Y, 5000f),
                ScreenManager  = ScreenManager,
                GameDifficulty = Difficulty,
                GameScale      = Scale
            };

            EmpireShipBonuses.RefreshBonuses();

            UniverseScreen.GameScaleStatic = Scale;
            WorkerThread.Abort();
            WorkerThread = null;
            ScreenManager.AddScreen(us);

            Log.Info("CreatingNewGameScreen.UpdateAllSystems(0.01)");
            us.UpdateAllSystems(0.01f);
            mmscreen.OnPlaybackStopped(null, null);
            ScreenManager.RemoveScreen(mmscreen);
 
            ExitScreen();
            return true;
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);
            ScreenManager.SpriteBatch.Begin();
            int width  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            ScreenManager.SpriteBatch.Draw(LoadingScreenTexture, new Rectangle(width / 2 - 960, height / 2 - 540, 1920, 1080), Color.White);
            var r = new Rectangle(width / 2 - 150, height - 25, 300, 25);
            new ProgressBar(r)
            {
                Max = 100f,
                Progress = PercentLoaded * 100f
            }.Draw(ScreenManager.SpriteBatch);
            var position = new Vector2(ScreenCenter.X - 250f, (float)(r.Y - Fonts.Arial12Bold.MeasureString(text).Y - 5.0));
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, position, Color.White);
            if (AllSystemsGenerated)
            {
                PercentLoaded = 1f;
                position.Y = (float)(position.Y - Fonts.Pirulen16.LineSpacing - 10.0);
                string token = Localizer.Token(2108);
                position.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(token).X / 2f;

                GameTime gameTime = Game1.Instance.GameTime;
                var color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 
                    (byte)(Math.Abs(Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * byte.MaxValue));
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, token, position, color);
            }
            ScreenManager.SpriteBatch.End();
        }

        protected override void Destroy()
        {
            lock (this) {
                WorkerBeginEvent?.Dispose(ref WorkerBeginEvent);
                WorkerCompletedEvent?.Dispose(ref WorkerCompletedEvent);
                LoadingScreenTexture?.Dispose(ref LoadingScreenTexture);                
            }
            base.Destroy();
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
