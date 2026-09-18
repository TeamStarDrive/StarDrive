using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Production is what a cybernetic race eats, so DetermineProdState is their survival state -
    /// DetermineFoodState returns immediately for them. These are the two protections the food
    /// state always had and the production state never did.
    /// </summary>
    [TestClass]
    public class CyberneticProdStateTests : StarDriveTest
    {
        readonly Planet Colony;

        public CyberneticProdStateTests()
        {
            CreateUniverseAndPlayerEmpire();
            Player.data.Traits.Cybernetic = 1;
            AddHomeWorldToEmpire(new Vector2(2000), Player); // a solo planet always STOREs, so the empire needs two
            Colony = AddDummyPlanetToEmpire(new Vector2(1000), Player, fertility: 1f, minerals: 1f, maxPop: 20000f);
            Colony.CType = Planet.ColonyType.Core;
            Colony.Storage.Max = 1000;
            Colony.UpdateIncomes();
        }

        [TestMethod]
        public void ARunningDryColonyImportsBeforeItsStoreEmpties()
        {
            Assert.IsTrue(Colony.IsCybernetic, "test needs a cybernetic colony");

            // a store the ratio calls comfortable, draining fast enough to run out before a
            // freighter could arrive - the old ratio-only check would have called this STORE
            Colony.Storage.Max = 100;
            Colony.ProdHere = 50;
            Colony.Prod.Percent = 0;
            Colony.Population = Colony.MaxPopulation;
            Colony.UpdateIncomes();
            Assert.IsTrue(Colony.ShortOnFood(), $"test needs a colony that is running out: net {Colony.Prod.NetIncome}");

            Colony.DoGoverning();

            Assert.AreEqual(Planet.GoodState.IMPORT, Colony.PS,
                "a cybernetic colony that is running out of production must ask for more, not wait for the storage ratio");
        }

        [TestMethod]
        public void AColonyThatCannotSustainItselfDoesNotExport()
        {
            Colony.Population = Colony.MaxPopulation;
            Colony.Prod.Percent = 1;
            Colony.UpdateIncomes();
            Assert.IsTrue(Colony.Prod.NetMaxPotential < 0,
                $"test needs a colony that cannot feed itself at full production: {Colony.Prod.NetMaxPotential}");

            // comfortable by ratio - above the export threshold - but it cannot replace what it ships away
            Colony.ProdHere = Colony.Storage.Max * 0.6f;
            Colony.DoGoverning();

            Assert.AreEqual(Planet.GoodState.STORE, Colony.PS,
                "a cybernetic colony that cannot sustain itself must not export the food it is living on");
        }

        [TestMethod]
        public void AColonyThatCannotSustainItselfStillExportsItsOverflow()
        {
            Colony.Population = Colony.MaxPopulation;
            Colony.Prod.Percent = 1;
            Colony.UpdateIncomes();
            Assert.IsTrue(Colony.Prod.NetMaxPotential < 0, "test needs a colony that cannot feed itself at full production");

            // brimming: what sits above 90% is genuinely spare, the same escape the food state has
            Colony.ProdHere = Colony.Storage.Max;
            Colony.DoGoverning();

            Assert.AreEqual(Planet.GoodState.EXPORT, Colony.PS,
                "overflow on a full store must still reach the colonies waiting for it");
        }
    }
}
