﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipRanges : StarDriveTest
    {
        public TestShipRanges()
        {
            CreateGameInstance();
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Excalibur-Class Supercarrier", "Supply Shuttle");
            CreateUniverseAndPlayerEmpire();
        }

        void UpdateStatus(Ship ship, CombatState state)
        {
            ship.AI.CombatState = state;
            ship.shipStatusChanged = true;
            ship.Update(new FixedSimTime(1f));
        }

        [TestMethod]
        public void ShipRanges()
        {
            Ship ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Player, Vector2.Zero);

            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(7500, ship.WeaponsMaxRange);
            Assert.That.Equal(4000, ship.WeaponsMinRange);
            Assert.That.Equal(2681, ship.WeaponsAvgRange);
            Assert.That.Equal(6750, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);

            UpdateStatus(ship, CombatState.Evade);
            Assert.That.Equal(Ship.UnarmedRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.HoldPosition);
            Assert.That.Equal(ship.WeaponsMaxRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.ShortRange);
            Assert.That.Equal(ship.WeaponsMinRange*0.9f, ship.DesiredCombatRange);
            
            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(ship.WeaponsMaxRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.BroadsideLeft);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.BroadsideRight);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.OrbitLeft);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitRight);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.AssaultShip);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitalDefense);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
        }

        [TestMethod]
        public void ShipRangesWithModifiers()
        {
            Ship ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            
            WeaponTagModifier kinetic = Player.WeaponBonuses(WeaponTag.Kinetic);
            WeaponTagModifier guided = Player.WeaponBonuses(WeaponTag.Guided);
            kinetic.Range = 1;
            guided.Range = 1;

            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(8000, ship.WeaponsMaxRange);
            Assert.That.Equal(7500, ship.WeaponsMinRange);
            Assert.That.Equal(4681, ship.WeaponsAvgRange);
            Assert.That.Equal(7200, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
        }
    }
}
