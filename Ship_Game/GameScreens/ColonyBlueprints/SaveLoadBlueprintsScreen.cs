using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;

namespace Ship_Game;

public class SaveLoadBlueprintsScreen : GenericLoadSaveScreen
{
    readonly BlueprintsScreen BlueprintsScreen;
    public static SubTexture BlueprintsIcon = ResourceManager.Texture("NewUI/blueprints");
    readonly BlueprintsTemplate BlueprintsToSave;
    const int ListItemHeight = 60;

    public SaveLoadBlueprintsScreen(BlueprintsScreen parent, BlueprintsTemplate blueprintsToSave) 
        : base(parent, SLMode.Save, blueprintsToSave.Name, "Save Blueprints As...", "Colony Blueprints", "Saved Blueprints exists. " +
            "If you choose to overwrite, planets with these Blueprints will be reloaded with the new version. Overwrite?", ListItemHeight)
    {
        BlueprintsScreen = parent;
        BlueprintsToSave = blueprintsToSave;
        InitPath();
    }

    public SaveLoadBlueprintsScreen(BlueprintsScreen parent) : base(parent, SLMode.Load, "", "Load Blueprints", "Saved Blueprints", ListItemHeight)
    {
        BlueprintsScreen = parent;
        InitPath();
    }

    // Load blueprints to a colony from the colonyscreen.
    public SaveLoadBlueprintsScreen(GameScreen parent, string planetName)
        : base(parent, SLMode.Load, "", $"Load Blueprints To {planetName}", "Saved Blueprints", ListItemHeight)
    {
        InitPath();
    }

    // Link blueprints to other Blueprints.
    public SaveLoadBlueprintsScreen(string blueprintsName, BlueprintsScreen parent)
        : base(parent, SLMode.Load, "", $"Link Blueprints To {blueprintsName}", "Saved Blueprints", ListItemHeight)
    {
        BlueprintsScreen = parent;
        InitPath();
    }

    void InitPath()
    {
        Path = Dir.StarDriveAppData + "/Colony Blueprints/" + BlueprintsTemplate.CurrentModName + "/";
        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    public override void DoSave()
    {
        if (BlueprintsScreen == null)
        {
            Log.Error("Cannot save Blueprints if BlueprintsScreen is null");
            ExitScreen();
            return;
        }

        try
        {
            string name = EnterNameArea.Text;
            string path = Path + name + ".yaml";
            BlueprintsToSave.Name = name;
            if (BlueprintsToSave.LinkTo == name)
                BlueprintsToSave.LinkTo = ""; // avoid cyclic link for new blueprints

            YamlSerializer.SerializeOne(path, BlueprintsToSave);
            ResourceManager.AddBlueprintsTemplate(BlueprintsToSave);
            BlueprintsScreen.AfterBluprintsSave(BlueprintsToSave);
        }
        catch (Exception e)
        {
            Log.Error(e, "Save Blueprints Failed");
        }
        finally
        {
            ExitScreen();
        }
    }

    protected override void Load()
    {
        if (SelectedFile?.FileLink != null && SelectedFile.Enabled)
        {
            var blueprints = YamlParser.DeserializeOne<BlueprintsTemplate>(SelectedFile?.FileLink);
            BlueprintsScreen.LoadBlueprintsTemplate(blueprints);
            ExitScreen();
        }
        else
        {
            GameAudio.NegativeClick();
        }
    }

    protected override bool DeleteFile(FileData toDelete)
    {

        if (!base.DeleteFile(toDelete))
            return false;

        var deleteBluprints = (BlueprintsTemplate)toDelete.Data;
        string deletedName = deleteBluprints.Name;
        BlueprintsScreen.AfterBluprintsDelete(deleteBluprints);

        string modName = BlueprintsTemplate.CurrentModName;
        foreach (FileInfo info in Dir.GetFiles(Path, "yaml"))
        {
            var blueprints = YamlParser.DeserializeOne<BlueprintsTemplate>(info);
            if (modName == blueprints.ModName && blueprints.LinkTo == deletedName)
            {
                blueprints.LinkTo = "";
                string path = Path + blueprints.Name + ".yaml";
                YamlSerializer.SerializeOne(path, blueprints);
                ResourceManager.AddBlueprintsTemplate(blueprints);
                BlueprintsScreen.RemoveAllBlueprintsLinkTo(blueprints);
            }
        }

        SavesSL.Reset();
        InitSaveList();
        return true;
    }

    FileData CreateBlueprintsSaveItem(FileInfo info, BlueprintsTemplate blueprints)
    {
        string title1;
        Color infoColor = Color.White;
        if (blueprints.Validated)
        {
            title1 = blueprints.Exclusive ? Localizer.Token(GameText.ExclusiveBlueprints) : "";
            if (blueprints.ColonyType != Planet.ColonyType.Colony)
                title1 = title1.NotEmpty() ? $"{title1} | Switch to: {blueprints.ColonyType}"
                                           : $"Switch to: {blueprints.ColonyType}";
        }
        else
        {
            title1 = "These Blueprints have some missing buildings and cannot be loaded.";
            infoColor = Color.Red;
        }

        string title2 = blueprints.Validated && blueprints.LinkTo.NotEmpty() ? $"Linked to: {blueprints.LinkTo}" : "";
        Color color = BlueprintsScreen.GetBlueprintsIconColor(blueprints);
        return new(info, blueprints, blueprints.Name, title1, title2, "", BlueprintsIcon, color)
        { Enabled = blueprints.Validated, InfoColor = infoColor, FileNameColor = color };
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        string modName = BlueprintsTemplate.CurrentModName;
        foreach (FileInfo info in Dir.GetFiles(Path, "yaml"))
        {
            var blueprints = YamlParser.DeserializeOne<BlueprintsTemplate>(info);
            if (modName == blueprints.ModName)
                items.Add(CreateBlueprintsSaveItem(info, blueprints));
        }

        AddItemsToSaveSL(items);
    }
}
