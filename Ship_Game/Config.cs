using System;

namespace Ship_Game
{
	public sealed class Config
	{

        public int XRES;

		public int YRES;

		public int WindowMode;

		public bool RanOnce;

		public bool ForceFullSim;

		public int AASamples = 2;

		public float MusicVolume = 0.7f;

		public float EffectsVolume = 1f;

		public string Language = "German";

		public Config()
		{
		}
	}
}