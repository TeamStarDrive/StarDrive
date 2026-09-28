using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Empires;

[TestClass]
public class CrippledPortTests : StarDriveTest
{
    readonly Planet ColonyPort;
    readonly Planet CorePort;
    readonly IShipDesign Scout;

    public CrippledPortTests()
    {
        CreateUniverseAndPlayerEmpire();
        ColonyPort = AddHomeWorldToEmpire(new Vector2(1000), Player);
        CorePort = AddHomeWorldToEmpire(new Vector2(3_000_000), Player);
        ColonyPort.HasSpacePort = true;
        CorePort.HasSpacePort = true;
        ColonyPort.CType = Planet.ColonyType.Colony;
        CorePort.CType = Planet.ColonyType.Core;
        Scout = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;

        CorePort.Construction.Enqueue(Scout, QueueItemType.CombatShip);
        CorePort.ConstructionQueue[CorePort.ConstructionQueue.Count - 1].Cost = 5_000_000;
    }

    Planet ChoosePort()
    {
        Assert.IsTrue(Player.FindPlanetToBuildShipAt(new[] { ColonyPort, CorePort }, Scout, out Planet chosen),
            "no port was found at all");
        return chosen;
    }

    [TestMethod]
    public void ASabotagedColonyTypePortIsPassedOver()
    {
        Assert.AreSame(ColonyPort, ChoosePort(), "setup: the idle Colony-type port must be the pick while it is healthy");

        ColonyPort.AddCrippledTurns(10);

        Assert.AreSame(CorePort, ChoosePort(), "a sabotaged port builds nothing, so the busy healthy one must be chosen");
    }
}
