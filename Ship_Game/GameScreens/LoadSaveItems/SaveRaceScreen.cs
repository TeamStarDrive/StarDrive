using System;
using System.IO;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;

namespace Ship_Game;

public sealed class SaveRaceScreen : GenericLoadSaveScreen
{
    readonly RaceDesignScreen Screen;
    readonly RaceSave RaceSave;

    public SaveRaceScreen(RaceDesignScreen screen, RacialTrait data) 
        : base(screen, SLMode.Save, data.Name, "Save Race", "Saved Races", "Saved Race already exists.  Overwrite?")
    {
        Screen = screen;
        Path = Dir.StarDriveAppData + "/Saved Races/";
        RaceSave = new(data);
    }

    public static void Save(string path, RaceSave save)
    {
        YamlSerializer.SerializeOne(path, save);
    }

    public static RaceSave Load(FileInfo fileInfo)
    {
        return YamlParser.DeserializeOne<RaceSave>(fileInfo);
    }

    public override void DoSave()
    {
        RaceSave.Name = EnterNameArea.Text;
        Save(Path + EnterNameArea.Text + ".yaml", RaceSave);
        ExitScreen();
    }

    protected override void InitSaveList()        // Set list of files to show
    {
        var saves = new Array<FileData>();
        foreach (FileInfo file in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                RaceSave data = Load(file);
                if (data.Name.IsEmpty())
                {
                    Log.Warning($"Invalid RaceSave: {file.FullName}");
                    continue;
                }

                // show all race saves with they mod name
                string info = "Race Name: " + data.Traits.Name;
                string extraInfo = data.ModName.NotEmpty() ? $"Mod: {data.ModName}" : "Vanilla";
                string tooltip = file.Name;
                saves.Add(new(file, data, data.Name, info, extraInfo, tooltip, data.Traits.FlagIcon, data.Traits.Color)
                {
                    Enabled = GlobalStats.IsValidForCurrentMod(data.ModName)
                });
            }
            catch
            {
            }
        }

        AddItemsToSaveSL(saves.OrderBy(data => data.FileName));
    }
}
