using System;
using System.IO;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Data;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public partial class ShipDesign
    {
        public void Save(string filePath)
        {
            Save(new FileInfo(filePath));
        }

        public void Save(FileInfo file)
        {
            ShipDesignWriter sw = CreateShipDataText();
            sw.FlushToFile(file);
        }

        public string GetBase64DesignString()
        {
            ShipDesignWriter sw = CreateShipDataText();
            byte[] ascii = sw.GetASCIIBytes();
            return Convert.ToBase64String(ascii, Base64FormattingOptions.None);
        }

        ShipDesignWriter CreateShipDataText()
        {
            var sw = new ShipDesignWriter();
            sw.Write("Version", Version);
            sw.Write("Name", Name);
            sw.Write("Hull", Hull);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", ShipStyle);
            sw.Write("Description", Description);
            sw.Write("Size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            sw.Write("GridCenter", $"{GridInfo.Center.X},{GridInfo.Center.Y}");
            
            if (IconPath != BaseHull.IconPath)
                sw.Write("IconPath", IconPath);
            if (SelectionGraphic != BaseHull.SelectIcon)
                sw.Write("SelectIcon", SelectionGraphic);
            if (FixedCost > 0)
                sw.Write("FixedCost", FixedCost);
            if (FixedUpkeep > 0f)
                sw.Write("FixedUpkeep", FixedUpkeep);

            sw.Write("DefaultAIState", DefaultAIState);
            sw.Write("DefaultCombatState", DefaultCombatState);
            sw.Write("ShipCategory", ShipCategory);
            sw.Write("HangarDesignation", HangarDesignation);
            sw.Write("IsShipyard", IsShipyard);
            sw.Write("IsOrbitalDefense", IsOrbitalDefense);
            sw.Write("IsCarrierOnly", IsCarrierOnly);
            sw.Write("EventOnDeath", EventOnDeath); // "DefeatedMothership" remnant event

            sw.WriteLine("# Maps module UIDs to Index, first UID has index 0");
            sw.Write("ModuleUIDs", UniqueModuleUIDs);
            sw.Write("Modules", DesignSlots.Length);
            sw.WriteLine("# gridX,gridY;moduleUIDIndex;sizeX,sizeY;turretAngle;moduleRot;hangarShipUID");
            for (int i = 0; i < DesignSlots.Length; ++i)
            {
                WriteDesignSlotString(sw, DesignSlots[i], SlotModuleUIDMapping[i]).WriteLine();
            }
            return sw;
        }
        
        // X,Y,moduleIdx[,sizeX,sizeY,turretAngle,moduleRot,hangarShipUid]
        public static ShipDesignWriter WriteDesignSlotString(ShipDesignWriter sw, DesignSlot slot, ushort moduleIdx)
        {
            Point gp = slot.Pos;
            var sz = slot.Size;
            var ta = slot.TurretAngle;
            var mr = (int)slot.ModuleRot;

            string[] fields = new string[6];
            fields[0] = $"{gp.X},{gp.Y}";
            fields[1] = moduleIdx.ToString();
            // everything after this is optional
            fields[2] = (sz.X == 1 && sz.Y == 1) ? "" : $"{sz.X},{sz.Y}";
            fields[3] = ta == 0 ? "" : ta.ToString();
            fields[4] = mr == 0 ? "" : mr.ToString();
            fields[5] = slot.HangarShipUID;

            int count = GetMaxValidFields(fields);
            sw.Write(string.Join(";", fields, 0, count));
            return sw;
        }

        // get the max span of valid elements, so we can discard empty ones and save space
        static int GetMaxValidFields(string[] fields)
        {
            int count = fields.Length;
            for (; count > 0; --count)
                if (fields[count - 1].NotEmpty())
                    break;
            return count;
        }

        static ShipDesign ParseDesign(FileInfo file)
        {
            using (var p = new GenericStringViewParser(file))
            {
                if (!p.ReadLine(out StringView firstLine) && !firstLine.StartsWith("Version"))
                    throw new InvalidDataException($"Ship design must start with a Version=? tag! File={file}");

                firstLine.Next('=');
                int version = firstLine.ToInt();
                if (version != Version)
                {
                    if (version == 0)
                        throw new InvalidDataException($"Ship design version is invalid: {firstLine.Text} File={file}");

                    // TODO: convert from this version to newer version
                }

                var data = new ShipDesign(p, source:file);
                if (data.Role == RoleName.disabled)
                    return null;

                if (data.BaseHull == null)
                {
                    Log.Warning(ConsoleColor.Red, $"Hull='{data.Hull}' does not exist for Design: {file.FullName}");
                    return null;
                }
                return data;
            }
        }

        ShipDesign(GenericStringViewParser p, FileInfo source = null)
        {
            Source = source;

            string[] moduleUIDs = null;
            DesignSlot[] modules = null;
            int numModules = 0;
            ShipHull hull = null;
            ShipGridInfo gridInfo = default;

            while (p.ReadLine(out StringView line))
            {
                if (modules == null || moduleUIDs == null)
                {
                    StringView key = line.Next('=');
                    StringView value = line;

                    if (key == "Name") Name = value.Text;
                    else if (key == "Hull")
                    {
                        Hull = value.Text;
                        if (!ResourceManager.Hull(Hull, out hull)) // If the hull is invalid, then ship loading fails!
                            return;
                    }
                    else if (key == "ModName")
                    {
                        ModName = value.Text;
                        if (!IsValidForCurrentMod || !hull.IsValidForCurrentMod)
                        {
                            Role = RoleName.disabled;
                            return; // this design doesn't need to be parsed
                        }
                    }
                    else if (key == "Role")
                    {
                        Enum.TryParse(value.Text, out RoleName role);
                        Role = role;
                        if (role == RoleName.disabled)
                            return; // no need to parse further
                    }
                    else if (key == "Style")       ShipStyle = value.Text;
                    else if (key == "Description") Description = value.Text;
                    else if (key == "Size")        gridInfo.Size = PointSerializer.FromString(value);
                    else if (key == "GridCenter")  gridInfo.Center = PointSerializer.FromString(value);
                    else if (key == "IconPath")    IconPath = value.Text;
                    else if (key == "SelectIcon")  SelectionGraphic = value.Text;
                    else if (key == "FixedCost")   FixedCost = value.ToInt();
                    else if (key == "FixedUpkeep") FixedUpkeep = value.ToFloat();
                    else if (key == "DefaultAIState")     DefaultAIState = Enum.TryParse(value.Text, out AIState das) ? das : DefaultAIState;
                    else if (key == "DefaultCombatState") DefaultCombatState = Enum.TryParse(value.Text, out CombatState dcs) ? dcs : DefaultCombatState;
                    else if (key == "ShipCategory")       ShipCategory = Enum.TryParse(value.Text, out ShipCategory sc) ? sc : ShipCategory;
                    else if (key == "HangarDesignation")  HangarDesignation = Enum.TryParse(value.Text, out HangarOptions ho) ? ho : HangarDesignation;
                    else if (key == "IsShipyard")         IsShipyard       = value.ToBool();
                    else if (key == "IsOrbitalDefense")   IsOrbitalDefense = value.ToBool();
                    else if (key == "IsCarrierOnly")      IsCarrierOnly    = value.ToBool();
                    else if (key == "EventOnDeath")       EventOnDeath     = value.Text;
                    else if (key == "ModuleUIDs")
                    {
                        moduleUIDs = value.Split(';').Select(s => string.Intern(s.Text));
                    }
                    else if (key == "Modules")
                    {
                        modules = new DesignSlot[value.ToInt()];
                    }
                }
                else
                {
                    if (numModules == modules.Length)
                        throw new InvalidDataException($"Ship design module count is incorrect: {p.Name}");

                    DesignSlot slot = ParseDesignSlot(line, moduleUIDs);
                    modules[numModules++] = slot;
                }
            }

            GridInfo = gridInfo;
            BaseHull = hull;
            Bonuses = hull.Bonuses;
            IsShipyard |= hull.IsShipyard;
            IsOrbitalDefense |= hull.IsOrbitalDefense;

            // if lazy loading, throw away the modules to free up memory
            if (!GlobalStats.LazyLoadShipDesignSlots)
                DesignSlots = modules;

            SetModuleUIDs(moduleUIDs);
            InitializeCommonStats(hull, modules);
        }

        // Implemented for Lazy-Loading, only load the design slots and nothing else
        public static DesignSlot[] LoadDesignSlots(FileInfo file, string[] moduleUIDs)
        {
            using (var p = new GenericStringViewParser(file))
            {
                DesignSlot[] modules = null;
                int numModules = 0;

                while (p.ReadLine(out StringView line))
                {
                    if (modules == null)
                    {
                        StringView key = line.Next('=');
                        if (key == "Modules")
                            modules = new DesignSlot[line.ToInt()];
                    }
                    else
                    {
                        if (numModules == modules.Length)
                            throw new InvalidDataException($"Ship design module count is incorrect: {p.Name}");

                        DesignSlot slot = ParseDesignSlot(line, moduleUIDs);
                        modules[numModules++] = slot;
                    }
                }
                return modules;
            }
        }

        public static DesignSlot ParseDesignSlot(StringView line, string[] moduleUIDs)
        {
            StringView pt = line.Next(';');
            StringView index = line.Next(';');
            StringView sz = line.Next(';');
            StringView turretAngle = line.Next(';');
            StringView moduleRotation = line.Next(';');
            StringView slotOptions = line.Next(';');

            return new DesignSlot(
                PointSerializer.FromString(pt),
                moduleUIDs[index.ToInt()],
                sz.IsEmpty ? new Point(1,1) : PointSerializer.FromString(sz),
                turretAngle.IsEmpty ? 0 : turretAngle.ToInt(),
                moduleRotation.IsEmpty ? ModuleOrientation.Normal
                                        : (ModuleOrientation)moduleRotation.ToInt(),
                slotOptions.IsEmpty ? null : slotOptions.Text
            );
        }

        public static byte[] GetModulesBytes(ShipDesignWriter sw, Ship ship)
        {
            ModuleSaveData[] saved = ship.GetModuleSaveData();
            return GetModulesBytes(sw, saved, ship.ShipData);
        }

        // TODO: this needs insane optimizations
        public static byte[] GetModulesBytes(ShipDesignWriter sw, ModuleSaveData[] saved, IShipDesign design)
        {
            sw.Clear();
            sw.Write("1\n"); // first line is version

            string[] moduleUIDs = design.UniqueModuleUIDs;
            ushort[] slotModuleUIDMapping = design.SlotModuleUIDMapping;

            // module1;module2;module3\n
            sw.WriteLine(moduleUIDs);
            // number of modules
            sw.WriteLine(saved.Length.ToString());

            // each module takes two lines
            // first line is DesignModule, second line is ModuleSaveData fields
            for (int i = 0; i < saved.Length; ++i)
            {
                WriteModuleSaveData(sw, saved[i], slotModuleUIDMapping[i]).WriteLine();
            }

            return sw.GetASCIIBytes();
        }

        public static ShipDesignWriter WriteModuleSaveData(ShipDesignWriter sw, ModuleSaveData slot, ushort moduleIdx)
        {
            WriteDesignSlotString(sw, slot, moduleIdx).WriteLine();

            // NOTE: "0" must be written out, so that StringViewParser doesn't ignore the line!
            sw.Write(slot.Health > 0 ? slot.Health.String(1) : "0");
            if (slot.ShieldPower > 0 || slot.HangarShipId > 0)
            {
                sw.Write(';');
                sw.Write(slot.ShieldPower > 0 ? slot.ShieldPower.String(1) : "");
                if (slot.HangarShipId > 0)
                {
                    sw.Write(';');
                    sw.Write(slot.HangarShipId.ToString());
                }
            }
            return sw;
        }

        public static ModuleSaveData ParseModuleSaveData(GenericStringViewParser p, string[] moduleUIDs)
        {
            StringView line1 = p.ReadLine();
            DesignSlot s = ParseDesignSlot(line1, moduleUIDs);

            StringView line2 = p.ReadLine();
            StringView healthPts = line2.Next(';');
            StringView shieldPwr = line2.Next(';');
            StringView hangarShp = line2.Next(';');

            return new ModuleSaveData(s,
                healthPts.IsEmpty ? 0 : healthPts.ToFloat(),
                shieldPwr.IsEmpty ? 0 : shieldPwr.ToFloat(),
                hangarShp.IsEmpty ? 0 : hangarShp.ToInt()
            );
        }

        public static (ModuleSaveData[] modules, string[] moduleUIDs) GetModuleSaveFromBytes(byte[] bytes)
        {
            //Log.Info(Encoding.ASCII.GetString(bytes));
            var p = new GenericStringViewParser("save", bytes);

            int version = p.ReadLine().ToInt();
            if (version != 1)
            {
                // TODO: convert from version 1 to version X
                throw new Exception($"Unsupported ModuleSaveData version: {version}");
            }

            string[] moduleUIDs = p.ReadLine().Split(';').Select(s => string.Intern(s.Text));
            int numModules = p.ReadLine().ToInt();
            var modules = new ModuleSaveData[numModules];
            
            for (int i = 0; i < modules.Length; ++i)
            {
                modules[i] = ParseModuleSaveData(p, moduleUIDs);
            }

            return (modules, moduleUIDs);
        }
    }
}
