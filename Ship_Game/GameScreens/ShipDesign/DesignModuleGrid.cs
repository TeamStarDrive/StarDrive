using System;
using System.Collections.Generic;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = Microsoft.Xna.Framework.Point;

namespace Ship_Game
{
    public class DesignModuleGrid
    {
        readonly ShipDesignScreen Screen;
        public readonly string Name;
        readonly SlotStruct[] Grid;
        readonly SlotStruct[] Slots;
        public readonly int Width;
        public readonly int Height;
        public readonly Vector2 WorldTopLeft; // top-left of the module grid in World coords
        public readonly Point GridCenter;

        // this constructs a [GridWidth][GridHeight] array of current hull
        public DesignModuleGrid(ShipDesignScreen screen, ShipDesign design)
            : this(screen, design.Name, design.BaseHull)
        {
        }

        public DesignModuleGrid(ShipDesignScreen screen, string name, ShipHull hull)
        {
            Screen = screen;
            Name = name;
            Width = hull.Size.X;
            Height = hull.Size.Y;
            GridCenter = hull.GridCenter;
            WorldTopLeft = GridCenter.Mul(-16f);

            Grid = new SlotStruct[Width * Height];
            Slots = new SlotStruct[hull.HullSlots.Length];
            for (int i = 0; i < hull.HullSlots.Length; ++i)
            {
                var slot = new SlotStruct(hull.HullSlots[i], GridCenter);
                Slots[i] = slot;
                Grid[slot.Pos.X + slot.Pos.Y * Width] = slot;
            }

        #if DEBUG
            ModuleGridUtils.DebugDumpGrid($"Debug/DesignModuleGrid/{Name}.HULL.txt",
                        Grid, Width, Height, ModuleGridUtils.DumpFormat.SlotStructEmptyHull);
        #endif
        }

        public void SaveDebugGrid()
        {
        #if DEBUG
            ModuleGridUtils.DebugDumpGrid($"Debug/DesignModuleGrid/{Name}.txt",
                        Grid, Width, Height, ModuleGridUtils.DumpFormat.SlotStruct);
        #endif
        }

        public IReadOnlyList<SlotStruct> SlotsList => Slots;

        /// NOTE: This is an adapter to unify ship stat calculation
        public Array<ShipModule> CopyModulesList()
        {
            var modules = new Array<ShipModule>();
            foreach (SlotStruct slot in Slots)
                if (slot.Module != null && slot.Parent == null)
                    modules.Add(slot.Module);
            return modules;
        }

        // Convert from GRID POS into WORLD coordinates
        public Vector2 GridPosToWorld(Point gridPos)
        {
            return WorldTopLeft + new Vector2(gridPos.X * 16f, gridPos.Y * 16f);
        }

        // Convert from WORLD coordinates to GridPos
        public Point WorldToGridPos(Vector2 worldPos)
        {
            var rounded = new Point((int)Math.Floor(worldPos.X / 16f),
                                    (int)Math.Floor(worldPos.Y / 16f));
            return new Point(rounded.X + GridCenter.X, rounded.Y + GridCenter.Y);
        }

        // Gets SlotStruct or null at the given Grid Pos
        public SlotStruct Get(Point gridPos)
        {
            if (gridPos.X < 0 || gridPos.Y < 0 || gridPos.X >= Width || gridPos.Y >= Height)
                return null; // out of bounds
            return Grid[gridPos.X + gridPos.Y * Width];
        }

        public bool Get(Point gridPos, out SlotStruct slot)
        {
            return (slot = Get(gridPos)) != null;
        }

        public bool IsEmptyDesign()
        {
            foreach (SlotStruct slot in Slots)
                if (slot.ModuleUID.NotEmpty() || slot.Parent != null)
                    return false;
            return true;
        }


        struct ModuleRect
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
        
        bool IsInBounds(int gridX, int gridY)
            => gridX >= 0 && gridY >= 0 && gridX < Width && gridY < Height;

        bool IsInBounds(ModuleRect r)
            => IsInBounds(r.X0, r.Y0) && IsInBounds(r.X1, r.Y1);

        ModuleRect GetModuleSpan(SlotStruct slot, int width, int height)
            => new ModuleRect(slot.Pos, width, height);


        #region Undo Redo

        enum ChangeType { Added, Removed }

        struct ChangedModule
        {
            public SlotStruct At;
            public ShipModule Module;
            public ChangeType Type;
        }

        readonly Array<Array<ChangedModule>> Undoable = new Array<Array<ChangedModule>>();
        readonly Array<Array<ChangedModule>> Redoable = new Array<Array<ChangedModule>>();

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
                if (change.Type == ChangeType.Removed)  PlaceModule(change.At, change.Module);
            }
            
            GameAudio.SmallServo();
            Redoable.Add(changes);
            Screen.OnDesignChanged();
        }

        public void Redo()
        {
            if (Redoable.IsEmpty)
                return;
            
            Array<ChangedModule> changes = Redoable.PopLast();

            // redo actions in original order
            foreach (ChangedModule change in changes)
            {
                if (change.Type == ChangeType.Added)   PlaceModule(change.At, change.Module);
                if (change.Type == ChangeType.Removed) RemoveModule(change.At, change.Module);
            }
            
            GameAudio.SmallServo();
            Undoable.Add(changes);
            Screen.OnDesignChanged();
        }

        void SaveAction(SlotStruct slot, ShipModule module, ChangeType type)
        {
            if (Undoable.IsEmpty)
                return; // do not save unless StartUndoableAction() was called

            Undoable.Last.Add(new ChangedModule
            {
                At = slot, Module = module, Type = type
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

                if (!CanBeReplaced(action1.Module, action2.Module) || action1.Type != ChangeType.Removed || action2.Type != ChangeType.Added) 
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


        static bool CanBeReplaced(ShipModule m, ShipModule by)
        {
            return m.XSize == by.XSize && m.YSize == by.YSize && m.Restrictions == by.Restrictions;
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

            if (module == null)
            {
                if (logFailure) Log.Warning("Design ShipModule was null");
                return false;
            }

            ModuleRect span = GetModuleSpan(slot, module.XSize, module.YSize);
            if (!IsInBounds(span))
            {
                if (logFailure) Log.Warning($"Design slot {span} was out of bounds");
                return false;
            }

            for (int x = span.X0; x <= span.X1; ++x)
            {
                for (int y = span.Y0; y <= span.Y1; ++y)
                {
                    SlotStruct target = Grid[x + y * Width];
                    if (target == null)
                    {
                        if (logFailure)
                            Log.Warning($"Design slot {{{x},{y}}} does not exist in ship design layout");
                        return false;
                    }
                    if (!target.CanSlotSupportModule(module))
                    {
                        if (logFailure)
                            Log.Warning($"Design slot {{{x},{y}}} ({target.HullRestrict}) cannot support module {module.UID} ({module.Restrictions})");
                        return false;
                    }
                }
            }

            return true;
        }

        void PlaceModule(SlotStruct slot, ShipModule newModule)
        {
            slot.ModuleUID = newModule.UID;
            slot.Module    = newModule;
            slot.Tex       = newModule.ModuleTexture;
            newModule.Pos = slot.Pos; // so it gets installed to the Ship correctly

            ModuleRect span = GetModuleSpan(slot, newModule.XSize, newModule.YSize);
            for (int x = span.X0; x <= span.X1; ++x)
            for (int y = span.Y0; y <= span.Y1; ++y)
            {
                SlotStruct target = Grid[x + y*Width];
                if (target != slot) target.Parent = slot;
            }
        }
        
        public void InstallModule(SlotStruct slot, ShipModule newModule)
        {
            ClearSlots(slot, newModule);
            PlaceModule(slot, newModule);
            SaveAction(slot, newModule, ChangeType.Added);
        }

        void RemoveModule(SlotStruct root, ShipModule module)
        {
            ModuleRect span = GetModuleSpan(root, module.XSize, module.YSize);
            for (int x = span.X0; x <= span.X1; ++x) 
            for (int y = span.Y0; y <= span.Y1; ++y)
                Grid[x + y*Width].Clear();
        }

        public void ClearSlots(SlotStruct slot, ShipModule forModule)
        {
            ModuleRect span = GetModuleSpan(slot, forModule.XSize, forModule.YSize);
            for (int x = span.X0; x <= span.X1; ++x)
            for (int y = span.Y0; y <= span.Y1; ++y)
            {
                SlotStruct root = Grid[x + y*Width].Root;
                if (root?.Module != null) // only clear module roots which have not been cleared yet
                {
                    ShipModule module = root.Module;
                    SaveAction(root, module, ChangeType.Removed);
                    RemoveModule(root, module);
                }
            }
        }

        #endregion
    }
}
