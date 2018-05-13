using System;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.AI;

namespace Ship_Game
{
    public sealed class SaveFleetDesignScreen : GenericLoadSaveScreen, IDisposable
    {
        private Fleet f;

        public SaveFleetDesignScreen(GameScreen parent, Fleet f) 
            : base(parent, SLMode.Save, f.Name, "Save Fleet As...", "Saved Fleets", "Saved Fleet already exists.  Overwrite?", 40)
        {
            this.f = f;        // set save file data and starting name
            this.Path = string.Concat(Dir.ApplicationData, "/StarDrive/Fleet Designs/");
        }

        public override void DoSave()
        {
            FleetDesign d = new FleetDesign()
            {
                Name = this.EnterNameArea.Text
            };
            foreach (FleetDataNode node in f.DataNodes)
            {
                d.Data.Add(node);
            }
            try
            {
                d.FleetIconIndex = f.FleetIconIndex;
                XmlSerializer Serializer = new XmlSerializer(typeof(FleetDesign));
                TextWriter WriteFileStream = new StreamWriter(string.Concat(this.Path, this.EnterNameArea.Text, ".xml"));
                Serializer.Serialize(WriteFileStream, d);
                WriteFileStream.Close();
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

        protected override void SetSavesSL()
        {
            foreach (FileInfo info in Dir.GetFiles(Path))
            {
                this.SavesSL.AddItem(new FileData(info, info, info.NameNoExt())).AddItemWithCancel(info);
            }
            foreach (FileInfo info in Dir.GetFiles("Content/FleetDesigns"))
            {
                this.SavesSL.AddItem(new FileData(info, info, info.NameNoExt()));
            }
        }
    }
}