using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Data;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Empires;

/// <summary>
/// An Assimilator race that invades a world of level 3 or more takes on the former owner's traits that have no
/// downside. A gained trait adds to a weakness of the same kind and replaces a weaker strength of that kind, and the
/// race's trait list records it, so a second invasion of the same race gives nothing more.
/// </summary>
[TestClass]
public class TraitAssimilationTests : StarDriveTest
{
    public TraitAssimilationTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    static Array<RacialTraitOption> AllTraits => ResourceManager.RaceTraits.TraitList;

    static RacialTraitOption Trait(string name)
    {
        foreach (RacialTraitOption trait in AllTraits)
            if (trait.TraitName == name)
                return trait;
        Assert.Fail($"setup: the vanilla trait {name} exists");
        return null;
    }

    static bool HasTrait(Empire e, string name) => System.Array.IndexOf(e.data.SelectedTraitSet.Split(", "), name) >= 0;

    // a research strength weaker than Smart, which vanilla lacks
    static RacialTraitOption AddWeakerSmart()
    {
        var sharp = new RacialTraitOption { TraitName = "Test Sharp", ResearchMod = Trait("Smart").ResearchMod * 0.5f };
        AllTraits.Add(sharp);
        return sharp;
    }

    [TestMethod]
    public void AGainedStrengthAddsToAWeaknessOfTheSameKind()
    {
        Player.data.SelectedTraitSet = "Dumb";
        Player.data.Traits.ResearchMod = Trait("Dumb").ResearchMod;
        Enemy.data.SelectedTraitSet = "Smart";

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, Trait("Dumb").ResearchMod + Trait("Smart").ResearchMod, Player.data.Traits.ResearchMod,
                    "Smart adds to Dumb");
        Assert.IsTrue(HasTrait(Player, "Dumb") && HasTrait(Player, "Smart"), "and the race has both now");
    }

    [TestMethod]
    public void AGainedStrengthReplacesAWeakerOneOfTheSameKind()
    {
        RacialTraitOption sharp = AddWeakerSmart();
        try
        {
            Player.data.SelectedTraitSet = sharp.TraitName;
            Player.data.Traits.ResearchMod = sharp.ResearchMod;
            Enemy.data.SelectedTraitSet = "Smart";

            TraitAssimilation.Assimilate(Player, Enemy);

            AssertEqual(0.0001f, Trait("Smart").ResearchMod, Player.data.Traits.ResearchMod, "Smart takes the place of the weaker strength");
            Assert.IsTrue(HasTrait(Player, "Smart"), "the race has Smart now");
            Assert.IsFalse(HasTrait(Player, sharp.TraitName), "and no longer the trait it replaced");
        }
        finally
        {
            AllTraits.Remove(sharp);
        }
    }

    [TestMethod]
    public void AWeakerStrengthOfTheSameKindGivesNothing()
    {
        RacialTraitOption sharp = AddWeakerSmart();
        try
        {
            Player.data.SelectedTraitSet = "Smart";
            Player.data.Traits.ResearchMod = Trait("Smart").ResearchMod;
            Enemy.data.SelectedTraitSet = sharp.TraitName;

            TraitAssimilation.Assimilate(Player, Enemy);

            AssertEqual(0.0001f, Trait("Smart").ResearchMod, Player.data.Traits.ResearchMod, "the race keeps its stronger trait's value");
            AssertEqual("Smart", Player.data.SelectedTraitSet);
        }
        finally
        {
            AllTraits.Remove(sharp);
        }
    }

    [TestMethod]
    public void ASecondInvasionOfTheSameRaceGivesNothingMore()
    {
        Player.data.SelectedTraitSet = "Dumb";
        Player.data.Traits.ResearchMod = Trait("Dumb").ResearchMod;
        Enemy.data.SelectedTraitSet = "Smart";

        TraitAssimilation.Assimilate(Player, Enemy);
        float once = Player.data.Traits.ResearchMod;
        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, once, Player.data.Traits.ResearchMod, "Smart counts once");
    }

    [TestMethod]
    public void TraitsWithADownsideOrForTheStartDoNotComeOver()
    {
        Player.data.SelectedTraitSet = "";
        Enemy.data.SelectedTraitSet = "Lazy, Pack, Cybernetic, Huge Homeworld, Astronomers, Prototype Flagship";
        RacialTrait before = Player.data.Traits.GetClone();

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual("", Player.data.SelectedTraitSet);
        AssertEqual(0.0001f, before.ProductionMod, Player.data.Traits.ProductionMod, "Lazy does not come over");
        AssertEqual(before.Pack, Player.data.Traits.Pack, "nor Pack, which weakens a ship alone");
        AssertEqual(before.Cybernetic, Player.data.Traits.Cybernetic, "nor Cybernetic");
        AssertEqual(0.0001f, before.RepairMod, Player.data.Traits.RepairMod, "nor Cybernetic's repair");
        AssertEqual(0.0001f, before.HomeworldSizeMod, Player.data.Traits.HomeworldSizeMod, "nor a homeworld trait");
    }

    // passengers as the race screen sets them for the player: the trait's bonus is in the modifier, not in PassengerBonus
    static void SetPassengers(Empire e, string traits, float tech)
    {
        e.data.SelectedTraitSet = traits;
        e.data.Traits.PassengerBonus = 0;
        e.data.Traits.PassengerModifier = 1f + tech + (traits == "Manifest Destiny" ? Trait("Manifest Destiny").PassengerBonus : 0f);
    }

    [TestMethod]
    public void ManifestDestinyIsNotCountedTwiceWithAPassengerTech()
    {
        float bonus = Trait("Manifest Destiny").PassengerBonus;
        SetPassengers(Player, "", tech: 0f);
        SetPassengers(Enemy, "Manifest Destiny", tech: 0.5f);

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, 1.5f + bonus, Player.data.Traits.PassengerModifier, "the trait and the tech each count once");
        Assert.IsTrue(HasTrait(Player, "Manifest Destiny"), "and the race has the trait now");
    }

    [TestMethod]
    public void ARaceWithManifestDestinyStillTakesABetterPassengerTech()
    {
        float bonus = Trait("Manifest Destiny").PassengerBonus;
        SetPassengers(Player, "Manifest Destiny", tech: 0f);
        SetPassengers(Enemy, "", tech: 0.5f);

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, 1.5f + bonus, Player.data.Traits.PassengerModifier, "its own trait does not count as tech");
    }

    [TestMethod]
    public void ExplorersComesOverForItsExploringRange()
    {
        Player.data.SelectedTraitSet = "";
        Player.data.Traits.ExploreDistanceMultiplier = 1f;
        Enemy.data.SelectedTraitSet = "Explorers";

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, Trait("Explorers").ExploreDistanceMultiplier, Player.data.Traits.ExploreDistanceMultiplier, "its ships explore as far as Explorers do");
        Assert.IsTrue(HasTrait(Player, "Explorers"), "and the race has the trait now");
    }

    [TestMethod]
    public void ABetterTerraformingTechComesOver()
    {
        Player.data.Traits.TerraformingLevel = 1;
        Enemy.data.Traits.TerraformingLevel = 3;

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(3, Player.data.Traits.TerraformingLevel, "the former owner's better terraforming");
    }

    [TestMethod]
    public void AnAIGainsAStrengthScaledByItsDifficulty()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        Enemy.TestInitModifiers();
        float difficulty = Enemy.DifficultyModifiers.ResearchMod;
        Assert.IsTrue(difficulty > 0f && difficulty != 1f, "setup: Hard scales the AI's research traits");
        Enemy.data.SelectedTraitSet = "";
        float before = Enemy.data.Traits.ResearchMod;
        Player.data.SelectedTraitSet = "Smart";

        TraitAssimilation.Assimilate(Enemy, Player);

        AssertEqual(0.0001f, before + Trait("Smart").ResearchMod * difficulty, Enemy.data.Traits.ResearchMod,
                    "Smart counts for the AI as its own traits did at game start");
    }

    [TestMethod]
    public void GainingSpiritualScalesTheArtifactsTheRaceHolds()
    {
        Artifact art = ResourceManager.ArtifactsDict.Values.First(a => a.ResearchMod > 0f);
        Player.data.SelectedTraitSet = "";
        Player.data.Traits.Spiritual = 0f;
        Player.data.Traits.ResearchMod = 0f;
        Player.AddArtifact(art);
        AssertEqual(0.0001f, art.ResearchMod, Player.data.Traits.ResearchMod, "setup: the artifact adds its research");
        Enemy.data.SelectedTraitSet = "Spiritual";

        TraitAssimilation.Assimilate(Player, Enemy);

        float spiritual = Trait("Spiritual").Spiritual;
        AssertEqual(0.0001f, spiritual, Player.data.Traits.Spiritual, "the race is Spiritual now");
        AssertEqual(0.0001f, art.ResearchMod * (1f + spiritual), Player.data.Traits.ResearchMod, "and its artifact gives that much more");
    }

    [TestMethod]
    public void GainingSpiritualChangesFertilityOnlyByTheArtifactsDifference()
    {
        Artifact art = ResourceManager.ArtifactsDict.Values.First(a => a.FertilityMod > 0f);
        Planet world = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        Player.data.SelectedTraitSet = "";
        Player.data.Traits.Spiritual = 0f;
        Player.AddArtifact(art);
        world.AddMaxBaseFertility(art.FertilityMod * 0.5f - world.BaseMaxFertility); // a world now poorer than the artifact's bonus
        float before = world.BaseMaxFertility;
        Enemy.data.SelectedTraitSet = "Spiritual";

        TraitAssimilation.Assimilate(Player, Enemy);

        AssertEqual(0.0001f, before + art.FertilityMod * Trait("Spiritual").Spiritual, world.BaseMaxFertility,
                    "the world gains just the extra the artifact now gives");
    }

    [TestMethod]
    public void OnlyAnInvasionOfALevel3WorldAssimilates()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Planet world = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        Player.data.Traits.Assimilators = true;
        Player.data.SelectedTraitSet = "";
        Enemy.data.SelectedTraitSet = "Smart";

        world.ChangeOwnerByInvasion(Player, planetLevel: 2);
        Assert.IsFalse(HasTrait(Player, "Smart"), "a level 2 world gives no trait");

        world.ChangeOwnerByInvasion(Enemy, planetLevel: 3);
        world.ChangeOwnerByInvasion(Player, planetLevel: 3);
        Assert.IsTrue(HasTrait(Player, "Smart"), "a level 3 world does");
    }
}
