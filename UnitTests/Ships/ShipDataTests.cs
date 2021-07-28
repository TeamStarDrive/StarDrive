using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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
            Assert.AreEqual(a.ModelPath, b.ModelPath);

            Assert.AreEqual(a.Level, b.Level);
            Assert.AreEqual(a.experience, b.experience);
            Assert.AreEqual(a.EventOnDeath, b.EventOnDeath);
            Assert.AreEqual(a.SelectionGraphic, b.SelectionGraphic);

            Assert.AreEqual(a.MechanicalBoardingDefense, b.MechanicalBoardingDefense);
            Assert.AreEqual(a.FixedUpkeep, b.FixedUpkeep);
            Assert.AreEqual(a.FixedCost, b.FixedCost);
            Assert.AreEqual(a.Animated, b.Animated);
            Assert.AreEqual(a.IsShipyard, b.IsShipyard);
            Assert.AreEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
            Assert.AreEqual(a.CarrierShip, b.CarrierShip);

            Assert.AreEqual(a.CombatState, b.CombatState);
            Assert.AreEqual(a.Role, b.Role);
            Assert.AreEqual(a.ShipCategory, b.ShipCategory);
            Assert.AreEqual(a.HangarDesignation, b.HangarDesignation);
            Assert.AreEqual(a.DefaultAIState, b.DefaultAIState);

            Assert.AreEqual(a.ThrusterList.Length, b.ThrusterList.Length);
            for (int i = 0; i < a.ThrusterList.Length; ++i)
            {
                Assert.AreEqual(a.ThrusterList[i].Position, b.ThrusterList[i].Position);
                Assert.AreEqual(a.ThrusterList[i].Scale, b.ThrusterList[i].Scale);
            }
            
            Assert.AreEqual(a.GridInfo.SurfaceArea, b.GridInfo.SurfaceArea);
            Assert.AreEqual(a.GridInfo.Size.X, b.GridInfo.Size.X);
            Assert.AreEqual(a.GridInfo.Size.Y, b.GridInfo.Size.Y);

            Assert.AreEqual(a.BaseStrength, b.BaseStrength);
            Assert.AreEqual(a.UnLockable, b.UnLockable);
            Assert.AreEqual(a.HullUnlockable, b.HullUnlockable);
            Assert.AreEqual(a.AllModulesUnlockable, b.AllModulesUnlockable);
            Assert.That.EqualCollections(a.TechsNeeded, b.TechsNeeded);

            Assert.AreEqual(a.Volume, b.Volume);
            Assert.AreEqual(a.ModelZ, b.ModelZ);

            if (checkModules)
            {
                Assert.AreEqual(a.ModuleSlots.Length, b.ModuleSlots.Length);
                for (int i = 0; i < a.ModuleSlots.Length; ++i)
                {
                    ModuleSlotData sa = a.ModuleSlots[i];
                    ModuleSlotData sb = b.ModuleSlots[i];
                    ShipModuleTests.AssertAreEqual(sa, sb);
                }
            }
        }

        public static void AssertAllModulesEmpty(ShipData a)
        {
            for (int i = 0; i < a.ModuleSlots.Length; ++i)
                Assert.IsNull(a.ModuleSlots[i].ModuleUID);
        }

        static ShipData Parse(string path, bool isHull)
        {
            ShipData data = ShipData.Parse(new FileInfo(path), isHullDefinition:isHull);
            if (path.EndsWith(".xml"))
            {
                // one of the main changes, new designs have a specific module sorting order
                Array.Sort(data.ModuleSlots, ModuleSlotData.Sorter);
            }
            return data;
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranShuttle()
        {
            ShipData hull = Parse("Content/Hulls/Terran/Shuttle.xml", isHull:true);
            Assert.AreEqual("Shuttle", hull.Name);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.ShipStyle);
            Assert.AreEqual("Terran/Shuttle", hull.Hull);
            Assert.AreEqual("ShipIcons/shuttle", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Shuttle/ship08", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.fighter, hull.Role);
            Assert.AreEqual(1, hull.ThrusterList.Length);
            Assert.AreEqual(true, hull.UnLockable);
            Assert.AreEqual(false, hull.HullUnlockable);
            Assert.AreEqual(false, hull.AllModulesUnlockable);
            Assert.AreEqual(10, hull.ModuleSlots.Length);
            Assert.AreEqual(10, hull.GridInfo.SurfaceArea);
            Assert.AreEqual(4, hull.GridInfo.Size.X);
            Assert.AreEqual(4, hull.GridInfo.Size.Y);
            AssertAllModulesEmpty(hull);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_VulcanScout()
        {
            ShipData hull = Parse("Content/StarterShips/Vulcan Scout.xml", isHull:false);
            Assert.AreEqual("Vulcan Scout", hull.Name);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.ShipStyle);
            Assert.AreEqual("Terran/Shuttle", hull.Hull);
            Assert.AreEqual("ShipIcons/shuttle", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Shuttle/ship08", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.fighter, hull.Role);
            Assert.AreEqual(1, hull.ThrusterList.Length);
            Assert.AreEqual(true, hull.UnLockable);
            Assert.AreEqual(false, hull.HullUnlockable);
            Assert.AreEqual(false, hull.AllModulesUnlockable);
            Assert.AreEqual(10, hull.ModuleSlots.Length);
            Assert.AreEqual(10, hull.GridInfo.SurfaceArea);
            Assert.AreEqual(4, hull.GridInfo.Size.X);
            Assert.AreEqual(4, hull.GridInfo.Size.Y);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranGunboat()
        {
            ShipData hull = Parse("Content/Hulls/Terran/Gunboat.xml", isHull:true);
            Assert.AreEqual("Gunboat", hull.Name);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.ShipStyle);
            Assert.AreEqual("Terran/Gunboat", hull.Hull);
            Assert.AreEqual("ShipIcons/10a", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.frigate, hull.Role);
            Assert.AreEqual(1, hull.ThrusterList.Length);
            Assert.AreEqual(true, hull.UnLockable);
            Assert.AreEqual(false, hull.HullUnlockable);
            Assert.AreEqual(false, hull.AllModulesUnlockable);
            Assert.AreEqual(70, hull.ModuleSlots.Length);
            Assert.AreEqual(70, hull.GridInfo.SurfaceArea);
            AssertAllModulesEmpty(hull);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate()
        {
            ShipData hull = Parse("Content/SavedDesigns/Prototype Frigate.xml", isHull:false);
            Assert.AreEqual("Prototype Frigate", hull.Name);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.ShipStyle);
            Assert.AreEqual("Terran/Gunboat", hull.Hull);
            Assert.AreEqual("ShipIcons/10a", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.prototype, hull.Role);
            Assert.AreEqual(1, hull.ThrusterList.Length);
            Assert.AreEqual(true, hull.UnLockable);
            Assert.AreEqual(false, hull.HullUnlockable);
            Assert.AreEqual(false, hull.AllModulesUnlockable);
            Assert.AreEqual(70, hull.ModuleSlots.Length);
            Assert.AreEqual(70, hull.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate_NewDesign()
        {
            if (!File.Exists("Content/SavedDesigns/Prototype Frigate.xml"))
            {
                ShipData old = Parse("Content/SavedDesigns/Prototype Frigate.xml", isHull:false);
                old.Save(new FileInfo("Content/SavedDesigns/Prototype Frigate.design"));
            }

            ShipData hull = Parse("Content/SavedDesigns/Prototype Frigate.design", isHull:false);
            Assert.AreEqual("Prototype Frigate", hull.Name);
            Assert.AreEqual("", hull.ModName);
            Assert.AreEqual("Terran", hull.ShipStyle);
            Assert.AreEqual("Terran/Gunboat", hull.Hull);
            Assert.AreEqual("ShipIcons/10a", hull.IconPath);
            Assert.AreEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            Assert.AreEqual(ShipData.RoleName.prototype, hull.Role);
            Assert.AreEqual(1, hull.ThrusterList.Length);
            Assert.AreEqual(true, hull.UnLockable);
            Assert.AreEqual(false, hull.HullUnlockable);
            Assert.AreEqual(false, hull.AllModulesUnlockable);
            Assert.AreEqual(40, hull.ModuleSlots.Length); // new designs don't have dummy modules
            Assert.AreEqual(70, hull.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_Clone_EqualToOriginal()
        {
            ShipData original = Parse("Content/SavedDesigns/Prototype Frigate.xml", isHull:false);
            ShipData clone = new ShipData(original);
            AssertAreEqual(original, clone, true);
        }

        [TestMethod]
        public void ShipDesign_NewFormat_SaveLoad_EqualToOldFormat()
        {
            ShipData old = Parse("Content/SavedDesigns/Prototype Frigate.xml", isHull:false);
            old.Save(new FileInfo("Content/SavedDesigns/Prototype Frigate.design"));
            
            ShipData neu = Parse("Content/SavedDesigns/Prototype Frigate.design", isHull:false);
            
            // can't check modules because new designs don't have dummy modules
            AssertAreEqual(old, neu, checkModules:false);
        }
    }
}
