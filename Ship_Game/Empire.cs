using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Ship_Game.AI;
using Newtonsoft.Json;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies.AI;

namespace Ship_Game
{
    using static ShipBuilder;
     
    public sealed class Empire : IDisposable
    {
        public float ProjectorRadius => Universe.SubSpaceProjectors.Radius;
        //private Map<int, Fleet> FleetsDict = new Map<int, Fleet>();
        private readonly Map<int, Fleet> FleetsDict    = new Map<int, Fleet>();
        public readonly Map<string, bool> UnlockedHullsDict     = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedTroopDict     = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedBuildingsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedModulesDict   = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Array<Troop> UnlockedTroops = new Array<Troop>();
        
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
        private Array<Ship> ForcePool = new Array<Ship>();
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;

        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesnt normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        public bool isFaction;
        public float Research;
        public Color EmpireColor;
        public static UniverseScreen Universe;
        //public Vector4 VColor;          //Not referenced in code, removing to save memory
        private EmpireAI EmpireAI;
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
        public bool canBuildSupportShips;
        public float currentMilitaryStrength;
        public float freighterBudget;
        public float cargoNeed = 0;
        public float MaxResearchPotential = 10;        
        public HashSet<string> ShipTechs = new HashSet<string>();
        //added by gremlin
        private float leftoverResearch;
        //public float exportPTrack;
        //public float exportFTrack;        //Removed by Gretman, these were only used for legacy debug visualization
        //public float averagePLanetStorage;
        [XmlIgnore][JsonIgnore] public Map<Point, Map<Point, PatchCacheEntry>> PathCache = new Map<Point, Map<Point, PatchCacheEntry>>();
        [XmlIgnore][JsonIgnore] public ReaderWriterLockSlim LockPatchCache = new ReaderWriterLockSlim();
        [XmlIgnore][JsonIgnore] public int pathcacheMiss = 0;
        [XmlIgnore][JsonIgnore] public byte[,] grid;
        [XmlIgnore][JsonIgnore] public int granularity = 0;
        [XmlIgnore][JsonIgnore] public int AtWarCount = 0;
        [XmlIgnore][JsonIgnore] public Array<string> BomberTech      = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> TroopShipTech   = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> CarrierTech     = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> SupportShipTech = new Array<string>();
        [XmlIgnore][JsonIgnore] public Ship BoardingShuttle => ResourceManager.ShipsDict["Assault Shuttle"];
        [XmlIgnore][JsonIgnore] public Ship SupplyShuttle   => ResourceManager.ShipsDict["Supply_Shuttle"];
        [XmlIgnore][JsonIgnore] public Planet[] RallyPoints = Empty<Planet>.Array;

        public Dictionary<ShipData.RoleName, string> PreferredAuxillaryShips = new Dictionary<ShipData.RoleName, string>();

        public void TriggerAllShipStatusUpdate()
        {
            foreach (Ship ship in OwnedShips)//@todo can make a global ship unlock flag. 
                ship.shipStatusChanged = true;
        }
        public Planet FindNearestRallyPoint(Vector2 location)
        {
            if (RallyPoints.Length == 0) return null;
            return RallyPoints?.FindMin(p => p.Center.SqDist(location)) ?? OwnedPlanets?.FindMin(p => p.Center.SqDist(location));
        }

        public Planet[] RallyShipYards => RallyPoints.FilterBy(sy => sy.HasShipyard);

        public Planet RallyShipYardNearestTo(Vector2 position)
        {
            Planet p = RallyPoints.Length == 0
                ? null
                : RallyPoints.FindMaxFiltered(planet => planet?.HasShipyard ?? false,
                    planet => -position.SqDist(planet?.Center ?? Vector2.Zero));
            if (p == null)
                Log.Warning($"RallyShipYardNearestTo Had null elements: RallyPoints {RallyPoints.Length}");
            return p;
        }

        public Planet[] BestBuildPlanets => RallyPoints.FilterBy(planet =>
        planet.HasShipyard && planet.ParentSystem.combatTimer <= 0
        && planet.DevelopmentLevel > 2
        && planet.colonyType != Planet.ColonyType.Research
        && (planet.colonyType != Planet.ColonyType.Industrial || planet.DevelopmentLevel > 3)
        );

        public Planet PlanetToBuildAt (float productionNeeded)
        {
            Planet planet = OwnedPlanets.FindMin(p => p.SbProduction.EstimateMinTurnsToBuildShip(productionNeeded));
            return planet;
        }
        public string Name => data.Traits.Name;

        // Empire unique ID. If this is 0, then this empire is invalid!
        // Set in EmpireManager.cs
        public int Id;

        public Empire()
        {
            // @note @todo This is very flaky and weird!
            UpdateTimer = RandomMath.RandomBetween(.02f, .3f);
        }

        public Empire(Empire parentEmpire)
        {
            TechnologyDict = parentEmpire.TechnologyDict;
        }

        public class PatchCacheEntry
        {
            public readonly Array<Vector2> Path;
            public int CacheHits;
            public PatchCacheEntry(Array<Vector2> path) { Path = path; }
        }

        public Map<int, Fleet> GetFleetsDict() => FleetsDict;

        public Fleet GetFleet(int key)
        {
            FleetsDict.TryGetValue(key, out Fleet fleet);
            return fleet;
        }

        public Fleet FirstFleet
        {
            get { return FleetsDict[1]; }
            set
            {
                for (int index = 0; index < FleetsDict[1].Ships.Count; index++)
                {
                    Ship s = FleetsDict[1].Ships[index];
                    s?.fleet?.RemoveShip(s);
                }
                FleetsDict[1] = value;
            }
        }

        public void SpawnHomePlanet(Planet newOrbital)
        {
            newOrbital.Owner           = this;
            Capital                    = newOrbital;
            newOrbital.InitializeSliders(this);
            AddPlanet(newOrbital);
            newOrbital.SetPlanetAttributes(26f);
            newOrbital.MineralRichness = 1f + data.Traits.HomeworldRichMod;
            newOrbital.Fertility       = 2f + data.Traits.HomeworldFertMod;
            newOrbital.MaxPopulation   = 14000f + 14000f * data.Traits.HomeworldSizeMod;
            newOrbital.Population      = 14000f;
            newOrbital.FoodHere        = 100f;
            newOrbital.ProductionHere  = 100f;
            newOrbital.HasShipyard     = true;
            newOrbital.AddGood("ReactorFuel", 1000);
            ResourceManager.CreateBuilding("Capital City").SetPlanet(newOrbital);
            ResourceManager.CreateBuilding("Space Port").SetPlanet(newOrbital);
            if (GlobalStats.HardcoreRuleset)
            {
                ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
                ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
                ResourceManager.CreateBuilding("Mine Fissionables").SetPlanet(newOrbital);
                ResourceManager.CreateBuilding("Fuel Refinery").SetPlanet(newOrbital);
            }
        }

        public void SetRallyPoints()
        {
            Array<Planet> rallyPlanets = new Array<Planet>();
            var goodSystems = new HashSet<SolarSystem>();
            foreach (SolarSystem systemCheck in OwnedSolarSystems)
            {
                if (systemCheck.combatTimer > 10) continue;
                bool systemHasUnfriendlies = false;
                foreach (Empire empire in systemCheck.OwnerList)
                {
                    if (!IsEmpireAttackable(empire)) continue;
                    systemHasUnfriendlies = true;
                    break;
                }
                if (systemHasUnfriendlies) continue;                     

                foreach (Planet planet in systemCheck.PlanetList)
                {
                    if (planet.Owner != this) continue;
                    rallyPlanets.Add(planet);
                }

            }
            if (rallyPlanets.Count > 0)
            {
                RallyPoints = rallyPlanets.ToArray();
                return;
            }

            foreach (Planet planet in OwnedPlanets)
            {
                if (planet.HasShipyard)
                    rallyPlanets.Add(planet);
            }

            if (rallyPlanets.Count > 0)
            {
                RallyPoints = rallyPlanets.ToArray();
                return;
            }
            rallyPlanets.Add(OwnedPlanets.FindMax(planet => planet.GrossProductionPerTurn));
            RallyPoints = rallyPlanets.ToArray();
            if (RallyPoints.Length == 0)
                Log.Error("SetRallyPoint: No Planets found");
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
            data.Defeated = false;
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
            OwnedShips.Clear();
            Relationships.Clear();
            EmpireAI = null;
            HostilesPresent.Clear();
            ForcePool.Clear();
            KnownShips.Clear();
            SensorNodes.Clear();
            BorderNodes.Clear();
            TechnologyDict.Clear();
            SpaceRoadsList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Dispose();
            FleetsDict.Clear();
            UnlockedBuildingsDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedTroopDict.Clear();
            UnlockedTroops.Clear();
            Inhibitors.Clear();
            OwnedProjectors.Clear();
            ShipsToAdd.Clear();
            ShipsWeCanBuild.Clear();
            structuresWeCanBuild.Clear();
            data.MoleList.Clear();
            data.OwnedArtifacts.Clear();
            data.AgentList.Clear();
            data.ResearchQueue.Clear();
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
                ship.AI.OrderQueue.Clear();
                ship.AI.State = AIState.AwaitingOrders;
            }
            EmpireAI.Goals.Clear();
            EmpireAI.TaskList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            
            Empire rebels = EmpireManager.CreateRebelsFromEmpireData(data, this);
            var rebelEmpireIndex = EmpireManager.Empires.IndexOf(rebels);
            SerializableDictionary<int, Snapshot> statDict = StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")];
             statDict[rebelEmpireIndex] = new Snapshot(Universe.StarDate);

            // StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")].Add(EmpireManager.Empires.IndexOf(rebels), new Snapshot(Universe.StarDate));
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
                ship.AI.OrderQueue.Clear();
                ship.AI.State = AIState.AwaitingOrders;
            }
            EmpireAI.Goals.Clear();
            EmpireAI.TaskList.Clear();
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
            using (SensorNodes.AcquireReadLock())
                for (int x = 0; x < SensorNodes.Count; x++)
                {
                    var node = SensorNodes[x];
                    if (node.Position.InRadius(point, node.Radius))
                        return true;
                }
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

        public IReadOnlyList<Troop> GetUnlockedTroops() => UnlockedTroops;

        public Map<string, bool> GetBDict()
        {
            return UnlockedBuildingsDict;
        }

        public bool IsModuleUnlocked(string moduleUID)
        {
            return UnlockedModulesDict.TryGetValue(moduleUID, out bool found) && found;
        }

        public void UnlockModuleForEmpire(string moduleUID)
        {
            UnlockedModulesDict[moduleUID] = true;
        }

        public Map<string, TechEntry> GetTDict()
        {
            return TechnologyDict;
        }


        public TechEntry GetTechEntry(string tech)
        {
            if (TechnologyDict.TryGetValue(tech, out TechEntry techEntry))
                return techEntry;
            Log.Error($"Attempt to find tech {tech} failed");
            return TechEntry.None;

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

        public SolarSystem[] GetBorderSystems(Empire them)
        {
            var solarSystems = new HashSet<SolarSystem>();

            foreach (var solarSystem in GetOwnedSystems())
            {
                SolarSystem ss = them.GetOwnedSystems().FindMin(s => s.Position.SqDist(solarSystem.Position));
                if (ss == null)
                    break;
                if (!ss.IsExploredBy(this)) continue;
                solarSystems.Add(ss);
            }
            return solarSystems.ToArray(); 
        }

        public void UpdatePlanetIncomes()
        {
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets) planet.UpdateIncomes(false);
        }

        public void RemovePlanet(Planet planet)
        {
            OwnedPlanets.Remove(planet);
            if (OwnedPlanets.All(p => p.ParentSystem != planet.ParentSystem)) // system no more in owned planets?
            {
                OwnedSolarSystems.Remove(planet.ParentSystem);
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
            if (planet.ParentSystem == null)
                throw new ArgumentNullException(nameof(planet.ParentSystem));

            if (!OwnedSolarSystems.Contains(planet.ParentSystem))
                OwnedSolarSystems.Add(planet.ParentSystem);
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
        public Array<Ship> GetShipsFromOffensePools(bool onlyAO = false)
        {
            Array<Ship> ships = new Array<Ship>();
            foreach (AO ao in GetGSAI().AreasOfOperations)
            {
                ships.AddRange(ao.GetOffensiveForcePool());
            }
            if(!onlyAO)
                ships.AddRange(ForcePool); 
            return ships;
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
            EmpireAI = new EmpireAI(this);
            for (int key = 1; key < 10; ++key)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(key);
                FleetsDict.Add(key, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            InitTechs();

            foreach (var kv in ResourceManager.HullsDict)     UnlockedHullsDict[kv.Value.Hull] = false;
            foreach (var tt in ResourceManager.TroopTypes)    UnlockedTroopDict[tt]            = false;
            foreach (var kv in ResourceManager.BuildingsDict) UnlockedBuildingsDict[kv.Key]    = false;
            foreach (var kv in ResourceManager.ShipModules)   UnlockedModulesDict[kv.Key]      = false;            
            //unlock from empire data file
            foreach (string building in data.unlockBuilding)  UnlockedBuildingsDict[building]  = true;
            UnlockedTroops.Clear();

            //unlock racial techs
            foreach (var kv in TechnologyDict)
            {
                var techEntry = kv.Value;
                data.Traits.TechUnlocks(techEntry, this);

                if (techEntry.Unlocked)
                    techEntry.Progress = techEntry.Tech.Cost * UniverseScreen.GamePaceStatic;
            }
            //Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();

            foreach (var entry in TechnologyDict)
            {
                var tech = entry.Value.Tech;
                if (tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0 && !WeCanUseThis(tech, ourShips))                
                    entry.Value.shipDesignsCanuseThis = false;
               
            }
            foreach (var entry in TechnologyDict)
            {
                if (!entry.Value.shipDesignsCanuseThis)
                    entry.Value.shipDesignsCanuseThis = WeCanUseThisLater(entry.Value);                
   
            }
            foreach (var entry in TechnologyDict.OrderBy(hulls => hulls.Value.Tech.HullsUnlocked.Count >0))
            {
                AddToShipTechLists(entry.Value);
                if (!entry.Value.Unlocked)
                    continue;
      
                entry.Value.Unlocked = false;
                entry.Value.Unlock(this);
            }
            foreach (var kv in TechnologyDict.Where(hulls => hulls.Value.Tech.HullsUnlocked.Count > 0 && hulls.Value.Tech.RootNode != 1))
            {
                AddToShipTechLists(kv.Value);
                if (!kv.Value.Unlocked)
                    continue;
                kv.Value.Unlocked = false;
                kv.Value.Unlock(this);
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
            data.TechDelayTime = 4;
            if (EmpireManager.NumEmpires ==0)
                UpdateTimer = 0;       
        }

        private void InitTechs()
        {
            var unlockedTechs = new Array<TechEntry>();
            foreach (var kv in ResourceManager.TechTree)
            {
                TechEntry techEntry = new TechEntry
                {
                    Progress = 0.0f,
                    UID = kv.Key
                };

                //added by McShooterz: Checks if tech is racial, hides it, and reveals it only to races that pass
                if (kv.Value.RaceRestrictions.Count != 0 || kv.Value.RaceExclusions.Count != 0)
                {
                    techEntry.Discovered |= kv.Value.RaceRestrictions.Count == 0 && kv.Value.ComesFrom.Count >0;
                    kv.Value.Secret |= kv.Value.RaceRestrictions.Count != 0; ;
                    foreach (Technology.RequiredRace raceTech in kv.Value.RaceRestrictions)
                    {
                        if (raceTech.ShipType != data.Traits.ShipType) continue;
                        techEntry.Discovered = true;                                                                                 
                        break;
                    }
                    if (techEntry.Discovered)
                    {
                        foreach (Technology.RequiredRace raceTech in kv.Value.RaceExclusions)
                        {
                            if (raceTech.ShipType != data.Traits.ShipType) continue;
                            techEntry.Discovered = false;
                            kv.Value.Secret = true;
                            break;
                        }
                    }
                   
                    if (techEntry.Discovered)
                        techEntry.Unlocked = kv.Value.RootNode == 1;
                }
                else //not racial tech
                {
                    bool secret = kv.Value.Secret || (kv.Value.ComesFrom.Count == 0 && kv.Value.RootNode == 0);
                    techEntry.Unlocked = kv.Value.RootNode == 1 && !secret;
                    techEntry.Discovered = !secret;
                }



                if (isFaction || data.Traits.Prewarp == 1)
                {
                    techEntry.Unlocked = false;
                }
                TechnologyDict.Add(kv.Key, techEntry);                            
            }

        }



        private void AddToShipTechLists(TechEntry tech)
        {
            foreach (var module in tech.GetUnlockableModules(this))
            {
                PopulateShipTechLists(module.ModuleUID, tech.UID, tech.Unlocked);
            }
        }

        public Array<Ship> GetOurFactionShips()
        {
            var ourFactionShips = new Array<Ship>();
            foreach (var kv in ResourceManager.ShipsDict)
                if (kv.Value.shipData.ShipStyle == data.Traits.ShipType 
                    || kv.Value.shipData.ShipStyle == "Platforms" || kv.Value.shipData.ShipStyle == "Misc")
                    ourFactionShips.Add(kv.Value);
            return ourFactionShips;
        }

        public void InitializeFromSave()
        {
            EmpireAI = new EmpireAI(this);
            for (int key = 1; key < 1; ++key)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(key);
                FleetsDict.Add(key, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            foreach (var kv in ResourceManager.HullsDict)     UnlockedHullsDict[kv.Value.Hull] = false;
            foreach (var tt in ResourceManager.TroopTypes)    UnlockedTroopDict[tt]            = false;
            foreach (var kv in ResourceManager.BuildingsDict) UnlockedBuildingsDict[kv.Key]    = false;
            foreach (var kv in ResourceManager.ShipModules)   UnlockedModulesDict[kv.Key]      = false;
            UnlockedTroops.Clear();

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
                AddToShipTechLists(kv.Value);
                if (!kv.Value.Unlocked)
                    continue;
                kv.Value.Unlocked = false;
                UnlockTechFromSave(kv.Value);
            }

            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;
            foreach (string ship in data.unlockShips) // unlock ships from empire data
                ShipsWeCanBuild.Add(ship);

            UpdateShipsWeCanBuild();

            if (data.EconomicPersonality == null)
                data.EconomicPersonality = new ETrait { Name = "Generalists" };
            economicResearchStrategy = ResourceManager.EconStrats[data.EconomicPersonality.Name];

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
            return economicResearchStrategy;
        }

        public bool WeCanBuildTroop(string ID)
        {
            return UnlockedTroopDict.ContainsKey(ID) && UnlockedTroopDict[ID];
        }
        
        public void UnlockEmpireShipModule(string moduleUID, string techUID = "")
        {
            UnlockedModulesDict[moduleUID] = true;
            PopulateShipTechLists(moduleUID, techUID);
        }

        private void PopulateShipTechLists(string moduleUID, string techUID, bool addToMainShipTechs = true)
        {
            if (addToMainShipTechs)
                ShipTechs.Add(techUID);
            if (!isFaction)
            {
                switch (ResourceManager.GetModuleTemplate(moduleUID).ModuleType)
                {
                    case ShipModuleType.Hangar:
                        TroopShipTech.AddUnique(techUID);
                        CarrierTech.AddUnique(techUID);
                        break;

                    case ShipModuleType.Bomb:
                        BomberTech.AddUnique(techUID);
                        break;
                    case ShipModuleType.Special:
                        SupportShipTech.AddUnique(techUID);
                        break;
                    case ShipModuleType.Countermeasure:
                        SupportShipTech.AddUnique(techUID);
                        break;
                    case ShipModuleType.Transporter:
                        SupportShipTech.AddUnique(techUID);
                        TroopShipTech.AddUnique(techUID);
                        break;
                    case ShipModuleType.Troop:
                        TroopShipTech.AddUnique(techUID);
                        break;
                }
            }
        }

        public void UnlockEmpireHull(string hullName, string techUID = "")
        {
            UnlockedHullsDict[hullName] = true;
            ShipTechs.Add(techUID);

        }
        public void UnlockEmpireTroop(string troopName)
        {
            UnlockedTroopDict[troopName] = true;
            UnlockedTroops.AddUniqueRef(ResourceManager.GetTroopTemplate(troopName));
        }
        public void UnlockEmpireBuilding(string buildingName)
        {
            UnlockedBuildingsDict[buildingName] = true;            
        }
        public void SetEmpireTechDiscovered(string techUID) => GetTechEntry(techUID)?.SetDiscovered(this);

        public void SetEmpireTechRevealed(string techUID) => GetTechEntry(techUID).DoRevelaedTechs(this);

        public void IncreaseEmpireShipRoleLevel(ShipData.RoleName role, int bonus)
        {
            foreach (Ship ship in OwnedShips)
            {
                if (ship.shipData.Role != role)
                    continue;
                ship.AddToShipLevel(bonus);
                
            }
        }

        public void UnlockTech(string techId) //@todo rewrite. the empire tech dictionary is made of techentries which have a reference to the technology.
        {
            var techEntry = GetTechEntry(techId);
            
            if (!techEntry.Unlock(this))
                return;

            UpdateShipsWeCanBuild();
            if (!isPlayer)
                EmpireAI.TriggerRefit();
            data.ResearchQueue.Remove(techId);
        }



        private void UnlockTechFromSave(TechEntry tech)
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
//            UnlockTroops(technology.TroopsUnlocked, data.Traits.ShipType, tech.AcquiredFrom);
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
            tech.UnlockTroops(this);
            
            foreach (Technology.UnlockedHull unlockedHull in technology.HullsUnlocked)
            {
                if (unlockedHull.ShipType == data.Traits.ShipType || unlockedHull.ShipType == null || unlockedHull.ShipType == tech.AcquiredFrom)
                {
                    UnlockedHullsDict[unlockedHull.Name] = true;
                }
            }
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target)
        {
            TechnologyDict[techID].AcquiredFrom = target.data.Traits.ShipType;
            UnlockTech(techID);
        }

        public void UnlockHullsSave(TechEntry techEntry, string servantEmpireShipType)
        {

            //var tech = ResourceManager.TechTree[techID];

            techEntry.ConqueredSource.Add(servantEmpireShipType);
            techEntry.UnlockTroops(this);
            techEntry.UnLockHulls(this);
            techEntry.UnlockModules(this);
            techEntry.UnlockBuildings(this);

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
            return EmpireAI.ThreatMatrix.FindShipsInOurBorders();
        }

        public void UpdateKnownShips()
        {
            KnownShips.Clear();
            if (data.Defeated)
                return;

            if (isPlayer && Universe.Debug) // if Debug overlay is enabled, make all ships visible
            {
                using (Universe.MasterShipList.AcquireReadLock())
                foreach (Ship nearby in Universe.MasterShipList)
                {
                    nearby.inSensorRange = true;
                    KnownShips.Add(nearby);
                    EmpireAI.ThreatMatrix.UpdatePin(nearby);
                }
                return;
            }

            InfluenceNode[] influenceNodes = SensorNodes.ToArray();

            for (int i = 0; i < Universe.MasterShipList.Count; ++i)
            {
                Ship nearby = Universe.MasterShipList[i];
                if (nearby.Active && IsShipInsideInfluence(nearby, influenceNodes))
                    KnownShips.Add(nearby);
            }

        }

        private bool IsShipInsideInfluence(Ship nearby, InfluenceNode[] influenceNodes)
        {
            nearby.BorderCheck.Remove(this);

            if (nearby.loyalty != this) // update from another empire
            {
                bool insensors = false;
                bool border = false;

                foreach (InfluenceNode node in influenceNodes)
                {
                    if (nearby.Center.OutsideRadius(node.Position, node.Radius))
                        continue;

                    if (TryGetRelations(nearby.loyalty, out Relationship loyalty) && !loyalty.Known)
                    {
                        GlobalStats.UiLocker.EnterWriteLock();
                        DoFirstContact(nearby.loyalty);
                        GlobalStats.UiLocker.ExitWriteLock();
                    }
                    insensors = true;
                    if (node.SourceObject is Ship shipKey && (shipKey.inborders || shipKey.Name == "Subspace Projector") ||
                        node.SourceObject is SolarSystem || node.SourceObject is Planet)
                    {
                        border = true;
                        nearby.BorderCheck.Add(this);
                    }
                    if (!isPlayer)
                        break;

                    nearby.inSensorRange = true;
                    if (nearby.System == null || !isFaction && !nearby.loyalty.isFaction && !loyalty.AtWar)
                        break;

                    nearby.System.DangerTimer = 120f;
                    break;
                }

                EmpireAI.ThreatMatrix.UpdatePin(nearby, border, insensors);
                return insensors;
            }

            // update our own empire ships
            EmpireAI.ThreatMatrix.ClearPinsInSensorRange(nearby.Center, nearby.SensorRange);
            if (isPlayer)
            {
                nearby.inSensorRange = true;
            }
            nearby.inborders = false;

            using (BorderNodes.AcquireReadLock())
            foreach (InfluenceNode node in BorderNodes)
            {
                if (node.Position.OutsideRadius(nearby.Center, node.Radius)) continue;
                nearby.inborders = true;
                nearby.BorderCheck.Add(this);
                break;
            }

            if (!nearby.inborders)
            {
                foreach (var relationship in Relationships)
                {
                    if (!relationship.Value.Treaty_Alliance) //Relationship.Key == this ||
                        continue;

                    using (relationship.Key.BorderNodes.AcquireReadLock())
                        foreach (InfluenceNode node in relationship.Key.BorderNodes)
                        {
                            if (node.Position.OutsideRadius(nearby.Center, node.Radius)) continue;
                            nearby.inborders = true;
                            nearby.BorderCheck.Add(this);
                            break;
                        }
                }
            }
            return true; // always add, because this ship is from our own empire
        }

        public IReadOnlyDictionary<Empire, Relationship> AllRelations => Relationships;
        public Relationship GetRelations(Empire withEmpire)
        {
            Relationships.TryGetValue(withEmpire, out Relationship rel);
            return rel;
          
        }

        public Empire FindExistingRelation(Empire empire)
        {
            foreach(var relation in Relationships)
            {
                if (relation.Key == empire) return empire;
                if (relation.Key.data.Traits.Name == empire.data.Traits.Name) return empire;
            }
            return null;
        }


        public void AddRelation(Empire empire)
        {
            if (FindExistingRelation(empire) == null)
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

            if (GlobalStats.RestrictAIPlayerInteraction && Universe.player == this)
                return;
            try
            {

                if (Universe.PlayerEmpire == this && !e.isFaction)
                {
                    Universe.ScreenManager.AddScreen(new DiplomacyScreen(Universe, e, Universe.PlayerEmpire, "First Contact"));
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
            catch (ArgumentException error)
            {
                Log.Error(error, "First ConTact Failed");
            }
        }

        public void Update(float elapsedTime)
        {
            #if PLAYERONLY
                if(!this.isPlayer && !this.isFaction)
                foreach (Ship ship in this.GetShips())
                    ship.GetAI().OrderScrapShip();
                if (this.GetShips().Count == 0)
                    return;
            #endif
            
            UpdateTimer -= elapsedTime;
            this.currentMilitaryStrength = 0;
            for (int index = 0; index < this.OwnedShips.Count; ++index)
            {
                Ship ship = this.OwnedShips[index];
                if (ship != null)
                {
                    if (ship.DesignRole < ShipData.RoleName.troopShip) continue;
                    this.currentMilitaryStrength += ship.GetStrength();
                }

            }
            if (UpdateTimer <= 0f && !data.Defeated)
            {                
                if (this == Universe.PlayerEmpire )
                {
                    Universe.StarDate += 0.1f;
                    Universe.StarDate = (float)Math.Round(Universe.StarDate, 1);

                    string starDate = Universe.StarDate.ToString("#.0");
                    if (!StatTracker.SnapshotsDict.ContainsKey(starDate))
                        StatTracker.SnapshotsDict.Add(starDate, new SerializableDictionary<int, Snapshot>());
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        //if (empire.data.IsRebelFaction)
                        //    continue;

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
                                    Node = planet.Center,
                                    Radius = 300000f,
                                    StarDateMade = Universe.StarDate
                                });
                            }
                        }
                    }
                    if (!InitialziedHostilesDict)
                    {
                        InitialziedHostilesDict = true;
                        foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                        {
                            bool flag = false;
                            foreach (Ship ship in system.ShipList)
                            {
                                if (ship.loyalty != this && (ship.loyalty.isFaction || this.Relationships[ship.loyalty].AtWar))
                                    flag = true;
                            }
                            HostilesPresent.Add(system, flag);
                        }
                    }
                    else
                        AssessHostilePresence();
                }
                //added by gremlin. empire ship reserve.
                int numWars = 0;
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in AllRelations)
                {
                    if (!Relationship.Value.AtWar || Relationship.Key.isFaction)
                    {
                        continue;
                    }
                    numWars++;
                }
                float defStr = EmpireAI.DefensiveCoordinator.GetForcePoolStrength();
                this.EmpireShipCountReserve = 0;

                if (!isPlayer)
                {
                    foreach (Planet planet in GetPlanets())
                    {

                        if (planet == Capital)
                            EmpireShipCountReserve = +5;
                        else
                            EmpireShipCountReserve += planet.DevelopmentLevel;
                        if (EmpireShipCountReserve > 50)
                        {
                            EmpireShipCountReserve = 50;
                            break;
                        }
                    }
                }

                //fbedard: Number of planets where you have combat
                empirePlanetCombat = 0;
                if (isPlayer)
                    foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (!p.IsExploredBy(Universe.PlayerEmpire) || !p.RecentCombat)
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

                empireShipTotal = 0;
                empireShipCombat = 0;
                foreach (Ship ship in OwnedShips)
                {
                    if (ship.fleet == null && ship.InCombat && ship.Mothership == null)  //fbedard: total ships in combat
                        empireShipCombat++;                    
                    if (ship.Mothership != null || ship.DesignRole == ShipData.RoleName.troop 
                                                || ship.DesignRole == ShipData.RoleName.freighter 
                                                || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                        continue;
                    empireShipTotal++;
                }
                UpdateTimer = GlobalStats.TurnTimer;
                DoMoney();
                TakeTurn();
            }
            SetRallyPoints();
            UpdateFleets(elapsedTime);
            OwnedShips.ApplyPendingRemovals();
            OwnedProjectors.ApplyPendingRemovals();  //fbedard

        }
        
        public DebugTextBlock DebugEmpireTradeInfo()
        {            
            
            var incomingData = new DebugTextBlock();
            foreach (Planet tradePlanet in OwnedPlanets)
            {
                if (tradePlanet.TradeAI == null) continue;
                Array<string> lines = new Array<string>();
                var totals = tradePlanet.TradeAI.DebugSummarizeIncomingFreight(lines);
                float foodHere = tradePlanet.FoodHere;
                float prodHere = tradePlanet.ProductionHere;
                float foodStorPerc = 100 * foodHere / tradePlanet.MaxStorage;
                float prodStorPerc = 100 * prodHere / tradePlanet.MaxStorage;                                
                string food = $"{(int)foodHere}(%{foodStorPerc:00.0}) {tradePlanet.FS}";
                string prod = $"{(int)prodHere}(%{prodStorPerc:00.0}) {tradePlanet.PS}"; 
                
                incomingData.AddLine($"{tradePlanet.ParentSystem.Name} : {tradePlanet.Name} : IN Cargo: {totals.Total}", Color.Yellow);
                incomingData.AddLine($"FoodHere: {food} IN: {totals.Food}", Color.White);                
                incomingData.AddLine($"ProdHere: {prod} IN: {totals.Prod}" );
                incomingData.AddLine($"IN Colonists: {totals.Colonists}");
                incomingData.AddLine($"");

            }            
            return incomingData;
        }
        public DebugTextBlock DebugEmpirePlanetInfo()
        {

            var incomingData = new DebugTextBlock();
            foreach (Planet tradePlanet in OwnedPlanets)
            {
                Array<string> lines = new Array<string>();
                TradeAI.DebugSummaryTotal totals = tradePlanet.DebugSummarizePlanetStats(lines);
                float foodHere = tradePlanet.FoodHere;
                float prodHere = tradePlanet.ProductionHere;
                float foodStorPerc = 100 * foodHere / tradePlanet.MaxStorage;
                float prodStorPerc = 100 * prodHere / tradePlanet.MaxStorage;
                string food = $"{(int)foodHere}(%{foodStorPerc:00.0}) {tradePlanet.FS}";
                string prod = $"{(int)prodHere}(%{prodStorPerc:00.0}) {tradePlanet.PS}";

                incomingData.AddLine($"{tradePlanet.ParentSystem.Name} : {tradePlanet.Name} ", Color.Yellow);
                incomingData.AddLine($"FoodHere: {food} ", Color.White);
                incomingData.AddLine($"ProdHere: {prod} ");
                incomingData.AddRange(lines);
                incomingData.AddLine($"");

            }
            return incomingData;
        }

        public void UpdateFleets(float elapsedTime)
        {
            updateContactsTimer -= elapsedTime;
            FleetUpdateTimer -= elapsedTime;            
            foreach (var kv in FleetsDict)
            {
                kv.Value.Update(elapsedTime);
                if (FleetUpdateTimer <= 0f)
                {
                    kv.Value.UpdateAI(elapsedTime, kv.Key);
                }
            }
            if (FleetUpdateTimer < 0.0)
                FleetUpdateTimer = 5f;
        }

        public float GetPlanetIncomes()
        {
            float income = 0.0f;
            foreach (Planet planet in OwnedPlanets)
            {
                planet.UpdateIncomes(false);
                income += planet.NetIncome;
            }
            return income;
        }

        private void DoMoney()
        {
            MoneyLastTurn = Money;
            ++numberForAverage;
            GrossTaxes = 0f;
            OtherIncome = 0f;

            using (OwnedPlanets.AcquireReadLock())
            foreach (Planet planet in OwnedPlanets)
            {
                planet.UpdateIncomes(false);
                GrossTaxes += planet.GrossMoneyPT + planet.GrossMoneyPT * data.Traits.TaxMod;
                OtherIncome += planet.PlusFlatMoneyPerTurn + (planet.Population / 1000f * planet.PlusCreditsPerColonist);
            }

            TradeMoneyAddedThisTurn = 0.0f;
            foreach (KeyValuePair<Empire, Relationship> kv in Relationships)
            {
                if (kv.Value.Treaty_Trade)
                {
                    float num = (float)(0.25 * kv.Value.Treaty_Trade_TurnsExisted - 3.0);
                    if (num > 3.0)
                        num = 3f;
                    TradeMoneyAddedThisTurn += num;
                }
            }

            DoShipMaintenanceCost();
            using (OwnedPlanets.AcquireReadLock())
            {
                float newBuildM = 0f;
                
                foreach (Planet planet in OwnedPlanets)
                {
                    planet.UpdateOwnedPlanet();
                    newBuildM += planet.TotalMaintenanceCostsPerTurn;
                }
                totalBuildingMaintenance = newBuildM;
            }
            
            totalMaint = GetTotalBuildingMaintenance() + GetTotalShipMaintenance();
            AllTimeMaintTotal += totalMaint;
            
            Money += GrossTaxes * data.TaxRate + OtherIncome;
            Money += data.FlatMoneyBonus;
            Money += TradeMoneyAddedThisTurn;
            Money -= totalMaint;
        }

        private void DoShipMaintenanceCost()
        {
            totalShipMaintenance = 0.0f;

            using (OwnedShips.AcquireReadLock())
                foreach (Ship ship in OwnedShips)
                {
                    if (!ship.Active || ship.AI.State >= AIState.Scrap) continue;
                    float maintenance = ship.GetMaintCost();
                    if (data.DefenseBudget > 0 && ((ship.shipData.HullRole == ShipData.RoleName.platform && ship.IsTethered())
                                                   || (ship.shipData.HullRole == ShipData.RoleName.station &&
                                                       (ship.shipData.IsOrbitalDefense || !ship.shipData.IsShipyard))))
                    {
                        data.DefenseBudget -= maintenance;
                        continue;
                    }
                    totalShipMaintenance += maintenance;
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
        }

        public float EstimateIncomeAtTaxRate(float rate)
        {
            return GrossTaxes * rate + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus - (GetTotalBuildingMaintenance() + GetTotalShipMaintenance());
        }
        public float PercentageOfIncome(float rate)
        {
            return (GrossTaxes + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus - (GetTotalBuildingMaintenance() + GetTotalShipMaintenance())) * rate;
        }
        public float Grossincome(float tax = -1)
        {
            if (tax  < 0) tax = data.TaxRate;
            return GrossTaxes * tax + OtherIncome + TradeMoneyAddedThisTurn + data.FlatMoneyBonus;
        }
        public float EstimateShipCapacityAtTaxRate(float rate)
        {
            return (GrossTaxes + OtherIncome + TradeMoneyAddedThisTurn 
                + data.FlatMoneyBonus - GetTotalBuildingMaintenance()) * rate ;
        }

        public float GetActualNetLastTurn()
        {
            return Money - MoneyLastTurn;
        }

        public float GetAverageNetIncome()
        {
            return (GrossTaxes * data.TaxRate + (float)totalTradeIncome - AllTimeMaintTotal) / (float)numberForAverage;
        }

        public void UpdateShipsWeCanBuild(Array<string> hulls = null)
        {
            foreach (var kv in ResourceManager.ShipsDict)
            {
                var ship = kv.Value;
                if (hulls != null && !hulls.Contains(ship.shipData.Hull)) 
                    continue;

                if (ship.Deleted || ResourceManager.ShipRoles[ship.shipData.Role].Protected || ShipsWeCanBuild.Contains(ship.Name))
                    continue;
                if (!isPlayer && !ship.ShipGoodToBuild(this))
                    continue;
                if (!WeCanBuildThis(ship.Name))
                    continue;
                try
                {
                    if (ship.shipData.Role <= ShipData.RoleName.station )//&& !ship.shipData.IsShipyard)
                        structuresWeCanBuild.Add(ship.Name);
                    //if (!ResourceManager.ShipRoles[ship.shipData.Role].Protected)
                        ShipsWeCanBuild.Add(ship.Name);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    ship.Deleted = true;  //This should prevent this Key from being evaluated again
                    continue;   //This keeps the game going without crashing
                }
                ship.MarkShipRolesUsableForEmpire(this);
            }

            if (Universe != null && isPlayer)
                Universe.aw.UpdateDropDowns();            
            PreferredAuxillaryShips[ShipData.RoleName.bomber]  = PickFromCandidates(ShipData.RoleName.bomber, this, targetModule: ShipModuleType.Bomb);
            PreferredAuxillaryShips[ShipData.RoleName.carrier] = PickFromCandidates(ShipData.RoleName.carrier, this, targetModule: ShipModuleType.Hangar);

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
                Universe?.DebugWin?.DebugLogText($"{data.PortraitName} : shipData is null : '{ship}'", Debug.DebugModes.Normal);
                return false;
            }
            
            // If the ship role is not defined don't try to use it
            if (!UnlockedHullsDict.TryGetValue(shipData.Hull, out bool goodHull) || !goodHull)
            {
                //Universe?.DebugWin?.DebugLogText($"{data.PortraitName} : Hull is Not Unlocked : '{shipData.Hull}'", Debug.DebugModes.Normal);
                return false;
            }
            
           
            if (shipData.TechsNeeded.Count > 0)
            {
                if (!shipData.UnLockable) return false;

                foreach (string shipTech in shipData.TechsNeeded)
                {                    
                    if (ShipTechs.Contains(shipTech)) continue;
                    TechEntry onlyShipTech = TechnologyDict[shipTech];
                    if (onlyShipTech.Unlocked) continue;

                    Universe?.DebugWin?.DebugLogText($"Locked Tech : '{shipTech}' in design : '{ship}'", Debug.DebugModes.Normal);
                    return false;
                }
                Universe?.DebugWin?.DebugLogText($"New Ship WeCanBuild {shipData.Name} Hull: '{shipData.Hull}' DesignRole: '{ship1.DesignRole}'"
                    , Debug.DebugModes.Last);
            }

            else
                // check if all modules in the ship are unlocked
                foreach (ModuleSlotData moduleSlotData in shipData.ModuleSlots)
                {
                    if (moduleSlotData.InstalledModuleUID.IsEmpty() ||
                        moduleSlotData.InstalledModuleUID == "Dummy" ||
                        UnlockedModulesDict[moduleSlotData.InstalledModuleUID])
                        continue;
                    Universe?.DebugWin?.DebugLogText($"Locked module : '{moduleSlotData.InstalledModuleUID}' in design : '{ship}'"
                        , Debug.DebugModes.Normal);
                    return false; // can't build this ship because it contains a locked Module
                }

            return true;
        }

        public bool WeCanUseThis(Technology tech, Array<Ship> ourFactionShips)
        {
            foreach (Ship ship in ourFactionShips)
            {
                foreach (Technology.UnlockedMod entry in tech.ModulesUnlocked)
                {
                    foreach (ModuleSlotData moduleSlotData in ship.shipData.ModuleSlots)
                    {
                        if (entry.ModuleUID == moduleSlotData.InstalledModuleUID)
                            return true;
                    }
                }
            }
            return false;
        }


        

        public bool WeCanUseThisNow(Technology tech)
        {
            var unlocklist = new HashSet<string>();
            foreach(Technology.UnlockedMod unlocked in tech.ModulesUnlocked)
            {
                unlocklist.Add(unlocked.ModuleUID);
            }
            
            foreach(KeyValuePair<string,Ship> ship in ResourceManager.ShipsDict)
            {
                ShipData shipData = ship.Value.shipData;
                if (shipData.ShipStyle == null || shipData.ShipStyle == data.Traits.ShipType)
                {
                    if (shipData == null || (!UnlockedHullsDict.ContainsKey(shipData.Hull) || !UnlockedHullsDict[shipData.Hull]))
                        continue;
                    foreach (ModuleSlotData moduleSlotData in ship.Value.shipData.ModuleSlots)
                    {
                        if (unlocklist.Contains(moduleSlotData.InstalledModuleUID))
                            return true;
                    }
                }
            }
            return false;
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
                foreach (Planet p in OwnedPlanets)
                    num += p.NetFoodPerTurn;
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
            if (richness >= 1 && fertility >= 1 && pop > 7)
                return Planet.ColonyType.Core;
            if (fertility > .5 && fertility <= 1 && richness <= 1 && pop < 8 && pop > 3)
                 return Planet.ColonyType.Research;
             if (fertility > 1 && richness < 1 && pop >=2)
                 return Planet.ColonyType.Agricultural;            
             if (richness >= 1 )
                 return Planet.ColonyType.Industrial;

             //return Planet.ColonyType.Research;
             //if (richness > .5 && fertility < .5 && pop < 2)
             //    return Planet.ColonyType.Industrial;
             //if (richness <= 1 && fertility < 1 && pop >= 1)
             //    return Planet.ColonyType.Research;
             return Planet.ColonyType.Colony;
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
            
            if (p.MineralRichness > .50)
            {
                MineralWealth += p.MineralRichness +p.MaxPopulation / 1000;                ;
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
            bool wellKnown = EmpireManager.Player == this || EmpireManager.Player.TryGetRelations(this, out Relationship rel) && rel.Treaty_Alliance;
            bool known     = wellKnown || EmpireManager.Player.TryGetRelations(this, out Relationship relKnown) && (relKnown.Treaty_Trade || relKnown.Treaty_OpenBorders);
            var allies     = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.Relationships)
            {
                if (keyValuePair.Value.Treaty_Alliance)
                    allies.Add(keyValuePair.Key);
            }
            SetBordersKnownByAllies(allies, wellKnown);
            SetBordersByPlanet(known);
            foreach (Mole mole in data.MoleList)   // Moles are spies who have successfuly been planted during 'Infiltrate' type missions, I believe - Doctor
                SensorNodes.Add(new InfluenceNode()
                {
                    Position = Universe.PlanetsDict[mole.PlanetGuid].Center,
                    Radius = ProjectorRadius * this.data.SensorModifier,
                    Known = true
                });
            this.Inhibitors.Clear();
            foreach (Ship ship in this.OwnedShips)
            {
                if (ship.InhibitionRadius > 0.0f)
                    this.Inhibitors.Add(ship);
                InfluenceNode influenceNode = this.SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode.Position = ship.Center;
                influenceNode.Radius = ship.SensorRange;
                influenceNode.SourceObject = ship;                
                this.SensorNodes.Add(influenceNode);
            }

            foreach (Ship ship in this.OwnedProjectors)
            {
                if (ship.InhibitionRadius > 0f)
                    Inhibitors.Add(ship);

                InfluenceNode influenceNodeS = this.SensorNodes.RecycleObject() ?? new Empire.InfluenceNode();
                InfluenceNode influenceNodeB = this.BorderNodes.RecycleObject() ?? new Empire.InfluenceNode();

                influenceNodeS.Position     = ship.Center;
                influenceNodeS.Radius       = ProjectorRadius;  //projectors used as sensors again
                influenceNodeS.SourceObject = (object)ship;

                influenceNodeB.Position     = ship.Center;
                influenceNodeB.Radius       = ProjectorRadius;  //projectors used as sensors again
                influenceNodeB.SourceObject = (object)ship;
                bool seen                   = known || EmpireManager.Player.GetGSAI().ThreatMatrix.ContainsGuid(ship.guid);
                influenceNodeB.Known        = seen;
                influenceNodeS.Known        = seen;
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
                        if (item6.SourceObject == item5.SourceObject && item6.Radius < item5.Radius)
                            BorderNodes.QueuePendingRemoval(item6);
                    }
                }
            BorderNodes.ApplyPendingRemovals();
            
        }

        private void SetBordersByPlanet(bool empireKnown)
        {
            foreach (Planet planet in GetPlanets())
            {
                bool known = empireKnown|| planet.ParentSystem.IsExploredBy(EmpireManager.Player);
                //loop over OWN planets
                InfluenceNode influenceNode1 = BorderNodes.RecycleObject() ?? new InfluenceNode();

                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.usePlanetaryProjection)
                {
                    influenceNode1.SourceObject = planet;
                    influenceNode1.Position = planet.Center;
                }
                else
                {
                    influenceNode1.SourceObject = planet.ParentSystem;
                    influenceNode1.Position = planet.ParentSystem.Position;
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
                    influenceNode1.Radius = isFaction ? 20000f : ProjectorRadius + (10000f * planet.Population / 1000f);
                }

                influenceNode1.Known = known;
                BorderNodes.Add(influenceNode1);
                //InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();

                //influenceNode2.SourceObject = planet;
                //influenceNode2.Position = planet.Center;
                //influenceNode2.Radius =
                //    1f; //this == Empire.Universe.PlayerEmpire ? 300000f * this.data.SensorModifier : 600000f * this.data.SensorModifier;
                //influenceNode2.Known = known;
                //SensorNodes.Add(influenceNode2);
                InfluenceNode influenceNode3 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode3.SourceObject = planet;
                influenceNode3.Position = planet.Center;
                influenceNode3.Radius = isFaction ? 1f : data.SensorModifier;
                influenceNode3.Known = known;
                foreach (Building t in planet.BuildingList)
                {
                    if (t.SensorRange * data.SensorModifier > influenceNode3.Radius)
                    {
                        influenceNode3.Radius = t.SensorRange * this.data.SensorModifier;
                    }
                }

                this.SensorNodes.Add(influenceNode3);
            }
        }

        private void SetBordersKnownByAllies(Array<Empire> allies, bool wellKnown)
        {
            foreach (Empire empire in allies)
            {
                //if (empire == EmpireManager.Player) continue;
                foreach (Planet planet in empire.OwnedPlanets.ToArray())
                {
                    //InfluenceNode influenceNode1 = SensorNodes.RecycleObject() ?? new InfluenceNode();

                    //influenceNode1.SourceObject = planet;
                    //influenceNode1.Position = planet.Center;
                    //influenceNode1.Radius = 1f;

                    //influenceNode1.Known = wellKnown;
                    //SensorNodes.Add(influenceNode1);


                    InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode2.Position = planet.Center;
                    influenceNode2.Radius = isFaction
                        ? 1f
                        : this == Universe.PlayerEmpire
                            ? ProjectorRadius / 5f * empire.data.SensorModifier
                            : ProjectorRadius / 3f * empire.data.SensorModifier;
                    foreach (Building building in planet.BuildingList)                    
                        influenceNode2.Radius = Math.Max(influenceNode2.Radius, building.SensorRange * data.SensorModifier);                   
                    
                    if (influenceNode2.Radius <= 1) continue;
                    influenceNode2.Known = wellKnown;
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
                    influenceNode.SourceObject = ship;
                }

                foreach (Ship ship in empire.GetProjectors())
                {
                    //loop over all ALLIED projectors                    
                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode.Position = ship.Center;
                    influenceNode.Radius = ProjectorRadius; //projectors currently use their projection radius as sensors
                    SensorNodes.Add(influenceNode);
                    influenceNode.SourceObject = ship;
                    influenceNode.Known = wellKnown;
                }
            }
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
                    Game1.Instance.EndingGame(true);
                    foreach (Ship ship in Universe.MasterShipList)
                    {
                        ship.Die(null, true);
                    }

                    Universe.Paused = true;
                    HelperFunctions.CollectMemory();
                    Game1.Instance.EndingGame(false);
                    Universe.ScreenManager.AddScreen(new YouLoseScreen(Empire.Universe));
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
                this.data.TurnsBelowZero += (short)(1 + -1 * (this.Money) / 500);
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
                                            Empire.Universe.NotificationManager.AddPeacefulMergerNotification(biggest, strongest);
                                        else
                                            Empire.Universe.NotificationManager.AddSurrendered(biggest, strongest);
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
                    if (empire.GetPlanets().Count > 0 && !empire.isFaction && empire != this)
                    {
                        allEmpiresDead = false;
                        break;
                    }
                }
                if (allEmpiresDead)
                {
                    Universe.ScreenManager.AddScreen(new YouWinScreen(Empire.Universe));
                    return;
                }
                else
                {
                    foreach (Planet planet in OwnedPlanets)
                    {
                        if (!this.data.IsRebelFaction)
                            StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                        if (planet.HasWinBuilding)
                        {
                            Universe.ScreenManager.AddScreen(new YouWinScreen(Empire.Universe, Localizer.Token(5085)));
                            return;
                        }
                    }
                }
            }
            Research = 0;
            MaxResearchPotential = 0;
            foreach (Planet planet in OwnedPlanets)
            {
                if (!this.data.IsRebelFaction)
                    StatTracker.SnapshotsDict[Universe.StarDate.ToString("#.0")][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                int num2 = planet.HasWinBuilding ? 1 : 0;
                Research += planet.NetResearchPerTurn;
                MaxResearchPotential += planet.GetMaxResearchPotential;
            }
            if (this.data.TurnsBelowZero > 0 && (this.Money < 0.0 && !Universe.Debug))// && this.isPlayer)) // && this == Empire.Universe.PlayerEmpire)
            {
                if (this.data.TurnsBelowZero >= 25)
                {
                    Empire rebelsFromEmpireData = EmpireManager.GetEmpireByName(this.data.RebelName);
                    Log.Info("Rebellion for: " + data.Traits.Name);
                    if (rebelsFromEmpireData == null)
                        foreach (Empire rebel in EmpireManager.Empires)
                        {
                            if (rebel.data.PortraitName == this.data.RebelName)
                            {
                                Log.Info("Found Existing Rebel: " + rebel.data.PortraitName);
                                rebelsFromEmpireData = rebel;
                                break;
                            }
                        }
                    if (rebelsFromEmpireData == null)
                    {
                        rebelsFromEmpireData = EmpireManager.CreateRebelsFromEmpireData(this.data, this);
             
                    }

                    if (rebelsFromEmpireData != null)
                    {
                        Vector2 weightedCenter = GetWeightedCenter();
                        if (OwnedPlanets.FindMax(out Planet planet, p => weightedCenter.SqDist(p.Center)))
                        {
                            if (isPlayer)
                                Universe.NotificationManager.AddRebellionNotification(planet, rebelsFromEmpireData); //Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable
                            for (int index = 0; index < planet.Population / 1000; ++index)
                            {
                                Troop troop = EmpireManager.CreateRebelTroop(rebelsFromEmpireData);
                                troop.AssignTroopToTile(planet); //Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable)
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
                    else Log.Info($"Rebellion Failure: {data.RebelName}");
                    data.TurnsBelowZero = 0;
                }

            }
            this.CalculateScore();


            if (!string.IsNullOrEmpty(ResearchTopic))
            {

                float research = Research + leftoverResearch;
                TechEntry tech = GetTechEntry(ResearchTopic);

                if (tech != null)
                {
                    float cyberneticMultiplier = 1.0f;
                    if (data.Traits.Cybernetic > 0)
                    {
                        foreach (Technology.UnlockedBuilding buildingName in tech.Tech.BuildingsUnlocked)
                        {
                            Building building = ResourceManager.GetBuildingTemplate(buildingName.Name);
                            if (building.PlusFlatFoodAmount > 0 || building.PlusFoodPerColonist > 0 || building.PlusTerraformPoints > 0)
                            {
                                cyberneticMultiplier = .5f;
                                break;
                            }
                        }
                    }
                    if ((tech.Tech.Cost * cyberneticMultiplier) * UniverseScreen.GamePaceStatic - tech.Progress > research)
                    {
                        tech.Progress += research;
                        leftoverResearch = 0f;
                        research = 0;
                    }
                    else
                    {
                        research -= (tech.Tech.Cost * cyberneticMultiplier) * UniverseScreen.GamePaceStatic - tech.Progress;
                        tech.Progress = tech.Tech.Cost * UniverseScreen.GamePaceStatic;
                        UnlockTech(ResearchTopic);
                        if (isPlayer)
                            Universe.NotificationManager.AddResearchComplete(ResearchTopic, this);
                        data.ResearchQueue.Remove(ResearchTopic);
                        if (data.ResearchQueue.Count > 0)
                        {
                            ResearchTopic = data.ResearchQueue[0];
                            data.ResearchQueue.RemoveAt(0);
                        }
                        else
                            ResearchTopic = "";
                    }
                }
                leftoverResearch = research;
            }
            else if (data.ResearchQueue.Count > 0)
                ResearchTopic = data.ResearchQueue[0];

            UpdateRelationships();

            if (this.isFaction)
                this.EmpireAI.FactionUpdate();
            else if (!this.data.Defeated)
                this.EmpireAI.Update();
            if (Money > data.CounterIntelligenceBudget)
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

            if (this.isFaction)
                return;
            if (!this.isPlayer)
            {
                this.AssessFreighterNeeds();
                this.AssignExplorationTasks();
            }
            else
            {
                if (this.AutoFreighters)
                    this.AssessFreighterNeeds();
                if (this.AutoExplore)
                    this.AssignExplorationTasks();
            }
        }

        private void UpdateRelationships()
        {
            if (isFaction) return;
            int atwar = 0;
            foreach (var kv in Relationships)
                if (kv.Value.Known || isPlayer)
                {
                    kv.Value.UpdateRelationship(this, kv.Key);
                    if (kv.Value.AtWar && !kv.Key.isFaction) atwar++;
                }
            AtWarCount = atwar;
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
            
         
            this.data.MilitaryScoreTotal += this.currentMilitaryStrength;
            this.TotalScore = (int)((double)this.MilitaryScore / 100.0 + (double)this.IndustrialScore + (double)this.TechScore + (double)this.ExpansionScore);
            MilitaryScore = data.ScoreAverage == 0 ? 0f : data.MilitaryScoreTotal / data.ScoreAverage;
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
                if (!planet.ParentSystem.OwnerList.Contains(this))
                {
                    planet.ParentSystem.OwnerList.Add(this);
                    planet.ParentSystem.OwnerList.Remove(target);
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
                ship.fleet?.RemoveShip(ship);
                ship.AI.State = AIState.AwaitingOrders;
                ship.AI.OrderQueue.Clear();
            }
            foreach (Ship ship in target.GetProjectors())
            {
                this.OwnedProjectors.Add(ship);
                ship.loyalty = this;
                ship.fleet?.RemoveShip(ship);
                ship.AI.State = AIState.AwaitingOrders;
                ship.AI.OrderQueue.Clear();
            }
            target.GetShips().Clear();
            target.GetProjectors().Clear();
            foreach (KeyValuePair<string, TechEntry> keyValuePair in target.GetTDict())
            {
                if (keyValuePair.Value.Unlocked && !TechnologyDict[keyValuePair.Key].Unlocked)
                    UnlockTech(keyValuePair.Key);
            }
            foreach (KeyValuePair<string, bool> kv in target.GetHDict())
            {
                if (kv.Value)
                    UnlockedHullsDict[kv.Key] = true;
            }
            foreach (KeyValuePair<string, bool> kv in target.GetTrDict())
            {
                if (kv.Value)
                {
                    UnlockedTroopDict[kv.Key] = true;
                    UnlockedTroops.AddUniqueRef(ResourceManager.GetTroopTemplate(kv.Key));
                }
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
                    this.EmpireAI.TaskList.ForEach(item_7=>//foreach (MilitaryTask item_7 in (Array<MilitaryTask>)this.GSAI.TaskList)
                        { item_7.EndTask(); }, false, false, false);
                    this.EmpireAI.TaskList.ApplyPendingRemovals();
                }
                this.EmpireAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                this.EmpireAI.DefensiveCoordinator.DefenseDict.Clear();
                this.ForcePool.Clear();
                //foreach (Ship s in (Array<Ship>)this.OwnedShips) //.OrderByDescending(experience=> experience.experience).ThenBy(strength=> strength.BaseStrength))
                foreach (Ship s in this.OwnedShips)
                {
                    //added by gremlin Do not include 0 strength ships in defensive force pool
                    s.AI.OrderQueue.Clear();
                    s.AI.State = AIState.AwaitingOrders;
                    //ShipsToAdd.Add(s);
                    
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
                            b.ScrapBuilding(planet);
                    }
                }
            }
            foreach (Agent agent in (Array<Agent>)target.data.AgentList)
            {
                this.data.AgentList.Add(agent);
                agent.Mission = AgentMission.Defending;
                agent.TargetEmpire = (string)null;
            }
            EmpireAI.DefensiveCoordinator.ManageForcePool();
            target.data.AgentList.Clear();
            target.data.AbsorbedBy = this.data.Traits.Name;
            this.CalculateScore();
        }

        private void SystemDefensePlanner(SolarSystem system)
        {
        }

        public void ForcePoolAdd(Ship s)
        {
            if (s.shipData.Role <= ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian )
                return;
            this.EmpireAI.AssignShipToForce(s);
        }

        public void ForcePoolRemove(Ship s)
        {
            this.ForcePool.RemoveSwapLast(s);
        }

        public Array<Ship> GetForcePool()
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
                Technology technology = ResourceManager.GetTreeTech(keyValuePair.Key);
                foreach (Technology.LeadsToTech leadsToTech in technology.LeadsTo)
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
                if (keyValuePair.Value.Unlocked || !keyValuePair.Value.Discovered )
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

        public bool TradeBlocked { get; private set; }

        private void AssessFreighterNeeds()
        {
            int tradeShips = 0;
            int passengerShips = 0;

            float moneyForFreighters = this.Money * .01f - this.freighterBudget;
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
                if (ship == null || ship.loyalty != this)
                    continue;
                if (ship.shipData.Role == ShipData.RoleName.troop || ship.DesignRole == ShipData.RoleName.troopShip)
                {
                    if (ship.AI.State == AIState.PassengerTransport || ship.AI.State == AIState.SystemTrader)
                    {
                        ship.AI.OrderQueue.Clear();
                        ship.AI.State = AIState.AwaitingOrders;
                    }
                    continue;
                }
                
                //fbedard: civilian can be freighter too!
                //if (!(ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.Role == ShipData.RoleName.freighter) || ship.isColonyShip || ship.CargoSpace_Max == 0 || ship.GetAI() == null)
                if ((ship.shipData.ShipCategory != ShipData.Category.Civilian && ship.DesignRole != ShipData.RoleName.freighter) 
                    || ship.isColonyShip || ship.CargoSpaceMax == 0 || ship.AI == null || ship.isConstructor
                    || ship.AI.State == AIState.Refit || ship.AI.State == AIState.Scrap
                    )
                {                   
                    //if (ship.AI.State == AIState.PassengerTransport || ship.AI.State == AIState.SystemTrader)
                    //    ship.AI.State = AIState.AwaitingOrders;
                    continue;
                }
              
                this.freighterBudget += ship.GetMaintCost();
                if (ship.Velocity != Vector2.Zero && ship.AI.State != AIState.AwaitingOrders && ship.AI.State != AIState.PassengerTransport && ship.AI.State != AIState.SystemTrader)
                    continue;
                    
                if (  ship.AI.OrderQueue.IsEmpty || ship.AI.start == null || ship.AI.end == null) 
                {                
                    //if (ship.TradeTimer != 0 && ship.TradeTimer < 1)
                        unusedFreighters.Add(ship);
                    //else
                      //  assignedShips.Add(ship);


                }
                else if (ship.AI.State == AIState.PassengerTransport)
                {
                    passengerShips++;
                }
                else if (ship.AI.State == AIState.SystemTrader)
                    tradeShips++;
                else
                {
                    assignedShips.Add(ship);
                }
            }
            int totalShipcount = tradeShips + passengerShips + unusedFreighters.Count;
            totalShipcount = totalShipcount > 0 ? totalShipcount : 1;
            freighterBudget = freighterBudget > 0 ? freighterBudget : .1f;
            float avgmaint = freighterBudget / totalShipcount;
            moneyForFreighters -= freighterBudget;

            int minFreightCount = 3 + this.getResStrat().ExpansionPriority + (int)(( tradeShips) * .5f); 

            int skipped = 0;

             while (unusedFreighters.Count - skipped > minFreightCount)
            {
                Ship ship = unusedFreighters[0 + skipped];                
                if ( ship.TradeTimer < 1 && ship.CargoSpaceUsed == 0)
                {
                    ship.AI.OrderScrapShip();
                    unusedFreighters.Remove(ship);
                }
                else skipped++;
            }
            //unusedFreighters.AddRange(assignedShips);
            //assignedShips.Clear();
            int freighters = unusedFreighters.Count;
            //get number of freighters being built

            TradeBlocked = IsTradeBlocked();

            int type = 1;
            while (freighters > 0 )
            {
                Ship ship = unusedFreighters[0];                
                //assignedShips.Add(ship);
                unusedFreighters.Remove(ship);
                freighters--;
                if (ship.AI.State != AIState.Flee)
                {
                    switch (type)
                    {
                        case 1:
                            ship.AI.FoodOrProd = Goods.Food;
                            ship.AI.State = AIState.SystemTrader;
                            ship.AI.OrderTrade(0.1f);
                            ++type;
                            break;
                        case 2:
                            ship.AI.FoodOrProd = Goods.Production;
                            ship.AI.State = AIState.SystemTrader;
                            ship.AI.OrderTrade(0.1f);
                            ++type;
                            break;
                        default:
                            ship.AI.State = AIState.PassengerTransport;
                            ship.TradingFood = false;
                            ship.TradingProd = false;
                            ship.AI.FoodOrProd = Goods.Colonists;
                            ship.AI.OrderTransportPassengers(0.1f);
                            type = 1;                            
                            break;
                    }
                    if (ship.AI.start == null && ship.AI.end == null)
                        assignedShips.Add(ship);
                }
            }
            unusedFreighters.AddRange(assignedShips);
            freighters = 0; // unusedFreighters.Count;
            int goalLimt = 1  + this.getResStrat().IndustryPriority;
            foreach (Goal goal in EmpireAI.Goals)
            {
                if (goal is IncreaseFreighters || goal is IncreasePassengerShips)
                {
                    ++freighters;
                    --goalLimt;
                }
            }
            moneyForFreighters -= freighters * avgmaint;
            freighters += unusedFreighters.Count ;
             if (moneyForFreighters > 0 && freighters < minFreightCount && goalLimt >0)
            {
                ++freighters;
                EmpireAI.Goals.Add(new IncreaseFreighters(this));
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


        public bool IsTradeBlocked()
        {
            var allincombat = true;
            var noimport = true;
            foreach (Planet p in GetPlanets())
            {
                if (p.ParentSystem.combatTimer <= 0)
                    allincombat = false;
                if (p.PS == Planet.GoodState.IMPORT || p.FS == Planet.GoodState.IMPORT)
                    noimport = false;
            }

            return allincombat || noimport;
        }

        public void ReportGoalComplete(Goal g)
        {
            for (int index = 0; index < this.EmpireAI.Goals.Count; ++index)
            {
                if (this.EmpireAI.Goals[index] != g) continue;
                this.EmpireAI.Goals.QueuePendingRemoval(this.EmpireAI.Goals[index]);
                break;
            }
        }

        public EmpireAI GetGSAI()
        {
            return this.EmpireAI;
        }

        public Vector2 GetWeightedCenter()
        {
            int planets = 0;
            Vector2 vector2 = new Vector2();
            using (OwnedPlanets.AcquireReadLock())
            foreach (Planet planet in OwnedPlanets)
            {
                for (int x = 0; x < planet.Population / 1000.0; ++x)
                {
                    ++planets;
                    vector2 += planet.Center;
                }
            }
            if (planets == 0)
                planets = 1;
            return vector2 / planets;
        }


        private void AssignExplorationTasks()
        {
            int unexplored =0;
            bool haveUnexploredSystems = false;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (solarSystem.IsExploredBy(this)) continue;
                if (++unexplored > 20) break;
                
                
            }
            haveUnexploredSystems = unexplored != 0;
            int numScouts = 0;
            if (!haveUnexploredSystems)
            {
                foreach (Ship ship in OwnedShips)
                {
                    if (ship.AI.State == AIState.Explore)
                        ship.AI.OrderOrbitNearest(true);
                }

                return;
            }

            // already building a scout? then just quit
            foreach (Goal goal in EmpireAI.Goals)
                if (goal.type == GoalType.BuildScout)
                    return;
            var desiredScouts = unexplored * economicResearchStrategy.ExpansionRatio * .5f;
            foreach (Ship ship in OwnedShips)
            {
                if (ship.DesignRole != ShipData.RoleName.scout || ship.PlayerShip)
                    continue;
                ship.DoExplore();
                if (++numScouts >= desiredScouts)
                    return;
            }
            
            bool notBuilding = true;
            foreach (Goal goal in EmpireAI.Goals)
            {
                if (goal.type == GoalType.BuildScout)
                {
                    notBuilding = false;
                    break;
                }
            }

            if (notBuilding)
                EmpireAI.Goals.Add(new BuildScout(this));
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
                data.Traits.ModHpModifier += (art.ModuleHPMod + art.ModuleHPMod * data.Traits.Spiritual);
                EmpireShipBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
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
                data.Traits.ModHpModifier -= (art.ModuleHPMod + art.ModuleHPMod * data.Traits.Spiritual);
                EmpireShipBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
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
                EmpireShipBonuses.RefreshBonuses(this);
            }
            if (art.ShieldPenBonus > 0f)
            {
                this.data.ShieldPenBonusChance -= (art.ShieldPenBonus + art.ShieldPenBonus * this.data.Traits.Spiritual);
                EmpireShipBonuses.RefreshBonuses(this);
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
            GetGSAI().DefensiveCoordinator.Remove(ship);
            
            ship.AI.OrderQueue.Clear();
           
            ship.AI.State = AIState.AwaitingOrders;
            ship.ClearFleet();
        }
        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null)
        {
            if (targetEmpire == this) return false;
            if (targetEmpire == null) return false;            
            if (!TryGetRelations(targetEmpire, out Relationship rel) || rel == null) return false;            
            if(!rel.Known) return true;
            if (rel.AtWar) return true;
            if (rel.Treaty_NAPact) return false;
            if (isFaction || targetEmpire.isFaction ) return true;
            if (target == null) return true;
            if (rel.TotalAnger > 50) return true;            
            return target.IsAttackable(this, rel);

        }

        public class InfluenceNode
        {
            public Vector2 Position;
            public object SourceObject; // SolarSystem, Planet OR Ship
            public bool DrewThisTurn;
            public float Radius;
            public bool Known;

            public void Wipe()
            {
                Position      = Vector2.Zero;
                SourceObject  = null;
                DrewThisTurn  = false;
                Radius        = 0;
                Known         = false;
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
            ForcePool = null;
            BorderNodes?.Dispose(ref BorderNodes);
            SensorNodes?.Dispose(ref SensorNodes);
            KnownShips?.Dispose(ref KnownShips);
            OwnedShips?.Dispose(ref OwnedShips);
            DefensiveFleet?.Dispose(ref DefensiveFleet);
            EmpireAI?.Dispose(ref EmpireAI);
            if (data != null)
            {
                data.AgentList = new BatchRemovalCollection<Agent>();
                data.MoleList = new BatchRemovalCollection<Mole>();
            }
            LockPatchCache?.Dispose(ref LockPatchCache);
            OwnedPlanets?.Dispose(ref OwnedPlanets);
            OwnedProjectors?.Dispose(ref OwnedProjectors);
            OwnedSolarSystems?.Dispose(ref OwnedSolarSystems);
        }

        public override string ToString() => $"Id={Id} Name={Name} Player={isPlayer} Faction={isFaction}";
    }
}
