using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

            float amount = maxAmount.UpperBound(ProductionHere);
            if (amount > Owner.Money)
                return false; // Not enough credits to rush

            // dont charge rush fees if in debug and the rush button was clicked
            bool rushFees = !Empire.Universe.Debug || !rushButton;

            // inject artificial surplus to instantly rush & finish production
            if (Empire.Universe.Debug && rushButton)
                amount = SurplusThisTurn = 1000;

            return ApplyProductionToQueue(maxAmount: amount, itemIndex, rushFees);
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
            if (!Empire.Universe.Debug)
                Owner.ChargeCreditsOnProduction(q, netSpend);

            // if we spent everything, this QueueItem is complete
            return spend <= 0f;
        }

        // @note `maxProduction` is a max limit, this method will attempt
        //       to consume no more than `maxAmount` from local production
        // @return true if at least some production was applied
        bool ApplyProductionToQueue(float maxAmount, int itemIndex, bool rushFees)
        {
            if (maxAmount <= 0.0f || ConstructionQueue.IsEmpty)
                return false;

            // apply production to specified item
            if (ConstructionQueue.Count > itemIndex)
            {
                QueueItem item = ConstructionQueue[itemIndex];
                SpendProduction(item, maxAmount);

                if (rushFees && (!Owner.isPlayer || !Empire.Universe.Debug))
                {
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
                    (tile.Building == null || (tile.Building != null && tile.Building.CombatStrength == 0)))
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
            if (!GlobalStats.SuppressOnBuildNotifications
                && !Empire.Universe.IsViewingColonyScreen(P)
                && P.Owner == EmpireManager.Player
                && (q.IsPlayerAdded || q.Building.IsCapital))
            {
                Empire.Universe.NotificationManager.AddBuildingConstructed(P, b);
            }

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

            if (shipAt.shipData.IsColonyShip)
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
            ApplyProductionToQueue(maxAmount: limitSpentProd * percentToApply, 0, rushFees: false);
            TryPlayerRush();
        }

        void TryPlayerRush() // Apply rush if player marked items as continuous rush
        {
            if (!Owner.isPlayer || Count == 0 || P.CrippledTurns > 0 || P.RecentCombat)
                return;

            QueueItem item = ConstructionQueue[0];
            if (item.Rush || Owner.RushAllConstruction)
            {
                float prodToRush = item.ProductionNeeded.UpperBound(P.ProdHere);
                if (prodToRush * GlobalStats.RushCostPercentage + 1000 < EmpireManager.Player.Money)
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
                QueueNumber     = ConstructionQueue.Count
            };

            if (b.AssignBuildingToTile(b, ref where, P))
            {
                where.QItem = qi;
                qi.pgs      = where; // reset PGS if we got a new one
                ConstructionQueue.Add(qi);
                P.RefreshBuildingsWeCanBuildHere();
                return true;
            }

            return false;
        }

        public void Enqueue(Ship platform, ShipDesign constructor, Goal goal = null)
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName   = $"{constructor.Name} ({platform.Name})",
                QueueNumber   = ConstructionQueue.Count,
                sData         = constructor,
                Cost          = platform.GetCost(Owner),
                Rush          = P.Owner.RushAllConstruction
            };
            if (goal != null) goal.PlanetBuildingAt = P;
            ConstructionQueue.Add(qi);
        }

        public void Enqueue(Ship orbitalRefit, ShipDesign constructor, float refitCost, Goal goal)
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName   = $"{constructor.Name} ({orbitalRefit.Name})",
                QueueNumber   = ConstructionQueue.Count,
                sData         = constructor,
                Cost          = refitCost,
                Rush          = P.Owner.RushAllConstruction,
            };
            
            ConstructionQueue.Add(qi);
        }

        public void Enqueue(ShipDesign ship, Goal goal = null, bool notifyOnEmpty = true, string displayName = "")
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                isOrbital     = ship.IsPlatformOrStation,
                Goal          = goal,
                sData         = ship,
                Cost          = ship.GetCost(Owner),
                NotifyOnEmpty = notifyOnEmpty,
                QueueNumber   = ConstructionQueue.Count,
                Rush          = P.Owner.RushAllConstruction
            };

            if (displayName.NotEmpty())
                qi.DisplayName = displayName;

            if (goal != null) 
                goal.PlanetBuildingAt = P;

            ConstructionQueue.Add(qi);
        }

        public void Enqueue(Troop template, Goal goal = null)
        {
            var qi = new QueueItem(P)
            {
                isTroop     = true,
                QueueNumber = ConstructionQueue.Count,
                TroopType   = template.Name,
                Goal        = goal,
                Cost        = template.ActualCost,
                Rush        = P.Owner.RushAllConstruction,
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

        public bool Cancel(Building b)
        {
            QueueItem item = ConstructionQueue.Find(q => q.Building == b);
            item?.SetCanceled();
            return item != null;
        }

        public bool Cancel(Goal g)
        {
            QueueItem item = ConstructionQueue.Find(q => q.Goal == g);
            item?.SetCanceled();
            return item != null;
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

        public void PrioritizeShip(ShipDesign ship, int atPeace, int atWar = 4)
        {
            int queueOffset = Owner.IsAtWarWithMajorEmpire ? atWar : atPeace;
            if (ConstructionQueue.Count > queueOffset + 1)
                for (int i = queueOffset; i < ConstructionQueue.Count; ++i)
                {
                    QueueItem q = ConstructionQueue[i];
                    if (q.isShip && q.sData == ship)
                    {
                        MoveTo(queueOffset, i);
                        break;
                    }
                }
        }

        public void PrioritizeProjector(Vector2 buildPos)
        {
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isShip 
                    && q.DisplayName != null
                    && q.DisplayName.Contains("Subspace Projector")
                    && q.Goal?.BuildPosition == buildPos)
                {
                    MoveTo(0, i);
                    break;
                }
            }
        }

        public void RefitShipsBeingBuilt(Ship oldShip, Ship newShip)
        {
            float refitCost = oldShip.RefitCost(newShip);
            foreach (QueueItem q in ConstructionQueue)
            {
                if (q.isShip && q.sData.Name == oldShip.Name)
                {
                    float percentCompleted = q.ProductionSpent / q.ActualCost;
                    q.sData = newShip.shipData;
                    q.Cost = percentCompleted.AlmostZero() 
                           ? newShip.GetCost(Owner) 
                           : q.Cost + refitCost * percentCompleted * P.ShipBuildingModifier;

                }
            }
        }

        public void PrioritizeTroop()
        {
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isTroop)
                {
                    MoveTo(0, i);
                    break;
                }
            }
        }

        public bool ContainsTroopWithGoal(Goal g) => ConstructionQueue.Any(q => q.isTroop && q.Goal == g);

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

        public void MoveToAndContinuousRushFirstItem()
        {
            if (Empty)
                return;

            if (Count > 1)
                MoveTo(0, Count - 1);

            ConstructionQueue[0].Rush = true;
        }

        public void SwitchRushAllConstruction(bool rush)
        {
            for (int i = 0; i < ConstructionQueue.Count; ++i)
                 ConstructionQueue[i].Rush = rush;
        }

        public void ClearQueue()
        {
            ConstructionQueue.Clear();
            foreach (PlanetGridSquare tile in P.TilesList)
                tile.QItem = null; // Clear all planned buildings from tiles
        }

        public bool FirstItemCanFeedUs()
        {
            if (ConstructionQueue.Count == 0 || !ConstructionQueue[0].isBuilding)
                return false;

            QueueItem first = ConstructionQueue[0];
            return P.NonCybernetic && first.Building.ProducesFood
                   || P.IsCybernetic && first.Building.ProducesProduction;
        }
    }
}