using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

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
        public float Money {get; private set; } = 1000f;
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
        private int TurnCount = 1;
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
        private EconomicResearchStrategy economicResearchStrategy;
        private float UpdateTimer;
        public bool isPlayer;
        public float TotalShipMaintenance { get; private set; }
        public float updateContactsTimer;
        private bool InitialziedHostilesDict;
        public float NetPlanetIncomes { get; private set; }
        public float GrossPlanetIncome { get; private set; }
        public float TradeMoneyAddedThisTurn { get; private set; }
        public float ExcessGoodsMoneyAddedThisTurn { get; private set; } // money tax from excess goods
        public float MoneyLastTurn;
        public int AllTimeTradeIncome;
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
        public float MaxResearchPotential = 10;
        public float MaxColonyValue { get; private set; }
        public Ship BestPlatformWeCanBuild { get; private set; }
        public Ship BestStationWeCanBuild { get; private set; }
        public HashSet<string> ShipTechs = new HashSet<string>();
        public int ColonyRankModifier { get; private set; }
        //added by gremlin
        private float leftoverResearch;
        [XmlIgnore][JsonIgnore] public byte[,] grid;
        [XmlIgnore][JsonIgnore] public int granularity = 0;
        [XmlIgnore][JsonIgnore] public int AtWarCount;
        [XmlIgnore][JsonIgnore] public Array<string> BomberTech      = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> TroopShipTech   = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> CarrierTech     = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> SupportShipTech = new Array<string>();
        [XmlIgnore][JsonIgnore] public Planet[] RallyPoints          = Empty<Planet>.Array;
        [XmlIgnore][JsonIgnore] public Ship BoardingShuttle     => ResourceManager.ShipsDict["Assault Shuttle"];
        [XmlIgnore][JsonIgnore] public Ship SupplyShuttle       => ResourceManager.ShipsDict["Supply_Shuttle"];
        [XmlIgnore][JsonIgnore] public int FreighterCap         => OwnedPlanets.Count * 3 + GetResStrat().ExpansionPriority;
        [XmlIgnore][JsonIgnore] public int FreightersBeingBuilt => EmpireAI.Goals.Count(goal => goal is IncreaseFreighters);
        [XmlIgnore][JsonIgnore] public int MaxFreightersInQueue => 1 + GetResStrat().IndustryPriority;
        [XmlIgnore][JsonIgnore] public int TotalFreighters      => OwnedShips.Count(s => s.IsFreighter);
        [XmlIgnore][JsonIgnore] public Ship[] IdleFreighters    => OwnedShips.Filter(s => s.IsIdleFreighter);
        [XmlIgnore][JsonIgnore] public bool IsCybernetic        => data.Traits.Cybernetic != 0;
        [XmlIgnore][JsonIgnore] public bool NonCybernetic       => data.Traits.Cybernetic == 0;

        public Dictionary<ShipData.RoleName, string> PreferredAuxillaryShips = new Dictionary<ShipData.RoleName, string>();

        // Income this turn before deducting ship maintenance
        public float GrossIncome              => GrossPlanetIncome + TradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus;
        public float NetIncome                => GrossIncome - BuildingAndShipMaint;
        public float TotalBuildingMaintenance => GrossPlanetIncome - NetPlanetIncomes;
        public float BuildingAndShipMaint     => TotalBuildingMaintenance + TotalShipMaintenance;

        public void AddMoney(float moneyDiff)
        {
            Money += moneyDiff;
        }

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

        public Planet[] RallyShipYards => RallyPoints.Filter(sy => sy.HasSpacePort);

        public Planet RallyShipYardNearestTo(Vector2 position)
        {
            Planet p = RallyPoints.Length == 0
                ? null
                : RallyPoints.FindMaxFiltered(planet => planet?.HasSpacePort ?? false,
                    planet => -position.SqDist(planet?.Center ?? Vector2.Zero));
            switch (p) {
                case null when RallyPoints.Length > 0:
                    return RallyPoints.FindMin(planet => -position.SqDist(planet.Center));
                case null:
                    Log.Warning($"RallyShipYardNearestTo Had null elements: RallyPoints {RallyPoints.Length}");
                    break;
            }

            return p;
        }

        public Planet[] BestBuildPlanets => RallyPoints.Filter(planet =>
            planet.HasSpacePort && planet.ParentSystem.combatTimer <= 0
            && planet.IsVibrant
            && planet.colonyType != Planet.ColonyType.Research
            && (planet.colonyType != Planet.ColonyType.Industrial || planet.IsCoreWorld)
        );

        public Planet PlanetToBuildShipAt(float actualCost)
        {
            Planet planet = OwnedPlanets.FindMin(p => p.Construction.EstimateMinTurnsToBuildShip(actualCost));
            return planet;
        }

        public Planet FindClosestSpacePort(Vector2 position)
        {
            Planet[] spacePorts = OwnedPlanets.Filter(p => p.HasSpacePort);
            return spacePorts.FindMin(p => p.Center.SqDist(position));
        }

        public bool TryFindSpaceportToBuildShipAt(Ship ship, out Planet spacePort)
        {
            Planet[] spacePorts = OwnedPlanets.Filter(p => p.HasSpacePort);
            return FindPlanetToBuildAt(spacePorts, ship, out spacePort);
        }

        public bool FindPlanetToBuildOffensiveShipAt(Ship ship, out Planet planet)
        {
            Planet[] spacePorts = OwnedPlanets.Filter(p => p.HasSpacePort && p.colonyType != Planet.ColonyType.Research);
            return FindPlanetToBuildAt(spacePorts, ship, out planet);
        }

        public bool FindPlanetToBuildAt(IReadOnlyList<Planet> ports, Ship ship, out Planet chosen)
        {
            if (ports.Count != 0)
            {
                float cost = ship.GetCost(this);
                chosen = ports.FindMin(p => p.Construction.EstimateMinTurnsToBuildShip(cost));
                return true;
            }
            chosen = null;
            return false;
        }

        public bool FindClosestPlanetToBuildAt(Vector2 position, out Planet chosen)
        {
            Planet[] spacePorts = OwnedPlanets.Filter(p => p.HasSpacePort);
            chosen = spacePorts.FindMin(p => p.Center.SqDist(position));
            return chosen != null;
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

        public Empire(Empire parentEmpire) => TechnologyDict = parentEmpire.TechnologyDict;

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

        public void SpawnHomeWorld(Planet home, PlanetType type)
        {
            home.Owner = this;
            Capital    = home;
            AddPlanet(home);
            home.GenerateNewHomeWorld(type);
            home.InitializeWorkerDistribution(this);
            home.SetFertilityMinMax(2f + data.Traits.HomeworldFertMod);
            home.MineralRichness = 1f + data.Traits.HomeworldRichMod;
            home.MaxPopBase      = 14000f * data.Traits.HomeworldSizeMultiplier;
            home.Population      = 14000f;
            home.FoodHere        = 100f;
            home.ProdHere        = 100f;
            home.HasSpacePort     = true;
            home.AddGood("ReactorFuel", 1000); // WTF?
            ResourceManager.CreateBuilding(Building.CapitalId).SetPlanet(home);
            ResourceManager.CreateBuilding(Building.SpacePortId).SetPlanet(home);
            if (GlobalStats.HardcoreRuleset)
            {
                ResourceManager.CreateBuilding(Building.FissionablesId).SetPlanet(home);
                ResourceManager.CreateBuilding(Building.FissionablesId).SetPlanet(home);
                ResourceManager.CreateBuilding(Building.MineFissionablesId).SetPlanet(home);
                ResourceManager.CreateBuilding(Building.FuelRefineryId).SetPlanet(home);
            }
        }

        public void SetRallyPoints()
        {
            if (NoPlanetsForRally()) return;

            Array<Planet> rallyPlanets = new Array<Planet>();

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
                if (planet.HasSpacePort)
                    rallyPlanets.Add(planet);
            }

            if (rallyPlanets.Count > 0)
            {
                RallyPoints = rallyPlanets.ToArray();
                return;
            }
            rallyPlanets.Add(OwnedPlanets.FindMax(planet => planet.Prod.GrossIncome));
            RallyPoints = rallyPlanets.ToArray();
            if (RallyPoints.Length == 0)
                Log.Error("SetRallyPoint: No Planets found");
        }

        private bool NoPlanetsForRally()
        {
            //defeated empires and factions can use rally points now.
            if (OwnedPlanets.Count != 0) return false;
            Array<Planet> rallyPlanets = new Array<Planet>();
            rallyPlanets = GetPlanetsNearStations();
            if (rallyPlanets.Count == 0)
            {
                Planet p = GetNearestUnOwnedPlanet();
                if (p != null)
                    rallyPlanets.Add(p);
            }

            //super failSafe. just take any planet.
            if (rallyPlanets.Count == 0)
                rallyPlanets.Add(Universe.PlanetsDict.First().Value);

            RallyPoints = rallyPlanets.ToArray();
            return true;
        }


        private Array<Planet> GetPlanetsNearStations()
        {
            var planets = new Array<Planet>();
            foreach(var station in OwnedShips)
            {
                if (station.BaseHull.Role != ShipData.RoleName.station
                    && station.BaseHull.Role != ShipData.RoleName.platform) continue;
                if (station.IsTethered)
                {
                    planets.Add(station.GetTether());
                    continue;
                }

                if (station.System == null) continue;
                foreach(var planet in station.System.PlanetList)
                {
                    if (planet.Owner == null)
                        planets.Add(planet);
                }

            }
            return planets;
        }

        public Planet GetNearestUnOwnedPlanet()
        {
            foreach(var kv in Empire.Universe.SolarSystemDict)
            {
                if (kv.Value.OwnerList.Count > 0) continue;
                if (kv.Value.PlanetList.Count == 0) continue;
                return kv.Value.PlanetList[0];
            }
            return null;
        }

        public int GetUnusedKeyForFleet()
        {
            int key = 0;
            while (FleetsDict.ContainsKey(key))
                ++key;
            return key;
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
                kv.Value.ResetRelation();
                kv.Key.GetRelations(this).ResetRelation();
            }
            foreach (Ship ship in OwnedShips)
            {
                ship.AI.ClearOrders();
            }
            EmpireAI.Goals.Clear();
            EmpireAI.TaskList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Reset();

            Empire rebels = EmpireManager.CreateRebelsFromEmpireData(data, this);
            var rebelEmpireIndex = EmpireManager.Empires.IndexOf(rebels);
            SerializableDictionary<int, Snapshot> statDict = StatTracker.SnapshotsDict[Universe.StarDateString];
            statDict[rebelEmpireIndex] = new Snapshot(Universe.StarDate);

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

            foreach (var kv in Relationships)
            {
                kv.Value.ResetRelation();
                kv.Key.GetRelations(this).ResetRelation();
            }

            foreach (Ship ship in OwnedShips)
            {
                ship.AI.ClearOrders();
            }

            EmpireAI.Goals.Clear();
            EmpireAI.TaskList.Clear();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            OwnedShips.Clear();
            data.AgentList.Clear();
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

        public Map<string, bool> GetHDict() => UnlockedHullsDict;

        public bool IsHullUnlocked(string hullName) => UnlockedHullsDict.Get(hullName, out bool unlocked) && unlocked;

        public Map<string, bool> GetTrDict() => UnlockedTroopDict;

        public IReadOnlyList<Troop> GetUnlockedTroops() => UnlockedTroops;

        public Map<string, bool> GetBDict() => UnlockedBuildingsDict;
        public bool IsBuildingUnlocked(string name) => UnlockedBuildingsDict.TryGetValue(name, out bool unlocked) && unlocked;
        public bool IsBuildingUnlocked(int bid) => ResourceManager.GetBuilding(bid, out Building b)
                                                        && IsBuildingUnlocked(b.Name);

        public bool IsModuleUnlocked(string moduleUID) => UnlockedModulesDict.TryGetValue(moduleUID, out bool found) && found;

        public Map<string, TechEntry> GetTDict() => TechnologyDict;

        public TechEntry GetTechEntry(string tech)
        {
            if (TechnologyDict.TryGetValue(tech, out TechEntry techEntry))
                return techEntry;
            Log.Error($"Empire GetTechEntry: Failed To Find Tech: ({tech})");
            return TechEntry.None;
        }

        public float GetProjectedResearchNextTurn()
            => OwnedPlanets.Sum(p=> p.Res.NetIncome);

        public IReadOnlyList<SolarSystem> GetOwnedSystems() => OwnedSolarSystems;

        public IReadOnlyList<Planet> GetPlanets() => OwnedPlanets;

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

        public void RemovePlanet(Planet planet)
        {
            OwnedPlanets.Remove(planet);
            if (OwnedPlanets.All(p => p.ParentSystem != planet.ParentSystem)) // system no more in owned planets?
                OwnedSolarSystems.Remove(planet.ParentSystem);
        }

        public void ClearAllPlanets()
        {
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
        }

        public void AddPlanet(Planet planet)
        {
            if (planet == null)
                throw new ArgumentNullException(nameof(planet));

            OwnedPlanets.Add(planet);
            if (planet.ParentSystem == null)
                throw new ArgumentNullException(nameof(planet.ParentSystem));

            OwnedSolarSystems.AddUniqueRef(planet.ParentSystem);
        }

        public void TaxGoodsIfMercantile(float goods)
        {
            if (data.Traits.Mercantile.LessOrEqual(0))
                return;

            float taxedGoods         = goods * data.Traits.Mercantile * data.TaxRate;
            TradeMoneyAddedThisTurn += taxedGoods;
            AllTimeTradeIncome      += (int)taxedGoods;
        }

        public BatchRemovalCollection<Ship> GetShips() => OwnedShips;

        public Array<Ship> GetShipsFromOffensePools(bool onlyAO = false)
        {
            Array<Ship> ships = new Array<Ship>();
            foreach (AO ao in GetEmpireAI().AreasOfOperations)
                ships.AddRange(ao.GetOffensiveForcePool());

            if(!onlyAO)
                ships.AddRange(ForcePool);
            return ships;
        }

        public BatchRemovalCollection<Ship> GetProjectors() => OwnedProjectors;

        public void AddShip(Ship s)
        {
            switch (s.Name) {
                case "Subspace Projector":
                    OwnedProjectors.Add(s);
                    break;
                default:
                    OwnedShips.Add(s);
                    break;
            }
        }

        public void AddShipNextFrame(Ship s) => ShipsToAdd.Add(s);

        private void InitColonyRankModifier() // controls amount of orbital defense by difficuly
        {
            if (isPlayer) return;
            switch (CurrentGame.Difficulty)
            {
                case UniverseData.GameDifficulty.Easy:   ColonyRankModifier = -2; break;
                case UniverseData.GameDifficulty.Normal: ColonyRankModifier = 0;  break;
                case UniverseData.GameDifficulty.Hard:   ColonyRankModifier = 1;  break;
                case UniverseData.GameDifficulty.Brutal: ColonyRankModifier = 2; break;
            }
        }

        public void Initialize()
        {
            EmpireAI = new EmpireAI(this);
            for (int i = 1; i < 10; ++i)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(i);
                FleetsDict.Add(i, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            InitTechs();

            foreach (var hull in ResourceManager.Hulls)       UnlockedHullsDict[hull.Hull] = false;
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
                    techEntry.Progress = techEntry.TechCost;
            }
            //Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();

            ResetTechsUsableByShips(ourShips, unlockBonuses: true);
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
            InitColonyRankModifier();
        }

        private void ResetTechsUsableByShips(Array<Ship> ourShips, bool unlockBonuses)
        {
            foreach (var entry in TechnologyDict)
            {
                var tech = entry.Value.Tech;
                bool modulesNotHulls = tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0;
                if (modulesNotHulls && !WeCanUseThis(tech, ourShips))
                    entry.Value.shipDesignsCanuseThis = false;
            }

            foreach (var entry in TechnologyDict)
            {
                if (!entry.Value.shipDesignsCanuseThis)
                    entry.Value.shipDesignsCanuseThis = WeCanUseThisLater(entry.Value);
            }

            foreach (var entry in TechnologyDict.OrderBy(hulls => hulls.Value.Tech.HullsUnlocked.Count > 0))
            {
                AddToShipTechLists(entry.Value);
                if (!entry.Value.Unlocked)
                    continue;

                entry.Value.Unlocked = false;
                if (unlockBonuses)
                    entry.Value.Unlock(this);
                else
                    entry.Value.UnlockFromSave(this);

            }

            foreach (TechEntry techEntry in TechnologyDict.Values)
            {
                bool hullsNotRoot = techEntry.Tech.HullsUnlocked.Count > 0 && techEntry.Tech.RootNode != 1;
                if (!hullsNotRoot || !techEntry.Unlocked) continue;
                techEntry.Unlocked = false;
                if (unlockBonuses)
                    techEntry.Unlock(this);
                else
                    techEntry.UnlockFromSave(this);
            }
        }

        private void InitTechs()
        {
            foreach (var kv in ResourceManager.TechTree)
            {
                TechEntry techEntry = new TechEntry
                {
                    Progress = 0.0f,
                    UID = kv.Key
                };

                //added by McShooterz: Checks if tech is racial, hides it, and reveals it only to races that pass
                bool raceLimited = kv.Value.RaceRestrictions.Count != 0 || kv.Value.RaceExclusions.Count != 0;
                if (raceLimited)
                {
                    techEntry.Discovered |= kv.Value.RaceRestrictions.Count == 0 && kv.Value.ComesFrom.Count >0;
                    kv.Value.Secret |= kv.Value.RaceRestrictions.Count != 0;
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
                    techEntry.Unlocked = false;
                TechnologyDict.Add(kv.Key, techEntry);
            }
        }

        private void AddToShipTechLists(TechEntry tech)
        {
            Array<Technology.UnlockedMod> mods = tech.GetUnlockableModules(this);
            for (int i = 0; i < mods.Count; i++)
            {
                var module = mods[i];
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

            foreach (var hull in ResourceManager.Hulls)       UnlockedHullsDict[hull.Hull]  = false;
            foreach (var tt in ResourceManager.TroopTypes)    UnlockedTroopDict[tt]         = false;
            foreach (var kv in ResourceManager.BuildingsDict) UnlockedBuildingsDict[kv.Key] = false;
            foreach (var kv in ResourceManager.ShipModules)   UnlockedModulesDict[kv.Key]   = false;
            UnlockedTroops.Clear();

            // unlock from empire data file
            // Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();
            ResetTechsUsableByShips(ourShips, unlockBonuses: false);

            //fbedard: Add missing troop ship
            if (data.DefaultTroopShip.IsEmpty())
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;
            foreach (string ship in data.unlockShips) // unlock ships from empire data
                ShipsWeCanBuild.Add(ship);

            UpdateShipsWeCanBuild();

            if (data.EconomicPersonality == null)
                data.EconomicPersonality = new ETrait { Name = "Generalists" };
            economicResearchStrategy = ResourceManager.EconStrats[data.EconomicPersonality.Name];
            InitColonyRankModifier();
        }
        private bool WeCanUseThisLater(TechEntry tech)
        {
            foreach (Technology.LeadsToTech leadsToTech in tech.Tech.LeadsTo)
            {
                TechEntry entry = TechnologyDict[leadsToTech.UID];
                if (entry.shipDesignsCanuseThis || WeCanUseThisLater(entry))
                    return true;
            }
            return false;
        }


        public EconomicResearchStrategy GetResStrat() => economicResearchStrategy;

        public string[] GetTroopsWeCanBuild() => UnlockedTroopDict.Where(kv => kv.Value)
                                                                  .Select(kv => kv.Key).ToArray();

        public bool WeCanBuildTroop(string id) => UnlockedTroopDict.TryGetValue(id, out bool canBuild) && canBuild;

        public void UnlockEmpireShipModule(string moduleUID, string techUID = "")
        {
            UnlockedModulesDict[moduleUID] = true;
            PopulateShipTechLists(moduleUID, techUID);
        }

        private void PopulateShipTechLists(string moduleUID, string techUID, bool addToMainShipTechs = true)
        {
            if (addToMainShipTechs)
                ShipTechs.Add(techUID);
            if (isFaction) return;
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
        public void UnlockEmpireBuilding(string buildingName) => UnlockedBuildingsDict[buildingName] = true;

        public void SetEmpireTechDiscovered(string techUID)
        {
            TechEntry tech = GetTechEntry(techUID);
            if (tech == null)
            {
                Log.Warning($"SetEmpireTechDiscovered: Tech UID was not found: Tech({techUID})");
                return; //don't crash.
            }
            tech.SetDiscovered(this);
        }

        public void SetEmpireTechRevealed(string techUID) => GetTechEntry(techUID).DoRevealedTechs(this);

        public void IncreaseEmpireShipRoleLevel(ShipData.RoleName role, int bonus)
        {
            foreach (Ship ship in OwnedShips)
            {
                if (ship.shipData.Role == role)
                    ship.AddToShipLevel(bonus);
            }
        }

        public void UnlockTech(string techId) => UnlockTech(GetTechEntry(techId));

        public void UnlockTech(TechEntry techEntry)
        {
            if (!techEntry.Unlock(this))
                return;
            UpdateShipsWeCanBuild();
            EmpireAI.TriggerRefit();
            EmpireAI.TriggerFreightersScrap();
            data.ResearchQueue.Remove(techEntry.UID);
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target)
        {
            //acquiredFrom here should be an array.
            TechnologyDict[techID].AcquiredFrom = target.data.Traits.ShipType;
            UnlockTech(techID);
        }

        public void UnlockHullsSave(TechEntry techEntry, string servantEmpireShipType)
        {
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
                bool inSensorRadius = false;
                bool border = false;

                for (int i = 0; i < influenceNodes.Length; i++)
                {
                    InfluenceNode node = influenceNodes[i];
                    if (nearby.Center.OutsideRadius(node.Position, node.Radius))
                        continue;

                    if (TryGetRelations(nearby.loyalty, out Relationship loyalty) && !loyalty.Known)
                        DoFirstContact(nearby.loyalty);

                    inSensorRadius = true;
                    if (node.SourceObject is Ship shipKey &&
                        (shipKey.inborders || shipKey.Name == "Subspace Projector") ||
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

                EmpireAI.ThreatMatrix.UpdatePin(nearby, border, inSensorRadius);
                return inSensorRadius;
            }

            // update our own empire ships
            EmpireAI.ThreatMatrix.ClearPinsInSensorRange(nearby.Center, nearby.SensorRange);
            if (isPlayer)
                nearby.inSensorRange = true;
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
                    if (!relationship.Value.Treaty_Alliance)
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

        public void AddRelation(Empire empire)
        {
            if (empire == this) return;
            if (!TryGetRelations(empire, out Relationship relation))
                Relationships.Add(empire, new Relationship(empire.data.Traits.Name));
        }
        public bool TryGetRelations(Empire empire, out Relationship relations) => Relationships.TryGetValue(empire, out relations);

        public void AddRelationships(Empire e, Relationship i) => Relationships.Add(e, i);

        public void DamageRelationship(Empire e, string why, float amount, Planet p)
        {
            if (!Relationships.TryGetValue(e, out Relationship relationship))
                return;
            if (why == "Colonized Owned System" || why == "Destroyed Ship")
                relationship.DamageRelationship(this, e, why, amount, p);
        }

        public void DoFirstContact(Empire e)
        {
            Relationships[e].SetInitialStrength(e.data.Traits.DiplomacyMod * 100f);
            Relationships[e].Known = true;
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
            currentMilitaryStrength = 0;
            for (int index = 0; index < OwnedShips.Count; ++index)
            {
                Ship ship = OwnedShips[index];
                if (ship != null)
                {
                    if (ship.DesignRole < ShipData.RoleName.troopShip) continue;
                    currentMilitaryStrength += ship.GetStrength();
                }

            }
            if (UpdateTimer <= 0f && !data.Defeated)
            {
                if (this == Universe.PlayerEmpire )
                {
                    Universe.StarDate += 0.1f;
                    Universe.StarDate = (float)Math.Round(Universe.StarDate, 1);

                    string starDate = Universe.StarDateString;
                    if (!StatTracker.SnapshotsDict.ContainsKey(starDate))
                        StatTracker.SnapshotsDict.Add(starDate, new SerializableDictionary<int, Snapshot>());
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        var snapshots = StatTracker.SnapshotsDict[starDate];
                        int empireIndex = EmpireManager.Empires.IndexOf(empire);
                        if (!snapshots.ContainsKey(empireIndex))
                            snapshots.Add(empireIndex, new Snapshot(Universe.StarDate));
                    }
                    if (Universe.StarDate.AlmostEqual(1000.09f))
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
                                if (ship.loyalty != this && (ship.loyalty.isFaction || Relationships[ship.loyalty].AtWar))
                                    flag = true;
                            }
                            HostilesPresent.Add(system, flag);
                        }
                    }
                    else
                        AssessHostilePresence();
                }
                //added by gremlin. empire ship reserve.

                EmpireShipCountReserve = 0;

                if (!isPlayer)
                {
                    foreach (Planet planet in GetPlanets())
                    {

                        if (planet == Capital)
                            EmpireShipCountReserve = +5;
                        else
                            EmpireShipCountReserve += planet.Level;
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
                                    if (troop?.Loyalty != Universe.PlayerEmpire) continue;
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

        private void UpdateMaxColonyValue()
        {
            if (OwnedPlanets.Count > 0)
                MaxColonyValue = OwnedPlanets.Max(p => p.ColonyValue);
        }

        private void UpdateBestOrbitals()
        {
            // FB - this is done here for more performance. having set values here prevents calling shipbuilder by every planet every turn
            BestPlatformWeCanBuild = BestShipWeCanBuild(ShipData.RoleName.platform, this);
            BestStationWeCanBuild  = BestShipWeCanBuild(ShipData.RoleName.station, this);
        }

        public DebugTextBlock DebugEmpireTradeInfo()
        {
            int foodShips      = NumFreightersTrading(Goods.Food);
            int prodShips      = NumFreightersTrading(Goods.Production);
            int colonistsShips = NumFreightersTrading(Goods.Colonists);

            int foodImportPlanets = OwnedPlanets.Count(p => p.FoodImportSlots > 0);
            int prodImportPlanets = OwnedPlanets.Count(p => p.ProdImportSlots > 0);
            int coloImportPlanets = OwnedPlanets.Count(p => p.ColonistsImportSlots > 0);
            int foodExportPlanets = OwnedPlanets.Count(p => p.FoodExportSlots > 0);
            int prodExportPlanets = OwnedPlanets.Count(p => p.ProdExportSlots > 0);
            int coloExportPlanets = OwnedPlanets.Count(p => p.ColonistsExportSlots > 0);

            var debug = new DebugTextBlock();
            debug.AddLine($"Total Freighters / Cap: {TotalFreighters}/{FreighterCap}");
            debug.AddLine($"Freighter Types: F: {foodShips}  P: {prodShips} C: {colonistsShips}");
            debug.AddLine($"Freighters in Queue / Max: {FreightersBeingBuilt}/{MaxFreightersInQueue}");
            debug.AddLine($"Idle Freighters: {IdleFreighters.Length}");
            debug.AddLine("");
            debug.AddLine("Planet Trade:");
            debug.AddLine($"Importing Planets: F: {foodImportPlanets}  P: {prodImportPlanets}  C: {coloImportPlanets}");
            debug.AddLine($"Exporting Planets: F: {foodExportPlanets}  P: {prodExportPlanets}  C: {coloExportPlanets}");
            debug.AddLine("");
            debug.AddLine("Planets List:");
            debug.AddLine("");
            foreach (Planet p in OwnedPlanets)
            {
                int importSlots = p.FoodImportSlots + p.ProdImportSlots + p.ColonistsImportSlots;
                int exportSlots = p.FoodExportSlots + p.ProdExportSlots + p.ColonistsExportSlots;
                string incoming = p.IncomingFreighters.Count.ToString();
                string outgoing = p.OutgoingFreighters.Count.ToString();
                string starving = p.Storage.Food.AlmostZero() && p.Food.NetIncome < 0 ? " (Starving!)" : "";
                debug.AddLine($"{p.ParentSystem.Name} : {p.Name}{starving}");
                debug.AddLine($"Incoming / Import Slots: {incoming}/{importSlots}");
                debug.AddLine($"Outgoing / Export Slots: {outgoing}/{exportSlots}");
                debug.AddLine("");
            }
            debug.Header = Name;
            debug.HeaderColor = EmpireColor;
            return debug;
        }

        public DebugTextBlock DebugEmpirePlanetInfo()
        {
            var debug = new DebugTextBlock();
            foreach (Planet p in OwnedPlanets)
            {
                var lines = new Array<string>();
                string food = $"{(int)p.FoodHere}(%{100*p.Storage.FoodRatio:00.0}) {p.FS}";
                string prod = $"{(int)p.ProdHere}(%{100*p.Storage.ProdRatio:00.0}) {p.PS}";
                debug.AddLine($"{p.ParentSystem.Name} : {p.Name} ", Color.Yellow);
                debug.AddLine($"FoodHere: {food} ", Color.White);
                debug.AddLine($"ProdHere: {prod}");
                debug.AddRange(lines);
                debug.AddLine("");
            }
            debug.Header = Name;
            debug.HeaderColor = EmpireColor;
            return debug;
        }

        public void UpdateFleets(float elapsedTime)
        {
            updateContactsTimer -= elapsedTime;
            FleetUpdateTimer -= elapsedTime;
            foreach (var kv in FleetsDict)
            {
                kv.Value.Update(elapsedTime);
                if (FleetUpdateTimer <= 0f)
                    kv.Value.UpdateAI(elapsedTime, kv.Key);
            }
            if (FleetUpdateTimer < 0.0)
                FleetUpdateTimer = 5f;
        }

        void DoMoney()
        {
            MoneyLastTurn = Money;
            ++TurnCount;

            UpdateEmpirePlanets();
            UpdateTradeIncome();
            UpdateNetPlanetIncomes();
            UpdateShipMaintenance();
            Money += NetIncome;
        }

        public void UpdateNetPlanetIncomes()
        {
            NetPlanetIncomes              = 0;
            GrossPlanetIncome             = 0;
            ExcessGoodsMoneyAddedThisTurn = 0;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                {
                    planet.UpdateIncomes(false);
                    NetPlanetIncomes              += planet.Money.NetRevenue;
                    GrossPlanetIncome             += planet.Money.GrossRevenue;
                    ExcessGoodsMoneyAddedThisTurn += planet.ExcessGoodsIncome;
                }
        }

        public void UpdateEmpirePlanets()
        {
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                    planet.UpdateOwnedPlanet();
        }

        public float TotalAvgTradeIncome => GetTotalTradeIncome() + GetAverageTradeIncome();

        public int GetAverageTradeIncome()
        {
            return AllTimeTradeIncome / TurnCount;
        }

        public float GetTotalTradeIncome()
        {
            float total = 0f; 
            foreach (KeyValuePair<Empire, Relationship> kv in Relationships)
                if (kv.Value.Treaty_Trade) total += kv.Value.TradeIncome();
            return total;
        }

        void UpdateTradeIncome()
        {
            TradeMoneyAddedThisTurn = GetTotalTradeIncome();
        }

        private void UpdateShipMaintenance()
        {
            TotalShipMaintenance = 0.0f;

            using (OwnedShips.AcquireReadLock())
                foreach (Ship ship in OwnedShips)
                {
                    if (!ship.Active || ship.AI.State >= AIState.Scrap) continue;
                    float maintenance = ship.GetMaintCost();
                    if (data.DefenseBudget > 0 && ((ship.shipData.HullRole == ShipData.RoleName.platform && ship.IsTethered)
                                                   || (ship.shipData.HullRole == ShipData.RoleName.station &&
                                                       (ship.shipData.IsOrbitalDefense || !ship.shipData.IsShipyard))))
                    {
                        data.DefenseBudget -= maintenance;
                    }
                    TotalShipMaintenance += maintenance;
                }

            using (OwnedProjectors.AcquireReadLock())
                foreach (Ship ship in OwnedProjectors)
                {
                    if (data.SSPBudget > 0)
                    {
                        data.SSPBudget -= ship.GetMaintCost();
                        continue;
                    }
                    TotalShipMaintenance += ship.GetMaintCost();
                }

            TotalShipMaintenance *= data.Traits.MaintMultiplier;
        }

        public float EstimateNetIncomeAtTaxRate(float rate)
        {
            float plusNetIncome = (rate-data.TaxRate) * NetPlanetIncomes;
            return GrossIncome + plusNetIncome - BuildingAndShipMaint;
        }

        public float GetActualNetLastTurn() => Money - MoneyLastTurn; 

        public void FactionShipsWeCanBuild()
        {
            if (!isFaction) return;
            foreach (var ship in ResourceManager.ShipsDict)
            {
                if (data.Traits.ShipType == ship.Value.shipData.ShipStyle
                    || ship.Value.shipData.ShipStyle == "Misc"
                    || ship.Value.shipData.ShipStyle.IsEmpty())
                {
                    ShipsWeCanBuild.Add(ship.Key);
                    foreach (var hangar in ship.Value.Carrier.AllHangars)
                        ShipsWeCanBuild.Add(hangar.hangarShipUID);
                }
            }
            foreach (var hull in UnlockedHullsDict.Keys.ToArray())
                UnlockedHullsDict[hull] = true;
        }

        public void UpdateShipsWeCanBuild(Array<string> hulls = null)
        {
            if (isFaction)
            {
                FactionShipsWeCanBuild();
                return;
            }

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
                    if (ship.shipData.Role <= ShipData.RoleName.station)
                        structuresWeCanBuild.Add(ship.Name);
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

            PreferredAuxillaryShips[ShipData.RoleName.bomber]
                = PickFromCandidates(ShipData.RoleName.bomber, this, targetModule: ShipModuleType.Bomb);

            PreferredAuxillaryShips[ShipData.RoleName.carrier]
                = PickFromCandidates(ShipData.RoleName.carrier, this, targetModule: ShipModuleType.Hangar);

        }

        public bool WeCanBuildThis(string ship)
        {
            if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship ship1))
                return false;

            ShipData shipData = ship1.shipData;
            if (shipData == null)
            {
                Universe?.DebugWin?.DebugLogText($"{data.PortraitName} : shipData is null : '{ship}'", DebugModes.Normal);
                return false;
            }

            // If the ship role is not defined don't try to use it
            if (!UnlockedHullsDict.TryGetValue(shipData.Hull, out bool goodHull) || !goodHull)
                return false;

            if (shipData.TechsNeeded.Count > 0)
            {
                if (!shipData.UnLockable) return false;

                foreach (string shipTech in shipData.TechsNeeded)
                {
                    if (ShipTechs.Contains(shipTech)) continue;
                    TechEntry onlyShipTech = TechnologyDict[shipTech];
                    if (onlyShipTech.Unlocked) continue;

                    Universe?.DebugWin?.DebugLogText($"Locked Tech : '{shipTech}' in design : '{ship}'", DebugModes.Normal);
                    return false;
                }
                Universe?.DebugWin?.DebugLogText($"New Ship WeCanBuild {shipData.Name} Hull: '{shipData.Hull}' DesignRole: '{ship1.DesignRole}'"
                    , DebugModes.Last);
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
                        , DebugModes.Normal);
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

        public float GetTotalPop()
        {
            float num = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet p in OwnedPlanets)
                    num += p.PopulationBillion;
            return num;
        }

        public float GetGrossFoodPerTurn()
        {
            float num = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet p in OwnedPlanets)
                    num += p.Food.GrossIncome;
            return num;
        }

        public Planet.ColonyType AssessColonyNeeds2(Planet p)
        {
            float fertility = p.Fertility;
            float richness = p.MineralRichness;
            float pop = p.MaxPopulationBillion;
            if (IsCybernetic)
                 fertility = richness;
            if (richness >= 1.0f && fertility >= 1 && pop > 7)
                return Planet.ColonyType.Core;
            if (fertility > 0.5f && fertility <= 1 && richness <= 1 && pop < 8 && pop > 3)
                 return Planet.ColonyType.Research;
            if (fertility > 1.0f && richness < 1 && pop >=2)
                 return Planet.ColonyType.Agricultural;
            if (richness >= 1.0f )
                 return Planet.ColonyType.Industrial;

            return Planet.ColonyType.Colony;
        }

        public Planet.ColonyType AssessColonyNeeds(Planet p)
        {
            Planet.ColonyType type  = AssessColonyNeeds2(p);
            if (type != Planet.ColonyType.Colony)
                return type;

            float mineralWealth     = 0.0f;
            float popSupport        = 0.0f;
            float researchPotential = 0.0f;
            float fertility         = 0.0f;
            float militaryPotential = 0.0f;

            if (p.MineralRichness > .50f)
            {
                mineralWealth += p.MineralRichness + p.MaxPopulationBillion;
            }
            else
                mineralWealth += p.MineralRichness;

            if (p.MaxPopulation > 1000)
            {
                researchPotential += p.MaxPopulationBillion;
                if (IsCybernetic)
                {
                    if (p.MineralRichness > 1)
                        popSupport += p.MaxPopulationBillion + p.MineralRichness;
                }
                else
                {
                    if (p.Fertility > 1f)
                    {
                        if (p.MineralRichness > 1)
                            popSupport += p.MaxPopulationBillion + p.Fertility + p.MineralRichness;
                        fertility += p.Fertility + p.MaxPopulationBillion;
                    }
                }
            }
            else
            {
                militaryPotential += fertility + p.MineralRichness + p.MaxPopulationBillion;
                if (p.MaxPopulation >=500)
                {
                    
                    if (ResourceManager.TechTree.TryGetValue(ResearchTopic, out Technology tech))
                        researchPotential = (tech.ActualCost - Research) / tech.ActualCost
                                            * (p.Fertility*2 + p.MineralRichness + (p.MaxPopulation / 500));
                }
            }

            if (IsCybernetic)
                fertility = 0;

            int coreCount         = 0;
            int industrialCount   = 0;
            int agriculturalCount = 0;
            int militaryCount     = 0;
            int researchCount     = 0;
            using (OwnedPlanets.AcquireReadLock())
            {
                foreach (Planet planet in OwnedPlanets)
                {
                    if (planet.colonyType == Planet.ColonyType.Agricultural) ++agriculturalCount;
                    if (planet.colonyType == Planet.ColonyType.Core)         ++coreCount;
                    if (planet.colonyType == Planet.ColonyType.Industrial)   ++industrialCount;
                    if (planet.colonyType == Planet.ColonyType.Research)     ++researchCount;
                    if (planet.colonyType == Planet.ColonyType.Military)     ++militaryCount;
                }
            }
            float assignedFactor = (coreCount + industrialCount + agriculturalCount + militaryCount + researchCount)
                                   / (OwnedPlanets.Count + 0.01f);

            float coreDesire        = popSupport        + (assignedFactor - coreCount) ;
            float industrialDesire  = mineralWealth     + (assignedFactor - industrialCount);
            float agricultureDesire = fertility         + (assignedFactor - agriculturalCount);
            float militaryDesire    = militaryPotential + (assignedFactor - militaryCount);
            float researchDesire    = researchPotential + (assignedFactor - researchCount);



            if (coreDesire > industrialDesire && coreDesire > agricultureDesire
                                              && (coreDesire > militaryDesire && coreDesire > researchDesire))
                return Planet.ColonyType.Core;
            if (industrialDesire > coreDesire && industrialDesire > agricultureDesire
                                              && (industrialDesire > militaryDesire && industrialDesire > researchDesire))
                return Planet.ColonyType.Industrial;
            if (agricultureDesire > industrialDesire && agricultureDesire > coreDesire
                                                     && (agricultureDesire > militaryDesire && agricultureDesire > researchDesire))
                return Planet.ColonyType.Agricultural;
            return researchDesire > coreDesire && researchDesire > agricultureDesire
                                               && (researchDesire > militaryDesire && researchDesire > industrialDesire)
                ? Planet.ColonyType.Research : Planet.ColonyType.Military;
        }

        public void ResetBorders()
        {
            BorderNodes.ClearAndRecycle();
            SensorNodes.ClearAndRecycle();

            bool wellKnown = EmpireManager.Player == this || EmpireManager.Player.TryGetRelations(this, out Relationship rel)
                             && rel.Treaty_Alliance;
            bool known     = wellKnown || EmpireManager.Player.TryGetRelations(this, out Relationship relKnown)
                             && (relKnown.Treaty_Trade || relKnown.Treaty_OpenBorders);
            var allies     = new Array<Empire>();

            foreach (KeyValuePair<Empire, Relationship> keyValuePair in Relationships)
            {
                if (keyValuePair.Value.Treaty_Alliance)
                    allies.Add(keyValuePair.Key);
            }

            SetBordersKnownByAllies(allies, wellKnown);
            SetBordersByPlanet(known);

            // Moles are spies who have successfully been planted during 'Infiltrate' type missions, I believe - Doctor
            foreach (Mole mole in data.MoleList)
                SensorNodes.Add(new InfluenceNode
                {
                    Position = Universe.PlanetsDict[mole.PlanetGuid].Center,
                    Radius   = ProjectorRadius * data.SensorModifier,
                    Known    = true
                });

            Inhibitors.Clear();
            foreach (Ship ship in OwnedShips)
            {
                if (ship.InhibitionRadius > 0.0f)
                    Inhibitors.Add(ship);

                InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode.Position      = ship.Center;
                influenceNode.Radius        = ship.SensorRange;
                influenceNode.SourceObject  = ship;
                SensorNodes.Add(influenceNode);
            }

            foreach (Ship ship in OwnedProjectors)
            {
                if (ship.InhibitionRadius > 0f)
                    Inhibitors.Add(ship);

                InfluenceNode influenceNodeS = SensorNodes.RecycleObject() ?? new InfluenceNode();
                InfluenceNode influenceNodeB = BorderNodes.RecycleObject() ?? new InfluenceNode();

                influenceNodeS.Position     = ship.Center;
                influenceNodeS.Radius       = ProjectorRadius;  //projectors used as sensors again
                influenceNodeS.SourceObject = ship;

                influenceNodeB.Position     = ship.Center;
                influenceNodeB.Radius       = ProjectorRadius;  //projectors used as sensors again
                influenceNodeB.SourceObject = ship;
                bool seen                   = known || EmpireManager.Player.GetEmpireAI().ThreatMatrix.ContainsGuid(ship.guid);
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
                    influenceNode1.Radius = isFaction ? 20000f : ProjectorRadius + 10000f * planet.PopulationBillion;

                influenceNode1.Known = known;
                BorderNodes.Add(influenceNode1);

                InfluenceNode influenceNode3 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode3.SourceObject  = planet;
                influenceNode3.Position      = planet.Center;
                influenceNode3.Radius        = isFaction ? 1f : data.SensorModifier;
                influenceNode3.Known         = known;
                foreach (Building t in planet.BuildingList)
                {
                    if (t.SensorRange * data.SensorModifier > influenceNode3.Radius)
                    {
                        influenceNode3.Radius = t.SensorRange * data.SensorModifier;
                    }
                }

                SensorNodes.Add(influenceNode3);
            }
        }

        private void SetBordersKnownByAllies(Array<Empire> allies, bool wellKnown)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                Empire empire = allies[i];
                Planet[] array = empire.OwnedPlanets.ToArray();
                for (int y = 0; y < array.Length; y++)
                {
                    Planet planet = array[y];
                    InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode2.Position = planet.Center;
                    influenceNode2.Radius = isFaction
                        ? 1f
                        : this == Universe.PlayerEmpire
                            ? ProjectorRadius / 5f * empire.data.SensorModifier
                            : ProjectorRadius / 3f * empire.data.SensorModifier;
                    foreach (Building building in planet.BuildingList)
                        influenceNode2.Radius =
                            Math.Max(influenceNode2.Radius, building.SensorRange * data.SensorModifier);

                    if (influenceNode2.Radius <= 1) continue;
                    influenceNode2.Known = wellKnown;
                    SensorNodes.Add(influenceNode2);
                }

                BatchRemovalCollection<Ship> ships = empire.GetShips();
                for (int z = 0; z < ships.Count; z++)
                {
                    Ship ship = ships[z];

                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode.Position      = ship.Center;
                    influenceNode.Radius        = ship.SensorRange;
                    influenceNode.SourceObject  = ship;
                    SensorNodes.Add(influenceNode);
                }

                BatchRemovalCollection<Ship> projectors = empire.GetProjectors();
                for (int z = 0; z < projectors.Count; z++)
                {
                    Ship ship = projectors[z];
                    //loop over all ALLIED projectors
                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode.Position = ship.Center;
                    influenceNode.Radius =
                        ProjectorRadius; //projectors currently use their projection radius as sensors
                    SensorNodes.Add(influenceNode);
                    influenceNode.SourceObject = ship;
                    influenceNode.Known = wellKnown;
                }
            }
        }

        private void TakeTurn()
        {
            if (IsEmpireDead()) return;

            var list1 = new Array<Planet>();
            foreach (Planet planet in OwnedPlanets)
            {
                if (planet.Owner == null)
                    list1.Add(planet);
            }
            foreach (Planet planet in list1)
                OwnedPlanets.Remove(planet);
            for (int index = 0; index < data.AgentList.Count; ++index)
            {
                if (data.AgentList[index].Mission != AgentMission.Defending && data.AgentList[index].TurnsRemaining > 0)
                {
                    --data.AgentList[index].TurnsRemaining;
                    if (data.AgentList[index].TurnsRemaining == 0)
                        data.AgentList[index].DoMission(this);
                }
                //Age agents
                data.AgentList[index].Age += 0.1f;
                data.AgentList[index].ServiceYears += 0.1f;
            }
            data.AgentList.ApplyPendingRemovals();
            if (Money < 0.0 && !isFaction)
            {
                data.TurnsBelowZero += (short)(1 + -1 * (Money) / 500);
            }
            else
            {
                --data.TurnsBelowZero;
                if (data.TurnsBelowZero < 0)
                    data.TurnsBelowZero = 0;
            }
            float militaryStrength = 0.0f;
            string starDate = Universe.StarDateString;
            for (int index = 0; index < OwnedShips.Count; ++index)
            {
                Ship ship = OwnedShips[index];
                militaryStrength += ship.GetStrength();

                if (!data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(starDate))
                    StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].ShipCount++;
            }
            if (!data.IsRebelFaction && StatTracker.SnapshotsDict.ContainsKey(starDate))
            {
                StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].MilitaryStrength = militaryStrength;
                StatTracker.SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(this)].TaxRate = data.TaxRate;
            }
            if (isPlayer)
            {
                if (Universe.StarDate > 1060.0f)
                {
                    float aiTotalScore = 0.0f;
                    float score = 0.0f;
                    var aggressiveEmpires = new Array<Empire>();
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        if (!empire.isFaction && !empire.data.Defeated && empire != EmpireManager.Player)
                        {
                            aiTotalScore += empire.TotalScore;
                            if (empire.TotalScore > score)
                                score = empire.TotalScore;
                            if (empire.data.DiplomaticPersonality.Name == "Aggressive"
                                || empire.data.DiplomaticPersonality.Name == "Ruthless"
                                || empire.data.DiplomaticPersonality.Name == "Xenophobic")
                                aggressiveEmpires.Add(empire);
                        }
                    }

                    float playerTotalScore = EmpireManager.Player.TotalScore;
                    float allEmpireScore = aiTotalScore + playerTotalScore;

                    if (playerTotalScore > 0.5f * allEmpireScore)
                    {
                        if (playerTotalScore > score * 2.0)
                        {
                            if (aggressiveEmpires.Count >= 2)
                            {
                                Empire biggest = aggressiveEmpires.FindMax(emp => emp.TotalScore);
                                var leaders = new Array<Empire>();
                                foreach (Empire empire in aggressiveEmpires)
                                {
                                    if (empire != biggest && empire.GetRelations(biggest).Known
                                                          && biggest.TotalScore * 0.6f > empire.TotalScore)
                                        leaders.Add(empire);
                                }

                                //Added by McShooterz: prevent AI from automatically merging together
                                if (leaders.Count > 0 && !GlobalStats.PreventFederations)
                                {
                                    Empire strongest = leaders.FindMax(emp => biggest.GetRelations(emp).GetStrength());
                                    if (!biggest.GetRelations(strongest).AtWar)
                                        Universe.NotificationManager.AddPeacefulMergerNotification(biggest, strongest);
                                    else
                                        Universe.NotificationManager.AddSurrendered(biggest, strongest);
                                    biggest.AbsorbEmpire(strongest);
                                }
                            }
                        }
                    }
                }

                RandomEventManager.UpdateEvents();
                if (data.TurnsBelowZero == 5 && Money < 0.0)
                    Universe.NotificationManager.AddMoneyWarning();

                if (!Universe.NoEliminationVictory)
                {
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
                        Universe.ScreenManager.AddScreen(new YouWinScreen(Universe));
                        return;
                    }
                }

                foreach (Planet planet in OwnedPlanets)
                {
                    if (!data.IsRebelFaction)
                        StatTracker.SnapshotsDict[Universe.StarDateString][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                    if (planet.HasWinBuilding)
                    {
                        Universe.ScreenManager.AddScreen(new YouWinScreen(Universe, Localizer.Token(5085)));
                        return;
                    }
                }
            }

            Research = 0;
            MaxResearchPotential = 0;
            foreach (Planet planet in OwnedPlanets)
            {
                if (!data.IsRebelFaction)
                    StatTracker.SnapshotsDict[Universe.StarDateString][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;

                Research             += planet.Res.NetIncome;
                MaxResearchPotential += planet.Res.GrossMaxPotential;
            }

            if (data.TurnsBelowZero > 0 && Money < 0.0 && !Universe.Debug)
                Bankruptcy();

            CalculateScore();

            ApplyResearchPoints();

            UpdateRelationships();

            if (isFaction)
                EmpireAI.FactionUpdate();
            else if (!data.Defeated)
            {
                EmpireAI.Update();
                UpdateMaxColonyValue();
                UpdateBestOrbitals();
            }
            if (Money > data.CounterIntelligenceBudget)
            {
                Money -= data.CounterIntelligenceBudget;
                foreach (KeyValuePair<Empire, Relationship> keyValuePair in Relationships)
                {
                    var relationWithUs = keyValuePair.Key.GetRelations(this);
                    relationWithUs.IntelligencePenetration -= data.CounterIntelligenceBudget / 10f;
                    if (relationWithUs.IntelligencePenetration < 0.0f)
                        relationWithUs.IntelligencePenetration = 0.0f;
                }
            }

            if (isFaction)
                return;

            DispatchBuildAndScrapFreighters();
            if (isPlayer || AutoExplore)
                AssignExplorationTasks();
        }

        void Bankruptcy()
        {
            if (data.TurnsBelowZero >= 25)
            {
                Log.Info($"Rebellion for: {data.Traits.Name}");

                Empire rebels = EmpireManager.GetEmpireByName(data.RebelName)
                             ?? EmpireManager.FindRebellion(data.RebelName)
                             ?? EmpireManager.CreateRebelsFromEmpireData(data, this);

                if (rebels != null)
                {
                    Vector2 weightedCenter = GetWeightedCenter();
                    if (OwnedPlanets.FindMax(out Planet planet, p => weightedCenter.SqDist(p.Center)))
                    {
                        if (isPlayer)
                            Universe.NotificationManager.AddRebellionNotification(planet,
                                rebels);
                        for (int index = 0; index < planet.PopulationBillion; ++index)
                        {
                            Troop troop = EmpireManager.CreateRebelTroop(rebels);
                            troop.AssignTroopToTile(
                                planet);
                        }
                    }

                    Ship pirate = null;
                    using (GetShips().AcquireReadLock())
                        foreach (Ship pirateChoice in GetShips())
                        {
                            if (pirateChoice == null || !pirateChoice.Active)
                                continue;
                            pirate = pirateChoice;
                            break;
                        }

                    pirate?.ChangeLoyalty(rebels);
                }
                else Log.Error($"Rebellion failed: {data.RebelName}");

                data.TurnsBelowZero = 0;
            }
        }

        private bool IsEmpireDead()
        {
            if (isFaction) return false;
            if (data.Defeated) return true;
            if (!GlobalStats.EliminationMode && OwnedPlanets.Count != 0)
                return false;
            if (GlobalStats.EliminationMode && (Capital == null || Capital.Owner == this))
                return false;

            SetAsDefeated();
            if (!isPlayer)
            {
                if (EmpireManager.Player.TryGetRelations(this, out Relationship relationship) && relationship.Known)
                    Universe.NotificationManager.AddEmpireDiedNotification(this);
                return true;
            }

            StarDriveGame.Instance.EndingGame(true);
            foreach (Ship ship in Universe.MasterShipList)
                ship.Die(null, true);

            Universe.Paused = true;
            HelperFunctions.CollectMemory();
            StarDriveGame.Instance.EndingGame(false);
            Universe.ScreenManager.AddScreen(new YouLoseScreen(Universe));
            Universe.Paused = false;
            return true;

        }

        private void ApplyResearchPoints()
        {
            if (string.IsNullOrEmpty(ResearchTopic))
            {
                if (data.ResearchQueue.Count > 0)
                    ResearchTopic = data.ResearchQueue[0];
                else
                    return;
            }

            float research = Research + leftoverResearch;
            TechEntry tech = GetTechEntry(ResearchTopic);
            if (tech.UID.IsEmpty())
                return;
            //reduce the impact of tech that doesnt affect cybernetics.
            float cyberneticMultiplier = 1.0f;
            if (IsCybernetic && tech.UnlocksFoodBuilding)
                cyberneticMultiplier = .5f;

            float techCost = tech.TechCost * cyberneticMultiplier;
            if (techCost - tech.Progress > research)
            {
                tech.Progress += research;
                leftoverResearch = 0f;
                return;
            }

            research -= techCost - tech.Progress;
            tech.Progress = techCost;
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

            leftoverResearch = research;
        }

        private void UpdateRelationships()
        {
            if (isFaction) return;
            int atWarCount = 0;
            foreach (var kv in Relationships)
                if (kv.Value.Known || isPlayer)
                {
                    kv.Value.UpdateRelationship(this, kv.Key);
                    if (kv.Value.AtWar && !kv.Key.isFaction) atWarCount++;
                }
            AtWarCount = atWarCount;
        }

        private void CalculateScore()
        {
            TotalScore = 0;
            TechScore = 0.0f;
            IndustrialScore = 0.0f;
            ExpansionScore = 0.0f;
            foreach (KeyValuePair<string, TechEntry> keyValuePair in TechnologyDict)
            {
                if (keyValuePair.Value.Unlocked)
                    TechScore += ResourceManager.Tech(keyValuePair.Key).ActualCost / 100;
            }
            foreach (Planet planet in OwnedPlanets)
            {
                ExpansionScore += (float)(planet.Fertility + (double)planet.MineralRichness + planet.PopulationBillion);
                foreach (Building building in planet.BuildingList)
                    IndustrialScore += building.ActualCost / 20f;
            }


            data.MilitaryScoreTotal += currentMilitaryStrength;
            TotalScore = (int)(MilitaryScore / 100.0 + IndustrialScore + TechScore + ExpansionScore);
            MilitaryScore = data.ScoreAverage == 0 ? 0f : data.MilitaryScoreTotal / data.ScoreAverage;
            ++data.ScoreAverage;
            if (data.ScoreAverage >= 120)  //fbedard: reset every 60 turns
            {
                data.MilitaryScoreTotal = MilitaryScore * 60f;
                data.ScoreAverage = 60;
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
                        if (troop.Loyalty == target)
                            troop.SetOwner(this);
                    }
                }
            }
            target.ClearAllPlanets();
            foreach (Ship ship in target.GetShips())
            {
                OwnedShips.Add(ship);
                ship.loyalty = this;
                ship.fleet?.RemoveShip(ship);
                ship.AI.ClearOrders();
            }
            foreach (Ship ship in target.GetProjectors())
            {
                OwnedProjectors.Add(ship);
                ship.loyalty = this;
                ship.fleet?.RemoveShip(ship);
                ship.AI.ClearOrders();
            }
            target.GetShips().Clear();
            target.GetProjectors().Clear();
            foreach (TechEntry techEntry in target.GetTDict().Values)
            {
                if (techEntry.Unlocked)
                    AcquireTech(techEntry.UID, target);
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
                data.OwnedArtifacts.Add(artifact);
                AddArtifact(artifact);
            }
            target.data.OwnedArtifacts.Clear();
            if (target.Money > 0.0)
            {
                Money += target.Money;
                target.Money = 0.0f;
            }
            target.SetAsMerged();
            ResetBorders();
            UpdateShipsWeCanBuild();
            if (this != EmpireManager.Player)
            {
                data.difficulty = Difficulty.Brutal;
                EmpireAI.TaskList.ForEach(
                    militaryTask =>
                    {
                        militaryTask.EndTask();
                    }, false, false, false);
                EmpireAI.TaskList.ApplyPendingRemovals();
                EmpireAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                EmpireAI.DefensiveCoordinator.DefenseDict.Clear();
                ForcePool.Clear();
                foreach (Ship s in OwnedShips)
                {
                    // added by gremlin Do not include 0 strength ships in defensive force pool
                    s.AI.ClearOrders();
                }

                if (IsCybernetic)
                {
                    foreach (Planet planet in OwnedPlanets)
                    {
                        Array<Building> list = new Array<Building>();
                        foreach (Building building in planet.BuildingList)
                        {
                            if (building.PlusFlatFoodAmount > 0.0 || building.PlusFoodPerColonist > 0.0 ||
                                building.PlusTerraformPoints > 0.0)
                                list.Add(building);
                        }

                        foreach (Building b in list)
                            b.ScrapBuilding(planet);
                    }
                }
            }

            foreach (Agent agent in target.data.AgentList)
            {
                data.AgentList.Add(agent);
                agent.Mission = AgentMission.Defending;
                agent.TargetEmpire = null;
            }
            EmpireAI.DefensiveCoordinator.ManageForcePool();
            target.data.AgentList.Clear();
            target.data.AbsorbedBy = data.Traits.Name;
            CalculateScore();
        }

        public void ForcePoolAdd(Ship s)
        {
            if (s.shipData.Role <= ShipData.RoleName.freighter ||
                s.shipData.ShipCategory == ShipData.Category.Civilian)
                return;
            EmpireAI.AssignShipToForce(s);
        }

        public void ForcePoolRemove(Ship s) => ForcePool.RemoveSwapLast(s);

        public Array<Ship> GetForcePool() => ForcePool;

        public float GetForcePoolStrength()
        {
            float num = 0.0f;
            foreach (Ship ship in ForcePool)
                num += ship.GetStrength();
            return num;
        }

        public bool HavePreReq(string techID) => GetTechEntry(techID).HasPreReq(this);

        private void DispatchBuildAndScrapFreighters()
        {
            // Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (NonCybernetic)
                DispatchOrBuildFreighters(Goods.Food);

            DispatchOrBuildFreighters(Goods.Production);
            DispatchOrBuildFreighters(Goods.Colonists);
            UpdateFreighterTimersAndScrap();
        }

        private void UpdateFreighterTimersAndScrap()
        {
            if (isPlayer && !AutoFreighters)
                return;

            Ship[] ownedFreighters = OwnedShips.Filter(s => s.IsFreighter);
            for (int i = 0; i < ownedFreighters.Length; ++i)
            {
                Ship freighter = ownedFreighters[i];
                if (freighter.IsIdleFreighter)
                {
                    freighter.TradeTimer -= 5; // each turn is 5 seconds
                    if (freighter.TradeTimer < 0)
                    {
                        freighter.AI.OrderScrapShip();
                        freighter.TradeTimer = 300;
                    }
                }
                else
                    freighter.TradeTimer = 300;
            }
        }

        private void DispatchOrBuildFreighters(Goods goods)
        {
            Planet[] importingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsImportSlots(goods) > 0);
            if (importingPlanets.Length == 0)
                return;

            Planet[] exportingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsExportSlots(goods) > 0);
            if (exportingPlanets.Length == 0)
                return;

            if (IdleFreighters.Length == 0)
            {
                if (FreightersBeingBuilt < MaxFreightersInQueue)
                    BuildFreighter();
                return;
            }

            foreach (Planet importPlanet in importingPlanets)
            {
                Ship closestIdleFreighter;
                Planet exportPlanet = exportingPlanets.FindClosestTo(importPlanet);
                if (exportPlanet == null) // no more exporting planets
                    break;

                if (!isPlayer || AutoFreighters)
                    closestIdleFreighter = IdleFreighters.FindClosestTo(exportPlanet);
                else
                    closestIdleFreighter = ClosestIdleFreighterManual(exportPlanet, goods);

                if (closestIdleFreighter == null) // no more available freighters
                    break;

                closestIdleFreighter.AI.SetupFreighterPlan(exportPlanet, importPlanet, goods);
            }
        }

        private Ship ClosestIdleFreighterManual(Planet exportPlanet, Goods goods)
        {
            Ship closestIdleFreighter = null;
            switch (goods)
            {
                case Goods.Production:
                case Goods.Food:
                    closestIdleFreighter = IdleFreighters.FindClosestTo(exportPlanet, s => s.TransportingProduction
                                                                                              || s.TransportingFood);
                    break;
                case Goods.Colonists:
                    closestIdleFreighter = IdleFreighters.FindClosestTo(exportPlanet, s => s.DoingPassengerTransport);
                    break;
            }
            return closestIdleFreighter;
        }

        private void BuildFreighter()
        {
            if (isPlayer && !AutoFreighters)
                return;

            if (FreighterCap > TotalFreighters + FreightersBeingBuilt && MaxFreightersInQueue > FreightersBeingBuilt)
                EmpireAI.Goals.Add(new IncreaseFreighters(this));
        }

        int NumFreightersTrading(Goods goods)
        {
            return OwnedShips.Count(s => s.IsFreighter && !s.IsIdleFreighter && s.AI.HasTradeGoal(goods));
        }

        public void ReportGoalComplete(Goal g)
        {
            for (int index = EmpireAI.Goals.Count - 1; index >= 0; --index)
            {
                if (EmpireAI.Goals[index] != g) continue;
                EmpireAI.Goals.RemoveAtSwapLast(index);
                break;
            }
        }

        public EmpireAI GetEmpireAI() => EmpireAI;

        public Vector2 GetWeightedCenter()
        {
            int planets     = 0;
            Vector2 avgPlanetCenter = new Vector2();

            using (OwnedPlanets.AcquireReadLock())
            foreach (Planet planet in OwnedPlanets)
            {
                for (int x = 0; x < planet.PopulationBillion; ++x)
                {
                    ++planets;
                    avgPlanetCenter += planet.Center;
                }
            }
            if (planets == 0)
                planets = 1;
            return avgPlanetCenter / planets;
        }

        public void TheyKilledOurShip(Empire they, ShipRole.Race expData)
        {
            if (!isFaction || this != EmpireManager.Remnants) return;
            if (!they.isPlayer) return;
            if (GlobalStats.ActiveModInfo?.removeRemnantStory == true) return;

            GlobalStats.IncrementRemnantKills((int)expData.KillExp);
        }

        private void AssignExplorationTasks()
        {
            int unexplored =0;
            bool haveUnexploredSystems = false;
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem solarSystem = UniverseScreen.SolarSystemList[i];
                if (solarSystem.IsExploredBy(this)) continue;
                if (++unexplored > 20) break;
            }

            haveUnexploredSystems = unexplored != 0;
            int numScouts = 0;
            if (!haveUnexploredSystems)
            {
                for (int i = 0; i < OwnedShips.Count; i++)
                {
                    Ship ship = OwnedShips[i];
                    if (ship.AI.State == AIState.Explore)
                        ship.AI.OrderOrbitNearest(true);
                }
                return;
            }

            // already building a scout? then just quit
            for (int i = 0; i < EmpireAI.Goals.Count; i++)
            {
                Goal goal = EmpireAI.Goals[i];
                if (goal.type == GoalType.BuildScout)
                    return;
            }

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

        private void ApplyFertilityChange(float amount)
        {
            if (amount.AlmostEqual(0)) return;

            data.EmpireFertilityBonus += amount;
            IReadOnlyList<Planet> list = GetPlanets();
            for (int i = 0; i < list.Count; i++)
            {
                Planet planet = list[i];
                planet.AddFertility(amount);
            }
        }

        public void AddArtifact(Artifact art)
        {
            data.OwnedArtifacts.Add(art);
            ApplyFertilityChange(art.GetFertilityBonus(data));
            data.Traits.DiplomacyMod         += art.GetDiplomacyBonus(data);
            data.Traits.GroundCombatModifier += art.GetGroundCombatBonus(data);
            data.Traits.ModHpModifier        += art.GetModuleHpMod(data);
            data.FlatMoneyBonus              += art.GetFlatMoneyBonus(data);
            data.Traits.ProductionMod        += art.GetProductionBonus(data);
            data.Traits.ReproductionMod      += art.GetReproductionMod(data);
            data.Traits.ResearchMod          += art.GetResearchMod(data);
            data.SensorModifier              += art.GetSensorMod(data);
            data.ShieldPenBonusChance        += art.GetShieldPenMod(data);
            EmpireShipBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
        }

        public void RemoveArtifact(Artifact art)
        {
            data.OwnedArtifacts.Remove(art);

            ApplyFertilityChange(-art.GetFertilityBonus(data));
            data.Traits.DiplomacyMod         -= art.GetDiplomacyBonus(data);
            data.Traits.GroundCombatModifier -= art.GetGroundCombatBonus(data);
            data.Traits.ModHpModifier        -= art.GetModuleHpMod(data);
            data.FlatMoneyBonus              -= art.GetFlatMoneyBonus(data);
            data.Traits.ProductionMod        -= art.GetProductionBonus(data);
            data.Traits.ReproductionMod      -= art.GetReproductionMod(data);
            data.Traits.ResearchMod          -= art.GetResearchMod(data);
            data.SensorModifier              -= art.GetSensorMod(data);
            data.ShieldPenBonusChance        -= art.GetShieldPenMod(data);
            EmpireShipBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
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
            GetEmpireAI().DefensiveCoordinator.Remove(ship);

            ship.AI.ClearOrders();
            ship.ClearFleet();
        }


        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            if (!TryGetRelations(targetEmpire, out Relationship rel) || rel == null)
                return false;
            if(!rel.Known || rel.AtWar)
                return true;
            if (rel.Treaty_NAPact)
                return false;
            if (isFaction || targetEmpire.isFaction)
                return true;
            if (rel.TotalAnger > 50)
                return true;

            if (target == null)
                return true; // this is an inanimate check, so it won't cause trouble?
            return target.IsAttackable(this, rel);
        }

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.guid == planetGuid)
                    return p;
            return null;
        }

        public class InfluenceNode
        {
            public Vector2 Position;
            public object SourceObject; // SolarSystem, Planet OR Ship
            public bool DrewThisTurn;
            public float Radius;
            public bool Known;
            public GameplayObject GameObject;

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
            OwnedPlanets?.Dispose(ref OwnedPlanets);
            OwnedProjectors?.Dispose(ref OwnedProjectors);
            OwnedSolarSystems?.Dispose(ref OwnedSolarSystems);
        }

        public override string ToString() => $"{(isPlayer?"Player":"AI")}({Id}) '{Name}'";
    }
}
