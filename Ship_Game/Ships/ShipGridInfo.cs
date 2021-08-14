using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public struct ShipGridInfo
    {
        // slot dimensions of the grid, for example 4x4 for Vulcan Scout
        public Point Size;
        // grid origin, which should match BaseHull. If it doesn't then slots need adjustment
        public Point Origin;
        public int SurfaceArea;

        public override string ToString() => $"Size={Size} Slots={SurfaceArea}";

        // This is used for ACTUAL ShipData
        public ShipGridInfo(ShipHull hull)
        {
            Size = hull.Size;
            Origin = hull.GridOrigin;
            SurfaceArea = hull.SurfaceArea;
        }

        // This is used for TESTING
        public ShipGridInfo(HullSlot[] slots)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            SurfaceArea = slots.Length;

            for (int i = 0; i < slots.Length; ++i)
            {
                HullSlot s = slots[i];
                var botRight = new Point(s.Pos.X + 1, s.Pos.Y + 1);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }

            Origin = new Point(-Size.X / 2, -Size.Y / 2);
        }

        // This is used for DEBUGGING and TESTING
        public ShipGridInfo(ShipModule[] modules)
        {
            Size = Point.Zero; // [0,0] is always the top-left
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

            Origin = new Point(-Size.X / 2, -Size.Y / 2);
        }
    }
}