using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Spatial;

namespace Ship_Game
{
    public partial class Planet : SolarSystemBody, IDisposable
    {
        public static Array<Planet> GetPlanetsFromGuids(Array<Guid> guids)
        {
            var items = new Array<Planet>();
            for (int i = 0; i < guids.Count; i++)
            {
                var guid = guids[i];
                var item = GetPlanetFromGuid(guid);
                if (item != null)
                    items.Add(item);
            }

            return items;
        }

        public static Planet GetPlanetFromGuid(Guid guid) => Empire.Universe.GetPlanet(guid);

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
            $"{Name} ({Owner?.Name ?? "No Owner"}) T:{colonyType} NET(FD:{Food.NetIncome.String()} PR:{Prod.NetIncome.String()}) {ImportsDescr()}";

        public GeodeticManager GeodeticManager;
        public TroopManager TroopManager;
        public SpaceStation Station = new SpaceStation(null);

        public bool DontScrapBuildings = false;
        public bool Quarantine         = false;
        public bool AllowInfantry;

        public int CrippledTurns;
        public int TotalDefensiveStrength { get; private set; }
        public float TotalTroopConsumption { get; private set; }

        public bool HasWinBuilding;
        float ShipBuildingModifierValue;
        public float ShipBuildingModifier
        {
            get => ShipBuildingModifierValue;
            private set => ShipBuildingModifierValue = value.Clamped(0.001f, 1);
        }

        // Timers
        float PlanetUpdatePerTurnTimer;

        public int NumShipyards;
        public float Consumption { get; private set; } // Food (NonCybernetic) or Production (IsCybernetic)
        private float Unfed;
        public bool IsStarving => Unfed < 0f;
        public bool QueueEmptySent = true;
        public float RepairPerTurn;
        public float SensorRange { get; private set; }
        public float ProjectorRange { get; private set; }
        public bool SpaceCombatNearPlanet { get; private set; } // FB - warning - this will be false if there is owner for the planet
        public float ColonyValue { get; private set; }
        public float ExcessGoodsIncome { get; private set; } // FB - excess goods tax for empire to collect
        public float SpaceDefMaintenance { get; private set; }
        public float GroundDefMaintenance { get; private set; }
        public float InfraStructure { get; private set; }
        public bool HasDynamicBuildings { get; private set; } // Has buildings which should update per turn even if no owner
        public bool HasLimitedResourceBuilding { get; private set; } // if true, these buildings will be updated per turn until depleted

        private const string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions

        float NoSpaceCombatTargetsFoundDelay = 0;

        public bool RecentCombat    => TroopManager.RecentCombat;
        public float MaxConsumption => MaxPopulationBillion + Owner.data.Traits.ConsumptionModifier * MaxPopulationBillion;

        public float ConsumptionPerColonist     => 1 + Owner.data.Traits.ConsumptionModifier;
        public float FoodConsumptionPerColonist => NonCybernetic ? ConsumptionPerColonist : 0;
        public float ProdConsumptionPerColonist => IsCybernetic ? ConsumptionPerColonist : 0;

        public IReadOnlyList<QueueItem> ConstructionQueue => Construction.GetConstructionQueue();

        public bool WeCanLandTroopsViaSpacePort(Empire us) => HasSpacePort && Owner == us && !SpaceCombatNearPlanet;

        public int CountEmpireTroops(Empire us) => TroopManager.NumEmpireTroops(us);
        public int GetDefendingTroopCount()     => TroopManager.NumDefendingTroopCount;


        public bool Safe => !MightBeAWarZone(Owner) && !Quarantine;

        public int NumTroopsCanLaunchFor(Empire empire) => TroopManager.NumTroopsCanLaunchFor(empire);

        public float GetDefendingTroopStrength()  => TroopManager.OwnerTroopStrength;

        public int GetEstimatedTroopStrengthToInvade(int bestTroopStrength = 10)
        {
            float strength = TroopManager.GroundStrength(Owner); //.ClampMin(100);
            return strength > 0 ? (int)Math.Ceiling(strength / bestTroopStrength.LowerBound(1)) : 0;

        }
        public bool AnyOfOurTroops(Empire us)           => TroopManager.WeHaveTroopsHere(us);
        public int GetFreeTiles(Empire us)              => TroopManager.NumFreeTiles(us);
        public int GetEnemyAssets(Empire us)            => TroopManager.GetEnemyAssets(this, us);
        public float GetGroundStrength(Empire empire)   => TroopManager.GroundStrength(empire);
        public int GetPotentialGroundTroops()           => TroopManager.GetPotentialGroundTroops();
        public bool TroopsHereAreEnemies(Empire empire) => TroopManager.TroopsHereAreEnemies(empire);
        public bool WeAreInvadingHere(Empire empire)    => TroopManager.WeAreInvadingHere(empire);
        public bool MightBeAWarZone(Empire empire)      => TroopManager.MightBeAWarZone(empire);
        public bool ForeignTroopHere(Empire empire)     => TroopManager.ForeignTroopHere(empire);
        public bool NoGovernorAndNotTradeHub            => !Governor && colonyType != ColonyType.TradeHub;
        public int SpecialCommodities                   => BuildingList.Count(b => b.IsCommodity);
        public bool Governor                            => colonyType != ColonyType.Colony;
        public bool IsCrippled                          => CrippledTurns > 0 || RecentCombat;

        public float GetGroundStrengthOther(Empire allButThisEmpire)
            => TroopManager.GroundStrengthOther(allButThisEmpire);
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) 
            => TroopManager.TakeEmpireTroops(empire, maxToTake);

        public GameplayObject[] FindNearbyFriendlyShips()
            => UniverseScreen.Spatial.FindNearby(GameObjectType.Ship, Center, GravityWellRadius,
                                                      maxResults:128, onlyLoyalty:Owner);

        public float Fertility                      => FertilityFor(Owner);
        public float MaxFertility                   => MaxFertilityFor(Owner);
        public float FertilityFor(Empire empire)    => BaseFertility * Empire.RacialEnvModifer(Category, empire);
        public float MaxFertilityFor(Empire empire) => (BaseMaxFertility + BuildingsFertility) * Empire.RacialEnvModifer(Category, empire);

        public bool IsCybernetic  => Owner != null && Owner.IsCybernetic;
        public bool NonCybernetic => Owner != null && Owner.NonCybernetic;
        public int TileArea       => TileMaxX * TileMaxY; // FB currently this limited by number of tiles, all planets are 7 x 5

        public float MaxPopulationBillion                   => MaxPopulation / 1000;
        public float MaxPopulationBillionFor(Empire empire) => MaxPopulationFor(empire) / 1000;

        public float MaxPopulation => MaxPopulationFor(Owner);

        public float PotentialMaxPopBillionsFor(Empire empire, bool forceOnlyBiospheres = false)
            => PotentialMaxPopFor(empire, forceOnlyBiospheres) / 1000;

        float PotentialMaxPopFor(Empire empire, bool forceOnlyBiospheres = false)
        {
            bool bioSpheresResearched = empire.IsBuildingUnlocked(Building.BiospheresId);
            bool terraformResearched  = empire.CanFullTerraformPlanets;

            // We calculate this and not using MaxPop since it might be an enemy planet with biospheres
            int numNaturalHabitableTiles = TilesList.Count(t => t.Habitable && !t.Biosphere);
            float racialEnvModifier      = Empire.RacialEnvModifer(Category, empire);
            float naturalMaxPop          = BasePopPerTile * numNaturalHabitableTiles * racialEnvModifier;
            if (!forceOnlyBiospheres && !bioSpheresResearched && !terraformResearched)
                return naturalMaxPop + PopulationBonus;

            // Only Biosphere researched so we are checking specifically for biospheres alone
            if (bioSpheresResearched  && !terraformResearched || forceOnlyBiospheres)
            {
                int numBiospheresNeeded = TileArea - numNaturalHabitableTiles;
                float bioSphereMaxPop   = PopPerBiosphere(empire) * numBiospheresNeeded;
                return bioSphereMaxPop + naturalMaxPop + PopulationBonus;
            }

            if (bioSpheresResearched) // Biospheres and terraformers researched
            {
                int terraformableTiles  = TilesList.Count(t => t.CanTerraform);
                int numBiospheresNeeded = TileArea - numNaturalHabitableTiles - terraformableTiles;
                float bioSphereMaxPop   = PopPerBiosphere(empire) * numBiospheresNeeded;
                float preferredEnvMod   = empire.PlayerPreferredEnvModifier;
                naturalMaxPop           = BasePopPerTile * (numNaturalHabitableTiles + terraformableTiles) * preferredEnvMod;
                return bioSphereMaxPop + naturalMaxPop + PopulationBonus;
            }

            // Only Terraformers researched
            int potentialTiles = TilesList.Count(t => t.Terraformable);
            return BasePopPerTile*potentialTiles*racialEnvModifier + PopulationBonus;
        }

        public float PopPerBiosphere(Empire empire)
        {
            return BasePopPerBioSphere * Empire.RacialEnvModifer(Category, empire);
        }

        public float BasePopPerBioSphere => BasePopPerTile / 2;

        public float PotentialMaxFertilityFor(Empire empire)
        {
            float minimumMaxFertilityPotential = empire.CanFullTerraformPlanets ? 1 : 0;
            return MaxFertilityFor(empire).LowerBound(minimumMaxFertilityPotential);
        }

        public float MaxPopulationFor(Empire empire)
        {
            if (!Habitable)
                return 0;

            float minimumPop = BasePopPerTile/2; // At least 1/2 tile's worth population and any max pop bonus buildings have
            if (empire == null)
                return (MaxPopValFromTiles + PopulationBonus).LowerBound(minimumPop);

            float maxPopValToUse = MaxPopValFromTiles;
            if (TilesList.Count(t => t.Habitable) == 0)
                maxPopValToUse = minimumPop; // No Habitable tiles, so using the minimum pop

            return (maxPopValToUse * Empire.RacialEnvModifer(Category, empire) + PopulationBonus).LowerBound(minimumPop);
        }

        public int FreeTilesWithRebaseOnTheWay(Empire empire)
        {
            var ships = Owner.OwnedShips;
                 int rebasingTroops = ships.Filter(s => s?.IsDefaultTroopTransport == true)
                                          .Count(s => s?.AI.OrderQueue.Any(goal => goal.TargetPlanet == this) == true);
                return (GetFreeTiles(empire) - rebasingTroops).Clamped(0, TileArea);
        }

        void CreateManagers()
        {
            TroopManager    = new TroopManager(this);
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

        public Planet()
        {
            CreateManagers();
            HasSpacePort = false;
        }

        public Planet(float fertility, float minerals, float maxPop)
        {
            CreateManagers();
            HasSpacePort      = false;
            BaseFertility     = fertility;
            MineralRichness   = minerals;
            BasePopPerTileVal = maxPop;
            if (fertility > 0)
                Type          = ResourceManager.RandomPlanet(PlanetCategory.Terran);
        }

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null, float preDefinedPop = 0)
        {
            CreateManagers();

            Name = name;
            OrbitalAngle = randomAngle;
            ParentSystem = system;

            SunZone sunZone;
            float zoneSize = ringMax;
            if      (ringRadius < zoneSize * 0.15f) sunZone = SunZone.Near;
            else if (ringRadius < zoneSize * 0.25f) sunZone = SunZone.Habital;
            else if (ringRadius < zoneSize * 0.7f)  sunZone = SunZone.Far;
            else                                    sunZone = SunZone.VeryFar;


            if (owner != null && owner.Capital == null && sunZone >= SunZone.Habital)
            {
                GenerateNewHomeWorld(owner, preDefinedPop);
                Name = system.Name + " " + RomanNumerals.ToRoman(1);
            }
            else
            {
                PlanetType chosenType = ChooseTypeByWeight(sunZone);
                float scale     = RandomMath.RandomBetween(0.75f, 1.5f);
                if (chosenType.Category == PlanetCategory.GasGiant)
                    ++scale;

                scale += chosenType.Scale;
                InitNewMinorPlanet(chosenType, scale);
            }

            float planetRadius = 1000f * (float)(1 + (Math.Log(Scale) / 1.5));
            ObjectRadius = planetRadius;
            OrbitalRadius = ringRadius + planetRadius;
            Center = system.Position + MathExt.PointOnCircle(randomAngle, ringRadius);
            PlanetTilt = RandomMath.RandomBetween(45f, 135f);

            GenerateMoons(this);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                HasRings = true;
                RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
        }

        // This will launch troops without having issues with modifying it's own TroopsHere
        public void LaunchTroops(Troop[] troopsToLaunch)
        {
            foreach (Troop troop in troopsToLaunch)
                troop.Launch();
        }

        public void ForceLaunchInvadingTroops(Empire loyaltyToLaunch)
        {
            for (int i = TroopsHere.Count - 1; i >= 0; i--)
            {
                Troop t      = TroopsHere[i];
                Empire owner = t?.Loyalty;

                if (owner == loyaltyToLaunch && owner?.data.DefaultTroopShip != null)
                {
                    Ship troopship = t.Launch(ignoreMovement: true);
                    troopship?.AI.OrderRebaseToNearest();
                }
            }
        }

        float GetTotalTroopConsumption()
        {
            int numTroops;
            using (TroopsHere.AcquireReadLock())
                numTroops = TroopsHere.Count(t => t.Loyalty == Owner);

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
            if (!Empire.Universe.GravityWells)
                return 0;

            if (Owner == null)
                return GravityWellRadius;

            if (Owner == empire || Owner.IsAlliedWith(empire))
                return 0;

            return GravityWellRadius;
        }

        // this is calculating colonyRawValue twice.
        public float ColonyDiplomaticValueTo(Empire empire)
        {
            float worth = ColonyBaseValue(empire) + ColonyRawValue(empire);
            if (empire.NonCybernetic)
                worth += (FoodHere / 50f) + (ProdHere / 50f);
            else 
                worth += (ProdHere / 25f);

            return worth.LowerBound(15);
        }

        public void SetInGroundCombat(Empire empire, bool notify = false)
        {
            if (!RecentCombat && notify && Owner == EmpireManager.Player && Owner.IsAtWarWith(empire))
                Empire.Universe.NotificationManager.AddEnemyTroopsLandedNotification(this, empire);

            TroopManager.SetInCombat();
        }

        public float EmpireFertility(Empire empire) =>
            empire.IsCybernetic ? MineralRichness : FertilityFor(empire);

        public float ColonyBaseValue(Empire empire)
        {
            float value = 0;
            value += ColonyRawValue(empire);
            value += BuildingList.Any(b => b.IsCapital) ? 100 : 0;
            value += BuildingList.Sum(b => b.ActualCost) / 100;
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

        public float ColonyPotentialValue(Empire empire)
        {
            if (!Habitable)
                return 0;

            float value = 0;
            if (empire.NonCybernetic)
                value += PotentialMaxFertilityFor(empire) * 10;
            else
                value += TilesList.Count(t => t.VolcanoHere) * 5; // Volcanoes can increase production, which is good for cybernetics

            value += SpecialCommodities * 20;
            value += MineralRichness * 10;
            value += PotentialMaxPopBillionsFor(empire) * PopMultiplier();

            return value;

            float PopMultiplier()
            {
                float multiplier = 5;
                if (empire.NonCybernetic 
                    && HabitablePercentage < 0.25f
                    && empire.IsBuildingUnlocked(Building.BiospheresId)
                    && !empire.CanFullTerraformPlanets)
                {
                    multiplier = 2.5f; // Avoid crappy barren planets unless they have really large pop potential
                }

                return multiplier;
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
                fertilityDamage = popKilled / 4; // Old bomb support

            fertilityDamage *= attacker.data.BombEnvironmentDamageMultiplier;
            float netPopKill = PopulationRatio > 0.1f ? popKilled * PopulationRatio.LowerBound(0.2f)  // Harder to kill sparse pop
                                                      : popKilled; // Unless very small pop left

            Population -= 1000f * netPopKill;
            AddBaseFertility(-fertilityDamage); // environment suffers temp damage
            if (BaseFertility.LessOrEqual(0) && RandomMath.RollDice(fertilityDamage * 250))
                AddMaxBaseFertility(-0.01f); // permanent damage to Max Fertility

            if (Population.AlmostZero())
                WipeOutColony(attacker);
        }

        public void Update(FixedSimTime timeStep)
        {
            UpdateHabitable(timeStep);
            UpdatePosition(timeStep);

        }

        void UpdateHabitable(FixedSimTime timeStep)
        {
            // none of the code below requires an owner.
            if (!Habitable)
                return;

            PlanetUpdatePerTurnTimer -= timeStep.FixedTime;
            if (PlanetUpdatePerTurnTimer < 0)
            {
                UpdateBaseFertility();
                UpdateDynamicBuildings();
                PlanetUpdatePerTurnTimer = GlobalStats.TurnTimer;
            }

            TroopManager.Update(timeStep);
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

            bool enemyInRange = ParentSystem.DangerousForcesPresent(Owner);
            if (NoSpaceCombatTargetsFoundDelay < 2f || enemyInRange)
            {
                bool targetNear = false;
                NoSpaceCombatTargetsFoundDelay -= timeStep.FixedTime;

                for (int i = 0; i < BuildingList.Count; ++i)
                {
                    Building building = BuildingList[i];
                    if (building != null)
                    {
                        bool targetFound = building.UpdateSpaceCombatActions(timeStep, this);
                        targetNear |= targetFound;
                    }
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

            for (int i = 0; i < ParentSystem.ShipList.Count; ++i)
            {
                Ship ship = ParentSystem.ShipList[i];
                if (ship?.Center.InRadius(Center, 15000) == true
                    && ship.BaseStrength > 10
                    && (!ship.IsTethered || ship.GetTether() == this) // orbitals orbiting another nearby planet
                    && Owner.IsEmpireAttackable(ship.loyalty))
                {
                    return true;
                }
            }

            return false;
        }

        public Ship ScanForSpaceCombatTargets(float weaponRange) // @todo FB - need to work on this
        {
            // don't do this expensive scan if there are no hostiles
            if (!ParentSystem.HostileForcesPresent(Owner))
                return null;

            weaponRange = weaponRange.UpperBound(SensorRange);
            float closestTroop = weaponRange*weaponRange;
            float closestShip = weaponRange*weaponRange;
            Ship troop = null;
            Ship closest = null;

            var opt = new SearchOptions(Center, weaponRange, GameObjectType.Ship)
            {
                MaxResults = 32,
                ExcludeLoyalty = Owner,
            };
            GameplayObject[] enemyShips = UniverseScreen.Spatial.FindNearby(ref opt);

            for (int j = 0; j < enemyShips.Length; ++j)
            {
                var ship = (Ship)enemyShips[j];
                if (ship.dying || ship.IsInWarp || !Owner.IsEmpireAttackable(ship.loyalty))
                    continue;

                float dist = Center.SqDist(ship.Center);
                if (dist < closestTroop && (ship.IsTroopShip || ship.IsBomber))
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
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building building = BuildingList[i];
                if (building.TryLandOnBuilding(ship))
                    break; // Ship has landed
            }

            Owner?.RefundCreditsPostRemoval(ship, percentOfAmount: 1f);
        }

        /// <summary>
        ///  This will not Destroy Volcanoes. Use static Volcano.RemoveVolcano if you want to remove a Volcano
        /// </summary>
        public void DestroyTile(PlanetGridSquare tile) => DestroyBioSpheres(tile); // since it does the same as DestroyBioSpheres

        public void DestroyBioSpheres(PlanetGridSquare tile, bool destroyBuilding = true)
        {
            if (!tile.VolcanoHere && destroyBuilding)
                DestroyBuildingOn(tile);

            tile.Habitable = false;
            if (tile.QItem != null && tile.QItem.isBuilding && !tile.QItem.Building.CanBuildAnywhere)
                Construction.Cancel(tile.QItem);

            if (tile.Biosphere)
                ClearBioSpheresFromList(tile);
            else
                tile.Terraformable = RandomMath.RollDice(50);

            UpdateMaxPopulation();
            ResetHasDynamicBuildings();
        }

        public void ScrapBuilding(Building b, PlanetGridSquare tile = null)
        {
            if (tile != null)
                RemoveBuildingFromPlanet(tile);
            else
                RemoveBuildingFromPlanet(b);

            ProdHere += b.ActualCost / 2f;
            Owner.GetEmpireAI().MaintSavedByBuildingScrappedThisTurn += b.Maintenance;
        }

        public void DestroyBuildingOn(PlanetGridSquare tile)
        {
            RemoveBuildingFromPlanet(tile, true);
        }

        private void RemoveBuildingFromPlanet(Building b, bool destroy = false)
        {
            BuildingList.Remove(b);
            PlanetGridSquare tile = TilesList.Find(t => t.Building == b);
            if (tile != null)
                tile.Building = null;
            else
                Log.Error($"{this} failed to find tile with building {b}");

            PostBuildingRemoval(b, tile, destroy);
        }

        private void RemoveBuildingFromPlanet(PlanetGridSquare tile, bool destroy = false)
        {
            if (tile?.Building == null)
                return;

            Building b = tile.Building;
            BuildingList.Remove(b);
            tile.Building = null;
            PostBuildingRemoval(b, tile, destroy);
        }

        private void PostBuildingRemoval(Building b, PlanetGridSquare tile, bool destroy = false)
        {
            if (tile != null)
                tile.CrashSite = new DynamicCrashSite(false);

            // FB - we are reversing MaxFertilityOnBuild when scrapping even bad
            // environment buildings can be scrapped and the planet will slowly recover
            AddBuildingsFertility(-b.MaxFertilityOnBuild);
            MineralRichness = (MineralRichness - b.IncreaseRichness).LowerBound(0);

            if (b.IsTerraformer && !TerraformingHere)
                UpdateTerraformPoints(0); // FB - no terraformers present, terraform effort halted

            if (!destroy)
                Owner?.RefundCreditsPostRemoval(b);

            ResetHasDynamicBuildings();
        }

        public void ClearBioSpheresFromList(PlanetGridSquare tile)
        {
            tile.Biosphere = false;

            var biospheresList = BuildingList.Filter(b => b.IsBiospheres);
            if (biospheresList.Length > 0)
                BuildingList.Remove(biospheresList.First());
        }

        public bool InSafeDistanceFromRadiation()
        {
            return ParentSystem.InSafeDistanceFromRadiation(Center);
        }

        public void UpdateOwnedPlanet()
        {
            TurnsSinceTurnover += 1;
            CrippledTurns = (CrippledTurns - 1).LowerBound(0);
            UpdateDevelopmentLevel();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            CalcAverageImportTurns();
            ApplyTerraforming();
            UpdateColonyValue();
            CalcIncomingGoods();
            InitResources(); // must be done before Governing
            UpdateOrbitalsMaintenance();
            UpdateMilitaryBuildingMaintenance();
            NotifyEmptyQueue();
            RechargePlanetaryShields();
            ApplyResources();
            UpdateLimitedResourceCaches();
            GrowPopulation();
            TroopManager.HealTroops(2);
            RepairBuildings(1);
            CallForHelp();
            TotalTroopConsumption = GetTotalTroopConsumption();
        }

        void CalcAverageImportTurns()
        {
            if (Owner.isFaction)
                return;

            if (Owner.GetPlanets().Count <= 1)
            {
                AverageImportTurns = 0;
                return;
            }

            // Calc per 2 Years (20 turns)
            if (AverageImportTurns.Greater(0) && (Empire.Universe.StarDate % 2).Greater(0))
                return;

            AverageImportTurns = Center.Distance(Owner.WeightedCenter) * 2 / (Owner.AverageFreighterFTLSpeed * GlobalStats.TurnTimer);
            AverageImportTurns = AverageImportTurns.LowerBound(1);
        }

        private void NotifyEmptyQueue()
        {
            if (!GlobalStats.ExtraNotifications || Owner == null || !Owner.isPlayer)
                return;

            if (ConstructionQueue.Count == 0 && !QueueEmptySent)
            {
                if (colonyType != ColonyType.Colony)
                    return;

                QueueEmptySent = true;
                Empire.Universe.NotificationManager.AddEmptyQueueNotification(this);
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

        // these are intentionally duplicated so we don't easily modify them...
        private float BasePopPerTileVal, MaxPopValFromTiles, PopulationBonus, MaxPopBillionVal;
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
            if (!Type.Habitable)
                return;

            int numHabitableTiles = TilesList.Count(t => t.Habitable && !t.Biosphere);
            PopulationBonus       = BuildingList.Filter(b => !b.IsBiospheres).Sum(b => b.MaxPopIncrease);
            MaxPopValFromTiles    = (BasePopPerTile * numHabitableTiles) 
                                    + BuildingList.Count(b => b.IsBiospheres) * BasePopPerBioSphere;

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
                for (int i =0; i < BuildingList.Count; i++)
                {
                    Building b = BuildingList[i];
                    if (b.isWeapon)
                        b.UpdateOffense(this);
                }
            }

            if (Prod.NetIncome >= 10.0 && HasSpacePort)
                DevelopmentStatus += Localizer.Token(GameText.ThisPlanetIsParticularlyNotable); // fine shipwright
            else if (Fertility >= 2.0 && Food.NetIncome > MaxPopulation)
                DevelopmentStatus += Localizer.Token(GameText.ThisPlanetIsWellKnown); // fine agriculture
            else if (Res.NetIncome > 5.0)
                DevelopmentStatus += Localizer.Token(GameText.TheQualityOfTheUniversities); // universities are good

            if (AllowInfantry && TroopsHere.Count > 6)
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
            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building b = BuildingList[i];
                if (b.IsMilitary)
                    GroundDefMaintenance += b.ActualMaintenance(this);
            }
        }

        public LocalizedText ColonyTypeInfoText
        {
            get
            {
                switch (colonyType)
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
                switch (colonyType)
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

        public void AddBuildingsFertility(float amount)
        {
            BuildingsFertility += amount;
        }

        // FB: to enable bombs to temp change fertility immediately by specified amount
        public void AddBaseFertility(float amount)
        {
            BaseFertility = (BaseFertility + amount).LowerBound(0);
        }

        // FB: note that this can be called multiple times in a turn - especially when selecting the planet or in colony screen
        // FB: @todo - this needs refactoring - its too long
        public void UpdateIncomes(bool loadUniverse)
        {
            if (Owner == null)
                return;

            bool spacePort            = false;
            AllowInfantry             = false;
            InfraStructure            = 1;
            RepairPerTurn             = 0;
            TerraformToAdd            = 0;
            ShieldStrengthMax         = 0;
            ShipBuildingModifier      = 0;
            SensorRange               = ObjectRadius + 2000;
            TotalDefensiveStrength    = 0;
            PlusFlatPopulationPerTurn = 0;
            float totalStorage        = 0;
            float projectorRange      = 0;
            float sensorRange         = 0;

            NumShipyards         = OrbitalStations.Count(s => s.Active && s.shipData.IsShipyard);
            ShipBuildingModifier = CalcShipBuildingModifier(NumShipyards); // NumShipyards is either counted above or loaded from a save
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building b                 = BuildingList[i];
                PlusFlatPopulationPerTurn += b.PlusFlatPopulation;
                ShieldStrengthMax         += b.PlanetaryShieldStrengthAdded;
                TerraformToAdd            += b.PlusTerraformPoints;
                totalStorage              += b.StorageAdded;
                RepairPerTurn             += b.ShipRepair;
                InfraStructure            += b.Infrastructure;

                if (b.SensorRange > sensorRange)
                    sensorRange = b.SensorRange;

                projectorRange = Math.Max(projectorRange, b.ProjectorRange + ObjectRadius);
                if (b.AllowInfantry)
                    AllowInfantry = true;
                if (b.WinsGame)
                    HasWinBuilding = true;
                if (b.AllowShipBuilding || b.IsSpacePort)
                    spacePort = true;
            }

            InfraStructure = InfraStructure.LowerBound(1);
            RepairPerTurn  = RepairPerTurn.LowerBound(0);

            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.usePlanetaryProjection)
                ProjectorRange = Owner.GetProjectorRadius() + ObjectRadius;
            else 
                ProjectorRange = projectorRange;

            TerraformToAdd /= Scale; // Larger planets take more time to terraform, visa versa for smaller ones

            SensorRange = sensorRange;
            UpdateMaxPopulation();
            TotalDefensiveStrength = (int)TroopManager.GroundStrength(Owner);

            ShieldStrengthMax *= (1 + Owner.data.ShieldPowerMod);
            // Added by Gretman -- This will keep a planet from still having shields even after the shield building has been scrapped.
            ShieldStrengthCurrent = ShieldStrengthCurrent.Clamped(0,ShieldStrengthMax);
            HasSpacePort          = spacePort && (colonyType != ColonyType.Research || Owner.isPlayer); // FB todo - why research Governor is omitted here?
            //this is a hack to prevent research planets from wasting workers on production.

            // greedy bastards
            Consumption = (ConsumptionPerColonist * PopulationBillion) + TotalTroopConsumption;
            Food.Update(NonCybernetic ? Consumption : 0f);
            Prod.Update(IsCybernetic  ? Consumption : 0f);
            Res.Update(0f);
            Money.Update();

            if (!loadUniverse)
                Station.SetVisibility(HasSpacePort, Empire.Universe.ScreenManager, this);

            Storage.Max = totalStorage.Clamped(10f, 10000000f);
        }

        public bool ShipWithinSensorRange(Ship ship)
        {
            return ship.Center.Distance(Center) < SensorRange;
        }

        private static float CalcShipBuildingModifier(int numShipyards)
        {
            float shipyardDiminishedReturn = 1;
            float shipBuildingModifier     = 1;

            for (int i = 0; i < numShipyards; ++i)
            {
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.ShipyardBonus > 0)
                    shipBuildingModifier *= 1 - (GlobalStats.ActiveModInfo.ShipyardBonus / shipyardDiminishedReturn);
                else
                    shipBuildingModifier *= 1 - (0.25f / shipyardDiminishedReturn);

                shipyardDiminishedReturn += 0.2f;
            }

            return shipBuildingModifier;
        }

        public bool TryGetShipsNeedRearm(out Ship[] shipsNeedRearm, Empire empire)
        {
            // Not using the planet Owner since it might have been changed by invasion
            shipsNeedRearm = null;
            if (ParentSystem.DangerousForcesPresent(empire) || ParentSystem.ShipList.Count == 0)
                return false;

            shipsNeedRearm = ParentSystem.ShipList.Filter(s => (s.loyalty == empire || s.loyalty.IsAlliedWith(empire))
                                                               && s.IsSuitableForPlanetaryRearm());

            shipsNeedRearm = shipsNeedRearm.SortedDescending(s => s.OrdinanceMax - s.Ordinance);
            return shipsNeedRearm.Length > 0;
        }

        public int NumSupplyShuttlesCanLaunch() // Net, after subtracting already launched shuttles
        {
            var planetSupplyGoals = Owner.GetEmpireAI()
                .Goals.Filter(g => g.type == AI.GoalType.RearmShipFromPlanet && g.PlanetBuildingAt == this);

            return (int)InfraStructure - planetSupplyGoals.Length;
        }

        private void UpdateHomeDefenseHangars(Building b)
        {
            if (SpaceCombatNearPlanet || b.CurrentNumDefenseShips == b.DefenseShipsCapacity)
                return;

            if (ParentSystem.ShipList.Any(t => t.IsHomeDefense))
                return; // if there are still defense ships our there, don't update building's hangars

            b.UpdateCurrentDefenseShips(1);
        }

        public void UpdateDefenseShipBuildingOffense()
        {
            if (Owner == null)
                return;

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building b = BuildingList[i];
                b.UpdateDefenseShipBuildingOffense(Owner, this);
            }
        }

        public void SearchAndRemoveTroopFromTile(Troop t)
        {
            for (int i = 0; i < TilesList.Count; i++)
            {
                PlanetGridSquare tile = TilesList[i];
                tile.TroopsHere.Remove(t);
            }
        }

        public void TryCrashOn(Ship ship)
        {
            if (!Habitable || NumActiveCrashSites >= (ship.IsMeteor ? 10 : 5))
                return;

            float survivalChance = TryMeteorHitShield() ? 0 : GetSurvivalChance();
            if (RandomMath.RollDice(survivalChance) 
                && TryGetCrashTile(out PlanetGridSquare crashTile)
                && (ship.IsMeteor || !crashTile.LavaHere))
            {
                int numTroopsSurvived = GetNumTroopSurvived(out string troopName);
                if (ship.IsMeteor)
                    CrashMeteor(ship, crashTile);
                else
                    crashTile.CrashSite.CrashShip(ship.loyalty, ship.Name, troopName, numTroopsSurvived, this, crashTile, ship.SurfaceArea);
            }

            // Local Functions
            float GetSurvivalChance()
            {
                float chance = 20 + ship.Level * 2;
                chance      *= 1 / Scale; // Gravity affects how hard is a crash

                if (!ship.CanBeRefitted)
                    chance *= 0.1f; // Dont recover hangar ships or home defense ships so easily.

                if (!Type.EarthLike)
                    chance *= 1.5f; // No atmosphere, not able to burn during planetfall

                chance *= 1 + ship.loyalty.data.Traits.ModHpModifier; // Skilled engineers (or not)
                chance += ship.SurfaceArea / 100f;
                return chance.Clamped(1, 100);
            }

            bool TryGetCrashTile(out PlanetGridSquare tile)
            {
                tile = null;
                float destroyBuildingChance = ship.SurfaceArea / (ship.IsMeteor ? 5f : 50f);
                var potentialTiles = RandomMath.RollDice(destroyBuildingChance)
                                     ? TilesList.Filter(t => t.CanCrashHere) 
                                     : TilesList.Filter(t => t.NoBuildingOnTile);

                if (potentialTiles.Length == 0)
                    return false;

                tile = potentialTiles.RandItem();
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
                    if (RandomMath.RollDice(troopSurvival))
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

                if (Empire.Universe.IsSystemViewOrCloser
                    && Empire.Universe.Frustum.Contains(Center, OrbitalRadius * 2))
                {
                    Shield.HitShield(this, ship, Center, SO.WorldBoundingSphere.Radius + 100f);
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
                DestroyBuildingOn(tile); // todo notify
                if (Owner == EmpireManager.Player)
                    Empire.Universe.NotificationManager.AddBuildingDestroyedByMeteor(this, tile.Building);
            }

            int bid;
            string message;
            bool richness;
            switch (RandomMath.RollDie(20))
            {
                case 1:  bid = Building.Crater1Id; message = Localizer.Token(GameText.AMeteorHasCrashedOn); richness = false; break;
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

            Building b = ResourceManager.CreateBuilding(bid);
            tile.PlaceBuilding(b, this);
            SetHasDynamicBuildings(true);
            if (Owner == EmpireManager.Player)
                Empire.Universe.NotificationManager.AddMeteorRelated(this, message);

            Population = (Population - popKilled).LowerBound(0);
            if (Owner != null && Population.AlmostZero())
                WipeOutColony(EmpireManager.Unknown);
        }

        public void SetHasLimitedResourceBuilding(bool value)
        {
            HasLimitedResourceBuilding = value;
        }

        void UpdateLimitedResourceCaches()
        {
            if (!HasLimitedResourceBuilding)
                return;

            bool foundCache = false;
            for (int i = BuildingList.Count - 1; i >= 0; i--)
            {
                Building b = BuildingList[i];
                if (b.FoodCache.Greater(0))
                {
                    foundCache = true;
                    b.FoodCache -= b.PlusFlatFoodAmount;
                    if (b.FoodCache.LessOrEqual(0))
                    {
                        if (Owner == EmpireManager.Player)
                            Empire.Universe.NotificationManager.AddBuildingDestroyed(this, b, Localizer.Token(GameText.WasRemovedSinceItsResource));

                        RemoveBuildingFromPlanet(b, destroy: true);
                    }
                }

                if (b.ProdCache.Greater(0))
                {
                    foundCache = true;
                    b.ProdCache -= Prod.Percent * PopulationBillion * b.PlusProdPerColonist;
                    if (b.ProdCache.LessOrEqual(0))
                    {
                        if (Owner == EmpireManager.Player)
                            Empire.Universe.NotificationManager.AddBuildingDestroyed(this, b, Localizer.Token(GameText.WasRemovedSinceItsResource));

                        RemoveBuildingFromPlanet(b, destroy: true);
                    }
                }
            }

            SetHasLimitedResourceBuilding(foundCache);
        }

        private void ApplyResources()
        {
            float foodRemainder = Storage.AddFoodWithRemainder(Food.NetIncome);
            float prodRemainder = Storage.AddProdWithRemainder(Prod.NetIncome);

            // produced food is already consumed by denizens during resource update
            // if remainder is negative even after adding to storage,
            // then we are starving
            Unfed = IsCybernetic ? prodRemainder : foodRemainder;
            if (Unfed > 0f) Unfed = 0f; // we have surplus, nobody is unfed

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

            // If the planet outputs 100 production on Brutal, the chance to decay is 5%
            float decayChance = Prod.GrossIncome / (Owner.DifficultyModifiers.MineralDecayDivider / GlobalStats.CustomMineralDecay);

            // Larger planets have less chance for reduction
            decayChance /= Scale.LowerBound(0.1f);

            // Decreasing chance of decay if Richness below 1
            // Increasing Chance of decay if richness is above one (limit to max of *2)
            decayChance *= MineralRichness.UpperBound(2f);

            // Longer pace decreases decay chance
            decayChance *= 1 / CurrentGame.ProductionPace;

            if (RandomMath.RollDice(decayChance))
            {
                bool notifyPlayer = MineralRichness.AlmostEqual(1);
                MineralRichness  -= 0.02f;
                if (notifyPlayer)
                {
                    string fullText = $"{Name} {Localizer.Token(GameText.MineralRichnessHasGoneDown)}";
                    Empire.Universe.NotificationManager.AddRandomEventNotification(
                        fullText, Type.IconPath, "SnapToPlanet", this);
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
                if (Owner.isPlayer)
                    threshold = AutoBuildTroops ? 0 : GarrisonSize;

                return (TroopsHere.Count - threshold).LowerBound(0);
            }
        }

        /// <param name="clearAndPresentDanger">indicates threats can destroy friendly ships</param>
        public bool EnemyInRange(bool clearAndPresentDanger = false)
        {
            if (clearAndPresentDanger ? !ParentSystem.DangerousForcesPresent(Owner) 
                                      : !ParentSystem.HostileForcesPresent(Owner))
                return false;

            float distance = GravityWellRadius.LowerBound(7500);
            foreach (Ship ship in ParentSystem.ShipList)
            {
                if (Owner?.IsEmpireAttackable(ship.loyalty, ship) == true && ship.InRadius(Center, distance))
                    return true;
            }
            return false;
        }

        public bool OurShipsCanScanSurface(Empire us)
        {
            // this is one of the reasons i want to change the way sensors are done to have a class containing sensor information.
            // so we dont have to do this scan more than once. 
            // todo: Build common sensor container class. 
            // this scan should only need to be done once.
            
            var ships      = us.OwnedShips;
            var projectors = us.OwnedProjectors;

            bool scanned = ships.Any(s => s.Active && s.Center.InRadius(Center, s.SensorRange));
            if (!scanned)
                scanned = projectors.Any(s => s.Active && s.Center.InRadius(Center, s.SensorRange));

            return scanned;
        }

        private void GrowPopulation()
        {
            if (Owner == null || MightBeAWarZone(Owner))
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
            Population = 0f;
            if (Owner == null)
                return;

            UpdateTerraformPoints(0);
            Owner.RemovePlanet(this, attacker);
            if (IsExploredBy(EmpireManager.Player) && (Owner.isPlayer || attacker.isPlayer))
                Empire.Universe.NotificationManager.AddPlanetDiedNotification(this);

            bool removeOwner = true;
            foreach (Planet other in ParentSystem.PlanetList)
            {
                if (other.Owner != Owner || other == this)
                    continue;

                removeOwner = false;
            }

            if (removeOwner)
                ParentSystem.OwnerList.Remove(Owner);

            Construction.ClearQueue();
            Owner = null;
        }

        public bool EventsOnTiles()
        {
            return TilesList.Any(t => t.EventOnTile);
        }

        public int NumActiveCrashSites => TilesList.Count(t => t.CrashSite.Active);

        // Bump out an enemy troop to make room available (usually for spawned troops via events)
        public bool BumpOutTroop(Empire empire)
        {
            Troop[] enemyTroops = TroopsHere.Filter(t => t.Loyalty != empire);
            if (enemyTroops.Length == 0) // we completely filled the planet by ourselves
                return false;
            Troop lastEnemyTroop = enemyTroops[enemyTroops.Length - 1];
            return lastEnemyTroop.Launch() != null;
        }

        public int TotalInvadeInjure         => BuildingList.Sum(b => b.InvadeInjurePoints);
        public float BuildingGeodeticOffense => BuildingList.Sum(b => b.Offense);
        public int BuildingGeodeticCount     => BuildingList.Count(b => b.Offense > 0);
        public float TotalGeodeticOffense    => BuildingGeodeticOffense + OrbitalStations.Sum(o => o.BaseStrength);
        public int MaxDefenseShips           => BuildingList.Sum(b => b.DefenseShipsCapacity);
        public int CurrentDefenseShips       => BuildingList.Sum(b => b.CurrentNumDefenseShips) + ParentSystem.ShipList.Count(s => s?.HomePlanet == this);
        public float HabitablePercentage     => (float)TilesList.Count(tile => tile.Habitable) / TileArea;
        public float HabitableBuiltCoverage  => 1 - (float)FreeHabitableTiles/TotalHabitableTiles;

        public int FreeHabitableTiles    => TilesList.Count(tile => tile.Habitable && tile.NoBuildingOnTile);
        public int TotalHabitableTiles   => TilesList.Count(tile => tile.Habitable);
        public float MoneyBuildingRatio  => (float)TotalMoneyBuildings / TotalBuildings;
        public int TotalMoneyBuildings   => TilesList.Count(tile => tile.BuildingOnTile &&  tile.Building.IsMoneyBuilding);

        public int TotalBuildings    => TilesList.Count(tile => tile.BuildingOnTile);
        public bool TerraformingHere => BuildingList.Any(b => b.IsTerraformer);
        public int  TerraformersHere => BuildingList.Count(b => b.IsTerraformer);
        public bool HasCapital       => BuildingList.Any(b => b.IsCapital);


        public void SetHasDynamicBuildings(bool value)
        {
            HasDynamicBuildings = value;
        }

        public void ResetHasDynamicBuildings()
        {
            HasDynamicBuildings = BuildingList.Any(b => b.IsDynamicUpdate);
        }

        private void RepairBuildings(int repairAmount)
        {
            if (MightBeAWarZone(Owner))
                return;

            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building b        = BuildingList[i];
                Building t        = ResourceManager.GetBuildingTemplate(b.BID);
                b.CombatStrength  = (b.CombatStrength + repairAmount).Clamped(0, t.CombatStrength);
                b.Strength        = (b.Strength + repairAmount).Clamped(0, t.Strength);
                UpdateHomeDefenseHangars(b);
            }
        }

        void CallForHelp()
        {
            if (!SpaceCombatNearPlanet)
                return;

            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship = ParentSystem.ShipList[i];
                if (ship.loyalty != Owner || ship.InCombat)
                    continue;

                if (!ship.IsFreighter 
                    && ship.BaseStrength > 0 
                    && (ship.AI.State == AI.AIState.AwaitingOrders || ship.AI.State == AI.AIState.Orbit))
                {
                    // Move Offensively to planet
                    Vector2 finalDir = ship.Position.DirectionToTarget(Center);
                    ship.AI.OrderMoveToNoStop(Center, finalDir, false, AI.AIState.MoveTo, null, true);
                }
            }
        }

        public PlanetGridSquare GetTileByCoordinates(int x, int y)
        {
            if (x < 0 || x >= TileMaxX || y < 0 || y >= TileMaxY) // FB >= because coords start from 0
                return null;

            return TilesList.Find(pgs => pgs.X == x && pgs.Y == y);
        }

        ~Planet() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        private void Destroy()
        {
            ActiveCombats?.Dispose(ref ActiveCombats);
            OrbitalDropList?.Dispose(ref OrbitalDropList);
            Construction    = null;
            Storage   = null;
            TroopManager    = null;
            GeodeticManager = null;
            TroopsHere?.Dispose(ref TroopsHere);
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
            debug.AddLine($"{ParentSystem.Name} : {Name}", Color.Green);
            debug.AddLine($"Scale: {Scale}");
            debug.AddLine($"Population per Habitable Tile: {BasePopPerTile}");
            debug.AddLine($"Environment Modifier for {EmpireManager.Player.Name}: {EmpireManager.Player.PlayerEnvModifier(Category)}");
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

