using System;
using Ship_Game.Gameplay;
using Point = SDGraphics.Point;

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

        // This is used for HULL EDITING
        public static (Point NewSize, Point NewTopLeft) GetEditedHullInfo(HullSlot[] slots)
        {
            Point max = Point.Zero;
            for (int i = 0; i < slots.Length; ++i)
            {
                HullSlot s = slots[i];
                var botRight = new Point(s.Pos.X + 1, s.Pos.Y + 1);
                if (max.X < botRight.X) max.X = botRight.X;
                if (max.Y < botRight.Y) max.Y = botRight.Y;
            }

            Point min = max;
            for (int i = 0; i < slots.Length; ++i)
            {
                HullSlot s = slots[i];
                if (s.Pos.X < min.X) min.X = s.Pos.X;
                if (s.Pos.Y < min.Y) min.Y = s.Pos.Y;
            }

            return (NewSize:max.Sub(min), NewTopLeft:min);
        }

        // This is used for HULL EDITING and TESTING
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

            Center = new Point(Size.X / 2, Size.Y / 2);
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
                var botRight = new Point(m.Pos.X + m.XSize,
                                         m.Pos.Y + m.YSize);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }
        }

        // This is used for DEBUGGING and TESTING
        public ShipGridInfo(DesignSlot[] slots)
        {
            Size = Point.Zero; // [0,0] is always the top-left
            SurfaceArea = 0;

            for (int i = 0; i < slots.Length; ++i)
            {
                DesignSlot m = slots[i];
                SurfaceArea += m.Size.X * m.Size.Y;
                var botRight = new Point(m.Pos.X + m.Size.X,
                                         m.Pos.Y + m.Size.Y);
                if (Size.X < botRight.X) Size.X = botRight.X;
                if (Size.Y < botRight.Y) Size.Y = botRight.Y;
            }

            Center = new Point(Size.X / 2, Size.Y / 2);
        }
    }
}