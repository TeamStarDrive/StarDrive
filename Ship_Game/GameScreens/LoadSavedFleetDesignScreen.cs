using System;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class LoadSavedFleetDesignScreen : GenericLoadSaveScreen, IDisposable
    {
        private FleetDesignScreen parentScreen;


        public LoadSavedFleetDesignScreen(GameScreen parent) : base(parent, SLMode.Load, "", "Load Saved Fleet", "Saved Fleets", 40)
        {
            Path = Dir.StarDriveAppData + "/Fleet Designs/";
        }

        public LoadSavedFleetDesignScreen(FleetDesignScreen caller) : base(caller, SLMode.Load, "", "Load Saved Fleet", "Saved Fleets", 40)
        {
            parentScreen = caller;
            Path = Dir.StarDriveAppData + "/Fleet Designs/";
        }

        protected override void Load()
        {
            if (selectedFile != null)
            {
                XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
                FleetDesign data = (FleetDesign)serializer1.Deserialize(selectedFile.FileLink.OpenRead());
                parentScreen.LoadData(data);
            }
            else
            {
                GameAudio.PlaySfxAsync("UI_Misc20");
            }
            ExitScreen();
        }

        protected override void InitSaveList()
        {
            var serializer = new XmlSerializer(typeof(FleetDesign));

            foreach (FileInfo info in ResourceManager.GatherFilesModOrVanilla("FleetDesigns", "xml"))
            {
                bool ok = true;
                foreach (FleetDataNode node in serializer.Deserialize<FleetDesign>(info).Data)
                {
                    if (EmpireManager.Player.WeCanBuildThis(node.ShipName))
                        continue;
                    ok = false;
                    break;
                }
                if (ok)
                {
                    SavesSL.AddItem(new FileData(info, info, info.NameNoExt()));
                }
            }

            foreach (FileInfo info in Dir.GetFiles(Path)) // player made fleets, can be deleted
            {
                bool ok = true;
                foreach (FleetDataNode node in serializer.Deserialize<FleetDesign>(info).Data)
                {
                    if (EmpireManager.Player.WeCanBuildThis(node.ShipName))
                        continue;
                    ok = false;
                    break;
                }
                if (ok)
                {
                    SavesSL.AddItem(new FileData(info, info, info.NameNoExt())).AddSubItem(info);
                }
            }
        }
    }
}