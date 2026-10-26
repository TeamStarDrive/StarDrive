using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class MissileTargetLoadTests : StarDriveTest
{
    static readonly Vector2 Gun = new(300_000, 0);

    public MissileTargetLoadTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    static Weapon GuidedWeapon(Ship ship)
    {
        foreach (Weapon w in ship.Weapons)
            if (w.Tag_Guided)
                return w;
        return null;
    }

    // a missile fired from the gun at a ship 6000 off, flying for a step, and the target
    Projectile FireAt(string weaponUid, out Ship target, out ShipModule aimedAt)
    {
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -6000));
        aimedAt = target.Modules[1];
        Weapon w = weaponUid == null ? GuidedWeapon(launcher) : ResourceManager.CreateWeapon(UState, weaponUid, launcher, null);
        Projectile missile = Projectile.Create(w, launcher, Gun, Vectors.Up, aimedAt, playSound: false);
        Assert.IsNotNull(missile.MissileAI, "setup: the weapon must fire guided missiles");
        RunObjectsSim(TestSimStep);
        return missile;
    }

    static Projectile Loaded(UniverseState loaded, Projectile shot)
    {
        Projectile found = loaded.Objects.GetAllProjectilesAndBeams().FirstOrDefault(p => p.Id == shot.Id);
        Assert.IsNotNull(found, "setup: the missile must be in the save");
        return found;
    }

    void Think(Projectile missile, int steps)
    {
        for (int i = 0; i < steps; ++i)
            missile.MissileAI.Think(TestSimStep);
    }

    [TestMethod]
    public void AMissileFliesOnAtTheModuleItWasAfterWhenLoaded()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Projectile missile = FireAt(null, out Ship target, out ShipModule aimedAt);

        Projectile loaded = Loaded(BinarySerializerTests.SerDes(UState), missile);
        loaded.MissileAI.Think(TestSimStep);

        var aim = loaded.MissileAI.Target as ShipModule;
        Assert.IsNotNull(aim, "a loaded missile flies on at a module");
        AssertEqual(target.Id, aim.GetParent().Id, "of the ship it was after");
        AssertEqual(Array.IndexOf(target.Modules, aimedAt), Array.IndexOf(aim.GetParent().Modules, aim), "the module it was after");
    }

    [TestMethod]
    public void AMissileWithoutSmartMissilesSurvivesALoad()
    {
        Enemy.data.Traits.SmartMissiles = false;
        Projectile missile = FireAt(null, out _, out _);

        Projectile loaded = Loaded(BinarySerializerTests.SerDes(UState), missile);
        Think(loaded, 30);

        Assert.IsTrue(loaded.Active, "a loaded missile with its target still there flies on");
    }

    [TestMethod]
    public void ALockedOnMissileKeepsItsLockThroughALoad()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Projectile missile = FireAt(null, out Ship target, out _);
        missile.LockedOn = target;

        Projectile loaded = Loaded(BinarySerializerTests.SerDes(UState), missile);
        Think(loaded, 30);

        Assert.IsTrue(loaded.Active, "a loaded missile flies on");
        AssertEqual(target.Id, loaded.LockedOn?.Id ?? -1, "a loaded missile keeps its lock on the ship it was after");
    }

    [TestMethod]
    public void ASunkTargetIsNotSavedWithItsMissile()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Projectile missile = FireAt(null, out Ship target, out _);

        target.Die(null, cleanupOnly: true);
        Projectile loaded = Loaded(BinarySerializerTests.SerDes(UState), missile);

        object saved = typeof(Projectile).GetField("SavedTargetShip", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(loaded);
        Assert.IsNull(saved, "a save holds no ship that is gone");
    }

    [TestMethod]
    public void ATorpedoTakesBackItsSavedTargetOnlyOnce()
    {
        Projectile torpedo = FireAt("BasicTorpedo", out Ship target, out ShipModule aimedAt);
        Assert.IsTrue(torpedo.Weapon.Tag_Torpedo, "setup: BasicTorpedo must fire torpedoes");

        UniverseState universe = BinarySerializerTests.SerDes(UState);
        Projectile loaded = Loaded(universe, torpedo);
        loaded.MissileAI.Think(TestSimStep);
        var aim = loaded.MissileAI.Target as ShipModule;
        Assert.IsNotNull(aim, "a loaded torpedo takes back its target");

        AssertStaysOffItsLostAimPoint(loaded, aim, "the saved target is taken back once, not again later");
    }

    // its aim point is destroyed, so it flies straight on, and never turns back to it
    void AssertStaysOffItsLostAimPoint(Projectile torpedo, ShipModule aim, string what)
    {
        aim.Active = false;
        for (int i = 0; i < 100 && torpedo.MissileAI.Target != null; ++i)
            torpedo.MissileAI.Think(TestSimStep);
        Assert.IsNull(torpedo.MissileAI.Target, "setup: a torpedo whose aim point is gone flies straight on");

        for (int i = 0; i < 20; ++i)
        {
            torpedo.MissileAI.Think(TestSimStep);
            Assert.IsNull(torpedo.MissileAI.Target, $"{what} (step {i})");
        }
    }

    [TestMethod]
    public void ASaveLeavesTheMissilesInPlayAsTheyWere()
    {
        Projectile torpedo = FireAt("BasicTorpedo", out _, out ShipModule aimedAt);

        BinarySerializerTests.SerDes(UState);

        AssertStaysOffItsLostAimPoint(torpedo, aimedAt, "a torpedo in play does not turn back to its old target after a save");
    }
}
