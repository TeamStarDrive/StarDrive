using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    // Issue #283: a design shows one strength on the load screen, in the shipyard and on a new ship
    [TestClass]
    public class ShipStrengthTests : StarDriveTest
    {
        const string Shielded = "Dodaving mk1-a"; // shields boosted by amplifiers
        const string Carrier = "Muon Carrier Mk1"; // dynamic hangars
        const string Fighter = "Ving Defender";
        const string LightlyArmed = "Kuma Oki L"; // an armed freighter, weapons on under 10% of the hull and defense above offense
        const string Freighter = "Large Freighter";
        const string Scout = "Vingscout";

        public ShipStrengthTests()
        {
            LoadStarterShips(Shielded, Carrier, Fighter, LightlyArmed, Freighter, Scout);
            CreateUniverseAndPlayerEmpire("Human"); // a carrier with no buildable fighters falls back to their Vulcan Scout
        }

        static IShipDesign Design(string name) => ResourceManager.Ships.GetDesign(name);

        // amplifiers change a ship's strength by about 0.01%, the same sums in the same order differ by far less
        const float Precision = 0.000001f;

        // out of the camera, which would draw module deaths
        static readonly Vector2 FarAway = new(200_000);

        float ShipyardStrength(string name) => new DesignShip(UState, (ShipDesign)Design(name)).GetStrength();

        void AssertSameStrength(string name)
        {
            float design = Design(name).GetStrength(Player);
            AssertEqual(design * Precision, design, ShipyardStrength(name), $"{name}: the load screen and the shipyard show different strengths");
            AssertShipStrength(design, design, SpawnShip(name, Player, FarAway), $"{name}: a new ship");
        }

        // nothing left for the carrier's hangars to launch, whichever starter ships other tests loaded
        void RemoveBuildableShipsButTheCarrier()
        {
            foreach (IShipDesign design in Player.ShipsWeCanBuildSnapshot)
                if (design.Name != Carrier)
                    Player.RemoveBuildableShip(design);
        }

        static void AssertShipStrength(float expectedBase, float expectedCurrent, Ship ship, string what)
        {
            AssertEqual(expectedBase * Precision, expectedBase, ship.BaseStrength, $"{what} has the wrong strength in full working order");
            AssertEqual(expectedCurrent * Precision, expectedCurrent, ship.GetStrength(), $"{what} has the wrong current strength");
        }

        // from each module's own figures, with its shields as the ship powers and amplifies them
        static (float Offense, float Defense, int OffensiveArea) LiveModuleSums(Ship ship)
        {
            float offense = 0f, defense = 0f;
            int offensiveArea = 0;
            foreach (ShipModule m in ship.Modules)
            {
                offense += m.CalculateModuleOffense();
                defense += m.CalculateModuleDefense(ship.SurfaceArea);
                offensiveArea += ShipStrength.OffensiveArea(m);
            }
            return (offense, defense, offensiveArea);
        }

        static float StrengthOfLiveModules(Ship ship)
        {
            (float offense, float defense, int offensiveArea) = LiveModuleSums(ship);
            return ShipStrength.Combine(ship.SurfaceArea, offensiveArea, offense, defense);
        }

        static ShipModule FirstModule(Ship ship, Predicate<ShipModule> match, string what)
        {
            foreach (ShipModule m in ship.Modules)
                if (match(m))
                    return m;
            throw new AssertFailedException($"{ship.Name} has no {what}");
        }

        float DesignStrengthWithout(Ship ship, ShipModule removed)
        {
            DesignSlot[] slots = ship.ShipData.GetOrLoadDesignSlots();
            int index = Array.IndexOf(ship.Modules, removed);
            Assert.AreEqual(removed.UID, slots[index].ModuleUID, "setup: the ship's modules are not in the order of its design slots");
            var rest = new Array<DesignSlot>(slots);
            rest.RemoveAt(index);
            return ShipStrength.OfDesign(ship.ShipData, rest.ToArray(), Player);
        }

        Array<ShipModule> PlacedModules(ShipDesign design)
        {
            var placed = new Array<ShipModule>();
            foreach (DesignSlot slot in design.GetOrLoadDesignSlots())
            {
                ShipModule m = ShipModule.CreateDesignModule(UState, slot.ModuleUID, slot.ModuleRot, slot.TurretAngle, slot.HangarShipUID, design.BaseHull);
                m.Pos = slot.Pos;
                placed.Add(m);
            }
            return placed;
        }

        [TestMethod]
        public void ShieldsAndAmplifiersCountInTheDesignStrength()
        {
            AssertSameStrength(Shielded);

            TestShip ship = SpawnShip(Shielded, Player, FarAway);
            FirstModule(ship, m => m.IsAmplified, "amplified shields");
            float design = Design(Shielded).GetStrength(Player);
            AssertEqual(design * Precision, StrengthOfLiveModules(ship), design,
                "The design counts its shields differently from the ship's own shield modules");
        }

        [TestMethod]
        public void TheLowWeaponRuleIsTheSameForDesignsAndShips()
        {
            TestShip ship = SpawnShip(LightlyArmed, Player, FarAway);
            (float offense, float defense, int offensiveArea) = LiveModuleSums(ship);
            Assert.IsTrue(offensiveArea < ship.SurfaceArea * 0.1f && defense > offense,
                $"setup: {LightlyArmed} is not under the low weapon rule (weapons on {offensiveArea} of {ship.SurfaceArea}, offense {offense}, defense {defense})");

            float rule = 2f * offense + 0.1f * defense;
            AssertEqual(rule * Precision, rule, Design(LightlyArmed).GetStrength(Player), "The design does not follow the low weapon rule");
            AssertSameStrength(LightlyArmed);
        }

        [TestMethod]
        public void UnarmedSupportShipsHaveNoStrength()
        {
            AssertEqual(0f, Design(Scout).GetStrength(Player), "An unarmed scout design has strength");
            AssertEqual(0f, Design(Freighter).GetStrength(Player), "An unarmed freighter design has strength");
            AssertSameStrength(Scout);
            AssertSameStrength(Freighter);
            Assert.IsTrue(Design(Scout).BaseStrength > 0, "Without an empire the AI research picker needs the scout's defense counted");
        }

        [TestMethod]
        public void CarrierStrengthFollowsTheFightersTheEmpireCanBuild()
        {
            RemoveBuildableShipsButTheCarrier();
            float withoutFighter = Design(Carrier).GetStrength(Player);
            AssertSameStrength(Carrier);

            Player.AddBuildableShip(Design(Fighter));
            float withFighter = Design(Carrier).GetStrength(Player);
            Assert.AreNotEqual(withoutFighter, withFighter, "The carrier kept its strength from before the fighter could be built");
            AssertSameStrength(Carrier);
        }

        [TestMethod]
        public void BonusesGainedAfterBuildingDoNotMakeAnEqualDesignARefit()
        {
            TestShip oldShip = SpawnShip(Shielded, Player, FarAway);
            ShipDesign twin = ((ShipDesign)Design(Shielded)).GetClone("Strength Test Twin");
            ResourceManager.AddShipTemplate(twin, playerDesign: false);
            try
            {
                Player.AddBuildableShip(twin);
                float builtStrength = oldShip.BaseStrength;
                Player.data.Traits.ModHpModifier += 10f;
                EmpireHullBonuses.RefreshBonuses(Player);
                Assert.IsTrue(twin.GetStrength(Player) > builtStrength * 1.1f,
                    $"setup: the bonus does not raise the design past the refit threshold: {builtStrength} -> {twin.GetStrength(Player)}");
                Assert.IsNull(ShipBuilder.PickShipToRefit(oldShip, Player), "A design equal to the ship's own became a refit after a bonus tech");
            }
            finally
            {
                ResourceManager.Ships.Delete(twin.Name);
            }
        }

        [TestMethod]
        public void ShipsOfADeletedDesignKeepTheirStrength()
        {
            Player.data.Traits.ModHpModifier += 10f;
            EmpireHullBonuses.RefreshBonuses(Player);
            ShipDesign copy = ((ShipDesign)Design(Shielded)).GetClone("Strength Test Deleted Design");
            ResourceManager.AddShipTemplate(copy, playerDesign: false);
            TestShip ship;
            float strength;
            try
            {
                ship = SpawnShip(copy.Name, Player, FarAway);
                Assert.AreSame(copy, ship.ShipData, "setup: the ship is not of the copied design");
                strength = copy.GetStrength(Player);
                Assert.AreNotEqual(copy.BaseStrength, strength, "setup: the player's bonuses do not change the design's strength");
            }
            finally
            {
                // the shipyard refuses this while ships of the design exist, loading a save that holds two copies of a design does not
                ResourceManager.Ships.Delete(copy.Name);
            }
            Assert.IsTrue(copy.Deleted, "setup: the design was not deleted");

            Player.data.ShieldPowerMod += 0.5f;
            EmpireHullBonuses.RefreshBonuses(Player);
            RunObjectsSim(1.5f);
            AssertShipStrength(strength, strength, ship, "A ship of a deleted design after a bonus tech");

            ship.LoyaltyTracker.SetBoardingLoyalty(Enemy, addNotification: false); // an owner that never valued the design, as after a load
            RunObjectsSim(1.5f);
            Assert.AreEqual(Enemy, ship.Loyalty, "setup: the ship was not captured");
            AssertShipStrength(strength, strength, ship, "A ship of a deleted design whose owner never valued it");
            AssertEqual(copy.BaseStrength, copy.GetStrength(Enemy), "A deleted design is worth its strength without bonuses to an empire that never valued it");
        }

        [TestMethod]
        public void TheShipyardKeepsTheSameStrengthWhileEditing()
        {
            var design = (ShipDesign)Design(Carrier);
            var editing = new DesignShip(UState, design.GetClone(null));
            Array<ShipModule> placed = PlacedModules(design);
            foreach (ShipModule m in placed)
                m.CalculateModuleOffenseDefense(design.SurfaceArea); // the module panel shows it before it is placed
            editing.UpdateDesign(placed);

            float strength = design.GetStrength(Player);
            AssertEqual(strength * Precision, strength, editing.DesignStats.Strength,
                "Modules placed in the shipyard kept the strength they had before they were placed");
        }

        [TestMethod]
        public void TheShipyardValuesTheModulesPlacedNow()
        {
            var design = (ShipDesign)Design(Shielded);
            var editing = new DesignShip(UState, design.GetClone(null));
            Array<ShipModule> placed = PlacedModules(design);
            editing.UpdateDesign(placed);
            float full = editing.DesignStats.Strength;

            editing.UpdateDesign(placed.Filter(m => m.InstalledWeapon == null).ToArrayList());
            Assert.IsTrue(editing.DesignStats.Strength < full,
                $"The shipyard kept the strength of the removed weapons: {full} -> {editing.DesignStats.Strength}");
        }

        [TestMethod]
        public void StrengthFollowsTheEmpiresBonuses()
        {
            TestShip oldShip = SpawnShip(Shielded, Player, FarAway);
            RunObjectsSim(1.5f);
            float before = Design(Shielded).GetStrength(Player);

            Player.data.Traits.ModHpModifier += 0.5f;
            Player.data.ShieldPowerMod += 0.5f;
            EmpireHullBonuses.RefreshBonuses(Player);

            float after = Design(Shielded).GetStrength(Player);
            Assert.IsTrue(after > before, $"The design strength did not grow with the bonuses: {before} -> {after}");
            AssertSameStrength(Shielded);

            RunObjectsSim(1.5f); // ships compare their strength with their empire's once a second
            AssertShipStrength(after, after, oldShip, "A ship built before the research");
        }

        [TestMethod]
        public void ADamagedShipIsAsStrongAsItsDesignWithoutTheDestroyedModule()
        {
            TestShip ship = SpawnShip(Shielded, Player, FarAway);
            float design = Design(Shielded).GetStrength(Player);
            ShipModule weapon = FirstModule(ship, m => m.InstalledWeapon != null, "weapons");
            ShipModule amplifier = FirstModule(ship, m => m.AmplifyShields > 0f, "shield amplifiers");

            foreach (ShipModule destroyed in new[] { weapon, amplifier })
            {
                float expected = DesignStrengthWithout(ship, destroyed);
                Assert.IsTrue(expected < design, $"setup: the design is not weaker without its {destroyed.UID}");

                destroyed.SetHealth(0, "Test");
                ship.ShipStatusChange();
                AssertShipStrength(design, expected, ship, $"A ship with a destroyed {destroyed.UID}");

                destroyed.SetHealth(destroyed.ActualMaxHealth, "Test");
                ship.ShipStatusChange();
                AssertShipStrength(design, design, ship, $"A ship with a repaired {destroyed.UID}");
            }
        }

        [TestMethod]
        public void ADamagedShipKeepsItsStrengthThroughASave()
        {
            UState.StarDate = 1042.5f; // the loader needs a game past its first turns
            TestShip ship = SpawnShip(Shielded, Player, FarAway);
            RunObjectsSim(TestSimStep);
            ShipModule weapon = FirstModule(ship, m => m.InstalledWeapon != null, "weapons");
            float expected = DesignStrengthWithout(ship, weapon);
            weapon.SetHealth(0, "Test");
            ship.ShipStatusChange();
            AssertEqual(expected * Precision, expected, ship.GetStrength(), "setup: the damaged ship has the wrong strength");

            SavedGame save = Universe.Save("UnitTest.ShipStrength", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
            Ship loadedShip = loaded.UState.Objects.FindShip(ship.Id);
            Assert.IsNotNull(loadedShip, "setup: the ship was not in the save");
            float design = Design(Shielded).GetStrength(loaded.UState.Player);
            AssertShipStrength(design, expected, loadedShip, "A damaged ship after loading");
        }

        [TestMethod]
        public void CarriersTakeTheStrengthOfFightersTheEmpireCanNowBuild()
        {
            RemoveBuildableShipsButTheCarrier();
            TestShip carrier = SpawnShip(Carrier, Player, FarAway);
            RunObjectsSim(1.5f);
            float before = carrier.GetStrength();

            Player.AddBuildableShip(Design(Fighter));
            float after = Design(Carrier).GetStrength(Player);
            Assert.AreNotEqual(before, after, "setup: the new fighter does not change the carrier's strength");
            RunObjectsSim(1.5f);
            AssertShipStrength(after, after, carrier, "A carrier built before the fighter could be built");
        }

        [TestMethod]
        public void ACapturedShipTakesItsNewOwnersStrength()
        {
            Enemy.data.Traits.ModHpModifier += 0.5f;
            EmpireHullBonuses.RefreshBonuses(Enemy);
            float enemyStrength = Design(Shielded).GetStrength(Enemy);
            Assert.AreNotEqual(Design(Shielded).GetStrength(Player), enemyStrength, "setup: both empires build the design equally strong");

            TestShip ship = SpawnShip(Shielded, Player, FarAway);
            RunObjectsSim(TestSimStep);
            ship.LoyaltyTracker.SetBoardingLoyalty(Enemy, addNotification: false);
            RunObjectsSim(1.5f);
            Assert.AreEqual(Enemy, ship.Loyalty, "setup: the ship was not captured");
            AssertShipStrength(enemyStrength, enemyStrength, ship, "A captured ship");
        }
    }
}
