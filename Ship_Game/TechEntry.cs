using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MsgPack.Serialization;

namespace Ship_Game
{
	public sealed class TechEntry
	{
        [MessagePackMember(0)] public string UID;
        [MessagePackMember(1)] public float Progress;
        [MessagePackMember(2)] public bool Discovered;
        [MessagePackMember(3)] public bool Unlocked;
        [MessagePackMember(4)] public int  Level;
        [MessagePackMember(5)] public string AcquiredFrom = "";
        [MessagePackMember(6)] public bool shipDesignsCanuseThis = true;
        [MessagePackMember(7)] public float maxOffensiveValueFromthis = 0;

        [XmlIgnore][MessagePackIgnore]
        public float TechCost => Tech.Cost * (float)Math.Max(1, Math.Pow(2.0, Level));

	    [XmlIgnore][MessagePackIgnore]
        public Technology Tech => ResourceManager.TechTree[UID];
	}
}