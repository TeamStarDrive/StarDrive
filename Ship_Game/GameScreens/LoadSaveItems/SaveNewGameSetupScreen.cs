using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;
using Ship_Game.Universe;

namespace Ship_Game;

public sealed class SaveNewGameSetupScreen : GenericLoadSaveScreen
{
    readonly SetupSave SavedSetup;

    public SaveNewGameSetupScreen(RaceDesignScreen screen, UniverseParams settings) 
        : base(screen, SLMode.Save, "New Saved Setup", "Save Setup", "Saved Setups", 
            "Saved Setup already exists.  Overwrite?")
    {
        Path = Dir.StarDriveAppData + "/Saved Setups/";
        SavedSetup = new(settings);
    }

    public static void Save(string path, SetupSave save)
    {
        YamlSerializer.SerializeOne(path, save);
    }

    public static SetupSave Load(FileInfo fileInfo)
    {
        return YamlParser.DeserializeOne<SetupSave>(fileInfo);
    }

    public override void DoSave()
    {
        SavedSetup.Name = EnterNameArea.Text;
        Save(Path + SavedSetup.Name + ".yaml", SavedSetup);
        ExitScreen();
    }

    protected override void InitSaveList()        // Set list of files to show
    {
        var saves = new Array<FileData>();
        foreach (FileInfo file in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                SetupSave data = Load(file);
                if (data.Name.IsEmpty())
                {
                    Log.Warning($"Invalid NewGameSetup: {file.FullName}");
                    continue;
                }

                string info = data.Date;
                string extraInfo = data.ModName.NotEmpty() ? $"Mod: {data.ModName}" : "Vanilla";
                saves.Add(new(file, data, data.Name, info, extraInfo, null, Color.White)
                {
                    Enabled = GlobalStats.IsValidForCurrentMod(data.ModName)
                });
            }
            catch
            {
            }
        }
        
        saves.Sort(data => data.FileName);
        AddItemsToSaveSL(saves);
    }
}