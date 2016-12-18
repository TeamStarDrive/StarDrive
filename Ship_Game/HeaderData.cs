using System;
using System.IO;
using System.Xml.Serialization;
using MsgPack.Serialization;

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
        public int Version;

        [XmlIgnore][MessagePackIgnore]
        public FileInfo FI;

        public HeaderData()
		{
		}
	}
}