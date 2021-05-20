using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    /// <summary>
    /// A generic 2D ModuleGrid for fast lookup
    /// </summary>
    /// <typeparam name="T">Any type with a `Vector2 Position;` field</typeparam>
    public class ModuleGrid<T> where T : class
    {
        // ReSharper disable once StaticMemberInGenericType
        static readonly FieldInfo PositionField;
        // ReSharper disable once StaticMemberInGenericType
        static readonly MethodInfo GetSizeMethod;

        static ModuleGrid()
        {
            Type type = typeof(T);
            PositionField = type.GetField("Position", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            GetSizeMethod = type.GetMethod("GetSize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (PositionField == null)
                throw new Exception($"ModuleGrid<T> type T={type.GetTypeName()} does not contain `Position` field");
            if (PositionField.FieldType != typeof(Vector2))
                throw new Exception($"ModuleGrid<T> type T={type.GetTypeName()} field `Position` is not of type Vector2");

            if (GetSizeMethod == null)
                throw new Exception($"ModuleGrid<T> type T={type.GetTypeName()} does not contain `GetSize()` method");
            if (GetSizeMethod.ReturnType != typeof(Point))
                throw new Exception($"ModuleGrid<T> type T={type.GetTypeName()} method `GetSize()` return is not of type Point");
        }

        static Vector2 GetPosition(T module)
        {
            return (Vector2)PositionField.GetValue(module);
        }

        static Point GetSize(T module)
        {
            return (Point)GetSizeMethod.Invoke(module, null);
        }

        public readonly int Width;
        public readonly int Height;
        public readonly Vector2 Origin; // TopLeft boundary of the module grid
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
            Origin = gridInfo.Origin;
            Grid = new T[Width * Height];

            bool legacyModuleSlotOffset = typeof(T) == typeof(ModuleSlotData);
            Initialize(modules, legacyModuleSlotOffset);
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
            module = null;
            return false;
        }

        public Point ToGridPos(Vector2 positionInGrid)
        {
            Vector2 offset = positionInGrid - Origin;
            return new Point((int)Math.Floor(offset.X / 16f),
                             (int)Math.Floor(offset.Y / 16f));
        }

        void Initialize(T[] modules, bool legacyModuleSlotOffset)
        {
            for (int i = 0; i < modules.Length; ++i)
            {
                T module = modules[i];
                Vector2 position = GetPosition(module);
                if (legacyModuleSlotOffset)
                {
                    position.X -= ShipModule.ModuleSlotOffset;
                    position.Y -= ShipModule.ModuleSlotOffset;
                }
                Point pt = ToGridPos(position);
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
