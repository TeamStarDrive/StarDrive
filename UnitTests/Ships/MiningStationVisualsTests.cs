using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A refining mining station seen at planet view or closer puffs smoke from each of its mining bays and shows a pillar of
/// light in its owner's colour through each bay, brighter with higher output. The pillar flickers on like a fluorescent
/// tube when the station starts refining and flickers off when it stops.
/// </summary>
[TestClass]
public class MiningStationVisualsTests : StarDriveTest
{
    readonly Planet GasGiant;
    readonly Ship Station;
    readonly CountingParticle Flames = new();
    readonly CountingParticle Smoke = new();

    public MiningStationVisualsTests()
    {
        LoadStarterShips("Mining Ship");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        GasGiant = AddDummyPlanet(new Vector2(200_000), 0, 0, 0, new Vector2(205_000), explored: true);
        GasGiant.GenerateNewFromPlanetType(new SeededRandom(), ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant),
                                           scale: 1.5f, preDefinedPop: 16);
        GasGiant.Mining = new Mineable(GasGiant);
        Station = SpawnShip("Basic Mining Station", Player, GasGiant.Position + new Vector2(GasGiant.OrbitalRingRadius(0), 0));
        Station.TetherToPlanet(GasGiant);
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        Universe.Particles = new ParticleManager(Content) { PhotonExplosion = Flames, SmokePlume = Smoke };
    }

    public override void Cleanup()
    {
        Universe.Particles?.Dispose();
        Universe.Particles = null;
        base.Cleanup();
    }

    MiningBays Bays => Station.Carrier.MiningBays;
    ShipModule[] MiningBayModules => Station.Modules.Where(m => m.IsMiningBay).ToArray();

    void WatchRefining(float ratio, float seconds)
    {
        Bays.UpdateIsRefining(ratio);
        for (float time = 0f; time < seconds; time += TestSimStep.FixedTime)
        {
            Station.InFrustum = true;
            Station.KnownByEmpires.SetSeen(Player);
            Station.UpdateModulePositions(TestSimStep);
        }
        Assert.IsTrue(Station.IsVisibleToPlayer, "setup: the station must be on screen");
    }

    float BrightnessAt(float simTime) => Bays.PillarBrightness(simTime);

    void RefiningChangesAt(float simTime, float ratio)
    {
        Universe.CurrentSimTime = simTime;
        Bays.UpdateIsRefining(ratio);
    }

    [TestMethod]
    public void ARefiningStationPuffsSmokeFromEveryBayAndNoFlames()
    {
        Assert.AreEqual(2, MiningBayModules.Length, "setup: the Basic Mining Station has two mining bays");
        Assert.IsTrue(MiningBayModules.All(m => m.Powered), "setup: the bays must be powered");

        WatchRefining(ratio: 1f, seconds: 10f);
        AssertEqual(2f, 39f, Smoke.Added / 10f / MiningBayModules.Length, "smoke particles per second from each bay");
        Assert.AreEqual(0, Flames.Added, "the blue flames are replaced by the light pillars");
    }

    [TestMethod]
    public void AnIdleStationShowsNothing()
    {
        WatchRefining(ratio: 0f, seconds: 10f);
        Assert.AreEqual(0, Smoke.Added, "an idle station does not smoke");
        var pillars = new Array<LightPillar>();
        Bays.AddLightPillars(10f, pillars);
        Assert.AreEqual(0, pillars.Count, "an idle station shows no light pillars");
    }

    [TestMethod]
    public void EachMiningBayShowsAPillarInItsOwnersColourRisingThroughTheStation()
    {
        RefiningChangesAt(5f, 1f);
        var pillars = new Array<LightPillar>();
        Bays.AddLightPillars(6f, pillars);

        ShipModule[] bays = MiningBayModules;
        Assert.AreEqual(bays.Length, pillars.Count, "one pillar per mining bay");
        for (int i = 0; i < bays.Length; ++i)
        {
            LightPillar pillar = pillars[i];
            AssertEqual(0.01f, bays[i].Position.X, pillar.Position.X, "the pillar stands on its bay");
            AssertEqual(0.01f, bays[i].Position.Y, pillar.Position.Y, "the pillar stands on its bay");
            AssertLessThan(pillar.TopZ, -Station.Radius, "it rises above the station, toward the camera as the smoke does");
            AssertGreaterThan(pillar.BottomZ, 0f, "and starts below it");
            AssertEqual(0.01f, bays[i].Radius * 0.5f, pillar.HalfWidth * 2f, "it is half as wide as the bay's radius");
            Color empire = Player.EmpireColor;
            Assert.AreEqual((empire.R, empire.G, empire.B), (pillar.Color.R, pillar.Color.G, pillar.Color.B), "in the owner's colour");
        }
    }

    [TestMethod]
    public void APillarIsBrighterTheHigherTheOutput()
    {
        RefiningChangesAt(5f, 1f);
        AssertEqual(0.001f, 1f, BrightnessAt(10f), "full output, full brightness");
        RefiningChangesAt(6f, 0.5f);
        AssertEqual(0.001f, MiningBays.DimmestPillar + (1f - MiningBays.DimmestPillar) * 0.5f, BrightnessAt(10f), "half output");
        RefiningChangesAt(7f, 0.01f);
        AssertEqual(0.01f, MiningBays.DimmestPillar, BrightnessAt(10f), "the lowest output is still visible");
    }

    [TestMethod]
    public void APillarFlickersOnWhenRefiningStartsThenStaysLit()
    {
        RefiningChangesAt(5f, 0f);
        RefiningChangesAt(10f, 1f);

        var levels = new[] { BrightnessAt(10.05f), BrightnessAt(10.18f), BrightnessAt(10.4f), BrightnessAt(10.6f),
                             BrightnessAt(10.8f), BrightnessAt(11f) };
        Assert.AreEqual("0 1 0 0.6 0 1", string.Join(" ", levels.Select(l => l.ToString("0.#"))), "a fluorescent start");
        AssertLessThan(BrightnessAt(11.7f), 1f, "still flickering late in its two seconds");
        AssertEqual(0.001f, 1f, BrightnessAt(12f), "then steady after two seconds");
        AssertEqual(0.001f, 1f, BrightnessAt(100f), "and it stays lit");
    }

    [TestMethod]
    public void APillarFlickersOffWhenRefiningStops()
    {
        RefiningChangesAt(5f, 0f);
        RefiningChangesAt(10f, 1f);
        RefiningChangesAt(20f, 0f);

        AssertEqual(0.001f, 1f, BrightnessAt(20.1f), "it is still lit for a moment");
        AssertEqual(0.001f, 0f, BrightnessAt(20.75f), "then blinks out");
        AssertGreaterThan(BrightnessAt(21f), 0f, "and back");
        AssertGreaterThan(BrightnessAt(21.85f), 0f, "still sputtering late in its two seconds");
        AssertEqual(0.001f, 0f, BrightnessAt(22f), "then goes dark after two seconds");
        var pillars = new Array<LightPillar>();
        Bays.AddLightPillars(22f, pillars);
        Assert.AreEqual(0, pillars.Count, "a dark station draws no pillars");
    }

    [TestMethod]
    public void AStationAlreadyRefiningWhenLoadedIsLitWithoutFlicker()
    {
        RefiningChangesAt(10f, 1f);
        AssertEqual(0.001f, 1f, BrightnessAt(10.12f), "the first refining turn after a load or a launch is not a start");
    }

    [TestMethod]
    public void RefiningTurnAfterTurnDoesNotRestartTheFlicker()
    {
        AddHomeWorldToEmpire(new Vector2(150_000), Player);
        Player.DoMoney();
        GasGiant.Mining.ChangeOwner(Player);
        var ops = new MiningOps(Player, GasGiant, Station);
        if (Player.IsCybernetic)
            Station.LoadProduction(Station.MiningStationCargoSpaceMax);
        else
            Station.LoadFood(Station.MiningStationCargoSpaceMax);

        Universe.CurrentSimTime = 5f;
        ops.Evaluate();
        Assert.AreEqual(0, Bays.RefiningOutput, "setup: with no raw material the station is idle");

        Station.LoadCargo(GasGiant.Mining.CargoId, Station.MiningStationCargoSpaceMax);
        Universe.CurrentSimTime = 10f;
        ops.Evaluate();
        Assert.IsTrue(Bays.RefiningOutput > 0, "setup: with raw material and supplies the station refines");
        AssertEqual(0.001f, 0f, BrightnessAt(10.12f), "setup: the pillar flickers on when refining starts");

        Universe.CurrentSimTime = 20f;
        ops.Evaluate();
        Assert.IsTrue(Bays.RefiningOutput > 0, "setup: the station still refines the next turn");
        AssertGreaterThan(BrightnessAt(20.12f), 0.3f, "another refining turn keeps the pillar lit instead of flickering it again");
    }

    sealed class CountingParticle : IParticle
    {
        public int Added;

        public string Name => "Counting";
        public int ParticleId => 0;
        public bool IsEnabled { get; set; } = true;
        public bool EnableDebug { get; set; }
        public int MaxParticles => int.MaxValue;
        public int ActiveParticles => Added;
        public bool IsOutOfParticles => false;

        public void AddParticle(in Vector3 position) => ++Added;
        public void AddParticle(in Vector3 position, float scale) => ++Added;
        public void AddParticle(in Vector3 position, in Vector3 velocity) => ++Added;
        public void AddParticle(in Vector3 position, in Vector3 velocity, float scale, Color color) => ++Added;
        public void AddMultipleParticles(int numParticles, in Vector3 position) => Added += numParticles;
        public void AddMultipleParticles(int numParticles, in Vector3 position, float scale) => Added += numParticles;
        public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity) => Added += numParticles;
        public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity, float scale, Color color)
            => Added += numParticles;

        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition)
            => new(this, particlesPerSecond, 1f, initialPosition);
        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition, float scale)
            => new(this, particlesPerSecond, scale, initialPosition);
        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition)
            => new(this, particlesPerSecond, 1f, new Vector3(initialPosition, 0f));
        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition, float scale)
            => new(this, particlesPerSecond, scale, new Vector3(initialPosition, 0f));

        public void Update(float totalSimulationTime) { }
        public void Draw(in Matrix view, in Matrix projection, bool nearView) { }
        public void Dispose() { }
    }
}
