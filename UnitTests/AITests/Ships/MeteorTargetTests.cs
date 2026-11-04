using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// A meteor shower threatens only the planet it falls on, so Remnant ships leave its meteors alone, while an empire's
/// ships still take them as targets to guard their planets.
/// </summary>
[TestClass]
public class MeteorTargetTests : StarDriveTest
{
    static readonly Vector2 Site = new(200_000f, 200_000f);
    const float SensorRadius = 10_000f;

    public MeteorTargetTests()
    {
        LoadStarterShips("Meteor A", "Ancient Frigate", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    [TestMethod]
    public void RemnantShipsLeaveMeteorsAloneWhileEmpireShipsTargetThem()
    {
        CreateAMinorFaction("Remnant");
        var remnants = Faction;
        CreateAMinorFaction("Unknown");
        var unknown = Faction;
        Assert.IsTrue(remnants.WeAreRemnants && unknown == UState.Unknown, "setup: the Remnants and the meteors' Unknown owner");

        Ship meteor = SpawnShip("Meteor A", unknown, Site + new Vector2(0, 1_500f)); // off the line between the two, out of the way of their fire
        Ship guardian = SpawnShip("Ancient Frigate", remnants, Site + new Vector2(2_000f, 0));
        Ship scout = SpawnShip("Vulcan Scout", Player, Site + new Vector2(-2_000f, 0));
        Assert.IsTrue(meteor.IsMeteor, "setup: a meteor");
        UState.Objects.Update(TestSimStep); // places the ships in the spatial index the scans search

        scout.AI.ScanForEnemies(scout, SensorRadius);
        Assert.IsTrue(scout.AI.PotentialTargets.Contains(meteor), "an empire's ship takes the meteor as a target, to guard its planets");

        guardian.AI.ScanForEnemies(guardian, SensorRadius);
        Assert.IsTrue(guardian.AI.PotentialTargets.Contains(scout), "setup: the Remnant ship's scan finds its enemies");
        Assert.IsFalse(guardian.AI.PotentialTargets.Contains(meteor), "a Remnant ship leaves the meteor alone");

        remnants.GetRelations(unknown).Known = false;
        guardian.AI.ScanForEnemies(guardian, SensorRadius);
        Assert.IsFalse(guardian.AI.PotentialTargets.Contains(meteor), "even on a scan before the Remnants have met the meteors' owner");
    }
}
