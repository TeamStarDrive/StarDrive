using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Empires.Components;
using Ship_Game.Empires.ShipPools;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Fleets;
using Ship_Game.Universe;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

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

    [StarDataType]
    public sealed partial class Empire : IDisposable, IEmpireShipLists
    {
        [StarData] readonly Map<int, Fleet> FleetsDict;
        [StarData] public Map<string, TechEntry> TechnologyDict;

        [StarData] public readonly Map<string, bool> UnlockedHullsDict;
        [StarData] readonly Map<string, bool> UnlockedTroopDict;
        [StarData] readonly Map<string, bool> UnlockedBuildingsDict;
        [StarData] readonly Map<string, bool> UnlockedModulesDict;

        [StarData] readonly Array<Troop> UnlockedTroops;

        /// <summary>
        /// Returns an average of empire money over several turns.
        /// </summary>
        [StarData] public float NormalizedMoney { get; private set; }

        public void UpdateNormalizedMoney(float money, bool fromSave = false)
        {
            const float rate = 0.1f;
            if (fromSave || NormalizedMoney == 0f)
                NormalizedMoney = money;
            else // simple moving average:
                NormalizedMoney = NormalizedMoney*(1f-rate) + money*rate;
        }

        [StarData] public Array<Ship> Inhibitors;
        [StarData] public Array<SpaceRoad> SpaceRoadsList;

        public const float StartingMoney = 1000f;
        float MoneyValue = StartingMoney;
        [StarData] public float Money
        {
            get => MoneyValue;
            set => MoneyValue = value.NaNChecked(0f, "Empire.Money");
        }

        [StarData] readonly Array<Planet> OwnedPlanets;
        [StarData] readonly Array<SolarSystem> OwnedSolarSystems;
        public IReadOnlyList<Ship> OwnedShips => EmpireShips.OwnedShips;
        public IReadOnlyList<Ship> OwnedProjectors => EmpireShips.OwnedProjectors;

        readonly Map<SolarSystem, bool> HostilesLogged = new(); // Only for Player warnings
        public Array<IncomingThreat> SystemsWithThreat = new();
        [StarData] public HashSet<string> ShipsWeCanBuild;
        // shipyards, platforms, SSP-s
        [StarData] public HashSet<string> SpaceStationsWeCanBuild;
        float FleetUpdateTimer = 5f;
        int TurnCount = 1;

        [StarData] public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName => data.PortraitName;
        public bool ScanComplete = true;
        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesn't normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        [StarData] public bool IsFaction;

        // For Pirate Factions. This will allow the Empire to be pirates
        [StarData] public Pirates Pirates { get; private set; }

        // For Remnants Factions. This will allow the Empire to be Remnants
        [StarData] public Remnants Remnants { get; private set; }

        [StarData] public Color EmpireColor;

        [StarData] public UniverseState Universum; // Alias for static Empire.Universum

        [StarData] public EmpireAI AI;

        float UpdateTimer;
        [StarData] public bool isPlayer;
        public float TotalShipMaintenance { get; private set; }
        public float TotalWarShipMaintenance { get; private set; }
        public float TotalCivShipMaintenance { get; private set; }
        public float TotalEmpireSupportMaintenance { get; private set; }
        public float TotalOrbitalMaintenance { get; private set; }
        public float TotalMaintenanceInScrap { get; private set; }
        public float TotalTroopShipMaintenance { get; private set; }

        private bool HostilesDictForPlayerInitialized;
        public float NetPlanetIncomes { get; private set; }
        public float TroopCostOnPlanets { get; private set; } // Maintenance in all Owned planets
        public float TroopInSpaceFoodNeeds { get; private set; }
        public float TotalFoodPerColonist { get; private set; }
        public float GrossPlanetIncome { get; private set; }
        public float PotentialIncome { get; private set; }
        public float ExcessGoodsMoneyAddedThisTurn { get; private set; } // money tax from excess goods
        public float MoneyLastTurn;
        public int AllTimeTradeIncome;
        [StarData] public bool AutoBuild;
        [StarData] public bool AutoExplore;
        [StarData] public bool AutoColonize;
        [StarData] public bool AutoResearch;
        public int TotalScore;
        public float TechScore;
        public float ExpansionScore;
        public float MilitaryScore;
        public float IndustrialScore;

        // This is the original capital of the empire. It is used in capital elimination and 
        // is used in capital elimination and also to determine if another capital will be moved here if the
        // empire retakes this planet. This value should never be changed after it was set.
        [StarData] public Planet Capital { get; private set; } 

        public int EmpireShipCountReserve;
        public int empireShipTotal;
        public int empireShipCombat;    //fbedard
        public int empirePlanetCombat;  //fbedard
        public bool canBuildCapitals;
        public bool CanBuildBattleships;
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
        public ShipPool AIManagedShips;
        [StarData] public LoyaltyLists EmpireShips;
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue { get; private set; }
        public float TotalColonyValues { get; private set; }
        public float TotalColonyPotentialValues { get; private set; }
        public Ship BestPlatformWeCanBuild { get; private set; }
        public Ship BestStationWeCanBuild { get; private set; }
        public HashSet<string> ShipTechs = new();
        [StarData] public Vector2 WeightedCenter;
        [StarData] public bool RushAllConstruction;

        [StarDataType]
        public class DiplomacyQueueItem
        {
            [StarData] public int EmpireId;
            [StarData] public string Dialog;
        }

        // Empire IDs, for player only
        [StarData] public Array<DiplomacyQueueItem> DiplomacyContactQueue { get; private set; }
        [StarData] public bool AutoPickBestColonizer;
        [StarData] public Array<string> ObsoletePlayerShipModules;

        public int AtWarCount;
        public Planet[] RallyPoints = Empty<Planet>.Array;

        public const string DefaultBoardingShuttleName = "Assault Shuttle";
        public const string DefaultSupplyShuttleName   = "Supply Shuttle";
        public Ship BoardingShuttle => ResourceManager.GetShipTemplate(DefaultBoardingShuttleName, false);
        public Ship SupplyShuttle   => ResourceManager.GetShipTemplate(DefaultSupplyShuttleName);
        public bool IsCybernetic  => data.Traits.Cybernetic != 0;
        public bool NonCybernetic => data.Traits.Cybernetic == 0;
        public bool WeArePirates  => Pirates != null; // Use this to figure out if this empire is pirate faction
        public bool WeAreRemnants => Remnants != null; // Use this to figure out if this empire is pirate faction

        public float MaximumIncome       => PotentialIncome + TotalTradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus; // + AverageTradeIncome + data.FlatMoneyBonus;
        public float MaximumStableIncome => PotentialIncome + AverageTradeIncome + data.FlatMoneyBonus;
        // Income this turn before deducting ship maintenance
        public float GrossIncome                 => GrossPlanetIncome + TotalTradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus;
        public float NetIncome                   => GrossIncome - AllSpending;
        public float TotalBuildingMaintenance    =>  GrossPlanetIncome - (NetPlanetIncomes + TroopCostOnPlanets);
        public float BuildingAndShipMaint        => TotalBuildingMaintenance + TotalShipMaintenance;
        public float AllSpending                 => BuildingAndShipMaint + MoneySpendOnProductionThisTurn + TroopCostOnPlanets;
        public bool IsExpansionists              => data.EconomicPersonality?.Name == "Expansionists";
        public bool IsIndustrialists             => data.EconomicPersonality?.Name == "Industrialists";
        public bool IsGeneralists                => data.EconomicPersonality?.Name == "Generalists";
        public bool IsMilitarists                => data.EconomicPersonality?.Name == "Militarists";
        public bool IsTechnologists              => data.EconomicPersonality?.Name == "Technologists";

        public Planet[] SpacePorts       => OwnedPlanets.Filter(p => p.HasSpacePort);
        public Planet[] MilitaryOutposts => OwnedPlanets.Filter(p => p.AllowInfantry); // Capitals allow Infantry as well
        public Planet[] SafeSpacePorts   => OwnedPlanets.Filter(p => p.HasSpacePort && p.Safe);

        public float MoneySpendOnProductionThisTurn { get; private set; }

        [StarData] public readonly EmpireResearch Research;
        public float TotalPopBillion { get; private set; }
        public float MaxPopBillion { get; private set; }
        public DifficultyModifiers DifficultyModifiers { get; private set; }
        public PersonalityModifiers PersonalityModifiers { get; private set; }
        // Empire unique ID. If this is 0, then this empire is invalid!
        // Set in EmpireManager.cs
        public int Id;

        public string Name => data.Traits.Name;

        public void AddShipToManagedPools(Ship s)
        {
            AIManagedShips.Add(s);
        }

        Empire() { }

        public Empire(UniverseState us)
        {
            Universum = us;
            Research = new(this);

            AIManagedShips = new(us?.CreateId() ?? -1, this, "AIManagedShips");
            EmpireShips = new(this);

            FleetsDict = new();
            TechnologyDict = new();
            UnlockedHullsDict = new();
            UnlockedTroopDict = new();
            UnlockedBuildingsDict = new();
            UnlockedModulesDict = new();
            UnlockedTroops = new();

            Inhibitors = new();
            SpaceRoadsList = new();
            OwnedPlanets = new();
            OwnedSolarSystems = new();

            ShipsWeCanBuild = new();
            SpaceStationsWeCanBuild = new();

            DiplomacyContactQueue = new();
             ObsoletePlayerShipModules = new();
        }

        public Empire(UniverseState us, Empire parentEmpire) : this(us)
        {
            TechnologyDict = parentEmpire.TechnologyDict;
        }

        [StarDataDeserialized(typeof(TechEntry), typeof(EmpireData))]
        void OnDeserialized()
        {
            AIManagedShips = new(Universum.CreateId(), this, "AIManagedShips");
            dd = ResourceManager.GetDiplomacyDialog(data.DiplomacyDialogPath);

            EmpireManager.Add(this); // TODO: remove

            CommonInitialize();
        }

        public float GetProjectorRadius()
        {
            return Universum.DefaultProjectorRadius * data.SensorModifier;
        }

        public void SetAsPirates(EmpireAI ai)
        {
            if (data.Defeated)
                return;

            if (GlobalStats.DisablePirates)
            {
                data.Defeated = true;
            }
            else
            {
                Pirates = new Pirates(this, ai);
            }

        }

        public void SetAsRemnants(EmpireAI ai)
        {
            Remnants = new Remnants(this, ai);
        }

        public void AddMoney(float moneyDiff)
        {
            Money += moneyDiff;
        }

        public void TriggerAllShipStatusUpdate()
        {
            foreach (Ship ship in OwnedShips) //@todo can make a global ship unlock flag.
                ship.ShipStatusChanged = true;
        }

        public Planet FindNearestRallyPoint(Vector2 location)
        {
            return  RallyPoints.FindMinFiltered(p=> p.Owner == this, p => p.Position.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Position.SqDist(location));
        }

        public Planet FindNearestSafeRallyPoint(Vector2 location)
        {
            return SafeSpacePorts.FindMin(p => p.Position.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Position.SqDist(location));
        }

        public Planet RallyShipYardNearestTo(Vector2 position)
        {
            return RallyPoints.FindMinFiltered(p => p.HasSpacePort,
                                               p => position.SqDist(p.Position))
                ?? SpacePorts.FindMin(p => position.SqDist(p.Position))
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

        // this will get the name of an Assault Shuttle if defined in race.xml or use default one
        public string GetAssaultShuttleName()
        {
            if (data.DefaultAssaultShuttle.NotEmpty() &&
                ResourceManager.ShipTemplateExists(data.DefaultAssaultShuttle))
            {
                return data.DefaultAssaultShuttle;
            }
            return BoardingShuttle.Name;
        }

        // this will get the name of a Supply Shuttle if defined in race.xml or use default one
        public string GetSupplyShuttleName()
        {
            if (data.DefaultSupplyShuttle.NotEmpty() && 
                ResourceManager.ShipTemplateExists(data.DefaultSupplyShuttle))
            {
                return data.DefaultSupplyShuttle;
            }
            return DefaultSupplyShuttleName;
        }

        public bool FindClosestSpacePort(Vector2 position, out Planet closest)
        {
            closest = SpacePorts.FindMin(p => p.Position.SqDist(position));
            return closest != null;
        }

        /// <summary>
        /// checks planets for shortest time to build.
        /// port quality says how average a port can be.
        /// 1 is above average. 0.2 is below average.
        /// the default is below average. not recommended to set above 1 but you can.
        /// </summary>
        public bool FindPlanetToBuildShipAt(IReadOnlyList<Planet> ports, IShipDesign ship, out Planet chosen, float priority = 1f)
        {
            if (ports.Count != 0)
            {
                float cost = ship.GetCost(this);
                chosen     = FindPlanetToBuildAt(ports, cost, ship, priority);
                return chosen != null;
            }

            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {ship} at! Candidates:{ports.Count}");
            chosen = null;
            return false;
        }

        public bool FindPlanetToBuildTroopAt(IReadOnlyList<Planet> ports, Troop troop, float priority, out Planet chosen)
        {
            chosen = null;
            if (ports.Count != 0)
            {
                float cost = troop.ActualCost;
                chosen     = FindPlanetToBuildAt(ports, cost, sData: null, priority);
                return chosen != null;
            }

            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {troop} at! Candidates:{ports.Count}");
            return false;
        }

        public bool FindPlanetToSabotage(IReadOnlyList<Planet> ports, out Planet chosen)
        {
            chosen = null;
            if (ports.Count != 0)
            {
                chosen = ports.FindMax(p => p.Prod.NetMaxPotential);
                return true;
            }

            return false;
        }

        Planet FindPlanetToBuildAt(IReadOnlyList<Planet> ports, float cost, IShipDesign sData, float priority = 1f)
        {
            // focus on the best producing planets (number depends on the empire size)
            if (GetBestPorts(ports, out Planet[] bestPorts))
                return bestPorts.FindMin(p => p.TurnsUntilQueueComplete(cost, priority, sData));

            return null;
        }

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, Ship ship, IShipDesign newShip, bool travelBack, out Planet planet)
        {
            planet = null;
            int travelMultiplier = travelBack ? 2 : 1;

            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip)
                                        + ship.GetAstrogateTimeTo(p) * travelMultiplier);

            return planet != null;
        }

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, IShipDesign newShip, out Planet planet)
        {
            planet = null;
            if (ports.Count == 0)
                return false;

            ports  = ports.Filter(p => !p.IsCrippled);
            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip));
            return planet != null;
        }

        public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding(float portQuality) => GetBestPortsForShipBuilding(OwnedPlanets, portQuality);
        public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding(IReadOnlyList<Planet> ports, float portQuality)
        {
            if (ports == null) return Empty<Planet>.Array;
            GetBestPorts(ports, out Planet[] bestPorts, portQuality);
            return bestPorts?.Filter(p => p.HasSpacePort) ?? Empty<Planet>.Array;
        }

        bool GetBestPorts(IReadOnlyList<Planet> ports, out Planet[] bestPorts, float portQuality = 0.2f)
        {
            bestPorts = null;
            // If all the ports are research colonies, do not filter them
            bool filterResearchPorts = ports.Any(p => p.CType != Planet.ColonyType.Research);
            if (ports.Count > 0)
            {
                float averageMaxProd = ports.Average(p => p.Prod.NetMaxPotential);
                bestPorts            = ports.Filter(p => !p.IsCrippled
                                                         && (p.CType != Planet.ColonyType.Research || !filterResearchPorts)
                                                         && p.Prod.NetMaxPotential.GreaterOrEqual(averageMaxProd * portQuality));
            }

            return bestPorts?.Length > 0;
        }

        public Planet GetOrbitPlanetAfterBuild(Planet builtAt)
        {
            if (GetBestPorts(SafeSpacePorts, out Planet[] bestPorts) && !bestPorts.Contains(builtAt))
            {
                return bestPorts.Sorted(p => p.Position.Distance(builtAt.Position)).First();
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
                planet = FindNearestRallyPoint(ship.Position);
                if (planet == null || planet.Position.Distance(ship.Position) > 50000)
                    ship.ScuttleTimer = 5;

                return planet != null;
            }

            var scrapGoals = AI.FindGoals(g => g.Type == GoalType.ScrapShip);
            var potentialPlanets = OwnedPlanets.SortedDescending(p => p.MissingProdHereForScrap(scrapGoals)).TakeItems(5);
            if (potentialPlanets.Length == 0)
                return false;

            planet = potentialPlanets.FindMin(p => p.Position.Distance(ship.Position));
            return planet != null;
        }

        public float KnownEnemyStrengthIn(SolarSystem system, Predicate<ThreatMatrix.Pin> filter)
                     => AI.ThreatMatrix.GetStrengthInSystem(system, filter);

        public float KnownEnemyStrengthIn(SolarSystem system)
             => AI.ThreatMatrix.GetStrengthInSystem(system, p=> IsEmpireHostile(p.GetEmpire()));

        public float KnownEnemyStrengthIn(AO ao)
             => AI.ThreatMatrix.PingHostileStr(ao.Center, ao.Radius, this);

        public float KnownEmpireStrength(Empire empire) => AI.ThreatMatrix.KnownEmpireStrength(empire, p => p != null);
        public float KnownEmpireOffensiveStrength(Empire empire)
            => AI.ThreatMatrix.KnownEmpireStrength(empire, p => p != null && p.Ship?.IsPlatformOrStation == false);

        public WeaponTagModifier WeaponBonuses(WeaponTag which) => data.WeaponTags[which];
        public Map<int, Fleet> GetFleetsDict() => FleetsDict;
        public Fleet GetFleetOrNull(int key) => FleetsDict.TryGetValue(key, out Fleet fleet) ? fleet : null;
        public Fleet GetFleet(int key) => FleetsDict[key];

        public float TotalFactionsStrTryingToClear()
        {
            var claimTasks = AI.GetClaimTasks();
            float str = 0;
            for (int i = 0; i < claimTasks.Length; ++i)
            {
                var task = claimTasks[i];
                if (task.TargetPlanet.Owner == null) // indicates its remnant infested and not another empire
                    str += task.MinimumTaskForceStrength;
            }

            var assaultPirateTasks = AI.GetAssaultPirateTasks();
            if (assaultPirateTasks.Length > 0)
                str += assaultPirateTasks.Sum(t => t.MinimumTaskForceStrength);

            return str;
        }

        public int GetTypicalTroopStrength()
        {
            IReadOnlyList<Troop> unlockedTroops = GetUnlockedTroops();
            float str = unlockedTroops.Max(troop => troop.StrengthMax);
            str      *= 1 + data.Traits.GroundCombatModifier;
            return (int)str.LowerBound(1);
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
                    rallyPlanets.Add(Universum.Planets[0]);

                RallyPoints = rallyPlanets.ToArray();
                RallyPoints.Sort(rp => rp.ParentSystem.OwnerList.Count > 1);
                return;
            }

            rallyPlanets = new Array<Planet>();
            for (int i =0; i < OwnedPlanets.Count; i++)
            {
                Planet planet = OwnedPlanets[i];
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
            var nearAO = AI.AreasOfOperations
                .FilterSelect(ao => ao.CoreWorld?.ParentSystem.OwnerList.Count ==1,
                              ao => ao.CoreWorld);
            return nearAO;
        }

        public Planet[] GetAOCoreWorlds() => AI.AreasOfOperations.Select(ao => ao.CoreWorld);

        public AO GetAOFromCoreWorld(Planet coreWorld)
        {
            return AI.AreasOfOperations.Find(a => a.CoreWorld == coreWorld);
        }
        public Planet[] GetSafeAOWorlds()
        {
            var safeWorlds = new Array<Planet>();
            for (int i = 0; i < AI.AreasOfOperations.Count; i++)
            {
                var ao      = AI.AreasOfOperations[i];
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
            var coreWorld = ship.Loyalty.GetSafeAOCoreWorlds()?.FindClosestTo(ship);
            Planet home = null;

            if (coreWorld != null)
            {
                home = coreWorld;
            }
            else
            {
                home = ship.Loyalty.GetSafeAOWorlds().FindClosestTo(ship);
            }

            if (home == null)
            {
                var nearestAO = ship.Loyalty.AI.FindClosestAOTo(ship.Position);
                home = nearestAO.GetOurPlanets().FindClosestTo(ship);
            }

            if (home == null)
            {
                home = Universum.Planets.FindMin(p => p.Position.Distance(ship.Position));
            }

            return home;
        }

        Array<Planet> GetPlanetsNearStations()
        {
            var planets = new Array<Planet>();
            foreach(var station in OwnedShips)
            {
                if (station.BaseHull.Role != RoleName.station
                    && station.BaseHull.Role != RoleName.platform) continue;
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
            foreach(var s in Universum.Systems)
            {
                if (s.OwnerList.Count > 0) continue;
                if (s.PlanetList.Count == 0) continue;
                return s.PlanetList[0];
            }
            return null;
        }

        public int CreateFleetKey()
        {
            int key = 1;
            while (AI.UsedFleets.Contains(key))
                ++key;
            AI.UsedFleets.Add(key);
            return key;
        }

        public void SetAsDefeated()
        {
            if (data.Defeated)
                return;

            data.Defeated = true;
            ClearInfluenceList();
            foreach (SolarSystem solarSystem in Universum.Systems)
                solarSystem.OwnerList.Remove(this);

            if (IsFaction)
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
            AI.ClearGoals();
            AI.EndAllTasks();
            foreach (var kv in FleetsDict)
                kv.Value.Reset();

            Empire rebels = EmpireManager.CreateRebelsFromEmpireData(data, this);
            Universum.Stats.UpdateEmpire(Universum.StarDate, rebels);

            foreach (Ship s in OwnedShips)
            {
                s.LoyaltyChangeFromBoarding(rebels, false);
            }
            AIManagedShips.Clear();
            EmpireShips.Clear();
            data.AgentList.Clear();
        }

        public void SetAsMerged()
        {
            if (data.Defeated)
                return;

            data.Defeated = true;
            ClearInfluenceList();
            foreach (SolarSystem solarSystem in Universum.Systems)
                solarSystem.OwnerList.Remove(this);

            if (IsFaction)
                return;

            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                BreakAllTreatiesWith(them, includingPeace: true);
            }

            AI.ClearGoals();
            AI.EndAllTasks();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            AIManagedShips.Clear();
            EmpireShips.Clear();
            data.AgentList.Clear();
        }

        public string[] GetUnlockedHulls() => UnlockedHullsDict.FilterSelect((hull,unlocked) => unlocked,
                                                                             (hull,unlocked) => hull);
        public bool IsHullUnlocked(string hullName) => UnlockedHullsDict.Get(hullName, out bool unlocked) && unlocked;

        public IReadOnlyList<Troop> GetUnlockedTroops() => UnlockedTroops;

        public bool IsBuildingUnlocked(string name) => UnlockedBuildingsDict.TryGetValue(name, out bool unlocked) && unlocked;
        public bool IsBuildingUnlocked(int bid) => ResourceManager.GetBuilding(bid, out Building b)
                                                        && IsBuildingUnlocked(b.Name);

        public IEnumerable<Building> GetUnlockedBuildings()
        {
            foreach (KeyValuePair<string, bool> kv in UnlockedBuildingsDict)
                if (kv.Value && ResourceManager.GetBuilding(kv.Key, out Building b))
                    yield return b;
        }

        public bool CanTerraformVolcanoes   => IsBuildingUnlocked(Building.TerraformerId) && data.Traits.TerraformingLevel >= 1;
        public bool CanTerraformPlanetTiles => IsBuildingUnlocked(Building.TerraformerId) && data.Traits.TerraformingLevel >= 2;
        public bool CanFullTerraformPlanets => IsBuildingUnlocked(Building.TerraformerId) && data.Traits.TerraformingLevel >= 3;

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

        public Array<TechEntry> TechsAvailableForTrade()
        {
            var tradeTechs = new Array<TechEntry>();
            foreach (TechEntry entry in TechnologyDict.Values)
            {
                if (entry.Unlocked && !entry.IsMultiLevel) // FB: Multi level techs trade will not work well for now
                    tradeTechs.Add(entry);
            }
            return tradeTechs;
        }

        public Array<TechEntry> CurrentTechsResearchable()
        {
            var availableTechs = new Array<TechEntry>();

            foreach (TechEntry tech in TechEntries)
            {
                if (tech.CanBeResearched
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

        public int TechCost(Ship ship)       => TechCost(ship.ShipData.TechsNeeded.Except(ShipTechs));
        public bool HasTechEntry(string uid) => TechnologyDict.ContainsKey(uid);

        /// <summary>
        /// this appears to be broken.
        /// </summary>
        public IReadOnlyList<SolarSystem> GetOwnedSystems() => OwnedSolarSystems;
        public IReadOnlyList<Planet> GetPlanets()           => OwnedPlanets;
        public int NumPlanets                               => OwnedPlanets.Count;
        public int NumSystems                               => OwnedSolarSystems.Count;

        public int GetTotalPlanetsWarValue() => (int)OwnedPlanets.Sum(p => p.ColonyWarValueTo(this));

        public Array<SolarSystem> GetOurBorderSystemsTo(Empire them, bool hideUnexplored, float percentageOfMapSize = 0.5f)
        {
            Vector2 theirCenter = them.WeightedCenter;
            Vector2 ownCenter   = WeightedCenter;
            var directionToThem = ownCenter.DirectionToTarget(theirCenter);
            float midDistance   = ownCenter.Distance(theirCenter) / 2;
            Vector2 midPoint    = directionToThem * midDistance;
            float mapDistance   = (Universum.Size * percentageOfMapSize).UpperBound(ownCenter.Distance(theirCenter) * 1.2f);

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
            //UpdateWarRallyPlanetsLostPlanet(planet, attacker);
        }

        public void RemovePlanet(Planet planet)
        {
            OwnedPlanets.Remove(planet);
            Universum.OnPlanetOwnerRemoved(this, planet);

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
            // UpdateWarRallyPlanetsWonPlanet(planet, loser);
        }

        public void AddPlanet(Planet planet)
        {
            if (planet == null)
                throw new ArgumentNullException(nameof(planet));

            if (planet.ParentSystem == null)
                throw new ArgumentNullException(nameof(planet.ParentSystem));

            OwnedPlanets.Add(planet);
            Universum.OnPlanetOwnerAdded(this, planet);

            OwnedSolarSystems.AddUniqueRef(planet.ParentSystem);
            CalcWeightedCenter(calcNow: true);
        }

        public void TrySendInitialFleets(Planet p)
        {
            if (isPlayer)
                return;

            if (p.EventsOnTiles())
                AI.SendExplorationFleet(p);

            if (Universum.Difficulty <= GameDifficulty.Hard || p.ParentSystem.IsExclusivelyOwnedBy(this))
                return;

            if (PlanetRanker.IsGoodValueForUs(p, this) && KnownEnemyStrengthIn(p.ParentSystem).AlmostZero())
            {
                var task = MilitaryTask.CreateGuardTask(this, p);
                AI.AddPendingTask(task);
            }
        }

        // pr to protect ship list
        /// <summary>
        /// WARNING. Use this list ONLY for manipulating the live empire ship list.
        /// Use GetShipsAtomic() in all other cases such as UI use.
        /// </summary>
        //public IReadOnlyList<Ship> GetShips() => OwnedShips;
        //public Ship[] GetShipsAtomic() => OwnedShips.ToArray();

        public Array<Ship> AllFleetReadyShips()
        {
            //Get all available ships from AO's
            var ships = isPlayer ? new Array<Ship>(OwnedShips)
                                 : AIManagedShips.GetShipsFromOffensePools();

            var readyShips = new Array<Ship>();
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship == null || ship.Fleet != null)
                    continue;

                if (ship.AI.State == AIState.Resupply
                    || ship.AI.State == AIState.ResupplyEscort
                    || ship.AI.State == AIState.Refit
                    || ship.AI.State == AIState.Scrap
                    || ship.AI.State == AIState.Scuttle
                    || ship.IsPlatformOrStation)
                {
                    continue;
                }

                readyShips.Add(ship);
            }

            return readyShips;
        }

        public IReadOnlyList<Ship> GetProjectors() => OwnedProjectors;

        void IEmpireShipLists.AddNewShipAtEndOfTurn(Ship s) => EmpireShips.Add(s);

        void InitDifficultyModifiers()
        {
            DifficultyModifiers = new DifficultyModifiers(this, Universum.Difficulty);
        }

        void InitPersonalityModifiers()
        {
            PersonalityModifiers = new PersonalityModifiers(Personality);
        }

        public void TestInitModifiers() // For UnitTests only
        {
            InitDifficultyModifiers();
            //InitPersonalityModifiers(); // TODO: crashes in tests
        }

        void CommonInitialize()
        {
            KnownEmpires.Set(Id); // we know ourselves
            InitDifficultyModifiers();
            InitPersonalityModifiers();
            CreateThrusterColors();

            if (FleetsDict.Count == 0)
            {
                for (int i = 1; i < 10; ++i)
                {
                    Fleet fleet = new Fleet(Universum.CreateId()) { Owner = this };
                    fleet.SetNameByFleetIndex(i);
                    FleetsDict.Add(i, fleet);
                }
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            Research.Initialize();
            InitEmpireUnlocks();
        }

        // initializes an empire
        public void Initialize()
        {
            CreateEmpireTechTree(); // first-time init of the entire tech tree
            CommonInitialize();

            data.TechDelayTime = 0;
            if (EmpireManager.NumEmpires == 0)
                UpdateTimer = 0;

            AI = new(this);
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

        public void ResetTechsAndUnlocks()
        {
            Array<Ship> ourShips = GetOurFactionShips();

            foreach (TechEntry entry in TechnologyDict.Values)
            {
                var tech = entry.Tech;
                bool modulesNotHulls = tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0;
                if (modulesNotHulls && !WeCanUseThisInDesigns(entry, ourShips))
                    entry.shipDesignsCanuseThis = false;
            }

            foreach (TechEntry entry in TechnologyDict.Values)
            {
                if (!entry.shipDesignsCanuseThis)
                    entry.shipDesignsCanuseThis = WeCanUseThisLater(entry);
            }

            foreach (TechEntry entry in TechnologyDict.Values)
            {
                AddToShipTechLists(entry);
            }

            // now unlock the techs again to populate lists
            var unlockedEntries = TechnologyDict.Values.Filter(e => e.Unlocked);
            foreach (TechEntry entry in unlockedEntries)
            {
                entry.UnlockFromSave(this, unlockBonuses: false);
            }
        }

        void CreateEmpireTechTree()
        {
            foreach (Technology tech in ResourceManager.TechTree.Values)
            {
                var entry = new TechEntry(tech.UID);
                if (entry.IsHidden(this))
                {
                    entry.SetDiscovered(false);
                }
                else
                {
                    bool secret = tech.Secret || (tech.ComesFrom.Count == 0 && !tech.IsRootNode);
                    if (tech.IsRootNode && !secret)
                        entry.ForceFullyResearched();
                    else
                        entry.ForceNeedsFullResearch();
                    entry.SetDiscovered(!secret);
                }

                if (IsFaction || data.Traits.Prewarp == 1)
                    entry.ForceNeedsFullResearch();
                TechnologyDict.Add(tech.UID, entry);
            }
        }

        void ResetUnlocks()
        {
            UnlockedBuildingsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedTroopDict.Clear();
            UnlockedTroops.Clear();
            ShipsWeCanBuild.Clear();
        }

        void InitEmpireUnlocks()
        {
            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;
            
            foreach (string ship in data.unlockShips)
                ShipsWeCanBuild.Add(ship);

            foreach (var kv in TechnologyDict) // unlock racial techs
            {
                TechEntry techEntry = kv.Value;
                if (techEntry.Discovered)
                    data.Traits.UnlockAtGameStart(techEntry, this);
            }

            // Added by gremlin Figure out techs with modules that we have ships for.
            ResetTechsAndUnlocks();
            UpdateShipsWeCanBuild();

            foreach (Troop t in UnlockedTroops)
                UnlockEmpireTroop(t.Name);

            if (!IsFaction)
            {
                if (UnlockedBuildingsDict.Count == 0) Log.Error($"Empire UnlockedBuildingsDict is empty! {this}");
                if (UnlockedModulesDict.Count == 0) Log.Error($"Empire UnlockedModulesDict is empty! {this}");
                if (UnlockedHullsDict.Count == 0) Log.Error($"Empire UnlockedHullsDict is empty! {this}");
                if (UnlockedTroopDict.Count == 0) Log.Error($"Empire UnlockedTroopDict is empty! {this}");
                if (UnlockedTroops.Count == 0) Log.Error($"Empire UnlockedTroops is empty! {this}");
                if (ShipsWeCanBuild.Count == 0) Log.Error($"Empire ShipsWeCanBuild is empty! {this}");
                if (SpaceStationsWeCanBuild.Count == 0) Log.Error($"Empire ShipsWeCanBuild is empty! {this}");
            }
        }

        void AddToShipTechLists(TechEntry tech)
        {
            if (tech.Unlocked)
            {
                Array<Technology.UnlockedMod> mods = tech.GetUnlockableModules(this);
                if (mods.Count > 0)
                    ShipTechs.Add(tech.UID);
            }
        }

        public Array<Ship> GetOurFactionShips()
        {
            var ourFactionShips = new Array<Ship>();
            foreach (Ship template in ResourceManager.ShipTemplates)
            {
                if (ShipStyleMatch(template.ShipData.ShipStyle)
                    || template.ShipData.ShipStyle == "Platforms" || template.ShipData.ShipStyle == "Misc")
                {
                    ourFactionShips.Add(template);
                }
            }
            return ourFactionShips;
        }

        public bool ShipStyleMatch(string shipStyle)
        {
            if (shipStyle == data.Traits.ShipType)
                return true;

            foreach (Empire empire in EmpireManager.MajorEmpires)
            {
                if (empire.data.AbsorbedBy == data.Traits.Name && shipStyle == empire.data.Traits.ShipType)
                    return true;
            }

            return false;
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

        public string[] GetTroopsWeCanBuild() => UnlockedTroopDict.FilterSelect((k,v) => v, (k,v) => k);

        public bool WeCanBuildTroop(string id) => UnlockedTroopDict.TryGetValue(id, out bool canBuild) && canBuild;

        public void UnlockEmpireShipModule(string moduleUID)
        {
            UnlockedModulesDict[moduleUID] = true;
        }

        public void UnlockEmpireHull(string hullName, string techUID = "")
        {
            UnlockedHullsDict[hullName] = true;
            ShipTechs.Add(techUID);
        }

        public void UnlockEmpireTroop(string troopName)
        {
            UnlockedTroopDict[troopName] = true;

            var template = ResourceManager.GetTroopTemplate(troopName);

            for (int i = 0; i < UnlockedTroops.Count; ++i)
            {
                if (UnlockedTroops[i].Name == template.Name)
                {
                    UnlockedTroops[i] = template; // update existing template
                    return;
                }
            }

            // if it didn't exist, add it
            UnlockedTroops.Add(template);
        }

        public void UnlockEmpireBuilding(string buildingName) => UnlockedBuildingsDict[buildingName] = true;

        public void SetEmpireTechDiscovered(string techUID)
        {
            TechEntry tech = GetTechEntry(techUID);
            if (tech != TechEntry.None)
                tech.SetDiscovered(this);
        }

        public void IncreaseEmpireShipRoleLevel(RoleName role, int bonus)
        {
            IncreaseEmpireShipRoleLevel(new[] { role }, bonus);
        }

        public void IncreaseEmpireShipRoleLevel(RoleName[] role, int bonus)
        {
            for (int i = 0; i < OwnedShips.Count; i++)
            {
                Ship ship = OwnedShips[i];
                if (role.Contains(ship.ShipData.Role))
                    ship.AddToShipLevel(bonus);
            }
        }

        public void UnlockTech(string techId, TechUnlockType techUnlockType)
        {
            TechEntry techEntry = GetTechEntry(techId);
            UnlockTech(techEntry, techUnlockType, null);
        }

        public void UnlockTech(string techId, TechUnlockType techUnlockType, Empire otherEmpire)
        {
            TechEntry techEntry = GetTechEntry(techId);
            UnlockTech(techEntry, techUnlockType, otherEmpire);
        }

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
            AI.TriggerRefit();
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
                    Empire checkedEmpire = ship.Loyalty;

                    if (ignoreList.Contains(checkedEmpire)
                        || ship.NotThreatToPlayer()
                        || !ship.InSensorRange)
                    {
                        ignoreList.Add(checkedEmpire);
                        continue;
                    }

                    float strRatio = StrRatio(system, checkedEmpire);
                    Universum.Notifications.AddBeingInvadedNotification(system, checkedEmpire, strRatio);
                    HostilesLogged[system] = true;
                    break;
                }
            }

            float StrRatio(SolarSystem system, Empire hostiles)
            {
                float hostileOffense  = system.ShipList.Filter(s => s.Loyalty == hostiles).Sum(s => s.BaseStrength);
                var ourPlanetsOffense = system.PlanetList.Filter(p => p.Owner == this).Sum(p => p.BuildingGeodeticOffense);
                var ourSpaceAssets    = system.ShipList.Filter(s => s.Loyalty == this);
                float ourSpaceOffense = 0;

                if (ourSpaceAssets.Length > 0)
                    ourSpaceOffense += ourSpaceAssets.Sum(s => s.BaseStrength);

                float ourOffense = (ourSpaceOffense + ourPlanetsOffense).LowerBound(1);
                return hostileOffense / ourOffense;
            }
        }

        void ScanFromAllSensorPlanets()
        {
            InfluenceNode[] sensorNodes = SensorNodes;
            for (int i = 0; i < sensorNodes.Length; i++)
            {
                ref InfluenceNode node = ref sensorNodes[i];
                if (node.Source is Planet)
                    ScanForShipsFromPlanet(node.Position, node.Radius);
            }
        }

        void ScanForShipsFromPlanet(Vector2 pos, float radius)
        {
            // find ships in radius of node.
            GameObject[] targets = Universum.Spatial.FindNearby(
                GameObjectType.Ship, pos, radius, maxResults:1024, excludeLoyalty:this
            );

            for (int i = 0; i < targets.Length; i++)
            {
                var targetShip = (Ship)targets[i];
                targetShip.KnownByEmpires.SetSeen(this);
            }
        }

        void DoFirstContact(Empire them)
        {
            Relationship usToThem = GetRelations(them);
            usToThem.SetInitialStrength(them.data.Traits.DiplomacyMod * 100f);

            SetRelationsAsKnown(usToThem, them);

            if (!them.IsKnown(this)) // do THEY know us?
                them.DoFirstContact(this);

            if (Universum.Debug)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Universum.Player == this)
                return;

            if (isPlayer)
            {
                if (them.IsFaction)
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
                EncounterPopup.Show(Universum.Screen, Universum.Player, e, encounter);
            }
            else
            {
                Log.Warning($"Could not find First Contact Encounter for {e.Name}, " +
                            "make sure this faction has <FirstContact>true</FirstContact> in one of it's encounter dialog XMLs");
            }
        }

        public void Update(UniverseState us, FixedSimTime timeStep)
        {
            #if PLAYERONLY
                if(!this.isPlayer && !this.isFaction)
                foreach (Ship ship in this.OwnedShips)
                    ship.GetAI().OrderScrapShip();
                if (this.OwnedShips.Count == 0)
                    return;
            #endif

            UpdateTimer -= timeStep.FixedTime;

            if (UpdateTimer <= 0f && !data.Defeated)
            {
                if (isPlayer)
                {
                    Universum.Screen.UpdateStarDateAndTriggerEvents(Universum.StarDate + 0.1f);
                    Universum.Stats.StatUpdateStarDate(Universum.StarDate);
                    if (Universum.StarDate.AlmostEqual(1000.09f))
                    {
                        foreach (Empire empire in EmpireManager.Empires)
                        {
                            foreach (Planet planet in empire.OwnedPlanets)
                                Universum.Stats.StatAddPlanetNode(Universum.StarDate, planet);
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
                    foreach (SolarSystem system in Universum.Systems)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (!p.IsExploredBy(Universum.Player) || !p.RecentCombat)
                                continue;

                            if (p.Owner != Universum.Player)
                            {
                                foreach (Troop troop in p.TroopsHere)
                                {
                                    if ((troop?.Loyalty) == Universum.Player)
                                    {
                                        empirePlanetCombat++;
                                        break;
                                    }
                                }
                            }
                            else empirePlanetCombat++;
                        }
                    }

                empireShipTotal = 0;
                empireShipCombat = 0;
                Inhibitors.Clear();
                var ships = OwnedShips;
                for (int i = 0; i < ships.Count; i++)
                {
                    Ship ship = OwnedShips[i];
                    if (ship.InhibitionRadius > 0.0f)
                        Inhibitors.Add(ship);

                    if (ship.Fleet == null && ship.InCombat && !ship.IsHangarShip) //fbedard: total ships in combat
                        empireShipCombat++;

                    if (ship.IsHangarShip || ship.DesignRole == RoleName.troop
                                          || ship.DesignRole == RoleName.freighter
                                          || ship.ShipData.ShipCategory == ShipCategory.Civilian)
                    {
                        continue;
                    }

                    empireShipTotal++;
                }

                UpdateTimer = GlobalStats.TurnTimer + (Id -1) * timeStep.FixedTime;
                UpdateEmpirePlanets();
                UpdatePopulation();
                UpdateTroopsInSpaceConsumption();
                UpdateAI(); // Must be done before DoMoney
                GovernPlanets(); // this does the governing after getting the budgets from UpdateAI when loading a game
                DoMoney();
                AssignNewHomeWorldIfNeeded();
                TakeTurn(us);
                SetRallyPoints();
            }

            UpdateFleets(timeStep);
        }

        // FB - for unittest only!
        public void TestAssignNewHomeWorldIfNeeded() => AssignNewHomeWorldIfNeeded();

        void AssignNewHomeWorldIfNeeded()
        {
            if (isPlayer | IsFaction)
                return;

            if (!GlobalStats.EliminationMode 
                && Capital?.Owner != this 
                && !OwnedPlanets.Any(p => p.IsHomeworld))
            {
                var potentialHomeworld = OwnedPlanets.FindMaxFiltered(p => p.FreeHabitableTiles > 0, p => p.ColonyPotentialValue(this));
                potentialHomeworld?.BuildCapitalHere();
            }
        }

        public void UpdateTroopsInSpaceConsumption()
        {
            int numTroops         = OwnedShips.Sum(s => s.TroopCount);
            TroopInSpaceFoodNeeds = numTroops * Troop.Consumption * (1 + data.Traits.ConsumptionModifier);
        }

        public void UpdatePopulation()
        {
            TotalPopBillion = GetTotalPop(out float maxPopBillion);
            MaxPopBillion   = maxPopBillion;
        }

        void InitializeHostilesInSystemDict() // For Player warnings
        {
            HostilesDictForPlayerInitialized = true;
            for (int i = 0; i < Universum.Systems.Count; i++)
            {
                SolarSystem system = Universum.Systems[i];
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
        /// Initializes non-serialized empire values after save load
        /// </summary>
        public void InitEmpireFromSave(UniverseState us) // todo FB - why is this called on new game?
        {
            Universum = us;
            EmpireShips.UpdatePublicLists();
            Research.UpdateNetResearch();

            RestoreUnserializableDataFromSave();
            UpdateEmpirePlanets();

            UpdateNetPlanetIncomes();
            UpdateMilitaryStrengths();
            CalculateScore(fromSave: true);
            UpdateRelationships(takeTurn: false);
            UpdateShipMaintenance();
            UpdateMaxColonyValues();
            AI.RunEconomicPlanner(fromSave: true);
            if (!isPlayer)
                AI.OffensiveForcePoolManager.ManageAOs();
        }

        public void UpdateMilitaryStrengths()
        {
            CurrentMilitaryStrength = 0;
            CurrentTroopStrength    = 0;
            OffensiveStrength       = 0;
            var ships = OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
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
            for (int i = SystemsWithThreat.Count - 1 ; i >= 0; i--)
            {
                var threat = SystemsWithThreat[i];
                threat.UpdateTimer(timeStep);
                /* FB - commenting this for now since the UI is using this.
                if (!threat.UpdateTimer(timeStep))
                    SystemsWithThreat.Remove(threat);*/
            }

            var knownFleets = new Array<Fleet>();
            foreach ((Empire them, Relationship rel) in AllRelations)
            {
                if (IsAtWarWith(them) || them.isPlayer && !IsNAPactWith(them))
                {
                    foreach (var fleet in them.FleetsDict)
                    {
                        if (fleet.Value.Ships.Any(s => s?.IsInBordersOf(this) == true || s?.KnownByEmpires.KnownBy(this) == true))
                            knownFleets.Add(fleet.Value);
                    }
                }
            }

            for (int i = 0; i < OwnedSolarSystems.Count; i++)
            {
                var system = OwnedSolarSystems[i];
                var fleets = knownFleets.Filter(f => f.FinalPosition.InRadius(system.Position, system.Radius * (f.Owner.isPlayer ? 3 : 2)));
                if (fleets.Length > 0)
                {
                    if (!SystemsWithThreat.Any(s => s.UpdateThreat(system, fleets)))
                        SystemsWithThreat.Add(new IncomingThreat(this, system, fleets));
                }
            }
        }

        // Using memory to save CPU time. the question is how often is the value used and
        // How often would it be calculated.
        private void UpdateMaxColonyValues()
        {
            MaxColonyValue             = 0;
            TotalColonyValues          = 0;
            TotalColonyPotentialValues = 0;

            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet planet = OwnedPlanets[i];
                TotalColonyValues          += planet.ColonyValue;
                TotalColonyPotentialValues += planet.ColonyPotentialValue(this);
                if (planet.ColonyValue > MaxColonyValue)
                    MaxColonyValue = planet.ColonyValue;
            }
        }

        private void UpdateBestOrbitals()
        {
            // FB - this is done here for more performance. having set values here prevents calling shipbuilder by every planet every turn
            BestPlatformWeCanBuild = BestShipWeCanBuild(RoleName.platform, this);
            BestStationWeCanBuild  = BestShipWeCanBuild(RoleName.station, this);
        }

        public void UpdateDefenseShipBuildingOffense()
        {
            for (int i = 0 ; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                p.UpdateDefenseShipBuildingOffense();
            }
        }

        public void ApplyModuleHealthTechBonus(float bonus)
        {
            var ships = OwnedShips;
            for (int i = 0 ; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                ship.ApplyModuleHealthTechBonus(bonus);
            }
        }

        public float GetTroopMaintThisTurn()
        {
            // Troops maintenance on ships are calculated as part of ship maintenance
            int troopsOnPlanets = OwnedPlanets.Sum(p => p.TroopsHere.Count);
            return troopsOnPlanets * ShipMaintenance.TroopMaint;
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
            debug.AddLine($"Idle Freighters: {GetIdleFreighters(false).Length}");
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

        public void DoMoney()
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
            TroopCostOnPlanets            = 0;
            GrossPlanetIncome             = 0;
            ExcessGoodsMoneyAddedThisTurn = 0;
            PotentialIncome               = 0;
            TotalFoodPerColonist          = 0;

            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                p.UpdateIncomes();
                NetPlanetIncomes              += p.Money.NetRevenue;
                GrossPlanetIncome             += p.Money.GrossRevenue;
                PotentialIncome               += p.Money.PotentialRevenue;
                ExcessGoodsMoneyAddedThisTurn += p.ExcessGoodsIncome;
                TroopCostOnPlanets            += p.Money.TroopMaint;

                if      (p.IsCybernetic && p.Prod.NetMaxPotential.Greater(0))  TotalFoodPerColonist += p.Prod.NetMaxPotential;
                else if (p.NonCybernetic && p.Food.NetMaxPotential.Greater(0)) TotalFoodPerColonist += p.Food.NetMaxPotential;
            }
        }

        public void UpdateEmpirePlanets()
        {
            var random = new SeededRandom();

            ResetMoneySpentOnProduction();
            // expensive lock with several loops.
            Planet[] planetsToUpdate = OwnedPlanets.Sorted(p => p.ColonyPotentialValue(this));

            TotalProdExportSlots = OwnedPlanets.Sum(p => p.FreeProdExportSlots); // Done before UpdateOwnedPlanet
            for (int i = 0; i < planetsToUpdate.Length; i++)
            {
                Planet planet = OwnedPlanets[i];
                planet.UpdateOwnedPlanet(random);
            }
        }

        public void GovernPlanets()
        {
            if (!IsFaction && !data.Defeated)
                UpdateMaxColonyValues();

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
            foreach (Ship ship in OwnedShips)
            {
                float maintenance = ship.GetMaintCost();
                if (!ship.Active || ship.AI.State == AIState.Scrap)
                {
                    TotalMaintenanceInScrap += maintenance;
                    continue;
                }
                if (data.DefenseBudget > 0 && ((ship.ShipData.HullRole == RoleName.platform && ship.IsTethered)
                                               || (ship.ShipData.HullRole == RoleName.station &&
                                                   (ship.ShipData.IsOrbitalDefense || !ship.ShipData.IsShipyard))))
                {
                    data.DefenseBudget -= maintenance;
                }
                switch (ship.DesignRoleType)
                {
                    case RoleType.WarSupport:
                    case RoleType.Warship: TotalWarShipMaintenance             += maintenance; break;
                    case RoleType.Civilian: TotalCivShipMaintenance            += maintenance; break;
                    case RoleType.EmpireSupport: TotalEmpireSupportMaintenance += maintenance; break;
                    case RoleType.Orbital: TotalOrbitalMaintenance             += maintenance; break;
                    case RoleType.Troop: TotalTroopShipMaintenance             += maintenance; break;
                    case RoleType.NotApplicable: break;
                    default:
                        Log.Warning($"Type not included in maintenance and not in notapplicable {ship.DesignRoleType}\n    {ship} ");
                        break;
                }
                TotalShipMaintenance += maintenance;
            }

            foreach (Ship ship in OwnedProjectors)
            {
                if (data.SSPBudget > 0)
                {
                    data.SSPBudget -= ship.GetMaintCost();
                    continue;
                }
                TotalShipMaintenance += ship.GetMaintCost();
            }
        }

        public float EstimateNetIncomeAtTaxRate(float rate)
        {
            float plusNetIncome = (rate-data.TaxRate) * NetPlanetIncomes;
            return GrossIncome + plusNetIncome - AllSpending;
        }

        public float GetActualNetLastTurn() => Money - MoneyLastTurn;

        /// @return TRUE if this Empire can build this ship
        public bool CanBuildShip(string shipUID)
        {
            return ShipsWeCanBuild.Contains(shipUID);
        }

        public void FactionShipsWeCanBuild()
        {
            if (!IsFaction) return;
            foreach (Ship ship in ResourceManager.ShipTemplates)
            {
                if ((data.Traits.ShipType == ship.ShipData.ShipStyle
                    || ship.ShipData.ShipStyle == "Misc"
                    || ship.ShipData.ShipStyle.IsEmpty())
                    && ship.CanBeAddedToBuildableShips(this))
                {
                    ShipsWeCanBuild.Add(ship.Name);
                    foreach (ShipModule hangar in ship.Carrier.AllHangars)
                    {
                        if (hangar.HangarShipUID.NotEmpty())
                        {
                            var hangarShip = ResourceManager.GetShipTemplate(hangar.HangarShipUID, throwIfError: false);
                            if (hangarShip?.CanBeAddedToBuildableShips(this) == true)
                                ShipsWeCanBuild.Add(hangar.HangarShipUID);
                        }
                    }
                }
            }
            foreach (var hull in UnlockedHullsDict.Keys.ToArr())
                UnlockedHullsDict[hull] = true;
        }

        public void UpdateShipsWeCanBuild(Array<string> hulls = null, bool debug = false)
        {
            if (IsFaction)
            {
                FactionShipsWeCanBuild();
                return;
            }

            // TODO: This should operate on IShipDesign instead of Ship template
            //       which requires a lot of utilities in Ship.cs to be moved
            foreach (Ship ship in ResourceManager.ShipTemplates)
            {
                if (hulls != null && !hulls.Contains(ship.ShipData.Hull))
                    continue;

                // we can already build this
                if (ShipsWeCanBuild.Contains(ship.Name))
                    continue;
                if (!ship.CanBeAddedToBuildableShips(this))
                    continue;

                if (WeCanBuildThis(ship.ShipData, debug))
                {
                    if (ship.ShipData.Role <= RoleName.station)
                        SpaceStationsWeCanBuild.Add(ship.Name);

                    bool shipAdded = ShipsWeCanBuild.Add(ship.Name);

                    if (isPlayer)
                        Universum.Screen?.OnPlayerBuildableShipsUpdated();

                    if (shipAdded)
                    {
                        UpdateBestOrbitals();
                        UpdateDefenseShipBuildingOffense();
                        ship.MarkShipRolesUsableForEmpire(this);
                    }
                }
            }
        }

        public bool WeCanShowThisWIP(ShipDesign shipData)
        {
            return WeCanBuildThis(shipData, debug:true);
        }

        public bool WeCanBuildThis(string shipName, bool debug = false)
        {
            if (!ResourceManager.Ships.GetDesign(shipName, out IShipDesign shipData))
            {
                Log.Warning($"Ship does not exist: {shipName}");
                return false;
            }

            return WeCanBuildThis(shipData, debug);
        }

        bool WeCanBuildThis(IShipDesign design, bool debug = false)
        {
            // If this hull is not unlocked, then we can't build it
            if (!IsHullUnlocked(design.Hull))
            {
                if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedHull Design:{design.Name}");
                return false;
            }

            if (design.TechsNeeded.Count > 0)
            {
                if (!design.Unlockable)
                {
                    if (debug) Log.Write($"WeCanBuildThis:false Reason:NotUnlockable Design:{design.Name}");
                    return false;
                }

                foreach (string shipTech in design.TechsNeeded)
                {
                    if (ShipTechs.Contains(shipTech))
                        continue;
                    TechEntry onlyShipTech = TechnologyDict[shipTech];
                    if (onlyShipTech.Locked)
                    {
                        if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedTech={shipTech} Design:{design.Name}");
                        return false;
                    }
                }
            }
            else
            {
                // check if all modules in the ship are unlocked
                foreach (string moduleUID in design.UniqueModuleUIDs)
                {
                    if (!IsModuleUnlocked(moduleUID))
                    {
                        if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedModule={moduleUID} Design:{design.Name}");
                        return false; // can't build this ship because it contains a locked Module
                    }
                }
            }

            if (debug) Log.Write($"WeCanBuildThis:true Design:{design.Name}");
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
                    if (ship.ShipData.UniqueModuleUIDs.Contains(entry.ModuleUID))
                        return true;
                }
            }
            return false;
        }

        public bool GetTroopShipForRebase(out Ship troopShip, Vector2 pos, string planetName = "")
        {
            // Try free troop ships first if there is not one free, launch a troop from the nearest planet to space if possible
            return NearestFreeTroopShip(out troopShip, pos) || LaunchNearestTroopForRebase(out troopShip, pos, planetName);
        }

        public bool GetTroopShipForRebase(out Ship troopShip, Ship ship)
        {
            // Try free troop ships first if there is not one free, launch a troop from the nearest planet to space if possible
            return NearestFreeTroopShip(out troopShip, ship.Position) || LaunchNearestTroopForRebase(out troopShip, ship.Position);
        }

        private bool NearestFreeTroopShip(out Ship troopShip, Vector2 objectCenter)
        {
            troopShip = null;
            Array<Ship> troopShips;
            troopShips = new Array<Ship>(OwnedShips
                .Filter(troopship => troopship.IsIdleSingleTroopship)
                .OrderBy(distance => distance.Position.Distance(objectCenter)));

            if (troopShips.Count > 0)
                troopShip = troopShips.First();

            return troopShip != null;
        }

        public int NumFreeTroops()
        {
            return OwnedShips.Filter(s => s.IsIdleSingleTroopship).Length 
                   + OwnedPlanets.Sum(p => p.NumTroopsCanLaunch);
        }

        public int TotalTroops()
        {
            return OwnedShips.Sum(s => s.NumPlayerTroopsOnShip)
                   + OwnedPlanets.Sum(p => p.CountEmpireTroops(this));
        }

        private bool LaunchNearestTroopForRebase(out Ship troopShip, Vector2 objectCenter, string planetName = "")
        {
            troopShip = null;
            Array<Planet> candidatePlanets = new Array<Planet>(OwnedPlanets
                .Filter(p => p.NumTroopsCanLaunch > 0 && p.TroopsHere.Any(t => t.CanLaunch) && p.Name != planetName)
                .OrderBy(distance => distance.Position.Distance(objectCenter)));

            if (candidatePlanets.Count == 0)
                return false;

            var troops = candidatePlanets.First().TroopsHere;
            troopShip = troops.FirstOrDefault(t => t.Loyalty == this)?.Launch();
            return troopShip != null;
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

        public bool DetectPrepareForWarVsPlayer(Empire ai)
        {
            if (!isPlayer) // Only for the Player
                return false;

            int playerSpyDefense = GetSpyDefense();
            int aiSpyDefense     = ai.GetSpyDefense() + ai.DifficultyModifiers.WarSneakiness + ai.PersonalityModifiers.WarSneakiness;
            int rollModifier     = playerSpyDefense - aiSpyDefense; // higher modifier will make the roll smaller, which is better
            return RandomMath.RollDie(100 - rollModifier) <= playerSpyDefense;
        }

        /// <summary>
        /// Transfer the capital to the new planet if this planet was the original capital of the empire
        /// It will not transfer original capital worlds of other races, so federations can keep several capitals
        /// </summary>
        public void TryTransferCapital(Planet newHomeworld)
        {
            if (newHomeworld != Capital)
                return;

            foreach (Planet p in OwnedPlanets)
            {
                if (p.IsHomeworld && EmpireManager.MajorEmpires.Any(e => e.Capital != p))
                {
                    if (p.RemoveCapital() && isPlayer)
                        Universum.Notifications.AddCapitalTransfer(p, newHomeworld);
                }
            }

            newHomeworld.BuildCapitalHere();
        }

        /// <summary>
        /// Gets the total population in billions and option for max pop
        /// </summary>
        float GetTotalPop(out float maxPop)
        {
            float num = 0f;
            maxPop    = 0f;
            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                num    += OwnedPlanets[i].PopulationBillion;
                maxPop += OwnedPlanets[i].MaxPopulationBillion;
            }

            var ships = OwnedShips;
            for (int i = 0; i < ships.Count; i++)
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
            for (int i = 0; i < OwnedPlanets.Count; i++)
                num += OwnedPlanets[i].PotentialMaxPopBillionsFor(this);
            return num;
        }

        public float GetGrossFoodPerTurn()
        {
            float num = 0.0f;
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
            foreach (Planet planet in OwnedPlanets)
            {
                switch (planet.CType)
                {
                    case Planet.ColonyType.Agricultural: ++agriculturalCount; break;
                    case Planet.ColonyType.Core:         ++coreCount;         break;
                    case Planet.ColonyType.Industrial:   ++industrialCount;   break;
                    case Planet.ColonyType.Research:     ++researchCount;     break;
                    case Planet.ColonyType.Military:     ++militaryCount;     break;
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

        
        readonly Array<InfluenceNode> OurBorderPlanets = new(); // all of our planets
        readonly Array<InfluenceNode> OurBorderShips = new(); // SSP-s and some Bases

        readonly Array<InfluenceNode> OurSensorPlanets = new(); // all of our planets
        readonly Array<InfluenceNode> OurSensorShips = new(); // all ships

        readonly Array<InfluenceNode> TempBorderNodes = new();
        readonly Array<InfluenceNode> TempSensorNodes = new();

        public InfluenceNode[] BorderNodes = Empty<InfluenceNode>.Array;
        public InfluenceNode[] SensorNodes = Empty<InfluenceNode>.Array;
        public BorderNodeCache BorderNodeCache = new();

        public void AddBorderNode(Ship ship)
        {
            bool known = IsShipKnownToPlayer(ship);

            bool isBorderNode = ship.IsSubspaceProjector
                            || (WeAreRemnants && ship.Name == data.RemnantPortal)
                            || (WeArePirates && Pirates.IsBase(ship));

            if (isBorderNode)
            {
                var borderNode = new InfluenceNode(ship, GetProjectorRadius(), known);
                OurBorderShips.Add(borderNode);
            }

            // all ships/stations/SSP-s are sensor nodes
            var sensorNode = new InfluenceNode(ship, ship.SensorRange, known);
            OurSensorShips.Add(sensorNode);
        }

        public void AddBorderNode(Planet planet)
        {
            bool empireKnown = IsThisEmpireKnownByPlayer();
            bool known = empireKnown || planet.IsExploredBy(Universum.Player);

            var borderNode = new InfluenceNode(planet, planet.GetProjectorRange(), known);
            OurBorderPlanets.Add(borderNode);

            var sensorNode = new InfluenceNode(planet, planet.SensorRange, empireKnown);
            OurSensorPlanets.Add(sensorNode);
        }

        void RemoveBorderNode(GameObject source, Array<InfluenceNode> nodes)
        {
            int count = nodes.Count;
            InfluenceNode[] rawNodes = nodes.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                if (rawNodes[i].Source == source)
                {
                    nodes.RemoveAtSwapLast(i);
                    break;
                }
            }
        }

        public void RemoveBorderNode(GameObject source)
        {
            if (source is Ship)
            {
                RemoveBorderNode(source, OurBorderShips);
                RemoveBorderNode(source, OurSensorShips);
            }
            else if (source is Planet)
            {
                RemoveBorderNode(source, OurBorderPlanets);
                RemoveBorderNode(source, OurSensorPlanets);
            }
        }

        public bool ForceUpdateSensorRadiuses;

        void UpdateSensorAndBorderRadiuses()
        {
            ForceUpdateSensorRadiuses = false;

            bool useSensorRange = WeArePirates || WeAreRemnants;
            float projectorRadius = GetProjectorRadius();

            int nSensorShips = OurSensorShips.Count;
            int nSensorPlanets = OurSensorPlanets.Count;
            int nBorderShips = OurBorderShips.Count;
            int nBorderPlanets = OurBorderPlanets.Count;
            InfluenceNode[] sensorShips = OurSensorShips.GetInternalArrayItems();
            InfluenceNode[] sensorPlanets = OurSensorPlanets.GetInternalArrayItems();
            InfluenceNode[] borderShips = OurBorderShips.GetInternalArrayItems();
            InfluenceNode[] borderPlanets = OurBorderPlanets.GetInternalArrayItems();

            for (int i = 0; i < nSensorShips; ++i)
            {
                ref InfluenceNode n = ref sensorShips[i];
                n.Radius = ((Ship)n.Source).SensorRange;
            }
            for (int i = 0; i < nSensorPlanets; ++i)
            {
                ref InfluenceNode n = ref sensorPlanets[i];
                n.Radius = ((Planet)n.Source).SensorRange;
            }
            for (int i = 0; i < nBorderShips; ++i)
            {
                ref InfluenceNode n = ref borderShips[i];
                n.Radius = useSensorRange ? ((Ship)n.Source).SensorRange : projectorRadius;
            }
            for (int i = 0; i < nBorderPlanets; ++i)
            {
                ref InfluenceNode n = ref borderPlanets[i];
                n.Radius = ((Planet)n.Source).GetProjectorRange();
            }
        }

        void UpdateOurSensorNodes()
        {
            bool knownToPlayer = IsThisEmpireWellKnownByPlayer();

            int nSensorShips = OurSensorShips.Count;
            int nSensorPlanets = OurSensorPlanets.Count;
            InfluenceNode[] sensorShips = OurSensorShips.GetInternalArrayItems();
            InfluenceNode[] sensorPlanets = OurSensorPlanets.GetInternalArrayItems();

            for (int i = 0; i < nSensorShips; ++i)
            {
                ref InfluenceNode n = ref sensorShips[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = knownToPlayer;
            }
            for (int i = 0; i < nSensorPlanets; ++i)
            {
                ref InfluenceNode n = ref sensorPlanets[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = knownToPlayer;
            }
        }

        void UpdateOurBorderNodes()
        {
            bool knownToPlayer = IsThisEmpireKnownByPlayer();

            int nBorderShips = OurBorderShips.Count;
            int nBorderPlanets = OurBorderPlanets.Count;
            InfluenceNode[] borderShips = OurBorderShips.GetInternalArrayItems();
            InfluenceNode[] borderPlanets = OurBorderPlanets.GetInternalArrayItems();

            if (knownToPlayer)
            {
                for (int i = 0; i < nBorderShips; ++i)
                {
                    ref InfluenceNode n = ref borderShips[i];
                    n.Position = n.Source.Position;
                    n.KnownToPlayer = true;
                }
                for (int i = 0; i < nBorderPlanets; ++i)
                {
                    ref InfluenceNode n = ref borderPlanets[i];
                    n.Position = n.Source.Position;
                    n.KnownToPlayer = true;
                }
            }
            else
            {
                var player = Universum.Player;
                for (int i = 0; i < nBorderShips; ++i)
                {
                    ref InfluenceNode n = ref borderShips[i];
                    n.Position = n.Source.Position;
                    n.KnownToPlayer = ((Ship)n.Source).InSensorRange;
                }
                for (int i = 0; i < nBorderPlanets; ++i)
                {
                    ref InfluenceNode n = ref borderPlanets[i];
                    n.Position = n.Source.Position;
                    n.KnownToPlayer = ((Planet)n.Source).IsExploredBy(player);
                }
            }
        }

        bool IsShipKnownToPlayer(Ship ship)
        {
            return ship.InSensorRange || IsThisEmpireKnownByPlayer();
        }

        bool IsThisEmpireWellKnownByPlayer()
        {
            var us = Universum;
            bool wellKnown = isPlayer
                || us.Player?.IsAlliedWith(this) == true // support unit tests without Player
                || us.Debug && (us.Screen.SelectedShip == null || us.Screen.SelectedShip.Loyalty == this);
            return wellKnown;
        }

        bool IsThisEmpireKnownByPlayer()
        {
            return IsThisEmpireWellKnownByPlayer()
                || Universum.Player?.IsTradeOrOpenBorders(this) == true;
        }

        /// <summary>
        /// Border nodes are empire's projector influence from SSP's and Planets
        /// Sensor nodes are used to show the sensor range of things. Ship, planets, spies, etc
        /// </summary>
        void ResetBorders()
        {
            if (ForceUpdateSensorRadiuses)
                UpdateSensorAndBorderRadiuses();
            UpdateOurSensorNodes();
            UpdateOurBorderNodes();

            TempBorderNodes.AddRange(OurBorderPlanets);
            TempBorderNodes.AddRange(OurBorderShips);
            TempSensorNodes.AddRange(OurSensorPlanets);
            TempSensorNodes.AddRange(OurSensorShips);
            AddSensorsFromAllies(TempSensorNodes);
            AddSensorsFromMoles(TempSensorNodes);
            
            BorderNodes = TempBorderNodes.ToArray();
            SensorNodes = TempSensorNodes.ToArray();
            TempBorderNodes.Clear();
            TempSensorNodes.Clear();
        }

        void AddSensorsFromAllies(Array<InfluenceNode> sensorNodes)
        {
            bool knownToPlayer = isPlayer;

            foreach (Empire ally in Universum.Empires)
            {
                if (GetRelations(ally, out Relationship relation) && relation.Treaty_Alliance)
                {
                    int nSensorShips = ally.OurSensorShips.Count;
                    int nSensorPlanets = ally.OurSensorPlanets.Count;
                    InfluenceNode[] sensorShips = ally.OurSensorShips.GetInternalArrayItems();
                    InfluenceNode[] sensorPlanets = ally.OurSensorPlanets.GetInternalArrayItems();

                    for (int i = 0; i < nSensorShips; ++i)
                    {
                        InfluenceNode n = sensorShips[i];
                        n.KnownToPlayer |= knownToPlayer;
                        sensorNodes.Add(n);
                    }
                    for (int i = 0; i < nSensorPlanets; ++i)
                    {
                        InfluenceNode n = sensorPlanets[i];
                        n.KnownToPlayer |= knownToPlayer;
                        sensorNodes.Add(n);
                    }
                }
            }
        }

        // Moles are spies who have successfully been planted during 'Infiltrate' type missions
        void AddSensorsFromMoles(Array<InfluenceNode> sensorNodes)
        {
            if (data.MoleList.IsEmpty)
                return;

            float projectorRadius = GetProjectorRadius();
            foreach (Mole mole in data.MoleList)
            {
                var p = Universum.GetPlanet(mole.PlanetId);
                if (p != null)
                {
                    sensorNodes.Add(new InfluenceNode
                    {
                        Position = p.Position,
                        Radius = projectorRadius,
                        KnownToPlayer = isPlayer
                    });
                }
            }
        }

        public void TryCreateAssaultBombersGoal(Empire enemy, Planet planet)
        {
            if (enemy == this  || AI.HasGoal(g => g is AssaultBombers && g.PlanetBuildingAt == planet))
                return;

            AI.AddGoal(new AssaultBombers(planet, this, enemy));
        }

        public void TryAutoRequisitionShip(Fleet fleet, Ship ship)
        {
            if (!isPlayer || fleet == null || !fleet.AutoRequisition)
                return;

            if (!ShipsWeCanBuild.Contains(ship.Name) || !fleet.FindShipNode(ship, out FleetDataNode node))
                return;
            var ships = OwnedShips;

            for (int i = 0; i < ships.Count; i++)
            {
                Ship s = ships[i];
                if (s.Fleet == null
                    && s.Name == ship.Name
                    && s.OnLowAlert
                    && !s.IsHangarShip
                    && !s.IsHomeDefense
                    && s.AI.State != AIState.Refit
                    && !s.AI.HasPriorityOrder
                    && !s.AI.HasPriorityTarget)
                {
                    s.AI.ClearOrders();
                    fleet.AddExistingShip(s, node);
                    return;
                }
            }

            var g = new FleetRequisition(ship.Name, this, fleet, false);
            node.Goal = g;
            AI.AddGoal(g);
            g.Evaluate();
        }

        private void TakeTurn(UniverseState us)
        {
            if (IsEmpireDead())
                return;

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

            if (Money < 0.0 && !IsFaction)
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
                if (Universum.Stats.GetSnapshot(Universum.StarDate, this, out Snapshot snapshot))
                {
                    snapshot.ShipCount = OwnedShips.Count;
                    snapshot.MilitaryStrength = CurrentMilitaryStrength;
                    snapshot.TaxRate = data.TaxRate;
                }
            }

            if (isPlayer)
            {
                ExecuteDiplomacyContacts();
                CheckFederationVsPlayer(us);
                Universum.Events.UpdateEvents(Universum);

                if ((Money / AllSpending.LowerBound(1)) < 2)
                    Universum.Notifications.AddMoneyWarning();

                if (!Universum.NoEliminationVictory)
                {
                    bool allEmpiresDead = true;
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        var planets = empire.GetPlanets();
                        if (planets.Count > 0 && !empire.IsFaction && empire != this)
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
                            Universum.Screen.OnPlayerWon();
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
                        Universum.Screen.OnPlayerWon(GameText.AsTheRemnantExterminatorsSweep);
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                if (Universum.Stats.GetSnapshot(Universum.StarDate, this, out Snapshot snapshot))
                    snapshot.Population = OwnedPlanets.Sum(p => p.Population);
            }

            Research.Update();

            if (data.TurnsBelowZero > 0 && Money < 0.0 && (!Universum.Debug || !isPlayer))
                Bankruptcy();

            if ((Universum.StarDate % 1).AlmostZero())
                CalculateScore();

            UpdateRelationships(takeTurn: true);

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

            if (!IsFaction)
            {
                CalcWeightedCenter();
                DispatchBuildAndScrapFreighters();
                AssignSniffingTasks();
                AssignExplorationTasks();
            }
        }

        void ExecuteDiplomacyContacts()
        {
            if (DiplomacyContactQueue.Count == 0)
                return;

            Empire empire = EmpireManager.GetEmpireById(DiplomacyContactQueue[0].EmpireId);
            string dialog = DiplomacyContactQueue[0].Dialog;

            if (dialog == "DECLAREWAR")
                empire.AI.DeclareWarOn(this, WarType.ImperialistWar);
            else
                DiplomacyScreen.ContactPlayerFromDiplomacyQueue(empire, dialog);

            DiplomacyContactQueue.RemoveAt(0);
        }


        void CheckFederationVsPlayer(UniverseState us)
        {
            if (GlobalStats.PreventFederations || us.StarDate < 1100f || (us.StarDate % 1).NotZero())
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
                    Universum.Notifications.AddPeacefulMergerNotification(biggestAI, strongest);
                else
                    Universum.Notifications.AddSurrendered(biggestAI, strongest);

                biggestAI.AbsorbEmpire(strongest);
                if (biggestAI.GetRelations(this).ActiveWar == null)
                    biggestAI.AI.DeclareWarOn(this, WarType.ImperialistWar);
            }
        }

        void UpdateAI() => AI.Update();

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
                    if (OwnedPlanets.FindMax(out Planet planet, p => WeightedCenter.SqDist(p.Position)))
                    {
                        if (isPlayer)
                            Universum.Notifications.AddRebellionNotification(planet, rebels);

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
                    var ships = OwnedShips;
                        foreach (Ship pirateChoice in ships)
                        {
                            if (pirateChoice == null || !pirateChoice.Active)
                                continue;
                            pirate = pirateChoice;
                            break;
                        }

                    pirate?.LoyaltyChangeByGift(rebels);
                }
                else Log.Error($"Rebellion failed: {data.RebelName}");

                data.TurnsBelowZero = 0;
            }
        }

        public bool IsEmpireDead()
        {
            if (IsFaction) return false;
            if (data.Defeated) return true;
            if (!GlobalStats.EliminationMode && OwnedPlanets.Count != 0)
                return false;
            if (GlobalStats.EliminationMode && (Capital == null || Capital.Owner == this) && OwnedPlanets.Count != 0)
                return false;

            SetAsDefeated();
            if (!isPlayer)
            {
                if (Universum.Player.IsKnown(this))
                    Universum.Notifications.AddEmpireDiedNotification(this);
                return true;
            }

            Universum.Screen.OnPlayerDefeated();
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
            string hullName = ship.ShipData.Hull;
            if (IsHullUnlocked(hullName) || ship.ShipData.Role == RoleName.prototype)
                return; // It's ours or we got it elsewhere


            if (!TryReverseEngineer(ship, out TechEntry hullTech, out Empire empire))
                return; // We could not reverse engineer this, too bad

            UnlockTech(hullTech, TechUnlockType.Scrap, empire);

            if (isPlayer)
            {
                string modelIcon  = ship.BaseHull.IconPath;
                string hullString = ship.BaseHull.ToString();
                if (hullTech.Unlocked)
                {
                    string message = $"{hullString}{Localizer.Token(GameText.ReverseEngineered)}";
                    Universum.Notifications.AddScrapUnlockNotification(message, modelIcon, "ShipDesign");
                }
                else
                {
                    string message = $"{hullString}{Localizer.Token(GameText.HullScrappedAdvancingResearch)}";
                    Universum.Notifications.AddScrapProgressNotification(message, modelIcon, "ResearchScreen", hullTech.UID);
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
            switch (ship.ShipData.HullRole)
            {
                case RoleName.fighter:    unlockChance = 90; break;
                case RoleName.corvette:   unlockChance = 80; break;
                case RoleName.frigate:    unlockChance = 60; break;
                case RoleName.cruiser:    unlockChance = 40; break;
                case RoleName.battleship: unlockChance = 30; break;
                case RoleName.capital:    unlockChance = 20; break;
                default:                           unlockChance = 50; break;
            }

            unlockChance *= 1 + data.Traits.ModHpModifier; // skilled or bad engineers
            return RandomMath.RollDice(unlockChance);
        }

        bool TryGetTechFromHull(Ship ship, out TechEntry techEntry, out Empire empire)
        {
            techEntry        = null;
            empire           = null;
            string hullName  = ship.ShipData.Hull;
            foreach (string techName in ship.ShipData.TechsNeeded)
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
            Universum.Notifications?.AddBoardNotification(Localizer.Token(GameText.ShipCapturedByYou),
                                                              ship.BaseHull.IconPath, "SnapToShip", ship, null);
        }

        public void AddBoardedNotification(Ship ship, Empire boarder)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(GameText.YourShipWasCaptured)} {boarder.Name}!";
            Universum.Notifications?.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, boarder);
        }

        public void AddMutinyNotification(Ship ship, GameText text, Empire initiator)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(text)} {initiator.Name}!";
            Universum.Notifications.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, initiator);
        }

        void CalculateScore(bool fromSave = false)
        {
            TechScore = 0;
            foreach (TechEntry entry in TechnologyDict.Values)
                if (entry.Unlocked) TechScore += entry.TechCost;
            TechScore /= 100;

            IndustrialScore = 0;
            ExpansionScore  = 0;
            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                ExpansionScore  += p.Fertility*10 + p.MineralRichness*10 + p.PopulationBillion;
                IndustrialScore += p.BuildingList.Sum(b => b.ActualCost);
            }
            IndustrialScore /= 20;

            if (fromSave)
                MilitaryScore = data.MilitaryScoreAverage;
            else
                MilitaryScore = data.NormalizeMilitaryScore(CurrentMilitaryStrength); // Avoid fluctuations

            TotalScore = (int)(MilitaryScore + IndustrialScore + TechScore + ExpansionScore);
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
            var planets = target.GetPlanets();
            for (int i = planets.Count-1; i >= 0; i--)
            {
                Planet planet = planets[i];
                planet.SetOwner(this);
                if (!planet.ParentSystem.OwnerList.Contains(this))
                {
                    planet.ParentSystem.OwnerList.Add(this);
                    planet.ParentSystem.OwnerList.Remove(target);
                }
            }

            foreach (Planet planet in Universum.Planets)
            {
                foreach (Troop troop in planet.TroopsHere)
                    if (troop.Loyalty == target)
                        troop.ChangeLoyalty(this);
            }

            target.ClearAllPlanets();
            var ships = target.OwnedShips;
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                Ship ship = ships[i];
                ship.LoyaltyChangeByGift(this, addNotification: false);
            }

            var projectors = target.OwnedProjectors;
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship ship = projectors[i];
                ship.LoyaltyChangeByGift(this, addNotification: false);
            }

            target.AIManagedShips.Clear();
            AssimilateTech(target);
            foreach (TechEntry techEntry in target.TechEntries)
            {
                if (techEntry.Unlocked)
                    AcquireTech(techEntry.UID, target, TechUnlockType.Normal);
            }
            foreach (KeyValuePair<string, bool> kv in target.UnlockedHullsDict)
            {
                if (kv.Value)
                    UnlockedHullsDict[kv.Key] = true;
            }
            foreach (KeyValuePair<string, bool> kv in target.UnlockedTroopDict)
            {
                if (kv.Value)
                {
                    UnlockedTroopDict[kv.Key] = true;
                    UnlockedTroops.AddUniqueRef(ResourceManager.GetTroopTemplate(kv.Key));
                }
            }
            foreach (Artifact artifact in target.data.OwnedArtifacts)
            {
                AddArtifact(artifact);
            }

            ResetTechsAndUnlocks();

            target.data.OwnedArtifacts.Clear();
            if (target.Money > 0.0)
            {
                Money += target.Money;
                target.Money = 0.0f;
            }

            target.SetAsMerged();
            ResetBorders();
            UpdateShipsWeCanBuild();

            if (this != Universum.Player)
            {
                AI.EndAllTasks();
                AI.DefensiveCoordinator.DefensiveForcePool.Clear();
                AI.DefensiveCoordinator.DefenseDict.Clear();
            }

            foreach (Agent agent in target.data.AgentList)
            {
                data.AgentList.Add(agent);
                agent.Mission = AgentMission.Defending;
                agent.TargetEmpire = null;
            }
            AI.DefensiveCoordinator.ManageForcePool();
            target.data.AgentList.Clear();
            target.data.AbsorbedBy = data.Traits.Name;
            ThirdPartyAbsorb(target);
            CalculateScore();
        }

        // If we are absorbing an empire which absorbed another empire in the past
        // their absorbed empires will become absobred by us - to get all relevant tech content (like hulls and troops)
        void ThirdPartyAbsorb(Empire target)
        {
            foreach (Empire e in EmpireManager.MajorEmpires)
            {
                if (e.data.AbsorbedBy == target.data.Traits.Name)
                    e.data.AbsorbedBy = data.Traits.Name;
            }
        }

        public bool HavePreReq(string techId) => GetTechEntry(techId).HasPreReq(this);

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
                    avgPlanetCenter += planet.Position;
                }

                center = avgPlanetCenter / planets;
            }
            else
            {
                int items = 0;
                var avgEmpireCenter = new Vector2();
                var ships = OwnedShips;
                for (int i = 0; i < ships.Count; i++)
                {
                    var planet = ships[i];
                    ++items;
                    avgEmpireCenter += planet.Position;
                }
                center =avgEmpireCenter / items.LowerBound(1);
            }
            return center;
        }

        // This is also done when a planet is added or removed
        public void CalcWeightedCenter(bool calcNow = false)
        {
            if (!calcNow && (Universum.StarDate % 1).Greater(0))
                return; // Once per year

            int planets = 0;
            var avgPlanetCenter = new Vector2();

            foreach (Planet planet in OwnedPlanets)
            {
                for (int x = 0; x < planet.PopulationBillion; ++x)
                {
                    ++planets;
                    avgPlanetCenter += planet.Position;
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

            if (GlobalStats.ActiveModInfo?.RemoveRemnantStory == true)
                return false;

            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(killedShip);
            Remnants.IncrementKills(they, (int)killedExpSettings.KillExp);
            return true;
        }

        void AssignSniffingTasks()
        {
            if (!isPlayer && AI.CountGoals(g => g.Type == GoalType.ScoutSystem) < DifficultyModifiers.NumSystemsToSniff)
                AI.AddGoal(new ScoutSystem(this));
        }

        public bool ChooseScoutShipToBuild(out IShipDesign scout)
        {
            if (isPlayer && ResourceManager.Ships.GetDesign(EmpireManager.Player.data.CurrentAutoScout, out scout))
                return true;

            var scoutShipsWeCanBuild = new Array<IShipDesign>();
            foreach (string shipName in ShipsWeCanBuild)
            {
                if (ResourceManager.Ships.GetDesign(shipName, out IShipDesign ship) &&
                    ship.Role == RoleName.scout)
                {
                    scoutShipsWeCanBuild.Add(ship);
                }
            }

            if (scoutShipsWeCanBuild.IsEmpty)
            {
                scout = null;
                return false;
            }

            // pick the scout with fastest FTL speed
            scout = scoutShipsWeCanBuild.FindMax(s => s.BaseWarpThrust);
            return scout != null;
        }

        void AssignExplorationTasks()
        {
            if (isPlayer && !AutoExplore)
                return;

            int unexplored = Universum.Systems.Count(s => !s.IsFullyExploredBy(this)).UpperBound(12);
            var ships = OwnedShips;
            if (unexplored == 0 && isPlayer)
            {
                // FB: Done exploring, flag can be removed. Maybe add a notification for the player?
                // We also might be able to turn off AutoExplore for the AI and save the system count
                // for unexplored (but dont scrap the scouts for the AI, they are needed for sniffing
                // remnant systems
                AutoExplore = false; 
                for (int i = 0; i < ships.Count; i++)
                {
                    Ship ship = ships[i];
                    if (ship.IsIdleScout())
                        ship.AI.OrderScrapShip();
                }

                return;
            }

            float desiredScouts = unexplored * Research.Strategy.ExpansionRatio;
            if (!isPlayer)
                desiredScouts *= ((int)Universum.Difficulty).LowerBound(1);

            int numScouts = 0;
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship.IsGoodScout())
                {
                    // FB: log the num for determining is should build more scouts
                    // If the player built excess scouts, assign them too, that is why
                    // we are not exiting the loop when desired scouts num was reached
                    numScouts += 1; 
                    if (ship.IsIdleScout())
                        ship.DoExplore(); 
                }
            }

            // Build a scout if needed
            if (numScouts < desiredScouts  && !AI.HasGoal(GoalType.BuildScout))
                AI.AddGoal(new BuildScout(this));
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
            data.OngoingDiplomaticModifier   += art.GetDiplomacyBonus(data);
            data.Traits.GroundCombatModifier += art.GetGroundCombatBonus(data);
            data.Traits.ModHpModifier        += art.GetModuleHpMod(data);
            data.FlatMoneyBonus              += art.GetFlatMoneyBonus(data);
            data.Traits.ProductionMod        += art.GetProductionBonus(data);
            data.Traits.ReproductionMod      += art.GetReproductionMod(data);
            data.Traits.ResearchMod          += art.GetResearchMod(data);
            data.SensorModifier              += art.GetSensorMod(data);
            data.ShieldPenBonusChance        += art.GetShieldPenMod(data);
            EmpireHullBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
            ForceUpdateSensorRadiuses = true;
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
            EmpireHullBonuses.RefreshBonuses(this); // RedFox: This will refresh all empire module stats
            ForceUpdateSensorRadiuses = true;
        }

        void IEmpireShipLists.RemoveShipAtEndOfTurn(Ship s) => EmpireShips?.Remove(s);

        public bool IsEmpireAttackable(Empire targetEmpire, GameObject target = null, bool scanOnly = false)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if ((rel.CanAttack && target == null) || (scanOnly && !rel.Known))
                return true;

            return target?.IsAttackable(this, rel) ?? false;
        }

        public bool IsEmpireScannedAsEnemy(Empire targetEmpire, GameObject target = null)
        {
            return IsEmpireAttackable(targetEmpire, target, scanOnly: true);
        }

        public bool IsEmpireHostile(Empire targetEmpire)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelationsOrNull(targetEmpire);
            return rel?.IsHostile == true;
        }

        public bool WillInhibit(Empire e) => e != this && !e.WeAreRemnants && IsAtWarWith(e);

        public Planet FindPlanet(int planetId)
        {
            foreach (Planet p in OwnedPlanets)
                if (p.Id == planetId)
                    return p;
            return null;
        }

        public Planet FindPlanet(string planetName)
        {
            foreach (Planet p in OwnedPlanets)
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
                radius = furthest.Position.Distance(center);
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

        float ThreatMatrixUpdateTimer;
        const float ResetThreatMatrixSeconds = 2;

        public void UpdateContactsAndBorders(UniverseScreen us, FixedSimTime timeStep)
        {
            if (IsEmpireDead())
                return;

            us.ResetBordersPerf.Start();
            {
                ResetBorders();
            }
            us.ResetBordersPerf.Stop();

            us.ScanFromPlanetsPerf.Start();
            {
                ScanFromAllSensorPlanets();
            }
            us.ScanFromPlanetsPerf.Stop();

            // this is pretty much 0%, removing perf indicators
            CheckForFirstContacts();

            ThreatMatrixUpdateTimer -= timeStep.FixedTime;
            if (ThreatMatrixUpdateTimer <= 0f)
            {
                ThreatMatrixUpdateTimer = ResetThreatMatrixSeconds;

                us.ThreatMatrixPerf.Start();
                Parallel.Run(() => AI.ThreatMatrix.UpdateAllPins(this));
                us.ThreatMatrixPerf.Stop();
            }
        }

        public void IncrementCordrazineCapture()
        {
            if (!GlobalStats.CordrazinePlanetCaptured)
                Universum.Notifications.AddNotify(ResourceManager.EventsDict["OwlwokFreedom"]);

            GlobalStats.CordrazinePlanetCaptured = true;
        }


        SmallBitSet ReadyForFirstContact = new();

        public void SetReadyForFirstContact(Empire other)
        {
            ReadyForFirstContact.Set(other.Id);
        }

        void CheckForFirstContacts()
        {
            if (!ReadyForFirstContact.IsAnyBitsSet)
                return;

            foreach (Empire e in Universum.Empires)
            {
                if (ReadyForFirstContact.IsSet(e.Id))
                {
                    ReadyForFirstContact.Unset(e.Id);
                    if (!KnownEmpires.IsSet(e.Id))
                    {
                        DoFirstContact(e);
                        return;
                    }
                }
            }
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

        public void ChargeRushFees(float productionCost)
        {
            ChargeCredits(productionCost, rush: true);
        }

        void ChargeCredits(float cost, bool rush = false)
        {
            float creditsToCharge = rush ? cost  * GlobalStats.RushCostPercentage : ProductionCreditCost(cost);
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

        public void RestoreDiplomacyConcatQueue(Array<DiplomacyQueueItem> diplomacyContactQueue)
        {
            if (diplomacyContactQueue != null)
                DiplomacyContactQueue = diplomacyContactQueue;
        }

        public void SetCapital(Planet planet)
        {
            Capital = planet;
        }

        public void ResetAllTechsAndBonuses()
        // FB - There is a bug here. Some tech bonuses are not reset after they are unlocked
        // For instance - pop growth is not reset
        {
            Research.Reset(); // clear research progress bar and queue
            Research.SetNoResearchLeft(false);
            foreach (TechEntry techEntry in TechEntries)
            {
                techEntry.ResetUnlockedTech();
            }

            data.ResetAllBonusModifiers(this);

            ResetUnlocks();
            InitEmpireUnlocks();
        }

        public struct InfluenceNode
        {
            public Vector2 Position;
            public float Radius;
            public GameObject Source; // Planet OR Ship
            public bool KnownToPlayer;

            public InfluenceNode(Planet planet, bool known)
            {
                Position = planet.Position;
                Radius = planet.SensorRange;
                Source = planet;
                KnownToPlayer = known;
            }
            public InfluenceNode(Ship ship, bool known = false)
            {
                Position = ship.Position;
                Radius = ship.SensorRange;
                Source = ship;
                KnownToPlayer = known || ship.InSensorRange;
            }

            public InfluenceNode(GameObject source, float radius, bool knowToPlayer)
            {
                Position = source.Position;
                Radius = radius;
                Source = source;
                KnownToPlayer = knowToPlayer;
            }
        }

        public void RestoreUnserializableDataFromSave()
        {
            //restore relationShipData
            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                rel.RestoreWarsFromSave(Universum);
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Empire() { Destroy(); }

        void ClearInfluenceList()
        {
            OurSensorShips.Clear();
            OurSensorPlanets.Clear();
            OurBorderShips.Clear();
            OurBorderPlanets.Clear();

            BorderNodes = Empty<InfluenceNode>.Array;
            SensorNodes = Empty<InfluenceNode>.Array;
            TempSensorNodes.Clear();
            TempBorderNodes.Clear();
        }

        void Destroy()
        {
            if (AI == null)
                return; // Already disposed

            AI = null;
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
            ActiveRelations = Empty<OurRelationsToThem>.Array;
            RelationsMap = Empty<OurRelationsToThem>.Array;
            HostilesLogged.Clear();
            ClearInfluenceList();
            TechnologyDict.Clear();
            SpaceRoadsList.Clear();
            foreach (var kv in FleetsDict)
                kv.Value.Reset(returnShipsToEmpireAI: false);
            FleetsDict.Clear();

            ResetUnlocks();

            Inhibitors.Clear();
            ShipsWeCanBuild.Clear();
            SpaceStationsWeCanBuild.Clear();
            Research.Reset();

            // TODO: These should not be in EmpireData !!!
            data.Defeated = false;
            data.OwnedArtifacts.Clear();
            data.AgentList.Clear();
            data.MoleList.Clear();

            if (data != null)
            {
                data.AgentList = new BatchRemovalCollection<Agent>();
                data.MoleList = new BatchRemovalCollection<Mole>();
            }

            AIManagedShips = null;
            EmpireShips = null;
        }

        public override string ToString() => $"{(isPlayer?"Player":"AI")}({Id}) '{Name}'";
    }
}
