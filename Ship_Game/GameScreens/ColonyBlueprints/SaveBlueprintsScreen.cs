using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;

namespace Ship_Game;

public sealed class SaveBlueprintsScreen : GenericLoadSaveScreen
{
    readonly BlueprintsTemplate Blueprints;
    BlueprintsScreen Screen;

    public SaveBlueprintsScreen(BlueprintsScreen parent, BlueprintsTemplate blueprints)
        : base(parent, SLMode.Save, blueprints.Name, "Save Blueprints As...", "Colony Blueprints", "Saved Blueprints exists.  Overwrite?", 40)
    {
        Blueprints = blueprints;
        Path = Dir.StarDriveAppData + "/Colony Blueprints/";
        Screen = parent;
        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    public override void DoSave()
    {
        try
        {
            string path = Path + EnterNameArea.Text + ".yaml";
            Blueprints.Name = EnterNameArea.Text;
            YamlSerializer.SerializeRoot(path, Blueprints);
            Screen.LoadBlueprintsTemplate(Blueprints);
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

    FileData CreateBlueprintsSaveItem(FileInfo info, BlueprintsTemplate blueprints)
    {
        return new(info, info, blueprints.Name, "", "", "", null, Color.White);
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        foreach (FileInfo info in Dir.GetFiles(Path, "yaml"))
        {
            var blueprints = YamlParser.Deserialize<BlueprintsTemplate>(info);
            items.Add(CreateBlueprintsSaveItem(info, blueprints));
        }

        AddItemsToSaveSL(items);
    }
}
