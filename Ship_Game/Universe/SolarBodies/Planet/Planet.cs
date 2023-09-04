using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Spatial;
using Ship_Game.Gameplay;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.AI.Budget;
using Ship_Game.ExtensionMethods;

namespace Ship_Game
{
    [StarDataType]
    public sealed partial class Planet : SolarSystemBody, IDisposable
    {
        public enum ColonyType
        {
            Core = 0,
            Colony = 1,
            Industrial = 2,
            Research = 3,
            Agricultural = 4,
            Military = 5,
            TradeHub = 6,
        }

        public override string ToString() =>
            $"{Name} ({Owner?.Name ?? "No Owner"}) T:{CType} NET(FD:{Food.NetIncome.String()} PR:{Prod.NetIncome.String()}) {ImportsDescr()}";

        public GeodeticManager GeodeticManager;
        public SpaceStation Station;
        
        [StarData] public TroopManager Troops;
        [StarData] public PlanetBudget Budget;

        [StarData] public bool DontScrapBuildings = false;
        [StarData] public bool Quarantine         = false;
        public bool AllowInfantry;

        [StarData] public int CrippledTurns;
        public int TotalDefensiveStrength { get; private set; }
        public float TotalTroopConsumption { get; private set; }
        [StarData] int NumBuildShipsCanLaunch; // How many builder ships (for orbital) this planet can launch
        [StarData] int NumBuildShipsLaunched;

        public bool HasWinBuilding;

        float ShipCostModifierValue;

        // modifier which reduces ship costs
        public float ShipCostModifier
        {
            get => ShipCostModifierValue;
            private set => ShipCostModifierValue = value.Clamped(0.001f, 1);
        }

        // Timers
        float PlanetUpdatePerTurnTimer;

        [StarData] public int NumShipyards;
        public float Consumption { get; private set; } // Food (NonCybernetic) or Production (IsCybernetic)
        private float Unfed;
        public bool IsStarving => Unfed < 0f;
        public bool QueueEmptySent = true;

        public float TotalRepair; // Total Repair points a planet gives

        [StarData] public float SensorRange { get; private set; }
        public float ProjectorRange { get; private set; }
        public bool SpaceCombatNearPlanet { get; private set; } // FB - warning - this will be false if there is owner for the planet
        public float ColonyValue { get; private set; }
        public float ExcessGoodsIncome { get; private set; } // FB - excess goods tax for empire to collect
        public float SpaceDefMaintenance { get; private set; }
        public float GroundDefMaintenance { get; private set; }
        public float InfraStructure { get; private set; }
        public bool HasDynamicBuildings { get; private set; } // Has buildings which should update per turn even if no owner
        public float TerraformBudget { get; private set; }
        [StarData] public bool HasLimitedResourceBuilding { get; private set; } // if true, these buildings will be updated per turn until depleted
        [StarData] public int BombingIntensity { get; private set; } // The more bombs hitting the surface, the harder is to heal troops or repair buildings

        private const string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions

        float NoSpaceCombatTargetsFoundDelay = 0;

        public bool RecentCombat => Troops.RecentCombat;
        public float ConsumptionPerColonist => 1 + (Owner?.data.Traits.ConsumptionModifier ?? 0);
        public float FoodConsumptionPerColonist => NonCybernetic ? ConsumptionPerColonist : 0;
        public float ProdConsumptionPerColonist => IsCybernetic ? ConsumptionPerColonist : 0;

        public IReadOnlyList<QueueItem> ConstructionQueue => Construction.GetConstructionQueue();

        public bool WeCanLandTroopsViaSpacePort(Empire us) => HasSpacePort && Owner == us && !SpaceCombatNearPlanet;

        public int CountEmpireTroops(Empire us) => Troops.NumTroopsHere(us);
        public int GetDefendingTroopCount()     => Troops.NumDefendingTroopCount;

        public bool Safe => !Quarantine && !MightBeAWarZone(Owner);

        public int NumTroopsCanLaunchFor(Empire empire) => Troops.NumTroopsCanLaunchFor(empire);

        public bool AnyOfOurTroops(Empire us)           => Troops.WeHaveTroopsHere(us);
        public int GetFreeTiles(Empire us)              => Troops.NumFreeTiles(us);
        public int GetEnemyAssets(Empire us)            => Troops.GetEnemyAssets(us);
        public float GetGroundStrength(Empire empire)   => Troops.GroundStrength(empire);
        public bool TroopsHereAreEnemies(Empire empire) => Troops.TroopsHereAreEnemies(empire);
        public bool WeAreInvadingHere(Empire empire)    => Troops.WeAreInvadingHere(empire);
        public bool MightBeAWarZone(Empire empire)      => Troops.MightBeAWarZone(empire);
        public bool ForeignTroopHere(Empire empire)     => Troops.ForeignTroopHere(empire);
        public bool NoGovernorAndNotTradeHub            => !Governor && CType != ColonyType.TradeHub;
        public int SpecialCommodities                   => CountBuildings(b => b.IsCommodity);
        public bool HasCommodities => HasBuilding(b => b.IsCommodity || b.IsVolcano || b.IsCrater);
        public bool Governor => CType != ColonyType.Colony;
        public bool IsCrippled => CrippledTurns > 0 || RecentCombat;
        public bool CanLaunchBuilderShips => !SpaceCombatNearPlanet && NumBuildShipsLaunched < NumBuildShipsCanLaunch;
        public int NumBuildShipsCanLaunchperTurn => NumBuildShipsCanLaunch / 4;

        public float GetGroundStrengthOther(Empire allButThisEmpire)
            => Troops.GroundStrengthOther(allButThisEmpire);
        public IEnumerable<Troop> GetEmpireTroops(Empire empire, int maxToTake = int.MaxValue) 
            => Troops.GetLaunchableTroops(empire, maxToTake);

        public float Fertility                      => FertilityFor(Owner);
        public float MaxFertility                   => MaxFertilityFor(Owner);
        public float FertilityFor(Empire empire)    => BaseFertility * Empire.RacialEnvModifer(Category, empire);
        public float MaxFertilityFor(Empire empire) => (BaseMaxFertility + BuildingsFertility) * Empire.RacialEnvModifer(Category, empire);
        public float MaxBaseFertilityFor(Empire empire) => BaseMaxFertility * Empire.RacialEnvModifer(Category, empire);

        public bool IsCybernetic  => Owner is { IsCybernetic: true };
        public bool NonCybernetic => Owner is { NonCybernetic: true };
        public int TileArea       => TileMaxX * TileMaxY; // FB currently this limited by number of tiles, all planets are 7 x 5

        public float MaxPopulationBillion                   => MaxPopulation / 1000;
        public float MaxPopulationBillionFor(Empire empire) => MaxPopulationFor(empire) / 1000;

        public float MaxPopulation => MaxPopulationFor(Owner);

        public float PotentialMaxPopBillionsFor(Empire empire, bool forceOnlyBiospheres = false)
            => PotentialMaxPopFor(empire, forceOnlyBiospheres) / 1000;

        public float PotentialMaxPopBillionsWithTerraformFor(Empire empire)
            => PotentialMaxPopFor(empire, withTerraformers: true) / 1000; // with Biospheres and Terraformers researched

        float PotentialMaxPopFor(Empire empire, bool forceOnlyBiospheres = false, bool withTerraformers = false)
        {
            bool bioSpheresResearched = withTerraformers || empire.IsBuildingUnlocked(Building.BiospheresId);
            bool terraformResearched  = withTerraformers || empire.CanFullTerraformPlanets;

            // We calculate this and not using MaxPop since it might be an enemy planet with biospheres
            int numNaturalHabitableTiles = TilesList.Count(t => t.Habitable && !t.Biosphere);
            float racialEnvModifier      = Empire.RacialEnvModifer(Category, empire);
            float naturalMaxPop          = BasePopPerTile * numNaturalHabitableTiles * racialEnvModifier;
            if (!forceOnlyBiospheres && !bioSpheresResearched && !terraformResearched)
                return (naturalMaxPop + PopulationBonus).LowerBound(MinimumPop);

            // Only Biosphere researched so we are checking specifically for biospheres alone
            if (bioSpheresResearched  && !terraformResearched || forceOnlyBiospheres)
            {
                int numBiospheresNeeded = TileArea - numNaturalHabitableTiles;
                float bioSphereMaxPop   = PopPerBiosphere(empire) * numBiospheresNeeded;
                return (bioSphereMaxPop + naturalMaxPop + PopulationBonus).LowerBound(MinimumPop);
            }

            if (bioSpheresResearched) // Biospheres and terraformers researched
            {
                int terraformableTiles  = TilesList.Count(t => t.CanTerraform);
                int numBiospheresNeeded = TileArea - numNaturalHabitableTiles - terraformableTiles;
                float preferredEnvMod   = empire.PlayerPreferredEnvModifier;
                float bioSphereMaxPop   = PopPerBiosphere(empire, preferredEnvMod) * numBiospheresNeeded;
                naturalMaxPop           = BasePopPerTile * (numNaturalHabitableTiles + terraformableTiles) * preferredEnvMod;
                return (bioSphereMaxPop + naturalMaxPop + PopulationBonus).LowerBound(MinimumPop);
            }

            // Only Terraformers researched
            int potentialTiles = TilesList.Count(t => t.Terraformable);
            return (BasePopPerTile*potentialTiles*racialEnvModifier + PopulationBonus).LowerBound(MinimumPop);
        }

        public float PopPerBiosphere(Empire empire)
        {
            return BasePopPerBioSphere * Empire.RacialEnvModifer(Category, empire);
        }

        public float PopPerBiosphere(Empire empire, float prefEnvMod)
        {
            return BasePopPerBioSphere * prefEnvMod;
        }

        public float BasePopPerBioSphere => BasePopPerTile / 2;

        // useBaseMaxFertility will give a more stable value
        public float PotentialMaxFertilityFor(Empire empire, bool useBaseMaxFertility)
        {
            float minimumMaxFertilityPotential = empire.CanFullTerraformPlanets ? 1 : 0;
            float potentialFert = useBaseMaxFertility ? MaxBaseFertilityFor(empire) : MaxFertilityFor(empire);
            return potentialFert.LowerBound(minimumMaxFertilityPotential);
        }

        public float MinimumPop => BasePopPerTile / 2; // At least 1/2 tile's worth population and any max pop bonus buildings have
        public float MinimumPopBillion => MinimumPop / 1000;

        public float MaxPopulationFor(Empire empire)
        {
            if (!Habitable)
                return 0;

            float minimumPop = MinimumPop; 
            if (empire == null)
                return (MaxPopValFromTiles + PopulationBonus).LowerBound(minimumPop);

            float maxPopValToUse = MaxPopValFromTiles;
            if (TilesList.Count(t => t.Habitable) == 0)
                maxPopValToUse = minimumPop; // No Habitable tiles, so using the minimum pop

            return (maxPopValToUse * Empire.RacialEnvModifer(Category, empire) + PopulationBonus).LowerBound(minimumPop);
        }

        public int FreeTilesWithRebaseOnTheWay(Empire empire)
        {
            int rebasingTroops = 0;
            if (Owner != null)
            {
                rebasingTroops = Owner.OwnedShips.Filter(s => s.IsDefaultTroopTransport)
                         .Count(s => s.AI.OrderQueue.Any(goal => goal.TargetPlanet == this));
            }
            return (GetFreeTiles(empire) - rebasingTroops).Clamped(0, TileArea);
        }

        void CreateManagers()
        {
            Troops = new TroopManager(this);
            GeodeticManager = new GeodeticManager(this);
            Storage         = new ColonyStorage(this);
            Construction    = new SBProduction(this);

            Food  = new ColonyFood(this)       { Percent = 0.34f };
            Prod  = new ColonyProduction(this) { Percent = 0.33f };
            Res   = new ColonyResearch(this)   { Percent = 0.33f };
            Money = new ColonyMoney(this);
        }

        void InitResources()
        {
            if (Food.Initialized) return;
            Food.Update(0f);
            Prod.Update(0f);
            Res.Update(0f);
        }

        Planet(int id) : base(id, GameObjectType.Planet)
        {
            Active = true; // planets always exist ( for now ;) )
            CreateManagers();
        }

        // For TESTING only
        // You can't call GeneratePlanet / GenerateNewHomeworld after calling this
        // It will set planet scale/radius to default values
        public Planet(int id, SolarSystem system, Vector2 pos, 
                      float fertility, float minerals, float maxPop) : this(id)
        {
            SetSystem(system);
            BaseFertility     = fertility;
            MineralRichness   = minerals;
            BasePopPerTileVal = maxPop;
            Position = pos;
            if (fertility > 0)
                PType = ResourceManager.Planets.RandomPlanet(PlanetCategory.Terran);
            else
                PType = ResourceManager.Planets.PlanetOrRandom(0);
            Scale = 1f;
            Radius = PType.Types.BasePlanetRadius;
        }

        public Planet(int id, RandomBase random, SolarSystem system, float randomAngle, float ringRadius, string name,
                      float sysMaxRingRadius, Empire owner, SolarSystemData.Ring data, float researchableMultiplier = 1) : this(id)
        {
            SetSystem(system);
            OrbitalAngle = randomAngle;
            OrbitalRadius = ringRadius;

            Name = name.IsEmpty() ? GetDefaultPlanetName(data) : name;

            // if we have [data] always use data.HomePlanet,
            // else if no [data] check if owner has Capital or not
            bool isHomeworld = data?.HomePlanet ?? owner is { Capital: null };
            if (owner != null && isHomeworld)
            {
                GenerateNewHomeWorld(random, owner, data);
            }
            else if (data != null)
            {
                GeneratePlanetFromSystemData(random, data);
            }
            else
            {
                SunZone sunZone;
                if      (ringRadius < sysMaxRingRadius * 0.15f) sunZone = SunZone.Near;
                else if (ringRadius < sysMaxRingRadius * 0.25f) sunZone = SunZone.Habital;
                else if (ringRadius < sysMaxRingRadius * 0.7f)  sunZone = SunZone.Far;
                else                                            sunZone = SunZone.VeryFar;

                PlanetType type = ChooseTypeByWeight(sunZone, random);
                float scale = type.Scale + random.Float(0.75f, 1.5f);
                if (type.Category == PlanetCategory.GasGiant)
                    scale += 1f;

                if (!type.Habitable && random.RollDice(type.ResearchableChance * researchableMultiplier))
                {
                    SetResearchable(true, Universe);
                    //Log.Info($"{Name} can be researched");
                }

                InitNewMinorPlanet(random, type, scale);
            }

            PlanetTilt = random.Float(45f, 135f);

            GenerateMoons(system, newOrbital:this, data);
            
            if (data != null)
            {
                SpecialDescription = data.SpecialDescription;

                // Add buildings to planet
                foreach (string building in data.BuildingList)
                    ResourceManager.CreateBuilding(this, building).AssignBuildingToTilePlanetCreation(this, out _);
            }

            HasRings = data != null ? data.HasRings != null : (random.Float(1f, 100f) < 15f);
            if (HasRings)
            {
                RingTilt = random.Float(-80f, -45f).ToRadians();
            }
        }

        public string GetDefaultPlanetName(SolarSystemData.Ring data = null)
        {
            if (data != null)
                return data.Planet;
            int ringNum = 1 + System.RingList.IndexOf(r => r.Planet == this);
            return $"{System.Name} {RomanNumerals.ToRoman(ringNum)}";
        }

        void InitPlanetType(PlanetType type, float scale, bool fromSave)
        {
            if (scale == 0f) throw new InvalidOperationException("Planet initialized with scale=0");
            if (OrbitalRadius == 0f) throw new InvalidOperationException("Planet initialized with OrbitalRadius=0");

            PType = type;
            Scale = scale;
            Radius = type.Types.BasePlanetRadius * type.Types.PlanetScale * scale;
            if (!fromSave)
                OrbitalRadius += Radius;

            UpdatePositionOnly();
        }

        public void CreatePlanetBudget(Empire owner)
        {
            Budget = new(this, owner);
        }

        // This will launch troops without having issues with modifying it's own TroopsHere
        // We are using force launch since these toops are needed out of the planet regardless of their stats
        public void ForceLaunchAllTroops(Empire of, bool orderRebase = false)
        {
            foreach (Troop troop in Troops.GetLaunchableTroops(of, forceLaunch: true))
            {
                Ship troopTransport = troop.Launch(forceLaunch: true);
                if (orderRebase)
                    troopTransport?.AI.OrderRebaseToNearest();
            }
        }

        public void ForceLaunchInvadingTroops(Empire loyaltyToLaunch)
        {
            foreach (Troop t in Troops.GetLaunchableTroops(loyaltyToLaunch, forceLaunch: true))
            {
                Ship troopTransport = t.Launch(forceLaunch: true);
                troopTransport?.AI.OrderRebaseToNearest();
            }
        }

        float GetTotalTroopConsumption()
        {
            int numTroops = Troops.NumTroopsHere(Owner);

            float consumption = numTroops * Troop.Consumption * (1 + Owner.data.Traits.ConsumptionModifier);

            return consumption + GetFoodNeededForTroopsInSpace();

            // Local method
            float GetFoodNeededForTroopsInSpace()
            {
                if (Owner.TroopInSpaceFoodNeeds.AlmostZero() || Owner.TotalFoodPerColonist.AlmostZero())
                    return 0;

                float foodIncome = IsCybernetic ? Prod.NetMaxPotential : Food.NetMaxPotential;
                if (Owner.TroopInSpaceFoodNeeds.Greater(0) && foodIncome.Greater(0))
                {
                    float ratio = foodIncome / Owner.TotalFoodPerColonist;
                    return Owner.TroopInSpaceFoodNeeds * ratio;
                }

                return 0;
            }
        }

        public float GravityWellForEmpire(Empire empire)
        {
            if (Universe.P.GravityWellRange == 0f)
                return 0;

            if (Owner == null)
                return GravityWellRadius;

            if (Owner == empire || Owner.IsAlliedWith(empire))
                return 0;

            return GravityWellRadius;
        }

        public float ColonyDiplomaticValueTo(Empire empire)
        {
            float worth = ColonyBaseValue(empire);
            if (empire.NonCybernetic)
                worth += (FoodHere / 50f) + (ProdHere / 50f);
            else 
                worth += (ProdHere / 25f);

            return worth.LowerBound(5);
        }

        public void SetInGroundCombat(Empire empire, bool notify = false)
        {
            if (!RecentCombat && notify && Owner == Universe.Player && Owner.IsAtWarWith(empire))
                Universe.Notifications.AddEnemyTroopsLandedNotification(this, empire);

            Troops.SetInCombat();
        }

        public float EmpireFertility(Empire empire) =>
            empire.IsCybernetic ? MineralRichness : FertilityFor(empire);

        public float ColonyBaseValue(Empire empire)
        {
            float value = 0;
            value += ColonyRawValue(empire);
            value += HasCapital ? 100 : 0;
            value += SumBuildings(b => b.ActualCost) * 0.01f;
            value += PopulationBillion * 5;

            return value;
        }

        public float ColonyRawValue(Empire empire)
        {
            float value = 0;
            value += SpecialCommodities * 10;
            value += EmpireFertility(empire) * 10;
            value += MineralRichness * (empire.IsCybernetic ? 20 : 10);
            value += MaxPopulationBillionFor(empire) * 5;
            return value;
        }

        public float ColonyPotentialValue(Empire empire, bool useBaseMaxFertility = false)
        {
            if (!Habitable)
                return 0;

            float value = 0;
            if (empire.NonCybernetic)
            {
                float potentialFert = PotentialMaxFertilityFor(empire, useBaseMaxFertility);
                value += potentialFert * potentialFert * 50;
                value += MineralRichness * 5;
            }
            else
            {
                value += MineralRichness * 10;
                value += TilesList.Count(t => t.VolcanoHere) * 3; // Volcanoes can increase production, which is good for cybernetics;
            }

            value += SpecialCommodities * 10;
            value += PotentialMaxPopBillionsFor(empire) * HabitableMultiplier();

            return value;

            float HabitableMultiplier()
            {
                // Avoid crappy barren planets unless they have really large pop potential
                float multiplier = 10;
                if (!empire.CanFullTerraformPlanets)
                {
                    float habitablePercentage = HabitablePercentage;
                    multiplier = habitablePercentage * 10;
                    if (empire.IsBuildingUnlocked(Building.BiospheresId))
                        multiplier += (1 - habitablePercentage) * 5;
                }

                return multiplier.LowerBound(0.02f);
            }
        }
    
        public float ColonyWarValueTo(Empire empire)
        {
            if (Owner == null)             return ColonyPotentialValue(empire);
            if (Owner.IsAtWarWith(empire)) return ColonyBaseValue(empire) + ColonyPotentialValue(empire);

            return 0;
        }

        // added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);

        public void ApplyBombEnvEffects(float popKilled, float fertilityDamage, Empire attacker) // added by Fat Bastard
        {
            if (fertilityDamage.AlmostZero())
                fertilityDamage = popKilled * 0.25f; // Old bomb support

            fertilityDamage *= attacker.data.BombEnvironmentDamageMultiplier;
            float netPopKill = PopulationRatio > 0.25f ? popKilled * PopulationRatio.LowerBound(0.5f) // Harder to kill sparse pop
                                                       : popKilled; // Unless very small pop left

            Population -= 1000f * netPopKill;
            AddBaseFertility(-fertilityDamage); // environment suffers temp damage
            if (BaseFertility.LessOrEqual(0) && Random.RollDice(fertilityDamage * 250))
                AddMaxBaseFertility(-0.01f); // permanent damage to Max Fertility

            if (Population.AlmostZero())
                WipeOutColony(attacker);
        }

        public override void Update(FixedSimTime timeStep)
        {
            UpdateHabitable(timeStep);
            UpdatePosition(timeStep);

            if (HasSpacePort && InFrustum)
            {
                Station ??= new SpaceStation();
                Station.UpdateVisibleStation(this, timeStep);
            }
            else
            {
                Station?.RemoveSceneObject();
            }

            if (!HasSpacePort)
                Station = null;
        }

        void UpdateHabitable(FixedSimTime timeStep)
        {
            // none of the code below requires an owner.
            if (!Habitable)
                return;

            PlanetUpdatePerTurnTimer -= timeStep.FixedTime;
            if (PlanetUpdatePerTurnTimer < 0)
            {
                PlanetUpdatePerTurnTimer = Universe.P.TurnTimer;
                UpdateBaseFertility();
                UpdateDynamicBuildings();
                Mend(((int)InfraStructure + Level).Clamped(1, 10));
            }

            Troops.Update(timeStep);
            GeodeticManager.Update(timeStep);
            // this needs some work
            UpdateSpaceCombatBuildings(timeStep); // building weapon timers are in this method.
        }

        void UpdateDynamicBuildings()
        {
            if (!HasDynamicBuildings)
                return;

            for (int i = 0; i < TilesList.Count; ++i)
            {
                PlanetGridSquare tile = TilesList[i];
                if (tile.VolcanoHere)
                    tile.Volcano.Evaluate();
                else if (tile.LavaHere)
                    Volcano.UpdateLava(tile, this);
                else if (tile.CraterHere)
                    DynamicCrashSite.UpdateCrater(tile, this);
            }
        }

        public void RemoveFromOrbitalStations(Ship orbital)
        {
            OrbitalStations.RemoveSwapLast(orbital);
        }

        public void UpdateSpaceCombatBuildings(FixedSimTime timeStep)
        {
            if (Owner == null)
            {
                SpaceCombatNearPlanet = false;
                return;
            }

            bool enemyInRange = System.DangerousForcesPresent(Owner);
            if (!enemyInRange)
                SpaceCombatNearPlanet = false;

            if (NoSpaceCombatTargetsFoundDelay < 2f || enemyInRange)
            {
                bool targetNear = false;
                NoSpaceCombatTargetsFoundDelay -= timeStep.FixedTime;

                foreach (Building b in Buildings)
                {
                    bool targetFound = b.UpdateSpaceCombatActions(timeStep, this);
                    targetNear |= targetFound;
                }

                SpaceCombatNearPlanet |= targetNear;
                if (!targetNear && NoSpaceCombatTargetsFoundDelay <= 0)
                {
                    SpaceCombatNearPlanet = ThreatsNearPlanet(enemyInRange);
                    NoSpaceCombatTargetsFoundDelay = 2f;
                }
            }
        }

        bool ThreatsNearPlanet(bool enemyInRange)
        {
            if (!enemyInRange)
                return false;

            for (int i = 0; i < System.ShipList.Count; ++i)
            {
                Ship ship = System.ShipList[i];
                if (ship?.Position.InRadius(Position, 15000) == true
                    && ship.BaseStrength > 10
                    && (!ship.IsTethered || ship.GetTether() == this) // orbitals orbiting another nearby planet
                    && Owner.IsEmpireAttackable(ship.Loyalty))
                {
                    return true;
                }
            }

            return false;
        }

        // FB: Beter to scan once and let each building use the result - i think
        public Ship ScanForSpaceCombatTargets(Weapon w,  float weaponRange, bool canLaunchShips)
        {
            // don't do this expensive scan if there are no hostiles
            if (!System.HostileForcesPresent(Owner))
                return null;

            weaponRange = weaponRange.UpperBound(SensorRange);
            float closestTroop = weaponRange* weaponRange;
            float closestShip = weaponRange* weaponRange;
            Ship troop = null;
            Ship closest = null;

            var opt = new SearchOptions(Position, weaponRange, GameObjectType.Ship)
            {
                MaxResults = 32,
                ExcludeLoyalty = Owner,
            };
            SpatialObjectBase[] enemyShips = Universe.Spatial.FindNearby(ref opt);

            for (int i = 0; i < enemyShips.Length; ++i)
            {
                var ship = (Ship)enemyShips[i];
                if (ship.Dying
                    || ship.IsInWarp
                    || ship.EMPDisabled && w?.EMPDamage > 0 && enemyShips.Length > 1
                    || w != null && !w.TargetValid(ship) && !canLaunchShips
                    || !Owner.IsEmpireAttackable(ship.Loyalty))
                {
                    continue;
                }

                float dist = Position.SqDist(ship.Position);
                if (dist < closestTroop && (ship.IsSingleTroopShip || ship.IsDefaultAssaultShuttle || ship.IsBomber))
                {
                    closestTroop = dist;
                    troop = ship;
                }
                else if (dist < closestShip && troop == null)
                {
                    closestShip = dist;
                    closest = ship;
                }
            }

            // always prefer to target troop ships or bombers first (so evil!)
            if (troop != null)
                closest = troop;

            SpaceCombatNearPlanet = closest != null;
            return closest;
        }

        public void LandDefenseShip(Ship ship)
        {
            foreach (Building b in Buildings)
            {
                if (b.TryLandOnBuilding(ship))
                    break; // Ship has landed
            }

            Owner?.RefundCreditsPostRemoval(ship, percentOfAmount: 1f);
        }

        public void LandBuilderShip()
        {
            UpdateBuilderShipLaunched(-1);
        }

        public bool InSafeDistanceFromRadiation()
        {
            return System.InSafeDistanceFromRadiation(Position);
        }

        // this is done once per turn
        public void UpdateOwnedPlanet(FixedSimTime elapsedTurnTime, RandomBase random)
        {
            TurnsSinceTurnover += 1;
            CrippledTurns = (CrippledTurns - 1).LowerBound(0);
            UpdateDevelopmentLevel();
            Description = DevelopmentStatus;

            GeodeticManager.AffectNearbyShips(elapsedTurnTime);
            ApplyTerraforming(random);

            UpdateColonyValue();
            UpdateIncomingTradeGoods();

            InitResources(); // must be done before Governing
            UpdateOrbitalsMaintenance();
            UpdateMilitaryBuildingMaintenance();

            NotifyEmptyQueue();
            RechargePlanetaryShields();
            ApplyResources();
            UpdateLimitedResourceCaches();
            GrowPopulation();
            Troops.HealTroops(2);
            RepairBuildings(1);
            CallForHelp();
            UpdatePlanetShields();
            TotalTroopConsumption = GetTotalTroopConsumption();
            UpdateNumBuilderShipsCanLaunch();
        }

        void UpdateNumBuilderShipsCanLaunch()
        {
            NumBuildShipsCanLaunch = Level + NumShipyards*3 + (HasSpacePort ? 2 : 0);
            if (Universe.StarDate % 2 == 0) // slowly regenerate if some ships did not make it back
                NumBuildShipsLaunched = (NumBuildShipsLaunched - 1).LowerBound(0);
        }

        public void UpdateBuilderShipLaunched(int value)
        {
            NumBuildShipsLaunched = (NumBuildShipsLaunched + value).Clamped(0, NumBuildShipsCanLaunch);
        }

        public void LaunchBuilderShip(Ship targetConstructor, Empire empire)
        {
            string builderShipName = Owner.GetSupplyShuttleName();
            Vector2 launchFrom = GetBuilderShipTargetVector(launch: true);
            Ship builderShip = Ship.CreateShipAtPoint(Universe, builderShipName, Owner, launchFrom);
            if (builderShip != null)
            {
                builderShip.Direction = launchFrom.DirectionToTarget(targetConstructor.Position);
                UpdateBuilderShipLaunched(1);
                builderShip.AI.AddBuildOrbitalGoal(this, targetConstructor);
            }
        }

        public Vector2 GetBuilderShipTargetVector(bool launch)
        {
            Vector2 pos = Position;
            if (Owner.Random.RollDice(75))
            {
                var potentialShipyards = OrbitalStations.Filter(s => s.IsShipyard);
                if (potentialShipyards.Length > 0)
                    pos = Random.Item(potentialShipyards).Position;
            }

            return launch ? pos.GenerateRandomPointInsideCircle(Radius + 50, Owner.Random) : pos;
        }

        void UpdatePlanetShields()
        {
            if (ShieldStrengthCurrent != 0 && Shield == null)
                Shield = new Shield(Position);

            if (ShieldStrengthCurrent == 0 && Shield != null)
                Shield = null;
        }
        public bool CanRepairOrHeal()
        {
            return BombingIntensity == 0 || Random.RollDice(100 - BombingIntensity);
        }

        private void NotifyEmptyQueue()
        {
            if (!GlobalStats.NotifyEmptyPlanetQueue || !OwnerIsPlayer)
                return;

            if (ConstructionQueue.Count == 0 && !QueueEmptySent)
            {
                if (CType != ColonyType.Colony)
                    return;

                QueueEmptySent = true;
                Universe.Notifications.AddEmptyQueueNotification(this);
            }
            else if (ConstructionQueue.Count > 0)
            {
                QueueEmptySent = false;
            }
        }

        private void RechargePlanetaryShields()
        {
            if (ShieldStrengthMax.LessOrEqual(0) || ShieldStrengthCurrent.GreaterOrEqual(ShieldStrengthMax))
                return; // fully recharged

            float maxRechargeRate = ShieldStrengthMax / (SpaceCombatNearPlanet ? 100 : 30);
            float rechargeRate    = (ShieldStrengthCurrent * 100 / ShieldStrengthMax).Clamped(1, maxRechargeRate);
            ShieldStrengthCurrent = (ShieldStrengthCurrent + rechargeRate).Clamped(0, ShieldStrengthMax);
        }

        private void UpdateColonyValue() => ColonyValue = Owner != null ? ColonyBaseValue(Owner) : 0;

        public float PopPerTileFor(Empire empire) => BasePopPerTile * Empire.RacialEnvModifer(Category, empire);

        float MaxPopBillionNoBuildingBonus => MaxPopBillionVal; // Without pop bonus from bulidings but with Biospheres

        // these are intentionally duplicated so we don't easily modify them...
        [StarData] float BasePopPerTileVal;
        float MaxPopValFromTiles, PopulationBonus, MaxPopBillionVal;

        public float BasePopPerTile // population per tile with no racial modifiers
        {
            get => BasePopPerTileVal;
            set
            {
                BasePopPerTileVal = value;
                UpdateMaxPopulation();
            }
        }

        public void UpdateMaxPopulation()
        {
            if (!PType.Habitable)
                return;

            int numBaseHabitableTiles = TilesList.Count(t => t.Habitable && !t.Biosphere);
            PopulationBonus    = SumBuildings(b => !b.IsBiospheres ? b.MaxPopIncrease : 0);
            MaxPopValFromTiles = (BasePopPerTile * numBaseHabitableTiles) 
                               + CountBuildings(b => b.IsBiospheres) * BasePopPerBioSphere;

            MaxPopValFromTiles = MaxPopValFromTiles.LowerBound(BasePopPerTile / 2);
            MaxPopBillionVal   = MaxPopValFromTiles / 1000f;
        }

        public int Level { get; private set; }
        public string DevelopmentStatus { get; private set; } = "Undeveloped";

        public void UpdateDevelopmentLevel() // need to check this with Racial env
        {
            int newLevel = Level;
            if (PopulationBillion <= 0.5f)
            {
                newLevel = (int)DevelopmentLevel.Solitary;
                DevelopmentStatus = Localizer.Token(GameText.ASolitaryOutpostHasTaken);
                if      (MaxPopulationBillion >= 2f  && !IsBarrenType) DevelopmentStatus += Localizer.Token(GameText.WhileAllOfTheEconomic);
                else if (MaxPopulationBillion >= 2f  &&  IsBarrenType) DevelopmentStatus += Localizer.Token(GameText.VastBiodomeComplexesStandReady);
                else if (MaxPopulationBillion < 0.0f && !IsBarrenType) DevelopmentStatus += Localizer.Token(GameText.TheHarshnessOfThisPlanets);
                else if (MaxPopulationBillion < 0.5f &&  IsBarrenType) DevelopmentStatus += Localizer.Token(GameText.TheBarrenLandscapeBeyondThe);
            }
            else if (PopulationBillion > 0.5f && PopulationBillion <= 2)
            {
                newLevel = (int)DevelopmentLevel.Meager;
                DevelopmentStatus = Localizer.Token(GameText.ThisColonyIsWellOn);
                DevelopmentStatus += MaxPopulationBillion >= 2 ? Localizer.Token(GameText.EvenSoThereIsStill) : Localizer.Token(GameText.InFactHabitableLandHere);
            }
            else if (PopulationBillion > 2.0 && PopulationBillion <= 5.0)
            {
                newLevel = (int)DevelopmentLevel.Vibrant;
                DevelopmentStatus = Localizer.Token(GameText.ThisIsAVibrantColony);
                if      (MaxPopulationBillion >= 5.0) DevelopmentStatus += Localizer.Token(GameText.EvenSoThereIsStill2);
                else if (MaxPopulationBillion <  5.0) DevelopmentStatus += Localizer.Token(GameText.InFactHabitableLandHere2);
            }
            else if (PopulationBillion > 5.0 && PopulationBillion <= 10.0)
            {
                newLevel = (int)DevelopmentLevel.CoreWorld;
                DevelopmentStatus = Localizer.Token(GameText.WithAVibrantEconomyAnd);
            }
            else if (PopulationBillion > 10.0)
            {
                newLevel = (int)DevelopmentLevel.MegaWorld;
                DevelopmentStatus = Localizer.Token(GameText.ThisDenselyPopulatedPlanetIs); // densely populated
            }

            if (newLevel != Level) // need to update building offense
            {
                Level = newLevel;
                foreach (Building b in Buildings)
                {
                    if (b.IsWeapon)
                        b.UpdateOffense(this);
                }
            }

            if (Prod.NetIncome >= 10.0 && HasSpacePort)
                DevelopmentStatus += Localizer.Token(GameText.ThisPlanetIsParticularlyNotable); // fine shipwright
            else if (Fertility >= 2.0 && Food.NetIncome > MaxPopulation)
                DevelopmentStatus += Localizer.Token(GameText.ThisPlanetIsWellKnown); // fine agriculture
            else if (Res.NetIncome > 5.0)
                DevelopmentStatus += Localizer.Token(GameText.TheQualityOfTheUniversities); // universities are good

            if (AllowInfantry && Troops.Count > 6)
                DevelopmentStatus += Localizer.Token(GameText.ThisPlanetIsHeavilyFortified); // military culture
        }

        void UpdateOrbitalsMaintenance()
        {
            SpaceDefMaintenance = 0;
            foreach (Ship orbital in OrbitalStations)
            {
                SpaceDefMaintenance += orbital.GetMaintCost(Owner);
            }
        }

        void UpdateMilitaryBuildingMaintenance()
        {
            GroundDefMaintenance = 0;
            foreach (Building b in Buildings)
            {
                if (b.IsMilitary)
                    GroundDefMaintenance += b.ActualMaintenance(this);
            }
        }

        public LocalizedText ColonyTypeInfoText
        {
            get
            {
                switch (CType)
                {
                    default:
                    case ColonyType.Core:         return GameText.GovernorWillBuildABalanced;
                    case ColonyType.Colony:       return GameText.YouAreManagingThisColony;
                    case ColonyType.Industrial:   return GameText.GovernorWillFocusEntirelyOn;
                    case ColonyType.Research:     return GameText.GovernorWillBuildADedicated;
                    case ColonyType.Agricultural: return GameText.GovernorWillBuildAgriculturalBuildings;
                    case ColonyType.Military:     return GameText.GovernorWillBuildALimited;
                    case ColonyType.TradeHub:     return GameText.GovernorWillControlProductionLevels;
                }
            }
        }

        public LocalizedText WorldType
        {
            get
            {
                switch (CType)
                {
                    default:
                    case ColonyType.Core:         return GameText.CoreWorld;
                    case ColonyType.Colony:       return GameText.CustomColony;
                    case ColonyType.Industrial:   return GameText.IndustrialWorld;
                    case ColonyType.Research:     return GameText.ResearchWorld;
                    case ColonyType.Agricultural: return GameText.AgriculturalWorld;
                    case ColonyType.Military:     return GameText.MilitaryOutpost;
                    case ColonyType.TradeHub:     return GameText.Tradehub;
                }
            }
        }

        void UpdateBaseFertility()
        {
            float totalFertility = BaseMaxFertility + BuildingsFertility;
            if (BaseFertility.AlmostEqual(totalFertility))
                return;

            if (BaseFertility < totalFertility)
                BaseFertility = (BaseFertility + 0.01f).Clamped(0, totalFertility); // FB - Slowly increase fertility to max fertility
            else if (BaseFertility > totalFertility)
                BaseFertility = BaseFertility.Clamped(0, BaseFertility - 0.01f); // FB - Slowly decrease fertility to max fertility
        }

        public void SetBaseFertility(float fertility, float maxFertility)
        {
            BaseMaxFertility   = maxFertility;
            BaseFertility      = fertility;
        }

        public void SetBaseFertilityMinMax(float fertility) => SetBaseFertility(fertility, fertility);

        public void AddMaxBaseFertility(float amount)
        {
            BaseMaxFertility = (BaseMaxFertility + amount).LowerBound(0);
        }

        // FB: to enable bombs to temp change fertility immediately by specified amount
        public void AddBaseFertility(float amount)
        {
            BaseFertility = (BaseFertility + amount).LowerBound(0);
        }

        // FB: note that this can be called multiple times in a turn - especially when selecting the planet or in colony screen
        // FB: @todo - this needs refactoring - its too long
        public void UpdateIncomes()
        {
            if (Owner == null)
                return;

            UpdateMaxPopulation();

            // NumShipyards is either calculated before or loaded from a save
            ShipCostModifier = GetShipCostModifier(NumShipyards);
            TotalDefensiveStrength = (int)Troops.GroundStrength(Owner);

            // greedy bastards
            Consumption = (ConsumptionPerColonist * PopulationBillion) + TotalTroopConsumption;
            Food.Update(NonCybernetic ? Consumption : 0f);
            Prod.Update(IsCybernetic  ? Consumption : 0f);
            Res.Update(0f);
            Money.Update();
        }

        public float GetProjectorRadius(Empire owner)
        {
            return owner?.GetProjectorRadius() + 10000f * PopulationBillion ?? 0;
        }

        public float GetProjectorRange()
        {
            if (GlobalStats.Defaults.UsePlanetaryProjection)
                return ProjectorRange;
            return GetProjectorRadius(Owner);
        }

        static float GetShipCostModifier(int numShipyards)
        {
            float shipyardDiminishedReturn = 1;
            float shipBuildingModifier = 1;

            for (int i = 0; i < numShipyards; ++i)
            {
                shipBuildingModifier *= 1 - (GlobalStats.Defaults.ShipyardBonus / shipyardDiminishedReturn);
                shipyardDiminishedReturn += 0.2f;
            }

            return shipBuildingModifier;
        }

        /// <summary>
        /// Ships which need rearm sorted by ordnance pecent (lower first)
        /// </summary>
        /// <returns></returns>
        public bool TryGetShipsNeedRearm(out Ship[] shipsNeedRearm, Empire empire)
        {
            // Not using the planet Owner since it might have been changed by invasion
            shipsNeedRearm = null;
            if (System.DangerousForcesPresent(empire) || System.ShipList.Count == 0)
                return false;

            shipsNeedRearm = System.ShipList.Filter(s => (s.Loyalty == empire || s.Loyalty.IsAlliedWith(empire))
                                                               && s.IsSuitableForPlanetaryRearm()
                                                               && s.Supply.AcceptExternalSupply(SupplyType.Rearm));

            shipsNeedRearm = shipsNeedRearm.SortedDescending(s => s.Supply.MissingOrdnanceWithIncoming);
            return shipsNeedRearm.Length > 0;
        }

        public int NumSupplyShuttlesCanLaunch() // Net, after subtracting already launched shuttles
        {
            var planetSupplyGoals = Owner.AI
                .FindGoals(g => g is RearmShipFromPlanet && g.PlanetBuildingAt == this);

            return (int)InfraStructure - planetSupplyGoals.Length;
        }

        private void UpdateHomeDefenseHangars(Building b)
        {
            if (System.DangerousForcesPresent(Owner) || b.CurrentNumDefenseShips == b.DefenseShipsCapacity)
                return;

            if (System.ShipList.Any(t => t.IsHomeDefense))
                return; // if there are still defense ships our there, don't update building's hangars

            b.UpdateCurrentDefenseShips(1);
        }

        public void UpdateDefenseShipBuildingOffense()
        {
            if (Owner == null)
                return;

            foreach (Building b in Buildings)
            {
                b.UpdateDefenseShipBuildingOffense(Owner, this);
            }
        }

        public void SearchAndRemoveTroopFromAllTiles(Troop t)
        {
            for (int i = 0; i < TilesList.Count; i++)
                if (TilesList[i].TroopsHere.Remove(t))
                    return;
        }

        public void TryCrashOn(Ship ship)
        {
            if (!Habitable || NumActiveCrashSites >= (ship.IsMeteor ? 10 : 5))
                return;

            float survivalChance = TryMeteorHitShield() ? 0 : GetSurvivalChance();
            if (Random.RollDice(survivalChance) 
                && TryGetCrashTile(out PlanetGridSquare crashTile)
                && (ship.IsMeteor || !crashTile.LavaHere))
            {
                int numTroopsSurvived = GetNumTroopSurvived(out string troopName);
                if (ship.IsMeteor)
                    CrashMeteor(ship, crashTile);
                else
                    crashTile.CrashShip(ship.Loyalty, ship.Name, troopName, numTroopsSurvived, this, ship.SurfaceArea);
            }

            // Local Functions
            float GetSurvivalChance()
            {
                float chance = 20 + ship.Level * 2;
                chance      *= 1 / Scale; // Gravity affects how hard is a crash

                if (!ship.CanBeRefitted)
                    chance *= 0.1f; // Dont recover hangar ships or home defense ships so easily.

                if (!PType.Clouds)
                    chance *= 1.5f; // No atmosphere, not able to burn during planetfall

                chance *= 1 + ship.Loyalty.data.Traits.ModHpModifier; // Skilled engineers (or not)
                chance += ship.SurfaceArea / (ship.IsMeteor ? 50f : 100f);
                return chance.Clamped(1, 100);
            }

            bool TryGetCrashTile(out PlanetGridSquare tile)
            {
                float destroyBuildingChance = ship.SurfaceArea / (ship.IsMeteor ? 5f : 50f);
                tile = Random.RollDice(destroyBuildingChance)
                     ? Random.ItemFilter(TilesList, t => t.CanCrashHere)
                     : Random.ItemFilter(TilesList, t => t.NoBuildingOnTile);
                return tile != null;
            }

            int GetNumTroopSurvived(out string name)
            {
                int numTroops = 0;
                var ourTroops = ship.GetOurTroops();
                name          = "";

                for (int i = 0; i < ourTroops.Count; i++)
                {
                    Troop troop = ourTroops[i];
                    float troopSurvival = 50 * Empire.PreferredEnvModifier(troop.Loyalty);
                    if (Random.RollDice(troopSurvival))
                    {
                        numTroops += 1;
                        if (name.IsEmpty())
                            name = troop.Name;
                    }
                }

                return numTroops;
            }

            bool TryMeteorHitShield()
            {
                if (!ship.IsMeteor)
                    return false; // only meteors can hit shields

                float damage = ship.SurfaceArea * 2 * ship.HealthPercent;
                if (damage > ShieldStrengthCurrent)
                    return false; // Shield not strong enough

                if (Universe.IsSystemViewOrCloser
                    && Universe.Screen.IsInFrustum(Position, OrbitalRadius * 2))
                {
                    Shield.HitShield(this, ship, Position, Radius + 100f);
                }

                ShieldStrengthCurrent = (ShieldStrengthCurrent - damage).LowerBound(0);
                return true;
            }
        }

        void CrashMeteor(Ship meteor, PlanetGridSquare tile)
        {
            tile.KillAllTroops(this);
            if (tile.BuildingOnTile)
            {
                if (tile.BuildingOnTile)
                {
                    if (Owner == Universe.Player) // notify before removing the building
                        Universe.Notifications.AddBuildingDestroyedByMeteor(this, tile.Building);
                    DestroyBuildingOn(tile);
                }
            }

            int bid;
            string message;
            bool richness;
            switch (Random.RollDie(20))
            {
                case 1:  bid = Building.Crater1Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn); richness  = false; break;
                case 2:  bid = Building.Crater2Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn2); richness = false; break;
                case 3:  bid = Building.Crater3Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn3); richness = false; break;
                case 4:
                case 5:  bid = Building.Crater4Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn4); richness = true;  break; 
                default: bid = Building.Crater4Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn4); richness = false; break;
            }

            float popKilled = meteor.SurfaceArea * meteor.HealthPercent * (tile.Habitable ? 1 : 0.5f);
            popKilled = popKilled.UpperBound(Population);

            if (tile.Biosphere)
            {
                DestroyBioSpheres(tile, false);
                message = $"{message}\n{Localizer.Token(GameText.BiospheresInTheCrashSite)}";
            }

            if (popKilled.Greater(0))
                message = $"{message}\n{popKilled.String(0)} {Localizer.Token(GameText.MillionCiviliansWereKilled)}";

            if (richness)
            {
                MineralRichness += 0.1f;
                message = $"{message}\n{Localizer.Token(GameText.MineralRichnessWasIncreasedBy)}";
            }

            Building b = ResourceManager.CreateBuilding(this, bid);
            tile.PlaceBuilding(b, this);
            if (OwnerIsPlayer)
                Universe.Notifications.AddMeteorRelated(this, message);

            Population = (Population - popKilled).LowerBound(0);
            if (Owner != null && Population.AlmostZero())
                WipeOutColony(Universe.Unknown);
        }

        // deplete limited resource caches
        void UpdateLimitedResourceCaches()
        {
            if (!HasLimitedResourceBuilding)
                return;

            bool foundCache = false;

            // grab a copy of buildings, because we're about to modify the list
            Building[] buildings = Buildings.ToArray();
            foreach (Building b in buildings)
            {
                if (b.FoodCache > 0f)
                {
                    foundCache = true;
                    b.FoodCache -= b.PlusFlatFoodAmount;
                    b.FoodCache -= Food.Percent * PopulationBillion * b.PlusFoodPerColonist;
                    if (b.FoodCache <= 0f)
                    {
                        if (Owner == Universe.Player)
                            Universe.Notifications.AddBuildingDestroyed(this, b, GameText.WasRemovedSinceItsResource);

                        DestroyBuilding(b);
                        continue; // removed, continue to next building
                    }
                }

                if (b.ProdCache > 0f)
                {
                    foundCache = true;
                    b.ProdCache -= Prod.Percent * PopulationBillion * b.PlusProdPerColonist;
                    b.ProdCache -= b.PlusFlatProductionAmount;
                    b.ProdCache -= b.PlusProdPerRichness * MineralRichness;
                    if (b.ProdCache <= 0f)
                    {
                        if (Owner == Universe.Player)
                            Universe.Notifications.AddBuildingDestroyed(this, b, GameText.WasRemovedSinceItsResource);
                        
                        DestroyBuilding(b);
                        continue; // removed, continue to next building
                    }
                }
            }

            HasLimitedResourceBuilding = foundCache;
        }

        private void ApplyResources()
        {
            float foodRemainder = Storage.AddFoodWithRemainder(Food.NetIncome);
            float prodRemainder = Storage.AddProdWithRemainder(Prod.NetIncome);

            bool wasNotStarving = Unfed == 0;
            // produced food is already consumed by denizens during resource update
            // if remainder is negative even after adding to storage,
            // then we are starving
            Unfed = IsCybernetic ? prodRemainder : foodRemainder;
            if (Unfed > 0f) 
                Unfed = 0f; // we have surplus, nobody is unfed

            if (OwnerIsPlayer && wasNotStarving && IsStarving && Universe.P.EnableStarvationWarning)
                Universe.Notifications.AddStarvation(this);

            // special buildings generate ReactorFuel,Fissionables,etc.
            Storage.DistributeSpecialBuildingResources();

            // Empire Tax on remainders
            ExcessGoodsIncome = 0;
            if (NonCybernetic)
                ExcessGoodsIncome += MathExt.ConsumePercent(ref foodRemainder, Owner.data.TaxRate); // tax excess food

            ExcessGoodsIncome += MathExt.ConsumePercent(ref prodRemainder, Owner.data.TaxRate); // tax excess production

            // production surplus is sent to auto-construction
            float prodSurplus = Math.Max(prodRemainder, 0f);
            Construction.AutoApplyProduction(prodSurplus);
            DecayRichness();
        }

        void DecayRichness()
        {
            if (MineralRichness.LessOrEqual(0.1f)) // minimum decay limit
                return;

            // If the planet outputs 100 production on Brutal, the chance to decay is 2.5%, normal will be 1%
            float decayChance = Prod.GrossIncome / (Owner.DifficultyModifiers.MineralDecayDivider / Universe.P.CustomMineralDecay);

            // Larger planets have less chance for reduction
            decayChance /= Scale.LowerBound(0.1f);

            // Decreasing chance of decay if Richness below 1
            // Increasing Chance of decay if richness is above one (limit to max of *2)
            decayChance *= MineralRichness.UpperBound(2f);

            // Longer pace decreases decay chance
            decayChance *= 1 / Universe.ProductionPace;

            if (Random.RollDice(decayChance))
            {
                bool notifyPlayer = MineralRichness.AlmostEqual(1);
                MineralRichness  -= 0.01f;
                if (notifyPlayer)
                {
                    Universe.Screen.NotificationManager?.AddRandomEventNotification(
                        $"{Name} {Localizer.Token(GameText.MineralRichnessHasGoneDown)}", PType.IconPath, "SnapToPlanet", this);
                }

                Log.Info($"Mineral Decay in {Name}, Owner: {Owner}, Current Richness: {MineralRichness}");
            }
        }

        public void ResetGarrisonSize()
        {
            GarrisonSize = 0; // Default is 0 for players. AI manages it's own troops
        }

        public int NumTroopsCanLaunch
        {
            get
            {
                if (MightBeAWarZone(Owner))
                    return 0;

                int threshold = 0;
                if (OwnerIsPlayer)
                    threshold = AutoBuildTroops ? 0 : GarrisonSize;

                return (Troops.Count - threshold).LowerBound(0);
            }
        }

        public bool OurShipsCanScanSurface(Empire us)
        {
            // this is one of the reasons i want to change the way sensors are done to have a class containing sensor information.
            // so we dont have to do this scan more than once. 
            // todo: Build common sensor container class. 
            // this scan should only need to be done once.
            
            var ships      = us.OwnedShips;
            var projectors = us.OwnedProjectors;

            bool scanned = ships.Any(s => s.Active && s.Position.InRadius(Position, s.SensorRange));
            if (!scanned)
                scanned = projectors.Any(s => s.Active && s.Position.InRadius(Position, s.SensorRange));

            return scanned;
        }

        private void GrowPopulation()
        {
            if (Owner == null || RecentCombat || !CanRepairOrHeal())
                return;

            if (PopulationRatio.Greater(1)) // Over population - the planet cannot support this amount of population
            {
                float popToRemove = ((PopulationRatio - 1) * 1000).Clamped(100, 10000);
                Population        = Math.Max(Population - popToRemove, MaxPopulation);
            }
            else if (IsStarving)
                Population += Unfed * 10f; // Reduces population depending on starvation severity.
            else if (!RecentCombat)
            {
                // population is increased
                float balanceGrowth = (1 - PopulationRatio).Clamped(0.25f, 1f);
                float repRate       = Owner.data.BaseReproductiveRate * (Population/3) * balanceGrowth;
                if (Owner.data.Traits.PopGrowthMax.NotZero())
                    repRate = repRate.UpperBound(Owner.data.Traits.PopGrowthMax * 1000f);

                repRate     = repRate.LowerBound(Owner.data.Traits.PopGrowthMin * 1000f);
                repRate    += PlusFlatPopulationPerTurn;
                repRate    += repRate * Owner.data.Traits.ReproductionMod;
                Population += ShortOnFood() ? repRate * 0.1f : repRate;
                Population  = Population.Clamped(0, MaxPopulation);
            }

            Population = Math.Max(10, Population); // over population will decrease in time, so this is not clamped to max pop
        }

        public void WipeOutColony(Empire attacker)
        {
            SetHomeworld(false); // It is possible to capture a capital (it is exists), but it wont be rebuilt
            Population = 0f;
            if (Owner == null)
                return;

            TerraformPoints = 0;
            Construction.ClearQueue();

            if (IsExploredBy(Universe.Player) && (OwnerIsPlayer || attacker.isPlayer))
                Universe.Notifications.AddPlanetDiedNotification(this);

            SetOwner(null, attacker);
        }

        public void Mend(int value) => AddBombingIntensity(-value);
        public void AddBombingIntensity(int value)
        {
            BombingIntensity = (BombingIntensity + value).Clamped(0,100);
        }

        public bool EventsOnTiles()
        {
            return TilesList.Any(t => t.EventOnTile);
        }

        public int NumActiveCrashSites => TilesList.Count(t => t.IsCrashSiteActive);

        // Bump out an enemy troop to make room available (usually for spawned troops via events)
        public bool BumpOutTroop(Empire empire)
        {
            Troop[] enemyTroops = Troops.GetTroopsNotOf(empire).ToArr();
            if (enemyTroops.Length == 0) // we completely filled the planet by ourselves
                return false;
            Troop lastEnemyTroop = enemyTroops[enemyTroops.Length - 1];
            return lastEnemyTroop.Launch(forceLaunch: true) != null;
        }


        public float TotalGeodeticOffense => BuildingGeodeticOffense + OrbitalStations.Sum(o => o.BaseStrength);
        public int MaxDefenseShips => SumBuildings(b => b.DefenseShipsCapacity);
        public int CurrentDefenseShips => SumBuildings(b => b.CurrentNumDefenseShips) + System.ShipList.Count(s => s?.HomePlanet == this);
        
        // these are updated in UpdatePlanetStatsByRecalculation()
        public int TotalBuildings { get; private set; }
        public int  TerraformersHere { get; private set; }
        public float HabitablePercentage { get; private set; }
        public float HabitableBuiltCoverage { get; private set; }
        public int TotalInvadeInjure { get; private set; }
        public float BuildingGeodeticOffense { get; private set; }

        public int FreeHabitableTiles { get; private set; }
        public int TotalHabitableTiles { get; private set; }
        public int TotalMoneyBuildings { get; private set; }
        public float MoneyBuildingRatio { get; private set; }

        // these are updated in UpdatePlanetStatsFromPlacedBuilding() and UpdatePlanetStatsFromRemovedBuilding()
        public bool TerraformingHere { get; private set; }
        public bool HasCapital { get; private set; }
        public bool HasOutpost { get; private set; }
        public bool HasAnomaly { get; private set; }

        void RepairBuildings(int repairAmount)
        {
            if (RecentCombat)
                return;

            foreach (Building b in Buildings)
            {
                if (CanRepairOrHeal())
                {
                    b.ApplyRepair(repairAmount);
                    UpdateHomeDefenseHangars(b);
                }
            }
        }

        void CallForHelp()
        {
            if (!SpaceCombatNearPlanet)
                return;

            for (int i = 0; i < System.ShipList.Count; i++)
            {
                Ship ship = System.ShipList[i];
                if (ship.Loyalty != Owner || ship.InCombat)
                    continue;

                if (!ship.IsFreighter 
                    && ship.BaseStrength > 0 
                    && (ship.AI.State is AIState.AwaitingOrders or AIState.Orbit or AIState.HoldPosition))
                {
                    // Move Offensively to planet
                    Vector2 finalDir = ship.Position.DirectionToTarget(Position);
                    ship.AI.OrderMoveTo(Position, finalDir, MoveOrder.Aggressive|MoveOrder.AddWayPoint|MoveOrder.NoStop);
                }
            }
        }

        public PlanetGridSquare GetTileByCoordinates(int x, int y)
        {
            if (x < 0 || x >= TileMaxX || y < 0 || y >= TileMaxY) // FB >= because coords start from 0
                return null;

            return TilesList.Find(pgs => pgs.X == x && pgs.Y == y);
        }

        ~Planet() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        void Dispose(bool disposing)
        {
            Construction = null;
            Storage = null;
            Troops = null;
            GeodeticManager = null;
            TilesList = new();
        }

        public DebugTextBlock DebugPlanetInfo()
        {
            var debug = new DebugTextBlock();

            string importFood = FoodImportSlots - FreeFoodImportSlots + "/" + FoodImportSlots;
            string importProd = ProdImportSlots - FreeProdImportSlots + "/" + ProdImportSlots;
            string importColonists = ColonistsImportSlots - FreeColonistImportSlots + "/" + ColonistsImportSlots;
            string exportFood = FoodExportSlots - FreeFoodExportSlots + "/" + FoodExportSlots;
            string exportProd = ProdExportSlots - FreeProdExportSlots + "/" + ProdExportSlots;
            string exportColonists = ColonistsExportSlots - FreeColonistExportSlots + "/" + ColonistsExportSlots;
            int numHabitableTiles  = TilesList.Filter(t => t.Habitable).Length;
            debug.AddLine($"{System.Name} : {Name}", Color.Green);
            debug.AddLine($"Scale: {Scale}");
            debug.AddLine($"Population per Habitable Tile: {BasePopPerTile}");
            debug.AddLine($"Environment Modifier for {Universe.Player.Name}: {Universe.Player.PlayerEnvModifier(Category)}");
            debug.AddLine($"Habitable Tiles: {numHabitableTiles}");
            debug.AddLine("");
            debug.AddLine($"Incoming Freighters: {NumIncomingFreighters}");
            debug.AddLine($"Outgoing Freighters: {NumOutgoingFreighters}");
            debug.AddLine("");
            debug.AddLine($"Food Import Slots: {importFood}");
            debug.AddLine($"Prod Import Slots: {importProd}");
            debug.AddLine($"Colonists Import Slots: {importColonists}");
            debug.AddLine("");
            debug.AddLine($"Food Export Slots: {exportFood}");
            debug.AddLine($"Prod Export Slots: {exportProd}");
            debug.AddLine($"Colonists Export Slots: {exportColonists}");
            debug.AddLine("");
            string eatsWhat = NonCybernetic ? "Food" : "Prod";
            debug.AddLine($"Eats: {Consumption} {eatsWhat}");
            debug.AddLine("");
            return debug;
        }
    }
}

