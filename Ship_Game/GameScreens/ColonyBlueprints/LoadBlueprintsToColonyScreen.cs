using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using System.IO;

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

        string title2 = blueprints.LinkTo.NotEmpty() ? $"Linked to: {blueprints.LinkTo}" : "";
        Color color = BlueprintsScreen.GetBlueprintsIconColor(blueprints.ColonyType);
        return new(null, blueprints, blueprints.Name, title1, title2, "", BlueprintsIcon, color) 
        { FileNameColor = color };
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        foreach (BlueprintsTemplate template in ResourceManager.GetAllBlueprints())
        {
            items.Add(CreateBlueprintsSaveItem(template));
        }

        AddItemsToSaveSL(items, addCancel: false);
    }
}
