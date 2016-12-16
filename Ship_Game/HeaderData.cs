using System;
using System.IO;

namespace Ship_Game
{
	public sealed class HeaderData
	{
		public string SaveName;
		public string StarDate;
		public DateTime Time;
		public string PlayerName;
		public string RealDate;
		public string ModName = "";
        public string ModPath = "";
		public FileInfo FI;
        public int Version;

		public HeaderData()
		{
		}
	}
}