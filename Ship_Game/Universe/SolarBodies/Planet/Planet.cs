using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Universe.SolarBodies.AI;
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
        public SpaceStation Station = new SpaceStation();

        public bool GovBuildings = true;
        public bool GovSliders = true;
        public bool AllowInfantry;

        public int CrippledTurns;
        public int TotalDefensiveStrength { get; private set; }
        
        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public float Consumption { get; private set; } // Food (NonCybernetic) or Production (IsCybernetic)
        float Unfed;
        public bool IsStarving => Unfed < 0f;

        public bool CorsairPresence;
        public bool QueueEmptySent = true;
        public float RepairPerTurn;

        public bool RecentCombat => TroopManager.RecentCombat;
        public int CountEmpireTroops(Empire us) => TroopManager.CountEmpireTroops(us);
        public int GetDefendingTroopCount() => TroopManager.GetDefendingTroopCount();
        public bool AnyOfOurTroops(Empire us) => TroopManager.AnyOfOurTroops(us);
        public float GetGroundStrength(Empire empire) => TroopManager.GetGroundStrength(empire);
        public int GetPotentialGroundTroops() => TroopManager.GetPotentialGroundTroops();
        public float GetGroundStrengthOther(Empire AllButThisEmpire) => TroopManager.GetGroundStrengthOther(AllButThisEmpire);
        public bool TroopsHereAreEnemies(Empire empire) => TroopManager.TroopsHereAreEnemies(empire);
        public int GetGroundLandingSpots() => TroopManager.GetGroundLandingSpots();
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.GetEmpireTroops(empire, maxToTake);
        public float AvgPopulationGrowth { get; private set; }
        
        static string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions
        
        public bool IsCybernetic  => Owner != null && Owner.IsCybernetic;
        public bool NonCybernetic => Owner != null && Owner.NonCybernetic;

        void CreateManagers()
        {
            TroopManager = new TroopManager(this);
            GeodeticManager = new GeodeticManager(this);
            Storage = new ColonyStorage(this);
            SbProduction = new SBProduction(this);

            Food = new ColonyFood(this) { Percent = 0.34f };
            Prod = new ColonyProduction(this) { Percent = 0.33f };
            Res  = new ColonyResearch(this)  { Percent = 0.33f };
            Money = new ColonyMoney(this) { Percent = 1f };
        }

        public Planet()
        {
            CreateManagers();

            HasShipyard = false;
            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                AddGood(keyValuePair.Key, 0);
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
                PlanetType = RandomMath.IntBetween(0, 1) == 0 ? 27 : 29;
                owner.SpawnHomePlanet(this);
                Name = ParentSystem.Name + " " + NumberToRomanConvertor.NumberToRoman(1);
            }
            else
            {
                GenerateType(sunZone);
                SetPlanetAttributes(true);
            }

            float zoneBonus = ((int)sunZone + 1) * .2f * ((int)sunZone + 1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + .9f;
            if (PlanetType == 2 || PlanetType == 6 || PlanetType == 10 ||
                PlanetType == 12 || PlanetType == 15 || PlanetType == 20 ||
                PlanetType == 26)
                scale += 2.5f;

            float planetRadius = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            ObjectRadius = planetRadius;
            OrbitalRadius = ringRadius + planetRadius;
            Center = MathExt.PointOnCircle(randomAngle, ringRadius);
            Scale = scale;
            PlanetTilt = RandomMath.RandomBetween(45f, 135f);

            GenerateMoons(this);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                HasRings = true;
                RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
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
                worth += b.Cost / 50f;
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

        //added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);

        public void Update(float elapsedTime)
        {
            if (Shipyards.Count != 0)
            {
                Guid[] keys = Shipyards.Keys.ToArray();
                for (int x = 0; x < keys.Length; x++)
                {
                    Guid key = keys[x];
                    Ship shipyard = Shipyards[key];
                    if (shipyard == null || !shipyard.Active
                                         || shipyard.SurfaceArea == 0)
                        Shipyards.Remove(key);
                }
            }
            if (!Habitable)
            {
                UpdatePosition(elapsedTime);
                return;
            }
            TroopManager.Update(elapsedTime);
            GeodeticManager.Update(elapsedTime);

            UpdateSpaceCombatBuildings(elapsedTime);
            UpdatePlanetaryProjectiles(elapsedTime);

            UpdatePosition(elapsedTime);
        }

        private void UpdateSpaceCombatBuildings(float elapsedTime)
        {
            if (Owner == null) return;
            if (ParentSystem.ShipList.Count <= 0) return; 
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
                else continue;


                Ship target = null;
                Ship troop = null;
                bool defenseShipNameStillOut =  false;
                for (int j = 0; j < ParentSystem.ShipList.Count; ++j)
                {
                    Ship ship = ParentSystem.ShipList[j];
                    if (ship.loyalty == Owner)
                    {
                        defenseShipNameStillOut = ship.HomePlanet == this;
                        continue;
                    }
                    if (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact)
                        continue;
                    float currentD = Vector2.Distance(Center, ship.Center);
                    if (ship.shipData.Role == ShipData.RoleName.troop && currentD < previousT)
                    {
                        previousT = currentD;
                        troop = ship;
                        continue;
                    }
                    if (currentD < previousD && troop == null)
                    {
                        previousD = currentD;
                        target = ship;
                    }
                }

                if (troop != null)
                    target = troop;
                if (target == null)
                {
                    if (!defenseShipNameStillOut &&
                        building.CurrentNumDefenseShips < building.DefenseShipsCapacity)
                        building.UpdateCurrentDefenseShips(building.DefenseShipsCapacity);
                    
                    continue;
                }
                if (building.isWeapon)
                {
                    building.TheWeapon.Center = Center;
                    building.TheWeapon.FireFromPlanet(this, target);
                    building.WeaponTimer = building.TheWeapon.fireDelay;
                }
                else if (building.CurrentNumDefenseShips > 0)
                {
                    LaunchDefenseShips(building.DefenseShipsRole, Owner);
                    building.UpdateCurrentDefenseShips(-1);
                }
                break;
            }
        }

        private void LaunchDefenseShips(string roleName, Empire empire)
        {
            string defaultShip         = empire.data.StartingShip;
            ShipData.RoleName shipRole = (ShipData.RoleName)Enum.Parse(typeof(ShipData.RoleName), roleName);
            string selectedShip        = GetDefenseShipName(shipRole, empire) ?? defaultShip;
            Ship defenseShip           = Ship.CreateDefenseShip(selectedShip, empire, Center, this);
            if (defenseShip == null)
                Log.Warning($"Could not create defense ship, shipname = {selectedShip}");
            else
            {
                empire.AddMoney(-defenseShip.BaseCost);
                defenseShip.Velocity = UniverseRandom.RandomDirection() * defenseShip.Speed;
            }
        }

        private static string GetDefenseShipName(ShipData.RoleName roleName, Empire empire)
        {
            return ShipBuilder.PickFromCandidates(roleName, empire);
        }

        public void LandDefenseShip(ShipData.RoleName roleName, float shipCost, float shipHealthPercent)
        {
            string shipRole = roleName.ToString();
            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building building = BuildingList[i];
                if (building.DefenseShipsRole == shipRole 
                    && building.CurrentNumDefenseShips < building.DefenseShipsCapacity)
                {
                    building.UpdateCurrentDefenseShips(1);
                }
            }
            Owner.AddMoney(shipCost * shipHealthPercent);
        }

        void UpdatePlanetaryProjectiles(float elapsedTime)
        {
            for (int index = Projectiles.Count - 1; index >= 0; --index)
            {
                Projectile projectile = Projectiles[index];
                if (projectile.Active)
                {
                    if (elapsedTime > 0)
                        projectile.Update(elapsedTime);
                }
                else
                    Projectiles.RemoveAtSwapLast(index);
            }
        }

        public void TerraformExternal(float amount)
        {
            ChangeMaxFertility(amount);
            if (amount > 0)
                ImprovePlanetType();
            else
                DegradePlanetType();
        }

        public void ImprovePlanetType() // Refactored by Fat Bastard
        {
            // Barren  --> Desert --> Steppe --> Tundra --> Terran
            // Vocanic --> Ice    --> Swamp  --> Oceanic
            switch (Category)
            {
                case PlanetCategory.Barren when MaxFertility > 0.14:
                    PlanetType = 14; // desert
                    Terraform();
                    break;
                case PlanetCategory.Volcanic when MaxFertility > 0.14:
                    PlanetType = 17; // desert
                    Terraform();
                    break;
                case PlanetCategory.Desert when MaxFertility > 0.35:
                    PlanetType = 18; // steppe
                    Terraform();
                    break;
                case PlanetCategory.Ice when MaxFertility > 0.35:
                    PlanetType = 19; // swamp
                    Terraform();
                    break;
                case PlanetCategory.Swamp when MaxFertility > 0.75:
                    PlanetType = 21; // oceanic
                    Terraform();
                    break;
                case PlanetCategory.Steppe when MaxFertility > 0.6:
                    PlanetType = 11; // tundra
                    Terraform();
                    break;
                case PlanetCategory.Tundra when MaxFertility > 0.95:
                    PlanetType = 22; // terran
                    Terraform();
                    break;
            }
            MaxFertility = Math.Max(0, MaxFertility);
        }

        public void DegradePlanetType() // Added by Fat Bastard
        {
            // Terran  --> Desert --> Barren or Volcanic
            // Oceanic --> Ice    --> Barren or Volcanic
            // Swamp   --> Ice    --> Barren or Volcanic
            // Steppe  --> Desert --> Barren or Volcanic
            // Tundra  --> Desert --> Barren or Volcanic
            switch (Category)
            {
                case PlanetCategory.Terran when MaxFertility < 0.5:
                    PlanetType = 14; // desert
                    Terraform(recalculateTileHabitation: true);
                    break;
                case PlanetCategory.Oceanic when MaxFertility < 0.5:
                    PlanetType = 17; // ice
                    Terraform(recalculateTileHabitation: true);
                    break;
                case PlanetCategory.Swamp when MaxFertility < 0.2:
                    PlanetType = 17; // ice
                    Terraform(recalculateTileHabitation: true);
                    break;
                case PlanetCategory.Steppe when MaxFertility < 0.5:
                    PlanetType = 14; // desert
                    Terraform(recalculateTileHabitation: true);
                    break;
                case PlanetCategory.Tundra when MaxFertility < 0.5:
                    PlanetType = 14; // desert
                    Terraform(recalculateTileHabitation: true);
                    break;
                case PlanetCategory.Desert when MaxFertility < 0.1:
                case PlanetCategory.Ice when MaxFertility < 0.1:
                    PlanetType = RandomMath.IntBetween(1, 10) > 5 ? 9 : 7; // volcanic or desert
                    Terraform(recalculateTileHabitation: true);
                    break;
            }
            MaxFertility = Math.Max(0, MaxFertility);
        }

        void DoTerraforming() // Added by Fat Bastard
        {
            TerraformPoints += TerraformToAdd;
            if (TerraformPoints > 0.0f && Fertility < 1f)
            {
                ChangeMaxFertility(TerraformToAdd);
                MaxFertility = MaxFertility.Clamped(0f, 1f);
                ImprovePlanetType();
                if (MaxFertility.AlmostEqual(1f)) // remove Terraformers - their job is done
                    foreach (PlanetGridSquare planetGridSquare in TilesList)
                    {
                        if (planetGridSquare.building?.PlusTerraformPoints > 0)
                            planetGridSquare.building.ScrapBuilding(this);
                    }
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            if (CrippledTurns > 0) CrippledTurns--;
            else CrippledTurns = 0;

            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentLevel();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            DoTerraforming();
            UpdateFertility();
            DoGoverning();
            UpdateIncomes(false);

            // notification about empty queue
            if (GlobalStats.ExtraNotifications && Owner != null && Owner.isPlayer)
            {
                if (ConstructionQueue.Count == 0 && !QueueEmptySent)
                {
                    if (colonyType == ColonyType.Colony)
                    {
                        QueueEmptySent = true;
                        Empire.Universe.NotificationManager.AddEmptyQueueNotification(this);
                    }
                }
                else if (ConstructionQueue.Count > 0)
                {
                    QueueEmptySent = false;
                }
            }

            if (ShieldStrengthCurrent < ShieldStrengthMax)
            {
                if (!RecentCombat)
                {
                    if (ShieldStrengthCurrent > ShieldStrengthMax / 10)
                        ShieldStrengthCurrent += ShieldStrengthMax / 10;
                    else
                        ++ShieldStrengthCurrent;
                }
                if (ShieldStrengthCurrent > ShieldStrengthMax)
                    ShieldStrengthCurrent = ShieldStrengthMax;
            }

            //this.UpdateTimer = 10f;
            HarvestResources();
            ApplyProductionTowardsConstruction();
            GrowPopulation();
            TroopManager.HealTroops(2);
            RepairBuildings(1);

            CalculateIncomingTrade();
        }

        public float MaxPopBase { get; set; } // planetary base max population value
        public float MaxPopulation { get; private set; } // max pop with building bonuses
        public float MaxPopulationBillion { get; private set; }

        protected void UpdateMaxPopulation()
        {
            float popBonus = 0f;
            for (int i = 0; i < BuildingList.Count; ++i) // for speed
                popBonus += BuildingList[i].MaxPopIncrease;

            MaxPopulation = MaxPopBase + popBonus;
            MaxPopulationBillion = MaxPopulation / 1000f;
        }

        public int Level { get; private set; }
        public string DevelopmentStatus { get; private set; } = "Undeveloped";

        public void UpdateDevelopmentLevel()
        {
            if (PopulationBillion <= 0.5f)
            {
                Level = (int)DevelopmentLevel.Solitary;
                DevelopmentStatus = Localizer.Token(1763);
                if      (MaxPopulationBillion >= 2f  && Category != PlanetCategory.Barren) DevelopmentStatus += Localizer.Token(1764);
                else if (MaxPopulationBillion >= 2f  && Category == PlanetCategory.Barren) DevelopmentStatus += Localizer.Token(1765);
                else if (MaxPopulationBillion < 0.0f && Category != PlanetCategory.Barren) DevelopmentStatus += Localizer.Token(1766);
                else if (MaxPopulationBillion < 0.5f && Category == PlanetCategory.Barren) DevelopmentStatus += Localizer.Token(1767);
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

            if (Prod.NetIncome >= 10.0 && HasShipyard)
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
                Fertility += 0.01f;
            else
                Fertility -= 0.01f;

            Fertility = Fertility.Clamped(0, MaxFertility);
        }

        public void ChangeMaxFertility(float amount)
        {
            MaxFertility += amount;
            MaxFertility  = Math.Max(0, MaxFertility);
        }

        public void ChangeFertility(float amount) // FB: to enable bombs to temp change ferility immediately by specified amount
        {
            Fertility += amount;
            Fertility  = Math.Max(0, Fertility);
        }

        public void InitFertility(float amount)
        {
            Fertility = amount;
        }

        public void InitMaxFertility(float amount)
        {
            MaxFertility = amount;
        }

        public void InitFertilityValues(float amount)
        {
            InitFertility(amount);
            InitMaxFertility(amount);
        }

        public void UpdateIncomes(bool loadUniverse)
        {
            if (Owner == null)
                return;

            AllowInfantry = false;
            ShieldStrengthMax = 0f;
            TotalDefensiveStrength = 0;
            TotalMaintenanceCostsPerTurn = 0f;
            PlusFlatPopulationPerTurn = 0f;
            ShipBuildingModifier = 0f;

            float shipBuildingModifier = 1f;

            if (!loadUniverse)
            {
                var deadShipyards = new Array<Guid>();
                float shipyards = 1;
                foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
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
                    }
                    else if (!keyValuePair.Value.Active)
                        deadShipyards.Add(keyValuePair.Key);
                }
                foreach (Guid key in deadShipyards)
                    Shipyards.Remove(key);
                ShipBuildingModifier = shipBuildingModifier;
            }

            TerraformToAdd = 0f;
            RepairPerTurn = 0;
            float totalStorage = 0;
            bool shipyard = false;

            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building b = BuildingList[i];
                if (b.WinsGame)
                    HasWinBuilding = true;
                if (b.AllowShipBuilding || b.IsSpacePort)
                    shipyard = true;

                PlusFlatPopulationPerTurn += b.PlusFlatPopulation;
                ShieldStrengthMax += b.PlanetaryShieldStrengthAdded;
                TerraformToAdd += b.PlusTerraformPoints;

                if (b.AllowInfantry)
                    AllowInfantry = true;
                totalStorage += b.StorageAdded;
                TotalMaintenanceCostsPerTurn += b.Maintenance;
                RepairPerTurn += b.ShipRepair;
            }

            UpdateMaxPopulation();

            TotalDefensiveStrength = (int)TroopManager.GetGroundStrength(Owner);

            // Added by Gretman -- This will keep a planet from still having shields even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            HasShipyard = shipyard && (colonyType != ColonyType.Research || Owner.isPlayer);

            // greedy bastards
            Consumption = (PopulationBillion + Owner.data.Traits.ConsumptionModifier * PopulationBillion);
            Food.Update(NonCybernetic ? Consumption : 0f);
            Prod.Update(IsCybernetic ? Consumption : 0f);
            Res.Update(0f);
            Money.Update(GrossUpkeep);

            if (Station != null && !loadUniverse)
            {
                Station.SetVisibility(HasShipyard, Empire.Universe.ScreenManager, this);
            }

            Storage.Max = totalStorage.Clamped(10f, 10000000f);
        }

        void HarvestResources()
        {
            // produced food is already consumed by denizens during resource update
            // if we have shortage, then NetIncome will be negative
            Unfed = IsCybernetic ? Prod.NetIncome : Food.NetIncome;
            if (Unfed > 0f) Unfed = 0f;

            FoodHere += Food.NetIncome;
            ProdHere += Prod.NetIncome;

            // now if food income was < 0, we will have to get some from Storage:
            if (Unfed < 0)
            {
                float needed = -Unfed;
                if (Storage.RaceFood >= needed)
                {
                    Storage.RaceFood -= needed;
                    Unfed = 0;
                }
                else // consume everything (greedy bastards!)
                {
                    Unfed += Storage.RaceFood;
                    Storage.RaceFood = 0;
                }
            }
            Storage.BuildingResources();
        }


        private void GrowPopulation()
        {
            if (Owner == null) return;

            float repRate = Owner.data.BaseReproductiveRate * Population;
            if (repRate > Owner.data.Traits.PopGrowthMax * 1000 && !Owner.data.Traits.PopGrowthMax.AlmostZero())
                repRate = Owner.data.Traits.PopGrowthMax * 1000f;
            if (repRate < Owner.data.Traits.PopGrowthMin * 1000 )
                repRate = Owner.data.Traits.PopGrowthMin * 1000f;
            repRate += PlusFlatPopulationPerTurn;

            float adjustedRepRate = repRate + Owner.data.Traits.ReproductionMod * repRate;
            if (IsStarving)
                Population += Unfed * 10f; // <-- This reduces population due to starvation.
            else
                Population += adjustedRepRate;

            Population = Population.Clamped(100f, MaxPopulation);
            AvgPopulationGrowth = (AvgPopulationGrowth + adjustedRepRate) / 2;
        }


        public bool EventsOnBuildings()
        {
            bool events = false;
            foreach (Building building in BuildingList)
            {
                if (building.EventTriggerUID != null && !building.EventWasTriggered)
                {
                    events = true;
                    break;
                }
            }
            return events;
        }

        public int TotalInvadeInjure   => BuildingList.Filter(b => b.InvadeInjurePoints > 0).Sum(b => b.InvadeInjurePoints);
        public float TotalSpaceOffense => BuildingList.Filter(b => b.isWeapon).Sum(b => b.Offense);

        private void RepairBuildings(int repairAmount)
        {
            if (RecentCombat)
                return;

            for (int i = 0; i < BuildingList.Count; ++i)
            {
                Building building        = BuildingList[i];
                Building template        = ResourceManager.GetBuildingTemplate(BuildingList[i].Name);
                building.CombatStrength  = (building.CombatStrength + repairAmount).Clamped(0, template.CombatStrength);
                building.Strength        = (building.Strength + repairAmount).Clamped(0, template.Strength);
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Planet() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            ActiveCombats?.Dispose(ref ActiveCombats);
            OrbitalDropList?.Dispose(ref OrbitalDropList);
            SbProduction    = null;
            Storage   = null;
            TroopManager    = null;
            GeodeticManager = null;
            BasedShips?.Dispose(ref BasedShips);
            Projectiles?.Dispose(ref Projectiles);
            TroopsHere?.Dispose(ref TroopsHere);
        }

        //Debug Text
        public Array<DebugTextBlock> DebugPlanetInfo()
        {
            var incomingData = new DebugTextBlock();
            var blocks = new Array<DebugTextBlock>();
            var lines = new Array<string>();
            var totals = DebugSummarizePlanetStats(lines);
            string food = $"{(int)FoodHere}(%{100*Storage.FoodRatio:00.0}) {FS}";
            string prod = $"{(int)ProdHere}(%{100*Storage.ProdRatio:00.0}) {PS}";

            incomingData.AddLine($"{ParentSystem.Name} : {Name} : IN Cargo: {totals.Total}", Color.Yellow);
            incomingData.AddLine($"FoodHere: {food} IN: {totals.Food}", Color.White);
            incomingData.AddLine($"ProdHere: {prod} IN: {totals.Prod}");
            incomingData.AddLine($"IN Colonists: {totals.Colonists}");
            incomingData.AddLine($"");
            blocks.Add(incomingData);
            return blocks;
        }
        public TradeAI.DebugSummaryTotal DebugSummarizePlanetStats(Array<string> lines)
        {
            lines.Add($"Money: {Money.NetIncome}");
            lines.Add($"Eats: {Consumption}");
            lines.Add($"FoodWkrs: {Food.Percent}");
            lines.Add($"ProdWkrs: {Prod.Percent}  ");
            return new TradeAI.DebugSummaryTotal();
        }
    }
}

