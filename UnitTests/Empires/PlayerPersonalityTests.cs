using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
#pragma warning disable CA2213

namespace UnitTests.Empires;

/// <summary>
/// As on the race screen, the player's empire draws no personality: it has a blank diplomatic trait and the Generalists
/// strategy. The player's war offensive and expansion ratios use the average of the diplomatic traits; AI empires use
/// the trait they drew.
/// </summary>
[TestClass]
public class PlayerPersonalityTests : StarDriveTest
{
    public PlayerPersonalityTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    static void DrawTrait(Empire e, int territorialism, float opportunism)
        => e.data.DiplomaticPersonality = new DTrait { Territorialism = territorialism, Opportunism = opportunism };

    static (float territorialism, float opportunism) AverageTrait()
    {
        var traits = ResourceManager.DiplomaticTraits.DiplomaticTraitsList;
        Assert.IsTrue(traits.Count > 1, "setup: several diplomatic traits are loaded");
        float territorialism = 0, opportunism = 0;
        foreach (DTrait t in traits)
        {
            territorialism += t.Territorialism;
            opportunism += t.Opportunism;
        }
        return (territorialism / traits.Count, opportunism / traits.Count);
    }

    [TestMethod]
    public void ThePlayersEmpireDrawsNoPersonality()
    {
        AssertEqual("None", Player.data.DiplomaticPersonality.Name, "the player's empire has the blank diplomatic trait");
        AssertEqual(0f, Player.data.DiplomaticPersonality.Opportunism, "with no opportunism");
        AssertEqual("Generalists", Player.Research.Strategy.Name, "and the Generalists strategy");
        Assert.IsNull(Player.data.EconomicPersonality.Description, "made from no economic trait, not drawn from the list");
        Assert.AreNotEqual("None", Enemy.data.DiplomaticPersonality.Name, "an AI empire draws a trait");
    }

    [TestMethod]
    public void ThePlayersRatiosUseTheTraitsAverage()
    {
        (float territorialism, float opportunism) = AverageTrait();
        EconomicResearchStrategy strategy = Player.Research.Strategy;
        float cybernetic = Player.IsCybernetic ? 1 : 0;
        AssertEqual(0.0001f, (1 - territorialism / 100f + strategy.MilitaryRatio + opportunism) / 3, Player.GetWarOffensiveRatio(), "the war offensive ratio uses the average trait");
        AssertEqual(0.0001f, territorialism / 100f + strategy.ExpansionRatio + opportunism + cybernetic, Player.GetExpansionRatio(), "and so does the expansion ratio");
    }

    [TestMethod]
    public void ThePlayersRatiosIgnoreATraitGivenToItsEmpire()
    {
        DrawTrait(Player, 30, 0.75f);
        float offensive = Player.GetWarOffensiveRatio();
        float expansion = Player.GetExpansionRatio();
        DrawTrait(Player, 50, 0.2f);
        AssertEqual(0.0001f, offensive, Player.GetWarOffensiveRatio(), "the player's war offensive ratio");
        AssertEqual(0.0001f, expansion, Player.GetExpansionRatio(), "the player's expansion ratio");
    }

    [TestMethod]
    public void AnAIEmpiresRatiosFollowItsTrait()
    {
        DrawTrait(Enemy, 30, 0.75f);
        float offensive = Enemy.GetWarOffensiveRatio();
        float expansion = Enemy.GetExpansionRatio();
        DrawTrait(Enemy, 50, 0.2f);
        Assert.IsTrue(Math.Abs(offensive - Enemy.GetWarOffensiveRatio()) > 0.01f, "an AI empire's war offensive ratio follows its trait");
        Assert.IsTrue(Math.Abs(expansion - Enemy.GetExpansionRatio()) > 0.01f, "and so does its expansion ratio");
    }
}
