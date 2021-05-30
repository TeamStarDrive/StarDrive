namespace Ship_Game.Ships.DataPackets
{
    public class LoyaltyChanges
    {
        Empire BoardedShipNewLoyalty;
        Empire LoyaltyFoeSpawnedShip;
        readonly Ship Owner;

        public LoyaltyChanges(Ship ship)
        {
            Owner = ship;
        }

        public void SetBoardingLoyalty(Empire empire) => BoardedShipNewLoyalty = empire;
        public void SetLoyaltyForNewShip(Empire empire) => LoyaltyFoeSpawnedShip = empire;

        public bool ApplyAnyLoyaltyChanges(bool addNotification = true)
        {
            bool loyaltyChanged = false;
            if (BoardedShipNewLoyalty != null)
            {
                loyaltyChanged |= LoyaltyChangeDueToBoarding(addNotification); ;
            }
            if (LoyaltyFoeSpawnedShip != null)
            {
                Owner.loyalty.AddShip(Owner);
                LoyaltyFoeSpawnedShip = null;
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

            ship.loyalty = BoardedShipNewLoyalty;
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
                oldLoyalty.AddBoardedNotification(ship);
            }

            ship.loyalty.AddShip(ship);
            oldLoyalty.RemoveShip(ship);

            BoardedShipNewLoyalty = null;
            return true;
        }
    }
}