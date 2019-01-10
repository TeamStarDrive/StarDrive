using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    // Production facilities
    public class SBProduction
    {
        private readonly Planet Ground;

        private Array<PlanetGridSquare> TilesList                  => Ground.TilesList;
        private Empire Owner                                       => Ground.Owner;        
        private Array<Building> BuildingList                       => Ground.BuildingList;        
        private SolarSystem ParentSystem                           => Ground.ParentSystem;        
        public BatchRemovalCollection<QueueItem> ConstructionQueue = new BatchRemovalCollection<QueueItem>();
        private int CrippledTurns                                  => Ground.CrippledTurns;
        private bool RecentCombat                                  => Ground.RecentCombat;
        private float ShipBuildingModifier                         => Ground.ShipBuildingModifier;
        private float Fertility                                    => Ground.Fertility;
        private SpaceStation Station                               => Ground.Station;        
        private Planet.GoodState PS                                => Ground.PS;
        private Planet.ColonyType colonyType                       => Ground.colonyType;
        private float NetProductionPerTurn                         => Ground.Prod.NetIncome;

        private float ProductionHere
        {
            get => Ground.ProdHere;
            set => Ground.ProdHere = value;
        }

        public SBProduction(Planet planet)
        {
            Ground = planet;
        }

        public bool ApplyStoredProduction(int index)
        {

            if (CrippledTurns > 0 || RecentCombat || (ConstructionQueue.Count <= 0 || Owner == null))//|| this.Owner.Money <=0))
                return false;
            if (Owner != null && !Owner.isPlayer && Owner.data.Traits.Cybernetic > 0)
                return false;

            float amountToRush = Ground.Prod.NetMaxPotential; //for debug help
            float amount = Math.Min(ProductionHere, amountToRush);
            if (Empire.Universe.Debug && Owner.isPlayer)
                amount = float.MaxValue;
            if (amount < 1)
            {
                return false;
            }
            ProductionHere -= amount;
            ApplyProductiontoQueue(amount, index);

            return true;
        }

        public void ApplyProductiontoQueue(float howMuch, int whichItem)
        {
            if (CrippledTurns > 0 || RecentCombat || howMuch <= 0.0)
            {
                if (howMuch > 0 && CrippledTurns <= 0)
                    ProductionHere += howMuch;
                return;
            }

            if (ConstructionQueue.Count > 0 && ConstructionQueue.Count > whichItem)
            {
                QueueItem item = ConstructionQueue[whichItem];
                float cost = item.Cost;
                if (item.isShip)
                    cost *= ShipBuildingModifier;
                //cost -= item.productionTowards;
                item.productionTowards += howMuch;
                float remainder = item.productionTowards - cost;
                ProductionHere += Math.Max(0, remainder);                                
            }
            else ProductionHere += howMuch;

            for (int index1 = 0; index1 < ConstructionQueue.Count; ++index1)
            {
                QueueItem queueItem = ConstructionQueue[index1];

                //Added by gremlin remove exess troops from queue 
                if (queueItem.isTroop)
                {

                    int space = 0;
                    foreach (PlanetGridSquare tilesList in TilesList)
                    {
                        if (tilesList.TroopsHere.Count >= tilesList.number_allowed_troops || tilesList.building != null 
                            && (tilesList.building == null || tilesList.building.CombatStrength != 0))
                        {
                            continue;
                        }
                        space++;
                    }

                    if (space < 1)
                    {
                        if (queueItem.productionTowards == 0)
                        {
                            ConstructionQueue.Remove(queueItem);
                        }
                        else
                        {
                            ProductionHere += queueItem.productionTowards;
                            if (queueItem.pgs != null)
                                queueItem.pgs.QItem = null;
                            ConstructionQueue.Remove(queueItem);
                        }
                    }
                }

                if (queueItem.isBuilding && queueItem.productionTowards >= queueItem.Cost)
                {
                    bool dupBuildingWorkaround = false;
                    if (!queueItem.Building.IsBiospheres)
                        foreach (Building dup in BuildingList)
                        {
                            if (dup.Name == queueItem.Building.Name)
                            {
                                ProductionHere += queueItem.productionTowards;
                                ConstructionQueue.QueuePendingRemoval(queueItem);
                                dupBuildingWorkaround = true;
                            }
                        }
                    if (!dupBuildingWorkaround)
                    {
                        Building building = ResourceManager.CreateBuilding(queueItem.Building.Name);
                        if (queueItem.IsPlayerAdded)
                            building.IsPlayerAdded = queueItem.IsPlayerAdded;
                        BuildingList.Add(building);
                        Ground.ChangeMaxFertility(-building.MinusFertilityOnBuild);
                        if (queueItem.pgs != null)
                        {
                            if (queueItem.Building != null && queueItem.Building.IsBiospheres)
                            {
                                queueItem.pgs.Habitable = true;
                                queueItem.pgs.Biosphere = true;
                                queueItem.pgs.building = null;
                                queueItem.pgs.QItem = null;
                            }
                            else
                            {
                                queueItem.pgs.building = building;
                                queueItem.pgs.QItem = null;
                            }
                        }
                        if (queueItem.Building.IsSpacePort)
                        {
                            Station.planet = Ground;
                            Station.ParentSystem = ParentSystem;
                            Station.LoadContent(Empire.Universe.ScreenManager);
                            Ground.HasShipyard = true;
                        }
                        if (queueItem.Building.AllowShipBuilding)
                            Ground.HasShipyard = true;
                        if (building.EventOnBuild != null && Owner != null && Owner == Empire.Universe.PlayerEmpire)
                            Empire.Universe.ScreenManager.AddScreen(new EventPopup(Empire.Universe, Empire.Universe.PlayerEmpire, ResourceManager.EventsDict[building.EventOnBuild], ResourceManager.EventsDict[building.EventOnBuild].PotentialOutcomes[0], true));
                        ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                }
                else if (queueItem.isShip && !ResourceManager.ShipsDict.ContainsKey(queueItem.sData.Name))
                {
                    ConstructionQueue.QueuePendingRemoval(queueItem);
                    ProductionHere += queueItem.productionTowards;
                }
                else if (queueItem.isShip && queueItem.productionTowards >= queueItem.Cost * ShipBuildingModifier)
                {
                    Ship shipAt;
                    if (queueItem.isRefit)
                        shipAt = Ship.CreateShipAt(queueItem.sData.Name, Owner, Ground, true, !string.IsNullOrEmpty(queueItem.RefitName) ? queueItem.RefitName : queueItem.sData.Name, queueItem.sData.Level);
                    else
                        shipAt = Ship.CreateShipAt(queueItem.sData.Name, Owner, Ground, true);
                    ConstructionQueue.QueuePendingRemoval(queueItem);

                    if (queueItem.sData.Role == ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform)
                    {
                        int num = Ground.Shipyards.Count / 9;
                        shipAt.Position = Ground.Center + MathExt.PointOnCircle(Ground.Shipyards.Count * 40, 2000 + 2000 * num * Ground.Scale);
                        shipAt.Center = shipAt.Position;
                        shipAt.TetherToPlanet(Ground);
                        Ground.Shipyards.Add(shipAt.guid, shipAt);
                    }
                    if (queueItem.Goal != null)
                    {
                        if (queueItem.Goal is BuildConstructionShip)
                        {
                            shipAt.AI.OrderDeepSpaceBuild(queueItem.Goal);
                            shipAt.isConstructor = true;
                            shipAt.VanityName = "Construction Ship";
                        }
                        else if (!(queueItem.Goal is BuildDefensiveShips) 
                            && !(queueItem.Goal is BuildOffensiveShips) 
                            && !(queueItem.Goal is FleetRequisition))
                        {
                            queueItem.Goal.AdvanceToNextStep();
                        }
                        else
                        {
                            if (Owner != Empire.Universe.PlayerEmpire)
                                Owner.ForcePoolAdd(shipAt);
                            queueItem.Goal.ReportShipComplete(shipAt);
                        }
                    }
                    else if ((queueItem.sData.Role != ShipData.RoleName.station || queueItem.sData.Role == ShipData.RoleName.platform)
                        && Owner != Empire.Universe.PlayerEmpire)
                        Owner.ForcePoolAdd(shipAt);
                }
                else if (queueItem.isTroop && queueItem.productionTowards >= queueItem.Cost)
                {
                    if (ResourceManager.CreateTroop(queueItem.troopType, Owner).AssignTroopToTile(Ground))
                    {
                        queueItem.Goal?.NotifyMainGoalCompleted();
                        ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                }
            }
            ConstructionQueue.ApplyPendingRemovals();
        }

        public void ApplyAllStoredProduction(int Index)
        {
            if (CrippledTurns > 0 || RecentCombat || (ConstructionQueue.Count <= 0 || Owner == null)) //|| this.Owner.Money <= 0))
                return;

            float amount = Empire.Universe.Debug ? float.MaxValue : ProductionHere;
            ProductionHere = 0f;
            ApplyProductiontoQueue(amount, Index);

        }

        public void ApplyProductionTowardsConstruction()
        {
            if (CrippledTurns > 0 || RecentCombat)
                return;
         
            float maxp = Ground.Prod.NetMaxPotential * (1 - Ground.Food.Percent); 
            if (maxp < 5)
                maxp = 5;

            float storageRatio = ProductionHere / Ground.Storage.Max;
            float take10Turns = maxp * storageRatio;

            if (PS != Planet.GoodState.EXPORT)
                take10Turns *= (storageRatio < 0.75f ? PS == Planet.GoodState.EXPORT ? 0.5f : PS == Planet.GoodState.STORE ? 0.25f : 1 : 1);

            if (colonyType == Planet.ColonyType.Colony)
            {
                take10Turns = NetProductionPerTurn;
            }

            float normalAmount = take10Turns;

            normalAmount = ProductionHere.Clamped(0, normalAmount);
            ProductionHere -= normalAmount;

            ApplyProductiontoQueue(normalAmount, 0);
            ProductionHere += NetProductionPerTurn > 0.0f ? NetProductionPerTurn : 0.0f;

            //fbedard: apply all remaining production on Planet with no governor
            if (PS != Planet.GoodState.EXPORT && colonyType == Planet.ColonyType.Colony && Owner.isPlayer)
            {
                normalAmount = ProductionHere;
                ProductionHere = 0f;
                ApplyProductiontoQueue(normalAmount, 0);
            }
        }

        public void AddBuildingToCQ(Building b, bool playerAdded = false)
        {
            var qi = new QueueItem(Ground)
            {
                IsPlayerAdded = playerAdded,
                isBuilding = true,
                Building = b,
                Cost = b.Cost * UniverseScreen.GamePaceStatic,
                productionTowards = 0.0f,
                NotifyOnEmpty = false
            };

            if (!ResourceManager.BuildingsDict.TryGetValue("Terraformer", out Building terraformer))
            {
                foreach (KeyValuePair<string, bool> bdict in Owner.GetBDict())
                {
                    if (!bdict.Value)
                        continue;
                    Building check = ResourceManager.GetBuildingTemplate(bdict.Key);

                    if (check.PlusTerraformPoints <= 0)
                        continue;
                    terraformer = check;
                }
            }
            if (b.AssignBuildingToTile(qi, Ground))
            {
                ConstructionQueue.Add(qi);
                Ground.RefreshBuildingsWeCanBuildHere();
            }
            else if (Owner.data.Traits.Cybernetic <= 0 && Owner.GetBDict()[terraformer.Name] && Fertility < 1.0
                && Ground.WeCanAffordThis(terraformer, colonyType))
            {
                bool flag = true;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isBuilding && queueItem.Building.Name == terraformer.Name)
                        flag = false;
                }
                foreach (Building building in BuildingList)
                {
                    if (building.Name == terraformer.Name)
                        flag = false;
                }
                if (!flag)
                    return;
                AddBuildingToCQ(ResourceManager.CreateBuilding(terraformer.Name), false);
            }
            else
            {
                if (!Owner.GetBDict()["Biospheres"])
                    return;
                Ground.TryBiosphereBuild(ResourceManager.CreateBuilding(Building.BiospheresId), qi);
            }
        }
        public int EstimatedTurnsTillComplete(QueueItem qItem, float industry = float.MinValue)
        {
            float production = qItem.Cost;
            industry = industry < 0 ? NetProductionPerTurn : industry;
            if (qItem.isShip)
                production *= ShipBuildingModifier;
            production -= -qItem.productionTowards;
            production /= industry;
            int turns = (int)Math.Ceiling(production);
            return industry > 0.0 ? turns : 999;
        }
        //public int TotalTurnsInProductionQueue() => ConstructionQueue.Sum(q => EstimatedTurnsTillComplete(q, NetProductionPerTurn));
        public int TotalTurnsInProductionQueue(float industry) => ConstructionQueue.Sum(q=> EstimatedTurnsTillComplete(q,industry));

        public int EstimateMinTurnsToBuildShip(float shipCost)
        {
            shipCost *= ShipBuildingModifier;
            var prodPow = Ground.Prod.NetMaxPotential;
            int turns = TotalTurnsInProductionQueue(prodPow); //Ground.GetMaxGoodProd("Production")
            turns += (int)Math.Ceiling(shipCost / prodPow);
            return Math.Min(999, turns);

        }


        public bool TryBiosphereBuild(Building b, QueueItem qi)
        {
            if (qi.isBuilding == false && Ground.NeedsFood()) //(FarmerPercentage > .5f || NetFoodPerTurn < 0))
                return false;
            var list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (!planetGridSquare.Habitable && planetGridSquare.building == null && (!planetGridSquare.Biosphere && planetGridSquare.QItem == null))
                    list.Add(planetGridSquare);
            }
            if (!b.IsBiospheres || list.Count <= 0) return false;

            int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
            PlanetGridSquare planetGridSquare1 = list[index];
            foreach (PlanetGridSquare planetGridSquare2 in TilesList)
            {
                if (planetGridSquare2 == planetGridSquare1)
                {
                    qi.Building = b;
                    qi.isBuilding = true;
                    qi.Cost = b.Cost;
                    qi.productionTowards = 0.0f;
                    planetGridSquare2.QItem = qi;
                    qi.pgs = planetGridSquare2;
                    qi.NotifyOnEmpty = false;
                    ConstructionQueue.Add(qi);
                    return true;
                }
            }
            return false;
        }

        public float GetTotalConstructionQueueMaintenance()
        {
            float count = 0;
            foreach (QueueItem b in ConstructionQueue)
            {
                if (!b.isBuilding) continue;
                count -= b.Building.Maintenance + b.Building.Maintenance * Owner.data.Traits.MaintMod;
            }
            return count;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SBProduction() { Dispose(false); }
        private void Dispose(bool disposing)
        {
            ConstructionQueue?.Dispose(ref ConstructionQueue);
        }
    }
}