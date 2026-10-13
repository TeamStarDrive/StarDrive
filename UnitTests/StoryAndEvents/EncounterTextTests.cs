using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.StoryAndEvents;

namespace UnitTests.StoryAndEvents;

[TestClass]
public class EncounterTextTests : StarDriveTest
{
    public EncounterTextTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    string Shown(string text)
    {
        var messages = new Array<Message>();
        messages.Add(new Message { Text = text });
        var encounter = new Encounter { MessageList = messages };
        return new EncounterInstance(encounter, Player, Enemy).CurrentDialogText;
    }

    [TestMethod]
    public void OrdinaryWordsKeepTheirPunctuation()
    {
        const string text = "Greetings, traveler. Are you lost?! Wait...  Leave now!";
        Assert.AreEqual(text, Shown(text), "the encounter text reads as written");
    }

    [TestMethod]
    public void KeywordsAreReplacedAndKeepTheirPunctuation()
    {
        string sing = Player.data.Traits.Singular;
        string plural = Player.data.Traits.Plural;

        Assert.AreEqual($"Hello, {sing}. You {plural}, pay 0.", Shown("Hello, SING. You PLURAL, pay MONEY."),
                        "keywords take the player's names and the demand, and keep the punctuation after them");
    }
}
