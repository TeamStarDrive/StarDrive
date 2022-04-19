using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This is the shared ModuleGrid of a ShipDesign, initialized and stored in the design
    ///
    /// Ship instances can use this information as a fast lookup table.
    /// In the past, every Ship had its own ModuleGrid which took too much memory,
    /// so this can be viewed as sort of a Flyweight pattern
    /// 
    /// This also contains information about shields and amplifiers
    /// </summary>
    public class ModuleGridFlyweight
    {
        /// <summary>  Ship slot (1x1 modules) width </summary>
        public readonly int Width;
        
        /// <summary>Ship slot (1x1 modules) height </summary>
        public readonly int Height;

        /// <summary>Total surface area in 1x1 modules</summary>
        public readonly int SurfaceArea;

        // center of the ship in ShipLocal world coordinates, ex [64f, 0f], always positive
        public readonly Vector2 GridLocalCenter;

        public readonly float Radius;

        static bool EnableDebugGridExport = false;

        // to reduce memory usage, this is just a grid of ModuleIndexes
        // since this is a sparse Width x Height grid, many of these
        // will be -1 meaning no module at that point
        short[] ModuleIndexGrid;

        short[] ShieldsIndex; // all shield module indices
        short[] AmplifiersIndex; // all amplifier module indices

        // total number of installed module SLOTS with Internal Restrictions
        // example: a 2x2 internal module would give +4
        public readonly int TotalInternalModuleSlots;

        public ModuleGridFlyweight(ShipDesign design)
        {
            var info = design.GridInfo;
            SurfaceArea = info.SurfaceArea;
            Width  = info.Size.X;
            Height = info.Size.Y;
            GridLocalCenter = new Vector2(info.Size.X * 8f, info.Size.Y * 8f);
        
            // Ship's true radius is half of Module Grid's Diagonal Length
            var span = new Vector2(info.Size.X, info.Size.Y) * 16f;
            Radius = span.Length() * 0.5f;

            var slots = design.GetOrLoadDesignSlots();
            
            ModuleIndexGrid = new short[slots.Length];
            for (int i = 0; i < ModuleIndexGrid.Length; ++i)
                ModuleIndexGrid[i] = -1;

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot s = slots[i];
                Point p = s.Pos;
                int endX = p.X + s.Size.X, endY = p.Y + s.Size.Y;
                for (int y = p.Y; y < endY; ++y)
                for (int x = p.X; x < endX; ++x)
                    ModuleIndexGrid[x + y * Width] = (short)i;
            }

            var shields = new Array<short>();
            var amplifiers = new Array<short>();

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot s = slots[i];
                ShipModule module = ResourceManager.GetModuleTemplate(s.ModuleUID);
                if (module.ShieldPowerMax > 0f)
                    shields.Add((short)i);

                if (module.AmplifyShields > 0f)
                    amplifiers.Add((short)i);

                if (module.HasInternalRestrictions)
                    TotalInternalModuleSlots += s.Size.X * s.Size.Y;
            }

            ShieldsIndex = shields.ToArray();
            AmplifiersIndex = amplifiers.ToArray();

            if (EnableDebugGridExport)
            {
                var slotsGrid = ModuleIndexGrid.Select(index => index != -1 ? slots[index] : null);
                ModuleGridUtils.DebugDumpGrid($"Debug/SparseGrid/{design.Name}.txt",
                    slotsGrid, Width, Height, ModuleGridUtils.DumpFormat.DesignSlot);
            }
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        public bool Get(ShipModule[] modules, int x, int y, out ShipModule module)
        {
            if ((uint)x < Width && (uint)y < Height)
            {
                int index = ModuleIndexGrid[x + y * Width];
                module = modules[index];
                return module != null;
            }
            module = null;
            return false;
        }

        public ShipModule Get(ShipModule[] modules, Point gridPos)
        {
            int index = ModuleIndexGrid[gridPos.X + gridPos.Y * Width];
            return modules[index];
        }

        public ShipModule Get(ShipModule[] modules, int gridPosX, int gridPosY)
        {
            int index = ModuleIndexGrid[gridPosX + gridPosY * Width];
            return modules[index];
        }

        public ShipModule Get(ShipModule[] modules, int gridIndex)
        {
            int index = ModuleIndexGrid[gridIndex];
            return modules[index];
        }
    }

}
