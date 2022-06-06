using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.Legacy;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

namespace UnitTests.Ships
{
    /// <summary>
    /// Ensures ShipData is properly parsed, serialized, deserialized
    /// </summary>
    [TestClass]
    public class ShipDataTests : StarDriveTest
    {
        public ShipDataTests()
        {
            LoadStarterShips("Terran-Prototype");
        }

        // Makes sure two ShipData are absolutely equal
        public static void AssertAreEqual(ShipDesign a, ShipDesign b, bool checkModules)
        {
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.ModName, b.ModName);
            Assert.AreEqual(a.ShipStyle, b.ShipStyle);
            Assert.AreEqual(a.Hull, b.Hull);
            Assert.AreEqual(a.IconPath, b.IconPath);

            Assert.AreEqual(a.EventOnDeath, b.EventOnDeath);
            Assert.AreEqual(a.SelectionGraphic, b.SelectionGraphic);

            Assert.AreEqual(a.FixedUpkeep, b.FixedUpkeep);
            Assert.AreEqual(a.FixedCost, b.FixedCost);
            Assert.AreEqual(a.IsShipyard, b.IsShipyard);
            Assert.AreEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
            Assert.AreEqual(a.IsCarrierOnly, b.IsCarrierOnly);

            Assert.AreEqual(a.Role, b.Role);
            Assert.AreEqual(a.ShipCategory, b.ShipCategory);
            Assert.AreEqual(a.HangarDesignation, b.HangarDesignation);
            Assert.AreEqual(a.DefaultAIState, b.DefaultAIState);
            Assert.AreEqual(a.DefaultCombatState, b.DefaultCombatState);

            Assert.AreEqual(a.GridInfo.SurfaceArea, b.GridInfo.SurfaceArea);
            Assert.AreEqual(a.GridInfo.Size.X, b.GridInfo.Size.X);
            Assert.AreEqual(a.GridInfo.Size.Y, b.GridInfo.Size.Y);

            Assert.AreEqual(a.Unlockable, b.Unlockable);
            Assert.That.EqualCollections(a.TechsNeeded, b.TechsNeeded);

            if (checkModules)
            {
                var aSlots = a.GetOrLoadDesignSlots();
                var bSlots = b.GetOrLoadDesignSlots();
                Assert.AreEqual(aSlots.Length, bSlots.Length);
                for (int i = 0; i < aSlots.Length; ++i)
                {
                    DesignSlot sa = aSlots[i];
                    DesignSlot sb = bSlots[i];
                    ShipModuleTests.AssertAreEqual(sa, sb);
                }
            }
        }

        static void AssertAreEqual(LegacyShipData a, ShipDesign b)
        {
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.ModName, b.ModName);
            Assert.AreEqual(a.ShipStyle, b.ShipStyle);
            Assert.AreEqual(a.Hull, b.Hull);
            Assert.AreEqual(a.IconPath, b.IconPath);

            Assert.AreEqual(a.EventOnDeath, b.EventOnDeath);
            Assert.AreEqual(a.SelectionGraphic, b.SelectionGraphic);

            Assert.AreEqual(a.FixedUpkeep, b.FixedUpkeep);
            Assert.AreEqual(a.FixedCost, b.FixedCost);
            Assert.AreEqual(a.IsShipyard, b.IsShipyard);
            Assert.AreEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
            Assert.AreEqual(a.CarrierShip, b.IsCarrierOnly);

            Assert.AreEqual(a.Role.ToString(), b.Role.ToString());
            Assert.AreEqual(a.ShipCategory.ToString(), b.ShipCategory.ToString());
            Assert.AreEqual(a.HangarDesignation.ToString(), b.HangarDesignation.ToString());
            Assert.AreEqual(a.DefaultAIState, b.DefaultAIState);
            Assert.AreEqual(a.CombatState, b.DefaultCombatState);

            Assert.AreEqual(a.ThrusterList.Length, b.BaseHull.Thrusters.Length);
            for (int i = 0; i < a.ThrusterList.Length; ++i)
            {
                Assert.AreEqual(a.ThrusterList[i].Position, b.BaseHull.Thrusters[i].Position);
                Assert.AreEqual(a.ThrusterList[i].Scale, b.BaseHull.Thrusters[i].Scale);
            }
            
            Assert.AreEqual(a.GridInfo.SurfaceArea, b.GridInfo.SurfaceArea);
            Assert.AreEqual(a.GridInfo.Size.X, b.GridInfo.Size.X);
            Assert.AreEqual(a.GridInfo.Size.Y, b.GridInfo.Size.Y);

            Assert.AreEqual(a.UnLockable, b.Unlockable);
            Assert.That.EqualCollections(a.TechsNeeded, b.TechsNeeded);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranShuttle()
        {
            var hull = new ShipHull("Content/Hulls/Terran/Shuttle.hull");
            Assert.AreEqual("Terran/Shuttle", hull.HullName);
            Assert.AreEqual("Shuttle", hull.VisibleName);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.Style);
            Assert.AreEqual("ShipIcons/shuttle", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Shuttle/ship08", hull.ModelPath);
            Assert.AreEqual(RoleName.fighter, hull.Role);
            Assert.AreEqual(1, hull.Thrusters.Length);
            Assert.AreEqual(true, hull.Unlockable);
            Assert.AreEqual(12, hull.HullSlots.Length);
            Assert.AreEqual(12, hull.SurfaceArea);
            Assert.AreEqual(new Point(4,4), hull.Size);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranGunboat()
        {
            var hull = new ShipHull("Content/Hulls/Terran/Gunboat.hull");
            Assert.AreEqual("Terran/Gunboat", hull.HullName);
            Assert.AreEqual("Gunboat", hull.VisibleName);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.Style);
            Assert.AreEqual("ShipIcons/10a", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            Assert.AreEqual(RoleName.frigate, hull.Role);
            Assert.AreEqual(2, hull.Thrusters.Length);
            Assert.AreEqual(true, hull.Unlockable);
            Assert.AreEqual(70, hull.HullSlots.Length);
            Assert.AreEqual(70, hull.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_VulcanScout()
        {
            ShipDesign design = ShipDesign.Parse("Content/ShipDesigns/Human/Vulcan Scout.design");
            Assert.AreEqual("Vulcan Scout", design.Name);
            Assert.AreEqual("", design.ModName);
            Assert.AreEqual("Terran", design.ShipStyle);
            Assert.AreEqual("Terran/Shuttle", design.Hull);
            Assert.AreEqual("ShipIcons/shuttle", design.IconPath);
            Assert.AreEqual(RoleName.fighter, design.Role);
            Assert.AreEqual(true, design.Unlockable);
            Assert.AreEqual(10, design.GetOrLoadDesignSlots().Length);
            Assert.AreEqual(12, design.GridInfo.SurfaceArea);
            Assert.AreEqual(new Point(4,4), design.GridInfo.Size);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate()
        {
            ShipDesign design = ShipDesign.Parse("Content/ShipDesigns/Prototypes/Terran-Prototype.design");
            Assert.AreEqual("Terran-Prototype", design.Name);
            Assert.AreEqual("", design.ModName);
            Assert.AreEqual("Terran", design.ShipStyle);
            Assert.AreEqual("Terran/LightCruiser", design.Hull);
            Assert.AreEqual("ShipIcons/icon_LightCruiser", design.IconPath);
            Assert.AreEqual(RoleName.prototype, design.Role);
            Assert.AreEqual(true, design.Unlockable);
            Assert.AreEqual(135, design.GetOrLoadDesignSlots().Length);
            Assert.AreEqual(310, design.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_Clone_EqualToOriginal()
        {
            ShipDesign original = ShipDesign.Parse("Content/ShipDesigns/Prototypes/Terran-Prototype.design");
            ShipDesign clone = original.GetClone(null);
            AssertAreEqual(original, clone, true);
        }

        [TestMethod]
        public void ShipDesign_Base64_Serialization()
        {
            CreateUniverseAndPlayerEmpire("Human");
            Ship ship = SpawnShip("Terran-Prototype", Player, Vector2.Zero);

            // completely nulls this module, this catches empty serialization line bug
            ship.Modules[5].Health = 0f;

            ModuleSaveData[] toSave = ship.GetModuleSaveData();
            byte[] bytes = ShipDesign.GetModulesBytes(new ShipDesignWriter(), toSave, ship.ShipData);

            Log.Info(Encoding.ASCII.GetString(bytes));

            (ModuleSaveData[] loaded, _) = ShipDesign.GetModuleSaveFromBytes(bytes);

            for (int i = 0; i < toSave.Length && i < loaded.Length; ++i)
            {
                ShipModuleTests.AssertAreEqual(toSave[i], loaded[i]);
            }
            Assert.AreEqual(toSave.Length, loaded.Length, "Loaded modules are not the same length");
        }
    }
}
