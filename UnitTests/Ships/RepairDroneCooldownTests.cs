using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    // ships with a repair drone bay cool their drone launchers once a second, and only those
    [TestClass]
    public class RepairDroneCooldownTests : StarDriveTest
    {
        const string DroneCarrier = "Dodaving mk1-a";
        readonly Ship Ship;
        readonly Weapon Gun;
        readonly Weapon Launcher;

        public RepairDroneCooldownTests()
        {
            LoadStarterShips(DroneCarrier, "TEST_Vulcan Scout");
            CreateUniverseAndPlayerEmpire();
            Ship = SpawnShip(DroneCarrier, Player, new Vector2(300_000, 300_000));
            SpawnShip("TEST_Vulcan Scout", Player, new Vector2(301_000, 300_000));
            Gun = Ship.Weapons.First(w => !w.IsRepairDrone && w.WeaponType != "Drone");
            Launcher = Ship.Weapons.First(w => w.IsRepairDrone);
        }

        void CoolFor(float seconds)
        {
            RunObjectsSim(2f);
            Assert.IsTrue(Ship.HasRepairModule, $"setup: {DroneCarrier} has no repair drone bay");
            Assert.IsTrue(Ship.AI.FriendliesNearby.Length > 0, "setup: the ship sees no friendly ship");
            Gun.CooldownTimer = 30f;
            Launcher.CooldownTimer = 30f;
            RunObjectsSim(seconds);
        }

        [TestMethod]
        public void OtherWeaponsCoolAtTheirOwnRate()
        {
            CoolFor(5f);
            AssertEqual(0.2f, 25f, Gun.CooldownTimer, $"{Gun.UID} cooled faster than time passed");
        }

        [TestMethod]
        public void TheDroneLauncherCoolsOneSecondPerSecond()
        {
            CoolFor(5f);
            AssertEqual(1.01f, 25f, Launcher.CooldownTimer, "the repair drone launcher did not cool about one second per second");
        }
    }
}
