using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // Two rules the governor must obey, both stated in the "Governor Will Not Scrap Buildings"
    // tooltip: the setting stops it scrapping but still lets it upgrade, and a building the
    // player placed by hand is never scrapped, whatever the setting says.
    [TestClass]
    public class GovernorScrapRulesTests : StarDriveTest
    {
        readonly Planet P;
        readonly Building PlayerBuilt;
        readonly Building GovernorBuilt;

        public GovernorScrapRulesTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);

            string name = ResourceManager.BuildingsDict.Values
                .First(b => !b.IsBiospheres && !b.IsMilitary && b.Scrappable && !b.IsSpacePort
                            && !b.BuildOnlyOnce && b.PlusTerraformPoints <= 0).Name;

            PlayerBuilt = ResourceManager.CreateBuilding(P, name);
            PlayerBuilt.IsPlayerAdded = true;
            GovernorBuilt = ResourceManager.CreateBuilding(P, name);
        }

        bool Suitable(Building b, bool overBudget, bool replacing)
            => P.SuitableForScrap(b, overBudget, storageInUse: 0, scrapZeroMaintenance: true, replacing);

        // SuitableForScrap never reads DontScrapBuildings, which is exactly why rule 2 holds
        // whatever the setting says - both scrap paths reach this predicate and stop here
        [TestMethod]
        public void PlayerBuiltIsNeverScrapped()
        {
            foreach (bool overBudget in new[] { false, true })
            {
                foreach (bool replacing in new[] { false, true })
                {
                    Assert.IsFalse(Suitable(PlayerBuilt, overBudget, replacing),
                        $"Player built building was offered for scrap with overBudget={overBudget}, replacing={replacing}");
                }
            }
        }

        [TestMethod]
        public void TheGuardDoesNotCoverGovernorBuiltToo()
        {
            // the player guard must stay narrow. ReplaceBuilding reaches SuitableForScrap through
            // ChooseWorstBuilding, so widening it here would also stop the governor upgrading its
            // own buildings, which the no-scrap setting is meant to keep allowed
            Assert.IsTrue(Suitable(GovernorBuilt, overBudget: false, replacing: true),
                "The governor can no longer offer its own buildings for replacement");
        }

        [TestMethod]
        public void PlayerBuiltIsScrappableOnceWeNoLongerOwnThePlanet()
        {
            P.SetOwner(Enemy);
            Assert.IsTrue(Suitable(PlayerBuilt, overBudget: false, replacing: false),
                "An AI that captured the planet is still refusing to scrap the old owner's buildings");
        }

        // Military buildings never reach SuitableForScrap - it excludes IsMilitary outright - so
        // TryScrapMilitaryBuilding is the only path that scraps them, and both rules were missing
        Building PlaceMilitary(bool playerAdded)
        {
            string name = ResourceManager.BuildingsDict.Values.First(b => b.IsMilitary && b.Scrappable).Name;
            Building b = ResourceManager.CreateBuilding(P, name);
            b.IsPlayerAdded = playerAdded;
            P.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(b, P);
            return b;
        }

        [TestMethod]
        public void TheScrapSettingCoversMilitaryBuildingsToo()
        {
            Building governorBuilt = PlaceMilitary(playerAdded: false);
            P.DontScrapBuildings = true;
            P.TryScrapMilitaryBuilding();

            Assert.IsTrue(P.HasBuilding(b => b == governorBuilt),
                "A military building was scrapped while the governor was told not to scrap");
        }

        [TestMethod]
        public void PlayerBuiltMilitarySurvivesUnderBlueprints()
        {
            Building playerMilitary = PlaceMilitary(playerAdded: true);
            // a plan that does not name this building leaves it un-required, so the blueprints
            // branch of the scrap search considers it. An empty plan would do the same but drives
            // PercentAchievable to its 0 sentinel, so name one
            var planned = new HashSet<string> { ResourceManager.BuildingsDict.Values.First(b => !b.IsMilitary).Name };
            P.AddBlueprints(new BlueprintsTemplate("test", false, null, planned, Planet.ColonyType.Colony), Player);
            P.TryScrapMilitaryBuilding();

            Assert.IsTrue(P.HasBuilding(b => b == playerMilitary),
                "The blueprints branch scrapped a military building the player placed by hand");
        }

        // A queued building is still a building the player asked for, so the over budget sweep
        // must leave it alone. SBProduction already checks IsPlayerAdded in its own queue paths
        [TestMethod]
        public void PlayerQueuedBuildingIsNotCancelledWhenOverBudget()
        {
            Building costly = ResourceManager.BuildingsDict.Values
                .First(b => !b.IsMilitary && !b.IsTerraformer && !b.IsBiospheres && b.Maintenance > 0);

            Assert.IsTrue(P.Construction.Enqueue(costly, null, playerAdded: true), "Could not queue the test building");
            int queued = P.ConstructionQueue.Count;

            P.TryCancelOverBudgetCivilianBuilding(budget: 0f);

            AssertEqual(queued, P.ConstructionQueue.Count,
                "The governor cancelled a building the player queued by hand");
            Assert.IsTrue(P.ConstructionQueue.Any(q => q.Building == costly && q.IsPlayerAdded),
                "The player's queued building is no longer in the queue");
        }

        // Exclusive blueprints are the player saying "the plan and nothing but the plan", so
        // they lift the protection from everything the player placed by hand
        void AddExclusiveBlueprints()
        {
            var planned = new HashSet<string> { ResourceManager.BuildingsDict.Values.First(b => !b.IsMilitary).Name };
            P.AddBlueprints(new BlueprintsTemplate("test", true, null, planned, Planet.ColonyType.Colony), Player);
        }

        [TestMethod]
        public void ExclusiveBlueprintsMayScrapPlayerBuilt()
        {
            AddExclusiveBlueprints();
            Assert.IsTrue(Suitable(PlayerBuilt, overBudget: false, replacing: false),
                "Exclusive blueprints could not clear a building the player placed by hand");
        }

        [TestMethod]
        public void ExclusiveBlueprintsMayScrapPlayerBuiltMilitary()
        {
            Building playerMilitary = PlaceMilitary(playerAdded: true);
            AddExclusiveBlueprints();
            P.TryScrapMilitaryBuilding();

            Assert.IsFalse(P.HasBuilding(b => b == playerMilitary),
                "Exclusive blueprints could not clear a military building the player placed by hand");
        }

        [TestMethod]
        public void ExclusiveBlueprintsMayCancelPlayerQueued()
        {
            Building costly = ResourceManager.BuildingsDict.Values
                .First(b => !b.IsMilitary && !b.IsTerraformer && !b.IsBiospheres && b.Maintenance > 0);

            Assert.IsTrue(P.Construction.Enqueue(costly, null, playerAdded: true), "Could not queue the test building");
            AddExclusiveBlueprints();

            P.TryCancelOverBudgetCivilianBuilding(budget: 0f);

            Assert.IsFalse(P.ConstructionQueue.Any(q => q.Building == costly),
                "Exclusive blueprints could not cancel a building the player queued by hand");
        }
    }
}
