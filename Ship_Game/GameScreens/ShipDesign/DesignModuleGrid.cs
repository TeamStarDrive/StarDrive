using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
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
        
        public int SlotsCount => Slots.Length;
        public IReadOnlyList<SlotStruct> SlotsList => Slots;

        /// NOTE: This is an adapter to unify ship stat calculation
        public ShipModule[] CopyModulesList()
        {
            var modules = new Array<ShipModule>();
            foreach (SlotStruct slot in Slots)
                if (slot.Module != null)
                    modules.Add(slot.Module);
            return modules.ToArray();
        }

        #region Grid Coordinate Utils

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


        #region ModuleRect Bounds

        private struct ModuleRect
        {
            public int X0, X1; // inclusive span [X0, X1] eg [firstX, lastX]
            public int Y0, Y1; // inclusive span [Y0, Y1] eg [firstY, lastY]
            public ModuleRect(Point pos, int moduleWidth, int moduleHeight)
            {
                X0 = pos.X;
                Y0 = pos.Y;
                X1 = pos.X + (moduleWidth  - 1);
                Y1 = pos.Y + (moduleHeight - 1);
            }
            public override string ToString()
            {
                return $"X:{X0} Y:{Y0} W:{X1-X0+1} H: {Y1-Y0+1}";
            }
        }
        
        private bool IsInBounds(Point gridPoint)
            => gridPoint.X >= 0 && gridPoint.Y >= 0 && gridPoint.X < Width && gridPoint.Y < Height;

        private bool IsInBounds(int gridX, int gridY)
            => gridX >= 0 && gridY >= 0 && gridX < Width && gridY < Height;

        private bool IsInBounds(ModuleRect r)
            => IsInBounds(r.X0, r.Y0) && IsInBounds(r.X1, r.Y1);

        private ModuleRect GetModuleSpan(SlotStruct slot, int width, int height)
            => new ModuleRect(ToGridPos(slot.Position), width, height);

        #endregion


        #region Undo Redo

        private enum ChangeType { Added, Removed }

        private struct ChangedModule
        {
            public SlotStruct At;
            public ShipModule Module;
            public ModuleOrientation Orientation;
            public ChangeType Type;
        }

        private readonly Array<Array<ChangedModule>> Undoable = new Array<Array<ChangedModule>>();
        private readonly Array<Array<ChangedModule>> Redoable = new Array<Array<ChangedModule>>();

        public void StartUndoableAction()
        {
            if (Undoable.IsEmpty || !Undoable.Last.IsEmpty) // only start new if we actually need to
            {
                Undoable.Add(new Array<ChangedModule>());
                Redoable.Clear(); // once we start a new action, we can no longer redo old things
            }
        }

        public void Undo()
        {
            if (Undoable.IsEmpty)
                return;

            Array<ChangedModule> changes = Undoable.PopLast();

            // undo actions in reverse order
            for (int i = changes.Count-1; i >= 0; --i)
            {
                ChangedModule change = changes[i];
                if (change.Type == ChangeType.Added)   RemoveModule(change.At, change.Module);
                if (change.Type == ChangeType.Removed)  PlaceModule(change.At, change.Module, change.Orientation);
            }
            
            GameAudio.SmallServo();
            Redoable.Add(changes);
            RecalculatePower();
        }

        public void Redo()
        {
            if (Redoable.IsEmpty)
                return;
            
            Array<ChangedModule> changes = Redoable.PopLast();

            // redo actions in original order
            foreach (ChangedModule change in changes)
            {
                if (change.Type == ChangeType.Added)   PlaceModule(change.At, change.Module, change.Orientation);
                if (change.Type == ChangeType.Removed) RemoveModule(change.At, change.Module);
            }
            
            GameAudio.SmallServo();
            Undoable.Add(changes);
            RecalculatePower();
        }

        private void SaveAction(SlotStruct slot, ShipModule module, ModuleOrientation orientation, ChangeType type)
        {
            if (Undoable.IsEmpty)
                return; // do not save unless StartUndoableAction() was called

            Undoable.Last.Add(new ChangedModule
            {
                At = slot, Module = module, Orientation = orientation, Type = type
            });
        }

        /// <summary>
        /// Look in Undoable actions and see if there are 3 repeated actions,
        ///  meaning bulk replace could be handy
        /// </summary>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public bool RepeatedReplaceActionsThreshold(int threshold = 3)
        {
            ShipModule oldModule = null;
            ShipModule newModule = null;
            int counter          = 0;

            for (int i = Undoable.Count - 1; i >= 0; i--)
            {
                Array<ChangedModule> actions = Undoable[i];
                if (actions.Count < 2)
                    return false;

                ChangedModule action2 = actions[actions.Count - 1]; // Last Action
                ChangedModule action1 = actions[actions.Count - 2]; // Before Last Action

                if (!ReplaceAble(action1.Module, action2.Module) || action1.Type != ChangeType.Removed || action2.Type != ChangeType.Added) 
                    return false;

                if (i == Undoable.Count - 1) // First check
                {
                    oldModule = action1.Module;
                    newModule = action2.Module;
                    counter   = 1;
                }
                else
                {
                    if (oldModule?.UID == action1.Module.UID
                        && newModule?.UID == action2.Module.UID)
                    {
                        counter += 1;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (counter == threshold)
                    return true;
            }

            return false;
        }


        bool ReplaceAble(ShipModule module1, ShipModule module2)
        {
            return module1.XSIZE == module2.XSIZE 
                   && module1.YSIZE == module2.YSIZE 
                   && module1.Restrictions == module2.Restrictions;
        }


        #endregion

        #region Installing and Removing modules

        public bool ModuleFitsAtSlot(SlotStruct slot, ShipModule module, bool logFailure = false)
        {
            if (slot == null)
            {
                if (logFailure) Log.Warning("Design slot was null");
                return false;
            }

            ModuleRect span = GetModuleSpan(slot, module.XSIZE, module.YSIZE);
            if (!IsInBounds(span))
            {
                if (logFailure) Log.Warning($"Design slot {span} was out of bounds");
                return false;
            }

            for (int x = span.X0; x <= span.X1; ++x) 
            for (int y = span.Y0; y <= span.Y1; ++y)
            {
                SlotStruct target = Grid[x + y*Width];
                if (target == null)
                {
                    if (logFailure) Log.Warning($"Design slot {{{x},{y}}} does not exist in ship design layout");
                    return false;
                }
                if (!target.CanSlotSupportModule(module))
                {
                    if (logFailure)
                        Log.Warning($"Design slot {{{x},{y}}} ({target.Restrictions}) cannot support module {module.UID} ({module.Restrictions})");
                    return false;
                }
            }
            return true;
        }

        private void PlaceModule(SlotStruct slot, ShipModule newModule, ModuleOrientation orientation)
        {
            slot.ModuleUID   = newModule.UID;
            slot.Module      = newModule;
            slot.Orientation = orientation;
            slot.Facing      = newModule.FacingDegrees;
            slot.Tex         = newModule.ModuleTexture;
            slot.Module.SetAttributes();

            ModuleRect span = GetModuleSpan(slot, newModule.XSIZE, newModule.YSIZE);
            for (int x = span.X0; x <= span.X1; ++x)
            for (int y = span.Y0; y <= span.Y1; ++y)
            {
                SlotStruct target = Grid[x + y*Width];
                if (target != slot) target.Parent = slot;
            }
        }
        
        public void InstallModule(SlotStruct slot, ShipModule newModule, ModuleOrientation orientation)
        {
            ClearSlots(slot, newModule);
            PlaceModule(slot, newModule, orientation);
            SaveAction(slot, newModule, orientation, ChangeType.Added);
        }

        private void RemoveModule(SlotStruct root, ShipModule module)
        {
            ModuleRect span = GetModuleSpan(root, module.XSIZE, module.YSIZE);
            for (int x = span.X0; x <= span.X1; ++x) 
            for (int y = span.Y0; y <= span.Y1; ++y)
                Grid[x + y*Width].Clear();
        }

        public void ClearSlots(SlotStruct slot, ShipModule forModule)
        {
            ModuleRect span = GetModuleSpan(slot, forModule.XSIZE, forModule.YSIZE);
            for (int x = span.X0; x <= span.X1; ++x)
            for (int y = span.Y0; y <= span.Y1; ++y)
            {
                SlotStruct root = Grid[x + y*Width].Root;
                if (root?.Module != null) // only clear module roots which have not been cleared yet
                {
                    SaveAction(root, root.Module, root.Orientation, ChangeType.Removed);
                    RemoveModule(root, root.Module);
                }
            }
        }

        #endregion


        #region Recalculate Power

        public void RecalculatePower()
        {
            Stopwatch sw = Stopwatch.StartNew();
            NumPowerChecks = 0;

            foreach (SlotStruct slot in Slots) // reset everything
            {
                slot.InPowerRadius = false;
                slot.PowerChecked  = false;
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
                if (module.Is(ShipModuleType.PowerPlant))
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
        
        public SubTexture GetConduitGraphicAt(SlotStruct ss)
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
            SetInPowerRadius(x1+1, x1+radius, y1+1, y1+radius, x1, y1, radius); // Check SouthEast
            SetInPowerRadius(x0-radius, x0-1, y1+1, y1+radius, x0, y1, radius); // Check SouthWest
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
