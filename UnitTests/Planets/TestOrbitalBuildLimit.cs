using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

// Colony screen "+" button shift/ctrl-clicks queue 5/10 orbitals in one batch. The orbital build
// limit (ShipBuilder.OrbitalsLimit platforms+stations, ShipBuilder.ShipYardsLimit shipyards) is
// enforced via Planet.IsOutOfOrbitalsLimit, which counts pending build goals. Those goals are added
// on the sim thread (deferred via RunOnSimThread), so a tight batch loop can't see what it already
// queued earlier in the same loop - it must pass the in-batch count through extraPending. These
// tests pin that extraPending accounting so a batch can never breach either limit.
[TestClass]
public class TestOrbitalBuildLimit : StarDriveTest
{
    readonly Planet Homeworld;
    readonly IShipDesign Platform;
    readonly IShipDesign Shipyard;

    public TestOrbitalBuildLimit()
    {
        LoadStarterShips("Platform Base mk1-a", "Shipyard");
        CreateUniverseAndPlayerEmpire();
        Homeworld = AddHomeWorldToEmpire(new Vector2(2000), Player);
        Platform  = ResourceManager.GetShipTemplate("Platform Base mk1-a").ShipData;
        Shipyard  = ResourceManager.GetShipTemplate("Shipyard").ShipData;

        // Guard the assumptions the threshold tests rely on: design roles and an empty baseline.
        Assert.IsTrue(Platform.IsPlatformOrStation && !Platform.IsShipyard, "Platform design role changed");
        // A shipyard must consume BOTH a shipyard slot and a shared orbital slot - if it ever stopped
        // counting as IsPlatformOrStation the orbital-cap branch would silently skip it.
        Assert.IsTrue(Shipyard.IsShipyard && Shipyard.IsPlatformOrStation,
            "Shipyard must count as both a shipyard and an orbital");
        Assert.AreEqual(0, Homeworld.OrbitalStations.Count, "Fresh homeworld must start with no orbital stations");
        Assert.AreEqual(0, Homeworld.OrbitalsBeingBuilt(Platform.Role), "Fresh homeworld must have no pending platform goals");
        Assert.AreEqual(0, Homeworld.OrbitalsBeingBuilt(Shipyard.Role), "Fresh homeworld must have no pending station goals");
        Assert.AreEqual(0, Homeworld.ShipyardsBeingBuilt(), "Fresh homeworld must have no pending shipyard goals");
    }

    [TestMethod]
    public void FreshPlanet_IsNotOutOfLimit()
    {
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Platform), "A planet with no orbitals should allow building");
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Shipyard), "A planet with no shipyards should allow building");
    }

    [TestMethod]
    public void PlatformBatch_StopsAtOrbitalsLimit()
    {
        // extraPending models orbitals already queued earlier in the same batch (shift/ctrl click).
        // One slot below the limit is still buildable; reaching the limit blocks the rest of the batch.
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Platform, ShipBuilder.OrbitalsLimit - 1),
            "Should still allow the last orbital before the limit");
        Assert.IsTrue(Homeworld.IsOutOfOrbitalsLimit(Platform, ShipBuilder.OrbitalsLimit),
            "Must block once the in-batch count reaches the orbitals limit");
    }

    [TestMethod]
    public void ShipyardBatch_StopsAtShipyardLimit()
    {
        // Shipyards have their own, much smaller limit on top of the shared orbitals limit.
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Shipyard, ShipBuilder.ShipYardsLimit - 1),
            "Should still allow the last shipyard before the shipyard limit");
        Assert.IsTrue(Homeworld.IsOutOfOrbitalsLimit(Shipyard, ShipBuilder.ShipYardsLimit),
            "Must block once the in-batch count reaches the shipyard limit");
    }

    [TestMethod]
    public void ExistingOrbitalsCountAgainstBatch()
    {
        // Seed real stations so the baseline isn't empty; the batch headroom shrinks accordingly.
        const int seeded = 5;
        for (int i = 0; i < seeded; i++)
            Homeworld.OrbitalStations.Add(SpawnShip("Platform Base mk1-a", Player, new Vector2(3000 + i*50)));

        int headroom = ShipBuilder.OrbitalsLimit - seeded;
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Platform, headroom - 1),
            "With existing stations, the batch can still fill the remaining slots");
        Assert.IsTrue(Homeworld.IsOutOfOrbitalsLimit(Platform, headroom),
            "Existing stations plus the in-batch count must not exceed the orbitals limit");
    }

    [TestMethod]
    public void PendingOrbitalsOfAnyRoleCount()
    {
        for (int i = 0; i < ShipBuilder.OrbitalsLimit - 1; i++)
            Homeworld.OrbitalStations.Add(SpawnShip("Platform Base mk1-a", Player, new Vector2(3000 + i*50)));
        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Platform), "setup: one orbital slot must be left");

        Homeworld.AddOrbital(Shipyard);
        Assert.IsTrue(Homeworld.IsOutOfOrbitalsLimit(Platform),
            "A shipyard on its way must take the last orbital slot, as a platform would");
    }

    [TestMethod]
    public void ARefitDoesNotTakeASecondSlot()
    {
        for (int i = 0; i < ShipBuilder.OrbitalsLimit - 1; i++)
            SpawnShip("Platform Base mk1-a", Player, new Vector2(3000 + i*50)).TetherToPlanet(Homeworld);

        // RefitOrbital is internal to the game assembly
        Type refitOrbital = typeof(Planet).Assembly.GetType("Ship_Game.Commands.Goals.RefitOrbital");
        Player.AI.AddGoal((Goal)Activator.CreateInstance(refitOrbital, Homeworld.OrbitalStations[0], Platform, Player, false));
        AssertEqual(1, Homeworld.OrbitalsBeingBuilt(Platform.Role), "setup: the refit must be on its way to this planet");

        Assert.IsFalse(Homeworld.IsOutOfOrbitalsLimit(Shipyard),
            "A refit replaces an orbital that is already counted, so it must not take the last slot");
    }
}
