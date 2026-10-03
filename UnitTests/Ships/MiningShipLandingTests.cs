using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A mining ship leaves its mining station the way a fighter leaves its carrier, without the barrel roll, aimed at
/// the planet, and dives toward the planet from there. Back from the planet it lands on the station the same way,
/// played backwards: a short glide in at launch speed down into the station's centre. It unloads its ore and frees
/// its bay only once it has touched down.
/// </summary>
[TestClass]
public class MiningShipLandingTests : StarDriveTest
{
    readonly Planet GasGiant;
    readonly Ship Station;

    public MiningShipLandingTests()
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
        Station.ChangeOrdnance(Station.OrdinanceMax);
    }

    string Ore => GasGiant.Mining.CargoId;

    ShipModule BayOf(Ship miner) => Station.Modules.Find(m => m.TryGetHangarShip(out Ship s) && s == miner);

    // launches a miner with one unit of hold left, so it is full soon after it starts mining
    Ship LaunchMiner()
    {
        foreach (ShipModule bay in Station.Modules)
            if (bay.IsMiningBay) bay.HangarTimer = 0;
        Station.Carrier.MiningBays.ProcessMiningBays(0, GasGiant);

        Ship miner = null;
        foreach (ShipModule bay in Station.Modules)
            if (bay.IsMiningBay && bay.TryGetHangarShipActive(out Ship s)) miner = s;
        Assert.IsTrue(miner is { IsMiningShip: true, IsLaunching: true }, "setup: the station must launch a mining ship");
        miner.LoadCargo(Ore, miner.CargoSpaceMax - 1);
        return miner;
    }

    void RunUntilLanding(Ship miner) => RunSimWhile((simTimeout: 120, fatal: true), () => !miner.IsLanding);

    void RunUntilDiving(Ship miner) => RunSimWhile((simTimeout: 5, fatal: true), () => miner.LaunchShip is not { MinesPlanet: true });

    [TestMethod]
    public void AMinerLeavesItsStationAsAFighterLaunchesThenDivesTowardThePlanet()
    {
        Ship miner = LaunchMiner();
        Assert.IsTrue(miner.LaunchShip is { LeavesHangar: true, MinesPlanet: false }, "a miner first launches out of its station");
        float toPlanet = Station.Position.AngleToTarget(GasGiant.Position);
        float startDistance = miner.Position.Distance(GasGiant.Position);

        float rise = 0f, lastPitch = 0f, maxPitchStep = 0f, deepestPitch = 0f, maxTurn = 0f;
        float diveSeconds = 0f;
        for (float time = TestSimStep.FixedTime; diveSeconds < 1.5f; time += TestSimStep.FixedTime)
        {
            AssertLessThan(time, 5f, "the miner must start its dive");
            bool rising = miner.LaunchShip is { LeavesHangar: true };
            float heading = miner.RotationDegrees;
            RunObjectsSim(TestSimStep);
            float pitch = miner.XRotation.ToDegrees();
            if (rising)
            {
                AssertEqual(0.01f, toPlanet, miner.RotationDegrees, "the miner launches straight at the planet");
                AssertLessThan(pitch, 31.6f, "it climbs out nose up from 31.5 degrees, as a fighter does");
                if (miner.LaunchShip is { MinesPlanet: true })
                {
                    rise = time;
                    AssertGreaterThan(startDistance - miner.Position.Distance(GasGiant.Position), 400f,
                                      "the launch carries it 420 toward the planet, at 300 for 1.4 seconds");
                }
            }
            else
            {
                diveSeconds += TestSimStep.FixedTime;
                deepestPitch = Math.Min(deepestPitch, pitch);
            }
            if (time > TestSimStep.FixedTime)
            {
                maxPitchStep = Math.Max(maxPitchStep, Math.Abs(pitch - lastPitch));
                maxTurn = Math.Max(maxTurn, Math.Abs(miner.RotationDegrees - heading));
            }
            lastPitch = pitch;
        }

        AssertEqual(TestSimStep.FixedTime * 2, LaunchShip.HangarDuration(miner), rise, "the launch takes as long as a fighter's, 1.4 seconds");
        AssertLessThan(maxPitchStep, 1f, "the dive noses down from the level end of the launch, with no jump");
        AssertLessThan(maxTurn, 0.5f, "and keeps heading for the planet");
        AssertEqual(1f, -60f, deepestPitch, "the dive is steepest a fifth of the way down, at 60 degrees");
    }

    [TestMethod]
    public void AMinerCalledBackWhileLeavingItsStationLandsWithoutDiving()
    {
        Ship miner = LaunchMiner();
        RunSimWhile((simTimeout: 0.5, fatal: false));
        Assert.IsTrue(miner.LaunchShip is { LeavesHangar: true }, "setup: the miner must still be leaving its station");

        miner.AI.OrderReturnToHangar();
        bool dived = false;
        // the launch ends at the edge of the landing range, so now and then the miner turns back first (11 s)
        RunSimWhile((simTimeout: 30, fatal: true), () => !miner.IsLanding, () => dived |= miner.LaunchShip is { LeavesHangar: false });
        Assert.IsFalse(dived, "a miner called back before its dive never starts it");
        RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        Assert.IsFalse(miner.Dying, "the recalled miner lands and is taken in");
        AssertEqual(miner.CargoSpaceMax - 1, Station.GetOtherCargo(Ore), "it unloads what it had");
    }

    [TestMethod]
    public void AMinerLeavingItsStationDivesAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship miner = LaunchMiner();
        RunSimWhile((simTimeout: 0.5, fatal: false));

        SavedGame save = Universe.Save("UnitTest.MiningShipLaunch", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship leaving = loaded.UState.Objects.FindShip(miner.Id);
        Assert.IsTrue(leaving is { Active: true, LaunchShip.LeavesHangar: true, AI.State: AIState.Mining },
                      "a miner leaving its station must still be leaving it after a load");

        for (double time = 0; leaving.LaunchShip is not { MinesPlanet: true }; time += TestSimStepD)
        {
            AssertLessThan(time, 3.0, "the loaded miner must go on to its dive");
            loaded.UState.Objects.Update(TestSimStep);
        }
    }

    [TestMethod]
    public void AFullMinerLandsOnItsStationAndUnloadsAtTouchdown()
    {
        Ship miner = LaunchMiner();
        ShipModule bay = BayOf(miner);
        RunUntilLanding(miner);

        float range = LandShip.HangarLandingRange(miner);
        AssertEqual(420f, range, "the Mining Ship glides in at its launch speed, 300, for the hangar launch's 1.4 seconds");
        AssertLessThan(miner.Position.Distance(Station.Position), range + 1f, "the landing starts within the glide range");
        AssertLessThan(miner.Velocity.Length(), LaunchShip.HangarSpeed(miner) + 10f, "the miner comes in no faster than it launches");
        AssertEqual(AIState.ReturnToHangar, miner.AI.State, "a landing miner is still returning to its hangar");
        AssertEqual(0f, Station.GetOtherCargo(Ore), "nothing is unloaded before touchdown");
        Assert.AreSame(miner, bay.HangarShip, "the bay still holds its miner while it lands, so it launches no other");

        double landing = RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        AssertEqual(TestSimStepD * 2, LaunchShip.HangarDuration(miner), landing, "the landing takes as long as the hangar launch");
        Assert.IsFalse(miner.Dying, "a landed miner is taken in, not destroyed");
        AssertLessThan(miner.Position.Distance(Station.Position), 1f, "the miner touches down at the station's centre, where it launched");
        AssertEqual(0.01f, -31.5f, miner.XRotation.ToDegrees(), "the miner ends nose down, as the hangar launch starts nose up");
        AssertEqual(miner.CargoSpaceMax, Station.GetOtherCargo(Ore), "the whole load is unloaded at touchdown");
        Assert.IsNull(bay.HangarShip, "the bay is free again");
        AssertEqual(0.1f, 5f, bay.HangarTimer, "the bay readies the next miner as before, at least 5 seconds");
    }

    [TestMethod]
    public void ALandingMinerFollowsItsStation()
    {
        Ship miner = LaunchMiner();
        RunUntilLanding(miner);

        Station.TetherOffset += new Vector2(0, 600);
        RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        AssertLessThan(miner.Position.Distance(Station.Position), 1f, "the miner touches down where the station is now");
        AssertEqual(miner.CargoSpaceMax, Station.GetOtherCargo(Ore), "and unloads there");
    }

    [TestMethod]
    public void AMinerWhoseStationIsLostWhileItLandsIsRemovedWithItsLoad()
    {
        Ship miner = LaunchMiner();
        RunUntilLanding(miner);

        Station.QueueTotalRemoval();
        float ordnance = Station.Ordinance;
        RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        Assert.IsFalse(miner.Dying, "the miner finishes its landing on the spot the station left, and is removed");
        AssertEqual(0f, Station.GetOtherCargo(Ore), "a lost station takes no ore");
        AssertEqual(ordnance, Station.Ordinance, "nor the ordnance the miner would bring back");
    }

    [TestMethod]
    public void AMinerCalledBackDuringItsDiveTakesOffBeforeItLands()
    {
        Ship miner = LaunchMiner();
        RunUntilDiving(miner);
        RunSimWhile((simTimeout: 1, fatal: false));
        Assert.IsTrue(miner.LaunchShip is { MinesPlanet: true }, "setup: the miner must still be diving toward the planet");

        miner.AI.OrderReturnToHangar();
        RunUntilLanding(miner);
        RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        Assert.IsFalse(miner.Dying, "the recalled miner lands and is taken in");
        AssertEqual(miner.CargoSpaceMax - 1, Station.GetOtherCargo(Ore), "it unloads what it had");
    }

    [TestMethod]
    public void AMinerIsUntouchableWhileItMines()
    {
        Ship miner = LaunchMiner();
        miner.UnloadCargo(Ore, miner.GetOtherCargo(Ore));
        RunSimWhile((simTimeout: 60, fatal: true), () => miner.GetOtherCargo(Ore) <= 0f);
        Assert.IsTrue(miner.LaunchShip is { MinesPlanet: true }, "a mining ship is still in its mining dive while it mines");

        float health = miner.Health;
        Ship dying = SpawnShip("Vulcan Scout", Enemy, miner.Position + new Vector2(0, 300));
        RunObjectsSim(TestSimStep);
        miner.UpdateModulePositions(TestSimStep, forceUpdate: true);
        for (int i = 0; i < 20; ++i)
            UState.Spatial.ShipExplode(dying, 10_000f, miner.Position, 500f);
        AssertEqual(health, miner.Health, "nothing hurts a miner at the planet, as nothing hurts a launching ship");
    }

    [TestMethod]
    public void AFreighterRoleMinerComesHomeWhileItsStationHasFightersOut()
    {
        Player.data.DefaultMiningShip = "Small Transport";
        Station.Carrier.FightersOut = true;
        Ship miner = LaunchMiner();
        Assert.IsTrue(miner is { Name: "Small Transport", IsSupplyShuttle: false },
                      "setup: a mining ship design of freighter role, as most Combined Arms miners are");

        RunUntilLanding(miner);
        RunSimWhile((simTimeout: 10, fatal: true), () => miner.Active);
        AssertEqual(miner.CargoSpaceMax, Station.GetOtherCargo(Ore), "the miner lands and unloads instead of escorting its station");
    }

    [TestMethod]
    public void ALandingMinerCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship miner = LaunchMiner();
        RunUntilLanding(miner);

        SavedGame save = Universe.Save("UnitTest.MiningShipLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(miner.Id);
        Ship loadedStation = loaded.UState.Objects.FindShip(Station.Id);
        Assert.IsTrue(landing is { Active: true, LandShip.InHangar: true, AI.State: AIState.ReturnToHangar },
                      "a landing miner must still be landing after a load");
        Assert.AreSame(loadedStation, landing.LandShip.Mothership, "it still lands on its own station");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 10.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsFalse(landing.Dying, "a landed miner is taken in, not destroyed");
        AssertEqual(landing.CargoSpaceMax, loadedStation.GetOtherCargo(Ore), "the loaded landing still unloads at touchdown");
    }
}
