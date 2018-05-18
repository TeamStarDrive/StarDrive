using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    // @todo Make this generic enough so that `SlotStruct` is no longer needed
    public class DesignModuleGrid
    {
        private readonly SlotStruct[] Grid;
        private readonly SlotStruct[] Slots;
        private readonly int Width;
        private readonly int Height;
        private readonly Point Offset;
        private int NumPowerChecks;

        // this constructs a [GridWidth][GridHeight] array of current hull
        // and allows for quick lookup for neighbours
        public DesignModuleGrid(ModuleSlotData[] slotData, Vector2 slotOffset)
        {
            Slots = new SlotStruct[slotData.Length];
            for (int i = 0; i < slotData.Length; ++i)
                Slots[i] = new SlotStruct(slotData[i], slotOffset);

            Point min = Slots[0].Position;
            Point max = min;
            foreach (SlotStruct slot in Slots)
            {
                Point pos  = slot.Position;
                Point size = slot.ModuleSize;
                if (pos.X < min.X) min.X = pos.X;
                if (pos.Y < min.Y) min.Y = pos.Y;
                if (pos.X+size.X > max.X) max.X = pos.X+size.X;
                if (pos.Y+size.Y > max.Y) max.Y = pos.Y+size.Y;
            }

            int width  = max.X - min.X;
            int height = max.Y - min.Y;
            Width  = width  / 16;
            Height = height / 16;
            Offset = min;

            Grid = new SlotStruct[Width * Height];
            foreach (SlotStruct slot in Slots)
            {
                Point pt = ToGridPos(slot.Position);
                Grid[pt.X + pt.Y * Width] = slot;
            }
        }


        #region Grid Coordinate Utils

        public int SlotsCount => Slots.Length;
        public IReadOnlyList<SlotStruct> SlotsList => Slots;

        public Point ToGridPos(Point modulePos) => new Point((modulePos.X - Offset.X) / 16,
                                                             (modulePos.Y - Offset.Y) / 16);

        // Gets slotstruct or null at the given location
        // @note modulePos is in 16x coordinates
        public SlotStruct Get(Point modulePos)
        {
            Point pos = ToGridPos(modulePos);
            if (pos.X < 0 || pos.Y < 0 || pos.X >= Width || pos.Y >= Height)
                return null; // out of bounds
            return Grid[pos.X + pos.Y * Width];
        }

        public bool Get(Point modulePos, out SlotStruct slot)
        {
            return (slot = Get(modulePos)) != null;
        }

        private bool GetSlotAt(int gridX, int gridY, ShipModuleType type, out SlotStruct slot)
        {
            slot = Grid[gridX + gridY * Width];
            return slot?.Module?.ModuleType == type;
        }

        private bool SlotMatches(int gridX, int gridY, ShipModuleType type)
        {
            if (gridX < 0 || gridY < 0 || gridX >= Width || gridY >= Height)
                return false; // out of bounds
            return Grid[gridX + gridY * Width]?.Module?.ModuleType == type;
        }

        private void ClampGridCoords(ref int x0, ref int x1, ref int y0, ref int y1)
        {
            x0 = Math.Max(0, x0);
            y0 = Math.Max(0, y0);
            x1 = Math.Min(x1, Width  - 1);
            y1 = Math.Min(y1, Height - 1);
        }

        private void ModuleCoords(SlotStruct m, out int x0, out int x1, out int y0, out int y1)
        {
            x0 = (m.PQ.X - Offset.X)/16;
            y0 = (m.PQ.Y - Offset.Y)/16;
            x1 = x0 + m.Module.XSIZE - 1;
            y1 = y0 + m.Module.YSIZE - 1; 
        }

        public bool IsEmptyDesign()
        {
            foreach (SlotStruct slot in Slots)
                if (slot.ModuleUID.NotEmpty() || slot.Parent != null)
                    return false;
            return true;
        }

        #endregion


        public Texture2D GetConduitGraphicAt(SlotStruct ss)
        {
            Point ssPos = ToGridPos(ss.Position);
            var conduit = new Ship.ConduitGraphic();

            if (SlotMatches(ssPos.X - 1, ssPos.Y, ShipModuleType.PowerConduit)) conduit.AddGridPos(-1, 0); // Left
            if (SlotMatches(ssPos.X + 1, ssPos.Y, ShipModuleType.PowerConduit)) conduit.AddGridPos(+1, 0); // Right
            if (SlotMatches(ssPos.X, ssPos.Y - 1, ShipModuleType.PowerConduit)) conduit.AddGridPos(0, -1); // North
            if (SlotMatches(ssPos.X, ssPos.Y + 1, ShipModuleType.PowerConduit)) conduit.AddGridPos(0, +1); // South

            string graphic = conduit.GetGraphic();
            if (ss.Module.Powered)
                graphic = graphic + "_power";
            return ResourceManager.Texture(graphic);
        }


        #region Installing and Removing modules

        public void ClearDestinationSlots(SlotStruct slot, ShipModule newModule)
        {

        }

        public void InstallModule(SlotStruct slot, ShipModule newModule)
        {

        }

        #endregion


        #region Recalculate Power

        public void RecalculatePower()
        {
            Stopwatch sw = Stopwatch.StartNew();
            NumPowerChecks = 0;

            foreach (SlotStruct slot in Slots) // reset everything
            {
                slot.InPowerRadius    = false;
                slot.PowerChecked = false;
                if (slot.Module != null) slot.Module.Powered = false;
            }

            foreach (SlotStruct slot in Slots)
            {
                SlotStruct powerSource = slot.Module != null ? slot : slot.Parent;
                if (powerSource?.PowerChecked != false)
                    continue;

                ShipModule module = powerSource.Module;
                if (module == null || module.PowerRadius <= 0 || module.ModuleType == ShipModuleType.PowerConduit)
                    continue;

                DistributePowerFrom(powerSource);

                // only PowerPlants can power conduits
                if (module.ModuleType == ShipModuleType.PowerPlant)
                    ConnectPowerConduits(powerSource);
            }

            foreach (SlotStruct slot in Slots)
            {
                if (slot.InPowerRadius)
                {
                    // apply power to modules, but not to conduits
                    if (slot.Module != null && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        slot.Module.Powered = true;

                    // @todo Get rid of parent links
                    if (slot.Parent?.Module != null)
                        slot.Parent.Module.Powered = true;
                }
                else if (slot.Module != null && (slot.Module.AlwaysPowered || slot.Module.PowerDraw <= 0))
                {
                    slot.Module.Powered = true;
                }

                // for conduits we assign their conduit graphic instead
                if (slot.Module?.ModuleType == ShipModuleType.PowerConduit)
                    slot.Tex = GetConduitGraphicAt(slot);
            }

            double elapsed = sw.Elapsed.TotalMilliseconds;
            Log.Info($"RecalculatePower elapsed:{elapsed:G5}ms  modules:{Slots.Length}  totalchecks:{NumPowerChecks}");
        }

        #endregion


        #region Connect PowerConduits from powerplant using floodfill

        private void ConnectPowerConduits(SlotStruct powerPlant)
        {
            var open = new Array<SlotStruct>();
            GetNeighbouringConduits(powerPlant, open);

            while (open.NotEmpty) // floodfill through unpowered neighbouring conduits
            {
                SlotStruct conduit = open.PopLast();
                if (conduit.PowerChecked)
                    continue;
                DistributePowerFrom(conduit);
                GetNeighbouringConduits(conduit, open);
            }
        }

        private void GetNeighbouringConduits(SlotStruct source, Array<SlotStruct> open)
        {
            ModuleCoords(source, out int x0, out int x1, out int y0, out int y1);

            GetNeighbouringConduits(x0, x1, y0-1, y0-1, open); // Check North;
            GetNeighbouringConduits(x0, x1, y1+1, y1+1, open); // Check South;
            GetNeighbouringConduits(x0-1, x0-1, y0, y1, open); // Check West;
            GetNeighbouringConduits(x1+1, x1+1, y0, y1, open); // Check East;
        }

        private void GetNeighbouringConduits(int x0, int x1, int y0, int y1, Array<SlotStruct> open)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                SlotStruct m = Grid[x + y * Width];
                if (m != null && !m.PowerChecked && m.Module?.ModuleType == ShipModuleType.PowerConduit)
                    open.Add(m);
            }
        }
        #endregion


        #region Distribute power in radius of power source

        // set all modules in power range as InPowerRadius
        private void DistributePowerFrom(SlotStruct source)
        {
            source.PowerChecked   = true;
            source.InPowerRadius  = true;
            source.Module.Powered = true;
            int radius = source.Module.PowerRadius;

            ModuleCoords(source, out int x0, out int x1, out int y0, out int y1);

            SetInPowerRadius(x0, x1, y0-radius, y0-1); // Check North
            SetInPowerRadius(x0, x1, y1+1, y1+radius); // Check South
            SetInPowerRadius(x0-radius, x0-1, y0, y1); // Check West
            SetInPowerRadius(x1+1, x1+radius, y0, y1); // Check East

            SetInPowerRadius(x0-radius, x0-1, y0-radius, y0-1, x0, y0, radius); // Check NorthWest
            SetInPowerRadius(x1+1, x1+radius, y0-radius, y0-1, x1, y0, radius); // Check NorthEast
            SetInPowerRadius(x1+1, x1+radius, y1+1, y0+radius, x1, y1, radius); // Check SouthEast
            SetInPowerRadius(x0-radius, x0-1, y0-1, y0+radius, x0, y1, radius); // Check SouthWest
        }

        private void SetInPowerRadius(int x0, int x1, int y0, int y1)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                SlotStruct m = Grid[x + y*Width];
                if (m != null) m.InPowerRadius = true;
            }
        }

        private void SetInPowerRadius(int x0, int x1, int y0, int y1, int powerX, int powerY, int radius)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                int dx = Math.Abs(x - powerX);
                int dy = Math.Abs(y - powerY);
                if ((dx + dy) > radius) continue;
                SlotStruct m = Grid[x + y*Width];
                if (m != null) m.InPowerRadius = true;
            }
        }

        #endregion
    }
}
