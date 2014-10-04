using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class SavedGame
	{
		public SavedGame.UniverseSaveData data = new SavedGame.UniverseSaveData();

		public static Thread thread;

		public SavedGame(UniverseScreen screenToSave, string SaveAs)
		{
			Building building;
			this.data.RemnantKills = GlobalStats.RemnantKills;
			this.data.RemnantArmageddon = GlobalStats.RemnantArmageddon;
			this.data.gameDifficulty = screenToSave.GameDifficulty;
			this.data.AutoColonize = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoColonize;
			this.data.AutoExplore = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoExplore;
			this.data.AutoFreighters = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoFreighters;
			this.data.AutoProjectors = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoBuild;
			this.data.GamePacing = UniverseScreen.GamePaceStatic;
			this.data.GameScale = UniverseScreen.GameScaleStatic;
			this.data.StarDate = screenToSave.StarDate;
			this.data.SolarSystemDataList = new List<SavedGame.SolarSystemSaveData>();
			this.data.FTLModifier = screenToSave.FTLModifier;
            this.data.EnemyFTLModifier = screenToSave.EnemyFTLModifier;
			this.data.GravityWells = screenToSave.GravityWells;
			this.data.PlayerLoyalty = screenToSave.PlayerLoyalty;
			this.data.RandomEvent = RandomEventManager.ActiveEvent;
			this.data.campos = new Vector2(screenToSave.camPos.X, screenToSave.camPos.Y);
			this.data.camheight = screenToSave.camHeight;
            this.data.MemoryLimiter = GlobalStats.MemoryLimiter;
            this.data.MinimumWarpRange = GlobalStats.MinimumWarpRange;
            this.data.OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            this.data.TurnTimer = GlobalStats.TurnTimer;
            this.data.IconSize=GlobalStats.IconSize;
            this.data.preventFederations=GlobalStats.preventFederations;
            this.data.GravityWellRange=GlobalStats.GravityWellRange;
            this.data.EliminationMode = GlobalStats.EliminationMode;

			foreach (SolarSystem system in UniverseScreen.SolarSystemList)
			{
				SavedGame.SolarSystemSaveData sdata = new SavedGame.SolarSystemSaveData()
				{
					Name = system.Name,
					Position = system.Position,
					SunPath = system.SunPath,
					AsteroidsList = new List<Asteroid>(),
                    Moons = new List<Moon>(),
				};
				foreach (Asteroid roid in system.AsteroidsList)
				{
					sdata.AsteroidsList.Add(roid);
				}
                foreach (Moon moon in system.MoonList)
                    sdata.Moons.Add(moon);
				sdata.guid = system.guid;
				sdata.RingList = new List<SavedGame.RingSave>();
				foreach (SolarSystem.Ring ring in system.RingList)
				{
					SavedGame.RingSave rsave = new SavedGame.RingSave()
					{
						Asteroids = ring.Asteroids,
						OrbitalDistance = ring.Distance
					};
					if (ring.planet == null)
					{
						sdata.RingList.Add(rsave);
					}
					else
					{
						SavedGame.PlanetSaveData pdata = new SavedGame.PlanetSaveData()
						{
							Crippled_Turns = ring.planet.Crippled_Turns,
							guid = ring.planet.guid,
							FoodState = ring.planet.fs,
							ProdState = ring.planet.ps,
							FoodLock = ring.planet.FoodLocked,
							ProdLock = ring.planet.ProdLocked,
							ResLock = ring.planet.ResLocked,
							Name = ring.planet.Name,
							ShieldStrength = ring.planet.ShieldStrengthCurrent,
							Population = ring.planet.Population,
							PopulationMax = ring.planet.MaxPopulation,
							Fertility = ring.planet.Fertility,
							Richness = ring.planet.MineralRichness,
							Owner = (ring.planet.Owner != null ? ring.planet.Owner.data.Traits.Name : ""),
							WhichPlanet = ring.planet.planetType,
							OrbitalAngle = ring.planet.OrbitalAngle,
							OrbitalDistance = ring.planet.OrbitalRadius,
							HasRings = ring.planet.hasRings,
							Radius = ring.planet.ObjectRadius,
							farmerPercentage = ring.planet.FarmerPercentage,
							workerPercentage = ring.planet.WorkerPercentage,
							researcherPercentage = ring.planet.ResearcherPercentage,
							foodHere = ring.planet.FoodHere,
							TerraformPoints = ring.planet.TerraformPoints,
							prodHere = ring.planet.ProductionHere,
							GovernorOn = ring.planet.GovernorOn,
							ColonyType = ring.planet.colonyType,
							StationsList = new List<Guid>()
						};
						foreach (KeyValuePair<Guid, Ship> station in ring.planet.Shipyards)
						{
							pdata.StationsList.Add(station.Key);
						}
						pdata.QISaveList = new List<SavedGame.QueueItemSave>();
						if (ring.planet.Owner != null)
						{
							foreach (QueueItem item in ring.planet.ConstructionQueue)
							{
								SavedGame.QueueItemSave qi = new SavedGame.QueueItemSave()
								{
									isBuilding = item.isBuilding,
									IsRefit = item.isRefit
								};
								if (qi.IsRefit)
								{
									qi.RefitCost = item.Cost;
								}
								if (qi.isBuilding)
								{
									qi.UID = item.Building.Name;
								}
								qi.isShip = item.isShip;
								qi.DisplayName = item.DisplayName;
								if (qi.isShip)
								{
									qi.UID = item.sData.Name;
								}
								qi.isTroop = item.isTroop;
								if (qi.isTroop)
								{
									qi.UID = item.troop.Name;
								}
								qi.ProgressTowards = item.productionTowards;
								if (item.Goal != null)
								{
									qi.GoalGUID = item.Goal.guid;
								}
								if (item.pgs != null)
								{
									qi.pgsVector = new Vector2((float)item.pgs.x, (float)item.pgs.y);
								}
								pdata.QISaveList.Add(qi);
							}
						}
						pdata.PGSList = new List<SavedGame.PGSData>();
						foreach (PlanetGridSquare tile in ring.planet.TilesList)
						{
							SavedGame.PGSData pgs = new SavedGame.PGSData()
							{
								x = tile.x,
								y = tile.y,
								resbonus = tile.resbonus,
								prodbonus = tile.prodbonus,
								Habitable = tile.Habitable,
								foodbonus = tile.foodbonus,
								Biosphere = tile.Biosphere
							};
							if (tile.building != null)
							{
								building = tile.building;
							}
							else
							{
								building = null;
							}
							pgs.building = building;
							pgs.TroopsHere = tile.TroopsHere;
							pdata.PGSList.Add(pgs);
						}
						pdata.EmpiresThatKnowThisPlanet = new List<string>();
						foreach (KeyValuePair<Empire, bool> ExploreItem in system.ExploredDict)
						{
							if (!ExploreItem.Value)
							{
								continue;
							}
							pdata.EmpiresThatKnowThisPlanet.Add(ExploreItem.Key.data.Traits.Name);
						}
						rsave.Planet = pdata;
						sdata.RingList.Add(rsave);
					}
					sdata.EmpiresThatKnowThisSystem = new List<string>();
					foreach (KeyValuePair<Empire, bool> ExploreItem in system.ExploredDict)
					{
						if (!ExploreItem.Value)
						{
							continue;
						}
						sdata.EmpiresThatKnowThisSystem.Add(ExploreItem.Key.data.Traits.Name);
					}
				}
				this.data.SolarSystemDataList.Add(sdata);
			}
			this.data.EmpireDataList = new List<SavedGame.EmpireSaveData>();
			foreach (Empire e in EmpireManager.EmpireList)
			{
				SavedGame.EmpireSaveData empireToSave = new SavedGame.EmpireSaveData()
				{
					IsFaction = e.isFaction,
                    isMinorRace = e.MinorRace,
					Relations = new List<Relationship>()
				};
				foreach (KeyValuePair<Empire, Relationship> relation in e.GetRelations())
				{
					empireToSave.Relations.Add(relation.Value);
				}
				empireToSave.Name = e.data.Traits.Name;
				empireToSave.empireData = e.data.GetClone();
				empireToSave.Traits = e.data.Traits;
				empireToSave.Research = e.Research;
				empireToSave.ResearchTopic = e.ResearchTopic;
				empireToSave.Money = e.Money;
                empireToSave.CurrentAutoScout = e.data.CurrentAutoScout;
                empireToSave.CurrentAutoFreighter = e.data.CurrentAutoFreighter;
                empireToSave.CurrentAutoColony = e.data.CurrentAutoColony;
				empireToSave.OwnedShips = new List<SavedGame.ShipSaveData>();
				empireToSave.TechTree = new List<TechEntry>();
				foreach (AO area in e.GetGSAI().AreasOfOperations)
				{
					area.PrepareForSave();
				}
				empireToSave.AOs = e.GetGSAI().AreasOfOperations;
				empireToSave.FleetsList = new List<SavedGame.FleetSave>();
				foreach (KeyValuePair<int, Ship_Game.Gameplay.Fleet> Fleet in e.GetFleetsDict())
				{
					SavedGame.FleetSave fs = new SavedGame.FleetSave()
					{
						Name = Fleet.Value.Name,
						IsCoreFleet = Fleet.Value.IsCoreFleet,
						TaskStep = Fleet.Value.TaskStep,
						Key = Fleet.Key,
						facing = Fleet.Value.facing,
						FleetGuid = Fleet.Value.guid,
						Position = Fleet.Value.Position,
						ShipsInFleet = new List<SavedGame.FleetShipSave>()
					};
					foreach (FleetDataNode node in Fleet.Value.DataNodes)
					{
						if (node.GetShip() == null)
						{
							continue;
						}
						node.ShipGuid = node.GetShip().guid;
					}
					fs.DataNodes = Fleet.Value.DataNodes;
					foreach (Ship ship in Fleet.Value.Ships)
					{
						SavedGame.FleetShipSave ssave = new SavedGame.FleetShipSave()
						{
							fleetOffset = ship.RelativeFleetOffset,
							shipGuid = ship.guid
						};
						fs.ShipsInFleet.Add(ssave);
					}
					empireToSave.FleetsList.Add(fs);
				}
				empireToSave.SpaceRoadData = new List<SavedGame.SpaceRoadSave>();
				foreach (SpaceRoad road in e.SpaceRoadsList)
				{
					SavedGame.SpaceRoadSave rdata = new SavedGame.SpaceRoadSave()
					{
						OriginGUID = road.GetOrigin().guid,
						DestGUID = road.GetDestination().guid,
						RoadNodes = new List<SavedGame.RoadNodeSave>()
					};
					foreach (RoadNode node in road.RoadNodesList)
					{
						SavedGame.RoadNodeSave ndata = new SavedGame.RoadNodeSave()
						{
							Position = node.Position
						};
						if (node.Platform != null)
						{
							ndata.Guid_Platform = node.Platform.guid;
						}
						rdata.RoadNodes.Add(ndata);
					}
					empireToSave.SpaceRoadData.Add(rdata);
				}
				SavedGame.GSAISAVE gsaidata = new SavedGame.GSAISAVE()
				{
					UsedFleets = e.GetGSAI().UsedFleets,
					Goals = new List<SavedGame.GoalSave>(),
					PinGuids = new List<Guid>(),
					PinList = new List<ThreatMatrix.Pin>()
				};
				foreach (KeyValuePair<Guid, ThreatMatrix.Pin> guid in e.GetGSAI().ThreatMatrix.Pins)
				{
					
                    gsaidata.PinGuids.Add(guid.Key);
					gsaidata.PinList.Add(guid.Value);
				}
				gsaidata.MilitaryTaskList = new List<MilitaryTask>();
				foreach (MilitaryTask task in e.GetGSAI().TaskList)
				{
					gsaidata.MilitaryTaskList.Add(task);
					if (task.GetTargetPlanet() == null)
					{
						continue;
					}
					task.TargetPlanetGuid = task.GetTargetPlanet().guid;
				}
				for (int i = 0; i < e.GetGSAI().Goals.Count; i++)
				{
					Goal g = e.GetGSAI().Goals[i];
					SavedGame.GoalSave gdata = new SavedGame.GoalSave()
					{
						BuildPosition = g.BuildPosition
					};
					if (g.GetColonyShip() != null)
					{
						gdata.colonyShipGuid = g.GetColonyShip().guid;
					}
					gdata.GoalStep = g.Step;
					if (g.GetMarkedPlanet() != null)
					{
						gdata.markedPlanetGuid = g.GetMarkedPlanet().guid;
					}
					gdata.ToBuildUID = g.ToBuildUID;
					gdata.type = g.type;
					if (g.GetPlanetWhereBuilding() != null)
					{
						gdata.planetWhereBuildingAtGuid = g.GetPlanetWhereBuilding().guid;
					}
					if (g.GetFleet() != null)
					{
						gdata.fleetGuid = g.GetFleet().guid;
					}
					gdata.GoalGuid = g.guid;
					gdata.GoalName = g.GoalName;
					if (g.beingBuilt != null)
					{
						gdata.beingBuiltGUID = g.beingBuilt.guid;
					}
					gsaidata.Goals.Add(gdata);
				}
				empireToSave.GSAIData = gsaidata;
				foreach (KeyValuePair<string, TechEntry> Tech in e.GetTDict())
				{
					empireToSave.TechTree.Add(Tech.Value);
				}
				foreach (Ship ship in e.GetShips())
				{
					SavedGame.ShipSaveData sdata = new SavedGame.ShipSaveData()
					{
						guid = ship.guid,
						data = ship.ToShipData(),
						Position = ship.Position,
						experience = ship.experience,
						kills = ship.kills,
						Velocity = ship.Velocity
					};
					if (ship.GetTether() != null)
					{
						sdata.TetheredTo = ship.GetTether().guid;
						sdata.TetherOffset = ship.TetherOffset;
					}
					sdata.Name = ship.VanityName;
					if (ship.PlayerShip)
					{
						sdata.IsPlayerShip = true;
					}
					sdata.Hull = ship.GetShipData().Hull;
					sdata.Power = ship.PowerCurrent;
					sdata.Ordnance = ship.Ordinance;
					sdata.yRotation = ship.yRotation;
					sdata.Rotation = ship.Rotation;
					sdata.InCombatTimer = ship.InCombatTimer;
					if (ship.GetCargo().ContainsKey("Food"))
					{
						sdata.FoodCount = ship.GetCargo()["Food"];
					}
					if (ship.GetCargo().ContainsKey("Production"))
					{
						sdata.ProdCount = ship.GetCargo()["Production"];
					}
					if (ship.GetCargo().ContainsKey("Colonists_1000"))
					{
						sdata.PopCount = ship.GetCargo()["Colonists_1000"];
					}
					sdata.TroopList = ship.TroopList;
					sdata.AISave = new SavedGame.ShipAISave()
					{
						FoodOrProd = ship.GetAI().FoodOrProd,
						state = ship.GetAI().State
					};
					if (ship.GetAI().Target != null && ship.GetAI().Target is Ship)
					{
						sdata.AISave.AttackTarget = (ship.GetAI().Target as Ship).guid;
					}
					sdata.AISave.defaultstate = ship.GetAI().DefaultAIState;
					if (ship.GetAI().start != null)
					{
						sdata.AISave.startGuid = ship.GetAI().start.guid;
					}
					if (ship.GetAI().end != null)
					{
						sdata.AISave.endGuid = ship.GetAI().end.guid;
					}
					sdata.AISave.GoToStep = ship.GetAI().GotoStep;
					sdata.AISave.MovePosition = ship.GetAI().MovePosition;
					sdata.AISave.ActiveWayPoints = new List<Vector2>();
					foreach (Vector2 waypoint in ship.GetAI().ActiveWayPoints)
					{
						sdata.AISave.ActiveWayPoints.Add(waypoint);
					}
					sdata.AISave.ShipGoalsList = new List<SavedGame.ShipGoalSave>();
					foreach (ArtificialIntelligence.ShipGoal sgoal in ship.GetAI().OrderQueue)
					{
						SavedGame.ShipGoalSave gsave = new SavedGame.ShipGoalSave()
						{
							DesiredFacing = sgoal.DesiredFacing
						};
						if (sgoal.fleet != null)
						{
							gsave.fleetGuid = sgoal.fleet.guid;
						}
						gsave.FacingVector = sgoal.FacingVector;
						if (sgoal.goal != null)
						{
							gsave.goalGuid = sgoal.goal.guid;
						}
						gsave.MovePosition = sgoal.MovePosition;
						gsave.Plan = sgoal.Plan;
						if (sgoal.TargetPlanet != null)
						{
							gsave.TargetPlanetGuid = sgoal.TargetPlanet.guid;
						}
						gsave.VariableString = sgoal.VariableString;
						gsave.SpeedLimit = sgoal.SpeedLimit;
						sdata.AISave.ShipGoalsList.Add(gsave);
					}
					if (ship.GetAI().OrbitTarget != null)
					{
						sdata.AISave.OrbitTarget = ship.GetAI().OrbitTarget.guid;
					}
					if (ship.GetAI().ColonizeTarget != null)
					{
						sdata.AISave.ColonizeTarget = ship.GetAI().ColonizeTarget.guid;
					}
					if (ship.GetAI().SystemToDefend != null)
					{
						sdata.AISave.SystemToDefend = ship.GetAI().SystemToDefend.guid;
					}
					if (ship.GetAI().EscortTarget != null)
					{
						sdata.AISave.EscortTarget = ship.GetAI().EscortTarget.guid;
					}
					if (ship.GetHome() != null)
					{
						sdata.HomePlanet = ship.GetHome().Name;
					}
					sdata.Projectiles = new List<SavedGame.ProjectileSaveData>();
					for (int i = 0; i < ship.Projectiles.Count; i++)
					{
						Projectile p = ship.Projectiles[i];
						SavedGame.ProjectileSaveData pdata = new SavedGame.ProjectileSaveData()
						{
							Velocity = p.Velocity,
							Rotation = p.Rotation,
							Weapon = p.weapon.UID,
							Position = p.Center,
							Duration = p.duration
						};
						sdata.Projectiles.Add(pdata);
					}
					empireToSave.OwnedShips.Add(sdata);
				}
				this.data.EmpireDataList.Add(empireToSave);
			}
			this.data.Snapshots = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> Entry in StatTracker.SnapshotsDict)
			{
				this.data.Snapshots.Add(Entry.Key, Entry.Value);
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.data.path = path;
			this.data.SaveAs = SaveAs;
			this.data.Size = screenToSave.Size;
			this.data.FogMapName = string.Concat(SaveAs, "fog");
			screenToSave.FogMap.Save(string.Concat(path, "/StarDrive/Saved Games/Fog Maps/", SaveAs, "fog.png"), ImageFileFormat.Png);
			SavedGame.thread = new Thread(new ParameterizedThreadStart(this.DoSave));
			SavedGame.thread.Start(this.data);
		}

		private void DoSave(object info)
		{
			SavedGame.UniverseSaveData data = (SavedGame.UniverseSaveData)info;
			XmlSerializer Serializer = new XmlSerializer(typeof(SavedGame.UniverseSaveData));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(data.path, "/StarDrive/Saved Games/", data.SaveAs, ".xml"));
			Serializer.Serialize(WriteFileStream, data);
			WriteFileStream.Close();
			WriteFileStream.Dispose();
			FileInfo fi = new FileInfo(string.Concat(data.path, "/StarDrive/Saved Games/", data.SaveAs, ".xml"));
			HelperFunctions.Compress(fi);
			try
			{
				fi.Delete();
			}
			catch
			{
			}
			HeaderData header = new HeaderData()
			{
				PlayerName = data.PlayerLoyalty,
				StarDate = data.StarDate.ToString("#.0"),
				Time = DateTime.Now
			};
			string str = DateTime.Now.ToString("M/d/yyyy");
			DateTime now = DateTime.Now;
			header.RealDate = string.Concat(str, " ", now.ToShortTimeString());
			header.SaveName = data.SaveAs;
			if (GlobalStats.ActiveMod != null)
			{
				header.ModName = GlobalStats.ActiveMod.ModPath;
			}
			XmlSerializer Serializer1 = new XmlSerializer(typeof(HeaderData));
			TextWriter wf = new StreamWriter(string.Concat(data.path, "/StarDrive/Saved Games/Headers/", data.SaveAs, ".xml"));
			Serializer1.Serialize(wf, header);
			wf.Close();
			wf.Dispose();
			GC.Collect();
		}

		public struct EmpireSaveData
		{
			public string Name;

			public List<Relationship> Relations;

			public List<SavedGame.SpaceRoadSave> SpaceRoadData;

			public bool IsFaction;

            public bool isMinorRace;

			public RacialTrait Traits;

			public EmpireData empireData;

			public List<SavedGame.ShipSaveData> OwnedShips;

			public float Research;

			public float Money;

			public List<TechEntry> TechTree;

			public SavedGame.GSAISAVE GSAIData;

			public string ResearchTopic;

			public List<AO> AOs;

			public List<SavedGame.FleetSave> FleetsList;

            public string CurrentAutoFreighter;

            public string CurrentAutoColony;

            public string CurrentAutoScout;
		}

		public struct FleetSave
		{
			public bool IsCoreFleet;

			public string Name;

			public int TaskStep;

			public Vector2 Position;

			public Guid FleetGuid;

			public float facing;

			public int Key;

			public List<SavedGame.FleetShipSave> ShipsInFleet;

			public List<FleetDataNode> DataNodes;
		}

		public struct FleetShipSave
		{
			public Guid shipGuid;

			public Vector2 fleetOffset;
		}

		public struct GoalSave
		{
			public GoalType type;

			public int GoalStep;

			public Guid markedPlanetGuid;

			public Guid colonyShipGuid;

			public Vector2 BuildPosition;

			public string ToBuildUID;

			public Guid planetWhereBuildingAtGuid;

			public string GoalName;

			public Guid beingBuiltGUID;

			public Guid fleetGuid;

			public Guid GoalGuid;
		}

		public class GSAISAVE
		{
            public List<int> UsedFleets;

			public List<SavedGame.GoalSave> Goals;

			public List<MilitaryTask> MilitaryTaskList;

			public List<Guid> PinGuids;

            //[XmlIgnore]
			public List<ThreatMatrix.Pin>   PinList ;//= new List<ThreatMatrix.Pin>();
		}

		public struct PGSData
		{
			public int x;

			public int y;

			public List<Troop> TroopsHere;

			public bool Biosphere;

			public Building building;

			public bool Habitable;

			public int foodbonus;

			public int resbonus;

			public int prodbonus;
		}

		public struct PlanetSaveData
		{
			public Guid guid;

			public string Name;

			public string Owner;

			public float Population;

			public float PopulationMax;

			public float Fertility;

			public float Richness;

			public int WhichPlanet;

			public float OrbitalAngle;

			public float OrbitalDistance;

			public float Radius;

			public bool HasRings;

			public float farmerPercentage;

			public float workerPercentage;

			public float researcherPercentage;

			public float foodHere;

			public float prodHere;

			public List<SavedGame.PGSData> PGSList;

			public bool GovernorOn;

			public List<SavedGame.QueueItemSave> QISaveList;

			public Planet.ColonyType ColonyType;

			public Planet.GoodState FoodState;

			public int Crippled_Turns;

			public Planet.GoodState ProdState;

			public List<string> EmpiresThatKnowThisPlanet;

			public float TerraformPoints;

			public List<Guid> StationsList;

			public bool FoodLock;

			public bool ResLock;

			public bool ProdLock;

			public float ShieldStrength;
		}

		public struct ProjectileSaveData
		{
			public string Weapon;

			public float Duration;

			public float Rotation;

			public Vector2 Velocity;

			public Vector2 Position;
		}

		public struct QueueItemSave
		{
			public string UID;

			public Guid GoalGUID;

			public float ProgressTowards;

			public bool isBuilding;

			public bool isTroop;

			public bool isShip;

			public string DisplayName;

			public bool IsRefit;

			public float RefitCost;

			public Vector2 pgsVector;
		}

		public struct RingSave
		{
			public SavedGame.PlanetSaveData Planet;

			public bool Asteroids;

			public float OrbitalDistance;
		}

		public struct RoadNodeSave
		{
			public Vector2 Position;

			public Guid Guid_Platform;
		}

		public struct ShipAISave
		{
			public AIState state;

			public int numFood;

			public int numProd;

			public string FoodOrProd;

			public AIState defaultstate;

			public List<SavedGame.ShipGoalSave> ShipGoalsList;

			public List<Vector2> ActiveWayPoints;

			public Guid startGuid;

			public Guid endGuid;

			public int GoToStep;

			public Vector2 MovePosition;

			public Guid OrbitTarget;

			public Guid ColonizeTarget;

			public Guid SystemToDefend;

			public Guid AttackTarget;

			public Guid EscortTarget;
		}

		public struct ShipGoalSave
		{
			public ArtificialIntelligence.Plan Plan;

			public Guid goalGuid;

			public string VariableString;

			public Guid fleetGuid;

			public float SpeedLimit;

			public Vector2 MovePosition;

			public float DesiredFacing;

			public float FacingVector;

			public Guid TargetPlanetGuid;
		}

		public struct ShipSaveData
		{
			public Guid guid;

			public bool AfterBurnerOn;

			public SavedGame.ShipAISave AISave;

			public Vector2 Position;

			public Vector2 Velocity;

			public float Rotation;

			public ShipData data;

			public string Hull;

			public string Name;

			public bool IsPlayerShip;

			public float yRotation;

			public float Power;

			public float Ordnance;

			public string HomePlanet;

			public float InCombatTimer;

			public float experience;

			public int kills;

			public List<Troop> TroopList;

			public float FoodCount;

			public float ProdCount;

			public float PopCount;

			public Guid TetheredTo;

			public Vector2 TetherOffset;

			public List<SavedGame.ProjectileSaveData> Projectiles;
		}

		public struct SolarSystemSaveData
		{
			public Guid guid;

			public string SunPath;

			public string Name;

			public Vector2 Position;

			public List<SavedGame.RingSave> RingList;

			public List<Asteroid> AsteroidsList;

            public List<Moon> Moons;

			public List<string> EmpiresThatKnowThisSystem;
		}

		public struct SpaceRoadSave
		{
			public List<SavedGame.RoadNodeSave> RoadNodes;

			public Guid OriginGUID;

			public Guid DestGUID;
		}

		public class UniverseSaveData
		{
			public string path;

			public string SaveAs;

			public string FileName;

			public string FogMapName;

			public string PlayerLoyalty;

			public Vector2 campos;

			public float camheight;

			public Vector2 Size;

			public float StarDate;

			public float GameScale;

			public float GamePacing;

			public List<SavedGame.SolarSystemSaveData> SolarSystemDataList;

			public List<SavedGame.EmpireSaveData> EmpireDataList;

			public UniverseData.GameDifficulty gameDifficulty;

			public bool AutoExplore;

			public bool AutoColonize;

			public bool AutoFreighters;

			public bool AutoProjectors;

			public int RemnantKills;

			public bool RemnantArmageddon;

			public float FTLModifier = 1.0f;
            public float EnemyFTLModifier = 1.0f;

			public bool GravityWells;

			public RandomEvent RandomEvent;

			public SerializableDictionary<string, SerializableDictionary<int, Snapshot>> Snapshots;
            public float OptionIncreaseShipMaintenance=GlobalStats.OptionIncreaseShipMaintenance;
            public float MinimumWarpRange=GlobalStats.MinimumWarpRange;

            public float MemoryLimiter=GlobalStats.MemoryLimiter;
            
            public int IconSize;

            public byte TurnTimer;

            public bool preventFederations;
            public float GravityWellRange=GlobalStats.GravityWellRange;
            public bool EliminationMode;

			public UniverseSaveData()
			{
			}
		}

	}
}