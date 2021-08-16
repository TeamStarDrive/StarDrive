using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Container for ShipTemplate's
    /// </summary>
    public class ShipsManager
    {
        // Ship designs, mapped by ship.Name
        readonly Map<string, Ship> ShipsMap = new Map<string, Ship>();
        readonly Map<string, ShipDesign> DesignsMap = new Map<string, ShipDesign>();

        readonly HashSet<string> Names = new HashSet<string>();
        readonly Array<ShipDesign> Designs = new Array<ShipDesign>();

        readonly object Sync = new object();

        public ShipsManager()
        {
        }

        public void Clear()
        {
            foreach (var s in ShipsMap)
                s.Value.Dispose();

            ShipsMap.Clear();
            DesignsMap.Clear();
            Names.Clear();
            Designs.Clear();
        }

        public IReadOnlyList<ShipDesign> GetShips()
        {
            return Designs;
        }

        public HashSet<string> GetShipNames()
        {
            return Names;
        }

        public void Add(ShipDesign shipDesign, bool playerDesign, bool readOnly = false)
        {
            shipDesign.IsPlayerDesign   = playerDesign;
            shipDesign.IsReadonlyDesign = readOnly;

            Ship shipTemplate = Ship.CreateNewShipTemplate(shipDesign);
            if (shipTemplate == null) // happens if module creation failed
                return;

            lock (Sync)
            {
                if (!Names.Contains(shipDesign.Name))
                {
                    ShipsMap.Add(shipTemplate.Name, shipTemplate);
                    DesignsMap.Add(shipDesign.Name, shipDesign);
                    Names.Add(shipDesign.Name);
                    Designs.Add(shipDesign);
                }
            }
        }

        public void Delete(string shipName)
        {
            if (DesignsMap.TryGetValue(shipName, out ShipDesign template))
            {
                template.Deleted = true;
                ShipsMap.Remove(shipName);
                DesignsMap.Remove(shipName);
                Names.Remove(shipName);
                Designs.Remove(template);
            }
        }

        public Ship Get(string shipName, bool throwIfError = true)
        {
            if (throwIfError)
                return ShipsMap[shipName];

            ShipsMap.TryGetValue(shipName, out Ship ship);
            return ship;
        }

        public bool Get(string shipName, out Ship template)
        {
            return ShipsMap.TryGetValue(shipName, out template);
        }

        public bool Exists(string shipName)
        {
            return ShipsMap.ContainsKey(shipName);
        }
    }
}
