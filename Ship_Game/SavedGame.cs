using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using System.Globalization;
using System.Configuration;
using MsgPack.Serialization;

namespace Ship_Game
{
	public sealed class SavedGame
	{
		private readonly UniverseSaveData SaveData = new UniverseSaveData();
		private static Thread SaveThread;

        public static bool IsSaving  => SaveThread != null && SaveThread.IsAlive;
        public static bool NotSaving => SaveThread == null || !SaveThread.IsAlive;

		public SavedGame(UniverseScreen screenToSave, string saveAs)
		{
		    SaveData.RemnantKills        = GlobalStats.RemnantKills;
            SaveData.RemnantActivation   = GlobalStats.RemnantActivation;
            SaveData.RemnantArmageddon   = GlobalStats.RemnantArmageddon;
			SaveData.gameDifficulty      = screenToSave.GameDifficulty;
			SaveData.AutoColonize        = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoColonize;
			SaveData.AutoExplore         = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoExplore;
			SaveData.AutoFreighters      = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoFreighters;
			SaveData.AutoProjectors      = EmpireManager.GetEmpireByName(screenToSave.PlayerLoyalty).AutoBuild;
			SaveData.GamePacing          = UniverseScreen.GamePaceStatic;
			SaveData.GameScale           = UniverseScreen.GameScaleStatic;
			SaveData.StarDate            = screenToSave.StarDate;
			SaveData.FTLModifier         = screenToSave.FTLModifier;
            SaveData.EnemyFTLModifier    = screenToSave.EnemyFTLModifier;
			SaveData.GravityWells        = screenToSave.GravityWells;
			SaveData.PlayerLoyalty       = screenToSave.PlayerLoyalty;
			SaveData.RandomEvent         = RandomEventManager.ActiveEvent;
			SaveData.campos              = new Vector2(screenToSave.camPos.X, screenToSave.camPos.Y);
			SaveData.camheight           = screenToSave.camHeight;
            SaveData.MemoryLimiter       = GlobalStats.MemoryLimiter;
            SaveData.MinimumWarpRange    = GlobalStats.MinimumWarpRange;
            SaveData.TurnTimer           = GlobalStats.TurnTimer;
            SaveData.IconSize            = GlobalStats.IconSize;
            SaveData.preventFederations  = GlobalStats.preventFederations;
            SaveData.GravityWellRange    = GlobalStats.GravityWellRange;
            SaveData.EliminationMode     = GlobalStats.EliminationMode;
			SaveData.EmpireDataList      = new List<EmpireSaveData>();
			SaveData.SolarSystemDataList = new List<SolarSystemSaveData>();
            SaveData.OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            

			foreach (SolarSystem system in UniverseScreen.SolarSystemList)
			{
				SolarSystemSaveData sysSave = new SolarSystemSaveData
				{
					Name = system.Name,
					Position = system.Position,
					SunPath = system.SunPath,
					AsteroidsList = new List<Asteroid>(),
                    Moons = new List<Moon>(),
				};
				foreach (Asteroid roid in system.AsteroidsList)
				{
					sysSave.AsteroidsList.Add(roid);
				}
                foreach (Moon moon in system.MoonList)
                    sysSave.Moons.Add(moon);
				sysSave.guid = system.guid;
				sysSave.RingList = new List<RingSave>();
				foreach (SolarSystem.Ring ring in system.RingList)
				{
					RingSave rsave = new RingSave
					{
						Asteroids = ring.Asteroids,
						OrbitalDistance = ring.Distance
					};
					if (ring.planet == null)
					{
						sysSave.RingList.Add(rsave);
					}
					else
					{
						PlanetSaveData pdata = new PlanetSaveData
						{
							Crippled_Turns       = ring.planet.Crippled_Turns,
							guid                 = ring.planet.guid,
							FoodState            = ring.planet.fs,
							ProdState            = ring.planet.ps,
							FoodLock             = ring.planet.FoodLocked,
							ProdLock             = ring.planet.ProdLocked,
							ResLock              = ring.planet.ResLocked,
							Name                 = ring.planet.Name,
                            Scale                = ring.planet.scale,
							ShieldStrength       = ring.planet.ShieldStrengthCurrent,
							Population           = ring.planet.Population,
							PopulationMax        = ring.planet.MaxPopulation,
							Fertility            = ring.planet.Fertility,
							Richness             = ring.planet.MineralRichness,
							Owner                = ring.planet.Owner?.data.Traits.Name ?? "",
							WhichPlanet          = ring.planet.planetType,
							OrbitalAngle         = ring.planet.OrbitalAngle,
							OrbitalDistance      = ring.planet.OrbitalRadius,
							HasRings             = ring.planet.hasRings,
							Radius               = ring.planet.ObjectRadius,
							farmerPercentage     = ring.planet.FarmerPercentage,
							workerPercentage     = ring.planet.WorkerPercentage,
							researcherPercentage = ring.planet.ResearcherPercentage,
							foodHere             = ring.planet.FoodHere,
							TerraformPoints      = ring.planet.TerraformPoints,
							prodHere             = ring.planet.ProductionHere,
							GovernorOn           = ring.planet.GovernorOn,
							ColonyType           = ring.planet.colonyType,
							StationsList         = new List<Guid>(),
                            SpecialDescription = ring.planet.SpecialDescription
						};
						foreach (var station in ring.planet.Shipyards)
						{
							if (station.Value.Active) pdata.StationsList.Add(station.Key);
						}
						pdata.QISaveList = new List<SavedGame.QueueItemSave>();
						if (ring.planet.Owner != null)
						{
							foreach (QueueItem item in ring.planet.ConstructionQueue)
							{
								QueueItemSave qi = new QueueItemSave()
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
									qi.pgsVector = new Vector2(item.pgs.x, item.pgs.y);
								}
                                qi.isPlayerAdded = item.IsPlayerAdded;
								pdata.QISaveList.Add(qi);
							}
						}
						pdata.PGSList = new List<SavedGame.PGSData>();
						foreach (PlanetGridSquare tile in ring.planet.TilesList)
						{
						    PGSData pgs = new PGSData
						    {
						        x          = tile.x,
						        y          = tile.y,
						        resbonus   = tile.resbonus,
						        prodbonus  = tile.prodbonus,
						        Habitable  = tile.Habitable,
						        foodbonus  = tile.foodbonus,
						        Biosphere  = tile.Biosphere,
						        building   = tile.building,
						        TroopsHere = tile.TroopsHere
						    };
						    pdata.PGSList.Add(pgs);
						}
						pdata.EmpiresThatKnowThisPlanet = new List<string>();
						foreach (var explored in system.ExploredDict)
						{
							if (explored.Value)
							    pdata.EmpiresThatKnowThisPlanet.Add(explored.Key.data.Traits.Name);
						}
						rsave.Planet = pdata;
						sysSave.RingList.Add(rsave);
					}
					sysSave.EmpiresThatKnowThisSystem = new List<string>();
					foreach (var explored in system.ExploredDict)
					{
						if (explored.Value)
						    sysSave.EmpiresThatKnowThisSystem.Add(explored.Key.data.Traits.Name); // @todo This is a duplicate??
					}
				}
				SaveData.SolarSystemDataList.Add(sysSave);
			}
			
            foreach (Empire e in EmpireManager.EmpireList)
			{
				EmpireSaveData empireToSave = new EmpireSaveData
				{
					IsFaction   = e.isFaction,
                    isMinorRace = e.MinorRace,
					Relations   = new List<Relationship>()
				};
				foreach (KeyValuePair<Empire, Relationship> relation in e.AllRelations)
				{
					empireToSave.Relations.Add(relation.Value);
				}
				empireToSave.Name                 = e.data.Traits.Name;
				empireToSave.empireData           = e.data.GetClone();
				empireToSave.Traits               = e.data.Traits;
				empireToSave.Research             = e.Research;
				empireToSave.ResearchTopic        = e.ResearchTopic;
				empireToSave.Money                = e.Money;
                empireToSave.CurrentAutoScout     = e.data.CurrentAutoScout;
                empireToSave.CurrentAutoFreighter = e.data.CurrentAutoFreighter;
                empireToSave.CurrentAutoColony    = e.data.CurrentAutoColony;
                empireToSave.CurrentConstructor   = e.data.CurrentConstructor;
				empireToSave.OwnedShips           = new List<ShipSaveData>();
				empireToSave.TechTree             = new List<TechEntry>();
				foreach (AO area in e.GetGSAI().AreasOfOperations)
				{
					area.PrepareForSave();
				}
				empireToSave.AOs = e.GetGSAI().AreasOfOperations;
				empireToSave.FleetsList = new List<FleetSave>();
				foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
				{
					FleetSave fs = new FleetSave()
					{
						Name        = fleet.Value.Name,
						IsCoreFleet = fleet.Value.IsCoreFleet,
						TaskStep    = fleet.Value.TaskStep,
						Key         = fleet.Key,
						facing      = fleet.Value.facing,
						FleetGuid   = fleet.Value.guid,
						Position    = fleet.Value.Position,
						ShipsInFleet = new List<FleetShipSave>()
					};
					foreach (FleetDataNode node in fleet.Value.DataNodes)
					{
						if (node.Ship== null)
						{
							continue;
						}
						node.ShipGuid = node.Ship.guid;
					}
					fs.DataNodes = fleet.Value.DataNodes;
					foreach (Ship ship in fleet.Value.Ships)
					{
						FleetShipSave ssave = new FleetShipSave()
						{
							fleetOffset = ship.RelativeFleetOffset,
							shipGuid = ship.guid
						};
						fs.ShipsInFleet.Add(ssave);
					}
					empireToSave.FleetsList.Add(fs);
				}
				empireToSave.SpaceRoadData = new List<SpaceRoadSave>();
				foreach (SpaceRoad road in e.SpaceRoadsList)
				{
					SpaceRoadSave rdata = new SpaceRoadSave()
					{
						OriginGUID = road.GetOrigin().guid,
						DestGUID = road.GetDestination().guid,
						RoadNodes = new List<RoadNodeSave>()
					};
					foreach (RoadNode node in road.RoadNodesList)
					{
						RoadNodeSave ndata = new RoadNodeSave()
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
				GSAISAVE gsaidata = new GSAISAVE()
				{
					UsedFleets = e.GetGSAI().UsedFleets,
					Goals      = new List<GoalSave>(),
					PinGuids   = new List<Guid>(),
					PinList    = new List<ThreatMatrix.Pin>()
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
				foreach (Goal g in e.GetGSAI().Goals)
				{
				    GoalSave gdata = new GoalSave()
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
				foreach (KeyValuePair<string, TechEntry> tech in e.GetTDict())
				{
					empireToSave.TechTree.Add(tech.Value);
				}

                foreach (Ship ship in e.GetShips())
				{
					ShipSaveData sdata = new ShipSaveData()
					{
						guid       = ship.guid,
						data       = ship.ToShipData(),
						Position   = ship.Position,
						experience = ship.experience,
						kills      = ship.kills,
						Velocity   = ship.Velocity,
                        
					};
					if (ship.GetTether() != null)
					{
						sdata.TetheredTo = ship.GetTether().guid;
						sdata.TetherOffset = ship.TetherOffset;
					}
					sdata.Name = ship.Name;
                    sdata.VanityName = ship.VanityName;
					if (ship.PlayerShip)
					{
						sdata.IsPlayerShip = true;
					}
					sdata.Hull          = ship.GetShipData().Hull;
					sdata.Power         = ship.PowerCurrent;
					sdata.Ordnance      = ship.Ordinance;
					sdata.yRotation     = ship.yRotation;
					sdata.Rotation      = ship.Rotation;
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

                    sdata.AreaOfOperation = ship.AreaOfOperation;
               
					sdata.AISave = new ShipAISave()
					{
						FoodOrProd = ship.GetAI().FoodOrProd,
						state      = ship.GetAI().State
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
					sdata.AISave.ShipGoalsList = new List<ShipGoalSave>();
					foreach (ArtificialIntelligence.ShipGoal sgoal in ship.GetAI().OrderQueue)
					{
						ShipGoalSave gsave = new ShipGoalSave()
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
					sdata.Projectiles = new List<ProjectileSaveData>();
					foreach (Projectile p in ship.Projectiles)
					{
					    ProjectileSaveData pdata = new ProjectileSaveData()
					    {
					        Velocity = p.Velocity,
					        Rotation = p.Rotation,
					        Weapon   = p.weapon.UID,
					        Position = p.Center,
					        Duration = p.duration
					    };
					    sdata.Projectiles.Add(pdata);
					}
					empireToSave.OwnedShips.Add(sdata);
				}

                foreach (Ship ship in e.GetProjectors())  //fbedard
                {
                    ShipSaveData sdata = new ShipSaveData()
                    {
                        guid       = ship.guid,
                        data       = ship.ToShipData(),
                        Position   = ship.Position,
                        experience = ship.experience,
                        kills      = ship.kills,
                        Velocity   = ship.Velocity,

                    };
                    if (ship.GetTether() != null)
                    {
                        sdata.TetheredTo = ship.GetTether().guid;
                        sdata.TetherOffset = ship.TetherOffset;
                    }
                    sdata.Name = ship.Name;
                    sdata.VanityName = ship.VanityName;
                    if (ship.PlayerShip)
                    {
                        sdata.IsPlayerShip = true;
                    }
                    sdata.Hull          = ship.GetShipData().Hull;
                    sdata.Power         = ship.PowerCurrent;
                    sdata.Ordnance      = ship.Ordinance;
                    sdata.yRotation     = ship.yRotation;
                    sdata.Rotation      = ship.Rotation;
                    sdata.InCombatTimer = ship.InCombatTimer;
                    sdata.AISave = new ShipAISave
                    {
                        FoodOrProd      = ship.GetAI().FoodOrProd,
                        state           = ship.GetAI().State,
                        defaultstate    = ship.GetAI().DefaultAIState,
                        GoToStep        = ship.GetAI().GotoStep,
                        MovePosition    = ship.GetAI().MovePosition,
                        ActiveWayPoints = new List<Vector2>(),
                        ShipGoalsList   = new List<ShipGoalSave>(),
                    };
                    sdata.Projectiles = new List<ProjectileSaveData>();
                    empireToSave.OwnedShips.Add(sdata);
                }

				SaveData.EmpireDataList.Add(empireToSave);
			}
			SaveData.Snapshots = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
			foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> Entry in StatTracker.SnapshotsDict)
			{
				SaveData.Snapshots.Add(Entry.Key, Entry.Value);
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			SaveData.path = path;
			SaveData.SaveAs = saveAs;
			SaveData.Size = screenToSave.Size;
			SaveData.FogMapName = saveAs + "fog";
			screenToSave.FogMap.Save(path + "/StarDrive/Saved Games/Fog Maps/" + saveAs + "fog.png", ImageFileFormat.Png);
		    SaveThread = new Thread(SaveUniverseDataAsync) {Name = "Save Thread: " + saveAs};
		    SaveThread.Start(SaveData);
		}

		private static void SaveUniverseDataAsync(object universeSaveData)
		{
			UniverseSaveData data = (UniverseSaveData)universeSaveData;
            try
            {
            #if true // new MsgPack file saving
                FileInfo info = new FileInfo(data.path + "/StarDrive/Saved Games/" + data.SaveAs + ".sav");
                using (FileStream writeStream = info.OpenWrite())
                    MessagePackSerializer.Get<UniverseSaveData>().Pack(writeStream, data);
                HelperFunctions.Compress(info);
                info.Delete();
            #else // old XML file saving (ugh 100MB XML files)
                FileInfo info = new FileInfo(data.path + "/StarDrive/Saved Games/" + data.SaveAs + ".xml");
                using (FileStream writeStream = info.OpenWrite())
                    new XmlSerializer(typeof(UniverseSaveData)).Serialize(writeStream, data);
                HelperFunctions.Compress(info);
                info.Delete();
            #endif
            }
            catch
            {
            }

            DateTime now = DateTime.Now;
			HeaderData header = new HeaderData
			{
				PlayerName = data.PlayerLoyalty,
				StarDate   = data.StarDate.ToString("#.0"),
				Time       = now,
                SaveName   = data.SaveAs,
                RealDate   = now.ToString("M/d/yyyy") + " " + now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat),
                ModPath    = GlobalStats.ActiveMod?.ModPath ?? "",
                ModName    = GlobalStats.ActiveMod?.mi.ModName ?? "",
                Version    = Convert.ToInt32(ConfigurationManager.AppSettings["SaveVersion"])
			};
            using (var wf = new StreamWriter(data.path + "/StarDrive/Saved Games/Headers/" + data.SaveAs + ".xml"))
                new XmlSerializer(typeof(HeaderData)).Serialize(wf, header);
		}

        public static UniverseSaveData DeserializeCompressedSave(FileInfo compressedSave)
        {
            UniverseSaveData usData;
            FileInfo decompressed = new FileInfo(HelperFunctions.Decompress(compressedSave));

            if (decompressed.Extension == "sav") // new MsgPack savegame format
            {
                var serializer = MessagePackSerializer.Get<UniverseSaveData>();
                using (FileStream stream = decompressed.OpenRead())
                    usData = serializer.Unpack(stream);
            }
            else // old 100MB XML savegame format (haha)
            {
                XmlSerializer serializer1;
                try
                {
                    serializer1 = new XmlSerializer(typeof(UniverseSaveData));
                }
                catch
                {
                    var attributeOverrides = new XmlAttributeOverrides();
                    attributeOverrides.Add(typeof(SolarSystemSaveData), "MoonList", new XmlAttributes { XmlIgnore = true });
                    attributeOverrides.Add(typeof(EmpireSaveData), "MoonList", new XmlAttributes { XmlIgnore = true });
                    serializer1 = new XmlSerializer(typeof(UniverseSaveData), attributeOverrides);
                }

                using (FileStream stream = decompressed.OpenRead())
                    usData = (UniverseSaveData)serializer1.Deserialize(stream);
            }
            decompressed.Delete();
            return usData;
        }

        public struct EmpireSaveData
		{
			[MessagePackMember(0)] public string Name;
			[MessagePackMember(1)] public List<Relationship> Relations;
			[MessagePackMember(2)] public List<SpaceRoadSave> SpaceRoadData;
			[MessagePackMember(3)] public bool IsFaction;
            [MessagePackMember(4)] public bool isMinorRace;
			[MessagePackMember(5)] public RacialTrait Traits;
			[MessagePackMember(6)] public EmpireData empireData;
			[MessagePackMember(7)] public List<ShipSaveData> OwnedShips;
			[MessagePackMember(8)] public float Research;
			[MessagePackMember(9)] public float Money;
			[MessagePackMember(10)] public List<TechEntry> TechTree;
			[MessagePackMember(11)] public GSAISAVE GSAIData;
			[MessagePackMember(12)] public string ResearchTopic;
			[MessagePackMember(13)] public List<AO> AOs;
			[MessagePackMember(14)] public List<FleetSave> FleetsList;
            [MessagePackMember(15)] public string CurrentAutoFreighter;
            [MessagePackMember(16)] public string CurrentAutoColony;
            [MessagePackMember(17)] public string CurrentAutoScout;
            [MessagePackMember(18)] public string CurrentConstructor;
		}

		public struct FleetSave
		{
            [MessagePackMember(0)] public bool IsCoreFleet;
            [MessagePackMember(1)] public string Name;
            [MessagePackMember(2)] public int TaskStep;
            [MessagePackMember(3)] public Vector2 Position;
            [MessagePackMember(4)] public Guid FleetGuid;
            [MessagePackMember(5)] public float facing;
            [MessagePackMember(6)] public int Key;
            [MessagePackMember(7)] public List<FleetShipSave> ShipsInFleet;
            [MessagePackMember(8)] public List<FleetDataNode> DataNodes;
		}

		public struct FleetShipSave
		{
			[MessagePackMember(0)] public Guid shipGuid;
			[MessagePackMember(1)] public Vector2 fleetOffset;
		}

		public struct GoalSave
		{
			[MessagePackMember(0)] public GoalType type;
			[MessagePackMember(1)] public int GoalStep;
			[MessagePackMember(2)] public Guid markedPlanetGuid;
			[MessagePackMember(3)] public Guid colonyShipGuid;
			[MessagePackMember(4)] public Vector2 BuildPosition;
			[MessagePackMember(5)] public string ToBuildUID;
			[MessagePackMember(6)] public Guid planetWhereBuildingAtGuid;
			[MessagePackMember(7)] public string GoalName;
			[MessagePackMember(8)] public Guid beingBuiltGUID;
			[MessagePackMember(9)] public Guid fleetGuid;
			[MessagePackMember(10)] public Guid GoalGuid;
		}

		public class GSAISAVE
		{
            [MessagePackMember(0)] public List<int> UsedFleets;
			[MessagePackMember(1)] public List<GoalSave> Goals;
			[MessagePackMember(2)] public List<MilitaryTask> MilitaryTaskList;
			[MessagePackMember(3)] public List<Guid> PinGuids;
			[MessagePackMember(4)] public List<ThreatMatrix.Pin> PinList;
		}

		public struct PGSData
		{
			[MessagePackMember(0)] public int x;
			[MessagePackMember(1)] public int y;
			[MessagePackMember(2)] public List<Troop> TroopsHere;
			[MessagePackMember(3)] public bool Biosphere;
			[MessagePackMember(4)] public Building building;
			[MessagePackMember(5)] public bool Habitable;
			[MessagePackMember(6)] public int foodbonus;
			[MessagePackMember(7)] public int resbonus;
			[MessagePackMember(8)] public int prodbonus;
		}

		public struct PlanetSaveData
		{
			[MessagePackMember(0)] public Guid guid;
            [MessagePackMember(1)] public string SpecialDescription;
			[MessagePackMember(2)] public string Name;
            [MessagePackMember(3)] public float Scale;
			[MessagePackMember(4)] public string Owner;
			[MessagePackMember(5)] public float Population;
			[MessagePackMember(6)] public float PopulationMax;
			[MessagePackMember(7)] public float Fertility;
			[MessagePackMember(8)] public float Richness;
			[MessagePackMember(9)] public int WhichPlanet;
			[MessagePackMember(10)] public float OrbitalAngle;
			[MessagePackMember(11)] public float OrbitalDistance;
			[MessagePackMember(12)] public float Radius;
			[MessagePackMember(13)] public bool HasRings;
			[MessagePackMember(14)] public float farmerPercentage;
			[MessagePackMember(15)] public float workerPercentage;
			[MessagePackMember(16)] public float researcherPercentage;
			[MessagePackMember(17)] public float foodHere;
			[MessagePackMember(18)] public float prodHere;
			[MessagePackMember(19)] public List<PGSData> PGSList;
			[MessagePackMember(20)] public bool GovernorOn;
			[MessagePackMember(21)] public List<QueueItemSave> QISaveList;
			[MessagePackMember(22)] public Planet.ColonyType ColonyType;
			[MessagePackMember(23)] public Planet.GoodState FoodState;
			[MessagePackMember(24)] public int Crippled_Turns;
			[MessagePackMember(25)] public Planet.GoodState ProdState;
			[MessagePackMember(26)] public List<string> EmpiresThatKnowThisPlanet;
			[MessagePackMember(27)] public float TerraformPoints;
			[MessagePackMember(28)] public List<Guid> StationsList;
			[MessagePackMember(29)] public bool FoodLock;
			[MessagePackMember(30)] public bool ResLock;
			[MessagePackMember(31)] public bool ProdLock;
			[MessagePackMember(32)] public float ShieldStrength;
		}

		public struct ProjectileSaveData
		{
			[MessagePackMember(0)] public string Weapon;
			[MessagePackMember(1)] public float Duration;
			[MessagePackMember(2)] public float Rotation;
			[MessagePackMember(3)] public Vector2 Velocity;
			[MessagePackMember(4)] public Vector2 Position;
		}

		public struct QueueItemSave
		{
			[MessagePackMember(0)] public string UID;
			[MessagePackMember(1)] public Guid GoalGUID;
			[MessagePackMember(2)] public float ProgressTowards;
			[MessagePackMember(3)] public bool isBuilding;
			[MessagePackMember(4)] public bool isTroop;
			[MessagePackMember(5)] public bool isShip;
			[MessagePackMember(6)] public string DisplayName;
			[MessagePackMember(7)] public bool IsRefit;
			[MessagePackMember(8)] public float RefitCost;
			[MessagePackMember(9)] public Vector2 pgsVector;
            [MessagePackMember(10)] public bool isPlayerAdded;
		}

		public struct RingSave
		{
			[MessagePackMember(0)] public PlanetSaveData Planet;
			[MessagePackMember(1)] public bool Asteroids;
			[MessagePackMember(2)] public float OrbitalDistance;
		}

		public struct RoadNodeSave
		{
			[MessagePackMember(0)] public Vector2 Position;
			[MessagePackMember(1)] public Guid Guid_Platform;
		}

		public struct ShipAISave
		{
			[MessagePackMember(0)] public AIState state;
			[MessagePackMember(1)] public int numFood;
			[MessagePackMember(2)] public int numProd;
			[MessagePackMember(3)] public string FoodOrProd;
			[MessagePackMember(4)] public AIState defaultstate;
			[MessagePackMember(5)] public List<ShipGoalSave> ShipGoalsList;
			[MessagePackMember(6)] public List<Vector2> ActiveWayPoints;
			[MessagePackMember(7)] public Guid startGuid;
			[MessagePackMember(8)] public Guid endGuid;
			[MessagePackMember(9)] public int GoToStep;
			[MessagePackMember(10)] public Vector2 MovePosition;
			[MessagePackMember(11)] public Guid OrbitTarget;
			[MessagePackMember(12)] public Guid ColonizeTarget;
			[MessagePackMember(13)] public Guid SystemToDefend;
			[MessagePackMember(14)] public Guid AttackTarget;
			[MessagePackMember(15)] public Guid EscortTarget;
		}

		public struct ShipGoalSave
		{
			[MessagePackMember(0)] public ArtificialIntelligence.Plan Plan;
			[MessagePackMember(1)] public Guid goalGuid;
			[MessagePackMember(2)] public string VariableString;
			[MessagePackMember(3)] public Guid fleetGuid;
			[MessagePackMember(4)] public float SpeedLimit;
			[MessagePackMember(5)] public Vector2 MovePosition;
			[MessagePackMember(6)] public float DesiredFacing;
			[MessagePackMember(7)] public float FacingVector;
			[MessagePackMember(8)] public Guid TargetPlanetGuid;
		}

		public struct ShipSaveData
		{
			[MessagePackMember(0)] public Guid guid;
			[MessagePackMember(1)] public bool AfterBurnerOn;
			[MessagePackMember(2)] public ShipAISave AISave;
			[MessagePackMember(3)] public Vector2 Position;
			[MessagePackMember(4)] public Vector2 Velocity;
			[MessagePackMember(5)] public float Rotation;
			[MessagePackMember(6)] public ShipData data;
			[MessagePackMember(7)] public string Hull;
			[MessagePackMember(8)] public string Name;
            [MessagePackMember(9)] public string VanityName;
			[MessagePackMember(10)] public bool IsPlayerShip;
			[MessagePackMember(11)] public float yRotation;
			[MessagePackMember(12)] public float Power;
			[MessagePackMember(13)] public float Ordnance;
			[MessagePackMember(14)] public float InCombatTimer;
			[MessagePackMember(15)] public float experience;
			[MessagePackMember(16)] public int kills;
			[MessagePackMember(17)] public List<Troop> TroopList;
            [MessagePackMember(18)] public List<Rectangle> AreaOfOperation;
			[MessagePackMember(19)] public float FoodCount;
			[MessagePackMember(20)] public float ProdCount;
			[MessagePackMember(21)] public float PopCount;
			[MessagePackMember(22)] public Guid TetheredTo;
			[MessagePackMember(23)] public Vector2 TetherOffset;
			[MessagePackMember(24)] public List<ProjectileSaveData> Projectiles;
		}

		public struct SolarSystemSaveData
		{
			[MessagePackMember(0)] public Guid guid;
			[MessagePackMember(1)] public string SunPath;
			[MessagePackMember(2)] public string Name;
			[MessagePackMember(3)] public Vector2 Position;
			[MessagePackMember(4)] public List<RingSave> RingList;
			[MessagePackMember(5)] public List<Asteroid> AsteroidsList;
            [MessagePackMember(6)] public List<Moon> Moons;
			[MessagePackMember(7)] public List<string> EmpiresThatKnowThisSystem;
		}

		public struct SpaceRoadSave
		{
			[MessagePackMember(0)] public List<RoadNodeSave> RoadNodes;
			[MessagePackMember(1)] public Guid OriginGUID;
			[MessagePackMember(2)] public Guid DestGUID;
		}

		public class UniverseSaveData
		{
			[MessagePackMember(0)] public string path;
			[MessagePackMember(1)] public string SaveAs;
			[MessagePackMember(2)] public string FileName;
			[MessagePackMember(3)] public string FogMapName;
			[MessagePackMember(4)] public string PlayerLoyalty;
			[MessagePackMember(5)] public Vector2 campos;
			[MessagePackMember(6)] public float camheight;
			[MessagePackMember(7)] public Vector2 Size;
			[MessagePackMember(8)] public float StarDate;
			[MessagePackMember(9)] public float GameScale;
			[MessagePackMember(10)] public float GamePacing;
			[MessagePackMember(11)] public List<SolarSystemSaveData> SolarSystemDataList;
			[MessagePackMember(12)] public List<EmpireSaveData> EmpireDataList;
			[MessagePackMember(13)] public UniverseData.GameDifficulty gameDifficulty;
			[MessagePackMember(14)] public bool AutoExplore;
			[MessagePackMember(15)] public bool AutoColonize;
			[MessagePackMember(16)] public bool AutoFreighters;
			[MessagePackMember(17)] public bool AutoProjectors;
			[MessagePackMember(18)] public int RemnantKills;
            [MessagePackMember(19)] public int RemnantActivation;
            [MessagePackMember(20)] public bool RemnantArmageddon;
			[MessagePackMember(21)] public float FTLModifier = 1.0f;
            [MessagePackMember(22)] public float EnemyFTLModifier = 1.0f;
			[MessagePackMember(23)] public bool GravityWells;
			[MessagePackMember(24)] public RandomEvent RandomEvent;
			[MessagePackMember(25)] public SerializableDictionary<string, SerializableDictionary<int, Snapshot>> Snapshots;
            [MessagePackMember(26)] public float OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            [MessagePackMember(27)] public float MinimumWarpRange = GlobalStats.MinimumWarpRange;
            [MessagePackMember(28)] public float MemoryLimiter    = GlobalStats.MemoryLimiter;
            [MessagePackMember(29)] public int IconSize;
            [MessagePackMember(30)] public byte TurnTimer;
            [MessagePackMember(31)] public bool preventFederations;
            [MessagePackMember(32)] public float GravityWellRange = GlobalStats.GravityWellRange;
            [MessagePackMember(33)] public bool EliminationMode;
		}

	}
}