using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.Legacy;

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
        }

        // Makes sure two ShipData are absolutely equal
        static void AssertAreEqual(ShipData a, ShipData b, bool checkModules)
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
            Assert.AreEqual(a.CarrierShip, b.CarrierShip);

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
                Assert.AreEqual(a.ModuleSlots.Length, b.ModuleSlots.Length);
                for (int i = 0; i < a.ModuleSlots.Length; ++i)
                {
                    DesignSlot sa = a.ModuleSlots[i];
                    DesignSlot sb = b.ModuleSlots[i];
                    ShipModuleTests.AssertAreEqual(sa, sb);
                }
            }
        }

        static void AssertAreEqual(LegacyShipData a, ShipData b)
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
            Assert.AreEqual(a.CarrierShip, b.CarrierShip);

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
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.Style);
            Assert.AreEqual("ShipIcons/shuttle", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Shuttle/ship08", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.fighter, hull.Role);
            Assert.AreEqual(1, hull.Thrusters.Length);
            Assert.AreEqual(true, hull.Unlockable);
            Assert.AreEqual(10, hull.HullSlots.Length);
            Assert.AreEqual(10, hull.SurfaceArea);
            Assert.AreEqual(4, hull.Size.X);
            Assert.AreEqual(4, hull.Size.Y);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_VulcanScout()
        {
            ShipData design = ShipData.Parse("Content/ShipDesigns/Vulcan Scout.design");
            Assert.AreEqual("Vulcan Scout", design.Name);
            Assert.AreEqual("", design.ModName);
            Assert.AreEqual("Terran", design.ShipStyle);
            Assert.AreEqual("Terran/Shuttle", design.Hull);
            Assert.AreEqual("ShipIcons/shuttle", design.IconPath);
            Assert.AreEqual(ShipData.RoleName.fighter, design.Role);
            Assert.AreEqual(true, design.Unlockable);
            Assert.AreEqual(10, design.ModuleSlots.Length);
            Assert.AreEqual(10, design.GridInfo.SurfaceArea);
            Assert.AreEqual(4, design.GridInfo.Size.X);
            Assert.AreEqual(4, design.GridInfo.Size.Y);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranGunboat()
        {
            var hull = new ShipHull("Content/Hulls/Terran/Gunboat.hull");
            Assert.AreEqual("Terran/Gunboat", hull.HullName);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.Style);
            Assert.AreEqual("ShipIcons/10a", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.frigate, hull.Role);
            Assert.AreEqual(1, hull.Thrusters.Length);
            Assert.AreEqual(true, hull.Unlockable);
            Assert.AreEqual(70, hull.HullSlots.Length);
            Assert.AreEqual(70, hull.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate()
        {
            ShipData design = ShipData.Parse("Content/ShipDesigns/Prototype Frigate.design");
            Assert.AreEqual("Prototype Frigate", design.Name);
            Assert.AreEqual("", design.ModName);
            Assert.AreEqual("Terran", design.ShipStyle);
            Assert.AreEqual("Terran/Gunboat", design.Hull);
            Assert.AreEqual("ShipIcons/10a", design.IconPath);
            Assert.AreEqual(ShipData.RoleName.prototype, design.Role);
            Assert.AreEqual(true, design.Unlockable);
            Assert.AreEqual(40, design.ModuleSlots.Length);
            Assert.AreEqual(70, design.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate_NewDesign()
        {
            if (!File.Exists("Content/ShipDesigns/Prototype Frigate.xml"))
            {
                LegacyShipData old = LegacyShipData.Parse("Content/ShipDesigns/Prototype Frigate.xml", isHullDefinition:false);
                old.SaveDesign("Content/ShipDesigns/Prototype Frigate.design");
            }

            ShipData design = ShipData.Parse("Content/ShipDesigns/Prototype Frigate.design");
            Assert.AreEqual("Prototype Frigate", design.Name);
            Assert.AreEqual("", design.ModName);
            Assert.AreEqual("Terran", design.ShipStyle);
            Assert.AreEqual("Terran/Gunboat", design.Hull);
            Assert.AreEqual("ShipIcons/10a", design.IconPath);
            Assert.AreEqual(ShipData.RoleName.prototype, design.Role);
            Assert.AreEqual(true, design.Unlockable);
            Assert.AreEqual(40, design.ModuleSlots.Length); // new designs don't have dummy modules
            Assert.AreEqual(70, design.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_Clone_EqualToOriginal()
        {
            ShipData original = ShipData.Parse("Content/ShipDesigns/Prototype Frigate.design");
            ShipData clone = original.GetClone();
            AssertAreEqual(original, clone, true);
        }

        [TestMethod]
        public void ShipDesign_NewFormat_SaveLoad_EqualToOldFormat()
        {
            LegacyShipData legacy = LegacyShipData.Parse("Content/ShipDesigns/Prototype Frigate.xml", isHullDefinition:false);
            legacy.SaveDesign("Content/ShipDesigns/Prototype Frigate.design");
            
            ShipData neu = ShipData.Parse("Content/ShipDesigns/Prototype Frigate.design");
            AssertAreEqual(legacy, neu);
        }
    }
}
