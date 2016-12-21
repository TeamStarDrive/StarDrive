using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public sealed class LoadSavedFleetDesignScreen : GenericLoadSaveScreen, IDisposable
	{
		private FleetDesignScreen parentScreen;


		public LoadSavedFleetDesignScreen() : base(SLMode.Load, "", "Load Saved Fleet", "Saved Fleets", 40)
        {
            this.Path = string.Concat(Dir.ApplicationData, "/StarDrive/Fleet Designs/");
        }

        public LoadSavedFleetDesignScreen(FleetDesignScreen caller) : base(SLMode.Load, "", "Load Saved Fleet", "Saved Fleets", 40)
        {
			this.parentScreen = caller;
            this.Path = string.Concat(Dir.ApplicationData, "/StarDrive/Fleet Designs/");
        }

        protected override void Load()
        {
            if (this.selectedFile != null)
            {
                XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
                FleetDesign data = (FleetDesign)serializer1.Deserialize(this.selectedFile.FileLink.OpenRead());
                this.parentScreen.LoadData(data);
            }
            else
            {
                AudioManager.PlayCue("UI_Misc20");
            }
            this.ExitScreen();
        }

        protected override void SetSavesSL()
		{
            FileInfo[] filesFromDirectory;

            if (GlobalStats.ActiveMod != null && Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/FleetDesigns")))
            {
                filesFromDirectory = Dir.GetFiles(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/FleetDesigns"));
            }
            else
            {
                filesFromDirectory = Dir.GetFiles("Content/FleetDesigns");
            }

			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				bool OK = true;
				XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
				foreach (FleetDataNode node in ((FleetDesign)serializer1.Deserialize(FI.OpenRead())).Data)
				{
					if (EmpireManager.GetEmpireByName(this.parentScreen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(node.ShipName))
					{
						continue;
					}
					OK = false;
					break;
				}
				if (OK)
				{
					this.SavesSL.AddItem(new FileData(FI, FI as object, System.IO.Path.GetFileNameWithoutExtension(FI.Name)));
				}
			}

			FileInfo[] fileInfoArray = Dir.GetFiles(this.Path);            // player made fleets, can be deleted
			for (int j = 0; j < (int)fileInfoArray.Length; j++)
			{
				FileInfo FI = fileInfoArray[j];
				bool OK = true;
				XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
				foreach (FleetDataNode node in ((FleetDesign)serializer1.Deserialize(FI.OpenRead())).Data)
				{
					if (EmpireManager.GetEmpireByName(this.parentScreen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(node.ShipName))
					{
						continue;
					}
					OK = false;
					break;
				}
				if (OK)
				{
                    this.SavesSL.AddItem(new FileData(FI, FI as object, System.IO.Path.GetFileNameWithoutExtension(FI.Name))).AddItemWithCancel(FI);
                }
			}
		}
	}
}