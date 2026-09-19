using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // DestroyBuildingInUprise is now the single implementation behind both the espionage uprise
    // and the bankruptcy rebellion, so what it refuses to touch matters to both
    [TestClass]
    public class RebellionDamageTests : StarDriveTest
    {
        readonly Planet P;

        public RebellionDamageTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);

            string name = ResourceManager.BuildingsDict.Values
                .First(b => !b.IsBiospheres && !b.IsMilitary && b.Scrappable && !b.BuildOnlyOnce).Name;

            foreach (PlanetGridSquare tile in P.TilesList.Filter(t => t.Habitable && t.NoBuildingOnTile))
                tile.PlaceBuilding(ResourceManager.CreateBuilding(P, name), P);
        }

        [TestMethod]
        public void AnUpriseWrecksBuildingsInsteadOfSellingThemBack()
        {
            int before = P.NumBuildings;
            float prodBefore = P.ProdHere;
            float moneyBefore = Player.Money;

            P.DestroyBuildingInUprise(UpriseBuildingType.Random, out string destroyed);

            AssertEqual(before - 1, P.NumBuildings, "Expected exactly one building to be wrecked");
            Assert.IsFalse(destroyed.IsEmpty(), "The wrecked building was not named for the notification");
            AssertEqual(prodBefore, P.ProdHere, "The uprise paid the colony production for what it wrecked");
            AssertEqual(moneyBefore, Player.Money, "The uprise refunded credits for what it wrecked");
        }

        // Scrappable is what separates a structure from terrain and story pieces - wrecking a
        // volcano or a crater would be doing the colony a favour
        [TestMethod]
        public void AnUpriseLeavesTerrainAlone()
        {
            foreach (Building b in P.Buildings.ToArray())
                P.DestroyBuilding(b);

            ResourceManager.GetBuilding("Active Volcano", out Building volcano);
            Assert.IsNotNull(volcano, "Expected an Active Volcano template");
            Assert.IsFalse(volcano.Scrappable, "Active Volcano is expected to be unscrappable");
            P.TilesList.First().PlaceBuilding(ResourceManager.CreateBuilding(P, volcano.Name), P);

            int before = P.NumBuildings;
            P.DestroyBuildingInUprise(UpriseBuildingType.Random, out string destroyed);

            AssertEqual(before, P.NumBuildings, "The uprise wrecked terrain");
            Assert.IsTrue(destroyed.IsEmpty(), $"The uprise reported wrecking {destroyed}");
        }
    }
}
