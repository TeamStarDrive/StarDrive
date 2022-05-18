using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

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

        // Radius of the module grid
        public readonly float Radius;

        static bool EnableDebugGridExport = false;

        // to reduce memory usage, this is just a grid of ModuleIndexes
        // since this is a sparse Width x Height grid, many of these
        // will be -1 meaning no module at that point
        readonly short[] ModuleIndexGrid;

        // this a sparse Width x Height grid of overlapping shield radius
        readonly short[][] ShieldIndicesGrid;

        readonly short[] ShieldsIndex; // all shield module indices
        readonly short[] AmplifiersIndex; // all amplifier module indices

        // total number of installed module SLOTS with Internal Restrictions
        // example: a 2x2 internal module would give +4
        public readonly int NumInternalSlots;

        public ModuleGridFlyweight(string name, in ShipGridInfo info, DesignSlot[] slots)
        {
            SurfaceArea = info.SurfaceArea;
            Width  = info.Size.X;
            Height = info.Size.Y;
            GridLocalCenter = new Vector2(info.Center.X*16f, info.Center.Y*16f);
        
            // Ship's true radius is half of Module Grid's Diagonal Length
            var span = new Vector2(Width, Height) * 16f;
            Radius = span.Length() * 0.5f;

            ModuleIndexGrid = new short[Width * Height];
            for (int i = 0; i < ModuleIndexGrid.Length; ++i)
                ModuleIndexGrid[i] = -1;

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot s = slots[i];
                Point p = s.Pos;
                int endX = p.X + s.Size.X, endY = p.Y + s.Size.Y;
                for (int y = p.Y; y < endY; ++y)
                for (int x = p.X; x < endX; ++x)
                {
                    int index = x + y * Width;
                    if (ModuleIndexGrid[index] != -1)
                    {
                        Log.Error($"Overlapping DesignSlot in design={name} slot={s}");
                    }
                    ModuleIndexGrid[index] = (short)i;
                }
            }

            var shields = new Array<(DesignSlot, ShipModule, int)>();
            var shieldIndices = new Array<short>();
            var amplifierIndices = new Array<short>();

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot s = slots[i];
                ShipModule module = ResourceManager.GetModuleTemplate(s.ModuleUID);
                if (module.ShieldPowerMax > 0f)
                {
                    shields.Add((s, module, i));
                    shieldIndices.Add((short)i);
                }

                if (module.AmplifyShields > 0f)
                    amplifierIndices.Add((short)i);

                if (module.HasInternalRestrictions)
                    NumInternalSlots += s.Size.X * s.Size.Y;
            }

            ShieldsIndex = shieldIndices.ToArray();
            AmplifiersIndex = amplifierIndices.ToArray();

            // create the Shield Grid
            // WARNING: THIS DOES NOT WORK IF SHIELD RADIUS CHANGES DURING GAME
            var shieldsGrid = new Array<short>[Width * Height];

            // TESTING: this is tested in ModuleGridFlyweightTests
            // in addition there is debug visualization in DevSandbox Shipyard (press TAB key)
            foreach ((DesignSlot sh, ShipModule shieldModule, int shieldSlotIndex) in shields)
            {
                Vector2 shieldPos = new Vector2(sh.Pos)*16 + new Vector2(sh.Size)*8;
                float shieldRad = shieldModule.ShieldHitRadius - 2f;

                Point min = GridLocalToPointClipped(shieldPos - new Vector2(shieldRad));
                Point max = GridLocalToPointClipped(shieldPos + new Vector2(shieldRad));

                for (int y = min.Y; y <= max.Y; ++y)
                {
                    for (int x = min.X; x <= max.X; ++x)
                    {
                        var slotRect = new RectF(x*16f, y*16f, 16f, 16f);
                        if (slotRect.Overlaps(shieldPos.X, shieldPos.Y, shieldRad))
                        {
                            int index = x + y * Width;
                            shieldsGrid[index] ??= new Array<short>();
                            shieldsGrid[index].Add((short)shieldSlotIndex);
                        }
                    }
                }
            }

            ShieldIndicesGrid = shieldsGrid.Select(a => a?.ToArr());

            if (EnableDebugGridExport)
            {
                DebugDumpGrid(name, slots);
            }
        }

        public void DebugDumpGrid(IShipDesign design)
        {
            DebugDumpGrid(design.Name, design.GetOrLoadDesignSlots());
        }

        public void DebugDumpGrid(string name, DesignSlot[] slots)
        {
            var grid = CreateGridArray(slots);
            ModuleGridUtils.DebugDumpGrid($"Debug/SparseGrid/{name}.txt",
                grid, Width, Height, ModuleGridUtils.DumpFormat.DesignSlot);
        }

        // used for debugging
        [Pure] public T[] CreateGridArray<T>(T[] modules)
        {
            return ModuleIndexGrid.Select(index => index != -1 ? modules[index] : default);
        }

        // Converts a grid-local pos to a grid point
        // TESTED in ShipModuleGridTests
        [Pure] public Point GridLocalToPoint(in Vector2 localPos)
        {
            return new Point((int)Math.Floor(localPos.X / 16f),
                             (int)Math.Floor(localPos.Y / 16f));
        }

        // Converts a grid-local pos to a grid point AND clips it to grid bounds
        // TESTED in ShipModuleGridTests
        [Pure] public Point GridLocalToPointClipped(in Vector2 localPos)
        {
            return ClipLocalPoint(GridLocalToPoint(localPos));
        }

        // TESTED in ShipModuleGridTests
        [Pure] public Point ClipLocalPoint(Point pt)
        {
            if (pt.X < 0) pt.X = 0; else if (pt.X >= Width)  pt.X = Width  - 1;
            if (pt.Y < 0) pt.Y = 0; else if (pt.Y >= Height) pt.Y = Height - 1;
            return pt;
        }

        [Pure] public bool LocalPointInBounds(Point point)
        {
            return (uint)point.X < Width && (uint)point.Y < Height;
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        [Pure] public bool Get(ShipModule[] modules, int x, int y, out ShipModule module)
        {
            if ((uint)x < Width && (uint)y < Height)
            {
                return (module = Get(modules, x, y)) != null;
            }
            module = null;
            return false;
        }

        [Pure] public ShipModule this[ShipModule[] modules, int x, int y]
        {
            get
            {
                int index = ModuleIndexGrid[x + y * Width];
                return index != -1 ? modules[index] : null;
            }
        }

        [Pure] public ShipModule Get(ShipModule[] modules, int gridPosX, int gridPosY)
        {
            int index = ModuleIndexGrid[gridPosX + gridPosY * Width];
            return index != -1 ? modules[index] : null;
        }

        [Pure] public ShipModule Get(ShipModule[] modules, Point gridPos)
        {
            int index = ModuleIndexGrid[gridPos.X + gridPos.Y * Width];
            return index != -1 ? modules[index] : null;
        }

        [Pure] public ShipModule Get(ShipModule[] modules, int gridIndex)
        {
            int index = ModuleIndexGrid[gridIndex];
            return index != -1 ? modules[index] : null;
        }

        [Pure] public IEnumerable<ShipModule> GetShields(ShipModule[] modules)
        {
            for (int i = 0; i < ShieldsIndex.Length; ++i)
                yield return modules[ShieldsIndex[i]];
        }

        [Pure] public IEnumerable<ShipModule> GetAmplifiers(ShipModule[] modules)
        {
            for (int i = 0; i < AmplifiersIndex.Length; ++i)
                yield return modules[AmplifiersIndex[i]];
        }

        // safe and fast SHIELD lookup by x,y where coordinates (0,1) (2,1) etc
        [Pure] public bool GetActiveShield(ShipModule[] modules, int x, int y, out ShipModule shield)
        {
            if ((uint)x < Width && (uint)y < Height)
            {
                return (shield = GetActiveShield(modules, x, y)) != null;
            }
            shield = null;
            return false;
        }

        [Pure] public short[] GetShieldIndicesAt(int gridPosX, int gridPosY)
        {
            return ShieldIndicesGrid[gridPosX + gridPosY * Width];
        }

        [Pure] public ShipModule GetActiveShield(ShipModule[] modules, short index)
        {
            ShipModule shield = modules[index];
            return shield.ShieldsAreActive ? shield : null;
        }

        [Pure] public ShipModule GetActiveShield(ShipModule[] modules, int gridPosX, int gridPosY)
        {
            short[] shields = GetShieldIndicesAt(gridPosX, gridPosY);
            if (shields == null) return null;

            for (int i = 0; i < shields.Length; ++i)
            {
                ShipModule shield = GetActiveShield(modules, shields[i]);
                if (shield != null)
                    return shield;
            }

            return null; // no active shields
        }

        /// <returns>Performs optimized HitTest for Shields through the ShieldGrid</returns>
        [Pure] public ShipModule HitTestShieldsAt(ShipModule[] modules, Point gridPos, float hitRadius)
        {
            short[] shields = GetShieldIndicesAt(gridPos.X, gridPos.Y);
            if (shields == null) return null;

            for (int i = 0; i < shields.Length; ++i)
            {
                ShipModule shield = GetActiveShield(modules, shields[i]);
                if (shield != null && shield.HitTestShield(gridPos, hitRadius))
                    return shield;
            }

            return null;
        }

        /// <returns>Enumerates all active shields at grid pos</returns>
        [Pure] public IEnumerable<ShipModule> GetActiveShieldsAt(ShipModule[] modules, int gridPosX, int gridPosY)
        {
            short[] shields = GetShieldIndicesAt(gridPosX, gridPosY);
            if (shields == null) yield break;

            for (int i = 0; i < shields.Length; ++i)
            {
                ShipModule shield = GetActiveShield(modules, shields[i]);
                if (shield != null)
                    yield return shield;
            }
        }

        /// <summary>
        /// Enumerates all modules at Grid position (MUST BE IN BOUNDS!) using a generator
        /// 
        /// If checkShields, then this will enumerate all ACTIVE shields overlapping gridPos
        /// If there is an ACTIVE module under the slot, it will be returned last
        /// If there is no ACTIVE shields or modules overlapping gridPos, then no elements are yielded.
        /// </summary>
        public IEnumerable<ShipModule> GetModulesAt(ShipModule[] modules, Point gridPos, bool checkShields)
        {
            // hit test any shields at current slot
            if (checkShields)
            {
                short[] shields = GetShieldIndicesAt(gridPos.X, gridPos.Y);
                if (shields != null)
                {
                    for (int i = 0; i < shields.Length; ++i)
                    {
                        ShipModule shield = GetActiveShield(modules, shields[i]);
                        if (shield != null && shield.HitTestShield(gridPos, 8f))
                            yield return shield;
                    }
                }
            }

            ShipModule m = Get(modules, gridPos);
            if (m != null && m.Active)
                yield return m;
        }

        /// <returns>How many shields are potentially covering slot at grid pos</returns>
        [Pure] public int GetNumShieldsAt(int gridPosX, int gridPosY)
        {
            short[] shields = GetShieldIndicesAt(gridPosX, gridPosY);
            return shields?.Length ?? 0;
        }
    }
}
