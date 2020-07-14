using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Empires;
using Ship_Game.Empires.ShipPools;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Fleets;

namespace Ship_Game
{
    using static ShipBuilder;
    public enum TechUnlockType
    {
        Normal,
        Spy,
        Diplomacy,
        Event,
        Scrap
    }
    public sealed partial class Empire : IDisposable
    {
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! Empire must not be serialized! Add [XmlIgnore][JsonIgnore] to `public Empire XXX;` PROPERTIES/FIELDS. {this}");

        public float GetProjectorRadius() => Universe.SubSpaceProjectors.Radius * data.SensorModifier;
        public float GetProjectorRadius(Planet planet) => GetProjectorRadius() + 10000f * planet.PopulationBillion;

        readonly Map<int, Fleet> FleetsDict = new Map<int, Fleet>();


        public readonly Map<string, bool> UnlockedHullsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedTroopDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedBuildingsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedModulesDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Array<Troop> UnlockedTroops = new Array<Troop>();

        public float GetWarOffensiveRatio()
        {
            float territorialism = 1 - (data.DiplomaticPersonality?.Territorialism ?? 100) / 100f;
            float militaryRatio  = Research.Strategy.MilitaryRatio;
            float opportunism    = data.DiplomaticPersonality?.Opportunism ?? 1;
            return (1 + territorialism + militaryRatio + opportunism) / 4;
        }

        readonly int[] MoneyHistory = new int[10];
        int MoneyHistoryIndex = 0;

        public void SaveMoneyHistory(SavedGame.EmpireSaveData empire)
        {
            if (empire.NormalizedMoney == null) empire.NormalizedMoney = new Array<float>();
            for (int x = 0; x < MoneyHistory.Length; x++) empire.NormalizedMoney.Add(MoneyHistory[x]);
        }

        public void RestoreMoneyHistoryFromSave(SavedGame.EmpireSaveData empire)
        {
            if (empire.NormalizedMoney == null)
            {
                NormalizedMoney = empire.Money;
            }
            else
            {
                for (int x = 0; x < empire.NormalizedMoney.Count(); x++)
                    NormalizedMoney = (int)empire.NormalizedMoney[x];
            }
        }

        public float NormalizedMoney
        {
            get
            {
                float total = 0;
                int count = 0;
                for (int index = 0; index < MoneyHistory.Length; index++)
                {
                    int money = MoneyHistory[index];
                    if (money <= 0) continue;
                    count++;
                    total += money;
                }
                return count > 0 ? total / count : Money;
            }
            set
            {
                MoneyHistory[MoneyHistoryIndex] = (int)value;
                MoneyHistoryIndex = ++MoneyHistoryIndex > 9 ? 0 : MoneyHistoryIndex;
            }
        }


        public Map<string, TechEntry> TechnologyDict = new Map<string, TechEntry>(StringComparer.InvariantCultureIgnoreCase);
        public Array<Ship> Inhibitors = new Array<Ship>();
        //public Array<Ship> ShipsToAdd = new Array<Ship>();
        public Array<SpaceRoad> SpaceRoadsList = new Array<SpaceRoad>();

        float MoneyValue = 1000f;
        public float Money
        {
            get => MoneyValue;
            set => MoneyValue = value.NaNChecked(0f, "Empire.Money");
        }

        private BatchRemovalCollection<Planet> OwnedPlanets = new BatchRemovalCollection<Planet>();
        private BatchRemovalCollection<SolarSystem> OwnedSolarSystems = new BatchRemovalCollection<SolarSystem>();
        private BatchRemovalCollection<Ship> OwnedProjectors = new BatchRemovalCollection<Ship>();
        private BatchRemovalCollection<Ship> OwnedShips = new BatchRemovalCollection<Ship>();
        public BatchRemovalCollection<Ship> KnownShips = new BatchRemovalCollection<Ship>();
        public BatchRemovalCollection<InfluenceNode> BorderNodes = new BatchRemovalCollection<InfluenceNode>();
        public BatchRemovalCollection<InfluenceNode> SensorNodes = new BatchRemovalCollection<InfluenceNode>();
        private readonly Map<SolarSystem, bool> HostilesPresent = new Map<SolarSystem, bool>();
        private readonly Map<Empire, Relationship> Relationships = new Map<Empire, Relationship>();
        public HashSet<string> ShipsWeCanBuild = new HashSet<string>();
        public HashSet<string> structuresWeCanBuild = new HashSet<string>();
        private float FleetUpdateTimer = 5f;
        private int TurnCount = 1;
        private Fleet DefensiveFleet = new Fleet();
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;

        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesn't normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        public bool isFaction;

        // For Pirate Factions. This will allow the Empire to be pirates
        public Pirates Pirates { get; private set; }

        public Color EmpireColor;
        public static UniverseScreen Universe;
        private EmpireAI EmpireAI;
        private float UpdateTimer;
        public bool isPlayer;
        public float TotalShipMaintenance { get; private set; }
        public float TotalWarShipMaintenance { get; private set; }
        public float TotalCivShipMaintenance { get; private set; }
        public float TotalEmpireSupportMaintenance { get; private set; }
        public float TotalOrbitalMaintenance { get; private set; }
        public float TotalMaintenanceInScrap { get; private set; }
        public float TotalTroopShipMaintenance { get; private set; }

        public float updateContactsTimer = .2f;
        private bool InitializedHostilesDict;
        public float NetPlanetIncomes { get; private set; }
        public float GrossPlanetIncome { get; private set; }
        public float PotentialIncome { get; private set; }
        public float ExcessGoodsMoneyAddedThisTurn { get; private set; } // money tax from excess goods
        public float MoneyLastTurn;
        public int AllTimeTradeIncome;
        public bool AutoBuild;
        public bool AutoExplore;
        public bool AutoColonize;
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
        public bool CanBuildPlatforms;
        public bool CanBuildStations;
        public bool CanBuildShipyards;
        public float CurrentMilitaryStrength;
        public ShipPool Pool;
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue { get; private set; }
        public Ship BestPlatformWeCanBuild { get; private set; }
        public Ship BestStationWeCanBuild { get; private set; }
        public HashSet<string> ShipTechs = new HashSet<string>();
        public EmpireUI UI;
        public int GetEmpireTechLevel() => (int)Math.Floor(ShipTechs.Count / 3f);

        public int AtWarCount;
        public Array<string> BomberTech      = new Array<string>();
        public Array<string> TroopShipTech   = new Array<string>();
        public Array<string> CarrierTech     = new Array<string>();
        public Array<string> SupportShipTech = new Array<string>();
        public Planet[] RallyPoints          = Empty<Planet>.Array;
        public Ship BoardingShuttle          => ResourceManager.ShipsDict["Assault Shuttle"];
        public Ship SupplyShuttle            => ResourceManager.ShipsDict["Supply_Shuttle"];
        public bool IsCybernetic             => data.Traits.Cybernetic != 0;
        public bool NonCybernetic            => data.Traits.Cybernetic == 0;
        public bool WeArePirates             => Pirates != null; // Use this to figure out if this empire is pirate faction

        public Dictionary<ShipData.RoleName, string> PreferredAuxillaryShips = new Dictionary<ShipData.RoleName, string>();

        // Income this turn before deducting ship maintenance
        public float GrossIncome              => GrossPlanetIncome + TotalTradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus;
        public float NetIncome                => GrossIncome - AllSpending;
        public float TotalBuildingMaintenance => GrossPlanetIncome - NetPlanetIncomes;
        public float BuildingAndShipMaint     => TotalBuildingMaintenance + TotalShipMaintenance;
        public float AllSpending              => BuildingAndShipMaint + MoneySpendOnProductionThisTurn;

        public Planet[] SpacePorts => OwnedPlanets.Filter(p => p.HasSpacePort);
        public Planet[] MilitaryOutposts => OwnedPlanets.Filter(p => p.AllowInfantry); // Capitals allow Infantry as well
        public Planet[] SafeSpacePorts => OwnedPlanets.Filter(p => p.HasSpacePort && p.Safe);

        public float MoneySpendOnProductionThisTurn { get; private set; }

        public readonly EmpireResearch Research;

        public DifficultyModifiers DifficultyModifiers { get; private set; }
        // Empire unique ID. If this is 0, then this empire is invalid!
        // Set in EmpireManager.cs
        public int Id;

        public string Name => data.Traits.Name;
        public void AddShipNextFrame(Ship s)
        {
            s.AI.ClearOrdersAndWayPoints(AIState.AwaitingOrders, false);
            Pool.AddShipNextFame(s);
        }

        public void AddShipNextFrame(Ship[] s)
        {
            foreach (var ship in s)
                Pool.AddShipNextFame(ship);
        }
        public void AddShipNextFrame(Array<Ship> s)
        {
            foreach (var ship in s)
                Pool.AddShipNextFame(ship);
        }

        public Empire()
        {
            UI       = new EmpireUI(this);
            Research = new EmpireResearch(this);
            Pool     = new ShipPool(this);

            // @note @todo This is very flaky and weird!
            UpdateTimer = RandomMath.RandomBetween(.02f, .3f);
        }

        public Empire(Empire parentEmpire)
        {
            UI             = new EmpireUI(this);
            Research       = new EmpireResearch(this);
            TechnologyDict = parentEmpire.TechnologyDict;
            Pool           = new ShipPool(this);
        }

        public void SetAsPirates(bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Pirates = new Pirates(this, fromSave, goals);
        }

        public void AddMoney(float moneyDiff)
        {
            Money += moneyDiff;
        }

        public void TriggerAllShipStatusUpdate()
        {
            foreach (Ship ship in OwnedShips) //@todo can make a global ship unlock flag.
                ship.shipStatusChanged = true;
        }

        public Planet FindNearestRallyPoint(Vector2 location)
        {
            return RallyPoints.FindMin(p => p.Center.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Center.SqDist(location));
        }

        public Planet FindNearestSafeRallyPoint(Vector2 location)
        {
            return RallyPoints.FindMinFiltered(p => !IsEmpireAttackable(p.Owner), p => p.Center.SqDist(location))
                ?? OwnedPlanets.FindMinFiltered(p=> !IsEmpireAttackable(p.Owner), p => p.Center.SqDist(location));
        }

        public Planet RallyShipYardNearestTo(Vector2 position)
        {
            return RallyPoints.FindMinFiltered(p => p.HasSpacePort,
                                               p => position.SqDist(p.Center))
                ?? SpacePorts.FindMin(p => position.SqDist(p.Center))
                ?? FindNearestRallyPoint(position);
        }

        public bool GetCurrentCapital(out Planet capital)
        {
            capital      = null;
            var capitals = OwnedPlanets.Filter(p => p.BuildingList.Any(b => b.IsCapital));
            if (capitals.Length > 0)
                capital = capitals.First();

            return capitals.Length > 0;
        }

        public bool FindClosestSpacePort(Vector2 position, out Planet closest)
        {
            closest = SpacePorts.FindMin(p => p.Center.SqDist(position));
            return closest != null;
        }

        public bool FindPlanetToBuildAt(IReadOnlyList<Planet> ports, Ship ship, out Planet chosen)
        {
            if (ports.Count != 0)
            {
                float cost = ship.GetCost(this);

                chosen = FindPlanetToBuildAt(ports, cost);
                return true;
            }
            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {ship} at! Candidates:{ports.Count}");
            chosen = null;
            return false;
        }

        public bool FindPlanetToBuildAt(IReadOnlyList<Planet> ports, Troop troop, out Planet chosen)
        {
            if (ports.Count != 0)
            {
                float cost = troop.ActualCost;
                chosen = FindPlanetToBuildAt(ports, cost);
                return true;
            }
            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {troop} at! Candidates:{ports.Count}");
            chosen = null;
            return false;
        }

        public Planet FindPlanetToBuildAt(IReadOnlyList<Planet> ports, float cost)
        {
            // focus on the best producing planets (number depends on the empire size)
            if (GetBestPorts(ports, out Planet[] bestPorts))
                return bestPorts.Sorted(p => p.TurnsUntilQueueComplete(cost)).First();

            return null;
        }

        bool GetBestPorts(IReadOnlyList<Planet> ports, out Planet[] bestPorts)
        {
            bestPorts = null;
            if (ports.Count > 0)
            {
                int numPlanetsToFocus = (OwnedPlanets.Count / 5).Clamped(1, ports.Count + 1);
                bestPorts = ports.SortedDescending(p => p.Prod.NetMaxPotential);
                bestPorts = bestPorts.Take(numPlanetsToFocus).ToArray();
            }

            return bestPorts != null;
        }

        public Planet GetOrbitPlanetAfterBuild(Planet builtAt)
        {
            if (GetBestPorts(SafeSpacePorts, out Planet[] bestPorts) && !bestPorts.Contains(builtAt))
            {
                return bestPorts.Sorted(p => p.Center.Distance(builtAt.Center)).First();
            }

            return builtAt;
        }

        public float KnownEnemyStrengthIn(SolarSystem system)
                     => EmpireAI.ThreatMatrix.PingHostileStr(system.Position, system.Radius, this);
        public float KnownEnemyStrengthIn(AO ao)
             => EmpireAI.ThreatMatrix.PingHostileStr(ao.Center, ao.Radius, this);

        public WeaponTagModifier WeaponBonuses(WeaponTag which) => data.WeaponTags[which];
        public Map<int, Fleet> GetFleetsDict() => FleetsDict;
        public Fleet GetFleetOrNull(int key) => FleetsDict.TryGetValue(key, out Fleet fleet) ? fleet : null;
        public Fleet GetFleet(int key) => FleetsDict[key];

        public float TotalRemnantStrTryingToClear()
        {
            Fleet[] fleets = FleetsDict.Values.ToArray();
            float str = 0;
            for (int i = 0; i < fleets.Length; ++i)
            {
                Fleet fleet = fleets[i];
                if (fleet.FleetTask?.TargetPlanet?.Guardians.Count > 0)
                    str += fleet.FleetTask.EnemyStrength;
            }

            return str;
        }


        public Fleet FirstFleet
        {
            get => FleetsDict[1];
            set
            {
                Fleet existing = FleetsDict[1];
                if (existing != value)
                {
                    existing.Reset();
                    FleetsDict[1] = value;
                }
            }
        }

        public void SetRallyPoints()
        {
            Array<Planet> rallyPlanets;
            // defeated empires and factions can use rally points now.
            if (OwnedPlanets.Count == 0)
            {
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
                RallyPoints.Sort(rp => rp.ParentSystem.OwnerList.Count > 1);
                return;
            }

            rallyPlanets = new Array<Planet>();
            foreach (Planet planet in OwnedPlanets)
            {
                if (planet.HasSpacePort && planet.Safe)
                    rallyPlanets.Add(planet);
            }

            if (rallyPlanets.Count > 0)
            {
                RallyPoints = rallyPlanets.ToArray();
                return;
            }

            // Could not find any planet with space port and with no enemies in sensor range
            // So get the most producing planet and hope for the best
            rallyPlanets.Add(OwnedPlanets.FindMax(planet => planet.Prod.GrossIncome));
            RallyPoints = rallyPlanets.ToArray();
            if (RallyPoints.Length == 0)
                Log.Error("SetRallyPoint: No Planets found");
        }

        public Planet[] GetSafeAOCoreWorlds()
        {
            var nearAO = EmpireAI.AreasOfOperations
                .FilterSelect(ao => ao.CoreWorld?.ParentSystem.OwnerList.Count ==1,
                              ao => ao.CoreWorld);
            return nearAO;
        }

        public Planet[] GetAOCoreWorlds() => EmpireAI.AreasOfOperations.Select(ao => ao.CoreWorld);

        public AO GetAOFromCoreWorld(Planet coreWorld)
        {
            return EmpireAI.AreasOfOperations.Find(a => a.CoreWorld == coreWorld);
        }
        public Planet[] GetSafeAOWorlds()
        {
            var safeWorlds = new Array<Planet>();
            for (int i = 0; i < EmpireAI.AreasOfOperations.Count; i++)
            {
                var ao      = EmpireAI.AreasOfOperations[i];
                var planets = ao.GetPlanets().Filter(p => p.ParentSystem.OwnerList.Count == 1);
                safeWorlds.AddRange(planets);
            }

            return safeWorlds.ToArray();
        }

        /// <summary>
        /// Find nearest safe world to orbit.
        /// first check AI coreworlds
        /// then check ai area of operation worlds.
        /// then check any planet in closest ao.
        /// then just find a planet. 
        /// </summary>
        public Planet GetBestNearbyPlanetToOrbitForAI(Ship ship)
        {
            var coreWorld = ship.loyalty.GetSafeAOCoreWorlds()?.FindClosestTo(ship);
            Planet home = null;

            if (coreWorld != null)
            {
                home = coreWorld;
            }
            else
            {
                home = ship.loyalty.GetSafeAOWorlds().FindClosestTo(ship);
            }

            if (home == null)
            {
                var nearestAO = ship.loyalty.GetEmpireAI().FindClosestAOTo(ship.Center);
                home = nearestAO.GetPlanets().FindClosestTo(ship);
            }

            if (home == null)
            {
                home = Universe.PlanetsDict.FindMinValue(p => p.Center.Distance(ship.Center));
            }

            return home;
        }

        Array<Planet> GetPlanetsNearStations()
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

        public float RacialEnvModifer(PlanetCategory category)
        {
            float modifer = 1f; // If no Env tags were found, the multiplier is 1.
            switch (category)
            {
                case PlanetCategory.Terran:  modifer = data.EnvTerran;  break;
                case PlanetCategory.Oceanic: modifer = data.EnvOceanic; break;
                case PlanetCategory.Steppe:  modifer = data.EnvSteppe;  break;
                case PlanetCategory.Tundra:  modifer = data.EnvTundra;  break;
                case PlanetCategory.Swamp:   modifer = data.EnvSwamp;   break;
                case PlanetCategory.Desert:  modifer = data.EnvDesert;  break;
                case PlanetCategory.Ice:     modifer = data.EnvIce;     break;
                case PlanetCategory.Barren:  modifer = data.EnvBarren;  break;
            }

            return modifer;
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

        public int CreateFleetKey()
        {
            int key = 1;
            while (EmpireAI.UsedFleets.Contains(key))
                ++key;
            EmpireAI.UsedFleets.Add(key);
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
            Pool.ClearForcePools();
            KnownShips.Clear();
            SensorNodes.Clear();
            BorderNodes.Clear();
            TechnologyDict.Clear();
            SpaceRoadsList.Clear();
            foreach (var kv in FleetsDict)
                kv.Value.Reset();
            FleetsDict.Clear();
            UnlockedBuildingsDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedTroopDict.Clear();
            UnlockedTroops.Clear();
            Inhibitors.Clear();
            OwnedProjectors.Clear();
            ShipsWeCanBuild.Clear();
            structuresWeCanBuild.Clear();
            data.MoleList.Clear();
            data.OwnedArtifacts.Clear();
            data.AgentList.Clear();
            Research.Reset();
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
                BreakAllTreatiesWith(kv.Key, includingPeace: true);
                var them = kv.Key;
                GetRelations(them).AtWar = false;
                them.GetRelations(this).AtWar = false;
            }

            foreach (Ship ship in OwnedShips)
            {
                ship.AI.ClearOrders();
            }
            EmpireAI.Goals.Clear();
            EmpireAI.EndAllTasks();
            foreach (var kv in FleetsDict)
                kv.Value.Reset();

            Empire rebels = EmpireManager.CreateRebelsFromEmpireData(data, this);
            StatTracker.UpdateEmpire(Universe.StarDate, rebels);

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
                BreakAllTreatiesWith(kv.Key, includingPeace: true);

            foreach (Ship ship in OwnedShips)
            {
                ship.AI.ClearOrders();
            }

            EmpireAI.Goals.Clear();
            EmpireAI.EndAllTasks();
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

        public Map<string, TechEntry>.ValueCollection TechEntries => TechnologyDict.Values;

        // @note this is used for comparing rival empire tech entries
        public TechEntry GetTechEntry(TechEntry theirTech) => GetTechEntry(theirTech.UID);
        public TechEntry GetTechEntry(Technology theirTech) => GetTechEntry(theirTech.UID);

        public TechEntry GetTechEntry(string uid)
        {
            if (TechnologyDict.TryGetValue(uid, out TechEntry techEntry))
                return techEntry;
            Log.Error($"Empire GetTechEntry: Failed To Find Tech: ({uid})");
            return TechEntry.None;
        }

        public TechEntry GetNextDiscoveredTech(string uid) => GetTechEntry(uid).FindNextDiscoveredTech(this);

        public bool TryGetTechEntry(string uid, out TechEntry techEntry)
        {
            return TechnologyDict.TryGetValue(uid, out techEntry);
        }

        public Array<TechEntry> TechsAvailableForTrade(Empire them)
        {
            var tradeTechs = new Array<TechEntry>();
            foreach (var kv in TechnologyDict)
            {
                TechEntry tech = kv.Value;
                if (tech.Unlocked)
                    tradeTechs.Add(tech);
            }
            return tradeTechs;
        }

        public Array<TechEntry> CurrentTechsResearchable()
        {
            var availableTechs = new Array<TechEntry>();

            foreach (TechEntry tech in TechEntries)
            {
                if (!tech.Unlocked 
                    && tech.Discovered 
                    && (tech.shipDesignsCanuseThis || tech.Tech.BonusUnlocked.NotEmpty)
                    && HavePreReq(tech.UID))
                {
                    availableTechs.Add(tech);
                    tech.SetLookAhead(this);
                }
            }
            return availableTechs;
        }

        public bool HasUnlocked(string uid)       => GetTechEntry(uid).Unlocked;
        public bool HasUnlocked(TechEntry tech)   => GetTechEntry(tech).Unlocked;
        public bool HasDiscovered(string techId)  => GetTechEntry(techId).Discovered;
        public float TechProgress(TechEntry tech) => GetTechEntry(tech).Progress;
        public float TechCost(TechEntry tech)     => GetTechEntry(tech).TechCost;
        public float TechCost(string techId)      => GetTechEntry(techId).TechCost;
        public Array<string> AcquiredFrom(TechEntry tech) => GetTechEntry(tech).WasAcquiredFrom;
        public Array<string> AcquiredFrom(string techId)  => GetTechEntry(techId).WasAcquiredFrom;

        int TechCost(IEnumerable<string> techIds)
        {
            float costAccumulator = 0;
            foreach (string tech in techIds)
            {
                costAccumulator += TechCost(tech);
            }
            return (int)costAccumulator;
        }

        public int TechCost(Ship ship)       => TechCost(ship.shipData.TechsNeeded.Except(ShipTechs));
        public bool HasTechEntry(string uid) => TechnologyDict.ContainsKey(uid);

        public IReadOnlyList<SolarSystem> GetOwnedSystems() => OwnedSolarSystems;
        public IReadOnlyList<Planet> GetPlanets()           => OwnedPlanets;
        public int NumPlanets                               => OwnedPlanets.Count;

        public Array<SolarSystem> GetBorderSystems(Empire them, bool hideUnexplored)
        {
            var solarSystems = new Array<SolarSystem>();
            Vector2 theirCenter = them.GetWeightedCenter();
            float maxDistance = theirCenter.Distance(GetWeightedCenter()) - 300000;
            
            foreach (var solarSystem in GetOwnedSystems())
            {
                if (hideUnexplored && !solarSystem.IsExploredBy(them)) continue;

                if (maxDistance < solarSystem.Position.Distance(theirCenter))
                    solarSystems.AddUniqueRef(solarSystem);
            }
            return solarSystems;
        }

        public void RemovePlanet(Planet planet, Empire attacker)
        {
            GetRelations(attacker).LostAColony(planet, attacker);
            RemovePlanet(planet);
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

        public void AddPlanet(Planet planet, Empire loser)
        {
            GetRelations(loser).WonAColony(planet, loser);
            AddPlanet(planet);
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

        public BatchRemovalCollection<Ship> GetShips() => OwnedShips;

        public Ship[] AllFleetReadyShips()
        {
            //Get all available ships from AO's
            var ships = Pool.GetShipsFromOffensePools();

            var readyShips = new Array<Ship>();
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship.fleet != null)
                    continue;
                if (ship.AI.State == AIState.Resupply
                    && ship.AI.State == AIState.Refit
                    && ship.AI.State == AIState.Scrap
                    && ship.AI.State == AIState.Scuttle)
                    continue;
                readyShips.Add(ship);
            }

            return readyShips.ToArray();
        }

        public FleetShips AllFleetsReady(Vector2 targetPosition)
        {
            var ships = AllFleetReadyShips();
            ships.Sort(s => s.Center.SqDist(targetPosition));
            //return a fleet creator. 
            return new FleetShips(this, ships);
        }

        public FleetShips AllFleetsReady()
        {
            var ships = AllFleetReadyShips();
            //return a fleet creator. 
            return new FleetShips(this, ships);
        }

        public BatchRemovalCollection<Ship> GetProjectors() => OwnedProjectors;

        public void AddShip(Ship s)
        {
            if (s.IsSubspaceProjector)
                OwnedProjectors.AddUniqueRef(s);
            else
                OwnedShips.AddUniqueRef(s);
        }

        void InitDifficultyModifiers()
        {
            DifficultyModifiers = new DifficultyModifiers(this, CurrentGame.Difficulty);
        }

        public void TestInitDifficultyModifiers() // For UnitTests only
        {
            InitDifficultyModifiers();
        }

        public void Initialize()
        {
            for (int i = 1; i < 10; ++i)
            {
                Fleet fleet = new Fleet {Owner = this};
                fleet.SetNameByFleetIndex(i);
                FleetsDict.Add(i, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            InitTechs();

            foreach (var hull in ResourceManager.Hulls)       UnlockedHullsDict[hull.Hull]  = false;
            foreach (var tt in ResourceManager.TroopTypes)    UnlockedTroopDict[tt]         = false;
            foreach (var kv in ResourceManager.BuildingsDict) UnlockedBuildingsDict[kv.Key] = false;
            foreach (var kv in ResourceManager.ShipModules)   UnlockedModulesDict[kv.Key]   = false;
            //unlock from empire data file
            foreach (string building in data.unlockBuilding)  UnlockedBuildingsDict[building]  = true;
            UnlockedTroops.Clear();

            //unlock racial techs
            foreach (var kv in TechnologyDict)
            {
                var techEntry = kv.Value;
                data.Traits.TechUnlocks(techEntry, this);
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

            data.TechDelayTime = 0;

            if (EmpireManager.NumEmpires ==0)
                UpdateTimer = 0;

            InitDifficultyModifiers();
            CreateThrusterColors();
            EmpireAI = new EmpireAI(this, fromSave: false);
            Research.Update();
        }

        private void CreateThrusterColors()
        {
            ThrustColor0 = new Color(data.ThrustColor0R, data.ThrustColor0G, data.ThrustColor0B);
            ThrustColor1 = new Color(data.ThrustColor1R, data.ThrustColor1G, data.ThrustColor1B);
            if (ThrustColor0 == Color.Black)
                ThrustColor0 = Color.LightBlue;

            if (ThrustColor1 == Color.Black)
                ThrustColor1 = Color.OrangeRed;
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
                entry.Value.UnlockFromSave(this, unlockBonuses);
            }
        }

        private void InitTechs()
        {
            foreach (var kv in ResourceManager.TechTree)
            {
                var techEntry = new TechEntry(kv.Key);

                if (techEntry.IsHidden(this))
                    techEntry.SetDiscovered(false);
                else
                {
                    bool secret = kv.Value.Secret || (kv.Value.ComesFrom.Count == 0 && kv.Value.RootNode == 0);
                    if (kv.Value.RootNode == 1 && !secret)
                        techEntry.ForceFullyResearched();
                    else
                        techEntry.ForceNeedsFullResearch();
                    techEntry.SetDiscovered(!secret);
                }

                if (isFaction || data.Traits.Prewarp == 1)
                    techEntry.ForceNeedsFullResearch();
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
            EmpireAI = new EmpireAI(this, fromSave: true);
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
            InitDifficultyModifiers();
            CreateThrusterColors();
            UpdateShipsWeCanBuild();
            Research.Update();
        }

        bool WeCanUseThisLater(TechEntry tech)
        {
            foreach (Technology.LeadsToTech leadsToTech in tech.Tech.LeadsTo)
            {
                TechEntry entry = TechnologyDict[leadsToTech.UID];
                if (entry.shipDesignsCanuseThis || WeCanUseThisLater(entry))
                    return true;
            }
            return false;
        }

        public string[] GetTroopsWeCanBuild() => UnlockedTroopDict.Where(kv => kv.Value)
                                                                  .Select(kv => kv.Key).ToArray();

        public bool WeCanBuildTroop(string id) => UnlockedTroopDict.TryGetValue(id, out bool canBuild) && canBuild;

        public void UnlockEmpireShipModule(string moduleUID, string techUID = "")
        {
            UnlockedModulesDict[moduleUID] = true;
            PopulateShipTechLists(moduleUID, techUID);
        }

        void PopulateShipTechLists(string moduleUID, string techUID, bool addToMainShipTechs = true)
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
            if (tech != TechEntry.None)
                tech.SetDiscovered(this);
        }

        public void IncreaseEmpireShipRoleLevel(ShipData.RoleName role, int bonus)
        {
            foreach (Ship ship in OwnedShips)
            {
                if (ship.shipData.Role == role)
                    ship.AddToShipLevel(bonus);
            }
        }

        public void UnlockTech(string techId, TechUnlockType techUnlockType)
            => UnlockTech(techId, techUnlockType, null);

        public void UnlockTech(string techId, TechUnlockType techUnlockType, Empire otherEmpire)
            => UnlockTech(GetTechEntry(techId), techUnlockType, otherEmpire);

        public void UnlockTech(TechEntry techEntry, TechUnlockType techUnlockType)
            => UnlockTech(techEntry, techUnlockType, null);

        public void UnlockTech(TechEntry techEntry, TechUnlockType techUnlockType, Empire otherEmpire)
        {
            switch (techUnlockType)
            {
                case TechUnlockType.Normal    when techEntry.Unlock(this):
                case TechUnlockType.Event     when techEntry.Unlock(this):
                case TechUnlockType.Diplomacy when techEntry.UnlockFromDiplomacy(this, otherEmpire):
                case TechUnlockType.Spy       when techEntry.UnlockFromSpy(this, otherEmpire):
                case TechUnlockType.Scrap     when techEntry.UnlockFromScrap(this, otherEmpire):
                    UpdateForNewTech(); break;
            }
        }

        public void UpdateForNewTech()
        {
            UpdateShipsWeCanBuild();
            EmpireAI.TriggerRefit();
            TriggerFreightersRefit();
        }

        public void AssimilateTech(Empire conqueredEmpire)
        {
            foreach (TechEntry conquered in conqueredEmpire.TechEntries)
            {
                TechEntry ourTech = GetTechEntry(conquered);
                ourTech.UnlockByConquest(this, conqueredEmpire, conquered);
            }
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target, TechUnlockType techUnlockType)
        {
            UnlockTech(techID, techUnlockType, target);
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

        void UpdateKnownShips()
        {
            KnownShips.Clear();
            InfluenceNode[] influenceNodes = SensorNodes.ToArray();

            bool showAll = isPlayer && Universe.Debug;

            for (int i = 0; i < Universe.MasterShipList.Count; ++i)
            {
                Ship nearby = Universe.MasterShipList[i];
                if (nearby.Active && UpdateShipInfluence(nearby, influenceNodes, showAll))
                    KnownShips.Add(nearby);
            }
        }

        bool UpdateShipInfluence(Ship nearby, InfluenceNode[] influenceNodes, bool showAll)
        {
            if (nearby.loyalty != this) // update from another empire
            {
                bool inSensorRadius = false;
                bool border = false;

                for (int i = 0; i < influenceNodes.Length; i++)
                {
                    InfluenceNode node = influenceNodes[i];
                    // showAll only has an effect in debug. so it wont save cycles putting it first. 
                    if (nearby.Center.InRadius(node.Position, node.Radius) || showAll)
                    {
                        if (TryGetRelations(nearby.loyalty, out Relationship loyalty) && !loyalty.Known)
                            DoFirstContact(nearby.loyalty);

                        inSensorRadius = true;
                        if (node.SourceObject is Ship shipKey && (shipKey.inborders || shipKey.IsSubspaceProjector) ||
                            node.SourceObject is SolarSystem || node.SourceObject is Planet)
                        {
                            border = true;
                        }
                        nearby.inSensorRange |= isPlayer;
                        break;
                    }
                }

                nearby.SetProjectorInfluence(this, border);
                EmpireAI.ThreatMatrix.UpdatePin(nearby, border, inSensorRadius);
                return inSensorRadius;
            }

            // update our own empire ships
            EmpireAI.ThreatMatrix.ClearPinsInSensorRange(nearby.Center, nearby.SensorRange);
            nearby.inSensorRange |= isPlayer;
            nearby.inborders = false;

            for (int i = 0; i < BorderNodes.Count; ++i)
            {
                InfluenceNode node = BorderNodes[i];
                if (node.Position.InRadius(nearby.Center, node.Radius))
                {
                    nearby.inborders = true;
                    break;
                }
            }

            if (!nearby.inborders)
            {
                foreach (KeyValuePair<Empire, Relationship> relationship in Relationships)
                {
                    if (relationship.Value.Treaty_Alliance)
                    {
                        Empire e = relationship.Key;
                        for (int i = 0; i < e.BorderNodes.Count; ++i)
                        {
                            InfluenceNode node = e.BorderNodes[i];
                            if (node.Position.InRadius(nearby.Center, node.Radius))
                            {
                                nearby.inborders = true;
                                break;
                            }
                        }
                    }
                }
            }

            nearby.SetProjectorInfluence(this, nearby.inborders);
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
            if (!TryGetRelations(empire, out _))
                Relationships.Add(empire, new Relationship(empire.data.Traits.Name));
        }

        public void SetRelationsAsKnown(Empire empire)
        {
            AddRelation(empire);
            Relationships[empire].Known = true;
            if (!empire.GetRelations(this).Known)
                empire.SetRelationsAsKnown(this);
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

        public War GetOldestWar()
        {
            War olderWar = null;
            foreach(var kv in AllRelations)
            {
                if ( kv.Value.AtWar)
                {
                    var war = kv.Value.ActiveWar;
                    if (olderWar == null || olderWar.StartDate > war.StartDate)
                        olderWar = war;
                }
            }
            return olderWar;
        }

        void DoFirstContact(Empire e)
        {
            Relationships[e].SetInitialStrength(e.data.Traits.DiplomacyMod * 100f);
            Relationships[e].Known = true;
            if (!e.GetRelations(this).Known)
                e.DoFirstContact(this);

            if (Universe.Debug)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Universe.player == this)
                return;

            if (Universe.PlayerEmpire == this)
            {
                if (e.isFaction)
                    DoFactionFirstContact(e);
                else
                    DiplomacyScreen.Show(e, "First Contact");
            }
        }

        void DoFactionFirstContact(Empire e)
        {
            var factionContacts = ResourceManager.Encounters.Filter(enc => enc.Faction == e.data.Traits.Name);
            if (factionContacts.Length == 0)
                return; // no dialogs for this faction, no use to look for first contact

            var firstContacts = factionContacts.Filter(enc => enc.FirstContact);
            if (firstContacts.Length > 0)
            {
                Encounter encounter = firstContacts.First();
                EncounterPopup.Show(Universe, Universe.PlayerEmpire, e, encounter);
            }
            else
            {
                Log.Warning($"Could not find First Contact Encounter for {e.Name}, " +
                            "make sure this faction has <FirstContact>true</FirstContact> in one of it's encounter dialog XMLs");
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
            UpdateMilitaryStrengths();

            if (UpdateTimer <= 0f && !data.Defeated)
            {
                if (this == Universe.PlayerEmpire)
                {
                    Universe.StarDate += 0.1f;
                    Universe.StarDate = (float)Math.Round(Universe.StarDate, 1);

                    StatTracker.StatUpdateStarDate(Universe.StarDate);
                    if (Universe.StarDate.AlmostEqual(1000.09f))
                    {
                        foreach (Empire empire in EmpireManager.Empires)
                        {
                            using (empire.OwnedPlanets.AcquireReadLock())
                            {
                                foreach (Planet planet in empire.OwnedPlanets)
                                    StatTracker.StatAddPlanetNode(Universe.StarDate, planet);
                            }
                        }
                    }
                    if (!InitializedHostilesDict)
                    {
                        InitializedHostilesDict = true;
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
                UpdateEmpirePlanets();
                UpdateAI(); // Must be done before DoMoney
                GovernPlanets(); // this does the governing after getting the budgets from UpdateAI when loading a game
                DoMoney();
                TakeTurn();
            }
            SetRallyPoints();
            UpdateFleets(elapsedTime);
            OwnedShips.ApplyPendingRemovals();
            OwnedProjectors.ApplyPendingRemovals();  //fbedard
        }

        /// <summary>
        /// This should be run on save load to set economic values without taking a turn.
        /// </summary>
        public void InitEmpireEconomy()
        {
            UpdateEmpirePlanets();
            UpdateNetPlanetIncomes();
            UpdateContactsAndBorders(1f);
            CalculateScore();
            UpdateRelationships();
            UpdateShipMaintenance(); ;
            EmpireAI.RunEconomicPlanner();
        }

        private void UpdateMilitaryStrengths()
        {
            CurrentMilitaryStrength = 0;
            CurrentTroopStrength    = 0;

            for (int i = 0; i < OwnedShips.Count; ++i)
            {
                Ship ship = OwnedShips[i];
                if (ship != null)
                {
                    CurrentTroopStrength += ship.Carrier.MaxTroopStrengthInShipToCommit;
                    CurrentMilitaryStrength += ship.GetStrength();
                }
            }

            for (int x = 0; x < OwnedPlanets.Count; x++)
            {
                var planet = OwnedPlanets[x];
                CurrentTroopStrength += planet.TroopManager.OwnerTroopStrength;
            }
        }

        //Using memory to save CPU time. the question is how often is the value used and
        //How often would it be calculated.
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

        private void UpdateDefenseShipBuildingOffense()
        {
            for (int i = 0 ; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                p.UpdateDefenseShipBuildingOffense();
            }
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
            debug.AddLine($"Fast or Big Ratio: {FastVsBigFreighterRatio}");
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
                string incoming = p.NumIncomingFreighters.ToString();
                string outgoing = p.NumOutgoingFreighters.ToString();
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
                Fleet fleet = kv.Value;
                fleet.Update(elapsedTime);
                if (FleetUpdateTimer <= 0f)
                    fleet.UpdateAI(elapsedTime, kv.Key);
            }
            if (FleetUpdateTimer < 0.0)
                FleetUpdateTimer = 5f;
        }

        void DoMoney()
        {
            MoneyLastTurn = Money;
            ++TurnCount;

            UpdateTradeIncome();
            ResetMoneySpentOnProduction();
            UpdateNetPlanetIncomes();
            UpdateShipMaintenance();
            Money += NetIncome;
        }

        void ResetMoneySpentOnProduction()
        {
            MoneySpendOnProductionThisTurn = 0; // reset for next turn
        }

        public void UpdateNetPlanetIncomes()
        {
            NetPlanetIncomes              = 0;
            GrossPlanetIncome             = 0;
            ExcessGoodsMoneyAddedThisTurn = 0;
            PotentialIncome               = 0;
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                {
                    planet.UpdateIncomes(false);
                    NetPlanetIncomes              += planet.Money.NetRevenue;
                    GrossPlanetIncome             += planet.Money.GrossRevenue;
                    PotentialIncome               += planet.Money.PotentialRevenue;
                    ExcessGoodsMoneyAddedThisTurn += planet.ExcessGoodsIncome;
                }
        }

        public void UpdateEmpirePlanets()
        {
            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                    planet.UpdateOwnedPlanet();
        }

        public void GovernPlanets()
        {
            if (!isFaction && !data.Defeated)
                UpdateMaxColonyValue();

            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                    planet.DoGoverning();
        }

        private void UpdateShipMaintenance()
        {
            TotalShipMaintenance          = 0.0f;
            TotalWarShipMaintenance       = 0f;
            TotalCivShipMaintenance       = 0f;
            TotalOrbitalMaintenance       = 0;
            TotalEmpireSupportMaintenance = 0;
            TotalMaintenanceInScrap       = 0f;
            TotalTroopShipMaintenance     = 0;
            using (OwnedShips.AcquireReadLock())
                foreach (Ship ship in OwnedShips)
                {
                    float maintenance = ship.GetMaintCost();
                    if (!ship.Active || ship.AI.State >= AIState.Scrap)
                    {
                        TotalMaintenanceInScrap += maintenance;
                        continue;
                    }
                    if (data.DefenseBudget > 0 && ((ship.shipData.HullRole == ShipData.RoleName.platform && ship.IsTethered)
                                                   || (ship.shipData.HullRole == ShipData.RoleName.station &&
                                                       (ship.shipData.IsOrbitalDefense || !ship.shipData.IsShipyard))))
                    {
                        data.DefenseBudget -= maintenance;
                    }
                    switch (ship.DesignRoleType) {
                        case ShipData.RoleType.WarSupport:
                        case ShipData.RoleType.Warship:
                            TotalWarShipMaintenance += maintenance;
                            break;
                        case ShipData.RoleType.Civilian:
                            TotalCivShipMaintenance += maintenance;
                            break;
                        case ShipData.RoleType.EmpireSupport:
                            TotalEmpireSupportMaintenance += maintenance;
                            break;
                        case ShipData.RoleType.Orbital:
                            TotalOrbitalMaintenance += maintenance;
                            break;
                        case ShipData.RoleType.Troop:
                            TotalTroopShipMaintenance += maintenance;
                            break;
                        default:
                            Log.Warning("what is it");
                            break;
                    }
                    TotalShipMaintenance   += maintenance; 
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
            return GrossIncome + plusNetIncome - AllSpending;
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

            foreach (KeyValuePair<string, Ship> kv in ResourceManager.ShipsDict)
            {
                var ship = kv.Value;
                if (hulls != null && !hulls.Contains(ship.shipData.Hull))
                    continue;

                if (ship.Deleted || ResourceManager.ShipRoles[ship.shipData.Role].Protected 
                                 || ShipsWeCanBuild.Contains(ship.Name))
                    continue;
                if (!isPlayer && !ship.ShipGoodToBuild(this))
                    continue;
                if (!WeCanBuildThis(ship.Name))
                    continue;
                try
                {
                    if (ship.shipData.Role <= ShipData.RoleName.station)
                    {
                        structuresWeCanBuild.Add(ship.Name);
                    }

                    {
                        ShipsWeCanBuild.Add(ship.Name);
                    }
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

            UpdateBestOrbitals();
            UpdateDefenseShipBuildingOffense();

        }

        public bool WeCanBuildThis(string shipName)
        {
            if (!ResourceManager.GetShipTemplate(shipName, out Ship ship))
            {
                Log.Warning($"Ship does not exist: {shipName}");
                return false;
            }

            ShipData shipData = ship.shipData;
            if (shipData == null)
            {
                Log.Warning($"{data.PortraitName} : shipData is null : '{shipName}'");
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
                    if (!onlyShipTech.Unlocked)
                    {
                        //Log.Info($"Locked Tech : '{shipTech}' in design : '{shipName}'");
                        return false;
                    }
                }
                //Log.Info($"New Ship WeCanBuild {shipData.Name} Hull: '{shipData.Hull}' DesignRole: '{ship1.DesignRole}'");
            }
            else
            {
                // check if all modules in the ship are unlocked
                foreach (ModuleSlotData moduleSlotData in shipData.ModuleSlots)
                {
                    if (moduleSlotData.InstalledModuleUID.IsEmpty() ||
                        moduleSlotData.InstalledModuleUID == "Dummy" ||
                        UnlockedModulesDict[moduleSlotData.InstalledModuleUID])
                        continue;
                    //Log.Info($"Locked module : '{moduleSlotData.InstalledModuleUID}' in design : '{ship}'");
                    return false; // can't build this ship because it contains a locked Module
                }

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

        public bool GetTroopShipForRebase(out Ship troopShip, Planet planet)
        {
            troopShip = null;
            // Try free troop ships first if there is not one free, launch a troop from the nearest planet to space if possible
            return NearestFreeTroopShip(out troopShip, planet.Center) || LaunchNearestTroopForRebase(out troopShip, planet.Center, planet.Name);
        }

        public bool GetTroopShipForRebase(out Ship troopShip, Ship ship)
        {
            troopShip = null;
            // Try free troop ships first if there is not one free, launch a troop from the nearest planet to space if possible
            return NearestFreeTroopShip(out troopShip, ship.Center) || LaunchNearestTroopForRebase(out troopShip, ship.Center);
        }

        private bool NearestFreeTroopShip(out Ship troopShip, Vector2 objectCenter)
        {
            troopShip = null;
            Array<Ship> troopShips;
            using (OwnedShips.AcquireReadLock())
                troopShips = new Array<Ship>(OwnedShips
                    .Filter(troopship => troopship.Name == data.DefaultTroopShip
                                        && troopship.HasOurTroops
                                        && (troopship.AI.State == AIState.AwaitingOrders || troopship.AI.State == AIState.Orbit)
                                        && troopship.fleet == null && !troopship.InCombat)
                    .OrderBy(distance => Vector2.Distance(distance.Center, objectCenter)));

            if (troopShips.Count > 0)
                troopShip = troopShips.First();

            return troopShip != null;
        }

        private bool LaunchNearestTroopForRebase(out Ship troopShip, Vector2 objectCenter, string planetName = "")
        {
            troopShip = null;
            Array<Planet> candidatePlanets = new Array<Planet>(OwnedPlanets
                .Filter(p => p.NumTroopsCanLaunch > 0 && p.Name != planetName)
                .OrderBy(distance => Vector2.Distance(distance.Center, objectCenter)));

            if (candidatePlanets.Count == 0)
                return false;

            var troops = candidatePlanets.First().TroopsHere;
            using (troops.AcquireWriteLock())
            {
                troopShip = troops.First(t => t.Loyalty == this).Launch();
                return troopShip != null;
            }
        }

        public int GetSpyDefense()
        {
            float defense = 0;
            for (int i = 0; i < data.AgentList.Count; i++)
            {
                if (data.AgentList[i].Mission == AgentMission.Defending)
                    defense += data.AgentList[i].Level;
            }

            defense *= ResourceManager.AgentMissionData.DefenseLevelBonus;
            defense /= (OwnedPlanets.Count / 3).LowerBound(1);
            defense += data.SpyModifier;
            defense += data.DefensiveSpyBonus;

            return (int)defense;
        }

        /// <summary>
        /// Gets the total population in billions
        /// </summary>
        public float GetTotalPop(out float maxPop)
        {
            float num = 0f;
            maxPop    = 0f;
            using (OwnedPlanets.AcquireReadLock())
                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    num    += OwnedPlanets[i].PopulationBillion;
                    maxPop += OwnedPlanets[i].MaxPopulationBillion;
                }

            using (OwnedShips.AcquireReadLock())
                for (int i = 0; i < OwnedShips.Count; i++)
                {
                    num += OwnedShips[i].GetCargo(Goods.Colonists) / 1000;
                }

            return num;
        }

        /// <summary>
        /// Gets the total potential population in billions (with biospheres/Terraformers if researched)
        /// </summary>
        public float GetTotalPopPotential()
        {
            float num = 0.0f;
            using (OwnedPlanets.AcquireReadLock())
                for (int i = 0; i < OwnedPlanets.Count; i++)
                    num += OwnedPlanets[i].PotentialMaxPopBillionsFor(this);

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
            float fertility = p.FertilityFor(this);
            float richness = p.MineralRichness;
            float pop = p.MaxPopulationBillionFor(this);
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
            float militaryPotential = 0.0f;
            float maxPopBillion     = p.MaxPopulationBillionFor(this);
            float fertility         = p.FertilityFor(this);

            if (p.MineralRichness > 0.5f)
                mineralWealth += p.MineralRichness + maxPopBillion;
            else
                mineralWealth += p.MineralRichness;

            if (maxPopBillion > 1)
            {
                researchPotential += maxPopBillion;
                if (IsCybernetic)
                {
                    if (p.MineralRichness > 1)
                        popSupport += maxPopBillion + p.MineralRichness;
                }
                else
                {
                    if (fertility > 1f)
                    {
                        if (p.MineralRichness > 1)
                            popSupport += maxPopBillion + fertility + p.MineralRichness;
                        fertility += fertility + maxPopBillion;
                    }
                }
            }
            else
            {
                militaryPotential += fertility + p.MineralRichness + maxPopBillion;
                if (maxPopBillion >= 0.5)
                {
                    if (ResourceManager.TryGetTech(Research.Topic, out Technology tech))
                        researchPotential = (tech.ActualCost - Research.NetResearch) / tech.ActualCost
                                            * (fertility * 2 + p.MineralRichness + (maxPopBillion / 0.5f));
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
                    switch (planet.colonyType)
                    {
                        case Planet.ColonyType.Agricultural: ++agriculturalCount; break;
                        case Planet.ColonyType.Core:         ++coreCount;         break;
                        case Planet.ColonyType.Industrial:   ++industrialCount;   break;
                        case Planet.ColonyType.Research:     ++researchCount;     break;
                        case Planet.ColonyType.Military:     ++militaryCount;     break;
                    }
                }
            }

            float assignedFactor = (coreCount + industrialCount + agriculturalCount + militaryCount + researchCount)
                                   / (OwnedPlanets.Count + 0.01f);

            float coreDesire        = popSupport        + (assignedFactor - coreCount) ;
            float industrialDesire  = mineralWealth     + (assignedFactor - industrialCount);
            float agricultureDesire = fertility         + (assignedFactor - agriculturalCount);
            float militaryDesire    = militaryPotential + (assignedFactor - militaryCount);
            float researchDesire    = researchPotential + (assignedFactor - researchCount);

            (Planet.ColonyType, float)[] desires =
            {
                (Planet.ColonyType.Core,         coreDesire),
                (Planet.ColonyType.Industrial,   industrialDesire),
                (Planet.ColonyType.Agricultural, agricultureDesire),
                (Planet.ColonyType.Military,     militaryDesire),
                (Planet.ColonyType.Research,     researchDesire),
            };

            // get the type with maximum desire
            Planet.ColonyType maxDesireType = desires.FindMax(typeAndDesire => typeAndDesire.Item2).Item1;
            return maxDesireType;
        }

        void ResetBorders()
        {
            BorderNodes.ClearAndRecycle();
            SensorNodes.ClearAndRecycle();

            bool wellKnown = EmpireManager.Player == this || EmpireManager.Player.TryGetRelations(this, out Relationship rel)
                             && rel.Treaty_Alliance;
            bool known     = wellKnown || EmpireManager.Player.TryGetRelations(this, out Relationship relKnown)
                             && (relKnown.Treaty_Trade || relKnown.Treaty_OpenBorders);
            var allies     = EmpireManager.GetAllies(this);

            SetBordersKnownByAllies(allies, wellKnown);
            SetBordersByPlanet(known);

            // Moles are spies who have successfully been planted during 'Infiltrate' type missions, I believe - Doctor
            foreach (Mole mole in data.MoleList)
                SensorNodes.Add(new InfluenceNode
                {
                    Position = Universe.GetPlanet(mole.PlanetGuid).Center,
                    Radius   = GetProjectorRadius(),
                    Known    = true
                });

            Inhibitors.Clear();
            for (int i = 0; i < OwnedShips.Count; i++)
            {
                Ship ship = OwnedShips[i];
                if (ship.InhibitionRadius > 0.0f)
                    Inhibitors.Add(ship);

                InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode.Position      = ship.Center;
                influenceNode.Radius        = ship.SensorRange;
                influenceNode.SourceObject  = ship;
                SensorNodes.Add(influenceNode);
            }

            for (int i = 0; i < OwnedProjectors.Count; i++)
            {
                Ship ship = OwnedProjectors[i];
                if (ship.InhibitionRadius > 0f)
                    Inhibitors.Add(ship);

                InfluenceNode influenceNodeS = SensorNodes.RecycleObject() ?? new InfluenceNode();
                InfluenceNode influenceNodeB = BorderNodes.RecycleObject() ?? new InfluenceNode();

                influenceNodeS.Position     = ship.Center;
                influenceNodeS.Radius       = GetProjectorRadius(); //projectors used as sensors again
                influenceNodeS.SourceObject = ship;

                influenceNodeB.Position     = ship.Center;
                influenceNodeB.Radius       = GetProjectorRadius(); //projectors used as sensors again
                influenceNodeB.SourceObject = ship;
                bool seen                   = known || EmpireManager.Player.GetEmpireAI().ThreatMatrix.ContainsGuid(ship.guid);
                influenceNodeB.Known        = seen;
                influenceNodeS.Known        = seen;
                SensorNodes.Add(influenceNodeS);
                BorderNodes.Add(influenceNodeB);
            }

            SetPirateBorders();
            BorderNodes.ClearPendingRemovals();
            SensorNodes.ClearPendingRemovals();
            using (BorderNodes.AcquireReadLock())
                for (int i = 0; i < BorderNodes.Count; i++)
                {
                    InfluenceNode item5 = BorderNodes[i];
                    foreach (InfluenceNode item6 in BorderNodes)
                    {
                        if (item6.SourceObject == item5.SourceObject && item6.Radius < item5.Radius)
                            BorderNodes.QueuePendingRemoval(item6);
                    }
                }

            BorderNodes.ApplyPendingRemovals();
        }

        private void SetPirateBorders()
        {
            if (!WeArePirates || !Pirates.GetBases(out Array<Ship> bases))
                return;

            for (int i = 0; i < bases.Count; i++)
            {
                Ship pirateBase             = bases[i];
                InfluenceNode influenceNode = BorderNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode.Position      = pirateBase.Center;
                influenceNode.Radius        = 60000;
                influenceNode.SourceObject  = pirateBase;
                influenceNode.Known         = EmpireManager.Player.GetEmpireAI().ThreatMatrix.ContainsGuid(pirateBase.guid);
                BorderNodes.Add(influenceNode);
            }
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
                    for (int i = 0; i < planet.BuildingList.Count; i++)
                    {
                        Building t = planet.BuildingList[i];
                        if (influenceNode1.Radius < t.ProjectorRange)
                            influenceNode1.Radius = t.ProjectorRange;
                    }
                }
                else
                    influenceNode1.Radius = isFaction ? 20000f : GetProjectorRadius(planet);

                influenceNode1.Known = known;
                BorderNodes.Add(influenceNode1);

                InfluenceNode influenceNode3 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                influenceNode3.SourceObject  = planet;
                influenceNode3.Position      = planet.Center;
                influenceNode3.Radius        = isFaction ? 1f : data.SensorModifier;
                influenceNode3.Known         = known;
                for (int i = 0; i < planet.BuildingList.Count; i++)
                {
                    Building t = planet.BuildingList[i];
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
                    Planet planet                = array[y];
                    InfluenceNode influenceNode2 = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode2.Position      = planet.Center;
                    influenceNode2.Radius        = isFaction
                        ? 1f
                        : this                   == Universe.PlayerEmpire
                            ? GetProjectorRadius() / 5f * empire.data.SensorModifier
                            : GetProjectorRadius() / 3f * empire.data.SensorModifier;
                    foreach (Building building in planet.BuildingList)
                        influenceNode2.Radius    =
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
                    Ship ship                   = projectors[z];
                    //loop over all ALLIED projectors
                    InfluenceNode influenceNode = SensorNodes.RecycleObject() ?? new InfluenceNode();
                    influenceNode.Position      = ship.Center;
                    influenceNode.Radius        = GetProjectorRadius(); //projectors currently use their projection radius as sensors
                    SensorNodes.Add(influenceNode);
                    influenceNode.SourceObject  = ship;
                    influenceNode.Known         = wellKnown;
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
                data.AgentList[index].Update(this);

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

            if (!data.IsRebelFaction)
            {
                if (StatTracker.GetSnapshot(Universe.StarDate, this, out Snapshot snapshot))
                {
                    snapshot.ShipCount = OwnedShips.Count;
                    snapshot.MilitaryStrength = CurrentMilitaryStrength;
                    snapshot.TaxRate = data.TaxRate;
                }
            }

            if (isPlayer)
            {
                if (Universe.StarDate > 1005.0f)
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
                            if (empire.IsAggressive || empire.IsRuthless || empire.IsXenophobic)
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
                if (data.TurnsBelowZero > 0 && Money < 0.0)
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
                        Universe.ScreenManager.AddScreenDeferred(new YouWinScreen(Universe));
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                if (StatTracker.GetSnapshot(Universe.StarDate, this, out Snapshot snapshot))
                    snapshot.Population = OwnedPlanets.Sum(p => p.Population);
            }

            if (isPlayer)
            {
                foreach (Planet planet in OwnedPlanets)
                {
                    if (planet.HasWinBuilding)
                    {
                        Universe.ScreenManager.AddScreenDeferred(new YouWinScreen(Universe, Localizer.Token(5085)));
                        return;
                    }
                }
            }

            Research.Update();

            if (data.TurnsBelowZero > 0 && Money < 0.0 && (!Universe.Debug || !isPlayer))
                Bankruptcy();

            CalculateScore();
            UpdateRelationships();

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

            if (!isFaction)
            {
                CalcAverageFreighterCargoCap();
                DispatchBuildAndScrapFreighters();
                AssignExplorationTasks();
            }
        }

        void UpdateAI() => EmpireAI.Update();

        void Bankruptcy()
        {
            if (data.TurnsBelowZero >= RandomMath.RollDie(8))
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

                        for (int index = 0; index < planet.PopulationBillion * 2; ++index)
                        {
                            Troop troop = EmpireManager.CreateRebelTroop(rebels);

                            var chance = (planet.TileArea - planet.GetFreeTiles(this)) / planet.TileArea;

                            if (planet.TroopsHere.NotEmpty && RandomMath.Roll3DiceAvg(chance * 50))
                            {
                                var t = RandomMath.RandItem(planet.TroopsHere);
                                if (t != null)
                                    troop.ChangeLoyalty(rebels);
                            }

                            if (planet.BuildingList.NotEmpty && RandomMath.Roll3DiceAvg(chance * 50))
                            {
                                var building = RandomMath.RandItem(planet.BuildingList
                                                                   .Filter(b=> !b.IsBiospheres));
                                if (building != null)
                                    planet.ScrapBuilding(building);
                            }

                            troop.TryLandTroop(planet);
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

        public void InitRebellion(Empire origin)
        {
            data.IsRebelFaction  = true;
            data.Traits.Name     = origin.data.RebelName;
            data.Traits.Singular = origin.data.RebelSing;
            data.Traits.Plural   = origin.data.RebelPlur;
            isFaction            = true;

            foreach (Empire e in EmpireManager.Empires)
            {
                e.AddRelation(this);
                AddRelation(e);
            }

            EmpireManager.Add(this);
            origin.data.RebellionLaunched = true;
        }

        bool IsEmpireDead()
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

            StarDriveGame.Instance?.EndingGame(true);
            foreach (Ship ship in Universe.MasterShipList)
                ship.Die(null, true);

            Universe.Paused = true;
            HelperFunctions.CollectMemory();
            StarDriveGame.Instance?.EndingGame(false);
            Universe.ScreenManager.AddScreenDeferred(new YouLoseScreen(Universe));
            Universe.Paused = false;
            return true;
        }

        public void UpdateRelationships()
        {
            int atWarCount = 0;
            foreach (var kv in Relationships)
                if (kv.Value.Known || isPlayer)
                {
                    kv.Value.UpdateRelationship(this, kv.Key);
                    if (kv.Value.AtWar && !kv.Key.isFaction) atWarCount++;
                }
            AtWarCount = atWarCount;
        }

        public void TryUnlockByScrap(Ship ship)
        {
            string hullName = ship.shipData.Hull;
            if (IsHullUnlocked(hullName) || ship.shipData.Role == ShipData.RoleName.prototype)
                return; // It's ours or we got it elsewhere


            if (!TryReverseEngineer(ship, out TechEntry hullTech, out Empire empire))
                return; // We could not reverse engineer this, too bad

            UnlockTech(hullTech, TechUnlockType.Scrap, empire);

            if (isPlayer)
            {
                string modelIcon  = ship.BaseHull.ActualIconPath;
                string hullString = ship.BaseHull.ToString();
                if (hullTech.Unlocked)
                {
                    string message = $"{hullString}{new LocalizedText(GameText.ReverseEngineered).Text}";
                    Universe.NotificationManager.AddScrapUnlockNotification(message, modelIcon, "ShipDesign");
                }
                else
                {
                    string message = $"{hullString}{new LocalizedText(GameText.HullScrappedAdvancingResearch).Text}";
                    Universe.NotificationManager.AddScrapProgressNotification(message, modelIcon, "ResearchScreen", hullTech.UID);
                }
            }
        }

        private bool TryReverseEngineer(Ship ship, out TechEntry hullTech, out Empire empire)
        {
            if (!TryGetTechFromHull(ship, out hullTech, out empire))
                return false;

            if (hullTech.Locked)
                return true; // automatically advance in research

            float unlockChance;
            switch (ship.shipData.HullRole)
            {
                case ShipData.RoleName.fighter:  unlockChance = 90;    break;
                case ShipData.RoleName.corvette: unlockChance = 80;    break;
                case ShipData.RoleName.frigate:  unlockChance = 60;    break;
                case ShipData.RoleName.cruiser:  unlockChance = 40;    break;
                case ShipData.RoleName.capital:  unlockChance = 20f;   break;
                default:                         unlockChance = 50f;   break;
            }

            unlockChance *= 1 + data.Traits.ModHpModifier; // skilled or bad engineers
            return RandomMath.RollDice(unlockChance);
        }

        bool TryGetTechFromHull(Ship ship, out TechEntry techEntry, out Empire empire)
        {
            techEntry        = null;
            empire           = null;
            string hullName  = ship.shipData.Hull;
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                techEntry = GetTechEntry(techName);
                foreach (var hull in techEntry.Tech.HullsUnlocked)
                {
                    if (hull.Name == hullName)
                    {
                        empire = EmpireManager.GetEmpireByShipType(hull.ShipType);
                        if (empire == null)
                        {
                            Log.Warning("Unlock by Scrap - tried to unlock rom an empire which does" +
                                        $"not exist in this game ({hull.ShipType}), probably " +
                                        "due to debug spawn ships or fleets.");

                            return false;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public void AddBoardSuccessNotification(Ship ship)
        {
            if (!isPlayer)
                return;

            string message = new LocalizedText(GameText.ShipCapturedByYou).Text;
            Universe.NotificationManager.AddBoardNotification(message, ship.BaseHull.ActualIconPath, "SnapToShip", ship, null);
        }

        public void AddBoardedNotification(Ship ship)
        {
            if (!isPlayer) 
                return;

            string message = $"{new LocalizedText(GameText.YourShipWasCaptured).Text} {ship.loyalty.Name}!";
            Universe.NotificationManager.AddBoardNotification(message, ship.BaseHull.ActualIconPath, "SnapToShip", ship, ship.loyalty);
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
                ExpansionScore += (float)(planet.FertilityFor(this) + (double)planet.MineralRichness + planet.PopulationBillion);
                foreach (Building building in planet.BuildingList)
                    IndustrialScore += building.ActualCost / 20f;
            }


            data.MilitaryScoreTotal += CurrentMilitaryStrength;
            TotalScore = (int)(MilitaryScore / 100.0 + IndustrialScore + TechScore + ExpansionScore);
            MilitaryScore = data.ScoreAverage == 0 ? 0f : data.MilitaryScoreTotal / data.ScoreAverage;
            ++data.ScoreAverage;
            if (data.ScoreAverage >= 120)  //fbedard: reset every 60 turns
            {
                data.MilitaryScoreTotal = MilitaryScore * 60f;
                data.ScoreAverage = 60;
            }
        }

        private void AbsorbAllEnvPreferences(Empire target)
        {
            data.EnvTerran  = Math.Max(data.EnvTerran, target.data.EnvTerran);
            data.EnvOceanic = Math.Max(data.EnvOceanic, target.data.EnvOceanic);
            data.EnvSteppe  = Math.Max(data.EnvSteppe, target.data.EnvSteppe);
            data.EnvTundra  = Math.Max(data.EnvTundra, target.data.EnvTundra);
            data.EnvSwamp   = Math.Max(data.EnvSwamp, target.data.EnvSwamp);
            data.EnvDesert  = Math.Max(data.EnvDesert, target.data.EnvDesert);
            data.EnvIce     = Math.Max(data.EnvIce, target.data.EnvIce);
            data.EnvBarren  = Math.Max(data.EnvBarren, target.data.EnvBarren);
        }

        public void AbsorbEmpire(Empire target)
        {
            AbsorbAllEnvPreferences(target);
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
            var ships = target.GetShips();
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                Ship ship = ships[i];
                ship.ChangeLoyalty(this);
            }

            var projectors = target.GetProjectors();
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship ship = projectors[i];
                ship.ChangeLoyalty(this);
            }

            target.GetShips().Clear();
            target.GetProjectors().Clear();
            AssimilateTech(target);
            foreach (TechEntry techEntry in target.TechEntries)
            {
                if (techEntry.Unlocked)
                    AcquireTech(techEntry.UID, target, TechUnlockType.Normal);
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
                EmpireAI.EndAllTasks();
                EmpireAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                EmpireAI.DefensiveCoordinator.DefenseDict.Clear();
                Pool.ClearForcePools();
                foreach (Ship s in OwnedShips)
                {
                    // added by gremlin Do not include 0 strength ships in defensive force pool
                    s.AI.ClearOrders();
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

        public bool HavePreReq(string techId) => GetTechEntry(techId).HasPreReq(this);

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

        public Vector2 GetCenter()
        {
            Vector2 center = Vector2.Zero;
            float radius = 0;
            if (OwnedPlanets.Count > 0)
            {
                int planets = 0;
                var avgPlanetCenter = new Vector2();
                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    Planet planet = OwnedPlanets[i];
                    ++planets;
                    avgPlanetCenter += planet.Center;
                }

                center = avgPlanetCenter / planets;
            }
            else
            {
                int items = 0;
                var avgEmpireCenter = new Vector2();
                for (int i = 0; i < OwnedShips.Count; i++)
                {
                    var planet = OwnedShips[i];
                    ++items;
                    avgEmpireCenter += planet.Center;
                }
                center =avgEmpireCenter / items.LowerBound(1);
            }
            return center;
        }

        public Vector2 GetWeightedCenter()
        {
            int planets = 0;
            var avgPlanetCenter = new Vector2();

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

        public void TheyKilledOurShip(Empire they, Ship killedShip)
        {
            if (KillsForRemnantStory(they, killedShip)) return;
            if (!TryGetRelations(they, out Relationship rel))
                return;
            rel.LostAShip(killedShip);
        }

        public void WeKilledTheirShip(Empire they, Ship killedShip)
        {
            if (!TryGetRelations(they, out Relationship rel))
                return;
            rel.KilledAShip(killedShip);
        }

        public bool KillsForRemnantStory(Empire they, Ship killedShip)
        {
            if (!isFaction || this != EmpireManager.Remnants) return false;
            if (!they.isPlayer) return false;
            if (GlobalStats.ActiveModInfo?.removeRemnantStory == true) return false;
            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(killedShip);
            GlobalStats.IncrementRemnantKills((int)killedExpSettings.KillExp, they);
            return true;
        }

        void AssignExplorationTasks()
        {
            if (isPlayer && !AutoExplore)
                return;

            int unexplored =0;
            bool haveUnexploredSystems = false;
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem solarSystem = UniverseScreen.SolarSystemList[i];
                if (solarSystem.IsExploredBy(this)) 
                    continue;

                if (++unexplored > 20) 
                    break;
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
            if (EmpireAI.HasGoal(GoalType.BuildScout))
                return;

            var desiredScouts = unexplored * Research.Strategy.ExpansionRatio * 0.75f;
            foreach (Ship ship in OwnedShips)
            {
                if (ship.DesignRole != ShipData.RoleName.scout || ship.fleet != null)
                    continue;
                ship.DoExplore();
                if (++numScouts >= desiredScouts)
                    return;
            }

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
                planet.AddMaxBaseFertility(amount);
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
            if (ship == null)
            {
                Log.Error($"Empire '{Name}' RemoveShip failed: ship was null");
                return;
            }

            if (ship.IsSubspaceProjector)
                OwnedProjectors.RemoveRef(ship);
            else
                OwnedShips.RemoveRef(ship);
            
            ship.AI.ClearOrders();
            Pool.RemoveShipFromFleetAndPools(ship);
        }

        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if (rel?.AtWar != false || !rel.Known)
                return true;

            if (rel.Treaty_Peace || rel.Treaty_NAPact || rel.Treaty_Alliance)
                return false;

            if (isFaction || targetEmpire.isFaction)
                return true;

            if (!isPlayer)
            {
                float trustworthiness = targetEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 100;
                float peacefulness    = 1 - targetEmpire.Research.Strategy.MilitaryRatio;

                if (rel.TotalAnger > trustworthiness * peacefulness)
                    return true;
            }

            if (target == null)
                return true; // this is an inanimate check, so it won't cause trouble?
            //CG:there is an additional check that can be done for the ship itself.
            //if no target is applied then it is assumed the target is attackable at this point.
            //but an additional check can be done if a gameplay object is passed.
            //maybe its a freighter or something along those lines which might not be attackable.
            return target.IsAttackable(this, rel);
        }

        public bool IsEmpireHostile(Empire targetEmpire)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if (rel == null) return false;
            
            return rel.AtWar || (isFaction || targetEmpire.isFaction) && !rel.Treaty_Peace && !rel.Treaty_NAPact;
        }

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.guid == planetGuid)
                    return p;
            return null;
        }

        public AO EmpireAO()
        {
            Vector2 center = GetCenter();
            float radius = 0;
            if (OwnedPlanets.Count > 0)
            {
                var furthestSystem = OwnedSolarSystems.FindMax(s => s.Position.SqDist(center));
                radius = furthestSystem.Position.Distance(center);
            }
            else
            {
                var furthest = OwnedShips.FindMax(s => s.Position.SqDist(center));
                radius = furthest.Center.Distance(center);
            }
            return new AO(center, radius);
        }

        public SolarSystem FindFurthestOwnedSystemFrom(Vector2 position)
        {
            return OwnedSolarSystems.FindFurthestFrom(position);
        }

        public SolarSystem FindNearestOwnedSystemTo(Vector2 position)
        {
            return OwnedSolarSystems.FindClosestTo(position);
        }

        /// <summary>
        /// Finds the nearest owned system to systems in list. Returns TRUE if found.
        /// </summary>
        public bool FindNearestOwnedSystemTo(IEnumerable<SolarSystem>systems, out SolarSystem nearestSystem)
        {
            nearestSystem  = null;
            if (OwnedSolarSystems.Count == 0)
                return false; // We do not have any system owned, maybe we are defeated

            float distance = float.MaxValue;
            Vector2 center = GetWeightedCenter();
            foreach(SolarSystem system in systems)
            {
                var nearest = OwnedSolarSystems.FindClosestTo(system);
                if (nearest == null) continue;
                float approxDistance = center.SqDist(nearest.Position);
                if (center.SqDist(nearest.Position) < distance)
                {
                    distance      = approxDistance;
                    nearestSystem = nearest;
                }
            }
            return nearestSystem != null;
        }

        public float MinDistanceToNearestOwnedSystemIn(IEnumerable<SolarSystem> systems, out SolarSystem nearestSystem)
        {
            nearestSystem = null;
            if (OwnedSolarSystems.Count == 0)
                return -1; // We do not have any system owned, maybe we are defeated

            float distance = float.MaxValue;
            foreach (SolarSystem system in systems)
            {
                var nearest = OwnedSolarSystems.FindClosestTo(system);
                if (nearest == null) continue;
                float testDistance = system.Position.Distance(nearest.Position);
                if (testDistance < distance)
                {
                    distance = testDistance;
                    nearestSystem = nearest;
                }
            }
            return distance;
        }

        public bool UpdateContactsAndBorders(float elapsedTime)
        {
            bool bordersChanged = false;
            updateContactsTimer -= elapsedTime;
            if (updateContactsTimer < 0f && !data.Defeated)
            {
                int oldBorderNodesCount = BorderNodes.Count;
                ResetBorders();
                bordersChanged = (BorderNodes.Count != oldBorderNodesCount);

                UpdateKnownShips();
                updateContactsTimer = elapsedTime + RandomMath.RandomBetween(4f, 6.5f);
            }
            return bordersChanged;
        }

        public int EstimateCreditCost(float itemCost)   => (int)Math.Round(ProductionCreditCost(itemCost), 0);
        public void ChargeCreditsHomeDefense(Ship ship) => ChargeCredits(ship.GetCost(this));

        public void ChargeCreditsOnProduction(QueueItem q, float spentProduction)
        {
            if (q.IsMilitary || q.isShip)
                ChargeCredits(spentProduction);
        }

        public void RefundCreditsPostRemoval(Ship ship, float percentOfAmount = 0.5f)
        {
            if (!ship.IsDefaultAssaultShuttle && !ship.IsDefaultTroopShip)
                RefundCredits(ship.GetCost(this) * ship.HealthPercent, percentOfAmount);
        }

        public void RefundCreditsPostRemoval(Building b)
        {
            if (b.IsMilitary)
                RefundCredits(b.ActualCost, 0.5f);
        }

        void ChargeCredits(float cost)
        {
            float creditsToCharge = ProductionCreditCost(cost);
            MoneySpendOnProductionThisTurn += creditsToCharge;
            AddMoney(-creditsToCharge);
        }

        void RefundCredits(float cost, float percentOfAmount)
        {
            float creditsToRefund = cost * DifficultyModifiers.CreditsMultiplier * percentOfAmount;
            MoneySpendOnProductionThisTurn -= creditsToRefund;
            AddMoney(creditsToRefund);
        }

        float ProductionCreditCost(float spentProduction)
        {
            // fixed costs for players, feedback tax loop for the AI
            float taxModifer = isPlayer ? 1 : 1 - data.TaxRate;
            return spentProduction * taxModifer * DifficultyModifiers.CreditsMultiplier;
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

        public void RestoreUnserializableDataFromSave()
        {
            //restore relationShipData
            foreach(var kv in Relationships)
            {
                var relationship = kv.Value;
                relationship.RestoreWarsFromSave();
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Empire() { Destroy(); }

        void Destroy()
        {
            Pool?.Dispose(ref Pool);
            BorderNodes?.Dispose(ref BorderNodes);
            SensorNodes?.Dispose(ref SensorNodes);
            KnownShips?.Dispose(ref KnownShips);
            OwnedShips?.Dispose(ref OwnedShips);
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
