using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    public class ShipRole
    {
        public string Name;

        public int Localization;

        public float Upkeep;

        public float KillExp;

        public float KillExpPerLevel;

        public float ExpPerLevel;

        public bool Protected;

        public List<Race> RaceList;

        public struct Race
        {
            public string ShipType;
            public int Localization;
            public float Upkeep;
            public float KillExp;
            public float KillExpPerLevel;
            public float ExpPerLevel;
        };

        public ShipRole()
        {
            RaceList = new List<Race>();
        }
    }
}
