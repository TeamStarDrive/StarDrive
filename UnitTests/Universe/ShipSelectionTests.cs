using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Universe
{
    /// <summary>
    /// What Shift and a click, a double-click and the selected-ships panel do to the selection: a ship of the
    /// selection's side comes out of it or goes in, the panel and the right-click orders follow the new
    /// selection, and a selection that is a whole fleet again is the fleet.
    /// </summary>
    [TestClass]
    public class ShipSelectionTests : StarDriveTest
    {
        const string WarshipName = "Missileboat mk1-a";
        const string ScoutName = "Unarmed Scout";

        public ShipSelectionTests()
        {
            LoadStarterShips(WarshipName, ScoutName);
            CreateUniverseAndPlayerEmpire();
            Universe.ShipInfoUIElement ??= new(new Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
            Universe.shipListInfoUI ??= new(new Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        }

        Ship Warship(Empire owner, float x = 0) => SpawnShip(WarshipName, owner, new Vector2(x, 0));

        // the click, then the refresh the universe screen runs every frame
        void ShiftClick(Ship ship)
        {
            Universe.ShiftClickShip(ship);
            Universe.UpdateSelectedShips();
        }

        // the camera over a spot and one sim step, so the ships on screen and the player's sensors are current
        void LookDownAt(Vector2 position, double height)
        {
            Universe.CamPos = new Vector3d(position.X, position.Y, height);
            Universe.SetViewPerspective(Matrices.CreateLookAtDown(position.X, position.Y, -height), maxDistance: UniverseScreen.CAM_MAX);
            UState.Objects.EnableParallelUpdate = false;
            UState.Objects.Update(TestSimStep);
        }

        void Chase(Ship ship)
        {
            Universe.SetSelectedShip(ship);
            Universe.ShipToView = ship;
            Universe.ViewingShip = true;
        }

        static bool Holds(IReadOnlyList<Ship> list, Ship ship)
        {
            foreach (Ship s in list)
                if (s == ship)
                    return true;
            return false;
        }

        static void AssertHolds(IReadOnlyList<Ship> list, string what, params Ship[] ships)
        {
            Assert.AreEqual(ships.Length, list.Count, $"{what}: how many ships");
            foreach (Ship ship in ships)
                Assert.IsTrue(Holds(list, ship), $"{what}: {ship} is in it");
        }

        // One selected ship was put back alone every frame, so a second ship never stayed.
        [TestMethod]
        public void ShiftClickAddsASecondShipToOneSelectedShip()
        {
            Ship a = Warship(Player), b = Warship(Player, 500);
            Universe.SetSelectedShip(a);
            ShiftClick(b);

            AssertHolds(Universe.SelectedShips, "the selection", a, b);
            Assert.IsNull(Universe.SelectedShip, "two ships are a group, not one selected ship");
            AssertHolds(Universe.shipListInfoUI.ShownShips, "the panel", a, b);
        }

        // The selection changed but the panel kept its own old list, and its order buttons with it.
        [TestMethod]
        public void ShiftClickTakesAShipOutAndPutsItBackAndThePanelFollows()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), c = Warship(Player, 1000);
            Universe.SetSelectedShipList(new[] { a, b, c }, fleet: null);

            ShiftClick(b);
            AssertHolds(Universe.SelectedShips, "the selection", a, c);
            AssertHolds(Universe.shipListInfoUI.ShownShips, "the panel", a, c);

            ShiftClick(b);
            AssertHolds(Universe.SelectedShips, "the selection", a, b, c);
            AssertHolds(Universe.shipListInfoUI.ShownShips, "the panel", a, b, c);
        }

        [TestMethod]
        public void ShiftClickOnTheOnlySelectedShipDeselectsIt()
        {
            Ship a = Warship(Player);
            Universe.SetSelectedShip(a);
            ShiftClick(a);

            Assert.AreEqual(0, Universe.SelectedShips.Count, "the ship comes out of the selection");
            Assert.IsNull(Universe.SelectedShip, "and is no longer the selected ship");
        }

        // Right-click orders go to a selected fleet before the selected ships, so a changed selection
        // has to stop being the fleet, or the added ship stays behind and a removed one still moves.
        [TestMethod]
        public void ShiftClickingAFleetMakesItAGroupUntilItIsTheWholeFleetAgain()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), c = Warship(Player, 1000);
            Fleet fleet = Player.CreateFleet(1, null);
            fleet.AddShips(new[] { a, b });
            Universe.SetSelectedFleet(fleet);
            Assert.AreSame(fleet, Universe.SelectedFleet, "setup: the fleet is selected");

            ShiftClick(c);
            Assert.IsNull(Universe.SelectedFleet, "with a ship from outside it the orders go to the ships");
            AssertHolds(Universe.SelectedShips, "the selection", a, b, c);

            ShiftClick(c);
            Assert.AreSame(fleet, Universe.SelectedFleet, "taking that ship out leaves the whole fleet again");

            ShiftClick(b);
            Assert.IsNull(Universe.SelectedFleet, "part of a fleet is not the fleet");
            Assert.AreSame(a, Universe.SelectedShip, "the one ship left is the selected ship");
        }

        // A selection holding the other side's ships takes no orders, so the two never mix.
        [TestMethod]
        public void ShiftClickOnAShipOfTheOtherSideStartsANewSelection()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), enemy = Warship(Enemy, 1000);
            Universe.SetSelectedShipList(new[] { a, b }, fleet: null);

            ShiftClick(enemy);
            AssertHolds(Universe.SelectedShips, "the selection", enemy);
            Assert.AreSame(enemy, Universe.SelectedShip, "their ship is selected on its own");

            ShiftClick(a);
            AssertHolds(Universe.SelectedShips, "the selection", a);
            Assert.AreSame(a, Universe.SelectedShip, "and one of ours replaces it the same way");
        }

        // A ship alone in its fleet went to the list panel with no selected ship, so no command wheel.
        [TestMethod]
        public void ShiftClickOnTheirShipAloneInItsFleetSelectsItAsAPlainClickDoes()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), enemy = Warship(Enemy, 1000);
            Enemy.CreateFleet(1, null).AddShips(new[] { enemy });
            Universe.SetSelectedShipList(new[] { a, b }, fleet: null);

            ShiftClick(enemy);
            Assert.AreSame(enemy, Universe.SelectedShip, "their ship is the selected ship");
        }

        [TestMethod]
        public void ShiftClickKeepsTheCameraChase()
        {
            Ship a = Warship(Player), b = Warship(Player, 500);
            Chase(a);

            ShiftClick(b);
            Assert.IsTrue(Universe.ViewingShip, "adding a ship keeps the chase");
            ShiftClick(b);
            Assert.IsTrue(Universe.ViewingShip, "taking it out again keeps it");
            ShiftClick(a);
            Assert.IsTrue(Universe.ViewingShip, "and so does taking out the last ship");
        }

        [TestMethod]
        public void ShiftClickOnTheirShipEndsTheCameraChaseAsAPlainClickDoes()
        {
            Ship a = Warship(Player), enemy = Warship(Enemy, 1000);
            Chase(a);

            ShiftClick(enemy);
            Assert.IsFalse(Universe.ViewingShip, "a new selection ends the chase");
        }

        // The every-frame refresh ended the chase when a ship of the selected group was lost or landed.
        [TestMethod]
        public void LosingShipsOfTheGroupKeepsTheCameraChase()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), c = Warship(Player, 1000);
            Chase(a);
            ShiftClick(b);
            ShiftClick(c);

            c.Active = false;
            Universe.UpdateSelectedShips();
            AssertHolds(Universe.SelectedShips, "the selection", a, b);
            Assert.IsTrue(Universe.ViewingShip, "losing a ship of the group keeps the chase");

            b.Active = false;
            Universe.UpdateSelectedShips();
            Assert.AreSame(a, Universe.SelectedShip, "the one ship left is the selected ship");
            Assert.IsTrue(Universe.ViewingShip, "and the chase goes on");
        }

        // The waypoint lines of the selected ships show while this timer runs.
        [TestMethod]
        public void ShiftClickShowsTheWaypointLinesAgain()
        {
            Ship a = Warship(Player), b = Warship(Player, 500), c = Warship(Player, 1000);
            Universe.SetSelectedShipList(new[] { a, b, c }, fleet: null);
            Universe.SelectedSomethingTimer = 0f;

            ShiftClick(c);
            Assert.AreEqual(3f, Universe.SelectedSomethingTimer, "a Shift edit is a fresh selection");
        }

        [TestMethod]
        public void ThePanelFilterShowsTheWaypointLinesAgain()
        {
            Ship a = Warship(Player), b = Warship(Player, 500);
            Ship scout = SpawnShip(ScoutName, Player, new Vector2(1000, 0));
            Universe.SetSelectedShipList(new[] { a, b, scout }, fleet: null);
            Universe.SelectedSomethingTimer = 0f;

            Universe.shipListInfoUI.FilterShipList(s => s.Name == a.Name);
            Assert.AreEqual(3f, Universe.SelectedSomethingTimer, "a filtered selection is a fresh selection");
        }

        // Filtering by an icon in the panel left a whole fleet as a plain group of ships.
        [TestMethod]
        public void ThePanelFilterLeavingAWholeFleetSelectsTheFleet()
        {
            Ship a = Warship(Player), b = Warship(Player, 500);
            Ship scout = SpawnShip(ScoutName, Player, new Vector2(1000, 0));
            Fleet fleet = Player.CreateFleet(1, null);
            fleet.AddShips(new[] { a, b });
            Universe.SetSelectedShipList(new[] { a, b, scout }, fleet: null);

            Universe.shipListInfoUI.FilterShipList(s => s.Name == a.Name);
            AssertHolds(Universe.SelectedShips, "the selection", a, b);
            Assert.AreSame(fleet, Universe.SelectedFleet, "what is left is the whole fleet");
        }

        // The double-click went through every ship in the view, hidden ones included.
        [TestMethod]
        public void ADoubleClickTakesOnlyTheShipsThePlayerCanSee()
        {
            Warship(Player);
            Ship seen = Warship(Enemy, 2000), hidden = Warship(Enemy, 300_000);
            LookDownAt(new Vector2(150_000, 0), 1_500_000);
            Assert.IsTrue(Holds(UState.Objects.VisibleShips, hidden), "setup: the hidden ship is on screen");
            Assert.IsTrue(seen.InPlayerSensorRange, "setup: the near ship is in sensor range");
            Assert.IsFalse(hidden.InPlayerSensorRange, "setup: the far ship is not");

            Universe.SelectShipsLike(seen, ctrl: false, alt: false);
            AssertHolds(Universe.SelectedShips, "the selection", seen);
        }

        [TestMethod]
        public void ADoubleClickTakingAWholeFleetSelectsTheFleet()
        {
            Ship a = Warship(Player), b = Warship(Player, 500);
            Fleet fleet = Player.CreateFleet(1, null);
            fleet.AddShips(new[] { a, b });
            LookDownAt(new Vector2(250, 0), 20_000);

            Universe.SelectShipsLike(a, ctrl: false, alt: false);
            AssertHolds(Universe.SelectedShips, "the selection", a, b);
            Assert.AreSame(fleet, Universe.SelectedFleet, "all of the fleet and nothing else is the fleet");
        }
    }
}
