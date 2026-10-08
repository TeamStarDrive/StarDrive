using System.Threading;
using Ship_Game.Empires;
using Ship_Game.Empires.Components;

namespace Ship_Game.Ships.Components
{
    public class LoyaltyChanges
    {
        public enum Type
        {
            None,
            Spawn,
            Boarded, BoardedNotify,
            Absorbed, AbsorbedNotify
        }

        sealed class Pending
        {
            public readonly Empire To;
            public readonly Type Type;

            public Pending(Empire to, Type type)
            {
                To = to;
                Type = type;
            }
        }

        Pending Change;

        public LoyaltyChanges(Ship ship, Empire loyalty)
        {
            ship.Loyalty = loyalty;
        }

        // Need to use this as a proxy because of
        // ship.loyalty => LoyaltyTracker.CurrentLoyalty;
        // Classic chicken-egg paradox
        public void OnSpawn(Ship ship)
        {
            if (ship.Loyalty != Empire.Void)
                SetLoyaltyForNewShip(ship.Loyalty);
        }

        // Loyalty change is ignored if loyalty == CurrentLoyalty
        public void SetLoyaltyForNewShip(Empire loyalty)
        {
            Change = new(loyalty, Type.Spawn);
        }

        public void SetBoardingLoyalty(Empire loyalty, bool addNotification = true)
        {
            Change = new(loyalty, addNotification ? Type.BoardedNotify : Type.Boarded);
        }

        public void SetLoyaltyForAbsorbedShip(Empire loyalty, bool addNotification = true)
        {
            Change = new(loyalty, addNotification ? Type.AbsorbedNotify : Type.Absorbed);
        }

        /// <returns>TRUE if loyalty changed</returns>
        public bool Update(Ship ship)
        {
            if (Volatile.Read(ref Change) == null)
                return false;

            Pending change = Interlocked.Exchange(ref Change, null);
            if (change == null || change.Type != Type.Spawn && (change.To == null || change.To == ship.Loyalty))
                return false;

            return DoLoyaltyChange(ship, change.Type, change.To);
        }

        static bool DoLoyaltyChange(Ship ship, Type type, Empire changeTo)
        {
            // Spawned ships should not clear orders since some of them are given immediate orders
            // like pirates and meteors. Must run BEFORE the transfer handlers: orders they
            // issue for the new owner (e.g. pirate flee-home) must survive.
            if (type != Type.Spawn)
                ship.AI.ClearOrdersAndWayPoints();

            switch (type)
            {
                default:
                case Type.None:                                                                break;
                case Type.Spawn:          LoyaltyChangeDueToSpawn(ship, changeTo);             break;
                case Type.Boarded:        LoyaltyChangeDueToBoarding(ship, changeTo, false);   break;
                case Type.BoardedNotify:  LoyaltyChangeDueToBoarding(ship, changeTo, true);    break;
                case Type.Absorbed:       LoyaltyChangeDueToFederation(ship, changeTo, false); break;
                case Type.AbsorbedNotify: LoyaltyChangeDueToFederation(ship, changeTo, true);  break;
            }

            return true;
        }

        static void LoyaltyChangeDueToSpawn(Ship ship, Empire newLoyalty)
        {
            ship.Loyalty = newLoyalty;
            IEmpireShipLists newShips = newLoyalty;
            newShips.AddNewShipAtEndOfTurn(ship);
        }

        static void LoyaltyChangeDueToBoarding(Ship ship, Empire newLoyalty, bool notification)
        {
            Empire oldLoyalty = ship.Loyalty;
            if (ship.IsSubspaceProjector)
                oldLoyalty.AI.SpaceRoadsManager.RemoveProjectorFromRoadList(ship);

            ship.RemoveFromPoolAndFleet(clearOrders: true);
            oldLoyalty.TheyKilledOurShip(newLoyalty, ship);
            newLoyalty.WeKilledTheirShip(oldLoyalty, ship);
            SafelyTransferShip(ship, oldLoyalty, newLoyalty);
            newLoyalty.ResetTargetsForShipsTargetingAfterBoarding(ship);

            if (notification)
            {
                newLoyalty.AddBoardSuccessNotification(ship);
                oldLoyalty.AddBoardedNotification(ship, newLoyalty);
            }
        }

        static void LoyaltyChangeDueToFederation(Ship ship, Empire newLoyalty, bool notification)
        {
            Empire oldLoyalty = ship.Loyalty;
            SafelyTransferShip(ship, oldLoyalty, newLoyalty);

            // TODO: change to absorbed ship notification
            if (notification)
            {
                newLoyalty.AddBoardSuccessNotification(ship);
            }
        }

        static void SafelyTransferShip(Ship ship, Empire oldLoyalty, Empire newLoyalty)
        {
            // ship shouldn't stay in any pools or fleets after being captured or federated
            // also, any orders should be cleared the ensure new owner can assign suitable jobs
            ship.RemoveFromPoolAndFleet(clearOrders: true);

            // set the new loyalty before updating anything else
            ship.Loyalty = newLoyalty;

            // empire's border scan nodes and subspace influence needs to be transferred
            ship.Universe.UpdateShipInfluence(ship, oldLoyalty, newLoyalty);

            ship.ShipStatusChanged = true;
            ship.StrengthOutdated = true;
            ship.SwitchTroopLoyalty(oldLoyalty, newLoyalty);
            ship.ReCalculateTroopsAfterBoard();
            ship.ScuttleTimer = -1f; // Cancel any active self destruct
            ship.LandShip?.OnOwnerChanged();
            ship.PiratePostChangeLoyalty();
            ship.IsGuardian = newLoyalty.WeAreRemnants;

            (oldLoyalty as IEmpireShipLists).RemoveShipAtEndOfTurn(ship);
            (newLoyalty as IEmpireShipLists).AddNewShipAtEndOfTurn(ship);

            if (ship.IsShipyard)
                ship.GetTether()?.UpdateShipyards();
        }
    }
}