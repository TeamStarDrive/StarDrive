using System;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class LoadSavedFleetDesignScreen : GenericLoadSaveScreen, IDisposable
    {
        private FleetDesignScreen parentScreen;


        public LoadSavedFleetDesignScreen(FleetDesignScreen caller) : base(caller, SLMode.Load, "", "Load Saved Fleet", "Saved Fleets", 40)
        {
            parentScreen = caller;
            Path = Dir.StarDriveAppData + "/Fleet Designs/";
        }

        protected override void Load()
        {
            if (SelectedFile != null)
            {
                XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
                FleetDesign data = (FleetDesign)serializer1.Deserialize(SelectedFile.FileLink.OpenRead());
                parentScreen.LoadData(data);
            }
            else
            {
                GameAudio.NegativeClick();
            }
            ExitScreen();
        }

        static bool PlayerCanBuildFleet(FleetDesign fleetDesign)
        {
            foreach (FleetDataNode node in fleetDesign.Data)
            {
                if (!EmpireManager.Player.WeCanBuildThis(node.ShipName))
                {
                    return false;
                }
            }
            return true;
        }

        protected override void InitSaveList()
        {
            var serializer = new XmlSerializer(typeof(FleetDesign));

            FileInfo[] coreDesigns = ResourceManager.GatherFilesModOrVanilla("FleetDesigns", "xml");
            foreach (FileInfo info in coreDesigns)
            {
                var design = serializer.Deserialize<FleetDesign>(info);
                if (PlayerCanBuildFleet(design))
                {
                    AddItemToSaveSL(info, design.Icon);
                }
                else
                {
                    Log.Info($"Player cannot build fleet {design.Name}");
                }
            }

            FileInfo[] playerDesigns = Dir.GetFiles(Path);
            foreach (FileInfo info in playerDesigns) // player made fleets, can be deleted
            {
                var design = serializer.Deserialize<FleetDesign>(info);
                if (PlayerCanBuildFleet(design))
                {
                    AddItemToSaveSL(info, design.Icon);
                }
                else
                {
                    Log.Info($"Player cannot build fleet {design.Name}");
                }
            }
        }
    }
}