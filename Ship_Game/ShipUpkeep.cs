using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: allows mods to set custom upkeep cost by role and race
    public class ShipUpkeep
    {
        //Default values for Roles
        public float freighterUpkeep = 0.2f;
        public float platformUpkeep = 0.2f;
        public float scoutUpkeep = 5.1f;
        public float fighterUpKeep = 0.2f;
        public float corvetteUpkeep = 0.35f;
        public float frigateUpkeep = 1.0f;
        public float cruiserUpkeep = 2.5f;
        public float carrierUpkeep = 4.0f;
        public float capitalUpkeep = 6.0f;
        public float stationUpkeep = 6.0f;

        public List<ShipUpkeep.UpkeepTable> RacialUpkeepTables = new List<ShipUpkeep.UpkeepTable>();

        public struct UpkeepTable
        {
            public List<string> ShipTypes;
            public float freighterUpkeep;
            public float platformUpkeep;
            public float scoutUpkeep;
            public float fighterUpKeep;
            public float corvetteUpkeep;
            public float frigateUpkeep;
            public float cruiserUpkeep;
            public float carrierUpkeep;
            public float capitalUpkeep;
            public float stationUpkeep;
        }

        public float GetMaintanence(string Role, string Shiptype)
        {
            Boolean raceFound = false;
            float maintanence=0f;
            int index=0;

            for (int i = 0; i < RacialUpkeepTables.Count(); i++)
            {
                for (int j = 0; j < RacialUpkeepTables.ElementAt(i).ShipTypes.Count(); j++)
                {
                    if (RacialUpkeepTables.ElementAt(i).ShipTypes.ElementAt(j) == Shiptype)
                    {
                        index = i;
                        raceFound = true;
                    }
                }
            }
            if (raceFound)
            {
                switch (Role)
                {
                    case "freighter":
                        maintanence = RacialUpkeepTables.ElementAt(index).freighterUpkeep;
                        break;
                    case "platform":
                        maintanence = RacialUpkeepTables.ElementAt(index).platformUpkeep;
                        break;
                    case "fighter":
                        maintanence = RacialUpkeepTables.ElementAt(index).fighterUpKeep;
                        break;
                    case "corvette":
                        maintanence = RacialUpkeepTables.ElementAt(index).corvetteUpkeep;
                        break;
                    case "scout":
                        maintanence = RacialUpkeepTables.ElementAt(index).scoutUpkeep;
                        break;
                    case "frigate":
                        maintanence = RacialUpkeepTables.ElementAt(index).frigateUpkeep;
                        break;
                    case "cruiser":
                        maintanence = RacialUpkeepTables.ElementAt(index).cruiserUpkeep;
                        break;
                    case "carrier":
                        maintanence = RacialUpkeepTables.ElementAt(index).carrierUpkeep;
                        break;
                    case "capital":
                        maintanence = RacialUpkeepTables.ElementAt(index).capitalUpkeep;
                        break;
                    case "station":
                        maintanence = RacialUpkeepTables.ElementAt(index).stationUpkeep;
                        break;
                    default:
                        maintanence = 0.0f;
                        break;
                }
            }
            else
            {
                switch (Role)
                {
                    case "freighter":
                        maintanence = freighterUpkeep;
                        break;
                    case "platform":
                        maintanence = platformUpkeep;
                        break;
                    case "fighter":
                        maintanence = fighterUpKeep;
                        break;
                    case "corvette":
                        maintanence = corvetteUpkeep;
                        break;
                    case "scout":
                        maintanence = scoutUpkeep;
                        break;
                    case "frigate":
                        maintanence = frigateUpkeep;
                        break;
                    case "cruiser":
                        maintanence = cruiserUpkeep;
                        break;
                    case "carrier":
                        maintanence = carrierUpkeep;
                        break;
                    case "capital":
                        maintanence = capitalUpkeep;
                        break;
                    case "station":
                        maintanence = stationUpkeep;
                        break;        
                    default:
                        maintanence = 0.0f;
                        break;
                }
            }
            return maintanence;
        }
    }
}
