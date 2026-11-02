using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class InertialDamperMassTests : StarDriveTest
{
    const string Reactor = "Fusion Reactor"; // 2x2, mass 46
    const string Cargo = "BasicCargoHold"; // 1x1, mass 3
    const string Damper = "Inertial Damper"; // 2x2, mass -2000

    readonly ShipHull Hull;

    public InertialDamperMassTests()
    {
        CreateUniverseAndPlayerEmpire();
        Assert.IsTrue(ResourceManager.Hull("Terran/Dreadnought", out Hull), "setup: hull");
    }

    ShipModule Module(string uid)
    {
        ShipModule m = ShipModule.CreateDesignModule(UState, uid, ModuleOrientation.Normal, 0, null, Hull);
        m.Powered = true;
        return m;
    }

    ShipModule[] Modules(int reactors, int cargoHolds, int dampers)
    {
        var modules = new Array<ShipModule>();
        for (int i = 0; i < reactors; ++i) modules.Add(Module(Reactor));
        for (int i = 0; i < cargoHolds; ++i) modules.Add(Module(Cargo));
        for (int i = 0; i < dampers; ++i) modules.Add(Module(Damper));
        return modules.ToArray();
    }

    // no surface area unless given, so the hull floor is 0
    float Mass(ShipModule[] modules, int surfaceArea = 0)
    {
        TestShip ship = SpawnShip("Vulcan Scout", Player, new Vector2(200_000));
        return ship.Stats.InitializeMass(modules, Player, surfaceArea, ordnancePercent: 1f);
    }

    // reactors and dampers on separate 2x2 blocks of the hull
    ShipDesign Design(int reactors, int dampers)
    {
        var free = new HashSet<Point>();
        foreach (HullSlot s in Hull.HullSlots)
            free.Add(s.Pos);

        var slots = new Array<DesignSlot>();
        foreach (HullSlot s in Hull.HullSlots)
        {
            if (slots.Count == reactors + dampers)
                break;
            Point p = s.Pos;
            var block = new[] { p, new Point(p.X + 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X + 1, p.Y + 1) };
            bool open = true;
            foreach (Point c in block)
                open &= free.Contains(c);
            if (!open)
                continue;
            foreach (Point c in block)
                free.Remove(c);
            string uid = slots.Count < reactors ? Reactor : Damper;
            slots.Add(new DesignSlot(p, uid, new Point(2, 2), 0, ModuleOrientation.Normal, null));
        }
        AssertEqual(reactors + dampers, slots.Count, "setup: room on the hull");

        var design = new ShipDesign(Hull, $"Damper Test {reactors} {dampers}");
        design.SetDesignSlots(slots.ToArray());
        return design;
    }

    [TestMethod]
    public void DampersTakeOffAtMostHalfOfTheMassLeftAfterTheEmpiresMassBonus()
    {
        Player.data.MassModifier = 0.8f;
        // 1000 mass, 800 after the bonus, against a 2000 damper
        ShipModule[] modules = Modules(reactors: 10, cargoHolds: 180, dampers: 1);
        AssertEqual(0.01f, 400f, Mass(modules));
    }

    [TestMethod]
    public void TheHullFloorCountsInTheMassDampersMayTakeHalfOf()
    {
        Player.data.MassModifier = 0.8f;
        // 100 slots have a floor of 60, so 1060 mass, 848 after the bonus
        ShipModule[] modules = Modules(reactors: 10, cargoHolds: 180, dampers: 1);
        AssertEqual(0.01f, 424f, Mass(modules, surfaceArea: 100));
    }

    [TestMethod]
    public void ADamperTakingOffLessThanHalfTakesOffAllOfItsMass()
    {
        Player.data.MassModifier = 0.8f;
        // 6900 mass, 5520 after the bonus
        ShipModule[] modules = Modules(reactors: 150, cargoHolds: 0, dampers: 1);
        AssertEqual(0.01f, 3520f, Mass(modules));
    }

    [TestMethod]
    public void ADesignsBaseMassStopsAtHalfOfTheMassOfItsOtherModules()
    {
        AssertEqual(0.01f, 460f, Design(reactors: 10, dampers: 0).BaseMass, "ten reactors");
        AssertEqual(0.01f, 230f, Design(reactors: 10, dampers: 1).BaseMass, "ten reactors and a 2000 damper");
    }

    [TestMethod]
    public void TheShipyardWarnsInOrangeOfDamperMassPastTheLimit()
    {
        var issues = new ShipDesignIssues(Universe, ResourceManager.Ships.GetDesign("Vulcan Scout"));
        issues.CheckDampersPastLimit(positiveMass: 800f, dampersMass: -400f, minMass: 0);
        Assert.IsTrue(issues.CurrentDesignIssues.IsEmpty, "dampers taking off exactly half are not flagged");

        issues.CheckDampersPastLimit(positiveMass: 800f, dampersMass: -600f, minMass: 0);
        AssertEqual(1, issues.CurrentDesignIssues.Count, "dampers taking off more than half are flagged");
        DesignIssueDetails issue = issues.CurrentDesignIssues[0];
        Assert.AreEqual(DesignIssueType.DampersPastLimit, issue.Type);
        Assert.AreEqual(WarningLevel.Major, issue.Severity, "in orange");
        Assert.AreEqual($" {Localizer.Token(GameText.DesignIssueDampersPastLimitMass)} {200f.GetNumberString()}.", issue.AdditionalText,
                        "naming the mass past the limit");
    }

    [TestMethod]
    public void TheShipyardWarningCountsDamperMassBelowTheHullFloor()
    {
        var issues = new ShipDesignIssues(Universe, ResourceManager.Ships.GetDesign("Vulcan Scout"));
        // a 900 floor leaves the dampers 100 of the 1000 to take off
        issues.CheckDampersPastLimit(positiveMass: 1000f, dampersMass: -600f, minMass: 900);
        AssertEqual(1, issues.CurrentDesignIssues.Count, "flagged");
        Assert.AreEqual($" {Localizer.Token(GameText.DesignIssueDampersPastLimitMass)} {500f.GetNumberString()}.",
                        issues.CurrentDesignIssues[0].AdditionalText, "naming all the damper mass that takes nothing off");
    }
}
