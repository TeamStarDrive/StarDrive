using Ship_Game.Empires;
using Ship_Game.Empires.Components;

namespace Ship_Game.Ships.Components
{
    public class LoyaltyChanges
    {
        enum Type
        {
            None,
            Spawn,
            Boarded, BoardedNotify,
            Absorbed, AbsorbedNotify
        }

        Empire ChangeTo;
        Type ChangeType;

        public LoyaltyChanges(Ship ship, Empire loyalty)
        {
            ship.loyalty = loyalty;
            ChangeTo = null;
            ChangeType = Type.None;
        }

        // Need to use this as a proxy because of
        // ship.loyalty => LoyaltyTracker.CurrentLoyalty;
        // Classic chicken-egg paradox
        public void OnSpawn(Ship ship)
        {
            if (ship.loyalty != EmpireManager.Void)
                LoyaltyChangeDueToSpawn(ship, ship.loyalty);
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
            if (changeTo == null || changeTo == ship.loyalty)
                return false;

            Type type = ChangeType;
            ChangeTo = null;
            ChangeType = Type.None;
            bool loyaltyChanged = DoLoyaltyChange(ship, type, changeTo);
            if (loyaltyChanged)
                ship.loyalty.AddShipToManagedPools(ship);
            return loyaltyChanged;
        }

        static bool DoLoyaltyChange(Ship ship, Type type, Empire changeTo)
        {
            switch (type)
            {
                default:
                case Type.None: return false;
                case Type.Spawn:          LoyaltyChangeDueToSpawn(ship, changeTo);             return true;
                case Type.Boarded:        LoyaltyChangeDueToBoarding(ship, changeTo, false);   return true;
                case Type.BoardedNotify:  LoyaltyChangeDueToBoarding(ship, changeTo, true);    return true;
                case Type.Absorbed:       LoyaltyChangeDueToFederation(ship, changeTo, false); return true;
                case Type.AbsorbedNotify: LoyaltyChangeDueToFederation(ship, changeTo, true);  return true;
            }
        }

        static void LoyaltyChangeDueToSpawn(Ship ship, Empire newLoyalty)
        {
            ship.loyalty = newLoyalty;
            IEmpireShipLists newShips = newLoyalty;
            newShips.AddNewShipAtEndOfTurn(ship);
        }

        static void LoyaltyChangeDueToBoarding(Ship ship, Empire newLoyalty, bool notification)
        {
            Empire oldLoyalty = ship.loyalty;
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
            Empire oldLoyalty = ship.loyalty;
            SafelyTransferShip(ship, oldLoyalty, newLoyalty);

            // TODO: change to absorbed ship notification
            if (notification)
            {
                newLoyalty.AddBoardSuccessNotification(ship);
            }
        }

        static void SafelyTransferShip(Ship ship, Empire oldLoyalty, Empire newLoyalty)
        {
            // remove ship from fleet but do not add it back to empire pools.
            ship.fleet?.RemoveShip(ship, false);
            ship.AI.ClearOrders();

            ship.loyalty = newLoyalty;

            oldLoyalty.GetEmpireAI().ThreatMatrix.RemovePin(ship);
            ship.shipStatusChanged = true;
            ship.SwitchTroopLoyalty(oldLoyalty, newLoyalty);
            ship.ReCalculateTroopsAfterBoard();
            ship.ScuttleTimer = -1f; // Cancel any active self destruct
            ship.PiratePostChangeLoyalty();
            ship.IsGuardian = newLoyalty.WeAreRemnants;

            IEmpireShipLists oldShips = oldLoyalty;
            IEmpireShipLists newShips = newLoyalty;
            
            oldShips.RemoveShipAtEndOfTurn(ship);
            oldLoyalty.RemoveShipFromAIPools(ship);
            newShips.AddNewShipAtEndOfTurn(ship);;
        }
    }
}