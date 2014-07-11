using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: class to store names for ships
    public class ShipNames
    {

        public List<EmpireEntry> EmpireEntries = new List<EmpireEntry>();

        public struct EmpireEntry
        {
            public string ShipType;
            public List<RoleNames> NamesByRoles;
        };

        public struct RoleNames
        {
            public string Role;
            public List<string> Names;
        };

        //Check to see if there are names to use
        public bool CheckForName(string Empire, string Role)
        {
            for (int i = 0; i < EmpireEntries.Count; i++)
            {
                if (Empire == EmpireEntries[i].ShipType)
                {
                    for (int j = 0; j < EmpireEntries[i].NamesByRoles.Count; j++)
                    {
                        if (Role == EmpireEntries[i].NamesByRoles[j].Role && EmpireEntries[i].NamesByRoles[j].Names.Count != 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //Grab random name from list to use
        public string GetName(string Empire, string Role)
        {
            for (int i = 0; i < EmpireEntries.Count; i++)
            {
                if (Empire == EmpireEntries[i].ShipType)
                {
                    for (int j = 0; j < EmpireEntries[i].NamesByRoles.Count; j++)
                    {
                        if (Role == EmpireEntries[i].NamesByRoles[j].Role)
                        {
                            Random r = new Random();
                            int randint = r.Next(0, EmpireEntries[i].NamesByRoles[j].Names.Count);
                            return EmpireEntries[i].NamesByRoles[j].Names[randint];
                        }
                    }
                }
            }
            return "failed to name";
        }
    }
}
