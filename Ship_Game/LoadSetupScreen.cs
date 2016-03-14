using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class LoadSetupScreen : GenericLoadSaveScreen, IDisposable
    {
        private RaceDesignScreen screen;

        private SetupSave SS;

        public LoadSetupScreen(RaceDesignScreen screen) : base(SLMode.Load, "", "Load Saved Setup", "")
        {
            this.screen = screen;
        }

        protected override FileHeader GetFileHeader(ScrollList.Entry e)
        {
            FileHeader fh = new FileHeader();
            SetupSave data = e.item as SetupSave;

            fh.FileName = data.Name;
            fh.Info = data.Date;
            fh.ExtraInfo = (data.ModName != "" ? String.Concat("Mod: ", data.ModName) : "Default");
            fh.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];

            return fh;
        }

        protected override void Load()
        {
            if (this.SS != null)
            {
                GlobalStats.FTLInSystemModifier = this.SS.FTLModifier;
                GlobalStats.EnemyFTLInSystemModifier = this.SS.EnemyFTLModifier;
                GlobalStats.OptionIncreaseShipMaintenance = this.SS.OptionIncreaseShipMaintenance;
                GlobalStats.MinimumWarpRange = this.SS.MinimumWarpRange;
                GlobalStats.MemoryLimiter = this.SS.MemoryLimiter;
                GlobalStats.TurnTimer = this.SS.TurnTimer;
                GlobalStats.preventFederations = this.SS.preventFederations;
                GlobalStats.GravityWellRange = this.SS.GravityWellRange;
                GlobalStats.ExtraPlanets = this.SS.ExtraPlanets;
                GlobalStats.StartingPlanetRichness = this.SS.StartingPlanetRichness;
                GlobalStats.PlanetaryGravityWells = this.SS.PlanetaryGravityWells;
                GlobalStats.WarpInSystem = this.SS.WarpInSystem;
                this.screen.SetCustomSetup(this.SS.GameDifficulty, this.SS.StarEnum, this.SS.Galaxysize, this.SS.Pacing, this.SS.ExtraRemnant, this.SS.numOpponents, this.SS.mode);
            }
            else
            {
                AudioManager.PlayCue("UI_Misc20");
            }
            this.ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<SetupSave> saves = new List<SetupSave>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Setups/"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    //EmpireData data = (EmpireData)ResourceManager.HeadeSSerializer.Deserialize(file);
                    XmlSerializer serializer1 = new XmlSerializer(typeof(SetupSave));
                    SetupSave data = (SetupSave)serializer1.Deserialize(file);
                    if (!string.IsNullOrEmpty(data.Name) || data.Version < 308)
                    {
                        //file.Close();
                        file.Dispose();
                        continue;
                    }

                    if (GlobalStats.ActiveMod != null)
                    {
                        if (data.ModPath != GlobalStats.ActiveMod.ModPath)
                        {
                            //file.Close();
                            file.Dispose();
                            continue;
                        }
                    }
                    else if (!string.IsNullOrEmpty(data.ModPath))
                    {
                        //file.Close();
                        file.Dispose();
                        continue;
                    }
                    saves.Add(data);
                    //file.Close();
                    file.Dispose();
                }
                catch
                {
                    //file.Close();
                    file.Dispose();
                }
                //Label0:
                //  continue;
            }
            IOrderedEnumerable<SetupSave> sortedList =
                from data in saves
                orderby data.Name descending
                select data;
            foreach (SetupSave data in sortedList)
            {
                this.SavesSL.AddItem(data);
            }
        }

        protected override void SwitchFile(ScrollList.Entry e)
        {
            this.SS = (e.item as SetupSave);
            AudioManager.PlayCue("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as SetupSave).Name;
        }
    }
}