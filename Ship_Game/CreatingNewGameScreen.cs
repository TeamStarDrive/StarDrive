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
                EmpireManager.Clear();
                XmlSerializer serializer2 = new XmlSerializer(typeof(DiplomaticTraits));
                //Added by McShooterz: mod folder support
                this.dtraits = (DiplomaticTraits)serializer2.Deserialize((new FileInfo(File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Diplomacy/DiplomaticTraits.xml") : "Content/Diplomacy/DiplomaticTraits.xml")).OpenRead());
                Ship_Game.ResourceManager.LoadEncounters();
                this.playerEmpire = empire;
                empire.Initialize();
                empire.data.CurrentAutoColony = empire.data.DefaultColonyShip;
                empire.data.CurrentAutoFreighter = empire.data.DefaultSmallTransport;
                empire.data.CurrentAutoScout = empire.data.StartingScout;
                empire.data.CurrentConstructor = empire.data.DefaultConstructor;
                this.data = new UniverseData()
                {
                    FTLSpeedModifier = GlobalStats.FTLInSystemModifier,
                    EnemyFTLSpeedModifier = GlobalStats.EnemyFTLInSystemModifier,                    
                    GravityWells = GlobalStats.PlanetaryGravityWells,
                    FTLinNeutralSystem = GlobalStats.WarpInSystem
                };
                string str = size;
                string str1 = str;
                if (str != null)
                {
                    if (str1 == "Tiny")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                            this.numSystems = (int)(16f * StarNumModifier); //16 is ok for Corners match, so no need to change -Gretman
                        this.data.Size = new Vector2(1750000);
                    }
                    else if (str1 == "Small")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                            this.numSystems = (int)(30f * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(28f * StarNumModifier); //Gretman
                        this.data.Size = new Vector2(3500000);
                    }
                    else if (str1 == "Medium")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                            this.numSystems = (int)(45f * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(40f * StarNumModifier); //Gretman
                        this.data.Size = new Vector2(5500000);
                        Empire.ProjectorRadius = (this.data.Size.X / 70);
                    }
                    else if (str1 == "Large")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                            this.numSystems = (int)(70f * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(64f * StarNumModifier); //Gretman
                        this.data.Size = new Vector2(9000000);
                            Empire.ProjectorRadius = (this.data.Size.X / 70);
                    }
                    else if (str1 == "Huge")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                            this.numSystems = (int)(92 * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(84f * StarNumModifier); //Gretman
                        this.data.Size = new Vector2(13500000);  //27,000,000
                            Empire.ProjectorRadius = (this.data.Size.X / 70);
                    }
                    else if (str1 == "Epic")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else //33554423  33,554,432.
                            this.numSystems = (int)(115 * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(104f * StarNumModifier); //Gretman
                        this.data.Size = new Vector2(20000000);
                            Empire.ProjectorRadius = (this.data.Size.X / 70);
                            //this.data.Size = new Vector2(36000000, 36000000);
                        //this.scale = 2;

                        //this.numSystems = (int)(125f * StarNumModifier);
                        //this.data.Size = new Vector2(3.6E+07f, 3.6E+07f);

                    }
                    else if (str1 == "TrulyEpic")
                    {
                        //if (mode == RaceDesignScreen.GameMode.Warlords)
                        //{
                        //    this.numSystems = (int)(12 * StarNumModifier);
                        //}
                        //else
                        this.numSystems = (int)(160 * StarNumModifier);
                        if (this.mode == RaceDesignScreen.GameMode.Corners) this.numSystems = (int)(140f * StarNumModifier); //I have resurrected the TrulyEpic Map size! -Gretman
                        this.data.Size = new Vector2(33554423);
                        //this.data.Size = new Vector2(7.2E+07f, 7.2E+07f);
                        //this.scale = 4;
                            Empire.ProjectorRadius = (this.data.Size.X / 70);

                    }
                    //if (this.numSystems <= this.numOpponents+2)
                    //{
                    //    this.numSystems = this.numOpponents + 2;
                    //}
                }                
                UniverseData.UniverseWidth = this.data.Size.X;
                UniverseData universeDatum = this.data;
                universeDatum.Size = universeDatum.Size * this.scale;
                this.data.EmpireList.Add(empire);
                EmpireManager.EmpireList.Add(empire);
                this.GalacticCenter = new Vector2(0f, 0f);  //Gretman (for new negative Map dimentions)
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
            {
                if(Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen")))
                    this.TextureList.Add(this.ScreenManager.Content.Load<Texture2D>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen/", Path.GetFileNameWithoutExtension(this.textList[index].Name))));
                else
                    this.TextureList.Add(this.ScreenManager.Content.Load<Texture2D>("LoadingScreen/" + Path.GetFileNameWithoutExtension(this.textList[index].Name)));
            }
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
                        if (!(empireData.Traits.Name == this.EmpireToRemoveName) && empireData.Faction == 0 && !empireData.MinorRace)
                            removalCollection.Add(empireData);                        
                    }
                    int num = removalCollection.Count - this.numOpponents;
                    int shipsPurged = 0;
                    float SpaceSaved = GC.GetTotalMemory(true);
                    for (int opponents = 0; opponents < num; ++opponents)
                    {
                        int index2 = (int)RandomMath.RandomBetween(0.0f, (float)(removalCollection.Count - 1));

                        if (false)
                        {
                            List<string> shipkill = new List<string>();

                            foreach (KeyValuePair<string, Ship> ship in ResourceManager.ShipsDict)
                            {
                                if (ship.Value.shipData.ShipStyle == removalCollection[index2].Traits.ShipType)
                                {
                                    bool killSwitch = true;
                                    foreach (Empire ebuild in EmpireManager.EmpireList)
                                    {
                                        if (ebuild.ShipsWeCanBuild.Contains(ship.Key))
                                            killSwitch = false;
                                        break;
                                    }


                                    if (killSwitch)
                                        foreach (Ship mship in this.data.MasterShipList)
                                        {
                                            if (ship.Key == mship.Name)
                                            {
                                                killSwitch = false;
                                                break;
                                            }
                                        }
                                    if (killSwitch)
                                    {
                                        shipsPurged++;

                                        // System.Diagnostics.Debug.WriteLine("Removed "+ship.Value.shipData.Role.ToString()+" : " + ship.Key + " from: " + ship.Value.shipData.ShipStyle);
                                        shipkill.Add(ship.Key);
                                    }
                                }
                            }
                            foreach (string shiptoclear in shipkill)
                            {
                                ResourceManager.ShipsDict.Remove(shiptoclear);
                            } 
                        }

                        System.Diagnostics.Debug.WriteLine("Race excluded from game: " + removalCollection[index2].PortraitName + "  (Index " + index2 + " of " + (removalCollection.Count - 1) + ")");
                        removalCollection.RemoveAt(index2);
                    }

                    System.Diagnostics.Debug.WriteLine("Ships Purged: " + shipsPurged.ToString());
                    System.Diagnostics.Debug.WriteLine("Memory purged: " + (SpaceSaved - GC.GetTotalMemory(true)).ToString());
                                           
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
                                empireFromEmpireData.data.FlatMoneyBonus += 10;
                                empireFromEmpireData.data.Traits.ProductionMod += 0.5f;
                                empireFromEmpireData.data.Traits.ResearchMod += 0.75f;
                                empireFromEmpireData.data.Traits.TaxMod += 0.5f;
                                //empireFromEmpireData.data.Traits.ModHpModifier += 0.5f;
                                empireFromEmpireData.data.Traits.ShipCostMod -= 0.2f;
                                break;
                            case UniverseData.GameDifficulty.Brutal:
                                empireFromEmpireData.data.FlatMoneyBonus += 50;
                                ++empireFromEmpireData.data.Traits.ProductionMod;
                                empireFromEmpireData.data.Traits.ResearchMod = 2.0f;
                                ++empireFromEmpireData.data.Traits.TaxMod;
                                //++empireFromEmpireData.data.Traits.ModHpModifier;
                                empireFromEmpireData.data.Traits.ShipCostMod -= 0.5f;
                                break;
                        }
                        EmpireManager.EmpireList.Add(empireFromEmpireData);
                    }
                    
                    foreach (EmpireData data in ResourceManager.Empires)
                    {
                        if (data.Faction != 0 || data.MinorRace)
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
                            {
                                Relationship r = new Relationship(e.data.Traits.Name);
                                empire.AddRelationships(e, r);
                                if(this.playerEmpire == e)
                                {                                    
                                    float angerMod = ((int)this.difficulty ) * (90-empire.data.DiplomaticPersonality.Trustworthiness);
                                    r.Anger_DiplomaticConflict = angerMod;
                                    //r.Anger_FromShipsInOurBorders = angerMod;
                                    r.Anger_MilitaryConflict = 1;
                                    //r.Anger_TerritorialConflict = angerMod;
                                }
                            }
                        }
                    }
                    ResourceManager.MarkShipDesignsUnlockable();                    
                    

                    System.Diagnostics.Debug.WriteLine("Ships Purged: " + shipsPurged.ToString());
                    System.Diagnostics.Debug.WriteLine("Memory purged: " + (SpaceSaved - GC.GetTotalMemory(true)).ToString());

                    foreach (Empire Owner in this.data.EmpireList)
                    {
                        if (!Owner.isFaction && !Owner.MinorRace)
                        {
                            SolarSystem solarSystem = new SolarSystem();
                            //Added by McShooterz: support for SolarSystems folder for mods
                            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/SolarSystems/", Owner.data.Traits.HomeSystemName, ".xml")))
                            {
                                solarSystem = SolarSystem.GenerateSystemFromData((SolarSystemData)new XmlSerializer(typeof(SolarSystemData)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/SolarSystems/", Owner.data.Traits.HomeSystemName, ".xml")).OpenRead()), Owner);
                                solarSystem.isStartingSystem = true;
                            }
                            else if (File.Exists("Content/SolarSystems/" + Owner.data.Traits.HomeSystemName + ".xml"))
                            {
                                solarSystem = SolarSystem.GenerateSystemFromData((SolarSystemData)new XmlSerializer(typeof(SolarSystemData)).Deserialize((Stream)new FileInfo("Content/SolarSystems/" + Owner.data.Traits.HomeSystemName + ".xml").OpenRead()), Owner);
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
                    int SystemCount = 0;
                    if (Directory.Exists(Ship_Game.ResourceManager.WhichModPath + "/SolarSystems/Random"))
                    {
                        foreach (string system in Directory.GetFiles(Ship_Game.ResourceManager.WhichModPath + "/SolarSystems/Random"))
                        {
                            if (SystemCount > this.numSystems)
                                break;
                            SolarSystem solarSystem = new SolarSystem();
                            solarSystem = SolarSystem.GenerateSystemFromData((SolarSystemData)new XmlSerializer(typeof(SolarSystemData)).Deserialize((Stream)new FileInfo(system).OpenRead()), null);
                            this.data.SolarSystemsList.Add(solarSystem);
                            solarSystem.DontStartNearPlayer = true; //Added by Gretman
                            SystemCount++;
                        }
                    }
                    MarkovNameGenerator markovNameGenerator = new MarkovNameGenerator(File.ReadAllText("Content/NameGenerators/names.txt"), 3, 5);
                    SolarSystem solarSystem1 = new SolarSystem();
                    solarSystem1.GenerateCorsairSystem(markovNameGenerator.NextName);
                    solarSystem1.DontStartNearPlayer = true;
                    this.data.SolarSystemsList.Add(solarSystem1);
                    for (; SystemCount < this.numSystems; ++SystemCount)
                    {
                        SolarSystem solarSystem2 = new SolarSystem();
                        solarSystem2.GenerateRandomSystem(markovNameGenerator.NextName, this.data, this.scale);
                        this.data.SolarSystemsList.Add(solarSystem2);
                        ++this.counter;
                        this.percentloaded = (float)(this.counter / (this.numSystems * 2));
                    }

                    //This section added by Gretman
                    if (this.mode != RaceDesignScreen.GameMode.Corners)
                    {
                        foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)
                        {
                            if (solarSystem2.isStartingSystem || solarSystem2.DontStartNearPlayer)
                                solarSystem2.Position = this.GenerateRandom(this.data.Size.X / 4f);
                        }

                        foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)    //Unaltered Vanilla stuff
                        {
                            if (!solarSystem2.isStartingSystem && !solarSystem2.DontStartNearPlayer)
                                solarSystem2.Position = this.GenerateRandom(350000f);
                        }
                    }
                    else
                    {
                        short whichcorner = (short)RandomMath.RandomBetween(0, 4); //So the player doesnt always end up in the same corner;
                        foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)    
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
                                                                    (-this.data.Size.X + (this.data.Size.X * (MinOffset + RandomoffsetX))),
                                                                    (-this.data.Size.Y + (this.data.Size.Y * (MinOffset + RandomoffsetX))));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 1:
                                            solarSystem2.Position = new Vector2(
                                                                    (this.data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                                                    (-this.data.Size.Y + (this.data.Size.Y * (MinOffset + RandomoffsetX))));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 2:
                                            solarSystem2.Position = new Vector2(
                                                                    (-this.data.Size.X + (this.data.Size.X * (MinOffset + RandomoffsetX))),
                                                                    (this.data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                        case 3:
                                            solarSystem2.Position = new Vector2(
                                                                    (this.data.Size.X * (MinOffset + RandomoffsetX + CornerOffset)),
                                                                    (this.data.Size.Y * (MinOffset + RandomoffsetX + CornerOffset)));
                                            this.ClaimedSpots.Add(solarSystem2.Position);
                                            break;
                                    }
                                }
                                else solarSystem2.Position = this.GenerateRandomCorners(whichcorner);   //This will distribute the extra planets from "/SolarSystems/Random" evenly
                                whichcorner += 1;
                                if (whichcorner > 3) whichcorner = 0;
                            }
                        }

                        foreach (SolarSystem solarSystem2 in this.data.SolarSystemsList)
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

                    int count = this.data.SolarSystemsList.Count;
                    this.ThrusterEffect = this.ScreenManager.Content.Load<Effect>("Effects/Thrust");
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
                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)asteroid.GetSO());
                }
                foreach (Moon moon in (List<Moon>)this.data.SolarSystemsList[this.systemToMake].MoonList)
                {
                    moon.Initialize();
                    this.ScreenManager.inter.ObjectManager.Submit((ISceneObject)moon.GetSO());
                }
                foreach (Ship ship in (List<Ship>)this.data.SolarSystemsList[this.systemToMake].ShipList)
                {
                    ship.Position = ship.loyalty.GetPlanets()[0].Position + new Vector2(6000f, 2000f);
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
                        if (!index.isFaction && !index.MinorRace)
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
                                //Added by McShooterz: Starting ship support for automatic naming
                                if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(ship1.loyalty.data.Traits.ShipType, ship1.shipData.Role))
                                    ship1.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(ship1.loyalty.data.Traits.ShipType, ship1.shipData.Role);
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
                                //Added by McShooterz: Starting ship support for automatic naming
                                if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(ship2.loyalty.data.Traits.ShipType, ship2.shipData.Role))
                                    ship2.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(ship2.loyalty.data.Traits.ShipType, ship2.shipData.Role);
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
                                    //this.playerShip.GetAI().State = AIState.ManualControl;
                                    this.playerShip.DoOrbit(planet1);
                                    //Added by McShooterz: Starting ship support for automatic naming
                                    if (GlobalStats.ActiveModInfo != null && Ship_Game.ResourceManager.ShipNames.CheckForName(this.playerShip.loyalty.data.Traits.ShipType, this.playerShip.shipData.Role))
                                        this.playerShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(this.playerShip.loyalty.data.Traits.ShipType, this.playerShip.shipData.Role);
                                    else
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
                                    // Doctor: I think commenting this should completely stop all the recognition of the starter ship being the 'controlled' ship for the pie menu.
                                    this.data.playerShip = this.playerShip;
                                    //this.playerShip.PlayerShip = true;
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
                                    //Added by McShooterz: Starting ship support for automatic naming
                                    if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(ship3.loyalty.data.Traits.ShipType, ship3.shipData.Role))
                                        ship3.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(ship3.loyalty.data.Traits.ShipType, ship3.shipData.Role);
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
            Vector2 sysPos = new Vector2(RandomMath.RandomBetween(-this.data.Size.X + 100000f, this.data.Size.X - 100000f), RandomMath.RandomBetween(-this.data.Size.X + 100000f, this.data.Size.Y - 100000f)); //Fixed to make use of negative map values -Gretman
            if (this.SystemPosOK(sysPos, spacing))
            {
                this.ClaimedSpots.Add(sysPos);
                return sysPos;
            }
            else
            {
                while (!this.SystemPosOK(sysPos, spacing))
                    sysPos = new Vector2(RandomMath.RandomBetween(-this.data.Size.X + 100000f, this.data.Size.X - 100000f), RandomMath.RandomBetween(-this.data.Size.X + 100000f, this.data.Size.Y - 100000f));
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

            float SizeX = this.data.Size.X * 2;     //Allow for new negative coordinates
            float SizeY = this.data.Size.Y * 2;

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
                sysPos = new Vector2(   RandomMath.RandomBetween(-this.data.Size.X + (float)offsetX, -this.data.Size.X + (float)(CornerSizeX + offsetX)),
                                        RandomMath.RandomBetween(-this.data.Size.Y + (float)offsetY, -this.data.Size.Y + (float)(CornerSizeY + offsetY)));
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
            {                                                                   //Updated to make use of the negative map values -Gretman
                if ((double)Vector2.Distance(vector2, sysPos) < 300000.0 * (double)this.scale || ((double)sysPos.X > (double)this.data.Size.X || (double)sysPos.Y > (double)this.data.Size.Y || ((double)sysPos.X < -this.data.Size.X || (double)sysPos.Y < -this.data.Size.Y)))
                    return false;
            }
            return flag;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            bool flag = true;
            foreach (Vector2 vector2 in this.ClaimedSpots)
            {
                if ((double)Vector2.Distance(vector2, sysPos) < (double)spacing || ((double)sysPos.X > (double)this.data.Size.X || (double)sysPos.Y > (double)this.data.Size.Y || ((double)sysPos.X < -this.data.Size.X || (double)sysPos.Y < -this.data.Size.Y)))
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
            empireData.DefaultTroopShip = data.DefaultTroopShip;
            if (string.IsNullOrEmpty(empireData.DefaultTroopShip))
            {
                empireData.DefaultTroopShip = empireData.PortraitName + " " + "Troop";
            }
            empireData.DefaultConstructor = data.DefaultConstructor;
            empireData.DefaultShipyard = data.DefaultShipyard;
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
            //Doctor: Armour Mass Mod
            empireData.ArmourMassModifier = data.ArmourMassModifier;
            empireData.MissileDodgeChance = data.MissileDodgeChance;
            empireData.MissileHPModifier = data.MissileHPModifier;
            empireData.OrdnanceEffectivenessBonus = data.OrdnanceEffectivenessBonus;
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
            empireData.PowerFlowMod = data.PowerFlowMod;
            empireData.ShieldPowerMod = data.ShieldPowerMod;
            //Doctor: Civilian Maint Mod
            empireData.CivMaintMod = data.CivMaintMod;

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
            if (data.MinorRace)
                empire.MinorRace = true;
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
            //Added by McShooterz: set values for alternate race file structure
            data.Traits.SetValues();
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
 
            //this.Dispose();
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

        protected void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this) {
                if (this.WorkerBeginEvent != null)
                    this.WorkerBeginEvent.Dispose();
                if (this.WorkerCompletedEvent != null)
                    this.WorkerCompletedEvent.Dispose();
                if (this.us != null)
                    this.us.Dispose();

                this.WorkerBeginEvent = null;
                this.WorkerCompletedEvent = null;
                this.us = null;
            }

                
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}
