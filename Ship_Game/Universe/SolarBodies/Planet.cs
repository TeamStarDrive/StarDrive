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

        public override string ToString() => $"{Name}  Type:{colonyType}  NetFood:{NetFoodPerTurn}  NetProd:{NetProductionPerTurn}  ImportFood:{ImportFood}  ImportProd:{ImportProd}";

        public GoodState FS = GoodState.STORE;
        public GoodState PS = GoodState.STORE;
        public bool ImportFood => FS == GoodState.IMPORT;
        public bool ImportProd => PS == GoodState.IMPORT;
        public GoodState GetGoodState(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FS;
                case Goods.Production: return PS;
                default:               return 0;
            }
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
        public int StorageAdded;        
        
        
        public float NetFoodPerTurn;
        public float GetNetGoodProd(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return NetFoodPerTurn;
                case Goods.Production: return NetProductionPerTurn;
                default:               return 0;
            }
        }
        public float GetMaxGoodProd(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return NetFoodPerTurn;
                case Goods.Production: return MaxProductionPerTurn;
                default:               return 0;
            }
        }
        public float FoodPercentAdded;
        public float FlatFoodAdded;
        public float NetProductionPerTurn;
        private float MaxProductionPerTurn;
        public float GrossProductionPerTurn;
        public float PlusFlatProductionPerTurn;
        public float NetResearchPerTurn;
        public float PlusTaxPercentage;
        public float PlusFlatResearchPerTurn;
        public float ResearchPercentAdded;
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
        
        public float GetGoodHere(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FoodHere;
                case Goods.Production: return ProductionHere;
                default:               return 0;
            }
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

        public void SetExportWeight(Goods good, float weight)
        {
            switch (good)
            {
                case Goods.Food:       ExportFSWeight = weight; break;
                case Goods.Production: ExportPSWeight = weight; break;
            }   
        }

        public float GetExportWeight(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return ExportFSWeight;
                case Goods.Production: return ExportPSWeight;
                default:               return 0;
            }
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
            if (ImportFood && !ImportProd) return Goods.Food;
            if (ImportProd && !ImportFood) return Goods.Production;

            const float lookahead = 10;
            float predictedFood = FoodHere + IncomingFood + NetFoodPerTurn * lookahead;

            if (predictedFood < 0f) // we will starve!
            {
                if (FindConstructionBuilding(Goods.Food, out QueueItem item))
                {
                    // will the building complete in reasonable time?
                    int turns = NumberOfTurnsUntilCompleted(item);
                    if (turns > lookahead*2)
                        return Goods.Food; // No! We will seriously starve even if this solves starving

                    float foodProduced = item.Building.FoodProduced(this);
                    if (NetFoodPerTurn + foodProduced >= 0f) // this building will solve starving
                        return Goods.Production; // send production to finish it faster!
                }

                // we will definitely starve without food, so plz send food!
                return Goods.Food;
            }

            // we have enough food incoming, so focus on production instead
            float predictedProduction = ProductionHere + IncomingProduction + NetProductionPerTurn * lookahead;

            // We are not starving and we're constructing stuff, so import production
            if (ConstructionQueue.Count > 0)
                return Goods.Production;

            // we are not starving and we are not constructing anything
            // just pick which stockpile is smaller
            return predictedFood < predictedProduction ? Goods.Food : Goods.Production;
        }

        private bool FindConstructionBuilding(Goods goods, out QueueItem item)
        {
            foreach (QueueItem it in ConstructionQueue)
            {
                if (it.isBuilding) switch (goods)
                {
                    case Goods.Food:       if (it.Building.ProducesFood)       { item = it; return true; } break;
                    case Goods.Production: if (it.Building.ProducesProduction) { item = it; return true; } break;
                    case Goods.Colonists:  if (it.Building.ProducesPopulation) { item = it; return true; } break;
                }
            }
            item = null;
            return false;
        }

        private int NumberOfTurnsUntilCompleted(QueueItem item)
        {
            int totalTurns = 0;
            foreach (QueueItem it in ConstructionQueue)
            {
                totalTurns += it.EstimatedTurnsToComplete;
                if (it == item)
                    break;
            }
            return totalTurns;
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
            --CrippledTurns;
            if (CrippledTurns < 0)
                CrippledTurns = 0;
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
            if (GovernorOn)
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
            float num = MaxPopulation / 1000f;
            if (Density <= 0.5f)
            {
                DevelopmentLevel = 1;
                DevelopmentStatus = Localizer.Token(1763);
                if (num >= 2 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if (num >= 2f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 0 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 0.5f && Type == "Barren")
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
                if (num >= 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 2)
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
                if (num >= 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if (num < 5.0)
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

                    if (AddToIncomingTrade(ref IncomingFood,       ship.CargoSpaceMax * (ship.AI.IsFood ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.CargoSpaceMax * (ship.AI.IsProd ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.CargoSpaceMax)) return;
                }
            }
        }

        private float CalculateFarmerPercentForSurplus(float desiredSurplus)
        {
            float Surplus = 0.0f;
            float NoDivByZero = .0000001f;
            if(Owner.data.Traits.Cybernetic >0)
            {

                Surplus = Surplus = (float)((Consumption + desiredSurplus - PlusFlatProductionPerTurn) / ((Population / 1000.0) 
                    * (MineralRichness + PlusProductionPerColonist)) * (1 - Owner.data.TaxRate) + NoDivByZero);

                if (Surplus < .75f)
                {
                    if (Surplus < 0)
                        return 0.0f;
                    return Surplus;
                }
                else
                {
                    return .75f;
                }
            }
            
            if (Fertility == 0.0)
                return 0.0f;
            // replacing while loop with singal fromula, should save some clock cycles

           
            Surplus = (float)((Consumption + desiredSurplus - FlatFoodAdded) / ((Population / 1000.0) 
                * (Fertility + PlusFoodPerColonist) * (1 + FoodPercentAdded) +NoDivByZero));
            if (Surplus < .75f)
            {
                if (Surplus < 0)
                    return 0.0f;
                return Surplus;
            }
            else
            {
                //if you cant reach the desired surplus, produce as much as you can
                return .75f;
            }
        }     

        public Array<Building> GetBuildingsWeCanBuildHere()
        {
            if (Owner == null)
                return new Array<Building>();
            BuildingsCanBuild.Clear();
            bool flag1 = true;
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    flag1 = false;
                    break;
                }
            }
            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (keyValuePair.Value)
                {
                    Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];
                    bool flag2 = true;
                    if(Owner.data.Traits.Cybernetic >0)
                    {
                        if(building1.PlusFlatFoodAmount >0 || building1.PlusFoodPerColonist >0)
                        {
                            continue;
                        }
                    }
                    if (!flag1 && (building1.Name == "Outpost" || building1.Name == "Capital City"))
                        flag2 = false;
                    if (building1.BuildOnlyOnce)
                    {
                        foreach (Planet planet in Owner.GetPlanets())
                        {
                            foreach (Building building2 in planet.BuildingList)
                            {
                                if (planet.Name == building1.Name)
                                {
                                    flag2 = false;
                                    break;
                                }
                            }
                            if (flag2)
                            {
                                foreach (QueueItem queueItem in planet.ConstructionQueue)
                                {
                                    if (queueItem.isBuilding && queueItem.Building.Name == building1.Name)
                                    {
                                        flag2 = false;
                                        break;
                                    }
                                }
                                if (!flag2)
                                    break;
                            }
                            else
                                break;
                        }
                    }
                    if (flag2)
                    {
                        foreach (Building building2 in BuildingList)
                        {
                            if (building2.Name == building1.Name && building1.Name != "Biospheres" && building2.Unique)
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        for (int index = 0; index < ConstructionQueue.Count; ++index)
                        {
                            QueueItem queueItem = ConstructionQueue[index];
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && (building1.Name != "Biospheres" && queueItem.Building.Unique))
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        if(building1.Name == "Biosphers")
                        {
                            foreach(PlanetGridSquare tile in TilesList)
                            {
                                if (!tile.Habitable)
                                    break;
                                flag2 = false;

                            }
                        }
                    }
                    if (flag2)
                        BuildingsCanBuild.Add(building1);
                }
            }
            return BuildingsCanBuild;
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
            //foreach (var pgs in TilesList)
            //{
            //    if (pgs.QItem?.isBuilding  == true && pgs.QItem.Building.Name == UID)
            //        return true;
            //}
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

        public void DoGoverning()
        {
            float income = GrossMoneyPT - TotalMaintenanceCostsPerTurn;
            if (colonyType == Planet.ColonyType.Colony)
                return;
            GetBuildingsWeCanBuildHere();
            Building cheapestFlatfood =
                BuildingsCanBuild.Where(flatfood => flatfood.PlusFlatFoodAmount > 0)
                    .OrderByDescending(cost => cost.Cost).FirstOrDefault();

            Building cheapestFlatprod = BuildingsCanBuild.Where(flat => flat.PlusFlatProductionAmount > 0)
                .OrderByDescending(cost => cost.Cost).FirstOrDefault();
            Building cheapestFlatResearch = BuildingsCanBuild.Where(flat => flat.PlusFlatResearchAmount > 0)
                .OrderByDescending(cost => cost.Cost).FirstOrDefault();
            if (Owner.data.Traits.Cybernetic > 0)
            {
                cheapestFlatfood =
                    cheapestFlatprod; // this.BuildingsCanBuild.Where(flat => flat.PlusProdPerColonist > 0).OrderByDescending(cost => cost.Cost).FirstOrDefault();
            }
            Building pro = cheapestFlatprod;
            Building food = cheapestFlatfood;
            Building res = cheapestFlatResearch;
            bool noMoreBiospheres = true;
            if (income > .05f && !NeedsFood())
                foreach (PlanetGridSquare pgs in TilesList)
                {
                    if (pgs.Habitable)
                        continue;
                    noMoreBiospheres = false;
                    break;
                }
            int buildingsinQueue = ConstructionQueue.Where(isbuilding => isbuilding.isBuilding).Count();
            bool needsBiospheres =
                ConstructionQueue.Where(isbuilding => isbuilding.isBuilding && isbuilding.Building.Name == "Biospheres")
                    .Count() != buildingsinQueue;
            bool
                StuffInQueueToBuild =
                    ConstructionQueue.Count >
                    5; // .Where(building => building.isBuilding || (building.Cost - building.productionTowards > this.ProductionHere)).Count() > 0;
            bool ForgetReseachAndBuild =
                string.IsNullOrEmpty(Owner.ResearchTopic) || StuffInQueueToBuild ||
                (DevelopmentLevel < 3 && (ProductionHere + 1) / (MaxStorage + 1) < .5f);
            if (colonyType == ColonyType.Research && string.IsNullOrEmpty(Owner.ResearchTopic))
            {
                colonyType = ColonyType.Industrial;
            }

            switch (colonyType)
            {
                case Planet.ColonyType.Core:

                    #region Core

                {
                    #region Resource control

                    //Determine Food needs first
                    //if (this.DetermineIfSelfSufficient())

                    #region MyRegion

                    {
                        //this.fs = GoodState.EXPORT;
                        //Determine if excess food

                        float surplus = (NetFoodPerTurn * (string.IsNullOrEmpty(Owner.ResearchTopic) ? 1 : .5f)) *
                                        (1 - (FoodHere + 1) / (MaxStorage + 1));
                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            surplus = GrossProductionPerTurn - Consumption;
                            surplus = surplus * ((string.IsNullOrEmpty(Owner.ResearchTopic) ? 1 : .5f)) *
                                      (1 - (ProductionHere + 1) / (MaxStorage + 1));
                            //(1 - (this.ProductionHere + 1) / (this.MAX_STORAGE + 1));
                        }
                        FarmerPercentage = CalculateFarmerPercentForSurplus(surplus);
                        if (FarmerPercentage == 1 && StuffInQueueToBuild)
                            FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                        if (FarmerPercentage == 1 && StuffInQueueToBuild)
                            FarmerPercentage = .9f;
                        WorkerPercentage =
                            (1f - FarmerPercentage) *
                            (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));

                        float Remainder = 1f - FarmerPercentage;
                        //Research is happening
                        WorkerPercentage = (Remainder * (string.IsNullOrEmpty(Owner.ResearchTopic)
                                                ? 1
                                                : (1 - (ProductionHere) / (MaxStorage))));
                        if (ProductionHere / MaxStorage > .9 && !StuffInQueueToBuild)
                            WorkerPercentage = 0;
                        ResearcherPercentage = Remainder - WorkerPercentage;
                        if (Owner.data.Traits.Cybernetic > 0)
                        {
                            WorkerPercentage += FarmerPercentage;
                            FarmerPercentage = 0;
                        }
                    }

                    #endregion

                    SetExportState(colonyType);

                    if (Owner != Empire.Universe.PlayerEmpire
                        && !Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                        && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard)
                        )
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
                            ConstructionQueue.Add(new QueueItem(this)
                            {
                                isShip = true,
                                sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                                Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                                       UniverseScreen.GamePaceStatic
                            });
                    }

                    #endregion

                    byte num5 = 0;
                    bool flag5 = false;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding && queueItem.Building.Name != "Biospheres")
                            ++num5;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            ++num5;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            flag5 = true;
                    }
                    bool flag6 = true;
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Outpost" || building.Name == "Capital City")
                            flag6 = false;
                        if (building.Name == "Terraformer")
                            flag5 = true;
                    }
                    if (flag6)
                    {
                        bool flag1 = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                            {
                                flag1 = true;
                                break;
                            }
                        }
                        if (!flag1)
                            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"), false);
                    }
                    if (num5 < 2)
                    {
                        GetBuildingsWeCanBuildHere();

                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                pro = null;
                            if (PGS.building != cheapestFlatfood &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                food = cheapestFlatfood;

                            if (PGS.building != cheapestFlatResearch &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                res = cheapestFlatResearch;
                        }

                        Building buildthis = null;
                        buildthis = pro;
                        buildthis = pro ?? food ?? res;

                        if (buildthis != null)
                        {
                            num5++;
                            AddBuildingToCQ(buildthis, false);
                        }
                    }
                    if (num5 < 2)
                    {
                        float coreCost = 99999f;
                        GetBuildingsWeCanBuildHere();
                        Building b = null;
                        foreach (Building building in BuildingsCanBuild)
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            //if you dont want it to be built put it here.
                            //this first if is the low pri build spot. 
                            //the second if will override items that make it through this if. 
                            if (cheapestFlatfood == null && cheapestFlatprod == null &&
                               
                                ((building.MinusFertilityOnBuild <= 0.0f || Owner.data.Traits.Cybernetic > 0) &&
                                 !(building.Name == "Biospheres"))
                                && (building.PlusTerraformPoints < 0 ||
                                    !flag5 && (Fertility < 1.0 && Owner.data.Traits.Cybernetic <= 0))
                                    
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
                                || cheapestFlatfood == b || cheapestFlatprod == b || cheapestFlatResearch == b                                
                            )) 
                        {
                            bool flag1 = true;
                            if (b.BuildOnlyOnce)
                            {
                                for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                {
                                    if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                    {
                                        flag1 = false;
                                        break;
                                    }
                                }
                            }
                            if (flag1)
                                AddBuildingToCQ(b, false);
                        }
                        //if it must be built with high pri put it here. 
                        else if (b != null)
                        {
                            bool flag1 = true;
                            if (b.BuildOnlyOnce)
                            {
                                for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                {
                                    if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                    {
                                        flag1 = false;
                                        break;
                                    }
                                }
                            }
                            if (flag1)
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
                                    TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem(this));
                            }
                            else if (Population / (MaxPopulation + MaxPopBonus) > 0.94999f &&
                                     (Owner.EstimateIncomeAtTaxRate(0.5f) -
                                      ResourceManager.BuildingsDict["Biospheres"].Maintenance > 0.0f ||
                                      Owner.Money > Owner.GrossTaxes * 3))
                                TryBiosphereBuild(ResourceManager.BuildingsDict["Biospheres"], new QueueItem(this));
                        }
                    }

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
                        else if (queueItem1.isBuilding &&
                                 (queueItem1.Building.PlusFlatProductionAmount > 0.0f ||
                                  queueItem1.Building.PlusProdPerColonist > 0.0f ||
                                  queueItem1.Building.Name == "Outpost"))
                        {
                            ConstructionQueue.Remove(queueItem1);
                            ConstructionQueue.Insert(0, queueItem1);
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
                        (NetFoodPerTurn) * 
                        (1 - (FoodHere + 1) / (MaxStorage + 1));
                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        IndySurplus = GrossProductionPerTurn - Consumption;
                        IndySurplus = IndySurplus * (1 - (FoodHere + 1) / (MaxStorage + 1));                        
                    }                    
                {
                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);
                    FarmerPercentage *= (FoodHere / MaxStorage) > .25 ? .5f : 1;
                    if (FarmerPercentage == 1 && StuffInQueueToBuild)
                        FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                    WorkerPercentage =
                        (1f - FarmerPercentage) 
                        * (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));
                    if (ProductionHere / MaxStorage > .75 && !StuffInQueueToBuild)
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1 - FarmerPercentage - WorkerPercentage; // 0.0f;
                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }
                }
                    SetExportState(colonyType);


                    float num6 = 0.0f;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding)
                            ++num6;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            ++num6;
                    }
                    bool flag7 = true;
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Outpost" || building.Name == "Capital City")
                            flag7 = false;
                    }
                    if (flag7)
                    {
                        bool flag1 = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                            {
                                flag1 = true;
                                break;
                            }
                        }
                        if (!flag1)
                            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                    }

                    bool flag8 = false;
                    GetBuildingsWeCanBuildHere();
                    if (num6 < 2)
                    {
                        GetBuildingsWeCanBuildHere();

                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                pro = null;
                            if (PGS.building != cheapestFlatfood &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                food = cheapestFlatfood;

                            if (PGS.building != cheapestFlatResearch &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                res = cheapestFlatResearch;
                        }
                        Building buildthis = null;
                        buildthis = pro;
                        buildthis = pro ?? food ?? res;

                        if (buildthis != null)
                        {
                            num6++;
                            AddBuildingToCQ(buildthis);
                        }
                    }
                {
                    double num1 = 0;
                    foreach (Building building1 in BuildingsCanBuild)
                    {
                        if (building1.PlusFlatProductionAmount > 0.0
                            || building1.PlusProdPerColonist > 0.0
                            || building1.PlusProdPerRichness > 0.0
                        )
                        {
                            foreach (Building building2 in BuildingList)
                            {
                                if (building2 == building1)
                                    ++num1;
                            }
                            flag8 = num1 <= 9;
                            break;
                        }
                    }
                }
                    bool flag9 = true;
                    if (flag8)
                    {
                        using (ConstructionQueue.AcquireReadLock())
                            foreach (QueueItem queueItem in ConstructionQueue)
                            {
                                if (queueItem.isBuilding
                                    && (queueItem.Building.PlusFlatProductionAmount > 0.0
                                        || queueItem.Building.PlusProdPerColonist > 0.0
                                        || queueItem.Building.PlusProdPerRichness > 0.0)
                                )
                                {
                                    flag9 = false;
                                    break;
                                }
                            }
                    }
                    if (flag9 && num6 < 2f)
                    {
                        float indycost = 99999f;
                        Building b = null;
                        foreach (Building building in BuildingsCanBuild) //.OrderBy(cost=> cost.Cost))
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            if (building.PlusFlatProductionAmount > 0.0f
                                || building.PlusProdPerColonist > 0.0f
                                || (building.PlusProdPerRichness > 0.0f
                                )
                            ) //this.WeCanAffordThis(b,this.colonyType) )//
                            {
                                indycost = building.Cost;
                                b = building;
                                break;
                            }
                            else if (indycost > building.Cost) //building.Name!="Biospheres" || developmentLevel >2 )
                                indycost = building.Cost;
                            b = building;
                        }
                        if (b != null
                        ) //(this.GrossMoneyPT - this.TotalMaintenanceCostsPerTurn > 0.0 || (b.CreditsPerColonist > 0 || this.PlusTaxPercentage > 0))) // ((double)this.Owner.EstimateIncomeAtTaxRate(0.25f) - (double)b.Maintenance > 0.0 || this.Owner.Money > this.Owner.GrossTaxes * 3)) //this.WeCanAffordThis(b, this.colonyType)) //
                        {
                            bool flag1 = true;
                            if (b.BuildOnlyOnce)
                            {
                                for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                {
                                    if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                    {
                                        flag1 = false;
                                        break;
                                    }
                                }
                            }
                            if (flag1)
                            {
                                AddBuildingToCQ(b);

                                ++num6;
                            }
                        }
                        
                    }
                    break;

                #endregion

                case Planet.ColonyType.Research:

                    #region Research
                    
                    FarmerPercentage = 0.0f;
                    WorkerPercentage = 0.0f;
                    ResearcherPercentage = 1f;
                  
                    ForgetReseachAndBuild =
                        string.IsNullOrEmpty(Owner.ResearchTopic); 
                    IndySurplus = (NetFoodPerTurn) * ((MaxStorage - FoodHere * 2f) / MaxStorage);
                    
                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                    WorkerPercentage = (1f - FarmerPercentage);

                    if (StuffInQueueToBuild)
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
                    
                    float num8 = 0.0f;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding)
                            ++num8;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            ++num8;
                    }
                    bool flag10 = true;
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Outpost" || building.Name == "Capital City")
                            flag10 = false;
                    }
                    if (flag10)
                    {
                        bool flag1 = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                            {
                                flag1 = true;
                                break;
                            }
                        }
                        if (!flag1)
                            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                    }
                    if (num8 < 2.0)
                    {
                        GetBuildingsWeCanBuildHere();

                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                pro = null;
                            if (PGS.building != cheapestFlatfood &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                food = cheapestFlatfood;

                            if (PGS.building != cheapestFlatResearch &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                res = cheapestFlatResearch;
                        }
                        Building buildthis = null;
                        buildthis = pro;
                        buildthis = pro ?? food ?? res;

                        if (buildthis != null && WeCanAffordThis(buildthis, colonyType))
                        {
                            num8++;
                            AddBuildingToCQ(buildthis);
                        }
                    }
                    if (num8 < 2.0)
                    {
                        GetBuildingsWeCanBuildHere();
                        Building b = null;
                        float num1 = 99999f;
                        foreach (Building building in BuildingsCanBuild)
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            if (building.Name == "Outpost") 
                            {
                        
                                b = building;
                                break;
                            }
                            else if (num8 < 2 && building.Cost < num1 &&
                                     (building.Name != "Biospheres" ||
                                      (num8 == 0 && DevelopmentLevel > 2 && !noMoreBiospheres)))
                        
                            {
                                num1 = building.Cost;
                                b = building;
                                num8++;
                            }

                            if (b != null && num8 < 2) 
                            {
                                bool flag1 = true;

                                if (b.BuildOnlyOnce)
                                {
                                    for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                    {
                                        if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                        {
                                            flag1 = false;
                                            break;
                                        }
                                    }
                                }
                                if (flag1)
                                {
                                    AddBuildingToCQ(b);
                                    num8++;
                                }
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
                    StuffInQueueToBuild = ConstructionQueue.Where(building =>
                                                  building.isBuilding || (building.Cost > NetProductionPerTurn * 10))
                                              .Count() > 0;
                    ForgetReseachAndBuild =
                        string.IsNullOrEmpty(Owner.ResearchTopic); //? 1 : .5f;
                    IndySurplus = (NetFoodPerTurn) * ((MaxStorage - FoodHere) / MaxStorage);

                    FarmerPercentage = CalculateFarmerPercentForSurplus(IndySurplus);

                    WorkerPercentage = (1f - FarmerPercentage);

                    if (StuffInQueueToBuild)
                        WorkerPercentage *= ((MaxStorage - ProductionHere) / MaxStorage);
                    else
                        WorkerPercentage = 0;

                    ResearcherPercentage = 1f - FarmerPercentage - WorkerPercentage;

                    if (Owner.data.Traits.Cybernetic > 0)
                    {
                        WorkerPercentage += FarmerPercentage;
                        FarmerPercentage = 0;
                    }

                    float num9 = 0.0f;

                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (queueItem.isBuilding)
                            ++num9;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            ++num9;

                    }
                    bool flag12 = true;
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Outpost" || building.Name == "Capital City")
                            flag12 = false;

                    }
                    if (flag12)
                    {
                        bool flag1 = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                            {
                                flag1 = true;
                                break;
                            }
                        }
                        if (!flag1)
                            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                    }
                    if (num9 < 2)
                    {
                        GetBuildingsWeCanBuildHere();

                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                pro = null;
                            if (PGS.building != cheapestFlatfood &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                food = cheapestFlatfood;

                            if (PGS.building != cheapestFlatResearch &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                res = cheapestFlatResearch;
                        }
                        Building buildthis = null;
                        buildthis = pro;
                        buildthis = pro ?? food ?? res;

                        if (buildthis != null)
                        {
                            num9++;
                            AddBuildingToCQ(buildthis);
                        }
                    }

                    if (num9 < 2.0f)
                    {
                        GetBuildingsWeCanBuildHere();
                        Building b = null;
                        float num1 = 99999f;
                        foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            if (building.Name == "Outpost") 
                            {
                                float num2 = building.Cost;
                                b = building;
                                break;
                            }
                            else if (building.Cost < num1
                                     && cheapestFlatfood == null
                                     && cheapestFlatprod == null
                                     && cheapestFlatResearch == null &&
                                     (building.Name == "Biospheres" && !noMoreBiospheres))

                            {
                                num1 = building.Cost;
                                b = building;
                            }
                            else if (building.Cost < num1 &&
                                     (building.Name != "Biospheres" ||
                                      (num9 == 0 && DevelopmentLevel > 2 && !noMoreBiospheres)))

                            {
                                num1 = building.Cost;
                                b = building;
                            }
                        }
                        if (b != null
                        ) 
                        {
                            bool flag1 = true;
                            if (b.BuildOnlyOnce)
                            {
                                for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                {
                                    if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                    {
                                        flag1 = false;
                                        break;
                                    }
                                }
                            }
                            if (flag1)
                                AddBuildingToCQ(b);
                        }
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

                    if (StuffInQueueToBuild)
                        WorkerPercentage *= ((MaxStorage - ProductionHere) / MaxStorage);
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
                            ++buildingCount;
                        if (queueItem.isBuilding && queueItem.Building.Name == "Biospheres")
                            ++buildingCount;
                    }
                    bool missingOutpost = true;
                    foreach (Building building in BuildingList)
                    {
                        if (building.Name == "Outpost" || building.Name == "Capital City")
                            missingOutpost = false;
                    }
                    if (missingOutpost)
                    {
                        bool hasOutpost = false;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                            {
                                hasOutpost = true;
                                break;
                            }
                        }
                        if (!hasOutpost)
                            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"));
                    }
                    if (Owner != EmpireManager.Player
                        && !Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                        && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard) && GrossMoneyPT > 3.0)
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
                        if (!hasShipyard)
                            ConstructionQueue.Add(new QueueItem(this)
                            {
                                isShip = true,
                                sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                                Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner)
                            });
                    }
                    if (buildingCount < 2.0f)
                    {
                        GetBuildingsWeCanBuildHere();

                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (PGS.building == cheapestFlatprod || qitemTest && PGS.QItem.Building == cheapestFlatprod)
                                pro = null;
                            if (PGS.building != cheapestFlatfood &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatfood))
                                food = cheapestFlatfood;

                            if (PGS.building != cheapestFlatResearch &&
                                !(qitemTest && PGS.QItem.Building == cheapestFlatResearch))
                                res = cheapestFlatResearch;
                        }
                        Building buildthis = null;
                        buildthis = pro;
                        buildthis = pro ?? food ?? res;

                        if (buildthis != null)
                        {
                            buildingCount++;
                            AddBuildingToCQ(buildthis);
                        }


                        GetBuildingsWeCanBuildHere();
                        Building b = null;
                        float num1 = 99999f;
                        foreach (Building building in BuildingsCanBuild.OrderBy(cost => cost.Cost))
                        {
                            if (!WeCanAffordThis(building, colonyType))
                                continue;
                            if (building.Name == "Outpost") 
                            {
                                float num2 = building.Cost;
                                b = building;
                                break;
                            }
                            else if (building.Cost < num1
                                     && cheapestFlatfood == null
                                     && cheapestFlatprod == null
                                     && cheapestFlatResearch == null &&
                                     (building.Name == "Biospheres" && !noMoreBiospheres))
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
                        if (b != null
                        ) 
                        {
                            bool flag1 = true;
                            if (b.BuildOnlyOnce)
                            {
                                for (int index = 0; index < Owner.GetPlanets().Count; ++index)
                                {
                                    if (Owner.GetPlanets()[index].BuildingInQueue(b.Name))
                                    {
                                        flag1 = false;
                                        break;
                                    }
                                }
                            }
                            if (flag1)
                                AddBuildingToCQ(b);
                        }
                    }
                    break;

                #endregion

                case Planet.ColonyType.TradeHub:

                    #region TradeHub

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
                        if (FarmerPercentage == 1 && StuffInQueueToBuild)
                            FarmerPercentage = CalculateFarmerPercentForSurplus(0);
                        WorkerPercentage =
                            (1f - FarmerPercentage) 
                            * (ForgetReseachAndBuild ? 1 : (1 - (ProductionHere + 1) / (MaxStorage + 1)));
                        if (ProductionHere / MaxStorage > .75 && !StuffInQueueToBuild)
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

                #endregion
            }

            if (ConstructionQueue.Count < 5 && !ParentSystem.CombatInSystem && DevelopmentLevel > 2 &&
                colonyType != ColonyType.Research) 

                #region Troops and platforms

            {
                //Added by McShooterz: Colony build troops

                #region MyRegion

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
                            QueueItem qi = new QueueItem(this);
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

                #endregion

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
                                    ConstructionQueue.Add(new QueueItem(this)
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
                                ConstructionQueue.Add(new QueueItem(this)
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

        public bool GoodBuilding (Building b)
        {
            return true;
        }

        public float GetMaxProductionPotential()
        {
            float num1 = 0.0f;
            float num2 = MineralRichness * Population / 1000;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    num1 += building.PlusProdPerRichness * MineralRichness;
                num1 += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    num2 += building.PlusProdPerColonist;
            }
            float num3 = num2 + num1 * Population / 1000;
            float num4 = num3;
            if (Owner.data.Traits.Cybernetic > 0)
                return num4 + Owner.data.Traits.ProductionMod * num4 - Consumption;
            return num4 + Owner.data.Traits.ProductionMod * num4;
        }

        public float GetMaxResearchPotential =>        
            (Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn
            * (1+ ResearchPercentAdded
            + Owner.data.Traits.ResearchMod* NetResearchPerTurn);

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
            StorageAdded = 0;
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
            bool shipyard =false;
            float repairPerTurn = 0;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.WinsGame)
                    HasWinBuilding = true;
                //if (building.NameTranslationIndex == 458)
                if (building.AllowShipBuilding || building.Name == "Space Port" )
                    shipyard= true;
                
                if (building.PlusFlatPopulation > 0)
                    PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                PlusCreditsPerColonist += building.CreditsPerColonist;
                if (building.PlusTerraformPoints > 0)
                    TerraformToAdd += building.PlusTerraformPoints;
                if (building.Strength > 0)
                    TotalDefensiveStrength += building.CombatStrength;
                PlusTaxPercentage += building.PlusTaxPercentage;
                if (building.IsCommodity)
                    CommoditiesPresent.Add(building.Name);
                if (building.AllowInfantry)
                    AllowInfantry = true;
                if (building.StorageAdded > 0)
                    StorageAdded += building.StorageAdded;
                if (building.PlusFoodPerColonist > 0)
                    PlusFoodPerColonist += building.PlusFoodPerColonist;
                if (building.PlusResearchPerColonist > 0)
                    PlusResearchPerColonist += building.PlusResearchPerColonist;
                if (building.PlusFlatResearchAmount > 0)
                    PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                if (building.PlusProdPerRichness > 0)
                    PlusFlatProductionPerTurn += building.PlusProdPerRichness * MineralRichness;
                PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0)
                    PlusProductionPerColonist += building.PlusProdPerColonist;
                if (building.MaxPopIncrease > 0)
                    MaxPopBonus += building.MaxPopIncrease;
                if (building.Maintenance > 0)
                    TotalMaintenanceCostsPerTurn += building.Maintenance;
                FlatFoodAdded += building.PlusFlatFoodAmount;
                repairPerTurn += building.ShipRepair;
                //Repair if no combat
                if(!RecentCombat)
                {
                    building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                    building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
                }
            }
            RepairPerTurn = repairPerTurn;
            //Added by Gretman -- This will keep a planet from still having sheilds even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            if (shipyard && (colonyType != ColonyType.Research || Owner.isPlayer))
                HasShipyard = true;
            else
                HasShipyard = false;
            //Research
            NetResearchPerTurn =  (ResearcherPercentage * Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + ResearchPercentAdded * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + Owner.data.Traits.ResearchMod * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn - Owner.data.TaxRate * NetResearchPerTurn;
            //Food
            NetFoodPerTurn =  (FarmerPercentage * Population / 1000 * (Fertility + PlusFoodPerColonist)) + FlatFoodAdded;
            NetFoodPerTurn = NetFoodPerTurn + FoodPercentAdded * NetFoodPerTurn;
            GrossFood = NetFoodPerTurn;
            //Production
            NetProductionPerTurn =  (WorkerPercentage * Population / 1000f * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            NetProductionPerTurn = NetProductionPerTurn + Owner.data.Traits.ProductionMod * NetProductionPerTurn;
            MaxProductionPerTurn = GetMaxProductionPotential();
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

            Consumption =  (Population / 1000 + Owner.data.Traits.ConsumptionModifier * Population / 1000);
            if(Owner.data.Traits.Cybernetic >0)
            {
                if(Population > 0.1 && NetProductionPerTurn <= 0)
                {

                }
            }
            //Money
            GrossMoneyPT = Population / 1000f;
            GrossMoneyPT += PlusTaxPercentage * GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            MaxStorage = StorageAdded;
            if (MaxStorage < 10)
                MaxStorage = 10f;
        }

        private void HarvestResources()
        {
            Unfed = SbCommodities.HarvestFood();
            SbCommodities.BuildingResources();            
        }

        public float GetGoodAmount(string good) => SbCommodities.GetGoodAmount(good);
        
        private void GrowPopulation()
        {
            if (Owner == null)
                return;
            
            float num1 = Owner.data.BaseReproductiveRate * Population;
            if ( num1 > Owner.data.Traits.PopGrowthMax * 1000  && Owner.data.Traits.PopGrowthMax != 0 )
                num1 = Owner.data.Traits.PopGrowthMax * 1000f;
            if ( num1 < Owner.data.Traits.PopGrowthMin * 1000 )
                num1 = Owner.data.Traits.PopGrowthMin * 1000f;
            float num2 = num1 + PlusFlatPopulationPerTurn;
            float num3 = num2 + Owner.data.Traits.ReproductionMod * num2;
            if ( Math.Abs(Unfed) <= 0 )
            {

                
                Population += num3;
                //if (Population +  num3 > MaxPopulation + MaxPopBonus)
                //    Population = MaxPopulation + MaxPopBonus;
            }
            else
                Population += Unfed * 10f;
            if (Population >= 100.0)
                return;
            Population = 100f;
        }

        public float CalculateGrowth(float EstimatedFoodGain)
        {
            if (Owner != null)
            {
                float num1 = Owner.data.BaseReproductiveRate * Population;
                if ( num1 > Owner.data.Traits.PopGrowthMax)
                    num1 = Owner.data.Traits.PopGrowthMax;
                if ( num1 < Owner.data.Traits.PopGrowthMin)
                    num1 = Owner.data.Traits.PopGrowthMin;
                float num2 = num1 + PlusFlatPopulationPerTurn;
                float num3 = num2 + Owner.data.Traits.ReproductionMod * num2;
                if (Owner.data.Traits.Cybernetic > 0)
                {
                    if (ProductionHere + NetProductionPerTurn - Consumption <= 0.1)
                        return -(Math.Abs(ProductionHere + NetProductionPerTurn - Consumption) / 10f);
                    if (Population < MaxPopulation + MaxPopBonus && Population +  num3 < MaxPopulation + MaxPopBonus)
                        return Owner.data.BaseReproductiveRate * Population;
                }
                else
                {
                    if (FoodHere + NetFoodPerTurn - Consumption <= 0f)
                        return -(Math.Abs(FoodHere + NetFoodPerTurn - Consumption) / 10f);
                    if (Population < MaxPopulation + MaxPopBonus && Population +  num3 < MaxPopulation + MaxPopBonus)
                        return Owner.data.BaseReproductiveRate * Population;
                }
            }
            return 0.0f;
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

