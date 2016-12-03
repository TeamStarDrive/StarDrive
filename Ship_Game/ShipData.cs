using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Ship_Game
{
    public sealed class ShipData
    {
        public bool Animated;
        public string ShipStyle;
        public string EventOnDeath;
        public byte experience;
        public byte Level;
        public string SelectionGraphic = "";
        public string Name;
        public bool HasFixedCost;
        public short FixedCost;
        public bool HasFixedUpkeep;
        public float FixedUpkeep;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public string IconPath;
        public CombatState CombatState = CombatState.AttackRuns;
        public float MechanicalBoardingDefense;
        public string Hull;
        public RoleName Role = RoleName.fighter;
        public List<ShipToolScreen.ThrusterZone> ThrusterList;
        public string ModelPath;
        public AIState DefaultAIState;
        // The Doctor: intending to use this for 'Civilian', 'Recon', 'Fighter', 'Bomber' etc.
        public Category ShipCategory = Category.Unclassified;

        // @todo This lookup is expensive and never changes once initialized, find a way to initialize this properly
        public RoleName HullRole => ResourceManager.HullsDict.TryGetValue(Hull, out ShipData role) ? role.Role : Role;
        public ShipData HullData => ResourceManager.HullsDict.TryGetValue(Hull, out ShipData hull) ? hull : null;

        // The Doctor: intending to use this as a user-toggled flag which tells the AI not to build a design as a stand-alone vessel from a planet; only for use in a hangar
        public bool CarrierShip = false;
        public float BaseStrength;
        public bool BaseCanWarp;
        public List<ModuleSlotData> ModuleSlotList = new List<ModuleSlotData>();
        public bool hullUnlockable = false;
        public bool allModulesUnlocakable = true;
        public bool unLockable = false;
        //public HashSet<string> EmpiresThatCanUseThis = new HashSet<string>();
        public HashSet<string> techsNeeded = new HashSet<string>();
        public int TechScore = 0;
        //public Dictionary<string, HashSet<string>> EmpiresThatCanUseThis = new Dictionary<string, HashSet<string>>();
        private static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        private static readonly string[] CategoryArray = typeof(Category).GetEnumNames();

        public ShipData()
        {
        }

        private static void Parse(XmlReader r, out string value)
        {
            r.Read();
            value = r.Value;
        }
        private static void Parse(XmlReader r, out bool value)
        {
            r.Read();
            bool.TryParse(r.Value, out value);
        }
        private static void Parse(XmlReader r, out byte value)
        {
            r.Read();
            byte.TryParse(r.Value, out value);
        }
        private static void Parse(XmlReader r, out short value)
        {
            r.Read();
            short.TryParse(r.Value, out value);
        }
        private static void Parse(XmlReader r, out int value)
        {
            r.Read();
            int.TryParse(r.Value, out value);
        }
        private static void Parse(XmlReader r, out float value)
        {
            r.Read();
            float.TryParse(r.Value, out value);
        }
        private static void ParseEnum<T>(XmlReader r, out T value) where T : struct
        {
            r.Read();
            Enum.TryParse(r.Value, out value);
        }

        // Added by RedFox - manual parsing of ShipData, because this is the slowest part in loading
        public static ShipData Parse(FileInfo info)
        {
            var serializer = new SDNative.ShipDataSerializer();
            if (serializer.LoadFromFile(info.FullName))
            {
                
            }
            return null;

            using (XmlReader r = new XmlTextReader(info.OpenRead()))
            {
                if (!r.ReadToFollowing("ShipData"))
                    throw new SyntaxErrorException("Invalid ShipData XML");

                ShipData s = new ShipData();
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        // ModuleSlotList is by far the most intensive part of parsing, so give it special treatment
                        if (r.Name == "ModuleSlotList")
                            s.ParseModuleSlotList(r);
                        else switch (r.Name)
                        {
                            case "Animated": Parse(r, out s.Animated); break;
                            case "ShipStyle": Parse(r, out s.ShipStyle); break;
                            case "EventOnDeath": Parse(r, out s.EventOnDeath); break;
                            case "experience": Parse(r, out s.experience); break;
                            case "Level": Parse(r, out s.Level); break;
                            case "SelectionGraphic": s.SelectionGraphic = r.Value; break;
                            case "Name": Parse(r, out s.Name); break;
                            case "HasFixedCost": Parse(r, out s.HasFixedCost); break;
                            case "FixedCost": Parse(r, out s.FixedCost); break;
                            case "HasFixedUpkeep": Parse(r, out s.HasFixedUpkeep); break;
                            case "FixedUpkeep": Parse(r, out s.FixedUpkeep); break;
                            case "IsShipyard": Parse(r, out s.IsShipyard); break;
                            case "IsOrbitalDefense": Parse(r, out s.IsOrbitalDefense); break;
                            case "IconPath": Parse(r, out s.IconPath); break;
                            case "CombatState": ParseEnum(r, out s.CombatState); break;
                            case "MechanicalBoardingDefense": float.TryParse(r.Value, out s.MechanicalBoardingDefense); break;
                            case "Hull": Parse(r, out s.Hull); break;
                            case "Role": ParseEnum(r, out s.Role); break;
                            case "ModelPath": Parse(r, out s.ModelPath); break;
                            case "DefaultAIState": ParseEnum(r, out s.DefaultAIState); break;
                            case "ShipCategory": ParseEnum(r, out s.ShipCategory); break;
                            case "CarrierShip": Parse(r, out s.CarrierShip); break;
                            case "BaseStrength": Parse(r, out s.BaseStrength); break;
                            case "BaseCanWarp": Parse(r, out s.BaseCanWarp); break;
                            case "hullUnlockable": Parse(r, out s.hullUnlockable); break;
                            case "allModulesUnlocakable": Parse(r, out s.allModulesUnlocakable); break;
                            case "unLockable": Parse(r, out s.unLockable); break;
                            case "TechScore": Parse(r, out s.TechScore); break;
                            case "techsNeeded": s.ParseTechsNeeded(r); break;
                            case "ThrusterList": s.ParseThrusterList(r); break;
                        }
                    }
                    else if (r.NodeType == XmlNodeType.EndElement && r.Name == "ShipData")
                        break;

                }
                return s;
            }
        }

        private void ParseTechsNeeded(XmlReader r)
        {
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Whitespace)
                    break;
                if (r.NodeType == XmlNodeType.EndElement)
                    break;
                if (r.NodeType == XmlNodeType.Element)
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void ParseThrusterList(XmlReader r)
        {
            if (ThrusterList == null) ThrusterList = new List<ShipToolScreen.ThrusterZone>();
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Whitespace)
                    break;
                if (r.NodeType == XmlNodeType.EndElement)
                    break;
                if (r.NodeType == XmlNodeType.Element && r.Name == "ThrusterZone")
                {
                    var thruster = new ShipToolScreen.ThrusterZone();
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                        {
                            switch (r.Name)
                            {
                                case "X": Parse(r, out thruster.Position.X); break;
                                case "Y": Parse(r, out thruster.Position.Y); break;
                                case "scale": Parse(r, out thruster.scale); break;
                            }
                        }
                        if (r.NodeType == XmlNodeType.EndElement && r.Name == "ThrusterZone")
                            break;
                    }
                    ThrusterList.Add(thruster);
                }
            }
        }

        private void ParseModuleSlotList(XmlReader r)
        {
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element && r.Name == "ModuleSlotData")
                {
                    ModuleSlotData slot = new ModuleSlotData();
                    // Note: this is optimized heavily to support strict ordering
                    r.Read(); r.Read(); r.Read(); r.Read(); // skip <Position>
                    Parse(r, out slot.Position.X); r.Read(); r.Read(); r.Read();
                    Parse(r, out slot.Position.Y); r.Read(); r.Read(); r.Read();
                    r.Read(); r.Read();
                    Parse(r, out slot.InstalledModuleUID); r.Read(); r.Read(); r.Read();
                    r.Read(); slot.HangarshipGuid = new Guid(r.Value); r.Read(); r.Read(); r.Read();
                    Parse(r, out slot.Health); r.Read(); r.Read(); r.Read();
                    Parse(r, out slot.Shield_Power); r.Read(); r.Read(); r.Read();
                    Parse(r, out slot.facing); r.Read(); r.Read(); r.Read();
                    ParseEnum(r, out slot.state); r.Read(); r.Read(); r.Read();
                    ParseEnum(r, out slot.Restrictions); r.Read(); r.Read(); r.Read();
                    r.Read();
                            //switch (r.Name) {
                            //    case "X": Parse(r, out slot.Position.X); break;
                            //    case "Y": Parse(r, out slot.Position.Y); break;
                            //    case "InstalledModuleUID": Parse(r, out slot.InstalledModuleUID); break;
                            //    case "HangarshipGuid": r.Read(); slot.HangarshipGuid = new Guid(r.Value); break;
                            //    case "Health":       Parse(r, out slot.Health); break;
                            //    case "Shield_Power": Parse(r, out slot.Shield_Power); break;
                            //    case "facing": Parse(r, out slot.facing);break;
                            //    case "state": ParseEnum(r, out slot.state);break;
                            //    case "Restrictions": ParseEnum(r, out slot.Restrictions); break;
                            //    //case "SlotOptions": Parse(r, out slot.SlotOptions); break;
                            //}
                    ModuleSlotList.Add(slot);
                }
                else if (r.NodeType == XmlNodeType.EndElement && r.Name == "ModuleSlotList")
                    break;
            }
        }

        public ShipData GetClone()
        {
            return (ShipData)MemberwiseClone();
        }

        public string GetRole()
        {
            return RoleArray[(int)Role];
        }

        public string GetCategory()
        {
            return CategoryArray[(int)ShipCategory];
        }

        public enum Category
        {
            Unclassified,
            Civilian,
            Recon,
            Combat,
            Bomber,
            Fighter,            
            Kamikaze
        }

        public enum RoleName
        {
            disabled,
            platform,
            station,
            construction,
            supply,
            freighter,
            troop,
            fighter,
            scout,
            gunboat,
            drone,
            corvette,
            frigate,
            destroyer,
            cruiser,
            carrier,
            capital,
            prototype
        }
    }
}