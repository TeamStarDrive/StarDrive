﻿using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace UnitTests.AITests.Empire
{
    using static EmpireAI;
    
    [TestClass]
    public class TestEmpireAI : StarDriveTest
    {
        public TestEmpireAI()
        {
            LoadPlanetContent();
            CreateGameInstance();
            CreateTestEnv();
        }

        private Planet P;
        private Ship_Game.Empire TestEmpire;

        public void AddPlanetToUniverse(Planet p, bool explored, Vector2 pos)
        {
            var s1         = new SolarSystem {Position = pos};
            p.Center       = pos + Vector2.One;
            p.ParentSystem = s1;
            s1.PlanetList.Add(p);
            if (explored)
                s1.SetExploredBy(TestEmpire);
            Ship_Game.Empire.Universe.PlanetsDict.Add(Guid.NewGuid(), p);
            UniverseScreen.SolarSystemList.Add(s1);
        }
        public void AddPlanetToUniverse(float fertility, float minerals, float pop, bool explored, Vector2 pos)
        {
            AddDummyPlanet(fertility, minerals, pop, out Planet p);
            p.Center = pos;
            AddPlanetToUniverse(p, explored, pos);
        }

        void CreateTestEnv()
        {
            CreateUniverseAndPlayerEmpire(out TestEmpire);
            AddPlanetToUniverse(2, 2, 40000, true,Vector2.One);
            AddPlanetToUniverse(1.9f, 1.9f, 40000, true, new Vector2(5000));
            AddPlanetToUniverse(1.7f, 1.7f, 40000, true, new Vector2(-5000));
            for (int x = 0; x < 50; x++)
                AddPlanetToUniverse(0.1f, 0.1f, 1000, true, Vector2.One);
            AddHomeWorldToEmpire(TestEmpire, out P);
            AddPlanetToUniverse(P,true, Vector2.Zero);
            AddHomeWorldToEmpire(Enemy, out P);
            AddPlanetToUniverse(P, true, new Vector2(2000));
            LoadStarterShips(new[] { "Excalibur-Class Supercarrier", "Corsair", "Supply Shuttle"
                , "Flak Fang", "Akagi-Class Mk Ia Escort Carrier", "Rocket Inquisitor" });
            foreach (var ship in ResourceManager.ShipsDict.Keys)
            {
                Player.ShipsWeCanBuild.Add(ship);
            }
        }

        void ClearEmpireShips()
        {
            while (Player.GetShips().TryPopLast(out Ship toRemove))
                toRemove.RemoveFromUniverseUnsafe();
        }

        /* going to add tests here
        [TestMethod]
        public void TestExpansionPlannerColonize()
        {
            var expansionAI = TestEmpire.GetEmpireAI().ExpansionAI;
            TestEmpire.AutoColonize = true;
            expansionAI.RunExpansionPlanner();

            Assert.AreEqual(13, expansionAI.RankedPlanets.Length,
                "Colonization target list should be 13");

            var markedPlanet = expansionAI.GetColonizationGoalPlanets();
            Assert.AreEqual(3, markedPlanet.Length, "Expected 3 colony goals ");


            //mock colonization success
            expansionAI.DesiredPlanets[0].Owner = TestEmpire;
            TestEmpire.GetEmpireAI().EndAllTasks();
            expansionAI.RunExpansionPlanner();
            Assert.AreEqual(13, expansionAI.RankedPlanets.Length);
            markedPlanet = expansionAI.GetColonizationGoalPlanets();
            Assert.AreEqual(3, markedPlanet.Length, "Expected 3 colony goals ");
            expansionAI.RunExpansionPlanner();

        }*/

        [TestMethod]
        [Ignore]
        public void FirstTestShipBuilt()
        {
            ClearEmpireShips();
            var build = new RoleBuildInfo(3, Player.GetEmpireAI(), true);
            string shipName = Player.GetEmpireAI().GetAShip(build);
            Assert.IsTrue(shipName == "Rocket Inquisitor", "Build did not create expected ship");
        }

        [TestMethod]
        [Ignore]
        public void TestBuildCounts()
        {
            ClearEmpireShips();
            // setup Build
            var build           = new RoleBuildInfo(2, Player.GetEmpireAI(), true);

            // init base variables
            var combatRole      = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ShipData.RoleName.fighter);
            float roleBudget    = build.RoleBudget(combatRole);
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            int count;

            // try to build a lot of ships. this loop should break
            for(count = 1; count < 50; count++)
            {
                string shipName   = Player.GetEmpireAI().GetAShip(build);

                if (shipName.IsEmpty())
                {
                    bool canBuildMore = build.CanBuildMore(combatRole);
                    Assert.IsFalse(canBuildMore, "Role says it cant build more but build process says no");
                    break;
                }

                // create the ship
                var ship            = SpawnShip(shipName, Player, Vector2.Zero);
                float shipMaint     = ship.GetMaintCost();

                Assert.IsFalse(shipMaint > roleUnitMaint
                    , $"Ship maintenance: {shipMaint} should never be more than per unit maintenance: {roleUnitMaint}");

                float currentMaint = build.RoleCurrentMaintenance(combatRole);
                Assert.IsTrue((roleUnitMaint * count).AlmostEqual(currentMaint)
                    , $"Current Maintenance: {currentMaint} should equal projected: {roleUnitMaint * count}");
            }

            Assert.IsTrue(count < 49, $"Test failure! Loop completed! Investigate");

            Assert.AreEqual(build.RoleCount(combatRole), (int)(roleBudget / roleUnitMaint));
        }

        [TestMethod]
        [Ignore]
        public void TestBuildScrap()
        {
            ClearEmpireShips();
            var combatRole = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ShipData.RoleName.fighter);
            float buildCapacity = 0.75f;
            var build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), true);
            string shipName = "";
            for (int x = 0; x < 50; x++)
            {
                // This will peak the "Rocket Inquisitor" ships since it is stronger
                shipName = Player.GetEmpireAI().GetAShip(build);
                if (shipName.IsEmpty()) 
                    break;

                SpawnShip(shipName, Player, Vector2.Zero);
            }

            // The expected maintenance for the Flak Fang is 0.12, since Cordrazine
            // Have -25% maintenance reduction
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            Assert.AreEqual(0.12f, roleUnitMaint, "Unexpected maintenance value");
            var ships = Player.GetShips();
            for (int x = 0; x < 20; ++x)
            {
                buildCapacity = build.RoleBudget(combatRole) - roleUnitMaint;
                if (buildCapacity < 0)
                {
                    for (int i = ships.Count - 1; i >= 0; i--)
                    {
                        var ship = ships[i];
                        if (ship.AI.State == AIState.Scrap)
                        {
                            ships.RemoveAtSwapLast(i);
                            ship.RemoveFromUniverseUnsafe();
                        }
                    }

                    buildCapacity = roleUnitMaint * 3;
                }
                build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), false);
                int roleCountWanted = build.RoleCountDesired(combatRole);
                int shipsBeingScrapped = ships.Count(s => s.AI.State == AIState.Scrap);
                int expectedBuildCount = (int)(build.RoleBudget(combatRole) / roleUnitMaint);
                Assert.AreEqual(expectedBuildCount, roleCountWanted);
                
                float currentMain = build.RoleCurrentMaintenance(combatRole);

                if (currentMain + roleUnitMaint > buildCapacity || build.RoleIsScrapping(combatRole))
                {
                    shipName = Player.GetEmpireAI().GetAShip(build);
                    Assert.IsTrue(shipName.IsEmpty(), "Scrap Loop");

                }
                else
                {
                    Assert.IsTrue(shipsBeingScrapped <= 0, "Scrap Loop");
                    shipName = Player.GetEmpireAI().GetAShip(build);
                    Assert.IsTrue(shipName.NotEmpty(), "Scrap Loop");
                }
            }
        }

        [TestMethod]
        public void TestOverBudgetSpending()
        {
            ClearEmpireShips();
            Player.Money = 1000;

            for (int x = -1; x < 11; x++)
            {
                float percent = x * 0.1f;
                float overSpend = Player.GetEmpireAI().OverSpendRatio(1000, percent, 10f);
                percent = 2 - percent;
                Assert.IsTrue(overSpend.AlmostEqual(percent), $"Expected {percent} got {overSpend}");
            }
            Player.Money = 100;
            for (int x = -1; x < 1; x++)
            {
                float percent = x * 0.1f;
                float overSpend = Player.GetEmpireAI().OverSpendRatio(1000, percent, 10f);
                percent = 0.2f - percent;
                Assert.IsTrue(overSpend.AlmostEqual(percent), $"Expected {percent} got {overSpend}");
            }
        }


    }
}

