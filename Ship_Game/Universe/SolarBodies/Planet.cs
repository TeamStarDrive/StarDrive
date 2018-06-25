using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{

    public sealed class Planet : SolarSystemBody, IDisposable
    {
        public enum ColonyType
        {
            Core,
            Colony,
            Industrial,
            Research,
            Agricultural,
            Military,
            TradeHub,
        }
        public GeodeticManager GeodeticManager;
        public SBCommodities SbCommodities;

        public TroopManager TroopManager;
        public bool GovBuildings = true;
        public bool GovSliders = true;
        public float ProductionHere
        {
            get => SbCommodities.ProductionHere;
            set => SbCommodities.ProductionHere = value;
        }

        public float FoodHere
        {
            get => SbCommodities.FoodHere;
            set => SbCommodities.FoodHere = value;
        }
        public float Population
        {
            get => SbCommodities.Population;
            set => SbCommodities.Population = value;
        }


        public GoodState FS = GoodState.STORE;
        public GoodState PS = GoodState.STORE;
        public GoodState GetGoodState(string good)
        {
            switch (good)
            {
                case "Food":
                    return FS;
                case "Production":
                    return PS;
            }
            return 0;
        }        
        public SpaceStation Station = new SpaceStation();        
        
        
        public float FarmerPercentage = 0.34f;
        public float WorkerPercentage = 0.33f;
        public float ResearcherPercentage = 0.33f;        
        public float MaxStorage = 10f;                
        public bool FoodLocked;
        public bool ProdLocked;
        public bool ResLocked;
        
        public int CrippledTurns;
   
        //public bool isSelected;
        public float BuildingRoomUsed;
        
        
        public float NetFoodPerTurn;
        public float GetNetGoodProd(string good)
        {
            switch (good)
            {
                case "Food":
                    return NetFoodPerTurn;
                case "Production":
                    return NetProductionPerTurn;
            }
            return 0;
        }
        public float GetMaxGoodProd(string good)
        {
            switch (good)
            {
                case "Food":
                    return NetFoodPerTurn;
                case "Production":
                    return MaxProductionPerTurn;
            }
            return 0;
        }
        //public float FoodPercentAdded;  //This variable is never used... -Gretman
        public float FlatFoodAdded;
        public float NetProductionPerTurn;
        private float MaxProductionPerTurn;
        public float GrossProductionPerTurn;
        public float PlusFlatProductionPerTurn;
        public float NetResearchPerTurn;
        public float PlusTaxPercentage;
        public float PlusFlatResearchPerTurn;
        //public float ResearchPercentAdded;      //This is never used
        public float PlusResearchPerColonist;
        public float TotalMaintenanceCostsPerTurn;
        public float PlusFlatMoneyPerTurn;
        private float PlusFoodPerColonist;
        public float PlusProductionPerColonist;
        public float MaxPopBonus;
        public bool AllowInfantry;
        public float PlusFlatPopulationPerTurn;
        public int TotalDefensiveStrength;
        public float GrossFood;
        public float GrossMoneyPT;
        public float GrossIncome =>
                    (this.GrossMoneyPT + this.GrossMoneyPT * (float)this.Owner?.data.Traits.TaxMod) * (float)this.Owner?.data.TaxRate
                    + this.PlusFlatMoneyPerTurn + (this.Population / 1000f * this.PlusCreditsPerColonist);
        public float GrossUpkeep =>
                    (float)((double)this.TotalMaintenanceCostsPerTurn + (double)this.TotalMaintenanceCostsPerTurn
                    * (double)this.Owner?.data.Traits.MaintMod);
        public float NetIncome => this.GrossIncome - this.GrossUpkeep;
        public float PlusCreditsPerColonist;
        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public float Consumption;
        private float Unfed;
        
        public float GetGoodHere(string good)
        {
            switch (good)
            {
                case "Food":
                    return FoodHere;
                case "Production":
                    return ProductionHere;
            }
            return 0;
        }
        public Array<string> CommoditiesPresent => SbCommodities.CommoditiesPresent;
        public bool CorsairPresence;
        public bool QueueEmptySent = true;
        public float RepairPerTurn = 0;        
        public bool PSexport { get; private set; }

        public float ExportPSWeight =0;
        public float ExportFSWeight = 0;


        public float TradeIncomingColonists = 0;

        public bool RecentCombat                                          => TroopManager.RecentCombat;
        public float GetDefendingTroopStrength()                          => TroopManager.GetDefendingTroopStrength();
        public int CountEmpireTroops(Empire us)                           => TroopManager.CountEmpireTroops(us);
        public int GetDefendingTroopCount()                               => TroopManager.GetDefendingTroopCount();
        public bool AnyOfOurTroops(Empire us)                             => TroopManager.AnyOfOurTroops(us);
        public float GetGroundStrength(Empire empire)                     => TroopManager.GetGroundStrength(empire);
        public int GetPotentialGroundTroops()                             => TroopManager.GetPotentialGroundTroops();
        public float GetGroundStrengthOther(Empire AllButThisEmpire)      => TroopManager.GetGroundStrengthOther(AllButThisEmpire);
        public bool TroopsHereAreEnemies(Empire empire)                   => TroopManager.TroopsHereAreEnemies(empire);
        public int GetGroundLandingSpots()                                => TroopManager.GetGroundLandingSpots();
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.GetEmpireTroops(empire, maxToTake);
        public void HealTroops()                                          => TroopManager.HealTroops();

        public void SetExportWeight(string goodType, float weight)
        {
            switch (goodType)
            {
                case "Food":
                    ExportFSWeight = weight;
                    break;
                case "Production":
                    ExportPSWeight = weight;
                    break;

            }   
        }

        public float GetExportWeight(string goodType)
        {
            switch (goodType)
            {
                case "Food":
                    return ExportFSWeight;
                case "Production":
                    return ExportPSWeight;                    
            }
            return 0;
        }

        public Planet()
        {
            TroopManager = new TroopManager(this, Habitable);
            GeodeticManager = new GeodeticManager(this);
            SbCommodities = new SBCommodities(this);
            base.SbProduction = new SBProduction(this);
            HasShipyard = false;

            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                AddGood(keyValuePair.Key, 0);
        }

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null)
        {                        
            var newOrbital = this;
            TroopManager = new TroopManager(this, Habitable);
            GeodeticManager = new GeodeticManager(this);
            SbCommodities = new SBCommodities(this);
            SbProduction = new SBProduction(this);
            Name = name;
            OrbitalAngle = randomAngle;
            ParentSystem = system;
                
            
            SunZone sunZone;
            float zoneSize = ringMax;
            if (ringRadius < zoneSize * .15f)
                sunZone = SunZone.Near;
            else if (ringRadius < zoneSize * .25f)
                sunZone = SunZone.Habital;
            else if (ringRadius < zoneSize * .7f)
                sunZone = SunZone.Far;
            else
                sunZone = SunZone.VeryFar;
            if (owner != null && owner.Capital == null && sunZone >= SunZone.Habital)
            {
                PlanetType = RandomMath.IntBetween(0, 1) == 0 ? 27 : 29;
                owner.SpawnHomePlanet(newOrbital);
                Name = ParentSystem.Name + " " + NumberToRomanConvertor.NumberToRoman(1);
            }
            else            
            {
                GenerateType(sunZone);
                newOrbital.SetPlanetAttributes(true);
            }
            
            float zoneBonus = ((int)sunZone +1) * .2f * ((int)sunZone +1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + .9f;
            if (newOrbital.PlanetType == 2 || newOrbital.PlanetType == 6 || newOrbital.PlanetType == 10 ||
                newOrbital.PlanetType == 12 || newOrbital.PlanetType == 15 || newOrbital.PlanetType == 20 ||
                newOrbital.PlanetType == 26)
                scale += 2.5f;

            float planetRadius       = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            newOrbital.ObjectRadius  = planetRadius;
            newOrbital.OrbitalRadius = ringRadius + planetRadius;
            Vector2 planetCenter     = MathExt.PointOnCircle(randomAngle, ringRadius);
            newOrbital.Center        = planetCenter;
            newOrbital.Scale         = scale;            
            newOrbital.PlanetTilt    = RandomMath.RandomBetween(45f, 135f);


            GenerateMoons(newOrbital);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                newOrbital.HasRings = true;
                newOrbital.RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
          
        }

        public void SetInGroundCombat()
        {
            TroopManager.SetInCombat();
        }

        public Goods ImportPriority()
        {
            if (NetFoodPerTurn <= 0 || FarmerPercentage > .5f)
            {
                if (ConstructingGoodsBuilding(Goods.Food))
                    return Goods.Production;
                return Goods.Food;
            }
            if (ConstructionQueue.Count > 0) return Goods.Production;
            if (PS == GoodState.IMPORT) return Goods.Production;
            if (FS == GoodState.IMPORT) return Goods.Food;
            return Goods.Food;
        }

        public bool ConstructingGoodsBuilding(Goods goods)
        {
            if (ConstructionQueue.IsEmpty) return false;
            switch (goods)
            {
                case Goods.Production:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesProduction)
                        {
                            return true;
                        }
                    }
                    break;
                case Goods.Food:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesFood)
                        {
                            return true;
                        }
                    }
                    break;
                case Goods.Colonists:
                    break;
                default:
                    break;
            }
            return false;
        }

        public float EmpireFertility(Empire empire) =>
            (empire.data?.Traits.Cybernetic ?? 0) > 0 ? MineralRichness : Fertility;            

        public float EmpireBaseValue(Empire empire) => (
            CommoditiesPresent.Count +
            (1 + EmpireFertility(empire))
            * (1 + MineralRichness )
            * (float)Math.Ceiling(MaxPopulation / 1000f)
            );

        public bool NeedsFood()
        {
            if (Owner?.isFaction ?? true) return false;
            bool cyber = Owner.data.Traits.Cybernetic > 0;
            float food = cyber ? ProductionHere : FoodHere;
            bool badProduction = cyber ? NetProductionPerTurn <= 0 && WorkerPercentage > .5f : 
                (NetFoodPerTurn <= 0 && FarmerPercentage >.5f);
            return food / MaxStorage < .10f || badProduction;
        }
        
        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        //added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);        

        public float GetNetFoodPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetFoodPerTurn;
            else
                return NetFoodPerTurn - Consumption;
        }

        public void ApplyAllStoredProduction(int index) => SbProduction.ApplyAllStoredProduction(index);

        public bool ApplyStoredProduction(int index) => SbProduction.ApplyStoredProduction(index);

        public void ApplyProductiontoQueue(float howMuch, int whichItem) => SbProduction.ApplyProductiontoQueue(howMuch, whichItem);

        public float GetNetProductionPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetProductionPerTurn - Consumption;
            else
                return NetProductionPerTurn;
        }

        public bool TryBiosphereBuild(Building b, QueueItem qi) => SbProduction.TryBiosphereBuild(b, qi);

        public void Update(float elapsedTime)
        {
    
            Array<Guid> list = new Array<Guid>();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (!keyValuePair.Value?.Active ?? true //Remove this null check later. 
                    || keyValuePair.Value.Size == 0)
                    list.Add(keyValuePair.Key);
            }
            foreach (Guid key in list)
                Shipyards.Remove(key);
            TroopManager.Update(elapsedTime);
            GeodeticManager.Update(elapsedTime);
           
            for (int index1 = 0; index1 < BuildingList.Count; ++index1)
            {
                //try
                {
                    Building building = BuildingList[index1];
                    if (building.isWeapon)
                    {
                        building.WeaponTimer -= elapsedTime;
                        if (building.WeaponTimer < 0 && ParentSystem.ShipList.Count > 0)
                        {
                            if (Owner != null)
                            {
                                Ship target = null;
                                Ship troop = null;
                                float currentD = 0;
                                float previousD = building.theWeapon.Range + 1000f;
                                //float currentT = 0;
                                float previousT = building.theWeapon.Range + 1000f;
                                //this.system.ShipList.thisLock.EnterReadLock();
                                for (int index2 = 0; index2 < ParentSystem.ShipList.Count; ++index2)
                                {
                                    Ship ship = ParentSystem.ShipList[index2];
                                    if (ship.loyalty == Owner || (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact) )
                                        continue;
                                    currentD = Vector2.Distance(Center, ship.Center);                                   
                                    if (ship.shipData.Role == ShipData.RoleName.troop && currentD  < previousT)
                                    {
                                        previousT = currentD;
                                        troop = ship;
                                        continue;
                                    }
                                    if(currentD < previousD && troop ==null)
                                    {
                                        previousD = currentD;
                                        target = ship;
                                    }

                                }

                                if (troop != null)
                                    target = troop;
                                if(target != null)
                                {
                                    building.theWeapon.Center = Center;
                                    building.theWeapon.FireFromPlanet(this, target);
                                    building.WeaponTimer = building.theWeapon.fireDelay;
                                    break;
                                }


                            }
                        }
                    }
                }
            }
            for (int index = 0; index < Projectiles.Count; ++index)
            {
                Projectile projectile = Projectiles[index];
                if (projectile.Active)
                {
                    if (elapsedTime > 0)
                        projectile.Update(elapsedTime);
                }
                else
                    Projectiles.QueuePendingRemoval(projectile);
            }
            Projectiles.ApplyPendingRemovals();
            UpdatePosition(elapsedTime);
        }
   
        public void TerraformExternal(float amount)
        {
            Fertility += amount;
            if (Fertility <= 0.0)
            {
                Fertility = 0.0f;
                PlanetType = 7;
                Terraform();
            }
            else if (Type == "Barren" && Fertility > 0.01)
            {
                PlanetType = 14;
                Terraform();
            }
            else if (Type == "Desert" && Fertility > 0.35)
            {
                PlanetType = 18;
                Terraform();
            }
            else if (Type == "Ice" && Fertility > 0.35)
            {
                PlanetType = 19;
                Terraform();
            }
            else if (Type == "Swamp" && Fertility > 0.75)
            {
                PlanetType = 21;
                Terraform();
            }
            else if (Type == "Steppe" && Fertility > 0.6)
            {
                PlanetType = 11;
                Terraform();
            }
            else
            {
                if (!(Type == "Tundra") || Fertility <= 0.95)
                    return;
                PlanetType = 22;
                Terraform();
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            if (CrippledTurns > 0) CrippledTurns--;
            else CrippledTurns = 0;

            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentStatus();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            TerraformPoints += TerraformToAdd;
            if (TerraformPoints > 0.0f && Fertility < 1.0)
            {
                Fertility += TerraformToAdd;
                if (Type == "Barren" && Fertility > 0.01)
                {
                    PlanetType = 14;
                    Terraform();
                }
                else if (Type == "Desert" && Fertility > 0.35)
                {
                    PlanetType = 18;
                    Terraform();
                }
                else if (Type == "Ice" && Fertility > 0.35)
                {
                    PlanetType = 19;
                    Terraform();
                }
                else if (Type == "Swamp" && Fertility > 0.75)
                {
                    PlanetType = 21;
                    Terraform();
                }
                else if (Type == "Steppe" && Fertility > 0.6)
                {
                    PlanetType = 11;
                    Terraform();
                }
                else if (Type == "Tundra" && Fertility > 0.95)
                {
                    PlanetType = 22;
                    Terraform();
                }
                if (Fertility > 1.0)
                    Fertility = 1f;
            }
            DoGoverning();
            UpdateIncomes(false);

            // notification about empty queue
            if (GlobalStats.ExtraNotifications && Owner != null && Owner.isPlayer)
            {
                if (ConstructionQueue.Count == 0 && !QueueEmptySent)
                {
                    if (colonyType == ColonyType.Colony || colonyType == ColonyType.Core || colonyType == ColonyType.Industrial || !GovernorOn)
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
                Planet shieldStrengthCurrent = this;

                if (!RecentCombat)
                {

                    if (ShieldStrengthCurrent > ShieldStrengthMax / 10)
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent += shieldStrengthCurrent.ShieldStrengthMax / 10;
                    }
                    else
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent++;
                    }
                }
                if (ShieldStrengthCurrent > ShieldStrengthMax)
                    ShieldStrengthCurrent = ShieldStrengthMax;
            }

            //this.UpdateTimer = 10f;
            HarvestResources();
            ApplyProductionTowardsConstruction();
            GrowPopulation();
            HealTroops();
            CalculateIncomingTrade();
        }

        public float IncomingFood = 0;
        public float IncomingProduction = 0;
        public float IncomingColonists = 0;

        public void UpdateDevelopmentStatus()
        {
            Density = Population / 1000f;
            float maxPop = MaxPopulation / 1000f;
            if (Density <= 0.5f)
            {
                DevelopmentLevel = 1;
                DevelopmentStatus = Localizer.Token(1763);
                if (maxPop >= 2 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop >= 2f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 0 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 0.5f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 0.5f && Density <= 2)
            {
                DevelopmentLevel = 2;
                DevelopmentStatus = Localizer.Token(1768);
                if (maxPop >= 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1770);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 2.0 && Density <= 5.0)
            {
                DevelopmentLevel = 3;
                DevelopmentStatus = Localizer.Token(1771);
                if (maxPop >= 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1773);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 5.0 && Density <= 10.0)
            {
                DevelopmentLevel = 4;
                DevelopmentStatus = Localizer.Token(1774);
            }
            else if (Density > 10.0)
            {
                DevelopmentLevel = 5;
                DevelopmentStatus = Localizer.Token(1775);
            }
            if (NetProductionPerTurn >= 10.0 && HasShipyard)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1776);
                planet.DevelopmentStatus = str;
            }
            else if (Fertility >= 2.0 && NetFoodPerTurn > (double)MaxPopulation)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1777);
                planet.DevelopmentStatus = str;
            }
            else if (NetResearchPerTurn > 5.0)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1778);
                planet.DevelopmentStatus = str;
            }
            if (!AllowInfantry || TroopsHere.Count <= 6)
                return;
            var planet1 = this;
            string str1 = planet1.DevelopmentStatus + Localizer.Token(1779);
            planet1.DevelopmentStatus = str1;
        }

        private static bool AddToIncomingTrade(ref float type, float amount)
        {
            if (amount < 1) return false;
            type += amount;
            return true;
        }

        private void CalculateIncomingTrade()
        {
            if (Owner == null || Owner.isFaction) return;
            IncomingProduction = 0;
            IncomingFood = 0;
            TradeIncomingColonists = 0;
            using (Owner.GetShips().AcquireReadLock())
            {
                foreach (var ship in Owner.GetShips())
                {
                    if (ship.DesignRole != ShipData.RoleName.freighter) continue;
                    if (ship.AI.end != this) continue;
                    if (ship.AI.State != AIState.SystemTrader && ship.AI.State != AIState.PassengerTransport) continue;

                    if (AddToIncomingTrade(ref IncomingFood, ship.GetFood())) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.GetProduction())) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.GetColonists())) return;

                    if (AddToIncomingTrade(ref IncomingFood, ship.CargoSpaceMax * (ship.AI.FoodOrProd == "Food" ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.CargoSpaceMax * (ship.AI.FoodOrProd == "Prod" ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.CargoSpaceMax)) return;
                }
            }
        }

        private float CalculateFarmerPercentForSurplus(float desiredSurplus)
        {
            if (Fertility + PlusFoodPerColonist <= 0.1) return 0.0f; //Easy out for crap-planets

            float Surplus = 0.0f;
            if (Owner.data.Traits.Cybernetic > 0)
            {
                float totalConsumption = Consumption + desiredSurplus - PlusFlatProductionPerTurn;
                float totalPopulation = (Population / 1000);
                float totalProductionRate = (1 - Owner.data.TaxRate);

                if (totalConsumption == 0 || totalPopulation == 0 || totalProductionRate == 0) return 0.0f; //No divide by 0

                Surplus = totalConsumption / totalPopulation / totalProductionRate;
            }
            else
            {                         //'Consumption' = Amount of food consumed after race modifier (Gluttonous, Efficient Metabolism, etc)
                float totalConsumption = Consumption + desiredSurplus - FlatFoodAdded;
                float totalPopulation = Population / 1000;
                float totalProductionRate = Fertility + PlusFoodPerColonist;

                if (totalConsumption == 0 || totalPopulation == 0 || totalProductionRate == 0) return 0.0f; //No divide by 0

                Surplus = totalConsumption / totalPopulation / totalProductionRate;
            }

            if      (Surplus <= 0) return 0.0f;
            else if (Surplus >= 1) return 1.0f;
            else return Surplus;
            
        }     

        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null) return;
            BuildingsCanBuild.Clear();

            //See if it already has a command building or not.
            bool needCommandBuilding = true;
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    needCommandBuilding = false;
                    break;
                }
            }

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value) continue;
                Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];

                //Skip adding +food buildings for cybernetic races
                if (Owner.data.Traits.Cybernetic > 0 && (building1.PlusFlatFoodAmount > 0 || building1.PlusFoodPerColonist > 0)) continue;

                //Skip adding command buildings if planet already has one
                if (!needCommandBuilding && (building1.Name == "Outpost" || building1.Name == "Capital City")) continue;

                bool foundIt = false;

                //Make sure the building isn't already built on this planet
                foreach (Building building2 in BuildingList)
                {
                    if (!building2.Unique) continue;

                    if (building2.Name == building1.Name)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Make sure the building isn't already being built on this planet
                for (int index = 0; index < ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem = ConstructionQueue[index];
                    if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && queueItem.Building.Unique)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Hide Biospheres if the entire planet is already habitable
                if (building1.Name == "Biosphers")
                {
                    bool allHabitable = true;
                    foreach (PlanetGridSquare tile in TilesList)
                    {
                        if (!tile.Habitable)
                        {
                            allHabitable = false;
                            break;
                        }
                    }
                    if (allHabitable) continue;
                }

                //If this is a one-per-empire building, make sure it hasn't been built already elsewhere
                //Reusing fountIt bool from above
                if (building1.BuildOnlyOnce)
                {
                    //Check for this unique building across the empire
                    foreach (Planet planet in Owner.GetPlanets())
                    {
                        //First check built buildings
                        foreach (Building building2 in planet.BuildingList)
                        {
                            if (building2.Name == building1.Name)
                            {
                                foundIt = false;
                                break;
                            }
                        }
                        if (foundIt) break;

                        //Then check production queue
                        foreach (QueueItem queueItem in planet.ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name)
                            {
                                foundIt = true;
                                break;
                            }
                        }
                        if (foundIt) break;
                    }
                    if (foundIt) continue;
                }

                //If the building is still a candidate after all that, then add it to the list!
                BuildingsCanBuild.Add(building1);
            }
        }

        public void AddBuildingToCQ(Building b) => SbProduction.AddBuildingToCQ(b);
     
        public void AddBuildingToCQ(Building b, bool PlayerAdded) => SbProduction.AddBuildingToCQ(b, PlayerAdded);     

        public bool BuildingInQueue(string UID)
        {
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                if (ConstructionQueue[index].isBuilding && ConstructionQueue[index].Building.Name == UID)
                    return true;
            }
            return false;
        }

        public bool BuildingExists(Building exactInstance) => BuildingList.Contains(exactInstance);

        public bool BuildingExists(string buildingName)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
                if (BuildingList[i].Name == buildingName)
                    return true;
            return BuildingInQueue(buildingName);
            
        }

        public bool WeCanAffordThis(Building building, Planet.ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            Empire empire = Owner;
            float buildingMaintenance = empire.GetTotalBuildingMaintenance();
            float grossTaxes = empire.GrossTaxes;
          
            bool itsHere = BuildingList.Contains(building);
            
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
                    bool added =queueItem.Building == building;
                    if (added) itsHere = true;
                }
                
            }
            buildingMaintenance += building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod;
            
            bool LowPri = buildingMaintenance / grossTaxes < .25f;
            bool MedPri = buildingMaintenance / grossTaxes < .60f;
            bool HighPri = buildingMaintenance / grossTaxes < .80f;
            float income = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);           
            float maintCost = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - building.Maintenance- (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);
            bool makingMoney = maintCost > 0;
      
            int defensiveBuildings = BuildingList.Count(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded >0 ||combat.theWeapon !=null);           
           int possibleoffensiveBuilding = BuildingsCanBuild.Count(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.theWeapon != null);
           bool isdefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon ;
           float defenseratio =0;
            if(defensiveBuildings+possibleoffensiveBuilding >0)
                defenseratio = (defensiveBuildings + 1) / (float)(defensiveBuildings + possibleoffensiveBuilding + 1);
            SystemCommander SC;
            bool needDefense =false;
            
            if (Owner.data.TaxRate > .5f)
                makingMoney = false;
            //dont scrap buildings if we can use treasury to pay for it. 
            if (building.AllowInfantry && !BuildingList.Contains(building) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SC))
            {
                if (makingMoney)
                    needDefense = SC.RankImportance >= defenseratio *10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }
            
            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == Type)
                return false;
            

            if (itsHere && building.Unique && (makingMoney || building.Maintenance < Owner.Money * .001))
                return true;

            if (building.PlusTaxPercentage * GrossMoneyPT >= building.Maintenance 
                || building.CreditsProduced(this) >= building.Maintenance 

                
                ) 
                return true;
            if (building.Name == "Outpost" || building.WinsGame  )
                return true;
            //dont build +food if you dont need to

            if (Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (NetFoodPerTurn > 0 && FarmerPercentage < .3 || BuildingExists(building.Name))

                    return false;
                else
                    return true;
               
            }
            if (Owner.data.Traits.Cybernetic < 1 && income > building.Maintenance ) 
            {
                float food = building.FoodProduced(this);
                if (food * FarmerPercentage > 1)
                {
                    return true;
                }
                else
                {
                    
                }
            }
            if(Owner.data.Traits.Cybernetic >0)
            {
                if(NetProductionPerTurn - Consumption <0)
                {
                    if(building.PlusFlatProductionAmount >0 && (WorkerPercentage > .5 || income >building.Maintenance*2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * (Population / 1000) > building.Maintenance *(2- WorkerPercentage))
                    {
                        if (income > ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * MineralRichness > building.Maintenance )
                        return true;
                }
            }
            if(building.PlusTerraformPoints >0)
            {
                if (!makingMoney || Owner.data.Traits.Cybernetic>0|| BuildingList.Contains(building) || BuildingInQueue(building.Name))
                    return false;
                
            }
            if(!makingMoney || DevelopmentLevel < 3)
            {
                if (building.Name == "Biospheres")
                    return false;
            }
                
            bool iftrue = false;
            switch  (governor)
            {
                case ColonyType.Agricultural:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential()>20 )
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <=0)
                            return false;
                        if (HighPri)
                        {
                            if (building.PlusFlatFoodAmount > 0
                                || (building.PlusFoodPerColonist > 0 && Population > 500f)
                                
                                //|| this.developmentLevel > 4
                                || ((building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0) && Population > MaxPopulation * .5f)
                                || building.PlusFlatFoodAmount > 0
                                || building.PlusFlatProductionAmount > 0
                                || building.StorageAdded > 0 
                               // || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount>0))
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;
                                //iftrue = true;
                            
                        }
                        if (!iftrue && MedPri && DevelopmentLevel > 2 && makingMoney)
                        {
                            if (
                                building.Name == "Biospheres"||
                                ( building.PlusTerraformPoints > 0 && Fertility < 3)
                                || building.MaxPopIncrease > 0 
                                || building.PlusFlatPopulation > 0
                                || DevelopmentLevel > 3
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                || (needDefense && isdefensive )

                                )
                                return true;
                        }
                        if (LowPri && DevelopmentLevel > 4 && makingMoney)
                        {
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Core:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {

                            if (building.StorageAdded > 0
                                || (Owner.data.Traits.Cybernetic <=0 && (building.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)                             
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFlatFoodAmount > 0)
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFoodPerColonist > 0)                                
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness >0
                                || building.PlusProdPerColonist >0
                                || building.PlusFlatResearchAmount>0
                                || (building.PlusResearchPerColonist>0 && Population / 1000 > 1)
                                //|| building.Name == "Biospheres"                                
                                
                                || (needDefense && isdefensive && DevelopmentLevel > 3)                                
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 3 &&makingMoney )
                        {
                            if (DevelopmentLevel > 2 && needDefense && (building.theWeapon != null || building.Strength > 0))
                                return true;
                            iftrue = true;
                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 4 && makingMoney && income > building.Maintenance)
                        {
                            
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Industrial:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (HighPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0
                                || (Owner.data.Traits  .Cybernetic <=0 && Fertility < 1f && building.PlusFlatFoodAmount > 0)                             
                                || building.StorageAdded > 0
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 2 && makingMoney)
                        {
                            if (building.PlusResearchPerColonist * Population / 1000 >building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                            || (Owner.data.Traits.Cybernetic <= 0 && building.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation && MaxPopulation > 2000 && income>building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0)
                                ||building.PlusFlatResearchAmount >0
                                || (building.PlusResearchPerColonist >0 && MaxPopulation > 999)
                                )
                               
                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 3 && makingMoney && income >building.Maintenance)
                        {
                            if (needDefense && isdefensive && DevelopmentLevel > 2)
                                return true;
                            
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Military:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {
                            if (building.isWeapon
                                || building.IsSensor
                                || building.Defense > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (MineralRichness < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlanetaryShieldStrengthAdded > 0
                                || (building.AllowShipBuilding  && GrossProductionPerTurn > 1)
                                || (building.ShipRepair > 0&& GrossProductionPerTurn > 1)
                                || building.Strength > 0
                                || (building.AllowInfantry && GrossProductionPerTurn > 1)
                                || needDefense &&(building.theWeapon !=null || building.Strength >0)
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                iftrue = true;
                        }
                        if (!iftrue && MedPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0)
                                iftrue = true;
                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 4)
                        {
                            //if(building.Name!= "Biospheres")
                            iftrue = true;

                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Research:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;

                        if (HighPri)
                        {
                            if (building.PlusFlatResearchAmount > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlusFlatProductionAmount >0
                                || building.PlusResearchPerColonist > 0
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0 ))
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;

                        }
                        if ( MedPri && DevelopmentLevel > 3 && makingMoney)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                            || Owner.data.Traits.Cybernetic <=0 &&( (building.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * .5f && MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0))
                                )
                                return true;
                        }
                        if ( LowPri && DevelopmentLevel > 4 && makingMoney)
                        {
                            if (needDefense && isdefensive && DevelopmentLevel > 2)
                                
                            return true;
                        }
                        break;
                    } 
                    #endregion
            }
            return iftrue;

        }

        private void SetExportState(ColonyType colonyType)
        {

            bool FSexport = false;
            bool PSexport = false;
            int pc = Owner.GetPlanets().Count;




            bool exportPSFlag = true;
            bool exportFSFlag = true;
            float exportPTrack = Owner.exportPTrack;
            float exportFTrack = Owner.exportFTrack;

            if (pc == 1)
            {
                FSexport = false;
                PSexport = false;
            }
            exportFSFlag = exportFTrack / pc * 2 >= ExportFSWeight;
            exportPSFlag = exportPTrack / pc * 2 >= ExportPSWeight;

            if (!exportFSFlag || Owner.averagePLanetStorage >= MaxStorage)
                FSexport = true;

            if (!exportPSFlag || Owner.averagePLanetStorage >= MaxStorage)
                PSexport = true;
            float PRatio = ProductionHere / MaxStorage;
            float FRatio = FoodHere / MaxStorage;

            int queueCount = ConstructionQueue.Count;
            switch (colonyType)
            {

                case ColonyType.Colony:
                case ColonyType.Industrial:
                    if (Population >= 1000 && MaxPopulation >= Population)
                    {
                        if (PRatio < .9 && queueCount > 0) 
                            PS = GoodState.IMPORT;
                        else if (queueCount == 0)
                        {
                            PS = GoodState.EXPORT;
                        }
                        else
                            PS = GoodState.STORE;

                    }
                    else if (queueCount > 0 || Owner.data.Traits.Cybernetic > 0)
                    {
                        if (PRatio < .5f)
                            PS = GoodState.IMPORT;
                        else if (!PSexport && PRatio > .5)
                            PS = GoodState.EXPORT;
                        else
                            PS = GoodState.STORE;
                    }
                    else
                    {
                        if (PRatio > .5f && !PSexport)
                            PS = GoodState.EXPORT;
                        else if (PRatio > .5f && PSexport)
                            PS = GoodState.STORE;
                        else PS = GoodState.EXPORT;

                    }

                    if (NetFoodPerTurn < 0)
                        FS = Planet.GoodState.IMPORT;
                    else if (FRatio > .75f)
                        FS = Planet.GoodState.STORE;
                    else
                        FS = Planet.GoodState.IMPORT;
                    break;


                case ColonyType.Agricultural:
                    if (PRatio > .75 && !PSexport)
                        PS = Planet.GoodState.EXPORT;
                    else if (PRatio < .5 && PSexport)
                        PS = Planet.GoodState.IMPORT;
                    else
                        PS = GoodState.STORE;


                    if (NetFoodPerTurn > 0)
                        FS = Planet.GoodState.EXPORT;
                    else if (NetFoodPerTurn < 0)
                        FS = Planet.GoodState.IMPORT;
                    else if (FRatio > .75f)
                        FS = Planet.GoodState.STORE;
                    else
                        FS = Planet.GoodState.IMPORT;

                    break;

                case ColonyType.Research:

                    {
                        if (PRatio > .75f && !PSexport)
                            PS = Planet.GoodState.EXPORT;
                        else if (PRatio < .5f) //&& PSexport
                            PS = Planet.GoodState.IMPORT;
                        else
                            PS = GoodState.STORE;

                        if (NetFoodPerTurn < 0)
                            FS = Planet.GoodState.IMPORT;
                        else if (NetFoodPerTurn < 0)
                            FS = Planet.GoodState.IMPORT;
                        else
                        if (FRatio > .75f && !FSexport)
                            FS = Planet.GoodState.EXPORT;
                        else if (FRatio < .75) //FSexport &&
                            FS = Planet.GoodState.IMPORT;
                        else
                            FS = GoodState.STORE;

                        break;
                    }

                case ColonyType.Core:
                    if (MaxPopulation > Population * .75f && Population > DevelopmentLevel * 1000)
                    {

                        if (PRatio > .33f)
                            PS = GoodState.EXPORT;
                        else if (PRatio < .33)
                            PS = GoodState.STORE;
                        else
                            PS = GoodState.IMPORT;
                    }
                    else
                    {
                        if (PRatio > .75 && !FSexport)
                            PS = GoodState.EXPORT;
                        else if (PRatio < .5) //&& FSexport
                            PS = GoodState.IMPORT;
                        else PS = GoodState.STORE;
                    }

                    if (NetFoodPerTurn < 0)
                        FS = Planet.GoodState.IMPORT;
                    else if (FRatio > .25)
                        FS = GoodState.EXPORT;
                    else if (NetFoodPerTurn > DevelopmentLevel * .5)
                        FS = GoodState.STORE;
                    else
                        FS = GoodState.IMPORT;


                    break;
                case ColonyType.Military:
                case ColonyType.TradeHub:
                    if (FS != GoodState.STORE)
                        if (FRatio > .50)
                            FS = GoodState.EXPORT;
                        else
                            FS = GoodState.IMPORT;
                    if (PS != GoodState.STORE)
                        if (PRatio > .50)
                            PS = GoodState.EXPORT;
                        else
                            PS = GoodState.IMPORT;

                    break;

                default:
                    break;
            }
            if (!PSexport)
                this.PSexport = true;
            else
            {
                this.PSexport = false;
            }


        }

        private void BuildShipywardifAble()
        {
            if (Owner != Empire.Universe.PlayerEmpire
                && !Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
            {
                bool hasShipyard = false;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isShip && queueItem.sData.IsShipyard)
                    {
                        hasShipyard = true;
                        break;
                    }
                }
                if (!hasShipyard && DevelopmentLevel > 2)
                    ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                        Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                                UniverseScreen.GamePaceStatic
                    });
            }
        }

        private void BuildOutpostifAble() //A Gretman function to support DoGoverning()
        {
            bool foundOutpost = false;

            //First check the existing buildings
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Outpost" || building.Name == "Capital City")
                {
                    foundOutpost = true;
                    break;
                }
            }
            if (foundOutpost) return;

            //Then check the queue
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                {
                    foundOutpost = true;
                    break;
                }
            }
            if (foundOutpost) return;

            //Still no? Build it!
            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"), false);

            //Move Outpost to the top of the list, and rush production
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                QueueItem queueItem1 = ConstructionQueue[index];
                if (index == 0 && queueItem1.isBuilding)
                {
                    if (queueItem1.Building.Name == "Outpost")
                    {
                        SbProduction.ApplyAllStoredProduction(0);
                    }
                    break;
                }
                else if (queueItem1.isBuilding && queueItem1.Building.Name == "Outpost")
                {
                    ConstructionQueue.Remove(queueItem1);
                    ConstructionQueue.Insert(0, queueItem1);
                    break;
                }
            }
        }

        private bool BuildBasicInfrastructure() //A Gretman function to support DoGoverning()
        {
            //Figure out the cheapest buildings for each category
            Building cheapestFlatprod = BuildingsCanBuild.Where(flat => flat.PlusFlatProductionAmount > 0)
                    .OrderByDescending(cost => cost.Cost).FirstOrDefault();

            Building cheapestFlatfood;
            if (Owner.data.Traits.Cybernetic > 0)
            {
                cheapestFlatfood = cheapestFlatprod;
            }
            else
            {
                cheapestFlatfood = BuildingsCanBuild.Where(flatfood => flatfood.PlusFlatFoodAmount > 0)
                            .OrderByDescending(cost => cost.Cost).FirstOrDefault();
            }

            Building cheapestFlatResearch = BuildingsCanBuild.Where(flat => flat.PlusFlatResearchAmount > 0)
                .OrderByDescending(cost => cost.Cost).FirstOrDefault();

            Building buildthis = null;
            buildthis = cheapestFlatprod ?? cheapestFlatfood ?? cheapestFlatResearch;

            if (buildthis == null) return false;

            AddBuildingToCQ(buildthis);
            return true;
        }

        private int BiasedCountQueue()
        {
            //I have no idea why the Biospheres are double counted in the original code, but they are. ConstructionQueue.Count would be so much easier.
            int total = 0;
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    ++total;
                    if (queueItem.Building.Name == "Biospheres") ++total;
                }
            }
            return total;
        }

        private float CalculateFoodWorkers()    //Simply calculates what percentage of workers are needed for farming (between 0 and 1)
        {
            if (Owner.data.Traits.Cybernetic != 0 || Fertility + PlusFoodPerColonist <= 0) return 0.0f;

            float workers = (Consumption - FlatFoodAdded) / (Population / 1000) / (Fertility + PlusFoodPerColonist);
            if (workers > 1.0f) return 1.0f;
            if (workers < 0.0f) return 0.0f;
            else return workers;
        }

        private bool workerFillStorage(float workers, float percentage) //Returns true if workers were assigned, or false if they werent
        {
            if (MaxStorage == 0) return false;
            float storedFoodRatio = FoodHere / MaxStorage;
            float storedProdRatio = ProductionHere / MaxStorage;
            if (Fertility + PlusFoodPerColonist <= 0.1 || Owner.data.Traits.Cybernetic > 0) storedFoodRatio = 1.0f; //No farming here, so skip it
            if (PlusFlatProductionPerTurn > 0) storedProdRatio += PlusFlatProductionPerTurn * .05f;     //Dont top off if there is flatprod, since we cant turn it off

            if (storedFoodRatio < percentage && storedProdRatio < percentage)
            {
                FarmerPercentage += workers * 0.5f;
                WorkerPercentage += workers * 0.5f;
                return true;
            }
            else if (storedFoodRatio < percentage)
            {
                FarmerPercentage += workers;
                return true;
            }
            else if (storedProdRatio < percentage)
            {
                WorkerPercentage += workers;
                return true;
            }

            return false;
        }

        private bool workerFillStorageFood(float workers, float percentage) //Returns true if workers were assigned, or false if they werent
        {
            float storedFoodRatio = FoodHere / (MaxStorage + 0.0001f);
            if (Fertility + PlusFoodPerColonist <= 0.1 || Owner.data.Traits.Cybernetic > 0) storedFoodRatio = 1.0f; //No farming here, so skip it

            if (storedFoodRatio < percentage)
            {
                FarmerPercentage += workers;
                return true;
            }
            else return false;
        }

        private bool workerFillStorageProd(float workers, float percentage) //Returns true if workers were assigned, or false if they werent
        {
            float storedProdRatio = ProductionHere / (MaxStorage + 0.0001f);

            if (storedProdRatio < percentage)
            {
                WorkerPercentage += workers;
                return true;
            }
            else return false;
        }

        private bool ShouldWeBuildThis(float maintCost, bool acceptLoss = false)
        {
            if (Owner.Money < 0.0 || Owner.MoneyLastTurn <= 0.0) return false;   //You have bad credit!
            float acceptableLoss = Owner.data.FlatMoneyBonus * 0.05f;
            if (acceptLoss) acceptableLoss += Math.Min(0.5f, Math.Min(Owner.Money * 0.01f, Owner.MoneyLastTurn * 0.1f));

            if (GrossMoneyPT + acceptableLoss > TotalMaintenanceCostsPerTurn + maintCost) return true;
            else return false;
        }

        private Building WhichIsBetter(Building first, Building second, Func<Building, float> property)
        {
            if (first == null) return second;

            //Resource provided / maintenance gives a good simple way to look at efficiency
            if (second.PlusFlatFoodAmount / (second.Maintenance + 0.0001f) > first.PlusFlatFoodAmount / (first.Maintenance + 0.0001f))
                return second;
            else return first;
        }   //Building bestCost   = Building.SelectGreater(first, second, b => b.Cost);

        private float EvaluateBuilding(Building building, float income)     //Gretman function, to support DoGoverning()
        {
            float finalScore = 0.0f;    //End result score for entire building
            float score = 0.0f;         //Reused variable for each step

            float maxPopulation = (MaxPopulation + MaxPopBonus) / 1000f;
            bool doingResearch = !string.IsNullOrEmpty(Owner.ResearchTopic);

            if (Name == "MerVille")
                 { double spotForABreakpoint = Math.PI; }

            //First things first! How much is it gonna' cost?
            if (building.Maintenance != 0)
            {
                score += building.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
                if (income < building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod)
                    score += score + (building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod);   //Really dont want this if we cant afford it
                score -= Owner.data.FlatMoneyBonus * 0.03f;      //Acceptible loss (Note what this will do at high Difficulty)

                finalScore -= score;
            }

            //Flat Food
            if (building.PlusFlatFoodAmount != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    score += building.PlusFlatFoodAmount / maxPopulation;   //Percentage of population this will feed
                    score += 1.5f - (Fertility + PlusFoodPerColonist);   //Bonus for low Fertility planets
                    if (score < building.PlusFlatFoodAmount * 0.1f) score = building.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                    if (building.PlusFlatFoodAmount + FlatFoodAdded - 0.5f > maxPopulation) score = 0;   //Dont want this if a lot would go to waste
                }
                finalScore += score;
            }

            //Food per Colonist
            if (building.PlusFoodPerColonist != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * maxPopulation * 2; //for negative value
                else
                {
                    score += building.PlusFoodPerColonist * maxPopulation - FlatFoodAdded;  //How much food could this create (with penalty for FlatFood)
                    score += Fertility - 0.5f;  //Bonus for high fertility planets
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (Fertility + building.PlusFoodPerColonist + PlusFoodPerColonist < 1.1f) score = 0;     //Dont try to add farming to a planet without enough to sustain itself
                }
                finalScore += score;
            }

            //Flat Prod
            if (building.PlusFlatProductionAmount != 0)
            {
                score = 0;
                if (building.PlusFlatProductionAmount < 0) score = building.PlusFlatProductionAmount * 2; //for negative value
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += farmers;   //Bonus the fewer workers there are available
                    score += Math.Max(0, 1 - (MineralRichness + PlusProductionPerColonist));    //Bonus for low richness planets (this tips the scale for edge cases)
                    score += building.PlusFlatProductionAmount - (MineralRichness + PlusProductionPerColonist) * ((1 - farmers) * maxPopulation);   //How much more Prod this would produce
                    if (score < building.PlusFlatProductionAmount * 0.1f) score = building.PlusFlatProductionAmount * 0.1f; //A little production is always useful
                }
                finalScore += score;
            }

            //Prod per Colonist
            if (building.PlusProdPerColonist != 0)
            {
                score = 0;
                if (building.PlusProdPerColonist < 0) score = building.PlusProdPerColonist * maxPopulation * 2;
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += 1 - farmers;   //Bonus the more workers there are available
                    score += building.PlusProdPerColonist * maxPopulation * farmers;    //Prod this building will add
                    if (score < building.PlusProdPerColonist * 0.1f) score = building.PlusProdPerColonist * 0.1f; //A little production is always useful
                }
                finalScore += score;
            }

            //Prod per Richness
            if (building.PlusProdPerRichness != 0)  //This one can produce a pretty high building value, which is normally offset by its huge maintenance cost and Fertility loss
            {
                score = 0;
                if (building.PlusProdPerRichness < 0) score = building.PlusProdPerRichness * MineralRichness * 2;
                else
                {
                    score += building.PlusProdPerRichness * MineralRichness;        //Production this would generate
                    if (!HasShipyard) score *= 0.75f;       //Do we have a use for all this production?
                }
                finalScore += score;
            }

            //Storage
            if (building.StorageAdded != 0)
            {
                score = 0;

                float desiredStorage = 50.0f;
                if (Fertility + PlusFoodPerColonist > 2.5f || MineralRichness + PlusProductionPerColonist > 2.5f || PlusFlatProductionPerTurn > 5) desiredStorage += 100.0f;  //Potential high output
                if (HasShipyard) desiredStorage += 100.0f;      //For buildin' ships 'n shit
                if (MaxStorage < desiredStorage) score += (building.StorageAdded * 0.002f) - (building.Cost * 0.001f);  //If we need more storage, rate this building
                if (building.Maintenance > 0) score *= 0.25f;       //Prefer free storage
                if (score < 0.01f) score = 0.01f; //A little storage is always a useful

                finalScore += score;
            }

            //Plus Population Growth
            if (building.PlusFlatPopulation != 0)
            {
                score = 0;
                if (building.PlusFlatPopulation < 0) score = building.PlusFlatPopulation * 0.02f;  //Which is sorta like     0.01f * 2
                else
                {
                    score += (maxPopulation * 0.02f - 1.0f) + (building.PlusFlatPopulation * 0.01f);        //More desireable on high pop planets
                    if (score < building.PlusFlatPopulation * 0.01f) score = building.PlusFlatPopulation * 0.01f;     //A little extra is still good
                }
                if (Owner.data.Traits.PhysicalTraitLessFertile) score *= 2;     //These are calculated outside the else, so they will affect negative flatpop too
                if (Owner.data.Traits.PhysicalTraitFertile) score *= 0.5f;
                finalScore += score;
            }

            //Plus Max Population
            if (building.MaxPopIncrease != 0)
            {
                score = 0;
                if (building.MaxPopIncrease < 0) score = building.MaxPopIncrease * 0.002f;      //Which is sorta like     0.001f * 2
                else
                {
                    //Basically, only add to the score if we would be able to feed the extra people
                    if ((Fertility + PlusFoodPerColonist + building.PlusFoodPerColonist) * ((maxPopulation + building.MaxPopIncrease) / 1000)
                        >= ((maxPopulation + building.MaxPopIncrease) / 1000) - FlatFoodAdded - building.PlusFlatFoodAmount)
                        score += building.MaxPopIncrease * 0.001f;
                }
                finalScore += score;
            }

            //Flat Research
            if (building.PlusFlatResearchAmount != 0 && doingResearch)
            {
                score = 0.001f;
                if (building.PlusFlatResearchAmount < 0)            //Surly no one would make a negative research building
                {
                    if (ResearcherPercentage > 0 || PlusFlatResearchPerTurn > 0) score += building.PlusFlatResearchAmount * 2;
                    else score += building.PlusFlatResearchAmount;
                }
                else
                {                   //Can we easily afford this
                    if ((building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod) * 1.5 <= income) score += building.PlusFlatResearchAmount * 2;
                    if (score < building.PlusFlatResearchAmount * 0.1f) score = building.PlusFlatResearchAmount * 0.1f; //A little extra research is always useful
                }
                finalScore += score;
            }

            //Research per Colonist
            if (building.PlusResearchPerColonist != 0 && doingResearch)
            {
                score = 0;
                if (building.PlusResearchPerColonist < 0)
                {
                    if (ResearcherPercentage > 0 || PlusFlatResearchPerTurn > 0) score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation) * 2;
                    else score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation);
                }
                else
                {
                    score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation);       //Research this will generate
                }
                finalScore += score;
            }

            //Credits per Colonist
            if (building.CreditsPerColonist != 0)
            {
                score = 0;
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * maxPopulation * 2;
                else score += (building.CreditsPerColonist * maxPopulation) / 2;        //Dont want to cause this to have building preference over infrastructure buildings
                finalScore += score;
            }

            //Plus Tax Percentage
            if (building.PlusTaxPercentage != 0)
            {
                score = 0;

                if (building.PlusTaxPercentage < 0) score += building.PlusTaxPercentage * GrossMoneyPT * 2;
                else score += building.PlusTaxPercentage * GrossMoneyPT / 2;
                finalScore += score;
            }

            //Allow Ship Building
            if (building.AllowShipBuilding)
            {
                score = 0;              //This one probably wont produce overwhelming building value, so will rely on other building tags to overcome the maintenance cost
                float farmers = CalculateFoodWorkers();
                float prodFromLabor = ((1 - farmers) * maxPopulation * (MineralRichness + PlusProductionPerColonist + building.PlusProdPerColonist));
                float prodFromFlat  = PlusFlatProductionPerTurn + building.PlusFlatProductionAmount + (building.PlusProdPerRichness * MineralRichness);
                //Do we have enough production capability to really justify trying to build ships
                if (prodFromLabor + prodFromFlat > 5.0f) score += 5.0f - prodFromLabor + prodFromFlat;
                finalScore += score;
            }

            if (false && building.PlusTerraformPoints != 0)
            {
                //Still working on this one...
            }

            //Fertility loss on build
            if (building.MinusFertilityOnBuild != 0)
            {
                score = 0;
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
                else
                {                                   //How much fertility will actually be lost
                    float fertLost = Math.Min(Fertility, building.MinusFertilityOnBuild);
                    float foodFromLabor = maxPopulation * ((Fertility - fertLost) + PlusFoodPerColonist + building.PlusFoodPerColonist);
                    float foodFromFlat = FlatFoodAdded + building.PlusFlatFoodAmount;
                                //Will we still be able to feed ourselves?
                    if (foodFromFlat + foodFromLabor < Consumption) score += fertLost * 10;
                    else score += fertLost * 4;
                }
                finalScore -= score;
            }

            return finalScore;
        }

        public void DoGoverning()
        {
            BuildOutpostifAble();   //If there is no Outpost or Capital, build it

            if (colonyType == Planet.ColonyType.Colony) return; //No Governor? Nevermind!

            RefreshBuildingsWeCanBuildHere();
            float income = GrossMoneyPT - TotalMaintenanceCostsPerTurn;

            //Do some existing bulding recon
            int openTiles = 0;
            bool noMoreBiospheres = true;
            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (pgs.Habitable)
                {
                    if (pgs.building == null) openTiles++;  //Habitable spot, without a building
                }
                else noMoreBiospheres = false;
            }

            //Construction queue recon
            bool biosphereInTheWorks = false;
            foreach (var thingie in ConstructionQueue)
            {
                if (!thingie.isBuilding) continue;          //Include buildings in queue in income calculations
                income -= thingie.Building.Maintenance + thingie.Building.Maintenance * Owner.data.Traits.MaintMod;
                if (thingie.Building.Name == "Biospheres") biosphereInTheWorks = true;
            }

            //Stuff we can build recon
            Building bioSphere = null;
            foreach (var building in BuildingsCanBuild)
            {
                if (building.Name == "Biospheres")
                {
                    bioSphere = building;
                    break;
                }
            }

            bool notResearching = string.IsNullOrEmpty(Owner.ResearchTopic);
            bool lotsInQueueToBuild = ConstructionQueue.Count >= 4;
            bool littleInQueueToBuild = ConstructionQueue.Count >= 1;
            float foodMinimum = CalculateFoodWorkers();
            bool ForgetReseachAndBuild = 
                notResearching || lotsInQueueToBuild ||
                (DevelopmentLevel < 3 && (ProductionHere + 1) / (MaxStorage + 1) < .5f);

            //Switch to Industrial if there is nothing in the research queue (Does not actually change assigned Governor)
            if (colonyType == ColonyType.Research && notResearching)
                colonyType = ColonyType.Industrial;

            //Get biosphere biased count of the buildings in the production queue
            int buildingsInQueue = BiasedCountQueue();

            switch (colonyType)
            {
                case Planet.ColonyType.TradeHub:
                case Planet.ColonyType.Core:

                #region Core
                {
                    //New resource management by Gretman
                    FarmerPercentage = foodMinimum;
                    WorkerPercentage = 0.0f;
                    ResearcherPercentage = 0.0f;

                    if (FarmerPercentage >= 0.90f) FarmerPercentage = 0.90f;  //Dont let Farming consume all labor

                    float leftoverWorkers = 1 - FarmerPercentage;
                    float allocateWorkers = 0.0f;
                    if (leftoverWorkers > 0.0)
                    {
                        allocateWorkers = Math.Min(leftoverWorkers, 0.15f);
                        leftoverWorkers -= allocateWorkers;

                        if (littleInQueueToBuild) WorkerPercentage += allocateWorkers;          //First priority project for this group is build shit
                        else if (workerFillStorage(allocateWorkers, 0.60f)) ;                   //Second priority is to fill storage up to 60%
                        else if (notResearching) workerFillStorage(allocateWorkers, 1.00f);
                        else ResearcherPercentage += allocateWorkers;                           //Last priority is research, or top off storage if no research
                    }

                    if (leftoverWorkers > 0.0)  //If there are more workers, then we can divide them into groups with different priorities
                    {
                        allocateWorkers = Math.Min(leftoverWorkers, 0.15f);
                        leftoverWorkers -= allocateWorkers;
                        
                        if (lotsInQueueToBuild) WorkerPercentage += allocateWorkers;
                        else if (workerFillStorage(allocateWorkers, 1.00f));
                        else ResearcherPercentage += allocateWorkers;
                    }

                    if (leftoverWorkers > 0.0)
                    {
                        allocateWorkers = Math.Min(leftoverWorkers, 0.20f);
                        leftoverWorkers -= allocateWorkers;

                        if (littleInQueueToBuild) WorkerPercentage += allocateWorkers;
                        else if (notResearching) workerFillStorage(allocateWorkers, 1.00f);
                        else ResearcherPercentage += allocateWorkers;
                    }

                    if (leftoverWorkers > 0.0)
                    {
                        allocateWorkers = leftoverWorkers;  //All the rest
                        leftoverWorkers = 0.0f;

                        if (littleInQueueToBuild && Population < 2000) WorkerPercentage += allocateWorkers;    //Only help build if this is a low pop planet
                        else if (notResearching) workerFillStorage(allocateWorkers, 1.00f);
                        else ResearcherPercentage += allocateWorkers;
                    }

                    if (colonyType == Planet.ColonyType.TradeHub) break;

                    //New Build Logic by Gretman
                    if (!lotsInQueueToBuild) BuildShipywardifAble(); //If we can build a shipyard but dont have one, build it
                    
                    if (openTiles > 0)
                    {
                        if (!littleInQueueToBuild)
                        {
                            Building bestBuilding = null;
                            float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                            for (int i = 0; i < BuildingsCanBuild.Count; i++)
                            {
                                    //Find the building with the highest score
                                    if (EvaluateBuilding(BuildingsCanBuild[i], income) > bestValue) bestBuilding = BuildingsCanBuild[i];
                            }

                            if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                        }
                    }
                    else
                    {
                        if (bioSphere != null && !biosphereInTheWorks && BuildingList.Count < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                        {
                            AddBuildingToCQ(bioSphere);
                        }
                        //Log.Info($"Do Land Troop: Troop Assault Canceled with {Owner.TroopList.Count} troops and {goal.TargetPlanet.GetGroundLandingSpots()} Landing Spots ");
                        //Log.Info(ConsoleColor.Gray, "bioSphere construction rejected.");
                    }

                    break;


                    float surplus = 0;

                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        surplus = GrossProductionPerTurn - Consumption;
                        surplus = surplus * (notResearching ? 1 : .5f) *
                                    (1 - (ProductionHere + 1) / (MaxStorage + 1));
                    }
                    else
                    {
                        surplus = (NetFoodPerTurn * (notResearching ? 1 : .5f)) *
                                                                (1 - (FoodHere + 1) / (MaxStorage + 1));
                    }

                    //Try and work out a surplus
                    FarmerPercentage = CalculateFarmerPercentForSurplus(surplus);
                    //If that requires too much, then try again without the surplus
                    if (FarmerPercentage == 1 && lotsInQueueToBuild)
                        FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                    //If it still needs all of the workers, reserve a small amount for other tasks.
                    if (FarmerPercentage == 1 && lotsInQueueToBuild)
                        FarmerPercentage = .9f;

                    WorkerPercentage =
                        (1f - FarmerPercentage) *
                        (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));

                    float Remainder = 1f - FarmerPercentage;
                    //Research is happening
                    WorkerPercentage = (Remainder * (notResearching
                                            ? 1
                                            : (1 - (ProductionHere) / (MaxStorage))));
                    if (ProductionHere / MaxStorage > .9 && !lotsInQueueToBuild)
                        WorkerPercentage = 0;
                    ResearcherPercentage = Remainder - WorkerPercentage;
                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }

                    SetExportState(colonyType);

                    //If we can build a shipyard, but dont have one, build it
                    BuildShipywardifAble();

                    bool haveTerraformer = false;   //Why the special interest in the terraformer?
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Terraformer")
                        {
                            haveTerraformer = true;
                            break;
                        }
                    }

                    //Try and build some basic infrastructure
                    if (buildingsInQueue < 2)
                    {
                        if (BuildBasicInfrastructure()) buildingsInQueue++;
                    }

                    if (buildingsInQueue < 2)
                    {
                        float coreCost = 99999f;
                        Building b = null;
                        foreach (Building building in BuildingsCanBuild)
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            //if you dont want it to be built put it here.
                            //this first if is the low pri build spot. 
                            //the second if will override items that make it through this if. 
                            if (((building.MinusFertilityOnBuild <= 0.0f || Owner.data.Traits.Cybernetic > 0) &&
                                 !(building.Name == "Biospheres"))
                                && (building.PlusTerraformPoints < 0 ||
                                    !haveTerraformer && (Fertility < 1.0 && Owner.data.Traits.Cybernetic <= 0))
                                    
                            )
                            {
                                b = building;
                                coreCost = b.Cost;
                                break;
                            }
                            else if (building.Cost < coreCost &&
                                     ((building.Name != "Biospheres" && building.PlusTerraformPoints <= 0) ||
                                      Population / MaxPopulation <= 0.25 && DevelopmentLevel > 2 && !noMoreBiospheres))
                            {
                                b = building;
                                coreCost = b.Cost;
                            }
                        }
                        //if you want it to be built with priority put it here.
                        if (b != null && 
                            ( b.PlusFlatProductionAmount > 0 || b.PlusProdPerRichness > 0 || b.PlusProdPerColonist > 0
                                || b.PlusFoodPerColonist > 0 || b.PlusFlatFoodAmount > 0
                                || b.CreditsPerColonist > 0 || b.PlusTaxPercentage > 0
                            )) 
                        {
                                AddBuildingToCQ(b, false);
                        }
                        //if it must be built with high pri put it here. 
                        else if (b != null)
                        {
                                AddBuildingToCQ(b);
                        }
                        else if (Owner.GetBDict()["Biospheres"] && MineralRichness >= 1.0f &&
                                 ((Owner.data.Traits.Cybernetic > 0 && GrossProductionPerTurn > Consumption) ||
                                  Owner.data.Traits.Cybernetic <= 0 && Fertility >= 1.0))
                        {
                            if (Owner == Empire.Universe.PlayerEmpire)
                            {
                                if (Population / (MaxPopulation + MaxPopBonus) > 0.94999f &&
                                    (Owner.EstimateIncomeAtTaxRate(Owner.data.TaxRate) -
                                     ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f ||
                                     Owner.Money > Owner.GrossTaxes * 3))
                                    TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                            }
                            else if (Population / (MaxPopulation + MaxPopBonus) > 0.94999f &&
                                     (Owner.EstimateIncomeAtTaxRate(0.5f) -
                                      ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f ||
                                      Owner.Money > Owner.GrossTaxes * 3))
                                TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem());
                        }
                    }
                    break;
                }
                #endregion

                case Planet.ColonyType.Industrial:

                    #region Industrial
                    FarmerPercentage = 0.0f;
                    WorkerPercentage = 1f;
                    ResearcherPercentage = 0.0f;
                    float IndySurplus =
                        (NetFoodPerTurn) * (1 - (FoodHere + 1) / (MaxStorage + 1));
                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        IndySurplus = GrossProductionPerTurn - Consumption;
                        IndySurplus = IndySurplus * (1 - (FoodHere + 1) / (MaxStorage + 1));                        
                    }                    
                {
                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);
                    FarmerPercentage *= (FoodHere / MaxStorage) > .25 ? .5f : 1;
                    if (FarmerPercentage == 1 && lotsInQueueToBuild)
                        FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                    WorkerPercentage =
                        (1f - FarmerPercentage) 
                        * (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));
                    if (ProductionHere / MaxStorage > .75 && !lotsInQueueToBuild)
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1 - FarmerPercentage - WorkerPercentage; // 0.0f;
                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }
                }
                SetExportState(colonyType);

                //Try and build some basic infrastructure
                if (buildingsInQueue < 2)
                {
                    if (BuildBasicInfrastructure()) buildingsInQueue++;
                }

                if (buildingsInQueue < 2f)
                {
                    float indycost = 99999f;
                    Building b = null;
                    foreach (Building building in BuildingsCanBuild) //.OrderBy(cost=> cost.Cost))
                    {
                        if (!WeCanAffordThis(building, colonyType))
                            continue;
                        if (building.PlusFlatProductionAmount > 0.0f
                            || building.PlusProdPerColonist > 0.0f
                            || building.PlusProdPerRichness > 0.0f ) 
                        {
                            indycost = building.Cost;
                            b = building;
                            break;
                        }
                        else if (indycost > building.Cost) //building.Name!="Biospheres" || developmentLevel >2 )
                            indycost = building.Cost;
                        b = building;
                    }
                    if (b != null)
                    {
                        AddBuildingToCQ(b);
                        ++buildingsInQueue;
                    }
                }
                break;

                #endregion

                case Planet.ColonyType.Research:

                    #region Research
                    
                    FarmerPercentage = 0.0f;
                    WorkerPercentage = 0.0f;
                    ResearcherPercentage = 1f;
                  
                    ForgetReseachAndBuild = notResearching; 
                    IndySurplus = (NetFoodPerTurn) * ((MaxStorage - FoodHere * 2f) / MaxStorage);
                    
                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                    WorkerPercentage = (1f - FarmerPercentage);

                    if (lotsInQueueToBuild)
                        WorkerPercentage *= ((MaxStorage - ProductionHere) / MaxStorage) / DevelopmentLevel;
                    else
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }


                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }
                    SetExportState(colonyType);

                    //Try and build some basic infrastructure
                    if (buildingsInQueue < 2)
                    {
                        if (BuildBasicInfrastructure()) buildingsInQueue++;
                    }

                    if (buildingsInQueue < 2.0)
                    {
                        Building b = null;
                        float currentBestCost = 99999f;
                        foreach (Building building in BuildingsCanBuild)    //This will basically build anything?!
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;

                            if (buildingsInQueue < 2 && building.Cost < currentBestCost &&
                                     (building.Name != "Biospheres" ||
                                      (buildingsInQueue == 0 && DevelopmentLevel > 2 && !noMoreBiospheres)))
                        
                            {
                                currentBestCost = building.Cost;
                                b = building;
                                buildingsInQueue++;
                            }

                            if (b != null && buildingsInQueue < 2) 
                            {
                                AddBuildingToCQ(b);
                                buildingsInQueue++;
                            }
                        }
                    }
                    break;

                #endregion

                case Planet.ColonyType.Agricultural:

                    #region Agricultural


                    FarmerPercentage = 1f;
                    WorkerPercentage = 0.0f;
                    ResearcherPercentage = 0.0f;

                    SetExportState(colonyType);
                    lotsInQueueToBuild = ConstructionQueue.Where(building =>
                                                  building.isBuilding || (building.Cost > NetProductionPerTurn * 10))
                                              .Count() > 0;
                    ForgetReseachAndBuild = notResearching; //? 1 : .5f;
                    IndySurplus = (NetFoodPerTurn) * ((MaxStorage - FoodHere) / MaxStorage);

                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                    WorkerPercentage = (1f - FarmerPercentage);

                    if (lotsInQueueToBuild)
                        WorkerPercentage *= ((MaxStorage - ProductionHere) / MaxStorage);
                    else
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }

                    //Try and build some basic infrastructure
                    if (buildingsInQueue < 2)
                    {
                        if (BuildBasicInfrastructure()) buildingsInQueue++;
                    }

                    if (buildingsInQueue < 2.0f)
                    {
                        Building b = null;
                        float num1 = 99999f;
                        foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;

                            if (building.Cost < num1 && (building.Name == "Biospheres" && !noMoreBiospheres))
                            {
                                num1 = building.Cost;
                                b = building;
                            }
                            else if (building.Cost < num1 &&
                                     (building.Name != "Biospheres" ||
                                      (buildingsInQueue == 0 && DevelopmentLevel > 2 && !noMoreBiospheres)))

                            {
                                num1 = building.Cost;
                                b = building;
                            }
                        }
                        if (b != null) AddBuildingToCQ(b);
                    }
                    break;

                #endregion

                case Planet.ColonyType.Military:

                    #region Military                        

                    FarmerPercentage = 0.0f;
                    WorkerPercentage = 1f;
                    ResearcherPercentage = 0.0f;
                    if (FoodHere <= Consumption)
                    {
                        FarmerPercentage = CalculateFarmerPercentForSurplus(0.01f);
                        WorkerPercentage = 1f - FarmerPercentage;
                    }

                    WorkerPercentage = (1f - FarmerPercentage);

                    if (lotsInQueueToBuild)
                        WorkerPercentage *= ((MaxStorage - ProductionHere) / MaxStorage);
                    else
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }

                    if (!Owner.isPlayer && FS == GoodState.STORE)
                    {
                        FS = GoodState.IMPORT;
                        PS = GoodState.IMPORT;
                    }
                    SetExportState(colonyType);                    
                    float buildingCount = 0.0f;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding)
                        {
                            ++buildingCount;
                            if (queueItem.Building.Name == "Biospheres")
                                ++buildingCount;
                        }
                    }

                    //If we can build a shipyard, but dont have one, build it
                    BuildShipywardifAble();

                    //Try and build some basic infrastructure
                    if (buildingCount < 2)
                    {
                        if (BuildBasicInfrastructure()) buildingCount++;
                    }

                    {
                        Building b = null;
                        float num1 = 99999f;
                        foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;

                            if (building.Cost < num1 && (building.Name == "Biospheres" && !noMoreBiospheres))
                            {
                                num1 = building.Cost;
                                b = building;
                            }
                            else if (building.Cost < num1 &&
                                     (building.Name != "Biospheres" ||
                                      (buildingCount == 0 && DevelopmentLevel > 2 && !noMoreBiospheres)))
                            {
                                num1 = building.Cost;
                                b = building;
                            }
                        }
                        if (b != null) AddBuildingToCQ(b);

                    }

                    //Added by McShooterz: Colony build troops

                    if (Owner.isPlayer && colonyType == ColonyType.Military)
                    {
                        bool addTroop = false;
                        foreach (PlanetGridSquare planetGridSquare in TilesList)
                        {
                            if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops)
                            {
                                addTroop = true;
                                break;
                            }
                        }
                        if (addTroop && AllowInfantry)
                        {
                            foreach (string troopType in ResourceManager.TroopTypes)
                            {
                                if (!Owner.WeCanBuildTroop(troopType))
                                    continue;
                                QueueItem qi = new QueueItem();
                                qi.isTroop = true;
                                qi.troopType = troopType;
                                qi.Cost = ResourceManager.GetTroopCost(troopType);
                                qi.productionTowards = 0f;
                                qi.NotifyOnEmpty = false;
                                ConstructionQueue.Add(qi);
                                break;
                            }
                        }
                    }

                    break;

                    #endregion

                    //This used to be the TradeHub Governor code. Leaving this here so I can look at it later if I need. -Gretman
                    if (false)
                    {
                        FarmerPercentage = 0.0f;
                        WorkerPercentage = 1f;
                        ResearcherPercentage = 0.0f;
                        PS = ProductionHere >= 20 ? Planet.GoodState.EXPORT : Planet.GoodState.IMPORT;
                        float IndySurplus2 =
                            (NetFoodPerTurn) *
                            (1 - (FoodHere + 1) / (MaxStorage + 1));
                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            IndySurplus = GrossProductionPerTurn - Consumption;
                            IndySurplus = IndySurplus * (1 - (FoodHere + 1) / (MaxStorage + 1));
                        }

                        {
                            FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus2);
                            if (FarmerPercentage == 1 && lotsInQueueToBuild)
                                FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                            WorkerPercentage =
                                (1f - FarmerPercentage)
                                * (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));
                            if (ProductionHere / MaxStorage > .75 && !lotsInQueueToBuild)
                                WorkerPercentage = 0;
                            ResearcherPercentage = 1 - FarmerPercentage - WorkerPercentage; // 0.0f;
                            if (Owner.data.Traits.Cybernetic > 0)
                            {
                                WorkerPercentage += FarmerPercentage;
                                FarmerPercentage = 0;
                            }
                            SetExportState(colonyType);
                        }
                        break;
                    }
            } //End Gov type Switch

            if (ConstructionQueue.Count < 5 && !ParentSystem.CombatInSystem && DevelopmentLevel > 2 &&
                colonyType != ColonyType.Research) 

                #region Troops and platforms

            {

                //Added by McShooterz: build defense platforms

                if (HasShipyard && !ParentSystem.CombatInSystem
                    && (!Owner.isPlayer || colonyType == ColonyType.Military))
                {
                    SystemCommander systemCommander;
                    if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out systemCommander))
                    {
                        float defBudget = Owner.data.DefenseBudget * systemCommander.PercentageOfValue;

                        float maxProd = GetMaxProductionPotential();
                        float platformUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.platform].Upkeep;
                        float stationUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.station].Upkeep;
                        string station = Owner.GetGSAI().GetStarBase();
                        int PlatformCount = 0;
                        int stationCount = 0;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (!queueItem.isShip)
                                continue;
                            if (queueItem.sData.HullRole == ShipData.RoleName.platform)
                            {
                                if (defBudget - platformUpkeep < -platformUpkeep * .5
                                ) 
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                defBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                            if (queueItem.sData.HullRole == ShipData.RoleName.station)
                            {
                                if (defBudget - stationUpkeep < -stationUpkeep)
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                defBudget -= stationUpkeep;
                                stationCount++;
                            }
                        }

                        foreach (Ship platform in Shipyards.Values)
                        {
                            
                            if (platform.AI.State == AIState.Scrap)
                                continue;
                            if (platform.shipData.HullRole == ShipData.RoleName.station )
                            {
                                stationUpkeep = platform.GetMaintCost();
                                if (defBudget - stationUpkeep < -stationUpkeep)
                                {
                                    platform.AI.OrderScrapShip();
                                    continue;
                                }
                                defBudget -= stationUpkeep;
                                stationCount++;
                            }
                            if (platform.shipData.HullRole == ShipData.RoleName.platform
                            ) 
                            {
                                platformUpkeep = platform.GetMaintCost();
                                if (defBudget - platformUpkeep < -platformUpkeep)
                                {
                                    platform.AI.OrderScrapShip();

                                    continue;
                                }
                                defBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                        }

                        if (defBudget > stationUpkeep &&
                            stationCount < (int) (systemCommander.RankImportance * .5f)
                            && stationCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {                            
                            if (!string.IsNullOrEmpty(station))
                            {
                                Ship ship = ResourceManager.ShipsDict[station];
                                if (ship.GetCost(Owner) / GrossProductionPerTurn < 10)
                                    ConstructionQueue.Add(new QueueItem()
                                    {
                                        isShip = true,
                                        sData = ship.shipData,
                                        Cost = ship.GetCost(Owner)
                                    });
                            }
                            defBudget -= stationUpkeep;
                        }
                        if (defBudget > platformUpkeep 
                            && PlatformCount <
                            systemCommander.RankImportance 
                            && PlatformCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {
                            string platform = Owner.GetGSAI().GetDefenceSatellite();
                            if (!string.IsNullOrEmpty(platform))
                            {
                                Ship ship = ResourceManager.ShipsDict[platform];
                                ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ship.shipData,
                                    Cost = ship.GetCost(Owner)
                                });
                            }
                        }
                    }
                }
            }

            #endregion

            #region Scrap

            {
                Array<Building> list1 = new Array<Building>();
                if (Fertility >= 1)
                {
                    foreach (Building building in BuildingList)
                    {
                        if (building.PlusTerraformPoints > 0.0f && building.Maintenance > 0)
                            list1.Add(building);
                    }
                }


                {
                    using (ConstructionQueue.AcquireReadLock())
                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (qitemTest && PGS.QItem.IsPlayerAdded)
                                continue;
                            if (PGS.building != null && PGS.building.IsPlayerAdded)
                                continue;
                            if ((qitemTest && PGS.QItem.Building.Name == "Biospheres") ||
                                (PGS.building != null && PGS.building.Name == "Biospheres"))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatProductionAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatProductionAmount > 0))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatFoodAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatFoodAmount > 0))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatResearchAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatResearchAmount > 0))
                                continue;
                            if (PGS.building != null && !qitemTest && PGS.building.Scrappable &&
                                !WeCanAffordThis(PGS.building, colonyType)
                            ) 
                            {
                                PGS.building.ScrapBuilding(this);
                            }
                            if (qitemTest && !WeCanAffordThis(PGS.QItem.Building, colonyType))
                            {
                                ProductionHere += PGS.QItem.productionTowards;
                                ConstructionQueue.QueuePendingRemoval(PGS.QItem);
                                PGS.QItem = null;
                            }
                        }

                    ConstructionQueue.ApplyPendingRemovals();
                }

                #endregion
            }
        }

        public float GetMaxProductionPotential() { return MaxProductionPerTurn; }

        private float GetMaxProductionPotentialCalc()
        {
            float bonusProd = 0.0f;
            float baseProd = MineralRichness * Population / 1000;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    bonusProd += building.PlusProdPerRichness * MineralRichness;
                bonusProd += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    baseProd += building.PlusProdPerColonist;
            }
            float finalProd = baseProd + bonusProd * Population / 1000;
            if (Owner.data.Traits.Cybernetic > 0)
                return finalProd + Owner.data.Traits.ProductionMod * finalProd - Consumption;
            return finalProd + Owner.data.Traits.ProductionMod * finalProd;
        }

        public float GetMaxResearchPotential =>
            (Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;

        public void ApplyProductionTowardsConstruction() => SbProduction.ApplyProductionTowardsConstruction();

        public void InitializeSliders(Empire o)
        {
            if (o.data.Traits.Cybernetic == 1 || Type == "Barren")
            {
                FarmerPercentage = 0.0f;
                WorkerPercentage = 0.5f;
                ResearcherPercentage = 0.5f;
            }
            else
            {
                FarmerPercentage = 0.55f;
                ResearcherPercentage = 0.2f;
                WorkerPercentage = 0.25f;
            }
        }

        public bool CanBuildInfantry()
        {
            for (int i = 0; i < BuildingList.Count; i++)
            {
                if (BuildingList[i].AllowInfantry)
                    return true;
            }
            return false;
        }

        public void UpdateIncomes(bool LoadUniverse)
        {
            if (Owner == null)
                return;
            PlusFlatPopulationPerTurn = 0f;
            ShieldStrengthMax = 0f;
            TotalMaintenanceCostsPerTurn = 0f;
            float storageAdded = 0;
            AllowInfantry = false;
            TotalDefensiveStrength = 0;
            GrossFood = 0f;
            PlusResearchPerColonist = 0f;
            PlusFlatResearchPerTurn = 0f;
            PlusFlatProductionPerTurn = 0f;
            PlusProductionPerColonist = 0f;
            FlatFoodAdded = 0f;
            PlusFoodPerColonist = 0f;
            PlusFlatPopulationPerTurn = 0f;
            ShipBuildingModifier = 0f;
            CommoditiesPresent.Clear();
            float shipbuildingmodifier = 1f;
            Array<Guid> list = new Array<Guid>();
            float shipyards =1;
            
            if (!LoadUniverse)
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (keyValuePair.Value == null)
                    list.Add(keyValuePair.Key);
                    
                else if (keyValuePair.Value.Active && keyValuePair.Value.shipData.IsShipyard)
                {

                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.ShipyardBonus > 0)
                    {
                        shipbuildingmodifier *= (1 - (GlobalStats.ActiveModInfo.ShipyardBonus / shipyards)); //+= GlobalStats.ActiveModInfo.ShipyardBonus;
                    }
                    else
                    {
                        shipbuildingmodifier *= (1-(.25f/shipyards));
                    }
                    shipyards += .2f;
                }
                else if (!keyValuePair.Value.Active)
                    list.Add(keyValuePair.Key);
            }
            ShipBuildingModifier = shipbuildingmodifier;
            foreach (Guid key in list)
            {
                Shipyards.Remove(key);
            }
            PlusCreditsPerColonist = 0f;
            MaxPopBonus = 0f;
            PlusTaxPercentage = 0f;
            TerraformToAdd = 0f;
            bool shipyard = false;
            RepairPerTurn = 0;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.WinsGame)
                    HasWinBuilding = true;
                //if (building.NameTranslationIndex == 458)
                if (building.AllowShipBuilding || building.Name == "Space Port" )
                    shipyard= true;
                
                PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                PlusCreditsPerColonist += building.CreditsPerColonist;
                TerraformToAdd += building.PlusTerraformPoints;
                TotalDefensiveStrength += building.CombatStrength;
                PlusTaxPercentage += building.PlusTaxPercentage;
                CommoditiesPresent.Add(building.Name);
                if (building.AllowInfantry) AllowInfantry = true;
                storageAdded += building.StorageAdded;
                PlusFoodPerColonist += building.PlusFoodPerColonist;
                PlusResearchPerColonist += building.PlusResearchPerColonist;
                PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                PlusFlatProductionPerTurn += building.PlusProdPerRichness * MineralRichness;
                PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                PlusProductionPerColonist += building.PlusProdPerColonist;
                MaxPopBonus += building.MaxPopIncrease;
                TotalMaintenanceCostsPerTurn += building.Maintenance;
                FlatFoodAdded += building.PlusFlatFoodAmount;
                RepairPerTurn += building.ShipRepair;
                //Repair if no combat
                if(!RecentCombat)
                {
                    building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                    building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
                }
            }
            //Added by Gretman -- This will keep a planet from still having sheilds even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            if (shipyard && (colonyType != ColonyType.Research || Owner.isPlayer))
                HasShipyard = true;
            else
                HasShipyard = false;
            //Research
            NetResearchPerTurn = (ResearcherPercentage * Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + Owner.data.Traits.ResearchMod * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn - Owner.data.TaxRate * NetResearchPerTurn;
            //Food
            NetFoodPerTurn =  (FarmerPercentage * Population / 1000 * (Fertility + PlusFoodPerColonist)) + FlatFoodAdded;
            GrossFood = NetFoodPerTurn;     //NetFoodPerTurn is finished being calculated in another file...
            //Production
            NetProductionPerTurn = (WorkerPercentage * Population / 1000f * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            NetProductionPerTurn = NetProductionPerTurn + Owner.data.Traits.ProductionMod * NetProductionPerTurn;
            MaxProductionPerTurn = GetMaxProductionPotentialCalc();

            Consumption =  (Population / 1000 + Owner.data.Traits.ConsumptionModifier * Population / 1000);

            if (Owner.data.Traits.Cybernetic > 0)
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * (NetProductionPerTurn - Consumption) ;
            else
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * NetProductionPerTurn;

            GrossProductionPerTurn =  (Population / 1000  * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            GrossProductionPerTurn = GrossProductionPerTurn + Owner.data.Traits.ProductionMod * GrossProductionPerTurn;


            if (Station != null && !LoadUniverse)
            {
                if (!HasShipyard)
                    Station.SetVisibility(false, Empire.Universe.ScreenManager, this);
                else
                    Station.SetVisibility(true, Empire.Universe.ScreenManager, this);
            }
            
            //Money
            GrossMoneyPT = Population / 1000f;
            GrossMoneyPT += PlusTaxPercentage * GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            MaxStorage = storageAdded;
            if (MaxStorage < 10) MaxStorage = 10f;
        }

        private void HarvestResources()
        {
            Unfed = SbCommodities.HarvestFood();
            SbCommodities.BuildingResources();              //Building resources is unused?
        }

        public float GetGoodAmount(string good) => SbCommodities.GetGoodAmount(good);
        
        private void GrowPopulation()
        {
            if (Owner == null) return;
            
            float normalRepRate = Owner.data.BaseReproductiveRate * Population;
            if ( normalRepRate > Owner.data.Traits.PopGrowthMax * 1000  && Owner.data.Traits.PopGrowthMax != 0 )
                normalRepRate = Owner.data.Traits.PopGrowthMax * 1000f;
            if ( normalRepRate < Owner.data.Traits.PopGrowthMin * 1000 )
                normalRepRate = Owner.data.Traits.PopGrowthMin * 1000f;
            normalRepRate += PlusFlatPopulationPerTurn;
            float adjustedRepRate = normalRepRate + Owner.data.Traits.ReproductionMod * normalRepRate;
            if (Unfed == 0) Population += adjustedRepRate;  //Unfed is calculated so it is 0 if everyone got food (even if just from storage)
            else        //  ^-- This one increases population if there is enough food to feed everyone
                Population += Unfed * 10f;      //So this else would only happen if there was not enough food. <-- This reduces population due to starvation.
            if (Population < 100.0) Population = 100f;      //Minimum population. I guess they wont all dire from starvation
        }

        public void AddGood(string goodId, int amount) => SbCommodities.AddGood(goodId, amount);
        

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

        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT,
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
            SbCommodities   = null;
            TroopManager    = null;
            GeodeticManager = null;
            BasedShips?.Dispose(ref BasedShips);
            Projectiles?.Dispose(ref Projectiles);
            TroopsHere?.Dispose(ref TroopsHere);            
        }
    }
}

