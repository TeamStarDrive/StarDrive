using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Ship_Game
{
	public sealed class TechEntry
	{
        [Serialize(0)] public string UID;
        [Serialize(1)] public float Progress;
        [Serialize(2)] public bool Discovered;
        [Serialize(3)] public bool Unlocked;
        [Serialize(4)] public int  Level;
        [Serialize(5)] public string AcquiredFrom = "";
        [Serialize(6)] public bool shipDesignsCanuseThis = true;
        [Serialize(7)] public float maxOffensiveValueFromthis = 0;

        [XmlIgnore][JsonIgnore]
        public float TechCost => Tech.Cost * (float)Math.Max(1, Math.Pow(2.0, Level));

	    [XmlIgnore][JsonIgnore]
        public Technology Tech => ResourceManager.TechTree[UID];
	}
}