using System;
using System.Collections.Generic;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.Empires.DataPackets
{
    public class LoyaltyLists : IDisposable
    {
        // These are the actual ships arrays, it's safe to add/remove at any time
        SafeArray<Ship> ActualOwnedShips      = new SafeArray<Ship>();
        SafeArray<Ship> ActualOwnedProjectors = new SafeArray<Ship>();
        readonly Empire Owner;
        bool ShipListChanged;
        bool ProjecterListChanged;

        public Ship[] OwnedShips      = new Ship[] { };
        public Ship[] OwnedProjectors = new Ship[] { };

        public LoyaltyLists(Empire empire)
        {
            Owner = empire;
        }

        public void Add(Ship ship)
        {
            if (ship.loyalty != Owner)
            {
                Log.Error($"Attempted to add ship without setting loyalty");
                return;
            } 

            if (ship.IsSubspaceProjector)
            {
                if (Empire.Universe.DebugWin != null && ActualOwnedProjectors.ContainsRef(ship))
                {
                    Log.Error($"Attempted to add an existing projector");                 
                }
                else
                {
                    ActualOwnedProjectors.Add(ship);
                    ProjecterListChanged = true;
                }
            }
            else
            {
                if (Empire.Universe.DebugWin != null && ActualOwnedShips.ContainsRef(ship))
                {
                    Log.Error($"Attempted to add an existing ship");                    
                }
                else
                {
                    ActualOwnedShips.Add(ship);
                    ShipListChanged = true;
                }
            }
        }

        public void Remove(Ship ship)
        {
            if (ship.IsSubspaceProjector)
            {                
                ActualOwnedProjectors.Remove(ship);
                ProjecterListChanged = true;
            }
            else
            {                
                ActualOwnedShips.Remove(ship);
                ShipListChanged = true;
            }
        }

        public void UpdatePublicLists()
        {
            if (ProjecterListChanged)
                OwnedProjectors = ActualOwnedProjectors.ToArray();
            if (ShipListChanged)
                OwnedShips = ActualOwnedShips.ToArray();
            
            ProjecterListChanged = false;
            ShipListChanged      = false;
        }

        public void CleanOut()
        {
            ActualOwnedProjectors = new SafeArray<Ship>();
            ActualOwnedShips      = new SafeArray<Ship>();
            OwnedProjectors       = null;
            OwnedShips            = null;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            CleanOut();
        }
    }
}