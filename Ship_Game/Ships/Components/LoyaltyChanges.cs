using Ship_Game.Empires;
using Ship_Game.Empires.Components;

namespace Ship_Game.Ships.Components
{
    public class LoyaltyChanges
    {
        Empire BoardedShipNewLoyalty;
        Empire LoyaltyForSpawnedShip;
        Empire AbsorbedShipNewLoyalty;
        bool AddNotification = false;
        IEmpireShipLists CurrentEmpire;
        public Empire ShipOwner { get; private set; }

        readonly Ship Owner;

        public LoyaltyChanges(Ship ship, Empire empire)
        {
            Owner = ship;
            LoyaltyForSpawnedShip = empire;
            CurrentEmpire = empire;
            ShipOwner = empire;
        }

        public void SetBoardingLoyalty(Empire empire, bool addNotification = true)
        {
            BoardedShipNewLoyalty = empire;
            AddNotification       = addNotification;
        }

        public void SetLoyaltyForNewShip(Empire empire, bool addNotification = true)
        {
            LoyaltyForSpawnedShip = empire;
            AddNotification = addNotification;
        }

        public void SetLoyaltyForAbsorbedShip(Empire empire, bool addNotification = true)
        {
            AbsorbedShipNewLoyalty = empire;
            AddNotification = addNotification;
        }

        void SetCurrentLoyalty(Empire empire)
        {
            CurrentEmpire = empire;
            ShipOwner     = empire;
        }
        public bool ApplyAnyLoyaltyChanges()
        {
            bool loyaltyChanged = false;
            if (BoardedShipNewLoyalty != null)
            {
                loyaltyChanged |= LoyaltyChangeDueToBoarding(AddNotification); ;
            }
            if (LoyaltyForSpawnedShip != null)
            {
                SetCurrentLoyalty(LoyaltyForSpawnedShip);
                CurrentEmpire.AddNewShipAtEndOfTurn(Owner);
                LoyaltyForSpawnedShip = null;
                loyaltyChanged = true;
            }
            if (AbsorbedShipNewLoyalty != null)
            {
                LoyaltyChangeDueToFederation(AddNotification);
                AbsorbedShipNewLoyalty = null;
                loyaltyChanged = true;
            }
            return loyaltyChanged;
        }

        bool LoyaltyChangeDueToBoarding(bool notification)
        {
            var ship = Owner;
            Empire oldLoyalty = ship.loyalty;
            oldLoyalty.TheyKilledOurShip(BoardedShipNewLoyalty, ship);
            BoardedShipNewLoyalty.WeKilledTheirShip(oldLoyalty, ship);
            // remove ship from fleet but do not add it back to empire pools.
            ship.fleet?.RemoveShip(ship, false);
            ship.AI.ClearOrders();

            SetCurrentLoyalty(BoardedShipNewLoyalty);
            oldLoyalty.GetEmpireAI().ThreatMatrix.RemovePin(ship);
            ship.shipStatusChanged = true;
            ship.SwitchTroopLoyalty(oldLoyalty, ship.loyalty);
            ship.ReCalculateTroopsAfterBoard();
            ship.ScuttleTimer = -1f; // Cancel any active self destruct
            ship.PiratePostChangeLoyalty();
            ship.IsGuardian = BoardedShipNewLoyalty.WeAreRemnants;

            if (notification)
            {
                BoardedShipNewLoyalty.AddBoardSuccessNotification(ship);
                oldLoyalty.AddBoardedNotification(ship,BoardedShipNewLoyalty);
            }

            CurrentEmpire.AddNewShipAtEndOfTurn(ship);
            ((IEmpireShipLists) oldLoyalty).RemoveShipAtEndOfTurn(ship);

            BoardedShipNewLoyalty = null;
            return true;
        }

        bool LoyaltyChangeDueToFederation(bool notification)
        {
            var ship = Owner;
            Empire oldLoyalty = ship.loyalty;
            // remove ship from fleet but do not add it back to empire pools.
            ship.fleet?.RemoveShip(ship, false);
            ship.AI.ClearOrders();

            SetCurrentLoyalty(AbsorbedShipNewLoyalty);
            ship.shipStatusChanged = true;
            ship.SwitchTroopLoyalty(oldLoyalty, ship.loyalty);
            ship.ScuttleTimer = -1f; // Cancel any active self destruct
            ship.PiratePostChangeLoyalty();
            ship.IsGuardian = AbsorbedShipNewLoyalty.WeAreRemnants;

            // TODO: change to absorbed ship notification
            if (notification)
            {
                BoardedShipNewLoyalty.AddBoardSuccessNotification(ship);
                oldLoyalty.AddBoardedNotification(ship, BoardedShipNewLoyalty);
            }

            CurrentEmpire.AddNewShipAtEndOfTurn(ship);

            AbsorbedShipNewLoyalty = null;
            return true;
        }
    }
}