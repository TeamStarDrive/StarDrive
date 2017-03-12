using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Ship_Game.AI;

namespace Ship_Game
{
	public sealed class LoadUniverseScreen : GameScreen
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
        private Texture2D LoadingImage;
		private Effect ThrusterEffect;
        private Model ThrusterModel;
        private Texture3D ThrusterTexture;
		private int systemToMake;
		private ManualResetEvent GateKeeper = new ManualResetEvent(false);
		private bool Loaded;
		private UniverseScreen us;
		private Ship playerShip;
		private float percentloaded;
		private bool ready;

		public LoadUniverseScreen(FileInfo activeFile) : base(null/*no parent*/)
		{
            GlobalStats.RemnantKills = 0;
			GlobalStats.RemnantArmageddon = false;
            GlobalStats.Statreset();
			BackgroundWorker bgw = new BackgroundWorker();
			bgw.DoWork += DecompressFile;
			bgw.RunWorkerCompleted += LoadEverything;
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
                
                foreach (string key in e.data.WeaponTags.Keys)
                {
                    if(data.empireData.WeaponTags.ContainsKey(key))
                        continue;
                    data.empireData.WeaponTags.Add(key,new WeaponTagModifier());
                }
                e.data = data.empireData;
                
				e.data.ResearchQueue = data.empireData.ResearchQueue;
				e.ResearchTopic      = data.ResearchTopic ?? "";
				e.PortraitName       = e.data.PortraitName;
			    e.dd                 = ResourceManager.DDDict[e.data.DiplomacyDialogPath];
				e.EmpireColor = new Color((byte)e.data.Traits.R, (byte)e.data.Traits.G, (byte)e.data.Traits.B);
                e.data.CurrentAutoScout     = data.CurrentAutoScout     ?? e.data.StartingScout;
                e.data.CurrentAutoFreighter = data.CurrentAutoFreighter ?? e.data.DefaultSmallTransport;
                e.data.CurrentAutoColony    = data.CurrentAutoColony    ?? e.data.DefaultColonyShip;
                e.data.CurrentConstructor   = data.CurrentConstructor   ?? e.data.DefaultConstructor;
                if(string.IsNullOrEmpty(data.empireData.DefaultTroopShip))
                {
                    e.data.DefaultTroopShip = e.data.PortraitName + " " + "Troop";
                }

			}
            foreach(TechEntry tech in data.TechTree)
            {
                e.TechnologyDict.Add(tech.UID, tech);
            }            
			e.InitializeFromSave();
			e.Money = data.Money;
			e.Research = data.Research;
			e.GetGSAI().AreasOfOperations = data.AOs;            
  
			return e;
		}

		private Planet CreatePlanetFromPlanetSaveData(SolarSystem forSystem, SavedGame.PlanetSaveData data)
		{
		    Planet p = new Planet
		    {
		        system = forSystem,
		        ParentSystem = forSystem,
                guid = data.guid,
                Name = data.Name
            };
		    if (!string.IsNullOrEmpty(data.Owner))
			{
				p.Owner = EmpireManager.GetEmpireByName(data.Owner);
				p.Owner.AddPlanet(p);
			}
            if(!string.IsNullOrEmpty(data.SpecialDescription))
            {
                p.SpecialDescription = data.SpecialDescription;
            }
            if (data.Scale > 0f)
            {
                p.scale = data.Scale;
            }
            else
            {
                float scale = RandomMath.RandomBetween(1f, 2f);
                p.scale = scale;
            }
			p.colonyType = data.ColonyType;
			if (!data.GovernorOn)
			{
				p.GovernorOn = false;
				p.colonyType = Planet.ColonyType.Colony;
			}
			p.OrbitalAngle          = data.OrbitalAngle;
			p.fs                    = data.FoodState;
			p.ps                    = data.ProdState;
			p.FoodLocked            = data.FoodLock;
			p.ProdLocked            = data.ProdLock;
			p.ResLocked             = data.ResLock;
			p.OrbitalRadius         = data.OrbitalDistance;
			p.Population            = data.Population;
			p.MaxPopulation         = data.PopulationMax;
			p.Fertility             = data.Fertility;
			p.MineralRichness       = data.Richness;
			p.TerraformPoints       = data.TerraformPoints;
			p.hasRings              = data.HasRings;
			p.planetType            = data.WhichPlanet;
			p.ShieldStrengthCurrent = data.ShieldStrength;
			p.LoadAttributes();
			p.Crippled_Turns = data.Crippled_Turns;
			p.planetTilt = RandomMath.RandomBetween(45f, 135f);
            p.ObjectRadius = 1000f * (float)(1 + ((Math.Log(p.scale)) / 1.5)); // p.scale; //(1 + ((Math.Log(planet.scale))/1.5) )
			foreach (Guid guid in data.StationsList)
			{
				p.Shipyards.TryAdd(guid, new Ship());
                
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
				PlanetGridSquare pgs = new PlanetGridSquare(d.x, d.y, d.foodbonus, d.prodbonus, 
                                            d.resbonus, d.building, d.Habitable)
				{
					Biosphere = d.Biosphere
				};
				if (pgs.Biosphere)
				{
					p.BuildingList.Add(ResourceManager.CreateBuilding("Biospheres"));
				}
				p.TilesList.Add(pgs);
                foreach (Troop t in d.TroopsHere)
                {
                    if (!ResourceManager.TroopsDict.ContainsKey(t.Name))
                        continue;
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
				pgs.building.theWeapon = ResourceManager.WeaponsDict[pgs.building.Weapon];
			}
			return p;
		}

		private SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData data)
		{
			SolarSystem system = new SolarSystem()
			{
				Name     = data.Name,
				Position = data.Position,
				SunPath  = data.SunPath,
				AsteroidsList = new BatchRemovalCollection<Asteroid>(),
                MoonList = new Array<Moon>()
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
			foreach (Empire e in EmpireManager.Empires)
			{
				system.ExploredDict.Add(e, false);
			}
			foreach (string empireName in data.EmpiresThatKnowThisSystem)
			{
				system.ExploredDict[EmpireManager.GetEmpireByName(empireName)] = true;
			}
			system.RingList = new Array<SolarSystem.Ring>();
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
					Planet p = this.CreatePlanetFromPlanetSaveData(system, ring.Planet);
					p.Position = system.Position.PointOnCircle(p.OrbitalAngle, p.OrbitalRadius);
                    
					foreach (Building b in p.BuildingList)
					{
						if (b.Name != "Space Port")
							continue;
						p.Station = new SpaceStation
						{
							planet       = p,
							Position     = p.Position,
							ParentSystem = p.system
						};
						p.Station.LoadContent(ScreenManager);
						p.HasShipyard = true;
                        
					}
                    
					if (p.Owner != null && !system.OwnerList.Contains(p.Owner))
					{
						system.OwnerList.Add(p.Owner);
					}
					system.PlanetList.Add(p);
					foreach (Empire e in EmpireManager.Empires)
					{
						p.ExploredDict.Add(e, false);
					}
					foreach (string empireName in data.EmpiresThatKnowThisSystem)
					{
						p.ExploredDict[EmpireManager.GetEmpireByName(empireName)] = true;
					}
					SolarSystem.Ring r1 = new SolarSystem.Ring()
					{
						planet = p,
						Asteroids = false
					};
					system.RingList.Add(r1);
                    p.UpdateIncomes(true);  //fbedard: needed for OrderTrade()
				}
			}
			return system;
		}

		private void DecompressFile(object info, DoWorkEventArgs e)
		{
            var usData = SavedGame.DeserializeFromCompressedSave((FileInfo)e.Argument);

			GlobalStats.RemnantKills                  = usData.RemnantKills;
            GlobalStats.RemnantActivation             = usData.RemnantActivation;
            GlobalStats.RemnantArmageddon             = usData.RemnantArmageddon;
            GlobalStats.GravityWellRange              = usData.GravityWellRange;
            GlobalStats.IconSize                      = usData.IconSize;
            GlobalStats.MinimumWarpRange              = usData.MinimumWarpRange;
            GlobalStats.ShipMaintenanceMulti = usData.OptionIncreaseShipMaintenance;
            GlobalStats.PreventFederations            = usData.preventFederations;
            GlobalStats.EliminationMode               = usData.EliminationMode;
            if (usData.TurnTimer == 0)
                usData.TurnTimer = 5;
            GlobalStats.TurnTimer = usData.TurnTimer;

			savedData     = usData;
			camPos        = usData.campos;
			camHeight     = usData.camheight;
			GamePace      = usData.GamePacing;
			GameScale     = usData.GameScale;
			PlayerLoyalty = usData.PlayerLoyalty;
            UniverseScreen.GamePaceStatic  = GamePace;
			UniverseScreen.GameScaleStatic = GameScale;
			RandomEventManager.ActiveEvent = null;
			StatTracker.SnapshotsDict.Clear();
			StatTracker.SnapshotsDict = usData.Snapshots;
            UniverseData.UniverseWidth = usData.Size.X;
			GateKeeper.Set();
		}

		protected override void Dispose(bool disposing)
		{
            lock (this)
            {
                GateKeeper?.Dispose(ref GateKeeper);
                data = null;
            }
            base.Dispose(disposing);
        }

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			base.ScreenManager.SpriteBatch.Begin();
			Rectangle ArtRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
			base.ScreenManager.SpriteBatch.Draw(LoadingImage, ArtRect, Color.White);
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


		public void Go()
		{
			foreach (Empire e in this.data.EmpireList)
			{
				e.GetGSAI().InitialzeAOsFromSave(this.data);
			}
			foreach (Ship ship in this.us.MasterShipList)
			{
                if (!ship.Active)
                {
                    us.MasterShipList.QueuePendingRemoval(ship);
                    continue;
                }

                ship.UpdateSystem(0f);
				if (ship.loyalty != EmpireManager.Player)
				{
					if (!ship.AddedOnLoad) ship.loyalty.ForcePoolAdd(ship);
				}
				else if (ship.GetAI().State == AIState.SystemDefender)
                {
                    ship.loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
					ship.AddedOnLoad = true;
				}
			}
            us.MasterShipList.ApplyPendingRemovals();
			ScreenManager.musicCategory.Stop(AudioStopOptions.AsAuthored);
			us.EmpireUI.empire = us.player;
			ScreenManager.AddScreenNoLoad(us);
            ExitScreen();
		}

		public override void HandleInput(InputState input)
		{
			if (ready && input.InGameSelect)
			{
				Go();
			}
		}

		public override void LoadContent()
		{
			ScreenCenter = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth  / 2f, 
                                       ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f);

            LoadingImage = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            text = HelperFunctions.ParseText(Fonts.Arial12Bold, ResourceManager.LoadRandomAdvice(), 500f);
			base.LoadContent();
		}

		private void LoadEverything(object sender, RunWorkerCompletedEventArgs ev)
		{
		    ResourceManager.LoadShips();
		    base.ScreenManager.inter.ObjectManager.Clear();
			this.data = new UniverseData();
			RandomEventManager.ActiveEvent = this.savedData.RandomEvent;
			UniverseScreen.DeepSpaceManager = new SpatialManager();
			ThrusterEffect  = Game1.GameContent.Load<Effect>("Effects/Thrust");
            ThrusterModel   = Game1.GameContent.Load<Model>("Effects/ThrustCylinderB");
            ThrusterTexture = Game1.GameContent.Load<Texture3D>("Effects/NoiseVolume");
            this.data.loadFogPath           = this.savedData.FogMapName;
			this.data.difficulty            = UniverseData.GameDifficulty.Normal;
			this.data.difficulty            = this.savedData.gameDifficulty;
			this.data.Size                  = this.savedData.Size;
			this.data.FTLSpeedModifier      = this.savedData.FTLModifier;
            this.data.EnemyFTLSpeedModifier = this.savedData.EnemyFTLModifier;
			this.data.GravityWells          = this.savedData.GravityWells;
            //added by gremlin: adjuse projector radius to map size. but only normal or higher. 
            //this is pretty bad as its not connected to the creating game screen code that sets the map sizes. If someone changes the map size they wont know to change this as well.
            if (this.data.Size.X > 3500000f) 
            Empire.ProjectorRadius = this.data.Size.X / 70f;
			EmpireManager.Clear();
            if (Empire.Universe!=null && Empire.Universe.MasterShipList != null)
                Empire.Universe.MasterShipList.Clear();
            
         
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
				EmpireManager.Add(e);
			}
			foreach (Empire e in this.data.EmpireList)
			{
				if (e.data.AbsorbedBy == null)
				{
					continue;
				}
                Empire empireAbsorbedBy = EmpireManager.GetEmpireByName(e.data.AbsorbedBy);
				foreach (KeyValuePair<string, TechEntry> tech in empireAbsorbedBy.GetTDict())
				{
					if (!tech.Value.Unlocked)
					{
						continue;
					}
                    empireAbsorbedBy.UnlockHullsSave(tech.Key, e.data.Traits.ShipType);
				}
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.Name);
				foreach (Relationship r in d.Relations)
				{
                    var empire = EmpireManager.GetEmpireByName(r.Name);
					e.AddRelationships(empire, r);
				    r.ActiveWar?.SetCombatants(e, empire);
				}
			}
			this.data.SolarSystemsList = new Array<SolarSystem>();
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
                    if (shipData.Name != shipData.VanityName)//  !string.IsNullOrEmpty(shipData.VanityName))
                        ship.VanityName = shipData.VanityName;
                    else
                    {
                        if (ship.shipData.Role == ShipData.RoleName.troop)
                        {
                            if (shipData.TroopList.Count > 0)
                            {
                                ship.VanityName = shipData.TroopList[0].Name;
                            }
                            else
                                ship.VanityName = shipData.Name;
                        }
                        else
                            ship.VanityName = shipData.Name;
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
					if (!ResourceManager.ShipsDict.ContainsKey(shipData.Name))
					{
						shipData.data.Hull = shipData.Hull;
						Ship newShip = Ship.CreateShipFromShipData(shipData.data);
						newShip.SetShipData(shipData.data);
						if (!newShip.Init(fromSave: false))
						{
							continue;
						}
						newShip.InitializeStatus();
						newShip.IsPlayerDesign = false;
						newShip.FromSave = true;
						ResourceManager.ShipsDict.Add(shipData.Name, newShip);
					}				
                    else if (ResourceManager.ShipsDict[shipData.Name].FromSave)
					{
						ship.IsPlayerDesign = false;
						ship.FromSave = true;
					}
                    ship.BaseStrength = ResourceManager.CalculateBaseStrength(ship);

                    foreach (ModuleSlotData moduleSD in shipData.data.ModuleSlotList)
                    {
                        if (ResourceManager.ModuleExists(moduleSD.InstalledModuleUID))
                            continue;
                        Log.Info("mismatch = {0}", moduleSD.InstalledModuleUID);
                    }


					ship.PowerCurrent = shipData.Power;
					ship.yRotation = shipData.yRotation;
					ship.Ordinance = shipData.Ordnance;
					ship.Rotation = shipData.Rotation;
					ship.Velocity = shipData.Velocity;
					ship.isSpooling = shipData.AfterBurnerOn;
					ship.InCombatTimer = shipData.InCombatTimer;

					if (shipData.TroopList != null)
                    foreach (Troop t in shipData.TroopList)
					{
						t.SetOwner(EmpireManager.GetEmpireByName(t.OwnerString));
						ship.TroopList.Add(t);
					}

                    if (shipData.AreaOfOperation != null)
                    foreach (Rectangle aoRect in shipData.AreaOfOperation)
                    {
                        ship.AreaOfOperation.Add(aoRect);
                    }
					ship.TetherGuid = shipData.TetheredTo;
					ship.TetherOffset = shipData.TetherOffset;
					if (ship.InCombatTimer > 0f)
					{
						ship.InCombat = true;
					}
					ship.loyalty = e;
					ship.InitializeAI();
					ship.GetAI().CombatState          = shipData.data.CombatState;
					ship.GetAI().FoodOrProd           = shipData.AISave.FoodOrProd;
					ship.GetAI().State                = shipData.AISave.state;
					ship.GetAI().DefaultAIState       = shipData.AISave.defaultstate;
					ship.GetAI().GotoStep             = shipData.AISave.GoToStep;
					ship.GetAI().MovePosition         = shipData.AISave.MovePosition;
					ship.GetAI().OrbitTargetGuid      = shipData.AISave.OrbitTarget;
                    //ship.GetAI().ColonizeTargetGuid = shipData.AISave.ColonizeTarget;          //Not referenced in code, removing to save memory
                    ship.GetAI().TargetGuid           = shipData.AISave.AttackTarget;
					ship.GetAI().SystemToDefendGuid   = shipData.AISave.SystemToDefend;
					ship.GetAI().EscortTargetGuid     = shipData.AISave.EscortTarget;
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
                        ship.GetAI().FoodOrProd = "Pass";
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
					Fleet fleet = new Fleet()
					{
						Guid = fleetsave.FleetGuid,
						IsCoreFleet = fleetsave.IsCoreFleet,
						Facing = fleetsave.facing
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
							node.Ship = ship;
							node.ShipName = ship.Name;
							break;
						}
					}
					fleet.AssignPositions(fleet.Facing);
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
                    fleet.FindAveragePositionset();
                    fleet.Setavgtodestination();
                    
				}
                /* fbedard: not needed
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
                */
			}
			foreach (SavedGame.EmpireSaveData d in this.savedData.EmpireDataList)
			{
				Empire e = EmpireManager.GetEmpireByName(d.Name);
                e.SpaceRoadsList = new Array<SpaceRoad>();
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
						foreach (KeyValuePair<int, Fleet> Fleet in e.GetFleetsDict())
						{
							if (Fleet.Value.Guid != gsave.fleetGuid)
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
					e.GetGSAI().ThreatMatrix.Pins.Add(d.GSAIData.PinGuids[i], d.GSAIData.PinList[i]);
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
                            foreach (SolarSystem s in data.SolarSystemsList)
                            {
                                bool stop = false;
                                foreach (Planet p in s.PlanetList)
                                {
                                    if (p.guid != task.TargetPlanetGuid)
                                        continue;
                                    task.SetTargetPlanet(p);
                                    stop = true;
                                    break;
                                }
                                if (stop) break;
                            }
						}
						foreach (Guid guid in task.HeldGoals)
						{
							foreach (Goal g in e.GetGSAI().Goals)
							{
								if (g.guid == guid)
								    g.Held = true;
							}
						}
						try
						{
							if (task.WhichFleet != -1)
								e.GetFleetsDict()[task.WhichFleet].FleetTask = task;
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
								foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
								{
									if (fleet.Value.Guid != sg.fleetGuid)
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
							ship.GetAI().OrderQueue.Enqueue(g);
                            if (g.Plan == ArtificialIntelligence.Plan.DeployStructure)
                                ship.isConstructor = true;
						}
					}
				}
			}
			foreach (SavedGame.SolarSystemSaveData sdata in this.savedData.SolarSystemDataList)
			{
				foreach (SavedGame.RingSave rsave in sdata.RingList)
				{
					Planet p = null;
					foreach (SolarSystem s in data.SolarSystemsList)
					{
						foreach (Planet p1 in s.PlanetList)
						{
						    if (p1.guid != rsave.Planet.guid) continue;
						    p = p1;
						    break;
						}
					}
					if (p?.Owner == null)
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
                            qi.IsPlayerAdded = qisave.isPlayerAdded;
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
							qi.troop = ResourceManager.TroopsDict[qisave.UID];
                            qi.Cost = qi.troop.GetCost();
                            qi.NotifyOnEmpty = false;
						}
						if (qisave.isShip)
						{
							qi.isShip = true;
							if (!ResourceManager.ShipsDict.ContainsKey(qisave.UID))
								continue;

                            Ship shipTemplate = ResourceManager.GetShipTemplate(qisave.UID);
                            qi.sData = shipTemplate.GetShipData();
							qi.DisplayName = qisave.DisplayName;
							qi.Cost = 0f;
							foreach (ModuleSlot slot in shipTemplate.ModuleSlotList)
							{
								if (slot.InstalledModuleUID == null)
									continue;
								qi.Cost += ResourceManager.GetModuleCost(slot.InstalledModuleUID) * savedData.GamePacing;
							}
							QueueItem queueItem = qi;
                            queueItem.Cost += qi.Cost * p.Owner.data.Traits.ShipCostMod;

                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                            {
                                string hull = ResourceManager.GetShipHull(qisave.UID);
                                if (ResourceManager.HullBonuses.TryGetValue(hull, out HullBonus bonus))
                                    queueItem.Cost *= 1f - bonus.CostBonus;
                            }

							if (qi.sData.HasFixedCost)
							{
								qi.Cost = qi.sData.FixedCost;
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
							qi.Goal.beingBuilt = ResourceManager.GetShipTemplate(qisave.UID);
						}
						qi.productionTowards = qisave.ProgressTowards;
						p.ConstructionQueue.Add(qi);
					}
				}
			}
            //int shipsPurged = 0;
            //float SpaceSaved = GC.GetTotalMemory(true);
            //foreach(Empire empire in  EmpireManager.Empires)
            //{
            //    if (empire.data.Defeated && !empire.isFaction)
            //    {
            //        Array<string> shipkill = new Array<string>();
            //        HashSet<string> model =  new HashSet<string>();
            //        foreach (KeyValuePair<string, Ship> ship in ResourceManager.ShipsDict)
            //        {
            //            if (ship.Value.shipData.ShipStyle == empire.data.Traits.ShipType )
            //            {                            

            //                bool killSwitch = true;
            //                foreach (Empire ebuild in EmpireManager.Empires)
            //                {
            //                    if (ebuild == empire)
            //                        continue;
            //                    if (ebuild.ShipsWeCanBuild.Contains(ship.Key))
            //                    {
            //                        killSwitch = false;
            //                        model.Add(ship.Value.shipData.Hull);
            //                        break;
            //                    }
            //                }


            //                if (killSwitch)
            //                    foreach (Ship mship in this.data.MasterShipList)
            //                    {
            //                        if (ship.Key == mship.Name)
            //                        {
            //                            killSwitch = false;
            //                            model.Add(ship.Value.shipData.Hull);
            //                            break;
            //                        }
            //                    }
            //                if (killSwitch)
            //                {
            //                    shipsPurged++;
            //                    shipkill.Add(ship.Key);
            //                }
            //            }
            //        }
            //        foreach (string shiptoclear in shipkill)
            //        {
            //            ResourceManager.ShipsDict.Remove(shiptoclear);
            //        }
                    //foreach (string hull in empire.GetHDict().Keys)
                    //{
                    //    if (model.Contains(hull))
                    //        continue;
                    //    ResourceManager.ModelDict.Remove(ResourceManager.HullsDict[hull].ModelPath);


                    //}

                //}

                
            //}
            //Log.Info("Ships Purged: " + shipsPurged.ToString());
            //Log.Info("Memory purged: " + (SpaceSaved - GC.GetTotalMemory(false)).ToString());
			this.Loaded = true;
            UniverseData.UniverseWidth = data.Size.X ;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			if (!GateKeeper.WaitOne(0) || ready || !Loaded)
				return;

            var system = data.SolarSystemsList[systemToMake];
			system.spatialManager.Setup((int)(200000f * this.GameScale), (int)(200000f * this.GameScale), (int)(100000f * this.GameScale), system.Position);
			this.percentloaded = systemToMake / (float)data.SolarSystemsList.Count;
			foreach (Planet p in system.PlanetList)
			{
				p.system = system;
				p.InitializeUpdate();
				base.ScreenManager.inter.ObjectManager.Submit(p.SO);
			}
            foreach (Asteroid roid in system.AsteroidsList)
                base.ScreenManager.inter.ObjectManager.Submit(roid.So);
            foreach (Moon moon in system.MoonList)
                base.ScreenManager.inter.ObjectManager.Submit(moon.So);
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
									continue;
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
							Array<Ship> toadd = new Array<Ship>();
							foreach (KeyValuePair<Guid, Ship> station in p.Shipyards)
							{
								if (station.Key != ship.guid || p.Owner !=null && p.Owner == station.Value.loyalty)
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
                    if (ship.GetShipData().IsShipyard && !ship.IsTethered())
                        ship.Active = false;
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
						t.load_and_assign_effects(Game1.GameContent, ThrusterModel, ThrusterTexture, ThrusterEffect);
						t.InitializeForViewing();
					}
					foreach (Projectile p in ship.Projectiles)
					{
						p.firstRun = false;
					}
				}
				foreach (SolarSystem sys in this.data.SolarSystemsList)
				{
					Array<LoadUniverseScreen.SysDisPair> dysSys = new Array<LoadUniverseScreen.SysDisPair>();
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
							dysSys[indexOfFarthestSystem] = new SysDisPair
							{
								System = toCheck, Distance = Distance
							};
						}
						else
						{
							dysSys.Add(new SysDisPair
							{
								System = toCheck, Distance = Distance
							});
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

                // Just to be sure
                this.camPos = savedData.campos;
                this.camHeight = savedData.camheight;

				this.us.camPos = new Vector3(this.camPos.X, this.camPos.Y, this.camHeight);
                // Finally fucking fixes the 'LOOK AT ME PA I'M ZOOMED RIGHT IN' vanilla bug when loading a saved game: the universe screen uses camheight separately to the campos z vector to actually do zoom.
                this.us.camHeight = this.camHeight;

				this.us.player = EmpireManager.Player;
				this.us.LoadContent();

                Log.Info("LoadUniverseScreen.UpdateAllSystems(0.01)");
				this.us.UpdateAllSystems(0.01f);
                ResourceManager.MarkShipDesignsUnlockable();
                /*
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
                */
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