using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public struct ShipGridInfo
    {
        // slot dimensions of the grid, for example 4x4 for Vulcan Scout
        public Point Size;
        // offset from grid TopLeft to the Center slot
        // this should match BaseHull. If it doesn't then slots need adjustment
        public Point Center;
        public int SurfaceArea;

        public override string ToString() => $"Size={Size} Slots={SurfaceArea}";

        // This is used for ACTUAL ShipData
        public ShipGridInfo(ShipHull hull)
        {
            Size = hull.Size;
            Center = hull.GridCenter;
            SurfaceArea = hull.SurfaceArea;
        }

        // This is used for TESTING
        public ShipGridInfo(HullSlot[] slots)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            Center = Point.Zero; // NOTE: we don't need this in Tests currently
            SurfaceArea = slots.Length;

            for (int i = 0; i < slots.Length; ++i)
            {
                HullSlot s = slots[i];
                var botRight = new Point(s.Pos.X + 1, s.Pos.Y + 1);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }
        }

        // This is used for DEBUGGING and TESTING
        public ShipGridInfo(ShipModule[] modules)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            Center = Point.Zero; // NOTE: we don't need this in Tests currently
            SurfaceArea = 0;

            for (int i = 0; i < modules.Length; ++i)
            {
                ShipModule m = modules[i];
                SurfaceArea += m.Area;
                var botRight = new Point(m.Pos.X + m.XSIZE,
                                         m.Pos.Y + m.YSIZE);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }
        }
    }
}