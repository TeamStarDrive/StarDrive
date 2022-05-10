using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.Empires.Components
{
    public class LoyaltyLists
    {
        // These are the actual ships arrays, it's safe to add/remove at any time
        SafeArray<Ship> ActualOwnedShips      = new SafeArray<Ship>();
        SafeArray<Ship> ActualOwnedProjectors = new SafeArray<Ship>();
        readonly Empire Owner;
        bool ShipListChanged;
        bool ProjecterListChanged;

        public Ship[] OwnedShips { get; private set; } = Empty<Ship>.Array;
        public Ship[] OwnedProjectors { get; private set; } = Empty<Ship>.Array;

        public LoyaltyLists(Empire empire)
        {
            Owner = empire;
        }

        public void Add(Ship ship)
        {
            if (ship.Loyalty != Owner)
            {
                Log.Error($"Attempted to add ship without setting loyalty {ship}");
                return;
            }

            if (ship.IsSubspaceProjector)
            {
                // Need to use Contains() here instead of ContainsRef() because of the SafeArray impl
                if (ship.Universe?.DebugWin != null && ActualOwnedProjectors.Contains(ship))
                {
                    Log.Error($"Attempted to add an existing projector {ship}");
                }
                else
                {
                    ActualOwnedProjectors.Add(ship);
                    ProjecterListChanged = true;
                }
            }
            else
            {
                if (ship.Universe?.DebugWin != null && ActualOwnedShips.Contains(ship))
                {
                    Log.Error($"Attempted to add an existing ship {ship}");
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

        public void Clear()
        {
            ActualOwnedProjectors.Clear();
            ActualOwnedShips.Clear();
            OwnedProjectors = Empty<Ship>.Array;
            OwnedShips      = Empty<Ship>.Array;
        }
    }
}