using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;

namespace UnitTests.Data;

[TestClass]
public class RaceOrderTests
{
    static EmpireData Race(string archetype, string name, byte faction = 0)
        => new() { PortraitName = archetype, Faction = faction, Traits = new RacialTrait { Name = name } };

    [TestMethod]
    public void RacesAreListedHumansFirstThenAlphabeticallyWithFactionsLastInLoadOrder()
    {
        var races = new Array<IEmpireData>
        {
            Race("Vulfen", "Vulfar Technocracy"),
            Race("vulcan", "Vulcan Confederacy"),
            Race("Imperium", "Remnant", faction: 1),
            Race("Dauntless", "Dauntless Hegemony"),
            Race("Cordrazine", "Cordrazine Collective"),
            Race("Imperium", "Corsairs", faction: 1),
            Race("Chukk", "Chukk Affiliation"),
            Race("Human", "United Federation"),
        };

        ResourceManager.SortRaces(races);

        Assert.AreEqual("Human, Chukk, Cordrazine, Dauntless, vulcan, Vulfen, Remnant, Corsairs",
                        string.Join(", ", races.Select(r => r.IsFactionOrMinorRace ? r.Name : r.ArchetypeName)));
    }
}
