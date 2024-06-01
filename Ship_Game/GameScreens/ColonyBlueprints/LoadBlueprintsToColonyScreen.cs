using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;

namespace Ship_Game;

public sealed class LoadBlueprintsToColonyScreen : SaveLoadBlueprintsScreen
{
    readonly GovernorDetailsComponent GovernorTab;


    public LoadBlueprintsToColonyScreen(GameScreen caller, GovernorDetailsComponent governorTab, string planetName) 
        : base(caller, planetName)
    {
        GovernorTab = governorTab;
    }

    protected override void Load()
    {
        if (SelectedFile != null)
            GovernorTab.OnBlueprintsChanged((BlueprintsTemplate)SelectedFile.Data);

        ExitScreen();
    }

    // Cannot save in this screen.
    public override void DoSave(){ }

    FileData CreateBlueprintsSaveItem(BlueprintsTemplate blueprints)
    {
        string title1;
        title1 = blueprints.Exclusive ? Localizer.Token(GameText.ExclusiveBlueprints) : "";
        if (blueprints.ColonyType != Planet.ColonyType.Colony)
            title1 = title1.NotEmpty() ? $"{title1} | Switch to: {blueprints.ColonyType}"
                                        : $"Switch to: {blueprints.ColonyType}";

        string title2 = blueprints.Validated && blueprints.LinkTo.NotEmpty() ? $"Linked to: {blueprints.LinkTo}" : "";
        return new(null, blueprints, blueprints.Name, title1, title2, "", BlueprintsIcon, GetBlueprintsIconColor(blueprints));
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        foreach (BlueprintsTemplate template in ResourceManager.GetAllBlueprints())
        {
            items.Add(CreateBlueprintsSaveItem(template));
        }

        AddItemsToSaveSL(items);
    }
}
