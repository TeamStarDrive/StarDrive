using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Data;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    // NOTE: public variables are SERIALIZED
    public partial class ShipData
    {
        public void Save(FileInfo file)
        {
            var sw = new ShipDataWriter();
            sw.Write("Version", CurrentVersion);
            sw.Write("Name", Name);
            sw.Write("Hull", Hull);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", ShipStyle);
            sw.Write("Description", Description);
            sw.Write("Size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            sw.Write("LegacyOrigin", $"{GridInfo.Origin.X},{GridInfo.Origin.Y}");
            
            if (this != BaseHull && IconPath != BaseHull.IconPath)
                sw.Write("IconPath", IconPath);
            if (this != BaseHull && SelectionGraphic != BaseHull.SelectionGraphic)
                sw.Write("SelectIcon", SelectionGraphic);
            if (FixedCost > 0)
                sw.Write("FixedCost", FixedCost);
            if (FixedUpkeep > 0f)
                sw.Write("FixedUpkeep", FixedUpkeep);
            
            if (UnLockable)
                sw.Write("Unlockable", UnLockable);
            if (HullUnlockable)
                sw.Write("HullUnlockable", HullUnlockable);
            if (AllModulesUnlockable)
                sw.Write("AllModulesUnlockable", AllModulesUnlockable);

            sw.Write("DefaultAIState", DefaultAIState);
            sw.Write("CombatState", CombatState);
            sw.Write("EventOnDeath", EventOnDeath); // "DefeatedMothership" remnant event

            var moduleUIDsToIdx = new Array<string>();
            foreach (ModuleSlotData slot in ModuleSlots)
            {
                if (!slot.IsDummy && !moduleUIDsToIdx.Contains(slot.ModuleUID))
                    moduleUIDsToIdx.Add(slot.ModuleUID);
            }

            var moduleLines = new Array<string>();
            foreach (ModuleSlotData slot in ModuleSlots)
            {
                // New data format does not have dummy modules
                if (slot.IsDummy)
                    continue;

                Vector2 p = (slot.Position - new Vector2(ShipModule.ModuleSlotOffset)) - GridInfo.Origin;
                var gp = new Point((int)(p.X / 16f), (int)(p.Y / 16f));
                if (p.X < 0f || p.Y < 0f)
                    Log.Error($"Ship {Name} Save BUG: LegacyPos={slot.Position} converted to invalid GridPos={gp}");
                int moduleIndex = moduleUIDsToIdx.IndexOf(slot.ModuleUID);
                var sz = slot.GetSize();
                var f = (int)slot.Facing;
                var o = (int)slot.GetOrientation();

                string[] fields = new string[6];
                fields[0] = gp.X + "," + gp.Y;
                fields[1] = moduleIndex.ToString();
                // everything after this is optional
                fields[2] = (sz.X == 1 && sz.Y == 1) ? "" : sz.X + "," + sz.Y;
                fields[3] = f == 0 ? "" : f.ToString();
                fields[4] = o == 0 ? "" : o.ToString();
                fields[5] = slot.SlotOptions.IsEmpty() ? "" : slot.SlotOptions;

                // get the max span of valid elements, so we can discard empty ones and save space
                int count = fields.Length;
                for (; count > 0; --count)
                    if (fields[count - 1] != "")
                        break;

                moduleLines.Add(string.Join(";", fields, 0, count));
            }

            sw.WriteLine("# Maps module UIDs to Index, first UID has index 0");
            sw.Write("ModuleUIDs", string.Join(";", moduleUIDsToIdx));
            sw.Write("Modules", moduleLines.Count);
            sw.WriteLine("# gridX,gridY;moduleUIDIndex;sizeX,sizeY;turretAngle;moduleRotation;slotOptions");
            foreach (string m in moduleLines)
                sw.WriteLine(m);

            sw.FlushToFile(file);
        }

        static ShipData ParseDesign(FileInfo file)
        {
            using (var p = new GenericStringViewParser(file))
            {
                if (!p.ReadLine(out StringView firstLine) && !firstLine.StartsWith("Version"))
                    throw new InvalidDataException($"Ship design must start with a Version=? tag! File={file}");

                firstLine.Next('=');
                int version = firstLine.ToInt();
                if (version != CurrentVersion)
                {
                    if (version == 0)
                        throw new InvalidDataException($"Ship design version is invalid: {firstLine.Text} File={file}");

                    // TODO: convert from this version to newer version
                }
                return new ShipData(p);
            }
        }

        ShipData(GenericStringViewParser p)
        {
            string[] moduleUIDs = null;
            DesignSlot[] modules = null;
            int numModules = 0;

            while (p.ReadLine(out StringView line))
            {
                if (modules == null || moduleUIDs == null)
                {
                    StringView key = line.Next('=');
                    StringView value = line;

                    if      (key == "Name") Name = value.Text;
                    else if (key == "Hull") Hull = value.Text;
                    else if (key == "Role") Enum.TryParse(value.Text, out Role);
                    else if (key == "Style")       ShipStyle = value.Text;
                    else if (key == "Description") Description = value.Text;
                    else if (key == "Size")        GridInfo.Size = PointSerializer.FromString(value);
                    else if (key == "LegacyOrigin")GridInfo.Origin = Vector2Serializer.FromString(value);
                    else if (key == "IconPath")    IconPath = value.Text;
                    else if (key == "SelectIcon")  SelectionGraphic = value.Text;
                    else if (key == "FixedCost")   FixedCost = value.ToInt();
                    else if (key == "FixedUpkeep") FixedUpkeep = value.ToFloat();
                    else if (key == "Unlockable")           UnLockable = value.ToBool();
                    else if (key == "HullUnlockable")       HullUnlockable = value.ToBool();
                    else if (key == "AllModulesUnlockable") AllModulesUnlockable = value.ToBool();
                    else if (key == "DefaultAIState") Enum.TryParse(value.Text, out DefaultAIState);
                    else if (key == "CombatState")    Enum.TryParse(value.Text, out CombatState);
                    else if (key == "EventOnDeath")   EventOnDeath = value.Text;
                    else if (key == "ModuleUIDs")
                        moduleUIDs = value.Split(';').Select(s => string.Intern(s.Text));
                    else if (key == "Modules")
                        modules = new DesignSlot[value.ToInt()];
                }
                else
                {
                    if (numModules == modules.Length)
                        throw new InvalidDataException($"Ship design module count is incorrect: {p.Name}");

                    StringView pt = line.Next(';');
                    StringView index = line.Next(';');
                    StringView sz = line.Next(';');
                    StringView turretAngle = line.Next(';');
                    StringView moduleRotation = line.Next(';');
                    StringView slotOptions = line.Next(';');

                    var slot = new DesignSlot(
                        PointSerializer.FromString(pt),
                        moduleUIDs[index.ToInt()],
                        sz.IsEmpty ? new Point(1,1) : PointSerializer.FromString(sz),
                        turretAngle.IsEmpty ? 0 : turretAngle.ToInt(),
                        moduleRotation.IsEmpty ? ModuleOrientation.Normal
                                               : (ModuleOrientation)moduleRotation.ToInt(),
                        slotOptions.IsEmpty ? null : slotOptions.Text
                    );

                    modules[numModules++] = slot;
                }
            }

            if (!ResourceManager.NewHull(Hull, out ShipHull hull))
                throw new InvalidDataException($"Hull {Hull} not found");

            ThrusterList = hull.Thrusters;
            ShipStyle = hull.Style;
            IconPath = hull.IconPath;
            ModelPath = hull.ModelPath;
            Animated = hull.Animated;
            IsShipyard = hull.IsShipyard;
            IsOrbitalDefense = hull.IsOrbitalDefense;

            //if (Name.Contains("Acolyte of Flak II"))
            //    Debugger.Break();

            Vector2 realOrigin = GridInfo.Origin + new Vector2(ShipModule.ModuleSlotOffset);
            GridInfo.Span = new Vector2(GridInfo.Size.X, GridInfo.Size.Y) * 16f;
            GridInfo.SurfaceArea = hull.Area;

            ModuleSlots = ConvertDesignSlotToModuleSlots(this, hull, modules, realOrigin);
            
            UpdateBaseHull();
        }

        // Legacy compatibility util
        public static ModuleSlotData[] ConvertDesignSlotToModuleSlots(ShipData data, ShipHull hull, DesignSlot[] slots, Vector2 origin)
        {
            var modules = new ModuleSlotData[slots.Length];
            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot slot = slots[i];
                Vector2 pos = origin + new Vector2(slot.Pos.X*16f, slot.Pos.Y*16f);

                var r = Restrictions.IO;
                HullSlot hs = hull.FindSlot(slot.Pos);
                if (hs != null)
                    r = hs.R;
                else
                    Log.Warning($"Hull {hull.HullName} does not match design {data.Name} at grid={slot.Pos} legacy={pos} hullSize={hull.Size} dataSize={data.GridInfo.Size}");

                modules[i] = new ModuleSlotData(pos, r, slot.ModuleUID, slot.TurretAngle,
                    ModuleSlotData.GetOrientationString(slot.ModuleRotation), slot.SlotOptions);
            }
            return modules;
        }
    }
}
