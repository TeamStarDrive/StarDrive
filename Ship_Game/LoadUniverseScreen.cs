using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class LoadUniverseScreen : GameScreen, IDisposable
	{
		private Vector2 ScreenCenter;

		private UniverseData data;

		private SavedGame.UniverseSaveData savedData;

		private float GamePace;

		private float GameScale;

		private Vector2 camPos;

		private float camHeight;

		private string PlayerLoyalty;

		//private bool ReadyToRoll;

		private string text;

		private int whichAdvice;

		private int whichTexture;

		private FileInfo[] textList;

		private List<Texture2D> TextureList = new List<Texture2D>();

		private List<string> AdviceList;

		private Effect ThrusterEffect;

		private int systemToMake;

		private ManualResetEvent GateKeeper = new ManualResetEvent(false);

		private bool Loaded;

		private UniverseScreen us;

		private Ship playerShip;

		private float percentloaded;

		private bool ready;

		public LoadUniverseScreen(FileInfo activeFile)
		{
            
            GlobalStats.RemnantKills = 0;
			GlobalStats.RemnantArmageddon = false;
            GlobalStats.Statreset();
			BackgroundWorker bgw = new BackgroundWorker();
			bgw.DoWork += new DoWorkEventHandler(this.DecompressFile);
            
            GC.Collect();
			bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.LoadEverything);
			bgw.RunWorkerAsync(activeFile);
		}

		private Empire CreateEmpireFromEmpireSaveData(SavedGame.EmpireSaveData data)
		{
			Empire e = new Empire();
            //TempEmpireData  Tdata = new TempEmpireData();

			if (data.IsFaction)
			{
				e.isFaction = true;
			}
            if (data.isMinorRace)
            {
                e.MinorRace = true;
            }
			if (data.empireData == null)
			{
				e.data.Traits = data.Traits;
				e.EmpireColor = new Color((byte)data.Traits.R, (byte)data.Traits.G, (byte)data.Traits.B);
			}
			else
			{
                e.data = new EmpireData();
                //Tdata = data.empireData as TempEmpireData;
                
                foreach (string key in e.data.WeaponTags.Keys)
                {
                    if(data.empireData.WeaponTags.ContainsKey(key))
                        continue;
                    data.empireData.WeaponTags.Add(key,new WeaponTagModifier());
                }
                e.data = data.empireData;
                
               // e.data.
				e.data.ResearchQueue = data.empireData.ResearchQueue;
				e.ResearchTopic = data.ResearchTopic;
				if (e.ResearchTopic == null)
				{
					e.ResearchTopic = "";
				}
				e.dd = Ship_Game.ResourceManager.DDDict[e.data.DiplomacyDialogPath];
				e.PortraitName = e.data.PortraitName;
				e.EmpireColor = new Color((byte)e.data.Traits.R, (byte)e.data.Traits.G, (byte)e.data.Traits.B);
                if (data.CurrentAutoScout != null)
                    e.data.CurrentAutoScout = data.CurrentAutoScout;
                else
                    e.data.CurrentAutoScout = e.data.StartingScout;
                if (data.CurrentAutoFreighter != null)
                    e.data.CurrentAutoFreighter = data.CurrentAutoFreighter;
                else
                    e.data.CurrentAutoFreighter = e.data.DefaultSmallTransport;
                if (data.CurrentAutoColony != null)
                    e.data.CurrentAutoColony = data.CurrentAutoColony;
                else
                    e.data.CurrentAutoColony = e.data.DefaultColonyShip;
			}
			e.Initialize();
			e.Money = data.Money;
			e.Research = data.Research;
			e.GetGSAI().AreasOfOperations = data.AOs;
			foreach (TechEntry tech in data.TechTree)
			{
				if (!e.GetTDict().ContainsKey(tech.UID))
				{
					continue;
				}
                if (tech.AcquiredFrom != null)
                    e.GetTDict()[tech.UID].AcquiredFrom = tech.AcquiredFrom;
				if (tech.Unlocked)
				{
					e.UnlockTechFromSave(tech.UID);
				}
				e.GetTDict()[tech.UID].Progress = tech.Progress;
				e.GetTDict()[tech.UID].Discovered = tech.Discovered;
                e.GetTDict()[tech.UID].level = tech.level;
			}
			return e;
		}

		private Planet CreatePlanetFromPlanetSaveData(SavedGame.PlanetSaveData data)
		{
			Building building;
			Planet p = new Planet();
			if (data.Owner != "")
			{
				p.Owner = EmpireManager.GetEmpireByName(data.Owner);
				p.Owner.AddPlanet(p);
			}
			p.guid = data.guid;
			p.Name = data.Name;
			p.colonyType = data.ColonyType;
			if (!data.GovernorOn)
			{
				p.GovernorOn = data.GovernorOn;
				p.colonyType = Planet.ColonyType.Colony;
			}
			p.OrbitalAngle = data.OrbitalAngle;
			p.fs = data.FoodState;
			p.ps = data.ProdState;
			p.FoodLocked = data.FoodLock;
			p.ProdLocked = data.ProdLock;
			p.ResLocked = data.ResLock;
			p.OrbitalRadius = data.OrbitalDistance;
			p.Population = data.Population;
			p.MaxPopulation = data.PopulationMax;
			p.Fertility = data.Fertility;
			p.MineralRichness = data.Richness;
			p.TerraformPoints = data.TerraformPoints;
			p.hasRings = data.HasRings;
			p.planetType = data.WhichPlanet;
			p.ShieldStrengthCurrent = data.ShieldStrength;
			p.LoadAttributes();
			p.Crippled_Turns = data.Crippled_Turns;
			p.planetTilt = RandomMath.RandomBetween(45f, 135f);
			float scale = RandomMath.RandomBetween(1f, 2f);
			p.scale = scale;
			p.ObjectRadius = 100f * scale;
			foreach (Guid guid in data.StationsList)
			{
				p.Shipyards.Add(guid, new Ship());
			}
			p.FarmerPercentage = data.farmerPercentage;
			p.WorkerPercentage = data.workerPercentage;
			p.ResearcherPercentage = data.researcherPercentage;
			p.FoodHere = data.foodHere;
			p.ProductionHere = data.prodHere;
			if (p.hasRings)
			{
				p.ringTilt = RandomMath.RandomBetween(-80f, -45f);
			}
			foreach (SavedGame.PGSData d in data.PGSList)
			{
				int num = d.x;
				int num1 = d.y;
				int num2 = d.foodbonus;
				int num3 = d.prodbonus;
				int num4 = d.resbonus;
				if (d.building != null)
				{
					building = d.building;
				}
				else
				{
					building = null;
				}
				PlanetGridSquare pgs = new PlanetGridSquare(num, num1, num2, num3, num4, building, d.Habitable)
				{
					Biosphere = d.Biosphere
				};
				if (pgs.Biosphere)
				{
					p.BuildingList.Add(Ship_Game.ResourceManager.GetBuilding("Biospheres"));
				}
				p.TilesList.Add(pgs);
				foreach (Troop t in d.TroopsHere)
				{
					pgs.TroopsHere.Add(t);
					p.TroopsHere.Add(t);
					t.SetPlanet(p);
				}
				if (pgs.building == null)
				{
					continue;
				}
				p.BuildingList.Add(pgs.building);
				if (!pgs.building.isWeapon)
				{
					continue;
				}
				pgs.building.theWeapon = Ship_Game.ResourceManager.WeaponsDict[pgs.building.Weapon];
			}
			return p;
		}

		private SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData data)
		{
			SolarSystem system = new SolarSystem()
			{
				Name = data.Name,
				Position = data.Position,
				SunPath = data.SunPath,
				AsteroidsList = new BatchRemovalCollection<Asteroid>(),
                MoonList = new List<Moon>()
			};
			foreach (Asteroid roid in data.AsteroidsList)
			{
				roid.Initialize();
				system.AsteroidsList.Add(roid);
			}
            foreach (Moon moon in data.Moons)
            {
                moon.Initialize();
                system.MoonList.Add(moon);
            }
			foreach (Empire e in EmpireManager.EmpireList)
			{
				system.ExploredDict.Add(e, false);
			}
			foreach (string EmpireName in data.EmpiresThatKnowThisSystem)
			{
				system.ExploredDict[EmpireManager.GetEmpireByName(EmpireName)] = true;
			}
			system.RingList = new List<SolarSystem.Ring>();
			foreach (SavedGame.RingSave ring in data.RingList)
			{
				if (ring.Asteroids)
				{
					SolarSystem.Ring r1 = new SolarSystem.Ring()
					{
						Asteroids = true
					};
					system.RingList.Add(r1);
				}
				else
				{
					Planet p = this.CreatePlanetFromPlanetSaveData(ring.Planet);
					p.system = system;
					p.ParentSystem = system;
					p.Position = HelperFunctions.GeneratePointOnCircle(p.OrbitalAngle, system.Position, p.OrbitalRadius);
                    
					foreach (Building b in p.BuildingList)
					{
						if (b.Name != "Space Port")
						{
							continue;
						}
						p.Station = new SpaceStation()
						{
							planet = p,
							Position = p.Position,
							ParentSystem = p.system
                            
						};
						p.Station.LoadContent(base.ScreenManager);
						p.HasShipyard = true;
                        
					}
                    
					if (p.Owner != null && !system.OwnerList.Contains(p.Owner))
					{
						system.OwnerList.Add(p.Owner);
					}
					system.PlanetList.Add(p);
					foreach (Empire e in EmpireManager.EmpireList)
					{
						p.ExploredDict.Add(e, false);
					}
					foreach (string EmpireName in data.EmpiresThatKnowThisSystem)
					{
						p.ExploredDict[EmpireManager.GetEmpireByName(EmpireName)] = true;
					}
					SolarSystem.Ring r1 = new SolarSystem.Ring()
					{
						planet = p,
						Asteroids = false
					};
					system.RingList.Add(r1);
				}
			}
			return system;
		}

		private void DecompressFile(object info, DoWorkEventArgs e)
		{
			FileInfo activeFile = (FileInfo)e.Argument;
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo decompressed = new FileInfo(HelperFunctions.Decompress(activeFile));

            //XmlAttributes saveCompatibility = new XmlAttributes();
            XmlSerializer serializer1=null;// = new XmlSerializer();
            try
            {
                //XmlSerializer 
                    serializer1 = new XmlSerializer(typeof(SavedGame.UniverseSaveData));
            }
            catch
            {
                var attributeOverrides = new XmlAttributeOverrides();
                attributeOverrides.Add(typeof(SavedGame.SolarSystemSaveData), "MoonList", new XmlAttributes { XmlIgnore = true });
                serializer1 = new XmlSerializer(typeof(SavedGame.UniverseSaveData), attributeOverrides);
            }
			FileStream stream = decompressed.OpenRead();
			SavedGame.UniverseSaveData savedData = (SavedGame.UniverseSaveData)serializer1.Deserialize(stream);
			stream.Close();
			stream.Dispose();
			decompressed.Delete();
			GlobalStats.RemnantKills = savedData.RemnantKills;
			GlobalStats.RemnantArmageddon = savedData.RemnantArmageddon;
            
            GlobalStats.GravityWellRange = savedData.GravityWellRange;            
            GlobalStats.IconSize = savedData.IconSize;        
            GlobalStats.MemoryLimiter = savedData.MemoryLimiter;          
            GlobalStats.MinimumWarpRange = savedData.MinimumWarpRange;         
            GlobalStats.OptionIncreaseShipMaintenance = savedData.OptionIncreaseShipMaintenance;            
            GlobalStats.preventFederations = savedData.preventFederations;
            GlobalStats.EliminationMode = savedData.EliminationMode;
            if (savedData.TurnTimer == 0)
                savedData.TurnTimer = 5;
            GlobalStats.TurnTimer = savedData.TurnTimer;



			this.savedData = savedData;
			this.camPos = savedData.campos;
			this.camHeight = savedData.camheight;
			this.GamePace = savedData.GamePacing;
			UniverseScreen.GamePaceStatic = this.GamePace;
			this.GameScale = savedData.GameScale;
			this.PlayerLoyalty = savedData.PlayerLoyalty;
			UniverseScreen.GamePaceStatic = this.GamePace;
			UniverseScreen.GameScaleStatic = this.GameScale;
			RandomEventManager.ActiveEvent = null;
			StatTracker.SnapshotsDict.Clear();
			StatTracker.SnapshotsDict = savedData.Snapshots;
			this.GateKeeper.Set();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			base.ScreenManager.SpriteBatch.Begin();
			Rectangle ArtRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
			base.ScreenManager.SpriteBatch.Draw(this.TextureList[this.whichTexture], ArtRect, Color.White);
			Rectangle MeterBar = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 150, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 25, 300, 25);
			ProgressBar pb = new ProgressBar(MeterBar)
			{
				Max = 100f,
				Progress = this.percentloaded * 100f
			};
			pb.Draw(base.ScreenManager.SpriteBatch);
			Vector2 Cursor = new Vector2(this.ScreenCenter.X - 250f, (float)MeterBar.Y - Fonts.Arial12Bold.MeasureString(this.text).Y - 5f);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.text, Cursor, Color.White);
			if (this.ready)
			{
				this.percentloaded = 1f;
				Cursor.Y = Cursor.Y - (float)Fonts.Pirulen16.LineSpacing - 10f;
				string begin = "Click to Continue!";
				Cursor.X = this.ScreenCenter.X - Fonts.Pirulen16.MeasureString(begin).X / 2f;
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, begin, Cursor, flashColor);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~LoadUniverseScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public void Go()
		{
			foreach (Empire e in this.data.EmpireList)
			{
				e.GetGSAI().InitialzeAOsFromSave(this.data);
			}
			foreach (Ship ship in this.us.MasterShipList)
			{
				ship.UpdateSystem(0f);
				if (ship.loyalty != EmpireManager.GetEmpireByName(this.us.PlayerLoyalty))
				{
					if (ship.AddedOnLoad)
					{
						continue;
					}
					ship.loyalty.ForcePoolAdd(ship);
				}
				else
				{
					if (ship.GetAI().State != AIState.SystemDefender)
					{
						continue;
					}
					ship.loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
					ship.AddedOnLoad = true;
				}
			}
			base.ScreenManager.musicCategory.Stop(AudioStopOptions.AsAuthored);
			this.ExitScreen();
			this.us.EmpireUI.empire = this.us.player;
			base.ScreenManager.AddScreenNoLoad(this.us);
		}

		public override void HandleInput(InputState input)
		{
			if (this.ready && input.InGameSelect)
			{
				this.Go();
			}
		}

		public override void LoadContent()
		{
			this.ScreenCenter = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
			this.textList = HelperFunctions.GetFilesFromDirectory(Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen") : "Content/LoadingScreen");
			XmlSerializer serializer2 = new XmlSerializer(typeof(List<string>));
            //Added by McShooterz: mod folder support of Advice folder
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Advice/", GlobalStats.Config.Language, "/Advice.xml")))
            {
                this.AdviceList = (List<string>)serializer2.Deserialize((new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Advice/", GlobalStats.Config.Language, "/Advice.xml"))).OpenRead());
            }
            else
            {
                this.AdviceList = (List<string>)serializer2.Deserialize((new FileInfo(string.Concat("Content/Advice/", GlobalStats.Config.Language, "/Advice.xml"))).OpenRead());
            }
            //Added by McShooterz: fix to load game crash, not finding loading screen
			for (int i = 0; i < (int)this.textList.Length; i++)
			{
                Texture2D what;
                if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen")))
                    what = base.ScreenManager.Content.Load<Texture2D>(string.Concat("../",Ship_Game.ResourceManager.WhichModPath, "/LoadingScreen/", Path.GetFileNameWithoutExtension(this.textList[i].Name)));
                else
                    what = base.ScreenManager.Content.Load<Texture2D>(string.Concat("LoadingScreen/", Path.GetFileNameWithoutExtension(this.textList[i].Name)));
				this.TextureList.Add(what);
			}
			this.whichAdvice = (int)RandomMath.RandomBetween(0f, (float)this.AdviceList.Count);
			this.whichTexture = (int)RandomMath.RandomBetween(0f, (float)this.TextureList.Count);
			this.text = HelperFunctions.parseText(Fonts.Arial12Bold, this.AdviceList[this.whichAdvice], 500f);
			base.LoadContent();
		}

		private void LoadEverything(object sender, RunWorkerCompletedEventArgs ev)
		{
			bool stop;
			List<SolarSystem>.Enumerator enumerator;
			base.ScreenManager.inter.ObjectManager.Clear();
			this.data = new UniverseData();
			RandomEventManager.ActiveEvent = this.savedData.RandomEvent;
			UniverseScreen.DeepSpaceManager = new SpatialManager();
			this.ThrusterEffect = base.ScreenManager.Content.Load<Effect>("Effects/Thrust");
			int count = this.data.SolarSystemsList.Count;
			this.data.loadFogPath = this.savedData.FogMapName;
			this.data.difficulty = UniverseData.GameDifficulty.Normal;
			this.data.difficulty = this.savedData.gameDifficulty;
			this.data.Size = this.savedData.Size;
			this.data.FTLSpeedModifier = this.savedData.FTLModifier;
            this.data.EnemyFTLSpeedModifier = this.savedData.EnemyFTLModifier;
			this.data.GravityWells = this.savedData.GravityWells;
			EmpireManager.EmpireList.Clear();
            if (Empire.universeScreen!=null && Empire.universeScreen.MasterShipList != null)
                Empire.universeScreen.MasterShipList.Clear();
            
         
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e =new Empire();
                e.data = new EmpireData();
                    e= this.CreateEmpireFromEmpireSaveData(d);
				this.data.EmpireList.Add(e);
				if (e.data.Traits.Name == this.PlayerLoyalty)
				{
					e.AutoColonize = this.savedData.AutoColonize;
					e.AutoExplore = this.savedData.AutoExplore;
					e.AutoFreighters = this.savedData.AutoFreighters;
					e.AutoBuild = this.savedData.AutoProjectors;
				}
				EmpireManager.EmpireList.Add(e);
			}
			foreach (Empire e in this.data.EmpireList)
			{
				if (e.data.AbsorbedBy == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, TechEntry> tech in EmpireManager.GetEmpireByName(e.data.AbsorbedBy).GetTDict())
				{
					if (!tech.Value.Unlocked)
					{
						continue;
					}
					EmpireManager.GetEmpireByName(e.data.AbsorbedBy).UnlockHullsSave(tech.Key, e.data.Traits.ShipType);
				}
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.Name);
				foreach (Relationship r in d.Relations)
				{
					e.GetRelations().Add(EmpireManager.GetEmpireByName(r.Name), r);
					if (r.ActiveWar == null)
					{
						continue;
					}
					r.ActiveWar.SetCombatants(e, EmpireManager.GetEmpireByName(r.Name));
				}
			}
			this.data.SolarSystemsList = new List<SolarSystem>();
			foreach (SavedGame.SolarSystemSaveData sdata in this.savedData.SolarSystemDataList)
			{
				SolarSystem system = this.CreateSystemFromData(sdata);
				system.guid = sdata.guid;
				this.data.SolarSystemsList.Add(system);
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.empireData.Traits.Name);
				foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
				{
					Ship ship = Ship.LoadSavedShip(shipData.data);
					ship.guid = shipData.guid;
					ship.Name = shipData.Name;
                    if(shipData.VanityName != "")
                        ship.VanityName = shipData.VanityName;
                    else
                        ship.VanityName = shipData.Name;
					if (ship.Role == "troop")
					{
						if (shipData.TroopList.Count > 0)
						{
							ship.VanityName = shipData.TroopList[0].Name;
						}
					}
					ship.Position = shipData.Position;
					if (shipData.IsPlayerShip)
					{
						this.playerShip = ship;
						this.playerShip.PlayerShip = true;
						this.data.playerShip = this.playerShip;
					}
					ship.experience = shipData.experience;
					ship.kills = shipData.kills;
					if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(shipData.Name))
					{
						shipData.data.Hull = shipData.Hull;
						Ship newShip = Ship.CreateShipFromShipData(shipData.data);
						newShip.SetShipData(shipData.data);
						if (!newShip.InitForLoad())
						{
							continue;
						}
						newShip.InitializeStatus();
						newShip.IsPlayerDesign = false;
						newShip.FromSave = true;
						Ship_Game.ResourceManager.ShipsDict.Add(shipData.Name, newShip);
					}				
                    else if (Ship_Game.ResourceManager.ShipsDict[shipData.Name].FromSave)
					{
						ship.IsPlayerDesign = false;
						ship.FromSave = true;


					}
                    float oldbasestr = ship.BaseStrength;
                    float newbasestr = ResourceManager.CalculateBaseStrength(ship);
                    //if (oldbasestr==0&& (ship.Name !="Subspace Projector" &&ship.Role !="troop"&&ship.Role !="freighter"))
                    //{
                    //    System.Diagnostics.Debug.WriteLine(ship.Name);
                    //    System.Diagnostics.Debug.WriteLine("BaseStrength: " + oldbasestr);
                    //    System.Diagnostics.Debug.WriteLine("NewStrength: " + newbasestr);
                    //    System.Diagnostics.Debug.WriteLine("");
                        
                    //}
                    ship.BaseStrength = newbasestr;

                    foreach(ModuleSlotData moduleSD in shipData.data.ModuleSlotList)
                    {
                        ShipModule mismatch =null;
                        bool exists =ResourceManager.ShipModulesDict.TryGetValue(moduleSD.InstalledModuleUID,out mismatch);
                        if (exists)
                            continue;
                        System.Diagnostics.Debug.WriteLine(string.Concat("mismatch =", moduleSD.InstalledModuleUID));
                    }


					ship.PowerCurrent = shipData.Power;
					ship.yRotation = shipData.yRotation;
					ship.Ordinance = shipData.Ordnance;
					ship.Rotation = shipData.Rotation;
					ship.Velocity = shipData.Velocity;
					ship.isSpooling = shipData.AfterBurnerOn;
					ship.InCombatTimer = shipData.InCombatTimer;
					foreach (Troop t in shipData.TroopList)
					{
						t.SetOwner(EmpireManager.GetEmpireByName(t.OwnerString));
						ship.TroopList.Add(t);
					}
					ship.TetherGuid = shipData.TetheredTo;
					ship.TetherOffset = shipData.TetherOffset;
					if (ship.InCombatTimer > 0f)
					{
						ship.InCombat = true;
					}
					ship.loyalty = e;
					ship.InitializeAI();
					ship.GetAI().CombatState = shipData.data.CombatState;
					ship.GetAI().FoodOrProd = shipData.AISave.FoodOrProd;
					ship.GetAI().State = shipData.AISave.state;
					ship.GetAI().DefaultAIState = shipData.AISave.defaultstate;
					ship.GetAI().GotoStep = shipData.AISave.GoToStep;
					ship.GetAI().MovePosition = shipData.AISave.MovePosition;
					ship.GetAI().OrbitTargetGuid = shipData.AISave.OrbitTarget;
					ship.GetAI().ColonizeTargetGuid = shipData.AISave.ColonizeTarget;
					ship.GetAI().TargetGuid = shipData.AISave.AttackTarget;
					ship.GetAI().SystemToDefendGuid = shipData.AISave.SystemToDefend;
					ship.GetAI().EscortTargetGuid = shipData.AISave.EscortTarget;
					bool hasCargo = false;
					if (shipData.FoodCount > 0f)
					{
						ship.AddGood("Food", (int)shipData.FoodCount);
						ship.GetAI().FoodOrProd = "Food";
						hasCargo = true;
					}
					if (shipData.ProdCount > 0f)
					{
						ship.AddGood("Production", (int)shipData.ProdCount);
						ship.GetAI().FoodOrProd = "Prod";
						hasCargo = true;
					}
					if (shipData.PopCount > 0f)
					{
						ship.AddGood("Colonists_1000", (int)shipData.PopCount);
					}
					AIState state = ship.GetAI().State;
					if (state == AIState.SystemTrader)
					{
						ship.GetAI().OrderTradeFromSave(hasCargo, shipData.AISave.startGuid, shipData.AISave.endGuid);
					}
					else if (state == AIState.PassengerTransport)
					{
						ship.GetAI().OrderTransportPassengersFromSave();
					}
					e.AddShip(ship);
					foreach (SavedGame.ProjectileSaveData pdata in shipData.Projectiles)
					{
						Weapon w = Ship_Game.ResourceManager.GetWeapon(pdata.Weapon);
						Projectile p = w.LoadProjectiles(pdata.Velocity, ship);
						p.Velocity = pdata.Velocity;
						p.Position = pdata.Position;
						p.Center = pdata.Position;
						p.duration = pdata.Duration;
						ship.Projectiles.Add(p);
					}
					this.data.MasterShipList.Add(ship);
				}
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.Name);
				foreach (SavedGame.FleetSave fleetsave in d.FleetsList)
				{
					Ship_Game.Gameplay.Fleet fleet = new Ship_Game.Gameplay.Fleet()
					{
						guid = fleetsave.FleetGuid,
						IsCoreFleet = fleetsave.IsCoreFleet,
						facing = fleetsave.facing
					};
					foreach (SavedGame.FleetShipSave ssave in fleetsave.ShipsInFleet)
					{
						foreach (Ship ship in this.data.MasterShipList)
						{
							if (ship.guid != ssave.shipGuid)
							{
								continue;
							}
							ship.RelativeFleetOffset = ssave.fleetOffset;
							fleet.AddShip(ship);
						}
					}
					foreach (FleetDataNode node in fleetsave.DataNodes)
					{
						fleet.DataNodes.Add(node);
					}
					foreach (FleetDataNode node in fleet.DataNodes)
					{
						foreach (Ship ship in fleet.Ships)
						{
							if (!(node.ShipGuid != Guid.Empty) || !(ship.guid == node.ShipGuid))
							{
								continue;
							}
							node.SetShip(ship);
							node.ShipName = ship.Name;
							break;
						}
					}
					fleet.AssignPositions(fleet.facing);
					fleet.Name = fleetsave.Name;
					fleet.TaskStep = fleetsave.TaskStep;
					fleet.Owner = e;
					fleet.Position = fleetsave.Position;
					if (e.GetFleetsDict().ContainsKey(fleetsave.Key))
					{
						e.GetFleetsDict()[fleetsave.Key] = fleet;
					}
					else
					{
						e.GetFleetsDict().Add(fleetsave.Key, fleet);
					}
					e.GetFleetsDict()[fleetsave.Key].SetSpeed();
				}
				foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
				{
					foreach (Ship ship in e.GetShips())
					{
						if (ship.Position != shipData.Position)
						{
							continue;
						}
					}
				}
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.Name);
                e.SpaceRoadsList = new List<SpaceRoad>();
				foreach (SavedGame.SpaceRoadSave roadsave in d.SpaceRoadData)
				{
					SpaceRoad road = new SpaceRoad();
					foreach (SolarSystem s in this.data.SolarSystemsList)
					{
						if (roadsave.OriginGUID == s.guid)
						{
							road.SetOrigin(s);
						}
						if (roadsave.DestGUID != s.guid)
						{
							continue;
						}
						road.SetDestination(s);
					}
					foreach (SavedGame.RoadNodeSave nodesave in roadsave.RoadNodes)
					{
						RoadNode node = new RoadNode();
						foreach (Ship s in this.data.MasterShipList)
						{
							if (nodesave.Guid_Platform != s.guid)
							{
								continue;
							}
							node.Platform = s;
						}
						node.Position = nodesave.Position;
						road.RoadNodesList.Add(node);
					}
					e.SpaceRoadsList.Add(road);
				}
				foreach (SavedGame.GoalSave gsave in d.GSAIData.Goals)
				{
					Goal g = new Goal()
					{
						empire = e,
						type = gsave.type
					};
					if (g.type == GoalType.BuildShips && gsave.ToBuildUID != null && !Ship_Game.ResourceManager.ShipsDict.ContainsKey(gsave.ToBuildUID))
					{
						continue;
					}
					g.ToBuildUID = gsave.ToBuildUID;
					g.Step = gsave.GoalStep;
					g.guid = gsave.GoalGuid;
					g.GoalName = gsave.GoalName;
					g.BuildPosition = gsave.BuildPosition;
					if (gsave.fleetGuid != Guid.Empty)
					{
						foreach (KeyValuePair<int, Ship_Game.Gameplay.Fleet> Fleet in e.GetFleetsDict())
						{
							if (Fleet.Value.guid != gsave.fleetGuid)
							{
								continue;
							}
							g.SetFleet(Fleet.Value);
						}
					}
					foreach (SolarSystem s in this.data.SolarSystemsList)
					{
						foreach (Planet p in s.PlanetList)
						{
							if (p.guid == gsave.planetWhereBuildingAtGuid)
							{
								g.SetPlanetWhereBuilding(p);
							}
							if (p.guid != gsave.markedPlanetGuid)
							{
								continue;
							}
							g.SetMarkedPlanet(p);
						}
					}
					foreach (Ship s in this.data.MasterShipList)
					{
						if (gsave.colonyShipGuid == s.guid)
						{
							g.SetColonyShip(s);
						}
						if (gsave.beingBuiltGUID != s.guid)
						{
							continue;
						}
						g.SetBeingBuilt(s);
					}
					e.GetGSAI().Goals.Add(g);
				}
				for (int i = 0; i < d.GSAIData.PinGuids.Count; i++)
				{
					e.GetGSAI().ThreatMatrix.Pins.TryAdd(d.GSAIData.PinGuids[i], d.GSAIData.PinList[i]);
				}
				e.GetGSAI().UsedFleets = d.GSAIData.UsedFleets;
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in d.GSAIData.MilitaryTaskList)
					{
						task.SetEmpire(e);
						e.GetGSAI().TaskList.Add(task);
						if (task.TargetPlanetGuid != Guid.Empty)
						{
							enumerator = this.data.SolarSystemsList.GetEnumerator();
							try
							{
								do
								{
									if (!enumerator.MoveNext())
									{
										break;
									}
									SolarSystem s = enumerator.Current;
									stop = false;
									foreach (Planet p in s.PlanetList)
									{
										if (p.guid != task.TargetPlanetGuid)
										{
											continue;
										}
										task.SetTargetPlanet(p);
										stop = true;
										break;
									}
								}
								while (!stop);
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
						}
						foreach (Guid guid in task.HeldGoals)
						{
							foreach (Goal g in e.GetGSAI().Goals)
							{
								if (g.guid != guid)
								{
									continue;
								}
								g.Held = true;
							}
						}
						try
						{
							if (task.WhichFleet != -1)
							{
								e.GetFleetsDict()[task.WhichFleet].Task = task;
							}
						}
						catch
						{
							task.WhichFleet = 0;
						}
					}
				}
				foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
				{
					foreach (Ship ship in this.data.MasterShipList)
					{
						if (ship.guid != shipData.guid)
						{
							continue;
						}
						foreach (Vector2 waypoint in shipData.AISave.ActiveWayPoints)
						{
							ship.GetAI().ActiveWayPoints.Enqueue(waypoint);
						}
						foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
						{
							ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(sg.Plan, sg.MovePosition, sg.FacingVector);
							foreach (SolarSystem s in this.data.SolarSystemsList)
							{
								foreach (Planet p in s.PlanetList)
								{
									if (sg.TargetPlanetGuid == p.guid)
									{
										g.TargetPlanet = p;
										ship.GetAI().ColonizeTarget = p;
									}
									if (p.guid == shipData.AISave.startGuid)
									{
										ship.GetAI().start = p;
									}
									if (p.guid != shipData.AISave.endGuid)
									{
										continue;
									}
									ship.GetAI().end = p;
								}
							}
							if (sg.fleetGuid != Guid.Empty)
							{
								foreach (KeyValuePair<int, Ship_Game.Gameplay.Fleet> fleet in e.GetFleetsDict())
								{
									if (fleet.Value.guid != sg.fleetGuid)
									{
										continue;
									}
									g.fleet = fleet.Value;
								}
							}
							g.VariableString = sg.VariableString;
							g.DesiredFacing = sg.DesiredFacing;
							g.SpeedLimit = sg.SpeedLimit;
							foreach (Goal goal in ship.loyalty.GetGSAI().Goals)
							{
								if (sg.goalGuid != goal.guid)
								{
									continue;
								}
								g.goal = goal;
							}
							ship.GetAI().OrderQueue.AddLast(g);
						}
					}
				}
			}
			foreach (SavedGame.SolarSystemSaveData sdata in this.savedData.SolarSystemDataList)
			{
				foreach (SavedGame.RingSave rsave in sdata.RingList)
				{
					Planet p = new Planet();
					foreach (SolarSystem s in this.data.SolarSystemsList)
					{
						foreach (Planet p1 in s.PlanetList)
						{
							if (p1.guid != rsave.Planet.guid)
							{
								continue;
							}
							p = p1;
							break;
						}
					}
					if (p.Owner == null)
					{
						continue;
					}
					foreach (SavedGame.QueueItemSave qisave in rsave.Planet.QISaveList)
					{
						QueueItem qi = new QueueItem();
						if (qisave.isBuilding)
						{
							qi.isBuilding = true;
							qi.Building = Ship_Game.ResourceManager.BuildingsDict[qisave.UID];
							qi.Cost = qi.Building.Cost * this.savedData.GamePacing;
                            qi.NotifyOnEmpty = false;
							foreach (PlanetGridSquare pgs in p.TilesList)
							{
								if ((float)pgs.x != qisave.pgsVector.X || (float)pgs.y != qisave.pgsVector.Y)
								{
									continue;
								}
								pgs.QItem = qi;
								qi.pgs = pgs;
								break;
							}
						}
						if (qisave.isTroop)
						{
							qi.isTroop = true;
							qi.troop = Ship_Game.ResourceManager.TroopsDict[qisave.UID];
                            qi.Cost = qi.troop.GetCost();
                            qi.NotifyOnEmpty = false;
						}
						if (qisave.isShip)
						{
							qi.isShip = true;
							if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(qisave.UID))
							{
								continue;
							}
							qi.sData = Ship_Game.ResourceManager.GetShip(qisave.UID).GetShipData();
							qi.DisplayName = qisave.DisplayName;
							qi.Cost = 0f;
							foreach (ModuleSlot slot in Ship_Game.ResourceManager.GetShip(qisave.UID).ModuleSlotList)
							{
								if (slot.InstalledModuleUID == null)
								{
									continue;
								}
								QueueItem cost = qi;
								cost.Cost = cost.Cost + Ship_Game.ResourceManager.GetModule(slot.InstalledModuleUID).Cost * this.savedData.GamePacing;
							}
							QueueItem queueItem = qi;
                            queueItem.Cost += qi.Cost * p.Owner.data.Traits.ShipCostMod;
                            queueItem.Cost *= (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(Ship_Game.ResourceManager.GetShip(qisave.UID).GetShipData().Hull) ? 1f - ResourceManager.HullBonuses[Ship_Game.ResourceManager.GetShip(qisave.UID).GetShipData().Hull].CostBonus : 1);
							if (qi.sData.HasFixedCost)
							{
								qi.Cost = (float)qi.sData.FixedCost;
							}
							if (qisave.IsRefit)
							{
								qi.isRefit = true;
								qi.Cost = qisave.RefitCost;
							}
						}
						foreach (Goal g in p.Owner.GetGSAI().Goals)
						{
							if (g.guid != qisave.GoalGUID)
							{
								continue;
							}
							qi.Goal = g;
                            qi.NotifyOnEmpty = false;
						}
						if (qisave.isShip && qi.Goal != null)
						{
							qi.Goal.beingBuilt = Ship_Game.ResourceManager.GetShip(qisave.UID);
						}
						qi.productionTowards = qisave.ProgressTowards;
						p.ConstructionQueue.Add(qi);
					}
				}
			}
			this.Loaded = true;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			if (!this.GateKeeper.WaitOne(0))
			{
				return;
			}
			if (this.ready)
			{
				return;
			}
			if (!this.Loaded)
			{
				return;
			}
			this.data.SolarSystemsList[this.systemToMake].spatialManager.Setup((int)(200000f * this.GameScale), (int)(200000f * this.GameScale), (int)(100000f * this.GameScale), this.data.SolarSystemsList[this.systemToMake].Position);
			this.percentloaded = (float)this.systemToMake / (float)this.data.SolarSystemsList.Count;
			foreach (Planet p in this.data.SolarSystemsList[this.systemToMake].PlanetList)
			{
				p.system = this.data.SolarSystemsList[this.systemToMake];
				p.InitializeUpdate();
				base.ScreenManager.inter.ObjectManager.Submit(p.SO);
			}
            foreach (Asteroid roid in this.data.SolarSystemsList[this.systemToMake].AsteroidsList)
                base.ScreenManager.inter.ObjectManager.Submit(roid.GetSO());
            foreach (Moon moon in this.data.SolarSystemsList[this.systemToMake].MoonList)
                base.ScreenManager.inter.ObjectManager.Submit(moon.GetSO());
			LoadUniverseScreen loadUniverseScreen = this;
			loadUniverseScreen.systemToMake = loadUniverseScreen.systemToMake + 1;
			if (this.systemToMake == this.data.SolarSystemsList.Count)
			{
				foreach (Ship ship in this.data.MasterShipList)
				{
					ship.LoadFromSave();
					ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.Position, 0f));
					ship.InitializeFromSave();
					if (ship.Name == "Brimstone")
					{
						ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.Position, 0f));
					}
					if (ship.GetHangars().Count > 0)
					{
						foreach (ShipModule hangar in ship.GetHangars())
						{
							foreach (Ship othership in ship.loyalty.GetShips())
							{
								if (hangar.installedSlot.HangarshipGuid != othership.guid)
								{
									continue;
								}
								hangar.SetHangarShip(othership);
								othership.Mothership = ship;
							}
						}
					}
					Guid orbitTargetGuid = ship.GetAI().OrbitTargetGuid;
					foreach (SolarSystem s in this.data.SolarSystemsList)
					{
						foreach (Planet p in s.PlanetList)
						{
							List<Ship> toadd = new List<Ship>();
							foreach (KeyValuePair<Guid, Ship> station in p.Shipyards)
							{
								if (station.Key != ship.guid)
								{
									continue;
								}
								toadd.Add(ship);
							}
							foreach (Ship add in toadd)
							{
								p.Shipyards[add.guid] = add;
								add.TetherToPlanet(p);
							}
							if (p.guid != ship.GetAI().OrbitTargetGuid)
							{
								continue;
							}
							ship.GetAI().OrbitTarget = p;
							if (ship.GetAI().State != AIState.Orbit)
							{
								continue;
							}
							ship.GetAI().OrderToOrbit(p, true);
						}
					}
					Guid systemToDefendGuid = ship.GetAI().SystemToDefendGuid;
					if (ship.GetAI().State == AIState.SystemDefender)
					{
						foreach (SolarSystem s in this.data.SolarSystemsList)
						{
							if (s.guid != ship.GetAI().SystemToDefendGuid)
							{
								continue;
							}
							ship.GetAI().SystemToDefend = s;
							ship.GetAI().State = AIState.SystemDefender;
						}
					}
					Guid escortTargetGuid = ship.GetAI().EscortTargetGuid;
					foreach (Ship s in this.data.MasterShipList)
					{
						if (s.guid == ship.GetAI().EscortTargetGuid)
						{
							ship.GetAI().EscortTarget = s;
						}
						if (s.guid != ship.GetAI().TargetGuid)
						{
							continue;
						}
						ship.GetAI().Target = s;
					}
					base.ScreenManager.inter.ObjectManager.Submit(ship.GetSO());
					foreach (Thruster t in ship.GetTList())
					{
						t.load_and_assign_effects(base.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
						t.InitializeForViewing();
					}
					foreach (Projectile p in ship.Projectiles)
					{
						p.firstRun = false;
					}
				}
				foreach (SolarSystem sys in this.data.SolarSystemsList)
				{
					List<LoadUniverseScreen.SysDisPair> dysSys = new List<LoadUniverseScreen.SysDisPair>();
					foreach (SolarSystem toCheck in this.data.SolarSystemsList)
					{
						if (sys == toCheck)
						{
							continue;
						}
						float Distance = Vector2.Distance(sys.Position, toCheck.Position);
						if (dysSys.Count >= 5)
						{
							int indexOfFarthestSystem = 0;
							float farthest = 0f;
							for (int i = 0; i < 5; i++)
							{
								if (dysSys[i].Distance > farthest)
								{
									indexOfFarthestSystem = i;
									farthest = dysSys[i].Distance;
								}
							}
							if (Distance >= farthest)
							{
								continue;
							}
							LoadUniverseScreen.SysDisPair sp = new LoadUniverseScreen.SysDisPair()
							{
								System = toCheck,
								Distance = Distance
							};
							dysSys[indexOfFarthestSystem] = sp;
						}
						else
						{
							LoadUniverseScreen.SysDisPair sp = new LoadUniverseScreen.SysDisPair()
							{
								System = toCheck,
								Distance = Distance
							};
							dysSys.Add(sp);
						}
					}
					foreach (LoadUniverseScreen.SysDisPair sp in dysSys)
					{
						sys.FiveClosestSystems.Add(sp.System);
					}
				}
				this.us = new UniverseScreen(this.data, this.PlayerLoyalty)
				{
					GamePace = this.GamePace,
					GameScale = this.GameScale,
					GameDifficulty = this.data.difficulty
				};
				float starDate = this.savedData.StarDate;
				this.us.StarDate = this.savedData.StarDate;
				this.us.ScreenManager = base.ScreenManager;
				this.us.camPos = new Vector3(this.camPos.X, this.camPos.Y, this.camHeight);
				this.us.player = EmpireManager.GetEmpireByName(this.PlayerLoyalty);
				this.us.LoadContent();
				this.us.UpdateAllSystems(0.01f);
				foreach (Ship ship in this.data.MasterShipList)
				{
					AIState state = ship.GetAI().State;
					if (state == AIState.SystemTrader)
					{
						ship.GetAI().OrderTradeFromSave((ship.CargoSpace_Used > 0f ? true : false), Guid.Empty, Guid.Empty);
					}
					else
					{
						if (state != AIState.PassengerTransport)
						{
							continue;
						}
						ship.GetAI().OrderTransportPassengersFromSave();
					}
				}
				this.ready = true;
			}
			Game1.Instance.ResetElapsedTime();
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		private struct SysDisPair
		{
			public SolarSystem System;

			public float Distance;
		}
	}
}