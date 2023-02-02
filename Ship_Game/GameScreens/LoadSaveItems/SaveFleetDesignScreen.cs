using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;
using Ship_Game.Fleets;

namespace Ship_Game;

public sealed class SaveFleetDesignScreen : GenericLoadSaveScreen
{
    readonly Fleet Fleet;

    public SaveFleetDesignScreen(GameScreen parent, Fleet fleet) 
        : base(parent, SLMode.Save, fleet.Name, "Save Fleet As...", "Saved Fleets", "Saved Fleet already exists.  Overwrite?", 40)
    {
        Fleet = fleet; // set save file data and starting name
        Path = Dir.StarDriveAppData + "/Fleet Designs/";
    }

    public override void DoSave()
    {
        var d = new FleetDesign
        {
            Name = EnterNameArea.Text
        };
        foreach (FleetDataNode node in Fleet.DataNodes)
        {
            d.Nodes.Add(node.GetDesignOnly());
        }
        try
        {
            d.FleetIconIndex = Fleet.FleetIconIndex;
            string path = Path + EnterNameArea.Text + ".yaml";
            YamlSerializer.SerializeRoot(path, d);
        }
        catch(Exception e)
        {
            Log.Error(e, "Save Fleet Failed");
        }
        finally
        {
            ExitScreen();
        }
    }

    FileData CreateFleetDesignSaveItem(FileInfo info, FleetDesign design)
    {
        return new(info, info, info.NameNoExt(), "", "", "", design.Icon, Color.White);
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        foreach (FileInfo info in Dir.GetFiles(Path, "yaml"))
        {
            var design = YamlParser.Deserialize<FleetDesign>(info);
            items.Add(CreateFleetDesignSaveItem(info, design));
        }
        foreach (FileInfo info in Dir.GetFiles("Content/FleetDesigns", "yaml"))
        {
            var design = YamlParser.Deserialize<FleetDesign>(info);
            items.Add(CreateFleetDesignSaveItem(info, design));
        }
        AddItemsToSaveSL(items);
    }
}
