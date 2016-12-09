using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: class to store names for ships
    public sealed class ShipNames
    {
        public List<EmpireEntry> EmpireEntries = new List<EmpireEntry>();

        public struct EmpireEntry
        {
            public string ShipType;
            public List<RoleNames> NamesByRoles;
        };

        public struct RoleNames
        {
            public ShipData.RoleName Role;
            public List<string> Names;
        };

        // Refactored by RedFox
        // Check to see if there are names to use
        public bool CheckForName(string empire, ShipData.RoleName role)
        {
            foreach (EmpireEntry e in EmpireEntries)
            {
                if (empire != e.ShipType)
                    continue;
                if (e.NamesByRoles.Any(roleNames => roleNames.Names.Count != 0 && role == roleNames.Role))
                    return true;
            }
            return false;
        }

        // Grab random name from list to use
        public string GetName(string empire, ShipData.RoleName role)
        {
            foreach (EmpireEntry e in EmpireEntries)
            {
                if (empire != e.ShipType)
                    continue;
                foreach (RoleNames roleNames in e.NamesByRoles)
                    if (role == roleNames.Role)
                        return roleNames.Names[new Random().Next(0, roleNames.Names.Count)];
            }
            return string.Empty;
        }
    }
}
