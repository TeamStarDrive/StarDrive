using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class BlueprintLinkTests : StarDriveTest
{
    const string Next = "BlueprintLinkTests.Next";
    readonly Planet P;
    readonly string Researched;
    readonly string Unresearched;
    readonly string AlsoUnresearched;

    public BlueprintLinkTests()
    {
        CreateUniverseAndPlayerEmpire();
        P = AddHomeWorldToEmpire(new Vector2(1000), Player);
        string[] locked = ResourceManager.BuildingsDict.Values
            .Where(b => b.IsSuitableForBlueprints && !b.IsMilitary && b.EventOnBuild == null && !Player.IsBuildingUnlocked(b.Name))
            .Select(b => b.Name).Take(3).ToArray();
        AssertEqual(3, locked.Length, "test needs three buildings the empire has not researched");
        Researched = locked[0];
        Unresearched = locked[1];
        AlsoUnresearched = locked[2];
        Player.UnlockEmpireBuilding(Researched);
        ResourceManager.AddBlueprintsTemplate(new BlueprintsTemplate(Next, false, null, new HashSet<string> { Researched }, Planet.ColonyType.Colony));
    }

    const string LoopA = "BlueprintLinkTests.LoopA";
    const string LoopB = "BlueprintLinkTests.LoopB";

    [TestCleanup]
    public void RemoveLinkedBlueprints()
    {
        foreach (string name in new[] { Next, LoopA, LoopB })
            ResourceManager.BlueprintsTemplatesDict.Remove(name);
    }

    void Upload(bool skipUnresearched, params string[] planned)
    {
        P.AddBlueprints(new BlueprintsTemplate("first", false, Next, new HashSet<string>(planned), Planet.ColonyType.Colony, skipUnresearched), Player);
    }

    void Build(string name)
    {
        PlanetGridSquare tile = P.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
        Assert.IsNotNull(tile, "test planet ran out of habitable tiles");
        tile.PlaceBuilding(ResourceManager.CreateBuilding(P, name), P);
        P.Blueprints.UpdateCompletion();
    }

    [TestMethod]
    public void APlanSkippingUnresearchedBuildingsMovesToItsLinkOnceTheResearchedOnesStand()
    {
        Upload(skipUnresearched: true, Researched, Unresearched);
        AssertEqual("first", P.Blueprints.Name, "setup: nothing stands yet");

        Build(Researched);

        AssertEqual(Next, P.Blueprints.Name, "the one researched building stands, so the linked plan takes over");
    }

    [TestMethod]
    public void APlanWithoutTheFlagWaitsForEveryBuilding()
    {
        Upload(skipUnresearched: false, Researched, Unresearched);

        Build(Researched);

        AssertEqual("first", P.Blueprints.Name, "an ordinary plan moves on only when all of it stands");
    }

    [TestMethod]
    public void APlanSkippingUnresearchedWithNothingResearchedDoesNotMoveOn()
    {
        Upload(skipUnresearched: true, Unresearched, AlsoUnresearched);

        P.Blueprints.UpdateCompletion();

        AssertEqual("first", P.Blueprints.Name, "a plan with nothing researched has not been started, let alone done");
    }

    [TestMethod]
    public void TwoPlansLinkedToEachOtherStopOnceBothAreDone()
    {
        var a = new BlueprintsTemplate(LoopA, false, LoopB, new HashSet<string> { Researched, Unresearched }, Planet.ColonyType.Colony, skipUnresearched: true);
        ResourceManager.AddBlueprintsTemplate(a);
        ResourceManager.AddBlueprintsTemplate(new BlueprintsTemplate(LoopB, false, LoopA, new HashSet<string> { Researched }, Planet.ColonyType.Colony));
        PlanetGridSquare tile = P.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
        tile.PlaceBuilding(ResourceManager.CreateBuilding(P, Researched), P);

        P.AddBlueprints(a, Player);

        AssertEqual(LoopB, P.Blueprints.Name, "both plans are done; the colony moves on once and does not go round the loop forever");
    }

    void AddLoop()
    {
        ResourceManager.AddBlueprintsTemplate(new BlueprintsTemplate(LoopA, false, LoopB, new HashSet<string> { Researched }, Planet.ColonyType.Colony));
        ResourceManager.AddBlueprintsTemplate(new BlueprintsTemplate(LoopB, false, LoopA, new HashSet<string> { Researched }, Planet.ColonyType.Colony));
    }

    [TestMethod]
    public void PlansWhoseLinksLoopCannotBeLinkedTo()
    {
        AddLoop();
        ResourceManager.TryGetBlueprints(LoopA, out BlueprintsTemplate a);

        Assert.IsFalse(a.CanSafelyLinkFor("unlinked"), "a chain that goes round in a loop is not offered as a link");
    }

    [TestMethod]
    public void SavingAPlanUnderTheNameItsChainLeadsBackToDropsItsLink()
    {
        ResourceManager.AddBlueprintsTemplate(new BlueprintsTemplate(LoopA, false, LoopB, new HashSet<string> { Researched }, Planet.ColonyType.Colony));
        var edited = new BlueprintsTemplate("edited", false, LoopA, new HashSet<string> { Researched }, Planet.ColonyType.Colony);

        edited.RenameForSave(LoopB);

        AssertEqual(LoopB, edited.Name, "the plan takes its new name");
        AssertEqual("", edited.LinkTo, "saved as B, its link to A would close the loop A to B to A, so the link is dropped");
    }

    [TestMethod]
    public void SavingAPlanUnderANameItsChainDoesNotReachKeepsItsLink()
    {
        var edited = new BlueprintsTemplate("edited", false, Next, new HashSet<string> { Researched }, Planet.ColonyType.Colony);

        edited.RenameForSave(LoopB);

        AssertEqual(Next, edited.LinkTo, "a link that does not lead back to the plan stays");
    }

    [TestMethod]
    public void APlanBuiltBeyondWhatIsResearchedCountsAsAchieved()
    {
        P.AddBlueprints(new BlueprintsTemplate("unlinked", false, null, new HashSet<string> { Researched, Unresearched }, Planet.ColonyType.Colony), Player);

        Build(Researched);
        Build(Unresearched);

        AssertEqual(100, P.Blueprints.PercentCompleted, "setup: the whole plan stands, the unresearched building too, as on a captured colony");
        AssertEqual(50, P.Blueprints.PercentAchievable, "setup: half the plan is researched");
        Assert.IsTrue(P.Blueprints.IsAchievableCompleted, "a plan built past what is researched has reached its achievable part");
    }
}
