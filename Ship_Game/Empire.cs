// Type: Ship_Game.Empire
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Xml.Serialization;

namespace Ship_Game
{
    public class Empire
    {
        public static float ProjectorRadius = 150000f;
        private Dictionary<int, Fleet> FleetsDict = new Dictionary<int, Fleet>();
        private Dictionary<string, bool> UnlockedHullsDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, bool> UnlockedTroopDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, bool> UnlockedBuildingsDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, bool> UnlockedModulesDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, TechEntry> TechnologyDict = new Dictionary<string, TechEntry>(StringComparer.InvariantCultureIgnoreCase);
        public List<Ship> Inhibitors = new List<Ship>();
        public List<SpaceRoad> SpaceRoadsList = new List<SpaceRoad>();
        public float Money = 1000f;
        private List<Planet> OwnedPlanets = new List<Planet>();
        private BatchRemovalCollection<Ship> OwnedShips = new BatchRemovalCollection<Ship>();
        public List<Ship> ShipsToAdd = new List<Ship>();
        public List<Ship> KnownShips = new List<Ship>();
        public BatchRemovalCollection<Empire.InfluenceNode> BorderNodes = new BatchRemovalCollection<Empire.InfluenceNode>();
        public BatchRemovalCollection<Empire.InfluenceNode> SensorNodes = new BatchRemovalCollection<Empire.InfluenceNode>();
        private Dictionary<SolarSystem, bool> HostilesPresent = new Dictionary<SolarSystem, bool>();
        private Dictionary<Empire, Relationship> Relationships = new Dictionary<Empire, Relationship>();
        public HashSet<string> ShipsWeCanBuild = new HashSet<string>();
        private float FleetUpdateTimer = 5f;
        public HashSet<string> structuresWeCanBuild = new HashSet<string>();
        private int numberForAverage = 1;
        public int ColonizationGoalCount = 2;
        public string ResearchTopic = "";
        private List<War> Wars = new List<War>();
        private Fleet DefensiveFleet = new Fleet();
        private BatchRemovalCollection<Ship> ForcePool = new BatchRemovalCollection<Ship>();
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;
        public bool isFaction;
        public bool MinorRace;
        public float Research;
        public Color EmpireColor;
        public static UniverseScreen universeScreen;
        public Vector4 VColor;
        private GSAI GSAI;
        //private MilitaryResearchStrategy militaryResearchStrategy;
        private EconomicResearchStrategy economicResearchStrategy;
        private float UpdateTimer;
        public bool isPlayer;
        private float totalShipMaintenance;
        private float totalBuildingMaintenance;
        public float updateContactsTimer;
        private bool InitialziedHostilesDict;
        public float AllTimeMaintTotal;
        public float totalMaint;
        public float GrossTaxes;
        public float OtherIncome;
        public float DisplayIncome;
        public float ActualNetLastTurn;
        public float TradeMoneyAddedThisTurn;
        public float MoneyLastTurn;
        public int totalTradeIncome;
        public bool AutoBuild;
        public bool AutoExplore;
        public bool AutoColonize;
        public bool AutoFreighters;
        public bool AutoResearch;
        public bool AutoTaxes;
        public int TotalScore;
        public float TechScore;
        public float ExpansionScore;
        public float MilitaryScore;
        public float IndustrialScore;
        public float SensorRange;
        public bool IsSensor;
        //private float desiredForceStrength;
        public Planet Capital;
        public int EmpireShipCountReserve;
        public int empireShipTotal;
        public bool canBuildCapitals;
        public bool canBuildCruisers;
        public bool canBuildFrigates;
        public bool canBuildCorvettes;
        public float currentMilitaryStrength;
        [XmlIgnore]
        public ReaderWriterLockSlim SensorNodeLocker;
        [XmlIgnore]
        public ReaderWriterLockSlim BorderNodeLocker;
        static Empire()
        {
            
        }
        public Empire()
        {
            SensorNodeLocker = new ReaderWriterLockSlim();
            BorderNodeLocker = new ReaderWriterLockSlim();
        }

        public Dictionary<int, Fleet> GetFleetsDict()
        {
            return this.FleetsDict;
        }

        public int GetUnusedKeyForFleet()
        {
            int key = 0;
            while (this.FleetsDict.ContainsKey(key))
                ++key;
            return key;
        }

        public float GetPopulation()
        {
            float pop = 0.0f;
            lock (GlobalStats.OwnedPlanetsLock)
            {
                foreach (Planet p in this.OwnedPlanets)
                    pop += p.Population;
            }
            return pop / 1000f;
        }

        public void CleanOut()
        {
            this.OwnedPlanets.Clear();
            this.OwnedShips.Clear();
            this.Relationships.Clear();
            this.GSAI = (GSAI)null;
            this.HostilesPresent.Clear();
            this.ForcePool.Clear();
            this.KnownShips.Clear();
            this.SensorNodes.Clear();
            this.BorderNodes.Clear();
            this.TechnologyDict.Clear();
            this.SpaceRoadsList.Clear();
            foreach (KeyValuePair<int, Fleet> keyValuePair in this.FleetsDict)
                keyValuePair.Value.Ships.Clear();
            this.FleetsDict.Clear();
        }

        public void SetAsDefeated()
        {
            if (this.data.Defeated)
                return;
            this.data.Defeated = true;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                solarSystem.OwnerList.Remove(this);
            this.BorderNodeLocker.EnterWriteLock();
                this.BorderNodes.Clear();
                this.BorderNodeLocker.ExitWriteLock();
            //lock (GlobalStats.SensorNodeLocker)
            this.SensorNodeLocker.EnterWriteLock();
                this.SensorNodes.Clear();
                this.SensorNodeLocker.ExitWriteLock();
            if (this.isFaction)
                return;
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                keyValuePair.Value.AtWar = false;
                keyValuePair.Value.Treaty_Alliance = false;
                keyValuePair.Value.Treaty_NAPact = false;
                keyValuePair.Value.Treaty_OpenBorders = false;
                keyValuePair.Value.Treaty_Peace = false;
                keyValuePair.Value.Treaty_Trade = false;
                keyValuePair.Key.GetRelations()[this].AtWar = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Alliance = false;
                keyValuePair.Key.GetRelations()[this].Treaty_NAPact = false;
                keyValuePair.Key.GetRelations()[this].Treaty_OpenBorders = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Peace = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Trade = false;
            }
            foreach (Ship ship in (List<Ship>)this.OwnedShips)
            {
                ship.GetAI().OrderQueue.Clear();
                ship.GetAI().State = AIState.AwaitingOrders;
            }
            this.GSAI.Goals.Clear();
            lock (GlobalStats.TaskLocker)
                this.GSAI.TaskList.Clear();
            foreach (KeyValuePair<int, Fleet> keyValuePair in this.FleetsDict)
                keyValuePair.Value.Reset();
            Empire rebelsFromEmpireData = CreatingNewGameScreen.CreateRebelsFromEmpireData(this.data, this);
            rebelsFromEmpireData.data.Traits.Name = this.data.Traits.Singular + " Remnant";
            rebelsFromEmpireData.data.Traits.Singular = this.data.Traits.Singular;
            rebelsFromEmpireData.data.Traits.Plural = this.data.Traits.Plural;
            rebelsFromEmpireData.isFaction = true;
            foreach (Empire key in EmpireManager.EmpireList)
            {
                key.GetRelations().Add(rebelsFromEmpireData, new Relationship(rebelsFromEmpireData.data.Traits.Name));
                rebelsFromEmpireData.GetRelations().Add(key, new Relationship(key.data.Traits.Name));
            }
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                solarSystem.ExploredDict.Add(rebelsFromEmpireData, false);
                foreach (Planet planet in solarSystem.PlanetList)
                    planet.ExploredDict.Add(rebelsFromEmpireData, false);
            }
            EmpireManager.EmpireList.Add(rebelsFromEmpireData);
            this.data.RebellionLaunched = true;
            StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")].Add(EmpireManager.EmpireList.IndexOf(rebelsFromEmpireData), new Snapshot(Empire.universeScreen.StarDate));
            foreach (Ship s in (List<Ship>)this.OwnedShips)
            {
                s.loyalty = rebelsFromEmpireData;
                rebelsFromEmpireData.AddShip(s);
            }
            this.OwnedShips.Clear();
            this.data.AgentList.Clear();
        }

        public void SetAsMerged()
        {
            if (this.data.Defeated)
                return;
            this.data.Defeated = true;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                solarSystem.OwnerList.Remove(this);
            this.BorderNodeLocker.EnterWriteLock();
                this.BorderNodes.Clear();
                this.BorderNodeLocker.ExitWriteLock();
            this.SensorNodeLocker.EnterWriteLock();
                this.SensorNodes.Clear();
                this.SensorNodeLocker.ExitWriteLock();
            if (this.isFaction)
                return;
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                keyValuePair.Value.AtWar = false;
                keyValuePair.Value.Treaty_Alliance = false;
                keyValuePair.Value.Treaty_NAPact = false;
                keyValuePair.Value.Treaty_OpenBorders = false;
                keyValuePair.Value.Treaty_Peace = false;
                keyValuePair.Value.Treaty_Trade = false;
                keyValuePair.Key.GetRelations()[this].AtWar = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Alliance = false;
                keyValuePair.Key.GetRelations()[this].Treaty_NAPact = false;
                keyValuePair.Key.GetRelations()[this].Treaty_OpenBorders = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Peace = false;
                keyValuePair.Key.GetRelations()[this].Treaty_Trade = false;
            }
            foreach (Ship ship in (List<Ship>)this.OwnedShips)
            {
                ship.GetAI().OrderQueue.Clear();
                ship.GetAI().State = AIState.AwaitingOrders;
            }
            this.GSAI.Goals.Clear();
            lock (GlobalStats.TaskLocker)
                this.GSAI.TaskList.Clear();
            foreach (KeyValuePair<int, Fleet> keyValuePair in this.FleetsDict)
                keyValuePair.Value.Reset();
            this.OwnedShips.Clear();
            this.data.AgentList.Clear();
        }

        public bool IsPointInBorders(Vector2 point)
        {
            this.BorderNodeLocker.EnterReadLock();
            {
                foreach (Empire.InfluenceNode item_0 in (List<Empire.InfluenceNode>)this.BorderNodes)
                {
                    if ((double)Vector2.Distance(item_0.Position, point) <= (double)item_0.Radius)
                        return true;
                }
            }
            this.BorderNodeLocker.EnterReadLock();
            return false;
        }

        public Dictionary<string, bool> GetHDict()
        {
            return this.UnlockedHullsDict;
        }

        public Dictionary<string, bool> GetTrDict()
        {
            return this.UnlockedTroopDict;
        }

        public Dictionary<string, bool> GetBDict()
        {
            return this.UnlockedBuildingsDict;
        }

        public Dictionary<string, bool> GetMDict()
        {
            return this.UnlockedModulesDict;
        }

        public Dictionary<string, TechEntry> GetTDict()
        {
            return this.TechnologyDict;
        }

        public float GetProjectedResearchNextTurn()
        {
            float num = 0.0f;
            foreach (Planet planet in this.OwnedPlanets)
                num += planet.NetResearchPerTurn;
            return num;
        }

        public List<Planet> GetPlanets()
        {
            return this.OwnedPlanets;
        }

        public List<SolarSystem> GetOwnedSystems()
        {
            //List<SolarSystem> list = new List<SolarSystem>();
            HashSet<SolarSystem> list = new HashSet<SolarSystem>();
            //foreach (Planet planet in this.OwnedPlanets)
            for (int i = 0; i < this.OwnedPlanets.Count; i++)
            {
                //Planet planet =null;
                //lock(this.OwnedPlanets)
                Planet planet = this.OwnedPlanets[i];
                //if (!list.Contains(planet.system))
                    list.Add(planet.system);
            }
            return list.ToList();
        }

        public void AddPlanet(Planet p)
        {
            lock (GlobalStats.OwnedPlanetsLock)
                this.OwnedPlanets.Add(p);
        }

        public void AddTradeMoney(float HowMuch)
        {
            this.TradeMoneyAddedThisTurn += HowMuch;
            this.totalTradeIncome += (int)HowMuch;
            this.Money += HowMuch;
        }

        public BatchRemovalCollection<Ship> GetShips()
        {
            return this.OwnedShips;
        }

        public void AddShip(Ship s)
        {
            this.OwnedShips.Add(s);
        }

        public void AddShipNextFrame(Ship s)
        {
            this.ShipsToAdd.Add(s);
        }

        public void Initialize()
        {
            this.GSAI = new GSAI(this);
            for (int key = 1; key < 100; ++key)
            {
                Fleet fleet = new Fleet();
                fleet.Owner = this;
                string str = "";
                switch (key)
                {
                    case 0:
                        str = "10th";
                        break;
                    case 1:
                        str = "1st";
                        break;
                    case 2:
                        str = "2nd";
                        break;
                    case 3:
                        str = "3rd";
                        break;
                    case 4:
                        str = "4th";
                        break;
                    case 5:
                        str = "5th";
                        break;
                    case 6:
                        str = "6th";
                        break;
                    case 7:
                        str = "7th";
                        break;
                    case 8:
                        str = "8th";
                        break;
                    case 9:
                        str = "9th";
                        break;
                }
                fleet.Name = str + " fleet";
                this.FleetsDict.Add(key, fleet);
            }
            foreach (KeyValuePair<string, Technology> keyValuePair in ResourceManager.TechTree)
            {
                TechEntry techEntry = new TechEntry();
                techEntry.Progress = 0.0f;
                techEntry.UID = keyValuePair.Key;

                //added by McShooterz: Checks if tech is racial, hides it, and reveals it only to races that pass
                if (keyValuePair.Value.RaceRestrictions.Count != 0)
                {
                    techEntry.Discovered = false;
                    techEntry.GetTech().Secret = true;
                    foreach (Technology.RequiredRace raceTech in keyValuePair.Value.RaceRestrictions)
                    {
                        if (raceTech.ShipType == this.data.Traits.ShipType)
                        {
                            techEntry.Discovered = true;
                            techEntry.Unlocked = keyValuePair.Value.RootNode == 1;
                            if (this.data.Traits.Militaristic == 1 && techEntry.GetTech().Militaristic)
                                techEntry.Unlocked = true;
                            break;
                        }
                    }
                }
                else //not racial tech
                {
                    techEntry.Unlocked = keyValuePair.Value.RootNode == 1;
                }
                if (this.isFaction || this.data.Traits.Prewarp == 1)
                {
                    techEntry.Unlocked = false;
                }
                if (this.data.Traits.Militaristic == 1)
                {
                    //added by McShooterz: alternate way to unlock militaristic techs
                    if (techEntry.GetTech().Militaristic && techEntry.GetTech().RaceRestrictions.Count == 0)
                        techEntry.Unlocked = true;

                    // If using the customMilTraitsTech option in ModInformation, default traits will NOT be automatically unlocked. Allows for totally custom militaristic traits.
                    if (GlobalStats.ActiveMod == null || (GlobalStats.ActiveMod != null && !GlobalStats.ActiveMod.mi.customMilTraitTechs))
                    {
                        if (techEntry.UID == "HeavyFighterHull")
                        {
                            techEntry.Unlocked = true;
                        }
                        if (techEntry.UID == "Military")
                        {
                            techEntry.Unlocked = true;
                        }
                        if (techEntry.UID == "ArmorTheory")
                        {
                            techEntry.Unlocked = true;
                        }
                    }
                }
                if (techEntry.Unlocked)
                    techEntry.Progress = techEntry.GetTech().Cost * UniverseScreen.GamePaceStatic;
                this.TechnologyDict.Add(keyValuePair.Key, techEntry);
            }
            foreach (KeyValuePair<string, ShipData> keyValuePair in ResourceManager.HullsDict)
                this.UnlockedHullsDict.Add(keyValuePair.Value.Hull, false);
            foreach (KeyValuePair<string, Troop> keyValuePair in ResourceManager.TroopsDict)
                this.UnlockedTroopDict.Add(keyValuePair.Key, false);
            foreach (KeyValuePair<string, Building> keyValuePair in ResourceManager.BuildingsDict)
                this.UnlockedBuildingsDict.Add(keyValuePair.Key, false);
            foreach (KeyValuePair<string, ShipModule> keyValuePair in ResourceManager.ShipModulesDict)
                this.UnlockedModulesDict.Add(keyValuePair.Key, false);
            //unlock from empire data file
            foreach (string building in this.data.unlockBuilding)
                this.UnlockedBuildingsDict[building] = true;
            foreach (KeyValuePair<string, TechEntry> keyValuePair in this.TechnologyDict)
            {
                if (keyValuePair.Value.Unlocked)
                {
                    keyValuePair.Value.Unlocked = false;
                    this.UnlockTech(keyValuePair.Key);
                }
            }
            //unlock ships from empire data
            foreach (string ship in this.data.unlockShips)
                this.ShipsWeCanBuild.Add(ship);
            //clear these lists as they serve no more purpose
            this.data.unlockBuilding.Clear();
            this.data.unlockShips.Clear();
            this.UpdateShipsWeCanBuild();
            if (this.data.EconomicPersonality == null)
                this.data.EconomicPersonality = new ETrait
                {
                    Name = "Generalists"
                };
            //Added by McShooterz: mod support for EconomicResearchStrategy folder
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/EconomicResearchStrategy/" , this.data.EconomicPersonality.Name , ".xml")))
            {
                this.economicResearchStrategy = (EconomicResearchStrategy)ResourceManager.EconSerializer.Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/EconomicResearchStrategy/", this.data.EconomicPersonality.Name, ".xml")).OpenRead());
            }
            else
            {
                this.economicResearchStrategy = (EconomicResearchStrategy)ResourceManager.EconSerializer.Deserialize((Stream)new FileInfo("Content/EconomicResearchStrategy/" + this.data.EconomicPersonality.Name + ".xml").OpenRead());
            }

            //Added by gremlin Figure out techs with modules that we have ships for.
            foreach (KeyValuePair<string,TechEntry> tech in this.TechnologyDict)
            {
                if(tech.Value.GetTech().ModulesUnlocked.Count>0  &&  tech.Value.GetTech().HullsUnlocked.Count()==0 && !this.WeCanUseThis(tech.Value.GetTech()))
                {
                    this.TechnologyDict[tech.Key].shipDesignsCanuseThis = false;
                }

            }
            foreach (KeyValuePair<string,TechEntry> tech in this.TechnologyDict)
            {
                if(!tech.Value.shipDesignsCanuseThis)
                {
                    if(WeCanUseThisLater(tech.Value))
                    {
                        tech.Value.shipDesignsCanuseThis = true;
                    }
                }
            }
        }

        private bool WeCanUseThisLater(TechEntry tech)
        {
            //List<Technology.LeadsToTech> leadsto = new List<Technology.LeadsToTech>();
            //    leadsto =tech.GetTech().LeadsTo;
           
            foreach (Technology.LeadsToTech leadsto in tech.GetTech().LeadsTo)
                {
                    TechEntry entry = this.TechnologyDict[leadsto.UID];
                    if (entry.shipDesignsCanuseThis == true)
                        return true;
                    else
                        if (WeCanUseThisLater(entry))
                            return true;
                }
            return false;

        }
        
        public EconomicResearchStrategy getResStrat()
        {
            return this.economicResearchStrategy;
        }

        public bool WeCanBuildTroop(string ID)
        {
            return this.UnlockedTroopDict.ContainsKey(ID) && this.UnlockedTroopDict[ID];
        }

        public void UnlockTech(string techID)
        {
            if (this.TechnologyDict[techID].Unlocked)
                return;
            //Add to level of tech if more than one level
            if (ResourceManager.TechTree[techID].MaxLevel > 1)
            {
                this.TechnologyDict[techID].level++;
                if (this.TechnologyDict[techID].level == ResourceManager.TechTree[techID].MaxLevel)
                {
                    this.TechnologyDict[techID].Progress = this.TechnologyDict[techID].GetTechCost() * UniverseScreen.GamePaceStatic;
                    this.TechnologyDict[techID].Unlocked = true;
                }
                else
                {
                    this.TechnologyDict[techID].Unlocked = false;
                    this.TechnologyDict[techID].Progress = 0;
                }
            }
            else
            {
                this.TechnologyDict[techID].Progress = this.TechnologyDict[techID].GetTech().Cost * UniverseScreen.GamePaceStatic;
                this.TechnologyDict[techID].Unlocked = true;
            }
            //Set GSAI to build ship roles
            if (this.TechnologyDict[techID].GetTech().unlockBattleships || techID == "Battleships")
                this.canBuildCapitals = true;
            if (this.TechnologyDict[techID].GetTech().unlockCruisers || techID == "Cruisers")
                this.canBuildCruisers = true;
            if (this.TechnologyDict[techID].GetTech().unlockFrigates || techID == "FrigateConstruction")
                this.canBuildFrigates = true;
            if (this.TechnologyDict[techID].GetTech().unlockCorvettes || techID == "HeavyFighterHull")
                this.canBuildCorvettes = true;

            //Added by McShooterz: Race Specific buildings
            foreach (Technology.UnlockedBuilding unlockedBuilding in ResourceManager.TechTree[techID].BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == this.data.Traits.ShipType || unlockedBuilding.Type == null || unlockedBuilding.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            if (ResourceManager.TechTree[techID].RootNode == 0)
            {
                foreach (Technology.LeadsToTech leadsToTech in ResourceManager.TechTree[techID].LeadsTo)
                {
                    //added by McShooterz: Prevent Racial tech from being discovered by unintentional means
                    if (this.TechnologyDict[leadsToTech.UID].GetTech().RaceRestrictions.Count == 0 && !this.TechnologyDict[leadsToTech.UID].GetTech().Secret)
                        this.TechnologyDict[leadsToTech.UID].Discovered = true;
                }
            }
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in ResourceManager.TechTree[techID].ModulesUnlocked)
            {
                if (unlockedMod.Type == this.data.Traits.ShipType || unlockedMod.Type == null || unlockedMod.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedModulesDict[unlockedMod.ModuleUID] = true;
            }
            foreach (Technology.UnlockedTroop unlockedTroop in ResourceManager.TechTree[techID].TroopsUnlocked)
            {
                if (unlockedTroop.Type == this.data.Traits.ShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null || unlockedTroop.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in ResourceManager.TechTree[techID].HullsUnlocked)
            {
                if (unlockedHull.ShipType == this.data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedHullsDict[unlockedHull.Name] = true;
            }

            // Added by The Doctor - trigger events with unlocking of techs, via Technology XML
            foreach (Technology.TriggeredEvent triggeredEvent in ResourceManager.TechTree[techID].EventsTriggered)
            {
                if (triggeredEvent.CustomMessage != null)
                {
                    if ((triggeredEvent.Type == this.data.Traits.ShipType || triggeredEvent.Type == null || triggeredEvent.Type == this.TechnologyDict[techID].AcquiredFrom) && this.isPlayer)
                    {
                        Ship.universeScreen.NotificationManager.AddEventNotification(ResourceManager.EventsDict[triggeredEvent.EventUID], triggeredEvent.CustomMessage);
                    }
                }
                else if (triggeredEvent.CustomMessage == null)
                {
                    if ((triggeredEvent.Type == this.data.Traits.ShipType || triggeredEvent.Type == null || triggeredEvent.Type == this.TechnologyDict[techID].AcquiredFrom) && this.isPlayer)
                    {
                        Ship.universeScreen.NotificationManager.AddEventNotification(ResourceManager.EventsDict[triggeredEvent.EventUID]);
                    }
                }
           }

            // Added by The Doctor - reveal specified 'secret' techs with unlocking of techs, via Technology XML
            foreach (Technology.RevealedTech revealedTech in ResourceManager.TechTree[techID].TechsRevealed)
            {
                if (revealedTech.Type == this.data.Traits.ShipType || revealedTech.Type == null || revealedTech.Type == this.TechnologyDict[techID].AcquiredFrom)
                {
                    this.GetTDict()[revealedTech.RevUID].Discovered = true;
                }

            }
            foreach (Technology.UnlockedBonus unlockedBonus in ResourceManager.TechTree[techID].BonusUnlocked)
            {
                //Added by McShooterz: Race Specific bonus
                if (unlockedBonus.Type == null || unlockedBonus.Type == this.data.Traits.ShipType || unlockedBonus.Type == this.TechnologyDict[techID].AcquiredFrom)
                {
                    string str = unlockedBonus.BonusType;
                    if (string.IsNullOrEmpty(str))
                        str = unlockedBonus.Name;
                    if (unlockedBonus.Tags.Count > 0)
                    {
                        foreach (string index in unlockedBonus.Tags)
                        {
                            switch (unlockedBonus.BonusType)
                            {
                                case "Weapon_Speed":
                                    this.data.WeaponTags[index].Speed += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_Damage":
                                    this.data.WeaponTags[index].Damage += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_ExplosionRadius":
                                    this.data.WeaponTags[index].ExplosionRadius += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_TurnSpeed":
                                    this.data.WeaponTags[index].Turn += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_Rate":
                                    this.data.WeaponTags[index].Rate += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_Range":
                                    this.data.WeaponTags[index].Range += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_ShieldDamage":
                                    this.data.WeaponTags[index].ShieldDamage += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_ArmorDamage":
                                    this.data.WeaponTags[index].ArmorDamage += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_HP":
                                    this.data.WeaponTags[index].HitPoints += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_ShieldPenetration":
                                    this.data.WeaponTags[index].ShieldPenetration += unlockedBonus.Bonus;
                                    continue;
                                case "Weapon_ArmourPenetration":
                                    this.data.WeaponTags[index].ArmourPenetration += unlockedBonus.Bonus;
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                    if (str == "Xeno Compilers" || str == "Research Bonus")
                        this.data.Traits.ResearchMod += unlockedBonus.Bonus;
                    if (str == "FTL Spool Bonus")
                    {
                        if (unlockedBonus.Bonus < 1)
                            this.data.SpoolTimeModifier *= 1.0f - unlockedBonus.Bonus; // i.e. if there is a 0.2 (20%) bonus unlocked, the spool modifier is 1-0.2 = 0.8* existing spool modifier...
                        else if (unlockedBonus.Bonus >= 1)
                            this.data.SpoolTimeModifier = 0f; // insta-warp by modifier
                    }
                    if (str == "Top Guns" || str == "Bonus Fighter Levels")
                    {
                        this.data.BonusFighterLevels += (int)unlockedBonus.Bonus;
                        foreach (Ship ship in (List<Ship>)this.OwnedShips)
                        {
                            if (ship.Role == "fighter")
                            {
                                ship.Level += (int)unlockedBonus.Bonus;
                                if (ship.Level > 5)
                                    ship.Level = 5;
                            }
                        }
                    }
                    if (str == "Mass Reduction" || str == "Percent Mass Adjustment")
                        this.data.MassModifier += unlockedBonus.Bonus;
                    if (str == "Resistance is Futile" || str == "Allow Assimilation")
                        this.data.Traits.Assimilators = true;
                    if (str == "Cryogenic Suspension" || str == "Passenger Modifier")
                        this.data.Traits.PassengerModifier += (int)unlockedBonus.Bonus;
                    if (str == "ECM Bonus" || str == "Missile Dodge Change Bonus")
                        this.data.MissileDodgeChance += unlockedBonus.Bonus;
                    if (str == "Set FTL Drain Modifier")
                        this.data.FTLPowerDrainModifier = unlockedBonus.Bonus;
                    if (str == "Super Soldiers" || str == "Troop Strength Modifier Bonus")
                        this.data.Traits.GroundCombatModifier += unlockedBonus.Bonus;
                    if (str == "Fuel Cell Upgrade" || str == "Fuel Cell Bonus")
                        this.data.FuelCellModifier += unlockedBonus.Bonus;
                    if (str == "Trade Tariff" || str == "Bonus Money Per Trade")
                        this.data.Traits.Mercantile += (float)(int)unlockedBonus.Bonus;
                    if (str == "Missile Armor" || str == "Missile HP Bonus")
                        this.data.MissileHPModifier += unlockedBonus.Bonus;
                    if (str == "Hull Strengthening" || str == "Module HP Bonus")
                        this.data.Traits.ModHpModifier += unlockedBonus.Bonus;
                    if (str == "Reaction Drive Upgrade" || str == "STL Speed Bonus")
                        this.data.SubLightModifier += unlockedBonus.Bonus;
                    if (str == "Reactive Armor" || str == "Armor Explosion Reduction")
                        this.data.ExplosiveRadiusReduction += unlockedBonus.Bonus;
                    if (str == "Slipstreams" || str == "In Borders FTL Bonus")
                        this.data.Traits.InBordersSpeedBonus += unlockedBonus.Bonus;
                    if (str == "StarDrive Enhancement" || str == "FTL Speed Bonus")
                        this.data.FTLModifier += unlockedBonus.Bonus * this.data.FTLModifier;
                    if (str == "FTL Efficiency" || str == "FTL Efficiency Bonus")
                        this.data.FTLPowerDrainModifier = this.data.FTLPowerDrainModifier - unlockedBonus.Bonus * this.data.FTLPowerDrainModifier;
                    if (str == "Spy Offense" || str == "Spy Offense Roll Bonus")
                        this.data.OffensiveSpyBonus += unlockedBonus.Bonus;
                    if (str == "Spy Defense" || str == "Spy Defense Roll Bonus")
                        this.data.DefensiveSpyBonus += unlockedBonus.Bonus;
                    if (str == "Increased Lifespans" || str == "Population Growth Bonus")
                        this.data.Traits.ReproductionMod += unlockedBonus.Bonus;
                    if (str == "Set Population Growth Min")
                        this.data.Traits.PopGrowthMin = unlockedBonus.Bonus;
                    if (str == "Set Population Growth Max")
                        this.data.Traits.PopGrowthMax = unlockedBonus.Bonus;
                    if (str == "Xenolinguistic Nuance" || str == "Diplomacy Bonus")
                        this.data.Traits.DiplomacyMod += unlockedBonus.Bonus;
                    if (str == "Ordnance Effectiveness" || str == "Ordnance Effectiveness Bonus")
                        this.data.OrdnanceEffectivenessBonus += unlockedBonus.Bonus;
                    if (str == "Tachyons" || str == "Sensor Range Bonus")
                        this.data.SensorModifier += unlockedBonus.Bonus;
                    if (str == "Privatization")
                        this.data.Privatization = true;
                    if (str == "Armor Piercing" || str == "Armor Phasing")
                        this.data.ArmorPiercingBonus += (int)unlockedBonus.Bonus;
                    if (str == "Kulrathi Might")
                        this.data.Traits.ModHpModifier += unlockedBonus.Bonus;
                    if (str == "Subspace Inhibition")
                        this.data.Inhibitors = true;
                    //added by McShooterz: New Bonuses
                    if (str == "Production Bonus")
                        this.data.Traits.ProductionMod += unlockedBonus.Bonus;
                    if (str == "Construction Bonus")
                        this.data.Traits.ShipCostMod -= unlockedBonus.Bonus;
                    if (str == "Consumption Bonus")
                        this.data.Traits.ConsumptionModifier -= unlockedBonus.Bonus;
                    if (str == "Tax Bonus")
                        this.data.Traits.TaxMod += unlockedBonus.Bonus;
                    if (str == "Repair Bonus")
                        this.data.Traits.RepairMod += unlockedBonus.Bonus;
                    if (str == "Maintenance Bonus")
                        this.data.Traits.MaintMod -= unlockedBonus.Bonus;
                    if (str == "Power Flow Bonus")
                        this.data.PowerFlowMod += unlockedBonus.Bonus;
                    if (str == "Shield Power Bonus")
                        this.data.ShieldPowerMod += unlockedBonus.Bonus;
                    if (str == "Ship Experience Bonus")
                        this.data.ExperienceMod += unlockedBonus.Bonus;
                }
            }
            //update ship stats if a bonus was unlocked
            if (ResourceManager.TechTree[techID].BonusUnlocked.Count > 0)
            {
                foreach (Ship ship in this.OwnedShips)
                    ship.shipStatusChanged = true;
            }
            this.UpdateShipsWeCanBuild();
            if (Empire.universeScreen != null && this != EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
                this.GSAI.TriggerRefit();
            if (!this.data.ResearchQueue.Contains(techID))
                return;
            this.data.ResearchQueue.Remove(techID);
        }

        public void UnlockTechFromSave(string techID)
        {
            this.TechnologyDict[techID].Progress = this.TechnologyDict[techID].GetTech().Cost * UniverseScreen.GamePaceStatic;
            this.TechnologyDict[techID].Unlocked = true;
            foreach (Technology.UnlockedBuilding unlockedBuilding in ResourceManager.TechTree[techID].BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == this.data.Traits.ShipType || unlockedBuilding.Type == null || unlockedBuilding.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            if (ResourceManager.TechTree[techID].RootNode == 0)
            {
                foreach (Technology.LeadsToTech leadsToTech in ResourceManager.TechTree[techID].LeadsTo)
                    if (this.TechnologyDict[leadsToTech.UID].GetTech().RaceRestrictions.Count == 0 && !this.TechnologyDict[leadsToTech.UID].GetTech().Secret)
                        this.TechnologyDict[leadsToTech.UID].Discovered = true;
            }
            foreach (Technology.UnlockedMod unlockedMod in ResourceManager.TechTree[techID].ModulesUnlocked)
            {
                if (unlockedMod.Type == this.data.Traits.ShipType || unlockedMod.Type == null || unlockedMod.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedModulesDict[unlockedMod.ModuleUID] = true;
            }
            foreach (Technology.UnlockedTroop unlockedTroop in ResourceManager.TechTree[techID].TroopsUnlocked)
            {
                if (unlockedTroop.Type == this.data.Traits.ShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null || unlockedTroop.Type == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in ResourceManager.TechTree[techID].HullsUnlocked)
            {
                if (unlockedHull.ShipType == this.data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == this.TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedHullsDict[unlockedHull.Name] = true;
            }
            this.UpdateShipsWeCanBuild();
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target)
        {
            this.TechnologyDict[techID].AcquiredFrom = target.data.Traits.ShipType;
            this.UnlockTech(techID);
        }

        public void UnlockHullsSave(string techID, string AbsorbedShipType)
        {
            foreach (Technology.UnlockedTroop unlockedTroop in ResourceManager.TechTree[techID].TroopsUnlocked)
            {
                if (unlockedTroop.Type == AbsorbedShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null)
                    this.UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in ResourceManager.TechTree[techID].HullsUnlocked)
            {
                if (unlockedHull.ShipType == AbsorbedShipType || unlockedHull.ShipType == null)
                    this.UnlockedHullsDict[unlockedHull.Name] = true;
            }
            foreach (Technology.UnlockedMod unlockedMod in ResourceManager.TechTree[techID].ModulesUnlocked)
            {
                if (unlockedMod.Type == AbsorbedShipType || unlockedMod.Type == null)
                    this.UnlockedModulesDict[unlockedMod.ModuleUID] = true;
            }
            foreach (Technology.UnlockedBuilding unlockedBuilding in ResourceManager.TechTree[techID].BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == AbsorbedShipType || unlockedBuilding.Type == null)
                    this.UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            this.UpdateShipsWeCanBuild();
        }

        private void AssessHostilePresence()
        {
            List<SolarSystem> list = new List<SolarSystem>();
            foreach (Planet planet in this.OwnedPlanets)
            {
                if (!list.Contains(planet.system))
                    list.Add(planet.system);
            }
            foreach (SolarSystem beingInvaded in list)
            {
                foreach (Ship ship in (List<Ship>)beingInvaded.ShipList)
                {
                    if (ship.loyalty != this && (ship.loyalty.isFaction || this.Relationships[ship.loyalty].AtWar) && !this.HostilesPresent[beingInvaded])
                    {
                        Empire.universeScreen.NotificationManager.AddBeingInvadedNotification(beingInvaded, ship.loyalty);
                        this.HostilesPresent[beingInvaded] = true;
                        break;
                    }
                }
            }
        }

        public List<Ship> GetShipsInOurBorders()
        {
            //return this.UnownedShipsInOurBorders;
            return this.GetGSAI().ThreatMatrix.GetAllShipsInOurBorders();
        }

        public void UpdateKnownShipsold()
        {
            lock (GlobalStats.KnownShipsLock)
            {
                if (this.isPlayer && Empire.universeScreen.Debug)
                {
                    for (int i = 0; i < Empire.universeScreen.MasterShipList.Count; i++)
                    {
                        Ship item = Empire.universeScreen.MasterShipList[i];
                        item.inSensorRange = true;
                        this.KnownShips.Add(item);
                        this.GSAI.ThreatMatrix.UpdatePin(item);
                    }
                    return;
                }
            }
            for (int j = 0; j < Empire.universeScreen.MasterShipList.Count; j++)
            {
                Ship ship = Empire.universeScreen.MasterShipList[j];
                if (ship.loyalty != this)
                {
                    List<Ship> ships = new List<Ship>();
                    lock (this.SensorNodeLocker)
                    {
                        foreach (Empire.InfluenceNode sensorNode in this.SensorNodes)
                        {
                            if (Vector2.Distance(sensorNode.Position, ship.Center) >= sensorNode.Radius)
                            {
                                continue;
                            }
                            if (!this.Relationships[ship.loyalty].Known)
                            {
                                this.DoFirstContact(ship.loyalty);
                            }
                            ships.Add(ship);
                            this.GSAI.ThreatMatrix.UpdatePin(ship);
                            if (!this.isPlayer)
                            {
                                break;
                            }
                            ship.inSensorRange = true;
                            if (ship.GetSystem() == null || !this.isFaction && !ship.loyalty.isFaction && !this.Relationships[ship.loyalty].AtWar)
                            {
                                break;
                            }
                            ship.GetSystem().DangerTimer = 120f;
                            break;
                        }
                    }
                    lock (GlobalStats.KnownShipsLock)
                    {
                        foreach (Ship ship1 in ships)
                        {
                            this.KnownShips.Add(ship1);
                        }
                        ships.Clear();
                    }
                }
                else
                {
                    ship.inborders = false;
                    lock (GlobalStats.KnownShipsLock)
                    {
                        this.KnownShips.Add(ship);
                    }
                    if (this.isPlayer)
                    {
                        ship.inSensorRange = true;
                    }
                    this.BorderNodeLocker.EnterReadLock();
                    {
                        foreach (Empire.InfluenceNode borderNode in this.BorderNodes)
                        {
                            if (Vector2.Distance(borderNode.Position, ship.Center) >= borderNode.Radius)
                            {
                                continue;
                            }
                            ship.inborders = true;
                            break;
                        }
                        foreach (KeyValuePair<Empire, Relationship> relationship in this.Relationships)
                        {
                            if (relationship.Key != this || !relationship.Value.Treaty_OpenBorders)
                            {
                                continue;
                            }
                            foreach (Empire.InfluenceNode influenceNode in relationship.Key.BorderNodes)
                            {
                                if (Vector2.Distance(influenceNode.Position, ship.Center) >= influenceNode.Radius)
                                {
                                    continue;
                                }
                                ship.inborders = true;
                                break;
                            }
                        }
                    }
                    this.BorderNodeLocker.ExitReadLock();
                }
            }
        }

        public void UpdateKnownShips()
        {
           // this.GetGSAI().ThreatMatrix.ScrubMatrix(true);
            lock (GlobalStats.KnownShipsLock)
            {
                if (this.isPlayer && Empire.universeScreen.Debug)
                {
                    for (int i = 0; i < Empire.universeScreen.MasterShipList.Count; i++)
                    //Parallel.For(0, Empire.universeScreen.MasterShipList.Count, i =>
                    {
                        Ship nearby = Empire.universeScreen.MasterShipList[i];
                        nearby.inSensorRange = true;
                        //lock (this.KnownShips)
                        this.KnownShips.Add(nearby);
                        this.GSAI.ThreatMatrix.UpdatePin(nearby);
                    }//);
                    return;
                }
            }
            //added by gremlin ships in border search
            //for (int i = 0; i < Empire.universeScreen.MasterShipList.Count; i++)
            var source = Empire.universeScreen.MasterShipList.ToArray();
            var rangePartitioner = Partitioner.Create(0, source.Length);
            ConcurrentBag<Ship> Shipbag =new ConcurrentBag<Ship>();
            //Parallel.For(0, Empire.universeScreen.MasterShipList.Count, i =>  
            
            {
                Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {

                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        Ship nearby = Empire.universeScreen.MasterShipList[i];
                        if (nearby.loyalty != this)
                        {
                            List<Ship> toadd = new List<Ship>();
                            bool flag = false;
                            bool border = false;

                            List<Empire.InfluenceNode> influenceNodes;
                            //lock (GlobalStats.SensorNodeLocker)
                            this.SensorNodeLocker.EnterReadLock();
                            {
                                influenceNodes = new List<InfluenceNode>(this.SensorNodes);
                            }
                            this.SensorNodeLocker.ExitReadLock();
                            {


                                foreach (Empire.InfluenceNode node in influenceNodes)
                            //Parallel.ForEach<Empire.InfluenceNode>(this.SensorNodes, (node, status) =>
                            {
                                if (Vector2.Distance(node.Position, nearby.Center) >= node.Radius)
                                {
                                    // this.GSAI.ThreatMatrix.UpdatePin(nearby, border);
                                    continue;
                                    //return;
                                }
                                if (!this.Relationships[nearby.loyalty].Known)
                                {
                                    this.DoFirstContact(nearby.loyalty);
                                }
                                //toadd.Add(nearby);
                                flag = true;
                                if ((node.KeyedObject is Ship
                                    && ((node.KeyedObject as Ship).inborders //&& Vector2.Distance(nearby.Position,(node.KeyedObject as Ship).Position ) <300000)
                                    || (node.KeyedObject as Ship).Name == "Subspace Projector" || (node.KeyedObject as Ship).GetAI().State == AIState.SystemTrader)) || node.KeyedObject is SolarSystem)
                                {
                                    border = true;
                                    if (this.Relationships[nearby.loyalty].AtWar)
                                        nearby.IsIndangerousSpace = true;
                                    else if (this.Relationships[nearby.loyalty].Treaty_Alliance)
                                        nearby.IsInFriendlySpace = true;
                                    else //if (this.Relationships[nearby.loyalty].Treaty_OpenBorders || this.Relationships[nearby.loyalty].Treaty_NAPact)
                                        nearby.IsInNeutralSpace = true;
                                }
                                //this.GSAI.ThreatMatrix.UpdatePin(nearby);
                                if (!this.isPlayer)
                                {
                                    break;

                                    //status.Stop();
                                    //return;
                                }
                                nearby.inSensorRange = true;
                                if (nearby.GetSystem() == null || !this.isFaction && !nearby.loyalty.isFaction && !this.Relationships[nearby.loyalty].AtWar)
                                {
                                    break;

                                    //status.Stop();
                                    //return;



                                }

                                nearby.GetSystem().DangerTimer = 120f;
                                break;
                                //status.Stop();
                                //return;


                                //status.Stop();
                            }//);
                        }
                            if (flag)
                            {
                                toadd.Add(nearby);
                                //Shipbag.Add(nearby);

                                if (!this.isFaction)
                                    this.GSAI.ThreatMatrix.UpdatePin(nearby, border);
                                //if (border)
                                //{
                                //   this.GSAI.ThreatMatrix.Pins. 
                                //    //lock (this.UnownedShipsInOurBorders)
                                //    //    this.UnownedShipsInOurBorders.Add(nearby);
                                //}

                            }
                            //<--
                            
                            //lock (GlobalStats.KnownShipsLock)
                            {
                                foreach (Ship ship in toadd)
                                {
                                    Shipbag.Add(ship);
                              //      lock (this.KnownShips)
                                //        this.KnownShips.Add(ship);


                                }

                            }
                            toadd.Clear();
                        }
                        else
                        {
                            nearby.inborders = false;
                            //lock (GlobalStats.KnownShipsLock)
                            {
                                Shipbag.Add(nearby);
                              //  lock (this.KnownShips)
                                //    this.KnownShips.Add(nearby);
                            }
                            if (this.isPlayer)
                            {
                                nearby.inSensorRange = true;
                            }
                            this.BorderNodeLocker.EnterReadLock();
                            {
                                foreach (Empire.InfluenceNode node in this.BorderNodes)
                                {
                                    if (Vector2.Distance(node.Position, nearby.Center) >= node.Radius)
                                    {
                                        continue;
                                    }
                                    nearby.inborders = true;
                                    break;
                                }
                                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.Relationships)
                                {
                                    if (Relationship.Key != this || !Relationship.Value.Treaty_OpenBorders)
                                    {
                                        continue;
                                    }
                                    foreach (Empire.InfluenceNode node in Relationship.Key.BorderNodes)
                                    {
                                        if (Vector2.Distance(node.Position, nearby.Center) >= node.Radius)
                                        {
                                            continue;
                                        }
                                        nearby.inborders = true;
                                        break;
                                    }
                                }
                            }
                            this.BorderNodeLocker.ExitReadLock();
                        }
                    }
                });

                lock (GlobalStats.KnownShipsLock)
                {

                        foreach (Ship ship in Shipbag)
                        {


                            this.KnownShips.Add(ship);


                        }
                    

                }
            }
        }
        public Dictionary<Empire, Relationship> GetRelations()
        {
            return this.Relationships;
        }

        public void AddRelationships(Empire e, Relationship i)
        {
            this.Relationships.Add(e, i);
        }

        public void DamageRelationship(Empire e, string why, float Amount, Planet p)
        {
            if (!this.Relationships.ContainsKey(e))
                return;
            switch (why)
            {
                case "Colonized Owned System":
                    this.Relationships[e].DamageRelationship(this, e, why, Amount, p);
                    break;
                case "Destroyed Ship":
                    this.Relationships[e].DamageRelationship(this, e, why, Amount, p);
                    break;
            }
        }

        public void DoDeclareWar(War w)
        {
        }

        public void DoFirstContact(Empire e)
        {

            
            this.Relationships[e].SetInitialStrength(e.data.Traits.DiplomacyMod * 100f);
            this.Relationships[e].Known = true;
            if (!e.GetRelations()[this].Known)
                e.DoFirstContact(this);
#if PERF
            if (Empire.universeScreen.player == this)
                return;
#endif
            if (GlobalStats.perf && Empire.universeScreen.player == this)
                return;
            try
            {
                if (EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) == this && !e.isFaction && !e.MinorRace)
                {
                    Empire.universeScreen.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(e, EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty), "First Contact"));
                }
                else
                {
                    if (EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) != this || !e.isFaction)
                        return;
                    foreach (Encounter e1 in ResourceManager.Encounters)
                    {
                        if (e1.Faction == e.data.Traits.Name && e1.Name == "First Contact")
                            Empire.universeScreen.ScreenManager.AddScreen((GameScreen)new EncounterPopup(Empire.universeScreen, EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty), e, (SolarSystem)null, e1));
                    }
                }
            }
            catch (ArgumentException)
            {   //this feels bad
            }
        }

        public void Update(float elapsedTime)
        {
            //foreach (Ship s in this.ShipsToAdd)
            //{
            //    this.AddShip(s);
            //    if (!this.isPlayer)
            //        this.ForcePoolAdd(s);
            //}
#if PLAYERONLY
            if(!this.isPlayer)
            foreach (Ship ship in this.GetShips())
                ship.GetAI().OrderScrapShip();
            if (this.GetShips().Count == 0)
                return;

#endif
            //this.ShipsToAdd.Clear();
            //{
            //    Empire empire = this;
            //    empire.updateContactsTimer = empire.updateContactsTimer -  0.01666667f;//elapsedTime;
            //    if (this.updateContactsTimer <= 0f && !this.data.Defeated)
            //    {
            //        this.ResetBorders();
            //        lock (GlobalStats.KnownShipsLock)
            //        {
            //            this.KnownShips.Clear();
            //        }
            //        //this.UnownedShipsInOurBorders.Clear();
            //        this.UpdateKnownShips();
            //        this.updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
            //    }
            //}

            this.UpdateTimer -= elapsedTime;
            if ((double)this.UpdateTimer <= 0.0)
            {
                if (this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
                {
                    Empire.universeScreen.StarDate += 0.1f;
                    Empire.universeScreen.StarDate = (float)Math.Round((double)Empire.universeScreen.StarDate, 1);
                    if (!StatTracker.SnapshotsDict.ContainsKey(Empire.universeScreen.StarDate.ToString("#.0")))
                        StatTracker.SnapshotsDict.Add(Empire.universeScreen.StarDate.ToString("#.0"), new SerializableDictionary<int, Snapshot>());
                    foreach (Empire empire in EmpireManager.EmpireList)
                    {
                        if (!empire.data.IsRebelFaction && !StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")].ContainsKey(EmpireManager.EmpireList.IndexOf(empire)))
                            StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")].Add(EmpireManager.EmpireList.IndexOf(empire), new Snapshot(Empire.universeScreen.StarDate));
                    }
                    if ((double)Empire.universeScreen.StarDate == 1000.09997558594)
                    {
                        foreach (Empire empire in EmpireManager.EmpireList)
                        {
                            foreach (Planet planet in empire.GetPlanets())
                            {
                                if (StatTracker.SnapshotsDict.ContainsKey(Empire.universeScreen.StarDate.ToString("#.0")))
                                    StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(planet.Owner)].EmpireNodes.Add(new NRO()
                                    {
                                        Node = planet.Position,
                                        Radius = 300000f,
                                        StarDateMade = Empire.universeScreen.StarDate
                                    });
                            }
                        }
                    }
                    if (!this.InitialziedHostilesDict)
                    {
                        this.InitialziedHostilesDict = true;
                        foreach (SolarSystem key in UniverseScreen.SolarSystemList)
                        {
                            bool flag = false;
                            foreach (Ship ship in (List<Ship>)key.ShipList)
                            {
                                if (ship.loyalty != this && (ship.loyalty.isFaction || this.Relationships[ship.loyalty].AtWar))
                                    flag = true;
                            }
                            this.HostilesPresent.Add(key, flag);
                        }
                    }
                    else
                        this.AssessHostilePresence();
                }
                //added by gremlin. empire ship reserve.

                this.EmpireShipCountReserve = 0;

                if (!this.isPlayer)
                    foreach (Planet planet in this.GetPlanets())
                    {

                        if (planet == this.Capital)
                            this.EmpireShipCountReserve = +5;
                        else
                            this.EmpireShipCountReserve += planet.developmentLevel;
                        if (this.EmpireShipCountReserve > 50)
                        {
                            this.EmpireShipCountReserve = 50;
                            break;
                        }
                    }
                this.empireShipTotal = 0;
                foreach (Ship ship in this.OwnedShips)
                {
                    if (ship.Mothership != null || ship.Role == "troop" || ship.Name == "Subspace Projector" || ship.Role == "freighter")
                        continue;
                    this.empireShipTotal++;
                }
                this.UpdateTimer = (float)GlobalStats.TurnTimer;
                this.DoMoney();
                this.TakeTurn();
            }
            this.UpdateFleets(elapsedTime);
            this.OwnedShips.ApplyPendingRemovals();
        }

        public void UpdateFleets(float elapsedTime)
        {
            this.updateContactsTimer -= elapsedTime;
            this.FleetUpdateTimer -= elapsedTime;
            try
            {
                foreach (KeyValuePair<int, Fleet> keyValuePair in this.FleetsDict)
                {
                    keyValuePair.Value.Update(elapsedTime);
                    if ((double)this.FleetUpdateTimer <= 0.0)
                    {
                        
                        
                        keyValuePair.Value.UpdateAI(elapsedTime, keyValuePair.Key);
                    }
                }
            }
            catch
            {
            }
            if ((double)this.FleetUpdateTimer < 0.0)
                this.FleetUpdateTimer = 5f;
            this.OwnedShips.ApplyPendingRemovals();
        }

        public float GetPlanetIncomes()
        {
            float income = 0.0f;
            for (int index = 0; index < this.OwnedPlanets.Count; ++index)
            {
                Planet planet = this.OwnedPlanets[index];
                if (planet != null)
                {
                    planet.UpdateIncomes();
                    income += (planet.GrossMoneyPT + planet.GrossMoneyPT * this.data.Traits.TaxMod) * this.data.TaxRate;
                    income += planet.PlusFlatMoneyPerTurn + (planet.Population / 1000f * planet.PlusCreditsPerColonist);
                }
            }
            return income;
        }

        private void DoMoney()
        {
            this.MoneyLastTurn = this.Money;
            ++this.numberForAverage;
            this.GrossTaxes = 0f;
            this.OtherIncome = 0f;

            Parallel.Invoke(
                () =>
                {
                    lock (GlobalStats.OwnedPlanetsLock)
                    {
                        for (int i = 0; i < this.OwnedPlanets.Count; ++i)
                        {
                            Planet planet = this.OwnedPlanets[i];
                            if (planet != null)
                            {
                                planet.UpdateIncomes();
                                this.GrossTaxes += planet.GrossMoneyPT + planet.GrossMoneyPT * this.data.Traits.TaxMod;
                                this.OtherIncome += planet.PlusFlatMoneyPerTurn + (planet.Population / 1000f * planet.PlusCreditsPerColonist);
                            }
                        }
                    }
                    this.TradeMoneyAddedThisTurn = 0.0f;
                    foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
                    {
                        if (keyValuePair.Value.Treaty_Trade)
                        {
                            float num = (float)(0.25 * (double)keyValuePair.Value.Treaty_Trade_TurnsExisted - 3.0);
                            if ((double)num > 3.0)
                                num = 3f;
                            this.TradeMoneyAddedThisTurn += num;
                        }
                    }
                },
            () =>
            {
                this.totalShipMaintenance = 0.0f;
                this.totalBuildingMaintenance = 0.0f;
                foreach (Ship ship in (List<Ship>)this.OwnedShips.ToList())
                {
                    //Added by McShooterz: Remove Privativation stuff due to this being done in GetMaintCost()
                    if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useProportionalUpkeep)
                    {
                        this.totalShipMaintenance += ship.GetMaintCostRealism();
                    }
                    else
                    {
                        this.totalShipMaintenance += ship.GetMaintCost();
                    }
                    //added by gremlin reset border stats.
                    ship.IsInNeutralSpace = false;
                    ship.IsIndangerousSpace = false;

                    ship.IsInFriendlySpace = false;

                }
            },
            () =>
            {
                foreach (Planet planet in this.OwnedPlanets)
                {
                    planet.UpdateOwnedPlanet();
                    this.totalBuildingMaintenance += planet.TotalMaintenanceCostsPerTurn;
                }
            }
            );
            this.totalMaint = this.GetTotalBuildingMaintenance() + this.GetTotalShipMaintenance();
            this.AllTimeMaintTotal += this.totalMaint;
            this.Money += (this.GrossTaxes * this.data.TaxRate) + this.OtherIncome;
            this.Money += this.data.FlatMoneyBonus;
            this.Money += this.TradeMoneyAddedThisTurn;
            this.Money -= this.totalMaint;
        }

        public float EstimateIncomeAtTaxRate(float Rate)
        {
            return this.GrossTaxes * Rate + this.OtherIncome + this.TradeMoneyAddedThisTurn + this.data.FlatMoneyBonus - (this.GetTotalBuildingMaintenance() + this.GetTotalShipMaintenance());
        }
        public float EstimateShipCapacityAtTaxRate(float Rate)
        {
            return this.GrossTaxes * Rate + this.OtherIncome + this.TradeMoneyAddedThisTurn + this.data.FlatMoneyBonus - (this.GetTotalBuildingMaintenance() );
        }

        public float GetActualNetLastTurn()
        {
            return this.Money - this.MoneyLastTurn;
        }

        public float GetAverageNetIncome()
        {
            return (this.GrossTaxes * this.data.TaxRate + (float)this.totalTradeIncome - this.AllTimeMaintTotal) / (float)this.numberForAverage;
        }

        public void UpdateShipsWeCanBuild()
        {
            foreach (KeyValuePair<string, Ship> keyValuePair in ResourceManager.ShipsDict)
            {
                if (keyValuePair.Value.Deleted)
                    continue;
                if (this.WeCanBuildThis(keyValuePair.Key))
                {


                    try
                    {
                        if (!this.structuresWeCanBuild.Contains(keyValuePair.Key) && keyValuePair.Value.Role == "platform" || keyValuePair.Value.Role == "station" && !keyValuePair.Value.shipData.IsShipyard)
                            this.structuresWeCanBuild.Add(keyValuePair.Key);
                        if (!this.ShipsWeCanBuild.Contains(keyValuePair.Key) && !ResourceManager.ShipRoles[keyValuePair.Value.Role].Protected && keyValuePair.Value.Name != "Subspace Projector")
                            this.ShipsWeCanBuild.Add(keyValuePair.Key);
                    }
                    catch (Exception ex)
                    {

                        ex.Data["Ship Key"] = keyValuePair.Key;
                        ex.Data["Role Name"] = keyValuePair.Value.Role;
                        ex.Data["Ship Name"] = keyValuePair.Value.Name;
                        throw ex;
                    }
                }
            }
            if (Empire.universeScreen == null || this != EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
                return;
            Empire.universeScreen.aw.SetDropDowns();
        }

        public float GetTotalBuildingMaintenance()
        {
            return this.totalBuildingMaintenance + this.data.Traits.MaintMod * this.totalBuildingMaintenance;
        }

        public float GetTotalShipMaintenance()
        {
            return this.totalShipMaintenance + this.data.Traits.MaintMod * this.totalShipMaintenance;
        }

        public bool WeCanBuildThis(string ship)
        {
            if (!ResourceManager.ShipsDict.ContainsKey(ship))
                return false;
            ShipData shipData = ResourceManager.ShipsDict[ship].GetShipData();
            if (shipData == null || (!this.UnlockedHullsDict.ContainsKey(shipData.Hull) || !this.UnlockedHullsDict[shipData.Hull]))
                return false;
            foreach (ModuleSlotData moduleSlotData in shipData.ModuleSlotList)
            {
                if (!(moduleSlotData.InstalledModuleUID == "Dummy") && moduleSlotData.InstalledModuleUID != null && !this.UnlockedModulesDict[moduleSlotData.InstalledModuleUID])
                    return false;
            }
            return true;
        }

        public bool WeCanUseThis(Technology tech)
        {

            //foreach(KeyValuePair<string,Ship> ship in ResourceManager.ShipsDict)
            bool flag = false;
            //Parallel.ForEach(ResourceManager.ShipsDict, (ship, status) =>
            foreach(KeyValuePair<string,Ship> ship in ResourceManager.ShipsDict)
            {
                if (flag)
                    break;
                List<Technology> techtree = new List<Technology>();

                ShipData shipData = ship.Value.shipData;
                if (shipData.ShipStyle == null || shipData.ShipStyle == this.data.Traits.ShipType)
                {
                    foreach (ModuleSlotData module in ship.Value.shipData.ModuleSlotList)
                    {
                        if (tech.ModulesUnlocked.Where(uid => uid.ModuleUID == module.InstalledModuleUID).Count() > 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
            }
            return flag;
        }

        public bool WeCanUseThisNow(Technology tech)
        {
            bool flag = false;
            HashSet<string> unlocklist = new HashSet<string>();
            foreach(Technology.UnlockedMod unlocked in tech.ModulesUnlocked)
            {
                unlocklist.Add(unlocked.ModuleUID);
            }
            
            Parallel.ForEach(ResourceManager.ShipsDict, (ship, status) =>
            {
                
                ShipData shipData = ship.Value.shipData;
                if (shipData.ShipStyle == null || shipData.ShipStyle == this.data.Traits.ShipType)
                {
                    if (shipData == null || (!this.UnlockedHullsDict.ContainsKey(shipData.Hull) || !this.UnlockedHullsDict[shipData.Hull]))
                        return;
                    foreach (ModuleSlotData module in ship.Value.shipData.ModuleSlotList)
                    {
                        //if (tech.ModulesUnlocked.Where(uid => uid.ModuleUID == module.InstalledModuleUID).Count() > 0)
                        if(unlocklist.Contains(module.InstalledModuleUID))
                        {
                            flag = true;
                            status.Stop();
                            return;
                        }
                    }
                    if (status.IsStopped)
                        return;
                }
            });
            return flag;
        }

        public float GetTotalPop()
        {
            float num = 0.0f;
            lock (GlobalStats.OwnedPlanetsLock)
            {
                foreach (Planet item_0 in this.OwnedPlanets)
                    num += item_0.Population / 1000f;
            }
            return num;
        }

        public float GetGrossFoodPerTurn()
        {
            float num = 0.0f;
            lock (GlobalStats.OwnedPlanetsLock)
            {
                foreach (Planet item_0 in this.OwnedPlanets)
                    num += item_0.GrossFood;
            }
            return num;
        }

        public int GetAverageTradeIncome()
        {
            if (this.numberForAverage == 0)
                return 0;
            else
                return this.totalTradeIncome / this.numberForAverage;
        }

        public Planet.ColonyType AssessColonyNeeds(Planet p)
        {
            float MineralWealth = 0.0f;
            float PopSupport = 0.0f;
            float ResearchPotential = 0.0f;
            float Fertility = 0.0f;
            float MilitaryPotential = 0.0f;
            if ((double)p.Fertility > 1.0 && this.data.Traits.Cybernetic <= 0)
            {
                ++PopSupport;
                Fertility += p.Fertility;
                ResearchPotential += 0.5f;
            }
            if ((double)p.MineralRichness > 1.0)
            {
                if ((double)p.Fertility > 1.0)
                    ++PopSupport;
                MineralWealth += p.MineralRichness;
                MilitaryPotential += 0.5f;
            }
            if ((double)p.MaxPopulation > 1.0)
            {
                ++ResearchPotential;
                if ((double)p.Fertility > 1.0)
                    ++PopSupport;
                if ((double)p.MaxPopulation > 4.0)
                {
                    ++PopSupport;
                    ++ResearchPotential;
                    if ((double)p.MaxPopulation > 8.0)
                        ++PopSupport;
                    if ((double)p.MaxPopulation > 12.0)
                        PopSupport += 2f;
                }
            }
            int CoreCount = 0;
            int IndustrialCount = 0;
            int AgriculturalCount = 0;
            int MilitaryCount = 0;
            int ResearchCount = 0;
            lock (GlobalStats.OwnedPlanetsLock)
            {
                foreach (Planet item_0 in this.OwnedPlanets)
                {
                    if (item_0.colonyType == Planet.ColonyType.Agricultural)
                        ++AgriculturalCount;
                    if (item_0.colonyType == Planet.ColonyType.Core)
                        ++CoreCount;
                    if (item_0.colonyType == Planet.ColonyType.Industrial)
                        ++IndustrialCount;
                    if (item_0.colonyType == Planet.ColonyType.Research)
                        ++ResearchCount;
                    if (item_0.colonyType == Planet.ColonyType.Military)
                        ++MilitaryCount;
                }
            }
            float AssignedFactor = (float)(CoreCount + IndustrialCount + AgriculturalCount + MilitaryCount + ResearchCount) / ((float)this.OwnedPlanets.Count + 0.01f);
            float CoreDesire = PopSupport + (AssignedFactor - (float)CoreCount) + 2;
            float IndustrialDesire = MineralWealth + (AssignedFactor - (float)IndustrialCount);
            float AgricultureDesire = Fertility + (AssignedFactor - (float)AgriculturalCount);
            float MilitaryDesire = MilitaryPotential + (AssignedFactor - (float)MilitaryCount);
            float ResearchDesire = ResearchPotential + (AssignedFactor - (float)ResearchCount);
            if ((double)CoreDesire > (double)IndustrialDesire && (double)CoreDesire > (double)AgricultureDesire && ((double)CoreDesire > (double)MilitaryDesire && (double)CoreDesire > (double)ResearchDesire))
                return Planet.ColonyType.Core;
            if ((double)IndustrialDesire > (double)CoreDesire && (double)IndustrialDesire > (double)AgricultureDesire && ((double)IndustrialDesire > (double)MilitaryDesire && (double)IndustrialDesire > (double)ResearchDesire))
                return Planet.ColonyType.Industrial;
            if ((double)AgricultureDesire > (double)IndustrialDesire && (double)AgricultureDesire > (double)CoreDesire && ((double)AgricultureDesire > (double)MilitaryDesire && (double)AgricultureDesire > (double)ResearchDesire))
                return Planet.ColonyType.Agricultural;
            return (double)ResearchDesire > (double)CoreDesire && (double)ResearchDesire > (double)AgricultureDesire && ((double)ResearchDesire > (double)MilitaryDesire && (double)ResearchDesire > (double)IndustrialDesire) ? Planet.ColonyType.Research : Planet.ColonyType.Industrial;
        }

        public void ResetBorders()
        {
            this.BorderNodeLocker.EnterWriteLock();
                this.BorderNodes.Clear();
                this.BorderNodeLocker.ExitWriteLock();
            this.SensorNodeLocker.EnterWriteLock();
                this.SensorNodes.Clear();
                this.SensorNodeLocker.ExitWriteLock();
            List<Empire> list = new List<Empire>();
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                if (keyValuePair.Value.Treaty_Alliance)
                    list.Add(keyValuePair.Key);
            }
            try
            {
                foreach (Empire empire in list)
                {
                    List<Planet> tempPlanets = empire.GetPlanets();// new List<Planet>(empire.GetPlanets());
                    for (int index = 0; index < tempPlanets.Count; ++index)
                    {   //loops over all planets by all ALLIED empires
                        Planet planet = tempPlanets[index];
                        if (planet != null)
                        {
                            Empire.InfluenceNode influenceNode1 = new Empire.InfluenceNode();
                            influenceNode1.KeyedObject = (object)planet;
                            influenceNode1.Position = planet.Position;
                            influenceNode1.Radius = 1f; //this.isFaction ? 20000f : Empire.ProjectorRadius + (float)(10000.0 * (double)planet.Population / 1000.0);
                            // influenceNode1.Radius = this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) ? 300000f * this.data.SensorModifier : 600000f * this.data.SensorModifier;
                            this.SensorNodeLocker.EnterWriteLock();
                                this.SensorNodes.Add(influenceNode1);
                                this.SensorNodeLocker.ExitWriteLock();
                            Empire.InfluenceNode influenceNode2 = new Empire.InfluenceNode();
                            influenceNode2.Position = planet.Position;
                            influenceNode2.Radius = this.isFaction ? 1f : (this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) ? 300000f * empire.data.SensorModifier : 600000f * empire.data.SensorModifier);
                            foreach (Building building in planet.BuildingList)
                            {
                                //if (building.IsSensor)
                                if (building.SensorRange * this.data.SensorModifier > influenceNode2.Radius)
                                    influenceNode2.Radius = building.SensorRange * this.data.SensorModifier;
                            }
                            this.SensorNodeLocker.EnterWriteLock();
                                this.SensorNodes.Add(influenceNode2);
                                this.SensorNodeLocker.ExitWriteLock();
                        }
                    }
                        var clonedList = empire.GetShips();// new List<Ship>(empire.GetShips());
                        for (int index = 0; index < clonedList.Count; ++index)
                        {   //loop over all ALLIED ships
                            Ship ship = clonedList[index];
                            if (ship != null)
                            {
                                if (ship.Name == "Subspace Projector")
                                {
                                    Empire.InfluenceNode influenceNode = new Empire.InfluenceNode();
                                    influenceNode.Position = ship.Center;
                                    influenceNode.Radius = Empire.ProjectorRadius;  //projectors currently use their projection radius as sensors
                                    //lock (GlobalStats.SensorNodeLocker)
                                    this.SensorNodeLocker.EnterWriteLock();
                                        this.SensorNodes.Add(influenceNode);
                                        this.SensorNodeLocker.ExitWriteLock();
                                    influenceNode.KeyedObject = (object)ship; //*/
                                    //disabled until we figure out something better for projectors
                                }
                                else
                                {
                                    Empire.InfluenceNode influenceNode = new Empire.InfluenceNode();
                                    influenceNode.Position = ship.Center;
                                    influenceNode.Radius = ship.SensorRange;
                                    this.SensorNodeLocker.EnterWriteLock();
                                        this.SensorNodes.Add(influenceNode);
                                        this.SensorNodeLocker.ExitWriteLock();
                                    influenceNode.KeyedObject = (object)ship;
                                }
                            }
                        }
              

                }
            }
            catch { }
            List<Planet> tempPlanets2 = new List<Planet>(this.GetPlanets());
            foreach (Planet planet in tempPlanets2)
            {   //loop over OWN planets
                Empire.InfluenceNode influenceNode1 = new Empire.InfluenceNode();
                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.usePlanetaryProjection)
                {
                    influenceNode1.KeyedObject = (object)planet;
                    influenceNode1.Position = planet.Position;
                }
                else
                {
                    influenceNode1.KeyedObject = (object)planet.system;
                    influenceNode1.Position = planet.system.Position;
                }
                influenceNode1.Radius = 1f;
                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.usePlanetaryProjection)
                {
                    for (int index = 0; index < planet.BuildingList.Count; ++index)
                    {
                        //if (planet.BuildingList[index].IsProjector)
                        if (influenceNode1.Radius < planet.BuildingList[index].ProjectorRange)
                            influenceNode1.Radius = planet.BuildingList[index].ProjectorRange;
                    }
                }
                else
                {
                    influenceNode1.Radius = this.isFaction ? 20000f : Empire.ProjectorRadius + (float)(10000.0 * (double)planet.Population / 1000.0);
                }
                this.BorderNodeLocker.EnterWriteLock();
                    this.BorderNodes.Add(influenceNode1);
                    this.BorderNodeLocker.ExitWriteLock();
                Empire.InfluenceNode influenceNode2 = new Empire.InfluenceNode();

                influenceNode2.KeyedObject = (object)planet;
                influenceNode2.Position = planet.Position;
                influenceNode2.Radius = 1f; //this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) ? 300000f * this.data.SensorModifier : 600000f * this.data.SensorModifier;
                this.SensorNodeLocker.EnterWriteLock();
                    this.SensorNodes.Add(influenceNode2);
                    this.SensorNodeLocker.ExitWriteLock();
                Empire.InfluenceNode influenceNode3 = new Empire.InfluenceNode();
                influenceNode3.KeyedObject = (object)planet;
                influenceNode3.Position = planet.Position;
                influenceNode3.Radius = this.isFaction ? 1f : 1f * this.data.SensorModifier;
                for (int index = 0; index < planet.BuildingList.Count; ++index)
                {
                    //if (planet.BuildingList[index].IsSensor)
                    if (planet.BuildingList[index].SensorRange * this.data.SensorModifier > influenceNode3.Radius)
                        influenceNode3.Radius = planet.BuildingList[index].SensorRange * this.data.SensorModifier;
                }
                this.SensorNodeLocker.EnterWriteLock();
                    this.SensorNodes.Add(influenceNode3);
                    this.SensorNodeLocker.ExitWriteLock();
            }
            this.SensorNodeLocker.EnterWriteLock();
            foreach (Mole mole in (List<Mole>)this.data.MoleList)   // Moles are spies who have successfuly been planted during 'Infiltrate' type missions, I believe - Doctor
                
                this.SensorNodes.Add(new Empire.InfluenceNode()
                {
                    Position = Empire.universeScreen.PlanetsDict[mole.PlanetGuid].Position,
                    Radius = 100000f * this.data.SensorModifier
                });
            this.SensorNodeLocker.ExitWriteLock();
            this.Inhibitors.Clear();
            for (int index = 0; index < this.OwnedShips.Count; ++index)
            {   //loop over your own ships
                Ship ship = this.OwnedShips[index];
                if (ship != null)
                {
                    if ((double)ship.InhibitionRadius > 0.0)
                        this.Inhibitors.Add(ship);
                    if (ship.Name == "Subspace Projector")
                    {
                        Empire.InfluenceNode influenceNode = new Empire.InfluenceNode();
                        influenceNode.Position = ship.Center;
                        influenceNode.Radius = Empire.ProjectorRadius;  //projectors used as sensors again
                        influenceNode.KeyedObject = (object)ship;
                        this.SensorNodes.Add(influenceNode);
                        this.BorderNodeLocker.EnterWriteLock();
                            this.BorderNodes.Add(influenceNode);
                            this.BorderNodeLocker.ExitWriteLock();
                    }
                    else
                    {
                        Empire.InfluenceNode influenceNode = new Empire.InfluenceNode();
                        influenceNode.Position = ship.Center;
                        influenceNode.Radius = ship.SensorRange;
                        influenceNode.KeyedObject = (object)ship;
                        this.SensorNodeLocker.EnterWriteLock();
                            this.SensorNodes.Add(influenceNode);
                            this.SensorNodeLocker.ExitWriteLock();
                    }
                }
            }
            this.BorderNodeLocker.EnterReadLock();
            {
                foreach (Empire.InfluenceNode item_5 in (List<Empire.InfluenceNode>)this.BorderNodes)
                {
                    foreach (Empire.InfluenceNode item_6 in (List<Empire.InfluenceNode>)this.BorderNodes)
                    {
                        if (item_6.KeyedObject == item_5.KeyedObject && (double)item_6.Radius < (double)item_5.Radius)
                            this.BorderNodes.QueuePendingRemoval(item_6);
                    }
                }
                
            }
            this.BorderNodeLocker.ExitReadLock();
            this.BorderNodeLocker.EnterWriteLock();
            this.BorderNodes.ApplyPendingRemovals();
            this.BorderNodeLocker.ExitWriteLock();
            
        }

        public UniverseScreen GetUS()
        {
            return Empire.universeScreen;
        }

        private void TakeTurn()
        {
            //Added by McShooterz: Home World Elimination game mode
            if (!this.isFaction && !this.data.Defeated && (this.OwnedPlanets.Count == 0 || GlobalStats.EliminationMode && this.Capital != null && this.Capital.Owner != this))
            {
                this.SetAsDefeated();
                if (EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty) == this)
                {
                    Empire.universeScreen.ScreenManager.AddScreen((GameScreen)new YouLoseScreen());
                    return;
                }
                else
                    Empire.universeScreen.NotificationManager.AddEmpireDiedNotification(this);
                return;
            }
            List<Planet> list1 = new List<Planet>();
            foreach (Planet planet in this.OwnedPlanets)
            {
                if (planet.Owner == null)
                    list1.Add(planet);
            }
            foreach (Planet planet in list1)
                this.OwnedPlanets.Remove(planet);
            for (int index = 0; index < this.data.AgentList.Count; ++index)
            {
                if (this.data.AgentList[index].Mission != AgentMission.Defending && this.data.AgentList[index].TurnsRemaining > 0)
                {
                    --this.data.AgentList[index].TurnsRemaining;
                    if (this.data.AgentList[index].TurnsRemaining == 0)
                        this.data.AgentList[index].DoMission(this);
                }
                //Age agents
                this.data.AgentList[index].Age += 0.1f;
                this.data.AgentList[index].ServiceYears += 0.1f;
            }
            this.data.AgentList.ApplyPendingRemovals();
            if ((double)this.Money < 0.0)
            {
                ++this.data.TurnsBelowZero;
            }
            else
            {
                --this.data.TurnsBelowZero;
                if (this.data.TurnsBelowZero < 0)
                    this.data.TurnsBelowZero = 0;
            }
            float MilitaryStrength = 0.0f;
            for (int index = 0; index < this.OwnedShips.Count; ++index)
            {
                Ship ship = this.OwnedShips[index];
                MilitaryStrength += ship.GetStrength();
                if (!this.data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(Empire.universeScreen.StarDate.ToString("#.0")))
                    ++StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this)].ShipCount;
            }
            if (!this.data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(Empire.universeScreen.StarDate.ToString("#.0")))
            {
                StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this)].MilitaryStrength = MilitaryStrength;
                StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this)].TaxRate = this.data.TaxRate;
            }
            if (this.isPlayer)
            {
                if ((double)Empire.universeScreen.StarDate > 1060.0)
                {
#if !DEBUG
                    try
#endif
                    {
                        float num2 = 0.0f;
                        float num3 = 0.0f;
                        List<Empire> list2 = new List<Empire>();
                        foreach (Empire empire in EmpireManager.EmpireList)
                        {
                            if (!empire.isFaction && !empire.data.Defeated && empire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                num2 += (float)empire.TotalScore;
                                if ((double)empire.TotalScore > (double)num3)
                                    num3 = (float)empire.TotalScore;
                                if (empire.data.DiplomaticPersonality.Name == "Aggressive" || empire.data.DiplomaticPersonality.Name == "Ruthless" || empire.data.DiplomaticPersonality.Name == "Xenophobic")
                                    list2.Add(empire);
                            }
                        }
                        float num4 = (float)EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).TotalScore;
                        float num5 = num2 + num4;
                        if ((double)num4 > 0.5 * (double)num5)
                        {
                            if ((double)num4 > (double)num3 * 2.0)
                            {
                                if (list2.Count >= 2)
                                {
                                    Empire biggest = Enumerable.First<Empire>((IEnumerable<Empire>)Enumerable.OrderByDescending<Empire, int>((IEnumerable<Empire>)list2, (Func<Empire, int>)(emp => emp.TotalScore)));
                                    List<Empire> list3 = new List<Empire>();
                                    foreach (Empire empire in list2)
                                    {
                                        if (empire != biggest && empire.GetRelations()[biggest].Known && (double)biggest.TotalScore * 0.660000026226044 > (double)empire.TotalScore)
                                            list3.Add(empire);
                                    }
                                    //Added by McShooterz: prevent AI from automatically merging together
                                    if (list3.Count > 0 && !GlobalStats.preventFederations)
                                    {
                                        IOrderedEnumerable<Empire> orderedEnumerable = Enumerable.OrderByDescending<Empire, float>((IEnumerable<Empire>)list3, (Func<Empire, float>)(emp => biggest.GetRelations()[emp].GetStrength()));
                                        if (!biggest.GetRelations()[Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable)].AtWar)
                                            Ship.universeScreen.NotificationManager.AddPeacefulMergerNotification(biggest, Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable));
                                        else
                                            Ship.universeScreen.NotificationManager.AddSurrendered(biggest, Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable));
                                        biggest.AbsorbEmpire(Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable));
                                    }
                                }
                            }
                        }
                    }
#if !DEBUG
                    catch
#endif
                    {
                    }
                }
                RandomEventManager.UpdateEvents();
                if (this.data.TurnsBelowZero == 5 && (double)this.Money < 0.0)
                    Empire.universeScreen.NotificationManager.AddMoneyWarning();
                bool allEmpiresDead = true;
                foreach (Empire empire in EmpireManager.EmpireList)
                {
                    if (empire.GetPlanets().Count > 0 && !empire.isFaction && !empire.MinorRace && empire != this)
                    {
                        allEmpiresDead = false;
                        break;
                    }
                }
                if (allEmpiresDead)
                {
                    Empire.universeScreen.ScreenManager.AddScreen((GameScreen)new YouWinScreen());
                    return;
                }
                else
                {
                    foreach (Planet planet in this.OwnedPlanets)
                    {
                        if (!this.data.IsRebelFaction)
                            StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this)].Population += planet.Population;
                        if (planet.HasWinBuilding)
                        {
                            Empire.universeScreen.ScreenManager.AddScreen((GameScreen)new YouWinScreen(Localizer.Token(5085)));
                            return;
                        }
                    }
                }
            }
            foreach (Planet planet in this.OwnedPlanets)
            {
                if (!this.data.IsRebelFaction)
                    StatTracker.SnapshotsDict[Empire.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this)].Population += planet.Population;
                int num2 = planet.HasWinBuilding ? 1 : 0;
            }
            if (this.data.TurnsBelowZero >= 25 && this.data.TurnsBelowZero % 25 == 0 && ((double)this.Money < 0.0 && !Empire.universeScreen.Debug) && this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
            {
                if (!this.data.RebellionLaunched)
                {
                    Empire rebelsFromEmpireData = CreatingNewGameScreen.CreateRebelsFromEmpireData(this.data, this);
                    rebelsFromEmpireData.data.IsRebelFaction = true;
                    rebelsFromEmpireData.data.Traits.Name = this.data.RebelName;
                    rebelsFromEmpireData.data.Traits.Singular = this.data.RebelSing;
                    rebelsFromEmpireData.data.Traits.Plural = this.data.RebelPlur;
                    rebelsFromEmpireData.isFaction = true;
                    foreach (Empire key in EmpireManager.EmpireList)
                    {
                        key.GetRelations().Add(rebelsFromEmpireData, new Relationship(rebelsFromEmpireData.data.Traits.Name));
                        rebelsFromEmpireData.GetRelations().Add(key, new Relationship(key.data.Traits.Name));
                    }
                    EmpireManager.EmpireList.Add(rebelsFromEmpireData);
                    this.data.RebellionLaunched = true;
                }
                Empire empireByName = EmpireManager.GetEmpireByName(this.data.RebelName);
                IOrderedEnumerable<Planet> orderedEnumerable = Enumerable.OrderByDescending<Planet, float>((IEnumerable<Planet>)this.OwnedPlanets, (Func<Planet, float>)(planet => Vector2.Distance(this.GetWeightedCenter(), planet.Position)));
                if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable) > 0)
                {
                    Empire.universeScreen.NotificationManager.AddRebellionNotification(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable), empireByName);
                    for (int index = 0; index < 4; ++index)
                    {
                        foreach (KeyValuePair<string, Troop> keyValuePair in ResourceManager.TroopsDict)
                        {
                            if (this.WeCanBuildTroop(keyValuePair.Key))
                            {
                                Troop troop = ResourceManager.CreateTroop(keyValuePair.Value, empireByName);
                                troop.Name = Localizer.Token(empireByName.data.TroopNameIndex);
                                troop.Description = Localizer.Token(empireByName.data.TroopDescriptionIndex);
                                Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable).AssignTroopToTile(troop);
                                break;
                            }
                        }
                    }
                }
            }
            this.CalculateScore();
            //Process technology research
            //Parallel.Invoke(
            //    ()=>
                {
                    if (this.ResearchTopic != "")
                    {
                        foreach (Planet planet in this.OwnedPlanets)
                            this.Research += planet.NetResearchPerTurn;
                        TechEntry tech;
                        if (this.TechnologyDict.TryGetValue(this.ResearchTopic, out tech))
                        {
                            if (tech.GetTech().Cost * UniverseScreen.GamePaceStatic - tech.Progress > this.Research)
                            {
                                tech.Progress += this.Research;
                                this.Research = 0f;
                            }
                            else
                            {
                                this.Research -= tech.GetTech().Cost * UniverseScreen.GamePaceStatic - tech.Progress;
                                tech.Progress = tech.GetTech().Cost * UniverseScreen.GamePaceStatic;
                                this.UnlockTech(this.ResearchTopic);
                                if (this.isPlayer)
                                    Empire.universeScreen.NotificationManager.AddResearchComplete(this.ResearchTopic, this);
                                this.data.ResearchQueue.Remove(this.ResearchTopic);
                                if (this.data.ResearchQueue.Count > 0)
                                {
                                    this.ResearchTopic = this.data.ResearchQueue[0];
                                    this.data.ResearchQueue.RemoveAt(0);
                                }
                                else
                                    this.ResearchTopic = "";
                            }
                        }
                    }
                    else if (this.data.ResearchQueue.Count > 0)
                        this.ResearchTopic = this.data.ResearchQueue[0];
                    
                }//,

            //    ()=>
                {

                    if (!this.isFaction && this != EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
                        this.UpdateRelationships();
                    else if (this == EmpireManager.GetEmpireByName(Empire.universeScreen.PlayerLoyalty))
                    {
                        foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
                            keyValuePair.Value.UpdatePlayerRelations(this, keyValuePair.Key);
                    }

                    if (this.isFaction)
                        this.GSAI.FactionUpdate();
                    else if (!this.data.Defeated)
                        this.GSAI.Update();
                    if ((double)this.Money > (double)this.data.CounterIntelligenceBudget)
                    {
                        this.Money -= this.data.CounterIntelligenceBudget;
                        foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
                        {
                            keyValuePair.Key.GetRelations()[this].IntelligencePenetration -= this.data.CounterIntelligenceBudget / 10f;
                            if ((double)keyValuePair.Key.GetRelations()[this].IntelligencePenetration < 0.0)
                                keyValuePair.Key.GetRelations()[this].IntelligencePenetration = 0.0f;
                        }
                    } 
                }//);
                //()=>
                {
                    if (this.isFaction || this.MinorRace)
                        return;
                    if (!this.isPlayer)
                    {
                        //Parallel.Invoke(
                        //  ()  =>
                        {
                        this.AssessFreighterNeeds();
                        }//,
                        //()=>
                        {
                        this.AssignExplorationTasks();
                        }
                        //);
                    }
                    else
                    {
                        if (this.AutoFreighters)
                            this.AssessFreighterNeeds();
                        if (this.AutoExplore)
                            this.AssignExplorationTasks();
                    }  
                }
            
             return;
        }

        private void UpdateRelationships()
        {
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                if (keyValuePair.Value.Known)
                    keyValuePair.Value.UpdateRelationship(this, keyValuePair.Key);
            }
        }

        private void CalculateScore()
        {
            this.TotalScore = 0;
            this.TechScore = 0.0f;
            this.IndustrialScore = 0.0f;
            this.ExpansionScore = 0.0f;
            foreach (KeyValuePair<string, TechEntry> keyValuePair in this.TechnologyDict)
            {
                if (keyValuePair.Value.Unlocked)
                    this.TechScore += (float)((int)ResourceManager.TechTree[keyValuePair.Key].Cost / 100);
            }
            foreach (Planet planet in this.OwnedPlanets)
            {
                this.ExpansionScore += (float)((double)planet.Fertility + (double)planet.MineralRichness + (double)planet.Population / 1000.0);
                foreach (Building building in planet.BuildingList)
                    this.IndustrialScore += building.Cost / 20f;
            }
            this.currentMilitaryStrength =0;
            for (int index = 0; index < this.OwnedShips.Count; ++index)
            {
                Ship ship = this.OwnedShips[index];
                if (ship != null)
                {
                    
                    
                    this.currentMilitaryStrength += ship.GetStrength();
                }
                
            }
            this.data.MilitaryScoreTotal += this.currentMilitaryStrength;
            this.TotalScore = (int)((double)this.MilitaryScore / 100.0 + (double)this.IndustrialScore + (double)this.TechScore + (double)this.ExpansionScore);
            this.MilitaryScore = this.data.MilitaryScoreTotal / (float)this.data.ScoreAverage;
            ++this.data.ScoreAverage;
        }

        public void AbsorbEmpire(Empire target)
        {
            foreach (Planet planet in target.GetPlanets())
            {
                this.OwnedPlanets.Add(planet);
                planet.Owner = (Empire)null;
                planet.Owner = this;
                if (!planet.system.OwnerList.Contains(this))
                {
                    planet.system.OwnerList.Add(this);
                    planet.system.OwnerList.Remove(target);
                }
            }
            foreach (KeyValuePair<Guid, SolarSystem> keyValuePair in Empire.universeScreen.SolarSystemDict)
            {
                foreach (Planet planet in keyValuePair.Value.PlanetList)
                {
                    foreach (Troop troop in planet.TroopsHere)
                    {
                        if (troop.GetOwner() == target)
                            troop.SetOwner(this);
                    }
                }
            }
            target.GetPlanets().Clear();
            foreach (Ship ship in (List<Ship>)target.GetShips())
            {
                this.OwnedShips.Add(ship);
                ship.loyalty = this;
                ship.fleet = (Fleet)null;
                ship.GetAI().State = AIState.AwaitingOrders;
                ship.GetAI().OrderQueue.Clear();
            }
            target.GetShips().Clear();
            foreach (KeyValuePair<string, TechEntry> keyValuePair in target.GetTDict())
            {
                if (keyValuePair.Value.Unlocked && !this.TechnologyDict[keyValuePair.Key].Unlocked)
                    this.UnlockTech(keyValuePair.Key);
            }
            foreach (KeyValuePair<string, bool> keyValuePair in target.GetHDict())
            {
                if (keyValuePair.Value)
                    this.UnlockedHullsDict[keyValuePair.Key] = true;
            }
            foreach (KeyValuePair<string, bool> keyValuePair in target.GetTrDict())
            {
                if (keyValuePair.Value)
                    this.UnlockedTroopDict[keyValuePair.Key] = true;
            }
            foreach (Artifact artifact in target.data.OwnedArtifacts)
            {
                this.data.OwnedArtifacts.Add(artifact);
                this.AddArtifact(artifact);
            }
            target.data.OwnedArtifacts.Clear();
            if ((double)target.Money > 0.0)
            {
                this.Money += target.Money;
                target.Money = 0.0f;
            }
            target.SetAsMerged();
            this.ResetBorders();
            this.UpdateShipsWeCanBuild();
            if (this != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
            {
                this.data.difficulty = Difficulty.Brutal;
                lock (GlobalStats.TaskLocker)
                {
                    foreach (MilitaryTask item_7 in (List<MilitaryTask>)this.GSAI.TaskList)
                        item_7.EndTask();
                    this.GSAI.TaskList.ApplyPendingRemovals();
                }
                this.GSAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                this.GSAI.DefensiveCoordinator.DefenseDict.Clear();
                this.ForcePool.Clear();
                //foreach (Ship s in (List<Ship>)this.OwnedShips) //.OrderByDescending(experience=> experience.experience).ThenBy(strength=> strength.BaseStrength))
                for (int i = 0; i < this.OwnedShips.Count; i++)
                {
                    Ship s = ((List<Ship>)this.OwnedShips)[i];
                    //added by gremlin Do not include 0 strength ships in defensive force pool

                    s.GetAI().OrderQueue.Clear();
                    s.GetAI().State = AIState.AwaitingOrders;
                    this.ForcePoolAdd(s);
                }
                if (this.data.Traits.Cybernetic != 0)
                {
                    foreach (Planet planet in this.OwnedPlanets)
                    {
                        List<Building> list = new List<Building>();
                        foreach (Building building in planet.BuildingList)
                        {
                            if ((double)building.PlusFlatFoodAmount > 0.0 || (double)building.PlusFoodPerColonist > 0.0 || (double)building.PlusTerraformPoints > 0.0)
                                list.Add(building);
                        }
                        foreach (Building b in list)
                            planet.ScrapBuilding(b);
                    }
                }
            }
            foreach (Agent agent in (List<Agent>)target.data.AgentList)
            {
                this.data.AgentList.Add(agent);
                agent.Mission = AgentMission.Defending;
                agent.TargetEmpire = (string)null;
            }
            target.data.AgentList.Clear();
            target.data.AbsorbedBy = this.data.Traits.Name;
            this.CalculateScore();
        }

        private void SystemDefensePlanner(SolarSystem system)
        {
        }

        public void ForcePoolAdd(Ship s)
        {
            if (s.Role == "station" || s.Role == "freighter" || (s.Role == "scout" || s.Role == "platform") || (s.fleet != null || s.Role == "construction" || s.Role == "supply"))
                return;
            this.GSAI.AssignShipToForce(s);
        }

        public void ForcePoolRemove(Ship s)
        {
            this.ForcePool.Remove(s);
        }

        public BatchRemovalCollection<Ship> GetForcePool()
        {
            return this.ForcePool;
        }

        public float GetForcePoolStrength()
        {
            float num = 0.0f;
            foreach (Ship ship in (List<Ship>)this.ForcePool)
                num += ship.GetStrength();
            return num;
        }

        public string GetPreReq(string techID)
        {
            foreach (KeyValuePair<string, TechEntry> keyValuePair in this.TechnologyDict)
            {
                foreach (Technology.LeadsToTech leadsToTech in ResourceManager.TechTree[keyValuePair.Key].LeadsTo)
                {
                    if (leadsToTech.UID == techID)
                        return keyValuePair.Key;
                }
            }
            return "";
        }

        public bool HavePreReq(string techID)
        {
            if (ResourceManager.TechTree[techID].RootNode == 1)
                return true;
            foreach (KeyValuePair<string, TechEntry> keyValuePair in this.TechnologyDict)
            {
                if (keyValuePair.Value.Unlocked)
                {
                    foreach (Technology.LeadsToTech leadsToTech in ResourceManager.TechTree[keyValuePair.Key].LeadsTo)
                    {
                        if (leadsToTech.UID == techID)
                            return true;
                    }
                }
            }
            return false;
        }

        //Added by McShooterz: Change to freighter needs logic
        //modfied by gremlin to try not to use 
        private void AssessFreighterNeeds()
        {
            int tradeShips = 0;
            int passengerShips = 0;
            int freighterLimit = (this.OwnedPlanets.Count() * 2 > GlobalStats.freighterlimit ? (int)GlobalStats.freighterlimit : this.OwnedPlanets.Count() * 2);
            int TradeLimit = (int)(freighterLimit * 0.8f);
            int PassLimit = freighterLimit - TradeLimit;
            List<Ship> unusedFreighters = new List<Ship>();
            List<Ship> assignedShips = new List<Ship>();
            //foreach (Ship ship in (List<Ship>)this.OwnedShips)
            for (int x = 0; x < this.OwnedShips.Count; x++)
            {
                Ship ship;
                try
                {
                    ship = this.OwnedShips[x];
                }
                catch
                {
                    continue;
                }
                if (ship == null)
                    continue;
                if (ship.Role != "freighter" || ship.isColonyShip || ship.CargoSpace_Max == 0 || ship.GetAI() == null)
                    continue;
                if (ship.GetAI().State == AIState.SystemTrader)
                {
                    if (tradeShips < TradeLimit)
                        tradeShips++;
                    else
                        ship.GetAI().OrderScrapShip();
                }
                else if (ship.GetAI().State == AIState.PassengerTransport)
                {
                    if (passengerShips < PassLimit)
                        passengerShips++;
                    else
                        ship.GetAI().OrderScrapShip();
                }
                else if (ship.GetAI().State != AIState.Refit && ship.GetAI().State != AIState.Scrap)
                    unusedFreighters.Add(ship);
            }
            //get number of freighters being built
            foreach (Goal goal in (List<Goal>)this.GSAI.Goals)
            {
                if (goal.GoalName == "IncreaseFreighters")
                    ++tradeShips;
                else if (goal.GoalName == "IncreasePassengerShips")
                    ++passengerShips;
            }
            int doesntHelp = 0;
            int extraFrieghters =0;
            //foreach (Planet needs in this.GetPlanets())
            //{
            //    if (needs.fs == Planet.GoodState.IMPORT && needs.FoodHere > needs.MAX_STORAGE * .7f
            //        || needs.ps == Planet.GoodState.IMPORT && needs.ProductionHere > needs.MAX_STORAGE * .7f)
            //        moreFrieghters++;
            //    else
            //        doesntHelp++;
                    
            

            //}
            //if (doesntHelp < moreFrieghters)
            //    moreFrieghters = doesntHelp;


            if (tradeShips < TradeLimit )
            {
                //Do trade ships
                foreach (Ship ship in unusedFreighters)
                {
                    if (tradeShips > TradeLimit)
                        break;
                    if (ship.GetAI().State != AIState.Flee)
                    {
                        ship.GetAI().OrderTrade();
                    }
                    assignedShips.Add(ship);
                    ++tradeShips;
                }
                foreach (Ship ship in assignedShips)
                    unusedFreighters.Remove(ship);
                assignedShips.Clear();

                extraFrieghters = unusedFreighters.Count;
                if(unusedFreighters.Count ==0)
                //for (; tradeShips < TradeLimit; ++tradeShips)
                    this.GSAI.Goals.Add(new Goal(this)
                    {
                        GoalName = "IncreaseFreighters",
                        type = GoalType.BuildShips
                    });          
            }
            
            if (passengerShips < PassLimit)
            {
                //Do passenger ships
                foreach (Ship ship in unusedFreighters)
                {
                    if (passengerShips > PassLimit)
                        break;
                    ship.GetAI().OrderTransportPassengers();
                    assignedShips.Add(ship);
                    ++passengerShips;
                }
                foreach (Ship ship in assignedShips)
                    unusedFreighters.Remove(ship);
                assignedShips.Clear();
                if(unusedFreighters.Count ==0)
                //for (; passengerShips < PassLimit; ++passengerShips)
                    this.GSAI.Goals.Add(new Goal(this)
                    {
                        type = GoalType.BuildShips,
                        GoalName = "IncreasePassengerShips"
                    });
            }
            foreach (Ship ship in unusedFreighters)
                ship.GetAI().OrderTrade();
        }

        public void ReportGoalComplete(Goal g)
        {
            for (int index = 0; index < this.GSAI.Goals.Count; ++index)
            {
                if (this.GSAI.Goals[index] == g)
                    this.GSAI.Goals.QueuePendingRemoval(this.GSAI.Goals[index]);
            }
        }

        public GSAI GetGSAI()
        {
            return this.GSAI;
        }

        public Vector2 GetWeightedCenter()
        {
            int num = 0;
            Vector2 vector2 = new Vector2();
            foreach (Planet planet in this.OwnedPlanets)
            {
                for (int index = 0; (double)index < (double)planet.Population / 1000.0; ++index)
                {
                    ++num;
                    vector2 += planet.Position;
                }
            }
            if (num == 0)
                num = 1;
            return vector2 / (float)num;
        }

        private void AssignExplorationTasks()
        {
            bool flag1 = false;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (!solarSystem.ExploredDict[this])
                {
                    flag1 = true;
                    break;
                }
            }
            int num = 0;
            if (flag1)
            {
                foreach (Ship ship in (List<Ship>)this.OwnedShips)
                {
                    if (num < 2)
                    {
                        if (ship.Role == "scout" && !ship.isPlayerShip())
                        {
                            ship.DoExplore();
                            ++num;
                        }
                    }
                    else
                        break;
                }
                if (num == 0)
                {
                    bool flag2 = true;
                    foreach (Goal goal in (List<Goal>)this.GSAI.Goals)
                    {
                        if (goal.type == GoalType.BuildScout)
                        {
                            flag2 = false;
                            break;
                        }
                    }
                    if (!flag2)
                        return;
                    Goal goal1 = new Goal();
                    goal1.type = GoalType.BuildScout;
                    goal1.empire = this;
                    goal1.GoalName = "Build Scout";
                    this.GSAI.Goals.Add(goal1);
                    this.GSAI.Goals.Add(goal1);
                }
                else
                {
                    if (num >= 2 || this.data.DiplomaticPersonality == null)
                        return;
                    bool flag2 = true;
                    foreach (Goal goal in (List<Goal>)this.GSAI.Goals)
                    {
                        if (goal.type == GoalType.BuildScout)
                        {
                            flag2 = false;
                            break;
                        }
                    }
                    if (flag2)
                        this.GSAI.Goals.Add(new Goal()
                        {
                            type = GoalType.BuildScout,
                            empire = this,
                            GoalName = "Build Scout"
                        });
                    if (!(this.data.DiplomaticPersonality.Name == "Expansionist") || !flag2)
                        return;
                    this.GSAI.Goals.Add(new Goal()
                    {
                        type = GoalType.BuildScout,
                        empire = this,
                        GoalName = "Build Scout"
                    });
                }
            }
            else
            {
                foreach (Ship ship in (List<Ship>)this.OwnedShips)
                {
                    if (ship.GetAI().State == AIState.Explore)
                        ship.GetAI().OrderRebaseToNearest();
                }
            }
        }

        public void AddArtifact(Artifact art)
        {
            this.data.OwnedArtifacts.Add(art);
            if (art.DiplomacyMod > 0f)
            {
                this.data.Traits.DiplomacyMod += (art.DiplomacyMod + art.DiplomacyMod * this.data.Traits.Spiritual);
            }
            if (art.FertilityMod > 0f)
            {
                this.data.EmpireFertilityBonus += art.FertilityMod;
                foreach (Planet planet in this.GetPlanets())
                {
                    planet.Fertility += (art.FertilityMod + art.FertilityMod * this.data.Traits.Spiritual);
                }
            }
            if (art.GroundCombatMod > 0f)
            {
                this.data.Traits.GroundCombatModifier += (art.GroundCombatMod + art.GroundCombatMod * this.data.Traits.Spiritual);
            }
            if (art.ModuleHPMod > 0f)
            {
                this.data.Traits.ModHpModifier += (art.ModuleHPMod + art.ModuleHPMod * this.data.Traits.Spiritual);
            }
            if (art.PlusFlatMoney > 0f)
            {
                this.data.FlatMoneyBonus += (art.PlusFlatMoney + art.PlusFlatMoney * this.data.Traits.Spiritual);
            }
            if (art.ProductionMod > 0f)
            {
                this.data.Traits.ProductionMod += (art.ProductionMod + art.ProductionMod * this.data.Traits.Spiritual);
            }
            if (art.ReproductionMod > 0f)
            {
                this.data.Traits.ReproductionMod += (art.ReproductionMod + art.ReproductionMod * this.data.Traits.Spiritual);
            }
            if (art.ResearchMod > 0f)
            {
                this.data.Traits.ResearchMod += (art.ResearchMod + art.ResearchMod * this.data.Traits.Spiritual);
            }
            if (art.SensorMod > 0f)
            {
                this.data.SensorModifier += (art.SensorMod + art.SensorMod * this.data.Traits.Spiritual);
            }
            if (art.ShieldPenBonus > 0f)
            {
                this.data.ShieldPenBonusChance += (art.ShieldPenBonus + art.ShieldPenBonus * this.data.Traits.Spiritual);
            }
        }

        public void RemoveArtifact(Artifact art)
		{
			this.data.OwnedArtifacts.Remove(art);
            if (art.DiplomacyMod > 0f)
            {
                this.data.Traits.DiplomacyMod -= (art.DiplomacyMod + art.DiplomacyMod * this.data.Traits.Spiritual);
            }
            if (art.FertilityMod > 0f)
            {
                this.data.EmpireFertilityBonus -= art.FertilityMod;
                foreach (Planet planet in this.GetPlanets())
                {
                    planet.Fertility -= (art.FertilityMod + art.FertilityMod * this.data.Traits.Spiritual);
                }
            }
            if (art.GroundCombatMod > 0f)
            {
                this.data.Traits.GroundCombatModifier -= (art.GroundCombatMod + art.GroundCombatMod * this.data.Traits.Spiritual);
            }
            if (art.ModuleHPMod > 0f)
            {
                this.data.Traits.ModHpModifier -= (art.ModuleHPMod + art.ModuleHPMod * this.data.Traits.Spiritual);
            }
            if (art.PlusFlatMoney > 0f)
            {
                this.data.FlatMoneyBonus -= (art.PlusFlatMoney + art.PlusFlatMoney * this.data.Traits.Spiritual);
            }
            if (art.ProductionMod > 0f)
            {
                this.data.Traits.ProductionMod -= (art.ProductionMod + art.ProductionMod * this.data.Traits.Spiritual);
            }
            if (art.ReproductionMod > 0f)
            {
                this.data.Traits.ReproductionMod -= (art.ReproductionMod + art.ReproductionMod * this.data.Traits.Spiritual);
            }
            if (art.ResearchMod > 0f)
            {
                this.data.Traits.ResearchMod -= (art.ResearchMod + art.ResearchMod * this.data.Traits.Spiritual);
            }
            if (art.SensorMod > 0f)
            {
                this.data.SensorModifier -= (art.SensorMod + art.SensorMod * this.data.Traits.Spiritual);
            }
            if (art.ShieldPenBonus > 0f)
            {
                this.data.ShieldPenBonusChance -= (art.ShieldPenBonus + art.ShieldPenBonus * this.data.Traits.Spiritual);
            }
        }

        private void DeviseExpansionGoal()
        {
        }

        public void RemoveShip(Ship ship)
        {
            this.OwnedShips.QueuePendingRemoval(ship);
            this.OwnedShips.ApplyPendingRemovals();
        }

        //private List<Ship> ShipsInOurBorders()
        //{
        //    return this.GetGSAI().ThreatMatrix.GetAllShipsInOurBorders();
        ////}
        public class InfluenceNode
        {
            public Vector2 Position;
            public object KeyedObject;
            public bool DrewThisTurn;
            public float Radius;
        }

        
    }
}
