using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Ships;

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
        public BatchRemovalCollection<QueueItem> ConstructionQueue = new BatchRemovalCollection<QueueItem>();

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

        public bool RushProduction(int itemIndex, float maxAmount = 1000f)
        {
            // don't allow rush if we're crippled
            if (IsCrippled || ConstructionQueue.IsEmpty || Owner == null)
                return false;

            float amount = Math.Min(ProductionHere, maxAmount);

            // inject artificial surplus to instantly rush & finish production
            if (Empire.Universe.Debug || System.Diagnostics.Debugger.IsAttached)
            {
                amount = SurplusThisTurn = 1000;
            }

            return ApplyProductionToQueue(maxAmount: amount, itemIndex);
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

            q.ProductionSpent += (spendMax - spend); // apply it

            // if we spent everything, this QueueItem is complete
            return spend <= 0f;
        }

        // @note `maxProduction` is a max limit, this method will attempt
        //       to consume no more than `maxAmount` from local production
        // @return true if at least some production was applied
        bool ApplyProductionToQueue(float maxAmount, int itemIndex)
        {
            if (maxAmount <= 0.0f || ConstructionQueue.IsEmpty)
                return false;

            // apply production to specified item
            if (ConstructionQueue.Count > itemIndex)
            {
                SpendProduction(ConstructionQueue[itemIndex], maxAmount);                            
            }

            for (int i = 0; i < ConstructionQueue.Count; )
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isTroop && !HasRoomForTroops()) // remove excess troops from queue
                {
                    Cancel(q);
                    continue;
                }
                if (q.IsComplete)
                {
                    bool ok = false;
                    if (q.isBuilding)   ok = OnBuildingComplete(q);
                    else if (q.isShip)  ok = OnShipComplete(q);
                    else if (q.isTroop) ok = TrySpawnTroop(q);
                    Finish(q, success: ok);
                    continue; // this item was removed, so skip
                }
                ++i;
            }
            ConstructionQueue.ApplyPendingRemovals();
            return true;
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
            q.pgs.PlaceBuilding(b);
            b.OnBuildingBuiltAt(P);
            return true;
        }

        bool TrySpawnTroop(QueueItem q)
        {
            Troop troop = ResourceManager.CreateTroop(q.TroopType, Owner);
            if (!troop.AssignTroopToTile(P))
                return false; // eek-eek
            q.Goal?.NotifyMainGoalCompleted();
            return true;
        }

        bool OnShipComplete(QueueItem q)
        {
            if (!ResourceManager.ShipTemplateExists(q.sData.Name))
                return false;

            Ship shipAt;
            if (q.isRefit)
                shipAt = Ship.CreateShipAt(q.sData.Name, Owner, P, true, q.RefitName, q.ShipLevel);
            else
                shipAt = Ship.CreateShipAt(q.sData.Name, Owner, P, true);

            if (q.sData.Role == ShipData.RoleName.station || q.sData.Role == ShipData.RoleName.platform)
            {
                shipAt.Position = FindNewStationLocation();
                shipAt.Center = shipAt.Position;
                shipAt.TetherToPlanet(P);
                P.OrbitalStations.Add(shipAt.guid, shipAt);
            }

            q.Goal?.ReportShipComplete(shipAt);
            if (!Owner.isPlayer)
                Owner.ForcePoolAdd(shipAt);
            return true;
        }

        bool IsStationAlreadyPresentAt(Vector2 position)
        {
            foreach (Ship orbital in P.OrbitalStations.Values)
            {
                Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                    orbital.Position, orbital.Radius, Color.LightCyan, 10.0f);
                if (position.InRadius(orbital.Position, orbital.Radius))
                    return true;
            }
            return false;
        }

        Vector2 FindNewStationLocation()
        {
            const int ringLimit = ShipBuilder.OrbitalsLimit / 9 + 1; // FB - limit on rings, based on Orbitals Limit
            for (int ring = 0; ring < ringLimit; ring++) 
            {
                int degrees    = (int)RandomMath.RandomBetween(0f, 9f);
                float distance = 2000 + 1000 * ring * P.Scale;
                Vector2 pos    = P.Center + MathExt.PointOnCircle(degrees * 40, distance);
                if (!IsStationAlreadyPresentAt(pos))
                    return pos;

                for (int i = 0; i < 9; i++) // FB - 9 orbitals per ring
                {
                    pos = P.Center + MathExt.PointOnCircle(i * 40, distance);
                    if (!IsStationAlreadyPresentAt(pos))
                        return pos;
                }
            }
            return P.Center; // There is a limit on orbitals number
        }
        // Applies available production to production queue
        public void AutoApplyProduction(float surplusFromPlanet)
        {
            // surplus will be reset every turn and consumed at first opportunity
            SurplusThisTurn = surplusFromPlanet;
            if (ConstructionQueue.IsEmpty)
                return;
            
            float percentToApply = 1f;
            if (P.CrippledTurns > 0) // massive sabotage to planetary facilities
            {
                percentToApply = 0.05f;
            }
            else if (P.RecentCombat) // ongoing combat is hindering logistics
            {
                percentToApply = 0.2f;
            }
            else if (P.colonyType != Planet.ColonyType.Colony)
            {
                if (P.PS == Planet.GoodState.STORE && P.Storage.ProdRatio < 0.66f)
                    percentToApply = 0.5f; // only apply 50% if AI is trying to store goods
            }
            ApplyProductionToQueue(maxAmount: ProductionHere*percentToApply, 0);
        }

        // @return TRUE if building was added to CQ,
        //         FALSE if `where` is occupied or if there is no free random tiles
        public bool AddBuilding(Building b, PlanetGridSquare where = null, bool playerAdded = false)
        {
            var qi = new QueueItem(P)
            {
                IsPlayerAdded = playerAdded,
                isBuilding = true,
                Building = b,
                pgs = where,
                Cost = b.ActualCost,
                ProductionSpent = 0.0f,
                NotifyOnEmpty = false,
                QueueNumber = ConstructionQueue.Count
            };

            // if not added by player, then skip biosphere, let it be handled below:
            if (playerAdded || !b.IsBiospheres)
            {
                if (b.AssignBuildingToTile(b, ref where, P))
                {
                    where.QItem = qi;
                    qi.pgs = where; // reset PGS if we got a new one
                    ConstructionQueue.Add(qi);
                    P.RefreshBuildingsWeCanBuildHere();
                    return true;
                }
                if (playerAdded) // no magic terraform hocus-pocus for players
                    return false;
            }

            // Try to Auto-build TerraFormer, since it's better than Biospheres
            if (Owner.NonCybernetic && P.Fertility < 1.0f)
            {
                if (ResourceManager.GetBuilding(Building.TerraformerId, out Building terraFormer))
                {
                    if (P.BuildingBuiltOrQueued(terraFormer))
                        return false;
                    if (Owner.IsBuildingUnlocked(terraFormer.Name) && P.WeCanAffordThis(terraFormer, P.colonyType))
                        return AddBuilding(ResourceManager.CreateBuilding(terraFormer.BID));
                }
            }

            return Owner.IsBuildingUnlocked(Building.BiospheresId)
                && TryBiosphereBuild(ResourceManager.CreateBuilding(Building.BiospheresId), qi);
        }

        public void AddPlatform(Ship platform, Ship constructor, Goal goal = null)
        {
            var qi = new QueueItem(P)
            {
                isShip        = true,
                Goal          = goal,
                NotifyOnEmpty = false,
                DisplayName = "Construction Ship",
                QueueNumber = ConstructionQueue.Count,
                sData       = constructor.shipData,
                Cost        = platform.GetCost(Owner)
            };
            if (goal != null) goal.PlanetBuildingAt = P;
            ConstructionQueue.Add(qi);
        }

        public void AddShip(Ship ship, Goal goal = null, bool notifyOnEmpty = true)
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

        public void AddTroop(Troop template, Goal goal = null)
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

        public void Finish(QueueItem q, bool success)
        {
            if (success) Finish(q);
            else         Cancel(q);
        }

        public void Finish(QueueItem q)
        {
            P.ConstructionQueue.Remove(q);
            q.OnComplete?.Invoke(success: true);
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
                if (q.Goal is BuildConstructionShip)
                {
                    Owner.GetEmpireAI().Goals.Remove(q.Goal);
                }

                if (q.Goal.Fleet != null)
                    Owner.GetEmpireAI().Goals.Remove(q.Goal);
            }
            P.ConstructionQueue.Remove(q);
            q.OnComplete?.Invoke(success: false);
        }


        public bool TryBiosphereBuild(Building b, QueueItem qi)
        {
            if (!b.IsBiospheres)
                return false;
            if (qi.isBuilding == false && P.ShortOnFood())
                return false;

            PlanetGridSquare[] list = P.TilesList.Filter(
                g => !g.Habitable && g.building == null && !g.Biosphere && g.QItem == null);

            if (list.Length == 0)
                return false;

            qi.pgs = RandomMath.RandItem(list);
            qi.pgs.QItem = qi;
            qi.Building = b;
            qi.isBuilding = true;
            qi.Cost = b.ActualCost;
            qi.ProductionSpent = 0.0f;
            qi.NotifyOnEmpty = false;
            ConstructionQueue.Add(qi);
            return true;
        }

        // Returns maintenance as a positive number
        public float TotalQueuedBuildingMaintenance()
        {
            float maintenance = 0;
            foreach (QueueItem b in ConstructionQueue)
                if (b.isBuilding) maintenance += b.Building.ActualMaintenance(P);
            return maintenance;
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