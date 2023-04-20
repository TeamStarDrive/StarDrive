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

        Empire ChangeTo;
        public Type ChangeType { get; private set; }

        public LoyaltyChanges(Ship ship, Empire loyalty)
        {
            ship.Loyalty = loyalty;
            ChangeTo = null;
            ChangeType = Type.None;
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
            ChangeTo = loyalty;
            ChangeType = Type.Spawn;
        }

        public void SetBoardingLoyalty(Empire loyalty, bool addNotification = true)
        {
            ChangeTo = loyalty;
            ChangeType = addNotification ? Type.BoardedNotify : Type.Boarded;
        }

        public void SetLoyaltyForAbsorbedShip(Empire loyalty, bool addNotification = true)
        {
            ChangeTo = loyalty;
            ChangeType = addNotification ? Type.AbsorbedNotify : Type.Absorbed;
        }

        /// <returns>TRUE if loyalty changed</returns>
        public bool Update(Ship ship)
        {
            Empire changeTo = ChangeTo;
            if (ChangeType != Type.Spawn && (changeTo == null || changeTo == ship.Loyalty))
                return false;

            Type type  = ChangeType;
            ChangeTo   = null;
            ChangeType = Type.None;
            return DoLoyaltyChange(ship, type, changeTo);
        }

        static bool DoLoyaltyChange(Ship ship, Type type, Empire changeTo)
        {
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

            // Spawned ships should not clear orders since some of them are given immediate orders
            // Like pirates and meteors
            if (type != Type.Spawn)
                ship.AI.ClearOrdersAndWayPoints();

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
            ship.SwitchTroopLoyalty(oldLoyalty, newLoyalty);
            ship.ReCalculateTroopsAfterBoard();
            ship.ScuttleTimer = -1f; // Cancel any active self destruct
            ship.PiratePostChangeLoyalty();
            ship.IsGuardian = newLoyalty.WeAreRemnants;

            (oldLoyalty as IEmpireShipLists).RemoveShipAtEndOfTurn(ship);
            (newLoyalty as IEmpireShipLists).AddNewShipAtEndOfTurn(ship);
        }
    }
}