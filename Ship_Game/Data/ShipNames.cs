using System;
using Ship_Game.Ships;

namespace Ship_Game
{
    //Added by McShooterz: class to store names for ships
    public sealed class ShipNames
    {
        // @note This is auto-serialized
        public Array<EmpireEntry> EmpireEntries = new Array<EmpireEntry>();

        public struct EmpireEntry
        {
            public string ShipType;
            public Array<RoleNames> NamesByRoles;
        }

        public struct RoleNames
        {
            public ShipData.RoleName Role;
            public Array<string> Names;
        }

        public void Clear()
        {
            EmpireEntries.Clear();
        }

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
            if (EmpireEntries.IsEmpty)
                return string.Empty;
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
