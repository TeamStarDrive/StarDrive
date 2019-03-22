using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public bool GovBuildings = true;
        public bool GovOrbitals  = true;
        public bool AllowInfantry;

        public int CrippledTurns;
        public int TotalDefensiveStrength { get; private set; }

        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public int NumShipyards { get; private set; }
        public float Consumption { get; private set; } // Food (NonCybernetic) or Production (IsCybernetic)
        float Unfed;
        public bool IsStarving => Unfed < 0f;
        public bool CorsairPresence;
        public bool QueueEmptySent = true;
        public float RepairPerTurn;
        public static string GetDefenseShipName(ShipData.RoleName roleName, Empire empire) => ShipBuilder.PickFromCandidates(roleName, empire);
        public float ColonyValue { get; private set; }
        public float ExcessGoodsIncome { get; private set; } // FB - excess goods tax for empire to collect

        static string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions

        public bool RecentCombat    => TroopManager.RecentCombat;
        public float MaxConsumption => MaxPopulationBillion + Owner.data.Traits.ConsumptionModifier * MaxPopulationBillion;

        public int CountEmpireTroops(Empire us) => TroopManager.NumEmpireTroops(us);
        public int GetDefendingTroopCount()     => TroopManager.NumDefendingTroopCount;
        public bool AnyOfOurTroops(Empire us)   => TroopManager.WeHaveTroopsHere(us);
        public int GetGroundLandingSpots()      => TroopManager.NumGroundLandingSpots();

        public float GetGroundStrength(Empire empire)   => TroopManager.GroundStrength(empire);
        public int GetPotentialGroundTroops()           => TroopManager.GetPotentialGroundTroops();
        public bool TroopsHereAreEnemies(Empire empire) => TroopManager.TroopsHereAreEnemies(empire);

        public float GetGroundStrengthOther(Empire allButThisEmpire)      => TroopManager.GroundStrengthOther(allButThisEmpire);
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.EmpireTroops(empire, maxToTake);


        public bool IsCybernetic  => Owner != null && Owner.IsCybernetic;
        public bool NonCybernetic => Owner != null && Owner.NonCybernetic;
        public int MaxBuildings   => TileMaxX * TileMaxY; // FB currently this limited by number of tiles, all planets are 7 x 5
        public bool TradeBlocked  => RecentCombat || ParentSystem.HostileForcesPresent(Owner);

        public float OrbitalsMaintenance;

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

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null)
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
                owner.SpawnHomeWorld(this, ResourceManager.RandomPlanet(PlanetCategory.Terran));
                Name = system.Name + " " + RomanNumerals.ToRoman(1);
            }
            else
            {
                PlanetType chosenType = ChooseTypeByWeight(sunZone);
                InitNewMinorPlanet(chosenType);
            }

            float zoneBonus = ((int)sunZone + 1) * .2f * ((int)sunZone + 1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + 0.9f;
            scale += Type.Scale;

            float planetRadius = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            ObjectRadius = planetRadius;
            OrbitalRadius = ringRadius + planetRadius;
            Center = system.Position + MathExt.PointOnCircle(randomAngle, ringRadius);
            Scale = scale;
            PlanetTilt = RandomMath.RandomBetween(45f, 135f);

            GenerateMoons(this);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                HasRings = true;
                RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
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

        public float ColonyWorth(Empire toEmpire)
        {
            float worth = PopulationBillion + MaxPopulationBillion;
            if (toEmpire.NonCybernetic)
            {
                worth += (FoodHere / 50f) + (ProdHere / 50f);
                worth += Fertility*1.5f;
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
            if (toEmpire.data.EconomicPersonality.Name == "Expansionists")
                worth *= 1.35f;
            return worth;
        }

        public void SetInGroundCombat()
        {
            TroopManager.SetInCombat();
        }

        public float EmpireFertility(Empire empire) =>
            (empire.data?.Traits.Cybernetic ?? 0) > 0 ? MineralRichness : Fertility;

        public float EmpireBaseValue(Empire empire) => (
            Storage.CommoditiesCount +
            (1 + EmpireFertility(empire))
            * (1 + MineralRichness)
            * (float)Math.Ceiling(MaxPopulationBillion)
            );

        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        // added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);

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
            UpdateColonyValue();

            if (ParentSystem.HostileForcesPresent(Owner))
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

                    previousD = building.TheWeapon.Range + 1000f;
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
                    building.TheWeapon.Center = Center;
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
                Log.Warning($"Could not create defense ship, shipname = {selectedShip}");
            else
            {
                defenseShip.Level = 3;
                defenseShip.Velocity = UniverseRandom.RandomDirection() * defenseShip.Speed;
                empire.AddMoney(-defenseShip.GetCost(Owner));
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
            Owner.AddMoney(shipCost * shipHealthPercent);
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

        public void TerraformExternal(float amount)
        {
            AddMaxFertility(amount);
            if (amount > 0) ImprovePlanetType(MaxFertility);
            else            DegradePlanetType(MaxFertility);
        }

        public bool ImprovePlanetType(float value) // Refactored by Fat Bastard
        {
            var improve = new []
            {
                // Barren --> Desert --> Tundra --> Steppe --> Terran
                (AboveFertility:0.14f, ChangeFrom:PlanetCategory.Barren, Into:PlanetCategory.Desert),
                (AboveFertility:0.35f, ChangeFrom:PlanetCategory.Desert, Into:PlanetCategory.Tundra),
                (AboveFertility:0.60f, ChangeFrom:PlanetCategory.Tundra, Into:PlanetCategory.Steppe),
                (AboveFertility:0.95f, ChangeFrom:PlanetCategory.Steppe, Into:PlanetCategory.Terran),

                // Volcanic --> Ice --> Swamp --> Oceanic
                (AboveFertility:0.14f, ChangeFrom:PlanetCategory.Volcanic, Into:PlanetCategory.Ice),
                (AboveFertility:0.35f, ChangeFrom:PlanetCategory.Ice,      Into:PlanetCategory.Swamp),
                (AboveFertility:0.75f, ChangeFrom:PlanetCategory.Swamp,    Into:PlanetCategory.Oceanic),
            };
            foreach ((float aboveFertility, PlanetCategory from, PlanetCategory to) in improve)
            {
                if (value > aboveFertility && Category == from)
                {
                    Terraform(to);
                    return true;
                }
            }
            return false;
        }

        public bool DegradePlanetType(float value) // Added by Fat Bastard
        {
            var degrade = new []
            {
                // Terran --> Steppe --> Tundra --> Desert -> Barren
                (BelowFertility:0.90f, ChangeFrom:PlanetCategory.Terran, Into:PlanetCategory.Steppe),
                (BelowFertility:0.60f, ChangeFrom:PlanetCategory.Steppe, Into:PlanetCategory.Tundra),
                (BelowFertility:0.35f, ChangeFrom:PlanetCategory.Tundra, Into:PlanetCategory.Desert),
                (BelowFertility:0.14f, ChangeFrom:PlanetCategory.Desert, Into:PlanetCategory.Barren),

                // Oceanic --> Swamp --> Ice --> Volcanic
                (BelowFertility:0.75f, ChangeFrom:PlanetCategory.Oceanic, Into:PlanetCategory.Swamp),
                (BelowFertility:0.35f, ChangeFrom:PlanetCategory.Swamp,   Into:PlanetCategory.Ice),
                (BelowFertility:0.14f, ChangeFrom:PlanetCategory.Ice,     Into:PlanetCategory.Volcanic),
            };
            foreach ((float belowFertility, PlanetCategory from, PlanetCategory to) in degrade)
            {
                if (value < belowFertility && Category == from)
                {
                    Terraform(to);
                    if (MaxPopulation.AlmostZero())
                        WipeOutColony();

                    return true;
                }
            }
            return false;
        }

        private void DoTerraforming() // Added by Fat Bastard
        {
            if (TerraformToAdd.LessOrEqual(0))
                return;

            TerraformPoints        += TerraformToAdd; 
            AddMaxFertility(TerraformToAdd);
            MaxFertility            = MaxFertility.Clamped(0f, TerraformTargetFertility);
            bool improved           = ImprovePlanetType(TerraformPoints);
            if (TerraformPoints.AlmostEqual(1)) // scrap Terraformers - their job is done
            {
                foreach (PlanetGridSquare planetGridSquare in TilesList)
                {
                    if (planetGridSquare.building?.PlusTerraformPoints > 0)
                        planetGridSquare.building.ScrapBuilding(this);
                }
                UpdateTerraformPoints(0);
                if (Owner.isPlayer) // Notify player terraformers were scrapped.
                    Empire.Universe.NotificationManager.AddRandomEventNotification(
                        Name + " " + Localizer.Token(1971), Type.IconPath, "SnapToPlanet", this);
            }
            if (improved && Owner.isPlayer) // Notify player that planet was improved
                Empire.Universe.NotificationManager.AddRandomEventNotification(
                    Name + " " + Localizer.Token(1972), Type.IconPath, "SnapToPlanet", this);
        }

        public void UpdateTerraformPoints(float value)
        {
            TerraformPoints = value;
        }

        private void UpdateOrbitalsMaint()
        {
            OrbitalsMaintenance = 0;
            foreach (Ship orbital in OrbitalStations.Values)
            {
                OrbitalsMaintenance += orbital.GetMaintCost(Owner);
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            CrippledTurns = Math.Max(0, CrippledTurns - 1);
            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentLevel();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            DoTerraforming();
            RemoveInvalidFreighters(IncomingFreighters);
            RemoveInvalidFreighters(OutgoingFreighters);
            UpdateFertility();
            InitResources(); // must be done before Governing
            UpdateIncomes(false);
            UpdateOrbitalsMaint();
            DoGoverning();
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

        private void UpdateColonyValue()
        {
            ColonyValue = 0;
            if (Owner == null)
                return;

            ColonyValue  = BuildingList.Any(b => b.IsCapital) ? 100 : 0;
            ColonyValue += BuildingList.Sum(b => b.ActualCost) / 10;
            ColonyValue += (PopulationBillion + MaxPopulationBillion) * 5;
            ColonyValue += IsCybernetic ? MineralRichness * 20 : MineralRichness * 10 + Fertility * 10;
        }

        // these are intentionally duplicated so we don't easily modify them...
        float MaxPopBaseVal, MaxPopVal, MaxPopBillionVal;
        public float MaxPopBase // planetary base max population value
        {
            get => MaxPopBaseVal;
            set
            {
                MaxPopBaseVal = value;
                UpdateMaxPopulation();
            }
        }
        public float MaxPopulation => MaxPopVal; // max pop with building bonuses
        public float MaxPopulationBillion => MaxPopBillionVal;

        protected void UpdateMaxPopulation()
        {
            float popBonus = 0f;
            for (int i = 0; i < BuildingList.Count; ++i) // for speed
                popBonus += BuildingList[i].MaxPopIncrease;

            MaxPopVal = MaxPopBase + popBonus;
            MaxPopBillionVal = MaxPopulation / 1000f;
        }

        public int Level { get; private set; }
        public string DevelopmentStatus { get; private set; } = "Undeveloped";

        public void UpdateDevelopmentLevel()
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

        void UpdateFertility()
        {
            if (Fertility.AlmostEqual(MaxFertility))
                return;

            if (Fertility < MaxFertility)
                Fertility = (Fertility + 0.01f).Clamped(0, MaxFertility); // FB - Slowly increase fertility to max fertility
            else if (Fertility > MaxFertility)
                Fertility = Fertility.Clamped(0, Fertility - 0.01f); // FB - Slowly decrease fertility to max fertility
        }

        public void SetFertility(float fertility, float maxFertility)
        {
            MaxFertility = maxFertility;
            Fertility = fertility;
        }
        
        public void SetFertilityMinMax(float fertility) => SetFertility(fertility, fertility);

        public void AddMaxFertility(float amount)
        {
            MaxFertility += amount;
            MaxFertility  = Math.Max(0, MaxFertility);
        }

        // FB: to enable bombs to temp change fertility immediately by specified amount
        public void AddFertility(float amount)
        {
            Fertility += amount;
            Fertility  = Math.Max(0, Fertility);
        }

        public void UpdateIncomes(bool loadUniverse) // FB: note that this can be called multiple times in a turn
        {
            if (Owner == null)
                return;

            bool shipyard = false;
            AllowInfantry = false;
            RepairPerTurn        = 0;
            TerraformToAdd       = 0;
            ShieldStrengthMax    = 0;
            ShipBuildingModifier = 0;
            NumShipyards         = 0;
            TotalDefensiveStrength     = 0;
            PlusFlatPopulationPerTurn  = 0;
            float totalStorage         = 0;
            float shipBuildingModifier = 1;

            if (!loadUniverse)
            {
                var deadShipyards = new Array<Guid>();
                float shipyards   = 1;
                foreach (KeyValuePair<Guid, Ship> keyValuePair in OrbitalStations)
                {
                    if (keyValuePair.Value == null)
                        deadShipyards.Add(keyValuePair.Key);

                    else if (keyValuePair.Value.Active && keyValuePair.Value.shipData.IsShipyard)
                    {
                        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.ShipyardBonus > 0)
                            shipBuildingModifier *= (1 - (GlobalStats.ActiveModInfo.ShipyardBonus / shipyards)); //+= GlobalStats.ActiveModInfo.ShipyardBonus;
                        else
                            shipBuildingModifier *= (1-(.25f/shipyards));

                        shipyards += 0.2f;
                        NumShipyards++;
                    }
                    else if (!keyValuePair.Value.Active)
                        deadShipyards.Add(keyValuePair.Key);
                }
                foreach (Guid key in deadShipyards)
                    OrbitalStations.Remove(key);
                ShipBuildingModifier = shipBuildingModifier;
            }

            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building b                 = BuildingList[i];
                PlusFlatPopulationPerTurn += b.PlusFlatPopulation;
                ShieldStrengthMax         += b.PlanetaryShieldStrengthAdded;
                TerraformToAdd            += b.PlusTerraformPoints;
                totalStorage              += b.StorageAdded;
                RepairPerTurn             += b.ShipRepair;
                if (b.AllowInfantry)
                    AllowInfantry = true;
                if (b.WinsGame)
                    HasWinBuilding = true;
                if (b.AllowShipBuilding || b.IsSpacePort)
                    shipyard = true;
            }

            UpdateMaxPopulation();
            TotalDefensiveStrength = (int)TroopManager.GroundStrength(Owner);

            // Added by Gretman -- This will keep a planet from still having shields even after the shield building has been scrapped.
            ShieldStrengthCurrent = ShieldStrengthCurrent.Clamped(0,ShieldStrengthMax);
            HasSpacePort          = shipyard && (colonyType != ColonyType.Research || Owner.isPlayer);

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

        private void UpdateHomeDefenseHangars(Building b)
        {
            if (ParentSystem.HostileForcesPresent(Owner) || b.CurrentNumDefenseShips == b.DefenseShipsCapacity)
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

        private void GrowPopulation()
        {
            if (Owner == null) return;

            float balanceGrowth = (1 - PopulationRatio).Clamped(0.1f, 1f);
            float repRate       = Owner.data.BaseReproductiveRate * Population * balanceGrowth;
            if (Owner.data.Traits.PopGrowthMax.NotZero())
                repRate = Math.Min(repRate, Owner.data.Traits.PopGrowthMax * 1000f);

            repRate  = Math.Max(repRate, Owner.data.Traits.PopGrowthMin * 1000f);
            repRate += PlusFlatPopulationPerTurn;
            repRate += repRate * Owner.data.Traits.ReproductionMod;

            if (IsStarving)
                Population += Unfed * 10f; // <-- This reduces population depending on starvation severity.

            else if (!ShortOnFood())
                Population += repRate;

            Population = Population.Clamped(100f, MaxPopulation);
        }

        public void WipeOutColony()
        {
            Population = 0f;
            if (Owner == null)
                return;

            Owner.RemovePlanet(this);
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

        public int TotalInvadeInjure   => BuildingList.Sum(b => b.InvadeInjurePoints);
        public float TotalSpaceOffense => BuildingList.Sum(b => b.Offense) + OrbitalStations.Values.Sum(o => o.BaseStrength);
        public int MaxDefenseShips     => BuildingList.Sum(b => b.DefenseShipsCapacity);
        public int CurrentDefenseShips => BuildingList.Sum(b => b.CurrentNumDefenseShips) + ParentSystem.ShipList.Count(s => s.HomePlanet == this);

        public int OpenTiles           => TilesList.Count(tile => tile.Habitable && tile.building == null);
        public int TotalBuildings      => TilesList.Count(tile => tile.building != null && !tile.building.IsBiospheres);
        public float BuiltCoverage     => TotalBuildings / (float)MaxBuildings;

        public int ExistingMilitaryBuildings  => BuildingList.Count(b => b.IsMilitary);
        public float TerraformTargetFertility => BuildingList.Sum(b => b.MaxFertilityOnBuild) + 1;
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
                return (int)Math.Floor(militaryCoverage * sizeFactor * MaxBuildings);
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
            debug.AddLine($"{ParentSystem.Name} : {Name}", Color.Green);
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

