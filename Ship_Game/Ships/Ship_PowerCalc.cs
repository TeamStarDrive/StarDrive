using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Xna.Framework;
using Ship_Game.Utils;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        bool ShouldRecalculatePower;

        public void RecalculatePower()
        {
            ShouldRecalculatePower = false;
            PwrGrid.Recalculate(Modules);
        }

        public struct ConduitGraphic
        {
            public bool Right;
            public bool Left;
            public bool Down;
            public bool Up;
            public void Add(int dx, int dy)
            {
                AddGridPos(dx / 16, dy / 16);
            }
            public void AddGridPos(int dx, int dy)
            {
                Left  |= dx == -1 && dy == 0;
                Right |= dx == +1 && dy == 0;
                Down  |= dx ==  0 && dy == -1;
                Up    |= dx ==  0 && dy == +1;
            }
            public int Sides => (Left?1:0) + (Right?1:0) + (Down?1:0) + (Up?1:0);
            public string GetGraphic()
            {
                switch (Sides)
                {
                    case 1:
                        if (Down)  return "Conduits/conduit_powerpoint_down";
                        if (Up)    return "Conduits/conduit_powerpoint_up";
                        if (Left)  return "Conduits/conduit_powerpoint_right";
                        if (Right) return "Conduits/conduit_powerpoint_left";
                        break;
                    case 2:
                        if (Left && Down)  return "Conduits/conduit_corner_BR";
                        if (Left && Up)    return "Conduits/conduit_corner_TR";
                        if (Right && Down) return "Conduits/conduit_corner_BL";
                        if (Right && Up)   return "Conduits/conduit_corner_TL";
                        if (Down && Up)    return "Conduits/conduit_straight_vertical";
                        if (Left && Right) return "Conduits/conduit_straight_horizontal";
                        break;
                    case 3:
                        if (!Right)  return "Conduits/conduit_tsection_left";
                        if (!Left)   return "Conduits/conduit_tsection_right";
                        if (!Down)   return "Conduits/conduit_tsection_down";
                        if (!Up)     return "Conduits/conduit_tsection_up";
                        break;
                }
                return "Conduits/conduit_intersection";
            }
        }

        // This is a 2D representation of the ship's slot structs
        public struct PowerGrid
        {
            BitArray PwrGrid; // a grid of powered slots, width*height
            BitArray Checked; // grid of slots which have already been checked
            readonly ShipModule[] ModuleGrid;
            readonly int Width;
            readonly int Height;

            public PowerGrid(ShipModule[] grid, int gridWidth, int gridHeight)
            {
                ModuleGrid = grid;
                PwrGrid = new BitArray(gridWidth * gridHeight);
                Checked = new BitArray(gridWidth * gridHeight);
                Width = gridWidth;
                Height = gridHeight;
            }

            // whether a module has already been power-checked
            [Pure] public bool IsChecked(int x, int y)
            {
                return Checked.IsSet(x + y*Width);
            }

            void SetChecked(int x, int y)
            {
                Checked.Set(x + y*Width);
            }

            public void PrintPwrGrid() => PrintGrid(PwrGrid);
            void PrintGrid(in BitArray bits)
            {
                var sb = new StringBuilder();
                for (int y = 0; y < Height; ++y)
                {
                    for (int x = 0; x < Width; ++x)
                    {
                        sb.Append(bits.IsSet(x + y*Width) ? '+' : '0');
                    }
                    sb.Append(" \n");
                }
                Log.Write(sb.ToString());
            }

            // check if this 1x1 slot at [x,y] is powered
            [Pure] public bool IsPowered(Point gridPos)
            {
                return PwrGrid.IsSet(gridPos.X + gridPos.Y*Width);
            }
            
            // checks if this module is powered
            [Pure] public bool IsPowered(ShipModule m)
            {
                // we only need to check top-left, because SetPowered already fills the grid under it
                Point pt = m.Pos;
                return PwrGrid.IsSet(pt.X + pt.Y*Width);
            }

            void SetPowered(int x0, int y0)
            {
                int gridIndex = x0 + y0*Width;
                if (PwrGrid.IsSet(gridIndex))
                    return; // already powered

                // we need to find any underlying module and set all of it as powered
                ShipModule m = ModuleGrid[gridIndex];
                if (m != null)
                {
                    // fill everything under this module, so we don't need to check this area again
                    Point pt = m.Pos;
                    int x1 = pt.X + m.XSIZE - 1;
                    int y1 = pt.Y + m.YSIZE - 1;
                    for (int y = pt.Y; y <= y1; ++y)
                        for (int x = pt.X; x <= x1; ++x)
                            PwrGrid.Set(x + y * Width);
                }
                else // there's no module here, only set the slot
                {
                    PwrGrid.Set(gridIndex);
                }
            }


            bool SlotMatches(int gridX, int gridY, ShipModuleType type)
            {
                if (gridX < 0 || gridY < 0 || gridX >= Width || gridY >= Height)
                    return false; // out of bounds
                return ModuleGrid[gridX + gridY * Width]?.ModuleType == type;
            }
        
            // called during ship initialize to give the correct shape to the conduit
            public string GetConduitGraphic(ShipModule forModule)
            {
                Point ssPos = forModule.Pos;
                var conduit = new ConduitGraphic();

                if (SlotMatches(ssPos.X - 1, ssPos.Y, ShipModuleType.PowerConduit)) conduit.AddGridPos(-1, 0); // Left
                if (SlotMatches(ssPos.X + 1, ssPos.Y, ShipModuleType.PowerConduit)) conduit.AddGridPos(+1, 0); // Right
                if (SlotMatches(ssPos.X, ssPos.Y - 1, ShipModuleType.PowerConduit)) conduit.AddGridPos(0, -1); // North
                if (SlotMatches(ssPos.X, ssPos.Y + 1, ShipModuleType.PowerConduit)) conduit.AddGridPos(0, +1); // South

                return conduit.GetGraphic();
            }

            public void Recalculate(ShipModule[] modules)
            {
                PwrGrid.Clear(); // clear all current status
                Checked.Clear();
                var open = new Array<Point>(); // used as a fast buffer

                // distribute power from all PowerPlants
                for (int i = 0; i < modules.Length; ++i)
                {
                    ShipModule m = modules[i];
                    Point pt = m.Pos;
                    if (!IsChecked(pt.X, pt.Y))
                    {
                        if (m.PowerRadius > 0 && m.ModuleType != ShipModuleType.PowerConduit)
                        {
                            DistributePowerFrom(m, pt.X, pt.Y);

                            // only PowerPlants can power conduits
                            if (m.Is(ShipModuleType.PowerPlant))
                                ConnectPowerConduits(m, pt.X, pt.Y, open);
                        }
                    }
                }

                // apply power to modules
                for (int i = 0; i < modules.Length; ++i)
                {
                    ShipModule m = modules[i];
                    if (IsPowered(m))
                    {
                        // apply power to modules, but not to conduits
                        if (m.ModuleType != ShipModuleType.PowerConduit)
                            m.Powered = true;
                    }
                    else if (m.AlwaysPowered || m.PowerDraw <= 0)
                    {
                        m.Powered = true;
                    }
                    else // all else: the module is not powered
                    {
                        m.Powered = false;
                    }
                }
            }

            void ConnectPowerConduits(ShipModule m, int mX, int mY, Array<Point> open)
            {
                open.Clear();
                GetNeighbouringConduits(m, mX, mY, open);

                 // floodfill through unpowered neighbouring conduits
                while (open.NotEmpty)
                {
                    Point cp = open.PopLast();
                    if (!IsChecked(cp.X, cp.Y))
                    {
                        ShipModule conduit = ModuleGrid[cp.X + cp.Y*Width];
                        DistributePowerFrom(conduit, cp.X, cp.Y);
                        GetNeighbouringConduits(conduit, cp.X, cp.Y, open);
                    }
                }
            }

            void GetNeighbouringConduits(ShipModule m, int x0, int y0, Array<Point> open)
            {
                int x1 = x0 + m.XSIZE - 1;
                int y1 = y0 + m.YSIZE - 1;
                GetNeighbouringConduits(x0, x1, y0-1, y0-1, open); // Check North;
                GetNeighbouringConduits(x0, x1, y1+1, y1+1, open); // Check South;
                GetNeighbouringConduits(x0-1, x0-1, y0, y1, open); // Check West;
                GetNeighbouringConduits(x1+1, x1+1, y0, y1, open); // Check East;
            }

            void GetNeighbouringConduits(int x0, int x1, int y0, int y1, Array<Point> open)
            {
                ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
                for (int y = y0; y <= y1; ++y)
                for (int x = x0; x <= x1; ++x)
                {
                    ShipModule m = ModuleGrid[x + y * Width];
                    if (m != null) // if there is a module at this point
                    {
                        if (!IsChecked(x, y) && m.ModuleType == ShipModuleType.PowerConduit)
                            open.Add(new Point(x, y));
                    }
                }
            }

            // set all modules in power range as InPowerRadius
            void DistributePowerFrom(ShipModule m, int x0, int y0)
            {
                m.Powered = true; // if we are distributing power, then this module is powered
                int radius = m.PowerRadius;
                int x1 = x0 + m.XSIZE - 1;
                int y1 = y0 + m.YSIZE - 1;

                SetChecked(x0, y0); // make sure we don't visit it again
                SetPowered(x0, y0); // these slots are entirely POWERED

                SetInPowerRadius(x0, x1, y0-radius, y0-1); // Check North
                SetInPowerRadius(x0, x1, y1+1, y1+radius); // Check South
                SetInPowerRadius(x0-radius, x0-1, y0, y1); // Check West
                SetInPowerRadius(x1+1, x1+radius, y0, y1); // Check East

                SetInPowerRadius(x0-radius, x0-1, y0-radius, y0-1, x0, y0, radius); // Check NorthWest
                SetInPowerRadius(x1+1, x1+radius, y0-radius, y0-1, x1, y0, radius); // Check NorthEast
                SetInPowerRadius(x1+1, x1+radius, y1+1, y1+radius, x1, y1, radius); // Check SouthEast
                SetInPowerRadius(x0-radius, x0-1, y1+1, y1+radius, x0, y1, radius); // Check SouthWest
            }

            void SetInPowerRadius(int x0, int x1, int y0, int y1) // fill entire area
            {
                ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
                for (int y = y0; y <= y1; ++y)
                for (int x = x0; x <= x1; ++x)
                    SetPowered(x, y);
            }

            void SetInPowerRadius(int x0, int x1, int y0, int y1, int powerX, int powerY, int radius) // additional radius check
            {
                ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
                for (int y = y0; y <= y1; ++y)
                for (int x = x0; x <= x1; ++x)
                {
                    int dx = Math.Abs(x - powerX);
                    int dy = Math.Abs(y - powerY);
                    if ((dx + dy) <= radius) // Manhattan distance
                        SetPowered(x, y);
                }
            }

            void ClampGridCoords(ref int x0, ref int x1, ref int y0, ref int y1)
            {
                x0 = Math.Max(0, x0);
                y0 = Math.Max(0, y0);
                x1 = Math.Min(x1, Width  - 1);
                y1 = Math.Min(y1, Height - 1);
            }
        }
    }
}
