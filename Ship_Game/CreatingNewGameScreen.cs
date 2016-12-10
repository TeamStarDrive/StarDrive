// Type: Ship_Game.CreatingNewGameScreen
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;


namespace Ship_Game
{
    public sealed class CreatingNewGameScreen : GameScreen, IDisposable
    {
        //private Matrix worldMatrix = Matrix.Identity;
        private float Scale = 1f;
        //private Background bg = new Background();
        private int NumSystems = 50;
        //private List<Texture2D> TextureList = new List<Texture2D>();
        //private Vector2 ShipPosition = new Vector2(340f, 450f);
        private bool firstRun = true;
        //private List<string> UsedOpponents = new List<string>();
        private AutoResetEvent WorkerBeginEvent = new AutoResetEvent(false);
        private ManualResetEvent WorkerCompletedEvent = new ManualResetEvent(true);
        private List<Vector2> stars = new List<Vector2>();
        private List<Vector2> ClaimedSpots = new List<Vector2>();
        //private Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 800f);
        //private const float starsParallaxAmplitude = 2048f;
        //private Matrix view;
        //private Matrix projection;
        //private Model model;
        //private SceneObject shipSO;
        private RaceDesignScreen.GameMode Mode;
        private Vector2 GalacticCenter;
        private Vector2 ScreenCenter;
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
        //private BloomComponent bloomComponent;
        //private Starfield starfield;
        private int counter;
        private Ship playerShip;
        //private bool loading;
        private Thread WorkerThread;
        private UniverseScreen us;
        private bool ready;
        private float PercentLoaded;
        private int systemToMake;
        //private float Zrotate;

        public CreatingNewGameScreen(Empire empire, string universeSize, 
                float starNumModifier, string empireToRemoveName, 
                int numOpponents, RaceDesignScreen.GameMode gamemode, 
                int gameScale, UniverseData.GameDifficulty difficulty, MainMenuScreen mmscreen)
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
            empire.data.CurrentAutoColony    = empire.data.DefaultColonyShip;
            empire.data.CurrentAutoFreighter = empire.data.DefaultSmallTransport;
            empire.data.CurrentAutoScout     = empire.data.StartingScout;
            empire.data.CurrentConstructor   = empire.data.DefaultConstructor;
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
            if (size > 45)
                Empire.ProjectorRadius = Data.Size.X / 70; // reduce projector radius??
            System.Diagnostics.Debug.WriteLine("Empire.ProjectorRadius = {0}", Empire.ProjectorRadius);

            UniverseData.UniverseWidth = Data.Size.X * 2;
            Data.Size *= Scale;
            Data.EmpireList.Add(empire);
            EmpireManager.EmpireList.Add(empire);
            GalacticCenter = new Vector2(0f, 0f);  // Gretman (for new negative Map dimensions)
            StatTracker.SnapshotsDict.Clear();
        }

        ~CreatingNewGameScreen()
        {
            Dispose(false);
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
            // Refactored by RedFox
            ScreenManager.inter.ObjectManager.Clear();
            ScreenManager.inter.LightManager.Clear();

            LoadingScreenTexture = ResourceManager.LoadRandomLoadingScreen(ScreenManager.Content);
            string adviceString  = ResourceManager.LoadRandomAdvice();
            text = HelperFunctions.ParseText(Fonts.Arial12Bold, adviceString, 500f);

            ResourceManager.ScreenManager = ScreenManager;
            var present = ScreenManager.GraphicsDevice.PresentationParameters;
            ScreenCenter = 0.5f * new Vector2(present.BackBufferWidth, present.BackBufferHeight);
            WorkerThread = new Thread(Worker) { IsBackground = true };
            WorkerThread.Start();
            base.LoadContent();
        }

        private void Worker()
        {
            while (!ready)
            {
                if (firstRun)
                {
                    UniverseScreen.DeepSpaceManager = new SpatialManager();
                    var removalCollection = new BatchRemovalCollection<EmpireData>();
                    foreach (EmpireData empireData in ResourceManager.Empires)
                    {
                        if (empireData.Traits.Name != EmpireToRemoveName && empireData.Faction == 0 && !empireData.MinorRace)
                            removalCollection.Add(empireData);                        
                    }
                    int num = removalCollection.Count - NumOpponents;
                    int shipsPurged = 0;
                    float spaceSaved = GC.GetTotalMemory(true);
                    for (int opponents = 0; opponents < num; ++opponents)
                    {
                        //Intentionally using too high of a value here, because of the truncated decimal. -Gretman
                        int index = RandomMath.InRange(removalCollection.Count);

                        System.Diagnostics.Debug.WriteLine("Race excluded from game: " + removalCollection[index].PortraitName + "  (Index " + index + " of " + (removalCollection.Count - 1) + ")");
                        removalCollection.RemoveAt(index);
                    }

                    System.Diagnostics.Debug.WriteLine("Ships Purged:  {0}", shipsPurged);
                    System.Diagnostics.Debug.WriteLine("Memory purged: {0}", spaceSaved - GC.GetTotalMemory(true));
                                           
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
                                traits.ProductionMod  += 0.5f;
                                traits.ResearchMod    += 0.75f;
                                traits.TaxMod         += 0.5f;
                                traits.ShipCostMod    -= 0.2f;
                                break;
                            case UniverseData.GameDifficulty.Brutal:
                                empireFromEmpireData.data.FlatMoneyBonus += 50; // cheaty cheat
                                traits.ProductionMod += 1.0f;
                                traits.ResearchMod    = 2.0f;
                                traits.TaxMod        += 1.0f;
                                traits.ShipCostMod   -= 0.5f;
                                break;
                        }
                        EmpireManager.EmpireList.Add(empireFromEmpireData);
                    }
                    
                    foreach (EmpireData data in ResourceManager.Empires)
                    {
                        if (data.Faction == 0 && !data.MinorRace)
                            continue;
                        Empire empireFromEmpireData = CreateEmpireFromEmpireData(data);
                        Data.EmpireList.Add(empireFromEmpireData);
                        EmpireManager.EmpireList.Add(empireFromEmpireData);
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

                            float angerMod = (int)Difficulty * (90-empire.data.DiplomaticPersonality.Trustworthiness);
                            r.Anger_DiplomaticConflict = angerMod;
                            r.Anger_MilitaryConflict = 1;
                        }
                    }
                    ResourceManager.MarkShipDesignsUnlockable();                    
                    
                    System.Diagnostics.Debug.WriteLine("Ships Purged: " + shipsPurged.ToString());
                    System.Diagnostics.Debug.WriteLine("Memory purged: " + (spaceSaved - GC.GetTotalMemory(true)).ToString());

                    foreach (Empire empire in Data.EmpireList)
                    {
                        if (empire.isFaction || empire.MinorRace)
                            continue;

                        SolarSystem solarSystem;
                        SolarSystemData systemData = ResourceManager.LoadSolarSystemData(empire.data.Traits.HomeSystemName);
                        if (systemData == null)
                        {
                            solarSystem = new SolarSystem();
                            solarSystem.GenerateStartingSystem(empire.data.Traits.HomeSystemName, empire, Scale);
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
                        SolarSystem solarSystem = SolarSystem.GenerateSystemFromData(systemData, null);
                        solarSystem.DontStartNearPlayer = true; // Added by Gretman
                        Data.SolarSystemsList.Add(solarSystem);
                        systemCount++;
                    }
                    MarkovNameGenerator markovNameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
                    SolarSystem solarSystem1 = new SolarSystem();
                    solarSystem1.GenerateCorsairSystem(markovNameGenerator.NextName);
                    solarSystem1.DontStartNearPlayer = true;
                    Data.SolarSystemsList.Add(solarSystem1);
                    for (; systemCount < NumSystems; ++systemCount)
                    {
                        SolarSystem solarSystem2 = new SolarSystem();
                        solarSystem2.GenerateRandomSystem(markovNameGenerator.NextName, this.Data, this.Scale);
                        Data.SolarSystemsList.Add(solarSystem2);
                        ++counter;
                        PercentLoaded = counter / (float)(NumSystems * 2);
                    }

                    // This section added by Gretman
                    if (Mode != RaceDesignScreen.GameMode.Corners)
                    {
                        foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)
                        {
                            if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                                solarSystem2.Position = GenerateRandom(Data.Size.X / 4f);
                        }

                        foreach (SolarSystem solarSystem2 in Data.SolarSystemsList)    //Unaltered Vanilla stuff
                        {
                            if (!solarSystem2.isStartingSystem && !solarSystem2.DontStartNearPlayer)
                                solarSystem2.Position = GenerateRandom(350000f);
                        }
                    }
                    else
                    {
                        short whichcorner = (short)RandomMath.RandomBetween(0, 4); //So the player doesnt always end up in the same corner;
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
                                    float RandomoffsetX = RandomMath.RandomBetween(0, 19) / 100;   //Do want some variance in location, but still in the back
                                    float RandomoffsetY = RandomMath.RandomBetween(0, 19) / 100;
                                    float MinOffset = 0.04f;   //Minimum Offset
                                         //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                                    float CornerOffset = 0.75f;  //Additional Offset for being in corner
                                         //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                                    switch (whichcorner)
                                    {
                                        case 0:
                                            solarSystem2.Position = new Vector2(
                                                                    (-this.Data.Size.X + (this.Data.Size.X * (MinOffset + RandomoffsetX))),
                                                                    (-this.Data.Size.Y + (this.Data.Size.Y * (MinOffset + RandomoffsetX))));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 1:
                                            solarSystem2.Position = new Vector2(
                                                                    (this.Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                                                    (-this.Data.Size.Y + (this.Data.Size.Y * (MinOffset + RandomoffsetX))));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 2:
                                            solarSystem2.Position = new Vector2(
                                                                    (-this.Data.Size.X + (this.Data.Size.X * (MinOffset + RandomoffsetX))),
                                                                    (this.Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 3:
                                            solarSystem2.Position = new Vector2(
                                                                    (this.Data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                                                    (this.Data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                    }
                                }
                                else solarSystem2.Position = this.GenerateRandomCorners(whichcorner);   //This will distribute the extra planets from "/SolarSystems/Random" evenly
                                whichcorner += 1;
                                if (whichcorner > 3) whichcorner = 0;
                            }
                        }

                        foreach (SolarSystem solarSystem2 in this.Data.SolarSystemsList)
                        {
                            //This will distribute all the rest of the planets evenly
                            if (!solarSystem2.isStartingSystem && !solarSystem2.DontStartNearPlayer)
                            {
                                solarSystem2.Position = this.GenerateRandomCorners(whichcorner);
                                whichcorner += 1;   //Only change which corner if a system is actually created
                                if (whichcorner > 3) whichcorner = 0;
                            }
                        }
                    }// Done breaking stuff -- Gretman

                    ThrusterEffect = ScreenManager.Content.Load<Effect>("Effects/Thrust");
                    firstRun = false;
                }
                Data.SolarSystemsList[systemToMake].spatialManager.Setup((int)(200000.0 * (double)this.Scale), (int)(200000.0 * (double)this.Scale), (int)(100000.0 * (double)this.Scale), this.Data.SolarSystemsList[this.systemToMake].Position);
                PercentLoaded = (counter + systemToMake) / (float)(Data.SolarSystemsList.Count * 2);
                foreach (Empire key in Data.EmpireList)
                    Data.SolarSystemsList[systemToMake].ExploredDict.Add(key, false);
                foreach (Planet planet in Data.SolarSystemsList[systemToMake].PlanetList)
                {
                    planet.system = Data.SolarSystemsList[systemToMake];
                    planet.Position += Data.SolarSystemsList[systemToMake].Position;
                    planet.InitializeUpdate();
                    ScreenManager.inter.ObjectManager.Submit(planet.SO);
                    foreach (Empire key in Data.EmpireList)
                        planet.ExploredDict.Add(key, false);
                }
                foreach (Asteroid asteroid in Data.SolarSystemsList[systemToMake].AsteroidsList)
                {
                    asteroid.Position3D.X += Data.SolarSystemsList[systemToMake].Position.X;
                    asteroid.Position3D.Y += Data.SolarSystemsList[systemToMake].Position.Y;
                    asteroid.Initialize();
                    ScreenManager.inter.ObjectManager.Submit(asteroid.GetSO());
                }
                foreach (Moon moon in Data.SolarSystemsList[systemToMake].MoonList)
                {
                    moon.Initialize();
                    ScreenManager.inter.ObjectManager.Submit(moon.GetSO());
                }
                foreach (Ship ship in Data.SolarSystemsList[systemToMake].ShipList)
                {
                    ship.Position = ship.loyalty.GetPlanets()[0].Position + new Vector2(6000f, 2000f);
                    ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.Position, 0.0f));
                    ship.Initialize();
                    ScreenManager.inter.ObjectManager.Submit(ship.GetSO());
                    foreach (Thruster thruster in ship.GetTList())
                    {
                        thruster.load_and_assign_effects(ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                        thruster.InitializeForViewing();
                    }
                }
                ++this.systemToMake;
                if (this.systemToMake == this.Data.SolarSystemsList.Count)
                {
                    foreach (SolarSystem solarSystem1 in this.Data.SolarSystemsList)
                    {
                        List<CreatingNewGameScreen.SysDisPair> list = new List<CreatingNewGameScreen.SysDisPair>();
                        foreach (SolarSystem solarSystem2 in this.Data.SolarSystemsList)
                        {
                            if (solarSystem1 != solarSystem2)
                            {
                                float num1 = Vector2.Distance(solarSystem1.Position, solarSystem2.Position);
                                if (list.Count < 5)
                                {
                                    list.Add(new CreatingNewGameScreen.SysDisPair()
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
                                        if ((double)list[index2].Distance > (double)num2)
                                        {
                                            index1 = index2;
                                            num2 = list[index2].Distance;
                                        }
                                    }
                                    if ((double)num1 < (double)num2)
                                        list[index1] = new CreatingNewGameScreen.SysDisPair()
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
                    foreach (Empire empire in Data.EmpireList)
                    {
                        if (empire.isFaction || empire.MinorRace)
                            continue;

                        foreach (Planet planet in empire.GetPlanets())
                        {
                            planet.MineralRichness += GlobalStats.StartingPlanetRichness;
                            planet.system.ExploredDict[empire] = true;
                            planet.ExploredDict[empire] = true;

                            foreach (Planet p in planet.system.PlanetList)
                                p.ExploredDict[empire] = true;

                            if (planet.system.OwnerList.Count == 0)
                            {
                                planet.system.OwnerList.Add(empire);
                                foreach (Planet planet2 in planet.system.PlanetList)
                                    planet2.ExploredDict[empire] = true;
                            }
                            if (planet.HasShipyard)
                            {
                                SpaceStation spaceStation = new SpaceStation { planet = planet };
                                planet.Station = spaceStation;
                                spaceStation.ParentSystem = planet.system;
                                spaceStation.LoadContent(ScreenManager);
                            }

                            string colonyShip = empire.data.DefaultColonyShip;
                            if (GlobalStats.HardcoreRuleset) colonyShip += " STL";

                            // @todo This hack is here because SD has several tight coupling issues, need to fix loading order
                            ResourceManager.ScreenManager = ScreenManager;
                            Ship ship1 = ResourceManager.CreateShipAt(colonyShip, empire, planet, new Vector2(-2000, -2000), true);
                            Data.MasterShipList.Add(ship1);

                            string startingScout = empire.data.StartingScout;
                            if (GlobalStats.HardcoreRuleset) startingScout += " STL";

                            Ship ship2 = ResourceManager.CreateShipAt(startingScout, empire, planet, new Vector2(-2500, -2000), true);
                            Data.MasterShipList.Add(ship2);

                            if (empire == PlayerEmpire)
                            {
                                string starterShip = empire.data.Traits.Prototype == 0 ? empire.data.StartingShip : empire.data.PrototypeShip;

                                playerShip = ResourceManager.CreateShipAt(starterShip, empire, planet, new Vector2(350f, 0.0f), true);
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

                                Ship ship3 = ResourceManager.CreateShipAt(starterShip, empire, planet, new Vector2(-2500, -2000), true);
                                Data.MasterShipList.Add(ship3);

                                empire.AddShip(ship3);
                                empire.GetForcePool().Add(ship3);
                            }
                            // @todo Remove tight coupling hacks
                            ResourceManager.ScreenManager = null;
                        }
                    }
                    foreach (Empire empire in Data.EmpireList)
                    {
                        if (empire.isFaction || empire.data.Traits.BonusExplored <= 0)
                            continue;

                        var planet0 = empire.GetPlanets()[0];
                        var solarSystems = Data.SolarSystemsList;
                        var orderedEnumerable  = solarSystems.OrderBy(system => Vector2.Distance(planet0.Position, system.Position));
                        int numSystemsExplored = solarSystems.Count >= 20 ? empire.data.Traits.BonusExplored : solarSystems.Count;
                        for (int i = 0; i < numSystemsExplored; ++i)
                        {
                            var system = orderedEnumerable.ElementAt(i);
                            system.ExploredDict[empire] = true;
                            foreach (Planet planet in system.PlanetList)
                                planet.ExploredDict[empire] = true;
                        }
                    }
                    ready = true;
                }
            }
        }

        public Vector2 GenerateRandom(float spacing)
        {
            Vector2 sysPos = new Vector2(RandomMath.RandomBetween(-this.Data.Size.X + 100000f, this.Data.Size.X - 100000f), RandomMath.RandomBetween(-this.Data.Size.X + 100000f, this.Data.Size.Y - 100000f)); //Fixed to make use of negative map values -Gretman
            if (this.SystemPosOK(sysPos, spacing))
            {
                this.ClaimedSpots.Add(sysPos);
                return sysPos;
            }
            else
            {
                while (!this.SystemPosOK(sysPos, spacing))
                    sysPos = new Vector2(RandomMath.RandomBetween(-this.Data.Size.X + 100000f, this.Data.Size.X - 100000f), RandomMath.RandomBetween(-this.Data.Size.X + 100000f, this.Data.Size.Y - 100000f));
                this.ClaimedSpots.Add(sysPos);
                return sysPos;
            }
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
            bool flag = true;
            foreach (Vector2 vector2 in this.ClaimedSpots)
            {                                                                   //Updated to make use of the negative map values -Gretman
                if ((double)Vector2.Distance(vector2, sysPos) < 300000.0 * (double)this.Scale || ((double)sysPos.X > (double)this.Data.Size.X || (double)sysPos.Y > (double)this.Data.Size.Y || ((double)sysPos.X < -this.Data.Size.X || (double)sysPos.Y < -this.Data.Size.Y)))
                    return false;
            }
            return flag;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            bool flag = true;
            foreach (Vector2 vector2 in this.ClaimedSpots)
            {
                if ((double)Vector2.Distance(vector2, sysPos) < (double)spacing || ((double)sysPos.X > (double)this.Data.Size.X || (double)sysPos.Y > (double)this.Data.Size.Y || ((double)sysPos.X < -this.Data.Size.X || (double)sysPos.Y < -this.Data.Size.Y)))
                    return false;
            }
            return flag;
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

        public static Empire CreateRebelsFromEmpireData(EmpireData data, Empire parent)
        {
            Empire empire = new Empire
            {
                isFaction = true,
                data = CopyEmpireData(data)
            };
            //Added by McShooterz: mod folder support
            DiplomaticTraits diplomaticTraits = ResourceManager.DiplomaticTraits;
            int index1 = RandomMath.InRange(diplomaticTraits.DiplomaticTraitsList.Count);
            int index2 = RandomMath.InRange(diplomaticTraits.DiplomaticTraitsList.Count);
            int index3 = RandomMath.InRange(diplomaticTraits.EconomicTraitsList.Count);
            int index4 = RandomMath.InRange(diplomaticTraits.EconomicTraitsList.Count);
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index1];
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index2];
            empire.data.EconomicPersonality   = diplomaticTraits.EconomicTraitsList[index3];
            empire.data.EconomicPersonality   = diplomaticTraits.EconomicTraitsList[index4];
            empire.data.SpyModifier = data.Traits.SpyMultiplier;
            empire.PortraitName     = data.PortraitName;
            empire.EmpireColor      = new Color(128, 128, 128, 256);
            empire.Initialize();
            return empire;
        }

        private Empire CreateEmpireFromEmpireData(EmpireData data)
        {
            Empire empire = new Empire();
            if (data.Faction == 1)
                empire.isFaction = true;
            if (data.MinorRace)
                empire.MinorRace = true;
            int index1 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.DiplomaticTraitsList.Count);
            data.DiplomaticPersonality = this.DTraits.DiplomaticTraitsList[index1];
            while (!this.CheckPersonality(data))
            {
                int index2 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.DiplomaticTraitsList.Count);
                data.DiplomaticPersonality = this.DTraits.DiplomaticTraitsList[index2];
            }
            int index3 = (int)RandomMath.RandomBetween(0.0f, (float)this.DTraits.EconomicTraitsList.Count);
            data.EconomicPersonality = this.DTraits.EconomicTraitsList[index3];
            while (!this.CheckEPersonality(data))
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

        private bool CheckPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedDTraits)
            {
                if (str == data.DiplomaticPersonality.Name)
                    return false;
            }
            return true;
        }

        private bool CheckEPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedETraits)
            {
                if (str == data.EconomicPersonality.Name)
                    return false;
            }
            return true;
        }

        public override void HandleInput(InputState input)
        {
            if (!ready || !input.InGameSelect)
                return;
            Go();
        }

        public void Go()
        {
            ScreenManager.musicCategory.Stop(AudioStopOptions.AsAuthored);
            us = new UniverseScreen(Data)
            {
                player = PlayerEmpire,
                ScreenManager = ScreenManager,
                camPos = new Vector3(-playerShip.Center.X, playerShip.Center.Y, 5000f),
                GameDifficulty = Difficulty,
                GameScale = Scale
            };
            UniverseScreen.GameScaleStatic = Scale;
            WorkerThread.Abort();
            WorkerThread = null;
            ScreenManager.AddScreen(us);
            us.UpdateAllSystems(0.01f);
            mmscreen.OnPlaybackStopped(null, null);
            ScreenManager.RemoveScreen(mmscreen);
 
            //this.Dispose();
            ExitScreen();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            this.ScreenManager.GraphicsDevice.Clear(Color.Black);
            this.ScreenManager.SpriteBatch.Begin();
            this.ScreenManager.SpriteBatch.Draw(LoadingScreenTexture, new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080), Color.White);
            Rectangle r = new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 150, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 25, 300, 25);
            new ProgressBar(r)
            {
                Max = 100f,
                Progress = PercentLoaded * 100f
            }.Draw(this.ScreenManager.SpriteBatch);
            Vector2 position = new Vector2(this.ScreenCenter.X - 250f, (float)((double)r.Y - (double)Fonts.Arial12Bold.MeasureString(this.text).Y - 5.0));
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.text, position, Color.White);
            if (this.ready)
            {
                this.PercentLoaded = 1f;
                position.Y = (float)((double)position.Y - (double)Fonts.Pirulen16.LineSpacing - 10.0);
                string text = Localizer.Token(2108);
                position.X = this.ScreenCenter.X - Fonts.Pirulen16.MeasureString(text).X / 2f;
                Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, text, position, color);
            }
            this.ScreenManager.SpriteBatch.End();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this) {
                WorkerBeginEvent?.Dispose();
                WorkerCompletedEvent?.Dispose();
                LoadingScreenTexture?.Dispose();;
                us?.Dispose();

                WorkerBeginEvent = null;
                WorkerCompletedEvent = null;
                us = null;
            }
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
