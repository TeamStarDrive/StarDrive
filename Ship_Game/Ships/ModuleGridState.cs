using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Point = Microsoft.Xna.Framework.Point;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This is the stateful part of a Ship's module grid
    /// </summary>
    public struct ModuleGridState
    {
        public ModuleGridFlyweight Grid;
        public ShipModule[] Modules;
        public int Width => Grid.Width;
        public int Height => Grid.Height;

        public ModuleGridState(ModuleGridFlyweight grid, ShipModule[] modules)
        {
            Grid = grid;
            Modules = modules;
        }

        [Pure] public bool Get(int x, int y, out ShipModule m) => Grid.Get(Modules, x, y, out m);
        [Pure] public ShipModule this[int x, int y]   => Grid.Get(Modules, x, y);
        [Pure] public ShipModule Get(int x, int y)    => Grid.Get(Modules, x, y);
        [Pure] public ShipModule Get(Point gridPoint) => Grid.Get(Modules, gridPoint.X, gridPoint.Y);
        [Pure] public ShipModule Get(int gridIndex)   => Grid.Get(Modules, gridIndex);
    }
}
