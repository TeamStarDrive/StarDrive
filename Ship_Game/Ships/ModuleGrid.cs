using System;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Ship_Game.Ships
{
    /// <summary>
    /// A generic 2D ModuleGrid for fast lookup
    /// </summary>
    /// <typeparam name="T">Any type with a `Vector2 Position;` field</typeparam>
    public class ModuleGrid<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        static readonly MethodInfo GetGridPosM;
        // ReSharper disable once StaticMemberInGenericType
        static readonly MethodInfo GetSizeM;

        static ModuleGrid()
        {
            Type type = typeof(T);

            BindingFlags anyMember = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            GetGridPosM = type.GetMethod("GetGridPos", anyMember);
            GetSizeM    = type.GetMethod("GetSize", anyMember);

            void Error(string message) => throw new Exception($"ModuleGrid<T> type T={type.GetTypeName()} {message}");

            if (GetGridPosM == null)
                Error("does not contain `GetGridPos` method");
            else if (GetGridPosM.ReturnType != typeof(Point))
                Error($"method `GetGridPos()` return {GetGridPosM.ReturnType} is not of type Point");

            if (GetSizeM == null)
                Error("does not contain `GetSize()` method");
            else if (GetSizeM.ReturnType != typeof(Point))
                Error($"method `GetSize()` return {GetSizeM.ReturnType} is not of type Point");
        }

        static Point GetGridPos(T module)
        {
            return (Point)GetGridPosM.Invoke(module, null);
        }

        static Point GetSize(T module)
        {
            return (Point)GetSizeM.Invoke(module, null);
        }

        public readonly int Width;
        public readonly int Height;
        readonly T[] Grid;

        /// <summary>
        /// Assumes modules have a Position field which divides them in a 2D grid order with no module overlaps
        ///
        /// The grid Span, Width, Height etc is provided by ShipGridInfo
        ///
        /// </summary>
        public ModuleGrid(in ShipGridInfo gridInfo, T[] modules)
        {
            Width = gridInfo.Size.X;
            Height = gridInfo.Size.Y;
            Grid = new T[Width * Height];
            Initialize(modules);
        }
        
        // Unsafe direct access to Grid using index locations
        public T this[int x, int y] => Grid[x + y * Width];
        public T this[Point point]  => Grid[point.X + point.Y * Width];

        // Safe access to Grid, returns false if point is outside the grid
        public bool Get(Point point, out T module)
        {
            if ((uint)point.X < Width && (uint)point.Y < Height)
            {
                module = Grid[point.X + point.Y * Width];
                return module != null; // the module may be null if nothing occupies the grid slot
            }
            module = default;
            return false;
        }

        void Initialize(T[] modules)
        {
            for (int i = 0; i < modules.Length; ++i)
            {
                T module = modules[i];
                Point pt = GetGridPos(module);
                Point size = GetSize(module);
                UpdateGridSlot(module, pt, size);
            }
        }

        void UpdateGridSlot(T module, Point pt, Point size)
        {
            int endX = pt.X + size.X;
            int endY = pt.Y + size.Y;

            #if DEBUG
            if (pt.X < 0 || Width < endX)
                throw new IndexOutOfRangeException($"ModuleGrid<T> X={pt.X} out of bounds [0..{Width})");
            if (pt.Y < 0 || Height < endY)
                throw new IndexOutOfRangeException($"ModuleGrid<T> Y={pt.Y} out of bounds [0..{Height})");
            #endif

            for (int y = pt.Y; y < endY; ++y)
            {
                for (int x = pt.X; x < endX; ++x)
                {
                    Grid[x + y * Width] = module;
                }
            }
        }
    }
}
