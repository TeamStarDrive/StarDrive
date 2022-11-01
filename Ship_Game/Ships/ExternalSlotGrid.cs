using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;
using Ship_Game.Utils;

namespace Ship_Game.Ships
{
    public class ExternalSlotGrid
    {
        // Current number of external modules
        // This will change as the ship loses modules
        public int NumModules { get; private set; }

        // a 2D grid of external slots, width*height
        // if [x, y] bit is set, that slot contains an external module
        BitArray ExternalGrid;

        public ExternalSlotGrid(in ModuleGridState gs)
        {
            Initialize(gs, gs.Width, gs.Height);
        }

        public ShipModule Get(in ModuleGridState gs, int x, int y)
        {
            if (ExternalGrid.IsSet(x + y*gs.Grid.Width))
                return gs.Get(x, y);
            return null;
        }

        public void Initialize(in ModuleGridState gs, int w, int h)
        {
            ExternalGrid = new BitArray(w * h);
            for (int i = 0; i < gs.Modules.Length; ++i)
            {
                ShipModule m = gs.Modules[i];
                UpdateSlotsUnderModule(gs, m);
            }
        }

        // updates the isExternal status of a module and its neighbors
        public void Update(in ModuleGridState gs, ShipModule m)
        {
            UpdateSlotsUnderModule(gs, m);

            // if our module is 2x2, we must check all slots around us
            // [?][?][?][?]
            // [?][m][m][?]
            // [?][m][m][?]
            // [?][?][?][?]
            int x = m.Pos.X, y = m.Pos.Y;
            int left = x - 1, right  = x + m.XSize;
            int top  = y - 1, bottom = y + m.YSize;

            // check top row and bottom row simultaneously
            for (int ix = left; ix <= right; ++ix)
            {
                UpdateSlotsUnderModuleAt(gs, ix, top);
                UpdateSlotsUnderModuleAt(gs, ix, bottom);
            }

            // check sides, excluding the bottom corners which were already checked
            for (int iy = y; iy < bottom; ++iy)
            {
                UpdateSlotsUnderModuleAt(gs, left, iy);
                UpdateSlotsUnderModuleAt(gs, right, iy);
            }
        }

        // Updates only this ShipModule's IsExternal status
        public void UpdateSlotsUnderModule(in ModuleGridState gs, ShipModule m)
        {
            if (ShouldBeExternal(gs, m))
            {
                if (!m.IsExternal)
                {
                    m.IsExternal = true;
                    ++NumModules;
                    UpdateGridSlot(gs, m, isExternal: true);
                }
            }
            else
            {
                if (m.IsExternal)
                {
                    m.IsExternal = false;
                    --NumModules;
                    UpdateGridSlot(gs, m, isExternal: false);
                }
            }
        }

        // updates all slots that are under module at x,y
        void UpdateSlotsUnderModuleAt(in ModuleGridState gs, int x, int y)
        {
            if (gs.Get(x, y, out ShipModule m))
                UpdateSlotsUnderModule(gs, m);
        }

        void UpdateGridSlot(in ModuleGridState gs, ShipModule module, bool isExternal)
        {
            int posX = module.Pos.X, endX = posX + module.XSize;
            int posY = module.Pos.Y, endY = posY + module.YSize;
            int w = gs.Grid.Width;
            for (int y = posY; y < endY; ++y)
            {
                for (int x = posX; x < endX; ++x)
                {
                    ExternalGrid.Set(x + y * w, isExternal);
                }
            }
        }

        static bool ShouldBeExternal(in ModuleGridState gs, ShipModule m)
        {
            if (!m.Active)
                return false;

            // if our module is 2x2, we must check all slots around us
            // [?][?][?][?]
            // [?][m][m][?]
            // [?][m][m][?]
            // [?][?][?][?]
            int x = m.Pos.X, y = m.Pos.Y;
            int left = x - 1, right  = x + m.XSize;
            int top  = y - 1, bottom = y + m.YSize;

            // check top row and bottom row simultaneously
            for (int ix = left; ix <= right; ++ix)
                if (IsInactiveAt(gs, ix, top) || IsInactiveAt(gs, ix, bottom))
                    return true; // neighboring inactive slot then we are external

            // check sides, excluding the bottom corners which were already checked
            for (int iy = y; iy < bottom; ++iy)
                if (IsInactiveAt(gs, left, iy) || IsInactiveAt(gs, right, iy))
                    return true; // neighboring inactive slot then we are external

            return false;
        }

        static bool IsInactiveAt(in ModuleGridState gs, int x, int y)
        {
            return !gs.Get(x, y, out ShipModule m) || !m.Active;
        }

        public void DebugDump(string name, in ModuleGridState gs)
        {
            ModuleGridUtils.DebugDumpGrid($"Debug/ExternalSlotGrid/{name}.bits.txt",
                ExternalGrid.ToArray(), gs.Width, gs.Height, ModuleGridUtils.DumpFormat.ExternalSlotsBits);

            var modules = gs.Grid.CreateGridArray(gs.Modules);
            ModuleGridUtils.DebugDumpGrid($"Debug/ExternalSlotGrid/{name}.txt",
                modules, gs.Width, gs.Height, ModuleGridUtils.DumpFormat.ExternalSlotModules);
        }
    }
}
