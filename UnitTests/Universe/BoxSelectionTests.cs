using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Universe
{
    /// <summary>
    /// What a drag-box selects (issue #298). The rectangle holds every visible ship whatever its
    /// loyalty, so an enemy inside it used to decide what kind of box this was and throw away the
    /// player's own ships.
    /// </summary>
    [TestClass]
    public class BoxSelectionTests : StarDriveTest
    {
        const string ScoutName = "Unarmed Scout";
        const string TroopName = "Human Troop";
        const string WarshipName = "Missileboat mk1-a";

        public BoxSelectionTests()
        {
            LoadStarterShips(ScoutName, TroopName, WarshipName);
            CreateUniverseAndPlayerEmpire();
        }

        Ship Scout(Empire owner) => SpawnShip(ScoutName, owner, new Vector2(100));
        Ship Trooper(Empire owner) => SpawnShip(TroopName, owner, new Vector2(200));
        Ship Warship(Empire owner) => SpawnShip(WarshipName, owner, new Vector2(300));

        static Array<Ship> Select(Ship[] inBox, Ship[] alreadySelected = null,
                                  bool shift = false, bool ctrl = false, bool alt = false)
            => UniverseScreen.FilterBoxSelection(inBox, alreadySelected ?? Empty<Ship>.Array,
                                                 shift, ctrl, alt, out _);

        // These pin the properties the selection rules key on, not just the ship names. Arm the
        // scout design or reclassify the missileboat and the tests above would quietly stop
        // telling the two kinds of box apart.
        [TestMethod]
        public void TheShipsUsedHereAreTheKindsTheRulesTalkAbout()
        {
            Ship scout = Scout(Player), trooper = Trooper(Player), warship = Warship(Player);

            Assert.IsFalse(scout.IsSingleTroopShip, "the scout must not be a troop ship");
            Assert.AreEqual(0, scout.Weapons.Count, "the scout must be unarmed");
            Assert.IsFalse(scout.Carrier.HasFighterBays, "an unarmed carrier would still count as a combat ship");

            Assert.IsTrue(trooper.IsSingleTroopShip, "test needs a single troop transport");

            Assert.IsTrue(warship.Weapons.Count > 0, "test needs an armed ship");
            Assert.IsTrue(warship.ShipData.Role > RoleName.freighter, "a freighter counts as non combat whatever it carries");
            Assert.AreNotEqual(ShipCategory.Civilian, warship.ShipData.ShipCategory, "a civilian counts as non combat");
            Assert.AreNotEqual(RoleName.troop, warship.DesignRole);
        }

        // A player box holding both kinds drops the transports as non combat. Undocumented, and
        // unchanged here, but it is the rule the enemy-warship case was being confused with.
        [TestMethod]
        public void APlayerBoxWithWarshipsDropsThePlayersTransports()
        {
            Ship warship = Warship(Player);
            Array<Ship> selected = Select(new[] { warship, Trooper(Player) });

            Assert.AreEqual(1, selected.Count, "a combat box should give the combat ships");
            Assert.AreSame(warship, selected[0]);
        }

        // An enemy transport satisfied the prefer-transports filter, so the player's own ship was
        // discarded for not being one. The selection then came back empty.
        [TestMethod]
        public void AnEnemyTransportDoesNotStripThePlayersScout()
        {
            Ship scout = Scout(Player);
            Array<Ship> selected = Select(new[] { scout, Trooper(Enemy) });

            Assert.AreEqual(1, selected.Count, "an enemy troop transport in the box emptied the selection");
            Assert.AreSame(scout, selected[0]);
        }

        // The same mistake one block up: an enemy warship made the box look like a combat box,
        // and the player's unarmed ship was removed as a non-combat ship.
        [TestMethod]
        public void AnEnemyWarshipDoesNotStripThePlayersScout()
        {
            Ship scout = Scout(Player);
            Array<Ship> selected = Select(new[] { scout, Warship(Enemy) });

            Assert.AreEqual(1, selected.Count, "an enemy warship in the box emptied the selection");
            Assert.AreSame(scout, selected[0]);
        }

        [TestMethod]
        public void CtrlTakesEverythingOfThePlayersIncludingTransports()
        {
            Ship scout = Scout(Player);
            Ship trooper = Trooper(Player);
            Array<Ship> selected = Select(new[] { scout, trooper }, ctrl: true);

            Assert.AreEqual(2, selected.Count, "ctrl means everything of mine, transports included");
            Assert.IsTrue(selected.Contains(scout) && selected.Contains(trooper));
        }

        // Deliberate and kept: a box of your own civilians that holds transports gives you the
        // transports, which is what you want mid-invasion. Ctrl is the way past it.
        [TestMethod]
        public void APlainDragStillPrefersThePlayersOwnTransports()
        {
            Ship trooper = Trooper(Player);
            Array<Ship> selected = Select(new[] { Scout(Player), trooper });

            Assert.AreEqual(1, selected.Count, "a plain drag over civilians should prefer the transports");
            Assert.AreSame(trooper, selected[0]);
        }

        // Counting transports over the running selection rather than the box would break this:
        // the carried transport would make the box look like a transport box and drop the scout.
        [TestMethod]
        public void ShiftAddingKeepsACarriedTransportAndTheNewScout()
        {
            Ship carried = Trooper(Player);
            Ship scout = Scout(Player);
            Array<Ship> selected = Select(new[] { scout }, new[] { carried }, shift: true);

            Assert.AreEqual(2, selected.Count, "shift-add dropped a ship because of what was already selected");
            Assert.IsTrue(selected.Contains(carried) && selected.Contains(scout));
        }

        [TestMethod]
        public void AltTakesTheEnemyShipsAndLeavesOursAlone()
        {
            Ship enemy = Warship(Enemy);
            Array<Ship> selected = Select(new[] { Scout(Player), enemy }, alt: true);

            Assert.AreEqual(1, selected.Count, "alt should select the other side only");
            Assert.AreSame(enemy, selected[0]);
        }

        [TestMethod]
        public void AnEmptyBoxSelectsNothing()
        {
            Assert.AreEqual(0, Select(Empty<Ship>.Array).Count);
        }
    }
}
