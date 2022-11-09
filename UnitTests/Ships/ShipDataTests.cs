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
            AssertEqual(a.Name, b.Name);
            AssertEqual(a.ModName, b.ModName);
            AssertEqual(a.ShipStyle, b.ShipStyle);
            AssertEqual(a.Hull, b.Hull);
            AssertEqual(a.IconPath, b.IconPath);

            AssertEqual(a.EventOnDeath, b.EventOnDeath);
            AssertEqual(a.SelectionGraphic, b.SelectionGraphic);

            AssertEqual(a.FixedUpkeep, b.FixedUpkeep);
            AssertEqual(a.FixedCost, b.FixedCost);
            AssertEqual(a.IsShipyard, b.IsShipyard);
            AssertEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
            AssertEqual(a.IsCarrierOnly, b.IsCarrierOnly);

            AssertEqual(a.Role, b.Role);
            AssertEqual(a.ShipCategory, b.ShipCategory);
            AssertEqual(a.HangarDesignation, b.HangarDesignation);
            AssertEqual(a.DefaultCombatState, b.DefaultCombatState);

            AssertEqual(a.GridInfo.SurfaceArea, b.GridInfo.SurfaceArea);
            AssertEqual(a.GridInfo.Size.X, b.GridInfo.Size.X);
            AssertEqual(a.GridInfo.Size.Y, b.GridInfo.Size.Y);

            AssertEqual(a.Unlockable, b.Unlockable);
            AssertEqualCollections(a.TechsNeeded, b.TechsNeeded);

            if (checkModules)
            {
                var aSlots = a.GetOrLoadDesignSlots();
                var bSlots = b.GetOrLoadDesignSlots();
                AssertEqual(aSlots.Length, bSlots.Length);
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
            AssertEqual(a.Name, b.Name);
            AssertEqual(a.ModName, b.ModName);
            AssertEqual(a.ShipStyle, b.ShipStyle);
            AssertEqual(a.Hull, b.Hull);
            AssertEqual(a.IconPath, b.IconPath);

            AssertEqual(a.EventOnDeath, b.EventOnDeath);
            AssertEqual(a.SelectionGraphic, b.SelectionGraphic);

            AssertEqual(a.FixedUpkeep, b.FixedUpkeep);
            AssertEqual(a.FixedCost, b.FixedCost);
            AssertEqual(a.IsShipyard, b.IsShipyard);
            AssertEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
            AssertEqual(a.CarrierShip, b.IsCarrierOnly);

            AssertEqual(a.Role.ToString(), b.Role.ToString());
            AssertEqual(a.ShipCategory.ToString(), b.ShipCategory.ToString());
            AssertEqual(a.HangarDesignation.ToString(), b.HangarDesignation.ToString());
            AssertEqual(a.CombatState, b.DefaultCombatState);

            AssertEqual(a.ThrusterList.Length, b.BaseHull.Thrusters.Length);
            for (int i = 0; i < a.ThrusterList.Length; ++i)
            {
                AssertEqual(a.ThrusterList[i].Position, b.BaseHull.Thrusters[i].Position);
                AssertEqual(a.ThrusterList[i].Scale, b.BaseHull.Thrusters[i].Scale);
            }
            
            AssertEqual(a.GridInfo.SurfaceArea, b.GridInfo.SurfaceArea);
            AssertEqual(a.GridInfo.Size.X, b.GridInfo.Size.X);
            AssertEqual(a.GridInfo.Size.Y, b.GridInfo.Size.Y);

            AssertEqual(a.UnLockable, b.Unlockable);
            AssertEqualCollections(a.TechsNeeded, b.TechsNeeded);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranShuttle()
        {
            var hull = new ShipHull("Content/Hulls/Terran/Shuttle.hull");
            AssertEqual("Terran/Shuttle", hull.HullName);
            AssertEqual("Shuttle", hull.VisibleName);
            AssertEqual("", hull.ModName);
            AssertEqual("Terran", hull.Style);
            AssertEqual("ShipIcons/shuttle", hull.IconPath);
            AssertEqual("Model/Ships/Terran/Shuttle/ship08", hull.ModelPath);
            AssertEqual(RoleName.fighter, hull.Role);
            AssertEqual(1, hull.Thrusters.Length);
            AssertEqual(true, hull.Unlockable);
            AssertEqual(12, hull.HullSlots.Length);
            AssertEqual(12, hull.SurfaceArea);
            AssertEqual(new Point(4,4), hull.Size);
        }

        [TestMethod]
        public void ShipHull_LoadVanilla_TerranGunboat()
        {
            var hull = new ShipHull("Content/Hulls/Terran/Gunboat.hull");
            AssertEqual("Terran/Gunboat", hull.HullName);
            AssertEqual("Gunboat", hull.VisibleName);
            AssertEqual("", hull.ModName);
            AssertEqual("Terran", hull.Style);
            AssertEqual("ShipIcons/10a", hull.IconPath);
            AssertEqual("Model/Ships/Terran/Gunboat/Gunboat", hull.ModelPath);
            AssertEqual(RoleName.frigate, hull.Role);
            AssertEqual(2, hull.Thrusters.Length);
            AssertEqual(true, hull.Unlockable);
            AssertEqual(70, hull.HullSlots.Length);
            AssertEqual(70, hull.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_VulcanScout()
        {
            ShipDesign design = ShipDesign.Parse("Content/ShipDesigns/Human/Vulcan Scout.design");
            AssertEqual("Vulcan Scout", design.Name);
            AssertEqual("", design.ModName);
            AssertEqual("Terran", design.ShipStyle);
            AssertEqual("Terran/Shuttle", design.Hull);
            AssertEqual("ShipIcons/shuttle", design.IconPath);
            AssertEqual(RoleName.fighter, design.Role);
            AssertEqual(true, design.Unlockable);
            AssertEqual(10, design.GetOrLoadDesignSlots().Length);
            AssertEqual(12, design.GridInfo.SurfaceArea);
            AssertEqual(new Point(4,4), design.GridInfo.Size);
        }

        [TestMethod]
        public void ShipDesign_LoadVanilla_PrototypeFrigate()
        {
            ShipDesign design = ShipDesign.Parse("Content/ShipDesigns/Prototypes/Terran-Prototype.design");
            AssertEqual("Terran-Prototype", design.Name);
            AssertEqual("", design.ModName);
            AssertEqual("Terran", design.ShipStyle);
            AssertEqual("Terran/LightCruiser", design.Hull);
            AssertEqual("ShipIcons/icon_LightCruiser", design.IconPath);
            AssertEqual(RoleName.prototype, design.Role);
            AssertEqual(true, design.Unlockable);
            AssertEqual(135, design.GetOrLoadDesignSlots().Length);
            AssertEqual(310, design.GridInfo.SurfaceArea);
        }

        [TestMethod]
        public void ShipDesign_Clone_EqualToOriginal()
        {
            ShipDesign original = ShipDesign.Parse("Content/ShipDesigns/Prototypes/Terran-Prototype.design");
            ShipDesign clone = original.GetClone(null);
            AssertAreEqual(original, clone, true);
        }
    }
}
