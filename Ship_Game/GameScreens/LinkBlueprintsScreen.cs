using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;

namespace Ship_Game;

public sealed class LinkBlueprintsScreen : SaveLoadBlueprintsScreen
{
    readonly BlueprintsScreen BlueprintsScreen;
    string BlueprintsNameRequsetingLink;

    public LinkBlueprintsScreen(BlueprintsScreen parent, string blueprintsName)
        : base(blueprintsName, parent)
    {
        BlueprintsScreen = parent;
        BlueprintsNameRequsetingLink = blueprintsName;
    }

    protected override void Load()
    {
        if (SelectedFile != null && SelectedFile.Enabled)
        {
            var template = (BlueprintsTemplate)SelectedFile.Data;
            BlueprintsScreen.OnBlueprintsLinked(template);
            ExitScreen();
        }
        else
        {
            GameAudio.NegativeClick();
        }
    }

    // Cannot save in this screen.
    public override void DoSave() { }

    FileData CreateBlueprintsSaveItem(BlueprintsTemplate blueprints)
    {
        string title1 = "", title2;
        Color infoColor = Color.White;
        if (blueprints.CanSafelyLinkFor(BlueprintsNameRequsetingLink))
        {
            title1 = blueprints.Exclusive ? Localizer.Token(GameText.ExclusiveBlueprints) : "";
            if (blueprints.ColonyType != Planet.ColonyType.Colony)
                title1 = title1.NotEmpty() ? $"{title1} | Switch to: {blueprints.ColonyType}"
                                            : $"Switch to: {blueprints.ColonyType}";

            title2 = blueprints.LinkTo.NotEmpty() ? $"Linked to: {blueprints.LinkTo}" : "";
        }
        else
        {
            title2 = $"Cannot link these Blueprints as their link chain leads to {BlueprintsNameRequsetingLink}";
            infoColor = Color.Pink;
        }

        return new(null, blueprints, blueprints.Name, title1, title2, "", BlueprintsIcon, GetBlueprintsIconColor(blueprints)) 
        { Enabled = infoColor == Color.White, InfoColor = infoColor };
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        foreach (BlueprintsTemplate template in ResourceManager.GetAllBlueprints())
        {
            if (template.Name != BlueprintsNameRequsetingLink)
                items.Add(CreateBlueprintsSaveItem(template));
        }

        AddItemsToSaveSL(items, addCancel: false);
    }
}
