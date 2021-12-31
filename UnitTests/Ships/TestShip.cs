using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Ships
{
    /// <summary>
    /// This Test wrapper around `Ship` class helps us get additional
    /// information needed for testing.
    ///
    /// You can inherit upon this to further mock some of the ship's behaviour
    /// </summary>
    public class TestShip : Ship
    {
        public TestShip(UniverseScreen u, Ship template, Empire owner, Vector2 position)
            : base(u, template, owner, position)
        {
        }

        public int NumDieCalls; // TEST: # of times Die() has been called

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++NumDieCalls;
            // always call cleanupOnly, because we don't want tumbling ships in unit tests
            base.Die(source, cleanupOnly: true);
        }

        // TEST: tracks currently dead modules
        public HashSet<ShipModule> DeadModules = new HashSet<ShipModule>();

        public int NumModuleDeaths; // TEST: # of times a module has died

        public override void OnModuleDeath(ShipModule m)
        {
            if (!DeadModules.Add(m))
            {
                throw new InvalidOperationException($"OnModuleDeath called twice: {m}");
            }
            ++NumModuleDeaths;
            base.OnModuleDeath(m);
        }

        public int NumModuleResurrects; // TEST: # of times a module has been restored
        
        public override void OnModuleResurrect(ShipModule m)
        {
            if (!DeadModules.Remove(m))
            {
                throw new InvalidOperationException($"OnModuleResurrect called twice: {m}");
            }

            ++NumModuleResurrects;
            base.OnModuleResurrect(m);
        }
        
        public int NumShipsLaunched; // TEST: # of times a hangar ship has launched
        
        public override void OnShipLaunched(Ship ship)
        {
            ++NumShipsLaunched;
            base.OnShipLaunched(ship);
        }

        public int NumShipsReturned; // TEST: # of ships that have returned to hangar

        public override void OnShipReturned(Ship ship)
        {
            ++NumShipsReturned;
            base.OnShipReturned(ship);
        }
    }
}
