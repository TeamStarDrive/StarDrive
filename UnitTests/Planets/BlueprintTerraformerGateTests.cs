using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// A colony running a big blueprint plan should not spend its tiles on terraformers before the
    /// plan is under way: OkToBuildTerraformers takes the half-completion branch once the plan
    /// claims at least half the tile area. That branch compared the reachable share of the plan
    /// against half the COMPLETED share, and since a standing building is reachable too, it read
    /// true whatever the colony had built. The gate never closed.
    /// </summary>
    [TestClass]
    public class BlueprintTerraformerGateTests : StarDriveTest
    {
        readonly Planet P;
        Building[] Plannable;

        public BlueprintTerraformerGateTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);
        }

        // Everything in the plan is unlocked, so PercentAchievable is 100 and the gate turns
        // purely on how much of it is standing.
        void PlanBuildings(int count)
        {
            Plannable = ResourceManager.BuildingsDict.Values
                .Where(b => b.IsSuitableForBlueprints && !b.IsMilitary && b.EventOnBuild == null)
                .Take(count).ToArray();

            AssertEqual(count, Plannable.Length, "test needs this many buildings to plan");
            foreach (Building b in Plannable)
                Player.UnlockEmpireBuilding(b.Name);
            var planned = new HashSet<string>(Plannable.Select(b => b.Name));
            AssertEqual(count, planned.Count, "planned building names must be distinct");

            P.AddBlueprints(new BlueprintsTemplate("test", false, null, planned, Planet.ColonyType.Colony), Player);
            AssertEqual(100, P.Blueprints.PercentAchievable, "the whole plan should be reachable");
        }

        void BuildFirst(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                PlanetGridSquare tile = P.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
                Assert.IsNotNull(tile, "test planet ran out of habitable tiles");
                tile.PlaceBuilding(ResourceManager.CreateBuilding(P, Plannable[i].Name), P);
            }

            P.Blueprints.UpdateCompletion();
        }

        // A plan is "big" once it claims at least half the tile area; only then does the gate
        // take the half-completion branch this covers.
        int BigPlanSize => P.TileArea / 2 + 1;

        [TestMethod]
        public void ABarelyStartedPlanHoldsTerraformersBack()
        {
            PlanBuildings(BigPlanSize);

            Assert.IsTrue(P.Blueprints.PercentCompleted < 50, "test needs a plan that has barely started");
            Assert.IsFalse(P.Blueprints.OkToBuildTerraformers,
                "a colony that has built almost none of its plan must not spend tiles on terraformers");
        }

        [TestMethod]
        public void APlanPastHalfwayLetsTerraformersIn()
        {
            int size = BigPlanSize;
            PlanBuildings(size);
            BuildFirst(size / 2 + 1);

            Assert.IsTrue(P.Blueprints.PercentCompleted >= 50, "test needs a plan past the halfway mark");
            Assert.IsTrue(P.Blueprints.OkToBuildTerraformers,
                "once half the reachable plan is up, terraformers are allowed again");
        }

        [TestMethod]
        public void AnUnreachablePlanNeverBlocksTerraformers()
        {
            var unlocked = new HashSet<string>(Player.GetUnlockedBuildings().Select(b => b.Name));
            var planned = new HashSet<string>(ResourceManager.BuildingsDict.Values
                .Where(b => !unlocked.Contains(b.Name)).Take(BigPlanSize).Select(b => b.Name));
            AssertEqual(BigPlanSize, planned.Count, "test needs this many buildings the empire has not unlocked");

            P.AddBlueprints(new BlueprintsTemplate("test", false, null, planned, Planet.ColonyType.Colony), Player);

            AssertEqual(0, P.Blueprints.PercentAchievable, "test needs a plan the empire cannot build at all");
            Assert.IsTrue(P.Blueprints.OkToBuildTerraformers,
                "a plan nothing can be built from must not hold terraforming hostage forever");
        }
    }
}
