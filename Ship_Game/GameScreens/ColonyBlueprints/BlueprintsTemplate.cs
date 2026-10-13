using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using System.Collections.Generic;
using static Ship_Game.Planet;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

// Data used for saved Blueprints
[StarDataType]
public sealed class BlueprintsTemplate
{
    [StarData] public string Name;
    [StarData] public string ModName;
    [StarData] public bool Exclusive;
    [StarData] public string LinkTo;
    [StarData] public HashSet<string> PlannedBuildings;
    [StarData] public ColonyType ColonyType;
    [StarData] public bool SkipUnresearched;
    public static string CurrentModName =>  GlobalStats.HasMod ? GlobalStats.ModName : "BBplus";

    [StarDataConstructor] public BlueprintsTemplate() { }
    public BlueprintsTemplate(string name, bool exclusive, string linkTo, HashSet<string>plannedBuildings, ColonyType cType,
                              bool skipUnresearched = false)
    {
        Name = name;
        ModName = CurrentModName;
        Exclusive = exclusive;
        LinkTo = linkTo;
        PlannedBuildings = plannedBuildings;
        ColonyType = cType == ColonyType.TradeHub ? ColonyType.Colony : cType;
        SkipUnresearched = skipUnresearched;
    }

    public bool Validated => ResourceManager.BlueprintsValid(this, out _);

    public void RenameForSave(string name)
    {
        Name = name;
        if (!CanSafelyLinkFor(name))
            LinkTo = "";
    }

    public bool CanSafelyLinkFor(string requestingTemplateName)
    {
        var visited = new HashSet<string> { Name };
        for (string next = LinkTo; !string.IsNullOrEmpty(next);)
        {
            if (next == requestingTemplateName || !visited.Add(next))
                return false;

            if (!ResourceManager.TryGetBlueprints(next, out BlueprintsTemplate nextTemplate))
            {
                Log.Error($"Could not find template for {next} in Resource Manager");
                return true;
            }

            next = nextTemplate.LinkTo;
        }

        return true;
    }
}
