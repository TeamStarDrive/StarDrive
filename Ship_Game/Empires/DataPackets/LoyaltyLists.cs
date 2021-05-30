using System;
using System.Collections.Generic;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.Empires.DataPackets
{
    public class LoyaltyLists : IDisposable
    {
        // These are the actual ships arrays, it's safe to add/remove at any time
        SafeArray<Ship> ActualOwnedShips = new SafeArray<Ship>();
        SafeArray<Ship> ActualOwnedProjectors = new SafeArray<Ship>();
        readonly Empire Owner;
        bool ShipListChanged;
        bool ProjectListChanged;

        public Ship[] OwnedShips      = new Ship[] { };
        public Ship[] OwnedProjectors = new Ship[] { };

        public LoyaltyLists(Empire empire)
        {
            Owner = empire;
        }

        public void Add(Ship ship)
        {
            if (ship.IsSubspaceProjector)
            {
                ProjectListChanged = true;
                ActualOwnedProjectors.Add(ship);
            }
            else
            {
                ShipListChanged = true;
                ActualOwnedShips.Add(ship);
            }
        }

        public void Remove(Ship ship)
        {
            if (ship.IsSubspaceProjector)
            {
                ProjectListChanged = true;
                ActualOwnedProjectors.Remove(ship);
            }
            else
            {
                ShipListChanged = true;
                ActualOwnedShips.Remove(ship);
            }
        }

        public void UpdatePublicLists()
        {
            if (ProjectListChanged)
                OwnedProjectors = ActualOwnedProjectors.ToArray();
            if (ShipListChanged)
                OwnedShips = ActualOwnedShips.ToArray();
            
            ProjectListChanged = false;
            ShipListChanged    = false;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ActualOwnedProjectors = null;
            ActualOwnedShips      = null;
            OwnedProjectors       = null;
            OwnedShips            = null;
        }
    }
}