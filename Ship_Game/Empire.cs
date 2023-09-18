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
using Ship_Game.Data.Serialization;
using Ship_Game.Empires.Components;
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
        [StarData] public Map<string, TechEntry> TechnologyDict;

        [StarData] public readonly Map<string, bool> UnlockedHullsDict;
        [StarData] readonly Map<string, bool> UnlockedTroopDict;
        [StarData] readonly Map<string, bool> UnlockedBuildingsDict;
        [StarData] readonly Map<string, bool> UnlockedModulesDict;

        [StarData] readonly Array<Troop> UnlockedTroops;
        [StarData] public Array<Ship> Inhibitors;

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

        [StarData] public IncomingThreatDetector ThreatDetector;
        public IncomingThreat[] SystemsWithThreat => ThreatDetector.SystemsWithThreat;

        int TurnCount = 1;

        [StarData] public EmpireData data;
        public DiplomacyDialog dd;
        public string PortraitName => data.PortraitName;
        public bool ScanComplete = true;

        [StarData] public bool AutoTaxes;

        // faction means it's not an actual Empire like Humans or Kulrathi
        // it doesn't normally colonize or make war plans.
        // it gets special instructions, usually event based, for example Corsairs
        [StarData] public bool IsFaction;

        // For Pirate Factions. This will allow the Empire to be pirates
        [StarData] public Pirates Pirates { get; private set; }

        // For Remnants Factions. This will allow the Empire to be Remnants
        [StarData] public Remnants Remnants { get; private set; }

        [StarData] public Color EmpireColor;

        [StarData] public UniverseState Universe;

        [StarData] public EmpireAI AI;
        public ThreatMatrix Threats => AI.ThreatMatrix;

        float UpdateTimer;

        [StarData] public bool isPlayer;
        [StarData] public bool IsDefeated { get; private set; }

        public float TotalShipMaintenance { get; private set; }
        public float TotalWarShipMaintenance { get; private set; }
        public float TotalCivShipMaintenance { get; private set; }
        public float TotalEmpireSupportMaintenance { get; private set; }
        public float TotalOrbitalMaintenance { get; private set; }
        public float TotalMaintenanceInScrap { get; private set; }
        public float TotalTroopShipMaintenance { get; private set; }

        public float NetPlanetIncomes { get; private set; }
        public float TroopCostOnPlanets { get; private set; } // Maintenance in all Owned planets
        public float TroopInSpaceFoodNeeds { get; private set; }
        public float TotalFoodPerColonist { get; private set; }
        public float GrossPlanetIncome { get; private set; }
        public float PotentialIncome { get; private set; }
        public float ExcessGoodsMoneyAddedThisTurn { get; private set; } // money tax from excess goods
        public float MoneyLastTurn;
        public int AllTimeTradeIncome;
        [StarData] public bool AutoBuildSpaceRoads;
        [StarData] public bool AutoExplore;
        [StarData] public bool AutoColonize;
        [StarData] public bool AutoResearch;
        [StarData] public bool AutoBuildResearchStations;
        public int TotalScore;
        public float TechScore;
        public float ExpansionScore;
        public float MilitaryScore;
        public float IndustrialScore;

        // This is the original capital of the empire. It is used in capital elimination and 
        // is used in capital elimination and also to determine if another capital will be moved here if the
        // empire retakes this planet. This value should never be changed after it was set.
        [StarData] public Planet Capital { get; private set; } 

        public int EmpireShipCombat { get; private set; }    //fbedard
        public int EmpirePlanetCombat { get; private set; }  //fbedard
        public bool CanBuildCapitals { get; private set; }
        public bool CanBuildBattleships { get; private set; }
        public bool CanBuildCruisers { get; private set; }
        public bool CanBuildFrigates { get; private set; }
        public bool CanBuildCorvettes { get; private set; }
        public bool CanBuildCarriers { get; private set; }
        public bool CanBuildBombers { get; private set; }
        public bool CanBuildTroopShips { get; private set; }
        public bool CanBuildSupportShips { get; private set; }
        public bool CanBuildPlatforms { get; private set; }
        public bool CanBuildStations { get; private set; }
        public bool CanBuildShipyards { get; private set; }
        public bool CanBuildResearchStations { get; private set; }
        public bool CanBuildMiningStations { get; private set; }
        public float CurrentMilitaryStrength;
        public float OffensiveStrength; // No Orbitals
        [StarData] public LoyaltyLists EmpireShips;
        public float CurrentTroopStrength { get; private set; }
        public Color ThrustColor0;
        public Color ThrustColor1;
        public float MaxColonyValue { get; private set; }
        public float TotalColonyValues { get; private set; }
        public float TotalColonyPotentialValues { get; private set; }
        public IShipDesign BestPlatformWeCanBuild { get; private set; }
        public IShipDesign BestStationWeCanBuild { get; private set; }
        public IShipDesign BestResearchStationWeCanBuild { get; private set; }
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
        [StarData] public bool AutoPickConstructors;
        [StarData] public bool AutoBuildTerraformers;
        [StarData] public bool AutoPickBestResearchStation;
        [StarData] public bool SymmetricDesignMode = true;
        [StarData] public Array<string> ObsoletePlayerShipModules;

        public int AtWarCount;

        public const string DefaultBoardingShuttleName = "Assault Shuttle";
        public const string DefaultSupplyShuttleName   = "Supply Shuttle";
        public Ship BoardingShuttle => ResourceManager.GetShipTemplate(DefaultBoardingShuttleName, false);
        public Ship SupplyShuttle   => ResourceManager.GetShipTemplate(DefaultSupplyShuttleName);
        public bool IsCybernetic  => data.Traits.Cybernetic != 0;
        public bool NonCybernetic => data.Traits.Cybernetic == 0;
        public bool WeArePirates  => Pirates != null; // Use this to figure out if this empire is pirate faction
        public bool WeAreRemnants => Remnants != null; // Use this to figure out if this empire is pirate faction

        public bool HavePackMentality => data.Traits.Pack > 0;

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
        public float HomeDefenseShipCostMultiplier => DifficultyModifiers.CreditsMultiplier;

        public float MoneySpendOnProductionThisTurn { get; private set; }
        public float MoneySpendOnProductionNow { get; private set; }

        [StarData] public readonly EmpireResearch Research;
        public float TotalPopBillion { get; private set; }
        public float MaxPopBillion { get; private set; }
        public DifficultyModifiers DifficultyModifiers { get; private set; }
        public PersonalityModifiers PersonalityModifiers { get; private set; }

        // per-faction pseudo-random source
        public readonly RandomBase Random = new ThreadSafeRandom();

        /// <summary>
        /// Empire unique ID. If this is 0, then this empire is invalid!
        /// Set in UniverseState_Empires.cs
        /// Also: UniverseState.Empires.IndexOf(this) == (Id - 1)
        /// </summary>
        [StarData] public int Id;

        public string Name => data.Traits.Name;

        // @note This is used as a placeholder empire for entities that have no logical allegiance
        //       withing the known universe. They belong to the mythical `Void` -- pure Chaos of nothingness
        public static Empire Void = new(null)
        {
            Id = -1, data = new() { Traits = new() { Name = "Void" } }
        };

        [StarDataConstructor] Empire() { }

        public Empire(UniverseState us)
        {
            Universe = us;
            Research = new(this);

            EmpireShips = new(this);

            TechnologyDict = new();
            UnlockedHullsDict = new();
            UnlockedTroopDict = new();
            UnlockedBuildingsDict = new();
            UnlockedModulesDict = new();
            UnlockedTroops = new();

            Inhibitors = new();
            OwnedPlanets = new();
            OwnedSolarSystems = new();

            ThreatDetector = new();

            ShipsWeCanBuild = new();
            SpaceStationsWeCanBuild = new();

            DiplomacyContactQueue = new();
            ObsoletePlayerShipModules = new();
        }

        // Create REBELS
        public Empire(UniverseState us, Empire parentEmpire, EmpireData data) : this(us)
        {
            this.data = data;
            IsFaction = true;
            EmpireColor = new Color(128, 128, 128, 255);

            // clone the entire tech tree
            foreach (var tech in parentEmpire.TechnologyDict)
                TechnologyDict[tech.Key] = new TechEntry(clone: tech.Value, newOwner:this);

            Initialize();
            UpdatePopulation();
        }

        [StarDataDeserialized(typeof(TechEntry), typeof(EmpireData), typeof(UniverseParams), typeof(Planet))]
        void OnDeserialized(UniverseState us)
        {
            dd = ResourceManager.GetDiplomacyDialog(data.DiplomacyDialogPath);
            CommonInitialize();
        }

        public float GetProjectorRadius()
        {
            return Universe.DefaultProjectorRadius * data.SensorModifier;
        }

        public void SetAsPirates(EmpireAI ai)
        {
            if (IsDefeated)
                return;

            if (Universe.P.DisablePirates)
            {
                IsDefeated = true;
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

        public bool GetCurrentCapital(out Planet capital)
        {
            capital      = null;
            var capitals = OwnedPlanets.Filter(p => p.HasCapital);
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

        public float KnownEnemyStrengthIn(SolarSystem s, Empire e) => AI.ThreatMatrix.GetHostileStrengthAt(e, s.Position, s.Radius);
        public float KnownEnemyStrengthIn(SolarSystem s) => AI.ThreatMatrix.GetHostileStrengthAt(s.Position, s.Radius);
        public float KnownEnemyStrengthNoResearchStationsIn(Vector2 pos, float radius) 
            => AI.ThreatMatrix.GetHostileStrengthNoResearchStationsAt(pos, radius);

        public float KnownEnemyStrengthNoResearchStationsIn(SolarSystem s) 
            => AI.ThreatMatrix.GetHostileStrengthNoResearchStationsAt(s.Position, s.Radius);
        public float KnownEmpireStrength(Empire e) => AI.ThreatMatrix.KnownEmpireStrength(e);

        public WeaponTagModifier WeaponBonuses(WeaponTag which) => data.WeaponTags[which];

        public int GetTypicalTroopStrength()
        {
            IReadOnlyList<Troop> unlockedTroops = GetUnlockedTroops();
            float str = unlockedTroops.Max(troop => troop.StrengthMax);
            str      *= 1 + data.Traits.GroundCombatModifier;
            return (int)str.LowerBound(1);
        }

        /// <summary>
        /// Returns the Player's Environment Modifier based on a planet's category.
        /// </summary>
        public float PlayerEnvModifier(PlanetCategory category) => RacialEnvModifer(category, Universe.Player);

        /// <summary>
        /// Returns the Player's Preferred Environment Modifier.
        /// </summary>
        public float PlayerPreferredEnvModifier
            => RacialEnvModifer(Universe.Player.data.PreferredEnvPlanet, Universe.Player);


        /// <summary>
        /// Returns the preferred Environment Modifier of a given empire.This is null Safe.
        /// </summary>
        public static float PreferredEnvModifier(Empire empire)
            => empire == null ? 1 : RacialEnvModifer(empire.data.PreferredEnvPlanet, empire);


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
                case PlanetCategory.Terran:   modifer = empire.data.Traits.EnvTerran;   break;
                case PlanetCategory.Oceanic:  modifer = empire.data.Traits.EnvOceanic;  break;
                case PlanetCategory.Steppe:   modifer = empire.data.Traits.EnvSteppe;   break;
                case PlanetCategory.Tundra:   modifer = empire.data.Traits.EnvTundra;   break;
                case PlanetCategory.Swamp:    modifer = empire.data.Traits.EnvSwamp;    break;
                case PlanetCategory.Desert:   modifer = empire.data.Traits.EnvDesert;   break;
                case PlanetCategory.Ice:      modifer = empire.data.Traits.EnvIce;      break;
                case PlanetCategory.Barren:   modifer = empire.data.Traits.EnvBarren;   break;
                case PlanetCategory.Volcanic: modifer = empire.data.Traits.EnvVolcanic; break;
            }

            return modifer;
        }

        public void SetAsDefeated()
        {
            if (IsDefeated)
                return;

            IsDefeated = true;
            ClearInfluenceList();
            foreach (SolarSystem solarSystem in Universe.Systems)
                solarSystem.OwnerList.Remove(this);

            if (IsFaction)
                return;

            foreach (Relationship rel in ActiveRelations)
            {
                BreakAllTreatiesWith(rel.Them, includingPeace: true);
                GetRelations(rel.Them).AtWar = false;
                rel.Them.GetRelations(this).AtWar = false;
            }

            foreach (Ship ship in OwnedShips)
            {
                ship.AI.ClearOrders();
            }
            AI.ClearGoals();
            AI.EndAllTasks();
            ResetFleets();

            Empire rebels = Universe.CreateRebelsFromEmpireData(data, this);
            Universe.Stats.UpdateEmpire(Universe.StarDate, rebels);

            foreach (Ship s in OwnedShips)
            {
                if (s.IsResearchStation)
                    s.QueueTotalRemoval();

                s.LoyaltyChangeFromBoarding(rebels, false);
            }
            EmpireShips.Clear();
            data.AgentList.Clear();
        }

        public void SetAsMerged()
        {
            if (IsDefeated)
                return;

            IsDefeated = true;
            ClearInfluenceList();
            foreach (SolarSystem solarSystem in Universe.Systems)
                solarSystem.OwnerList.Remove(this);

            if (IsFaction)
                return;

            foreach (Relationship rel in ActiveRelations)
            {
                BreakAllTreatiesWith(rel.Them, includingPeace: true);
            }

            AI.ClearGoals();
            AI.EndAllTasks();
            ResetFleets();
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

        public float AverageSystemsSqdistFromCenter => OwnedSolarSystems.Average(s => s.Position.SqDist(WeightedCenter));

        public bool IsModuleUnlocked(string moduleUID) => UnlockedModulesDict.TryGetValue(moduleUID, out bool found) && found;

        public Map<string, TechEntry>.ValueCollection TechEntries => TechnologyDict.Values;
        public TechEntry[] UnlockedTechs => TechEntries.Filter(e => e.Unlocked);

        public TechEntry GetTechEntry(string uid)
        {
            if (TechnologyDict.TryGetValue(uid, out TechEntry techEntry))
                return techEntry;
            Log.Error($"Empire GetTechEntry: Failed To Find Tech: ({uid})");
            return TechEntry.None;
        }

        public bool TryGetTechEntry(string uid, out TechEntry techEntry)
        {
            return TechnologyDict.TryGetValue(uid, out techEntry);
        }

        public bool HasTechEntry(string uid) => TechnologyDict.ContainsKey(uid);

        public Array<TechEntry> TechsAvailableForTrade()
        {
            var tradeTechs = new Array<TechEntry>();
            foreach (TechEntry entry in TechEntries)
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
        public bool HasDiscovered(string techId)  => GetTechEntry(techId).Discovered;
        public float TechCost(string techId)      => GetTechEntry(techId).TechCost;

        /// <summary>
        /// this appears to be broken.
        /// </summary>
        public IReadOnlyList<SolarSystem> GetOwnedSystems() => OwnedSolarSystems;
        public IReadOnlyList<Planet> GetPlanets()           => OwnedPlanets;
        public int NumPlanets                               => OwnedPlanets.Count;
        public int NumSystems                               => OwnedSolarSystems.Count;

        public int GetTotalPlanetsWarValue() => (int)OwnedPlanets.Sum(p => p.ColonyWarValueTo(this));

        public void RemovePlanet(Planet planet, Empire attacker)
        {
            GetRelations(attacker).LostAColony(planet, attacker);
            RemovePlanet(planet);
            //UpdateWarRallyPlanetsLostPlanet(planet, attacker);
        }

        public void RemovePlanet(Planet planet)
        {
            OwnedPlanets.Remove(planet);
            Universe.OnPlanetOwnerRemoved(this, planet);

            if (!planet.System.HasPlanetsOwnedBy(this)) // system no more in owned planets?
                OwnedSolarSystems.Remove(planet.System);

            CalcWeightedCenter(calcNow: true);
            UpdateRallyPoints(); // update rally points every time OwnedPlanets changes
            AI.SpaceRoadsManager.RemoveRoadIfNeeded(planet.System);
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

            if (planet.System == null)
                throw new ArgumentNullException(nameof(planet.System));

            OwnedPlanets.Add(planet);
            Universe.OnPlanetOwnerAdded(this, planet);

            OwnedSolarSystems.AddUniqueRef(planet.System);
            CalcWeightedCenter(calcNow: true);
            UpdateRallyPoints(); // update rally points every time OwnedPlanets changes
        }

        void IEmpireShipLists.AddNewShipAtEndOfTurn(Ship s) => EmpireShips.Add(s);

        void InitDifficultyModifiers()
        {
            DifficultyModifiers = new DifficultyModifiers(this, Universe.P.Difficulty);
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
            CreateEmpireTechTree(); // update or init the tech tree
            KnownEmpires.Set(Id); // we know ourselves
            InitDifficultyModifiers();
            InitPersonalityModifiers();
            CreateThrusterColors();

            if (data.DefaultTroopShip.IsEmpty())
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";

            Research.Initialize();
            InitEmpireUnlocks();
            UpdateRallyPoints();
        }

        // initializes an empire
        public void Initialize()
        {
            CommonInitialize();

            data.TechDelayTime = 0;
            if (Universe.NumEmpires == 0)
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
            IShipDesign[] ourShips = AllFactionShipDesigns;

            foreach (TechEntry entry in TechEntries)
            {
                var tech = entry.Tech;
                bool modulesNotHulls = tech.ModulesUnlocked.Count > 0 && tech.HullsUnlocked.Count == 0;
                if (modulesNotHulls && !WeCanUseThisInDesigns(entry, ourShips))
                    entry.shipDesignsCanuseThis = false;
            }

            foreach (TechEntry entry in TechEntries)
            {
                if (!entry.shipDesignsCanuseThis)
                    entry.shipDesignsCanuseThis = WeCanUseThisLater(entry);
            }

            foreach (TechEntry entry in TechEntries)
            {
                AddToShipTechLists(entry);
            }

            // now unlock the techs again to populate lists
            foreach (TechEntry entry in UnlockedTechs)
            {
                entry.UnlockFromSave(this, unlockBonuses: false);
            }
        }

        void CreateEmpireTechTree()
        {
            foreach (Technology tech in ResourceManager.TechsList)
            {
                // double check ALL technologies, if one is missing because it's a
                // new tech probably added in a patch, it needs to be inserted and rediscovered
                // TODO: should we remove technologies which no longer exist?
                if (!TechnologyDict.ContainsKey(tech.UID))
                {
                    var entry = new TechEntry(tech.UID, Universe, this);
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
        }

        void ResetUnlocks()
        {
            UnlockedBuildingsDict.Clear();
            UnlockedModulesDict.Clear();
            UnlockedHullsDict.Clear();
            UnlockedTroopDict.Clear();
            UnlockedTroops.Clear();
            ClearShipsWeCanBuild();
        }

        void InitEmpireUnlocks()
        {
            foreach (string building in data.unlockBuilding)
                UnlockedBuildingsDict[building] = true;

            foreach (string ship in data.unlockShips)
            {
                IShipDesign design = ResourceManager.Ships.GetDesign(ship);
                AddBuildableShip(design);
            }

            foreach (TechEntry entry in TechEntries) // unlock racial techs
            {
                // save compatibility: first 1.41 savegames didn't have TechEntry.Owner
                entry.Owner ??= this;

                if (entry.Discovered)
                    data.Traits.UnlockAtGameStart(entry, this);
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
                if (SpaceStationsWeCanBuild.Count == 0 && ShipsWeCanBuild.Any(s => s.Role <= RoleName.station))
                    Log.Error($"Empire SpaceStationsWeCanBuild is empty! {this}");
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

        IShipDesign[] FactionDesignsCache;

        public IShipDesign[] AllFactionShipDesigns
        {
            get
            {
                // This is quite slow if we have hundreds/thousands of designs, so we need to cache them
                return FactionDesignsCache ??= ResourceManager.Ships.Designs.Filter(design => ShipStyleMatch(design.ShipStyle));
            }
        }

        public bool ShipStyleMatch(string shipStyle)
        {
            if (shipStyle == data.Traits.ShipType || shipStyle == "Platforms" || shipStyle == "Misc")
                return true;

            foreach (Empire empire in Universe.MajorEmpires)
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
                TechEntry entry = GetTechEntry(leadsToTech.UID);
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
            AI.SpaceRoadsManager.UpdateAllRoadsMaintenance();
            AI.TriggerRefit();
            TriggerFreightersRefit();
        }

        public void AssimilateTech(Empire conqueredEmpire)
        {
            foreach (TechEntry conquered in conqueredEmpire.TechEntries)
            {
                TechEntry ourTech = GetTechEntry(conquered.UID);
                ourTech.UnlockByConquest(this, conqueredEmpire, conquered);
            }
        }

        //Added by McShooterz: this is for techs obtain via espionage or diplomacy
        public void AcquireTech(string techID, Empire target, TechUnlockType techUnlockType)
        {
            UnlockTech(techID, techUnlockType, target);
        }

        /// <summary>Return TRUE if Empire Turn update was triggered</summary>
        public bool Update(UniverseState us, FixedSimTime timeStep)
        {
            #if PLAYERONLY
                if(!this.isPlayer && !this.isFaction)
                    foreach (Ship ship in this.OwnedShips)
                        ship.GetAI().OrderScrapShip();
                if (this.OwnedShips.Count == 0)
                    return false;
            #endif

            bool didUpdate = false;
            UpdateTimer -= timeStep.FixedTime;
            if (UpdateTimer <= 0f && !IsDefeated)
            {
                UpdateTimer = us.P.TurnTimer + (Id - 1) * timeStep.FixedTime;
                FixedSimTime elapsedTurnTime = new(UpdateTimer);

                AssessSystemsInDanger(elapsedTurnTime);

                if (isPlayer)
                {
                    UpdateStats();
                    ThreatDetector.AssessHostilePresenceForPlayerWarnings(this);
                    EmpirePlanetCombat = GetNumPlanetsWithTroopCombat();
                    EmpireShipCombat = GetNumShipsInCombat(EmpireShips.OwnedShips);
                }

                UpdateInhibitors(EmpireShips.OwnedShips);
                UpdateEmpirePlanets(elapsedTurnTime);
                UpdatePopulation();
                UpdateTroopsInSpaceConsumption();
                UpdateRallyPoints(); // rally points must exist before AI Update
                AssignNewHomeWorldIfNeeded();

                ShipsReadyForFleet = new FleetShips(this, AllFleetReadyShips());
                AI.Update(); // Must be done before DoMoney and Take turn
                GovernPlanets(); // this does the governing after getting the budgets from UpdateAI when loading a game
                DoMoney();
                TakeTurn(us);

                didUpdate = true;
            }

            // TODO: should this even be here? Maybe we should try a Component oriented design?
            //       so that components are updated automatically
            UpdateFleets(timeStep);

            return didUpdate;
        }

        // called every time Player empire update is triggered
        void UpdateStats()
        {
            Universe.Screen.UpdateStarDateAndTriggerEvents(Universe.StarDate + 0.1f);
            Universe.Stats.StatUpdateStarDate(Universe.StarDate);
            if (Universe.StarDate.AlmostEqual(1000.09f))
            {
                foreach (Empire empire in Universe.Empires)
                {
                    foreach (Planet planet in empire.OwnedPlanets)
                        Universe.Stats.StatAddPlanetNode(Universe.StarDate, planet);
                }
            }
        }

        void UpdateInhibitors(Ship[] ourShips)
        {
            // NOTE: Regarding Inhibitors, they should only be added here, to avoid bugs
            Inhibitors.Clear();
            foreach (Ship s in ourShips)
                if (s.InhibitionRadius > 0.0f)
                    Inhibitors.Add(s);
        }

        int GetNumShipsInCombat(Ship[] ourShips)
        {
            int inCombat = 0;
            foreach (Ship s in ourShips)
                if (s.Fleet == null && s.InCombat && !s.IsHangarShip) //fbedard: total ships in combat
                    ++inCombat;
            return inCombat;
        }

        int GetNumPlanetsWithTroopCombat() //fbedard: Number of planets where you have combat
        {
            int inCombat = 0;
            foreach (SolarSystem system in Universe.Systems)
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(Universe.Player) && p.RecentCombat)
                    {
                        if (p.Owner == Universe.Player)
                            ++inCombat;
                        else if (p.Troops.WeHaveTroopsHere(Universe.Player))
                            ++inCombat;
                    }
                }
            }
            return inCombat;
        }

        // FB - for unittest only!
        public void TestAssignNewHomeWorldIfNeeded() => AssignNewHomeWorldIfNeeded();

        void AssignNewHomeWorldIfNeeded()
        {
            if (isPlayer || IsFaction)
                return;

            if (!Universe.P.EliminationMode 
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

        /// <summary>
        /// Initializes non-serialized empire values after save load
        /// </summary>
        public void InitEmpireFromSave(UniverseState us) // todo FB - why is this called on new game?
        {
            Universe = us;
            EmpireShips.UpdatePublicLists();
            Research.UpdateNetResearch();

            UpdateEmpirePlanets(FixedSimTime.Zero);

            UpdateNetPlanetIncomes();
            UpdateMilitaryStrengths();
            CalculateScore(fromSave: true);
            UpdateRelationships(takeTurn: false);
            UpdateShipMaintenance();
            UpdateMaxColonyValues();
            CalcWeightedCenter(calcNow: true);
            AI.RunEconomicPlanner(fromSave: true);
        }

        public void UpdateMilitaryStrengths()
        {
            CurrentMilitaryStrength = 0;
            CurrentTroopStrength = 0;
            OffensiveStrength = 0;

            var ships = OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                float str = ship.GetStrength();
                CurrentMilitaryStrength += str;
                CurrentTroopStrength += ship.Carrier.MaxTroopStrengthInShipToCommit;
                if (!ship.IsPlatformOrStation)
                    OffensiveStrength += str;
            }

            for (int x = 0; x < OwnedPlanets.Count; x++)
            {
                var planet = OwnedPlanets[x];
                CurrentTroopStrength += planet.Troops.OwnerTroopStrength;
            }
        }

        public void AssessSystemsInDanger(FixedSimTime timeStep)
        {
            ThreatDetector ??= new(); // savegame compatibility
            ThreatDetector.Update(this, timeStep);
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
                TotalColonyPotentialValues += planet.ColonyPotentialValue(this, useBaseMaxFertility: false);
                if (planet.ColonyValue > MaxColonyValue)
                    MaxColonyValue = planet.ColonyValue;
            }
        }

        private void UpdateBestOrbitals()
        {
            // FB - this is done here for more performance. Cached values here prevents calling shipbuilder by every planet every turn
            BestPlatformWeCanBuild = BestShipWeCanBuild(RoleName.platform, this);
            BestStationWeCanBuild  = BestShipWeCanBuild(RoleName.station, this);
            BestResearchStationWeCanBuild = PickResearchStation(this);
        }

        public void UpdateDefenseShipBuildingOffense()
        {
            for (int i = 0 ; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                p.UpdateDefenseShipBuildingOffense();
            }
        }
        
        public void MarkShipRolesUsableForEmpire(IShipDesign s)
        {
            switch (s.Role)
            {
                case RoleName.bomber:     CanBuildBombers      = true; break;
                case RoleName.carrier:    CanBuildCarriers     = true; break;
                case RoleName.support:    CanBuildSupportShips = true; break;
                case RoleName.troopShip:  CanBuildTroopShips   = true; break;
                case RoleName.corvette:   CanBuildCorvettes    = true; break;
                case RoleName.frigate:    CanBuildFrigates     = true; break;
                case RoleName.cruiser:    CanBuildCruisers     = true; break;
                case RoleName.battleship: CanBuildBattleships  = true; break;
                case RoleName.capital:    CanBuildCapitals     = true; break;
                case RoleName.platform:   CanBuildPlatforms    = true; break;
                case RoleName.station:    CanBuildStations     = true; break;
            }
            if (s.IsShipyard)
                CanBuildShipyards = true;

            if (s.IsResearchStation)
                CanBuildResearchStations= true;

            if (s.IsMiningStation)
                CanBuildMiningStations= true;
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
            // TODO: are troops on unowned planets for free? 
            int troopsOnPlanets = OwnedPlanets.Sum(p => p.Troops.NumTroopsHere(this));
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
                debug.AddLine($"{p.System.Name} : {p.Name}{starving}");
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
                debug.AddLine($"{p.System.Name} : {p.Name} ", Color.Yellow);
                debug.AddLine($"FoodHere: {food} ", Color.White);
                debug.AddLine($"ProdHere: {prod}");
                debug.AddRange(lines);
                debug.AddLine("");
            }
            debug.Header = Name;
            debug.HeaderColor = EmpireColor;
            return debug;
        }

        public void DoMoney()
        {
            MoneyLastTurn = Money;
            ++TurnCount;

            UpdateTradeIncome();
            UpdateNetPlanetIncomes();
            UpdateShipMaintenance();
            UpdateAveragePlanetStorage();
            AddMoney(NetIncome);
        }

        void ResetMoneySpentOnProduction()
        {
            MoneySpendOnProductionThisTurn = 0; // reset for next turn
            MoneySpendOnProductionNow = 0;
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

        public void UpdateEmpirePlanets(FixedSimTime elapsedTurnTime)
        {
            var random = new SeededRandom();

            ResetMoneySpentOnProduction();
            // expensive lock with several loops.
            Planet[] planetsToUpdate = OwnedPlanets.Sorted(p => p.ColonyPotentialValue(this));

            TotalProdExportSlots = OwnedPlanets.Sum(p => p.FreeProdExportSlots); // Done before UpdateOwnedPlanet
            for (int i = 0; i < planetsToUpdate.Length; i++)
            {
                Planet planet = OwnedPlanets[i];
                planet.UpdateOwnedPlanet(elapsedTurnTime, random);
            }
        }

        public void GovernPlanets()
        {
            if (!IsFaction && !IsDefeated)
            {
                UpdateMaxColonyValues();
                UpdateTerraformerBudget();
            }

            foreach (Planet planet in OwnedPlanets)
                planet.DoGoverning();
        }

        void UpdateTerraformerBudget()
        {
            if (isPlayer && !AutoBuildTerraformers)
                return;

            Building terraformer = ResourceManager.GetBuildingTemplate(Building.TerraformerId);
            float maint = terraformer.Maintenance * data.Traits.MaintMultiplier;
            if (CanTerraformVolcanoes && AI.TerraformBudget >= maint)
            {
                float remainingBudget = AI.TerraformBudget;
                // Better potential planets will get the budget first
                // We continue the loop even when the budget is finished so other planets will short circuit reset budgets
                foreach (Planet planet in OwnedPlanets.SortedDescending(p => p.ColonyPotentialValue(this)))
                    planet.UpdateTerraformBudget(ref remainingBudget, maint);
            }
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

            for (int i = 0; i < OwnedProjectors.Count; i++)
            {
                Ship ship = OwnedProjectors[i];
                TotalShipMaintenance += ship.GetMaintCost();
            }
        }

        public float EstimateNetIncomeAtTaxRate(float rate)
        {
            float plusNetIncome = (rate-data.TaxRate) * NetPlanetIncomes;
            return GrossIncome + plusNetIncome - AllSpending - MoneySpendOnProductionNow;
        }

        public float GetActualNetLastTurn() => Money - MoneyLastTurn;

        // @return TRUE if ship did not already exist and was actually added

        // @return TRUE if the ship was actually found and removed

        // this is a fixup method for salvaging corrupted savegames due to a ShipDesignScreen flaw

        // remove duplicates, assume all designs are valid

        // TODO: this should be cached as well, because it is super intensive

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

        // TODO: this is a really bad idea to iterate through all ships
        //       would be better to use Spatial.FindNearby perhaps to find closest ships?
        bool NearestFreeTroopShip(out Ship troopShip, Vector2 objectCenter)
        {
            troopShip = EmpireShips.OwnedShips.FindMinFiltered(
                s => s.IsIdleSingleTroopship,
                s => s.Position.SqDist(objectCenter));
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
            var candidatePlanets = new Array<Planet>(OwnedPlanets
                .Filter(p => p.NumTroopsCanLaunch > 0 && p.Name != planetName)
                .OrderBy(distance => distance.Position.SqDist(objectCenter)));

            var launchFrom = candidatePlanets.FirstOrDefault();
            if (launchFrom == null)
                return false;

            var toLaunch = launchFrom.Troops.GetLaunchableTroops(this, 1);
            troopShip = toLaunch.FirstOrDefault()?.Launch();

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
            return Random.RollDie(100 - rollModifier) <= playerSpyDefense;
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
                if (p.IsHomeworld && Universe.MajorEmpires.Any(e => e.Capital != p))
                {
                    if (p.RemoveCapital() && isPlayer)
                        Universe.Notifications.AddCapitalTransfer(p, newHomeworld);
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
            else // maxPop <= 1bn
            {
                militaryPotential += fertility + p.MineralRichness + maxPopBillion;
                if (maxPopBillion >= 0.5f && Research.HasTopic) // pop within [0.5, 1.0] bn
                {
                    float techCost = Research.Current.TechCost;
                    float resourcePotential = (fertility * 2 + p.MineralRichness + (maxPopBillion / 0.5f));
                    researchPotential = (techCost - Research.NetResearch) / techCost * resourcePotential;
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

        public void TryCreateAssaultBombersGoal(Empire enemy, Planet planet)
        {
            if (enemy == this  || AI.HasGoal(g => g is AssaultBombers && g.PlanetBuildingAt == planet))
                return;

            AI.AddGoalAndEvaluate(new AssaultBombers(planet, this, enemy));
        }

        void TakeTurn(UniverseState us)
        {
            if (UpdateIsEmpireDefeated())
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
                if (Universe.Stats.GetSnapshot(Universe.StarDate, this, out Snapshot snapshot))
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
                Universe.Events.UpdateEvents(Universe);

                if ((Money / AllSpending.LowerBound(1)) < 2)
                    Universe.Notifications.AddMoneyWarning();

                if (!Universe.NoEliminationVictory)
                {
                    bool allEmpiresDead = true;
                    foreach (Empire empire in Universe.Empires)
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
                        Empire remnants = Universe.Remnants;
                        if (remnants.Remnants.Story == Remnants.RemnantStory.None || remnants.IsDefeated || !remnants.Remnants.Activated)
                        {
                            Universe.Screen.OnPlayerWon();
                        }
                        else
                        {
                            remnants.Remnants.TriggerOnlyRemnantsLeftEvent();
                        }
                    }
                }

                for (int i = 0; i < OwnedPlanets.Count; i++)
                {
                    Planet planet = OwnedPlanets[i];
                    if (planet.HasWinBuilding)
                    {
                        Universe.Screen.OnPlayerWon(GameText.AsTheRemnantExterminatorsSweep);
                        return;
                    }
                }
            }

            if (!data.IsRebelFaction)
            {
                if (Universe.Stats.GetSnapshot(Universe.StarDate, this, out Snapshot snapshot))
                    snapshot.Population = OwnedPlanets.Sum(p => p.Population);
            }

            Research.Update();

            if (data.TurnsBelowZero > 0 && Money < 0.0 && (!Universe.Debug || !isPlayer))
                Bankruptcy();

            if ((Universe.StarDate % 1).AlmostZero())
                CalculateScore();

            UpdateRelationships(takeTurn: true);

            if (Money > data.CounterIntelligenceBudget)
            {
                Money -= data.CounterIntelligenceBudget;
                foreach (Relationship rel in ActiveRelations)
                {
                    Relationship relWithUs = rel.Them.GetRelations(this);
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

            Empire empire = Universe.GetEmpireById(DiplomacyContactQueue[0].EmpireId);
            string dialog = DiplomacyContactQueue[0].Dialog;

            if (dialog == "DECLAREWAR")
                empire.AI.DeclareWarOn(this, WarType.ImperialistWar);
            else
                DiplomacyScreen.ContactPlayerFromDiplomacyQueue(empire, dialog);

            DiplomacyContactQueue.RemoveAt(0);
        }


        void CheckFederationVsPlayer(UniverseState us)
        {
            if (us.P.PreventFederations || us.StarDate < 1100f || us.StarDate % 1 > 0)
                return;

            float playerScore    = TotalScore;
            var aiEmpires        = Universe.ActiveNonPlayerMajorEmpires;
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
                    Universe.Notifications.AddPeacefulMergerNotification(biggestAI, strongest);
                else
                    Universe.Notifications.AddSurrendered(biggestAI, strongest);

                biggestAI.AbsorbEmpire(strongest);
                if (biggestAI.GetRelations(this).ActiveWar == null)
                    biggestAI.AI.DeclareWarOn(this, WarType.ImperialistWar);
            }
        }

        void Bankruptcy()
        {
            if (data.TurnsBelowZero >= Random.RollDie(8))
            {
                Log.Info($"Rebellion for: {data.Traits.Name}");

                Empire rebels = Universe.GetEmpireByName(data.RebelName)
                                ?? Universe.FindRebellion(data.RebelName)
                                ?? Universe.CreateRebelsFromEmpireData(data, this);

                if (rebels != null)
                {
                    if (OwnedPlanets.FindMax(out Planet planet, p => WeightedCenter.SqDist(p.Position)))
                    {
                        if (isPlayer)
                            Universe.Notifications.AddRebellionNotification(planet, rebels);

                        for (int index = 0; index < planet.PopulationBillion * 2; ++index)
                        {
                            Troop troop = Universe.CreateRebelTroop(rebels);
                            var chance = (planet.TileArea - planet.GetFreeTiles(this)) / planet.TileArea;
                            if (planet.Troops.Count > 0 && Random.Roll3DiceAvg(chance * 50))
                            {
                                // convert some random troops to rebels
                                var troops = planet.Troops.GetLaunchableTroops(this).ToArr();
                                if (troops.Length != 0)
                                {
                                    Random.Item(troops).ChangeLoyalty(rebels);
                                }
                            }

                            if (planet.NumBuildings > 0 && Random.Roll3DiceAvg(chance * 50))
                            {
                                var building = planet.FindBuilding(b => !b.IsBiospheres);
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

        /// <summary>Returns TRUE if empire is defeated</summary>
        public bool UpdateIsEmpireDefeated()
        {
            if (IsFaction) return false;
            if (IsDefeated) return true;
            if (!Universe.P.EliminationMode && OwnedPlanets.Count != 0)
                return false;
            if (Universe.P.EliminationMode && (Capital == null || Capital.Owner == this) && OwnedPlanets.Count != 0)
                return false;

            SetAsDefeated();
            if (!isPlayer)
            {
                if (Universe.Player.IsKnown(this))
                    Universe.Notifications?.AddEmpireDiedNotification(this);
                return true;
            }

            Universe.Screen.OnPlayerDefeated();
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
                    Universe.Notifications.AddScrapUnlockNotification(message, modelIcon, "ShipDesign");
                }
                else
                {
                    string message = $"{hullString}{Localizer.Token(GameText.HullScrappedAdvancingResearch)}";
                    Universe.Notifications.AddScrapProgressNotification(message, modelIcon, "ResearchScreen", hullTech.UID);
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
            return Random.RollDice(unlockChance);
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
                        empire = ship.Universe.GetEmpireByShipType(hull.ShipType);
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
            Universe.Notifications?.AddBoardNotification(Localizer.Token(GameText.ShipCapturedByYou),
                                                              ship.BaseHull.IconPath, "SnapToShip", ship, null);
        }

        public void AddBoardedNotification(Ship ship, Empire boarder)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(GameText.YourShipWasCaptured)} {boarder.Name}!";
            Universe.Notifications?.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, boarder);
        }

        public void AddMutinyNotification(Ship ship, GameText text, Empire initiator)
        {
            if (!isPlayer)
                return;

            string message = $"{Localizer.Token(text)} {initiator.Name}!";
            Universe.Notifications.AddBoardNotification(message, ship.BaseHull.IconPath, "SnapToShip", ship, initiator);
        }

        void CalculateScore(bool fromSave = false)
        {
            TechScore = TechEntries.Sum(e => e.Unlocked ? e.TechCost : 0) * 0.01f;
            IndustrialScore = 0;
            ExpansionScore  = 0;
            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                ExpansionScore  += p.Fertility*10 + p.MineralRichness*10 + p.PopulationBillion;
                IndustrialScore += p.SumBuildings(b => b.ActualCost);
            }
            IndustrialScore *= 0.05f;

            if (fromSave)
                MilitaryScore = data.MilitaryScoreAverage;
            else
                MilitaryScore = data.NormalizeMilitaryScore(CurrentMilitaryStrength); // Avoid fluctuations

            TotalScore = (int)(MilitaryScore + IndustrialScore + TechScore + ExpansionScore);
        }

        void AbsorbAllEnvPreferences(Empire target)
        {
            data.Traits.EnvTerran  = GetBonus(target.data.Traits.EnvTerran,  data.Traits.EnvTerran);
            data.Traits.EnvOceanic = GetBonus(target.data.Traits.EnvOceanic, data.Traits.EnvOceanic);
            data.Traits.EnvSteppe  = GetBonus(target.data.Traits.EnvSteppe,  data.Traits.EnvSteppe);
            data.Traits.EnvTundra  = GetBonus(target.data.Traits.EnvTundra,  data.Traits.EnvTundra);
            data.Traits.EnvSwamp   = GetBonus(target.data.Traits.EnvSwamp,   data.Traits.EnvSwamp);    
            data.Traits.EnvDesert  = GetBonus(target.data.Traits.EnvDesert,  data.Traits.EnvDesert);
            data.Traits.EnvIce     = GetBonus(target.data.Traits.EnvIce,     data.Traits.EnvIce);
            data.Traits.EnvBarren  = GetBonus(target.data.Traits.EnvBarren,  data.Traits.EnvBarren);

            float GetBonus(float theirBonus, float ourBonus)
            {
                float bonusDiff = theirBonus - ourBonus;
                return bonusDiff > 0 ? ourBonus + bonusDiff*0.5f : ourBonus;
            }
        }

        public void AbsorbEmpire(Empire target)
        {
            AbsorbAllEnvPreferences(target);
            var planets = target.GetPlanets();
            for (int i = planets.Count-1; i >= 0; i--)
            {
                Planet planet = planets[i];
                planet.SetOwner(this);
                if (!planet.System.OwnerList.Contains(this))
                {
                    planet.System.OwnerList.Add(this);
                    planet.System.OwnerList.Remove(target);
                }
            }

            foreach (Planet planet in Universe.Planets)
            {
                foreach (Troop troop in planet.Troops.GetTroopsOf(target))
                    troop.ChangeLoyalty(this);
            }

            target.ClearAllPlanets();
            var ships = target.OwnedShips;
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                Ship ship = ships[i];
                ship.LoyaltyChangeByGift(this, addNotification: false);
                if (ship.IsConstructor)
                    ship.AI.OrderScrapShip();
                if (ship.IsResearchStation)
                    ship.AI.OrderScuttleShip();
            }

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

            target.AI.SpaceRoadsManager.RemoveSpaceRoadsByAbsorb();
            target.SetAsMerged();
            ResetBorders();
            UpdateShipsWeCanBuild();

            if (this != Universe.Player)
            {
                AI.EndAllTasks();
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
            foreach (Empire e in Universe.MajorEmpires)
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
            if (!calcNow && (Universe.StarDate % 1).Greater(0))
                return; // Once per year

            float popRatio = 0;
            var avgPlanetCenter = new Vector2();

            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet planet = OwnedPlanets[i];
                popRatio += planet.PopulationBillion;
                avgPlanetCenter += planet.Position * planet.PopulationBillion;
            }

            WeightedCenter = avgPlanetCenter / popRatio.LowerBound(1);
        }

        public void TheyKilledOurShip(Empire they, Ship killedShip)
        {
            AddMoney(data.Traits.PenaltyPerKilledSlot * killedShip.SurfaceArea * -1);
            if (KillsForRemnantStory(they, killedShip))
                return;

            if (GetRelations(they, out Relationship rel))
                rel.LostAShip(killedShip);
        }

        public void WeKilledTheirShip(Empire they, Ship killedShip)
        {
            AddMoney(data.Traits.CreditsPerKilledSlot * killedShip.SurfaceArea);
            if (!GetRelations(they, out Relationship rel))
                return;
            rel.KilledAShip(killedShip);
        }

        public bool KillsForRemnantStory(Empire they, Ship killedShip)
        {
            if (!WeAreRemnants)
                return false;

            if (Universe.P.DisableRemnantStory)
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

        void AssignExplorationTasks()
        {
            if (isPlayer && !AutoExplore)
                return;

            int unexplored = Universe.Systems.Count(s => !s.IsFullyExploredBy(this)).UpperBound(12);
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
                desiredScouts *= ((int)Universe.P.Difficulty).LowerBound(1);

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

        public bool TryFindClosestScoutTo(Vector2 pos, out Ship scout)
        {
            scout = null;
            var ships = OwnedShips;
            var potentialScouts = OwnedShips.Filter(s => s.IsGoodScout());
            if (potentialScouts.Length > 0)
                scout = potentialScouts.FindMin(s => s.Position.SqDist(pos));

            return scout != null;
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

        // TODO: wtf is this scanOnly, it seems like another Crunchys unmaintainable/unexplained hacks
        public bool IsEmpireAttackable(Empire targetEmpire, GameObject target = null, bool scanOnly = false)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelations(targetEmpire);

            if ((rel.CanAttack && target == null) || (scanOnly && !rel.Known))
                return true;

            return target?.IsAttackable(this, rel) ?? false;
        }

        public bool IsEmpireHostile(Empire targetEmpire)
        {
            if (targetEmpire == this || targetEmpire == null)
                return false;

            Relationship rel = GetRelationsOrNull(targetEmpire);
            return rel?.IsHostile == true;
        }

        public IEnumerable<IncomingThreat> AlliedSystemsWithThreat(bool checkAttackable = false)
        {
            foreach (Empire e in Universe.MajorEmpires.Filter(e => e.IsAlliedWith(this)))
            {
                for (int i = e.SystemsWithThreat.Length - 1; i >= 0; i--)
                {
                    IncomingThreat threat = e.SystemsWithThreat[i];
                    if (!threat.TargetSystem.HasPlanetsOwnedBy(this)
                        && (!checkAttackable || threat.Enemies.Any(e => IsEmpireAttackable(e))))
                    {
                        yield return threat;
                    }
                }
            }
        }

        public bool IsSystemUnderThreatForUs(SolarSystem system)
            => SystemsWithThreat.Any(t => !t.ThreatTimedOut && t.TargetSystem == system);

        public bool IsSystemUnderThreatForAllies(SolarSystem system) 
            => AlliedSystemsWithThreat().Any(t => !t.ThreatTimedOut && t.TargetSystem == system);

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

        public void IncrementCordrazineCapture()
        {
            if (!Universe.P.CordrazinePlanetCaptured)
                Universe.Notifications.AddNotify(ResourceManager.EventsDict["OwlwokFreedom"]);

            Universe.P.CordrazinePlanetCaptured = true;
        }

        public int EstimateCreditCost(float itemCost)   => (int)Math.Round(ProductionCreditCost(itemCost), 0);
        public void ChargeCreditsHomeDefense(Ship ship) => ChargeCredits(ship.GetCost(this) * DifficultyModifiers.CreditsMultiplier, spendNow: true);

        public void ChargeCreditsOnProduction(QueueItem q, float spentProduction)
        {
            if (q.IsMilitary || q.isShip)
                ChargeCredits(spentProduction, spendNow: false);
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

        public void ChargeRushFees(float productionCost, bool immediate)
        {
            ChargeCredits(productionCost, immediate, rush: true);
        }

        void ChargeCredits(float cost, bool spendNow, bool rush = false)
        {
            float creditsToCharge = rush ? cost  * GlobalStats.Defaults.RushCostPercentage : ProductionCreditCost(cost);
            if (spendNow)
            {
                MoneySpendOnProductionNow += creditsToCharge;
                AddMoney(-creditsToCharge);
            }
            else
            {
                MoneySpendOnProductionThisTurn += creditsToCharge;
            }

            //Log.Info($"Charging Credits from {Name}: {creditsToCharge}, Rush: {rush}"); // For testing
        }

        void RefundCredits(float cost, float percentOfAmount)
        {
            float creditsToRefund = cost * DifficultyModifiers.CreditsMultiplier * percentOfAmount;
            MoneySpendOnProductionNow -= creditsToRefund;
            AddMoney(creditsToRefund);
        }

        float ProductionCreditCost(float spentProduction)
        {
            // fixed costs for players, feedback tax loop for the AI
            float taxModifer = isPlayer ? 1 : 1 - data.TaxRate;
            return spentProduction * taxModifer * DifficultyModifiers.CreditsMultiplier;
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

        // For Testing only!
        public void TestSetCanBuildCarriersFalse()
        {
            CanBuildCarriers = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Empire() { Dispose(false); }

        public bool IsDisposed => AI == null;

        void Dispose(bool disposing)
        {
            if (IsDisposed)
                return; // Already disposed

            Mem.Dispose(ref AI);
            OwnedPlanets.Clear();
            OwnedSolarSystems.Clear();
            ActiveRelations = Empty<Relationship>.Array;
            RelationsMap = Empty<Relationship>.Array;
            ThreatDetector.Clear();
            ClearInfluenceList();
            TechnologyDict.Clear();
            ResetFleets(returnShipsToEmpireAI: false);

            ResetUnlocks();

            Inhibitors.Clear();
            ClearShipsWeCanBuild();
            Research.Reset();

            // TODO: These should not be in EmpireData !!!
            data.OwnedArtifacts.Clear();
            data.AgentList.Clear();
            data.MoleList.Clear();

            if (data != null)
            {
                data.AgentList = new();
                data.MoleList = new();
            }

            EmpireShips = null;
        }

        public override string ToString() => $"{(isPlayer?"Player":"AI")}({Id}) '{Name}'";
    }
}
