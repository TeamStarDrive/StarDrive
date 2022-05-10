using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public interface IShipPool
    {
        int Id { get; }
        string Name { get; }
        Empire OwnerEmpire { get; }
        Array<Ship> Ships { get; }

        /// <summary>
        /// Add a new ship to this pool
        /// The ship will be automatically removed from any other IShipPool
        /// </summary>
        /// <returns>TRUE if added successfully</returns>
        bool Add(Ship ship);

        /// <summary>
        /// Tries to remove the ship from this pool
        /// </summary>
        /// <returns>TRUE if remove was successful</returns>
        bool Remove(Ship ship);

        /// <returns>TRUE if Ship.Pool == this</returns>
        bool Contains(Ship ship);
    }
}
