using System;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.AI;

namespace Ship_Game
{
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
                d.Data.Add(node);
            }
            try
            {
                d.FleetIconIndex = Fleet.FleetIconIndex;
                var serializer = new XmlSerializer(typeof(FleetDesign));
                using (var stream = new StreamWriter(string.Concat(Path, EnterNameArea.Text, ".xml")))
                    serializer.Serialize(stream, d);
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

        protected override void InitSaveList()
        {
            var serializer = new XmlSerializer(typeof(FleetDesign));
            foreach (FileInfo info in Dir.GetFiles(Path))
            {
                var design = serializer.Deserialize<FleetDesign>(info);
                AddItemToSaveSL(info, design.Icon);
            }
            foreach (FileInfo info in Dir.GetFiles("Content/FleetDesigns"))
            {
                var design = serializer.Deserialize<FleetDesign>(info);
                AddItemToSaveSL(info, design.Icon);
            }
        }
    }
}