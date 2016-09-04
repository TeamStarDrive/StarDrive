using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public sealed class SaveFleetDesignScreen : GenericLoadSaveScreen, IDisposable
	{
        private Fleet f;

		public SaveFleetDesignScreen(Fleet f) : base(SLMode.Save, f.Name, "Save Fleet As...", "Saved Fleets", "Saved Fleet already exists.  Overwrite?", 40)
        {
			this.f = f;        // set save file data and starting name
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Fleet Designs/");
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
            catch { }
		}

		protected override void SetSavesSL()
		{
			FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(this.Path);
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				this.SavesSL.AddItem(new FileData(FI, FI as object, System.IO.Path.GetFileNameWithoutExtension(FI.Name))).AddItemWithCancel(FI);
			}
			FileInfo[] fileInfoArray = HelperFunctions.GetFilesFromDirectory("Content/FleetDesigns");
			for (int j = 0; j < (int)fileInfoArray.Length; j++)
			{
				FileInfo FI = fileInfoArray[j];
				this.SavesSL.AddItem(new FileData(FI, FI as object, System.IO.Path.GetFileNameWithoutExtension(FI.Name)));
			}
		}
	}
}