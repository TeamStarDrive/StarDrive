using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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
            sw.Write("Size", $"{GridInfo.Size.X},{GridInfo.Size.Y}");
            
            if (this != BaseHull && IconPath != BaseHull.IconPath)
                sw.Write("IconPath", IconPath);
            if (this != BaseHull && SelectionGraphic != BaseHull.SelectionGraphic)
                sw.Write("SelectIcon", SelectionGraphic);
            if (FixedCost > 0)
                sw.Write("FixedCost", FixedCost);
            if (FixedUpkeep > 0f)
                sw.Write("FixedUpkeep", FixedUpkeep);

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

                var p = (slot.Position - new Vector2(ShipModule.ModuleSlotOffset)) - GridInfo.Origin;
                var gp = new Point((int)(p.X / 16f), (int)(p.Y / 16f));
                var sz = slot.GetSize();
                var f = (int)slot.Facing;
                var o = (int)slot.GetOrientation();

                // if we have SlotOptions, we will need to write out Facing and Orient to make it parseable
                bool facing = false, orient = false, opt = false;
                if (slot.SlotOptions.NotEmpty()) facing = orient = opt = true;
                if (o != 0) facing = orient = true;
                if (f != 0) facing = true;

                string optional = "";
                if (facing)  optional += ";" + f;
                if (orient)  optional += ";" + o;
                if (opt)     optional += ";" + slot.SlotOptions;

                int idx = moduleUIDsToIdx.IndexOf(slot.ModuleUID);
                moduleLines.Add($"{gp.X},{gp.Y};{idx};{sz.X},{sz.Y}{optional}");
            }

            sw.WriteLine("# Maps module UIDs to Index, first UID has index 0");
            sw.Write("ModuleUIDs", string.Join(";", moduleUIDsToIdx));
            sw.Write("Modules", moduleLines.Count);
            sw.WriteLine("# gridX,gridY;moduleUIDIndex;sizeX,sizeY(oriented);[facing(0-359)];[orientation(0-3)];[options]");
            foreach (string m in moduleLines)
                sw.WriteLine(m);

            sw.FlushToFile(file);
        }

        static ShipData ParseDesign(FileInfo file)
        {
            return null;
        }
    }
}
