using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class LoadNewGameSetupScreen : GenericLoadSaveScreen
    {
        private readonly RaceDesignScreen Screen;

        public LoadNewGameSetupScreen(RaceDesignScreen screen) : base(screen, SLMode.Load, "", "Load Saved Setup", "Saved Setups")
        {
            Screen = screen;
            Path = Dir.StarDriveAppData + "/Saved Setups/";
        }

        protected override void Load()
        {
            if (SelectedFile != null)
            {
                SetupSave ss = (SetupSave)SelectedFile.Data;
                GlobalStats.FTLInSystemModifier           = ss.FTLModifier;
                GlobalStats.EnemyFTLInSystemModifier      = ss.EnemyFTLModifier;
                GlobalStats.ShipMaintenanceMulti          = ss.OptionIncreaseShipMaintenance;
                GlobalStats.MinimumWarpRange              = ss.MinimumWarpRange;
                GlobalStats.TurnTimer                     = ss.TurnTimer;
                GlobalStats.PreventFederations            = ss.preventFederations;
                GlobalStats.GravityWellRange              = ss.GravityWellRange;
                GlobalStats.ExtraPlanets                  = ss.ExtraPlanets;
                GlobalStats.StartingPlanetRichness        = ss.StartingPlanetRichness;
                GlobalStats.PlanetaryGravityWells         = ss.PlanetaryGravityWells;
                GlobalStats.WarpInSystem                  = ss.WarpInSystem;
                Screen.SetCustomSetup(ss.GameDifficulty, ss.StarEnum, ss.Galaxysize, ss.Pacing, ss.ExtraRemnant, ss.numOpponents, ss.mode);
            }
            else
            {
                GameAudio.NegativeClick();
            }
            ExitScreen();
        }

        protected override void InitSaveList() // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path))
            {
                try
                {
                    var save = fileInfo.Deserialize<SetupSave>();
                    if (string.IsNullOrEmpty(save.Name) || save.Version < 308)
                        continue;

                    if (GlobalStats.HasMod)
                    {
                        if (save.ModPath != GlobalStats.ActiveMod.ModName)
                            continue;
                    }
                    else if (!string.IsNullOrEmpty(save.ModPath))
                        continue;

                    string info = save.Date;
                    string extraInfo = save.ModName != "" ? "Mod: "+save.ModName : "Default";
                    saves.Add(new FileData(fileInfo, save, save.Name, info, extraInfo, null, Color.White));
                }
                catch
                {
                }
            }

            AddItemsToSaveSL(saves.OrderBy(data => data.FileName));
        }
    }
}