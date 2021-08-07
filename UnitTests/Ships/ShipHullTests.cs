using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.Legacy;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipHullTests : StarDriveTest
    {
        static void AssertAreEqual(HullSlot a, HullSlot b)
        {
            Assert.AreEqual(a.Pos, b.Pos);
            Assert.AreEqual(a.R, b.R);
        }

        static void AssertAreEqual(ShipHull a, ShipHull b, bool checkModules)
        {
            Assert.AreEqual(a.HullName, b.HullName);
            Assert.AreEqual(a.ModName, b.ModName);
            Assert.AreEqual(a.Style, b.Style);
            Assert.AreEqual(a.Description, b.Description);
            Assert.AreEqual(a.Size, b.Size);
            Assert.AreEqual(a.SurfaceArea, b.SurfaceArea);
            Assert.AreEqual(a.IconPath, b.IconPath);
            Assert.AreEqual(a.ModelPath, b.ModelPath);

            Assert.AreEqual(a.Role, b.Role);
            Assert.AreEqual(a.SelectIcon, b.SelectIcon);
            Assert.AreEqual(a.Animated, b.Animated);
            Assert.AreEqual(a.IsShipyard, b.IsShipyard);
            Assert.AreEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);

            Assert.AreEqual(a.Thrusters.Length, b.Thrusters.Length);
            for (int i = 0; i < a.Thrusters.Length; ++i)
            {
                Assert.AreEqual(a.Thrusters[i].Position, b.Thrusters[i].Position);
                Assert.AreEqual(a.Thrusters[i].Scale, b.Thrusters[i].Scale);
            }

            if (checkModules)
            {
                Assert.AreEqual(a.HullSlots.Length, b.HullSlots.Length);
                for (int i = 0; i < a.HullSlots.Length; ++i)
                {
                    HullSlot sa = a.HullSlots[i];
                    HullSlot sb = b.HullSlots[i];
                    AssertAreEqual(sa, sb);
                }
            }
        }

        static void AssertAreEqual(ShipData a, ShipHull b)
        {
            Assert.AreEqual(a.Hull, b.HullName);
            Assert.AreEqual(a.ModName, b.ModName);
            Assert.AreEqual(a.ShipStyle, b.Style);
            Assert.AreEqual(a.Description, b.Description);
            Assert.AreEqual(a.GridInfo.Size, b.Size);
            Assert.AreEqual(a.GridInfo.SurfaceArea, b.SurfaceArea);
            Assert.AreEqual(a.IconPath, b.IconPath);
            Assert.AreEqual(a.ModelPath, b.ModelPath);

            Assert.AreEqual(a.Role, b.Role);
            Assert.AreEqual(a.SelectionGraphic, b.SelectIcon);
            Assert.AreEqual(a.Animated, b.Animated);
            Assert.AreEqual(a.IsShipyard, b.IsShipyard);
            Assert.AreEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);

            Assert.AreEqual(a.ThrusterList.Length, b.Thrusters.Length);
        }

        /// <summary>
        /// NOTE: This test can be removed once we delete all XML designs
        /// Make sure all new hulls have matching information compared to the old XML files
        /// </summary>
        [TestMethod]
        public void NewShipHullsAreEqualToOldHulls()
        {
            FileInfo[] xmlHulls = Dir.GetFiles("Content/Hulls/", "xml");

            foreach (FileInfo xmlHullFile in xmlHulls)
            {
                LegacyShipData oldXmlHull = LegacyShipData.Parse(xmlHullFile, isHullDefinition: true);
                var newHullConverted = new ShipHull(oldXmlHull);

                var newHullFile = new FileInfo(Path.ChangeExtension(xmlHullFile.FullName, "hull"));
                newHullConverted.Save(newHullFile);

                var newHull = new ShipHull(newHullFile);

                AssertAreEqual(newHullConverted, newHull, true);
            }
        }
    }
}
