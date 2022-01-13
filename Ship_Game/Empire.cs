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
using Ship_Game.Empires.Components;
using Ship_Game.Empires.ShipPools;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Fleets;
using Ship_Game.Utils;

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
    public sealed partial class Empire : IDisposable, IEmpireShipLists
    {
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! Empire must not be serialized! Add [XmlIgnore][JsonIgnore] to `public Empire XXX;` PROPERTIES/FIELDS. {this}");

        public float GetProjectorRadius() => Universum?.SubSpaceProjectors.Radius * data.SensorModifier ?? 10000;
        public float GetProjectorRadius(Planet planet) => GetProjectorRadius() + 10000f * planet.PopulationBillion;
        readonly Map<int, Fleet> FleetsDict = new Map<int, Fleet>();


        public readonly Map<string, bool> UnlockedHullsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        readonly Map<string, bool> UnlockedTroopDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        readonly Map<string, bool> UnlockedBuildingsDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        readonly Map<string, bool> UnlockedModulesDict = new Map<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        readonly Array<Troop> UnlockedTroops = new Array<Troop>();

        /// <summary>
        /// Returns an average of empire money over several turns.
        /// </summary>
        public float NormalizedMoney { get; private set; }

        public void UpdateNormalizedMoney(float money, bool fromSave = false)
        {
            const float rate = 0.1f;
            if (fromSave || NormalizedMoney == 0f)
                NormalizedMoney = money;
            else // simple moving average:
                NormalizedMoney = NormalizedMoney*(1f-rate) + money*rate;
        }

        public Map<string, TechEntry> TechnologyDict = new Map<string, TechEntry>(StringComparer.InvariantCultureIgnoreCase);
        public Array<Ship> Inhibitors = new Array<Ship>();
        public Array<SpaceRoad> SpaceRoadsList = new Array<SpaceRoad>();

        public const float StartingMoney = 1000f;
        float MoneyValue = StartingMoney;
        public float Money
        {
            get => MoneyValue;
            set => MoneyValue = value.NaNChecked(0f, "Empire.Money");
        }

        BatchRemovalCollection<Planet> OwnedPlanets = new BatchRemovalCollection<Planet>();
        BatchRemovalCollection<SolarSystem> OwnedSolarSystems = new BatchRemovalCollection<SolarSystem>();
        public IReadOnlyList<Ship> OwnedShips => EmpireShips.OwnedShips;
        public IReadOnlyList<Ship> OwnedProjectors => EmpireShips.OwnedProjectors;

        public InfluenceNode[] BorderNodes = Empty<InfluenceNode>.Array;
        public InfluenceNode[] SensorNodes = Empty<InfluenceNode>.Array;

        readonly Map<SolarSystem, bool> HostilesLogged = new Map<SolarSystem, bool>(); // Only for Player warnings
        public Array<IncomingThreat> SystemsWithThreat = new Array<IncomingThreat>();
        public HashSet<string> ShipsWeCanBuild = new HashSet<string>();
        public HashSet<string> structuresWeCanBuild = new HashSet<string>();
        float FleetUpdateTimer = 5f;
        int TurnCount = 1;
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

        public UniverseScreen Universum; // Alias for static Empire.Universum

        EmpireAI EmpireAI;
        float UpdateTimer;
        public bool isPlayer;
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
        public bool AutoBuild;
        public bool AutoExplore;
        public bool AutoColonize;
        public bool AutoResearch;
        public int TotalScore;
        public float TechScore;
        public float ExpansionScore;
        public float MilitaryScore;
        public float IndustrialScore;

        // This is the original capital of the empire. It is used in capital elimination and 
        // is used in capital elimination and also to determine if another capital will be moved here if the
        // empire retakes this planet. This value should never be changed after it was set.
        public Planet Capital { get; private set; } 

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
        public LoyaltyLists EmpireShips;
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue { get; private set; }
        public float TotalColonyValues { get; private set; }
        public float TotalColonyPotentialValues { get; private set; }
        public Ship BestPlatformWeCanBuild { get; private set; }
        public Ship BestStationWeCanBuild { get; private set; }
        public HashSet<string> ShipTechs = new HashSet<string>();
        public int GetEmpireTechLevel() => (int)Math.Floor(ShipTechs.Count / 3f);
        public Vector2 WeightedCenter;
        public bool RushAllConstruction;
        public List<KeyValuePair<int, string>> DiplomacyContactQueue { get; private set; } = new List<KeyValuePair<int, string>>();  // Empire IDs, for player only
        public bool AutoPickBestColonizer;

        public Array<string> ObsoletePlayerShipModules = new Array<string>();

        public int AtWarCount;
        public Array<string> BomberTech      = new Array<string>();
        public Array<string> TroopShipTech   = new Array<string>();
        public Array<string> CarrierTech     = new Array<string>();
        public Array<string> SupportShipTech = new Array<string>();
        public Planet[] RallyPoints          = Empty<Planet>.Array;

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

        public readonly EmpireResearch Research;
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

        public Empire()
        {
            Research = new EmpireResearch(this);
            
            AIManagedShips = new ShipPool(this, "AIManagedShips");
            EmpireShips = new LoyaltyLists(this);
        }

        public Empire(Empire parentEmpire) : this()
        {
            TechnologyDict = parentEmpire.TechnologyDict;
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
                ship.ShipStatusChanged = true;
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
            closest = SpacePorts.FindMin(p => p.Center.SqDist(position));
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

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, Ship ship, Ship newShip, bool travelBack, out Planet planet)
        {
            planet               = null;
            int travelMultiplier = travelBack ? 2 : 1;

            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip.ShipData)
                                        + ship.GetAstrogateTimeTo(p) * travelMultiplier);

            return planet != null;
        }

        public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, Ship newShip, out Planet planet)
        {
            planet = null;
            if (ports.Count == 0)
                return false;

            ports  = ports.Filter(p => !p.IsCrippled);
            if (ports.Count == 0)
                return false;

            planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip.ShipData));
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
            bool filterResearchPorts = ports.Any(p => p.colonyType != Planet.ColonyType.Research);
            if (ports.Count > 0)
            {
                float averageMaxProd = ports.Average(p => p.Prod.NetMaxPotential);
                bestPorts            = ports.Filter(p => !p.IsCrippled
                                                         && (p.colonyType != Planet.ColonyType.Research || !filterResearchPorts)
                                                         && p.Prod.NetMaxPotential.GreaterOrEqual(averageMaxProd * portQuality));
            }

            return bestPorts?.Length > 0;
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
                planet = FindNearestRallyPoint(ship.Position);
                if (planet == null || planet.Center.Distance(ship.Position) > 50000)
                    ship.ScuttleTimer = 5;

                return planet != null;
            }

            var scrapGoals       = GetEmpireAI().Goals.Filter(g => g.type == GoalType.ScrapShip);
            var potentialPlanets = OwnedPlanets.SortedDescending(p => p.MissingProdHereForScrap(scrapGoals)).Take(5).ToArray();
            if (potentialPlanets.Length == 0)
                return false;

            planet = potentialPlanets.FindMin(p => p.Center.Distance(ship.Position));
            return planet != null;
        }

        public float KnownEnemyStrengthIn(SolarSystem system, Predicate<ThreatMatrix.Pin> filter)
                     => EmpireAI.ThreatMatrix.GetStrengthInSystem(system, filter);

        public float KnownEnemyStrengthIn(SolarSystem system)
             => EmpireAI.ThreatMatrix.GetStrengthInSystem(system, p=> IsEmpireHostile(p.GetEmpire()));

        public float KnownEnemyStrengthIn(AO ao)
             => EmpireAI.ThreatMatrix.PingHostileStr(ao.Center, ao.Radius, this);

        public float KnownEmpireStrength(Empire empire) => EmpireAI.ThreatMatrix.KnownEmpireStrength(empire, p => p != null);
        public float KnownEmpireOffensiveStrength(Empire empire)
            => EmpireAI.ThreatMatrix.KnownEmpireStrength(empire, p => p != null && p.Ship?.IsPlatformOrStation == false);

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
                var nearestAO = ship.Loyalty.GetEmpireAI().FindClosestAOTo(ship.Position);
                home = nearestAO.GetOurPlanets().FindClosestTo(ship);
            }

            if (home == null)
            {
                home = Universum.Planets.FindMin(p => p.Center.Distance(ship.Position));
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
            while (EmpireAI.UsedFleets.Contains(key))
                ++key;
            EmpireAI.UsedFleets.Add(key);
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
            StatTracker.UpdateEmpire(Universum.StarDate, rebels);

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

            if (isFaction)
                return;

            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                BreakAllTreatiesWith(them, includingPeace: true);
            }

            EmpireAI.Goals.Clear();
            EmpireAI.EndAllTasks();
            foreach (var kv in FleetsDict) kv.Value.Reset();
            AIManagedShips.Clear();
            EmpireShips.Clear();
            data.AgentList.Clear();
        }

        public string[] GetUnlockedHulls() => UnlockedHullsDict.FilterSelect((hull,unlocked) => unlocked,
                                                                             (hull,unlocked) => hull);
        public bool IsHullUnlocked(string hullName) => UnlockedHullsDict.Get(hullName, out bool unlocked) && unlocked;

        public IReadOnlyList<Troop> GetUnlockedTroops() => UnlockedTroops;

        public Map<string, bool> GetBDict() => UnlockedBuildingsDict;
        public bool IsBuildingUnlocked(string name) => UnlockedBuildingsDict.TryGetValue(name, out bool unlocked) && unlocked;
        public bool IsBuildingUnlocked(int bid) => ResourceManager.GetBuilding(bid, out Building b)
                                                        && IsBuildingUnlocked(b.Name);

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
            float mapDistance   = (Universum.UniverseSize * percentageOfMapSize).UpperBound(ownCenter.Distance(theirCenter) * 1.2f);

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

            if (p.EventsOnTiles())
                EmpireAI.SendExplorationFleet(p);

            if (CurrentGame.Difficulty <= UniverseData.GameDifficulty.Hard || p.ParentSystem.IsExclusivelyOwnedBy(this))
                return;

            if (PlanetRanker.IsGoodValueForUs(p, this) && KnownEnemyStrengthIn(p.ParentSystem).AlmostZero())
            {
                var task = MilitaryTask.CreateGuardTask(this, p);
                EmpireAI.AddPendingTask(task);
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
            DifficultyModifiers = new DifficultyModifiers(this, CurrentGame.Difficulty);
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
            InitDifficultyModifiers();
            InitPersonalityModifiers();
            CreateThrusterColors();

            for (int i = 1; i < 10; ++i)
            {
                Fleet fleet = new Fleet { Owner = this };
                fleet.SetNameByFleetIndex(i);
                FleetsDict.Add(i, fleet);
            }

            if (string.IsNullOrEmpty(data.DefaultTroopShip))
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            InitEmpireUnlocks();
        }

        // initialize empire (before universe has been created)
        public void Initialize()
        {
            InitTechTree();
            CommonInitialize();

            data.TechDelayTime = 0;
            if (EmpireManager.NumEmpires == 0)
                UpdateTimer = 0;

            EmpireAI = new EmpireAI(this, fromSave: false);
            Research.Update();
        }

        public void InitializeFromSave()
        {
            CommonInitialize();
            EmpireAI = new EmpireAI(this, fromSave: true);
            Research.SetResearchStrategy();
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

        public void ResetTechsUsableByShips(Array<Ship> ourShips, bool unlockBonuses)
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

        private void InitTechTree()
        {
            TechnologyDict.Clear(); // we allow resetting this

            foreach (var kv in ResourceManager.TechTree)
            {
                var techEntry = new TechEntry(kv.Key);

                if (techEntry.IsHidden(this))
                {
                    techEntry.SetDiscovered(false);
                }
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

        void InitEmpireUnlocks()
        {
            foreach (var hull in ResourceManager.Hulls)       UnlockedHullsDict[hull.HullName] = false;
            foreach (var tt in ResourceManager.TroopTypes)    UnlockedTroopDict[tt] = false;
            foreach (var kv in ResourceManager.BuildingsDict) UnlockedBuildingsDict[kv.Key] = false;
            foreach (var kv in ResourceManager.ShipModules)   UnlockedModulesDict[kv.Key] = false;

            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;

            foreach (var kv in TechnologyDict) //unlock racial techs
            {
                var techEntry = kv.Value;
                data.Traits.TechUnlocks(techEntry, this);
            }
            //Added by gremlin Figure out techs with modules that we have ships for.
            var ourShips = GetOurFactionShips();

            UnlockedTroops.Clear();
            ResetTechsUsableByShips(ourShips, unlockBonuses: true); // this will also unlock troops. Very confusing
            ShipsWeCanBuild.Clear();
            foreach (string ship in data.unlockShips)
                ShipsWeCanBuild.Add(ship);

            UpdateShipsWeCanBuild();
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
                    Empire checkedEmpire = ship.Loyalty;

                    if (ignoreList.Contains(checkedEmpire)
                        || ship.NotThreatToPlayer()
                        || !ship.InSensorRange)
                    {
                        ignoreList.Add(checkedEmpire);
                        continue;
                    }

                    float strRatio = StrRatio(system, checkedEmpire);
                    Universum.NotificationManager.AddBeingInvadedNotification(system, checkedEmpire, strRatio);
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

        void ScanFromAllInfluenceNodes(FixedSimTime timeStep)
        {
            InfluenceNode[] borderNodes = BorderNodes;
            for (int i = 0; i < borderNodes.Length; i++)
            {
                ScanForInfluence(ref borderNodes[i], timeStep);
            }

            InfluenceNode[] sensorNodes = SensorNodes;
            for (int i = 0; i < sensorNodes.Length; i++)
            {
                ScanForShips(ref sensorNodes[i]);
            }
        }

        void ScanForShips(ref InfluenceNode node)
        {
            if (node.SourceObject is Ship)
                return;

            // find ships in radius of node.
            GameplayObject[] targets = Universum.Spatial.FindNearby(GameObjectType.Ship, node.Position, node.Radius, maxResults:1024);
            for (int i = 0; i < targets.Length; i++)
            {
                var targetShip = (Ship)targets[i];
                targetShip.KnownByEmpires.SetSeen(this);
            }
        }

        void ScanForInfluence(ref InfluenceNode node, FixedSimTime timeStep)
        {
            // find anyone within this influence node
            GameplayObject[] targets = Universum.Spatial.FindNearby(GameObjectType.Ship, node.Position, node.Radius, maxResults:128);
            for (int i = 0; i < targets.Length; i++)
            {
                var ship = (Ship)targets[i];
                ship.SetProjectorInfluence(this, true);

                // Civilian infrastructure spotting enemy fleets
                if (node.SourceObject is Ship ssp)
                {
                    ssp.HasSeenEmpires.Update(timeStep, ssp.Loyalty);
                    if (ship.Fleet != null)
                    {
                        if (isPlayer || Universum.Debug && Universum.SelectedShip?.Loyalty == this)
                        {
                            if (IsEmpireHostile(ship.Loyalty))
                                ssp.HasSeenEmpires.SetSeen(ship.Loyalty);
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

            if (Universum.Debug)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Universum.player == this)
                return;

            if (Universum.PlayerEmpire == this)
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
                EncounterPopup.Show(Universum, Universum.PlayerEmpire, e, encounter);
            }
            else
            {
                Log.Warning($"Could not find First Contact Encounter for {e.Name}, " +
                            "make sure this faction has <FirstContact>true</FirstContact> in one of it's encounter dialog XMLs");
            }
        }

        public void Update(UniverseScreen us, FixedSimTime timeStep)
        {
            #if PLAYERONLY
                if(!this.isPlayer && !this.isFaction)
                foreach (Ship ship in this.OwnedShips)
                    ship.GetAI().OrderScrapShip();
                if (this.OwnedShips.Count == 0)
                    return;
            #endif

            // TODO: figure out another way to initialize Universe for Rebel Factions
            Universum = us;
            UpdateTimer -= timeStep.FixedTime;

            if (UpdateTimer <= 0f && !data.Defeated)
            {
                if (this == Universum.PlayerEmpire)
                {
                    Universum.UpdateStarDateAndTriggerEvents(Universum.StarDate + 0.1f);
                    StatTracker.StatUpdateStarDate(Universum.StarDate);
                    if (Universum.StarDate.AlmostEqual(1000.09f))
                    {
                        foreach (Empire empire in EmpireManager.Empires)
                        {
                            using (empire.OwnedPlanets.AcquireReadLock())
                            {
                                foreach (Planet planet in empire.OwnedPlanets)
                                    StatTracker.StatAddPlanetNode(Universum.StarDate, planet);
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
                    foreach (SolarSystem system in Universum.Systems)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (!p.IsExploredBy(Universum.PlayerEmpire) || !p.RecentCombat)
                                continue;

                            if (p.Owner != Universum.PlayerEmpire)
                            {
                                foreach (Troop troop in p.TroopsHere)
                                {
                                    if (troop?.Loyalty != Universum.PlayerEmpire) continue;
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
            }
            SetRallyPoints();
            UpdateFleets(timeStep);
        }

        // FB - for unittest only!
        public void TestAssignNewHomeWorldIfNeeded() => AssignNewHomeWorldIfNeeded();

        void AssignNewHomeWorldIfNeeded()
        {
            if (isPlayer | isFaction)
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
        public void InitEmpireFromSave() // todo FB - why is this called on new game?
        {
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
            EmpireAI.RunEconomicPlanner(fromSave: true);
            if (!isPlayer)
                EmpireAI.OffensiveForcePoolManager.ManageAOs();
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

        public float GetTroopMaintThisTurn()
        {
            // Troops maintenance on ships are calculated as part of ship maintenance
            int troopsOnPlanets;
            using (OwnedPlanets.AcquireReadLock())
                troopsOnPlanets = OwnedPlanets.Sum(p => p.TroopsHere.Count);

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

            using (OwnedPlanets.AcquireReadLock())
                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    Planet p = OwnedPlanets[i];
                    p.UpdateIncomes(false);
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
            ResetMoneySpentOnProduction();
            // expensive lock with several loops.
            using (OwnedPlanets.AcquireReadLock())
            {
                Planet[] planetsToUpdate = OwnedPlanets.Sorted(p => p.ColonyPotentialValue(this));

                TotalProdExportSlots = OwnedPlanets.Sum(p => p.FreeProdExportSlots); // Done before UpdateOwnedPlanet
                for (int i = 0; i < planetsToUpdate.Length; i++)
                {
                    Planet planet = OwnedPlanets[i];
                    planet.UpdateOwnedPlanet();
                }
            }
        }

        public void GovernPlanets()
        {
            if (!isFaction && !data.Defeated)
                UpdateMaxColonyValues();

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
            if (!isFaction) return;
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

            foreach (Ship ship in ResourceManager.ShipTemplates)
            {
                if (hulls != null && !hulls.Contains(ship.ShipData.Hull))
                    continue;

                // we can already build this
                if (ShipsWeCanBuild.Contains(ship.Name))
                    continue;
                if (!ship.CanBeAddedToBuildableShips(this))
                    continue;

                if (WeCanBuildThis(ship.Name))
                {
                    if (ship.ShipData.Role <= RoleName.station)
                        structuresWeCanBuild.Add(ship.Name);

                    bool shipAdded = ShipsWeCanBuild.Add(ship.Name);

                    if (isPlayer)
                        Universum?.aw?.UpdateDropDowns();

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
            return WeCanBuildThis(shipData);
        }

        public bool WeCanBuildThis(string shipName)
        {
            if (!ResourceManager.Ships.GetDesign(shipName, out IShipDesign shipData))
            {
                Log.Warning($"Ship does not exist: {shipName}");
                return false;
            }

            return WeCanBuildThis(shipData);
        }

        bool WeCanBuildThis(IShipDesign shipData)
        {
            // If this hull is not unlocked, then we can't build it
            if (!IsHullUnlocked(shipData.Hull))
                return false;

            if (shipData.TechsNeeded.Count > 0)
            {
                if (!shipData.Unlockable)
                    return false;

                foreach (string shipTech in shipData.TechsNeeded)
                {
                    if (ShipTechs.Contains(shipTech))
                        continue;
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
                foreach (string moduleUID in shipData.UniqueModuleUIDs)
                {
                    if (!IsModuleUnlocked(moduleUID))
                    {
                        //Log.Info($"Locked module : '{moduleSlotData.InstalledModuleUID}' in design : '{ship}'");
                        return false; // can't build this ship because it contains a locked Module
                    }
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
                .OrderBy(distance => Vector2.Distance(distance.Position, objectCenter)));

            if (troopShips.Count > 0)
                troopShip = troopShips.First();

            return troopShip != null;
        }

        public int NumFreeTroops()
        {
            int numTroops;
            numTroops = OwnedShips.Filter(s => s.IsIdleSingleTroopship).Length +
                        OwnedPlanets.Sum(p => p.NumTroopsCanLaunch);

            return numTroops;
        }

        private bool LaunchNearestTroopForRebase(out Ship troopShip, Vector2 objectCenter, string planetName = "")
        {
            troopShip = null;
            Array<Planet> candidatePlanets = new Array<Planet>(OwnedPlanets
                .Filter(p => p.NumTroopsCanLaunch > 0 && p.TroopsHere.Any(t => t.CanLaunch) && p.Name != planetName)
                .OrderBy(distance => Vector2.Distance(distance.Center, objectCenter)));

            if (candidatePlanets.Count == 0)
                return false;

            var troops = candidatePlanets.First().TroopsHere;
            using (troops.AcquireWriteLock())
            {
                troopShip = troops.FirstOrDefault(t => t.Loyalty == this)?.Launch();
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
                        Universum.NotificationManager.AddCapitalTransfer(p, newHomeworld);
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
            using (OwnedPlanets.AcquireReadLock())
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

        readonly Array<InfluenceNode> TempSensorNodes = new Array<InfluenceNode>();
        readonly Array<InfluenceNode> TempBorderNodes = new Array<InfluenceNode>();

        /// <summary>
        /// Border nodes are used to show empire influence.
        /// Sensor nodes are used to show the sensor range of things. Ship, planets, spys, etc
        /// This process uses object recycling.
        /// the object is put into the pending removals portion of the batchremoval class.
        /// when a new node is wanted it is pulled from the pending pool, wiped and used or created new.
        /// this should cut down on garbage collection as the objects are cycled often.
        /// </summary>
        void ResetBorders(UniverseScreen us)
        {
            bool wellKnown = isPlayer || us.player.IsAlliedWith(this) ||
                Universum.Debug && (Universum.SelectedShip == null || Universum.SelectedShip.Loyalty == this);
            bool known = wellKnown || us.player.IsTradeOrOpenBorders(this);

            SetBordersKnownByAllies(TempSensorNodes);
            SetBordersByPlanet(known, TempBorderNodes, TempSensorNodes);

            float projectorRadius = GetProjectorRadius();

            // Moles are spies who have successfully been planted during 'Infiltrate' type missions, I believe - Doctor
            foreach (Mole mole in data.MoleList)
            {
                var p = us.GetPlanet(mole.PlanetGuid);
                if (p == null)
                    continue;
                TempSensorNodes.Add(new InfluenceNode
                {
                    Position = p.Center, Radius = projectorRadius, KnownToPlayer = true
                });
            }

            Ship[] ships = EmpireShips.OwnedShips;
            for (int i = 0; i < ships.Length; i++)
            {
                Ship ship = ships[i];
                TempSensorNodes.Add(new InfluenceNode(ship, wellKnown));
            }

            Ship[] projectors = EmpireShips.OwnedProjectors;
            for (int i = 0; i < projectors.Length; i++)
            {
                Ship ship = projectors[i];
                var node = new InfluenceNode(ship, known)
                {
                    Radius = projectorRadius
                };
                TempSensorNodes.Add(node);
                TempBorderNodes.Add(node);
            }

            SetPirateBorders(TempBorderNodes);
            SetRemnantPortalBorders(TempBorderNodes);

            SensorNodes = TempSensorNodes.ToArray();
            BorderNodes = TempBorderNodes.ToArray();
            TempSensorNodes.Clear();
            TempBorderNodes.Clear();
        }

        void SetPirateBorders(Array<InfluenceNode> borderNodes)
        {
            if (WeArePirates && Pirates.GetBases(out Array<Ship> bases))
                for (int i = 0; i < bases.Count; i++)
                    borderNodes.Add(new InfluenceNode(bases[i]));
        }

        void SetRemnantPortalBorders(Array<InfluenceNode> borderNodes)
        {
            if (WeAreRemnants && Remnants.GetPortals(out Ship[] portals))
                for (int i = 0; i < portals.Length; i++)
                    borderNodes.Add(new InfluenceNode(portals[i]));
        }

        void SetBordersByPlanet(bool empireKnown, Array<InfluenceNode> borderNodes, Array<InfluenceNode> sensorNodes)
        {
            Planet[] planets = OwnedPlanets.ToArray();
            foreach (Planet planet in planets)
            {
                bool known = empireKnown || planet.IsExploredBy(EmpireManager.Player);

                var borderNode = new InfluenceNode(planet, known);
                sensorNodes.Add(borderNode);

                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.usePlanetaryProjection)
                    borderNode.Radius = planet.ProjectorRange;
                else
                    borderNode.Radius = GetProjectorRadius(planet);
                borderNodes.Add(borderNode);
            }
        }

        void SetBordersKnownByAllies(Array<InfluenceNode> sensorNodes)
        {
            bool isPlayerInDebug = Universum.Debug && isPlayer && Universum.SelectedShip == null;
            foreach(var empire in EmpireManager.Empires)
            {
                if (GetRelations(empire, out Relationship relation) &&
                    (relation.Treaty_Alliance || isPlayerInDebug))
                {
                    Planet[] planets = empire.OwnedPlanets.ToArray();
                    for (int y = 0; y < planets.Length; y++)
                        sensorNodes.Add(new InfluenceNode(planets[y], true));

                    Ship[] ships = empire.EmpireShips.OwnedShips;
                    for (int z = 0; z < ships.Length; z++)
                        sensorNodes.Add(new InfluenceNode(ships[z], true));

                    // loop over all ALLIED projectors
                    Ship[] projectors = empire.EmpireShips.OwnedProjectors;
                    for (int z = 0; z < projectors.Length; z++)
                        sensorNodes.Add(new InfluenceNode(projectors[z], true));
                }
            }
        }

        public void TryCreateAssaultBombersGoal(Empire enemy, Planet planet)
        {
            if (enemy == this  || EmpireAI.Goals.Any(g => g.type == GoalType.AssaultBombers && g.PlanetBuildingAt == planet))
                return;

            var goal = new AssaultBombers(planet, this, enemy);
            EmpireAI.Goals.Add(goal);
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

            var g = new FleetRequisition(ship.Name, this, false) { Fleet = fleet };
            node.GoalGUID = g.guid;
            EmpireAI.Goals.Add(g);
            g.Evaluate();
        }

        private void TakeTurn(UniverseScreen us)
        {
            if (IsEmpireDead())
                return;

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
                if (StatTracker.GetSnapshot(Universum.StarDate, this, out Snapshot snapshot))
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
                RandomEventManager.UpdateEvents(Universum);

                if ((Money / AllSpending.LowerBound(1)) < 2)
                    Universum.NotificationManager.AddMoneyWarning();

                if (!Universum.NoEliminationVictory)
                {
                    bool allEmpiresDead = true;
                    foreach (Empire empire in EmpireManager.Empires)
                    {
                        var planets = empire.GetPlanets();
                        if (planets.Count > 0 && !empire.isFaction && empire != this)
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
                            Universum.ScreenManager.AddScreen(new YouWinScreen(Universum));
                            Universum.GameOver = true;
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
                        Universum.ScreenManager.AddScreen(new YouWinScreen(Universum, Localizer.Token(GameText.AsTheRemnantExterminatorsSweep)));
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                if (StatTracker.GetSnapshot(Universum.StarDate, this, out Snapshot snapshot))
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

            if (!isFaction)
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

            Empire empire = EmpireManager.GetEmpireById(DiplomacyContactQueue.First().Key);
            string dialog = DiplomacyContactQueue.First().Value;

            if (dialog == "DECLAREWAR")
                empire.GetEmpireAI().DeclareWarOn(this, WarType.ImperialistWar);
            else
                DiplomacyScreen.ContactPlayerFromDiplomacyQueue(empire, dialog);

            DiplomacyContactQueue.RemoveAt(0);
        }


        void CheckFederationVsPlayer(UniverseScreen us)
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
                    Universum.NotificationManager.AddPeacefulMergerNotification(biggestAI, strongest);
                else
                    Universum.NotificationManager.AddSurrendered(biggestAI, strongest);

                biggestAI.AbsorbEmpire(strongest);
                if (biggestAI.GetRelations(this).ActiveWar == null)
                    biggestAI.GetEmpireAI().DeclareWarOn(this, WarType.ImperialistWar);
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
                            Universum.NotificationManager.AddRebellionNotification(planet,
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
            if (isFaction) return false;
            if (data.Defeated) return true;
            if (!GlobalStats.EliminationMode && OwnedPlanets.Count != 0)
                return false;
            if (GlobalStats.EliminationMode && (Capital == null || Capital.Owner == this) && OwnedPlanets.Count != 0)
                return false;

            SetAsDefeated();
            if (!isPlayer)
            {
                if (EmpireManager.Player.IsKnown(this))
                    Universum.NotificationManager.AddEmpireDiedNotification(this);
                return true;
            }

            StarDriveGame.Instance?.EndingGame(true);
            Universum.GameOver = true;
            Universum.Paused = true;
            Universum.Objects.Clear();
            HelperFunctions.CollectMemory();
            StarDriveGame.Instance?.EndingGame(false);
            Universum.ScreenManager.AddScreen(new YouLoseScreen(Universum));
            Universum.Paused = false;
            return true;
        }

        public void MassScrap(Ship ship)
        {
            var shipList = ship.IsSubspaceProjector ? OwnedShips : OwnedProjectors;
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
                    Universum.NotificationManager.AddScrapUnlockNotification(message, modelIcon, "ShipDesign");
                }
                else
                {
                    string message = $"{hullString}{Localizer.Token(GameText.HullScrappedAdvancingResearch)}";
                    Universum.NotificationManager.AddScrapProgressNotification(message, modelIcon, "ResearchScreen", hullTech.UID);
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
            Universum.NotificationManager?.AddBoardNotification(Localizer.Token(GameText.ShipCapturedByYou),
                                                              ship.BaseHull.IconPath, "SnapToShip", ship, null);
        }

        public void AddBoardedNotification(Ship ship, Empire boarder)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(GameText.YourShipWasCaptured)} {boarder.Name}!";
            Universum.NotificationManager?.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, boarder);
        }

        public void AddMutinyNotification(Ship ship, GameText text, Empire initiator)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(text)} {initiator.Name}!";
            Universum.NotificationManager.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, initiator);
        }

        void CalculateScore(bool fromSave = false)
        {
            TechScore = 0;
            foreach (KeyValuePair<string, TechEntry> keyValuePair in TechnologyDict)
                if (keyValuePair.Value.Unlocked)
                    TechScore += ResourceManager.Tech(keyValuePair.Key).Cost;
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

            var ourShips = GetOurFactionShips();
            ResetTechsUsableByShips(ourShips, unlockBonuses: false);

            target.data.OwnedArtifacts.Clear();
            if (target.Money > 0.0)
            {
                Money += target.Money;
                target.Money = 0.0f;
            }

            target.SetAsMerged();
            ResetBorders(Universum);
            UpdateShipsWeCanBuild();

            if (this != Universum.player)
            {
                EmpireAI.EndAllTasks();
                EmpireAI.DefensiveCoordinator.DefensiveForcePool.Clear();
                EmpireAI.DefensiveCoordinator.DefenseDict.Clear();
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

            if (GlobalStats.ActiveModInfo?.RemoveRemnantStory == true)
                return false;

            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(killedShip);
            Remnants.IncrementKills(they, (int)killedExpSettings.KillExp);
            return true;
        }

        void AssignSniffingTasks()
        {
            if (!isPlayer && EmpireAI.Goals.Count(g => g.type == GoalType.ScoutSystem) < DifficultyModifiers.NumSystemsToSniff)
                EmpireAI.Goals.Add(new ScoutSystem(this));
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

            int unexplored = Universum.Systems.Count(s => !s.IsFullyExploredBy(this)).UpperBound(21);
            var ships = OwnedShips;
            if (unexplored == 0 && isPlayer)
            {
                for (int i = 0; i < ships.Count; i++)
                {
                    Ship ship = ships[i];
                    if (IsIdleScout(ship))
                        ship.AI.OrderScrapShip();
                }
                return;
            }

            float desiredScouts = unexplored * Research.Strategy.ExpansionRatio;
            if (!isPlayer)
                desiredScouts *= ((int)CurrentGame.Difficulty).LowerBound(1);

            int numScouts = 0;
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (IsIdleScout(ship))
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
            bool IsIdleScout(Ship s)
            {
                if (s.ShipData.Role == RoleName.supply)
                    return false; // FB - this is a workaround, since supply shuttle register as scouts design role.

                return s.AI.State != AIState.Flee
                       && s.AI.State != AIState.Scrap
                       && s.AI.State != AIState.Explore
                       && (isPlayer && s.Name == data.CurrentAutoScout
                           || !isPlayer && s.DesignRole == RoleName.scout && s.Fleet == null);
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
        }

        void IEmpireShipLists.RemoveShipAtEndOfTurn(Ship s) => EmpireShips?.Remove(s);

        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null, bool scanOnly = false)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if ((rel.CanAttack && target == null) || (scanOnly && !rel.Known))
                return true;

            return target?.IsAttackable(this, rel) ?? false;
        }

        public bool IsEmpireScannedAsEnemy(Empire targetEmpire, GameplayObject target = null)
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

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.Guid == planetGuid)
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

            Universum.ResetBordersPerf.Start();
            {
                ResetBorders(us);
            }
            Universum.ResetBordersPerf.Stop();

            Universum.ScanInfluencePerf.Start();
            {
                ScanFromAllInfluenceNodes(timeStep);
            }
            Universum.ScanInfluencePerf.Stop();

            CheckForFirstContacts();

            ThreatMatrixUpdateTimer -= timeStep.FixedTime;
            if (ThreatMatrixUpdateTimer <= 0f)
            {
                ThreatMatrixUpdateTimer = ResetThreatMatrixSeconds;

                Universum.ThreatMatrixPerf.Start();
                Parallel.Run(() => EmpireAI.ThreatMatrix.UpdateAllPins(this));
                Universum.ThreatMatrixPerf.Stop();
            }
        }

        public void IncrementCordrazineCapture()
        {
            if (!GlobalStats.CordrazinePlanetCaptured)
                Universum.NotificationManager.AddNotify(ResourceManager.EventsDict["OwlwokFreedom"]);

            GlobalStats.CordrazinePlanetCaptured = true;
        }

        void CheckForFirstContacts()
        {
            // mark known empires that already have done first contact
            var knownEmpires = new SmallBitSet();
            int numUnknown = 0;
            for (int i = 0; i < EmpireManager.NumEmpires; ++i)
            {
                Empire other = EmpireManager.Empires[i];
                if (other != this && !IsKnown(other))
                    ++numUnknown;
                else
                    knownEmpires.Set(other.Id);
            }

            // we already known everyone in the universe
            // this is an important optimization for late-game where we have 5000+ ships
            if (numUnknown == 0)
                return;

            // this part is quite heavy, we go through all of our ship's
            // scan results and try to find any first contact ships
            Ship[] ourShips = EmpireShips.OwnedShips;
            for (int i = 0; i < ourShips.Length; ++i)
            {
                Ship ship = ourShips[i];
                Ship[] enemyShips = ship.AI.PotentialTargets;
                for (int j = 0; j < enemyShips.Length; ++j)
                {
                    Ship enemy = enemyShips[j];
                    Empire other = enemy.Loyalty;
                    if (!knownEmpires.IsSet(other.Id))
                    {
                        DoFirstContact(other);
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

        public void RestoreDiplomacyConcatQueue(List<KeyValuePair<int, string>> diplomacyContactQueue)
        {
            if (diplomacyContactQueue != null)
                DiplomacyContactQueue = diplomacyContactQueue;
        }

        public void SetCapital(Planet planet)
        {
            Capital = planet;
        }

        public void ResetAllTechsAndBonuses()
        {
            Research.Reset(); // clear research progress bar and queue
            Research.SetNoResearchLeft(false);
            foreach (TechEntry techEntry in TechEntries)
            {
                techEntry.ResetUnlockedTech();
            }

            data.ResetAllBonusModifiers(this);

            InitEmpireUnlocks();
        }

        public struct InfluenceNode
        {
            public Vector2 Position;
            public float Radius;
            public object SourceObject; // SolarSystem, Planet OR Ship
            public bool KnownToPlayer;

            public InfluenceNode(Planet planet, bool known)
            {
                Position      = planet.Center;
                Radius        = planet.SensorRange;
                SourceObject  = planet;
                KnownToPlayer = known;
            }
            public InfluenceNode(Ship ship, bool known = false)
            {
                Position      = ship.Position;
                Radius        = ship.SensorRange;
                SourceObject  = ship;
                KnownToPlayer = known || ship.InSensorRange;
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
            BorderNodes = Empty<InfluenceNode>.Array;
            SensorNodes = Empty<InfluenceNode>.Array;
            TempSensorNodes.Clear();
            TempBorderNodes.Clear();
        }

        void Destroy()
        {
            if (EmpireAI == null)
                return; // Already disposed

            EmpireAI = null;
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
            UnlockedBuildingsDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedTroopDict.Clear();
            UnlockedTroops.Clear();
            Inhibitors.Clear();
            ShipsWeCanBuild.Clear();
            structuresWeCanBuild.Clear();
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
            OwnedPlanets?.Dispose(ref OwnedPlanets);
            OwnedSolarSystems?.Dispose(ref OwnedSolarSystems);

            AIManagedShips = null;
            EmpireShips = null;
        }

        public override string ToString() => $"{(isPlayer?"Player":"AI")}({Id}) '{Name}'";
    }
}
