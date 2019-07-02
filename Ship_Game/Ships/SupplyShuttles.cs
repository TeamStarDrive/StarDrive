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
        public void Launch(float radius)
        {
            //fbedard: for launch only
            //CG Omergawd. yes i tried getting rid of the orderby and cleaning this up
            //but im not willing to test that change here. I think i did some of this a long while back.  
            if (Owner == null || Owner.engineState == Ship.MoveState.Warp || !Owner.Carrier.HasSupplyBays)
                return;

            Ship[] shipsNeedingSupply = ShipsInNeedOfSupplyByPriority(FriendliesNearby, radius);

            int shipInNeedIndex    = 0;
            bool cantLaunchShuttle = false;

            //oh crap this is really messed up.  FB: working on it.
            foreach (ShipModule hangar in Owner.Carrier.AllActiveHangars.Filter(hangar => hangar.IsSupplyBay))
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

            if (supplyTarget != null || Owner.OrdnanceStatus < ShipStatus.Good)
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

        bool SupplyShipNeedsResupply(float shuttleStorage, bool HasSupplyTarget)
        {
            if (Owner.Ordinance >= shuttleStorage && HasSupplyTarget)
                return false;
            if (Owner.OrdnanceStatus >= ShipStatus.Good)
                return false;
            return true;
        }
        void CreateShuttle(ShipModule hangar)
        {
            Ship supplyShuttle     = Ship.CreateShipFromHangar(hangar, Owner.loyalty, Owner.Center, Owner);
            supplyShuttle.Velocity = UniverseRandom.RandomDirection() * supplyShuttle.Speed + Owner.Velocity;
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
