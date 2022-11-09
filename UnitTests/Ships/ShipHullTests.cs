using System;
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
            AssertEqual(a.Pos, b.Pos);
            AssertEqual(a.R, b.R);
        }

        static void AssertAreEqual(ShipHull a, ShipHull b, bool checkModules)
        {
            AssertEqual(a.HullName, b.HullName);
            AssertEqual(a.ModName, b.ModName);
            AssertEqual(a.Style, b.Style);
            AssertEqual(a.Description, b.Description);
            AssertEqual(a.Size, b.Size);
            AssertEqual(a.SurfaceArea, b.SurfaceArea);
            AssertEqual(a.IconPath, b.IconPath);
            AssertEqual(a.ModelPath, b.ModelPath);

            AssertEqual(a.Role, b.Role);
            AssertEqual(a.SelectIcon, b.SelectIcon);
            AssertEqual(a.Animated, b.Animated);
            AssertEqual(a.IsShipyard, b.IsShipyard);
            AssertEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);

            AssertEqual(a.Thrusters.Length, b.Thrusters.Length);
            for (int i = 0; i < a.Thrusters.Length; ++i)
            {
                AssertEqual(a.Thrusters[i].Position, b.Thrusters[i].Position);
                AssertEqual(a.Thrusters[i].Scale, b.Thrusters[i].Scale);
            }

            if (checkModules)
            {
                AssertEqual(a.HullSlots.Length, b.HullSlots.Length);
                for (int i = 0; i < a.HullSlots.Length; ++i)
                {
                    HullSlot sa = a.HullSlots[i];
                    HullSlot sb = b.HullSlots[i];
                    AssertAreEqual(sa, sb);
                }
            }
        }

        static void AssertAreEqual(ShipDesign a, ShipHull b)
        {
            AssertEqual(a.Hull, b.HullName);
            AssertEqual(a.ModName, b.ModName);
            AssertEqual(a.ShipStyle, b.Style);
            AssertEqual(a.Description, b.Description);
            AssertEqual(a.GridInfo.Size, b.Size);
            AssertEqual(a.GridInfo.SurfaceArea, b.SurfaceArea);
            AssertEqual(a.IconPath, b.IconPath);

            AssertEqual(a.Role, b.Role);
            AssertEqual(a.SelectionGraphic, b.SelectIcon);
            AssertEqual(a.IsShipyard, b.IsShipyard);
            AssertEqual(a.IsOrbitalDefense, b.IsOrbitalDefense);
        }
    }
}
