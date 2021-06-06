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

        public Empire CurrentLoyalty { get; private set; }
        Empire ChangeTo;
        Type ChangeType;

        public LoyaltyChanges(Empire loyalty)
        {
            CurrentLoyalty = loyalty;
            ChangeTo = null;
            ChangeType = Type.None;
        }

        // Need to use this as a proxy because of
        // ship.loyalty => LoyaltyTracker.CurrentLoyalty;
        // Classic chicken-egg paradox
        public void OnSpawn(Ship ship)
        {
            LoyaltyChangeDueToSpawn(ship, CurrentLoyalty);
        }
        
        // Loyalty change is ignored if loyalty == CurrentLoyalty
        public void SetLoyaltyForNewShip(Empire loyalty)
        {
            if (loyalty == CurrentLoyalty)
                return;
            ChangeTo = loyalty;
            ChangeType = Type.Spawn;
        }

        public void SetBoardingLoyalty(Empire loyalty, bool addNotification = true)
        {
            if (loyalty == CurrentLoyalty)
                return;
            ChangeTo = loyalty;
            ChangeType = addNotification ? Type.BoardedNotify : Type.Boarded;
        }

        public void SetLoyaltyForAbsorbedShip(Empire loyalty, bool addNotification = true)
        {
            if (loyalty == CurrentLoyalty)
                return;
            ChangeTo = loyalty;
            ChangeType = addNotification ? Type.AbsorbedNotify : Type.Absorbed;
        }

        /// <returns>TRUE if loyalty changed</returns>
        public bool Update(Ship ship)
        {
            Empire changeTo = ChangeTo;
            if (changeTo == null || changeTo == CurrentLoyalty)
                return false;

            Type type = ChangeType;
            ChangeTo = null;
            ChangeType = Type.None;
            bool loyaltyChanged = DoLoyaltyChange(ship, type, changeTo);
            if (loyaltyChanged)
                ship.loyalty.AddShipToManagedPools(ship);
            return loyaltyChanged;
        }

        private bool DoLoyaltyChange(Ship ship, Type type, Empire changeTo)
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

        void LoyaltyChangeDueToSpawn(Ship ship, Empire newLoyalty)
        {
            CurrentLoyalty = newLoyalty;
            IEmpireShipLists newShips = newLoyalty;
            newShips.AddNewShipAtEndOfTurn(ship);
        }

        void LoyaltyChangeDueToBoarding(Ship ship, Empire newLoyalty, bool notification)
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

        void LoyaltyChangeDueToFederation(Ship ship, Empire newLoyalty, bool notification)
        {
            Empire oldLoyalty = ship.loyalty;
            SafelyTransferShip(ship, oldLoyalty, newLoyalty);

            // TODO: change to absorbed ship notification
            if (notification)
            {
                newLoyalty.AddBoardSuccessNotification(ship);
            }
        }

        void SafelyTransferShip(Ship ship, Empire oldLoyalty, Empire newLoyalty)
        {
            // remove ship from fleet but do not add it back to empire pools.
            ship.fleet?.RemoveShip(ship, false);
            ship.AI.ClearOrders();

            // This will change ship.loyalty getter
            CurrentLoyalty = newLoyalty;

            oldLoyalty.GetEmpireAI().ThreatMatrix.RemovePin(ship);
            ship.shipStatusChanged = true;
            ship.SwitchTroopLoyalty(oldLoyalty, ship.loyalty);
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