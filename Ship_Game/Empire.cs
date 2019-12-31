using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Ship_Game
{
    using static ShipBuilder;
    public enum TechUnlockType
    {
        Normal,
        Spy,
        Diplomacy,
        Event
    }
    public sealed partial class Empire : IDisposable
    {
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! Empire must not be serialized! Add [XmlIgnore][JsonIgnore] to `public Empire XXX;` PROPERTIES/FIELDS. {this}");

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

        float MoneyValue = 1000f;
        public float Money
        {
            get => MoneyValue;
            set => MoneyValue = value.NaNChecked(0f, "Empire.Money");
        }

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
        private Fleet DefensiveFleet = new Fleet();
        private Array<Ship> ForcePool = new Array<Ship>();
        public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName;

        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesn't normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        public bool isFaction;

        public Color EmpireColor;
        public static UniverseScreen Universe;
        private EmpireAI EmpireAI;
        private float UpdateTimer;
        public bool isPlayer;
        public float TotalShipMaintenance { get; private set; }
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
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue          { get; private set; }
        public Ship BestPlatformWeCanBuild   { get; private set; }
        public Ship BestStationWeCanBuild    { get; private set; }
        public int ColonyRankModifier        { get; private set; }
        public HashSet<string> ShipTechs = new HashSet<string>();

        [XmlIgnore][JsonIgnore] public byte[,] grid;
        [XmlIgnore][JsonIgnore] public int granularity = 0;
        [XmlIgnore][JsonIgnore] public int AtWarCount;
        [XmlIgnore][JsonIgnore] public Array<string> BomberTech      = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> TroopShipTech   = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> CarrierTech     = new Array<string>();
        [XmlIgnore][JsonIgnore] public Array<string> SupportShipTech = new Array<string>();
        [XmlIgnore][JsonIgnore] public Planet[] RallyPoints     = Empty<Planet>.Array;
        [XmlIgnore][JsonIgnore] public Ship BoardingShuttle     => ResourceManager.ShipsDict["Assault Shuttle"];
        [XmlIgnore][JsonIgnore] public Ship SupplyShuttle       => ResourceManager.ShipsDict["Supply_Shuttle"];
        [XmlIgnore][JsonIgnore] public bool IsCybernetic        => data.Traits.Cybernetic != 0;
        [XmlIgnore][JsonIgnore] public bool NonCybernetic       => data.Traits.Cybernetic == 0;

        public Dictionary<ShipData.RoleName, string> PreferredAuxillaryShips = new Dictionary<ShipData.RoleName, string>();

        // Income this turn before deducting ship maintenance
        public float GrossIncome              => GrossPlanetIncome + TotalTradeMoneyAddedThisTurn + ExcessGoodsMoneyAddedThisTurn + data.FlatMoneyBonus;
        public float NetIncome                => GrossIncome - BuildingAndShipMaint;
        public float TotalBuildingMaintenance => GrossPlanetIncome - NetPlanetIncomes;
        public float BuildingAndShipMaint     => TotalBuildingMaintenance + TotalShipMaintenance;

        public Planet[] SpacePorts       => OwnedPlanets.Filter(p => p.HasSpacePort);
        public Planet[] MilitaryOutposts => OwnedPlanets.Filter(p => p.AllowInfantry); // Capitals allow Infantry as well
        public Planet[] SafeSpacePorts   => OwnedPlanets.Filter(p => p.HasSpacePort && !p.EnemyInRange());

        public readonly EmpireResearch Research;

        // Empire unique ID. If this is 0, then this empire is invalid!
        // Set in EmpireManager.cs
        public int Id;

        public string Name => data.Traits.Name;

        public Empire()
        {
            Research = new EmpireResearch(this);

            // @note @todo This is very flaky and weird!
            UpdateTimer = RandomMath.RandomBetween(.02f, .3f);
        }

        public Empire(Empire parentEmpire)
        {
            Research = new EmpireResearch(this);
            TechnologyDict = parentEmpire.TechnologyDict;
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
            return RallyPoints.FindMin( p => p.Center.SqDist(location))
                ?? OwnedPlanets.FindMin(p => p.Center.SqDist(location));
        }

        public Planet RallyShipYardNearestTo(Vector2 position)
        {
            return RallyPoints.FindMinFiltered(p => p.HasSpacePort,
                                               p => position.SqDist(p.Center))
                ?? SpacePorts.FindMin(p => position.SqDist(p.Center))
                ?? FindNearestRallyPoint(position);
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
            if (ports.Count != 0)
            {
                return ports.FindMin(p => p.TurnsUntilQueueComplete(cost));
            }
            return null;
        }

        public WeaponTagModifier WeaponBonuses(WeaponTag which) => data.WeaponTags[which];
        public Map<int, Fleet> GetFleetsDict() => FleetsDict;
        public Fleet GetFleetOrNull(int key) => FleetsDict.TryGetValue(key, out Fleet fleet) ? fleet : null;

        public Fleet FirstFleet
        {
            get => FleetsDict[1];
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
                return;
            }

            rallyPlanets = new Array<Planet>();
            foreach (Planet planet in OwnedPlanets)
            {
                if (planet.HasSpacePort && !planet.EnemyInRange())
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
            ForcePool.Clear();
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
            ShipsToAdd.Clear();
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
                if (tech.CanBeGivenTo(them))
                    tradeTechs.Add(tech);
            }
            return tradeTechs;
        }

        public Array<TechEntry> CurrentTechsResearchable()
        {
            var availableTechs = new Array<TechEntry>();

            foreach (TechEntry tech in TechEntries)
            {
                if (!tech.Unlocked && tech.Discovered && tech.shipDesignsCanuseThis && HavePreReq(tech.UID))
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

        public Array<Ship> GetShipsFromOffensePools(bool onlyAO = false)
        {
            var ships = new Array<Ship>();
            foreach (AO ao in GetEmpireAI().AreasOfOperations)
                ships.AddRange(ao.GetOffensiveForcePool());

            if(!onlyAO)
                ships.AddRange(ForcePool);
            return ships;
        }

        public BatchRemovalCollection<Ship> GetProjectors() => OwnedProjectors;

        public void AddShip(Ship s)
        {
            if (s.IsSubspaceProjector)
                OwnedProjectors.Add(s);
            else
                OwnedShips.Add(s);
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
            InitColonyRankModifier();
            CreateThrusterColors();
            EmpireAI = new EmpireAI(this);

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


            InitColonyRankModifier();
            CreateThrusterColors();
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
                case TechUnlockType.Normal:
                case TechUnlockType.Event:
                    if (techEntry.Unlock(this))
                    {
                        UpdateForNewTech();
                    }
                    break;
                case TechUnlockType.Diplomacy:
                    if (techEntry.UnlockFromDiplomacy(this, otherEmpire))
                    {
                        UpdateForNewTech();
                    }
                    break;
                case TechUnlockType.Spy:
                    if (techEntry.UnlockFromSpy(this, otherEmpire))
                    {
                        UpdateForNewTech();
                    }
                    break;
            }
        }

        void UpdateForNewTech()
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

            if (isPlayer && Universe.Debug) // if Debug overlay is enabled, make all ships visible
            {
                for (int i = 0; i < Universe.MasterShipList.Count; ++i)
                {
                    Ship nearby = Universe.MasterShipList[i];
                    if (nearby.Active)
                    {
                        nearby.inSensorRange = true;
                        UpdateShipInfluence(nearby, influenceNodes);
                        KnownShips.Add(nearby);
                        EmpireAI.ThreatMatrix.UpdatePin(nearby);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Universe.MasterShipList.Count; ++i)
                {
                    Ship nearby = Universe.MasterShipList[i];
                    if (nearby.Active && UpdateShipInfluence(nearby, influenceNodes))
                        KnownShips.Add(nearby);
                }
            }
        }

        bool UpdateShipInfluence(Ship nearby, InfluenceNode[] influenceNodes)
        {
            if (nearby.loyalty != this) // update from another empire
            {
                bool inSensorRadius = false;
                bool border = false;

                for (int i = 0; i < influenceNodes.Length; i++)
                {
                    InfluenceNode node = influenceNodes[i];
                    if (nearby.Center.InRadius(node.Position, node.Radius))
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
                    DiplomacyScreen.Show(e, "First Contact");
                }
                else if (Universe.PlayerEmpire == this && e.isFaction)
                {
                    foreach (Encounter e1 in ResourceManager.Encounters)
                    {
                        if (e1.Faction == e.data.Traits.Name && e1.Name == "First Contact")
                        {
                            EncounterPopup.Show(Universe, Universe.PlayerEmpire, e, e1);
                        }
                    }
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
            UpdateMilitaryStrengths();

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

        private void UpdateMilitaryStrengths()
        {
            CurrentMilitaryStrength = 0;
            CurrentTroopStrength    = 0;

            for (int index = 0; index < OwnedShips.Count; ++index)
            {
                Ship ship = OwnedShips[index];
                if (ship != null)
                {
                    if (ship.DesignRoleType == ShipData.RoleType.Troop)
                        foreach (Troop t in ship.TroopList)
                            CurrentTroopStrength += t.Strength;
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
            UpdateNetPlanetIncomes();
            UpdateShipMaintenance();
            Money += NetIncome;
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
            {
                UpdateMaxColonyValue();
                UpdateBestOrbitals();
            }

            using (OwnedPlanets.AcquireReadLock())
                foreach (Planet planet in OwnedPlanets)
                    planet.DoGoverning();
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

            foreach (KeyValuePair<string, Ship> kv in ResourceManager.ShipsDict)
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
                                        && troopship.TroopList.Count > 0
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
                .Filter(p => p.CountEmpireTroops(this) > p.GarrisonSize
                             && !p.MightBeAWarZone(this)
                             && p.Name != planetName)
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
                    Position = Universe.GetPlanet(mole.PlanetGuid).Center,
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
                foreach (Building t in planet.BuildingList) // FB - change this to the planet sensorRange
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
                    influenceNode.Radius = ProjectorRadius; //projectors currently use their projection radius as sensors
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
                if (data.TurnsBelowZero > 3 && Money < 0.0)
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

                foreach (Planet planet in OwnedPlanets)
                {
                    if (!data.IsRebelFaction)
                        StatTracker.SnapshotsDict[Universe.StarDateString][EmpireManager.Empires.IndexOf(this)].Population += planet.Population;
                    if (planet.HasWinBuilding)
                    {
                        Universe.ScreenManager.AddScreenDeferred(new YouWinScreen(Universe, Localizer.Token(5085)));
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                StatTracker.SnapshotsDict[Universe.StarDateString][EmpireManager.Empires.IndexOf(this)]
                    .Population = OwnedPlanets.Sum(p => p.Population);
            }

            Research.Update();

            if (data.TurnsBelowZero > 0 && Money < 0.0 && !Universe.Debug)
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
                DispatchBuildAndScrapFreighters();
                AssignExplorationTasks();
            }
        }

        void UpdateAI()
        {
            if (isFaction)
               EmpireAI.FactionUpdate();
            else if (!data.Defeated)
                EmpireAI.Update();
        }

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

                        for (int index = 0; index < planet.PopulationBillion; ++index)
                        {
                            Troop troop = EmpireManager.CreateRebelTroop(rebels);

                            var chance = (planet.TileArea - planet.FreeTiles) / planet.TileArea;
                            
                            if (RandomMath.RollDiceAvg(chance * 50))
                            {
                                var t = RandomMath.RandItem(planet.TroopsHere);
                                if (t != null)
                                    troop.ChangeLoyalty(rebels);
                            }

                            if (RandomMath.RollDiceAvg(chance * 50))
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

        void UpdateRelationships()
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
            if (s.shipData.Role > ShipData.RoleName.freighter && s.shipData.ShipCategory != ShipData.Category.Civilian)
            {
                EmpireAI.AssignShipToForce(s);
            }
        }

        public void ForcePoolRemove(Ship s) => ForcePool.RemoveRef(s);
        public bool ForcePoolContains(Ship s) => ForcePool.ContainsRef(s);

        public Array<Ship> GetForcePool() => ForcePool;

        public float GetForcePoolStrength()
        {
            float num = 0.0f;
            foreach (Ship ship in ForcePool)
                num += ship.GetStrength();
            return num;
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
            GlobalStats.IncrementRemnantKills((int)killedExpSettings.KillExp);
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
            if (EmpireAI.HasGoal(GoalType.BuildScout))
                return;

            var desiredScouts = unexplored * Research.Strategy.ExpansionRatio * .5f;
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

            GetEmpireAI().DefensiveCoordinator.Remove(ship);

            ship.AI.ClearOrders();
            ship.ClearFleet();
        }

        public void RemoveShipFromAOs(Ship ship)
        {
            Array<AO> aos = GetEmpireAI().AreasOfOperations;
            for (int x = 0; x < aos.Count; x++)
            {
                var ao = aos[x];
                if (ao.RemoveShip(ship))
                    break;
            }
        }

        public bool IsEmpireAttackable(Empire targetEmpire, GameplayObject target = null)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            if (!TryGetRelations(targetEmpire, out Relationship rel) || rel == null)
                return false;
            if(!rel.Known || rel.AtWar)
                return true;
            if (rel.Treaty_NAPact || rel.Treaty_Peace)
                return false;
            if (isFaction || targetEmpire.isFaction)
                return true;
            if (rel.TotalAnger > 50)
                return true;

            if (target == null)
                return true; // this is an inanimate check, so it won't cause trouble?
            //CG:there is an additional check that can be done for the ship itself. 
            //if no target is applied then it is assumed the target is attackable at this point. 
            //but an additional check can be done if a gameplay object is passed. 
            //maybe its a freighter or something along those lines which might not be attackable. 
            return target.IsAttackable(this, rel);
        }

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in this.OwnedPlanets)
                if (p.guid == planetGuid)
                    return p;
            return null;
        }

        public bool UpdateContactsAndBorders(float elapsedTime)
        {
            bool bordersChanged = false;
            if (updateContactsTimer < 0f && !data.Defeated)
            {
                int oldBorderNodesCount = BorderNodes.Count;
                ResetBorders();
                bordersChanged = (BorderNodes.Count != oldBorderNodesCount);

                UpdateKnownShips();
                updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
            }
            updateContactsTimer -= elapsedTime;
            return bordersChanged;
        }

        public void AddShipsToForcePoolFromShipsToAdd()
        {
            foreach (Ship s in ShipsToAdd)
            {
                AddShip(s);
                if (!isPlayer) ForcePoolAdd(s);
            }
            ShipsToAdd.Clear();
        }

        public void ResetForcePool()
        {
            //I am guessing the point of this is to filter ships out of the forcepool
            //that should not be in it/
            //I think this might be a hack to cover up a bug.
            if (!isPlayer)
            {
                Ship[] forcePool = ForcePool.ToArray();
                ForcePool.Clear();
                for (int i = 0; i < forcePool.Length; ++i)
                    ForcePoolAdd(forcePool[i]);
            }
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
            ForcePool = null;
            BorderNodes?.Dispose(ref BorderNodes);
            SensorNodes?.Dispose(ref SensorNodes);
            KnownShips?.Dispose(ref KnownShips);
            OwnedShips?.Dispose(ref OwnedShips);
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
