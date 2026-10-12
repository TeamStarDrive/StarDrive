using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // a planet with fighting near it calls the idle warships of its owner in its system; the player's ships stay on the player's orders
    [TestClass]
    public class PlanetCallForHelpTests : StarDriveTest
    {
        public PlanetCallForHelpTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        static void CallForHelp(Planet p)
        {
            typeof(Planet).GetProperty(nameof(Planet.SpaceCombatNearPlanet))!.SetValue(p, true);
            typeof(Planet).GetMethod("CallForHelp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(p, null);
        }

        [TestMethod]
        public void OnlyAIShipsAnswerAPlanetsCallForHelp()
        {
            Planet home = AddHomeWorldToEmpire(new Vector2(1000), Player);
            Planet enemyHome = AddHomeWorldToEmpire(new Vector2(800_000), Enemy);
            Ship holding = SpawnShip("Vulcan Scout", Player, home.Position + new Vector2(8000, 0));
            holding.AI.OrderHoldPosition();
            Ship idle = SpawnShip("Vulcan Scout", Player, home.Position + new Vector2(-8000, 0));
            Ship aiShip = SpawnShip("Vulcan Scout", Enemy, enemyHome.Position + new Vector2(8000, 0));
            UState.Objects.Update(new(time: 1f));
            UState.Objects.Update(new(time: 1f));
            Assert.IsTrue(home.System.ShipList.Contains(holding) && home.System.ShipList.Contains(idle)
                          && enemyHome.System.ShipList.Contains(aiShip), "setup: the ships are in their planets' systems");
            Assert.AreEqual(AIState.HoldPosition, holding.AI.State, "setup");
            Assert.AreEqual(AIState.AwaitingOrders, idle.AI.State, "setup");
            Assert.AreEqual(AIState.AwaitingOrders, aiShip.AI.State, "setup");

            CallForHelp(home);
            CallForHelp(enemyHome);

            Assert.AreEqual(AIState.MoveTo, aiShip.AI.State, "setup: an idle AI warship goes to defend its planet");
            Assert.AreEqual(AIState.HoldPosition, holding.AI.State, "the player's ship on Hold Position stays where the player put it");
            Assert.AreEqual(AIState.AwaitingOrders, idle.AI.State, "the player's idle ship is not moved either");
        }
    }
}
