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

    public override void DoSave()
    {
        SavedSetup.Name = EnterNameArea.Text;
        YamlSerializer.SerializeRoot(Path + SavedSetup.Name + ".yaml", SavedSetup);
        ExitScreen();
    }

    protected override void InitSaveList()        // Set list of files to show
    {
        var saves = new Array<FileData>();
        foreach (FileInfo file in Dir.GetFiles(Path, "yaml"))
        {
            try
            {
                SetupSave data = YamlParser.DeserializeOne<SetupSave>(file);
                if (data.Name.IsEmpty())
                {
                    data.Name = file.NameNoExt();
                    data.Version = 0;
                }

                string info;
                string extraInfo;
                if (data.Version < 308)     // Version checking
                {
                    info = "Invalid Setup File";
                    extraInfo = "";
                }
                else
                {
                    info = data.Date;
                    extraInfo = (data.ModName != "" ? "Mod: " + data.ModName : "Default");
                }
                saves.Add(new FileData(file, data, data.Name, info, extraInfo, null, Color.White));
            }
            catch
            {
            }
        }
        
        saves.Sort(data => data.FileName);
        AddItemsToSaveSL(saves);
    }
}