using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public partial class Planet : SolarSystemBody, IDisposable
    {
        public enum ColonyType
        {
            Core,
            Colony,
            Industrial,
            Research,
            Agricultural,
            Military,
            TradeHub
        }

        public override string ToString() =>
            $"{Name} ({Owner?.Name ?? "No Owner"}) T:{colonyType} NET(FD:{Food.NetIncome.String()} PR:{Prod.NetIncome.String()}) {ImportsDescr()}";

        public GeodeticManager GeodeticManager;
        public TroopManager TroopManager;
        public SpaceStation Station = new SpaceStation(null);

        public bool GovBuildings       = true;
        public bool GovOrbitals        = true;
        public bool GovMilitia         = false;
        public bool DontScrapBuildings = false;
        public bool AllowInfantry;

        public int CrippledTurns;
        public int TotalDefensiveStrength { get; private set; }

        public bool HasWinBuilding;
        private float ShipBuildingModifierBacker;
        public float ShipBuildingModifier
        {
            get => ShipBuildingModifierBacker;
            private set => ShipBuildingModifierBacker = value.Clamped(0.001f, 1);
        }

        public int NumShipyards;
        public float Consumption { get; private set; } // Food (NonCybernetic) or Production (IsCybernetic)
        private float Unfed;
        public bool IsStarving => Unfed < 0f;
        public bool CorsairPresence;
        public bool QueueEmptySent = true;
        public float RepairPerTurn;
        public float SensorRange { get; private set; }
        public bool SpaceCombatNearPlanet { get; private set; }
        public static string GetDefenseShipName(ShipData.RoleName roleName, Empire empire) => ShipBuilder.PickFromCandidates(roleName, empire);
        public float ColonyValue { get; private set; }
        public float ExcessGoodsIncome { get; private set; } // FB - excess goods tax for empire to collect
        public float OrbitalsMaintenance;

        private const string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions

        public bool RecentCombat    => TroopManager.RecentCombat;
        public float MaxConsumption => MaxPopulationBillion + Owner.data.Traits.ConsumptionModifier * MaxPopulationBillion;

        public bool WeCanLandTroopsViaSpacePort(Empire us) => HasSpacePort && Owner == us && !SpaceCombatNearPlanet;

        public int CountEmpireTroops(Empire us) => TroopManager.NumEmpireTroops(us);
        public int GetDefendingTroopCount()     => TroopManager.NumDefendingTroopCount;

        public int GetEstimatedTroopsToInvade(int bestTroopStrength = 10)
        {
            float strength = TroopManager.GroundStrength(Owner); //.ClampMin(100);
            return strength > 0 ? (int)Math.Ceiling(strength / bestTroopStrength.ClampMin(1)) : 0;

        }
        public bool AnyOfOurTroops(Empire us)   => TroopManager.WeHaveTroopsHere(us);
        public int GetGroundLandingSpots()      => TroopManager.NumGroundLandingSpots();

        public float GetGroundStrength(Empire empire)   => TroopManager.GroundStrength(empire);
        public int GetPotentialGroundTroops()           => TroopManager.GetPotentialGroundTroops();
        public bool TroopsHereAreEnemies(Empire empire) => TroopManager.TroopsHereAreEnemies(empire);
        public bool WeAreInvadingHere(Empire empire)    => TroopManager.WeAreInvadingHere(empire);
        public bool MightBeAWarZone(Empire empire)      => TroopManager.MightBeAWarZone(empire);
        public bool ForeignTroopHere(Empire empire)      => TroopManager.ForeignTroopHere(empire);


        public float GetGroundStrengthOther(Empire allButThisEmpire)      => TroopManager.GroundStrengthOther(allButThisEmpire);
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.EmpireTroops(empire, maxToTake);

        public bool NoGovernorAndNotTradeHub             => colonyType != ColonyType.Colony && colonyType != ColonyType.TradeHub;

        public float Fertility                      => FertilityFor(Owner);
        public float FertilityFor(Empire empire)    => BaseFertility * empire?.RacialEnvModifer(Category) ?? BaseFertility;
        public float MaxFertilityFor(Empire empire) => BaseMaxFertility * empire?.RacialEnvModifer(Category) ?? BaseMaxFertility;

        public bool IsCybernetic  => Owner != null && Owner.IsCybernetic;
        public bool NonCybernetic => Owner != null && Owner.NonCybernetic;
        public int TileArea       => TileMaxX * TileMaxY; // FB currently this limited by number of tiles, all planets are 7 x 5
        // FB - free tiles always leaves 1 free spot for invasions
        public int FreeTiles      => (TilesList.Count(t => t.TroopsHere.Count < t.MaxAllowedTroops && !t.CombatBuildingOnTile) - 1)
                                     .Clamped(0, TileArea);

        public float MaxPopulationBillion                   => MaxPopulation / 1000;
        public float MaxPopulationBillionFor(Empire empire) => MaxPopulationFor(empire) / 1000;

        public float MaxPopulation => MaxPopulationFor(Owner);

        public float MaxPopulationFor(Empire empire)
        {
            if (!Habitable)
                return 0;

            float minimumPop = BasePopPerTile + PopulationBonus; // At least a tile's worth population and any max pop bonus buildings have
            if (empire == null)
                return Math.Max(minimumPop, MaxPopValFromTiles + PopulationBonus);

            return Math.Max(minimumPop, MaxPopValFromTiles * empire.RacialEnvModifer(Category) + PopulationBonus);
        }

        public int FreeTilesWithRebaseOnTheWay
        {
            get {
                int rebasingTroops = Owner.GetShips().Filter(s => s.IsDefaultTroopTransport)
                                          .Count(s => s.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == this));
                return (FreeTiles - rebasingTroops).Clamped(0, TileArea);
            }
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

        public float GravityWellForEmpire(Empire empire)
        {
            if (!Empire.Universe.GravityWells)
                return 0;

            if (Owner == null)
                return GravityWellRadius;

            if (Owner == empire || Owner.GetRelations(empire).Treaty_Alliance)
                return 0;

            return GravityWellRadius;
        }

        public float ColonyWorthTo(Empire empire)
        {
            float worth = PopulationBillion + MaxPopulationBillionFor(empire);
            if (empire.NonCybernetic)
            {
                worth += (FoodHere / 50f) + (ProdHere / 50f);
                worth += FertilityFor(empire)*1.5f;
                worth += MineralRichness;
            }
            else // filthy Opteris
            {
                worth += (ProdHere / 25f);
                worth += MineralRichness*2.0f;
            }

            foreach (Building b in BuildingList)
                worth += b.ActualCost / 50f;

            if (worth < 15f)
                worth = 15f;

            if (empire.data.EconomicPersonality.Name == "Expansionists")
                worth *= 1.35f;

            return worth;
        }

        public void SetInGroundCombat()
        {
            TroopManager.SetInCombat();
        }

        public float EmpireFertility(Empire empire) =>
            empire.IsCybernetic ? MineralRichness : FertilityFor(empire);

        public float ColonyBaseValue(Empire empire)
        {
            float value = 0;
            value += BuildingList.Count(b => b.IsCommodity) * 30;
            value += EmpireFertility(empire) * 10;
            value += MineralRichness * 10;
            value += MaxPopulationBillionFor(empire) * 5;
            value += BuildingList.Any(b => b.IsCapital) ? 100 : 0;
            value += BuildingList.Sum(b => b.ActualCost) / 10;
            value += PopulationBillion * 5;

            return value;
        }

        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        // added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);

        public void ApplyBombEnvEffects(float amount, Empire attacker) // added by Fat Bastard
        {
            Population -= 1000f * amount;
            AddBaseFertility(amount * -0.25f); // environment suffers temp damage
            if (BaseFertility.LessOrEqual(0) && RandomMath.RollDice(amount * 200))
                AddMaxBaseFertility(-0.02f); // permanent damage to Max Fertility

            if (MaxPopulation.AlmostZero())
                WipeOutColony(attacker);
        }

        public void Update(float elapsedTime)
        {
            RefreshOrbitalStations();
            UpdateHabitable(elapsedTime);
            UpdatePosition(elapsedTime);
        }

        void UpdateHabitable(float elapsedTime)
        {
            if (!Habitable)
                return;

            TroopManager.Update(elapsedTime);
            GeodeticManager.Update(elapsedTime);

            SpaceCombatNearPlanet = EnemyInRange();
            if (SpaceCombatNearPlanet)
                UpdateSpaceCombatBuildings(elapsedTime);

            UpdatePlanetaryProjectiles(elapsedTime);
        }

        void RefreshOrbitalStations()
        {
            if (OrbitalStations.Count == 0)
                return;

            Guid[] keys = OrbitalStations.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                Guid key = keys[i];
                Ship shipyard = OrbitalStations[key];
                if (shipyard == null || !shipyard.Active || shipyard.SurfaceArea == 0)
                    OrbitalStations.Remove(key);
            }
        }

        void UpdateSpaceCombatBuildings(float elapsedTime) // @todo FB - need to work on this
        {
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                float previousD;
                float previousT;
                Building building = BuildingList[i];
                if (building.isWeapon)
                {
                    building.WeaponTimer -= elapsedTime;
                    if (building.WeaponTimer.Greater(0))
                        continue;

                    previousD = building.TheWeapon.BaseRange + 1000f;
                    previousT = previousD;
                }
                else if (building.DefenseShipsCapacity > 0 && Owner.Money > 0)
                {
                    previousD = 10000f;
                    previousT = previousD;
                }
                else
                    continue;

                Ship target = null;
                Ship troop = null;
                for (int j = 0; j < ParentSystem.ShipList.Count; ++j)
                {
                    Ship ship = ParentSystem.ShipList[j];
                    if (ship.loyalty == Owner)
                        continue;

                    if (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact)
                        continue;

                    float currentD = Vector2.Distance(Center, ship.Center);
                    if (ship.shipData.Role == ShipData.RoleName.troop && currentD < previousT)
                    {
                        previousT = currentD;
                        troop     = ship;
                        continue;
                    }
                    if (currentD < previousD && troop == null)
                    {
                        previousD = currentD;
                        target    = ship;
                    }
                }

                if (troop != null)
                    target = troop;
                if (target == null)
                    continue;

                if (building.isWeapon)
                {
                    building.TheWeapon.FireFromPlanet(this, target);
                    building.WeaponTimer = building.TheWeapon.fireDelay;
                }
                else if (building.CurrentNumDefenseShips > 0)
                {
                    LaunchDefenseShips(building.DefenseShipsRole, Owner);
                    building.UpdateCurrentDefenseShips(-1, Owner);
                }
            }
        }

        void LaunchDefenseShips(ShipData.RoleName roleName, Empire empire)
        {
            string defaultShip         = empire.data.StartingShip;
            string selectedShip        = GetDefenseShipName(roleName, empire) ?? defaultShip;
            Vector2 launchVector       = MathExt.RandomOffsetAndDistance(Center, 1000);
            Ship defenseShip           = Ship.CreateDefenseShip(selectedShip, empire, launchVector, this);
            if (defenseShip == null)
                Log.Warning($"Could not create defense ship, ship name = {selectedShip}");
            else
            {
                defenseShip.Level = 3;
                defenseShip.Velocity = UniverseRandom.RandomDirection() * defenseShip.Speed;
                empire.AddMoney(-defenseShip.GetCost(Owner) / 10);
            }
        }

        public void LandDefenseShip(ShipData.RoleName roleName, float shipCost, float shipHealthPercent)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building building = BuildingList[i];
                if (building.DefenseShipsRole == roleName
                    && building.CurrentNumDefenseShips < building.DefenseShipsCapacity)
                {
                    building.UpdateCurrentDefenseShips(1, Owner);
                }
            }
            Owner.AddMoney((shipCost * shipHealthPercent / 10));
        }

        void UpdatePlanetaryProjectiles(float elapsedTime)
        {
            if (elapsedTime <= 0f) return;
            for (int i = Projectiles.Count - 1; i >= 0; --i)
            {
                Projectile p = Projectiles[i];
                if (p.Active) p.Update(elapsedTime);
            }
            Projectiles.RemoveInActiveObjects();
        }

        public void DestroyTile(PlanetGridSquare tile) => DestroyBioSpheres(tile); // since it does the same as DestroyBioSpheres

        public void DestroyBioSpheres(PlanetGridSquare tile)
        {
            RemoveBuildingFromPlanet(tile);
            tile.Habitable = false;
            if (tile.Biosphere)
                ClearBioSpheresFromList(tile);

            UpdateMaxPopulation();
        }

        public void ScrapBuilding(Building b)
        {
            RemoveBuildingFromPlanet(b);
            ProdHere += b.ActualCost / 2f;
        }

        private void RemoveBuildingFromPlanet(Building b)
        {
            BuildingList.Remove(b);
            PlanetGridSquare pgs = TilesList.Find(tile => tile.building == b);
            if (pgs != null)
                pgs.building = null;
            else
                Log.Error($"{this} failed to find tile with building {b}");

            PostBuildingRemoval(b);
        }

        private void RemoveBuildingFromPlanet(PlanetGridSquare tile)
        {
            if (tile?.building == null)
                return;

            Building b = tile.building;
            BuildingList.Remove(b);
            tile.building = null;
            PostBuildingRemoval(b);
        }

        private void PostBuildingRemoval(Building b)
        {
            if (b.MaxFertilityOnBuild > 0)
                AddMaxBaseFertility(-b.MaxFertilityOnBuild); // FB - we are reversing positive MaxFertilityOnBuild when scrapping

            if (b.IsTerraformer && !TerraformingHere)
                UpdateTerraformPoints(0); // FB - no terraformers present, terraform effort halted
        }

        public void ClearBioSpheresFromList(PlanetGridSquare tile)
        {
            tile.Biosphere = false;

            var biospheresList = BuildingList.Filter(b => b.IsBiospheres);
            if (biospheresList.Length > 0)
                BuildingList.Remove(biospheresList.First());
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            CrippledTurns = Math.Max(0, CrippledTurns - 1);
            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentLevel();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            ApplyTerraforming();
            UpdateColonyValue();
            RemoveInvalidFreighters(IncomingFreighters);
            RemoveInvalidFreighters(OutgoingFreighters);
            UpdateBaseFertility();
            InitResources(); // must be done before Governing
            UpdateOrbitalsMaint();
            NotifyEmptyQueue();
            RechargePlanetaryShields();
            ApplyResources();
            GrowPopulation();
            TroopManager.HealTroops(2);
            RepairBuildings(1);
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
            if (ShieldStrengthMax.LessOrEqual(0) || ShieldStrengthCurrent.GreaterOrEqual(ShieldStrengthMax) || RecentCombat)
                return; // fully recharged or in combat

            float maxRechargeRate = ShieldStrengthMax / 25;
            float rechargeRate    = (ShieldStrengthCurrent * 100 / ShieldStrengthMax).Clamped(1, maxRechargeRate);
            ShieldStrengthCurrent = (ShieldStrengthCurrent + rechargeRate).Clamped(0, ShieldStrengthMax);
        }

        private void UpdateColonyValue() => ColonyValue = Owner != null ? ColonyBaseValue(Owner) : 0;

        public float PopPerTileFor(Empire empire) => BasePopPerTile * empire?.RacialEnvModifer(Category) ?? BasePopPerTile;

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
            int numHabitableTiles = 0;
            if (Type.Habitable)
            {
                numHabitableTiles = TilesList.Count(t => t.Habitable && !t.Biosphere);
                PopulationBonus   = BuildingList.Filter(b => !b.IsBiospheres).Sum(b => b.MaxPopIncrease)
                                    + BuildingList.Count(b => b.IsBiospheres) * BasePopPerTile;
            }

            MaxPopValFromTiles = Math.Max(BasePopPerTile, BasePopPerTile * numHabitableTiles);
            MaxPopBillionVal   = MaxPopValFromTiles / 1000f;
        }

        public int Level { get; private set; }
        public string DevelopmentStatus { get; private set; } = "Undeveloped";

        public void UpdateDevelopmentLevel() // need to check this with Racial env
        {
            if (PopulationBillion <= 0.5f)
            {
                Level = (int)DevelopmentLevel.Solitary;
                DevelopmentStatus = Localizer.Token(1763);
                if      (MaxPopulationBillion >= 2f  && !IsBarrenType) DevelopmentStatus += Localizer.Token(1764);
                else if (MaxPopulationBillion >= 2f  &&  IsBarrenType) DevelopmentStatus += Localizer.Token(1765);
                else if (MaxPopulationBillion < 0.0f && !IsBarrenType) DevelopmentStatus += Localizer.Token(1766);
                else if (MaxPopulationBillion < 0.5f &&  IsBarrenType) DevelopmentStatus += Localizer.Token(1767);
            }
            else if (PopulationBillion > 0.5f && PopulationBillion <= 2)
            {
                Level = (int)DevelopmentLevel.Meager;
                DevelopmentStatus = Localizer.Token(1768);
                DevelopmentStatus += MaxPopulationBillion >= 2 ? Localizer.Token(1769) : Localizer.Token(1770);
            }
            else if (PopulationBillion > 2.0 && PopulationBillion <= 5.0)
            {
                Level = (int)DevelopmentLevel.Vibrant;
                DevelopmentStatus = Localizer.Token(1771);
                if      (MaxPopulationBillion >= 5.0) DevelopmentStatus += Localizer.Token(1772);
                else if (MaxPopulationBillion <  5.0) DevelopmentStatus += Localizer.Token(1773);
            }
            else if (PopulationBillion > 5.0 && PopulationBillion <= 10.0)
            {
                Level = (int)DevelopmentLevel.CoreWorld;
                DevelopmentStatus = Localizer.Token(1774);
            }
            else if (PopulationBillion > 10.0)
            {
                Level = (int)DevelopmentLevel.MegaWorld;
                DevelopmentStatus = Localizer.Token(1775); // densely populated
            }

            if (Prod.NetIncome >= 10.0 && HasSpacePort)
                DevelopmentStatus += Localizer.Token(1776); // fine shipwright
            else if (Fertility >= 2.0 && Food.NetIncome > MaxPopulation)
                DevelopmentStatus += Localizer.Token(1777); // fine agriculture
            else if (Res.NetIncome > 5.0)
                DevelopmentStatus += Localizer.Token(1778); // universities are good

            if (AllowInfantry && TroopsHere.Count > 6)
                DevelopmentStatus += Localizer.Token(1779); // military culture
        }

        int ColonyTypeLocId()
        {
            switch (colonyType)
            {
                default:
                case ColonyType.Core:         return 378;
                case ColonyType.Colony:       return 382;
                case ColonyType.Industrial:   return 379;
                case ColonyType.Research:     return 381;
                case ColonyType.Agricultural: return 377;
                case ColonyType.Military:     return 380;
                case ColonyType.TradeHub:     return 394;
            }
        }
        public string ColonyTypeInfoText => Localizer.Token(ColonyTypeLocId());

        int WorldTypeLocId()
        {
            switch (colonyType)
            {
                default:
                case ColonyType.Core:         return 372;
                case ColonyType.Colony:       return 376;
                case ColonyType.Industrial:   return 373;
                case ColonyType.Research:     return 375;
                case ColonyType.Agricultural: return 371;
                case ColonyType.Military:     return 374;
                case ColonyType.TradeHub:     return 393;
            }
        }
        public string WorldType => Localizer.Token(WorldTypeLocId());

        void UpdateBaseFertility()
        {
            if (BaseFertility.AlmostEqual(BaseMaxFertility))
                return;

            if (BaseFertility < BaseMaxFertility)
                BaseFertility = (BaseFertility + 0.01f).Clamped(0, BaseMaxFertility); // FB - Slowly increase fertility to max fertility
            else if (BaseFertility > BaseMaxFertility)
                BaseFertility = BaseFertility.Clamped(0, BaseFertility - 0.01f); // FB - Slowly decrease fertility to max fertility
        }

        public void SetBaseFertility(float fertility, float maxFertility)
        {
            BaseMaxFertility = maxFertility;
            BaseFertility = fertility;
        }

        public void SetBaseFertilityMinMax(float fertility) => SetBaseFertility(fertility, fertility);

        public void AddMaxBaseFertility(float amount)
        {
            BaseMaxFertility += amount;
            BaseMaxFertility  = Math.Max(0, BaseMaxFertility);
        }

        // FB: to enable bombs to temp change fertility immediately by specified amount
        public void AddBaseFertility(float amount)
        {
            BaseFertility += amount;
            BaseFertility  = Math.Max(0, BaseFertility);
        }

        // FB: note that this can be called multiple times in a turn - especially when selecting the planet or in colony screen
        // FB: @todo - this needs refactoring - its too long
        public void UpdateIncomes(bool loadUniverse)
        {
            if (Owner == null)
                return;

            bool spacePort = false;
            AllowInfantry  = false;
            RepairPerTurn        = 0;
            TerraformToAdd       = 0;
            ShieldStrengthMax    = 0;
            ShipBuildingModifier = 0;
            SensorRange          = 0;
            TotalDefensiveStrength     = 0;
            PlusFlatPopulationPerTurn  = 0;
            float totalStorage         = 0;

            if (!loadUniverse) // FB - this is needed since OrbitalStations from save has only GUID, so we must not use this when loading a game
            {
                var deadShipyards = new Array<Guid>();
                NumShipyards      = 0; // reset NumShipyards since we are not loading it from a save

                foreach (KeyValuePair<Guid, Ship> orbitalStation in OrbitalStations)
                {
                    if (orbitalStation.Value == null)
                        deadShipyards.Add(orbitalStation.Key);
                    else if (orbitalStation.Value.Active && orbitalStation.Value.shipData.IsShipyard)
                        NumShipyards++; // Found a shipyard, increase the number
                    else if (!orbitalStation.Value.Active)
                        deadShipyards.Add(orbitalStation.Key);
                }

                foreach (Guid key in deadShipyards)
                    OrbitalStations.Remove(key);
            }

            ShipBuildingModifier = CalcShipBuildingModifier(NumShipyards); // NumShipyards is either counted above or loaded from a save
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building b                 = BuildingList[i];
                PlusFlatPopulationPerTurn += b.PlusFlatPopulation;
                ShieldStrengthMax         += b.PlanetaryShieldStrengthAdded;
                TerraformToAdd            += b.PlusTerraformPoints;
                totalStorage              += b.StorageAdded;
                RepairPerTurn             += b.ShipRepair;

                if (b.SensorRange > SensorRange)
                    SensorRange = b.SensorRange;
                if (b.AllowInfantry)
                    AllowInfantry = true;
                if (b.WinsGame)
                    HasWinBuilding = true;
                if (b.AllowShipBuilding || b.IsSpacePort)
                    spacePort = true;
            }

            UpdateMaxPopulation();
            TotalDefensiveStrength = (int)TroopManager.GroundStrength(Owner);

            // Added by Gretman -- This will keep a planet from still having shields even after the shield building has been scrapped.
            ShieldStrengthCurrent = ShieldStrengthCurrent.Clamped(0,ShieldStrengthMax);
            HasSpacePort          = spacePort && (colonyType != ColonyType.Research || Owner.isPlayer); // FB todo - why research Governor is omitted here?
            //this is a hack to prevent research planets from wasting workers on production.

            // greedy bastards
            Consumption = (PopulationBillion + Owner.data.Traits.ConsumptionModifier * PopulationBillion);
            Food.Update(NonCybernetic ? Consumption : 0f);
            Prod.Update(IsCybernetic  ? Consumption : 0f);
            Res.Update(0f);
            Money.Update();

            if (!loadUniverse)
                Station.SetVisibility(HasSpacePort, Empire.Universe.ScreenManager, this);

            Storage.Max = totalStorage.Clamped(10f, 10000000f);
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

        private void UpdateHomeDefenseHangars(Building b)
        {
            if (EnemyInRange() || b.CurrentNumDefenseShips == b.DefenseShipsCapacity)
                return;

            if (ParentSystem.ShipList.Any(t => t.HomePlanet != null))
                return; // if there are still defense ships our there, don't update building's hangars

            b.UpdateCurrentDefenseShips(1, Owner);
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
        }

        public int GarrisonSize
        {
            get
            {
                if (!Owner.isPlayer)
                    return 0;  // AI manages It's own troops

                if (GovMilitia && colonyType != ColonyType.Colony)
                    return 0; // Player Governor will replace garrisoned troops with new ones

                return 5; // Default value for non Governor Player Colonies
            }
        }

        public bool EnemyInRange()
        {
            if (!ParentSystem.HostileForcesPresent(Owner))
                return false;

            float distance = GravityWellRadius.Clamped(7500, 15000);
            foreach (Ship ship in ParentSystem.ShipList)
            {
                if (Owner.IsEmpireAttackable(ship.loyalty) && ship.InRadius(Center, distance))
                    return true;
            }
            return false;
        }

        private void GrowPopulation()
        {
            if (Owner == null)
                return;

            if (PopulationRatio.Greater(1)) // Over population - the planet cannot support this amount of population
            {
                float popToRemove = ((1 - PopulationRatio) * 10).Clamped(20,1000);
                Population        = Math.Max(Population - popToRemove, MaxPopulation);
            }
            else if (IsStarving)
                Population += Unfed * 10f; // Reduces population depending on starvation severity.
            else
            {
                // population is increased
                float balanceGrowth = (1 - PopulationRatio).Clamped(0.1f, 1f);
                float repRate       = Owner.data.BaseReproductiveRate * Population * balanceGrowth;
                if (Owner.data.Traits.PopGrowthMax.NotZero())
                    repRate = Math.Min(repRate, Owner.data.Traits.PopGrowthMax * 1000f);

                repRate     = Math.Max(repRate, Owner.data.Traits.PopGrowthMin * 1000f);
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
            if (IsExploredBy(Empire.Universe.PlayerEmpire))
                Empire.Universe.NotificationManager.AddPlanetDiedNotification(this, Empire.Universe.PlayerEmpire);

            bool removeOwner = true;
            foreach (Planet other in ParentSystem.PlanetList)
            {
                if (other.Owner != Owner || other == this)
                    continue;

                removeOwner = false;
            }

            if (removeOwner)
                ParentSystem.OwnerList.Remove(Owner);

            ConstructionQueue.Clear();
            Owner = null;
        }

        public bool EventsOnBuildings()
        {
            bool events = false;
            foreach (Building building in BuildingList)
            {
                if (building.EventHere && !building.EventWasTriggered)
                {
                    events = true;
                    break;
                }
            }
            return events;
        }

        // Bump out an enemy troop to make room available (usually for spawned troops via events)
        public bool BumpOutTroop(Empire empire)
        {
            Troop randomEnemyTroop = TroopsHere.Filter(t => t.Loyalty != empire).RandItem();
            return randomEnemyTroop.Launch() != null;
        }

        public int TotalInvadeInjure         => BuildingList.Sum(b => b.InvadeInjurePoints);
        public float BuildingGeodeticOffense => BuildingList.Sum(b => b.Offense);
        public float TotalGeodeticOffense    => BuildingGeodeticOffense + OrbitalStations.Values.Sum(o => o.BaseStrength);
        public int MaxDefenseShips           => BuildingList.Sum(b => b.DefenseShipsCapacity);
        public int CurrentDefenseShips       => BuildingList.Sum(b => b.CurrentNumDefenseShips) + ParentSystem.ShipList.Count(s => s?.HomePlanet == this);

        public int AvailableTiles      => TilesList.Count(tile => tile.Habitable && tile.NoBuildingOnTile);
        public int TotalBuildings      => TilesList.Count(tile => tile.building != null && !tile.building.IsBiospheres);
        public float BuiltCoverage     => TotalBuildings / (float)TileArea;

        public int ExistingMilitaryBuildings  => BuildingList.Count(b => b.IsMilitary);
        public float TerraformTargetFertility => BuildingList.Sum(b => b.MaxFertilityOnBuild) + 1 / (Owner?.RacialEnvModifer(Owner.data.PreferredEnv) ?? 1);
        public bool TerraformingHere          => BuildingList.Any(b => b.IsTerraformer);

        public int DesiredMilitaryBuildings
        {
            get
            {
                float militaryCoverage;
                switch (colonyType)
                {
                    case ColonyType.Military: militaryCoverage = 0.4f; break;
                    case ColonyType.Core: militaryCoverage = 0.3f; break;
                    default: militaryCoverage = 0.2f; break;
                }
                float sizeFactor = (PopulationRatio + BuiltCoverage) / 2;
                return (int)Math.Floor(militaryCoverage * sizeFactor * TileArea);
            }
        }

        private void RepairBuildings(int repairAmount)
        {
            if (RecentCombat)
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

        public PlanetGridSquare GetTileByCoordinates(int x, int y)
        {
            if (x < 0 || x >= TileMaxX || y < 0 || y >= TileMaxY) // FB >= because coords start from 0
                return null;

            return TilesList.Find(pgs => pgs.x == x && pgs.y == y);
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
            Projectiles?.Dispose(ref Projectiles);
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
            debug.AddLine($"Environment Modifier for {EmpireManager.Player.Name}: {EmpireManager.Player.RacialEnvModifer(Category)}");
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

