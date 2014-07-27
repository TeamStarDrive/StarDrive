using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Configuration;

namespace Ship_Game.Gameplay
{
	public class GSAI
	{
		public string EmpireName;

		private Empire empire;

		public BatchRemovalCollection<Goal> Goals = new BatchRemovalCollection<Goal>();

		public Ship_Game.Gameplay.ThreatMatrix ThreatMatrix = new Ship_Game.Gameplay.ThreatMatrix();

		public Ship_Game.DefensiveCoordinator DefensiveCoordinator;

		private int desired_ColonyGoals = 2;

		private List<SolarSystem> MarkedForExploration = new List<SolarSystem>();

		public List<AO> AreasOfOperations = new List<AO>();

		private int DesiredAgentsPerHostile = 2;

		private int DesiredAgentsPerNeutral = 1;

		private int DesiredAgentCount;

		private int BaseAgents;

		public List<int> UsedFleets = new List<int>();

		public BatchRemovalCollection<MilitaryTask> TaskList = new BatchRemovalCollection<MilitaryTask>();

		private int numberOfShipGoals = 6;

		private int numberTroopGoals = 2;

		private Dictionary<Ship, List<Ship>> InterceptorDict = new Dictionary<Ship, List<Ship>>();

		public List<MilitaryTask> TasksToAdd = new List<MilitaryTask>();

		public int num_current_invasion_tasks;

		private List<Planet> DesiredPlanets = new List<Planet>();

		private int FirstDemand = 20;

		private int SecondDemand = 75;

		//private int ThirdDemand = 75;

		private GSAI.ResearchStrategy res_strat = GSAI.ResearchStrategy.Scripted;
        bool modSupport = bool.Parse( ConfigurationManager.AppSettings["ModSupport"]);
        float minimumWarpRange = GlobalStats.MinimumWarpRange;
        //SizeLimiter
        float SizeLimiter = GlobalStats.MemoryLimiter;

		public GSAI(Empire e)
		{
			this.EmpireName = e.data.Traits.Name;
			this.empire = e;
			this.DefensiveCoordinator = new Ship_Game.DefensiveCoordinator(e);
			if (this.empire.data.EconomicPersonality != null)
			{
				this.numberOfShipGoals = this.numberOfShipGoals + this.empire.data.EconomicPersonality.ShipGoalsPlus;
			}
		}

		public void AcceptOffer(Offer ToUs, Offer FromUs, Empire us, Empire Them)
		{
			if (ToUs.PeaceTreaty)
			{
				this.empire.GetRelations()[Them].AtWar = false;
				this.empire.GetRelations()[Them].PreparingForWar = false;
				this.empire.GetRelations()[Them].ActiveWar.EndStarDate = this.empire.GetUS().StarDate;
				this.empire.GetRelations()[Them].WarHistory.Add(this.empire.GetRelations()[Them].ActiveWar);
				if (this.empire.data.DiplomaticPersonality != null)
				{
					this.empire.GetRelations()[Them].Posture = Posture.Neutral;
					if (this.empire.GetRelations()[Them].Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
					{
						this.empire.GetRelations()[Them].Anger_FromShipsInOurBorders = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
					}
					if (this.empire.GetRelations()[Them].Anger_TerritorialConflict > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
					{
						this.empire.GetRelations()[Them].Anger_TerritorialConflict = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
					}
				}
				this.empire.GetRelations()[Them].Anger_MilitaryConflict = 0f;
				this.empire.GetRelations()[Them].WarnedAboutShips = false;
				this.empire.GetRelations()[Them].WarnedAboutColonizing = false;
				this.empire.GetRelations()[Them].HaveRejected_Demand_Tech = false;
				this.empire.GetRelations()[Them].HaveRejected_OpenBorders = false;
				this.empire.GetRelations()[Them].HaveRejected_TRADE = false;
				this.empire.GetRelations()[Them].HasDefenseFleet = false;
				if (this.empire.GetRelations()[Them].DefenseFleet != -1)
				{
					this.empire.GetFleetsDict()[this.empire.GetRelations()[Them].DefenseFleet].Task.EndTask();
				}
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in this.TaskList)
					{
						if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != Them)
						{
							continue;
						}
						task.EndTask();
					}
				}
				this.empire.GetRelations()[Them].ActiveWar = null;
				Them.GetRelations()[this.empire].AtWar = false;
				Them.GetRelations()[this.empire].PreparingForWar = false;
				Them.GetRelations()[this.empire].ActiveWar.EndStarDate = Them.GetUS().StarDate;
				Them.GetRelations()[this.empire].WarHistory.Add(Them.GetRelations()[this.empire].ActiveWar);
				Them.GetRelations()[this.empire].Posture = Posture.Neutral;
				if (EmpireManager.GetEmpireByName(Them.GetUS().PlayerLoyalty) != Them)
				{
					if (Them.GetRelations()[this.empire].Anger_FromShipsInOurBorders > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
					{
						Them.GetRelations()[this.empire].Anger_FromShipsInOurBorders = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
					}
					if (Them.GetRelations()[this.empire].Anger_TerritorialConflict > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
					{
						Them.GetRelations()[this.empire].Anger_TerritorialConflict = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
					}
					Them.GetRelations()[this.empire].Anger_MilitaryConflict = 0f;
					Them.GetRelations()[this.empire].WarnedAboutShips = false;
					Them.GetRelations()[this.empire].WarnedAboutColonizing = false;
					Them.GetRelations()[this.empire].HaveRejected_Demand_Tech = false;
					Them.GetRelations()[this.empire].HaveRejected_OpenBorders = false;
					Them.GetRelations()[this.empire].HaveRejected_TRADE = false;
					if (Them.GetRelations()[this.empire].DefenseFleet != -1)
					{
						Them.GetFleetsDict()[Them.GetRelations()[this.empire].DefenseFleet].Task.EndTask();
					}
					lock (GlobalStats.TaskLocker)
					{
						foreach (MilitaryTask task in Them.GetGSAI().TaskList)
						{
							if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != this.empire)
							{
								continue;
							}
							task.EndTask();
						}
					}
				}
				Them.GetRelations()[this.empire].ActiveWar = null;
				if (Them == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) || this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
				{
					Ship.universeScreen.NotificationManager.AddPeaceTreatyEnteredNotification(this.empire, Them);
				}
				else if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[Them].Known && EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Known)
				{
					Ship.universeScreen.NotificationManager.AddPeaceTreatyEnteredNotification(this.empire, Them);
				}
			}
			if (ToUs.NAPact)
			{
				us.GetRelations()[Them].Treaty_NAPact = true;
				TrustEntry te = new TrustEntry();
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != us)
				{
					string name = us.data.DiplomaticPersonality.Name;
					string str = name;
					if (name != null)
					{
						if (str == "Pacifist")
						{
							te.TrustCost = 0f;
						}
						else if (str == "Cunning")
						{
							te.TrustCost = 0f;
						}
						else if (str == "Xenophobic")
						{
							te.TrustCost = 15f;
						}
						else if (str == "Aggressive")
						{
							te.TrustCost = 35f;
						}
						else if (str == "Honorable")
						{
							te.TrustCost = 5f;
						}
						else if (str == "Ruthless")
						{
							te.TrustCost = 50f;
						}
					}
				}
				te.Type = TrustEntryType.Treaty;
				us.GetRelations()[Them].TrustEntries.Add(te);
			}
			if (FromUs.NAPact)
			{
				Them.GetRelations()[us].Treaty_NAPact = true;
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != Them)
				{
					TrustEntry te = new TrustEntry();
					string name1 = Them.data.DiplomaticPersonality.Name;
					string str1 = name1;
					if (name1 != null)
					{
						if (str1 == "Pacifist")
						{
							te.TrustCost = 0f;
						}
						else if (str1 == "Cunning")
						{
							te.TrustCost = 0f;
						}
						else if (str1 == "Xenophobic")
						{
							te.TrustCost = 15f;
						}
						else if (str1 == "Aggressive")
						{
							te.TrustCost = 35f;
						}
						else if (str1 == "Honorable")
						{
							te.TrustCost = 5f;
						}
						else if (str1 == "Ruthless")
						{
							te.TrustCost = 50f;
						}
					}
					te.Type = TrustEntryType.Treaty;
					Them.GetRelations()[us].TrustEntries.Add(te);
				}
			}
			if (ToUs.TradeTreaty)
			{
				us.GetRelations()[Them].Treaty_Trade = true;
				us.GetRelations()[Them].Treaty_Trade_TurnsExisted = 0;
				TrustEntry te = new TrustEntry()
				{
					TrustCost = 0.1f,
					Type = TrustEntryType.Treaty
				};
				us.GetRelations()[Them].TrustEntries.Add(te);
			}
			if (FromUs.TradeTreaty)
			{
				Them.GetRelations()[us].Treaty_Trade = true;
				Them.GetRelations()[us].Treaty_Trade_TurnsExisted = 0;
				TrustEntry te = new TrustEntry()
				{
					TrustCost = 0.1f,
					Type = TrustEntryType.Treaty
				};
				Them.GetRelations()[us].TrustEntries.Add(te);
			}
			if (ToUs.OpenBorders)
			{
				us.GetRelations()[Them].Treaty_OpenBorders = true;
				TrustEntry te = new TrustEntry()
				{
					TrustCost = 5f,
					Type = TrustEntryType.Treaty
				};
				us.GetRelations()[Them].TrustEntries.Add(te);
			}
			if (FromUs.OpenBorders)
			{
				Them.GetRelations()[us].Treaty_OpenBorders = true;
				TrustEntry te = new TrustEntry()
				{
					TrustCost = 5f,
					Type = TrustEntryType.Treaty
				};
				Them.GetRelations()[us].TrustEntries.Add(te);
			}
			foreach (string tech in FromUs.TechnologiesOffered)
			{
				Them.UnlockTech(tech);
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) == us)
				{
					continue;
				}
				TrustEntry te = new TrustEntry()
				{
					TrustCost = (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
					TurnTimer = 40,
					Type = TrustEntryType.Technology
				};
				us.GetRelations()[Them].TrustEntries.Add(te);
			}
			foreach (string tech in ToUs.TechnologiesOffered)
			{
				us.UnlockTech(tech);
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) == Them)
				{
					continue;
				}
				TrustEntry te = new TrustEntry()
				{
					TrustCost = (Them.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
					Type = TrustEntryType.Treaty
				};
				Them.GetRelations()[us].TrustEntries.Add(te);
			}
			foreach (string Art in FromUs.ArtifactsOffered)
			{
				Artifact toGive = ResourceManager.ArtifactsDict[Art];
				foreach (Artifact arti in us.data.OwnedArtifacts)
				{
					if (arti.Name != Art)
					{
						continue;
					}
					toGive = arti;
				}
				this.RemoveArtifact(us, toGive);
				this.AddArtifact(Them, toGive);
			}
			foreach (string Art in ToUs.ArtifactsOffered)
			{
				Artifact toGive = ResourceManager.ArtifactsDict[Art];
				foreach (Artifact arti in Them.data.OwnedArtifacts)
				{
					if (arti.Name != Art)
					{
						continue;
					}
					toGive = arti;
				}
				this.RemoveArtifact(Them, toGive);
				this.AddArtifact(us, toGive);
			}
			foreach (string planetName in FromUs.ColoniesOffered)
			{
				List<Planet> toRemove = new List<Planet>();
				List<Ship> TroopShips = new List<Ship>();
				foreach (Planet p in us.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					foreach (PlanetGridSquare pgs in p.TilesList)
					{
						if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.empire)
						{
							continue;
						}
						pgs.TroopsHere[0].SetPlanet(p);
						TroopShips.Add(pgs.TroopsHere[0].Launch());
					}
					toRemove.Add(p);
					p.Owner = Them;
					Them.AddPlanet(p);
					if (Them != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
					{
						p.colonyType = Them.AssessColonyNeeds(p);
					}
					p.system.OwnerList.Clear();
					foreach (Planet pl in p.system.PlanetList)
					{
						if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
						{
							continue;
						}
						p.system.OwnerList.Add(pl.Owner);
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					TrustEntry te = new TrustEntry()
					{
						TrustCost = (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
						TurnTimer = 40,
						Type = TrustEntryType.Technology
					};
					us.GetRelations()[Them].TrustEntries.Add(te);
				}
				foreach (Planet p in toRemove)
				{
					us.GetPlanets().Remove(p);
				}
				foreach (Ship ship in TroopShips)
				{
					ship.GetAI().OrderRebaseToNearest();
				}
			}
			foreach (string planetName in ToUs.ColoniesOffered)
			{
				List<Planet> toRemove = new List<Planet>();
				List<Ship> TroopShips = new List<Ship>();
				foreach (Planet p in Them.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					toRemove.Add(p);
					p.Owner = us;
					us.AddPlanet(p);
					p.system.OwnerList.Clear();
					foreach (Planet pl in p.system.PlanetList)
					{
						if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
						{
							continue;
						}
						p.system.OwnerList.Add(pl.Owner);
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					foreach (PlanetGridSquare pgs in p.TilesList)
					{
						if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
						{
							continue;
						}
						pgs.TroopsHere[0].SetPlanet(p);
						TroopShips.Add(pgs.TroopsHere[0].Launch());
					}
					if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != Them)
					{
						TrustEntry te = new TrustEntry()
						{
							TrustCost = (Them.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
							TurnTimer = 40,
							Type = TrustEntryType.Technology
						};
						Them.GetRelations()[us].TrustEntries.Add(te);
					}
					if (us == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
					{
						continue;
					}
					p.colonyType = us.AssessColonyNeeds(p);
				}
				foreach (Planet p in toRemove)
				{
					Them.GetPlanets().Remove(p);
				}
				foreach (Ship ship in TroopShips)
				{
					ship.GetAI().OrderRebaseToNearest();
				}
			}
		}

		public void AcceptThreat(Offer ToUs, Offer FromUs, Empire us, Empire Them)
		{
			if (ToUs.PeaceTreaty)
			{
				this.empire.GetRelations()[Them].AtWar = false;
				this.empire.GetRelations()[Them].PreparingForWar = false;
				this.empire.GetRelations()[Them].ActiveWar.EndStarDate = this.empire.GetUS().StarDate;
				this.empire.GetRelations()[Them].WarHistory.Add(this.empire.GetRelations()[Them].ActiveWar);
				this.empire.GetRelations()[Them].Posture = Posture.Neutral;
				if (this.empire.GetRelations()[Them].Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
				{
					this.empire.GetRelations()[Them].Anger_FromShipsInOurBorders = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
				}
				if (this.empire.GetRelations()[Them].Anger_TerritorialConflict > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
				{
					this.empire.GetRelations()[Them].Anger_TerritorialConflict = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
				}
				this.empire.GetRelations()[Them].Anger_MilitaryConflict = 0f;
				this.empire.GetRelations()[Them].WarnedAboutShips = false;
				this.empire.GetRelations()[Them].WarnedAboutColonizing = false;
				this.empire.GetRelations()[Them].HaveRejected_Demand_Tech = false;
				this.empire.GetRelations()[Them].HaveRejected_OpenBorders = false;
				this.empire.GetRelations()[Them].HaveRejected_TRADE = false;
				this.empire.GetRelations()[Them].HasDefenseFleet = false;
				if (this.empire.GetRelations()[Them].DefenseFleet != -1)
				{
					this.empire.GetFleetsDict()[this.empire.GetRelations()[Them].DefenseFleet].Task.EndTask();
				}
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in this.TaskList)
					{
						if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != Them)
						{
							continue;
						}
						task.EndTask();
					}
				}
				this.empire.GetRelations()[Them].ActiveWar = null;
				Them.GetRelations()[this.empire].AtWar = false;
				Them.GetRelations()[this.empire].PreparingForWar = false;
				Them.GetRelations()[this.empire].ActiveWar.EndStarDate = Them.GetUS().StarDate;
				Them.GetRelations()[this.empire].WarHistory.Add(Them.GetRelations()[this.empire].ActiveWar);
				Them.GetRelations()[this.empire].Posture = Posture.Neutral;
				if (EmpireManager.GetEmpireByName(Them.GetUS().PlayerLoyalty) != Them)
				{
					if (Them.GetRelations()[this.empire].Anger_FromShipsInOurBorders > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
					{
						Them.GetRelations()[this.empire].Anger_FromShipsInOurBorders = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
					}
					if (Them.GetRelations()[this.empire].Anger_TerritorialConflict > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
					{
						Them.GetRelations()[this.empire].Anger_TerritorialConflict = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
					}
					Them.GetRelations()[this.empire].Anger_MilitaryConflict = 0f;
					Them.GetRelations()[this.empire].WarnedAboutShips = false;
					Them.GetRelations()[this.empire].WarnedAboutColonizing = false;
					Them.GetRelations()[this.empire].HaveRejected_Demand_Tech = false;
					Them.GetRelations()[this.empire].HaveRejected_OpenBorders = false;
					Them.GetRelations()[this.empire].HaveRejected_TRADE = false;
					if (Them.GetRelations()[this.empire].DefenseFleet != -1)
					{
						Them.GetFleetsDict()[Them.GetRelations()[this.empire].DefenseFleet].Task.EndTask();
					}
					lock (GlobalStats.TaskLocker)
					{
						foreach (MilitaryTask task in Them.GetGSAI().TaskList)
						{
							if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != this.empire)
							{
								continue;
							}
							task.EndTask();
						}
					}
				}
				Them.GetRelations()[this.empire].ActiveWar = null;
			}
			if (ToUs.NAPact)
			{
				us.GetRelations()[Them].Treaty_NAPact = true;
				FearEntry te = new FearEntry();
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != us)
				{
					string name = us.data.DiplomaticPersonality.Name;
					string str = name;
					if (name != null)
					{
						if (str == "Pacifist")
						{
							te.FearCost = 0f;
						}
						else if (str == "Cunning")
						{
							te.FearCost = 0f;
						}
						else if (str == "Xenophobic")
						{
							te.FearCost = 15f;
						}
						else if (str == "Aggressive")
						{
							te.FearCost = 35f;
						}
						else if (str == "Honorable")
						{
							te.FearCost = 5f;
						}
						else if (str == "Ruthless")
						{
							te.FearCost = 50f;
						}
					}
				}
				us.GetRelations()[Them].FearEntries.Add(te);
			}
			if (FromUs.NAPact)
			{
				Them.GetRelations()[us].Treaty_NAPact = true;
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != Them)
				{
					FearEntry te = new FearEntry();
					string name1 = Them.data.DiplomaticPersonality.Name;
					string str1 = name1;
					if (name1 != null)
					{
						if (str1 == "Pacifist")
						{
							te.FearCost = 0f;
						}
						else if (str1 == "Cunning")
						{
							te.FearCost = 0f;
						}
						else if (str1 == "Xenophobic")
						{
							te.FearCost = 15f;
						}
						else if (str1 == "Aggressive")
						{
							te.FearCost = 35f;
						}
						else if (str1 == "Honorable")
						{
							te.FearCost = 5f;
						}
						else if (str1 == "Ruthless")
						{
							te.FearCost = 50f;
						}
					}
					Them.GetRelations()[us].FearEntries.Add(te);
				}
			}
			if (ToUs.TradeTreaty)
			{
				us.GetRelations()[Them].Treaty_Trade = true;
				us.GetRelations()[Them].Treaty_Trade_TurnsExisted = 0;
				FearEntry te = new FearEntry()
				{
					FearCost = 5f
				};
				us.GetRelations()[Them].FearEntries.Add(te);
			}
			if (FromUs.TradeTreaty)
			{
				Them.GetRelations()[us].Treaty_Trade = true;
				Them.GetRelations()[us].Treaty_Trade_TurnsExisted = 0;
				FearEntry te = new FearEntry()
				{
					FearCost = 0.1f
				};
				Them.GetRelations()[us].FearEntries.Add(te);
			}
			if (ToUs.OpenBorders)
			{
				us.GetRelations()[Them].Treaty_OpenBorders = true;
				FearEntry te = new FearEntry()
				{
					FearCost = 5f
				};
				us.GetRelations()[Them].FearEntries.Add(te);
			}
			if (FromUs.OpenBorders)
			{
				Them.GetRelations()[us].Treaty_OpenBorders = true;
				FearEntry te = new FearEntry()
				{
					FearCost = 5f
				};
				Them.GetRelations()[us].FearEntries.Add(te);
			}
			foreach (string tech in FromUs.TechnologiesOffered)
			{
				Them.UnlockTech(tech);
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) == us)
				{
					continue;
				}
				FearEntry te = new FearEntry()
				{
					FearCost = (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
					TurnTimer = 40
				};
				us.GetRelations()[Them].FearEntries.Add(te);
			}
			foreach (string tech in ToUs.TechnologiesOffered)
			{
				us.UnlockTech(tech);
				if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) == Them)
				{
					continue;
				}
				FearEntry te = new FearEntry()
				{
					FearCost = (Them.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f)
				};
				Them.GetRelations()[us].FearEntries.Add(te);
			}
			foreach (string Art in FromUs.ArtifactsOffered)
			{
				Artifact toGive = ResourceManager.ArtifactsDict[Art];
				foreach (Artifact arti in us.data.OwnedArtifacts)
				{
					if (arti.Name != Art)
					{
						continue;
					}
					toGive = arti;
				}
				us.data.OwnedArtifacts.Remove(toGive);
				Them.data.OwnedArtifacts.Add(toGive);
			}
			foreach (string Art in ToUs.ArtifactsOffered)
			{
				Artifact toGive = ResourceManager.ArtifactsDict[Art];
				foreach (Artifact arti in Them.data.OwnedArtifacts)
				{
					if (arti.Name != Art)
					{
						continue;
					}
					toGive = arti;
				}
				Them.data.OwnedArtifacts.Remove(toGive);
				us.data.OwnedArtifacts.Add(toGive);
			}
			foreach (string planetName in FromUs.ColoniesOffered)
			{
				List<Planet> toRemove = new List<Planet>();
				List<Ship> TroopShips = new List<Ship>();
				foreach (Planet p in us.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					foreach (PlanetGridSquare pgs in p.TilesList)
					{
						if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.empire)
						{
							continue;
						}
						TroopShips.Add(pgs.TroopsHere[0].Launch());
					}
					toRemove.Add(p);
					p.Owner = Them;
					Them.AddPlanet(p);
					p.system.OwnerList.Clear();
					foreach (Planet pl in p.system.PlanetList)
					{
						if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
						{
							continue;
						}
						p.system.OwnerList.Add(pl.Owner);
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					FearEntry te = new FearEntry();
					if (value < 15f)
					{
						value = 15f;
					}
					te.FearCost = (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
					te.TurnTimer = 40;
					us.GetRelations()[Them].FearEntries.Add(te);
				}
				foreach (Planet p in toRemove)
				{
					us.GetPlanets().Remove(p);
				}
				foreach (Ship ship in TroopShips)
				{
					ship.GetAI().OrderRebaseToNearest();
				}
			}
			foreach (string planetName in ToUs.ColoniesOffered)
			{
				List<Planet> toRemove = new List<Planet>();
				List<Ship> TroopShips = new List<Ship>();
				foreach (Planet p in Them.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					toRemove.Add(p);
					p.Owner = us;
					us.AddPlanet(p);
					p.system.OwnerList.Clear();
					foreach (Planet pl in p.system.PlanetList)
					{
						if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
						{
							continue;
						}
						p.system.OwnerList.Add(pl.Owner);
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					foreach (PlanetGridSquare pgs in p.TilesList)
					{
						if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
						{
							continue;
						}
						TroopShips.Add(pgs.TroopsHere[0].Launch());
					}
					if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) == Them)
					{
						continue;
					}
					FearEntry te = new FearEntry()
					{
						FearCost = (Them.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
						TurnTimer = 40
					};
					Them.GetRelations()[us].FearEntries.Add(te);
				}
				foreach (Planet p in toRemove)
				{
					Them.GetPlanets().Remove(p);
				}
				foreach (Ship ship in TroopShips)
				{
					ship.GetAI().OrderRebaseToNearest();
				}
			}
			us.GetRelations()[Them].UpdateRelationship(us, Them);
		}

		private void AddArtifact(Empire Triggerer, Artifact art)
		{
			Triggerer.data.OwnedArtifacts.Add(art);
			if (art.DiplomacyMod > 0f)
			{
				RacialTrait traits = Triggerer.data.Traits;
				traits.DiplomacyMod = traits.DiplomacyMod + (art.DiplomacyMod + art.DiplomacyMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.FertilityMod > 0f)
			{
				EmpireData triggerer = Triggerer.data;
				triggerer.EmpireFertilityBonus = triggerer.EmpireFertilityBonus + art.FertilityMod;
				foreach (Planet planet in Triggerer.GetPlanets())
				{
					Planet fertility = planet;
					fertility.Fertility = fertility.Fertility + (art.FertilityMod + art.FertilityMod * Triggerer.data.Traits.Spiritual);
				}
			}
			if (art.GroundCombatMod > 0f)
			{
				RacialTrait groundCombatModifier = Triggerer.data.Traits;
				groundCombatModifier.GroundCombatModifier = groundCombatModifier.GroundCombatModifier + (art.GroundCombatMod + art.GroundCombatMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ModuleHPMod > 0f)
			{
				RacialTrait modHpModifier = Triggerer.data.Traits;
				modHpModifier.ModHpModifier = modHpModifier.ModHpModifier + (art.ModuleHPMod + art.ModuleHPMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.PlusFlatMoney > 0f)
			{
				EmpireData flatMoneyBonus = Triggerer.data;
				flatMoneyBonus.FlatMoneyBonus = flatMoneyBonus.FlatMoneyBonus + (art.PlusFlatMoney + art.PlusFlatMoney * Triggerer.data.Traits.Spiritual);
			}
			if (art.ProductionMod > 0f)
			{
				RacialTrait productionMod = Triggerer.data.Traits;
				productionMod.ProductionMod = productionMod.ProductionMod + (art.ProductionMod + art.ProductionMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ReproductionMod > 0f)
			{
				RacialTrait reproductionMod = Triggerer.data.Traits;
				reproductionMod.ReproductionMod = reproductionMod.ReproductionMod + (art.ReproductionMod + art.ReproductionMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ResearchMod > 0f)
			{
				RacialTrait researchMod = Triggerer.data.Traits;
				researchMod.ResearchMod = researchMod.ResearchMod + (art.ResearchMod + art.ResearchMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.SensorMod > 0f)
			{
				EmpireData sensorModifier = Triggerer.data;
				sensorModifier.SensorModifier = sensorModifier.SensorModifier + (art.SensorMod + art.SensorMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ShieldPenBonus > 0f)
			{
				EmpireData shieldPenBonusChance = Triggerer.data;
				shieldPenBonusChance.ShieldPenBonusChance = shieldPenBonusChance.ShieldPenBonusChance + (art.ShieldPenBonus + art.ShieldPenBonus * Triggerer.data.Traits.Spiritual);
			}
		}

		public string AnalyzeOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
		{
			if (ToUs.Alliance)
			{
				if (!ToUs.IsBlank() || !FromUs.IsBlank())
				{
					return "OFFER_ALLIANCE_TOO_COMPLICATED";
				}
				if (this.empire.GetRelations()[them].Trust < 90f || this.empire.GetRelations()[them].TotalAnger >= 20f || this.empire.GetRelations()[them].TurnsKnown <= 100)
				{
					return "AI_ALLIANCE_REJECT";
				}
				this.SetAlliance(true, them);
				return "AI_ALLIANCE_ACCEPT";
			}
			if (ToUs.PeaceTreaty)
			{
				GSAI.PeaceAnswer answer = this.AnalyzePeaceOffer(ToUs, FromUs, them, attitude);
				if (!answer.peace)
				{
					return answer.answer;
				}
				this.AcceptOffer(ToUs, FromUs, this.empire, them);
				this.empire.GetRelations()[them].Treaty_Peace = true;
				this.empire.GetRelations()[them].PeaceTurnsRemaining = 100;
				them.GetRelations()[this.empire].Treaty_Peace = true;
				them.GetRelations()[this.empire].PeaceTurnsRemaining = 100;
				return answer.answer;
			}
			Empire us = this.empire;
			float TotalTrustRequiredFromUS = 0f;
			DTrait dt = us.data.DiplomaticPersonality;
			if (FromUs.TradeTreaty)
			{
				TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float)dt.Trade;
			}
			if (FromUs.OpenBorders)
			{
				TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ((float)dt.NAPact + 7.5f);
			}
			if (FromUs.NAPact)
			{
				TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float)dt.NAPact;
				int numWars = 0;
				foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in us.GetRelations())
				{
					if (Relationship.Key.isFaction || !Relationship.Value.AtWar)
					{
						continue;
					}
					numWars++;
				}
				if (numWars > 0 && !us.GetRelations()[them].AtWar)
				{
					TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float)dt.NAPact;
				}
				else if (us.GetRelations()[them].Threat >= 20f)
				{
					TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float)dt.NAPact;
				}
			}
			foreach (string tech in FromUs.TechnologiesOffered)
			{
				TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ResourceManager.TechTree[tech].Cost / 50f;
			}
			float ValueFromUs = 0f;
			float ValueToUs = 0f;
			if (FromUs.OpenBorders)
			{
				ValueFromUs = ValueFromUs + 5f;
			}
			if (ToUs.OpenBorders)
			{
				ValueToUs = ValueToUs + 0.01f;
			}
			if (FromUs.NAPact)
			{
				ValueFromUs = ValueFromUs + 5f;
			}
			if (ToUs.NAPact)
			{
				ValueToUs = ValueToUs + 5f;
			}
			if (FromUs.TradeTreaty)
			{
				ValueFromUs = ValueFromUs + 5f;
			}
			if (ToUs.TradeTreaty)
			{
				ValueToUs = ValueToUs + 5f;
				if ((double)this.empire.EstimateIncomeAtTaxRate(0.5f) < 1)
				{
					ValueToUs = ValueToUs + 20f;
				}
			}
			foreach (string tech in FromUs.TechnologiesOffered)
			{
				ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f + ResourceManager.TechTree[tech].Cost / 50f : ResourceManager.TechTree[tech].Cost / 50f);
			}
			foreach (string artifactsOffered in FromUs.ArtifactsOffered)
			{
				ValueFromUs = ValueFromUs + 15f;
			}
			foreach (string str in ToUs.ArtifactsOffered)
			{
				ValueToUs = ValueToUs + 15f;
			}
			foreach (string tech in ToUs.TechnologiesOffered)
			{
				ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f + ResourceManager.TechTree[tech].Cost / 50f : ResourceManager.TechTree[tech].Cost / 50f);
			}
			if (us.GetPlanets().Count - FromUs.ColoniesOffered.Count + ToUs.ColoniesOffered.Count < 1)
			{
				us.GetRelations()[them].DamageRelationship(us, them, "Insulted", 25f, null);
				return "OfferResponse_Reject_Insulting";
			}
			foreach (string planetName in FromUs.ColoniesOffered)
			{
				foreach (Planet p in us.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					float value = p.Population / 1000f + p.FoodHere / 25f + p.ProductionHere / 25f + p.Fertility + p.MineralRichness + p.MaxPopulation / 1000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 25f;
						if (b.Name != "Capital City")
						{
							continue;
						}
						value = value + 100f;
					}
					float multiplier = 0f;
					foreach (Planet other in p.system.PlanetList)
					{
						if (other.Owner != p.Owner)
						{
							continue;
						}
						multiplier = multiplier + 1.25f;
					}
					value = value * multiplier;
					if (value < 15f)
					{
						value = 15f;
					}
					ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
				}
			}
			foreach (string planetName in ToUs.ColoniesOffered)
			{
				foreach (Planet p in them.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 2000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					int multiplier = 1;
					foreach (Planet other in p.system.PlanetList)
					{
						if (other.Owner != p.Owner)
						{
							continue;
						}
						multiplier++;
					}
					value = value * (float)multiplier;
					ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value * 0.5f + value : value);
				}
			}
			ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
			if (ValueFromUs == 0f && ValueToUs > 0f)
			{
				us.GetRelations()[them].ImproveRelations(ValueToUs, ValueToUs);
				this.AcceptOffer(ToUs, FromUs, us, them);
				return "OfferResponse_Accept_Gift";
			}
			ValueToUs = ValueToUs - ValueToUs * us.GetRelations()[them].TotalAnger / 100f;
			float offerdifferential = ValueToUs / (ValueFromUs + 0.01f);
			string OfferQuality = "";
			if (offerdifferential < 0.6f)
			{
				OfferQuality = "Insulting";
			}
			else if (offerdifferential < 0.9f && offerdifferential >= 0.6f)
			{
				OfferQuality = "Poor";
			}
			else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
			{
				OfferQuality = "Fair";
			}
			else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
			{
				OfferQuality = "Good";
			}
			else if (offerdifferential >= 1.45f)
			{
				OfferQuality = "Great";
			}
			if (ValueToUs == ValueFromUs)
			{
				OfferQuality = "Fair";
			}
			switch (attitude)
			{
				case Offer.Attitude.Pleading:
				{
					if (TotalTrustRequiredFromUS > us.GetRelations()[them].Trust)
					{
						if (OfferQuality != "Great")
						{
							return "OfferResponse_InsufficientTrust";
						}
						us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
						this.AcceptOffer(ToUs, FromUs, us, them);
						return "OfferResponse_AcceptGreatOffer_LowTrust";
					}
					if (offerdifferential < 0.6f)
					{
						OfferQuality = "Insulting";
					}
					else if (offerdifferential < 0.8f && offerdifferential > 0.65f)
					{
						OfferQuality = "Poor";
					}
					else if (offerdifferential >= 0.8f && offerdifferential < 1.1f)
					{
						OfferQuality = "Fair";
					}
					else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
					{
						OfferQuality = "Good";
					}
					else if (offerdifferential >= 1.45f)
					{
						OfferQuality = "Great";
					}
					if (OfferQuality == "Poor")
					{
						return "OfferResponse_Reject_PoorOffer_EnoughTrust";
					}
					if (OfferQuality == "Insulting")
					{
						us.GetRelations()[them].DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
						return "OfferResponse_Reject_Insulting";
					}
					if (OfferQuality == "Fair")
					{
						us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
						this.AcceptOffer(ToUs, FromUs, us, them);
						return "OfferResponse_Accept_Fair_Pleading";
					}
					if (OfferQuality == "Good")
					{
						us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
						this.AcceptOffer(ToUs, FromUs, us, them);
						return "OfferResponse_Accept_Good";
					}
					if (OfferQuality != "Great")
					{
						break;
					}
					us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
					this.AcceptOffer(ToUs, FromUs, us, them);
					return "OfferResponse_Accept_Great";
				}
				case Offer.Attitude.Respectful:
				{
					if (TotalTrustRequiredFromUS + us.GetRelations()[them].TrustUsed <= us.GetRelations()[them].Trust)
					{
						if (OfferQuality == "Poor")
						{
							return "OfferResponse_Reject_PoorOffer_EnoughTrust";
						}
						if (OfferQuality == "Insulting")
						{
							us.GetRelations()[them].DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
							return "OfferResponse_Reject_Insulting";
						}
						if (OfferQuality == "Fair")
						{
							us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
							this.AcceptOffer(ToUs, FromUs, us, them);
							return "OfferResponse_Accept_Fair";
						}
						if (OfferQuality == "Good")
						{
							us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
							this.AcceptOffer(ToUs, FromUs, us, them);
							return "OfferResponse_Accept_Good";
						}
						if (OfferQuality != "Great")
						{
							break;
						}
						us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
						this.AcceptOffer(ToUs, FromUs, us, them);
						return "OfferResponse_Accept_Great";
					}
					else
					{
						if (OfferQuality == "Great")
						{
							us.GetRelations()[them].ImproveRelations(ValueToUs - ValueFromUs, ValueToUs);
							this.AcceptOffer(ToUs, FromUs, us, them);
							return "OfferResponse_AcceptGreatOffer_LowTrust";
						}
						if (OfferQuality == "Poor")
						{
							return "OfferResponse_Reject_PoorOffer_LowTrust";
						}
						if (OfferQuality == "Fair" || OfferQuality == "Good")
						{
							return "OfferResponse_InsufficientTrust";
						}
						if (OfferQuality != "Insulting")
						{
							break;
						}
						us.GetRelations()[them].DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
						return "OfferResponse_Reject_Insulting";
					}
				}
				case Offer.Attitude.Threaten:
				{
					if (dt.Name == "Ruthless")
					{
						return "OfferResponse_InsufficientFear";
					}
					us.GetRelations()[them].DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
					if (OfferQuality == "Great")
					{
						this.AcceptThreat(ToUs, FromUs, us, them);
						return "OfferResponse_AcceptGreatOffer_LowTrust";
					}
					if (offerdifferential < 0.95f)
					{
						OfferQuality = "Poor";
					}
					else if (offerdifferential >= 0.95f)
					{
						OfferQuality = "Fair";
					}
					if (us.GetRelations()[them].Threat <= ValueFromUs || us.GetRelations()[them].FearUsed + ValueFromUs >= us.GetRelations()[them].Threat)
					{
						return "OfferResponse_InsufficientFear";
					}
					if (OfferQuality == "Poor")
					{
						this.AcceptThreat(ToUs, FromUs, us, them);
						return "OfferResponse_Accept_Bad_Threatening";
					}
					if (OfferQuality != "Fair")
					{
						break;
					}
					this.AcceptThreat(ToUs, FromUs, us, them);
					return "OfferResponse_Accept_Fair_Threatening";
				}
			}
			return "";
		}

		public GSAI.PeaceAnswer AnalyzePeaceOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
		{
			WarState state;
			Empire us = this.empire;
			DTrait dt = us.data.DiplomaticPersonality;
			float ValueToUs = 0f;
			float ValueFromUs = 0f;
			foreach (string tech in FromUs.TechnologiesOffered)
			{
				ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f);
			}
			foreach (string artifactsOffered in FromUs.ArtifactsOffered)
			{
				ValueFromUs = ValueFromUs + 15f;
			}
			foreach (string str in ToUs.ArtifactsOffered)
			{
				ValueToUs = ValueToUs + 15f;
			}
			foreach (string tech in ToUs.TechnologiesOffered)
			{
				ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f);
			}
			foreach (string planetName in FromUs.ColoniesOffered)
			{
				foreach (Planet p in us.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
					}
					ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
				}
			}
			List<Planet> PlanetsToUs = new List<Planet>();
			foreach (string planetName in ToUs.ColoniesOffered)
			{
				foreach (Planet p in them.GetPlanets())
				{
					if (p.Name != planetName)
					{
						continue;
					}
					PlanetsToUs.Add(p);
					float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
					foreach (Building b in p.BuildingList)
					{
						value = value + b.Cost / 50f;
						if (b.NameTranslationIndex != 409)
						{
							continue;
						}
						value = value + 1000000f;
					}
					ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value * 0.5f + value : value);
				}
			}
			string name = dt.Name;
			string str1 = name;
			if (name != null)
			{
				if (str1 == "Pacifist")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
					}
				}
				else if (str1 == "Honorable")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 15f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 8f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 8f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 15f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 15f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 8f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 8f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 15f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
					}
				}
				else if (str1 == "Cunning")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
					}
				}
				else if (str1 == "Xenophobic")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 15f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 8f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 8f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 15f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 15f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 8f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 8f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 15f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 5f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 10f;
									break;
								}
							}
							break;
						}
					}
				}
				else if (str1 == "Aggressive")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 75f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 200f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 75f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 200f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 10f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 75f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 200f;
									break;
								}
							}
							break;
						}
					}
				}
				else if (str1 == "Ruthless")
				{
					switch (us.GetRelations()[them].ActiveWar.WarType)
					{
						case WarType.BorderConflict:
						{
							switch (us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs))
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 1f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 120f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 300f;
									break;
								}
							}
							break;
						}
						case WarType.ImperialistWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 1f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 120f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 300f;
									break;
								}
							}
							break;
						}
						case WarType.DefensiveWar:
						{
							switch (us.GetRelations()[them].ActiveWar.GetWarScoreState())
							{
								case WarState.LosingBadly:
								{
									ValueToUs = ValueToUs + 5f;
									break;
								}
								case WarState.LosingSlightly:
								{
									ValueToUs = ValueToUs + 1f;
									break;
								}
								case WarState.WinningSlightly:
								{
									ValueFromUs = ValueFromUs + 120f;
									break;
								}
								case WarState.Dominating:
								{
									ValueFromUs = ValueFromUs + 300f;
									break;
								}
							}
							break;
						}
					}
				}
			}
			ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
			float offerdifferential = ValueToUs / (ValueFromUs + 0.0001f);
			string OfferQuality = "";
			if (offerdifferential < 0.6f)
			{
				OfferQuality = "Insulting";
			}
			else if (offerdifferential < 0.9f && offerdifferential > 0.65f)
			{
				OfferQuality = "Poor";
			}
			else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
			{
				OfferQuality = "Fair";
			}
			else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
			{
				OfferQuality = "Good";
			}
			else if (offerdifferential >= 1.45f)
			{
				OfferQuality = "Great";
			}
			if (ValueToUs == ValueFromUs && ValueToUs > 0f)
			{
				OfferQuality = "Fair";
			}
			GSAI.PeaceAnswer response = new GSAI.PeaceAnswer()
			{
				peace = false,
				answer = "REJECT_OFFER_PEACE_POOROFFER"
			};
			switch (us.GetRelations()[them].ActiveWar.WarType)
			{
				case WarType.BorderConflict:
				{
					state = us.GetRelations()[them].ActiveWar.GetBorderConflictState(PlanetsToUs);
					if (state == WarState.WinningSlightly)
					{
						if (OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else if ((OfferQuality == "Fair" || OfferQuality == "Good") && us.GetRelations()[them].ActiveWar.StartingNumContestedSystems > 0)
						{
							response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
							return response;
						}
						else if (OfferQuality == "Fair" || OfferQuality == "Good")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.Dominating)
					{
						if (OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.ColdWar)
					{
						if (OfferQuality != "Great")
						{
							response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
							return response;
						}
						else
						{
							response.answer = "ACCEPT_PEACE_COLDWAR";
							response.peace = true;
							return response;
						}
					}
					else if (state != WarState.EvenlyMatched)
					{
						if (state != WarState.LosingSlightly)
						{
							if (state != WarState.LosingBadly)
							{
								return response;
							}
							if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
							{
								response.answer = "ACCEPT_OFFER_PEACE";
								response.peace = true;
								return response;
							}
							else if (OfferQuality != "Poor")
							{
								response.answer = "REJECT_OFFER_PEACE_POOROFFER";
								return response;
							}
							else
							{
								response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
								response.peace = true;
								return response;
							}
						}
						else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (OfferQuality == "Great")
					{
						response.answer = "ACCEPT_OFFER_PEACE";
						response.peace = true;
						return response;
					}
					else if ((OfferQuality == "Fair" || OfferQuality == "Good") && us.GetRelations()[them].ActiveWar.StartingNumContestedSystems > 0)
					{
						response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
						return response;
					}
					else if (OfferQuality == "Fair" || OfferQuality == "Good")
					{
						response.answer = "ACCEPT_OFFER_PEACE";
						response.peace = true;
						return response;
					}
					else
					{
						response.answer = "REJECT_OFFER_PEACE_POOROFFER";
						return response;
					}
				}
				case WarType.ImperialistWar:
				{
					state = us.GetRelations()[them].ActiveWar.GetWarScoreState();
					if (state == WarState.WinningSlightly)
					{
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.Dominating)
					{
						if (OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.EvenlyMatched)
					{
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.ColdWar)
					{
						string name1 = this.empire.data.DiplomaticPersonality.Name;
						str1 = name1;
						if (name1 != null && str1 == "Pacifist")
						{
							if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
							{
								response.answer = "ACCEPT_OFFER_PEACE";
								response.peace = true;
								return response;
							}
							else
							{
								response.answer = "REJECT_OFFER_PEACE_POOROFFER";
								return response;
							}
						}
						else if (OfferQuality != "Great")
						{
							response.answer = "REJECT_PEACE_RUTHLESS";
							return response;
						}
						else
						{
							response.answer = "ACCEPT_PEACE_COLDWAR";
							response.peace = true;
							return response;
						}
					}
					else if (state != WarState.LosingSlightly)
					{
						if (state != WarState.LosingBadly)
						{
							return response;
						}
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else if (OfferQuality != "Poor")
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
						else
						{
							response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
							response.peace = true;
							return response;
						}
					}
					else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
					{
						response.answer = "ACCEPT_OFFER_PEACE";
						response.peace = true;
						return response;
					}
					else
					{
						response.answer = "REJECT_OFFER_PEACE_POOROFFER";
						return response;
					}
				}
				case WarType.GenocidalWar:
				{
					return response;
				}
				case WarType.DefensiveWar:
				{
					state = us.GetRelations()[them].ActiveWar.GetWarScoreState();
					if (state == WarState.WinningSlightly)
					{
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.Dominating)
					{
						if (OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.EvenlyMatched)
					{
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
					}
					else if (state == WarState.ColdWar)
					{
						string name2 = this.empire.data.DiplomaticPersonality.Name;
						str1 = name2;
						if (name2 != null && str1 == "Pacifist")
						{
							if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
							{
								response.answer = "ACCEPT_OFFER_PEACE";
								response.peace = true;
								return response;
							}
							else
							{
								response.answer = "REJECT_OFFER_PEACE_POOROFFER";
								return response;
							}
						}
						else if (OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_PEACE_COLDWAR";
							response.peace = true;
							return response;
						}
						else
						{
							response.answer = "REJECT_PEACE_RUTHLESS";
							return response;
						}
					}
					else if (state != WarState.LosingSlightly)
					{
						if (state != WarState.LosingBadly)
						{
							return response;
						}
						if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
						{
							response.answer = "ACCEPT_OFFER_PEACE";
							response.peace = true;
							return response;
						}
						else if (OfferQuality != "Poor")
						{
							response.answer = "REJECT_OFFER_PEACE_POOROFFER";
							return response;
						}
						else
						{
							response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
							response.peace = true;
							return response;
						}
					}
					else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
					{
						response.answer = "ACCEPT_OFFER_PEACE";
						response.peace = true;
						return response;
					}
					else
					{
						response.answer = "REJECT_OFFER_PEACE_POOROFFER";
						return response;
					}
				}
				default:
				{
					return response;
				}
			}
		}

		private void AssessAngerAggressive(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship, Posture posture, float usedTrust)
		{
			if (posture != Posture.Friendly)
			{
				this.AssessDiplomaticAnger(Relationship);
			}
			else if (Relationship.Value.Treaty_OpenBorders || !Relationship.Value.Treaty_Trade && !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_OpenBorders)
			{
				if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f && Relationship.Value.Trust < Relationship.Value.TotalAnger)
				{
					Relationship.Value.Posture = Posture.Neutral;
					return;
				}
			}
			else if (Relationship.Value.Trust >= 50f)
			{
				if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
				{
					Offer NAPactOffer = new Offer()
					{
						OpenBorders = true,
						AcceptDL = "Open Borders Accepted",
						RejectDL = "Open Borders Friends Rejected"
					};
					Ship_Game.Gameplay.Relationship value = Relationship.Value;
					NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders, (bool x) => value.HaveRejected_OpenBorders = x);
					Offer OurOffer = new Offer()
					{
						OpenBorders = true
					};
					if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
						return;
					}
					this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Open Borders Friends", OurOffer, NAPactOffer));
					return;
				}
			}
			else if (Relationship.Value.Trust >= 20f && Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >= 0.75f * (float)this.empire.data.DiplomaticPersonality.Territorialism)
			{
				if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
				{
					Offer NAPactOffer = new Offer()
					{
						OpenBorders = true,
						AcceptDL = "Open Borders Accepted",
						RejectDL = "Open Borders Rejected"
					};
					Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
					NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders, (bool x) => relationship.HaveRejected_OpenBorders = x);
					Offer OurOffer = new Offer()
					{
						OpenBorders = true
					};
					if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
						return;
					}
					this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Open Borders", OurOffer, NAPactOffer));
					return;
				}
			}
			else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known && Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				Ship_Game.Gameplay.Relationship r = Relationship.Value;
				if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
				{
					if (!r.WarnedAboutColonizing)
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, Relationship.Key, "Warning Ships"));
					}
					else
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
					}
					r.WarnedAboutShips = true;
					return;
				}
			}
		}

		private void AssessAngerPacifist(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship, Posture posture, float usedTrust)
		{
			if (posture != Posture.Friendly)
			{
				this.AssessDiplomaticAnger(Relationship);
			}
			else if (!Relationship.Value.Treaty_OpenBorders && (Relationship.Value.Treaty_Trade || Relationship.Value.Treaty_NAPact) && !Relationship.Value.HaveRejected_OpenBorders)
			{
				if (Relationship.Value.Trust >= 50f)
				{
					if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
					{
						Offer NAPactOffer = new Offer()
						{
							OpenBorders = true,
							AcceptDL = "Open Borders Accepted",
							RejectDL = "Open Borders Friends Rejected"
						};
						Ship_Game.Gameplay.Relationship value = Relationship.Value;
						NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders, (bool x) => value.HaveRejected_OpenBorders = x);
						Offer OurOffer = new Offer()
						{
							OpenBorders = true
						};
						if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
						{
							Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
							return;
						}
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Open Borders Friends", OurOffer, NAPactOffer));
						return;
					}
				}
				else if (Relationship.Value.Trust >= 20f && Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >= 0.75f * (float)this.empire.data.DiplomaticPersonality.Territorialism && Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
				{
					Offer NAPactOffer = new Offer()
					{
						OpenBorders = true,
						AcceptDL = "Open Borders Accepted",
						RejectDL = "Open Borders Rejected"
					};
					Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
					NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders, (bool x) => relationship.HaveRejected_OpenBorders = x);
					Offer OurOffer = new Offer()
					{
						OpenBorders = true
					};
					if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
						return;
					}
					this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Open Borders", OurOffer, NAPactOffer));
					return;
				}
			}
			else if (Relationship.Value.turnsSinceLastContact >= 10)
			{
				if (Relationship.Value.Known && Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
				{
					Ship_Game.Gameplay.Relationship r = Relationship.Value;
					if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
					{
						if (!r.WarnedAboutColonizing)
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, Relationship.Key, "Warning Ships"));
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
						}
						r.WarnedAboutShips = true;
						return;
					}
				}
			}
			else if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f && Relationship.Value.Trust < Relationship.Value.TotalAnger)
			{
				Relationship.Value.Posture = Posture.Neutral;
				return;
			}
		}

		private void AssessDiplomaticAnger(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship)
		{
			if (Relationship.Value.Known && Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				Ship_Game.Gameplay.Relationship r = Relationship.Value;
				Empire them = Relationship.Key;
				if ((double)r.Anger_MilitaryConflict >= 5 && !r.AtWar)
				{
					this.DeclareWarOn(them, WarType.DefensiveWar);
				}
				if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && !r.Treaty_Peace && !r.Treaty_OpenBorders)
				{
					if (!r.WarnedAboutColonizing)
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Warning Ships"));
					}
					else
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Warning Colonized then Ships", r.GetContestedSystem()));
					}
					r.turnsSinceLastContact = 0;
					r.WarnedAboutShips = true;
					return;
				}
				if (r.Threat < 25f && r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >= (float)this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar && !r.Treaty_OpenBorders && !r.Treaty_Peace)
				{
					r.PreparingForWar = true;
					r.PreparingForWarType = WarType.BorderConflict;
					return;
				}
				if (r.PreparingForWar && r.PreparingForWarType == WarType.BorderConflict)
				{
					r.PreparingForWar = false;
					return;
				}
			}
			else if (Relationship.Value.Known)
			{
				Ship_Game.Gameplay.Relationship r = Relationship.Value;
				Empire them = Relationship.Key;
				if ((double)r.Anger_MilitaryConflict >= 5 && !r.AtWar && !r.Treaty_Peace)
				{
					this.DeclareWarOn(them, WarType.DefensiveWar);
				}
				if (r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >= (float)this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar && !r.Treaty_OpenBorders && !r.Treaty_Peace)
				{
					r.PreparingForWar = true;
					r.PreparingForWarType = WarType.BorderConflict;
				}
				if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2) && !r.AtWar && !r.WarnedAboutShips)
				{
					r.turnsSinceLastContact = 0;
					r.WarnedAboutShips = true;
				}
			}
		}

		public SolarSystem AssignExplorationTargetORIG(Ship queryingShip)
		{
			List<SolarSystem> Potentials = new List<SolarSystem>();
			foreach (SolarSystem s in UniverseScreen.SolarSystemList)
			{
				if (s.ExploredDict[this.empire])
				{
					continue;
				}
				Potentials.Add(s);
			}
			foreach (SolarSystem s in this.MarkedForExploration)
			{
				Potentials.Remove(s);
			}
			IOrderedEnumerable<SolarSystem> sortedList = 
				from system in Potentials
				orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
				select system;
			if (sortedList.Count<SolarSystem>() <= 0)
			{
				queryingShip.GetAI().OrderQueue.Clear();
				return null;
			}
			this.MarkedForExploration.Add(sortedList.First<SolarSystem>());
			return sortedList.First<SolarSystem>();
		}
        //added by gremlin ExplorationTarget
        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            List<SolarSystem> Potentials = new List<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.empire])
                {
                    continue;
                }
                Potentials.Add(s);
            }
            foreach (SolarSystem s in this.MarkedForExploration)
            {
                Potentials.Remove(s);
            }
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in Potentials
                orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
                select system;
            if (sortedList.Count<SolarSystem>() <= 0)
            {
                queryingShip.GetAI().OrderQueue.Clear();
                return null;
            }
            //SolarSystem nearesttoScout = sortedList.OrderBy(furthest => Vector2.Distance(queryingShip.Center, furthest.Position)).FirstOrDefault();
            SolarSystem nearesttoHome = sortedList.OrderBy(furthest => Vector2.Distance(this.empire.GetWeightedCenter(), furthest.Position)).FirstOrDefault(); ;
            //SolarSystem potentialTarget = null;
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.CombatInSystem) continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(this.empire.GetWeightedCenter(), nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)
                {
                    continue;
                }
                nearesttoHome = nearest;
                break;

            }
            this.MarkedForExploration.Add(nearesttoHome);
            return nearesttoHome;
        }

		public void AssignShipToForce(Ship toAdd)
		{
			int numWars = 0;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.AtWar || Relationship.Key.isFaction)
				{
					continue;
				}
				numWars++;
			}
			float baseDefensePct = 0.1f;
			baseDefensePct = baseDefensePct + 0.15f * (float)numWars;
			if (baseDefensePct > 0.35f)
			{
				baseDefensePct = 0.35f;
			}
			float EntireStrength = 0f;
			foreach (Ship ship in this.empire.GetShips())
			{
				EntireStrength = EntireStrength + ship.GetStrength();
			}
			//added by gremlin dont add zero strength ships to defensive force pool
            if (this.DefensiveCoordinator.GetForcePoolStrength() / EntireStrength <= baseDefensePct && (toAdd.BombBays.Count <= 0 || toAdd.WarpThrust <= 0f)) //&&toAdd.GetStrength()>0 && toAdd.BaseCanWarp)  //
			{
				this.DefensiveCoordinator.DefensiveForcePool.Add(toAdd);
				toAdd.GetAI().SystemToDefend = null;
				toAdd.GetAI().SystemToDefendGuid = Guid.Empty;
				toAdd.GetAI().HasPriorityOrder = false;
				toAdd.GetAI().State = AIState.SystemDefender;
				return;
			}
			IOrderedEnumerable<AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
				orderby Vector2.Distance(toAdd.Position, ao.Position)
				select ao;
			if (sorted.Count<AO>() <= 0)
			{
				this.empire.GetForcePool().Add(toAdd);
				return;
			}
			sorted.First<AO>().AddShip(toAdd);
		}

		public void CallAllyToWar(Empire Ally, Empire Enemy)
		{
			Offer offer = new Offer()
			{
				AcceptDL = "HelpUS_War_Yes",
				RejectDL = "HelpUS_War_No"
			};
			string dialogue = "HelpUS_War";
			Offer OurOffer = new Offer()
			{
				ValueToModify = new Ref<bool>(() => Ally.GetRelations()[Enemy].AtWar, (bool x) => {
					if (x)
					{
						Ally.GetGSAI().DeclareWarOnViaCall(Enemy, WarType.ImperialistWar);
						return;
					}
					float Amount = 30f;
					if (this.empire.data.DiplomaticPersonality != null && this.empire.data.DiplomaticPersonality.Name == "Honorable")
					{
						Amount = 60f;
						offer.RejectDL = "HelpUS_War_No_BreakAlliance";
						this.empire.GetRelations()[Ally].Treaty_Alliance = false;
						Ally.GetRelations()[this.empire].Treaty_Alliance = false;
						this.empire.GetRelations()[Ally].Treaty_OpenBorders = false;
						this.empire.GetRelations()[Ally].Treaty_NAPact = false;
					}
					Relationship item = this.empire.GetRelations()[Ally];
					item.Trust = item.Trust - Amount;
					Relationship angerDiplomaticConflict = this.empire.GetRelations()[Ally];
					angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + Amount;
				})
			};
			if (Ally == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), dialogue, OurOffer, offer, Enemy));
			}
		}

		public void CheckClaim(KeyValuePair<Empire, Relationship> Them, Planet claimedPlanet)
		{
			if (this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				return;
			}
			if (this.empire.isFaction)
			{
				return;
			}
			if (!Them.Value.Known)
			{
				return;
			}
			if (Them.Value.WarnedSystemsList.Contains(claimedPlanet.system.guid) && claimedPlanet.Owner == Them.Key && !Them.Value.AtWar)
			{
				bool TheyAreThereAlready = false;
				foreach (Planet p in claimedPlanet.system.PlanetList)
				{
					if (p.Owner == null || p.Owner != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						continue;
					}
					TheyAreThereAlready = true;
				}
				if (TheyAreThereAlready && Them.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
				{
					Relationship item = this.empire.GetRelations()[Them.Key];
					item.Anger_TerritorialConflict = item.Anger_TerritorialConflict + (5f + (float)Math.Pow(5, (double)this.empire.GetRelations()[Them.Key].NumberStolenClaims));
					this.empire.GetRelations()[Them.Key].UpdateRelationship(this.empire, Them.Key);
					Relationship numberStolenClaims = this.empire.GetRelations()[Them.Key];
					numberStolenClaims.NumberStolenClaims = numberStolenClaims.NumberStolenClaims + 1;
					if (this.empire.GetRelations()[Them.Key].NumberStolenClaims == 1 && !this.empire.GetRelations()[Them.Key].StolenSystems.Contains(claimedPlanet.guid))
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Stole Claim", claimedPlanet.system));
					}
					else if (this.empire.GetRelations()[Them.Key].NumberStolenClaims == 2 && !this.empire.GetRelations()[Them.Key].HaveWarnedTwice && !this.empire.GetRelations()[Them.Key].StolenSystems.Contains(claimedPlanet.system.guid))
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Stole Claim 2", claimedPlanet.system));
						this.empire.GetRelations()[Them.Key].HaveWarnedTwice = true;
					}
					else if (this.empire.GetRelations()[Them.Key].NumberStolenClaims >= 3 && !this.empire.GetRelations()[Them.Key].HaveWarnedThrice && !this.empire.GetRelations()[Them.Key].StolenSystems.Contains(claimedPlanet.system.guid))
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Stole Claim 3", claimedPlanet.system));
						this.empire.GetRelations()[Them.Key].HaveWarnedThrice = true;
					}
					this.empire.GetRelations()[Them.Key].StolenSystems.Add(claimedPlanet.system.guid);
				}
			}
		}

		public void DeclareWarFromEvent(Empire them, WarType wt)
		{
			this.empire.GetRelations()[them].AtWar = true;
			this.empire.GetRelations()[them].Posture = Posture.Hostile;
			this.empire.GetRelations()[them].ActiveWar = new War(this.empire, them, this.empire.GetUS().StarDate)
			{
				WarType = wt
			};
			if (this.empire.GetRelations()[them].Trust > 0f)
			{
				this.empire.GetRelations()[them].Trust = 0f;
			}
			this.empire.GetRelations()[them].Treaty_OpenBorders = false;
			this.empire.GetRelations()[them].Treaty_NAPact = false;
			this.empire.GetRelations()[them].Treaty_Trade = false;
			this.empire.GetRelations()[them].Treaty_Alliance = false;
			this.empire.GetRelations()[them].Treaty_Peace = false;
			them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
		}

        public void DeclareWarOn(Empire them, WarType wt)
        {
            this.empire.GetRelations()[them].PreparingForWar = false;
            if (this.empire.isFaction || this.empire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;
            this.empire.GetRelations()[them].FedQuest = (FederationQuest)null;
            if (this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) && this.empire.GetRelations()[them].Treaty_NAPact)
            {
                this.empire.GetRelations()[them].Treaty_NAPact = false;
                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.GetRelations())
                {
                    if (keyValuePair.Key != them)
                    {
                        keyValuePair.Key.GetRelations()[this.empire].Trust -= 50f;
                        keyValuePair.Key.GetRelations()[this.empire].Anger_DiplomaticConflict += 20f;
                        keyValuePair.Key.GetRelations()[this.empire].UpdateRelationship(keyValuePair.Key, this.empire);
                    }
                }
                them.GetRelations()[this.empire].Trust -= 50f;
                them.GetRelations()[this.empire].Anger_DiplomaticConflict += 50f;
                them.GetRelations()[this.empire].UpdateRelationship(them, this.empire);
            }
            if (them == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) && !this.empire.GetRelations()[them].AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                        if (this.empire.GetRelations()[them].contestedSystemGuid != Guid.Empty)
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War BC TarSys", this.empire.GetRelations()[them].GetContestedSystem()));
                            break;
                        }
                        else
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War BC"));
                            break;
                        }
                    case WarType.ImperialistWar:
                        if (this.empire.GetRelations()[them].Treaty_NAPact)
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War Imperialism Break NA"));
                            using (Dictionary<Empire, Relationship>.Enumerator enumerator = this.empire.GetRelations().GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    KeyValuePair<Empire, Relationship> current = enumerator.Current;
                                    if (current.Key != them)
                                    {
                                        current.Value.Trust -= 50f;
                                        current.Value.Anger_DiplomaticConflict += 20f;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War Imperialism"));
                            break;
                        }
                    case WarType.DefensiveWar:
                        if (!this.empire.GetRelations()[them].Treaty_NAPact)
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War Defense"));
                            this.empire.GetRelations()[them].Anger_DiplomaticConflict += 25f;
                            this.empire.GetRelations()[them].Trust -= 25f;
                            break;
                        }
                        else if (this.empire.GetRelations()[them].Treaty_NAPact)
                        {
                            this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, them, "Declare War Defense BrokenNA"));
                            this.empire.GetRelations()[them].Treaty_NAPact = false;
                            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.GetRelations())
                            {
                                if (keyValuePair.Key != them)
                                {
                                    keyValuePair.Value.Trust -= 50f;
                                    keyValuePair.Value.Anger_DiplomaticConflict += 20f;
                                }
                            }
                            this.empire.GetRelations()[them].Trust -= 50f;
                            this.empire.GetRelations()[them].Anger_DiplomaticConflict += 50f;
                            break;
                        }
                        else
                            break;
                }
            }
            if (them == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) || this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                Ship.universeScreen.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            else if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[them].Known && EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Known)
                Ship.universeScreen.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            this.empire.GetRelations()[them].AtWar = true;
            this.empire.GetRelations()[them].Posture = Posture.Hostile;
            this.empire.GetRelations()[them].ActiveWar = new War(this.empire, them, this.empire.GetUS().StarDate);
            this.empire.GetRelations()[them].ActiveWar.WarType = wt;
            if ((double)this.empire.GetRelations()[them].Trust > 0.0)
                this.empire.GetRelations()[them].Trust = 0.0f;
            this.empire.GetRelations()[them].Treaty_OpenBorders = false;
            this.empire.GetRelations()[them].Treaty_NAPact = false;
            this.empire.GetRelations()[them].Treaty_Trade = false;
            this.empire.GetRelations()[them].Treaty_Alliance = false;
            this.empire.GetRelations()[them].Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

		public void DeclareWarOnViaCall(Empire them, WarType wt)
		{
			this.empire.GetRelations()[them].PreparingForWar = false;
			if (this.empire.isFaction || this.empire.data.Defeated || them.data.Defeated || them.isFaction)
			{
				return;
			}
			this.empire.GetRelations()[them].FedQuest = null;
			if (this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) && this.empire.GetRelations()[them].Treaty_NAPact)
			{
				this.empire.GetRelations()[them].Treaty_NAPact = false;
				Relationship item = them.GetRelations()[this.empire];
				item.Trust = item.Trust - 50f;
				Relationship angerDiplomaticConflict = them.GetRelations()[this.empire];
				angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
				them.GetRelations()[this.empire].UpdateRelationship(them, this.empire);
			}
			if (them == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) && !this.empire.GetRelations()[them].AtWar)
			{
				switch (wt)
				{
					case WarType.BorderConflict:
					{
						if (this.empire.GetRelations()[them].contestedSystemGuid == Guid.Empty)
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War BC"));
							break;
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War BC Tarsys", this.empire.GetRelations()[them].GetContestedSystem()));
							break;
						}
					}
					case WarType.ImperialistWar:
					{
						if (!this.empire.GetRelations()[them].Treaty_NAPact)
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War Imperialism"));
							break;
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War Imperialism Break NA"));
							break;
						}
					}
					case WarType.DefensiveWar:
					{
						if (this.empire.GetRelations()[them].Treaty_NAPact)
						{
							if (!this.empire.GetRelations()[them].Treaty_NAPact)
							{
								break;
							}
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War Defense BrokenNA"));
							this.empire.GetRelations()[them].Treaty_NAPact = false;
							Relationship trust = this.empire.GetRelations()[them];
							trust.Trust = trust.Trust - 50f;
							Relationship relationship = this.empire.GetRelations()[them];
							relationship.Anger_DiplomaticConflict = relationship.Anger_DiplomaticConflict + 50f;
							break;
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, them, "Declare War Defense"));
							Relationship item1 = this.empire.GetRelations()[them];
							item1.Anger_DiplomaticConflict = item1.Anger_DiplomaticConflict + 25f;
							Relationship trust1 = this.empire.GetRelations()[them];
							trust1.Trust = trust1.Trust - 25f;
							break;
						}
					}
				}
			}
			if (them == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) || this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				Ship.universeScreen.NotificationManager.AddWarDeclaredNotification(this.empire, them);
			}
			else if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[them].Known && EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Known)
			{
				Ship.universeScreen.NotificationManager.AddWarDeclaredNotification(this.empire, them);
			}
			this.empire.GetRelations()[them].AtWar = true;
			this.empire.GetRelations()[them].Posture = Posture.Hostile;
			this.empire.GetRelations()[them].ActiveWar = new War(this.empire, them, this.empire.GetUS().StarDate)
			{
				WarType = wt
			};
			if (this.empire.GetRelations()[them].Trust > 0f)
			{
				this.empire.GetRelations()[them].Trust = 0f;
			}
			this.empire.GetRelations()[them].Treaty_OpenBorders = false;
			this.empire.GetRelations()[them].Treaty_NAPact = false;
			this.empire.GetRelations()[them].Treaty_Trade = false;
			this.empire.GetRelations()[them].Treaty_Alliance = false;
			this.empire.GetRelations()[them].Treaty_Peace = false;
			them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
		}

		private void DoAggressiveRelations()
		{
			int numberofWars = 0;
			List<Empire> PotentialTargets = new List<Empire>();
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (Relationship.Key.data.Defeated || !Relationship.Value.AtWar && !Relationship.Value.PreparingForWar)
				{
					continue;
				}
				numberofWars++;
			}
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Value.AtWar || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar && (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" || Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
				{
					Offer NAPactOffer = new Offer()
					{
						TradeTreaty = true,
						AcceptDL = "Trade Accepted",
						RejectDL = "Trade Rejected"
					};
					Ship_Game.Gameplay.Relationship value = Relationship.Value;
					NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
					Offer OurOffer = new Offer()
					{
						TradeTreaty = true
					};
					Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
				}
				float usedTrust = 0f;
				foreach (TrustEntry te in Relationship.Value.TrustEntries)
				{
					usedTrust = usedTrust + te.TrustCost;
				}
				Relationship.Value.Posture = Posture.Neutral;
				if (Relationship.Value.Threat <= -25f)
				{
					if (!Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
					{
						Relationship.Value.HaveInsulted_Military = true;
						if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Insult Military"));
						}
					}
					Relationship.Value.Posture = Posture.Hostile;
				}
				else if (Relationship.Value.Threat > 25f && Relationship.Value.TurnsKnown > this.FirstDemand)
				{
					if (!Relationship.Value.HaveComplimented_Military && Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						Relationship.Value.HaveComplimented_Military = true;
						if (!Relationship.Value.HaveInsulted_Military || Relationship.Value.TurnsKnown <= this.SecondDemand)
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Compliment Military"));
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Compliment Military Better"));
						}
					}
					Relationship.Value.Posture = Posture.Friendly;
				}
				switch (Relationship.Value.Posture)
				{
					case Posture.Friendly:
					{
						if (Relationship.Value.TurnsKnown > this.SecondDemand && Relationship.Value.Trust - usedTrust > (float)this.empire.data.DiplomaticPersonality.Trade && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade)
						{
							Offer NAPactOffer = new Offer()
							{
								TradeTreaty = true,
								AcceptDL = "Trade Accepted",
								RejectDL = "Trade Rejected"
							};
							Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
							NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_TRADE, (bool x) => relationship.HaveRejected_TRADE = x);
							Offer OurOffer = new Offer()
							{
								TradeTreaty = true
							};
							if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
							{
								Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
							}
							else
							{
								this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Trade", OurOffer, NAPactOffer));
							}
						}
						this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
						if (Relationship.Value.TurnsAbove95 <= 100 || Relationship.Value.turnsSinceLastContact <= 10 || Relationship.Value.Treaty_Alliance || !Relationship.Value.Treaty_Trade || !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_Alliance || Relationship.Value.TotalAnger >= 20f)
						{
							continue;
						}
						Offer OfferAlliance = new Offer()
						{
							Alliance = true,
							AcceptDL = "ALLIANCE_ACCEPTED",
							RejectDL = "ALLIANCE_REJECTED"
						};
						Ship_Game.Gameplay.Relationship value1 = Relationship.Value;
						OfferAlliance.ValueToModify = new Ref<bool>(() => value1.HaveRejected_Alliance, (bool x) => {
							value1.HaveRejected_Alliance = x;
							this.SetAlliance(!value1.HaveRejected_Alliance);
						});
						Offer OurOffer0 = new Offer();
						if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
						{
							Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer0, OfferAlliance, this.empire, Offer.Attitude.Respectful);
							continue;
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "OFFER_ALLIANCE", OurOffer0, OfferAlliance));
							continue;
						}
					}
					case Posture.Neutral:
					{
						this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
						continue;
					}
					case Posture.Hostile:
					{
						if (Relationship.Value.Threat < -15f && Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Alliance)
						{
							if (Relationship.Value.TotalAnger < 75f)
							{
								int i = 0;
								while (i < 5)
								{
									if (i >= this.DesiredPlanets.Count)
									{
										break;
									}
									if (this.DesiredPlanets[i].Owner != Relationship.Key)
									{
										i++;
									}
									else
									{
										PotentialTargets.Add(Relationship.Key);
										break;
									}
								}
							}
							else
							{
								PotentialTargets.Add(Relationship.Key);
							}
						}
						else if (Relationship.Value.Threat <= -45f && Relationship.Value.TotalAnger > 20f)
						{
							PotentialTargets.Add(Relationship.Key);
						}
					//Label0:
						this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
						continue;
					}
					default:
					{
						continue;   //this doesn't actually do anything, since it's at the end of the loop anyways
					}
				}
			}
			if (PotentialTargets.Count > 0 && numberofWars <= 1)
			{
				Empire ToAttack = PotentialTargets.First<Empire>();
				this.empire.GetRelations()[ToAttack].PreparingForWar = true;
			}
		}

		private void DoAggRuthAgentManagerORIG()
		{
			string Names;
			this.DesiredAgentsPerHostile = 2;
			this.DesiredAgentsPerNeutral = 0;
			this.BaseAgents = 1;
			this.DesiredAgentCount = 0;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				if (Relationship.Value.Posture == Posture.Hostile)
				{
					GSAI desiredAgentCount = this;
					desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
				}
				if (Relationship.Value.Posture != Posture.Neutral)
				{
					continue;
				}
				GSAI gSAI = this;
				gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
			}
			GSAI desiredAgentCount1 = this;
			desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
			if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 500f)
			{
				Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
				string[] Tokens = Names.Split(new char[] { ',' });
				Agent a = new Agent()
				{
					Name = AgentComponent.GetName(Tokens)
				};
				this.empire.data.AgentList.Add(a);
				Empire money = this.empire;
				money.Money = money.Money - 250f;
			}
			int Defenders = 0;
			int Offense = 0;
			foreach (Agent a in this.empire.data.AgentList)
			{
				if (a.Mission == AgentMission.Defending)
				{
					Defenders++;
				}
				else if (a.Mission != AgentMission.Undercover)
				{
					Offense++;
				}
				if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.empire.Money <= 50f)
				{
					continue;
				}
				a.AssignMission(AgentMission.Training, this.empire, "");
			}
			int DesiredOffense = this.empire.data.AgentList.Count / 2;
			foreach (Agent agent in this.empire.data.AgentList)
			{
				if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 300f)
				{
					continue;
				}
				List<Empire> PotentialTargets = new List<Empire>();
				foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
				{
					if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
					{
						continue;
					}
					PotentialTargets.Add(Relation.Key);
				}
				if (PotentialTargets.Count <= 0)
				{
					continue;
				}
				List<AgentMission> PotentialMissions = new List<AgentMission>();
				Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
				if (this.empire.GetRelations()[Target].AtWar)
				{
					PotentialMissions.Add(AgentMission.InciteRebellion);
					PotentialMissions.Add(AgentMission.Sabotage);
					PotentialMissions.Add(AgentMission.Robbery);
				}
				if (this.empire.GetRelations()[Target].Posture == Posture.Hostile)
				{
					PotentialMissions.Add(AgentMission.StealTech);
					PotentialMissions.Add(AgentMission.Robbery);
					PotentialMissions.Add(AgentMission.Infiltrate);
				}
				if (this.empire.GetRelations()[Target].SpiesDetected > 0)
				{
					PotentialMissions.Add(AgentMission.Assassinate);
				}
				if (PotentialMissions.Count <= 0)
				{
					continue;
				}
				AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
				agent.AssignMission(am, this.empire, Target.data.Traits.Name);
				Offense++;
			}
		}
        //added by gremlin aggruthmanager
        private void DoAggRuthAgentManager()
        {
            string Names;

            int income = (int)this.empire.GetAverageNetIncome();


            this.DesiredAgentsPerHostile = (int)(income * .08f) + 1;
            this.DesiredAgentsPerNeutral = (int)(income * .03f) + 1;

            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 2;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            int empirePlanetSpys = empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 300f && currentSpies < empirePlanetSpys)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                Empire money = this.empire;
                money.Money = money.Money - 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.empire.Money <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .5); // (int)(0.33f * (float)this.empire.data.AgentList.Count);
            //int DesiredOffense = this.empire.data.AgentList.Count / 2;
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 250f)
                {
                    continue;
                }
                List<Empire> PotentialTargets = new List<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                List<AgentMission> PotentialMissions = new List<AgentMission>();
                Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
                if (this.empire.GetRelations()[Target].AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        //PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (this.empire.GetRelations()[Target].Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                }


                if (this.empire.GetRelations()[Target].SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }
		private void DoCunningAgentManagerORIG()
		{
			string Names;
			this.BaseAgents = 3;
			this.DesiredAgentsPerHostile = 2;
			this.DesiredAgentsPerNeutral = 2;
			this.DesiredAgentCount = 0;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				if (Relationship.Value.Posture == Posture.Hostile)
				{
					GSAI desiredAgentCount = this;
					desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
				}
				if (Relationship.Value.Posture != Posture.Neutral)
				{
					continue;
				}
				GSAI gSAI = this;
				gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
			}
			GSAI desiredAgentCount1 = this;
			desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
			if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 300f)
			{
				Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
				string[] Tokens = Names.Split(new char[] { ',' });
				Agent a = new Agent()
				{
					Name = AgentComponent.GetName(Tokens)
				};
				this.empire.data.AgentList.Add(a);
				Empire money = this.empire;
				money.Money = money.Money - 250f;
			}
			int Defenders = 0;
			int Offense = 0;
			foreach (Agent a in this.empire.data.AgentList)
			{
				if (a.Mission == AgentMission.Defending)
				{
					Defenders++;
				}
				else if (a.Mission != AgentMission.Undercover)
				{
					Offense++;
				}
				if (a.Mission != AgentMission.Defending || a.Level >= 3 || this.empire.Money <= 75f)
				{
					continue;
				}
				a.AssignMission(AgentMission.Training, this.empire, "");
			}
			int DesiredOffense = this.empire.data.AgentList.Count - (int)(0.33f * (float)this.empire.data.AgentList.Count);
			foreach (Agent agent in this.empire.data.AgentList)
			{
				if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 250f)
				{
					continue;
				}
				List<Empire> PotentialTargets = new List<Empire>();
				foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
				{
					if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
					{
						continue;
					}
					PotentialTargets.Add(Relation.Key);
				}
				if (PotentialTargets.Count <= 0)
				{
					continue;
				}
				List<AgentMission> PotentialMissions = new List<AgentMission>();
				Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
				if (this.empire.GetRelations()[Target].AtWar)
				{
					PotentialMissions.Add(AgentMission.InciteRebellion);
					PotentialMissions.Add(AgentMission.Sabotage);
					PotentialMissions.Add(AgentMission.Robbery);
				}
				if (this.empire.GetRelations()[Target].Posture == Posture.Hostile)
				{
					PotentialMissions.Add(AgentMission.StealTech);
					PotentialMissions.Add(AgentMission.Robbery);
					PotentialMissions.Add(AgentMission.Infiltrate);
				}
				if (this.empire.GetRelations()[Target].Posture == Posture.Neutral || this.empire.GetRelations()[Target].Posture == Posture.Friendly)
				{
					PotentialMissions.Add(AgentMission.StealTech);
					PotentialMissions.Add(AgentMission.Robbery);
				}
				if (this.empire.GetRelations()[Target].SpiesDetected > 0)
				{
					PotentialMissions.Add(AgentMission.Assassinate);
				}
				if (PotentialMissions.Count <= 0)
				{
					continue;
				}
				AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
				agent.AssignMission(am, this.empire, Target.data.Traits.Name);
				Offense++;
			}
		}
        //added by gremlin CunningAgent
        private void DoCunningAgentManager()
        {
            int income = (int)this.empire.GetAverageNetIncome();
            string Names;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentsPerHostile = (int)(income * .10f) + 1;
            this.DesiredAgentsPerNeutral = (int)(income * .05f) + 1;

            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            int empirePlanetSpys = this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            if (this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 300f && currentSpies < empirePlanetSpys)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                Empire money = this.empire;
                money.Money = money.Money - 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }

                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.empire.Money <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .6);// (int)(0.20f * (float)this.empire.data.AgentList.Count);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 250f)
                {
                    continue;
                }
                List<Empire> PotentialTargets = new List<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                List<AgentMission> PotentialMissions = new List<AgentMission>();
                Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
                if (this.empire.GetRelations()[Target].AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {

                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                        if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }


                }
                if (this.empire.GetRelations()[Target].Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);

                    }
                    if (agent.Level >= 4)
                    {

                        PotentialMissions.Add(AgentMission.Sabotage);
                        if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (this.empire.GetRelations()[Target].Posture == Posture.Neutral || this.empire.GetRelations()[Target].Posture == Posture.Friendly)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                    if (agent.Level >= 4)
                    {
                        //PotentialMissions.Add(AgentMission.Robbery);
                        if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }

                }
                if (this.empire.GetRelations()[Target].SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }

		private void DoCunningRelations()
		{
			this.DoHonorableRelations();
		}

        private void DoHonorableRelations()
        {
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.GetRelations())
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            float usedTrust1 = 0.0f;
                            foreach (TrustEntry trustEntry in (List<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust1 += trustEntry.TrustCost;
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && (double)Relationship.Value.Trust - (double)usedTrust1 > (double)this.empire.data.DiplomaticPersonality.Trade && (Relationship.Value.turnsSinceLastContact > this.SecondDemand && !Relationship.Value.Treaty_Trade) && !Relationship.Value.HaveRejected_TRADE)
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_TRADE), (Action<bool>)(x => r.HaveRejected_TRADE = x));
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust1);
                            if (Relationship.Value.TurnsAbove95 > 100 && Relationship.Value.turnsSinceLastContact > 10 && (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) && (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance && (double)Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_Alliance), (Action<bool>)(x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    this.SetAlliance(!r.HaveRejected_Alliance);
                                }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                {
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "OFFER_ALLIANCE", offer2, offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_NAPACT), (Action<bool>)(x => r.HaveRejected_NAPACT = x));
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            float usedTrust2 = 0.0f;
                            foreach (TrustEntry trustEntry in (List<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust2 += trustEntry.TrustCost;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust2);
                            continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                List<Empire> list = new List<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.GetRelations())
                                {
                                    if (keyValuePair.Value.Treaty_Alliance && keyValuePair.Key.GetRelations()[Relationship.Key].Known && !keyValuePair.Key.GetRelations()[Relationship.Key].AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) && this.empire.GetRelations()[Ally].turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if ((double)Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((double)(Relationship.Value.Anger_FromShipsInOurBorders + Relationship.Value.Anger_TerritorialConflict) > (double)this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                this.AssessAngerPacifist(Relationship, Posture.Hostile, 100f);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
            }
        }

		private void DoHonPacAgentManagerORIG()
		{
			string Names;
			this.DesiredAgentsPerHostile = 2;
			this.DesiredAgentsPerNeutral = 1;
			this.DesiredAgentCount = 0;
			this.BaseAgents = 1;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				if (Relationship.Value.Posture == Posture.Hostile)
				{
					GSAI desiredAgentCount = this;
					desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
				}
				if (Relationship.Value.Posture != Posture.Neutral)
				{
					continue;
				}
				GSAI gSAI = this;
				gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
			}
			GSAI desiredAgentCount1 = this;
			desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
			if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 500f)
			{
				Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
				string[] Tokens = Names.Split(new char[] { ',' });
				Agent a = new Agent()
				{
					Name = AgentComponent.GetName(Tokens)
				};
				this.empire.data.AgentList.Add(a);
				Empire money = this.empire;
				money.Money = money.Money - 250f;
			}
			int Defenders = 0;
			int Offense = 0;
			foreach (Agent a in this.empire.data.AgentList)
			{
				if (a.Mission == AgentMission.Defending)
				{
					Defenders++;
				}
				else if (a.Mission != AgentMission.Undercover)
				{
					Offense++;
				}
				if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.empire.Money <= 300f)
				{
					continue;
				}
				a.AssignMission(AgentMission.Training, this.empire, "");
			}
			int DesiredOffense = this.empire.data.AgentList.Count / 2;
			foreach (Agent agent in this.empire.data.AgentList)
			{
				if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 300f)
				{
					continue;
				}
				List<Empire> PotentialTargets = new List<Empire>();
				foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
				{
					if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
					{
						continue;
					}
					PotentialTargets.Add(Relation.Key);
				}
				if (PotentialTargets.Count <= 0)
				{
					continue;
				}
				List<AgentMission> PotentialMissions = new List<AgentMission>();
				Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
				if (this.empire.GetRelations()[Target].AtWar)
				{
					PotentialMissions.Add(AgentMission.InciteRebellion);
					PotentialMissions.Add(AgentMission.Sabotage);
					PotentialMissions.Add(AgentMission.Robbery);
					PotentialMissions.Add(AgentMission.StealTech);
				}
				if (this.empire.GetRelations()[Target].SpiesDetected > 0)
				{
					PotentialMissions.Add(AgentMission.Assassinate);
				}
				if (PotentialMissions.Count <= 0)
				{
					continue;
				}
				AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
				agent.AssignMission(am, this.empire, Target.data.Traits.Name);
				Offense++;
			}
		}
        //added by gremlin deveks HonPacManager
        private void DoHonPacAgentManager()
        {
            string Names;

            int income = (int)this.empire.GetAverageNetIncome();


            this.DesiredAgentsPerHostile = (int)(income * .05f) + 1;
            this.DesiredAgentsPerNeutral = (int)(income * .02f) + 1;


            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 1;
            this.DesiredAgentCount = 0;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            int empirePlanetSpys = empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            if (empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;

            if (this.empire.data.AgentList.Count < this.DesiredAgentCount && this.empire.Money >= 300f && this.empire.data.AgentList.Count < empirePlanetSpys)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                Empire money = this.empire;
                money.Money = money.Money - 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.empire.Money <= 200f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .75);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense || this.empire.Money <= 300f)
                {
                    continue;
                }
                List<Empire> PotentialTargets = new List<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.GetRelations())
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                List<AgentMission> PotentialMissions = new List<AgentMission>();
                Empire Target = PotentialTargets[HelperFunctions.GetRandomIndex(PotentialTargets.Count)];
                if (this.empire.GetRelations()[Target].AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {

                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        //PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (this.empire.GetRelations()[Target].SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[HelperFunctions.GetRandomIndex(PotentialMissions.Count)];
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }
        private void DoPacifistRelations()
        {
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.GetRelations())
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    float usedTrust = 0.0f;
                    foreach (TrustEntry trustEntry in (List<TrustEntry>)Relationship.Value.TrustEntries)
                        usedTrust += trustEntry.TrustCost;
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Trade && (!Relationship.Value.HaveRejected_TRADE && (double)Relationship.Value.Trust - (double)usedTrust > (double)this.empire.data.DiplomaticPersonality.Trade) && (!Relationship.Value.Treaty_Trade && Relationship.Value.turnsSinceLastContact > this.SecondDemand && !Relationship.Value.HaveRejected_TRADE))
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_TRADE), (Action<bool>)(x => r.HaveRejected_TRADE = x));
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust);
                            if (Relationship.Value.TurnsAbove95 > 100 && Relationship.Value.turnsSinceLastContact > 10 && (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) && (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance && (double)Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_Alliance), (Action<bool>)(x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    this.SetAlliance(!r.HaveRejected_Alliance);
                                }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                {
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "OFFER_ALLIANCE", offer2, offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_NAPACT), (Action<bool>)(x => r.HaveRejected_NAPACT = x));
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
                                    this.empire.GetUS().ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust);
                            if ((double)Relationship.Value.Trust > 50.0 && (double)Relationship.Value.TotalAnger < 10.0)
                            {
                                Relationship.Value.Posture = Posture.Friendly;
                                continue;
                            }
                            else
                                continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                List<Empire> list = new List<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.GetRelations())
                                {
                                    if (keyValuePair.Value.Treaty_Alliance && keyValuePair.Key.GetRelations()[Relationship.Key].Known && !keyValuePair.Key.GetRelations()[Relationship.Key].AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) && this.empire.GetRelations()[Ally].turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if ((double)Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((double)(Relationship.Value.Anger_FromShipsInOurBorders + Relationship.Value.Anger_TerritorialConflict) > (double)this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            else
                                continue;
                        default:
                            continue;
                    }
                }
            }
        }

		private void DoRuthlessRelations()
		{
			int numberofWars = 0;
			List<Empire> PotentialTargets = new List<Empire>();
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.AtWar || Relationship.Key.data.Defeated)
				{
					continue;
				}
				numberofWars++;
			}
		//Label0:
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar && (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" || Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
				{
					Offer NAPactOffer = new Offer()
					{
						TradeTreaty = true,
						AcceptDL = "Trade Accepted",
						RejectDL = "Trade Rejected"
					};
					Ship_Game.Gameplay.Relationship value = Relationship.Value;
					NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
					Offer OurOffer = new Offer()
					{
						TradeTreaty = true
					};
					Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
				}
				float usedTrust = 0f;
				foreach (TrustEntry te in Relationship.Value.TrustEntries)
				{
					usedTrust = usedTrust + te.TrustCost;
				}
				this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
				Relationship.Value.Posture = Posture.Hostile;
				if (!Relationship.Value.Known || Relationship.Value.AtWar)
				{
					continue;
				}
				Relationship.Value.Posture = Posture.Hostile;
				if (Relationship.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) && Relationship.Value.Threat <= -15f && !Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
				{
					Relationship.Value.HaveInsulted_Military = true;
					this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Insult Military"));
				}
				if (Relationship.Value.Threat > 0f || Relationship.Value.TurnsKnown <= this.SecondDemand || Relationship.Value.Treaty_Alliance)
				{
					if (Relationship.Value.Threat > -45f || numberofWars != 0)
					{
						continue;
					}
					PotentialTargets.Add(Relationship.Key);
				}
				else
				{
					int i = 0;
					while (i < 5)
					{
						if (i >= this.DesiredPlanets.Count)
						{
							//goto Label0;    //this tried to restart the loop it's in => bad mojo
                            break;
						}
						if (this.DesiredPlanets[i].Owner != Relationship.Key)
						{
							i++;
						}
						else
						{
							PotentialTargets.Add(Relationship.Key);
							//goto Label0;
                            break;
						}
					}
				}
			}
			if (PotentialTargets.Count > 0 && numberofWars <= 1)
			{
				IOrderedEnumerable<Empire> sortedList = 
					from target in PotentialTargets
					orderby Vector2.Distance(this.empire.GetWeightedCenter(), target.GetWeightedCenter())
					select target;
				bool foundwar = false;
				foreach (Empire e in PotentialTargets)
				{
					Empire ToAttack = e;
					if (this.empire.GetRelations()[e].Treaty_NAPact)
					{
						continue;
					}
					this.empire.GetRelations()[ToAttack].PreparingForWar = true;
					foundwar = true;
				}
				if (!foundwar)
				{
					Empire ToAttack = sortedList.First<Empire>();
					this.empire.GetRelations()[ToAttack].PreparingForWar = true;
				}
			}
		}

		private void DoXenophobicRelations()
		{
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				float usedTrust = 0f;
				foreach (TrustEntry te in Relationship.Value.TrustEntries)
				{
					usedTrust = usedTrust + te.TrustCost;
				}
				this.AssessDiplomaticAnger(Relationship);
				switch (Relationship.Value.Posture)
				{
					case Posture.Friendly:
					{
						if (Relationship.Value.TurnsKnown <= this.SecondDemand || Relationship.Value.Trust - usedTrust <= (float)this.empire.data.DiplomaticPersonality.Trade || Relationship.Value.Treaty_Trade || Relationship.Value.HaveRejected_TRADE || Relationship.Value.turnsSinceLastContact <= this.SecondDemand || Relationship.Value.HaveRejected_TRADE)
						{
							continue;
						}
						Offer NAPactOffer = new Offer()
						{
							TradeTreaty = true,
							AcceptDL = "Trade Accepted",
							RejectDL = "Trade Rejected"
						};
						Ship_Game.Gameplay.Relationship value = Relationship.Value;
						NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
						Offer OurOffer = new Offer()
						{
							TradeTreaty = true
						};
						if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
						{
							Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
							continue;
						}
						else
						{
							this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Offer Trade", new Offer(), NAPactOffer));
							continue;
						}
					}
					case Posture.Neutral:
					{
						if (Relationship.Value.TurnsKnown >= this.FirstDemand && !Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Demand_Tech && !Relationship.Value.XenoDemandedTech)
						{
							List<string> PotentialDemands = new List<string>();
							foreach (KeyValuePair<string, TechEntry> tech in Relationship.Key.GetTDict())
							{
								if (!tech.Value.Unlocked || this.empire.GetTDict()[tech.Key].Unlocked)
								{
									continue;
								}
								PotentialDemands.Add(tech.Key);
							}
							if (PotentialDemands.Count > 0)
							{
								int Random = (int)RandomMath.RandomBetween(0f, (float)PotentialDemands.Count + 0.75f);
								if (Random > PotentialDemands.Count - 1)
								{
									Random = PotentialDemands.Count - 1;
								}
								string TechToDemand = PotentialDemands[Random];
								Offer DemandTech = new Offer();
								DemandTech.TechnologiesOffered.Add(TechToDemand);
								Relationship.Value.XenoDemandedTech = true;
								Offer TheirDemand = new Offer()
								{
									AcceptDL = "Xeno Demand Tech Accepted",
									RejectDL = "Xeno Demand Tech Rejected"
								};
								Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
								TheirDemand.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_Demand_Tech, (bool x) => relationship.HaveRejected_Demand_Tech = x);
								Relationship.Value.turnsSinceLastContact = 0;
								if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
								{
									Relationship.Key.GetGSAI().AnalyzeOffer(DemandTech, TheirDemand, this.empire, Offer.Attitude.Threaten);
								}
								else
								{
									this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Xeno Demand Tech", DemandTech, TheirDemand));
								}
							}
						}
						if (!Relationship.Value.HaveRejected_Demand_Tech)
						{
							continue;
						}
						Relationship.Value.Posture = Posture.Hostile;
						continue;
					}
					default:
					{
						continue;
					}
				}
			}
		}

		public void EndWarFromEvent(Empire them)
		{
			this.empire.GetRelations()[them].AtWar = false;
			them.GetRelations()[this.empire].AtWar = false;
			lock (GlobalStats.TaskLocker)
			{
				foreach (MilitaryTask task in this.TaskList)
				{
					if (this.empire.GetFleetsDict().ContainsKey(task.WhichFleet) && this.empire.data.Traits.Name == "Corsairs")
					{
						bool foundhome = false;
						foreach (Ship ship in this.empire.GetShips())
						{
							if (!(ship.Role == "station") && !(ship.Role == "platform"))
							{
								continue;
							}
							foundhome = true;
							List<Ship>.Enumerator enumerator = this.empire.GetFleetsDict()[task.WhichFleet].Ships.GetEnumerator();
							try
							{
								while (enumerator.MoveNext())
								{
									Ship fship = enumerator.Current;
									fship.GetAI().OrderQueue.Clear();
									fship.DoEscort(ship);
								}
								break;
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
						}
						if (!foundhome)
						{
							foreach (Ship ship in this.empire.GetFleetsDict()[task.WhichFleet].Ships)
							{
								ship.GetAI().OrderQueue.Clear();
								ship.GetAI().State = AIState.AwaitingOrders;
							}
						}
					}
					task.EndTaskWithMove();
				}
			}
		}

		public void FactionUpdate()
		{
			string name = this.empire.data.Traits.Name;
			if (name != null && name == "Corsairs")
			{
				bool AttackingSomeone = false;
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in this.TaskList)
					{
						if (task.type != MilitaryTask.TaskType.CorsairRaid)
						{
							continue;
						}
						AttackingSomeone = true;
					}
				}
				if (!AttackingSomeone)
				{
					foreach (KeyValuePair<Empire, Relationship> r in this.empire.GetRelations())
					{
						if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || this.empire.GetShips().Count <= 0)
						{
							continue;
						}
						Vector2 center = new Vector2();
						foreach (Ship ship in this.empire.GetShips())
						{
							center = center + ship.Center;
						}
						center = center / (float)this.empire.GetShips().Count;
						IOrderedEnumerable<Planet> sortedList = 
							from planet in r.Key.GetPlanets()
							orderby Vector2.Distance(planet.Position, center)
							select planet;
						MilitaryTask task = new MilitaryTask(this.empire);
						task.SetTargetPlanet(sortedList.First<Planet>());
						task.TaskTimer = 300f;
						task.type = MilitaryTask.TaskType.CorsairRaid;
						lock (GlobalStats.TaskLocker)
						{
							this.TaskList.Add(task);
						}
					}
				}
			}
			lock (GlobalStats.TaskLocker)
			{
				foreach (MilitaryTask task in this.TaskList)
				{
					if (task.type != MilitaryTask.TaskType.Exploration)
					{
						task.Evaluate(this.empire);
					}
					else
					{
						task.EndTask();
					}
				}
			}
		}

		private void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
		{
			List<Planet> InvasionTargets = new List<Planet>();
			foreach (Planet p in this.empire.GetPlanets())
			{
				foreach (Planet toCheck in p.system.PlanetList)
				{
					if (toCheck.Owner == null || toCheck.Owner == this.empire || !toCheck.Owner.isFaction && !this.empire.GetRelations()[toCheck.Owner].AtWar)
					{
						continue;
					}
					InvasionTargets.Add(toCheck);
				}
			}
			if (InvasionTargets.Count > 0)
			{
				Planet target = InvasionTargets[0];
				bool OK = true;
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in this.TaskList)
					{
						if (task.GetTargetPlanet() != target)
						{
							continue;
						}
						OK = false;
						break;
					}
				}
				if (OK)
				{
					MilitaryTask InvadeTask = new MilitaryTask(target, this.empire);
					lock (GlobalStats.TaskLocker)
					{
						this.TaskList.Add(InvadeTask);
					}
				}
			}
			List<Planet> PlanetsWeAreInvading = new List<Planet>();
			lock (GlobalStats.TaskLocker)
			{
				foreach (MilitaryTask task in this.TaskList)
				{
					if (task.type != MilitaryTask.TaskType.AssaultPlanet || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != r.Key)
					{
						continue;
					}
					PlanetsWeAreInvading.Add(task.GetTargetPlanet());
				}
			}
			if (PlanetsWeAreInvading.Count < 3 && this.empire.GetPlanets().Count > 0)
			{
				Vector2 vector2 = this.FindAveragePosition(this.empire);
				this.FindAveragePosition(r.Key);
				IOrderedEnumerable<Planet> sortedList = 
					from planet in r.Key.GetPlanets()
					orderby Vector2.Distance(vector2, planet.Position)
					select planet;
				foreach (Planet p in sortedList)
				{
					if (PlanetsWeAreInvading.Contains(p))
					{
						continue;
					}
					if (PlanetsWeAreInvading.Count >= 3)
					{
						break;
					}
					PlanetsWeAreInvading.Add(p);
					MilitaryTask invade = new MilitaryTask(p, this.empire);
					lock (GlobalStats.TaskLocker)
					{
						this.TaskList.Add(invade);
					}
				}
			}
		}

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r)
        {
            switch (r.Value.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    List<Planet> list1 = new List<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet)));
                    for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1); ++index)
                    {
                        list1.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable1, index));
                        if (index == 2)
                            break;
                    }
                    using (List<Planet>.Enumerator enumerator = list1.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Planet current = enumerator.Current;
                            bool flag = true;
                            lock (GlobalStats.TaskLocker)
                            {
                                foreach (MilitaryTask item_0 in (List<MilitaryTask>)this.TaskList)
                                {
                                    if (item_0.GetTargetPlanet() == current && item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                            }
                            if (flag)
                            {
                                MilitaryTask militaryTask = new MilitaryTask(current, this.empire);
                                lock (GlobalStats.TaskLocker)
                                    this.TaskList.Add(militaryTask);
                            }
                        }
                        break;
                    }
                case WarType.ImperialistWar:
                    List<Planet> list2 = new List<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet)));
                    for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable2); ++index)
                    {
                        list2.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable2, index));
                        if (index == 2)
                            break;
                    }
                    using (List<Planet>.Enumerator enumerator = list2.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Planet current = enumerator.Current;
                            bool flag = true;
                            lock (GlobalStats.TaskLocker)
                            {
                                foreach (MilitaryTask item_1 in (List<MilitaryTask>)this.TaskList)
                                {
                                    if (item_1.GetTargetPlanet() == current && item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                            }
                            if (flag)
                            {
                                MilitaryTask militaryTask = new MilitaryTask(current, this.empire);
                                lock (GlobalStats.TaskLocker)
                                    this.TaskList.Add(militaryTask);
                            }
                        }
                        break;
                    }
            }
        }

		private Vector2 FindAveragePosition(Empire e)
		{
			Vector2 AvgPos = new Vector2();
			foreach (Planet p in e.GetPlanets())
			{
				AvgPos = AvgPos + p.Position;
			}
			if (e.GetPlanets().Count <= 0)
			{
				return Vector2.Zero;
			}
			Vector2 count = AvgPos / (float)e.GetPlanets().Count;
			AvgPos = count;
			return count;
		}

		private SolarSystem FindBestRoadOrigin(SolarSystem Origin, SolarSystem Destination)
		{
			SolarSystem Closest = Origin;
			List<SolarSystem> ConnectedToOrigin = new List<SolarSystem>();
			foreach (SpaceRoad road in this.empire.SpaceRoadsList)
			{
				if (road.GetOrigin() != Origin)
				{
					continue;
				}
				ConnectedToOrigin.Add(road.GetDestination());
			}
			foreach (SolarSystem system in ConnectedToOrigin)
			{
				if (Vector2.Distance(system.Position, Destination.Position) + 25000f >= Vector2.Distance(Closest.Position, Destination.Position))
				{
					continue;
				}
				Closest = system;
			}
			if (Closest != Origin)
			{
				Closest = this.FindBestRoadOrigin(Closest, Destination);
			}
			return Closest;
		}

		private float FindTaxRateToReturnAmount(float Amount)
		{
			for (int i = 0; i < 50; i++)
			{
				if (this.empire.EstimateIncomeAtTaxRate((float)i / 100f) >= Amount)
				{
					return (float)i / 100f;
				}
			}
			return 0.5f;
		}

		private string GetAnAssaultShip()
		{
			List<Ship> PotentialShips = new List<Ship>();
			foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
			{
				if (ResourceManager.ShipsDict[shipsWeCanBuild].TroopList.Count <= 0)
				{
					continue;
				}
				PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
			}
			if (PotentialShips.Count > 0)
			{
				IOrderedEnumerable<Ship> sortedList = 
					from ship in PotentialShips
					orderby ship.TroopList.Count descending
					select ship;
				if (sortedList.Count<Ship>() > 0)
				{
					return sortedList.First<Ship>().Name;
				}
			}
			return "";
		}

		private string GetAShipORIG()
		{
			string name;
			float ratio_Fighters = 7f;
			float ratio_Frigates = 7f;
			float ratio_Cruisers = 5f;
			float ratio_Capitals = 3f;
			float TotalMilShipCount = 0f;
			float numFighters = 0f;
			float numFrigates = 0f;
			float numCruisers = 0f;
			float numCapitals = 0f;
			float num_Bombers = 0f;
			for (int i = 0; i < this.empire.GetShips().Count; i++)
			{
				Ship item = this.empire.GetShips()[i];
				if (item != null)
				{
					string role = item.Role;
					string str = role;
					if (role != null)
					{
						if (str == "fighter")
						{
							numFighters = numFighters + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
						else if (str == "corvette")
						{
							numFighters = numFighters + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
						else if (str == "frigate")
						{
							numFrigates = numFrigates + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
						else if (str == "cruiser")
						{
							numCruisers = numCruisers + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
						else if (str == "capital")
						{
							numCapitals = numCapitals + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
						else if (str == "carrier")
						{
							numCapitals = numCapitals + 1f;
							TotalMilShipCount = TotalMilShipCount + 1f;
						}
					}
					if (item.BombBays.Count > 0)
					{
						num_Bombers = num_Bombers + 1f;
					}
				}
			}
			float single = TotalMilShipCount / 10f;
			int DesiredBombers = (int)(TotalMilShipCount / 10f * ratio_Fighters);
			int DesiredFrigates = (int)(TotalMilShipCount / 10f * ratio_Frigates);
			int DesiredCruisers = (int)(TotalMilShipCount / 10f * ratio_Cruisers);
			int DesiredCapitals = (int)(TotalMilShipCount / 10f * ratio_Capitals);
			bool canBuildCapitals = this.empire.GetTDict()["Battleships"].Unlocked;
			bool canBuildCruisers = this.empire.GetTDict()["Cruisers"].Unlocked;
			bool canBuildFrigates = this.empire.GetTDict()["FrigateConstruction"].Unlocked;
			if (!canBuildFrigates && numFighters > 50f)
			{
				return null;
			}
			List<Ship> PotentialShips = new List<Ship>();
			if (canBuildCapitals && numCapitals < (float)DesiredCapitals)
			{
				foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
				{
					if (!(ResourceManager.ShipsDict[shipsWeCanBuild].Role == "capital") && !(ResourceManager.ShipsDict[shipsWeCanBuild].Role == "carrier") || ResourceManager.ShipsDict[shipsWeCanBuild].BaseStrength <= 0f && !ResourceManager.ShipsDict[shipsWeCanBuild].BaseCanWarp)
					{
						continue;
					}
					PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
				}
				if (PotentialShips.Count > 0)
				{
					IOrderedEnumerable<Ship> sortedList = 
						from ship in PotentialShips
						orderby ship.BaseStrength
						select ship;
					float totalStrength = 0f;
					foreach (Ship ship1 in sortedList)
					{
						totalStrength = totalStrength + ship1.BaseStrength;
					}
					float ran = RandomMath.RandomBetween(0f, totalStrength);
					float strcounter = 0f;
					foreach (Ship ship2 in sortedList)
					{
						strcounter = strcounter + ship2.BaseStrength;
						if (strcounter <= ran)
						{
							continue;
						}
						name = ship2.Name;
						return name;
					}
				}
			}
			if (canBuildCruisers && numCruisers < (float)DesiredCruisers)
			{
				foreach (string shipsWeCanBuild1 in this.empire.ShipsWeCanBuild)
				{
					if (!(ResourceManager.ShipsDict[shipsWeCanBuild1].Role == "cruiser") || ResourceManager.ShipsDict[shipsWeCanBuild1].BaseStrength <= 0f && !ResourceManager.ShipsDict[shipsWeCanBuild1].BaseCanWarp)
					{
						continue;
					}
					PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild1]);
				}
				if (PotentialShips.Count == 0)
				{
					this.empire.UpdateShipsWeCanBuild();
					foreach (string str1 in this.empire.ShipsWeCanBuild)
					{
						if (!(ResourceManager.ShipsDict[str1].Role == "cruiser") || ResourceManager.ShipsDict[str1].BaseStrength <= 0f && !ResourceManager.ShipsDict[str1].BaseCanWarp)
						{
							continue;
						}
						PotentialShips.Add(ResourceManager.ShipsDict[str1]);
					}
				}
				if (PotentialShips.Count > 0)
				{
					int HighestTech = 0;
					foreach (Ship ship3 in PotentialShips)
					{
						int TechScore = ship3.GetTechScore();
						if (TechScore <= HighestTech)
						{
							continue;
						}
						HighestTech = TechScore;
					}
					List<Ship> toRemove = new List<Ship>();
					foreach (Ship ship4 in PotentialShips)
					{
						if (ship4.GetTechScore() >= HighestTech)
						{
							continue;
						}
						toRemove.Add(ship4);
					}
					foreach (Ship ship5 in toRemove)
					{
						PotentialShips.Remove(ship5);
					}
					IOrderedEnumerable<Ship> sortedList = 
						from ship in PotentialShips
						orderby ship.BaseCanWarp
						select ship;
					float totalStrength = 0f;
					foreach (Ship ship6 in sortedList)
					{
						totalStrength = totalStrength + ship6.BaseStrength;
					}
					float ran = RandomMath.RandomBetween(0f, totalStrength);
					float strcounter = 0f;
					foreach (Ship ship7 in sortedList)
					{
						strcounter = strcounter + ship7.BaseStrength;
						if (strcounter <= ran)
						{
							continue;
						}
						name = ship7.Name;
						return name;
					}
				}
			}
			if (num_Bombers < (float)DesiredBombers)
			{
				foreach (string shipsWeCanBuild2 in this.empire.ShipsWeCanBuild)
				{
					if (ResourceManager.ShipsDict[shipsWeCanBuild2].BombBays.Count <= 0 || ResourceManager.ShipsDict[shipsWeCanBuild2].BaseStrength <= 0f && !ResourceManager.ShipsDict[shipsWeCanBuild2].BaseCanWarp)
					{
						continue;
					}
					PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild2]);
				}
				if (PotentialShips.Count > 0)
				{
					IOrderedEnumerable<Ship> sortedList = 
						from ship in PotentialShips
						orderby ship.BaseStrength
						select ship;
					float totalStrength = 0f;
					foreach (Ship ship8 in sortedList)
					{
						totalStrength = totalStrength + ship8.BaseStrength;
					}
					float ran = RandomMath.RandomBetween(0f, totalStrength);
					float strcounter = 0f;
					foreach (Ship ship9 in sortedList)
					{
						strcounter = strcounter + ship9.BaseStrength;
						if (strcounter <= ran)
						{
							continue;
						}
						name = ship9.Name;
						return name;
					}
				}
			}
			if (canBuildFrigates && numFrigates < (float)DesiredFrigates)
			{
				foreach (string str2 in this.empire.ShipsWeCanBuild)
				{
					if (!(ResourceManager.ShipsDict[str2].Role == "frigate") || ResourceManager.ShipsDict[str2].BaseStrength <= 0f && !ResourceManager.ShipsDict[str2].BaseCanWarp)
					{
						continue;
					}
					PotentialShips.Add(ResourceManager.ShipsDict[str2]);
				}
				if (PotentialShips.Count > 0)
				{
					IOrderedEnumerable<Ship> sortedList = 
						from ship in PotentialShips
						orderby ship.BaseStrength
						select ship;
					float totalStrength = 0f;
					foreach (Ship ship10 in sortedList)
					{
						totalStrength = totalStrength + ship10.BaseStrength;
					}
					float ran = RandomMath.RandomBetween(0f, totalStrength);
					float strcounter = 0f;
					foreach (Ship ship11 in sortedList)
					{
						strcounter = strcounter + ship11.BaseStrength;
						if (strcounter <= ran)
						{
							continue;
						}
						name = ship11.Name;
						return name;
					}
				}
			}
			foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
			{
				if (!(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "fighter") && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "scout") && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "corvette") || ResourceManager.ShipsDict[shipsWeCanBuild3].BaseStrength <= 0f && !ResourceManager.ShipsDict[shipsWeCanBuild3].BaseCanWarp)
				{
					continue;
				}
				PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
			}
			if (PotentialShips.Count > 0)
			{
				IOrderedEnumerable<Ship> sortedList = 
					from ship in PotentialShips
					orderby ship.BaseStrength
					select ship;
				float totalStrength = 0f;
				foreach (Ship ship12 in sortedList)
				{
					totalStrength = totalStrength + ship12.BaseStrength;
				}
				float ran = RandomMath.RandomBetween(0f, totalStrength);
				float strcounter = 0f;
				foreach (Ship ship13 in sortedList)
				{
					strcounter = strcounter + ship13.BaseStrength;
					if (strcounter <= ran)
					{
						continue;
					}
					name = ship13.Name;
					return name;
				}
			}
			return null;
		}
        
        //added by Gremlin Deveks Get a shio
        private string GetAShip(float Capacity)
        {
            string name;
            float ratio_Fighters = 7f;
            float ratio_Frigates = 7f;
            float ratio_Cruisers = 5f;
            float ratio_Capitals = 3f;
            float TotalMilShipCount = 0f;
            float numFighters = 0f;
            float numFrigates = 0f;
            float numCruisers = 0f;
            float numCapitals = 0f;
            float num_Bombers = 0f;
            for (int i = 0; i < this.empire.GetShips().Count; i++)
            {
                Ship item = this.empire.GetShips()[i];
                if (item != null)
                {
                    string role = item.Role;
                    string str = role;
                    //item.PowerDraw * this.empire.data.FTLPowerDrainModifier <= item.PowerFlowMax 
                    //&& item.IsWarpCapable &&item.PowerStoreMax /(item.PowerDraw* this.empire.data.FTLPowerDrainModifier) * item.velocityMaximum >Properties.Settings.Default.minimumWarpRange  && item.Name != "Small Supply Ship"
                    if (role != null && item.Mothership == null)
                    {
                        if (str == "fighter")
                        {
                            numFighters = numFighters + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                        else if (str == "corvette")
                        {
                            numFighters = numFighters + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                        else if (str == "frigate")
                        {
                            numFrigates = numFrigates + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                        else if (str == "cruiser")
                        {
                            numCruisers = numCruisers + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                        else if (str == "capital")
                        {
                            numCapitals = numCapitals + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                        else if (str == "carrier")
                        {
                            numCapitals = numCapitals + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                        }
                    }
                    if (item.BombBays.Count > 0)
                    {
                        num_Bombers = num_Bombers + 1f;
                    }
                }
            }

            bool canBuildCapitals = this.empire.GetTDict()["Battleships"].Unlocked;
            bool canBuildCruisers = this.empire.GetTDict()["Cruisers"].Unlocked;
            bool canBuildFrigates = this.empire.GetTDict()["FrigateConstruction"].Unlocked;

            //Added by McShooterz: Used to find alternate techs that allow roles to be used by AI.
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useAlternateTech)
            {
                foreach (KeyValuePair<string, TechEntry> techEntry in this.empire.GetTDict())
                {
                    if (canBuildCapitals && canBuildCruisers && canBuildFrigates)
                    {
                        break;
                    }
                    if (techEntry.Value.Unlocked)
                    {
                        if (!canBuildCapitals && techEntry.Value.GetTech().unlockBattleships)
                        {
                            canBuildCapitals = true;
                            this.empire.GetTDict()["Battleships"].Unlocked = true;

                        }
                        else if (!canBuildCruisers && techEntry.Value.GetTech().unlockCruisers)
                        {
                            canBuildCruisers = true;
                            this.empire.GetTDict()["Cruisers"].Unlocked = true;
                        }
                        else if (!canBuildFrigates && techEntry.Value.GetTech().unlockFrigates)
                        {
                            canBuildFrigates = true;
                            this.empire.GetTDict()["FrigateConstruction"].Unlocked = true;
                        }
                    }
                }
            }

            if (canBuildCapitals)
            {
                ratio_Fighters = 0f;
                ratio_Frigates = 4f;
                ratio_Cruisers = 3f;
                ratio_Capitals = 1f;
            }
            else if (canBuildCruisers)
            {

                ratio_Fighters = 0f;
                ratio_Frigates = 5f;
                ratio_Cruisers = 2f;
                ratio_Capitals = 0f;
            }
            else if (canBuildFrigates)
            {
                ratio_Fighters =5.5f;
                ratio_Frigates = 3f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
            }
            //bool carriers = this.empire.ShipsWeCanBuild.Where(hangars => ResourceManager.ShipsDict[hangars].GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0).Count() > 0;
            //bool assaultShips = this.empire.ShipsWeCanBuild.Where(hangars => ResourceManager.ShipsDict[hangars].GetHangars().Where(fighters => fighters.IsTroopBay).Count() > 0).Count() > 0;
            //float ratio_Carriers;
            //float ratio_AssaultShips;
            //if (carriers)
            //{
            //    ratio_Capitals -= .2f;
            //    ratio_Cruisers -= .2f;
            //    ratio_Carriers = .4f;

            //}
            //if (assaultShips)
            //{
            //    ratio_Capitals -= .1f;
            //    ratio_Cruisers -= .1f;
            //    ratio_Carriers = .2f;
            //}



            float single = TotalMilShipCount / 10f;

            int DesiredBombers = (int)(TotalMilShipCount / 20f * ratio_Fighters != 0 ? ratio_Fighters : ratio_Frigates);
            int DesiredFrigates = (int)(TotalMilShipCount / 10f * ratio_Frigates);
            int DesiredCruisers = (int)(TotalMilShipCount / 10f * ratio_Cruisers);
            int DesiredCapitals = (int)(TotalMilShipCount / 10f * ratio_Capitals);
            if (!canBuildFrigates && numFighters > 50f)
            {
                return null;
            }
            List<Ship> PotentialShips = new List<Ship>();
            if (canBuildCapitals && numCapitals < (float)DesiredCapitals)
            {
                foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
                {
                    if (!((ResourceManager.ShipsDict[shipsWeCanBuild].Role == "capital" || ResourceManager.ShipsDict[shipsWeCanBuild].Role == "carrier") && ResourceManager.ShipsDict[shipsWeCanBuild].BaseStrength > 0f && Capacity >= ResourceManager.ShipsDict[shipsWeCanBuild].GetMaintCost()) && !(ResourceManager.ShipsDict[shipsWeCanBuild].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild].PowerDraw * this.empire.data.FTLPowerDrainModifier >= ResourceManager.ShipsDict[shipsWeCanBuild].PowerFlowMax || ResourceManager.ShipsDict[shipsWeCanBuild].PowerStoreMax / (ResourceManager.ShipsDict[shipsWeCanBuild].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild].velocityMaximum > minimumWarpRange)))
                    {
                        continue;
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
                }
                if (PotentialShips.Count > 0)
                {
                    IOrderedEnumerable<Ship> sortedList =
                        from ship in PotentialShips
                        orderby ship.BaseStrength
                        select ship;
                    float totalStrength = 0f;
                    foreach (Ship ship1 in sortedList)
                    {
                        totalStrength = totalStrength + ship1.BaseStrength;
                    }
                    float ran = RandomMath.RandomBetween(0f, totalStrength);
                    float strcounter = 0f;
                    foreach (Ship ship2 in sortedList)
                    {
                        strcounter = strcounter + ship2.BaseStrength;
                        if (strcounter <= ran)
                        {
                            continue;
                        }
                        name = ship2.Name;
                        return name;
                    }
                }
            }
            if (canBuildCruisers && numCruisers < (float)DesiredCruisers)
            {
                foreach (string shipsWeCanBuild1 in this.empire.ShipsWeCanBuild)
                {
                    if (!(ResourceManager.ShipsDict[shipsWeCanBuild1].Role == "cruiser" && ResourceManager.ShipsDict[shipsWeCanBuild1].BaseStrength >= 0f && Capacity >= ResourceManager.ShipsDict[shipsWeCanBuild1].GetMaintCost() && (ResourceManager.ShipsDict[shipsWeCanBuild1].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild1].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild1].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild1].PowerFlowMax || ResourceManager.ShipsDict[shipsWeCanBuild1].PowerStoreMax / (ResourceManager.ShipsDict[shipsWeCanBuild1].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild1].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild1].velocityMaximum > minimumWarpRange)))))
                    {
                        continue;
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild1]);
                }
                if (PotentialShips.Count == 0)
                {
                    this.empire.UpdateShipsWeCanBuild();
                    foreach (string str1 in this.empire.ShipsWeCanBuild)
                    {
                        if (!(ResourceManager.ShipsDict[str1].Role == "cruiser" && ResourceManager.ShipsDict[str1].BaseStrength <= 0f && ResourceManager.ShipsDict[str1].GetMaintCost() >= Capacity && (ResourceManager.ShipsDict[str1].BaseCanWarp && (ResourceManager.ShipsDict[str1].IsWarpCapable && (ResourceManager.ShipsDict[str1].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[str1].PowerFlowMax || ResourceManager.ShipsDict[str1].PowerStoreMax / (ResourceManager.ShipsDict[str1].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[str1].PowerFlowMax) * ResourceManager.ShipsDict[str1].velocityMaximum > minimumWarpRange)))))
                        {
                            continue;
                        }
                        PotentialShips.Add(ResourceManager.ShipsDict[str1]);
                    }
                }
                if (PotentialShips.Count > 0)
                {
                    int HighestTech = 0;
                    foreach (Ship ship3 in PotentialShips)
                    {
                        int TechScore = ship3.GetTechScore();
                        if (TechScore <= HighestTech)
                        {
                            continue;
                        }
                        HighestTech = TechScore;
                    }
                    List<Ship> toRemove = new List<Ship>();
                    foreach (Ship ship4 in PotentialShips)
                    {
                        if (ship4.GetTechScore() >= HighestTech)
                        {
                            continue;
                        }
                        toRemove.Add(ship4);
                    }
                    foreach (Ship ship5 in toRemove)
                    {
                        PotentialShips.Remove(ship5);
                    }
                    IOrderedEnumerable<Ship> sortedList =
                        from ship in PotentialShips
                        orderby ship.BaseCanWarp
                        select ship;
                    float totalStrength = 0f;
                    foreach (Ship ship6 in sortedList)
                    {
                        totalStrength = totalStrength + ship6.BaseStrength;
                    }
                    float ran = RandomMath.RandomBetween(0f, totalStrength);
                    float strcounter = 0f;
                    foreach (Ship ship7 in sortedList)
                    {
                        strcounter = strcounter + ship7.BaseStrength;
                        if (strcounter <= ran)
                        {
                            continue;
                        }
                        name = ship7.Name;
                        return name;
                    }
                }
            }
            if (num_Bombers < (float)DesiredBombers)
            {
                foreach (string shipsWeCanBuild2 in this.empire.ShipsWeCanBuild)
                {
                    if (ResourceManager.ShipsDict[shipsWeCanBuild2].BombBays.Count <= 0 || ResourceManager.ShipsDict[shipsWeCanBuild2].BaseStrength <= 0f || ResourceManager.ShipsDict[shipsWeCanBuild2].GetMaintCost() >= Capacity || !(ResourceManager.ShipsDict[shipsWeCanBuild2].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild2].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild2].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild2].PowerFlowMax || ResourceManager.ShipsDict[shipsWeCanBuild2].PowerStoreMax / (ResourceManager.ShipsDict[shipsWeCanBuild2].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild2].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild2].velocityMaximum > minimumWarpRange))))
                    {
                        continue;
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild2]);
                }
                if (PotentialShips.Count > 0)
                {
                    IOrderedEnumerable<Ship> sortedList =
                        from ship in PotentialShips
                        orderby ship.BaseStrength
                        select ship;
                    float totalStrength = 0f;
                    foreach (Ship ship8 in sortedList)
                    {
                        totalStrength = totalStrength + ship8.BaseStrength;
                    }
                    float ran = RandomMath.RandomBetween(0f, totalStrength);
                    float strcounter = 0f;
                    foreach (Ship ship9 in sortedList)
                    {
                        strcounter = strcounter + ship9.BaseStrength;
                        if (strcounter <= ran)
                        {
                            continue;
                        }
                        name = ship9.Name;
                        return name;
                    }
                }
            }
            if (canBuildFrigates && numFrigates < (float)DesiredFrigates)
            {
                foreach (string str2 in this.empire.ShipsWeCanBuild)
                {
                    if (!(ResourceManager.ShipsDict[str2].Role == "frigate") || ResourceManager.ShipsDict[str2].BaseStrength <= 0f || !(ResourceManager.ShipsDict[str2].BaseCanWarp && (ResourceManager.ShipsDict[str2].IsWarpCapable && (ResourceManager.ShipsDict[str2].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[str2].PowerFlowMax || (ResourceManager.ShipsDict[str2].PowerStoreMax) / (ResourceManager.ShipsDict[str2].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[str2].PowerFlowMax) * ResourceManager.ShipsDict[str2].velocityMaximum > minimumWarpRange))))
                    {
                        continue;
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[str2]);
                }
                if (PotentialShips.Count > 0)
                {
                    IOrderedEnumerable<Ship> sortedList =
                        from ship in PotentialShips
                        orderby ship.BaseStrength
                        select ship;
                    float totalStrength = 0f;
                    foreach (Ship ship10 in sortedList)
                    {
                        totalStrength = totalStrength + ship10.BaseStrength;
                    }
                    float ran = RandomMath.RandomBetween(0f, totalStrength);
                    float strcounter = 0f;
                    foreach (Ship ship11 in sortedList)
                    {
                        strcounter = strcounter + ship11.BaseStrength;
                        if (strcounter <= ran)
                        {
                            continue;
                        }
                        name = ship11.Name;
                        return name;
                    }
                }
            }
            foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
            {
                if (!(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "fighter") && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "scout") && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == "corvette") || ResourceManager.ShipsDict[shipsWeCanBuild3].BaseStrength <= 0f || !(ResourceManager.ShipsDict[shipsWeCanBuild3].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild3].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax || (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerStoreMax) / (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild3].velocityMaximum > minimumWarpRange))))
                {
                    continue;
                }
                PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
            }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship in PotentialShips
                    orderby ship.BaseStrength
                    select ship;
                float totalStrength = 0f;
                foreach (Ship ship12 in sortedList)
                {
                    totalStrength = totalStrength + ship12.BaseStrength;
                }
                float ran = RandomMath.RandomBetween(0f, totalStrength);
                float strcounter = 0f;
                foreach (Ship ship13 in sortedList)
                {
                    strcounter = strcounter + ship13.BaseStrength;
                    if (strcounter <= ran)
                    {
                        continue;
                    }
                    name = ship13.Name;
                    return name;
                }
            }

            //added by Gremlin Get Carriers
            //this.empire.ShipsWeCanBuild.Where(hangars => ResourceManager.ShipsDict[hangars].GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0) == true).Count() > 0;
            foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
            {
                if (!(ResourceManager.ShipsDict[shipsWeCanBuild3].GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0) || ResourceManager.ShipsDict[shipsWeCanBuild3].BaseStrength <= 0f || !(ResourceManager.ShipsDict[shipsWeCanBuild3].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild3].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax || (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerStoreMax) / (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild3].velocityMaximum > minimumWarpRange))))
                {
                    continue;
                }
                PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
            }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship in PotentialShips
                    orderby ship.BaseStrength
                    select ship;
                float totalStrength = 0f;
                foreach (Ship ship12 in sortedList)
                {
                    totalStrength = totalStrength + ship12.BaseStrength;
                }
                float ran = RandomMath.RandomBetween(0f, totalStrength);
                float strcounter = 0f;
                foreach (Ship ship13 in sortedList)
                {
                    strcounter = strcounter + ship13.BaseStrength;
                    if (strcounter <= ran)
                    {
                        continue;
                    }
                    name = ship13.Name;
                    return name;
                }
            }
            return null;
        }

		private float GetDistance(Empire e)
		{
			if (e.GetPlanets().Count == 0 || this.empire.GetPlanets().Count == 0)
			{
				return 0f;
			}
			Vector2 AvgPos = new Vector2();
			foreach (Planet p in e.GetPlanets())
			{
				AvgPos = AvgPos + p.Position;
			}
			AvgPos = AvgPos / (float)e.GetPlanets().Count;
			Vector2 Ouravg = new Vector2();
			foreach (Planet p in this.empire.GetPlanets())
			{
				Ouravg = Ouravg + p.Position;
			}
			Ouravg = Ouravg / (float)this.empire.GetPlanets().Count;
			return Vector2.Distance(AvgPos, Ouravg);
		}

		private float GetDistanceFromOurAO(Planet p)
		{
			IOrderedEnumerable<AO> sortedList = 
				from area in this.AreasOfOperations
				orderby Vector2.Distance(p.Position, area.Position)
				select area;
			if (sortedList.Count<AO>() == 0)
			{
				return 0f;
			}
			return Vector2.Distance(p.Position, sortedList.First<AO>().Position);
		}

		public void GetWarDeclaredOnUs(Empire WarDeclarant, WarType wt)
		{
			this.empire.GetRelations()[WarDeclarant].AtWar = true;
			this.empire.GetRelations()[WarDeclarant].FedQuest = null;
			this.empire.GetRelations()[WarDeclarant].Posture = Posture.Hostile;
			this.empire.GetRelations()[WarDeclarant].ActiveWar = new War(this.empire, WarDeclarant, this.empire.GetUS().StarDate)
			{
				WarType = wt
			};
			if (EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) != this.empire)
			{
				string name = this.empire.data.DiplomaticPersonality.Name;
				if (name != null && name == "Pacifist")
				{
					if (this.empire.GetRelations()[WarDeclarant].ActiveWar.StartingNumContestedSystems <= 0)
					{
						this.empire.GetRelations()[WarDeclarant].ActiveWar.WarType = WarType.DefensiveWar;
					}
					else
					{
						this.empire.GetRelations()[WarDeclarant].ActiveWar.WarType = WarType.BorderConflict;
					}
				}
			}
			if (this.empire.GetRelations()[WarDeclarant].Trust > 0f)
			{
				this.empire.GetRelations()[WarDeclarant].Trust = 0f;
			}
			this.empire.GetRelations()[WarDeclarant].Treaty_Alliance = false;
			this.empire.GetRelations()[WarDeclarant].Treaty_NAPact = false;
			this.empire.GetRelations()[WarDeclarant].Treaty_OpenBorders = false;
			this.empire.GetRelations()[WarDeclarant].Treaty_Trade = false;
			this.empire.GetRelations()[WarDeclarant].Treaty_Peace = false;
		}

		public void InitialzeAOsFromSave(UniverseData data)
		{
			foreach (AO area in this.AreasOfOperations)
			{
				foreach (SolarSystem sys in data.SolarSystemsList)
				{
					foreach (Planet p in sys.PlanetList)
					{
						if (p.guid != area.CoreWorldGuid)
						{
							continue;
						}
						area.SetPlanet(p);
					}
				}
				foreach (SolarSystem sys in data.SolarSystemsList)
				{
					foreach (Planet p in sys.PlanetList)
					{
						if (Vector2.Distance(p.Position, area.Position) >= area.Radius)
						{
							continue;
						}
						area.GetPlanets().Add(p);
					}
				}
				foreach (Guid guid in area.OffensiveForceGuids)
				{
					foreach (Ship ship in data.MasterShipList)
					{
						if (ship.guid != guid)//||ship.GetStrength() <=0)
						{
							continue;
						}
						area.GetOffensiveForcePool().Add(ship);
						ship.AddedOnLoad = true;
					}
				}
				foreach (Guid guid in area.ShipsWaitingGuids)
				{
					foreach (Ship ship in data.MasterShipList)
					{
						if (ship.guid != guid)
						{
							continue;
						}
						area.GetWaitingShips().Add(ship);
						ship.AddedOnLoad = true;
					}
				}
				foreach (KeyValuePair<int, Fleet> fleet in this.empire.GetFleetsDict())
				{
					if (fleet.Value.guid != area.fleetGuid)
					{
						continue;
					}
					area.SetFleet(fleet.Value);
				}
			}
		}

		public void ManageAOsORIG()
		{
			List<AO> ToRemove = new List<AO>();
			foreach (AO area in this.AreasOfOperations)
			{
				area.ThreatLevel = 0;
				if (area.GetPlanet().Owner != this.empire)
				{
					ToRemove.Add(area);
				}
				foreach (Empire e in EmpireManager.EmpireList)
				{
					if (e == this.empire || e.data.Defeated || !this.empire.GetRelations()[e].AtWar)
					{
						continue;
					}
					foreach (AO theirAO in e.GetGSAI().AreasOfOperations)
					{
						if (Vector2.Distance(area.Position, theirAO.Position) >= area.Radius * 2f)
						{
							continue;
						}
						AO threatLevel = area;
						threatLevel.ThreatLevel = threatLevel.ThreatLevel + 1;
					}
				}
			}
			foreach (AO toremove in ToRemove)
			{
				this.AreasOfOperations.Remove(toremove);
			}
			List<Planet> PotentialCores = new List<Planet>();
			foreach (Planet p in this.empire.GetPlanets())
			{
				if (p.GetMaxProductionPotential() <= 5f || !p.HasShipyard)
				{
					continue;
				}
				bool AlreadyExists = false;
				foreach (AO area in this.AreasOfOperations)
				{
					if (area.GetPlanet() != p)
					{
						continue;
					}
					AlreadyExists = true;
					break;
				}
				if (AlreadyExists)
				{
					continue;
				}
				PotentialCores.Add(p);
			}
			if (PotentialCores.Count == 0)
			{
				return;
			}
			IOrderedEnumerable<Planet> sortedList = 
				from planet in PotentialCores
				orderby planet.GetMaxProductionPotential() descending
				select planet;
			foreach (Planet p in sortedList)
			{
				bool FarEnough = true;
				foreach (AO area in this.AreasOfOperations)
				{
					if (Vector2.Distance(area.GetPlanet().Position, p.Position) >= 1500000f)
					{
						continue;
					}
					FarEnough = false;
					break;
				}
				if (!FarEnough)
				{
					continue;
				}
				AO area0 = new AO(p, 1500000f);
				this.AreasOfOperations.Add(area0);
			}
		}
        //addedby gremlin manageAOs
        public void ManageAOs()
        {
            float aoSize = Empire.universeScreen.Size.X * .2f;
            List<AO> aOs = new List<AO>();
            foreach (AO areasOfOperation in this.AreasOfOperations)
            {
                areasOfOperation.ThreatLevel = 0;
                if (areasOfOperation.GetPlanet().Owner != this.empire)
                {
                    aOs.Add(areasOfOperation);
                }
                foreach (Empire empireList in EmpireManager.EmpireList)
                {
                    if (empireList == this.empire || empireList.data.Defeated || !this.empire.GetRelations()[empireList].AtWar)
                    {
                        continue;
                    }
                    foreach (AO aO in empireList.GetGSAI().AreasOfOperations)
                    {
                        if (Vector2.Distance(areasOfOperation.Position, aO.Position) >= areasOfOperation.Radius * 2f)
                        {
                            continue;
                        }
                        AO threatLevel = areasOfOperation;
                        threatLevel.ThreatLevel = threatLevel.ThreatLevel + 1;
                    }
                }
            }
            foreach (AO aO1 in aOs)
            {
                this.AreasOfOperations.Remove(aO1);
            }
            List<Planet> planets = new List<Planet>();
            foreach (Planet planet1 in this.empire.GetPlanets())
            {
                if (planet1.GetMaxProductionPotential() <= 5f || !planet1.HasShipyard)
                {
                    continue;
                }
                bool flag = false;
                foreach (AO areasOfOperation1 in this.AreasOfOperations)
                {
                    if (areasOfOperation1.GetPlanet() != planet1)
                    {
                        continue;
                    }
                    flag = true;
                    break;
                }
                if (flag)
                {
                    continue;
                }
                planets.Add(planet1);
            }
            if (planets.Count == 0)
            {
                return;
            }
            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                orderby planet.GetMaxProductionPotential() descending
                select planet;
            foreach (Planet planet2 in maxProductionPotential)
            {
                bool flag1 = true;
                foreach (AO areasOfOperation2 in this.AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Position, planet2.Position) >= aoSize)
                    {
                        continue;
                    }
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }
                AO aO2 = new AO(planet2, aoSize);
                this.AreasOfOperations.Add(aO2);
            }
        }

		public void OfferPeace(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship, string whichPeace)
		{
			Offer OfferPeace = new Offer()
			{
				PeaceTreaty = true,
				AcceptDL = "OFFERPEACE_ACCEPTED",
				RejectDL = "OFFERPEACE_REJECTED"
			};
			Ship_Game.Gameplay.Relationship value = Relationship.Value;
			OfferPeace.ValueToModify = new Ref<bool>(() => false, (bool x) => value.SetImperialistWar());
			string dialogue = whichPeace;
			Offer OurOffer = new Offer()
			{
				PeaceTreaty = true
			};
			if (Relationship.Key != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, OfferPeace, this.empire, Offer.Attitude.Respectful);
				return;
			}
			this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), dialogue, new Offer(), OfferPeace));
		}

		public void ReformulateWarGoals()
		{
			foreach (MilitaryTask taskList in this.TaskList)
			{
			}
		}

		private void RemoveArtifact(Empire Triggerer, Artifact art)
		{
			Triggerer.data.OwnedArtifacts.Remove(art);
			if (art.DiplomacyMod > 0f)
			{
				RacialTrait traits = Triggerer.data.Traits;
				traits.DiplomacyMod = traits.DiplomacyMod - (art.DiplomacyMod + art.DiplomacyMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.FertilityMod > 0f)
			{
				EmpireData triggerer = Triggerer.data;
				triggerer.EmpireFertilityBonus = triggerer.EmpireFertilityBonus - art.FertilityMod;
				foreach (Planet planet in Triggerer.GetPlanets())
				{
					Planet fertility = planet;
					fertility.Fertility = fertility.Fertility - (art.FertilityMod + art.FertilityMod * Triggerer.data.Traits.Spiritual);
				}
			}
			if (art.GroundCombatMod > 0f)
			{
				RacialTrait groundCombatModifier = Triggerer.data.Traits;
				groundCombatModifier.GroundCombatModifier = groundCombatModifier.GroundCombatModifier - (art.GroundCombatMod + art.GroundCombatMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ModuleHPMod > 0f)
			{
				RacialTrait modHpModifier = Triggerer.data.Traits;
				modHpModifier.ModHpModifier = modHpModifier.ModHpModifier - (art.ModuleHPMod + art.ModuleHPMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.PlusFlatMoney > 0f)
			{
				EmpireData flatMoneyBonus = Triggerer.data;
				flatMoneyBonus.FlatMoneyBonus = flatMoneyBonus.FlatMoneyBonus - (art.PlusFlatMoney + art.PlusFlatMoney * Triggerer.data.Traits.Spiritual);
			}
			if (art.ProductionMod > 0f)
			{
				RacialTrait productionMod = Triggerer.data.Traits;
				productionMod.ProductionMod = productionMod.ProductionMod - (art.ProductionMod + art.ProductionMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ReproductionMod > 0f)
			{
				RacialTrait reproductionMod = Triggerer.data.Traits;
				reproductionMod.ReproductionMod = reproductionMod.ReproductionMod - (art.ReproductionMod + art.ReproductionMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ResearchMod > 0f)
			{
				RacialTrait researchMod = Triggerer.data.Traits;
				researchMod.ResearchMod = researchMod.ResearchMod - (art.ResearchMod + art.ResearchMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.SensorMod > 0f)
			{
				EmpireData sensorModifier = Triggerer.data;
				sensorModifier.SensorModifier = sensorModifier.SensorModifier - (art.SensorMod + art.SensorMod * Triggerer.data.Traits.Spiritual);
			}
			if (art.ShieldPenBonus > 0f)
			{
				EmpireData shieldPenBonusChance = Triggerer.data;
				shieldPenBonusChance.ShieldPenBonusChance = shieldPenBonusChance.ShieldPenBonusChance - (art.ShieldPenBonus + art.ShieldPenBonus * Triggerer.data.Traits.Spiritual);
			}
		}

		private void RunAgentManager()
		{
			string name = this.empire.data.DiplomaticPersonality.Name;
			string str = name;
			if (name != null)
			{
				if (str == "Cunning")
				{
					this.DoCunningAgentManager();
					return;
				}
				if (str == "Ruthless")
				{
					this.DoAggRuthAgentManager();
					return;
				}
				if (str == "Aggressive")
				{
					this.DoAggRuthAgentManager();
					return;
				}
				if (str == "Honorable")
				{
					this.DoHonPacAgentManager();
					return;
				}
				if (str == "Xenophobic")
				{
					this.DoCunningAgentManager();
					return;
				}
				if (str != "Pacifist")
				{
					return;
				}
				this.DoHonPacAgentManager();
			}
		}

		private void RunDiplomaticPlanner()
		{
			string name = this.empire.data.DiplomaticPersonality.Name;
			string str = name;
			if (name != null)
			{
				if (str == "Pacifist")
				{
					this.DoPacifistRelations();
				}
				else if (str == "Aggressive")
				{
					this.DoAggressiveRelations();
				}
				else if (str == "Honorable")
				{
					this.DoHonorableRelations();
				}
				else if (str == "Xenophobic")
				{
					this.DoXenophobicRelations();
				}
				else if (str == "Ruthless")
				{
					this.DoRuthlessRelations();
				}
				else if (str == "Cunning")
				{
					this.DoCunningRelations();
				}
			}
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (Relationship.Key.isFaction || this.empire.isFaction || Relationship.Key.data.Defeated)
				{
					continue;
				}
				this.RunEventChecker(Relationship);
			}
		}

		private void RunEconomicPlannerORIG()
		{
			if (this.empire.Money < 400f)
			{
				float TaxRate = this.FindTaxRateToReturnAmount(3f);
				this.empire.data.TaxRate = TaxRate;
				return;
			}
			float TaxRate0 = this.FindTaxRateToReturnAmount(-0.0035f * this.empire.Money);
			this.empire.data.TaxRate = TaxRate0;
		}
        //added by gremlin Economic planner
        private void RunEconomicPlanner()
        {
            float money = this.empire.Money;
            float treasuryGoal = 50f * this.empire.GetPlanets().Sum(development => development.developmentLevel);
            if (money < treasuryGoal)
            {
                float returnAmount = this.FindTaxRateToReturnAmount(Math.Abs(this.empire.GrossTaxes * .10f));
                if (this.empire.data.TaxRate >= .50f) returnAmount += .10f;
                this.empire.data.TaxRate = returnAmount;

                return;
            }
            float single = this.FindTaxRateToReturnAmount(0);
            this.empire.data.TaxRate = single;
        }

		public void RunEventChecker(KeyValuePair<Empire, Relationship> Them)
		{
			if (this.empire == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				return;
			}
			if (this.empire.isFaction)
			{
				return;
			}
			if (!Them.Value.Known)
			{
				return;
			}
			List<Planet> OurTargetPlanets = new List<Planet>();
			List<Planet> TheirTargetPlanets = new List<Planet>();
			foreach (Goal g in this.Goals)
			{
				if (g.type != GoalType.Colonize)
				{
					continue;
				}
				OurTargetPlanets.Add(g.GetMarkedPlanet());
			}
			foreach (Goal g in Them.Key.GetGSAI().Goals)
			{
				if (g.type != GoalType.Colonize)
				{
					continue;
				}
				TheirTargetPlanets.Add(g.GetMarkedPlanet());
			}
			bool MatchFound = false;
			SolarSystem sharedSystem = null;
			foreach (Ship ship in Them.Key.GetShips())
			{
				if (ship.GetAI().State != AIState.Colonize || ship.GetAI().ColonizeTarget == null)
				{
					continue;
				}
				TheirTargetPlanets.Add(ship.GetAI().ColonizeTarget);
			}
			List<Planet>.Enumerator enumerator = OurTargetPlanets.GetEnumerator();
			try
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						break;
					}
					Planet p = enumerator.Current;
					foreach (Planet other in TheirTargetPlanets)
					{
						if (p == null || other == null || p.system != other.system)
						{
							continue;
						}
						sharedSystem = p.system;
						MatchFound = true;
						break;
					}
				}
				while (!MatchFound);
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			if (sharedSystem != null && !Them.Value.AtWar && !Them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
			{
				bool TheyAreThereAlready = false;
				foreach (Planet p in sharedSystem.PlanetList)
				{
					if (p.Owner == null || p.Owner != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						continue;
					}
					TheyAreThereAlready = true;
				}
				if (!TheyAreThereAlready)
				{
					if (Them.Key == EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
					{
						this.empire.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(this.empire, EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty), "Claim System", sharedSystem));
					}
					Them.Value.WarnedSystemsList.Add(sharedSystem.guid);
				}
			}
		}

		private void RunExpansionPlannerORIG()
		{
			int numColonyGoals = 0;
			foreach (Goal g in this.Goals)
			{
				if (g.type != GoalType.Colonize)
				{
					continue;
				}
				numColonyGoals++;
			}
			if (numColonyGoals < this.desired_ColonyGoals + (this.empire.data.EconomicPersonality != null ? this.empire.data.EconomicPersonality.ColonyGoalsPlus : 0))
			{
				Planet toMark = null;
				Vector2 WeightedCenter = new Vector2();
				int numPlanets = 0;
				foreach (Planet p in this.empire.GetPlanets())
				{
					for (int i = 0; (float)i < p.Population / 1000f; i++)
					{
						WeightedCenter = WeightedCenter + p.Position;
						numPlanets++;
					}
				}
				WeightedCenter = WeightedCenter / (float)numPlanets;
				List<Goal.PlanetRanker> ranker = new List<Goal.PlanetRanker>();
				List<Goal.PlanetRanker> allPlanetsRanker = new List<Goal.PlanetRanker>();
				foreach (SolarSystem s in UniverseScreen.SolarSystemList)
				{
					if (!s.ExploredDict[this.empire])
					{
						continue;
					}
					foreach (Planet planetList in s.PlanetList)
					{
						bool ok = true;
						foreach (Goal g in this.Goals)
						{
							if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != planetList)
							{
								continue;
							}
							ok = false;
						}
						if (!ok)
						{
							continue;
						}
						IOrderedEnumerable<AO> sorted = 
							from ao in this.empire.GetGSAI().AreasOfOperations
							orderby Vector2.Distance(planetList.Position, ao.Position)
							select ao;
						if (sorted.Count<AO>() > 0)
						{
							AO ClosestAO = sorted.First<AO>();
							if (Vector2.Distance(planetList.Position, ClosestAO.Position) > ClosestAO.Radius * 2.15f)
							{
								continue;
							}
						}
						if (planetList.ExploredDict[this.empire] && planetList.habitable && planetList.Owner == null)
						{
							if (this.empire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire) > 0f)
							{
								continue;
							}
							Goal.PlanetRanker r = new Goal.PlanetRanker()
							{
								Distance = Vector2.Distance(WeightedCenter, planetList.Position)
							};
							float DistanceInJumps = r.Distance / 400000f;
							if (DistanceInJumps < 1f)
							{
								DistanceInJumps = 1f;
							}
							r.planet = planetList;
							if (this.empire.data.Traits.Cybernetic != 0)
							{
								if (planetList.MineralRichness < 1f)
								{
									continue;
								}
								r.PV = (planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps;
							}
							else
							{
								r.PV = (planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps;
							}
							//if (planetList.Type == "Barren" && this.empire.GetTDict()["Biospheres"].Unlocked)
                            //Added by McShooterz: changed the requirement from having research to having the building
                            if (planetList.Type == "Barren" && this.empire.GetBDict().ContainsKey("Biospheres"))
							{
								ranker.Add(r);
							}
							else if (planetList.Type != "Barren" && ((double)planetList.Fertility >= 1 || this.empire.GetTDict()["Aeroponics"].Unlocked || this.empire.data.Traits.Cybernetic != 0))
							{
								ranker.Add(r);
							}
						}
						if (!planetList.ExploredDict[this.empire] || !planetList.habitable || planetList.Owner == this.empire || this.empire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire) > 0f)
						{
							continue;
						}
						Goal.PlanetRanker r0 = new Goal.PlanetRanker()
						{
							Distance = Vector2.Distance(WeightedCenter, planetList.Position)
						};
						float DistanceInJumps0 = r0.Distance / 400000f;
						if (DistanceInJumps0 < 1f)
						{
							DistanceInJumps0 = 1f;
						}
						r0.planet = planetList;
						if (this.empire.data.Traits.Cybernetic != 0)
						{
							r0.PV = (planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps0;
						}
						else
						{
							r0.PV = (planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps0;
						}
						//if (!(planetList.Type == "Barren") || !this.empire.GetTDict()["Biospheres"].Unlocked)
                        if (!(planetList.Type == "Barren") || !this.empire.GetBDict().ContainsKey("Biospheres"))
						{
							if (!(planetList.Type != "Barren") || (double)planetList.Fertility < 1 && !this.empire.GetTDict()["Aeroponics"].Unlocked && this.empire.data.Traits.Cybernetic == 0)
							{
								continue;
							}
							allPlanetsRanker.Add(r0);
						}
						else
						{
							allPlanetsRanker.Add(r0);
						}
					}
				}
				if (ranker.Count > 0)
				{
					Goal.PlanetRanker winner = new Goal.PlanetRanker();
					float highest = 0f;
					foreach (Goal.PlanetRanker pr in ranker)
					{
						if (pr.PV <= highest)
						{
							continue;
						}
						bool ok = true;
						foreach (Goal g in this.Goals)
						{
							if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != pr.planet)
							{
								if (!g.Held || g.GetMarkedPlanet() == null || g.GetMarkedPlanet().system != pr.planet.system)
								{
									continue;
								}
								ok = false;
								break;
							}
							else
							{
								ok = false;
								break;
							}
						}
						if (!ok)
						{
							continue;
						}
						winner = pr;
						highest = pr.PV;
					}
					toMark = winner.planet;
				}
				if (allPlanetsRanker.Count > 0)
				{
					this.DesiredPlanets.Clear();
					IOrderedEnumerable<Goal.PlanetRanker> sortedList = 
						from ran in allPlanetsRanker
						orderby ran.PV descending
						select ran;
					for (int i = 0; i < allPlanetsRanker.Count; i++)
					{
						this.DesiredPlanets.Add(sortedList.ElementAt<Goal.PlanetRanker>(i).planet);
					}
				}
				if (toMark != null)
				{
					bool ok = true;
					foreach (Goal g in this.Goals)
					{
						if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != toMark)
						{
							continue;
						}
						ok = false;
					}
					if (ok)
					{
						Goal cgoal = new Goal(toMark, this.empire)
						{
							GoalName = "MarkForColonization"
						};
						this.Goals.Add(cgoal);
						numColonyGoals++;
					}
				}
			}
		}
        //Added By Gremlin ExpansionPlanner
        private void RunExpansionPlanner()
        {
            int numColonyGoals = 0;
            foreach (Goal g in this.Goals)
            {
                if (g.type != GoalType.Colonize)
                {
                    continue;
                }
                //added by Gremlin: Colony expansion changes
                if (g.GetMarkedPlanet() != null)
                {
                    if (g.GetMarkedPlanet().ParentSystem.ShipList.Where(ship => ship.loyalty != null && ship.loyalty.isFaction).Count() > 0)
                    {
                        numColonyGoals--;

                    }
                    //foreach (Ship enemy in g.GetMarkedPlanet().ParentSystem.ShipList)
                    //{
                    //    if (enemy.loyalty != this.empire)
                    //    {
                    //        numColonyGoals--;
                    //        break;
                    //    }
                    //}
                    numColonyGoals++;
                }
            }
            if (numColonyGoals < this.desired_ColonyGoals + (this.empire.data.EconomicPersonality != null ? this.empire.data.EconomicPersonality.ColonyGoalsPlus : 0))
            {
                Planet toMark = null;
                float DistanceInJumps = 0;
                Vector2 WeightedCenter = new Vector2();
                int numPlanets = 0;
                foreach (Planet p in this.empire.GetPlanets())
                {
                    for (int i = 0; (float)i < p.Population / 1000f; i++)
                    {
                        WeightedCenter = WeightedCenter + p.Position;
                        numPlanets++;
                    }
                }
                WeightedCenter = WeightedCenter / (float)numPlanets;
                List<Goal.PlanetRanker> ranker = new List<Goal.PlanetRanker>();
                List<Goal.PlanetRanker> allPlanetsRanker = new List<Goal.PlanetRanker>();
                foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                {
                    //added by gremlin make non offensive races act like it.
                    bool systemOK = true;
                    if (!this.empire.isFaction && this.empire.data != null && this.empire.data.DiplomaticPersonality != null && !((this.empire.GetRelations().Where(war => war.Value.AtWar).Count() > 0 && this.empire.data.DiplomaticPersonality.Name != "Honorable") || this.empire.data.DiplomaticPersonality.Name == "Agressive" || this.empire.data.DiplomaticPersonality.Name == "Ruthless" || this.empire.data.DiplomaticPersonality.Name == "Cunning"))
                    {
                        foreach (Empire enemy in s.OwnerList)
                        {
                            if (enemy != this.empire && !enemy.isFaction && !this.empire.GetRelations()[enemy].Treaty_Alliance)
                            {

                                systemOK = false;

                                break;
                            }
                        }
                    }
                    if (!systemOK) continue;
                    if (!s.ExploredDict[this.empire])
                    {

                        continue;
                    }
                    foreach (Planet planetList in s.PlanetList)
                    {

                        bool ok = true;
                        foreach (Goal g in this.Goals)
                        {
                            if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != planetList)
                            {
                                continue;
                            }
                            ok = false;
                        }
                        if (!ok)
                        {
                            continue;
                        }
                        IOrderedEnumerable<AO> sorted =
                            from ao in this.empire.GetGSAI().AreasOfOperations
                            orderby Vector2.Distance(planetList.Position, ao.Position)
                            select ao;
                        if (sorted.Count<AO>() > 0)
                        {
                            AO ClosestAO = sorted.First<AO>();
                            if (Vector2.Distance(planetList.Position, ClosestAO.Position) > ClosestAO.Radius * 2.15f)
                            {
                                continue;
                            }
                        }
                        int commodities = 0;
                        //Added by gremlin adding in commodities
                        foreach (Building commodity in planetList.BuildingList)
                        {
                            if (!commodity.IsCommodity) continue;
                            commodities += 1;
                        }


                        if (planetList.ExploredDict[this.empire] && (planetList.habitable || this.empire.data.Traits.Cybernetic != 0) && planetList.Owner == null)
                        {
                            if (this.empire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire) > 0f)
                            {
                                continue;
                            }
                            Goal.PlanetRanker r2 = new Goal.PlanetRanker()
                            {
                                Distance = Vector2.Distance(WeightedCenter, planetList.Position)
                            };
                            DistanceInJumps = r2.Distance / 400000f;
                            if (DistanceInJumps < 1f)
                            {
                                DistanceInJumps = 1f;
                            }
                            r2.planet = planetList;

                            if (this.empire.data.Traits.Cybernetic != 0)
                            {
                                if (planetList.MineralRichness < .5f)
                                {
                                    bool flag = false;
                                    foreach (Planet food in this.empire.GetPlanets())
                                    {
                                        if (food.ProductionHere < food.MAX_STORAGE * .7f || food.ps != Planet.GoodState.EXPORT)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                    if (!flag) continue;
                                }
                                r2.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                            }
                            else
                            {
                                r2.PV = (commodities + planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                            }

                            //if (planetList.Type == "Barren" && (commodities > 0 || this.empire.GetTDict()["Biospheres"].Unlocked || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                            if (planetList.Type == "Barren" && (commodities > 0 || this.empire.GetBDict().ContainsKey("Biospheres") || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                            {
                                ranker.Add(r2);
                            }
                            else if (planetList.Type != "Barren" && (commodities > 0 || (double)planetList.Fertility >= .5f || this.empire.GetTDict()["Aeroponics"].Unlocked || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                            {
                                ranker.Add(r2);
                            }
                            else if (planetList.Type != "Barren")
                            {
                                foreach (Planet food in this.empire.GetPlanets())
                                {
                                    if (food.FoodHere > food.MAX_STORAGE * .7f && food.fs == Planet.GoodState.EXPORT)
                                    {
                                        ranker.Add(r2);
                                        break;
                                    }
                                }
                            }


                        }
                        if (!planetList.ExploredDict[this.empire] || !planetList.habitable || planetList.Owner == this.empire || this.empire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire) > 0f)
                        {
                            continue;
                        }
                        Goal.PlanetRanker r = new Goal.PlanetRanker()
                        {
                            Distance = Vector2.Distance(WeightedCenter, planetList.Position)
                        };
                        DistanceInJumps = r.Distance / 400000f;
                        if (DistanceInJumps < 1f)
                        {
                            DistanceInJumps = 1f;
                        }
                        r.planet = planetList;
                        if (this.empire.data.Traits.Cybernetic != 0)
                        {
                            r.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                        }
                        else
                        {
                            r.PV = (commodities + planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                        }
                        //if (planetList.Type == "Barren" && (commodities > 0 || this.empire.GetTDict()["Biospheres"].Unlocked || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                        //if (!(planetList.Type == "Barren") || !this.empire.GetTDict()["Biospheres"].Unlocked)
                        if (planetList.Type == "Barren" && (commodities > 0 || this.empire.GetBDict().ContainsKey("Biospheres") || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                        {
                            if (!(planetList.Type != "Barren") || (double)planetList.Fertility < .5 && !this.empire.GetTDict()["Aeroponics"].Unlocked && this.empire.data.Traits.Cybernetic == 0)
                            {

                                foreach (Planet food in this.empire.GetPlanets())
                                {
                                    if (food.FoodHere > food.MAX_STORAGE * .9f && food.fs == Planet.GoodState.EXPORT)
                                    {
                                        allPlanetsRanker.Add(r);
                                        break;
                                    }
                                }

                                continue;
                            }

                            allPlanetsRanker.Add(r);

                        }
                        else
                        {
                            allPlanetsRanker.Add(r);
                        }
                    }
                }
                if (ranker.Count > 0)
                {
                    Goal.PlanetRanker winner = new Goal.PlanetRanker();
                    float highest = 0f;
                    foreach (Goal.PlanetRanker pr in ranker)
                    {
                        if (pr.PV <= highest)
                        {
                            continue;
                        }
                        bool ok = true;
                        foreach (Goal g in this.Goals)
                        {
                            if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != pr.planet)
                            {
                                if (!g.Held || g.GetMarkedPlanet() == null || g.GetMarkedPlanet().system != pr.planet.system)
                                {
                                    continue;
                                }
                                ok = false;
                                break;
                            }
                            else
                            {
                                ok = false;
                                break;
                            }
                        }
                        if (!ok)
                        {
                            continue;
                        }
                        winner = pr;
                        highest = pr.PV;
                    }
                    toMark = winner.planet;
                }
                if (allPlanetsRanker.Count > 0)
                {
                    this.DesiredPlanets.Clear();
                    IOrderedEnumerable<Goal.PlanetRanker> sortedList =
                        from ran in allPlanetsRanker
                        orderby ran.PV descending
                        select ran;
                    for (int i = 0; i < allPlanetsRanker.Count; i++)
                    {
                        this.DesiredPlanets.Add(sortedList.ElementAt<Goal.PlanetRanker>(i).planet);
                    }
                }
                if (toMark != null)
                {
                    bool ok = true;
                    foreach (Goal g in this.Goals)
                    {
                        if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != toMark)
                        {
                            continue;
                        }
                        ok = false;
                    }
                    if (ok)
                    {
                        Goal cgoal = new Goal(toMark, this.empire)
                        {
                            GoalName = "MarkForColonization"
                        };
                        this.Goals.Add(cgoal);
                        numColonyGoals++;
                    }
                }
            }
        }

		private void RunForcePoolManager()
		{
		}

		private void RunGroundPlanner()
		{
			float requiredStrength = (float)(this.empire.GetPlanets().Count * 50);
			requiredStrength = requiredStrength + requiredStrength * this.empire.data.Traits.GroundCombatModifier;
			if (Ship.universeScreen.GameDifficulty == UniverseData.GameDifficulty.Hard)
			{
				requiredStrength = requiredStrength * 1.5f;
			}
			if (Ship.universeScreen.GameDifficulty == UniverseData.GameDifficulty.Brutal)
			{
				requiredStrength = requiredStrength * 3f;
			}
			this.numberTroopGoals = this.AreasOfOperations.Count * 2;
			float currentStrength = 0f;
			foreach (Planet p in this.empire.GetPlanets())
			{
				foreach (Troop t in p.TroopsHere)
				{
					if (t.GetOwner() == null || t.GetOwner() != this.empire)
					{
						continue;
					}
					currentStrength = currentStrength + (float)t.Strength;
				}
			}
			for (int i = 0; i < this.empire.GetShips().Count; i++)
			{
				Ship ship = this.empire.GetShips()[i];
				if (ship != null)
				{
					for (int j = 0; j < ship.TroopList.Count; j++)
					{
						Troop t = ship.TroopList[j];
						if (t != null)
						{
							currentStrength = currentStrength + (float)t.Strength;
						}
					}
				}
			}
			int currentgoals = 0;
			for (int i = 0; i < this.Goals.Count; i++)
			{
				Goal g = this.Goals[i];
				if (g != null && g.GoalName == "Build Troop")
				{
					currentgoals++;
				}
			}
			if (currentStrength < requiredStrength && currentgoals < this.numberTroopGoals)
			{
				List<Planet> Potentials = new List<Planet>();
				float totalProduction = 0f;
				foreach (AO area in this.AreasOfOperations)
				{
					if (!area.GetPlanet().AllowInfantry)
					{
						continue;
					}
					Potentials.Add(area.GetPlanet());
					totalProduction = totalProduction + area.GetPlanet().GetNetProductionPerTurn();
				}
				if (Potentials.Count > 0)
				{
					float random = RandomMath.RandomBetween(0f, totalProduction);
					Planet selectedPlanet = null;
					float prodPick = 0f;
					foreach (Planet p in Potentials)
					{
						if (random <= prodPick || random >= prodPick + p.GetNetProductionPerTurn())
						{
							prodPick = prodPick + p.GetNetProductionPerTurn();
						}
						else
						{
							selectedPlanet = p;
						}
					}
					if (selectedPlanet != null)
					{
						List<string> PotentialTroops = new List<string>();
						foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
						{
							if (!this.empire.WeCanBuildTroop(troop.Key))
							{
								continue;
							}
							PotentialTroops.Add(troop.Key);
						}
						if (PotentialTroops.Count > 0)
						{
							int ran = (int)RandomMath.RandomBetween(0f, (float)PotentialTroops.Count + 0.75f);
							if (ran > PotentialTroops.Count - 1)
							{
								ran = PotentialTroops.Count - 1;
							}
							if (ran < 0)
							{
								ran = 0;
							}
							Goal g = new Goal(ResourceManager.TroopsDict[PotentialTroops[ran]], this.empire, selectedPlanet);
							this.Goals.Add(g);
						}
					}
				}
			}
		}

		private void RunInfrastructurePlanner()
		{
			foreach (SolarSystem ownedSystem in this.empire.GetOwnedSystems())
			{
				IOrderedEnumerable<SolarSystem> sortedList = 
					from otherSystem in this.empire.GetOwnedSystems()
					orderby Vector2.Distance(otherSystem.Position, ownedSystem.Position)
					select otherSystem;
				foreach (SolarSystem Origin in sortedList)
				{
					if (Origin == ownedSystem)
					{
						continue;
					}
					bool createRoad = true;
					foreach (SpaceRoad road in this.empire.SpaceRoadsList)
					{
						if (road.GetOrigin() != ownedSystem && road.GetDestination() != ownedSystem)
						{
							continue;
						}
						createRoad = false;
					}
					if (!createRoad)
					{
						continue;
					}
					SpaceRoad newRoad = new SpaceRoad(Origin, ownedSystem, this.empire);
					float UnderConstruction = 0f;
					foreach (Goal g in this.Goals)
					{
						if (g.GoalName == "BuildOffensiveShips")
						{
							UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
						}
						if (g.GoalName != "BuildConstructionShip")
						{
							continue;
						}
						UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
					}
					if ((double)((this.empire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) ? this.empire.EstimateIncomeAtTaxRate(this.empire.data.TaxRate) - UnderConstruction : this.empire.EstimateIncomeAtTaxRate(0.2f) - UnderConstruction)) - 0.24 * (double)newRoad.NumberOfProjectors <= 0.5)
					{
						continue;
					}
					this.empire.SpaceRoadsList.Add(newRoad);
				}
			}
			List<SpaceRoad> ToRemove = new List<SpaceRoad>();
			foreach (SpaceRoad road in this.empire.SpaceRoadsList)
			{
				if (!road.GetOrigin().OwnerList.Contains(this.empire) || !road.GetDestination().OwnerList.Contains(this.empire))
				{
					ToRemove.Add(road);
				}
				else
				{
					foreach (RoadNode node in road.RoadNodesList)
					{
						if (node.Platform != null && (node.Platform == null || node.Platform.Active))
						{
							continue;
						}
						bool AddNew = true;
						foreach (Goal g in this.Goals)
						{
							if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
							{
								continue;
							}
							AddNew = false;
						}
						lock (GlobalStats.BorderNodeLocker)
						{
							foreach (Empire.InfluenceNode bordernode in this.empire.BorderNodes)
							{
								if (Vector2.Distance(node.Position, bordernode.Position) >= bordernode.Radius)
								{
									continue;
								}
								AddNew = false;
							}
						}
						if (!AddNew)
						{
							continue;
						}
						Goal newRoad = new Goal(node.Position, "Subspace Projector", this.empire);
						this.Goals.Add(newRoad);
					}
				}
			}
			if (this.empire != Ship.universeScreen.player)
			{
				foreach (SpaceRoad road in ToRemove)
				{
					this.empire.SpaceRoadsList.Remove(road);
					foreach (RoadNode node in road.RoadNodesList)
					{
						if (node.Platform != null && (node.Platform == null || node.Platform.Active))
						{
							continue;
						}
						foreach (Goal g in this.Goals)
						{
							if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
							{
								continue;
							}
							this.Goals.QueuePendingRemoval(g);
							foreach (Planet p in this.empire.GetPlanets())
							{
								foreach (QueueItem qi in p.ConstructionQueue)
								{
									if (qi.Goal != g)
									{
										continue;
									}
									Planet productionHere = p;
									productionHere.ProductionHere = productionHere.ProductionHere + qi.productionTowards;
									if (p.ProductionHere > p.MAX_STORAGE)
									{
										p.ProductionHere = p.MAX_STORAGE;
									}
									p.ConstructionQueue.QueuePendingRemoval(qi);
								}
								p.ConstructionQueue.ApplyPendingRemovals();
							}
						}
						this.Goals.ApplyPendingRemovals();
					}
				}
			}
		}

		private void RunManagers()
		{
			if (this.empire.data.IsRebelFaction || this.empire.data.Defeated)
			{
				return;
			}
			this.ManageAOs();
			foreach (AO ao in this.AreasOfOperations)
			{
				ao.Update();
			}
			this.UpdateThreatMatrix();
			if (this.empire != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) || this.empire.AutoColonize)
			{
				this.RunExpansionPlanner();
			}
			if (this.empire != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty) || this.empire.AutoBuild)
			{
				this.RunInfrastructurePlanner();
			}
			this.DefensiveCoordinator.ManageForcePool();
			if (this.empire != EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty))
			{
				this.RunEconomicPlanner();
				this.RunDiplomaticPlanner();
				this.RunMilitaryPlanner();
				this.RunResearchPlanner();
				this.RunForcePoolManager();
				this.RunAgentManager();
				this.RunWarPlanner();
			}
		}

		private void RunMilitaryPlannerORIG()
		{
			List<AO>.Enumerator enumerator;
			this.RunGroundPlanner();
			this.numberOfShipGoals = 0;
			foreach (Planet p in this.empire.GetPlanets())
			{
				if (!p.HasShipyard || p.GetNetProductionPerTurn() < 2f)
				{
					continue;
				}
				GSAI gSAI = this;
				gSAI.numberOfShipGoals = gSAI.numberOfShipGoals + 3;
			}
			float numgoals = 0f;
			float UnderConstruction = 0f;
			float TroopStrengthUnderConstruction = 0f;
			foreach (Goal g in this.Goals)
			{
				if (g.GoalName == "BuildOffensiveShips")
				{
					UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
					foreach (Troop t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)
					{
						TroopStrengthUnderConstruction = TroopStrengthUnderConstruction + (float)t.Strength;
					}
					numgoals = numgoals + 1f;
				}
				if (g.GoalName != "BuildConstructionShip")
				{
					continue;
				}
				UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
			}
			float Capacity = this.empire.EstimateIncomeAtTaxRate(0.45f) - UnderConstruction;
			float allowable_deficit = -0.0035f * this.empire.Money;
			if (this.empire.Money < 500f)
			{
				allowable_deficit = 0f;
			}
			if (Capacity <= 0f)
			{
				float HowMuchWeAreScrapping = 0f;
				foreach (Ship ship1 in this.empire.GetShips())
				{
					if (ship1.GetAI().State != AIState.Scrap)
					{
						continue;
					}
					HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCost();
				}
				if (HowMuchWeAreScrapping < Math.Abs(Capacity))
				{
					float Added = 0f;
					IOrderedEnumerable<Ship> sortedList = 
						from ship in this.empire.GetShips()
						orderby ship.GetTechScore()
						select ship;
					using (IEnumerator<Ship> enumerator1 = sortedList.GetEnumerator())
					{
						do
						{
						Label0:
							if (!enumerator1.MoveNext())
							{
								break;
							}
							Ship current = enumerator1.Current;
							if (current.Mothership == null && !(current.Role == "freighter") && !(current.Role == "construction") && !(current.Role == "platform") && !(current.Role == "station") && current.fleet == null && !current.InCombat && !(current.Role == "troop") && current.GetAI().State != AIState.Explore)
							{
								current.GetAI().OrderScrapShip();
								Added = Added + current.GetMaintCost();
							}
							else
							{
								goto Label0;    //this will keep looping without evaluating the while condition
                                //enumerator position is still advanced, so this will terminate via the first if eventually
							}
						}
						while (Added + HowMuchWeAreScrapping < Math.Abs(Capacity));
					}
				}
			}
			Capacity = this.empire.EstimateIncomeAtTaxRate(0.45f) - UnderConstruction;
			int shipcount = 0;
			foreach (Ship ship2 in this.empire.GetShips())
			{
				if (!(ship2.Role != "platform") || !(ship2.Role != "freighter") || !(ship2.Role != "station"))
				{
					continue;
				}
				shipcount++;
			}
			while (Capacity > allowable_deficit && numgoals < (float)this.numberOfShipGoals && shipcount < 150)
			{
				string s = this.GetAShip(Capacity);
				if (s == null)
				{
					break;
				}
				Goal g = new Goal(s, "BuildOffensiveShips", this.empire)
				{
					type = GoalType.BuildShips
				};
				this.Goals.Add(g);
				Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost();
				numgoals = numgoals + 1f;
			}
			int numWars = 0;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
			{
				if (!Relationship.Value.AtWar || Relationship.Key.isFaction)
				{
					continue;
				}
				numWars++;
			}
			foreach (Goal g in this.Goals)
			{
				if (g.type != GoalType.Colonize || g.Held)
				{
					if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)
					{
						continue;
					}
					foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
					{
						this.empire.GetGSAI().CheckClaim(Relationship, g.GetMarkedPlanet());
					}
					this.Goals.QueuePendingRemoval(g);
					lock (GlobalStats.TaskLocker)
					{
						foreach (MilitaryTask task in this.TaskList)
						{
							foreach (Guid held in task.HeldGoals)
							{
								if (held != g.guid)
								{
									continue;
								}
								this.TaskList.QueuePendingRemoval(task);
								break;
							}
						}
					}
				}
				else
				{
					foreach (KeyValuePair<Guid, Ship_Game.Gameplay.ThreatMatrix.Pin> pin in this.ThreatMatrix.Pins)
					{
						if (Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire || pin.Value.Strength <= 0f || !this.empire.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction)
						{
							continue;
						}
						List<Goal> tohold = new List<Goal>()
						{
							g
						};
						MilitaryTask task = new MilitaryTask(g.GetMarkedPlanet().Position, 125000f, tohold, this.empire);
						lock (GlobalStats.TaskLocker)
						{
							this.TaskList.Add(task);
							break;
						}
					}
				}
			}
			if (this.empire.data.DiplomaticPersonality.Name == "Aggressive" || this.empire.data.DiplomaticPersonality.Name == "Ruthless" || this.empire.data.EconomicPersonality.Name == "Expansionist")
			{
				foreach (Goal g in this.Goals)
				{
					if (g.type != GoalType.Colonize || g.Held)
					{
						continue;
					}
					bool OK = true;
					lock (GlobalStats.TaskLocker)
					{
						foreach (MilitaryTask mt in this.TaskList)
						{
							if (mt.type != MilitaryTask.TaskType.DefendClaim && mt.type != MilitaryTask.TaskType.ClearAreaOfEnemies || !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))
							{
								continue;
							}
							OK = false;
							break;
						}
					}
					if (!OK)
					{
						continue;
					}
					MilitaryTask task = new MilitaryTask()
					{
						AO = g.GetMarkedPlanet().Position
					};
					task.SetEmpire(this.empire);
					task.AORadius = 75000f;
					task.SetTargetPlanet(g.GetMarkedPlanet());
					task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
					task.type = MilitaryTask.TaskType.DefendClaim;
					lock (GlobalStats.TaskLocker)
					{
						this.TaskList.Add(task);
					}
				}
			}
			this.Goals.ApplyPendingRemovals();
			lock (GlobalStats.TaskLocker)
			{
				List<MilitaryTask> ToughNuts = new List<MilitaryTask>();
				List<MilitaryTask> InOurSystems = new List<MilitaryTask>();
				List<MilitaryTask> InOurAOs = new List<MilitaryTask>();
				List<MilitaryTask> Remainder = new List<MilitaryTask>();
				foreach (MilitaryTask task in this.TaskList)
				{
					if (task.type != MilitaryTask.TaskType.AssaultPlanet)
					{
						continue;
					}
					if (task.IsToughNut)
					{
						ToughNuts.Add(task);
					}
					else if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
					{
						bool dobreak = false;
						foreach (KeyValuePair<Guid, Planet> entry in Ship.universeScreen.PlanetsDict)
						{
							if (task.GetTargetPlanet() != entry.Value)
							{
								continue;
							}
							enumerator = this.AreasOfOperations.GetEnumerator();
							try
							{
								while (enumerator.MoveNext())
								{
									AO area = enumerator.Current;
									if (Vector2.Distance(entry.Value.Position, area.Position) >= area.Radius)
									{
										continue;
									}
									InOurAOs.Add(task);
									dobreak = true;
									break;
								}
								break;
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
						}
						if (dobreak)
						{
							continue;
						}
						Remainder.Add(task);
					}
					else
					{
						InOurSystems.Add(task);
					}
				}
				List<MilitaryTask> TNInOurSystems = new List<MilitaryTask>();
				List<MilitaryTask> TNInOurAOs = new List<MilitaryTask>();
				List<MilitaryTask> TNRemainder = new List<MilitaryTask>();
				foreach (MilitaryTask task in ToughNuts)
				{
					if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
					{
						bool dobreak = false;
						foreach (KeyValuePair<Guid, Planet> entry in Ship.universeScreen.PlanetsDict)
						{
							if (task.GetTargetPlanet() != entry.Value)
							{
								continue;
							}
							enumerator = this.AreasOfOperations.GetEnumerator();
							try
							{
								while (enumerator.MoveNext())
								{
									AO area = enumerator.Current;
									if (Vector2.Distance(entry.Value.Position, area.Position) >= area.Radius)
									{
										continue;
									}
									TNInOurAOs.Add(task);
									dobreak = true;
									break;
								}
								break;
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
						}
						if (dobreak)
						{
							continue;
						}
						TNRemainder.Add(task);
					}
					else
					{
						TNInOurSystems.Add(task);
					}
				}
				foreach (MilitaryTask task in TNInOurAOs)
				{
					if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations()[task.GetTargetPlanet().Owner].ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
					{
						continue;
					}
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in TNInOurSystems)
				{
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in TNRemainder)
				{
					if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations()[task.GetTargetPlanet().Owner].ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
					{
						continue;
					}
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in InOurAOs)
				{
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in InOurSystems)
				{
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in Remainder)
				{
					task.Evaluate(this.empire);
				}
				foreach (MilitaryTask task in this.TaskList)
				{
					if (task.type != MilitaryTask.TaskType.AssaultPlanet)
					{
						task.Evaluate(this.empire);
					}
					if (task.type != MilitaryTask.TaskType.AssaultPlanet && task.type != MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null && task.GetTargetPlanet().Owner != this.empire)
					{
						continue;
					}
					task.EndTask();
				}
				this.TaskList.AddRange(this.TasksToAdd);
				this.TasksToAdd.Clear();
				this.TaskList.ApplyPendingRemovals();
			}
		}
        //added by gremlin deveksmod military planner
        private void RunMilitaryPlanner()
        {
            List<AO>.Enumerator enumerator;
            this.RunGroundPlanner();
            this.numberOfShipGoals = 0;
            foreach (Planet p in this.empire.GetPlanets())
            {
                if (!p.HasShipyard || p.GetNetProductionPerTurn() < 2f)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.numberOfShipGoals = gSAI.numberOfShipGoals + 3;
            }
            float numgoals = 0f;
            float offenseUnderConstruction = 0f;
            float UnderConstruction = 0f;
            float TroopStrengthUnderConstruction = 0f;
            foreach (Goal g in this.Goals)
            //Parallel.ForEach(this.Goals, g =>
            {
                if (g.GoalName == "BuildOffensiveShips")
                {
                    UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
                    offenseUnderConstruction += ResourceManager.ShipsDict[g.ToBuildUID].BaseStrength;
                    foreach (Troop t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)
                    {
                        TroopStrengthUnderConstruction = TroopStrengthUnderConstruction + (float)t.Strength;
                    }
                    numgoals = numgoals + 1f;
                }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }
                UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
            }

            float offensiveStrength = offenseUnderConstruction + this.empire.GetForcePoolStrength();

            bool atWar = this.empire.GetRelations().Where(war => war.Value.AtWar).Count() > 0;
            int prepareWar = this.empire.GetRelations().Where(angry => angry.Value.TotalAnger > angry.Value.Trust).Count();
            prepareWar += this.empire.GetRelations().Where(angry => angry.Value.Threat > 0).Count();
            float noIncome = this.FindTaxRateToReturnAmount(UnderConstruction);
            //float minStrength = this.TaskList.Where(noFleet => noFleet.WhichFleet == -1).Min(str => str.MinimumTaskForceStrength);
            //float costForFleet = ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();



            //float tasks = .45f- this.TaskList.Where(noFleet=> noFleet.WhichFleet ==-1). Count() * .06f;
            ////float minTaskForce = 0f;
            ////if(tasks <.45f) minTaskForce=   (float)TaskList.Where(noFleet => noFleet.WhichFleet == -1 ).Min(fleetneeded => fleetneeded.MinimumTaskForceStrength) ;
            //tasks -= prepareWar*10;
            //tasks = tasks < 0f ? 0f : tasks;

            float tax = atWar ? .25f + prepareWar * .05f : .10f + (prepareWar * .1f);  //.45f - (tasks);
            //if(tax <.45f) tax = minTaskForce> offensiveStrength ? .45f :tax;




            float Capacity = this.empire.EstimateIncomeAtTaxRate(tax) - UnderConstruction;
            float allowable_deficit = -Capacity;//-this.empire.ShipsWeCanBuild.AsParallel().Max(main => ResourceManager.ShipsDict[main].GetMaintCost()); 
            if (allowable_deficit >= 0f || noIncome > .35f)
            {
                allowable_deficit = -this.empire.Money;// 0f;
            }
            if (Capacity <= allowable_deficit) //(Capacity <= 0f)
            {
                float HowMuchWeAreScrapping = 0f;
                foreach (Ship ship1 in this.empire.GetShips())
                {
                    if (ship1.GetAI().State != AIState.Scrap)
                    {
                        continue;
                    }
                    HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCost();
                }
                if (HowMuchWeAreScrapping < Math.Abs(Capacity))
                {
                    float Added = 0f;
                    
                    //added by gremlin clear out building ships before active ships.
                    foreach (Goal g in this.Goals.Where(goal => goal.GoalName == "BuildOffensiveShips").OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID].GetMaintCost()))
                    {
                        bool flag = false;
                        if (g.GetPlanetWhereBuilding() == null)
                            continue;
                        foreach(QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                        {
                           
                            if (shipToRemove.Goal != g)
                            {
                                continue;
                                
                            }
                            g.GetPlanetWhereBuilding().ProductionHere += shipToRemove.productionTowards;
                            g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                            this.Goals.QueuePendingRemoval(g);
                            Added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost();
                            flag = true;
                            break;
              
                        }
                        if (flag)
                            g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();

                    }
                    this.Goals.ApplyPendingRemovals();
                    
                   
                    
                    

                    IOrderedEnumerable<Ship> sortedList =
                        from ship in this.empire.GetShips()
                        orderby ship.GetTechScore()
                        select ship;
                    using (IEnumerator<Ship> enumerator1 = sortedList.GetEnumerator())
                    {
                        do
                        {
                        Label0:
                            if (!enumerator1.MoveNext())
                            {
                                break;
                            }
                            Ship current = enumerator1.Current;
                            bool scrapFleet = current.fleet == null;
                            if (this.empire.Money < -this.empire.GrossTaxes)
                            {
                                scrapFleet = true;
                            }
                            if (current.Mothership == null && !(current.Role == "freighter") && !(current.Role == "construction") && !(current.Role == "platform") && !(current.Role == "station") && scrapFleet && !current.InCombat && !(current.Role == "troop") && current.GetAI().State != AIState.Explore)
                            {
                                if (current.fleet == null || (current.fleet != null && current.fleet.Task == null)) //&&current.fleet.TaskStep <1))
                                {
                                    float maintcost = current.GetMaintCost();
                                    if (maintcost > 0)
                                    {
                                        current.GetAI().OrderScrapShip();
                                        Added = Added + maintcost;
                                    }
                                }
                            }
                            else
                            {
                                goto Label0;
                            }
                        }
                        while (Added + HowMuchWeAreScrapping < Math.Abs(Capacity) + allowable_deficit);
                    }
                }

            }
            if (allowable_deficit > 0f || noIncome > tax)
            {
                allowable_deficit = Math.Abs(allowable_deficit);
            }

            Capacity = this.empire.EstimateIncomeAtTaxRate(tax) - UnderConstruction;
            int shipcount = 0;
            int shipsize = 0;
            //foreach (Ship ship2 in this.empire.GetShips())
            ////Parallel.ForEach(this.empire.GetShips(), ship2 =>
            //{
            //    if (!(ship2.Role != "platform") || !(ship2.Role != "freighter") || !(ship2.Role != "station"))
            //    {
            //        continue;
            //    }
            //    shipcount++;
            //    shipsize += ship2.Size;
            //}
            int Memory = (int)GC.GetTotalMemory(false);

            Memory = Memory / 1000;
            //added by gremlin shipsize limit
            //i think this could be made dynamic to reduce when memory constraints come into play
            while (Capacity > allowable_deficit && numgoals < (float)this.numberOfShipGoals && Memory < SizeLimiter) //shipsize < SizeLimiter)
            {
                /*string s = null;
                if (Properties.Settings.Default.OptionTestBits || Properties.Settings.Default.ModSupport)
                {
                    s = this.GetAShip(Capacity +UnderConstruction + this.empire.GetTotalShipMaintenance());
                }
                else
                {
                    s = this.GetAShip();
                }*/
                string s = this.GetAShip(Capacity);
                if (s == null)
                {
                    break;
                }
                Goal g = new Goal(s, "BuildOffensiveShips", this.empire)
                {
                    type = GoalType.BuildShips
                };
                this.Goals.Add(g);
                Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost();
                numgoals = numgoals + 1f;
            }
            int numWars = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
            {
                if (!Relationship.Value.AtWar || Relationship.Key.isFaction)
                {
                    continue;
                }
                numWars++;
            }
            foreach (Goal g in this.Goals)
            //Parallel.ForEach(this.Goals, g =>
            {
                if (g.type != GoalType.Colonize || g.Held)
                {
                    if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)
                    {
                        continue;
                    }
                    foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.GetRelations())
                    {
                        this.empire.GetGSAI().CheckClaim(Relationship, g.GetMarkedPlanet());
                    }
                    this.Goals.QueuePendingRemoval(g);
                    lock (GlobalStats.TaskLocker)
                    {
                        foreach (MilitaryTask task in this.TaskList)
                        {
                            foreach (Guid held in task.HeldGoals)
                            {
                                if (held != g.guid)
                                {
                                    continue;
                                }
                                this.TaskList.QueuePendingRemoval(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (g.GetMarkedPlanet() != null)
                    {
                        foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.ThreatMatrix.Pins.Where(pin => !((Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f) || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire || pin.Value.Strength <= 0f || !this.empire.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar)))
                        {
                            if (Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire || pin.Value.Strength <= 0f || !this.empire.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction)
                            {
                                continue;
                            }
                            List<Goal> tohold = new List<Goal>()
                        {
                            g
                        };
                            MilitaryTask task = new MilitaryTask(g.GetMarkedPlanet().Position, 125000f, tohold, this.empire);
                            lock (GlobalStats.TaskLocker)
                            {
                                this.TaskList.Add(task);
                                break;
                            }
                        }
                    }
                }
            }
            if (this.empire.data.DiplomaticPersonality.Name == "Aggressive" || this.empire.data.DiplomaticPersonality.Name == "Ruthless" || this.empire.data.EconomicPersonality.Name == "Expansionist")
            {
                foreach (Goal g in this.Goals)
                {
                    if (g.type != GoalType.Colonize || g.Held)
                    {
                        continue;
                    }
                    bool OK = true;
                    lock (GlobalStats.TaskLocker)
                    {
                        foreach (MilitaryTask mt in this.TaskList)
                        //Parallel.ForEach(this.TaskList, (mt,state) =>
                        {
                            if ((mt.type != MilitaryTask.TaskType.DefendClaim 
                                && mt.type != MilitaryTask.TaskType.ClearAreaOfEnemies )
                                || g.GetMarkedPlanet() != null 
                                && !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))
                                
                            {
                                continue;
                            }
                            OK = false;
                            break;
                        }
                    }
                    if (!OK)
                    {
                        continue;
                    }
                    if (g.GetMarkedPlanet() == null)
                        continue;
                    MilitaryTask task = new MilitaryTask()
                    {
                        AO = g.GetMarkedPlanet().Position
                    };
                    task.SetEmpire(this.empire);
                    task.AORadius = 75000f;
                    task.SetTargetPlanet(g.GetMarkedPlanet());
                    task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
                    task.type = MilitaryTask.TaskType.DefendClaim;
                    lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(task);
                    }
                }
            }
            this.Goals.ApplyPendingRemovals();
            lock (GlobalStats.TaskLocker)
            {
                List<MilitaryTask> ToughNuts = new List<MilitaryTask>();
                List<MilitaryTask> InOurSystems = new List<MilitaryTask>();
                List<MilitaryTask> InOurAOs = new List<MilitaryTask>();
                List<MilitaryTask> Remainder = new List<MilitaryTask>();
                foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        continue;
                    }
                    if (task.IsToughNut)
                    {
                        ToughNuts.Add(task);
                    }
                    else if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Ship.universeScreen.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)
                            {
                                continue;
                            }
                            enumerator = this.AreasOfOperations.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    AO area = enumerator.Current;
                                    if (Vector2.Distance(entry.Value.Position, area.Position) >= area.Radius)
                                    {
                                        continue;
                                    }
                                    InOurAOs.Add(task);
                                    dobreak = true;
                                    break;
                                }
                                break;
                            }
                            finally
                            {
                                ((IDisposable)enumerator).Dispose();
                            }
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        Remainder.Add(task);
                    }
                    else
                    {
                        InOurSystems.Add(task);
                    }
                }
                List<MilitaryTask> TNInOurSystems = new List<MilitaryTask>();
                List<MilitaryTask> TNInOurAOs = new List<MilitaryTask>();
                List<MilitaryTask> TNRemainder = new List<MilitaryTask>();
                foreach (MilitaryTask task in ToughNuts)
                {
                    if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Ship.universeScreen.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)
                            {
                                continue;
                            }
                            enumerator = this.AreasOfOperations.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    AO area = enumerator.Current;
                                    if (Vector2.Distance(entry.Value.Position, area.Position) >= area.Radius)
                                    {
                                        continue;
                                    }
                                    TNInOurAOs.Add(task);
                                    dobreak = true;
                                    break;
                                }
                                break;
                            }
                            finally
                            {
                                ((IDisposable)enumerator).Dispose();
                            }
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        TNRemainder.Add(task);
                    }
                    else
                    {
                        TNInOurSystems.Add(task);
                    }
                }
                foreach (MilitaryTask task in TNInOurAOs)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations()[task.GetTargetPlanet().Owner].ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                    }
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in TNInOurSystems)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in TNRemainder)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations()[task.GetTargetPlanet().Owner].ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                    }
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurAOs)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurSystems)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in Remainder)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        task.Evaluate(this.empire);
                    }
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet && task.type != MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null && task.GetTargetPlanet().Owner != this.empire)
                    {
                        continue;
                    }
                    task.EndTask();
                }
                this.TaskList.AddRange(this.TasksToAdd);
                this.TasksToAdd.Clear();
                this.TaskList.ApplyPendingRemovals();
            }
        }

		private void RunResearchPlanner()
		{
			if (this.empire.ResearchTopic == "")
			{
				bool InAWar = false;
				foreach (KeyValuePair<Empire, Relationship> relationship in this.empire.GetRelations())
				{
					if (relationship.Key.isFaction || !relationship.Value.AtWar)
					{
						continue;
					}
					InAWar = true;
					break;
				}
				switch (this.res_strat)
				{
					case GSAI.ResearchStrategy.Random:
					{
						List<string> AvailableTechs = new List<string>();
						foreach (KeyValuePair<string, Ship_Game.Technology> Technology in ResourceManager.TechTree)
						{
							if (!this.empire.HavePreReq(Technology.Key) || ResourceManager.TechTree[Technology.Key].Secret && !this.empire.GetTDict()[Technology.Key].Discovered)
							{
								continue;
							}
							AvailableTechs.Add(Technology.Key);
						}
						if (AvailableTechs.Count <= 0)
						{
							break;
						}
						int Random = (int)RandomMath.RandomBetween(0f, (float)AvailableTechs.Count + 0.99f);
						if (Random > AvailableTechs.Count - 1)
						{
							Random = AvailableTechs.Count - 1;
						}
						this.empire.ResearchTopic = AvailableTechs[Random];
						break;
					}
					case GSAI.ResearchStrategy.Scripted:
					{
						if (this.empire.getResStrat() != null)
						{
							if (InAWar && this.empire.GetTDict().ContainsKey("MissileTheory") && !this.empire.GetTDict()["MissileTheory"].Unlocked)
							{
								this.empire.ResearchTopic = "MissileTheory";
								return;
							}
							foreach (EconomicResearchStrategy.Tech tech in this.empire.getResStrat().TechPath)
							{
								if (this.empire.GetTDict()[tech.id].Unlocked)
								{
									continue;
								}
								this.empire.ResearchTopic = tech.id;
								break;
							}
						}
						if (this.empire.ResearchTopic == "ArmorTheory" && this.empire.GetTDict()[this.empire.ResearchTopic].Unlocked && !this.empire.GetTDict()["Point Defense"].Unlocked)
						{
							this.empire.ResearchTopic = "Point Defense";
						}
						if (this.empire.getResStrat() == null || this.empire.ResearchTopic == "")
						{
							this.res_strat = GSAI.ResearchStrategy.Random;
							return;
						}
						if (this.empire.ResearchTopic != "" && !this.empire.GetTDict().ContainsKey(this.empire.ResearchTopic))
						{
							this.res_strat = GSAI.ResearchStrategy.Random;
							return;
						}
						if (!(this.empire.ResearchTopic != "") || this.empire.HavePreReq(this.empire.ResearchTopic))
						{
							break;
						}
						this.res_strat = GSAI.ResearchStrategy.Random;
						return;
					}
					default:
					{
						return;
					}
				}
			}
		}

        private void RunWarPlanner()
        {
            foreach (KeyValuePair<Empire, Relationship> r in this.empire.GetRelations())
            {
                if (r.Key.isFaction)
                {
                    r.Value.AtWar = false;
                }
                else
                {
                    if (r.Value.PreparingForWar)
                    {
                        switch (r.Value.PreparingForWarType)
                        {
                            case WarType.BorderConflict:
                                List<Planet> list1 = new List<Planet>();
                                IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet)));
                                for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1); ++index)
                                {
                                    list1.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable1, index));
                                    if (index == 2)
                                        break;
                                }
                                using (List<Planet>.Enumerator enumerator = list1.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        Planet current = enumerator.Current;
                                        bool flag = true;
                                        lock (GlobalStats.TaskLocker)
                                        {
                                            foreach (MilitaryTask item_0 in (List<MilitaryTask>)this.TaskList)
                                            {
                                                if (item_0.GetTargetPlanet() == current && item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    flag = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if (flag)
                                        {
                                            MilitaryTask militaryTask = new MilitaryTask(current, this.empire);
                                            lock (GlobalStats.TaskLocker)
                                                this.TaskList.Add(militaryTask);
                                        }
                                    }
                                    break;
                                }
                            case WarType.ImperialistWar:
                                List<Planet> list2 = new List<Planet>();
                                IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet)));
                                for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable2); ++index)
                                {
                                    list2.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable2, index));
                                    if (index == 2)
                                        break;
                                }
                                using (List<Planet>.Enumerator enumerator = list2.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        Planet current = enumerator.Current;
                                        bool flag = true;
                                        lock (GlobalStats.TaskLocker)
                                        {
                                            foreach (MilitaryTask item_1 in (List<MilitaryTask>)this.TaskList)
                                            {
                                                if (item_1.GetTargetPlanet() == current && item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    flag = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if (flag)
                                        {
                                            MilitaryTask militaryTask = new MilitaryTask(current, this.empire);
                                            lock (GlobalStats.TaskLocker)
                                                this.TaskList.Add(militaryTask);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                    if (r.Value.AtWar)
                    {
                        int num = (int)this.empire.data.difficulty;
                        this.FightDefaultWar(r);
                    }
                }
            }
        }

		public void SetAlliance(bool ally)
		{
			if (ally)
			{
				this.empire.GetRelations()[EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty)].Treaty_Alliance = true;
				this.empire.GetRelations()[EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty)].Treaty_OpenBorders = true;
				EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Treaty_Alliance = true;
				EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Treaty_OpenBorders = true;
				return;
			}
			this.empire.GetRelations()[EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty)].Treaty_Alliance = false;
			this.empire.GetRelations()[EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty)].Treaty_OpenBorders = false;
			EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Treaty_Alliance = false;
			EmpireManager.GetEmpireByName(this.empire.GetUS().PlayerLoyalty).GetRelations()[this.empire].Treaty_OpenBorders = false;
		}

		public void SetAlliance(bool ally, Empire them)
		{
			if (ally)
			{
				this.empire.GetRelations()[them].Treaty_Alliance = true;
				this.empire.GetRelations()[them].Treaty_OpenBorders = true;
				them.GetRelations()[this.empire].Treaty_Alliance = true;
				them.GetRelations()[this.empire].Treaty_OpenBorders = true;
				return;
			}
			this.empire.GetRelations()[them].Treaty_Alliance = false;
			this.empire.GetRelations()[them].Treaty_OpenBorders = false;
			them.GetRelations()[this.empire].Treaty_Alliance = false;
			them.GetRelations()[this.empire].Treaty_OpenBorders = false;
		}

        public void TriggerRefit()
        {
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            foreach (KeyValuePair<string, bool> keyValuePair in this.empire.GetMDict())
            {
                if (keyValuePair.Value)
                {
                    ShipModule shipModule = ResourceManager.ShipModulesDict[keyValuePair.Key];
                    switch (shipModule.ModuleType)
                    {
                        case ShipModuleType.Turret:
                            if ((int)shipModule.TechLevel > num3)
                            {
                                num3 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.MainGun:
                            if ((int)shipModule.TechLevel > num3)
                            {
                                num3 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.PowerPlant:
                            if ((int)shipModule.TechLevel > num4)
                            {
                                num4 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.Engine:
                            if ((int)shipModule.TechLevel > num2)
                            {
                                num2 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.Shield:
                            if ((int)shipModule.TechLevel > num1)
                            {
                                num1 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.MissileLauncher:
                            if ((int)shipModule.TechLevel > num3)
                            {
                                num3 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        case ShipModuleType.Bomb:
                            if ((int)shipModule.TechLevel > num3)
                            {
                                num3 = (int)shipModule.TechLevel;
                                continue;
                            }
                            else
                                continue;
                        default:
                            continue;
                    }
                }
            }
            int num5 = 0;
            foreach (Ship ship in (List<Ship>)this.empire.GetForcePool())
            {
                if (num5 < 5)
                {
                    int techScore = ship.GetTechScore();
                    List<string> list = new List<string>();
                    foreach (string index in this.empire.ShipsWeCanBuild)
                    {
                        if (ResourceManager.ShipsDict[index].GetShipData().Hull == ship.GetShipData().Hull && ResourceManager.ShipsDict[index].GetTechScore() > techScore)
                            list.Add(index);
                    }
                    if (list.Count > 0)
                    {
                        IOrderedEnumerable<string> orderedEnumerable = Enumerable.OrderByDescending<string, int>((IEnumerable<string>)list, (Func<string, int>)(uid => ResourceManager.ShipsDict[uid].GetTechScore()));
                        ship.GetAI().OrderRefitTo(Enumerable.First<string>((IEnumerable<string>)orderedEnumerable));
                        this.empire.GetForcePool().QueuePendingRemoval(ship);
                        ++num5;
                    }
                }
                else
                    break;
            }
            this.empire.GetForcePool().ApplyPendingRemovals();
        }

		public void Update()
		{
			if (!this.empire.isFaction)
			{
				this.RunManagers();
			}
			foreach (Goal g in this.Goals)
			{
				g.Evaluate();
			}
			this.Goals.ApplyPendingRemovals();
		}

        private void UpdateThreatMatrix()
        {
            List<KeyValuePair<Guid, ThreatMatrix.Pin>> list = new List<KeyValuePair<Guid, ThreatMatrix.Pin>>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> keyValuePair in this.ThreatMatrix.Pins)
            {
                bool flag1 = true;
                bool flag2 = false;
                lock (GlobalStats.SensorNodeLocker)
                {
                    foreach (Empire.InfluenceNode item_0 in (List<Empire.InfluenceNode>)this.empire.SensorNodes)
                    {
                        if ((double)Vector2.Distance(item_0.Position, keyValuePair.Value.Position) <= (double)item_0.Radius)
                            flag2 = true;
                    }
                }
                if (flag2)
                {
                    for (int index = 0; index < this.empire.GetUS().MasterShipList.Count; ++index)
                    {
                        Ship ship = this.empire.GetUS().MasterShipList[index];
                        if (keyValuePair.Key == ship.guid)
                        {
                            flag1 = !ship.Active;
                            break;
                        }
                    }
                }
                else
                    flag1 = false;
                if (flag1)
                    list.Add(keyValuePair);
            }
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> keyValuePair in list)
                this.ThreatMatrix.Pins.Remove(keyValuePair.Key);
            list.Clear();
        }

		public struct PeaceAnswer
		{
			public string answer;

			public bool peace;
		}

		private enum ResearchStrategy
		{
			Random,
			Scripted
		}
	}
}