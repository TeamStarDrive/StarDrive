using System;
using System.Collections.Generic;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Container for ShipTemplates
    /// </summary>
    public class ShipsManager
    {
        readonly HashSet<string> Names = new HashSet<string>();

        // Ship designs, mapped by ship.Name
        readonly Map<string, ShipDesign> DesignsMap = new Map<string, ShipDesign>();
        readonly Map<string, Ship> ShipsMap = new Map<string, Ship>();
        readonly Array<ShipDesign> AllDesigns = new Array<ShipDesign>();
        readonly Array<Ship> AllShips = new Array<Ship>();

        readonly object Sync = new object();

        public IReadOnlyCollection<string> ShipNames => Names;
        public IReadOnlyList<ShipDesign> Designs => AllDesigns;
        public IReadOnlyList<Ship> Ships => AllShips;

        readonly Empire VoidEmpire = EmpireManager.Void; // thread-safety: initialize this lazy property only once

        public ShipsManager()
        {
        }

        public void Clear()
        {
            foreach (Ship s in AllShips)
                s.Dispose();

            foreach (ShipDesign s in AllDesigns)
                s.Dispose();

            Names.Clear();

            ShipsMap.Clear();
            DesignsMap.Clear();

            AllShips.Clear();
            AllDesigns.Clear();
        }

        // Add a new Design or replace an existing one
        public void Add(ShipDesign shipDesign, bool playerDesign, bool readOnly = false)
        {
            shipDesign.IsPlayerDesign   = playerDesign;
            shipDesign.IsReadonlyDesign = readOnly;

            Ship shipTemplate = Ship.CreateNewShipTemplate(VoidEmpire, shipDesign);
            if (shipTemplate == null) // happens if module creation failed
                return;

            string name = shipDesign.Name;

            lock (Sync)
            {
                // Delete existing, to allow overwrite
                if (DesignsMap.TryGetValue(name, out ShipDesign design))
                {
                    if (shipDesign == design)
                        return; // it's already added, deleting would corrupt it
                    else
                        Delete(name);
                }

                Names.Add(name);
                ShipsMap.Add(name, shipTemplate);
                DesignsMap.Add(name, shipDesign);
                AllShips.Add(shipTemplate);
                AllDesigns.Add(shipDesign);
            }
        }

        public void Delete(string shipName)
        {
            if (DesignsMap.TryGetValue(shipName, out ShipDesign design))
            {
                Ship ship = ShipsMap[shipName];
                design.Deleted = true;
                design.Dispose();
                ship.Dispose();

                Names.Remove(shipName);
                ShipsMap.Remove(shipName);
                DesignsMap.Remove(shipName);
                AllShips.Remove(ship);
                AllDesigns.Remove(design);
            }
        }

        public bool Exists(string shipName)
        {
            return Names.Contains(shipName);
        }

        public bool Get(string shipName, out Ship template)
        {
            return ShipsMap.TryGetValue(shipName, out template);
        }

        public Ship Get(string shipName, bool throwIfError = true)
        {
            if (throwIfError)
                return ShipsMap[shipName];

            ShipsMap.TryGetValue(shipName, out Ship ship);
            return ship;
        }

        public bool GetDesign(string shipName, out ShipDesign template)
        {
            return DesignsMap.TryGetValue(shipName, out template);
        }

        public ShipDesign GetDesign(string shipName, bool throwIfError = true)
        {
            if (throwIfError)
                return DesignsMap[shipName];

            DesignsMap.TryGetValue(shipName, out ShipDesign ship);
            return ship;
        }
    }
}
