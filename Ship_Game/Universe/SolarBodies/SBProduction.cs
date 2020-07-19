﻿using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;

namespace Ship_Game.Universe.SolarBodies
{
    // Production facilities
    public class SBProduction
    {
        readonly Planet P;
        Empire Owner => P.Owner;

        public bool NotEmpty => ConstructionQueue.NotEmpty;
        public bool Empty => ConstructionQueue.IsEmpty;
        public int Count => ConstructionQueue.Count;

        /// <summary>
        /// The Construction queue should be protected
        /// </summary>
        readonly Array<QueueItem> ConstructionQueue = new Array<QueueItem>();

        float ProductionHere
        {
            get => P.ProdHere;
            set => P.ProdHere = value;
        }

        float SurplusThisTurn;

        public SBProduction(Planet planet)
        {
            P = planet;
        }

        bool IsCrippled => P.CrippledTurns > 0 || P.RecentCombat;

        public IReadOnlyList<QueueItem> GetConstructionQueue()
        {
            return ConstructionQueue;
        }

        public bool RushProduction(int itemIndex, float maxAmount, bool rush = false)
        {
            // don't allow rush if we're crippled
            if (IsCrippled || ConstructionQueue.IsEmpty || Owner == null)
                return false;

            float amount = maxAmount.UpperBound(ProductionHere);

            // inject artificial surplus to instantly rush & finish production
            if (Empire.Universe.Debug)
            {
                amount = SurplusThisTurn = 1000;
            }

            return ApplyProductionToQueue(maxAmount: amount, itemIndex, rush);
        }

        // The Remnant get a "magic" production cheat
        public void RemnantCheatProduction()
        {
            foreach (QueueItem item in ConstructionQueue)
                item.Cost = 0;
            ProductionHere += 1f; // add some free production before building
            ApplyProductionToQueue(maxAmount:2f, 0);
        }

        // Spend up to `max` production for QueueItem
        // @return TRUE if QueueItem is complete
        bool SpendProduction(QueueItem q, float max)
        {
            float needed = q.ProductionNeeded;
            if (needed <= 0f) return true; // complete!

            float spendMax = Math.Min(needed, max); // how much can we spend?
            float spend = spendMax;

            MathExt.Consume(ref SurplusThisTurn, ref spend);
            P.Storage.ConsumeProduction(ref spend);

            float netSpend     = spendMax - spend;
            q.ProductionSpent += netSpend; // apply it
            Owner.ChargeCreditsOnProduction(q, netSpend);

            // if we spent everything, this QueueItem is complete
            return spend <= 0f;
        }

        // @note `maxProduction` is a max limit, this method will attempt
        //       to consume no more than `maxAmount` from local production
        // @return true if at least some production was applied
        bool ApplyProductionToQueue(float maxAmount, int itemIndex, bool rush = false)
        {
            if (maxAmount <= 0.0f || ConstructionQueue.IsEmpty)
                return false;

            // apply production to specified item
            if (ConstructionQueue.Count > itemIndex)
            {
                QueueItem item = ConstructionQueue[itemIndex];
                SpendProduction(item, maxAmount);
                if (rush && !Empire.Universe.Debug)
                {
                    Owner.AddMoney(-maxAmount);
                    Owner.ChargeRushFees(maxAmount);
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
                    (tile.building == null || (tile.building != null && tile.building.CombatStrength == 0)))
                    return true;
            }
            return false;
        }

        bool OnBuildingComplete(QueueItem q)
        {
            // we can't place it... there's some sort of bug
            if (q.Building.Unique && P.BuildingBuilt(q.Building.BID))
            {
                Log.Error($"Unique building {q.Building} already exists on planet {P}");
                return false;
            }
            if (q.pgs.CanBuildHere(q.Building))
            {
                Log.Error($"We can no longer build {q.Building} at tile {q.pgs}");
                return false;
            }

            Building b = ResourceManager.CreateBuilding(q.Building.Name);
            b.IsPlayerAdded = q.IsPlayerAdded;
            q.pgs.PlaceBuilding(b, P);
            return true;
        }

        bool TrySpawnTroop(QueueItem q)
        {
            Troop troop = ResourceManager.CreateTroop(q.TroopType, Owner);
            if (!troop.PlaceNewTroop(P) && troop.Launch(P) == null)
                return false; // Could not find a place to the troop or launch it to space
            q.Goal?.NotifyMainGoalCompleted();
            return true;
        }

        bool OnShipComplete(QueueItem q)
        {
            if (!ResourceManager.ShipTemplateExists(q.sData.Name))
                return false;

            Ship shipAt = Ship.CreateShipAt(q.sData.Name, Owner, P, true);
            q.Goal?.ReportShipComplete(shipAt);
            if (q.Goal is BuildConstructionShip || q.Goal is BuildOrbital)
            {
                shipAt.IsConstructor = true;
                shipAt.VanityName = q.sData.Name;
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

            if (!Owner.isPlayer)
                Owner.Pool.ForcePoolAdd(shipAt);
            return true;
        }

        // Applies available production to production queue
        public void AutoApplyProduction(float surplusFromPlanet)
        {
            // surplus will be reset every turn and consumed at first opportunity
            SurplusThisTurn = surplusFromPlanet;
            if (ConstructionQueue.IsEmpty)
                return;

            float percentToApply = 1f;
            if      (P.CrippledTurns > 0) percentToApply = 0.05f; // massive sabotage to planetary facilities
            else if (P.RecentCombat)      percentToApply = 0.2f;  // ongoing combat is hindering logistics

            float limitSpentProd = P.LimitedProductionExpenditure();
            ApplyProductionToQueue(maxAmount: limitSpentProd * percentToApply, 0);
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
                QueueNumber     = ConstructionQueue.Count
            };

            if (b.AssignBuildingToTile(b, ref where, P))
            {
                where.QItem = qi;
                qi.pgs = where; // reset PGS if we got a new one
                ConstructionQueue.Add(qi);
                P.RefreshBuildingsWeCanBuildHere();
                return true;
            }

            return false;
        }

        public void Enqueue(Ship platform, Ship constructor, Goal goal = null)
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName   = $"{constructor.Name} ({platform.Name})",
                QueueNumber   = ConstructionQueue.Count,
                sData         = constructor.shipData,
                Cost          = platform.GetCost(Owner)
            };
            if (goal != null) goal.PlanetBuildingAt = P;
            ConstructionQueue.Add(qi);
        }

        public void Enqueue(Ship ship, Goal goal = null, bool notifyOnEmpty = true)
        {
            var qi = new QueueItem(P)
            {
                isShip = true,
                isOrbital = ship.IsPlatformOrStation,
                Goal   = goal,
                sData  = ship.shipData,
                Cost   = ship.GetCost(Owner),
                NotifyOnEmpty = notifyOnEmpty,
                QueueNumber = ConstructionQueue.Count,
            };
            if (goal != null) goal.PlanetBuildingAt = P;
            ConstructionQueue.Add(qi);
        }

        public void Enqueue(Troop template, Goal goal = null)
        {
            var qi = new QueueItem(P)
            {
                isTroop = true,
                QueueNumber = ConstructionQueue.Count,
                TroopType = template.Name,
                Goal = goal,
                Cost = template.ActualCost
            };
            if (goal != null) goal.PlanetBuildingAt = P;
            ConstructionQueue.Add(qi);
        }

        public void Enqueue(QueueItem item)
        {
            ConstructionQueue.Add(item);
        }

        void Finish(QueueItem q, bool success)
        {
            if (success) Finish(q);
            else         Cancel(q);
        }

        void Finish(QueueItem q)
        {
            ConstructionQueue.Remove(q);
            q.OnComplete?.Invoke(success: true);
        }

        public bool Cancel(Goal g)
        {
            QueueItem item = ConstructionQueue.Find(q => q.Goal == g);
            item?.SetCanceled();
            return item != null;
        }

        public void Cancel(QueueItem q)
        {
            P.ProdHere += q.ProductionSpent;
            if (q.pgs != null)
            {
                q.pgs.QItem = null;
            }
            if (q.Goal != null)
            {
                if (q.Goal is BuildConstructionShip || q.Goal is BuildOrbital)
                    Owner.GetEmpireAI().Goals.Remove(q.Goal);

                if (q.Goal.Fleet != null)
                {
                    q.Goal.Fleet.RemoveGoalGuid(q.Goal.guid);
                    Owner.GetEmpireAI().Goals.Remove(q.Goal);
                }
            }

            ConstructionQueue.Remove(q);
            if (q.isBuilding)
                P.RefreshBuildingsWeCanBuildHere();

            q.OnComplete?.Invoke(success: false);
        }

        public void PrioritizeShip(Ship ship)
        {
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isShip && q.sData == ship.shipData)
                {
                    MoveTo(0, i);
                    break;
                }
            }
        }

        public void Reorder(int oldIndex, int newIndex)
        {
            ConstructionQueue.Reorder(oldIndex, newIndex);
        }

        public void Swap(int swapTo, int currentIndex)
        {
            swapTo = swapTo.Clamped(0, ConstructionQueue.Count - 1);
            currentIndex = currentIndex.Clamped(0, ConstructionQueue.Count - 1);

            QueueItem item = ConstructionQueue[swapTo];
            ConstructionQueue[swapTo] = ConstructionQueue[currentIndex];
            ConstructionQueue[currentIndex] = item;
        }

        public void MoveTo(int moveTo, int currentIndex)
        {
            QueueItem item = ConstructionQueue[currentIndex];
            ConstructionQueue.RemoveAt(currentIndex);
            ConstructionQueue.Insert(moveTo, item);
        }

        public void ClearQueue()
        {
            ConstructionQueue.Clear();
        }
    }
}