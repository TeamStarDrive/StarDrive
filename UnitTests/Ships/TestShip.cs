using Ship_Game;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    /// <summary>
    /// This Test wrapper around `Ship` class helps us get additional
    /// information needed for testing.
    ///
    /// You can inherit upon this to further mock some of the ship's behaviour
    /// </summary>
    [StarDataType]
    public class TestShip : Ship
    {
        public bool EnableDebugLogging;

        [StarDataConstructor] TestShip() : base() {}

        public TestShip(UniverseState u, Ship template, Empire owner, Vector2 position)
            : base(u, u.CreateId(), template, owner, position)
        {
        }

        [StarDataDeserialized(DeserializeBefore = new[]{ typeof(UniverseState) })]
        public new void OnDeserialized(UniverseState us)
        {
            base.OnDeserialized(us);
        }

        public int NumDieCalls; // TEST: # of times Die() has been called

        public override void Die(GameObject source, bool cleanupOnly)
        {
            if (EnableDebugLogging)
                Log.Write($"Ship.Die {this}");

            ++NumDieCalls;
            // always call cleanupOnly, because we don't want tumbling ships in unit tests
            base.Die(source, cleanupOnly: true);
        }

        // TEST: tracks currently dead modules
        public HashSet<ShipModule> DeadModules = new HashSet<ShipModule>();

        public int NumModuleDeaths; // TEST: # of times a module has died

        public override void OnHealthChange(float change, object source)
        {
            if (EnableDebugLogging)
                Log.Write($"Ship.OnHealthChange {change} source={source}  {this}");

            base.OnHealthChange(change, source);
        }

        public override void OnModuleDeath(ShipModule m)
        {
            if (EnableDebugLogging)
                Log.Write($"Ship.OnModuleDeath {m}");

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
            if (EnableDebugLogging)
                Log.Write($"Ship.OnModuleResurrect {m}");

            if (!DeadModules.Remove(m))
            {
                throw new InvalidOperationException($"OnModuleResurrect called twice: {m}");
            }

            ++NumModuleResurrects;
            base.OnModuleResurrect(m);
        }
        
        public int NumShipsLaunched; // TEST: # of times a hangar ship has launched
        
        public override void OnShipLaunched(Ship ship, ShipModule hangar)
        {
            if (EnableDebugLogging)
                Log.Write($"Carrier.OnShipLaunched {this} {ship}");

            ++NumShipsLaunched;
            base.OnShipLaunched(ship, hangar);
        }

        public int NumShipsReturned; // TEST: # of ships that have returned to hangar

        public override void OnShipReturned(Ship ship)
        {
            if (EnableDebugLogging)
                Log.Write($"Carrier.OnShipReturned {this} {ship}");

            ++NumShipsReturned;
            base.OnShipReturned(ship);
        }

        public override void OnWeaponInstalled(ShipModule m, Weapon w)
        {
            // for unit tests, we wrap the weapons so we can
            // easily edit the stats, without causing side-effects
            var tw = new WeaponTestWrapper(Universe, w, BaseHull);
            m.InstalledWeapon = tw;
            base.OnWeaponInstalled(m, tw);
        }
    }
}
