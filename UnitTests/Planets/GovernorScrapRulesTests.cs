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
    }
}
