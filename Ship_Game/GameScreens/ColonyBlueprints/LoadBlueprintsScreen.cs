using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;

namespace Ship_Game;

public sealed class LoadBlueprintsScreen : GenericLoadSaveScreen
{
    readonly BlueprintsScreen Screen;
    static SubTexture BlueprintsIcon = ResourceManager.Texture("NewUI/blueprints");

    public LoadBlueprintsScreen(BlueprintsScreen caller) : base(caller, SLMode.Load, "", "Load Blueprints", "Saved Blueprints", 40)
    {
        Screen = caller;
        Path = Dir.StarDriveAppData + "/Colony Blueprints/" + BlueprintsTemplate.CurrentModName + "/";
        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    protected override void Load()
    {
        if (SelectedFile?.FileLink != null && SelectedFile.Enabled)
        {
            var blueprints = YamlParser.DeserializeOne<BlueprintsTemplate>(SelectedFile?.FileLink);
            Screen.LoadBlueprintsTemplate(blueprints);
        }
        else
        {
            GameAudio.NegativeClick();
        }
        ExitScreen();
    }

    FileData CreateBlueprintsSaveItem(FileInfo info, BlueprintsTemplate blueprints)
    {
        return new(info, info, blueprints.Name, "", "", "", BlueprintsIcon, HelperFunctions.GetBlueprintsIconColor(blueprints)) 
        { Enabled = blueprints.Validated };
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
