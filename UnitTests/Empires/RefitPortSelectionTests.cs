using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Empires;

/// <summary>
/// Where a refit is sent when the player has prioritized ports (PR #402). Prioritized ports
/// narrow the candidates, but the refit selector still has to weigh the trip: an existing ship
/// flies to the yard and back, which a fresh build never does.
/// </summary>
[TestClass]
public class RefitPortSelectionTests : StarDriveTest
{
    readonly Planet Near;
    readonly Planet Far;
    readonly IShipDesign NewDesign;
    readonly Ship OldShip;

    public RefitPortSelectionTests()
    {
        CreateUniverseAndPlayerEmpire();
        Near = AddHomeWorldToEmpire(new Vector2(1000), Player);
        Far = AddHomeWorldToEmpire(new Vector2(3_000_000), Player);
        Near.HasSpacePort = true;
        Far.HasSpacePort = true;

        NewDesign = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        OldShip = SpawnShip("Rocket Scout", Player, Near.Position + new Vector2(500));

        Assert.IsTrue(Player.isPlayer, "prioritized ports only exist for the player");
        Assert.IsTrue(OldShip.GetAstrogateTimeTo(Far) > OldShip.GetAstrogateTimeTo(Near) + 1,
            "test needs the far port to be more than a turn's flight further away");
    }

    Planet[] Ports => new[] { Near, Far };
    float RefitCost => OldShip.RefitCost(NewDesign);

    void Prioritize(params Planet[] ports)
    {
        foreach (Planet p in Ports)
            p.SetPrioritizedPort(false);
        foreach (Planet p in ports)
            p.SetPrioritizedPort(true);
    }

    // Loads a port's queue so it takes strictly longer to get to a new item than the other one.
    void LoadQueue(Planet p, float cost)
    {
        p.Construction.Enqueue(NewDesign, QueueItemType.CombatShip);
        QueueItem front = p.ConstructionQueue[p.ConstructionQueue.Count - 1];
        front.Cost = cost;
        front.ProductionSpent = 0;
    }

    Planet ChooseWithTravel(Planet[] ports)
    {
        Assert.IsTrue(Player.FindPlanetToRefitAt(ports, RefitCost, OldShip, NewDesign, travelBack: false,
            out Planet chosen), "no port was found for the refit at all");
        return chosen;
    }

    Planet ChooseWithoutTravel(Planet[] ports)
    {
        Assert.IsTrue(Player.FindPlanetToRefitAt(ports, RefitCost, NewDesign, out Planet chosen),
            "no port was found for the orbital refit at all");
        return chosen;
    }

    [TestMethod]
    public void TheShipIsNotSentAcrossTheMapForAMarginallyEmptierYard()
    {
        Prioritize(Near, Far);
        LoadQueue(Near, cost: 40); // a short queue: worth a turn or two, not a galactic crossing

        // The overload with no ship cannot weigh travel, so it ranks by queue alone. Using it as
        // the control proves both ports are live candidates AND that the far one is the emptier:
        // without the trip in the sum, that is the one the selector reaches for.
        Assert.AreSame(Far, ChooseWithoutTravel(Ports),
            "test needs the far port to be the one a queue-only ranking prefers");

        Assert.AreSame(Near, ChooseWithTravel(Ports),
            "a refit that must fly there and back should not cross the map to save a couple of queue turns");
    }

    [TestMethod]
    public void AFarPortStillWinsWhenTheNearOneIsGenuinelyBacklogged()
    {
        Prioritize(Near, Far);
        // The near port winning on an empty queue is the control: it proves it is a live
        // candidate, so the switch below is the backlog talking and not a filtered-out port.
        Assert.AreSame(Near, ChooseWithTravel(Ports), "test needs the near port to be the default pick");

        LoadQueue(Near, cost: 5_000_000); // years of work queued up
        Assert.AreSame(Far, ChooseWithTravel(Ports),
            "travel time should weigh against a distant yard, not veto it outright");
    }

    [TestMethod]
    public void PortsOutsideThePrioritizedListAreNotConsidered()
    {
        Planet ignored = AddHomeWorldToEmpire(new Vector2(2000), Player);
        ignored.HasSpacePort = true;
        Prioritize(Near); // the player wants the near yard and nothing else
        LoadQueue(Near, cost: 5_000_000); // and it is the worst of the three by a mile

        Assert.AreSame(Near, ChooseWithTravel(new[] { Near, Far, ignored }),
            "a prioritized port is the only candidate even when the ports passed in look better");
    }

    [TestMethod]
    public void WithNoPrioritizedPortsTheTripStillCounts()
    {
        Prioritize(); // none: the AI's path, and the player's before they pick any
        LoadQueue(Near, cost: 40);

        Assert.AreSame(Far, ChooseWithoutTravel(Ports), "queue-only ranking should prefer the emptier port");
        Assert.AreSame(Near, ChooseWithTravel(Ports), "the unprioritized path always weighed travel and must keep doing so");
    }

    // The orbital overload used to bail out before it looked at the prioritized list, so a player
    // whose safe ports were all listed as unsafe got no refit even though they had named a yard.
    // It now matches the ship overload, which never had that early return.
    [TestMethod]
    public void AnOrbitalRefitUsesAPrioritizedPortEvenWithNoPortsPassedIn()
    {
        Prioritize(Far);

        Assert.AreSame(Far, ChooseWithoutTravel(Empty<Planet>.Array),
            "a named port should still be usable when the caller's own port list came up empty");
    }
}
