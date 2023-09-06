using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using SDUtils;
using System.Linq;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Universe.SolarBodies
{
    // Production facilities
    [StarDataType]
    public class SBProduction
    {
        [StarData] readonly Planet P;
        Empire Owner => P.Owner;

        public bool NotEmpty => ConstructionQueue.NotEmpty;
        public bool Empty => ConstructionQueue.IsEmpty;
        public int Count => ConstructionQueue.Count;

        /// <summary>
        /// The Construction queue should be protected
        /// </summary>
        [StarData] readonly Array<QueueItem> ConstructionQueue = new();

        float ProductionHere
        {
            get => P.ProdHere;
            set => P.ProdHere = value;
        }

        [StarData] float SurplusThisTurn;
        
        [StarDataConstructor]
        public SBProduction(Planet planet)
        {
            P = planet;
        }

        public IReadOnlyList<QueueItem> GetConstructionQueue()
        {
            return ConstructionQueue;
        }

        // Rush button is used only in debug mode for fast debug rush
        public bool RushProduction(int itemIndex, float maxAmount, bool rushButton = false)
        {
            // don't allow rush if we're crippled
            if (P.IsCrippled || ConstructionQueue.IsEmpty || Owner == null)
                return false;

            // dont charge rush fees if in debug and the rush button was clicked
            bool rushFees = !P.Universe.Debug || !rushButton;

            float amount = maxAmount.UpperBound(ProductionHere);
            if (rushFees && amount > Owner.Money)
                return false; // Not enough credits to rush

            // inject artificial surplus to instantly rush & finish production
            if (P.Universe.Debug && rushButton)
                amount = SurplusThisTurn = 1000;

            return ApplyProductionToQueue(maxAmount: amount, itemIndex, rushFees, immediate: rushButton);
        }

        // Spend up to `max` production for QueueItem
        // @return TRUE if QueueItem is complete
        bool SpendProduction(QueueItem q, float max, bool chargeFees)
        {
            float needed = q.ProductionNeeded;
            if (needed <= 0f) return true; // complete!

            float spendMax = Math.Min(needed, max); // how much can we spend?
            float spend = spendMax;

            MathExt.Consume(ref SurplusThisTurn, ref spend);
            P.Storage.ConsumeProduction(ref spend);

            float netSpend     = spendMax - spend;
            q.ProductionSpent += netSpend; // apply it
            if (chargeFees && (!P.Owner.isPlayer || !P.Universe.Debug))
                Owner.ChargeCreditsOnProduction(q, netSpend);

            // if we spent everything, this QueueItem is complete
            return spend <= 0f;
        }

        // @note `maxProduction` is a max limit, this method will attempt
        //       to consume no more than `maxAmount` from local production
        // @return true if at least some production was applied
        bool ApplyProductionToQueue(float maxAmount, int itemIndex, bool rushFees, bool immediate)
        {
            if (maxAmount <= 0.0f || ConstructionQueue.IsEmpty)
                return false;

            // apply production to specified item
            if (ConstructionQueue.Count > itemIndex)
            {
                QueueItem item = ConstructionQueue[itemIndex];
                if (rushFees && (!P.OwnerIsPlayer || !P.Universe.Debug))
                {
                    SpendProduction(item, maxAmount, chargeFees: false);
                    Owner.ChargeRushFees(maxAmount, immediate);
                }
                else
                {
                    SpendProduction(item, maxAmount, chargeFees: true);
                }
            }

            for (int i = 0; i < ConstructionQueue.Count;)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isTroop && !HasRoomForTroops()) // remove excess troops from queue
                {
                    Cancel(q);
                    continue; // this item was removed, so skip ++i
                }
                if (q.IsCancelled)
                {
                    Cancel(q);
                    continue; // this item was removed, so skip ++i
                }
                if (q.IsComplete)
                {
                    ProcessCompleteQueueItem(q);
                    continue; // this item was removed, so skip ++i
                }
                ++i;
            }

            return true;
        }

        void ProcessCompleteQueueItem(QueueItem q)
        {
            bool ok = false;

            if (q.isBuilding) ok = OnBuildingComplete(q);
            else if (q.isShip) ok = OnShipComplete(q);
            else if (q.isTroop) ok = TrySpawnTroop(q);

            Finish(q, success: ok);
        }

        bool HasRoomForTroops()
        {
            foreach (PlanetGridSquare tile in P.TilesList)
            {
                if (tile.TroopsHere.Count < tile.MaxAllowedTroops &&
                    (tile.Building == null || (tile.Building != null && tile.Building.CombatStrength == 0)))
                    return true;
            }
            return false;
        }

        bool OnBuildingComplete(QueueItem q)
        {
            // we can't place it...
            if (q.Building.Unique && P.BuildingBuilt(q.Building.BID))
            {
                Log.Warning($"Unique building {q.Building} already exists on planet {P}");
                return false;
            }
            if (!q.pgs.CanPlaceBuildingHere(q.Building))
            {
                Log.Warning($"We can no longer build {q.Building} at tile {q.pgs}");
                return false;
            }

            Building b = ResourceManager.CreateBuilding(P, q.Building.Name);
            b.IsPlayerAdded = q.IsPlayerAdded;
            q.pgs.PlaceBuilding(b, P);
            if (!P.Universe.P.SuppressOnBuildNotifications
                && !P.Universe.Screen.IsViewingColonyScreen(P)
                && P.OwnerIsPlayer
                && (q.IsPlayerAdded || q.Building.IsCapital))
            {
                P.Universe.Notifications.AddBuildingConstructed(P, b);
            }

            return true;
        }

        bool TrySpawnTroop(QueueItem q)
        {
            if (!ResourceManager.TryCreateTroop(q.TroopType, Owner, out Troop troop))
                return false;
            if (!troop.PlaceNewTroop(P) && troop.Launch(P) == null)
                return false; // Could not find a place to the troop or launch it to space
            q.Goal?.NotifyMainGoalCompleted();
            return true;
        }

        bool OnShipComplete(QueueItem q)
        {
            if (!ResourceManager.ShipTemplateExists(q.ShipData.Name))
                return false;

            Ship shipAt = Ship.CreateShipNearPlanet(P.Universe, q.ShipData.Name, Owner, P, true);
            shipAt.LaunchShip = new LaunchShip(shipAt);
            q.Goal?.ReportShipComplete(shipAt);
            if (q.Goal is BuildConstructionShip || q.Goal is BuildOrbital)
            {
                shipAt.VanityName = q.ShipData.Name;
                shipAt.AI.SetPriorityOrder(true);
            }

            if (shipAt.IsFreighter)
            {
                shipAt.DownloadTradeRoutes(q.TradeRoutes);
                shipAt.TransportingFood        = true;
                shipAt.TransportingProduction  = true;
                shipAt.TransportingColonists   = true;
                shipAt.AllowInterEmpireTrade   = true; 
                shipAt.AreaOfOperation         = q.AreaOfOperation;
                shipAt.TransportingColonists  &= q.TransportingColonists;
                shipAt.TransportingFood       &= q.TransportingFood;
                shipAt.TransportingProduction &= q.TransportingProduction;
                shipAt.AllowInterEmpireTrade  &= q.AllowInterEmpireTrade;
            }

            if (shipAt.ShipData.IsColonyShip)
            {
                float amount = shipAt.CargoSpaceFree.UpperBound(P.Population / 10);
                P.Population -= shipAt.LoadColonists(amount);
            }

            return true;
        }

        // Applies available production to production queue
        public void AutoApplyProduction(float surplusFromPlanet)
        {
            // surplus will be reset every turn and consumed at first opportunity
            SurplusThisTurn = surplusFromPlanet;
            if (ConstructionQueue.IsEmpty || P.CrippledTurns > 0)
                return; // Massive sabotage to planetary facilities or no items

            float percentToApply = P.RecentCombat ? 0.1f : 1f; // Ongoing combat is hindering logistics
            float limitSpentProd = P.LimitedProductionExpenditure(P.CurrentProductionToQueue);
            ApplyProductionToQueue(maxAmount: limitSpentProd * percentToApply, 0, rushFees: false, immediate: false);
            TryPlayerRush();
        }

        void TryPlayerRush() // Apply rush if player marked items as continuous rush
        {
            if (!P.OwnerIsPlayer || Count == 0 || P.CrippledTurns > 0 || P.RecentCombat)
                return;

            QueueItem item = ConstructionQueue[0];
            if (item.Rush || Owner.RushAllConstruction)
            {
                float prodToRush = item.ProductionNeeded.UpperBound(P.ProdHere);
                if (prodToRush * GlobalStats.Defaults.RushCostPercentage + 1000 < P.Universe.Player.Money)
                    RushProduction(0, prodToRush);
            }
        }

        // @return TRUE if building was added to CQ,
        //         FALSE if `where` is occupied or if there is no free random tiles
        public bool Enqueue(Building b, PlanetGridSquare where = null, bool playerAdded = false)
        {
            if (b.Unique || b.BuildOnlyOnce)
            {
                if (P.BuildingBuiltOrQueued(b))
                    return false; // unique building already built
            }

            var qi = new QueueItem(P)
            {
                IsPlayerAdded   = playerAdded,
                isBuilding      = true,
                IsMilitary      = b.IsMilitary,
                Building        = b,
                pgs             = where,
                Cost            = b.ActualCost,
                ProductionSpent = 0.0f,
                NotifyOnEmpty   = false,
                Rush            = P.Owner.RushAllConstruction,
                QType           = QueueItemType.Building
            };

            if (b.AssignBuildingToTile(b, ref where, P))
            {
                where.QItem = qi;
                qi.pgs = where; // reset PGS if we got a new one
                AddToQueueAndPrioritize(qi);
                P.RefreshBuildingsWeCanBuildHere();
                return true;
            }

            return false;
        }

        public void Enqueue(QueueItemType type, IShipDesign orbital, IShipDesign constructor, bool rush, Goal goal = null)
        {
            if (goal != null && goal.PlanetBuildingAt == null)
                throw new InvalidOperationException($"CQ.Enqueue not allowed if Goal.PlanetBuildingAt is null!");

            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName   = $"{constructor.Name} ({orbital.Name})",
                ShipData      = constructor,
                Cost          = orbital.GetCost(Owner),
                Rush          = P.Owner.RushAllConstruction || rush,
                QType         = type
            };
            if (goal != null) 
                goal.PlanetBuildingAt = P;

            AddToQueueAndPrioritize(qi);
        }

        public void Enqueue(IShipDesign orbitalRefit, IShipDesign constructor, float refitCost, Goal goal, bool rush)
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName   = $"{constructor.Name} ({orbitalRefit.Name})",
                ShipData      = constructor,
                Cost          = refitCost,
                Rush          = rush || P.Owner.RushAllConstruction,
                QType         = QueueItemType.CombatShip
            };

            AddToQueueAndPrioritize(qi);
        }

        public void Enqueue(IShipDesign ship, QueueItemType type, Goal goal = null, bool notifyOnEmpty = true, string displayName = "")
        {
            if (goal != null && goal.PlanetBuildingAt == null)
                throw new InvalidOperationException($"CQ.Enqueue not allowed if Goal.PlanetBuildingAt is null!");

            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = ship.IsPlatformOrStation,
                Goal          = goal,
                ShipData      = ship,
                Cost          = GetShipCost(),
                NotifyOnEmpty = notifyOnEmpty,
                Rush          = P.Owner.RushAllConstruction,
                QType         = type
            };  

            if (displayName.NotEmpty())
                qi.DisplayName = displayName;

            if (goal != null)
                goal.PlanetBuildingAt = P;

            AddToQueueAndPrioritize(qi);

            float GetShipCost()
            {
                if (!ship.IsSingleTroopShip)
                {
                    return ship.GetCost(Owner);
                }
                else // for when a player requisitions a single troop ship in a fleet
                {
                    Troop troopTemplate = Owner.GetUnlockedTroops().FindMax(troop => troop.SoftAttack);
                    if (troopTemplate != null)
                    {
                        return troopTemplate.ActualCost(P.Owner);
                    }
                    else
                    {
                        Log.Warning($"{Owner.Name} does not have any unlocked troops. Using troopship base cost.");
                        return ship.GetCost(Owner);
                    }
                }
            }
        }

        public void Enqueue(Troop template, QueueItemType type, Goal goal = null)
        {
            if (goal != null && goal.PlanetBuildingAt == null)
                throw new InvalidOperationException($"CQ.Enqueue not allowed if Goal.PlanetBuildingAt is null!");

            var qi = new QueueItem(P)
            {
                isTroop     = true,
                TroopType   = template.Name,
                Goal        = goal,
                Cost        = template.ActualCost(P.Owner),
                Rush        = P.Owner.RushAllConstruction,
                QType       = type
            };

            if (goal != null) 
                goal.PlanetBuildingAt = P;

            AddToQueueAndPrioritize(qi);
        }

        public void EnqueueRefitShip(QueueItem item)
        {
            AddToQueueAndPrioritize(item);
        }

        void AddToQueueAndPrioritize(QueueItem item)
        {
            lock (ConstructionQueue)
            {
                ConstructionQueue.Add(item);
                if (!P.OwnerIsPlayer)
                {
                    int totalFreighters = Owner.TotalFreighters;
                    ConstructionQueue.Sort(q => q.GetAndUpdatePriorityForAI(P, totalFreighters));
                }
            }
        }

        void Finish(QueueItem q, bool success)
        {
            if (success) Finish(q);
            else         Cancel(q);
        }

        void Finish(QueueItem q)
        {
            lock (ConstructionQueue)
                ConstructionQueue.Remove(q);
        }

        public bool Cancel(Building b)
        {
            lock (ConstructionQueue)
            {
                QueueItem item = ConstructionQueue.Find(q => q.Building == b);
                item?.SetCanceled();
                return item != null;
            }
        }

        public bool Cancel(Goal g)
        {
            lock (ConstructionQueue)
            {
                QueueItem item = ConstructionQueue.Find(q => q.Goal == g);
                item?.SetCanceled();
                return item != null;
            }
        }

        public void Cancel(QueueItem q, bool refund = true)
        {
            if (refund)
                P.ProdHere += q.ProductionSpent / 2;

            if (q.pgs != null)
            {
                q.pgs.QItem = null;
            }
            if (q.Goal != null)
            {
                if (q.Goal is BuildConstructionShip || q.Goal is BuildOrbital)
                    Owner.AI.RemoveGoal(q.Goal);

                if (q.Goal is FleetGoal fg)
                {
                    fg.Fleet?.RemoveGoal(q.Goal);
                    Owner.AI.RemoveGoal(q.Goal);
                }

                if (q.Goal is RefitOrbital)
                    q.Goal.OldShip?.AI.ClearOrders();
            }

            lock (ConstructionQueue)
                ConstructionQueue.Remove(q);
            if (q.isBuilding)
                P.RefreshBuildingsWeCanBuildHere();
        }

        public void PrioritizeProjector(Vector2 buildPos)
        {
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isShip 
                    && q.DisplayName != null
                    && q.DisplayName.Contains("Subspace Projector")
                    && q.Goal.BuildPosition == buildPos)
                {
                    MoveTo(0, i);
                    break;
                }
            }
        }

        public void RefitShipsBeingBuilt(Ship oldShip, IShipDesign newShip)
        {
            float refitCost = oldShip.RefitCost(newShip);
            foreach (QueueItem q in ConstructionQueue)
            {
                if (q.isShip && q.ShipData.Name == oldShip.Name)
                {
                    float percentCompleted = q.ProductionSpent / q.ActualCost;
                    q.ShipData = newShip;
                    q.Cost = percentCompleted.AlmostZero() 
                           ? newShip.GetCost(Owner) 
                           : q.Cost + refitCost * percentCompleted * P.ShipCostModifier;
                }
            }
        }

        public bool ContainsShipDesignName(string name) => ConstructionQueue.Any(q => q.isShip && q.ShipData.Name == name);
        public bool ContainsTroopWithGoal(Goal g) => ConstructionQueue.Any(q => q.isTroop && q.Goal == g);

        public bool CancelShipyard()
        {
            QueueItem shipyard = ConstructionQueue.LastOrDefault(q => q.isShip && q.ShipData.IsShipyard);
            if (shipyard != null)
            {
                Cancel(shipyard);
                return true;
            }
            return false;
        }

        public void Reorder(QueueItem item, int relativeChange)
        {
            lock (ConstructionQueue)
            {
                // When dragging an item, some items could be removed or moved
                // while the dragging is in process and before the scroll list is updated.
                // So we always need to double-check the itemIndex and newIndex
                int oldIndex = ConstructionQueue.IndexOf(item);
                if (oldIndex == -1)
                    return;

                int newIndex = oldIndex + relativeChange;
                if ((uint)newIndex < ConstructionQueue.Count)
                {
                    ConstructionQueue.Reorder(oldIndex, newIndex);
                }
            }
        }

        public void Swap(int swapTo, int currentIndex)
        {
            var cq = ConstructionQueue;
            lock (cq)
            {
                swapTo = swapTo.Clamped(0, cq.Count - 1);
                currentIndex = currentIndex.Clamped(0, cq.Count - 1);

                (cq[swapTo], cq[currentIndex]) = (cq[currentIndex], cq[swapTo]);
            }
        }

        public void MoveTo(int moveTo, int currentIndex)
        {
            lock (ConstructionQueue)
            {
                QueueItem item = ConstructionQueue[currentIndex];
                ConstructionQueue.RemoveAt(currentIndex);
                ConstructionQueue.Insert(moveTo, item);
            }
        }

        public void MoveToAndContinuousRushFirstItem()
        {
            if (Empty)
                return;

            lock (ConstructionQueue)
            {
                if (Count > 1)
                    MoveTo(0, Count - 1);

                ConstructionQueue[0].Rush = true;
            }
        }

        public void SwitchRushAllConstruction(bool rush)
        {
            lock (ConstructionQueue)
                for (int i = 0; i < ConstructionQueue.Count; ++i)
                     ConstructionQueue[i].Rush = rush;
        }

        public void ClearQueue()
        {
            lock (ConstructionQueue)
                ConstructionQueue.Clear();
            foreach (PlanetGridSquare tile in P.TilesList)
                tile.QItem = null; // Clear all planned buildings from tiles
        }

        public bool FirstItemCanFeedUs()
        {
            lock (ConstructionQueue)
            {
                if (ConstructionQueue.Count == 0 || !ConstructionQueue[0].isBuilding)
                    return false;

                QueueItem first = ConstructionQueue[0];
                return P.NonCybernetic && first.Building.ProducesFood
                    || P.IsCybernetic && first.Building.ProducesProduction;
            }
        }
    }
}
