﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SDGraphics;
using Point = SDGraphics.Point;
using SDUtils;

namespace Ship_Game.Ships.Legacy
{
    public partial class LegacyShipData
    {
        public void SaveDesign(string filePath)
        {
            var file = new FileInfo(filePath);
            SaveDesign(file);
        }

        /// <summary>
        /// Convert LegacyShipData into new ShipDesign
        /// </summary>
        /// <param name="file"></param>
        public void SaveDesign(FileInfo file)
        {
            var sw = new ShipDesignWriter();
            sw.Write("Version", 1);
            sw.Write("Name", Name);
            sw.Write("Hull", Hull);
            sw.Write("Role", Role);
            sw.Write("ModName", ModName);
            sw.Write("Style", ShipStyle);
            sw.Write("Description", Description);
            sw.Write("Size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            sw.Write("GridCenter", $"{GridInfo.GridCenter.X},{GridInfo.GridCenter.Y}");

            if (this != BaseHull && IconPath != BaseHull.IconPath)
                sw.Write("IconPath", IconPath);
            if (this != BaseHull && SelectionGraphic != BaseHull.SelectionGraphic)
                sw.Write("SelectIcon", SelectionGraphic);
            if (FixedCost > 0)
                sw.Write("FixedCost", FixedCost);
            if (FixedUpkeep > 0f)
                sw.Write("FixedUpkeep", FixedUpkeep);

            sw.Write("DefaultAIState", DefaultAIState);
            sw.Write("DefaultCombatState", CombatState);
            sw.Write("ShipCategory", ShipCategory);
            sw.Write("HangarDesignation", HangarDesignation);
            sw.Write("IsShipyard", IsShipyard);
            sw.Write("IsOrbitalDefense", IsOrbitalDefense);
            sw.Write("IsCarrierOnly", CarrierShip);
            sw.Write("EventOnDeath", EventOnDeath); // "DefeatedMothership" remnant event

            var moduleUIDsToIdx = new Array<string>();
            foreach (LegacyModuleSlotData slot in ModuleSlots)
            {
                if (!slot.IsDummy && !moduleUIDsToIdx.Contains(slot.ModuleUID))
                    moduleUIDsToIdx.Add(slot.ModuleUID);
            }

            var moduleLines = new Array<string>();
            foreach (LegacyModuleSlotData slot in ModuleSlots)
            {
                // New data format does not have dummy modules
                if (slot.IsDummy)
                    continue;

                Vector2 p = (slot.Position - new Vector2(LegacyShipGridInfo.ModuleSlotOffset)) - GridInfo.VirtualOrigin;
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
            sw.WriteLine("# gridX,gridY;moduleUIDIndex;sizeX,sizeY;turretAngle;moduleRot;hangarShipUID");
            foreach (string m in moduleLines)
                sw.WriteLine(m);

            sw.FlushToFile(file);
        }
    }
}
