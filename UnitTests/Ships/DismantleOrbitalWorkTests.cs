using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An orbital coming apart does no work, for itself or for anyone: it makes no ordnance and its planet does not rearm
/// it, no ship turns to it for supplies or repair and no repair drone heals it, its beams go out, miners bring it
/// nothing, a research station researches nothing and a mining station refines nothing, and a Dyson swarm no longer
/// counts its controller. A refit constructor on its way, to a planet or to deep space, scraps itself, and a new orbital
/// is not placed where one comes apart. Once it is gone, a research station's planet can be researched again and a
/// mining station lets go of its ops; while its ops are another empire's, a mining station's dismantling cannot be
/// called off.
/// </summary>
[TestClass]
public class DismantleOrbitalWorkTests : StarDriveTest
{
    const string PatrolStation = "Terran Small Patrol Station 1";
    const string SupplyBase = "Draug Asteroid Base";
    const string Platform = "Platform Base mk1-a";
    readonly Planet Colony;

    public DismantleOrbitalWorkTests()
    {
        LoadStarterShips(PatrolStation, SupplyBase, Platform, "Mining Ship", DysonSwarm.DysonSwarmControllerName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Colony = AddHomeWorldToEmpire(new Vector2(200_000f), Player);
    }

    Ship Orbital(string design, Planet at, float degrees = 0f)
    {
        Ship orbital = SpawnShip(design, Player, at.Position + MathExt.PointOnCircle(degrees, at.OrbitalRingRadius(0)));
        orbital.TetherToPlanet(at);
        return orbital;
    }

    static void TakeApart(Ship orbital)
    {
        orbital.AI.OrderScrapShip();
        Assert.IsTrue(orbital.Dismantling, "setup: it comes apart");
    }

    // a few sim steps, so the spatial grid puts it among its system's ships
    void InItsSystem(Ship orbital)
    {
        RunObjectsSim(0.5f);
        Assert.IsTrue(orbital.System?.ShipList.Contains(orbital) == true, "setup: it is among its system's ships");
    }

    // the empire turns of its whole dismantling
    void TakeApartToTheEnd(Ship orbital)
    {
        InItsSystem(orbital);
        TakeApart(orbital);
        DismantleOrbital goal = (DismantleOrbital)orbital.Loyalty.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == orbital);
        for (int turns = DismantleOrbital.Turns(orbital); turns > 0; --turns)
            goal.Evaluate();
        Assert.IsFalse(orbital.Active, "setup: it is gone");
    }

    Planet ResearchablePlanet()
    {
        Planet planet = AddDummyPlanet(new Vector2(600_000f), 0, 0, 0, new Vector2(605_000f), explored: true);
        planet.GenerateNewFromPlanetType(new SeededRandom(), ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant),
                                         scale: 1.5f, preDefinedPop: 16);
        planet.SetResearchable(true, UState);
        return planet;
    }

    Planet MinedPlanet()
    {
        Planet planet = AddDummyPlanet(new Vector2(600_000f), 0, 0, 0, new Vector2(605_000f), explored: true);
        planet.GenerateNewFromPlanetType(new SeededRandom(), ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant),
                                         scale: 1.5f, preDefinedPop: 16);
        planet.Mining = new Mineable(planet);
        planet.Mining.ChangeOwner(Player);
        return planet;
    }

    [TestMethod]
    public void ComingApartItMakesNoOrdnance()
    {
        Ship station = SpawnShip(PatrolStation, Player, new Vector2(600_000f));
        Ship control = SpawnShip(PatrolStation, Player, new Vector2(600_000f, 620_000f));
        AssertGreaterThan(station.OrdAddedPerSecond, 0f, "setup: its fabricator makes ordnance");
        station.ChangeOrdnance(-station.Ordinance);
        control.ChangeOrdnance(-control.Ordinance);

        TakeApart(station);
        RunObjectsSim(2f);
        AssertGreaterThan(control.Ordinance, 0f, "setup: one at work makes ordnance");
        AssertEqual(0f, station.Ordinance, "one coming apart makes none");
    }

    [TestMethod]
    public void ItsPlanetDoesNotRearmIt()
    {
        Ship station = Orbital(PatrolStation, Colony);
        Ship control = Orbital(PatrolStation, Colony, degrees: 120f);
        TakeApart(station);
        InItsSystem(station);
        InItsSystem(control);
        station.ChangeOrdnance(-station.Ordinance);
        control.ChangeOrdnance(-control.Ordinance);

        Colony.GeodeticManager.AffectNearbyShips();
        AssertGreaterThan(control.Ordinance, 0f, "setup: the planet rearms an orbital at work");
        AssertEqual(0f, station.Ordinance, "but not one coming apart");
    }

    [TestMethod]
    public void NoShipTurnsToItForSuppliesOrRepair()
    {
        Ship supplyBase = SpawnShip(SupplyBase, Player, new Vector2(600_000f));
        Ship scout = SpawnShip("Vulcan Scout", Player, supplyBase.Position + new Vector2(2_000f, 0));
        scout.AI.FriendliesNearby = new[] { supplyBase };
        Assert.IsTrue(supplyBase.SupplyShipCanSupply, "setup: its supply bays can supply");
        AssertEqual(supplyBase, scout.AI.NearBySupplyShip, "setup: a ship short of ordnance turns to it");
        AssertEqual(supplyBase, scout.AI.NearByRepairShip, "setup: and a damaged one to its repair");

        TakeApart(supplyBase);
        Assert.IsFalse(supplyBase.SupplyShipCanSupply, "coming apart it supplies no one");
        Assert.IsNull(scout.AI.NearBySupplyShip, "no ship turns to it for ordnance");
        Assert.IsNull(scout.AI.NearByRepairShip, "nor for repair");
    }

    [TestMethod]
    public void RepairDronesPassItOver()
    {
        Ship station = SpawnShip(PatrolStation, Player, new Vector2(600_000f));
        foreach (ShipModule m in station.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        Ship carrier = SpawnShip("Vulcan Scout", Player, station.Position + new Vector2(2_000f, 0));
        carrier.AI.FriendliesNearby = new[] { station };
        Weapon drones = ResourceManager.CreateWeapon(UState, "RepairDrone", carrier, carrier.Modules.First(m => m.InstalledWeapon != null));
        Projectile drone = Projectile.Create(drones, carrier, carrier.Position, new Vector2(0f, -1f), null, playSound: false);
        drone.DroneAI.ChooseTarget();
        AssertEqual(station, drone.DroneAI.DroneTarget, "setup: a repair drone heals a damaged station");

        TakeApart(station);
        drone.DroneAI.ChooseTarget();
        Assert.IsNull(drone.DroneAI.DroneTarget, "but passes over one coming apart");
    }

    [TestMethod]
    public void ItsBeamsGoOutAsItStartsComingApart()
    {
        Ship station = SpawnShip(PatrolStation, Player, new Vector2(600_000f));
        RunObjectsSim(TestSimStep);
        ShipModule emitter = station.Modules.First(m => m.InstalledWeapon != null);
        Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", station, emitter);
        var beam = new Beam(UState.CreateId(), laser, emitter.Position, emitter.Position + station.Direction * 500f);
        beam.Update(TestSimStep);
        Assert.IsTrue(beam.Active, "setup: its beam burns");

        TakeApart(station);
        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "coming apart, its beam goes out at once");
    }

    // what a mining ship returning to the station adds to its raw material
    float Delivered(Ship station, string ore)
    {
        float before = station.GetOtherCargo(ore);
        Ship miner = SpawnShip("Mining Ship", Player, station.Position + new Vector2(500f, 0));
        miner.LoadCargo(ore, 10f);
        AssertGreaterThan(miner.GetOtherCargo(ore), 0f, "setup: the miner brings raw material");
        miner.AI.ReturnToMothership(station);
        return station.GetOtherCargo(ore) - before;
    }

    [TestMethod]
    public void MinersBringItNothing()
    {
        Planet mined = MinedPlanet();
        Ship station = Orbital("Basic Mining Station", mined);
        string ore = mined.Mining.CargoId;
        AssertGreaterThan(Delivered(station, ore), 0f, "setup: at work it takes in what its miners bring");

        TakeApart(station);
        AssertEqual(0f, Delivered(station, ore), "coming apart, it takes in nothing");
    }

    [TestMethod]
    public void AResearchStationComingApartResearchesNothing()
    {
        Planet target = ResearchablePlanet();
        Ship station = Orbital("Basic Research Station", target);
        var research = new ProcessResearchStation(Player, station);
        Player.AI.AddGoal(research);
        Player.Research.SetTopic("FrigateConstruction");
        station.LoadProduction(station.CargoSpaceMax);
        float aboard = station.GetProduction();
        research.Evaluate();
        AssertLessThan(station.GetProduction(), aboard, "setup: at work it turns its production into research");

        TakeApart(station);
        aboard = station.GetProduction();
        research.Evaluate();
        AssertEqual(aboard, station.GetProduction(), "coming apart, it researches nothing");
        AssertEqual(AIState.Scrap, station.AI.State, "and keeps to its dismantling");
    }

    [TestMethod]
    public void OnceGoneAResearchStationsPlanetCanBeResearchedAgain()
    {
        Planet target = ResearchablePlanet();
        Ship station = Orbital("Basic Research Station", target);
        var research = new ProcessResearchStation(Player, station);
        Player.AI.AddGoal(research);
        Assert.IsTrue(target.IsResearchStationDeployedBy(Player), "setup: its station researches the planet");

        TakeApartToTheEnd(station);
        Assert.IsFalse(target.IsResearchStationDeployedBy(Player), "once it is gone, no station of the empire's researches the planet");
        research.Evaluate();
        Assert.IsTrue(target.CanBeResearchedBy(Player), "and with its research order over, the planet can be researched again");
    }

    // raw material and the supplies to refine it, as much as the station holds
    static void LoadForRefining(Ship station, Planet mined)
    {
        if (station.Loyalty.IsCybernetic)
            station.LoadProduction(station.MiningStationCargoSpaceMax);
        else
            station.LoadFood(station.MiningStationCargoSpaceMax);
        station.LoadCargo(mined.Mining.CargoId, station.MiningStationCargoSpaceMax);
    }

    [TestMethod]
    public void AMiningStationComingApartRefinesNothing()
    {
        Planet mined = MinedPlanet();
        Ship station = Orbital("Basic Mining Station", mined);
        var ops = new MiningOps(Player, mined, station);
        Player.AI.AddGoal(ops);
        Player.DoMoney(); // the empire's need for what it refines
        LoadForRefining(station, mined);
        Universe.CurrentSimTime = 10f;
        ops.Evaluate();
        AssertGreaterThan(station.Carrier.MiningBays.RefiningOutput, 0, "setup: at work it refines");

        TakeApart(station);
        LoadForRefining(station, mined);
        float raw = station.GetOtherCargo(mined.Mining.CargoId);
        Universe.CurrentSimTime = 20f;
        ops.Evaluate();
        AssertEqual(0, station.Carrier.MiningBays.RefiningOutput, "coming apart, it refines nothing");
        AssertEqual(raw, station.GetOtherCargo(mined.Mining.CargoId), "its raw material untouched");
    }

    [TestMethod]
    public void OnceGoneAMiningStationLetsGoOfItsOps()
    {
        Planet mined = MinedPlanet();
        Ship station = Orbital("Basic Mining Station", mined);
        Player.AI.AddGoal(new MiningOps(Player, mined, station));

        TakeApartToTheEnd(station);
        Assert.IsNull(mined.Mining.Owner, "once it is gone, with no other station of its empire there, its ops are let go");
    }

    [TestMethod]
    public void AMiningStationsDismantlingCannotBeCalledOffOnceItsOpsAreAnotherEmpires()
    {
        Planet mined = MinedPlanet();
        Ship station = Orbital("Basic Mining Station", mined);
        Player.AI.AddGoal(new MiningOps(Player, mined, station));
        TakeApart(station);
        Assert.IsTrue(DismantleOrbital.CanCallOff(station), "setup: while its ops are its empire's it can be called off");

        mined.Mining.ChangeOwner(Enemy);
        station.ScrapFromButton();
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(station.Dismantling, "its ops another empire's, Scrap again does not call it off");
    }

    [TestMethod]
    public void ADysonSwarmControllerComingApartNoLongerCounts()
    {
        Colony.System.SetDysonSwarmType(1);
        Colony.System.ActivateDysonSwarm(Player);
        DysonSwarm swarm = Colony.System.DysonSwarm;
        var rings = (Array<SunLayerState>)typeof(DysonSwarm).GetField("DysonSwarmRings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(swarm);
        rings.Add(null); // its rings are only drawn, so their textures stay unloaded
        Ship controller = SpawnShip(DysonSwarm.DysonSwarmControllerName, Player, swarm.SwarmControllers.Keys.First());
        Assert.IsTrue(swarm.TryConnectControllerToGrid(controller), "setup: the controller takes its place in the swarm");
        swarm.Update();
        AssertGreaterThan(swarm.ControllerCompletion, 0f, "setup: the swarm counts it");

        TakeApart(controller);
        swarm.Update();
        AssertEqual(0f, swarm.ControllerCompletion, "coming apart, it counts no more");
    }

    [TestMethod]
    public void ARefitConstructorOnItsWayScrapsItselfOnceItsOrbitalComesApart()
    {
        Ship station = Orbital(PatrolStation, Colony);
        var refit = new RefitOrbital(station, ResourceManager.Ships.GetDesign(PatrolStation), Player);
        Ship constructor = SpawnShip("Terran Constructor", Player, Colony.Position + new Vector2(30_000f, 0));
        refit.FinishedShip = constructor;
        constructor.AI.OrderDeepSpaceBuild(refit, 100f, station.Radius);
        RunObjectsSim(1f);
        Assert.AreNotEqual(AIState.Scrap, constructor.AI.State, "setup: the constructor flies to build the refit");

        TakeApart(station);
        RunObjectsSim(1f);
        AssertEqual(AIState.Scrap, constructor.AI.State, "its orbital coming apart, it scraps itself");
    }

    [TestMethod]
    public void ARefitConstructorForADeepSpaceOrbitalScrapsItselfOnceItComesApart()
    {
        Ship station = SpawnShip(PatrolStation, Player, new Vector2(600_000f));
        var refit = new RefitOrbital(station, ResourceManager.Ships.GetDesign(PatrolStation), Player);
        Ship constructor = SpawnShip("Terran Constructor", Player, station.Position + new Vector2(30_000f, 0));
        refit.FinishedShip = constructor;
        constructor.AI.OrderDeepSpaceBuild(refit, 100f, station.Radius);
        RunObjectsSim(1f);
        Assert.AreNotEqual(AIState.Scrap, constructor.AI.State, "setup: the constructor flies to build the refit in deep space");

        TakeApart(station);
        RunObjectsSim(1f);
        AssertEqual(AIState.Scrap, constructor.AI.State, "its orbital coming apart, it scraps itself");
    }

    [TestMethod]
    public void ANewOrbitalIsNotPlacedWhereOneComesApart()
    {
        for (int i = 0; i < 9; ++i)
            TakeApart(Orbital(Platform, Colony, degrees: i * 40f));
        var build = new BuildOrbital(Colony, Platform, Player);
        MethodInfo find = typeof(BuildOrbital).GetMethod("FindNewOrbitalLocation", BindingFlags.Instance | BindingFlags.NonPublic);

        var at = (Vector2)find.Invoke(build, null);
        AssertEqual(1f, Colony.OrbitalRingRadius(1), at.Distance(Colony.Position),
                    "each place on the first ring held by one coming apart, a new one goes on the second");
    }
}
