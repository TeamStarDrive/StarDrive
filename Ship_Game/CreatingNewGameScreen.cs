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
    public class CreatingNewGameScreen : GameScreen
    {
        private Matrix worldMatrix = Matrix.Identity;
        private float scale = 1f;
        private Background bg = new Background();
        private int numSystems = 50;
        private List<Texture2D> TextureList = new List<Texture2D>();
        private Vector2 ShipPosition = new Vector2(340f, 450f);
        private bool firstRun = true;
        private List<string> UsedOpponents = new List<string>();
        private AutoResetEvent WorkerBeginEvent = new AutoResetEvent(false);
        private ManualResetEvent WorkerCompletedEvent = new ManualResetEvent(true);
        private List<Vector2> stars = new List<Vector2>();
        private List<Vector2> ClaimedSpots = new List<Vector2>();
        private Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 800f);
        private const float starsParallaxAmplitude = 2048f;
        //private Matrix view;
        //private Matrix projection;
        //private Model model;
        //private SceneObject shipSO;
        private RaceDesignScreen.GameMode mode;
        private Vector2 GalacticCenter;
        private Vector2 ScreenCenter;
        private UniverseData data;
        private string EmpireToRemoveName;
        private Empire playerEmpire;
        private UniverseData.GameDifficulty difficulty;
        private int numOpponents;
        private MainMenuScreen mmscreen;
        private DiplomaticTraits dtraits;
        private int whichAdvice;
        private int whichTexture;
        private FileInfo[] textList;
        private List<string> AdviceList;
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
        private float percentloaded;
        private int systemToMake;
        //private float Zrotate;

        public CreatingNewGameScreen(Empire empire, string size, float StarNumModifier, string EmpireToRemoveName, int numOpponents, RaceDesignScreen.GameMode gamemode, int GameScale, UniverseData.GameDifficulty difficulty, MainMenuScreen mmscreen)
        {
            
            {
                GlobalStats.RemnantArmageddon = false;
                GlobalStats.RemnantKills = 0;
                this.mmscreen = mmscreen;
                foreach (KeyValuePair<string, Ship_Game.Artifact> Artifact in Ship_Game.ResourceManager.ArtifactsDict)
                {
                    Artifact.Value.Discovered = false;
                }
                RandomEventManager.ActiveEvent = null;
                this.difficulty = difficulty;
                this.scale = (float)GameScale;
                if (this.scale == 5) this.scale = 8;
                if (this.scale == 6) this.scale = 16;
                this.mode = gamemode;
                this.numOpponents = numOpponents;
                this.EmpireToRemoveName = EmpireToRemoveName;
                EmpireManager.EmpireList.Clear();
                XmlSerializer serializer2 = new XmlSerializer(typeof(DiplomaticTraits));
                //Added by McShooterz: mod folder support
                this.dtraits = (DiplomaticTraits)serializer2.Deserialize((new FileInfo(File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml") : "Content/Diplomacy/DiplomaticTraits.xml")).OpenRead());
                Ship_Game.ResourceManager.LoadEncounters();
                this.playerEmpire = empire;
                empire.Initialize();
                empire.data.CurrentAutoColony = empire.data.DefaultColonyShip;
                empire.data.CurrentAutoFreighter = empire.data.DefaultSmallTransport;
                empire.data.CurrentAutoScout = empire.data.StartingScout;
                this.data = new UniverseData()
                {
                    FTLSpeedModifier = GlobalStats.FTLInSystemModifier,
                    GravityWells = GlobalStats.PlanetaryGravityWells
                };
                string str = size;
                string str1 = str;
                if (str != null)
                {
                    if (str1 == "Tiny")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(16f * StarNumModifier);
                        this.data.Size = new Vector2(3500000f, 3500000f);
                    }
                    else if (str1 == "Small")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(30f * StarNumModifier);
                        this.data.Size = new Vector2(7300000f, 7300000f);
                    }
                    else if (str1 == "Medium")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(50f * StarNumModifier);
                        this.data.Size = new Vector2(9350000f, 9350000f);
                    }
                    else if (str1 == "Large")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(75f * StarNumModifier);
                        this.data.Size = new Vector2(1.335E+07f, 1.335E+07f);
                    }
                    else if (str1 == "Huge")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(100f * StarNumModifier);
                        this.data.Size = new Vector2(1.8E+07f, 1.8E+07f);
                    }
                    else if (str1 == "Epic")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(100f * StarNumModifier);
                        this.data.Size = new Vector2(1.8E+07f * 2, 1.8E+07f * 2);
                        //this.scale = 2;

                        //this.numSystems = (int)(125f * StarNumModifier);
                        //this.data.Size = new Vector2(3.6E+07f, 3.6E+07f);

                    }
                    else if (str1 == "TrulyEpic")
                    {
                        if (mode == RaceDesignScreen.GameMode.Warlords)
                        {
                            this.numSystems = (int)(12 * StarNumModifier);
                        }
                        else
                            this.numSystems = (int)(150f * StarNumModifier);
                        //this.numSystems = (int)(100f * StarNumModifier);
                        this.data.Size = new Vector2(1.8E+07f * 3f, 1.8E+07f * 3f);
                        //this.data.Size = new Vector2(7.2E+07f, 7.2E+07f);
                        //this.scale = 4;


                    }
                    //if (this.numSystems <= this.numOpponents+2)
                    //{
                    //    this.numSystems = this.numOpponents + 2;
                    //}
                }
                UniverseData universeDatum = this.data;
                universeDatum.Size = universeDatum.Size * this.scale;
                this.data.EmpireList.Add(empire);
                EmpireManager.EmpireList.Add(empire);
                this.GalacticCenter = new Vector2(this.data.Size.X / 2f, this.data.Size.Y / 2f);
                StatTracker.SnapshotsDict.Clear();
            }
        }

        ~CreatingNewGameScreen()
        {
            this.Dispose(false);
        }

        private void SaveRace(Empire empire)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(EmpireData));
            EmpireData empireData = new EmpireData();
            empireData.Traits = empire.data.Traits;
            TextWriter textWriter = (TextWriter)new StreamWriter("Content/Races/test.xml");
            xmlSerializer.Serialize(textWriter, (object)empireData);
            textWriter.Close();
        }

        public override void LoadContent()
        {
            //Added by McShooterz: Enable LoadingScreen folder for mods
            this.textList = HelperFunctions.GetFilesFromDirectory(Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen") : "Content/LoadingScreen");
            //Added by McShooterz: mod support for Advice folder
            this.AdviceList = (List<string>)new XmlSerializer(typeof(List<string>)).Deserialize((Stream)new FileInfo(File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Advice/" + GlobalStats.Config.Language + "/Advice.xml")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Advice/" + GlobalStats.Config.Language + "/Advice.xml") : "Content/Advice/" + GlobalStats.Config.Language + "/Advice.xml").OpenRead());
            this.ScreenManager.inter.ObjectManager.Clear();
            this.ScreenManager.inter.LightManager.Clear();
            for (int index = 0; index < this.textList.Length; ++index)
                this.TextureList.Add(this.ScreenManager.Content.Load<Texture2D>("LoadingScreen/" + Path.GetFileNameWithoutExtension(this.textList[index].Name)));
            this.whichAdvice = (int)RandomMath.RandomBetween(0.0f, (float)this.AdviceList.Count);
            this.whichTexture = (int)RandomMath.RandomBetween(0.0f, (float)this.TextureList.Count);
            this.text = HelperFunctions.parseText(Fonts.Arial12Bold, this.AdviceList[this.whichAdvice], 500f);
            this.ScreenCenter = new Vector2((float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            this.WorkerThread = new Thread(new ThreadStart(this.Worker));
            this.WorkerThread.IsBackground = true;
            this.WorkerThread.Start();
            base.LoadContent();
        }

        private void Worker()
        {
            while (!this.ready)
            {
                if (this.firstRun)
                {
                    UniverseScreen.DeepSpaceManager = new SpatialManager();
                    BatchRemovalCollection<EmpireData> removalCollection = new BatchRemovalCollection<EmpireData>();
                    foreach (EmpireData empireData in ResourceManager.Empires)
                    {
                        if (!(empireData.Traits.Name == this.EmpireToRemoveName) && empireData.Faction == 0)
                            removalCollection.Add(empireData);
                    }
                    int num = removalCollection.Count - this.numOpponents;
                    for (int index1 = 0; index1 < num; ++index1)
                    {
                        int index2 = (int)RandomMath.RandomBetween(0.0f, (float)(removalCollection.Count + 1));
                        if (index2 > removalCollection.Count - 1)
                            index2 = removalCollection.Count - 1;
                        removalCollection.RemoveAt(index2);
                    }
                    foreach (EmpireData data in (List<EmpireData>)removalCollection)
                    {
                        Empire empireFromEmpireData = this.CreateEmpireFromEmpireData(data);
                        this.data.EmpireList.Add(empireFromEmpireData);
                        switch (this.difficulty)
                        {
                            case UniverseData.GameDifficulty.Easy:
                                empireFromEmpireData.data.Traits.ProductionMod -= 0.25f;
                                empireFromEmpireData.data.Traits.ResearchMod -= 0.25f;
                                empireFromEmpireData.data.Traits.TaxMod -= 0.25f;
                                empireFromEmpireData.data.Traits.ModHpModifier -= 0.25f;
                                break;
                            case UniverseData.GameDifficulty.Hard:
                                empireFromEmpireData.data.Traits.ProductionMod += 0.5f;
                                empireFromEmpireData.data.Traits.ResearchMod += 0.5f;
                                empireFromEmpireData.data.Traits.TaxMod += 0.5f;
                                empireFromEmpireData.data.Traits.ModHpModifier += 0.5f;
                                empireFromEmpireData.data.Traits.ShipCostMod -= 0.2f;
                                break;
                            case UniverseData.GameDifficulty.Brutal:
                                ++empireFromEmpireData.data.Traits.ProductionMod;
                                ++empireFromEmpireData.data.Traits.ResearchMod;
                                ++empireFromEmpireData.data.Traits.TaxMod;
                                ++empireFromEmpireData.data.Traits.ModHpModifier;
                                empireFromEmpireData.data.Traits.ShipCostMod -= 0.5f;
                                break;
                        }
                        EmpireManager.EmpireList.Add(empireFromEmpireData);
                    }
                    foreach (EmpireData data in ResourceManager.Empires)
                    {
                        if (data.Faction != 0)
                        {
                            Empire empireFromEmpireData = this.CreateEmpireFromEmpireData(data);
                            this.data.EmpireList.Add(empireFromEmpireData);
                            EmpireManager.EmpireList.Add(empireFromEmpireData);
                        }
                    }
                    foreach (Empire empire in this.data.EmpireList)
                    {
                        foreach (Empire e in this.data.EmpireList)
                        {
                            if (empire != e)
                                empire.AddRelationships(e, new Relationship(e.data.Traits.Name));
                        }
                    }
                    foreach (Empire Owner in this.data.EmpireList)
                    {
                        if (!Owner.isFaction)
                        {
                            SolarSystem solarSystem = new SolarSystem();
                            //Added by McShooterz: support for SolarSystems folder for mods
                            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/SolarSystems/", Owner.data.Traits.HomeSystemName, ".xml")))
                            {
                                solarSystem = SolarSystem.GenerateSystemFromDataNormalSize((SolarSystemData)new XmlSerializer(typeof(SolarSystemData)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/SolarSystems/", Owner.data.Traits.HomeSystemName, ".xml")).OpenRead()), Owner);
                            }
                            else if (File.Exists("Content/SolarSystems/" + Owner.data.Traits.HomeSystemName + ".xml"))
                            {
                                solarSystem = SolarSystem.GenerateSystemFromDataNormalSize((SolarSystemData)new XmlSerializer(typeof(SolarSystemData)).Deserialize((Stream)new FileInfo("Content/SolarSystems/" + Owner.data.Traits.HomeSystemName + ".xml").OpenRead()), Owner);
                                solarSystem.isStartingSystem = true;
                            }
                            else
                            {
                                solarSystem.GenerateStartingSystem(Owner.data.Traits.HomeSystemName, Owner, this.scale);
                                solarSystem.isStartingSystem = true;
                            }
                            this.data.SolarSystemsList.Add(solarSystem);
                            if (Owner == this.playerEmpire)
                                this.PlayerSystem = solarSystem;

                        }
                    }
                    MarkovNameGenerator markovNameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
                    SolarSystem solarSystem1 = new SolarSystem();
                    solarSystem1.GenerateCorsairSystem(markovNameGenerator.NextName);
                    solarSystem1.DontStartNearPlayer = true;
                    this.data.SolarSystemsList.Add(solarSystem1);
                    for (int index = 0; index < this.numSystems; ++index)
                    {
                        SolarSystem solarSystem2 = new SolarSystem();
                        solarSystem2.GenerateRandomSystem(markovNameGenerator.NextName, this.data, this.scale);
                        this.data.SolarSystemsList.Add(solarSystem2);
                        ++this.counter;
                        this.percentloaded = (float)(this.counter / (this.numSystems * 2));
                    }
                    new SolarSystem().GeneratePrisonAnomaly(markovNameGenerator.NextName);
                    this.ThrusterEffect = this.ScreenManager.Content.Load<Effect>("Effects/Thrust");
                    foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)
                    {
                        if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                            solarSystem2.Position = this.GenerateRandom(this.data.Size.X / 4f);
                    }
                    foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)
                    {
                        if (!solarSystem2.isStartingSystem && !solarSystem2.DontStartNearPlayer)
                            solarSystem2.Position = this.GenerateRandom(350000f);
                    }
                    int count = this.data.SolarSystemsList.Count;
                    this.firstRun = false;
                }
                this.data.SolarSystemsList[this.systemToMake].spatialManager.Setup((int)(200000.0 * (double)this.scale), (int)(200000.0 * (double)this.scale), (int)(100000.0 * (double)this.scale), this.data.SolarSystemsList[this.systemToMake].Position);
                this.percentloaded = (float)(this.counter + this.systemToMake) / (float)(this.data.SolarSystemsList.Count * 2);
                foreach (Empire key in this.data.EmpireList)
                    this.data.SolarSystemsList[this.systemToMake].ExploredDict.Add(key, false);
                foreach (Planet planet in this.data.SolarSystemsList[this.systemToMake].PlanetList)
                {
                    planet.system = this.data.SolarSystemsList[this.systemToMake];
                    planet.Position += this.data.SolarSystemsList[this.systemToMake].Position;
                    planet.InitializeUpdate();
                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)planet.SO);
                    foreach (Empire key in this.data.EmpireList)
                        planet.ExploredDict.Add(key, false);
                }
                foreach (Asteroid asteroid in (List<Asteroid>)this.data.SolarSystemsList[this.systemToMake].AsteroidsList)
                {
                    asteroid.Position3D.X += this.data.SolarSystemsList[this.systemToMake].Position.X;
                    asteroid.Position3D.Y += this.data.SolarSystemsList[this.systemToMake].Position.Y;
                    asteroid.Initialize();
                }
                foreach (Ship ship in (List<Ship>)this.data.SolarSystemsList[this.systemToMake].ShipList)
                {
                    ship.Position = ship.GetHome().Position + new Vector2(6000f, 2000f);
                    ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.Position, 0.0f));
                    ship.Initialize();
                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)ship.GetSO());
                    foreach (Thruster thruster in ship.GetTList())
                    {
                        thruster.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                        thruster.InitializeForViewing();
                    }
                }
                ++this.systemToMake;
                if (this.systemToMake == this.data.SolarSystemsList.Count)
                {
                    foreach (SolarSystem solarSystem1 in this.data.SolarSystemsList)
                    {
                        List<CreatingNewGameScreen.SysDisPair> list = new List<CreatingNewGameScreen.SysDisPair>();
                        foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)
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
                        foreach (CreatingNewGameScreen.SysDisPair sysDisPair in list)
                            solarSystem1.FiveClosestSystems.Add(sysDisPair.System);
                    }
                    foreach (Empire index in this.data.EmpireList)
                    {
                        if (!index.isFaction)
                        {
                            foreach (Planet planet1 in index.GetPlanets())
                            {
                                planet1.MineralRichness += GlobalStats.StartingPlanetRichness;
                                planet1.system.ExploredDict[index] = true;
                                planet1.ExploredDict[index] = true;
                                foreach (Planet planet2 in planet1.system.PlanetList)
                                    planet2.ExploredDict[index] = true;
                                if (planet1.system.OwnerList.Count == 0)
                                {
                                    planet1.system.OwnerList.Add(index);
                                    foreach (Planet planet2 in planet1.system.PlanetList)
                                        planet2.ExploredDict[index] = true;
                                }
                                if (planet1.HasShipyard)
                                {
                                    SpaceStation spaceStation = new SpaceStation();
                                    spaceStation.planet = planet1;
                                    planet1.Station = spaceStation;
                                    spaceStation.ParentSystem = planet1.system;
                                    spaceStation.LoadContent(this.ScreenManager);
                                }
                                string key1 = index.data.DefaultColonyShip;
                                if (GlobalStats.HardcoreRuleset)
                                    key1 = key1 + " STL";
                                Ship ship1 = ResourceManager.GetShip(key1);
                                ship1.Position = planet1.Position + new Vector2(-2000f, -2000f);
                                ship1.loyalty = index;
                                ship1.Initialize();
                                ship1.SetHome(planet1);
                                ship1.DoOrbit(planet1);
                                ship1.GetSO().World = Matrix.CreateTranslation(new Vector3(ship1.Position, 0.0f));
                                ship1.isInDeepSpace = false;
                                ship1.SetSystem(planet1.system);
                                planet1.system.ShipList.Add(ship1);
                                planet1.system.spatialManager.CollidableObjects.Add((GameplayObject)ship1);
                                this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)ship1.GetSO());
                                this.data.MasterShipList.Add(ship1);
                                index.AddShip(ship1);
                                foreach (Thruster thruster in ship1.GetTList())
                                {
                                    thruster.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                                    thruster.InitializeForViewing();
                                }
                                string key2 = index.data.StartingScout;
                                if (GlobalStats.HardcoreRuleset)
                                    key2 = key2 + " STL";
                                Ship ship2 = ResourceManager.GetShip(key2);
                                ship2.Position = planet1.Position + new Vector2(-2500f, -2000f);
                                ship2.loyalty = index;
                                ship2.Initialize();
                                ship2.SetHome(planet1);
                                ship2.DoOrbit(planet1);
                                ship2.GetSO().World = Matrix.CreateTranslation(new Vector3(ship2.Position, 0.0f));
                                ship2.isInDeepSpace = false;
                                ship2.SetSystem(planet1.system);
                                this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)ship2.GetSO());
                                this.data.MasterShipList.Add(ship2);
                                planet1.system.spatialManager.CollidableObjects.Add((GameplayObject)ship2);
                                planet1.system.ShipList.Add(ship2);
                                index.AddShip(ship2);
                                foreach (Thruster thruster in ship2.GetTList())
                                {
                                    thruster.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                                    thruster.InitializeForViewing();
                                }
                                if (index == this.playerEmpire)
                                {
                                    this.playerShip = ResourceManager.GetShip(this.playerEmpire.data.Traits.Prototype == 0 ? this.playerEmpire.data.StartingShip : this.playerEmpire.data.PrototypeShip);
                                    this.playerShip.Position = planet1.Position + new Vector2(350f, 0.0f);
                                    this.playerShip.Rotation = 0.0f;
                                    this.playerShip.SensorRange = 100000f;
                                    this.playerShip.loyalty = this.playerEmpire;
                                    this.playerShip.loyalty.AddShip(this.playerShip);
                                    this.playerShip.Initialize();
                                    this.playerShip.GetAI().State = AIState.ManualControl;
                                    this.playerShip.VanityName = "Perseverance";
                                    this.playerShip.GetSO().World = Matrix.CreateRotationY(this.playerShip.yBankAmount) * Matrix.CreateRotationZ(this.playerShip.Rotation) * Matrix.CreateTranslation(new Vector3(this.playerShip.Center, 0.0f));
                                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)this.playerShip.GetSO());
                                    planet1.system.spatialManager.CollidableObjects.Add((GameplayObject)this.playerShip);
                                    this.playerShip.isInDeepSpace = false;
                                    this.playerShip.SetSystem(planet1.system);
                                    planet1.system.ShipList.Add(this.playerShip);
                                    this.data.MasterShipList.Add(this.playerShip);
                                    foreach (Thruster thruster in this.playerShip.GetTList())
                                    {
                                        thruster.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                                        thruster.InitializeForViewing();
                                    }
                                    this.data.playerShip = this.playerShip;
                                    this.playerShip.PlayerShip = true;
                                    planet1.GovernorOn = false;
                                    planet1.colonyType = Planet.ColonyType.Colony;
                                }
                                else
                                {
                                    string str = index.data.StartingShip;
                                    if (GlobalStats.HardcoreRuleset)
                                        str = str + " STL";
                                    Ship ship3 = ResourceManager.GetShip(index.data.Traits.Prototype == 0 ? str : index.data.PrototypeShip);
                                    ship3.Position = planet1.Position + new Vector2(-2500f, -2000f);
                                    ship3.loyalty = index;
                                    ship3.Initialize();
                                    ship3.SetHome(planet1);
                                    ship3.DoOrbit(planet1);
                                    ship3.GetSO().World = Matrix.CreateTranslation(new Vector3(ship3.Position, 0.0f));
                                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)ship3.GetSO());
                                    this.data.MasterShipList.Add(ship3);
                                    index.AddShip(ship3);
                                    index.GetForcePool().Add(ship3);
                                    planet1.system.spatialManager.CollidableObjects.Add((GameplayObject)ship3);
                                    ship3.isInDeepSpace = false;
                                    ship3.SetSystem(planet1.system);
                                    planet1.system.ShipList.Add(ship3);
                                    foreach (Thruster thruster in ship3.GetTList())
                                    {
                                        thruster.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                                        thruster.InitializeForViewing();
                                    }
                                }
                            }
                        }
                    }
                    using (List<Empire>.Enumerator enumerator = this.data.EmpireList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Empire empire = enumerator.Current;
                            if (!empire.isFaction && empire.data.Traits.BonusExplored > 0)
                            {
                                IOrderedEnumerable<SolarSystem> orderedEnumerable = Enumerable.OrderBy<SolarSystem, float>((IEnumerable<SolarSystem>)this.data.SolarSystemsList, (Func<SolarSystem, float>)(system => Vector2.Distance(empire.GetPlanets()[0].Position, system.Position)));
                                for (int index = 0; index < (this.data.SolarSystemsList.Count >= 20 ? empire.data.Traits.BonusExplored : this.data.SolarSystemsList.Count); ++index)
                                {
                                    Enumerable.ElementAt<SolarSystem>((IEnumerable<SolarSystem>)orderedEnumerable, index).ExploredDict[empire] = true;
                                    foreach (Planet planet in Enumerable.ElementAt<SolarSystem>((IEnumerable<SolarSystem>)orderedEnumerable, index).PlanetList)
                                        planet.ExploredDict[empire] = true;
                                }
                            }
                        }
                    }
                    this.ready = true;
                }
            }
        }

        public Vector2 GenerateRandom(float spacing)
        {
            Vector2 sysPos = new Vector2(RandomMath.RandomBetween(100000f, this.data.Size.X - 100000f), RandomMath.RandomBetween(100000f, this.data.Size.Y - 100000f));
            if (this.SystemPosOK(sysPos, spacing))
            {
                this.ClaimedSpots.Add(sysPos);
                return sysPos;
            }
            else
            {
                while (!this.SystemPosOK(sysPos, spacing))
                    sysPos = new Vector2(RandomMath.RandomBetween(100000f, this.data.Size.X - 100000f), RandomMath.RandomBetween(100000f, this.data.Size.Y - 100000f));
                this.ClaimedSpots.Add(sysPos);
                return sysPos;
            }
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            Random random = new Random();
            Vector2 vector2 = this.GalacticCenter;
            float num1 = (float)((double)(2f / (float)numOfStars) * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow((double)this.data.Size.X - 0.0850000008940697 * (double)this.data.Size.X, (double)((float)index / (float)numOfStars));
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
                        sysPos.X = this.GalacticCenter.X + RandomMath.RandomBetween((float)(-(double)this.data.Size.X / 2.0 + 0.0850000008940697 * (double)this.data.Size.X), (float)((double)this.data.Size.X / 2.0 - 0.0850000008940697 * (double)this.data.Size.X));
                        sysPos.Y = this.GalacticCenter.Y + RandomMath.RandomBetween((float)(-(double)this.data.Size.X / 2.0 + 0.0850000008940697 * (double)this.data.Size.X), (float)((double)this.data.Size.X / 2.0 - 0.0850000008940697 * (double)this.data.Size.X));
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
            {
                if ((double)Vector2.Distance(vector2, sysPos) < 300000.0 * (double)this.scale || ((double)sysPos.X > (double)this.data.Size.X || (double)sysPos.Y > (double)this.data.Size.Y || ((double)sysPos.X < 0.0 || (double)sysPos.Y < 0.0)))
                    return false;
            }
            return flag;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            bool flag = true;
            foreach (Vector2 vector2 in this.ClaimedSpots)
            {
                if ((double)Vector2.Distance(vector2, sysPos) < (double)spacing || ((double)sysPos.X > (double)this.data.Size.X || (double)sysPos.Y > (double)this.data.Size.Y || ((double)sysPos.X < 0.0 || (double)sysPos.Y < 0.0)))
                    return false;
            }
            return flag;
        }

        public static EmpireData CopyEmpireData(EmpireData data)
        {
            EmpireData empireData = new EmpireData();
            empireData.ArmorPiercingBonus = data.ArmorPiercingBonus;
            empireData.BaseReproductiveRate = data.BaseReproductiveRate;
            empireData.BonusFighterLevels = data.BonusFighterLevels;
            empireData.CounterIntelligenceBudget = 0.0f;
            empireData.DefaultColonyShip = data.DefaultColonyShip;
            empireData.DefaultSmallTransport = data.DefaultSmallTransport;
            empireData.DiplomacyDialogPath = data.DiplomacyDialogPath;
            empireData.DiplomaticPersonality = data.DiplomaticPersonality;
            empireData.EconomicPersonality = data.EconomicPersonality;
            empireData.EmpireFertilityBonus = data.EmpireFertilityBonus;
            empireData.EmpireWideProductionPercentageModifier = data.EmpireWideProductionPercentageModifier;
            empireData.ExcludedDTraits = data.ExcludedDTraits;
            empireData.ExcludedETraits = data.ExcludedETraits;
            empireData.ExplosiveRadiusReduction = data.ExplosiveRadiusReduction;
            empireData.FlatMoneyBonus = 0.0f;
            empireData.FTLModifier = data.FTLModifier;
            empireData.FTLPowerDrainModifier = data.FTLPowerDrainModifier;
            empireData.FuelCellModifier = data.FuelCellModifier;
            empireData.Inhibitors = data.Inhibitors;
            empireData.MassModifier = data.MassModifier;
            empireData.MissileDodgeChance = data.MissileDodgeChance;
            empireData.MissileHPModifier = data.MissileHPModifier;
            empireData.OrdnanceEffectivenessBonus = data.OrdnanceEffectivenessBonus;
            empireData.OrdnanceShieldPenChance = data.OrdnanceShieldPenChance;
            empireData.Privatization = data.Privatization;
            empireData.SensorModifier = data.SensorModifier;
            empireData.SpyModifier = data.SpyModifier;
            empireData.SpoolTimeModifier = data.SpoolTimeModifier;
            empireData.StartingScout = data.StartingScout;
            empireData.StartingShip = data.StartingShip;
            empireData.SubLightModifier = data.SubLightModifier;
            empireData.TaxRate = data.TaxRate;
            empireData.TroopDescriptionIndex = data.TroopDescriptionIndex;
            empireData.TroopNameIndex = data.TroopNameIndex;
            empireData.Traits = new RacialTrait();
            empireData.Traits.Aquatic = data.Traits.Aquatic;
            empireData.Traits.Assimilators = data.Traits.Assimilators;
            empireData.Traits.B = 128f;
            empireData.Traits.Blind = data.Traits.Blind;
            empireData.Traits.BonusExplored = data.Traits.BonusExplored;
            empireData.Traits.Burrowers = data.Traits.Burrowers;
            empireData.Traits.ConsumptionModifier = data.Traits.ConsumptionModifier;
            empireData.Traits.Cybernetic = data.Traits.Cybernetic;
            empireData.Traits.DiplomacyMod = data.Traits.DiplomacyMod;
            empireData.Traits.DodgeMod = data.Traits.DodgeMod;
            empireData.Traits.EnergyDamageMod = data.Traits.EnergyDamageMod;
            empireData.Traits.FlagIndex = data.Traits.FlagIndex;
            empireData.Traits.G = 128f;
            empireData.Traits.GenericMaxPopMod = data.Traits.GenericMaxPopMod;
            empireData.Traits.GroundCombatModifier = data.Traits.GroundCombatModifier;
            empireData.Traits.InBordersSpeedBonus = data.Traits.InBordersSpeedBonus;
            empireData.Traits.MaintMod = data.Traits.MaintMod;
            empireData.Traits.Mercantile = data.Traits.Mercantile;
            empireData.Traits.Militaristic = data.Traits.Militaristic;
            empireData.Traits.Miners = data.Traits.Miners;
            empireData.Traits.ModHpModifier = data.Traits.ModHpModifier;
            empireData.Traits.PassengerModifier = data.Traits.PassengerModifier;
            empireData.Traits.ProductionMod = data.Traits.ProductionMod;
            empireData.Traits.R = 128f;
            empireData.Traits.RepairMod = data.Traits.RepairMod;
            empireData.Traits.ReproductionMod = data.Traits.ReproductionMod;
            empireData.Traits.PopGrowthMax = data.Traits.PopGrowthMax;
            empireData.Traits.PopGrowthMin = data.Traits.PopGrowthMin;
            empireData.Traits.ResearchMod = data.Traits.ResearchMod;
            empireData.Traits.ShipCostMod = data.Traits.ShipCostMod;
            empireData.Traits.ShipType = data.Traits.ShipType;
            empireData.Traits.Singular = data.RebelSing;
            empireData.Traits.Plural = data.RebelPlur;
            empireData.Traits.Spiritual = data.Traits.Spiritual;
            empireData.Traits.SpyMultiplier = data.Traits.SpyMultiplier;
            empireData.Traits.TaxMod = data.Traits.TaxMod;
            empireData.TurnsBelowZero = 0;
            return empireData;
        }

        public static Empire CreateRebelsFromEmpireData(EmpireData data, Empire parent)
        {
            Empire empire = new Empire();
            empire.isFaction = true;
            empire.data = CreatingNewGameScreen.CopyEmpireData(data);
            //Added by McShooterz: mod folder support
            DiplomaticTraits diplomaticTraits = (DiplomaticTraits)new XmlSerializer(typeof(DiplomaticTraits)).Deserialize((Stream)new FileInfo(File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml") : "Content/Diplomacy/DiplomaticTraits.xml").OpenRead());
            int index1 = (int)RandomMath.RandomBetween(0.0f, (float)diplomaticTraits.DiplomaticTraitsList.Count);
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index1];
            int index2 = (int)RandomMath.RandomBetween(0.0f, (float)diplomaticTraits.DiplomaticTraitsList.Count);
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index2];
            int index3 = (int)RandomMath.RandomBetween(0.0f, (float)diplomaticTraits.EconomicTraitsList.Count);
            empire.data.EconomicPersonality = diplomaticTraits.EconomicTraitsList[index3];
            int index4 = (int)RandomMath.RandomBetween(0.0f, (float)diplomaticTraits.EconomicTraitsList.Count);
            empire.data.EconomicPersonality = diplomaticTraits.EconomicTraitsList[index4];
            empire.data.SpyModifier = data.Traits.SpyMultiplier;
            empire.PortraitName = data.PortraitName;
            empire.EmpireColor = new Color(128, 128, 128, 256);
            empire.Initialize();
            return empire;
        }

        private Empire CreateEmpireFromEmpireData(EmpireData data)
        {
            Empire empire = new Empire();
            if (data.Faction == 1)
                empire.isFaction = true;
            int index1 = (int)RandomMath.RandomBetween(0.0f, (float)this.dtraits.DiplomaticTraitsList.Count);
            data.DiplomaticPersonality = this.dtraits.DiplomaticTraitsList[index1];
            while (!this.CheckPersonality(data))
            {
                int index2 = (int)RandomMath.RandomBetween(0.0f, (float)this.dtraits.DiplomaticTraitsList.Count);
                data.DiplomaticPersonality = this.dtraits.DiplomaticTraitsList[index2];
            }
            int index3 = (int)RandomMath.RandomBetween(0.0f, (float)this.dtraits.EconomicTraitsList.Count);
            data.EconomicPersonality = this.dtraits.EconomicTraitsList[index3];
            while (!this.CheckEPersonality(data))
            {
                int index2 = (int)RandomMath.RandomBetween(0.0f, (float)this.dtraits.EconomicTraitsList.Count);
                data.EconomicPersonality = this.dtraits.EconomicTraitsList[index2];
            }
            empire.data = data;
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
            if (!this.ready || !input.InGameSelect)
                return;
            this.Go();
        }

        public void Go()
        {
            this.ScreenManager.musicCategory.Stop(AudioStopOptions.AsAuthored);
            this.us = new UniverseScreen(this.data);
            this.us.player = this.playerEmpire;
            this.us.ScreenManager = this.ScreenManager;
            this.us.camPos = new Vector3(-this.playerShip.Center.X, this.playerShip.Center.Y, 5000f);
            this.us.GameDifficulty = this.difficulty;
            this.us.GameScale = this.scale;
            UniverseScreen.GameScaleStatic = this.scale;
            this.WorkerThread.Abort();
            this.WorkerThread = (Thread)null;
            this.ScreenManager.AddScreen((GameScreen)this.us);
            this.us.UpdateAllSystems(0.01f);
            this.mmscreen.OnPlaybackStopped((object)null, (EventArgs)null);
            this.ScreenManager.RemoveScreen((GameScreen)this.mmscreen);
            this.Dispose();
            this.ExitScreen();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            this.ScreenManager.GraphicsDevice.Clear(Color.Black);
            this.ScreenManager.SpriteBatch.Begin();
            this.ScreenManager.SpriteBatch.Draw(this.TextureList[this.whichTexture], new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080), Color.White);
            Rectangle r = new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 150, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 25, 300, 25);
            new ProgressBar(r)
            {
                Max = 100f,
                Progress = (this.percentloaded * 100f)
            }.Draw(this.ScreenManager.SpriteBatch);
            Vector2 position = new Vector2(this.ScreenCenter.X - 250f, (float)((double)r.Y - (double)Fonts.Arial12Bold.MeasureString(this.text).Y - 5.0));
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.text, position, Color.White);
            if (this.ready)
            {
                this.percentloaded = 1f;
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this) { }
                
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
