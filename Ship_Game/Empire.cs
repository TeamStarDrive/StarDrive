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
using Ship_Game.AI.ExpansionAI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires;
using Ship_Game.Empires.DataPackets;
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

        public float GetProjectorRadius() => Universe?.SubSpaceProjectors.Radius * data.SensorModifier ?? 10000;
        public float GetProjectorRadius(Planet planet) => GetProjectorRadius() + 10000f * planet.PopulationBillion;
        readonly Map<int, Fleet> FleetsDict = new Map<int, Fleet>();


        public readonly Map<string, bool> UnlockedHullsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedTroopDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedBuildingsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Map<string, bool> UnlockedModulesDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Array<Troop> UnlockedTroops = new Array<Troop>();

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
        private readonly Map<SolarSystem, bool> HostilesLogged = new Map<SolarSystem, bool>(); // Only for Player warnings
        public Array<IncomingThreat> SystemWithThreat = new Array<IncomingThreat>();
        public HashSet<string> ShipsWeCanBuild = new HashSet<string>();
        public HashSet<string> structuresWeCanBuild = new HashSet<string>();
        private float FleetUpdateTimer = 5f;
        private int TurnCount = 1;
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;
        public bool ScanComplete = true;
        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesn't normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        public bool isFaction;

        // For Pirate Factions. This will allow the Empire to be pirates
        public Pirates Pirates { get; private set; }

        // For Remnants Factions. This will allow the Empire to be Remnants
        public Remnants Remnants { get; private set; }

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

        public float updateContactsTimer = 0f;
        public float MaxContactTimer = 0f;
        private bool HostilesDictForPlayerInitialized;
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
        public float OffensiveStrength; // No Orbitals
        public ShipPool Pool;
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue { get; private set; }
        public float TotalColonyValues { get; private set; }
        public Ship BestPlatformWeCanBuild { get; private set; }
        public Ship BestStationWeCanBuild { get; private set; }
        public HashSet<string> ShipTechs = new HashSet<string>();
        public EmpireUI UI;
        public int GetEmpireTechLevel() => (int)Math.Floor(ShipTechs.Count / 3f);
        public Vector2 WeightedCenter;
        public bool RushAllConstruction;
        public List<KeyValuePair<int, string>> DiplomacyContactQueue { get; private set; } = new List<KeyValuePair<int, string>>();  // Empire IDs, for player only

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
        public bool WeAreRemnants            => Remnants != null; // Use this to figure out if this empire is pirate faction

        public Dictionary<ShipData.RoleName, string> PreferredAuxillaryShips = new Dictionary<ShipData.RoleName, string>();

        // Income this turn before deducting ship maintenance
        public float GrossIncome              => GrossPlanetIncome + TotalTradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus;
        public float NetIncome                => GrossIncome - AllSpending;
        public float TotalBuildingMaintenance => GrossPlanetIncome - NetPlanetIncomes;
        public float BuildingAndShipMaint     => TotalBuildingMaintenance + TotalShipMaintenance;
        public float AllSpending              => BuildingAndShipMaint + MoneySpendOnProductionThisTurn;
        public bool IsExpansionists           => data.EconomicPersonality?.Name == "Expansionists";
        public bool IsIndustrialists          => data.EconomicPersonality?.Name == "Industrialists";
        public bool IsGeneralists             => data.EconomicPersonality?.Name == "Generalists";
        public bool IsMilitarists             => data.EconomicPersonality?.Name == "Militarists";
        public bool IsTechnologists           => data.EconomicPersonality?.Name == "Technologists";

        public Planet[] SpacePorts       => OwnedPlanets.Filter(p => p.HasSpacePort);
        public Planet[] MilitaryOutposts => OwnedPlanets.Filter(p => p.AllowInfantry); // Capitals allow Infantry as well
        public Planet[] SafeSpacePorts   => OwnedPlanets.Filter(p => p.HasSpacePort && p.Safe);

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
            if (fromSave && data.Defeated)
                return;

            if (!fromSave && GlobalStats.DisablePirates)
            {
                data.Defeated = true;
                return;
            }

            Pirates = new Pirates(this, fromSave, goals);
        }

        public void SetAsRemnants(bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Remnants = new Remnants(this, fromSave, goals);
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
            return  RallyPoints.FindMinFiltered(p=> p.Owner == this, p => p.Center.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Center.SqDist(location));
        }

        public Planet FindNearestSafeRallyPoint(Vector2 location)
        {
            return SafeSpacePorts.FindMin(p => p.Center.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Center.SqDist(location));
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

        public string GetAssaultShuttleName() // this will get the name of an Assault Shuttle if defined in race.xml or use default one
        {
            return data.DefaultAssaultShuttle.IsEmpty() ? BoardingShuttle.Name : data.DefaultAssaultShuttle;
        }

        public string GetSupplyShuttleName() // this will get the name of a Supply Shuttle if defined in race.xml or use default one
        {
            return data.DefaultSupplyShuttle.IsEmpty() ? SupplyShuttle.Name
                                                       : data.DefaultSupplyShuttle;
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
                return chosen != null;
            }
            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {ship} at! Candidates:{ports.Count}");
            chosen = null;
            return false;
        }

        public bool FindPlanetToBuildTroopAt(IReadOnlyList<Planet> ports, Troop troop, out Planet chosen)
        {
            if (ports.Count != 0)
            {
                float cost = troop.ActualCost;
                chosen     = FindPlanetToBuildAt(ports, cost, forTroop: true);
                return chosen != null;
            }

            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {troop} at! Candidates:{ports.Count}");
            chosen = null;
            return false;
        }

        public Planet FindPlanetToBuildAt(IReadOnlyList<Planet> ports, float cost, bool forTroop = false)
        {
            // focus on the best producing planets (number depends on the empire size)
            if (GetBestPorts(ports, out Planet[] bestPorts))
                return bestPorts.FindMin(p => p.TurnsUntilQueueComplete(cost, forTroop));

            return null;
        }

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, Ship ship, bool travelBack, out Planet planet)
        {
            planet               = null;
            int travelMultiplier = travelBack ? 2 : 1;

            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, false) 
                                        + ship.GetAstrogateTimeTo(p) * travelMultiplier);

            return planet != null;
        }

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, out Planet planet)
        {
            planet = null;
            if (ports.Count == 0)
                return false;

            ports  = ports.Filter(p => !p.IsCrippled);
            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, false));
            return planet != null;
        }

        public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding() => GetBestPortsForShipBuilding(OwnedPlanets);
        public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding(IReadOnlyList<Planet> ports)
        {
            if (ports == null) return Empty<Planet>.Array;
            GetBestPorts(ports, out Planet[] bestPorts); 
            return bestPorts?.Filter(p=> p.HasSpacePort || p.NumShipyards > 0) ?? Empty<Planet>.Array;
        }

        bool GetBestPorts(IReadOnlyList<Planet> ports, out Planet[] bestPorts)
        {
            bestPorts = null;
            if (ports.Count > 0)
            {
                float averageMaxProd = ports.Average(p => p.Prod.NetMaxPotential);
                bestPorts            = ports.Filter(p => !p.IsCrippled && p.Prod.NetMaxPotential.GreaterOrEqual(averageMaxProd));
                bestPorts            = bestPorts.SortedDescending(p => p.Prod.NetMaxPotential);
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

        public bool FindPlanetToScrapIn(Ship ship, out Planet planet)
        {
            planet = null;
            if (OwnedPlanets.Count == 0)
                return false;

            if (!ship.BaseCanWarp)
            {
                planet = FindNearestRallyPoint(ship.Center);
                if (planet == null || planet.Center.Distance(ship.Center) > 50000)
                    ship.ScuttleTimer = 5;
                
                return planet != null;
            }

            var scrapGoals       = GetEmpireAI().Goals.Filter(g => g.type == GoalType.ScrapShip);
            var potentialPlanets = OwnedPlanets.SortedDescending(p => p.MissingProdHereForScrap(scrapGoals)).Take(5).ToArray();
            if (potentialPlanets.Length == 0)
                return false;

            planet = potentialPlanets.FindMin(p => p.Center.Distance(ship.Center));
            return planet != null;
        }

        public float KnownEnemyStrengthIn(SolarSystem system, Predicate<ThreatMatrix.Pin> filter)
                     => EmpireAI.ThreatMatrix.GetStrengthInSystem(system, filter);
        public float KnownEnemyStrengthIn(SolarSystem system)
             => EmpireAI.ThreatMatrix.GetStrengthInSystem(system, p=> IsEmpireHostile(p.GetEmpire()));
        public float KnownEnemyStrengthIn(AO ao)
             => EmpireAI.ThreatMatrix.PingHostileStr(ao.Center, ao.Radius, this);
        public float KnownEmpireStrength(Empire empire) => EmpireAI.ThreatMatrix.KnownEmpireStrength(empire, p => p != null);

        public WeaponTagModifier WeaponBonuses(WeaponTag which) => data.WeaponTags[which];
        public Map<int, Fleet> GetFleetsDict() => FleetsDict;
        public Fleet GetFleetOrNull(int key) => FleetsDict.TryGetValue(key, out Fleet fleet) ? fleet : null;
        public Fleet GetFleet(int key) => FleetsDict[key];

        public float TotalFactionsStrTryingToClear()
        {
            var claimTasks = EmpireAI.GetClaimTasks();
            float str = 0;
            for (int i = 0; i < claimTasks.Length; ++i)
            {
                var task = claimTasks[i];
                if (task.TargetPlanet.Owner == null) // indicates its remnant infested and not another empire
                    str += task.MinimumTaskForceStrength;
            }

            var assaultPirateTasks = EmpireAI.GetAssaultPirateTasks();
            if (assaultPirateTasks.Length > 0)
                str += assaultPirateTasks.Sum(t => t.MinimumTaskForceStrength);

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
                var planets = ao.GetOurPlanets().Filter(p => p.ParentSystem.OwnerList.Count == 1);
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
                home = nearestAO.GetOurPlanets().FindClosestTo(ship);
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

        /// <summary>
        /// Returns the Player's Environment Modifier based on a planet's category.
        /// </summary>
        public float PlayerEnvModifier(PlanetCategory category) => RacialEnvModifer(category, EmpireManager.Player);

        /// <summary>
        /// Returns the Player's Preferred Environment Modifier.
        /// </summary>
        public float PlayerPreferredEnvModifier 
            => RacialEnvModifer(EmpireManager.Player.data.PreferredEnv, EmpireManager.Player);


        /// <summary>
        /// Returns the preferred Environment Modifier of a given empire.This is null Safe.
        /// </summary>
        public static float PreferredEnvModifier(Empire empire)
            => empire == null ? 1 : RacialEnvModifer(empire.data.PreferredEnv, empire);


        /// <summary>
        /// Returns the preferred Environment Modifier of a given empire. This is null Safe.
        /// </summary>
        public static float RacialEnvModifer(PlanetCategory category, Empire empire)
        {
            float modifer = 1f; // If no Env tags were found, the multiplier is 1.
            if (empire == null)
                return modifer;

            switch (category)
            {
                case PlanetCategory.Terran:  modifer = empire.data.EnvTerran;  break;
                case PlanetCategory.Oceanic: modifer = empire.data.EnvOceanic; break;
                case PlanetCategory.Steppe:  modifer = empire.data.EnvSteppe;  break;
                case PlanetCategory.Tundra:  modifer = empire.data.EnvTundra;  break;
                case PlanetCategory.Swamp:   modifer = empire.data.EnvSwamp;   break;
                case PlanetCategory.Desert:  modifer = empire.data.EnvDesert;  break;
                case PlanetCategory.Ice:     modifer = empire.data.EnvIce;     break;
                case PlanetCategory.Barren:  modifer = empire.data.EnvBarren;  break;
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
            ActiveRelations.Clear();
            RelationsMap.Clear();
            EmpireAI = null;
            HostilesLogged.Clear();
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

            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                BreakAllTreatiesWith(them, includingPeace: true);
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

            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                BreakAllTreatiesWith(them, includingPeace: true);
            }

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
            if (Empire.Universe.Debug && Universe.SelectedShip == null) return true;

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

        /// <summary>
        /// this appears to be broken. 
        /// </summary>
        public IReadOnlyList<SolarSystem> GetOwnedSystems() => OwnedSolarSystems;
        public IReadOnlyList<Planet> GetPlanets()           => OwnedPlanets;
        public int NumPlanets                               => OwnedPlanets.Count;
        public int NumSystems                               => OwnedSolarSystems.Count;

        public Array<SolarSystem> GetOurBorderSystemsTo(Empire them, bool hideUnexplored, float percentageOfMapSize = 0.5f)
        {
            Vector2 theirCenter = them.WeightedCenter;
            Vector2 ownCenter   = WeightedCenter;
            var directionToThem = ownCenter.DirectionToTarget(theirCenter);
            float midDistance   = ownCenter.Distance(theirCenter) / 2;
            Vector2 midPoint    = directionToThem * midDistance;
            float mapDistance   = (Universe.UniverseSize * percentageOfMapSize).UpperBound(ownCenter.Distance(theirCenter) * 1.2f);

            var solarSystems = new Array<SolarSystem>();
            foreach (var solarSystem in GetOwnedSystems())
            {
                if (hideUnexplored && !solarSystem.IsExploredBy(them)) continue;

                if (solarSystem.Position.InRadius(midPoint, mapDistance))
                    solarSystems.AddUniqueRef(solarSystem);
                else if (solarSystem.Position.InRadius(ownCenter, midDistance))
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

            CalcWeightedCenter(calcNow: true);
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
            CalcWeightedCenter(calcNow: true);
        }

        public void TrySendInitialFleets(Planet p)
        {
            if (isPlayer)
                return;

            if (p.TilesList.Any(t => t.EventOnTile))
                EmpireAI.SendExplorationFleet(p);

            if (CurrentGame.Difficulty <= UniverseData.GameDifficulty.Normal || p.ParentSystem.IsExclusivelyOwnedBy(this))
                return;

            if (PlanetRanker.IsGoodValueForUs(p, this) && KnownEnemyStrengthIn(p.ParentSystem).AlmostZero())
            {
                var task = MilitaryTask.CreateGuardTask(this, p);
                EmpireAI.AddPendingTask(task);
            }
        }

        public BatchRemovalCollection<Ship> GetShips() => OwnedShips;
        public Ship[] GetShipsAtomic() => OwnedShips.ToArray();

        public Ship[] AllFleetReadyShips()
        {
            //Get all available ships from AO's
            var ships = isPlayer ? OwnedShips : Pool.GetShipsFromOffensePools();

            var readyShips = new Array<Ship>();
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship == null || ship.fleet != null)
                    continue;

                if (ship.AI.State == AIState.Resupply
                    || ship.AI.State == AIState.ResupplyEscort
                    || ship.AI.State == AIState.Refit
                    || ship.AI.State == AIState.Scrap
                    || ship.AI.State == AIState.Scuttle)
                {
                    continue;
                }

                readyShips.Add(ship);
            }

            return readyShips.ToArray();
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
                if (modulesNotHulls && !WeCanUseThisInDesigns(entry.Value, ourShips))
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
            InitDifficultyModifiers();
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

            CreateThrusterColors();
            UpdateShipsWeCanBuild();
            Research.SetResearchStrategy();
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

        void AssessHostilePresenceForPlayerWarnings()
        {
            for (int i = 0; i < OwnedSolarSystems.Count; i++)
            {
                SolarSystem system = OwnedSolarSystems[i];
                if (HostilesLogged[system])
                {
                    HostilesLogged[system] = system.HostileForcesPresent(this);
                    continue;
                }

                HashSet<Empire> ignoreList = new HashSet<Empire>();
                for (int j = 0; j < system.ShipList.Count; j++)
                {
                    Ship ship            = system.ShipList[j];
                    Empire checkedEmpire = ship.loyalty;

                    if (ignoreList.Contains(checkedEmpire) 
                        || ship.NotThreatToPlayer()
                        || !ship.inSensorRange)
                    {
                        ignoreList.Add(checkedEmpire);
                        continue;
                    }

                    float strRatio = StrRatio(system, checkedEmpire);
                    Universe.NotificationManager.AddBeingInvadedNotification(system, checkedEmpire, strRatio);
                    HostilesLogged[system] = true;
                    break;
                }
            }

            float StrRatio(SolarSystem system, Empire hostiles)
            {
                float hostileOffense  = system.ShipList.Filter(s => s.loyalty == hostiles).Sum(s => s.BaseStrength);
                var ourPlanetsOffense = system.PlanetList.Filter(p => p.Owner == this).Sum(p => p.BuildingGeodeticOffense);
                var ourSpaceAssets    = system.ShipList.Filter(s => s.loyalty == this);
                float ourSpaceOffense = 0;

                if (ourSpaceAssets.Length > 0)
                    ourSpaceOffense += ourSpaceAssets.Sum(s => s.BaseStrength);

                float ourOffense = (ourSpaceOffense + ourPlanetsOffense).LowerBound(1);
                return hostileOffense / ourOffense;
            }
        }

        void ScanFromAllInfluenceNodes(FixedSimTime timeStep)
        {
            for (int i = 0; i < BorderNodes.Count; i++)
            {
                var node = BorderNodes[i];
                ScanForInfluence(node, timeStep);
            }

            for (int i = 0; i < SensorNodes.Count; i++)
            {
                var node = SensorNodes[i];
                ScanForShips(node);
            }
        }

        void ScanForShips(InfluenceNode node)
        {
            if (node.SourceObject is Ship)
                return;

            // find ships in radius of node. 
            GameplayObject[] targets = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                        node.Position, node.Radius, maxResults:1024);
            for (int i = 0; i < targets.Length; i++)
            {
                var targetShip = (Ship)targets[i];
                targetShip.KnownByEmpires.SetSeen(this);
            }
        }	

        void ScanForInfluence(InfluenceNode node, FixedSimTime timeStep)
        {
            // find anyone within this influence node
            GameplayObject[] targets = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                       node.Position, node.Radius, maxResults:1024);
            for (int i = 0; i < targets.Length; i++)
            {
                var ship = (Ship)targets[i];
                ship.SetProjectorInfluence(this, true);
                
                // Civilian infrastructure spotting enemy fleets
                if (node.SourceObject is Ship ssp)
                {
                    ssp.HasSeenEmpires.Update(timeStep);
                    if (ship.fleet != null)
                    {
                        if (isPlayer || Universe.Debug && Universe.SelectedShip?.loyalty == this)
                        {
                            if (IsEmpireHostile(ship.loyalty))
                                ssp.HasSeenEmpires.SetSeen(ship.loyalty);
                        }
                    }
                }
            }
        }


        void DoFirstContact(Empire them)
        {
            Relationship usToThem = GetRelations(them);
            usToThem.SetInitialStrength(them.data.Traits.DiplomacyMod * 100f);
            usToThem.Known = true;
            if (!them.IsKnown(this)) // do THEY know us?
                them.DoFirstContact(this);

            if (Universe.Debug)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Universe.player == this)
                return;

            if (Universe.PlayerEmpire == this)
            {
                if (them.isFaction)
                    DoFactionFirstContact(them);
                else
                    DiplomacyScreen.Show(them, "First Contact");
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

        public void Update(FixedSimTime timeStep)
        {
            #if PLAYERONLY
                if(!this.isPlayer && !this.isFaction)
                foreach (Ship ship in this.GetShips())
                    ship.GetAI().OrderScrapShip();
                if (this.GetShips().Count == 0)
                    return;
            #endif

            UpdateTimer -= timeStep.FixedTime;

            if (UpdateTimer <= 0f && !data.Defeated)
            {
                if (this == Universe.PlayerEmpire)
                {
                    Universe.UpdateStarDateAndTriggerEvents(Universe.StarDate + 0.1f);
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

                    if (!HostilesDictForPlayerInitialized)
                        InitializeHostilesInSystemDict();
                    else
                        AssessHostilePresenceForPlayerWarnings();
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
                Inhibitors.Clear();
                for (int i = 0; i < OwnedShips.Count; i++)
                {
                    Ship ship = OwnedShips[i];
                    //if (ship?.Active != true) continue;

                    if (ship.InhibitionRadius > 0.0f)
                        Inhibitors.Add(ship);

                    if (ship.fleet == null && ship.InCombat && ship.Mothership == null) //fbedard: total ships in combat
                        empireShipCombat++;

                    if (ship.Mothership != null || ship.DesignRole            == ShipData.RoleName.troop
                                                || ship.DesignRole            == ShipData.RoleName.freighter
                                                || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                        continue;
                    empireShipTotal++;
                }

                UpdateTimer = GlobalStats.TurnTimer + (Id -1) * timeStep.FixedTime;
                UpdateEmpirePlanets();
                UpdateAI(); // Must be done before DoMoney
                GovernPlanets(); // this does the governing after getting the budgets from UpdateAI when loading a game
                DoMoney();
                TakeTurn();
            }
            SetRallyPoints();
            UpdateFleets(timeStep);
            OwnedShips.ApplyPendingRemovals();
            OwnedProjectors.ApplyPendingRemovals();  //fbedard
        }

        void InitializeHostilesInSystemDict() // For Player warnings
        {
            HostilesDictForPlayerInitialized = true;
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem system = UniverseScreen.SolarSystemList[i];
                if (!system.HasPlanetsOwnedBy(EmpireManager.Player) || GlobalStats.NotifyEnemyInSystemAfterLoad)
                {
                    HostilesLogged.Add(system, false);
                    continue;
                }

                bool hostileFound = false;
                for (int j = 0; j < system.ShipList.Count; j++)
                {
                    Ship ship = system.ShipList[j];
                    if (!ship.NotThreatToPlayer() 
                        && system.PlanetList.Any(p => p.Owner == EmpireManager.Player && p.ShipWithinSensorRange(ship)))
                    {
                        hostileFound = true;
                        break;
                    }
                }

                HostilesLogged.Add(system, hostileFound);
            }
        }

        /// <summary>
        /// This should be run on save load to set economic values without taking a turn.
        /// </summary>
        public void InitEmpireEconomy()
        {
            UpdateEmpirePlanets();
            UpdateNetPlanetIncomes();
            UpdateMilitaryStrengths();
            CalculateScore();
            UpdateRelationships();
            UpdateShipMaintenance(); ;
            EmpireAI.RunEconomicPlanner();
        }

        public void UpdateMilitaryStrengths()
        {
            CurrentMilitaryStrength = 0;
            CurrentTroopStrength    = 0;
            OffensiveStrength       = 0;

            for (int i = 0; i < OwnedShips.Count; ++i)
            {
                Ship ship = OwnedShips[i];
                if (ship != null)
                {
                    float str                = ship.GetStrength();
                    CurrentMilitaryStrength += str;
                    CurrentTroopStrength    += ship.Carrier.MaxTroopStrengthInShipToCommit;
                    if (!ship.IsPlatformOrStation)
                        OffensiveStrength += str;
                }
            }

            for (int x = 0; x < OwnedPlanets.Count; x++)
            {
                var planet = OwnedPlanets[x];
                CurrentTroopStrength += planet.TroopManager.OwnerTroopStrength;
            }
        }

        public void AssessSystemsInDanger(FixedSimTime timeStep)
        {
            for (int i = 0; i < SystemWithThreat.Count; i++)
            {
                var threat = SystemWithThreat[i];
                threat.UpdateTimer(timeStep);
            }

            var knownFleets = new Array<Fleet>();
            for ( int i = 0; i < AllRelations.Count; i++)
            {
                var war = AllRelations[i];
                if (!IsAtWarWith(war.Them)) continue;
                var enemy = war.Them;

                foreach (var fleet in enemy.FleetsDict)
                {
                    if (fleet.Value.Ships.Any(s => s.IsInBordersOf(this) || s.KnownByEmpires.KnownBy(this)))
                    {
                        knownFleets.Add(fleet.Value);
                    } 
                }
            }

            for (int i = 0; i < OwnedSolarSystems.Count; i++)
            {
                var system = OwnedSolarSystems[i];
                var fleets = knownFleets.Filter(f => f.FinalPosition.InRadius(system.Position, system.Radius * 2));
                if (fleets.Length > 0)
                {

                    if (!SystemWithThreat.Any(s => s.UpdateThreat(system, fleets)))
                        SystemWithThreat.Add(new IncomingThreat(this, system, fleets));
                }
            }
        }

        //Using memory to save CPU time. the question is how often is the value used and
        //How often would it be calculated.
        private void UpdateMaxColonyValue()
        {
            MaxColonyValue    = 0;
            TotalColonyValues = 0;

            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet planet = OwnedPlanets[i];
                TotalColonyValues += planet.ColonyValue;
                if (planet.ColonyValue > MaxColonyValue)
                    MaxColonyValue = planet.ColonyValue;

            }
        }

        private void UpdateBestOrbitals()
        {
            // FB - this is done here for more performance. having set values here prevents calling shipbuilder by every planet every turn
            BestPlatformWeCanBuild = BestShipWeCanBuild(ShipData.RoleName.platform, this);
            BestStationWeCanBuild  = BestShipWeCanBuild(ShipData.RoleName.station, this);
        }

        public void UpdateDefenseShipBuildingOffense()
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

        void UpdateFleets(FixedSimTime timeStep)
        {
            FleetUpdateTimer -= timeStep.FixedTime;
            foreach (var kv in FleetsDict)
            {
                Fleet fleet = kv.Value;
                fleet.Update(timeStep);
                if (FleetUpdateTimer <= 0f)
                    fleet.UpdateAI(timeStep, kv.Key);
            }
            if (FleetUpdateTimer < 0.0)
                FleetUpdateTimer = 5f;
        }

        void DoMoney()
        {
            MoneyLastTurn = Money;
            ++TurnCount;

            UpdateTradeIncome();
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
            ResetMoneySpentOnProduction();
            using (OwnedPlanets.AcquireReadLock())
            {
                TotalProdExportSlots = OwnedPlanets.Sum(p => p.FreeProdExportSlots); // Done before UpdateOwnedPlanet
                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    Planet planet = OwnedPlanets[i];
                    planet.UpdateOwnedPlanet();
                }
            }
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

            Ship preferredBomber = PickFromCandidates(ShipData.RoleName.bomber, this, targetModule: ShipModuleType.Bomb);
            if (preferredBomber != null)
                PreferredAuxillaryShips[ShipData.RoleName.bomber] = preferredBomber.Name;

            Ship preferredCarrier = PickFromCandidates(ShipData.RoleName.carrier, this, targetModule: ShipModuleType.Hangar);
            if (preferredCarrier != null)
                PreferredAuxillaryShips[ShipData.RoleName.carrier] = preferredCarrier.Name;
                
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

        public bool WeCanUseThisTech(TechEntry checkedTech, Array<Ship> ourFactionShips)
        {
            if (checkedTech.IsHidden(this))
                return false;

            if (!checkedTech.IsOnlyShipTech() || isPlayer)
                return true;

            return WeCanUseThisInDesigns(checkedTech, ourFactionShips);
        }

        public bool WeCanUseThisInDesigns(TechEntry checkedTech, Array<Ship> ourFactionShips)
        {
            // Dont offer tech to AI if it does not have designs for it.
            Technology tech = checkedTech.Tech;
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
                    .Filter(troopship => troopship.IsIdleSingleTroopship)
                    .OrderBy(distance => Vector2.Distance(distance.Center, objectCenter)));

            if (troopShips.Count > 0)
                troopShip = troopShips.First();

            return troopShip != null;
        }

        public int NumFreeTroops()
        {
            int numTroops;
            using (OwnedShips.AcquireReadLock())
            {
                numTroops = OwnedShips.Filter(s => s.IsIdleSingleTroopship).Length + OwnedPlanets.Sum(p => p.NumTroopsCanLaunch);
            }

            return numTroops;
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
                troopShip = troops.FirstOrDefault(t => t.Loyalty == this).Launch();
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

        public float GetTotalPop() => GetTotalPop(out _);

        /// <summary>
        /// Gets the total population in billions and option for max pop
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
                    Ship ship = OwnedShips[i];
                    num += ship.GetCargo(Goods.Colonists) / 1000;
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

        public void SwitchRushAllConstruction(bool rush)
        {
            foreach (Planet planet in OwnedPlanets)
                planet.Construction.SwitchRushAllConstruction(rush);
        }

        public Planet.ColonyType AssessColonyNeeds2(Planet p)
        {
            float richness  = p.MineralRichness;
            float fertility = IsCybernetic ? richness : p.FertilityFor(this);
            float maxPop    = p.MaxPopulationBillionFor(this);

            if (richness >= 1 && fertility >= 1 && maxPop >= 7)
                return Planet.ColonyType.Core;

            if (fertility > 0.5f && fertility <= 1 && richness <= 1 && maxPop > 3)
                 return Planet.ColonyType.Research;

            if (fertility > 1 && richness < 1 && maxPop >= 2)
                 return Planet.ColonyType.Agricultural;

            if (richness >= 1 )
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

        /// <summary>
        /// Border nodes are used to show empire influence.
        /// Sensor nodes are used to show the sensor range of things. Ship, planets, spys, etc
        /// This process uses object recycling.
        /// the object is put into the pending removals portion of the batchremoval class.
        /// when a new node is wanted it is pulled from the pending pool, wiped and used or created new.
        /// this should cut down on garbage collection as the objects are cycled often. 
        /// </summary>
        void ResetBorders()
        {
            var tempBorderNodes = new Array<InfluenceNode>();
            var tempSensorNodes = new Array<InfluenceNode>();

            bool wellKnown = EmpireManager.Player == this || EmpireManager.Player.IsAlliedWith(this) || 
                Universe.Debug && (Universe.SelectedShip == null || Universe.SelectedShip.loyalty == this );
            bool known = wellKnown || EmpireManager.Player.IsTradeOrOpenBorders(this);

            SetBordersKnownByAllies(tempSensorNodes);
            SetBordersByPlanet(known, tempBorderNodes, tempSensorNodes);

            // Moles are spies who have successfully been planted during 'Infiltrate' type missions, I believe - Doctor
            foreach (Mole mole in data.MoleList)
                tempSensorNodes.Add(new InfluenceNode
                {
                    Position = Planet.GetPlanetFromGuid(mole.PlanetGuid).Center,
                    Radius   = GetProjectorRadius(),
                    KnownToPlayer    = true
                });

            var ships = OwnedShips.AtomicCopy();

            for (int i = 0; i < ships.Length; i++)
            {
                Ship ship = ships[i];
                if (ship == null) continue;
                InfluenceNode influenceNode = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                influenceNode.Position      = ship.Center;
                influenceNode.Radius        = ship.SensorRange;
                influenceNode.SourceObject  = ship;
                influenceNode.KnownToPlayer = IsSensorNodeVisible(wellKnown, ship);
                tempSensorNodes.Add(influenceNode);
            }

            for (int i = 0; i < OwnedProjectors.Count; i++)
            {
                Ship ship = OwnedProjectors[i];

                InfluenceNode influenceNodeS = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                InfluenceNode influenceNodeB = BorderNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();

                influenceNodeS.Position     = ship.Center;
                influenceNodeS.Radius       = GetProjectorRadius(); 
                influenceNodeS.SourceObject = ship;

                influenceNodeB.Position      = ship.Center;
                influenceNodeB.Radius        = GetProjectorRadius();
                influenceNodeB.SourceObject  = ship;

                bool seen                    = IsSensorNodeVisible(known, ship);
                influenceNodeB.KnownToPlayer = seen;
                influenceNodeS.KnownToPlayer = seen;
                tempSensorNodes.Add(influenceNodeS);
                tempBorderNodes.Add(influenceNodeB);
            }

            SetPirateBorders(tempBorderNodes);
            SetRemnantPortalBorders(tempBorderNodes);

            SensorNodes.ClearPendingRemovals();
            SensorNodes.ClearAndRecycle();
            SensorNodes.AddRange(tempSensorNodes);

            BorderNodes.ClearPendingRemovals();
            BorderNodes.ClearAndRecycle();
            BorderNodes.AddRange(tempBorderNodes);
        }

        private void SetPirateBorders(ICollection<InfluenceNode> borderNodes)
        {
            if (!WeArePirates || !Pirates.GetBases(out Array<Ship> bases))
                return;

            for (int i = 0; i < bases.Count; i++)
            {
                Ship pirateBase             = bases[i];
                InfluenceNode influenceNode = BorderNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                influenceNode.Position      = pirateBase.Center;
                influenceNode.Radius        = pirateBase.SensorRange;
                influenceNode.SourceObject  = pirateBase;
                influenceNode.KnownToPlayer = IsSensorNodeVisible(false, pirateBase);
                borderNodes.Add(influenceNode);
            }
        }

        private void SetRemnantPortalBorders(ICollection<InfluenceNode> borderNodes)
        {
            if (!WeAreRemnants || !Remnants.GetPortals(out Ship[] portals))
                return;

            for (int i = 0; i < portals.Length; i++)
            {
                Ship portal                 = portals[i];
                InfluenceNode influenceNode = BorderNodes.RecycleObject(n => n.Wipe()) ?? new InfluenceNode();
                influenceNode.Position      = portal.Center;
                influenceNode.Radius        = portal.SensorRange;
                influenceNode.SourceObject  = portal;
                influenceNode.KnownToPlayer = IsSensorNodeVisible(false, portal);
                borderNodes.Add(influenceNode);
            }
        }

        bool IsSensorNodeVisible(bool known, Ship ship)
        {
            return known || (!ship.BaseCanWarp && EmpireManager.Player.GetEmpireAI().ThreatMatrix.ContainsGuid(ship.guid)) 
                         || Universe.Debug && (Universe.SelectedShip == null || Universe.SelectedShip.loyalty == ship.loyalty 
                         || Universe.SelectedShip.loyalty.EmpireAI.ThreatMatrix.ContainsGuid(ship.guid));
        }

        private void SetBordersByPlanet(bool empireKnown, ICollection<InfluenceNode> borderNodes, ICollection<InfluenceNode> sensorNodes)
        {
            foreach (Planet planet in GetPlanets())
            {
                bool known = empireKnown || planet.IsExploredBy(EmpireManager.Player);
                //loop over OWN planets
                InfluenceNode borderNode = BorderNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();

                borderNode.SourceObject  = planet;
                borderNode.Position      = planet.Center;
                borderNode.Radius        = planet.SensorRange;
                borderNode.KnownToPlayer = known;
                
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.usePlanetaryProjection)
                    borderNode.Radius = planet.ProjectorRange;
                else
                    borderNode.Radius = GetProjectorRadius(planet);

                borderNodes.Add(borderNode);
                
                InfluenceNode sensorNode = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                sensorNode.SourceObject  = planet;
                sensorNode.Position      = planet.Center;
                sensorNode.Radius        = isFaction ? 1f : data.SensorModifier;
                sensorNode.KnownToPlayer = known;
                sensorNode.Radius        = planet.SensorRange;
                sensorNodes.Add(sensorNode);
            }
        }

        private void SetBordersKnownByAllies(ICollection<InfluenceNode> sensorNodes)
        {
            foreach(var empire in EmpireManager.Empires)
            {
                if (!GetRelations(empire, out Relationship relation) || 
                    !relation.Treaty_Alliance && (!Universe.Debug || !isPlayer || Universe.SelectedShip != null))
                    continue;

                bool wellKnown = true; // not a mistake. easier testing. 
                Planet[] array = empire.OwnedPlanets.ToArray();
                for (int y = 0; y < array.Length; y++)
                {
                    Planet planet                = array[y];
                    InfluenceNode influenceNode2 = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                    influenceNode2.Position      = planet.Center;
                    influenceNode2.Radius        = planet.SensorRange;
                    influenceNode2.KnownToPlayer = wellKnown;
                    influenceNode2.SourceObject  = planet;
                    sensorNodes.Add(influenceNode2);
                }

                var ships = empire.GetShipsAtomic();
                for (int z = 0; z < ships.Length; z++)
                {
                    Ship ship = ships[z];
                    if (ship?.Active != true) continue;

                    InfluenceNode influenceNode = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                    influenceNode.Position      = ship.Center;
                    influenceNode.Radius        = ship.SensorRange;
                    influenceNode.SourceObject  = ship;
                    influenceNode.KnownToPlayer = wellKnown;
                    sensorNodes.Add(influenceNode);
                }

                //loop over all ALLIED projectors
                var projectors = empire.GetProjectors();
                for (int z = 0; z < projectors.Count; z++)
                {
                    Ship ship = projectors[z];
                    if (ship?.Active != true) continue;
                    
                    InfluenceNode influenceNode = SensorNodes.RecycleObject(n=> n.Wipe()) ?? new InfluenceNode();
                    influenceNode.Position      = ship.Center;
                    influenceNode.Radius        = ship.SensorRange;
                    influenceNode.SourceObject  = ship;
                    influenceNode.KnownToPlayer = wellKnown;
                    sensorNodes.Add(influenceNode);
                }

            }
        }

        private void TakeTurn()
        {
            if (IsEmpireDead()) return;

            var list1 = new Array<Planet>();
            foreach (Planet planet in OwnedPlanets.AtomicCopy())
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
                float ratio = ((AllSpending - Money) / PotentialIncome.LowerBound(1));
                data.TurnsBelowZero += (short)(ratio);
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
                ExecuteDiplomacyContacts();
                CheckFederationVsPlayer();
                RandomEventManager.UpdateEvents();

                if ((Money / AllSpending.LowerBound(1)) < 2)
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
                        Empire remnants = EmpireManager.Remnants;
                        if (remnants.Remnants.Story == Remnants.RemnantStory.None || remnants.data.Defeated || !remnants.Remnants.Activated)
                        {
                            Universe.ScreenManager.AddScreenDeferred(new YouWinScreen(Universe));
                            Universe.GameOver = true;
                        }
                        else 
                        {
                            remnants.Remnants.TriggerOnlyRemnantsLeftEvent();
                        }
                    }
                }

                foreach (Planet planet in OwnedPlanets)
                {
                    if (planet.HasWinBuilding)
                    {
                        Universe.ScreenManager.AddScreenDeferred(new YouWinScreen(Universe, Localizer.Token(5085)));
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                if (StatTracker.GetSnapshot(Universe.StarDate, this, out Snapshot snapshot))
                    snapshot.Population = OwnedPlanets.Sum(p => p.Population);
            }

            Research.Update();

            if (data.TurnsBelowZero > 0 && Money < 0.0 && (!Universe.Debug || !isPlayer))
                Bankruptcy();

            if ((Universe.StarDate % 1).AlmostZero())
                CalculateScore();

            UpdateRelationships();

            if (Money > data.CounterIntelligenceBudget)
            {
                Money -= data.CounterIntelligenceBudget;
                foreach ((Empire them, Relationship rel) in ActiveRelations)
                {
                    Relationship relWithUs = them.GetRelations(this);
                    relWithUs.IntelligencePenetration -= data.CounterIntelligenceBudget / 10f;
                    if (relWithUs.IntelligencePenetration < 0.0f)
                        relWithUs.IntelligencePenetration = 0.0f;
                }
            }

            if (!isFaction)
            {
                CalcAverageFreighterCargoCapAndFTLSpeed();
                CalcWeightedCenter();
                DispatchBuildAndScrapFreighters();
                AssignExplorationTasks();
            }
        }

        void ExecuteDiplomacyContacts()
        {
            if (DiplomacyContactQueue.Count == 0)
                return;

            Empire empire = EmpireManager.GetEmpireById(DiplomacyContactQueue.First().Key);
            string dialog = DiplomacyContactQueue.First().Value;
            if (dialog == "DECLAREWAR")
            {
                empire.GetEmpireAI().DeclareWarOn(this, WarType.ImperialistWar);
            }
            else
            {
                DiplomacyScreen.ContactPlayerFromDiplomacyQueue(empire, dialog);
            }

            DiplomacyContactQueue.RemoveAt(0);
        }


        void CheckFederationVsPlayer()
        {
            if (Universe.StarDate < 1100f || (Universe.StarDate % 1).NotZero() || GlobalStats.PreventFederations)
                return;

            float playerScore    = TotalScore;
            var aiEmpires        = EmpireManager.ActiveNonPlayerMajorEmpires;
            float aiTotalScore   = aiEmpires.Sum(e => e.TotalScore);
            float allEmpireScore = aiTotalScore + playerScore;
            Empire biggestAI     = aiEmpires.FindMax(e => e.TotalScore);
            float biggestAIScore = biggestAI?.TotalScore ?? playerScore;

            if (playerScore < allEmpireScore / 2 || playerScore < biggestAIScore * 2 || aiEmpires.Length < 2)
                return;

            var leaders = new Array<Empire>();
            foreach (Empire e in aiEmpires)
            {
                if (e != biggestAI && e.IsKnown(biggestAI) && biggestAIScore * 0.6f > e.TotalScore)
                    leaders.Add(e);
            }

            if (leaders.Count > 0)
            {
                Empire strongest = leaders.FindMax(emp => biggestAI.GetRelations(emp).GetStrength());
                if (!biggestAI.IsAtWarWith(strongest))
                    Universe.NotificationManager.AddPeacefulMergerNotification(biggestAI, strongest);
                else
                    Universe.NotificationManager.AddSurrendered(biggestAI, strongest);

                biggestAI.AbsorbEmpire(strongest);
                if (biggestAI.GetRelations(this).ActiveWar == null)
                    biggestAI.GetEmpireAI().DeclareWarOn(this, WarType.ImperialistWar);
                else
                    biggestAI.GetRelations(this).ActiveWar.WarTheaters.AddCaptureAll();
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
                    if (OwnedPlanets.FindMax(out Planet planet, p => WeightedCenter.SqDist(p.Center)))
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

        public bool IsEmpireDead()
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
                if (EmpireManager.Player.IsKnown(this))
                    Universe.NotificationManager.AddEmpireDiedNotification(this);
                return true;
            }

            StarDriveGame.Instance?.EndingGame(true);
            Empire.Universe.GameOver = true;
            Universe.Objects.Clear();
            Universe.Paused = true;
            HelperFunctions.CollectMemory();
            StarDriveGame.Instance?.EndingGame(false);
            Universe.ScreenManager.AddScreenDeferred(new YouLoseScreen(Universe));
            Universe.Paused = false;
            return true;
        }

        public void MassScrap(Ship ship)
        {
            var shipList = ship.IsSubspaceProjector ? OwnedProjectors : OwnedShips;
            for (int i = 0; i < shipList.Count; i++)
            {
                Ship s = shipList[i];
                if (s.Name == ship.Name)
                    s.AI.OrderScrapShip();
            }
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

        public void AddMutinyNotification(Ship ship, GameText text, Empire initiator)
        {
            if (!isPlayer)
                return;

            string message = $"{new LocalizedText(text).Text} {initiator.Name}!";
            Universe.NotificationManager.AddBoardNotification(message, ship.BaseHull.ActualIconPath, "SnapToShip", ship, initiator);
        }

        private void CalculateScore()
        {
            TotalScore      = 0;
            TechScore       = 0;
            IndustrialScore = 0;
            ExpansionScore  = 0;

            CalcTechScore();
            CalcExpansionIndustrialScore();
            MilitaryScore = data.NormalizeMilitaryScore(CurrentMilitaryStrength); // Avoid fluctuations
            TotalScore    = (int)(MilitaryScore + IndustrialScore + TechScore + ExpansionScore);

            void CalcTechScore()
            {
                foreach (KeyValuePair<string, TechEntry> keyValuePair in TechnologyDict)
                {
                    if (keyValuePair.Value.Unlocked)
                        TechScore += ResourceManager.Tech(keyValuePair.Key).Cost;
                }

                TechScore /= 100;
            }

            void CalcExpansionIndustrialScore()
            {
                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    Planet planet    = OwnedPlanets[i];
                    ExpansionScore  += planet.Fertility*10 + planet.MineralRichness*10 + planet.PopulationBillion;
                    IndustrialScore += planet.BuildingList.Sum(b => b.ActualCost);
                }

                IndustrialScore /= 20;
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
            Vector2 center;
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

        // This is also done when a planet is added or removed
        public void CalcWeightedCenter(bool calcNow = false)
        {
            if (!calcNow && (Universe.StarDate % 1).Greater(0)) 
                return; // Once per year

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

            WeightedCenter=  avgPlanetCenter / planets.LowerBound(1);
        }

        public void TheyKilledOurShip(Empire they, Ship killedShip)
        {
            if (KillsForRemnantStory(they, killedShip))
                return;

            if (GetRelations(they, out Relationship rel))
                rel.LostAShip(killedShip);
        }

        public void WeKilledTheirShip(Empire they, Ship killedShip)
        {
            if (!GetRelations(they, out Relationship rel))
                return;
            rel.KilledAShip(killedShip);
        }

        public bool KillsForRemnantStory(Empire they, Ship killedShip)
        {
            if (!WeAreRemnants) 
                return false;

            if (GlobalStats.ActiveModInfo?.removeRemnantStory == true) 
                return false;

            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(killedShip);
            Remnants.IncrementKills(they, (int)killedExpSettings.KillExp);
            return true;
        }

        void AssignExplorationTasks()
        {
            if (isPlayer && !AutoExplore)
                return;

            int unexplored = UniverseScreen.SolarSystemList.Count(s => !s.IsExploredBy(this)).UpperBound(21);
            if (unexplored == 0)
            {
                for (int i = 0; i < OwnedShips.Count; i++)
                {
                    Ship ship = OwnedShips[i];
                    if (IsScout(ship) && ship.AI.State != AIState.Scrap)
                        ship.AI.OrderScrapShip();
                }
                return;
            }

            float desiredScouts = unexplored * Research.Strategy.ExpansionRatio;
            if (!isPlayer)
                desiredScouts *= ((int)CurrentGame.Difficulty).LowerBound(1);

            int numScouts = 0;
            for (int i = 0; i < OwnedShips.Count; i++)
            {
                Ship ship = OwnedShips[i];
                if (IsScout(ship))
                {
                    ship.DoExplore();
                    if (++numScouts >= desiredScouts)
                        return;
                }
            }

            // already building a scout? then just quit
            if (EmpireAI.HasGoal(GoalType.BuildScout))
                return;

            EmpireAI.Goals.Add(new BuildScout(this));


            // local 
            bool IsScout(Ship s)
            {
                if (s.shipData.Role == ShipData.RoleName.supply)
                    return false; // FB - this is a workaround, since supply shuttle register as scouts design role.

                return isPlayer && s.Name == data.CurrentAutoScout 
                       || !isPlayer && s.DesignRole == ShipData.RoleName.scout && s.fleet == null;
            }
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
            {
                using (OwnedProjectors.AcquireWriteLock())
                    OwnedProjectors.RemoveRef(ship);
            }
            else
            {
                using (OwnedShips.AcquireWriteLock())
                    OwnedShips.RemoveRef(ship);
            }

            ship.AI.ClearOrders();
            Pool.RemoveShipFromFleetAndPools(ship);
        }

        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if (rel.CanAttack && target is null)
                return true;
            
            return target?.IsAttackable(this, rel) ?? false;
        }

        public bool IsEmpireHostile(Empire targetEmpire)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);
            return rel.IsHostile;
        }

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.guid == planetGuid)
                    return p;
            return null;
        }

        public Planet FindPlanet(string planetName)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.Name == planetName)
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
            return new AO(this, center, radius);
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
        public bool FindNearestOwnedSystemTo(IEnumerable<SolarSystem> systems, out SolarSystem nearestSystem)
        {
            nearestSystem  = null;
            if (OwnedSolarSystems.Count == 0)
                return false; // We do not have any system owned, maybe we are defeated

            float distance = float.MaxValue;
            foreach(SolarSystem system in systems)
            {
                var nearest = OwnedSolarSystems.FindClosestTo(system);
                if (nearest == null) continue;
                float approxDistance = WeightedCenter.SqDist(nearest.Position);
                if (WeightedCenter.SqDist(nearest.Position) < distance)
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

        int ThreatMatrixUpdateTicks = ResetThreatMatrixTicks;
        const int ResetThreatMatrixTicks =5;
        
        public void UpdateContactsAndBorders(FixedSimTime timeStep)
        {
            updateContactsTimer -= timeStep.FixedTime;
            if (!IsEmpireDead())
            {
                MaxContactTimer = timeStep.FixedTime;
                ResetBorders();
                ScanFromAllInfluenceNodes(timeStep);
                PopulateKnownShips();
            }

            if (--ThreatMatrixUpdateTicks < 0)
            {
                Parallel.Run(()=> EmpireAI.ThreatMatrix.UpdateAllPins(this));
                ThreatMatrixUpdateTicks = ResetThreatMatrixTicks;
            }
        }

        public void PopulateKnownShips()
        {
            var currentlyKnown = new Array<Ship>();

            Array<Ship> ships = Universe.GetMasterShipList();

            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                bool shipKnown = ship != null
                            && (ship.loyalty == this || ship.KnownByEmpires.KnownBy(this));
                if (shipKnown)
                {
                    currentlyKnown.Add(ship);
                    if (ship.loyalty != this && !IsKnown(ship.loyalty))
                        DoFirstContact(ship.loyalty);
                }
            }
            KnownShips = new BatchRemovalCollection<Ship>(currentlyKnown);
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

        public void ChargeRushFees(float amount)
        {
            ChargeCredits(amount, rush: true);
        }

        void ChargeCredits(float cost, bool rush = false)
        {
            float creditsToCharge           = rush ? cost : ProductionCreditCost(cost);
            MoneySpendOnProductionThisTurn += creditsToCharge; 
            AddMoney(-creditsToCharge);
            //Log.Info($"Charging Credits from {Name}: {creditsToCharge}, Rush: {rush}"); // For testing
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

        public void RestoreDiplomacyConcatQueue(List<KeyValuePair<int, string>> diplomacyContactQueue)
        {
            if (diplomacyContactQueue != null)
                DiplomacyContactQueue = diplomacyContactQueue;
        }

        public class InfluenceNode
        {
            public Vector2 Position;
            public object SourceObject; // SolarSystem, Planet OR Ship
            public bool DrewThisTurn;
            public float Radius;
            public bool KnownToPlayer;
            public GameplayObject GameObject;

            public void Wipe()
            {
                Position      = Vector2.Zero;
                SourceObject  = null;
                DrewThisTurn  = false;
                Radius        = 0;
                KnownToPlayer = false;
            }
        }

        public void RestoreUnserializableDataFromSave()
        {
            //restore relationShipData
            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                rel.RestoreWarsFromSave();
            }

            EmpireAI.EmpireDefense?.RestoreFromSave(true);
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
