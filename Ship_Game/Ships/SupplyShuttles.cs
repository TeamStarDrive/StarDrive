using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public class SupplyShuttles
    {
        Ship Owner;
        Array<Ship> FriendliesNearby => Owner.AI.FriendliesNearby;

    public SupplyShuttles(Ship ship)
    {
            Owner = ship;
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
            if (Owner == null || Owner.engineState == Ship.MoveState.Warp || !Owner.Carrier.HasSupplyBays)
                return;

            Ship[] shipsNeedingSupply = ShipsInNeedOfSupplyByPriority(FriendliesNearby, radius);

            // nothing to do ¯\_(ツ)_/¯
            if (shipsNeedingSupply.Length == 0 && !SupplyShipNeedsResupply(0,false)
                                               && !Owner.Carrier.HasSupplyShuttlesInSpace)
            {
                return;
            }

            int shipInNeedIndex    = 0;
            bool cantLaunchShuttle = false;

            // Its Better!
            foreach (ShipModule hangar in Owner.Carrier.SupplyHangarsAlive)
            {
                Ship supplyShipInSpace = hangar.GetHangarShip();
                Ship supplyTarget = null;

                if (shipInNeedIndex < shipsNeedingSupply.Length)
                    supplyTarget = shipsNeedingSupply[shipInNeedIndex];

                if (supplyShipInSpace != null && supplyShipInSpace.Active)
                {
                    OrderIdleSupplyShips(supplyShipInSpace, supplyTarget);
                }
                else if (!cantLaunchShuttle)
                {
                    if (!LaunchShipSupplyShuttle(hangar, supplyTarget))
                        cantLaunchShuttle = true;
                }

                if (supplyTarget != null && !ShipNeedsMoreOrdnance(supplyTarget))
                    shipInNeedIndex++;
            }
        }
        bool LaunchShipSupplyShuttle(ShipModule hangar, Ship supplyTarget)
        {
            if (!hangar.Active || hangar.hangarTimer > 0f)
                return true;

            if (!CarrierHasSupplyToLaunch(hangar))
                return false;

            if (supplyTarget != null || SupplyShipNeedsResupply(0,false))
            {
                CreateShuttle(hangar);

                Ship supplyShuttle = hangar.GetHangarShip();
                supplyShuttle.ChangeOrdnance(-supplyShuttle.OrdinanceMax);
                if (!SupplyShipNeedsResupply(supplyShuttle.OrdinanceMax, supplyTarget != null))
                {
                    Owner.ChangeOrdnance(-supplyShuttle.OrdinanceMax);
                    supplyShuttle.ChangeOrdnance(supplyShuttle.OrdinanceMax);
                    SetSupplyTarget(supplyShuttle, supplyTarget);
                }
                else 
                {
                    supplyShuttle.AI.GoOrbitNearestPlanetAndResupply(true);
                }
            }
            return true;
        }

        bool CarrierHasSupplyToLaunch(ShipModule hangar)
        {
            hangar.hangarShipUID = Ship.GetSupplyShuttleName(Owner.loyalty);
            Ship supplyShuttleTemplate = ResourceManager.GetShipTemplate(hangar.hangarShipUID);

            return supplyShuttleTemplate.ShipOrdLaunchCost < Owner.Ordinance;
        }

        bool SupplyShipNeedsResupply(float shuttleStorage, bool hasSupplyTarget)
        {
            if (!hasSupplyTarget) 
                return Owner.OrdnanceStatus < ShipStatus.Maximum;

            return shuttleStorage > Owner.Ordinance;
        }
        void CreateShuttle(ShipModule hangar)
        {
            Ship supplyShuttle = Ship.CreateShipFromHangar(hangar, Owner.loyalty, Owner.Center, Owner);
            supplyShuttle.Velocity = Owner.Velocity + UniverseRandom.RandomDirection() * supplyShuttle.SpeedLimit;
            Owner.ChangeOrdnance(-supplyShuttle.ShipOrdLaunchCost);
            hangar.SetHangarShip(supplyShuttle);
        }

        void SetSupplyTarget(Ship supplySource, Ship supplyTarget)
        {
            supplySource.AI.AddSupplyShipGoal(supplyTarget);          
            supplyTarget.Supply.ChangeIncomingSupply(SupplyType.Rearm, supplySource.Ordinance);            
        }

        static bool SupplyShipIdle(Ship supplyShip)
        {
            switch (supplyShip.AI.State)
            {
                case AIState.Ferrying:
                    return supplyShip.AI.OrderQueue.IsEmpty && supplyShip.Ordinance > 1;
                case AIState.ReturnToHangar:
                case AIState.Resupply:
                case AIState.Scrap:
                    return false;
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
        Ship[] ShipsInNeedOfSupplyByPriority(Array<Ship> friendlyShips, float sensorRange)
        {
            Ship[] ShipsInNeed = friendlyShips.Filter(ship => ship.shipData.Role != ShipData.RoleName.supply
                                            && ship.Supply.AcceptExternalSupply(SupplyType.Rearm)
                                            && ship != Owner);
            ShipsInNeed.Sort(ship =>
            {
                var distance = Owner.Center.Distance(ship.Center);
                distance = (int)distance * 10 / sensorRange;
                var supplyStatus = ship.Supply.ShipStatusWithPendingResupply(SupplyType.Rearm);
                return (int)supplyStatus * distance + (ship.fleet == Owner.fleet ? 0 : 10);
            });
            return ShipsInNeed;
        }
    }
}
