using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public struct ShipGridInfo
    {
        public Point Size; // slot dimensions of the grid, for example 4x4 for Vulcan Scout
        public int SurfaceArea;

        public override string ToString() => $"surface={SurfaceArea} size={Size}";

        public ShipGridInfo(Point size, int surfaceArea)
        {
            Size = size;
            SurfaceArea = surfaceArea;
        }

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
        }

        public ShipGridInfo(DesignSlot[] slots)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            SurfaceArea = 0;

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot s = slots[i];
                SurfaceArea += s.Size.X * s.Size.Y;
                var botRight = new Point(s.Pos.X + s.Size.X,
                                         s.Pos.Y + s.Size.Y);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }
        }

        public ShipGridInfo(ShipModule[] modules)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            SurfaceArea = 0;

            for (int i = 0; i < modules.Length; ++i)
            {
                ShipModule m = modules[i];
                SurfaceArea += m.Area;
                var botRight = new Point(m.GridPos.X + m.XSIZE,
                                         m.GridPos.Y + m.YSIZE);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }
        }
    }
}