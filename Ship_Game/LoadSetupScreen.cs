using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ship_Game
{
    public sealed class LoadSetupScreen : GenericLoadSaveScreen
    {
        private readonly RaceDesignScreen Screen;

        public LoadSetupScreen(RaceDesignScreen screen) : base(SLMode.Load, "", "Load Saved Setup", "Saved Setups")
        {
            Screen = screen;
            Path = Dir.ApplicationData + "/StarDrive/Saved Setups/";
        }

        protected override void Load()
        {
            if (selectedFile != null)
            {
                SetupSave ss = (SetupSave)selectedFile.Data;
                GlobalStats.FTLInSystemModifier           = ss.FTLModifier;
                GlobalStats.EnemyFTLInSystemModifier      = ss.EnemyFTLModifier;
                GlobalStats.OptionIncreaseShipMaintenance = ss.OptionIncreaseShipMaintenance;
                GlobalStats.MinimumWarpRange              = ss.MinimumWarpRange;
                GlobalStats.MemoryLimiter                 = ss.MemoryLimiter;
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
                AudioManager.PlayCue("UI_Misc20");
            }
            ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            var saves = new List<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path))
            {
                try
                {
                    SetupSave data = fileInfo.Deserialize<SetupSave>();
                    if (string.IsNullOrEmpty(data.Name) || data.Version < 308)
                        continue;

                    if (GlobalStats.ActiveMod != null)
                    {
                        if (data.ModPath != GlobalStats.ActiveMod.ModPath)
                            continue;
                    }
                    else if (!string.IsNullOrEmpty(data.ModPath))
                        continue;

                    string info = data.Date;
                    string extraInfo = data.ModName != "" ? "Mod: "+data.ModName : "Default";
                    saves.Add(new FileData(fileInfo, data, data.Name, info, extraInfo));
                }
                catch
                {
                }
            }

            var sortedList = from data in saves orderby data.FileName ascending select data;
            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
        }
    }
}