// Type: Ship_Game.Empire
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class Empire : IDisposable
    {
        public static float ProjectorRadius = 150000f;
        //private Map<int, Fleet> FleetsDict = new Map<int, Fleet>();
        private readonly ConcurrentDictionary<int, Fleet> FleetsDict    = new ConcurrentDictionary<int, Fleet>();
        private readonly Map<string, bool> UnlockedHullsDict     = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedTroopDict     = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedBuildingsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedModulesDict   = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        public Map<string, TechEntry> TechnologyDict = new Map<string, TechEntry>(StringComparer.InvariantCultureIgnoreCase);
        public Array<Ship>      Inhibitors     = new Array<Ship>();
        public Array<Ship>      ShipsToAdd     = new Array<Ship>();
        public Array<SpaceRoad> SpaceRoadsList = new Array<SpaceRoad>();
        public float Money = 1000f;
        private BatchRemovalCollection<Planet>      OwnedPlanets      = new BatchRemovalCollection<Planet>();
        private BatchRemovalCollection<SolarSystem> OwnedSolarSystems = new BatchRemovalCollection<SolarSystem>();
        private BatchRemovalCollection<Ship>        OwnedProjectors   = new BatchRemovalCollection<Ship>();
        private BatchRemovalCollection<Ship>          OwnedShips  = new BatchRemovalCollection<Ship>();
        public  BatchRemovalCollection<Ship>          KnownShips  = new BatchRemovalCollection<Ship>();
        public  BatchRemovalCollection<InfluenceNode> BorderNodes = new BatchRemovalCollection<InfluenceNode>();
        public  BatchRemovalCollection<InfluenceNode> SensorNodes = new BatchRemovalCollection<InfluenceNode>();
        private readonly Map<SolarSystem, bool>  HostilesPresent = new Map<SolarSystem, bool>();
        private readonly Map<Empire, Relationship> Relationships = new Map<Empire, Relationship>();
        public HashSet<string> ShipsWeCanBuild      = new HashSet<string>();
        public HashSet<string> structuresWeCanBuild = new HashSet<string>();
        private float FleetUpdateTimer = 5f;
        private int numberForAverage = 1;
        //public int ColonizationGoalCount = 2;          //Not referenced in code, removing to save memory
        public string ResearchTopic = "";
        //private Array<War> Wars = new Array<War>();          //Not referenced in code, removing to save memory
        private Fleet DefensiveFleet = new Fleet();
        private BatchRemovalCollection<Ship> ForcePool = new BatchRemovalCollection<Ship>();
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;
        public bool isFaction;
        public bool MinorRace;
        public float Research;
        public Color EmpireColor;
        public static UniverseScreen Universe;
        //public Vector4 VColor;          //Not referenced in code, removing to save memory
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
        //public float DisplayIncome;          //Not referenced in code, removing to save memory
        //public float ActualNetLastTurn;          //Not referenced in code, removing to save memory
        public float TradeMoneyAddedThisTurn;
        public float MoneyLastTurn;
        public int totalTradeIncome;
        public bool AutoBuild;
        public bool AutoExplore;
        public bool AutoColonize;
        public bool AutoFreighters;
        public bool AutoResearch;
        public int TotalScore;
        public float TechScore;
        public float ExpansionScore;
        public float MilitaryScore;
        public float IndustrialScore;
        //public float SensorRange;          //Not referenced in code, removing to save memory
        //public bool IsSensor;          //Not referenced in code, removing to save memory
        //private float desiredForceStrength;
        public Planet Capital;
        public int EmpireShipCountReserve;
        public int empireShipTotal;
        public int empireShipCombat;    //fbedard
        public int empirePlanetCombat;  //fbedard
        public bool canBuildCapitals;
        public bool canBuildCruisers;
        public bool canBuildFrigates;
        public bool canBuildCorvettes;
        public bool canBuildCarriers;
        public bool canBuildBombers;
        public bool canBuildTroopShips;
        public float currentMilitaryStrength;
        public float freighterBudget;
        public bool RecalculateMaxHP;       //Added by Gretman, since the +ModHpModifier stuff wasn't retroactive.
        public float cargoNeed = 0;
        //[XmlIgnore][ScriptIgnore]
        //private Map<string, bool> UnlockAbleDesigns = new Map<string, bool>
        //adding for thread safe Dispose because class uses unmanaged resources 

        public HashSet<string> ShipTechs = new HashSet<string>();
        //added by gremlin
        float leftoverResearch;
        public float exportPTrack;
        public float exportFTrack;
        public float averagePLanetStorage;
        [XmlIgnore]
        public Map<Point, Map<Point, PatchCacheEntry>> PathCache = new Map<Point, Map<Point, PatchCacheEntry>>();
        //public Map<Array<Vector2>, int> pathcache = new Map<Array<Vector2>, int>();
        [XmlIgnore]
        public ReaderWriterLockSlim LockPatchCache = new ReaderWriterLockSlim();
        [XmlIgnore]
        public int pathcacheMiss = 0;
        [XmlIgnore]
        public byte[,] grid;
        [XmlIgnore]
        public int granularity = 0;

        public Empire()
        {
        }
        public class PatchCacheEntry
        {
            public readonly Array<Vector2> Path;
            public int CacheHits;
            public PatchCacheEntry(Array<Vector2> path) { Path = path; }
        }
        public ConcurrentDictionary<int, Fleet> GetFleetsDict()
        {
            return FleetsDict;
        }
        public Fleet FirstFleet
        {
            get { return FleetsDict[1]; }
            set
            {
                foreach (Ship s in FleetsDict[1].Ships) s.fleet = null;
                FleetsDict[1] = value;
            }
        }

        public int GetUnusedKeyForFleet()
        {
            int key = 0;
            while (FleetsDict.ContainsKey(key))
                ++key;
            return key;
        }

        public float GetPopulation()
        {
            float pop = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet p in OwnedPlanets)
                    pop += p.Population;
            return pop / 1000f;
        }

        public void CleanOut()
        {
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
            OwnedShips.Clear();
            Relationships.Clear();
            GSAI = null;
            HostilesPresent.Clear();
            ForcePool.Clear();
            KnownShips.Clear();
            SensorNodes.Clear();
            BorderNodes.Clear();
            TechnologyDict.Clear();
            SpaceRoadsList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Ships.Clear();
            FleetsDict.Clear();
            UnlockedBuildingsDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedTroopDict.Clear();
            Inhibitors.Clear();
            OwnedProjectors.Clear();
            ShipsToAdd.Clear();
            ShipsWeCanBuild.Clear();
            structuresWeCanBuild.Clear();
            data.MoleList.Clear();
            data.OwnedArtifacts.Clear();
            data.AgentList.Clear();
        }

        public void SetAsDefeated()
        {
            if (data.Defeated)
                return;
            data.Defeated = true;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                solarSystem.OwnerList.Remove(this);

            BorderNodes.Clear();
            SensorNodes.Clear();
            if (isFaction)
                return;

            foreach (var kv in Relationships)
            {
                kv.Value.AtWar              = false;
                kv.Value.Treaty_Alliance    = false;
                kv.Value.Treaty_NAPact      = false;
                kv.Value.Treaty_OpenBorders = false;
                kv.Value.Treaty_Peace       = false;
                kv.Value.Treaty_Trade       = false;
                var relation = kv.Key.GetRelations(this);
                relation.AtWar              = false;
                relation.Treaty_Alliance    = false;
                relation.Treaty_NAPact      = false;
                relation.Treaty_OpenBorders = false;
                relation.Treaty_Peace       = false;
                relation.Treaty_Trade       = false;
            }
            foreach (Ship ship in OwnedShips)
            {
                ship.GetAI().OrderQueue.Clear();
                ship.GetAI().State = AIState.AwaitingOrders;
            }
            GSAI.Goals.Clear();
            GSAI.TaskList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(data, this);
            rebels.data.Traits.Name     = data.Traits.Singular + " Remnant";
            rebels.data.Traits.Singular = data.Traits.Singular;
            rebels.data.Traits.Plural   = data.Traits.Plural;
            rebels.isFaction = true;
            foreach (Empire key in EmpireManager.Empires)
            {
                key.AddRelation(rebels);
                rebels.AddRelation(key);                
                
            }
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                solarSystem.ExploredDict.Add(rebels, false);
                foreach (Planet planet in solarSystem.PlanetList)
                    planet.ExploredDict.Add(rebels, false);
            }
            EmpireManager.Add(rebels);
            data.RebellionLaunched = true;
            StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")].Add(EmpireManager.Empires.IndexOf(rebels), new Snapshot(Universe.StarDate));
            foreach (Ship s in OwnedShips)
            {
                s.loyalty = rebels;
                rebels.AddShip(s);
            }
            OwnedShips.Clear();
            data.AgentList.Clear();
        }

        public void SetAsMerged()
        {
            if (data.Defeated)
                return;
            data.Defeated = true;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                solarSystem.OwnerList.Remove(this);

            BorderNodes.Clear();
            SensorNodes.Clear();
            if (isFaction)
                return;

            foreach (var relation in Relationships)
            {
                relation.Value.AtWar              = false;
                relation.Value.Treaty_Alliance    = false;
                relation.Value.Treaty_NAPact      = false;
                relation.Value.Treaty_OpenBorders = false;
                relation.Value.Treaty_Peace       = false;
                relation.Value.Treaty_Trade       = false;
                var relationWithUs = relation.Key.GetRelations(this);
                relationWithUs.AtWar              = false;
                relationWithUs.Treaty_Alliance    = false;
                relationWithUs.Treaty_NAPact      = false;
                relationWithUs.Treaty_OpenBorders = false;
                relationWithUs.Treaty_Peace       = false;
                relationWithUs.Treaty_Trade       = false;
            }
            foreach (Ship ship in OwnedShips)
            {
                ship.GetAI().OrderQueue.Clear();
                ship.GetAI().State = AIState.AwaitingOrders;
            }
            GSAI.Goals.Clear();
            GSAI.TaskList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            OwnedShips.Clear();
            data.AgentList.Clear();
        }

        // @todo This uses an expensive linear search. Consider using a QuadTree in the future
        public bool IsPointInBorders(Vector2 point)
        {
            using (BorderNodes.AcquireReadLock())
                foreach (var node in BorderNodes)
                    if (node.Position.InRadius(point, node.Radius))
                        return true;
            return false;
        }

        public bool IsPointInSensors(Vector2 point)
        {
            using (BorderNodes.AcquireReadLock())
                foreach (var node in SensorNodes)
                    if (node.Position.InRadius(point, node.Radius))
                        return true;
            return false;
        }

        public Map<string, bool> GetHDict()
        {
            return UnlockedHullsDict;
        }

        public bool IsHullUnlocked(string hullName)
        {
            return UnlockedHullsDict.TryGetValue(hullName, out bool unlocked) && unlocked;
        }

        public Map<string, bool> GetTrDict()
        {
            return UnlockedTroopDict;
        }

        public Map<string, bool> GetBDict()
        {
            return UnlockedBuildingsDict;
        }

        public Map<string, bool> GetMDict()
        {
            return UnlockedModulesDict;
        }

        public Map<string, TechEntry> GetTDict()
        {
            return TechnologyDict;
        }

        public float GetProjectedResearchNextTurn()
        {
            float num = 0.0f;
            foreach (Planet planet in OwnedPlanets)
                num += planet.NetResearchPerTurn;
            return num;
        }

        public IReadOnlyList<SolarSystem> GetOwnedSystems()
        {
            return OwnedSolarSystems;
        }

        public IReadOnlyList<Planet> GetPlanets()
        {
            return OwnedPlanets;
        }

        public int NumPlanets => OwnedPlanets.Count;

        public void UpdatePlanetIncomes()
        {
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets) planet.UpdateIncomes(false);
        }

        public void RemovePlanet(Planet planet)
        {
            OwnedPlanets.Remove(planet);
            if (OwnedPlanets.All(p => p.system != planet.system)) // system no more in owned planets?
            {
                OwnedSolarSystems.Remove(planet.system);
            }
        }

        public void ClearAllPlanets()
        {
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
        }

        public void AddPlanet(Planet planet)
        {
            //lock (GlobalStats.OwnedPlanetsLock)
            if (planet == null)
                throw new ArgumentNullException(nameof(planet));

            OwnedPlanets.Add(planet);
            if (planet.system == null)
                throw new ArgumentNullException(nameof(planet.system));

            if (!OwnedSolarSystems.Contains(planet.system))
                OwnedSolarSystems.Add(planet.system);
        }

        public void AddTradeMoney(float howMuch)
        {
            TradeMoneyAddedThisTurn += howMuch;
            totalTradeIncome        += (int)howMuch;
        }

        public BatchRemovalCollection<Ship> GetShips()
        {
            return OwnedShips;
        }

        public BatchRemovalCollection<Ship> GetProjectors()
        {
            return OwnedProjectors;
        }

        public void AddShip(Ship s)
        {
            if (s.Name == "Subspace Projector")
                OwnedProjectors.Add(s);
            else
                OwnedShips.Add(s);
        }

        public void AddShipNextFrame(Ship s)
        {
            ShipsToAdd.Add(s);
        }

        public void Initialize()
        {
            GSAI = new GSAI(this);
            for (int key = 1; key < 100; ++key)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(key);
                FleetsDict.TryAdd(key, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            foreach (KeyValuePair<string, Technology> keyValuePair in ResourceManager.TechTree)
            {
                TechEntry techEntry = new TechEntry();
                techEntry.Progress = 0.0f;
                techEntry.UID = keyValuePair.Key;

                //added by McShooterz: Checks if tech is racial, hides it, and reveals it only to races that pass
                if (keyValuePair.Value.RaceRestrictions.Count != 0)
                {
                    techEntry.Discovered = false;
                    techEntry.Tech.Secret = true;
                    foreach (Technology.RequiredRace raceTech in keyValuePair.Value.RaceRestrictions)
                    {
                        if (raceTech.ShipType == data.Traits.ShipType)
                        {
                            techEntry.Discovered = true;
                            techEntry.Unlocked = keyValuePair.Value.RootNode == 1;
                            if (data.Traits.Militaristic == 1 && techEntry.Tech.Militaristic)
                                techEntry.Unlocked = true;
                            break;
                        }
                    }
                } // BROKEN added race exclusions. in this case to prevent some techs from being exposed to the opteris and cybernetic races but also allow it to work in mods with extra races and what not.  
                else if (keyValuePair.Value.RaceExclusions.Count != 0)
                {
                    foreach (Technology.RequiredRace raceTech in keyValuePair.Value.RaceExclusions)
                    {
                        if (raceTech.ShipType == data.Traits.ShipType ||
                            (data.Traits.Cybernetic > 0 && raceTech.ShipType == "Opteris"))
                        {
                            techEntry.Discovered = false;
                            techEntry.Unlocked = false;

                            //techEntry.GetTech().Secret = true;                            

                        }
                    }
                }
                else //not racial tech
                {
                    techEntry.Unlocked = keyValuePair.Value.RootNode == 1;
                    techEntry.Discovered = true;
                }
                if (isFaction || data.Traits.Prewarp == 1)
                {
                    techEntry.Unlocked = false;
                }
                if (data.Traits.Militaristic == 1)
                {
                    //added by McShooterz: alternate way to unlock militaristic techs
                    if (techEntry.Tech.Militaristic && techEntry.Tech.RaceRestrictions.Count == 0)
                        techEntry.Unlocked = true;

                    // If using the customMilTraitsTech option in ModInformation, default traits will NOT be automatically unlocked. Allows for totally custom militaristic traits.
                    if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.customMilTraitTechs)
                    {
                        techEntry.Unlocked = techEntry.Unlocked || techEntry.UID == "HeavyFighterHull" || techEntry.UID == "Military" || techEntry.UID == "ArmorTheory";
                    }
                }
                if (data.Traits.Cybernetic > 0)
                {
                    if (techEntry.UID == "Biospheres")
                        techEntry.Unlocked = true;
                }
                if (techEntry.Unlocked)
                    techEntry.Progress = techEntry.Tech.Cost * UniverseScreen.GamePaceStatic;
                TechnologyDict.Add(keyValuePair.Key, techEntry);
            }

            foreach (var kv in ResourceManager.HullsDict)       UnlockedHullsDict[kv.Value.Hull]  = false;
            foreach (var kv in ResourceManager.TroopsDict)      UnlockedTroopDict[kv.Key]         = false;
            foreach (var kv in ResourceManager.BuildingsDict)   UnlockedBuildingsDict[kv.Key]     = false;
            foreach (var kv in ResourceManager.ShipModulesDict) UnlockedModulesDict[kv.Key]       = false;

            //unlock from empire data file
            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;


            //Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();
            foreach (KeyValuePair<string, TechEntry> entry in TechnologyDict)
            {
                var tech = entry.Value.Tech;
                if (tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0 && !WeCanUseThis(tech, ourShips))
                    entry.Value.shipDesignsCanuseThis = false;
            }
            foreach (KeyValuePair<string, TechEntry> tech in TechnologyDict)
            {
                if (!tech.Value.shipDesignsCanuseThis)
                    tech.Value.shipDesignsCanuseThis = WeCanUseThisLater(tech.Value);
            }
            foreach (var kv in TechnologyDict)
            {
                if (!kv.Value.Unlocked)
                    continue;
                kv.Value.Unlocked = false;
                UnlockTech(kv.Key);
            }

            //unlock ships from empire data
            foreach (string ship in data.unlockShips)
                ShipsWeCanBuild.Add(ship);

            // fbedard: Add missing troop ship
            if (data.DefaultTroopShip == null)
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            // clear these lists as they serve no more purpose
            data.unlockBuilding = new Array<string>();
            data.unlockShips    = new Array<string>();
            UpdateShipsWeCanBuild();

            if (data.EconomicPersonality == null)
                data.EconomicPersonality = new ETrait { Name = "Generalists" };
            economicResearchStrategy = ResourceManager.EconStrats[data.EconomicPersonality.Name];

            // @todo Is this part even used anymore? Should it get removed?
            #if false // purge designs that don't advance the ships
                Log.Info(this.data.PortraitName + " Before Purge : " + GC.GetTotalMemory(true));
                if (!this.isFaction)
                {
                    HashSet<string> techs = new HashSet<string>();
                    HashSet<string> purgelist = new HashSet<string>();
                    int count = 2; //how many best ships to pick
                    //pick the best ships and record their techs
                    foreach (Ship ship in ResourceManager.ShipsDict.Values.OrderByDescending(str => str.BaseStrength))
                    {
                        foreach (string techsneeded in ship.shipData.techsNeeded)
                            techs.Add(techsneeded);
                        if (count < 0)
                            break;
                        count--;

                    }
                    // use the recorded techs and purge ships that do not have enough of those techs.
                    foreach (Ship ship in ResourceManager.ShipsDict.Values)
                    {
                        if (ship.shipData.techsNeeded.Count == 0 || ship.shipData.BaseStrength == 0
                            ||
                            ship.shipData.Role < ShipData.RoleName.fighter || ship.shipData.Role == ShipData.RoleName.prototype
                            || ship.shipData.ShipStyle != this.data.Traits.ShipType || ship.shipData.techsNeeded.Count == 0
                            )
                            continue;
                        var difference = ship.shipData.techsNeeded.Except(techs);
                        if (difference.Count() == 0)
                            continue;
                        if (difference.Count() / ship.shipData.techsNeeded.Count < .1)
                        {
                            purgelist.Add(ship.shipData.Name);
                        }


                    }
                    Log.Info(this.data.PortraitName + " - Purging " + purgelist.Count.ToString());
                    foreach (string purge in purgelist)
                    {
                        ResourceManager.ShipsDict.Remove(purge);
                    }
                }
                
                GC.WaitForPendingFinalizers(); GC.Collect();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                Log.Info(this.data.PortraitName + " after Purge : " + GC.GetTotalMemory(true));
            #endif
        }

        public Array<Ship> GetOurFactionShips()
        {
            var ourFactionShips = new Array<Ship>();
            foreach (var kv in ResourceManager.ShipsDict)
                if (kv.Value.shipData.ShipStyle == data.Traits.ShipType)
                    ourFactionShips.Add(kv.Value);
            return ourFactionShips;
        }

        public void InitializeFromSave()
        {
            GSAI = new GSAI(this);
            for (int key = 1; key < 100; ++key)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(key);
                FleetsDict.TryAdd(key, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            foreach (var kv in ResourceManager.HullsDict)       UnlockedHullsDict[kv.Value.Hull]  = false;
            foreach (var kv in ResourceManager.TroopsDict)      UnlockedTroopDict[kv.Key]         = false;
            foreach (var kv in ResourceManager.BuildingsDict)   UnlockedBuildingsDict[kv.Key]     = false;
            foreach (var kv in ResourceManager.ShipModulesDict) UnlockedModulesDict[kv.Key]       = false;

            // unlock from empire data file
            // Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();
            foreach (KeyValuePair<string, TechEntry> entry in TechnologyDict)
            {
                var tech = entry.Value.Tech;
                if (tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0 && !WeCanUseThis(tech, ourShips))
                    entry.Value.shipDesignsCanuseThis = false;
            }
            foreach (KeyValuePair<string, TechEntry> tech in TechnologyDict)
            {
                if (!tech.Value.shipDesignsCanuseThis)
                    tech.Value.shipDesignsCanuseThis = WeCanUseThisLater(tech.Value);
            }

            //fbedard: Add missing troop ship
            if (data.DefaultTroopShip == null)
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            foreach (KeyValuePair<string, TechEntry> kv in TechnologyDict)
            {
                if (!kv.Value.Unlocked)
                    continue;
                kv.Value.Unlocked = false;
                UnlockTechFromSave(kv.Value);
            }
            UpdateShipsWeCanBuild();
            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;
            foreach (string ship in data.unlockShips) // unlock ships from empire data
                ShipsWeCanBuild.Add(ship);


            if (data.EconomicPersonality == null)
                data.EconomicPersonality = new ETrait { Name = "Generalists" };
            economicResearchStrategy = ResourceManager.EconStrats[data.EconomicPersonality.Name];

            #if false // purge designs that dont advance the ships
                Log.Info(this.data.PortraitName + " Before Purge : " + GC.GetTotalMemory(true));
                if (!this.isFaction)
                {
                    HashSet<string> techs = new HashSet<string>();
                    HashSet<string> purgelist = new HashSet<string>();
                    int count = 2; //how many best ships to pick
                    //pick the best ships and record their techs
                    foreach (Ship ship in ResourceManager.ShipsDict.Values.OrderByDescending(str => str.BaseStrength))
                    {
                        foreach (string techsneeded in ship.shipData.techsNeeded)
                            techs.Add(techsneeded);
                        if (count < 0)
                            break;
                        count--;

                    }
                    // use the recorded techs and purge ships that do not have enough of those techs.
                    foreach (Ship ship in ResourceManager.ShipsDict.Values)
                    {
                        if (ship.shipData.techsNeeded.Count == 0 || ship.shipData.BaseStrength == 0
                            ||
                            ship.shipData.Role < ShipData.RoleName.fighter || ship.shipData.Role == ShipData.RoleName.prototype
                            || ship.shipData.ShipStyle != this.data.Traits.ShipType || ship.shipData.techsNeeded.Count == 0
                            )
                            continue;
                        var difference = ship.shipData.techsNeeded.Except(techs);
                        if (difference.Count() == 0)
                            continue;
                        if (difference.Count() / ship.shipData.techsNeeded.Count < .1)
                        {
                            purgelist.Add(ship.shipData.Name);
                        }


                    }
                    Log.Info(this.data.PortraitName + " - Purging " + purgelist.Count);
                    foreach (string purge in purgelist)
                    {
                        ResourceManager.ShipsDict.Remove(purge);
                    }
                }
                GC.Collect();
                Log.Info(this.data.PortraitName + " after Purge : " + GC.GetTotalMemory(true));
            #endif
        }
        private bool WeCanUseThisLater(TechEntry tech)
        {
            foreach (Technology.LeadsToTech leadsto in tech.Tech.LeadsTo)
            {
                TechEntry entry = TechnologyDict[leadsto.UID];
                if (entry.shipDesignsCanuseThis || WeCanUseThisLater(entry))
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

        public void UnlockTech(string techID) //@todo rewrite. the empire tech dictionary is made of techentries which have a reference to the technology.
        {
            var techEntry = TechnologyDict[techID];
            if (techEntry.Unlocked)
                return;
            //Add to level of tech if more than one level
            var technology = ResourceManager.TechTree[techID];
            if (technology.MaxLevel > 1)
            {
                techEntry.Level++;
                if (techEntry.Level == technology.MaxLevel)
                {
                    techEntry.Progress = techEntry.TechCost* UniverseScreen.GamePaceStatic;
                    techEntry.Unlocked = true;
                }
                else
                {
                    techEntry.Unlocked = false;
                    techEntry.Progress = 0;
                }
            }
            else
            {
                techEntry.Progress = techEntry.Tech.Cost * UniverseScreen.GamePaceStatic;
                techEntry.Unlocked = true;
            }
            //Set GSAI to build ship roles

            //if (techEntry.GetTech().unlockBattleships || techID == "Battleships")
            //    this.canBuildCapitals = true;
            //if (techEntry.GetTech().unlockCruisers || techID == "Cruisers")
            //    this.canBuildCruisers = true;
            //if (techEntry.GetTech().unlockFrigates || techID == "FrigateConstruction")
            //    this.canBuildFrigates = true;
            //if (techEntry.GetTech().unlockCorvettes || techID == "HeavyFighterHull" )
            //    this.canBuildCorvettes = true;
            //if (!this.canBuildCorvettes  && techEntry.GetTech().TechnologyType == TechnologyType.ShipHull)
            //{
            //    foreach(KeyValuePair<string,bool> hull in this.GetHDict())
            //    {
            //        if (techEntry.GetTech().HullsUnlocked.Where(hulls => hulls.Name == hull.Key).Count() ==0)
            //            continue;
            //        if(ResourceManager.ShipsDict.Where(hulltech=> hulltech.Value.shipData.Hull == hull.Key && hulltech.Value.shipData.Role == ShipData.RoleName.corvette).Count()>0)
            //        {
            //            this.canBuildCorvettes = true;
            //            break;
            //        }
            //    }
            //}
   
            //Added by McShooterz: Race Specific buildings
            foreach (Technology.UnlockedBuilding unlockedBuilding in technology.BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == this.data.Traits.ShipType || unlockedBuilding.Type == null || unlockedBuilding.Type == TechnologyDict[techID].AcquiredFrom)
                    this.UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            if (technology.RootNode == 0)
            {
                foreach (Technology.LeadsToTech leadsToTech in technology.LeadsTo)
                {
                    //added by McShooterz: Prevent Racial tech from being discovered by unintentional means
                    if (TechnologyDict[leadsToTech.UID].Tech.RaceRestrictions.Count == 0 && !TechnologyDict[leadsToTech.UID].Tech.Secret)
                        TechnologyDict[leadsToTech.UID].Discovered = true;
                }
            }
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in technology.ModulesUnlocked)
            {
                if (unlockedMod.Type == data.Traits.ShipType || unlockedMod.Type == null || unlockedMod.Type == TechnologyDict[techID].AcquiredFrom)
                {
                    UnlockedModulesDict[unlockedMod.ModuleUID] = true;
                    if (ResourceManager.ShipModulesDict.TryGetValue(unlockedMod.ModuleUID, out ShipModule checkmod))
                    {
                        canBuildTroopShips = canBuildTroopShips || checkmod.IsTroopBay;
                        canBuildCarriers   = canBuildCarriers || checkmod.MaximumHangarShipSize > 0;
                        canBuildBombers    = canBuildBombers || checkmod.ModuleType == ShipModuleType.Bomb;
                    }
                }

            }
            foreach (Technology.UnlockedTroop unlockedTroop in technology.TroopsUnlocked)
            {
                if (unlockedTroop.Type == data.Traits.ShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null || unlockedTroop.Type == techEntry.AcquiredFrom)
                    UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in technology.HullsUnlocked)
            {
                if (unlockedHull.ShipType == data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == techEntry.AcquiredFrom)
                {
                   UnlockedHullsDict[unlockedHull.Name] = true;
                }

            }
            //this.UpdateShipsWeCanBuild();

            // Added by The Doctor - trigger events with unlocking of techs, via Technology XML
            if (isPlayer)
            {
                foreach (Technology.TriggeredEvent triggeredEvent in technology.EventsTriggered)
                {
                    string type = triggeredEvent.Type;
                    if (type != data.Traits.ShipType && type != null && type != techEntry.AcquiredFrom)
                        continue;

                    if (triggeredEvent.CustomMessage != null)
                        Universe.NotificationManager.AddNotify(triggeredEvent, triggeredEvent.CustomMessage);
                    else
                        Universe.NotificationManager.AddNotify(triggeredEvent);
                }
            }


            // Added by The Doctor - reveal specified 'secret' techs with unlocking of techs, via Technology XML
            foreach (Technology.RevealedTech revealedTech in technology.TechsRevealed)
            {
                if (revealedTech.Type == data.Traits.ShipType || revealedTech.Type == null || revealedTech.Type == techEntry.AcquiredFrom)
                    TechnologyDict[revealedTech.RevUID].Discovered = true;
            }
            foreach (Technology.UnlockedBonus unlockedBonus in technology.BonusUnlocked)
            {
                //Added by McShooterz: Race Specific bonus
                string type = unlockedBonus.Type;
                if (type != null && type != data.Traits.ShipType && type != techEntry.AcquiredFrom)
                    continue;

                string str = unlockedBonus.BonusType;
                if (string.IsNullOrEmpty(str))
                    str = unlockedBonus.Name;
                if (unlockedBonus.Tags.Count > 0)
                {
                    foreach (string index in unlockedBonus.Tags)
                    {
                        var tagmod = data.WeaponTags[index];
                        switch (unlockedBonus.BonusType)
                        {
                            case "Weapon_Speed":             tagmod.Speed             += unlockedBonus.Bonus; continue;
                            case "Weapon_Damage":            tagmod.Damage            += unlockedBonus.Bonus; continue;
                            case "Weapon_ExplosionRadius":   tagmod.ExplosionRadius   += unlockedBonus.Bonus; continue;
                            case "Weapon_TurnSpeed":         tagmod.Turn              += unlockedBonus.Bonus; continue;
                            case "Weapon_Rate":              tagmod.Rate              += unlockedBonus.Bonus; continue;
                            case "Weapon_Range":             tagmod.Range             += unlockedBonus.Bonus; continue;
                            case "Weapon_ShieldDamage":      tagmod.ShieldDamage      += unlockedBonus.Bonus; continue;
                            case "Weapon_ArmorDamage":       tagmod.ArmorDamage       += unlockedBonus.Bonus; continue;
                            case "Weapon_HP":                tagmod.HitPoints         += unlockedBonus.Bonus; continue;
                            case "Weapon_ShieldPenetration": tagmod.ShieldPenetration += unlockedBonus.Bonus; continue;
                            case "Weapon_ArmourPenetration": tagmod.ArmourPenetration += unlockedBonus.Bonus; continue;
                            default: continue;
                        }
                    }
                }

                switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
                {
                    case "Xeno Compilers":
                    case "Research Bonus": data.Traits.ResearchMod += unlockedBonus.Bonus; break;
                    case "FTL Spool Bonus":
                        if      (unlockedBonus.Bonus < 1)  data.SpoolTimeModifier *= 1.0f - unlockedBonus.Bonus; // i.e. if there is a 0.2 (20%) bonus unlocked, the spool modifier is 1-0.2 = 0.8* existing spool modifier...
                        else if (unlockedBonus.Bonus >= 1) data.SpoolTimeModifier = 0f; // insta-warp by modifier
                        break;
                    case "Top Guns":
                    case "Bonus Fighter Levels":
                        data.BonusFighterLevels += (int)unlockedBonus.Bonus;
                        foreach (Ship ship in OwnedShips)
                        {
                            if (ship.shipData.Role != ShipData.RoleName.fighter)
                                continue;
                            ship.Level += (int)unlockedBonus.Bonus;
                            if (ship.Level > 5) ship.Level = 5;
                        }
                        break;
                    case "Mass Reduction":
                    case "Percent Mass Adjustment": data.MassModifier       += unlockedBonus.Bonus; break;
                    case "ArmourMass":              data.ArmourMassModifier += unlockedBonus.Bonus; break;
                    case "Resistance is Futile":
                    case "Allow Assimilation":      data.Traits.Assimilators = true; break;
                    case "Cryogenic Suspension":
                    case "Passenger Modifier":      data.Traits.PassengerModifier += (int)unlockedBonus.Bonus; break;
                    case "ECM Bonus":
                    case "Missile Dodge Change Bonus": data.MissileDodgeChance   += unlockedBonus.Bonus; break;
                    case "Set FTL Drain Modifier":     data.FTLPowerDrainModifier = unlockedBonus.Bonus; break;
                    case "Super Soldiers":
                    case "Troop Strength Modifier Bonus": data.Traits.GroundCombatModifier += unlockedBonus.Bonus; break;
                    case "Fuel Cell Upgrade":
                    case "Fuel Cell Bonus":       data.FuelCellModifier  += unlockedBonus.Bonus;  break;
                    case "Trade Tariff":
                    case "Bonus Money Per Trade": data.Traits.Mercantile += unlockedBonus.Bonus; break;
                    case "Missile Armor":
                    case "Missile HP Bonus":      data.MissileHPModifier += unlockedBonus.Bonus; break;
                    case "Hull Strengthening":
                    case "Module HP Bonus":
                        data.Traits.ModHpModifier += unlockedBonus.Bonus;
                        RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
                        break;
                    case "Reaction Drive Upgrade":
                    case "STL Speed Bonus":           data.SubLightModifier += unlockedBonus.Bonus; break;
                    case "Reactive Armor":
                    case "Armor Explosion Reduction": data.ExplosiveRadiusReduction   += unlockedBonus.Bonus; break;
                    case "Slipstreams":
                    case "In Borders FTL Bonus":      data.Traits.InBordersSpeedBonus += unlockedBonus.Bonus; break;
                    case "StarDrive Enhancement":
                    case "FTL Speed Bonus":           data.FTLModifier += unlockedBonus.Bonus * data.FTLModifier; break;
                    case "FTL Efficiency":
                    case "FTL Efficiency Bonus":      data.FTLPowerDrainModifier -= unlockedBonus.Bonus * data.FTLPowerDrainModifier; break;
                    case "Spy Offense":
                    case "Spy Offense Roll Bonus":    data.OffensiveSpyBonus += unlockedBonus.Bonus; break;
                    case "Spy Defense":
                    case "Spy Defense Roll Bonus":    data.DefensiveSpyBonus += unlockedBonus.Bonus; break;
                    case "Increased Lifespans":
                    case "Population Growth Bonus":   data.Traits.ReproductionMod += unlockedBonus.Bonus; break;
                    case "Set Population Growth Min": data.Traits.PopGrowthMin     = unlockedBonus.Bonus; break;
                    case "Set Population Growth Max": data.Traits.PopGrowthMax     = unlockedBonus.Bonus; break;
                    case "Xenolinguistic Nuance":
                    case "Diplomacy Bonus":           data.Traits.DiplomacyMod    += unlockedBonus.Bonus; break;
                    case "Ordnance Effectiveness":
                    case "Ordnance Effectiveness Bonus": data.OrdnanceEffectivenessBonus += unlockedBonus.Bonus; break;
                    case "Tachyons":
                    case "Sensor Range Bonus":        data.SensorModifier += unlockedBonus.Bonus; break;
                    case "Privatization": data.Privatization = true; break;
                    // Doctor: Adding an actually configurable amount of civilian maintenance modification; privatisation is hardcoded at 50% but have left it in for back-compatibility.
                    case "Civilian Maintenance": data.CivMaintMod -= unlockedBonus.Bonus; break;
                    case "Armor Piercing":
                    case "Armor Phasing": data.ArmorPiercingBonus += (int)unlockedBonus.Bonus; break;
                    case "Kulrathi Might":
                        data.Traits.ModHpModifier += unlockedBonus.Bonus;
                        RecalculateMaxHP = true; //So existing ships will benefit from changes to ModHpModifier -Gretman
                        break;
                    case "Subspace Inhibition": data.Inhibitors = true; break;
                    // Added by McShooterz: New Bonuses
                    case "Production Bonus":   data.Traits.ProductionMod       += unlockedBonus.Bonus; break;
                    case "Construction Bonus": data.Traits.ShipCostMod         -= unlockedBonus.Bonus; break;
                    case "Consumption Bonus":  data.Traits.ConsumptionModifier -= unlockedBonus.Bonus; break;
                    case "Tax Bonus":          data.Traits.TaxMod              += unlockedBonus.Bonus; break;
                    case "Repair Bonus":       data.Traits.RepairMod   += unlockedBonus.Bonus; break;
                    case "Maintenance Bonus":  data.Traits.MaintMod    -= unlockedBonus.Bonus; break;
                    case "Power Flow Bonus":   data.PowerFlowMod       += unlockedBonus.Bonus; break;
                    case "Shield Power Bonus": data.ShieldPowerMod     += unlockedBonus.Bonus; break;
                    case "Ship Experience Bonus": data.ExperienceMod   += unlockedBonus.Bonus; break;
                }
            }
            //update ship stats if a bonus was unlocked
            if (ResourceManager.TechTree[techID].BonusUnlocked.Count > 0)
            {
                foreach (Ship ship in OwnedShips)//@todo can make a global ship unlock flag. 
                    ship.shipStatusChanged = true;
            }
            UpdateShipsWeCanBuild();
            if (!isPlayer)
                GSAI.TriggerRefit();
            data.ResearchQueue.Remove(techID);
        }

        public void UnlockTechFromSave(TechEntry tech)
        {
            var technology = tech.Tech;
            tech.Progress = technology.Cost * UniverseScreen.GamePaceStatic;
            tech.Unlocked = true;
            foreach (Technology.UnlockedBuilding unlockedBuilding in technology.BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == data.Traits.ShipType || unlockedBuilding.Type == null || unlockedBuilding.Type == tech.AcquiredFrom)
                    UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            if (technology.RootNode == 0)
            {
                foreach (Technology.LeadsToTech leadsToTech in technology.LeadsTo)
                {
                    TechEntry leadsTo = TechnologyDict[leadsToTech.UID];
                    Technology theTechnology = leadsTo.Tech;
                    if (theTechnology.RaceRestrictions.Count == 0 && !theTechnology.Secret)
                        leadsTo.Discovered = true;
                }
            }
            foreach (Technology.UnlockedMod unlockedMod in technology.ModulesUnlocked)
            {
                if (unlockedMod.Type == data.Traits.ShipType || unlockedMod.Type == null || unlockedMod.Type == tech.AcquiredFrom)
                    UnlockedModulesDict[unlockedMod.ModuleUID] = true;
            }
            foreach (Technology.UnlockedTroop unlockedTroop in technology.TroopsUnlocked)
            {
                if (unlockedTroop.Type == data.Traits.ShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null || unlockedTroop.Type == tech.AcquiredFrom)
                    UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in technology.HullsUnlocked)
            {
                if (unlockedHull.ShipType == data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == tech.AcquiredFrom)
                    UnlockedHullsDict[unlockedHull.Name] = true;
            }
            foreach (Technology.UnlockedMod unlockedMod in technology.ModulesUnlocked)
            {
                if (unlockedMod.Type == data.Traits.ShipType || unlockedMod.Type == null || unlockedMod.Type == tech.AcquiredFrom)
                {
                    UnlockedModulesDict[unlockedMod.ModuleUID] = true;
                }
            }
            foreach (Technology.UnlockedTroop unlockedTroop in technology.TroopsUnlocked)
            {
                if (unlockedTroop.Type == data.Traits.ShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null || unlockedTroop.Type == tech.AcquiredFrom)
                    UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in technology.HullsUnlocked)
            {
                if (unlockedHull.ShipType == data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == tech.AcquiredFrom)
                {
                    UnlockedHullsDict[unlockedHull.Name] = true;
                }
            }
            UpdateShipsWeCanBuild();
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target)
        {
            TechnologyDict[techID].AcquiredFrom = target.data.Traits.ShipType;
            UnlockTech(techID);
        }

        public void UnlockHullsSave(string techID, string AbsorbedShipType)
        {
            foreach (Technology.UnlockedTroop unlockedTroop in ResourceManager.TechTree[techID].TroopsUnlocked)
            {
                if (unlockedTroop.Type == AbsorbedShipType || unlockedTroop.Type == "ALL" || unlockedTroop.Type == null)
                    UnlockedTroopDict[unlockedTroop.Name] = true;
            }
            foreach (Technology.UnlockedHull unlockedHull in ResourceManager.TechTree[techID].HullsUnlocked)
            {
                if (unlockedHull.ShipType == AbsorbedShipType || unlockedHull.ShipType == null)
                    UnlockedHullsDict[unlockedHull.Name] = true;
            }
            foreach (Technology.UnlockedMod unlockedMod in ResourceManager.TechTree[techID].ModulesUnlocked)
            {
                if (unlockedMod.Type == AbsorbedShipType || unlockedMod.Type == null)
                    UnlockedModulesDict[unlockedMod.ModuleUID] = true;
            }
            foreach (Technology.UnlockedBuilding unlockedBuilding in ResourceManager.TechTree[techID].BuildingsUnlocked)
            {
                if (unlockedBuilding.Type == AbsorbedShipType || unlockedBuilding.Type == null)
                    UnlockedBuildingsDict[unlockedBuilding.Name] = true;
            }
            UpdateShipsWeCanBuild();
        }

        private void AssessHostilePresence()
        {
            foreach (SolarSystem beingInvaded in OwnedSolarSystems)
            {
                foreach (Ship ship in beingInvaded.ShipList)
                {
                    if (ship.loyalty == this || (!ship.loyalty.isFaction && !Relationships[ship.loyalty].AtWar) || HostilesPresent[beingInvaded])
                        continue;
                    Universe.NotificationManager.AddBeingInvadedNotification(beingInvaded, ship.loyalty);
                    HostilesPresent[beingInvaded] = true;
                    break;
                }
            }
        }

        public Array<Ship> FindShipsInOurBorders()
        {
            return GSAI.ThreatMatrix.FindShipsInOurBorders();
        }

        public void UpdateKnownShips()
        {
           // this.GetGSAI().ThreatMatrix.ScrubMatrix(true);
            if (data.Defeated)
                return;

            if (isPlayer && Universe.Debug)
            {
                using (Universe.MasterShipList.AcquireReadLock())
                    foreach (Ship nearby in Universe.MasterShipList)
                    {
                        nearby.inSensorRange = true;
                        KnownShips.Add(nearby);
                        GSAI.ThreatMatrix.UpdatePin(nearby);
                    }
                return;
            }
            //added by gremlin ships in border search
            //for (int i = 0; i < Empire.Universe.MasterShipList.Count; i++)            
            var source = Universe.MasterShipList.ToArray();
            var rangePartitioner = Partitioner.Create(0, source.Length);
            ConcurrentBag<Ship> shipbag = new ConcurrentBag<Ship>();
            var influenceNodes = SensorNodes.AtomicCopy();

            Parallel.ForEach(rangePartitioner, (range) =>
            {
                var toadd = new Array<Ship>();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    toadd.Clear();
                    Ship nearby = source[i];
                    if (nearby == null || !nearby.Active)
                        continue;
                    nearby.getBorderCheck.Remove(this);                        
                    if (nearby.loyalty != this)
                    {                            
                        bool insensors = false;
                        bool border = false;

                        foreach (InfluenceNode node in influenceNodes)
                        {
                            if (node.Position.SqDist(nearby.Center) >= node.Radius*node.Radius)
                            {
                                continue;
                            }
                            if (TryGetRelations(nearby.loyalty, out Relationship loyalty) && !loyalty.Known)
                            {
                                GlobalStats.UiLocker.EnterWriteLock();
                                DoFirstContact(nearby.loyalty);
                                GlobalStats.UiLocker.ExitWriteLock();
                            }
                            insensors = true;
                            Ship shipKey = node.KeyedObject as Ship;
                            if (shipKey != null && (shipKey.inborders || shipKey.Name == "Subspace Projector") ||
                                node.KeyedObject is SolarSystem || node.KeyedObject is Planet)
                            {
                                border = true;
                                nearby.getBorderCheck.Add(this);
                            }
                            if (!isPlayer)
                            {
                                break;
                            }
                            nearby.inSensorRange = true;
                            if (nearby.System== null || !isFaction && !nearby.loyalty.isFaction && !loyalty.AtWar)
                            {
                                break;
                            }

                            nearby.System.DangerTimer = 120f;
                            break;
                        }

                        GSAI.ThreatMatrix.UpdatePin(nearby, border, insensors);

                        if (insensors) toadd.Add(nearby);
                        foreach (Ship ship in toadd) shipbag.Add(ship);
                        toadd.Clear();
                    }
                    else
                    {
                        GSAI.ThreatMatrix.ClearPinsInSensorRange(nearby.Center, nearby.SensorRange);
                        shipbag.Add(nearby);
                        if (isPlayer)
                        {
                            nearby.inSensorRange = true;
                        }
                        nearby.inborders = false;

                        using (BorderNodes.AcquireReadLock())
                        foreach (InfluenceNode node in BorderNodes)
                        {
                            if (node.Position.SqDist(nearby.Center) > node.Radius * node.Radius)
                                continue;
                            nearby.inborders = true;
                            nearby.getBorderCheck.Add(this);
                            break;
                        }

                        if (!nearby.inborders)
                        {
                            foreach (var relationship in Relationships)
                            {
                                if (!relationship.Value.Treaty_Alliance) //Relationship.Key == this ||
                                {
                                    continue;
                                }
                                using (relationship.Key.BorderNodes.AcquireReadLock())
                                foreach (InfluenceNode node in relationship.Key.BorderNodes)
                                {
                                    if (node.Position.SqDist(nearby.Center) > node.Radius*node.Radius)
                                        continue;
                                    nearby.inborders = true;
                                    nearby.getBorderCheck.Add(this);
                                    break;
                                }
                            }
                        }
                    }
                }
            });

            foreach (Ship ship in shipbag)
                KnownShips.Add(ship);
            Task task4 = new Task(() =>
            {
                GSAI.ThreatMatrix.ScrubMatrix();
            });
            task4.Start();
            
        }

        public IReadOnlyDictionary<Empire, Relationship> AllRelations => Relationships;
        public Relationship GetRelations(Empire withEmpire) => Relationships[withEmpire];

        public void AddRelation(Empire empire)
        {
            Relationships.Add(empire, new Relationship(empire.data.Traits.Name));            
        }
        public bool TryGetRelations(Empire empire, out Relationship relations)
        {
            return Relationships.TryGetValue(empire, out relations);
        }
        public void AddRelationships(Empire e, Relationship i)
        {
            Relationships.Add(e, i);
        }

        public bool ExistsRelation(Empire withEmpire)
        {
            return Relationships.ContainsKey(withEmpire);
        }
        public void DamageRelationship(Empire e, string why, float amount, Planet p)
        {
            if (!Relationships.TryGetValue(e, out Relationship relationship))
                return;
            if (why == "Colonized Owned System" || why == "Destroyed Ship")
                relationship.DamageRelationship(this, e, why, amount, p);
        }

        public void DoDeclareWar(War w)
        {
        }

        public void DoFirstContact(Empire e)
        {
            this.Relationships[e].SetInitialStrength(e.data.Traits.DiplomacyMod * 100f);
            this.Relationships[e].Known = true;
            if (!e.GetRelations(this).Known)
                e.DoFirstContact(this);
#if PERF
            if (Empire.Universe.player == this)
                return;
#endif
            if (GlobalStats.perf && Universe.player == this)
                return;
            try
            {
                if (Universe.PlayerEmpire == this && !e.isFaction && !e.MinorRace)
                {
                    Universe.ScreenManager.AddScreen(new DiplomacyScreen(e, Universe.PlayerEmpire, "First Contact"));
                }
                else
                {
                    if (Universe.PlayerEmpire != this || !e.isFaction)
                        return;
                    foreach (Encounter e1 in ResourceManager.Encounters)
                        if (e1.Faction == e.data.Traits.Name && e1.Name == "First Contact")
                            Universe.ScreenManager.AddScreen(new EncounterPopup(Universe, Universe.PlayerEmpire, e, null, e1));
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
            if(!this.isPlayer && !this.isFaction)
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
            if (this.UpdateTimer <= 0f)
            {                
                if (this == Universe.PlayerEmpire)
                {
                    Universe.StarDate += 0.1f;
                    Universe.StarDate = (float)Math.Round(Universe.StarDate, 1);

                    string starDate = Universe.StarDate.ToString("#.0");
                    if (!StatTracker.SnapshotsDict.ContainsKey(starDate))
                        StatTracker.SnapshotsDict.Add(starDate, new SerializableDictionary<int, Snapshot>());
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        if (empire.data.IsRebelFaction)
                            continue;

                        var snapshots = StatTracker.SnapshotsDict[starDate];
                        int empireIndex = EmpireManager.Empires.IndexOf(empire);
                        if (!snapshots.ContainsKey(empireIndex))
                            snapshots.Add(empireIndex, new Snapshot(Universe.StarDate));
                    }
                    if (Universe.StarDate == 1000.09f)
                    {
                        foreach (Empire empire in EmpireManager.Empires)
                        {
                            using (empire.OwnedPlanets.AcquireReadLock())
                            foreach (Planet planet in empire.OwnedPlanets)
                            {
                                if (!StatTracker.SnapshotsDict.ContainsKey(starDate))
                                    continue;
                                int empireIndex = EmpireManager.Empires.IndexOf(planet.Owner);
                                StatTracker.SnapshotsDict[starDate][empireIndex].EmpireNodes.Add(new NRO
                                {
                                    Node = planet.Position,
                                    Radius = 300000f,
                                    StarDateMade = Universe.StarDate
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
                            foreach (Ship ship in (Array<Ship>)key.ShipList)
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

                //fbedard: Number of planets where you have combat
                empirePlanetCombat = 0;
                if (isPlayer)
                    foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (!p.ExploredDict[Universe.PlayerEmpire] || !p.RecentCombat)
                                continue;

                            if (p.Owner != Universe.PlayerEmpire)
                            {
                                foreach (Troop troop in p.TroopsHere)
                                {
                                    if (troop?.GetOwner() != Universe.PlayerEmpire) continue;
                                    empirePlanetCombat++;
                                    break;
                                }
                            }
                            else empirePlanetCombat++;
                        }
                    }

                this.empireShipTotal = 0;
                this.empireShipCombat = 0;
                foreach (Ship ship in this.OwnedShips)
                {
                    if (this.RecalculateMaxHP)            //This applies any new ModHPModifier that may have been gained -Gretman
                        ship.RecalculateMaxHP();
                    if (ship.fleet == null && ship.InCombat && ship.Mothership == null)  //fbedard: total ships in combat
                        this.empireShipCombat++;                    
                    if (ship.Mothership != null || ship.shipData.Role == ShipData.RoleName.troop || ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                        continue;
                    this.empireShipTotal++;
                }
                this.RecalculateMaxHP = false;
                this.UpdateTimer = (float)GlobalStats.TurnTimer;
                this.DoMoney();
                this.TakeTurn();
            }
            this.UpdateFleets(elapsedTime);
            this.OwnedShips.ApplyPendingRemovals();
            this.OwnedProjectors.ApplyPendingRemovals();  //fbedard
        }

        public void UpdateFleets(float elapsedTime)
        {
            this.updateContactsTimer -= elapsedTime;
            this.FleetUpdateTimer -= elapsedTime;
            //try
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
            //catch
            //{
            //}
            if ((double)this.FleetUpdateTimer < 0.0)
                this.FleetUpdateTimer = 5f;
            this.OwnedShips.ApplyPendingRemovals();
        }

        public float GetPlanetIncomes()
        {
            float income = 0.0f;
            foreach (Planet planet in OwnedPlanets)
            {
                planet.UpdateIncomes(false);
                income += (planet.GrossMoneyPT + planet.GrossMoneyPT * this.data.Traits.TaxMod) * this.data.TaxRate;
                income += planet.PlusFlatMoneyPerTurn + (planet.Population / 1000f * planet.PlusCreditsPerColonist);
            }
            return income;
        }

        private void DoMoney()
        {
            this.MoneyLastTurn = this.Money;
            ++this.numberForAverage;
            this.GrossTaxes = 0f;
            this.OtherIncome = 0f;

            using (OwnedPlanets.AcquireReadLock())
            {
                foreach (Planet planet in this.OwnedPlanets)
                {
                    planet.UpdateIncomes(false);
                    this.GrossTaxes += planet.GrossMoneyPT + planet.GrossMoneyPT * this.data.Traits.TaxMod;
                    this.OtherIncome += planet.PlusFlatMoneyPerTurn + (planet.Population / 1000f * planet.PlusCreditsPerColonist);
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
            {
                this.totalShipMaintenance = 0.0f;
                
                using (OwnedShips.AcquireReadLock())
                foreach (Ship ship in OwnedShips)
                {
                    if (data.DefenseBudget > 0 && ((ship.shipData.Role == ShipData.RoleName.platform && ship.BaseStrength > 0)
                        || (ship.shipData.Role == ShipData.RoleName.station && (ship.shipData.IsOrbitalDefense || !ship.shipData.IsShipyard))))
                    {
                        data.DefenseBudget -= ship.GetMaintCost();
                        continue;
                    }
                    totalShipMaintenance += ship.GetMaintCost();
                }

                using (OwnedProjectors.AcquireReadLock())
                foreach (Ship ship in OwnedProjectors)
                {
                        if (data.SSPBudget > 0)
                        {
                            data.SSPBudget -= ship.GetMaintCost();
                            continue;
                        }
                        totalShipMaintenance += ship.GetMaintCost();
                }

            }//,
           // () =>
            {
                using (OwnedPlanets.AcquireReadLock())
                {
                    float newBuildM = 0f;
                    int planetcount = this.GetPlanets().Count;
                    this.exportFTrack = 0;
                    this.exportPTrack = 0;
                    this.averagePLanetStorage =0;
                    foreach (Planet planet in this.OwnedPlanets)
                    {

                        this.exportFTrack += planet.ExportFSWeight;
                        this.exportPTrack += planet.ExportPSWeight;
                        this.averagePLanetStorage += (int)planet.MAX_STORAGE;
                    }
                    this.averagePLanetStorage /= planetcount;
                
                    foreach (Planet planet in this.OwnedPlanets)// .OrderBy(weight => (int)(weight.ExportFSWeight) + (int)(weight.ExportPSWeight)))
                    {
                    
                        planet.UpdateOwnedPlanet();

                        newBuildM += planet.TotalMaintenanceCostsPerTurn;
                    }
                    totalBuildingMaintenance = newBuildM;
                }
            }
           // );
            this.totalMaint = this.GetTotalBuildingMaintenance() + this.GetTotalShipMaintenance();
            this.AllTimeMaintTotal += this.totalMaint;
            this.Money += (this.GrossTaxes * this.data.TaxRate) + this.OtherIncome;
            this.Money += this.data.FlatMoneyBonus;
            this.Money += this.TradeMoneyAddedThisTurn;
            this.Money -= this.totalMaint;
        }

        public float EstimateIncomeAtTaxRate(float rate)
        {
            return GrossTaxes * rate + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus - (GetTotalBuildingMaintenance() + GetTotalShipMaintenance());
        }
        public float Grossincome()
        {
            return GrossTaxes + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus;
        }
        public float EstimateShipCapacityAtTaxRate(float rate)
        {
            return GrossTaxes * rate + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus - (GetTotalBuildingMaintenance() );
        }

        public float GetActualNetLastTurn()
        {
            return Money - MoneyLastTurn;
        }

        public float GetAverageNetIncome()
        {
            return (GrossTaxes * data.TaxRate + (float)totalTradeIncome - AllTimeMaintTotal) / (float)numberForAverage;
        }

        public void UpdateShipsWeCanBuild()
        {
            foreach (var kv in ResourceManager.ShipsDict)
            {
                var ship = kv.Value;
                if (ship.Deleted || !WeCanBuildThis(ship.Name))
                    continue;
                try
                {
                    if (ship.shipData.Role <= ShipData.RoleName.station && !ship.shipData.IsShipyard)
                        structuresWeCanBuild.Add(ship.Name);
                    if (!ResourceManager.ShipRoles[ship.shipData.Role].Protected)
                        ShipsWeCanBuild.Add(ship.Name);
                }
                catch
                {
                    ship.Deleted = true;  //This should prevent this Key from being evaluated again
                    continue;   //This keeps the game going without crashing
                }

                foreach (string shiptech in ship.shipData.techsNeeded)
                    ShipTechs.Add(shiptech);                        

                if (!GSAI.NonCombatshipIsGoodForGoals(ship))
                    continue;

                int bombcount = 0;
                int hangarcount = 0;
                foreach (ModuleSlot slot in ship.ModuleSlotList)
                {
                    if (slot.module.ModuleType == ShipModuleType.Bomb)
                    {
                        bombcount += slot.module.XSIZE * slot.module.YSIZE;
                        if (bombcount > ship.Size * .2)
                            canBuildBombers = true;
                    }
                    if (slot.module.MaximumHangarShipSize > 0)
                    {
                        hangarcount += slot.module.YSIZE * slot.module.XSIZE;
                        if (hangarcount > ship.Size * .2)
                            canBuildCarriers = true;
                    }
                    if (slot.module.IsTroopBay || slot.module.TransporterRange > 0)
                        canBuildTroopShips = true;
                }

                var r = ship.shipData.HullRole;
                canBuildCorvettes = canBuildCorvettes || (r ==  ShipData.RoleName.gunboat || r == ShipData.RoleName.corvette);
                canBuildFrigates  = canBuildFrigates || (r == ShipData.RoleName.frigate || r == ShipData.RoleName.destroyer);
                canBuildCruisers  = canBuildCruisers ||  r == ShipData.RoleName.cruiser;
                canBuildCapitals  = canBuildCapitals || (r == ShipData.RoleName.capital || r == ShipData.RoleName.carrier );
            }
            if (Universe == null || !isPlayer)
                return;
            Universe.aw.SetDropDowns();
        }

        public float GetTotalBuildingMaintenance()
        {
            return totalBuildingMaintenance + data.Traits.MaintMod * totalBuildingMaintenance;
        }

        public float GetTotalShipMaintenance()
        {
            return totalShipMaintenance + data.Traits.MaintMod * totalShipMaintenance;
        }

        public bool WeCanBuildThis(string ship)
        {
            if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship ship1))
                return false;

            ShipData shipData = ship1.shipData;
            if (shipData == null)
            {
                #if TRACE
                    //Log.Info("{0} : shipData is null : {1}", data.PortraitName, ship);
                #endif
                return false;
            }

            // If the ship role is not defined don't try to use it
            if (!UnlockedHullsDict.TryGetValue(shipData.Hull, out bool goodHull) || !goodHull)
            {
                //#if TRACE
                //    if (shipData.HullRole >= ShipData.RoleName.fighter && shipData.ShipStyle == data.Traits.ShipType)
                //        Log.Info("{0} : Bad hull  : {1} : {2} : {3} :hull unlockable: {4} :Modules Unlockable: {5} : {6}",
                //                data.PortraitName, ship, shipData.Hull, shipData.Role, shipData.hullUnlockable, shipData.allModulesUnlocakable, shipData.techsNeeded.Count);
                //#endif
                return false;
            }

            if (!ResourceManager.ShipRoles.ContainsKey(shipData.HullRole))
            {
                //#if TRACE
                //    if (shipData.ShipStyle == data.Traits.ShipType)
                //        Log.Info("{0} : Bad  role : {1} : {2} : {3} :hull unlockable: {4} :Modules Unlockable: {5}",
                //                data.PortraitName, ship, shipData.Hull, shipData.Role, shipData.hullUnlockable, shipData.allModulesUnlocakable);
                //#endif
                return false;
            }

            // check if all modules in the ship are unlocked
            foreach (ModuleSlotData moduleSlotData in shipData.ModuleSlotList)
            {
                if (string.IsNullOrEmpty(moduleSlotData.InstalledModuleUID) ||
                    moduleSlotData.InstalledModuleUID == "Dummy" ||
                    UnlockedModulesDict[moduleSlotData.InstalledModuleUID])
                    continue;

                //#if TRACE
                //    Log.Info("{0} : Bad Modules : {1} : {2} : {3} : {4} :hull unlockable: {5} :Modules Unlockable: {6}",
                //            data.PortraitName, ship, shipData.Hull, shipData.Role, moduleSlotData.InstalledModuleUID, shipData.hullUnlockable, shipData.allModulesUnlocakable);
                //#endif
                return false; // can't build this ship because it contains a locked Module
            }

            //#if TRACE
            //    Log.Info("{0} : good ship : {1} : {2} : {3}", data.PortraitName, ship, shipData.Hull, shipData.Role);
            //#endif
            return true;
        }

        public bool WeCanUseThis(Technology tech, Array<Ship> ourFactionShips)
        {
            foreach (Ship ship in ourFactionShips)
            {
                foreach (Technology.UnlockedMod entry in tech.ModulesUnlocked)
                {
                    foreach (ModuleSlotData module in ship.shipData.ModuleSlotList)
                    {
                        if (entry.ModuleUID == module.InstalledModuleUID)
                            return true;
                    }
                }
            }
            return false;
        }


        

        public bool WeCanUseThisNow(Technology tech)
        {
            bool flag = false;
            HashSet<string> unlocklist = new HashSet<string>();
            foreach(Technology.UnlockedMod unlocked in tech.ModulesUnlocked)
            {
                unlocklist.Add(unlocked.ModuleUID);
            }
            
            //Parallel.ForEach(ResourceManager.ShipsDict, (ship, status) =>
            ShipData shipData;
            foreach(KeyValuePair<string,Ship> ship in ResourceManager.ShipsDict )
            {

                shipData = ship.Value.shipData;
                if (shipData.ShipStyle == null || shipData.ShipStyle == this.data.Traits.ShipType)
                {
                    if (shipData == null || (!this.UnlockedHullsDict.ContainsKey(shipData.Hull) || !this.UnlockedHullsDict[shipData.Hull]))
                        continue;
                    foreach (ModuleSlotData module in ship.Value.shipData.ModuleSlotList)
                    {
                        //if (tech.ModulesUnlocked.Where(uid => uid.ModuleUID == module.InstalledModuleUID).Count() > 0)
                        if(unlocklist.Contains(module.InstalledModuleUID))
                        {
                            flag = true;
                            break;
                            //status.Stop();
                            //continue;
                        }
                    }
                    //if (status.IsStopped)
                    //    return;
                }
            }//);
            return flag;
        }
       
        public float GetTotalPop()
        {
            float num = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet item_0 in this.OwnedPlanets)
                    num += item_0.Population / 1000f;
            return num;
        }

        public float GetGrossFoodPerTurn()
        {
            float num = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet item_0 in this.OwnedPlanets)
                    num += item_0.GrossFood;
            return num;
        }

        public int GetAverageTradeIncome()
        {
            if (this.numberForAverage == 0)
                return 0;
            else
                return this.totalTradeIncome / this.numberForAverage;
        }
         public Planet.ColonyType AssessColonyNeeds2(Planet p)
        {
            float fertility = p.Fertility;
            float richness = p.MineralRichness;
            float pop = p.MaxPopulation /1000;
             if(this.data.Traits.Cybernetic >0)
                 fertility = richness;
             if (fertility > .5 && fertility <= 1 && richness < 1 && pop <= 4 && pop > .5)
                 return Planet.ColonyType.Research;
             if (fertility > 1 && richness < 1 && pop >=2)
                 return Planet.ColonyType.Agricultural;
             if (richness > 1 && fertility > 1 && pop >4)
                 return Planet.ColonyType.Core;
             if (richness >= 1 && fertility < .5)
                 return Planet.ColonyType.Industrial;
             return Planet.ColonyType.Research;
             //if (richness > .5 && fertility < .5 && pop < 2)
             //    return Planet.ColonyType.Industrial;
             //if (richness <= 1 && fertility < 1 && pop >= 1)
             //    return Planet.ColonyType.Research;
             //return Planet.ColonyType.Colony;
        }
        public Planet.ColonyType AssessColonyNeeds(Planet p)
        {
            Planet.ColonyType type = AssessColonyNeeds2(p);
            if (type != Planet.ColonyType.Colony)
                return type;
            float MineralWealth = 0.0f;
            float PopSupport = 0.0f;
            float ResearchPotential = 0.0f;
            float Fertility = 0.0f;
            float MilitaryPotential = 0.0f;
            //if (p.Fertility > 1.0)
            //{
                
            //    //ResearchPotential += 0.5f;
            //}
            //else
            //    Fertility += p.Fertility;
            //else if( this.data.Traits.Cybernetic <= 0)
            //    ResearchPotential++;
            
            if (p.MineralRichness > .50)
            {
                MineralWealth += p.MineralRichness +p.MaxPopulation / 1000;
                //MilitaryPotential += 0.5f;
            }
            else
                MineralWealth += p.MineralRichness;
            

            
            if (p.MaxPopulation > 1000)
            {
                ResearchPotential += p.MaxPopulation / 1000;
                if (this.data.Traits.Cybernetic > 0)
                {
                    if (p.MineralRichness > 1)
                    {
                        PopSupport += p.MaxPopulation / 1000 + p.MineralRichness;
                    }
                }
                else
                {
                    if (p.Fertility > 1f)
                    {
                        
                        if (p.MineralRichness > 1)
                            PopSupport += p.MaxPopulation / 1000 + p.Fertility + p.MineralRichness;
                        Fertility += p.Fertility + p.MaxPopulation / 1000;
                    }
                }
                
                //if (p.Fertility > 1f)
                //    ++PopSupport;
                //if (p.MaxPopulation > 4000)
                //{
                //    ++PopSupport;                    
                //    if (p.MaxPopulation > 8000)
                //        ++PopSupport;
                //    if (p.MaxPopulation > 12000)
                //        PopSupport += 2f;
                //}
            }
            else
            {
                MilitaryPotential += Fertility + p.MineralRichness + p.MaxPopulation / 1000;
                Technology tech = null;
               if(p.MaxPopulation >=500)
                if (ResourceManager.TechTree.TryGetValue(this.ResearchTopic, out tech))
                    ResearchPotential = (tech.Cost - this.Research) / tech.Cost * (p.Fertility * 2 + p.MineralRichness + p.MaxPopulation / 500); 
              
            }
            if (this.data.Traits.Cybernetic > 0)
            {

                Fertility = 0;
            }
                              //(tech.Cost - this.Research) / tech.Cost;// *this.OwnedPlanets.Count;

            int CoreCount = 0;
            int IndustrialCount = 0;
            int AgriculturalCount = 0;
            int MilitaryCount = 0;
            int ResearchCount = 0;
            using (OwnedPlanets.AcquireReadLock())
            {
                foreach (Planet item_0 in this.OwnedPlanets)
                {
                    if (item_0.colonyType == Planet.ColonyType.Agricultural) ++AgriculturalCount;
                    if (item_0.colonyType == Planet.ColonyType.Core)         ++CoreCount;
                    if (item_0.colonyType == Planet.ColonyType.Industrial)   ++IndustrialCount;
                    if (item_0.colonyType == Planet.ColonyType.Research)     ++ResearchCount;
                    if (item_0.colonyType == Planet.ColonyType.Military)     ++MilitaryCount;
                }
            }
            float AssignedFactor = (float)(CoreCount + IndustrialCount + AgriculturalCount + MilitaryCount + ResearchCount) / ((float)this.OwnedPlanets.Count + 0.01f);
            float CoreDesire = PopSupport + (AssignedFactor - (float)CoreCount) ;
            float IndustrialDesire = MineralWealth + (AssignedFactor - (float)IndustrialCount);
            float AgricultureDesire = Fertility + (AssignedFactor - (float)AgriculturalCount);
            float MilitaryDesire = MilitaryPotential + (AssignedFactor - (float)MilitaryCount);
            float ResearchDesire = ResearchPotential + (AssignedFactor - (float)ResearchCount);

          

            if (CoreDesire > IndustrialDesire && CoreDesire > AgricultureDesire && (CoreDesire > MilitaryDesire && CoreDesire > ResearchDesire))
                return Planet.ColonyType.Core;
            if (IndustrialDesire > CoreDesire && IndustrialDesire > AgricultureDesire && (IndustrialDesire > MilitaryDesire && IndustrialDesire > ResearchDesire))
                return Planet.ColonyType.Industrial;
            if (AgricultureDesire > IndustrialDesire && AgricultureDesire > CoreDesire && (AgricultureDesire > MilitaryDesire && AgricultureDesire > ResearchDesire))
                return Planet.ColonyType.Agricultural;
            return ResearchDesire > CoreDesire && ResearchDesire > AgricultureDesire && (ResearchDesire > MilitaryDesire && ResearchDesire > IndustrialDesire) ? Planet.ColonyType.Research : Planet.ColonyType.Military;
        }

        public void ResetBorders()
        {
            BorderNodes.ClearAndRecycle(); //
            SensorNodes.ClearAndRecycle(); //

            var list = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                if (keyValuePair.Value.Treaty_Alliance)
                    list.Add(keyValuePair.Key);
            }
            foreach (Empire empire in list)
            {
                Array<Planet> tempPlanets = new Array<Planet>(empire.OwnedPlanets);// new Array<Planet>(empire.GetPlanets());
                foreach (Planet planet in tempPlanets)
                {
                    InfluenceNode influenceNode1 = SensorNodes.RecycleObject() ?? new InfluenceNode();

                    influenceNode1.KeyedObject = (object)planet;
                    influenceNode1.Position = planet.Position;
                    influenceNode1.Radius = 1f; //this.isFaction ? 20000f : Empire.ProjectorRadius + (float)(10000.0 * (double)planet.Population / 1000.0);
                                                // influenceNode1.Radius = this == Empire.Universe.PlayerEmpire ? 300000f * this.data.SensorModifier : 600000f * this.data.SensorModifier;

                    SensorNodes.Add(influenceNode1);

                    InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode2.Position = planet.Position;
                    influenceNode2.Radius = isFaction 
                                            ? 1f : this == Universe.PlayerEmpire ? 300000f * empire.data.SensorModifier 
                                            : 600000f * empire.data.SensorModifier;
                    foreach (Building building in planet.BuildingList)
                    {
                        //if (building.IsSensor)
                        if (building.SensorRange * data.SensorModifier > influenceNode2.Radius)
                            influenceNode2.Radius = building.SensorRange * data.SensorModifier;
                    }

                    SensorNodes.Add(influenceNode2);
                }

                //var clonedList = empire.GetShips();// new Array<Ship>(empire.GetShips());
                foreach (Ship ship in empire.GetShips())
                {
                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    //this.SensorNodes.pendingRemovals.TryPop(out influenceNode);
                    influenceNode.Position = ship.Center;
                    influenceNode.Radius = ship.SensorRange;
                    SensorNodes.Add(influenceNode);
                    influenceNode.KeyedObject = (object)ship;
                }

                foreach (Ship ship in empire.GetProjectors())
                {   //loop over all ALLIED projectors
                    //Empire.InfluenceNode influenceNode = new Empire.InfluenceNode();
                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    //this.SensorNodes.pendingRemovals.TryPop(out influenceNode);
                    influenceNode.Position = ship.Center;
                    influenceNode.Radius = ProjectorRadius;  //projectors currently use their projection radius as sensors
                    SensorNodes.Add(influenceNode);
                    influenceNode.KeyedObject = (object)ship;
                }
            }
            foreach (Planet planet in GetPlanets())
            {   //loop over OWN planets
                //Empire.InfluenceNode influenceNode1 = new Empire.InfluenceNode();
                InfluenceNode influenceNode1 = BorderNodes.RecycleObject() ?? new InfluenceNode();// = new Empire.InfluenceNode();
                //this.BorderNodes.pendingRemovals.TryPop(out influenceNode1);
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.usePlanetaryProjection)
                {
                    influenceNode1.KeyedObject = planet;
                    influenceNode1.Position = planet.Position;
                }
                else
                {
                    influenceNode1.KeyedObject = planet.system;
                    influenceNode1.Position = planet.system.Position;
                }
                influenceNode1.Radius = 1f;
				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.usePlanetaryProjection)
                {
                    foreach (Building t in planet.BuildingList)
                    {
                        if (influenceNode1.Radius < t.ProjectorRange)
                            influenceNode1.Radius = t.ProjectorRange;
                    }
                }
                else
                {
                    influenceNode1.Radius = this.isFaction ? 20000f : Empire.ProjectorRadius + (float)(10000.0 * (double)planet.Population / 1000.0);
                }

                BorderNodes.Add(influenceNode1);
                InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();

                influenceNode2.KeyedObject = (object)planet;
                influenceNode2.Position = planet.Position;
                influenceNode2.Radius = 1f; //this == Empire.Universe.PlayerEmpire ? 300000f * this.data.SensorModifier : 600000f * this.data.SensorModifier;
                SensorNodes.Add(influenceNode2);
                InfluenceNode influenceNode3 = this.SensorNodes.RecycleObject() ?? new Empire.InfluenceNode();
                influenceNode3.KeyedObject = (object)planet;
                influenceNode3.Position = planet.Position;
                influenceNode3.Radius = this.isFaction ? 1f : 1f * this.data.SensorModifier;
                foreach (Building t in planet.BuildingList)
                {
                    if (t.SensorRange * data.SensorModifier > influenceNode3.Radius)
                        influenceNode3.Radius = t.SensorRange * this.data.SensorModifier;
                }
                this.SensorNodes.Add(influenceNode3);
            }
            foreach (Mole mole in data.MoleList)   // Moles are spies who have successfuly been planted during 'Infiltrate' type missions, I believe - Doctor
                SensorNodes.Add(new InfluenceNode()
                {
                    Position = Universe.PlanetsDict[mole.PlanetGuid].Position,
                    Radius = 100000f * this.data.SensorModifier
                });
            this.Inhibitors.Clear();
            foreach (Ship ship in this.OwnedShips)
            {
                if (ship.InhibitionRadius > 0.0f)
                    this.Inhibitors.Add(ship);
                InfluenceNode influenceNode = this.SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode.Position = ship.Center;
                influenceNode.Radius = ship.SensorRange;
                influenceNode.KeyedObject = ship;
                this.SensorNodes.Add(influenceNode);
            }

            foreach (Ship ship in this.OwnedProjectors)
            {
                    if (ship.InhibitionRadius > 0f)
                        Inhibitors.Add(ship);

                    InfluenceNode influenceNodeS = this.SensorNodes.RecycleObject() ?? new Empire.InfluenceNode();
                    InfluenceNode influenceNodeB = this.BorderNodes.RecycleObject() ?? new Empire.InfluenceNode();
                    
                    influenceNodeS.Position = ship.Center;
                    influenceNodeS.Radius = ProjectorRadius;  //projectors used as sensors again
                    influenceNodeS.KeyedObject = (object)ship;

                    influenceNodeB.Position = ship.Center;
                    influenceNodeB.Radius = ProjectorRadius;  //projectors used as sensors again
                    influenceNodeB.KeyedObject = (object)ship;
                    
                    SensorNodes.Add(influenceNodeS);
                    BorderNodes.Add(influenceNodeB);
            }
            BorderNodes.ClearPendingRemovals();
            SensorNodes.ClearPendingRemovals();
            using (BorderNodes.AcquireReadLock())
            foreach (InfluenceNode item5 in BorderNodes)
            {
                foreach (InfluenceNode item6 in BorderNodes)
                {
                    if (item6.KeyedObject == item5.KeyedObject && (double)item6.Radius < (double)item5.Radius)
                        BorderNodes.QueuePendingRemoval(item6);
                }
            }
            BorderNodes.ApplyPendingRemovals();
            
        }

        private void TakeTurn()
        {
            //Added by McShooterz: Home World Elimination game mode
            if (!isFaction && !data.Defeated 
                && (OwnedPlanets.Count == 0 || GlobalStats.EliminationMode 
                && Capital != null && Capital.Owner != this))
            {
                SetAsDefeated();
                if (Universe.PlayerEmpire == this)
                {
                    foreach(Ship ship in Universe.MasterShipList)
                    {
                        ship.Die(null, true);
                    }
                    Universe.Paused = true;
                    HelperFunctions.CollectMemory();
                    Universe.ScreenManager.AddScreen(new YouLoseScreen());
                    Universe.Paused = false;
                    return;
                }
                else
                    Universe.NotificationManager.AddEmpireDiedNotification(this);
                return;
            }

            var list1 = new Array<Planet>();
            foreach (Planet planet in OwnedPlanets)
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
            if (this.Money < 0.0 && !this.isFaction)
            {
                this.data.TurnsBelowZero += (short)(1+-1*(this.Money) /500);
            }
            else
            {
                --this.data.TurnsBelowZero;
                if (this.data.TurnsBelowZero < 0)
                    this.data.TurnsBelowZero = 0;
            }
            float MilitaryStrength = 0.0f;
            string starDate = Universe.StarDate.ToString("#.0");
            for (int index = 0; index < this.OwnedShips.Count; ++index)
            {
                Ship ship = this.OwnedShips[index];
                MilitaryStrength += ship.GetStrength();

                if (!this.data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(starDate))
                    ++StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].ShipCount;
            }
            if (!this.data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(starDate))
            {
                StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].MilitaryStrength = MilitaryStrength;
                StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].TaxRate = this.data.TaxRate;
            }
            if (isPlayer)
            {
                if (Universe.StarDate > 1060.0f)
                {
#if !DEBUG
                    try
#endif
                    {
                        float num2 = 0.0f;
                        float num3 = 0.0f;
                        Array<Empire> list2 = new Array<Empire>();
                        foreach (Empire empire in EmpireManager.Empires)
                        {
                            if (!empire.isFaction && !empire.data.Defeated && empire != EmpireManager.Player)
                            {
                                num2 += (float)empire.TotalScore;
                                if ((double)empire.TotalScore > (double)num3)
                                    num3 = (float)empire.TotalScore;
                                if (empire.data.DiplomaticPersonality.Name == "Aggressive" || empire.data.DiplomaticPersonality.Name == "Ruthless" || empire.data.DiplomaticPersonality.Name == "Xenophobic")
                                    list2.Add(empire);
                            }
                        }
                        float num4 = (float)EmpireManager.Player.TotalScore;
                        float num5 = num2 + num4;
                        if (num4 > 0.5f * num5)
                        {
                            if (num4 > num3 * 2.0)
                            {
                                if (list2.Count >= 2)
                                {
                                    Empire biggest = list2.OrderByDescending(emp => emp.TotalScore).First();
                                    Array<Empire> list3 = new Array<Empire>();
                                    foreach (Empire empire in list2)
                                    {
                                        if (empire != biggest && empire.GetRelations(biggest).Known && biggest.TotalScore * 0.660000026226044 > empire.TotalScore)
                                            list3.Add(empire);
                                    }
                                    //Added by McShooterz: prevent AI from automatically merging together
                                    if (list3.Count > 0 && !GlobalStats.PreventFederations)
                                    {
                                        Empire strongest = list3.OrderByDescending(emp => biggest.GetRelations(emp).GetStrength()).First();
                                        if (!biggest.GetRelations(strongest).AtWar)
                                            Ship.universeScreen.NotificationManager.AddPeacefulMergerNotification(biggest, strongest);
                                        else
                                            Ship.universeScreen.NotificationManager.AddSurrendered(biggest, strongest);
                                        biggest.AbsorbEmpire(strongest);
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
                    Universe.NotificationManager.AddMoneyWarning();
                bool allEmpiresDead = true;
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (empire.GetPlanets().Count > 0 && !empire.isFaction && !empire.MinorRace && empire != this)
                    {
                        allEmpiresDead = false;
                        break;
                    }
                }
                if (allEmpiresDead)
                {
                    Universe.ScreenManager.AddScreen((GameScreen)new YouWinScreen());
                    return;
                }
                else
                {
                    foreach (Planet planet in this.OwnedPlanets)
                    {
                        if (!this.data.IsRebelFaction)
                            StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                        if (planet.HasWinBuilding)
                        {
                            Universe.ScreenManager.AddScreen((GameScreen)new YouWinScreen(Localizer.Token(5085)));
                            return;
                        }
                    }
                }
            }
            foreach (Planet planet in this.OwnedPlanets)
            {
                if (!this.data.IsRebelFaction)
                    StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                int num2 = planet.HasWinBuilding ? 1 : 0;
            }
            if (this.data.TurnsBelowZero > 0  && (this.Money < 0.0 && !Universe.Debug))// && this.isPlayer)) // && this == Empire.Universe.PlayerEmpire)
            {
                if (this.data.TurnsBelowZero >= 25)
                {
                    Empire rebelsFromEmpireData = EmpireManager.GetEmpireByName(this.data.RebelName);
                    Log.Info("Rebellion for: "+ data.Traits.Name);
                    if(rebelsFromEmpireData == null)
                    foreach (Empire rebel in EmpireManager.Empires)
                    {
                        if (rebel.data.PortraitName == this.data.RebelName)
                        {
                            Log.Info("Found Existing Rebel: "+ rebel.data.PortraitName);
                            rebelsFromEmpireData = rebel;
                            break;
                        }
                    }
                    if (rebelsFromEmpireData == null)
                    {
                        rebelsFromEmpireData = CreatingNewGameScreen.CreateRebelsFromEmpireData(this.data, this);
                        if (rebelsFromEmpireData != null)
                        {
                            rebelsFromEmpireData.data.IsRebelFaction  = true;
                            rebelsFromEmpireData.data.Traits.Name     = data.RebelName;
                            rebelsFromEmpireData.data.Traits.Singular = data.RebelSing;
                            rebelsFromEmpireData.data.Traits.Plural   = data.RebelPlur;
                            rebelsFromEmpireData.isFaction = true;
                            foreach (Empire key in EmpireManager.Empires)
                            {
                                key.AddRelation(rebelsFromEmpireData);
                                rebelsFromEmpireData.AddRelation(key);
                            }
                            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                            {
                                solarSystem.ExploredDict.Add(rebelsFromEmpireData, false);
                                foreach (Planet planet in solarSystem.PlanetList)
                                    planet.ExploredDict.Add(rebelsFromEmpireData, false);

                            }
                            EmpireManager.Add(rebelsFromEmpireData);
                            this.data.RebellionLaunched = true;
                        }
                    }

                    if (rebelsFromEmpireData != null)
                    {
                        Vector2 weightedCenter = GetWeightedCenter();
                        if (OwnedPlanets.FindMax(out Planet planet, p => weightedCenter.SqDist(p.Position)))
                        {
                            if (isPlayer)
                                Universe.NotificationManager.AddRebellionNotification(planet, rebelsFromEmpireData); //Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable
                            for (int index = 0; index < planet.Population / 1000; ++index)
                            {
                                foreach (var keyValuePair in ResourceManager.TroopsDict)
                                {
                                    if (!WeCanBuildTroop(keyValuePair.Key))
                                        continue;

                                    Troop troop = ResourceManager.CreateTroop(keyValuePair.Value, rebelsFromEmpireData);
                                    troop.Name = Localizer.Token(rebelsFromEmpireData.data.TroopNameIndex);
                                    troop.Description = Localizer.Token(rebelsFromEmpireData.data.TroopDescriptionIndex);
                                    planet.AssignTroopToTile(troop); //Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable)
                                    break;
                                }
                            }
                        }
                        //if(this.data.TurnsBelowZero >10 && this.data.TurnsBelowZero < 20)
                        {
                            Ship pirate = null;
                            using (GetShips().AcquireReadLock())
                            foreach (Ship pirateChoice in GetShips())
                            {
                                if (pirateChoice == null || !pirateChoice.Active)
                                    continue;
                                pirate = pirateChoice;
                                break;
                            }
                            if (pirate != null)
                            {
                                pirate.loyalty = rebelsFromEmpireData;
                                this.RemoveShip(pirate);
                                //Empire.Universe.NotificationManager.AddRebellionNotification(planet, empireByName);
                            }

                        }
                    }
                    else Log.Info("Rebellion Failure: {0}", this.data.RebelName);
                    data.TurnsBelowZero = 0;
                }
               
            }
            this.CalculateScore();
            //Process technology research
            //Parallel.Invoke(
            //    ()=>
                {
                    if (!string.IsNullOrEmpty(this.ResearchTopic))
                    {
                        this.Research = 0;
                        foreach (Planet planet in this.OwnedPlanets)
                            this.Research += planet.NetResearchPerTurn;
                        float research = this.Research + this.leftoverResearch;
                        TechEntry tech;
                        if (this.TechnologyDict.TryGetValue(this.ResearchTopic, out tech))
                        {
                            float cyberneticMultiplier = 1.0f;
                            if (this.data.Traits.Cybernetic > 0)
                            {
                                foreach (Technology.UnlockedBuilding buildingName in tech.Tech.BuildingsUnlocked)
                                {
                                    Building building = ResourceManager.GetBuilding(buildingName.Name);
                                    if (building.PlusFlatFoodAmount > 0 || building.PlusFoodPerColonist > 0 || building.PlusTerraformPoints > 0)
                                    {
                                        cyberneticMultiplier = .5f;
                                        break;
                                    }

                                }
                            }
                            if ((tech.Tech.Cost*cyberneticMultiplier) * UniverseScreen.GamePaceStatic - tech.Progress > research)
                            {
                                tech.Progress += research;
                                this.leftoverResearch = 0f;
                                research = 0;
                            }
                            else
                            {
    
                                
                                research -= (tech.Tech.Cost * cyberneticMultiplier) * UniverseScreen.GamePaceStatic - tech.Progress;
                                tech.Progress = tech.Tech.Cost * UniverseScreen.GamePaceStatic;
                                this.UnlockTech(this.ResearchTopic);
                                if (this.isPlayer)
                                    Universe.NotificationManager.AddResearchComplete(this.ResearchTopic, this);
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
                        this.leftoverResearch = research;
                    }
                    else if (this.data.ResearchQueue.Count > 0)
                        this.ResearchTopic = this.data.ResearchQueue[0];
                    
                }//,

            //    ()=>
                {

                    if (this == Universe.PlayerEmpire)
                    {
                        foreach (var kv in Relationships)
                            kv.Value.UpdatePlayerRelations(this, kv.Key);
                    }
                    else if (!isFaction)
                        UpdateRelationships();

                    if (this.isFaction)
                        this.GSAI.FactionUpdate();
                    else if (!this.data.Defeated)
                        this.GSAI.Update();
                    if ((double)this.Money > (double)this.data.CounterIntelligenceBudget)
                    {
                        this.Money -= this.data.CounterIntelligenceBudget;
                        foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
                        {
                            var relationWithUs = keyValuePair.Key.GetRelations(this);
                            relationWithUs.IntelligencePenetration -= data.CounterIntelligenceBudget / 10f;
                            if (relationWithUs.IntelligencePenetration < 0.0f)
                            relationWithUs.IntelligencePenetration = 0.0f;
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
            if (this.data.ScoreAverage >= 120)  //fbedard: reset every 60 turns
            {
                this.data.MilitaryScoreTotal = this.MilitaryScore * 60f;
                this.data.ScoreAverage = 60;
            }
        }

        public void AbsorbEmpire(Empire target)
        {
            foreach (Planet planet in target.GetPlanets())
            {
                AddPlanet(planet);
                planet.Owner = this;
                if (!planet.system.OwnerList.Contains(this))
                {
                    planet.system.OwnerList.Add(this);
                    planet.system.OwnerList.Remove(target);
                }
            }
            foreach (KeyValuePair<Guid, SolarSystem> keyValuePair in Universe.SolarSystemDict)
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
            target.ClearAllPlanets();
            foreach (Ship ship in target.GetShips())
            {
                this.OwnedShips.Add(ship);
                ship.loyalty = this;
                ship.fleet = null;
                ship.GetAI().State = AIState.AwaitingOrders;
                ship.GetAI().OrderQueue.Clear();
            }
            foreach (Ship ship in target.GetProjectors())
            {
                this.OwnedProjectors.Add(ship);
                ship.loyalty = this;
                ship.fleet = null;
                ship.GetAI().State = AIState.AwaitingOrders;
                ship.GetAI().OrderQueue.Clear();
            }
            target.GetShips().Clear();
            target.GetProjectors().Clear();
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
            if (this != EmpireManager.Player)
            {
                this.data.difficulty = Difficulty.Brutal;
                //lock (GlobalStats.TaskLocker)
                {
                    this.GSAI.TaskList.ForEach(item_7=>//foreach (MilitaryTask item_7 in (Array<MilitaryTask>)this.GSAI.TaskList)
                        { item_7.EndTask(); }, false, false, false);
                    this.GSAI.TaskList.ApplyPendingRemovals();
                }
                this.GSAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                this.GSAI.DefensiveCoordinator.DefenseDict.Clear();
                this.ForcePool.Clear();
                //foreach (Ship s in (Array<Ship>)this.OwnedShips) //.OrderByDescending(experience=> experience.experience).ThenBy(strength=> strength.BaseStrength))
                foreach (Ship s in this.OwnedShips)
                {
                    //added by gremlin Do not include 0 strength ships in defensive force pool
                    s.GetAI().OrderQueue.Clear();
                    s.GetAI().State = AIState.AwaitingOrders;
                    ForcePoolAdd(s);
                }
                if (this.data.Traits.Cybernetic != 0)
                {
                    foreach (Planet planet in OwnedPlanets)
                    {
                        Array<Building> list = new Array<Building>();
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
            foreach (Agent agent in (Array<Agent>)target.data.AgentList)
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
            if (s.shipData.Role <= ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian || s.fleet != null )
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
            foreach (Ship ship in (Array<Ship>)this.ForcePool)
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
        private void AssessFreighterNeeds()
        {
            int tradeShips = 0;
            int passengerShips = 0;

            float moneyForFreighters = (this.Money * .1f) * .1f - this.freighterBudget;
            this.freighterBudget = 0;

            int freighterLimit = GlobalStats.FreighterLimit;

            Array<Ship> unusedFreighters = new Array<Ship>();
            Array<Ship> assignedShips = new Array<Ship>();
            // Array<Ship> scrapCheck = new Array<Ship>();
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
                //fbedard: civilian can be freighter too!
                //if (!(ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.Role == ShipData.RoleName.freighter) || ship.isColonyShip || ship.CargoSpace_Max == 0 || ship.GetAI() == null)
                if ((ship.shipData.ShipCategory != ShipData.Category.Civilian && ship.shipData.Role != ShipData.RoleName.freighter) || ship.isColonyShip || ship.CargoSpace_Max == 0 || ship.GetAI() == null || ship.isConstructor
                    || ship.GetAI().State == AIState.Refit || ship.GetAI().State == AIState.Scrap
                    )
                {
                    if (ship.GetAI().State == AIState.PassengerTransport || ship.GetAI().State == AIState.SystemTrader)
                        ship.GetAI().State = AIState.AwaitingOrders;
                    continue;
                }
                this.freighterBudget += ship.GetMaintCost();
                if (ship.GetAI().State != AIState.AwaitingOrders && ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader)
                    continue;
                    
                if ((ship.GetAI().start == null || ship.GetAI().end == null) ||  ship.GetAI().OrderQueue.Count ==0) // && //(ship.GetAI().State == AIState.SystemTrader || ship.GetAI().State == AIState.PassengerTransport) &&
                {
                    // if ()  //fbedard: dont scrap loaded ship
                    if (ship.TradeTimer != 0 && ship.TradeTimer < 1)
                        unusedFreighters.Add(ship);
                    else
                        assignedShips.Add(ship);


                }
                else if (ship.GetAI().State == AIState.PassengerTransport)
                {
                    passengerShips++;
                }
                else if (ship.GetAI().State == AIState.SystemTrader)
                    tradeShips++;
                else
                {
                    unusedFreighters.Add(ship);
                }
            }
            int totalShipcount = tradeShips + passengerShips + unusedFreighters.Count;
            totalShipcount = totalShipcount > 0 ? totalShipcount : 1;
            freighterBudget = freighterBudget > 0 ? freighterBudget : .1f;
            float avgmaint = freighterBudget / totalShipcount;
            moneyForFreighters -= freighterBudget;
          
            int minFreightCount = 3 + this.getResStrat().ExpansionPriority;

            int skipped = 0;

            while (unusedFreighters.Count - skipped > minFreightCount)
            {
                Ship ship = unusedFreighters[0 + skipped];                
                if ( ship.TradeTimer < 1 && ship.CargoSpace_Used ==0)
                {
                    ship.GetAI().OrderScrapShip();
                    unusedFreighters.Remove(ship);
                }
                else skipped++;
            }
            unusedFreighters.AddRange(assignedShips);
            assignedShips.Clear();
            int freighters = unusedFreighters.Count;
            //get number of freighters being built


            int type = 1;
            while (freighters > 0 )
            {
                Ship ship = unusedFreighters[0];                
                //assignedShips.Add(ship);
                unusedFreighters.Remove(ship);
                freighters--;
                if (ship.GetAI().State != AIState.Flee)
                {
                    switch (type)
                    {
                        case 1:
                            {
                                ship.GetAI().State = AIState.SystemTrader;
                                ship.GetAI().OrderTrade(0.1f);
                                type++;
                                break;
                            }
                        case 2:
                            {
                                ship.GetAI().State = AIState.PassengerTransport;
                                ship.GetAI().OrderTransportPassengers(0.1f);
                                type++;
                                break;
                            }

                        default:
                            break;
                    }
                    if (type > 2)
                        type = 1;
                    if (ship.GetAI().start == null && ship.GetAI().end == null)
                        assignedShips.Add(ship);



                }


            }
            unusedFreighters.AddRange(assignedShips);
            freighters = 0;// unusedFreighters.Count;
            int goalLimt = 1  + this.getResStrat().IndustryPriority;
            foreach (Goal goal in (Array<Goal>)this.GSAI.Goals)
            {
                if (goal.GoalName == "IncreaseFreighters")
                {
                    ++freighters;
                    goalLimt--;
                }
                else if (goal.GoalName == "IncreasePassengerShips")
                {
                    goalLimt--;
                    ++freighters;
                }
            }
            moneyForFreighters -= freighters * avgmaint;
            freighters += unusedFreighters.Count ;
            if (moneyForFreighters > 0 && freighters < minFreightCount && goalLimt >0)
            {
                freighters++;
                this.GSAI.Goals.Add(new Goal(this)
                {
                    GoalName = "IncreaseFreighters",
                    type = GoalType.BuildShips
                });
                moneyForFreighters -= avgmaint;

                //if (freighters < minFreightCount && moneyForFreighters > 0)
                //{
                //    freighters++;
                //    this.GSAI.Goals.Add(new Goal(this)
                //    {
                //        type = GoalType.BuildShips,
                //        GoalName = "IncreasePassengerShips"
                //    });
                //}
            }

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
            using (OwnedPlanets.AcquireReadLock())
            foreach (Planet planet in OwnedPlanets)
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
                foreach (Ship ship in (Array<Ship>)this.OwnedShips)
                {
                    if (num < 2)
                    {
                        if (ship.shipData.Role == ShipData.RoleName.scout && !ship.isPlayerShip())
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
                    foreach (Goal goal in (Array<Goal>)this.GSAI.Goals)
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
                    foreach (Goal goal in (Array<Goal>)this.GSAI.Goals)
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
                foreach (Ship ship in (Array<Ship>)this.OwnedShips)
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
                this.RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
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
                this.RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
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
            if (ship.Name == "Subspace Projector") // @todo Really??? Haha..
            {
                OwnedProjectors.Remove(ship);
            }
            else
            {
                OwnedShips.Remove(ship);
            }
            ship.fleet = null;
            GetGSAI().DefensiveCoordinator.remove(ship);
            
            ship.GetAI().OrderQueue.Clear();
           
            ship.GetAI().State = AIState.AwaitingOrders;
            ship.RemoveFromAllFleets();
        }

        public class InfluenceNode
        {
            public Vector2 Position;
            public object KeyedObject;
            public bool DrewThisTurn;
            public float Radius;

            public void Wipe()
            {
                Position     = Vector2.Zero;
                KeyedObject  = null;
                DrewThisTurn = false;
                Radius       = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Empire() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            ForcePool?.Dispose(ref ForcePool);
            BorderNodes?.Dispose(ref BorderNodes);
            SensorNodes?.Dispose(ref SensorNodes);
            KnownShips?.Dispose(ref KnownShips);
            OwnedShips?.Dispose(ref OwnedShips);
            DefensiveFleet?.Dispose(ref DefensiveFleet);
            GSAI?.Dispose(ref GSAI);
            data.AgentList?.Dispose(ref data.AgentList);
            data.MoleList?.Dispose(ref data.MoleList);
            LockPatchCache?.Dispose(ref LockPatchCache);
            OwnedPlanets?.Dispose(ref OwnedPlanets);
            OwnedProjectors?.Dispose(ref OwnedProjectors);
            OwnedSolarSystems?.Dispose(ref OwnedSolarSystems);

        }
    }
}
