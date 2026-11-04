using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A destroyed regenerating module comes back to life only while power reaches its slot, unless it needs no power,
/// with a chance each second of 20% plus its tech level. A living plate that carries power keeps its destroyed neighbors
/// in reach.
/// </summary>
[TestClass]
public class RegenerationTests : StarDriveTest
{
    static readonly FieldInfo Regenerate = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.Regenerate));
    static readonly FieldInfo PowerDraw = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.PowerDraw));
    static readonly FieldInfo TechLevel = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.TechLevel));
    static readonly FieldInfo PowerRadius = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.PowerRadius));
    const int Tries = 5000;

    readonly Ship Ship;
    readonly ShipModule Plate;

    public RegenerationTests()
    {
        LoadStarterShips("Dreadnought mk1-a");
        CreateUniverseAndPlayerEmpire();
        Ship = SpawnShip("Dreadnought mk1-a", Player, new Vector2(200_000f));
        Plate = Ship.Modules.First(m => m.ModuleType == ShipModuleType.Armor);
    }

    // the plates' template regenerates for the test, and is put back after
    void AsRegeneratingPlate(float powerDraw, int techLevel, System.Action test, int powerRadius = 0)
    {
        ShipModuleFlyweight template = Plate.Flyweight;
        object regenerate = Regenerate.GetValue(template), draw = PowerDraw.GetValue(template);
        object tech = TechLevel.GetValue(template), radius = PowerRadius.GetValue(template);
        try
        {
            Regenerate.SetValue(template, 50f);
            PowerDraw.SetValue(template, powerDraw);
            TechLevel.SetValue(template, techLevel);
            PowerRadius.SetValue(template, powerRadius);
            test();
        }
        finally
        {
            Regenerate.SetValue(template, regenerate);
            PowerDraw.SetValue(template, draw);
            TechLevel.SetValue(template, tech);
            PowerRadius.SetValue(template, radius);
        }
    }

    // the share of tries in which the destroyed plate came back within a second, powered as the grid left it unless set here
    static float ComebackRate(ShipModule plate, bool? powered = null)
    {
        int comebacks = 0;
        for (int i = 0; i < Tries; ++i)
        {
            plate.SetHealth(0f, "Test");
            Assert.IsFalse(plate.Active, "setup: the plate is destroyed");
            if (powered != null)
                plate.Powered = powered.Value;
            plate.RegenerateSelf();
            if (plate.Active)
                ++comebacks;
        }
        return comebacks / (float)Tries;
    }

    // whether the two modules' slots touch, at a side or a corner
    static bool Touches(ShipModule a, ShipModule b)
        => a.Pos.X <= b.Pos.X + b.XSize && b.Pos.X <= a.Pos.X + a.XSize
        && a.Pos.Y <= b.Pos.Y + b.YSize && b.Pos.Y <= a.Pos.Y + a.YSize;

    [TestMethod]
    public void ADestroyedPlateComesBackOnlyWhilePowerReachesIt()
    {
        AsRegeneratingPlate(powerDraw: 10f, techLevel: 4, () =>
        {
            Assert.AreEqual(0f, ComebackRate(Plate, powered: false), "with no power reaching its slot it stays destroyed");
            AssertEqual(0.025f, 0.24f, ComebackRate(Plate, powered: true), "with power it comes back at 20% plus its tech level of 4 a second");
        });
    }

    [TestMethod]
    public void AHigherTechLevelBringsADestroyedPlateBackMoreOften()
    {
        AsRegeneratingPlate(powerDraw: 10f, techLevel: 40, () =>
        {
            AssertEqual(0.03f, 0.60f, ComebackRate(Plate, powered: true), "at tech level 40 it comes back at 20% plus 40% a second");
        });
    }

    [TestMethod]
    public void ADestroyedPlateThatNeedsNoPowerComesBackWithoutIt()
    {
        AsRegeneratingPlate(powerDraw: 0f, techLevel: 4, () =>
        {
            AssertEqual(0.025f, 0.24f, ComebackRate(Plate, powered: false), "a plate that needs no power comes back at the same rate without any");
        });
    }

    [TestMethod]
    public void ALivingPlateThatCarriesPowerKeepsADestroyedNeighborInReach()
    {
        AsRegeneratingPlate(powerDraw: 10f, techLevel: 4, powerRadius: 1, test: () =>
        {
            ShipModule[] plates = Ship.Modules.Where(m => m.UID == Plate.UID).ToArray();
            ShipModule neighbor = plates.FirstOrDefault(m => m != Plate && Touches(m, Plate));
            Assert.IsNotNull(neighbor, "setup: the plate has a neighbor of its own kind");
            foreach (ShipModule m in plates)
                if (m != Plate)
                    m.SetHealth(0f, "Test");
            Ship.RecalculatePower();

            ShipModule outOfReach = plates.FirstOrDefault(m => m != Plate && !m.Powered);
            Assert.IsNotNull(outOfReach, "setup: a destroyed plate lies out of every power's reach");
            Assert.IsTrue(neighbor.Powered, "the living plate's power reaches its destroyed neighbor");
            AssertEqual(0.025f, 0.24f, ComebackRate(neighbor), "so the neighbor comes back");
            Assert.AreEqual(0f, ComebackRate(outOfReach), "while a destroyed plate out of every power's reach stays destroyed");
        });
    }
}
