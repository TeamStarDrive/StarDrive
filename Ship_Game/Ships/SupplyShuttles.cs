﻿using SDUtils;
using SDGraphics;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public class SupplyShuttles
    {
        Ship Owner;

        public SupplyShuttles(Ship ship)
        {
            Owner = ship;
        }

        public void Dispose()
        {
            Owner = null;
        }

        /// <summary>
        /// <para>check if any friendly ships need supply.</para>
        /// loop through existing supply shuttles.<para />
        /// recall shuttles not on task or put them on task<para />
        /// supply mothership if nothing else to do.<para />
        /// </summary>
        /// <param name="radius">Generally Sensor range of ship</param>
        public void ProcessSupplyShuttles(float radius)
        {
            if (Owner == null || Owner.IsSpoolingOrInWarp || !Owner.Carrier.HasSupplyBays)
                return;

            if (TryGetShipsInNeedOfSupplyByPriority(radius, out Ship[] shipsNeedingSupply)
                || Owner.Carrier.HasSupplyShuttlesInSpace)
            {
                int shipInNeedIndex = 0;
                bool canLaunchAnotherShuttle = true;
                foreach (ShipModule hangar in Owner.Carrier.SupplyHangarsAlive)
                {
                    hangar.TryGetHangarShipActive(out Ship supplyShipInSpace);
                    if (shipInNeedIndex < shipsNeedingSupply.Length)
                    {
                        Ship supplyTarget = shipsNeedingSupply[shipInNeedIndex];
                        if (supplyShipInSpace != null)
                        {
                            if (supplyShipInSpace.AI.State != AIState.Ferrying && supplyShipInSpace.Ordinance > 0)
                                OrderIdleSupplyShips(supplyShipInSpace, supplyTarget);

                            continue;
                        }
                        else if (canLaunchAnotherShuttle)
                        {
                            canLaunchAnotherShuttle = LaunchShipSupplyShuttle(hangar, supplyTarget);
                        }
                        else
                        {
                            continue;
                        }

                        if (!ShipNeedsMoreOrdnance(supplyTarget))
                            shipInNeedIndex++;
                    }
                    else
                    {
                        if (supplyShipInSpace != null && SupplyShipIdle(supplyShipInSpace))
                            supplyShipInSpace.AI.OrderReturnToHangarDeferred();
                    }
                }
            }
        }


        bool LaunchShipSupplyShuttle(ShipModule hangar, Ship supplyTarget)
        {
            if (!hangar.Active || hangar.HangarTimer > 0f || hangar.IsHangarShipActive)
                return true; // This hangar cant launch, but others might

            if (!CarrierHasSupplyToLaunch(hangar))
                return false;

            Ship supplyShuttle = Ship.CreateShipFromHangar(Owner.Universe, hangar, Owner.Loyalty, Owner.Position, Owner);
            if (supplyShuttle != null)
            {
                supplyShuttle.ChangeOrdnance(-supplyShuttle.OrdinanceMax); // setting to 0 so it could load from mothership
                float supplyToLoad = supplyShuttle.OrdinanceMax.UpperBound(Owner.Ordinance);
                Owner.OnShipLaunched(supplyShuttle, hangar);
                supplyShuttle.ChangeOrdnance(supplyToLoad);
                Owner.ChangeOrdnance(-supplyToLoad);
                SetSupplyTarget(supplyShuttle, supplyTarget);
                return !SupplyShipNeedsResupply(supplyShuttle.OrdinanceMax);
            }
            else
            {
                return false;
            }
        }

        bool CarrierHasSupplyToLaunch(ShipModule hangar)
        {
            hangar.HangarShipUID = Owner.Loyalty.GetSupplyShuttleName();
            Ship supplyShuttleTemplate = ResourceManager.GetShipTemplate(hangar.HangarShipUID);
            return supplyShuttleTemplate.ShipOrdLaunchCost + (supplyShuttleTemplate.OrdinanceMax*0.75f) < Owner.Ordinance;
        }

        bool SupplyShipNeedsResupply(float shuttleStorage) => shuttleStorage * 0.25f > Owner.Ordinance;

        void SetSupplyTarget(Ship supplySource, Ship supplyTarget)
        {
            supplySource.AI.AddSupplyShipGoal(supplyTarget);          
            supplyTarget?.Supply.ChangeIncomingOrdnance(supplySource.Ordinance);            
        }

        static bool SupplyShipIdle(Ship supplyShip)
        {
            switch (supplyShip.AI.State)
            {
                case AIState.Ferrying:
                    return supplyShip.AI.OrderQueue.IsEmpty 
                        && supplyShip.OrdnancePercent > 0.02f 
                        && supplyShip.AI.EscortTarget != null;
                case AIState.ReturnToHangar:
                case AIState.Resupply:
                case AIState.Scrap:
                case AIState.MoveTo:
                case AIState.Orbit:
                    return false;

                default:
                    return true;
            }
        }

        bool ShipNeedsMoreOrdnance(Ship supplyTarget)
        {
            return supplyTarget.Supply.AcceptExternalSupply(SupplyType.Rearm);
        }

        bool AssignIdleSupplyShip(Ship supplyShipInSpace, Ship supplyTarget)
        {
            if (supplyShipInSpace?.Active != true || supplyShipInSpace.AI.HasPriorityOrder)
                return true;

            if (supplyTarget != null)
            {
                if (SupplyShipIdle(supplyShipInSpace))
                    SetSupplyTarget(supplyShipInSpace, supplyTarget);
                return false;
            }
            return true;
        }

        void OrderIdleSupplyShips(Ship supplyShipInSpace, Ship supplyTarget)
        {
            if (supplyShipInSpace == null) return;
            if (AssignIdleSupplyShip(supplyShipInSpace, supplyTarget))
            {
                if (SupplyShipIdle(supplyShipInSpace))
                    supplyShipInSpace.AI.OrderReturnToHangar();
            }
        }

        bool TryGetShipsInNeedOfSupplyByPriority(float sensorRange, out Ship[] shipsInNeed)
        {
            shipsInNeed = Owner.AI.FriendliesNearby
                .Filter(ship => ship.Active
                                && !ship.IsSupplyShuttle
                                && ship != Owner
                                && ship.Supply.AcceptExternalSupply(SupplyType.Rearm));

            shipsInNeed.Sort(ship =>
            {
                var distance = Owner.Position.Distance(ship.Position);
                distance = (int)distance * 10 / sensorRange;
                var supplyStatus = ship.Supply.ShipStatusWithPendingRearm();
                return (int)supplyStatus * distance + (ship.Fleet == Owner.Fleet ? 0 : 10);
            });

            return shipsInNeed.Length > 0;
        }
    }
}
