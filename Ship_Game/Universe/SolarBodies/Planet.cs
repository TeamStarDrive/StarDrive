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
            TradeHub
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

        private float PopulationBillion;
        private float MaxPopulationBillion;

        private string ImportsDescr()
        {
            if (!ImportFood && !ImportProd) return "";
            if (ImportFood && !ImportProd) return "(IMPORT FOOD)";
            if (ImportProd && !ImportFood) return "(IMPORT PROD)";
            return "(IMPORT ALL)";
        }
        public override string ToString() => $"{Name} ({Owner?.Name ?? "No Owner"}) T:{colonyType} NET(FD:{GetNetFoodPerTurn().String(1)} PR:{GetNetProductionPerTurn().String(1)}) {ImportsDescr()}";

        public GoodState FS = GoodState.STORE;      //I dont like these names, but changing them will affect a lot of files
        public GoodState PS = GoodState.STORE;
        public bool ImportFood => FS == GoodState.IMPORT;
        public bool ImportProd => PS == GoodState.IMPORT;
        public bool ExportFood => FS == GoodState.EXPORT;
        public bool ExportProd => PS == GoodState.EXPORT;
        private float PopulationPercent
        {
            get
            {
                if (Population + TradeAI.AvgTradingColonists <= 0) return 0;
                return  (Population + TradeAI.AvgTradingColonists ) / MaxPopulation ;
            }
        }

        private GoodState ColonistsTradeState
        {
            get
            {
                if (PopulationBillion <= .2f)
                {
                    if (PopulationPercent > .9f) return GoodState.STORE;
                    if (AvgPopulationGrowth < 0) return GoodState.STORE;
                    return GoodState.IMPORT;
                }

                if (AvgPopulationGrowth <= 0)
                    return GoodState.EXPORT;
                if (PopulationPercent < .5f)
                    return GoodState.IMPORT;
                return GoodState.EXPORT;
            }
        }


        public GoodState GetGoodState(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FS;
                case Goods.Production: return PS;
                case Goods.Colonists:  return ColonistsTradeState;
                default:               return 0;
            }
        }
        public bool IsExporting()
        {
            foreach (Goods good in Enum.GetValues(typeof(Goods)))
            {
                if (GetGoodState(good) == GoodState.EXPORT)
                    return true;
            }
            return false;
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
        public int TotalDefensiveStrength { get; private set; }
        public float GrossMoneyPT;
        public float GrossIncome =>
                    (GrossMoneyPT + GrossMoneyPT * (float)Owner?.data.Traits.TaxMod) * (float)Owner?.data.TaxRate
                    + PlusFlatMoneyPerTurn + (Population / 1000 * PlusCreditsPerColonist);
        public float GrossUpkeep =>
                    (float)(TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn
                    * (double)Owner?.data.Traits.MaintMod);
        public float NetIncome => GrossIncome - GrossUpkeep;
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
        public float RepairPerTurn;

        public float ExportPSWeight;
        public float ExportFSWeight;

        public float TradeIncomingColonists;

        public bool RecentCombat => TroopManager.RecentCombat;
        public float GetDefendingTroopStrength() => TroopManager.GetDefendingTroopStrength();
        public int CountEmpireTroops(Empire us) => TroopManager.CountEmpireTroops(us);
        public int GetDefendingTroopCount() => TroopManager.GetDefendingTroopCount();
        public bool AnyOfOurTroops(Empire us) => TroopManager.AnyOfOurTroops(us);
        public float GetGroundStrength(Empire empire) => TroopManager.GetGroundStrength(empire);
        public int GetPotentialGroundTroops() => TroopManager.GetPotentialGroundTroops();
        public float GetGroundStrengthOther(Empire AllButThisEmpire) => TroopManager.GetGroundStrengthOther(AllButThisEmpire);
        public bool TroopsHereAreEnemies(Empire empire) => TroopManager.TroopsHereAreEnemies(empire);
        public int GetGroundLandingSpots() => TroopManager.GetGroundLandingSpots();
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.GetEmpireTroops(empire, maxToTake);
        public void HealTroops(int healAmount)                                          => TroopManager.HealTroops(healAmount);
        public float AvgPopulationGrowth { get; private set; }
        private static string ExtraInfoOnPlanet = "MerVille"; //This will generate log output from planet Governor Building decisions

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
            TroopManager = new TroopManager(this);
            GeodeticManager = new GeodeticManager(this);

            SbCommodities = new SBCommodities(this);

            SbProduction = new SBProduction(this);
            HasShipyard = false;
            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                AddGood(keyValuePair.Key, 0);


        }

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null)
        {
            var newOrbital = this;
            TroopManager = new TroopManager(this);
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

            float zoneBonus = ((int)sunZone + 1) * .2f * ((int)sunZone + 1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + .9f;
            if (newOrbital.PlanetType == 2 || newOrbital.PlanetType == 6 || newOrbital.PlanetType == 10 ||
                newOrbital.PlanetType == 12 || newOrbital.PlanetType == 15 || newOrbital.PlanetType == 20 ||
                newOrbital.PlanetType == 26)
                scale += 2.5f;

            float planetRadius = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            newOrbital.ObjectRadius = planetRadius;
            newOrbital.OrbitalRadius = ringRadius + planetRadius;
            Vector2 planetCenter = MathExt.PointOnCircle(randomAngle, ringRadius);
            newOrbital.Center = planetCenter;
            newOrbital.Scale = scale;
            newOrbital.PlanetTilt = RandomMath.RandomBetween(45f, 135f);


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

        private void DebugImportFood(float predictedFood, string text) =>
            Empire.Universe?.DebugWin?.DebugLogText($"IFOOD PREDFD:{predictedFood:0.#} {text} {this}", DebugModes.Trade);

        private void DebugImportProd(float predictedFood, string text) =>
            Empire.Universe?.DebugWin?.DebugLogText($"IPROD PREDFD:{predictedFood:0.#} {text} {this}", DebugModes.Trade);



        public Goods ImportPriority()
        {
            // Is this an Import-Export type of planet?
            if (ImportFood && ExportProd) return Goods.Food;
            if (ImportProd && ExportFood) return Goods.Production;

            bool debug = Debugger.IsAttached;
            const int lookahead = 30; // 1 turn ~~ 5 second, 12 turns ~~ 1min, 60 turns ~~ 5min
            float predictedFood = ProjectedFood(lookahead);

            if (predictedFood < 0f) // we will starve!
            {
                if (!FindConstructionBuilding(Goods.Food, out QueueItem item))
                {
                    // we will definitely starve without food, so plz send food!
                    DebugImportFood(predictedFood,"(no food buildings)");
                    return Goods.Food;
                }

                // will the building complete in reasonable time?
                int buildTurns = NumberOfTurnsUntilCompleted(item);
                int starveTurns = TurnsUntilOutOfFood();
                if (buildTurns > (starveTurns + 30))
                {
                    DebugImportFood(predictedFood, $"(build {buildTurns} > starve {starveTurns + 30})");
                    return Goods.Food; // No! We will seriously starve even if this solves starving
                }

                float foodProduced = item.Building.FoodProduced(this);
                if (NetFoodPerTurn + foodProduced >= 0f) // this building will solve starving
                {
                    DebugImportProd(predictedFood, $"(build {buildTurns})");
                    return Goods.Production; // send production to finish it faster!
                }

                // we can't wait until building is finished, import food!
                DebugImportFood(predictedFood, "(build has not enough food)");
                return Goods.Food;
            }

            // we have enough food incoming, so focus on production instead
            float predictedProduction = ProjectedProduction(lookahead);

            // We are not starving and we're constructing stuff
            if (ConstructionQueue.Count > 0)
            {
                // this is taking too long! import production to speed it up
                int totalTurns = NumberOfTurnsUntilCompleted(ConstructionQueue.Last);
                if (totalTurns >= 60)
                {
                    DebugImportProd(predictedFood, "(construct >= 60 turns)");
                    return Goods.Production;
                }

                // only import if we're constructing more than we're producing
                float projectedProd = ProjectedProduction(totalTurns);
                if (projectedProd <= 25f)
                {
                    DebugImportProd(predictedFood, $"(projected {projectedProd:0.#} <= 25)");
                    return Goods.Production;
                }
            }

            // we are not starving and we are not constructing anything
            // just pick which stockpile is smaller
            return predictedFood < predictedProduction ? Goods.Food : Goods.Production;
        }

        public float GetProjectedGood(Goods good, int turns)
        {
            switch (good)
            {
                case Goods.None:
                    return 0;
                case Goods.Production:
                    return ProjectedProduction(turns);
                case Goods.Food:
                    return ProjectedFood(turns);
                case Goods.Colonists:
                    return AvgPopulationGrowth * turns + Population;
            }
            return 0;
        }

        private const int NEVER = 10000;

        private float AvgIncomingFood => IncomingFood; // @todo Estimate this better
        private float AvgIncomingProd => IncomingProduction;

        private float AvgFoodPerTurn => GetNetFoodPerTurn() + AvgIncomingFood;
        private float AvgProdPerTurn => GetNetProductionPerTurn() + AvgIncomingProd;

        private int TurnsUntilOutOfFood()
        {
            if (Owner.data.Traits.Cybernetic == 1)
                return NEVER;

            float avg = AvgFoodPerTurn;
            if (avg > 0f) return NEVER;
            return (int)Math.Floor(FoodHere / Math.Abs(avg));
        }

        private float ProjectedFood(int turns)
        {
            float incomingAvg = IncomingFood;
            float netFood = NetFoodPerTurn;
            return FoodHere + incomingAvg + netFood * turns;
        }

        private float ProjectedProduction(int turns)
        {
            float incomingAvg = IncomingProduction;
            float netProd = GetNetProductionPerTurn();
            netProd = ConstructionQueue.Count > 0 ? Math.Min(0,netProd) : netProd;
            return ProductionHere + incomingAvg + netProd * turns;
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

        public int TotalTurnsInConstruction => ConstructionQueue.Count > 0 ? NumberOfTurnsUntilCompleted(ConstructionQueue.Last) : 0;

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
            * (1 + MineralRichness)
            * (float)Math.Ceiling(MaxPopulation / 1000)
            );

        public bool NeedsFood()
        {
            if (Owner?.isFaction ?? true) return false;
            bool cyber = Owner.data.Traits.Cybernetic > 0;
            float food = cyber ? ProductionHere : FoodHere;
            bool badProduction = cyber ? NetProductionPerTurn <= 0 && WorkerPercentage > .5f :
                (NetFoodPerTurn <= 0 && FarmerPercentage > .5f);
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

                return NetFoodPerTurn - Consumption; //This is already the correct value. whats incorrect is
            //how the variable is named at initial assignment.
            //it should be gross food. That will take a lot of invasive work to fix.
            //but its not correct in the UI... needs more checking and fixing. bleh.
        }

        public void ApplyAllStoredProduction(int index) => SbProduction.ApplyAllStoredProduction(index);

        public bool ApplyStoredProduction(int index) => SbProduction.ApplyStoredProduction(index);

        public void ApplyProductiontoQueue(float howMuch, int whichItem) => SbProduction.ApplyProductiontoQueue(howMuch, whichItem);

        public float GetNetProductionPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetProductionPerTurn - Consumption;
            return NetProductionPerTurn;
        }

        public bool TryBiosphereBuild(Building b, QueueItem qi) => SbProduction.TryBiosphereBuild(b, qi);

        public void Update(float elapsedTime)
        {
            Guid[] keys = Shipyards.Keys.ToArray();
            for (int x = 0; x < keys.Length; x++)
            {
                Guid key = keys[x];
                Ship shipyard = Shipyards[key];
                if (shipyard == null || !shipyard.Active //Remove this null check later.
                                     || shipyard.Size == 0)
                    Shipyards.Remove(key);
            }
            if (!Habitable)
            {
                UpdatePosition(elapsedTime);
                return;
            }
            TroopManager.Update(elapsedTime);
            GeodeticManager.Update(elapsedTime);

            for (int index1 = 0; index1 < BuildingList.Count; ++index1)
            {
                //try
                {
                    Building building = BuildingList[index1];
                    if (!building.isWeapon)
                        continue;
                    building.WeaponTimer -= elapsedTime;
                    if (building.WeaponTimer < 0 && ParentSystem.ShipList.Count > 0)
                    {
                        if (Owner == null) continue;
                        Ship target = null;
                        Ship troop = null;
                        float currentD = 0;
                        float previousD = building.TheWeapon.Range + 1000f;
                        //float currentT = 0;
                        float previousT = building.TheWeapon.Range + 1000f;
                        //this.system.ShipList.thisLock.EnterReadLock();
                        for (int index2 = 0; index2 < ParentSystem.ShipList.Count; ++index2)
                        {
                            Ship ship = ParentSystem.ShipList[index2];
                            if (ship.loyalty == Owner || (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact))
                                continue;
                            currentD = Vector2.Distance(Center, ship.Center);
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
                        if (target != null)
                        {
                            building.TheWeapon.Center = Center;
                            building.TheWeapon.FireFromPlanet(this, target);
                            building.WeaponTimer = building.TheWeapon.fireDelay;
                            break;
                        }
                    }
                }
            }
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
            PopulationBillion = Population / 1000;
            MaxPopulationBillion = (MaxPopulation + MaxPopBonus) / 1000;

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
            HealTroops(2);
            RepairBuildings(1);

            CalculateIncomingTrade();
        }

        public float IncomingFood;
        public float IncomingProduction;
        public float IncomingColonists;

        public void UpdateDevelopmentStatus()
        {
            if (PopulationBillion <= 0.5f)
            {
                DevelopmentLevel = 1;
                DevelopmentStatus = Localizer.Token(1763);
                if (MaxPopulationBillion >= 2 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if (MaxPopulationBillion >= 2f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (MaxPopulationBillion < 0 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if (MaxPopulationBillion < 0.5f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (PopulationBillion > 0.5f && PopulationBillion <= 2)
            {
                DevelopmentLevel = 2;
                DevelopmentStatus = Localizer.Token(1768);
                if (MaxPopulationBillion >= 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if (MaxPopulationBillion < 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1770);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (PopulationBillion > 2.0 && PopulationBillion <= 5.0)
            {
                DevelopmentLevel = 3;
                DevelopmentStatus = Localizer.Token(1771);
                if (MaxPopulationBillion >= 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if (MaxPopulationBillion < 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1773);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (PopulationBillion > 5.0 && PopulationBillion <= 10.0)
            {
                DevelopmentLevel = 4;
                DevelopmentStatus = Localizer.Token(1774);
            }
            else if (PopulationBillion > 10.0)
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

        public TradeAI TradeAI => SbCommodities.Trade;

        private void CalculateIncomingTrade()
        {
            if (Owner == null || Owner.isFaction) return;
            IncomingProduction = 0;
            IncomingFood = 0;
            TradeIncomingColonists = 0;
            TradeAI.ClearHistory();
            using (Owner.GetShips().AcquireReadLock())
            {
                foreach (var ship in Owner.GetShips())
                {
                    if (ship.DesignRole != ShipData.RoleName.freighter) continue;
                    if (ship.AI.State != AIState.SystemTrader && ship.AI.State != AIState.PassengerTransport) continue;
                    if (ship.AI.OrderQueue.IsEmpty) continue;

                    TradeAI.AddTrade(ship);
                }
            }
            TradeAI.ComputeAverages();
            IncomingFood       = TradeAI.AvgTradingFood;
            IncomingProduction = TradeAI.AvgTradingProduction;
            IncomingColonists  = TradeAI.AvgTradingColonists;
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

        public bool BuildingExists(string buildingName)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
                if (BuildingList[i].Name == buildingName)
                    return true;
            return BuildingInQueue(buildingName);

        }

        public bool WeCanAffordThis(Building building, ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            float buildingMaintenance = Owner.GetTotalBuildingMaintenance();
            float grossTaxes = Owner.GrossTaxes;

            bool itsHere = BuildingList.Contains(building);

            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
                    if (queueItem.Building == building) itsHere = true;
                }

            }
            buildingMaintenance += building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod;

            bool LowPri = buildingMaintenance / grossTaxes < .25f;
            bool MedPri = buildingMaintenance / grossTaxes < .60f;
            bool HighPri = buildingMaintenance / grossTaxes < .80f;
            float income = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);
            float maintCost = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - building.Maintenance - (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);
            bool makingMoney = maintCost > 0;

            int defensiveBuildings = BuildingList.Count(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded > 0 || combat.TheWeapon != null);
            int possibleoffensiveBuilding = BuildingsCanBuild.Count(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.TheWeapon != null);
            bool isdefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon;
            float defenseratio = 0;
            if (defensiveBuildings + possibleoffensiveBuilding > 0)
                defenseratio = (defensiveBuildings + 1) / (float)(defensiveBuildings + possibleoffensiveBuilding + 1);
            SystemCommander SC;
            bool needDefense = false;

            if (Owner.data.TaxRate > .5f)
                makingMoney = false;
            //dont scrap buildings if we can use treasury to pay for it.
            if (building.AllowInfantry && !BuildingList.Contains(building) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SC))
            {
                if (makingMoney)
                    needDefense = SC.RankImportance >= defenseratio * 10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }

            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == Type)
                return false;


            if (itsHere && building.Unique && (makingMoney || building.Maintenance < Owner.Money * .001))
                return true;

            if (building.PlusTaxPercentage * GrossMoneyPT >= building.Maintenance
                || building.CreditsProduced(this) >= building.Maintenance


                )
                return true;
            if (building.Name == "Outpost" || building.WinsGame)
                return true;
            //dont build +food if you dont need to

            if (Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (NetFoodPerTurn > 0 && FarmerPercentage < .3 || BuildingExists(building.Name))

                    return false;
                return true;

            }
            if (Owner.data.Traits.Cybernetic < 1 && income > building.Maintenance)
            {
                float food = building.FoodProduced(this);
                if (food * FarmerPercentage > 1)
                {
                    return true;
                }
            }
            if (Owner.data.Traits.Cybernetic > 0)
            {
                if (NetProductionPerTurn - Consumption < 0)
                {
                    if (building.PlusFlatProductionAmount > 0 && (WorkerPercentage > .5 || income > building.Maintenance * 2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * (Population / 1000) > building.Maintenance * (2 - WorkerPercentage))
                    {
                        if (income > ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * MineralRichness > building.Maintenance)
                        return true;
                }
            }
            if (building.PlusTerraformPoints > 0)
            {
                if (!makingMoney || Owner.data.Traits.Cybernetic > 0 || BuildingList.Contains(building) || BuildingInQueue(building.Name))
                    return false;

            }
            if (!makingMoney || DevelopmentLevel < 3)
            {
                if (building.Name == "Biospheres")
                    return false;
            }

            bool iftrue = false;
            switch (governor)
            {
                case ColonyType.Agricultural:
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
                                building.Name == "Biospheres" ||
                                (building.PlusTerraformPoints > 0 && Fertility < 3)
                                || building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0
                                || DevelopmentLevel > 3
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                || (needDefense && isdefensive)

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
                                || (Owner.data.Traits.Cybernetic <= 0 && (building.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                                || (Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)
                                || (Owner.data.Traits.Cybernetic <= 0 && building.PlusFoodPerColonist > 0)
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && (Population / 1000) > 1)
                                //|| building.Name == "Biospheres"

                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 3 && makingMoney)
                        {
                            if (DevelopmentLevel > 2 && needDefense && (building.TheWeapon != null || building.Strength > 0))
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
                                || (Owner.data.Traits.Cybernetic <= 0 && Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.StorageAdded > 0
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 2 && makingMoney)
                        {
                            if (building.PlusResearchPerColonist * (Population / 1000) > building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                            || (Owner.data.Traits.Cybernetic <= 0 && building.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation && MaxPopulation > 2000 && income > building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0)
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                )

                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 3 && makingMoney && income > building.Maintenance)
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
                                || (building.AllowShipBuilding && GrossProductionPerTurn > 1)
                                || (building.ShipRepair > 0 && GrossProductionPerTurn > 1)
                                || building.Strength > 0
                                || (building.AllowInfantry && GrossProductionPerTurn > 1)
                                || needDefense && (building.TheWeapon != null || building.Strength > 0)
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
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusResearchPerColonist > 0
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0))
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;

                        }
                        if (MedPri && DevelopmentLevel > 3 && makingMoney)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                            || Owner.data.Traits.Cybernetic <= 0 && ((building.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * .5f && MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0))
                                )
                                return true;
                        }
                        if (LowPri && DevelopmentLevel > 4 && makingMoney)
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

        private void DetermineFoodState(float importThreshold, float exportThreshold)
        {
            if (Owner.data.Traits.Cybernetic != 0) return;

            if (Owner.NumPlanets == 1)
            {
                FS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (FlatFoodAdded > PopulationBillion)     //Account for possible overproduction from FlatFood
            {
                float offsetAmount = (FlatFoodAdded - PopulationBillion) * 0.05f;
                offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
            }

            float ratio = FoodHere / MaxStorage;

            //This will allow a buffer for import / export, so they dont constantly switch between them
            if (ratio < importThreshold) FS = GoodState.IMPORT;                                     //if below importThreshold, its time to import.
            else if (FS == GoodState.IMPORT && ratio >= importThreshold * 2) FS = GoodState.STORE;  //until you reach 2x importThreshold, then switch to Store
            else if (FS == GoodState.EXPORT && ratio <= exportThreshold / 2) FS = GoodState.STORE;  //If we were exporing, and drop below half exportThreshold, stop exporting
            else if (ratio > exportThreshold) FS = GoodState.EXPORT;                                //until we get back to the Threshold, then export
        }

        private void DetermineProdState(float importThreshold, float exportThreshold)
        {
            if (Owner.NumPlanets == 1)
            {
                PS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (PlusFlatProductionPerTurn > 0)
            {
                if (Owner.data.Traits.Cybernetic != 0)  //Account for excess food for the filthy Opteris
                {
                    if (PlusFlatProductionPerTurn > PopulationBillion)
                    {
                        float offsetAmount = (PlusFlatProductionPerTurn - PopulationBillion) * 0.05f;
                        offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                        importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                        exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    }
                }
                else
                {
                    float offsetAmount = PlusFlatProductionPerTurn * 0.05f;
                    offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                    importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                }
            }

            float ratio = ProductionHere / MaxStorage;

            if (ratio < importThreshold) PS = GoodState.IMPORT;
            else if (PS == GoodState.IMPORT && ratio >= importThreshold * 2) PS = GoodState.STORE;
            else if (PS == GoodState.EXPORT && ratio <= exportThreshold / 2) PS = GoodState.STORE;
            else if (ratio > exportThreshold) PS = GoodState.EXPORT;
        }

        private void BuildShipyardifAble()
        {
            if (RecentCombat || !HasShipyard) return;
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
                    ConstructionQueue.Add(new QueueItem(this)
                    {
                        isShip = true,
                        sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                        Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                                UniverseScreen.GamePaceStatic
                    });
            }
        }

        private bool FindOutpost()
        {
            //First check the existing buildings
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Outpost" || building.Name == "Capital City")
                {
                    return true;
                }
            }

            //Then check the queue
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                {
                    return true;
                }
            }
            return false;
        }

        private void BuildOutpostifAble() //A Gretman function to support DoGoverning()
        {
            //Check Existing Buildings and the queue
            if (FindOutpost()) return;

            //Build it!
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

                if (queueItem1.isBuilding && queueItem1.Building.Name == "Outpost")
                {
                    ConstructionQueue.Remove(queueItem1);
                    ConstructionQueue.Insert(0, queueItem1);
                    break;
                }
            }
        }

        private float CalculateFoodWorkers()    //Simply calculates what percentage of workers are needed for farming (between 0.0 and 0.9)
        {
            if (Owner.data.Traits.Cybernetic != 0 || Fertility + PlusFoodPerColonist <= 0.5 || Population == 0) return 0.0f;

            float workers = (Consumption - FlatFoodAdded) / PopulationBillion / (Fertility + PlusFoodPerColonist);
            return workers.Clamped(0.0f, 0.9f);     //Dont allow farmers to consume all labor
        }

        private float CalculateFoodWorkersProjected(float pFlatFood = 0.0f, float pFoodPerCol = 0.0f) //Calculate farmers with these adjustments
        {
            if (Owner.data.Traits.Cybernetic != 0 || Fertility + PlusFoodPerColonist + pFoodPerCol <= 0.5 || Population == 0) return 0.0f;

            float workers = (Consumption - FlatFoodAdded - pFlatFood) / PopulationBillion / (Fertility + PlusFoodPerColonist + pFoodPerCol);
            return workers.Clamped(0.0f, 0.9f);     //Dont allow farmers to consume all labor
        }

        private float CalculateMod(float desiredPercent, float storageRatio)
        {
            float mod = (desiredPercent - storageRatio) * 2;             //Percentage currently over or under desired storage
            if (mod > 0 && mod < 0.05) mod = 0.05f;	//Avoid crazy small percentage
            if (mod < 0 && mod > -0.05) mod = 0.00f;	//Avoid bounce (stop if slightly over)

            return mod;
        }

        //This will calculate a smooth transition to maintain [percent]% of stored food. It will under-farm if over
        //[percent]% of storage, or over-farm if under it. Returns labor needed
        private float FarmToPercentage(float percent)   //Production and Research
        {
            if (MaxStorage == 0 || percent == 0) return 0;
            if (Fertility + PlusFoodPerColonist <= 0.5f || Owner.data.Traits.Cybernetic > 0) return 0; //No farming here, so never mind
            float minFarmers = CalculateFoodWorkers();          //Nominal Farmers needed to neither gain nor lose storage
            float storedFoodRatio = FoodHere / MaxStorage;      //Percentage of Food Storage currently filled

            if (FlatFoodAdded > 0)
            {
                //Stop producing food a little early, since the flat food will continue to pile up
                float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                if (FlatFoodAdded > maxPop) storedFoodRatio += 0.15f * Math.Min(FlatFoodAdded - maxPop, 3);
                storedFoodRatio = storedFoodRatio.Clamped(0, 1);
            }

            minFarmers += CalculateMod(percent, storedFoodRatio).Clamped(-0.35f, 0.50f);             //modify nominal farmers by overage or underage
            minFarmers = minFarmers.Clamped(0, 0.9f);                  //Tame resulting value, dont let farming completely consume all labor
            return minFarmers;                          //Return labor % of farmers to progress toward goal
        }

        private float WorkToPercentage(float percent)   //Production and Research
        {
            if (MaxStorage == 0 || percent == 0) return 0;
            float minWorkers = 0;
            if (Owner.data.Traits.Cybernetic > 0)
            {											//Nominal workers needed to feed all of the the filthy Opteris
                minWorkers = (Consumption - PlusFlatProductionPerTurn) / PopulationBillion / (MineralRichness + PlusProductionPerColonist);
                minWorkers = minWorkers.Clamped(0, 1);
            }

            float storedProdRatio = ProductionHere / MaxStorage;      //Percentage of Prod Storage currently filled

            if (PlusFlatProductionPerTurn > 0)      //Stop production early, since the flat production will continue to pile up
            {
                if (Owner.data.Traits.Cybernetic > 0)
                {
                    float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                    if (PlusFlatProductionPerTurn > maxPop) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn - maxPop, 3);
                }
                else
                {
                    storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn, 3);
                }
                storedProdRatio = storedProdRatio.Clamped(0, 1);
            }

            minWorkers += CalculateMod(percent, storedProdRatio).Clamped(-0.35f, 1.00f);
            minWorkers = minWorkers.Clamped(0, 1);
            return minWorkers;                          //Return labor % to progress toward goal
        }

        private void FillOrResearch(float labor)    //Core and TradeHub
        {
            FarmOrResearch(labor / 2);
            WorkOrResearch(labor / 2);
        }

        private void FarmOrResearch(float labor)   //Agreculture
        {
            if (MaxStorage == 0 || labor == 0) return;
            if (Owner.data.Traits.Cybernetic > 0)
            {
                WorkOrResearch(labor);  //Hand off to Prod instead;
                return;
            }
            float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
            float storedFoodRatio = FoodHere / MaxStorage;      //How much of Storage is filled
            if (Fertility + PlusFoodPerColonist <= 0.5f) storedFoodRatio = 1; //No farming here, so skip it

            //Stop producing food a little early, since the flat food will continue to pile up
            if (FlatFoodAdded > maxPop) storedFoodRatio += 0.15f * Math.Min(FlatFoodAdded - maxPop, 3);
            if (storedFoodRatio > 1) storedFoodRatio = 1;

            float farmers = 1 - storedFoodRatio;    //How much storage is left to fill
            if (farmers >= 0.5f) farmers = 1;		//Work out percentage of [labor] to allocate
            else farmers = farmers * 2;
            if (farmers > 0 && farmers < 0.1f) farmers = 0.1f;    //Avoid crazy small percentage of labor

            FarmerPercentage += farmers * labor;	//Assign Farmers
            ResearcherPercentage += labor - (farmers * labor);//Leftovers go to Research
        }

        private void WorkOrResearch(float labor)    //Industrial
        {
            if (MaxStorage == 0 || labor == 0) return;
            float storedProdRatio = ProductionHere / MaxStorage;      //How much of Storage is filled

            if (Owner.data.Traits.Cybernetic > 0)       //Stop production early, since the flat production will continue to pile up
            {
                float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                if (PlusFlatProductionPerTurn > maxPop) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn - maxPop, 3);
            }
            else
            {
                if (PlusFlatProductionPerTurn > 0) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn, 3);
            }
            if (storedProdRatio > 1) storedProdRatio = 1;

            float workers = 1 - storedProdRatio;    //How much storage is left to fill
            if (workers >= 0.5f) workers = 1;		//Work out percentage of [labor] to allocate
            else workers = workers * 2;
            if (workers > 0 && workers < 0.1f) workers = 0.1f;    //Avoid crazy small percentage of labor

            if (ConstructionQueue.Count > 1 && workers < 0.75f) workers = 0.75f;  //Minimum value if construction is going on

            WorkerPercentage += workers * labor;	//Assign workers
            ResearcherPercentage += labor - (workers * labor);//Leftovers go to Research
        }

        private float LeftoverWorkers()
        {
            //Returns the number of workers (in Billions) that are not assigned to farming.
            return ((1 - CalculateFoodWorkers()) * MaxPopulationBillion);
        }

        private float EvaluateBuildingScrapWeight(Building building, float income)
        {
            float score = 0;
            if (building.Maintenance != 0)
            {
                score += building.Maintenance;
                score -= Owner.data.FlatMoneyBonus * 0.015f;      //Acceptible loss (Note what this will do at high Difficulty)

                //This is where the logic for how bad the planet is doing will go + the value of this planet to the empire and all that.
                //For now, just testing with base of just being able to justify its own Maintenance cost

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Maintenance : Score was {-score}");
            }

            return score;
        }

        private float EvaluateBuildingMaintenance(Building building, float income)
        {
            float score = 0;
            if (building.Maintenance != 0)
            {
                score += building.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
                if (income < building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod)
                    score += score + (building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod);   //Really dont want this if we cant afford it
                score -= Owner.data.FlatMoneyBonus * 0.015f;      //Acceptible loss (Note what this will do at high Difficulty)

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Maintenance : Score was {-score}");
            }

            return score;
        }

        private float EvaluateBuildingFlatFood(Building building)
        {
            float score = 0;
            if (building.PlusFlatFoodAmount != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += ((building.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this will feed, weighted
                    score += 1.5f - (Fertility + (PlusFoodPerColonist / 2));//Bonus for low Effective Fertility
                    if (farmers == 0) score += building.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                    if (farmers > 0.5f) score += farmers - 0.5f;            //Bonus if planet is spending a lot of labor feeding itself
                    if (score < building.PlusFlatFoodAmount * 0.1f) score = building.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                    if (building.PlusFlatFoodAmount + FlatFoodAdded - 0.5f > MaxPopulationBillion) score = 0;   //Dont want this if a lot would go to waste
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatFood : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapFlatFood(Building building)
        {
            float score = 0;
            if (building.PlusFlatFoodAmount != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    float projectedFarmers = CalculateFoodWorkersProjected(pFlatFood: -building.PlusFlatFoodAmount);
                    score += ((building.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this is feeding, weighted
                    score += 1.5f - (Fertility + (PlusFoodPerColonist / 2));         //Bonus for low Effective Fertility
                    if (projectedFarmers == 0) score += building.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                    if (projectedFarmers > 0.5f) score += projectedFarmers - 0.5f;   //Bonus if planet would be spending a lot of labor feeding itself
                    if (score < 0) score = 0;                                        //No penalty for extra food
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} FlatFood : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingFoodPerCol(Building building)
        {
            float score = 0;
            if (building.PlusFoodPerColonist != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * MaxPopulationBillion * 2; //for negative value
                else
                {
                    float projectedFarmers = CalculateFoodWorkersProjected(pFoodPerCol: building.PlusFoodPerColonist);
                    score += building.PlusFoodPerColonist * projectedFarmers * MaxPopulationBillion;  //Food this would create if constructed
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (Fertility + building.PlusFoodPerColonist + PlusFoodPerColonist <= 1.0f) score = 0;     //Dont try to add farming to a planet without enough to sustain itself
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FoodPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapFoodPerCol(Building building)
        {
            float score = 0;
            if (building.PlusFoodPerColonist != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * MaxPopulationBillion * 2; //for negative value
                else
                {
                    score += building.PlusFoodPerColonist * CalculateFoodWorkers() * MaxPopulationBillion;  //Food this is producing
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (Fertility + PlusFoodPerColonist - building.PlusFoodPerColonist <= 1.0f) score = building.PlusFoodPerColonist;     //Dont scrap this if it would drop effective fertility below 1.0
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FoodPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingFlatProd(Building building)
        {
            float score = 0;
            if (building.PlusFlatProductionAmount != 0)
            {
                if (building.PlusFlatProductionAmount < 0) score = building.PlusFlatProductionAmount * 2; //for negative value
                else
                {
                    if (Owner.data.Traits.Cybernetic > 0)
                        score += building.PlusFlatProductionAmount / MaxPopulationBillion;     //Percentage of the filthy Opteris population this will feed
                    score += (0.5f - (PopulationBillion / MaxPopulationBillion)).Clamped(0.0f, 0.5f);   //Bonus if population is currently less than half of max population
                    score += 1.5f - (MineralRichness + (PlusProductionPerColonist / 2));      //Bonus for low richness planets
                    score += (0.66f - MineralRichness).Clamped(0.0f, 0.66f);      //More Bonus for really low richness planets
                    float currentOutput = (MineralRichness + PlusProductionPerColonist) * LeftoverWorkers() + PlusFlatProductionPerTurn;  //Current Prod Output
                    score += (building.PlusFlatProductionAmount / currentOutput).Clamped(0.0f, 2.0f);         //How much more this building will produce compared to labor prod
                    if (score < building.PlusFlatProductionAmount * 0.1f) score = building.PlusFlatProductionAmount * 0.1f; //A little production is always useful
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatProd : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingProdPerCol(Building building)
        {
            float score = 0;
            if (building.PlusProdPerColonist != 0 && PopulationBillion / MaxPopulationBillion >= 0.8f)
            {
                if (building.PlusProdPerColonist < 0) score = building.PlusProdPerColonist * MaxPopulationBillion * 2;
                else
                {
                    score += building.PlusProdPerColonist * LeftoverWorkers();    //Prod this building is contributing
                    if (score < building.PlusProdPerColonist * 0.1f) score = building.PlusProdPerColonist * 0.1f; //A little production is always useful
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ProdPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingProdPerRichness(Building building)
        {
            float score = 0;
            if (building.PlusProdPerRichness != 0)  //This one can produce a pretty high building value, which is normally offset by its huge maintenance cost and Fertility loss
            {
                if (building.PlusProdPerRichness < 0) score = building.PlusProdPerRichness * MineralRichness * 2;
                else
                {
                    score += building.PlusProdPerRichness * MineralRichness;        //Production this would generate
                    if (!HasShipyard) score *= 0.75f;       //Do we have a use for all this production?
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ProdPerRich : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingStorage(Building building)
        {
            float score = 0;
            if (building.StorageAdded != 0)
            {
                float desiredStorage = 70.0f;
                if (Fertility + PlusFoodPerColonist >= 2.5f || MineralRichness + PlusProductionPerColonist >= 2.5f || PlusFlatProductionPerTurn > 5) desiredStorage += 100.0f;  //Potential high output
                if (HasShipyard) desiredStorage += 100.0f;      //For buildin' ships 'n shit
                if (MaxStorage < desiredStorage) score += (building.StorageAdded * 0.002f);  //If we need more storage, rate this building
                if (building.Maintenance > 0) score *= 0.25f;       //Prefer free storage

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} StorageAdd : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingPopulationGrowth(Building building)
        {
            float score = 0;
            if (building.PlusFlatPopulation != 0)
            {
                if (building.PlusFlatPopulation < 0) score = building.PlusFlatPopulation * 0.02f;  //Which is sorta like     0.01f * 2
                else
                {
                    score += (MaxPopulationBillion * 0.02f - 1.0f) + (building.PlusFlatPopulation * 0.01f);        //More desireable on high pop planets
                    if (score < 0) score = 0;     //Dont let this cause a penalty to other building properties
                }
                if (Owner.data.Traits.PhysicalTraitLessFertile) score *= 2;     //These are calculated outside the else, so they will affect negative flatpop too
                if (Owner.data.Traits.PhysicalTraitFertile) score *= 0.5f;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} PopGrowth : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingPlusMaxPopulation(Building building)
        {
            float score = 0;
            if (building.MaxPopIncrease != 0)
            {
                if (building.MaxPopIncrease < 0) score = building.MaxPopIncrease * 0.002f;      //Which is sorta like     0.001f * 2
                else
                {
                    //Basically, only add to the score if we would be able to feed the extra people
                    if ((Fertility + PlusFoodPerColonist + building.PlusFoodPerColonist) * (MaxPopulationBillion + (building.MaxPopIncrease / 1000))
                        >= (MaxPopulationBillion + (building.MaxPopIncrease / 1000) - FlatFoodAdded - building.PlusFlatFoodAmount))
                        score += building.MaxPopIncrease * 0.001f;
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} MaxPop : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingFlatResearch(Building building, float income)
        {
            float score = 0;
            if (building.PlusFlatResearchAmount != 0)
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

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatResearch : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapFlatResearch(Building building, float income)
        {
            float score = 0;
            if (building.PlusFlatResearchAmount != 0)
            {
                if (building.PlusFlatResearchAmount < 0) score += building.PlusFlatResearchAmount * 2;
                else score += building.PlusFlatResearchAmount;


                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} FlatResearch : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingResearchPerCol(Building building)
        {
            float score = 0;
            if (building.PlusResearchPerColonist != 0 && PopulationBillion / MaxPopulationBillion >= 0.8f)
            {
                if (building.PlusResearchPerColonist < 0) score += building.PlusResearchPerColonist * 2;
                else score += building.PlusResearchPerColonist * (LeftoverWorkers() / 2);    //Reasonable extrapolation of how much research this will reliably produce

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ResPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingCreditsPerCol(Building building)
        {
            float score = 0;
            if (building.CreditsPerColonist != 0)
            {
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * MaxPopulationBillion * 2;
                else score += (building.CreditsPerColonist * PopulationBillion) / 2;        //Dont want to cause this to have building preference over infrastructure buildings

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} CredsPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapCreditsPerCol(Building building)
        {
            float score = 0;
            if (building.CreditsPerColonist != 0)
            {
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * MaxPopulationBillion;
                else score += (building.CreditsPerColonist * PopulationBillion) / 2;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} CredsPerCol : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingPlusTaxPercent(Building building, float income)
        {
            float score = 0;
            if (building.PlusTaxPercentage != 0)
            {
                float assumedIncome = MaxPopulationBillion * 0.20f;     //This is an assumed tax value, used only for determining how useful a PlusTaxPercentage building is
                if (building.PlusTaxPercentage < 0) score += building.PlusTaxPercentage * assumedIncome * 2;
                else score += building.PlusTaxPercentage * assumedIncome / 2;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} PlusTaxPercent : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingAllowShipBuilding(Building building)
        {
            float score = 0;
            if (building.AllowShipBuilding || building.Name == "Space Port" && PopulationBillion / MaxPopulationBillion >= 0.75f)
            {
                float prodFromLabor = LeftoverWorkers() * (MineralRichness + PlusProductionPerColonist + building.PlusProdPerColonist);
                float prodFromFlat = PlusFlatProductionPerTurn + building.PlusFlatProductionAmount + (building.PlusProdPerRichness * MineralRichness);
                //Do we have enough production capability to really justify trying to build ships
                if (prodFromLabor + prodFromFlat > 10.0f) score += ((prodFromLabor + prodFromFlat) / 10).Clamped(0.0f, 2.0f);

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} AllowShipBuilding : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingTerraforming(Building building)
        {
            float score = 0;
            if (building.PlusTerraformPoints != 0)
            {
                //Still working on this one...
                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Terraform : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapTerraforming(Building building)
        {
            float score = 0;
            if (building.PlusTerraformPoints != 0)
            {
                if (Fertility >= 1.0f)  score -= building.Maintenance;     //Are we done yet?
                else                    score += building.Maintenance;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} Terraform : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingFertilityLoss(Building building)
        {
            float score = 0;
            if (building.MinusFertilityOnBuild != 0 && Owner.data.Traits.Cybernetic == 0)       //Cybernetic dont care.
            {
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
                else
                {                                   //How much fertility will actually be lost
                    float fertLost = Math.Min(Fertility, building.MinusFertilityOnBuild);
                    float foodFromLabor = MaxPopulationBillion * ((Fertility - fertLost) + PlusFoodPerColonist + building.PlusFoodPerColonist);
                    float foodFromFlat = FlatFoodAdded + building.PlusFlatFoodAmount;
                    //Will we still be able to feed ourselves?
                    if (foodFromFlat + foodFromLabor < Consumption) score += fertLost * 10;
                    else score += fertLost * 4;
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FertLossOnBuild : Score was {score}");
            }

            return score;
        }

        private float EvaluateBuildingScrapFertilityLoss(Building building)
        {
            float score = 0;
            if (building.MinusFertilityOnBuild != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative MinusFertilityOnBuild is reversed if the building is removed.

                //There is no logic for a score penalty due to loss of Fertility... because the damage has already been done  =(

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FertLossOnBuild : Score was {score}");
            }

            return score;
        }

        private int ExistingMilitaryBuildings()
        {
            return BuildingList.Count(building =>
                        building.CombatStrength > 0 &&
                        building.Name != "Outpost" &&
                        building.Name != "Capital City" &&
                        building.MaxPopIncrease == 0);
        }

        private int DesiredMilitaryBuildings()
        {
            //This is a temporary quality rating. This will be replaced by an "importance to the Empire" calculation from the military planner at some point (hopefully soon). -Gretman
            float quality = Fertility + MineralRichness + MaxPopulationBillion / 2;
            if (Fertility > 1.6) quality += 1.5f;
            if (MineralRichness > 1.6) quality += 1.5f;

            return (int)(quality / 5); //Return just the whole number by truncating the decimal
        }

        private float FactorForConstructionCost(float score, float cost, float highestCost)
        {
            //1 minus cost divided by highestCost gives a decimal value that is higher for smaller construction cost. This will make buildings with lower cost more desirable,
            //but never disqualify a building that had a positive score to begin with. -Gretman
            highestCost = highestCost.Clamped(50, 250);
            return score * (1f - cost / highestCost).Clamped(0.001f, 1.0f);
        }

        private float EvaluateBuilding(Building building, float income, float highestCost)     //Gretman function, to support DoGoverning()
        {
            if (Name == "Drell VIfI") Debugger.Break();

            float buildingValue = 0.0f;    //End result value for entire building

            buildingValue -= EvaluateBuildingMaintenance(building, income);
            buildingValue += EvaluateBuildingFlatFood(building);
            buildingValue += EvaluateBuildingFoodPerCol(building);
            buildingValue += EvaluateBuildingFlatProd(building);
            buildingValue += EvaluateBuildingProdPerCol(building);
            buildingValue += EvaluateBuildingProdPerRichness(building);
            buildingValue += EvaluateBuildingStorage(building);
            buildingValue += EvaluateBuildingPopulationGrowth(building);
            buildingValue += EvaluateBuildingPlusMaxPopulation(building);
            buildingValue += EvaluateBuildingFlatResearch(building, income);
            buildingValue += EvaluateBuildingResearchPerCol(building);
            buildingValue += EvaluateBuildingCreditsPerCol(building);
            buildingValue += EvaluateBuildingPlusTaxPercent(building, income);
            buildingValue += EvaluateBuildingAllowShipBuilding(building);
            buildingValue += EvaluateBuildingTerraforming(building);
            buildingValue -= EvaluateBuildingFertilityLoss(building);

            if (buildingValue > 0) buildingValue = FactorForConstructionCost(buildingValue, building.Cost, highestCost);

            if (Name == ExtraInfoOnPlanet) Log.Info(ConsoleColor.Cyan, $"Evaluated {building.Name} Final Score was : {buildingValue}");

            return buildingValue;
        }

        private float EvaluateMilitaryBuilding(Building building, float income)
        {
            float combatScore = (building.Strength + building.Defense + building.CombatStrength + building.SoftAttack + building.HardAttack) / 100f;

            float weaponDPS = 0;
            if (building.isWeapon && !String.IsNullOrEmpty(building.Weapon))
            {
                Weapon theWeapon = ResourceManager.WeaponsDict[building.Weapon];
                weaponDPS = (theWeapon.DamageAmount / theWeapon.fireDelay) / 500;
            }

            float shieldScore = building.PlanetaryShieldStrengthAdded / 1000;

            float allowTroops = 0;
            if (building.AllowInfantry)
            {
                if (colonyType == ColonyType.Military) allowTroops = 1.0f;
                else allowTroops = 0.5f;
            }

            //Shield, weapon, and/or allowtroop weighting go here (which is why they are all seperate values)

            float ratingFactor = (((PopulationBillion / MaxPopulationBillion) - 0.5f) * 2.0f).Clamped(0.0f, 1.0f);  //Factor by current population, so military buildings will be delayed
            float finalRating = (combatScore + weaponDPS + shieldScore + allowTroops) * ratingFactor;

            if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated military building {building.Name} : Score was {finalRating}");
            return finalRating;
        }

        private void ChooseAndBuild(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return;
            Building bestBuilding = null;
            float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
            float buildingScore = 0.0f;
            float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
            for (int i = 0; i < BuildingsCanBuild.Count; i++)
            {
                //Find the building with the highest score
                buildingScore = EvaluateBuilding(BuildingsCanBuild[i], budget, highestCost);
                if (buildingScore > bestValue)
                {
                    bestBuilding = BuildingsCanBuild[i];
                    bestValue = buildingScore;
                }
            }
            if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
            else ChooseAndBuildMilitary(budget);
        }

        private void ChooseAndBuildMilitary(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return;    //Discourage building military buildings too early
            if (ExistingMilitaryBuildings() < DesiredMilitaryBuildings())
            {
                Building bestMBuilding = null;
                float bestValue = 0.0f;
                float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                {
                    Building bldg = BuildingsCanBuild[i];
                    if (bldg.CombatStrength == 0 || bldg.MaxPopIncrease > 0) continue;
                    if (bldg.Name == "Outpost" || bldg.Name == "Capital City") continue;

                    float mBuildingScore = -EvaluateBuildingMaintenance(bldg, budget);
                    mBuildingScore += EvaluateMilitaryBuilding(bldg, budget);
                    mBuildingScore = FactorForConstructionCost(mBuildingScore, bldg.Cost, highestCost);
                    if (mBuildingScore > bestValue)
                    {
                        bestMBuilding = bldg;
                        bestValue = mBuildingScore;
                    }
                }
                if (bestMBuilding != null) AddBuildingToCQ(bestMBuilding);
            }
        }

        private void BuildBuildings(float budget)
        {
            //Do some existing bulding recon
            int openTiles = TilesList.Count(tile => tile.Habitable && tile.building == null);
            int totalbuildings = TilesList.Count(tile => tile.building != null && tile.building.Name != "Biospheres");

            //Construction queue recon
            bool buildingInTheWorks = SbProduction.ConstructionQueue.Any(building => building.isBuilding);
            bool militaryBInTheWorks = SbProduction.ConstructionQueue.Any(building => building.isBuilding && building.Building.CombatStrength > 0);
            bool lotsInQueueToBuild = ConstructionQueue.Count >= 4;


            //New Build Logic by Gretman
            if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

            if (openTiles > 0)
            {
                if (!buildingInTheWorks) ChooseAndBuild(budget);
            }
            else
            {
                bool biosphereInTheWorks = SbProduction.ConstructionQueue.Find(building => building.isBuilding && building.Building.Name == "Biospheres") != null;
                Building bioSphere = BuildingsCanBuild.Find(building => building.Name == "Biospheres");

                if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < budget + 0.3f) //No habitable tiles, and not too much in debt
                    AddBuildingToCQ(bioSphere);
            }

            ScrapBuildings(budget);
        }

        private void ScrapBuildings(float income)
        {
            if (Name == "Cordron Vf") Debugger.Break();

            float buildingValue = 0.0f;
            float costWeight = 0.0f;

            Building bldg = null;

            for (int i = 0; i < BuildingList.Count; i++)
            {
                buildingValue = 0;
                costWeight = 0;
                bldg = BuildingList[i];
                if (bldg.Name == "Biospheres" || !bldg.Scrappable || bldg.IsPlayerAdded) continue;

                costWeight     = EvaluateBuildingScrapWeight(bldg, income);

                buildingValue += EvaluateBuildingScrapFlatFood(bldg);
                buildingValue += EvaluateBuildingScrapFoodPerCol(bldg);
                buildingValue += EvaluateBuildingFlatProd(bldg);
                buildingValue += EvaluateBuildingProdPerCol(bldg);
                buildingValue += EvaluateBuildingProdPerRichness(bldg);
                buildingValue += EvaluateBuildingStorage(bldg);
                buildingValue += EvaluateBuildingPopulationGrowth(bldg);
                buildingValue += EvaluateBuildingPlusMaxPopulation(bldg);
                buildingValue += EvaluateBuildingScrapFlatResearch(bldg, income);
                buildingValue += EvaluateBuildingResearchPerCol(bldg);
                buildingValue += EvaluateBuildingScrapCreditsPerCol(bldg);
                buildingValue += EvaluateBuildingPlusTaxPercent(bldg, income);
                buildingValue += EvaluateBuildingAllowShipBuilding(bldg);
                buildingValue += EvaluateBuildingScrapTerraforming(bldg);
                buildingValue -= EvaluateBuildingScrapFertilityLoss(bldg);  //Yes, -= because it is calculated as negative in the function
                if (bldg.CombatStrength > 0) buildingValue += EvaluateMilitaryBuilding(bldg, income);

                if (buildingValue < costWeight)
                {
                    Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {bldg.Name} on planet {Name}     buildingValue: {buildingValue}    costWeight: {costWeight}");
                    bldg.ScrapBuilding(this);
                    return;     //No mass scrappings
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {bldg.Name}  buildingValue: {buildingValue}    costWeight: {costWeight}");
            }
        }

        private float BuildingBudget()
        {
            //Empire budget will go here instead of planet budget

            float income = MaxPopulationBillion * (Owner.data.TaxRate).Clamped(0.1f, 0.4f);    //If taxes go way up, dont want the governors to go too crazy
            income += income * PlusTaxPercentage;
            income += income * Owner.data.Traits.TaxMod;
            income -= SbProduction.GetTotalConstructionQueueMaintenance();

            return income;
        }

        public void DoGoverning()
        {
            RefreshBuildingsWeCanBuildHere();
            MaxPopulationBillion = (MaxPopulation + MaxPopBonus) / 1000f;
            BuildOutpostifAble();   //If there is no Outpost or Capital, build it

            if (colonyType == ColonyType.Colony) return; //No Governor? Nevermind!

            float budget = BuildingBudget();

            bool notResearching = string.IsNullOrEmpty(Owner.ResearchTopic);
            float foodMinimum = CalculateFoodWorkers();

            //Switch to Industrial if there is nothing in the research queue (Does not actually change assigned Governor)
            if (colonyType == ColonyType.Research && notResearching)
                colonyType = ColonyType.Industrial;

            FarmerPercentage = 0;
            WorkerPercentage = 0;
            ResearcherPercentage = 0;

            switch (colonyType)
            {
                case ColonyType.TradeHub:
                case ColonyType.Core:
                    {
                        //New resource management by Gretman
                        FarmerPercentage = CalculateFoodWorkers();
                        FillOrResearch(1 - FarmerPercentage);

                        if (colonyType == ColonyType.TradeHub)
                        {
                            DetermineFoodState(0.15f, 0.95f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                            DetermineProdState(0.15f, 0.95f);
                            break;
                        }

                        BuildBuildings(budget);

                        DetermineFoodState(0.25f, 0.666f);   //these will evaluate to: Start Importing if stores drop below 25%, and stop importing once stores are above 50%.
                        DetermineProdState(0.25f, 0.666f);   //                        Start Exporting if stores are above 66%, but dont stop exporting unless stores drop below 33%.

                        break;
                    }

                case ColonyType.Industrial:
                    {
                        //Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                        FarmerPercentage = FarmToPercentage(0.333f);
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(1));
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);

                        BuildBuildings(budget);

                        DetermineFoodState(0.50f, 1.0f);     //Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                        DetermineProdState(0.15f, 0.666f);   //Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.

                        break;
                    }

                case ColonyType.Research:
                    {
                        //This governor will rely on imports, focusing on research as long as no one is starving
                        FarmerPercentage = FarmToPercentage(0.333f);    //Farm to a small savings, and prevent starvation
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.333f));        //Save a litle production too
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Otherwise, research!

                        BuildBuildings(budget);

                        DetermineFoodState(0.50f, 1.0f);     //Import if either drops below 50%, and stop importing once stores reach 100%.
                        DetermineProdState(0.50f, 1.0f);     //This planet will only export Food or Prod if there is excess FlatFood or FlatProd

                        break;
                    }

                case ColonyType.Agricultural:
                    {
                        FarmerPercentage = FarmToPercentage(1);     //Farm all you can
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.333f));    //Then work to a small savings
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Otherwise, research!

                        BuildBuildings(budget);

                        DetermineFoodState(0.15f, 0.666f);   //Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                        DetermineProdState(0.50f, 1.000f);   //Start Importing if prod drops below 50%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.

                        break;
                    }

                case ColonyType.Military:    //This on is incomplete
                    {
                        FarmerPercentage = FarmToPercentage(0.5f);     //Keep everyone fed, but dont be desperate for imports
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.5f));    //Keep some prod handy
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Research if bored

                        BuildBuildings(budget);

                        DetermineFoodState(0.4f, 1.0f);     //Import if either drops below 40%, and stop importing once stores reach 80%.
                        DetermineProdState(0.4f, 1.0f);     //This planet will only export Food or Prod due to excess FlatFood or FlatProd

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
                                if (defBudget - platformUpkeep < -platformUpkeep * .5)
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
        }

        public float GetMaxProductionPotential() { return MaxProductionPerTurn; }

        private float GetMaxProductionPotentialCalc()
        {
            float bonusProd = 0.0f;
            float baseProd = MineralRichness * PopulationBillion;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    bonusProd += building.PlusProdPerRichness * MineralRichness;
                bonusProd += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    baseProd += building.PlusProdPerColonist;
            }
            float finalProd = baseProd + bonusProd * PopulationBillion;
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
            }

            TotalDefensiveStrength = (int)TroopManager.GetGroundStrength(Owner); ;

            //Added by Gretman -- This will keep a planet from still having shields even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            if (shipyard && (colonyType != ColonyType.Research || Owner.isPlayer))
                HasShipyard = true;
            else
                HasShipyard = false;
            //Research
            NetResearchPerTurn = (ResearcherPercentage * (Population / 1000)) * PlusResearchPerColonist + PlusFlatResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + Owner.data.Traits.ResearchMod * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn - Owner.data.TaxRate * NetResearchPerTurn;
            //Food
            NetFoodPerTurn =  (FarmerPercentage * (Population / 1000) * (Fertility + PlusFoodPerColonist)) + FlatFoodAdded;//NetFoodPerTurn is finished being calculated in another file...
            //Production
            NetProductionPerTurn = (WorkerPercentage * (Population / 1000) * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            NetProductionPerTurn = NetProductionPerTurn + Owner.data.Traits.ProductionMod * NetProductionPerTurn;
            MaxProductionPerTurn = GetMaxProductionPotentialCalc();

            Consumption =  ((Population / 1000) + Owner.data.Traits.ConsumptionModifier * (Population / 1000));

            if (Owner.data.Traits.Cybernetic > 0)
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * (NetProductionPerTurn - Consumption) ;
            else
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * NetProductionPerTurn;

            GrossProductionPerTurn =  ((Population / 1000) * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            GrossProductionPerTurn = GrossProductionPerTurn + Owner.data.Traits.ProductionMod * GrossProductionPerTurn;


            if (Station != null && !LoadUniverse)
            {
                if (!HasShipyard)
                    Station.SetVisibility(false, Empire.Universe.ScreenManager, this);
                else
                    Station.SetVisibility(true, Empire.Universe.ScreenManager, this);
            }

            //Money
            GrossMoneyPT = (Population / 1000);
            GrossMoneyPT += PlusTaxPercentage * GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.PopulationBillion * this.PlusCreditsPerColonist;
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
            if (Population < 100.0) Population = 100f;      //Minimum population. I guess they wont all die from starvation
            AvgPopulationGrowth = (AvgPopulationGrowth + adjustedRepRate) / 2;
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

        public int TotalInvadeInjure   => BuildingList.FilterBy(b => b.InvadeInjurePoints > 0).Sum(b => b.InvadeInjurePoints);
        public float TotalSpaceOffense => BuildingList.FilterBy(b => b.isWeapon).Sum(b => b.Offense);

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

        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT
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

        //Debug Text
        public Array<DebugTextBlock> DebugPlanetInfo()
        {
            var tradePlanet = this;
            var incomingData = new DebugTextBlock();
            var blocks = new Array<DebugTextBlock>();
            Array<string> lines = new Array<string>();
            var totals = tradePlanet.DebugSummarizePlanetStats(lines);
            float foodHere = tradePlanet.FoodHere;
            float prodHere = tradePlanet.ProductionHere;
            float foodStorPerc = 100 * foodHere / tradePlanet.MaxStorage;
            float prodStorPerc = 100 * prodHere / tradePlanet.MaxStorage;
            string food = $"{(int)foodHere}(%{foodStorPerc:00.0}) {tradePlanet.FS}";
            string prod = $"{(int)prodHere}(%{prodStorPerc:00.0}) {tradePlanet.PS}";

            incomingData.AddLine($"{tradePlanet.ParentSystem.Name} : {tradePlanet.Name} : IN Cargo: {totals.Total}", Color.Yellow);
            incomingData.AddLine($"FoodHere: {food} IN: {totals.Food}", Color.White);
            incomingData.AddLine($"ProdHere: {prod} IN: {totals.Prod}");
            incomingData.AddLine($"IN Colonists: {totals.Colonists}");
            incomingData.AddLine($"");
            blocks.Add(incomingData);
            return blocks;
        }
        public TradeAI.DebugSummaryTotal DebugSummarizePlanetStats(Array<string> lines)
        {
            lines.Add($"Money: {NetIncome}");
            lines.Add($"Eats: {Consumption}");
            lines.Add($"FoodWkrs: {FarmerPercentage}");
            lines.Add($"ProdWkrs: {WorkerPercentage}  ");
            return new TradeAI.DebugSummaryTotal();
        }
    }
}

