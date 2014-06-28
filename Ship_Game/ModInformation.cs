using System;

namespace Ship_Game
{
	public class ModInformation
	{
		public string ModName = "";

		public string CustomMenuMusic = "";

		public string ModDescription = "";

		public bool DisableDefaultRaces;

		public string PortraitPath = "";

		public string ModImagePath_1920x1280 = "";

        //added by McShooterz
        public bool useRacialTech;
        public bool useAlternateTech;
        public bool useDestroyers;
        public bool useDrones;
        public bool enableECM;
        public bool extraFireArcs;


		public ModInformation()
		{
		}
	}
}