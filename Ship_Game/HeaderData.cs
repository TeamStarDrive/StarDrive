using System;
using System.IO;

namespace Ship_Game
{
	public class HeaderData
	{
		public string SaveName;

		public string StarDate;

		public DateTime Time;

		public string PlayerName;

		public string RealDate;

		public string ModName = "";

		private FileInfo FI;

		public HeaderData()
		{
		}

		public FileInfo GetFileInfo()
		{
			return this.FI;
		}

		public void SetFileInfo(FileInfo fi)
		{
			this.FI = fi;
		}
	}
}