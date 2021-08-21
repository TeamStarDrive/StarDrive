using System;
using System.Collections.Generic;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Container for ShipTemplate's
    /// </summary>
    public class ShipsManager
    {
        readonly HashSet<string> Names = new HashSet<string>();

        // Ship designs, mapped by ship.Name
        readonly Map<string, Ship> ShipsMap = new Map<string, Ship>();
        readonly Map<string, ShipDesign> DesignsMap = new Map<string, ShipDesign>();

        readonly Array<Ship> AllShips = new Array<Ship>();
        readonly Array<ShipDesign> AllDesigns = new Array<ShipDesign>();

        readonly object Sync = new object();

        public IReadOnlyCollection<string> ShipNames => Names;
        public IReadOnlyList<Ship> Ships => AllShips;
        public IReadOnlyList<ShipDesign> Designs => AllDesigns;

        public ShipsManager()
        {
        }

        public void Clear()
        {
            foreach (var s in ShipsMap)
                s.Value.Dispose();

            Names.Clear();

            ShipsMap.Clear();
            DesignsMap.Clear();

            AllShips.Clear();
            AllDesigns.Clear();
        }

        public void Add(ShipDesign shipDesign, bool playerDesign, bool readOnly = false)
        {
            shipDesign.IsPlayerDesign   = playerDesign;
            shipDesign.IsReadonlyDesign = readOnly;

            Ship shipTemplate = Ship.CreateNewShipTemplate(shipDesign);
            if (shipTemplate == null) // happens if module creation failed
                return;

            string name = shipDesign.Name;

            lock (Sync)
            {
                if (!Names.Contains(name))
                {
                    Names.Add(name);
                    ShipsMap.Add(name, shipTemplate);
                    DesignsMap.Add(name, shipDesign);
                    AllShips.Add(shipTemplate);
                    AllDesigns.Add(shipDesign);
                }
                else // overwrite existing
                {
                    ShipsMap[name] = shipTemplate;
                    DesignsMap[name] = shipDesign;
                    AllShips.RemoveFirst(s => s.Name == name);
                    AllDesigns.RemoveFirst(s => s.Name == name);
                }
            }
        }

        public void Delete(string shipName)
        {
            if (DesignsMap.TryGetValue(shipName, out ShipDesign design))
            {
                ShipsMap.TryGetValue(shipName, out Ship ship);
                design.Deleted = true;

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
