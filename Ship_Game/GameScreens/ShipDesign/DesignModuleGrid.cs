﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    // @todo Make this generic enough so that `SlotStruct` is no longer needed
    public class DesignModuleGrid
    {
        public readonly ShipData Hull;
        readonly SlotStruct[] Grid;
        readonly SlotStruct[] Slots;
        readonly int Width;
        readonly int Height;

        public Action OnGridChanged;

        // this constructs a [GridWidth][GridHeight] array of current hull
        // and allows for quick lookup for neighbours
        public DesignModuleGrid(ShipData design)
        {
            Hull = new ShipData(design);

            ShipHull hull = Hull.BaseHull;
            Width = hull.Size.X;
            Height = hull.Size.Y;

            Grid = new SlotStruct[Width * Height];
            Slots = new SlotStruct[hull.HullSlots.Length];
            for (int i = 0; i < hull.HullSlots.Length; ++i)
            {
                var slot = new SlotStruct(hull.HullSlots[i]);
                Slots[i] = slot;
                Point pt = ToGridPos(slot.Position);
                Grid[pt.X + pt.Y * Width] = slot;
            }

        #if DEBUG
            ModuleGridUtils.DebugDumpGrid($"Debug/DesignModuleGrid/{Hull.Name}.HULL.txt",
                        Grid, Width, Height, ModuleGridUtils.DumpFormat.SlotStructEmptyHull);
        #endif
        }

        public DesignModuleGrid(ShipHull hull, Vector2 slotOffset)
        {
            throw new NotImplementedException();
        }

        public void SaveDebugGrid()
        {
        #if DEBUG
            ModuleGridUtils.DebugDumpGrid($"Debug/DesignModuleGrid/{Hull.Name}.txt",
                        Grid, Width, Height, ModuleGridUtils.DumpFormat.SlotStruct);
        #endif
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

        public bool IsEmptyDesign()
        {
            foreach (SlotStruct slot in Slots)
                if (slot.ModuleUID.NotEmpty() || slot.Parent != null)
                    return false;
            return true;
        }

        #endregion


        #region ModuleRect Bounds

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
            => new ModuleRect(ToGridPos(slot.Position), width, height);

        #endregion


        #region Undo Redo

        enum ChangeType { Added, Removed }

        struct ChangedModule
        {
            public SlotStruct At;
            public ShipModule Module;
            public ModuleOrientation Orientation;
            public ChangeType Type;
        }

        readonly Array<Array<ChangedModule>> Undoable = new Array<Array<ChangedModule>>();
        readonly Array<Array<ChangedModule>> Redoable = new Array<Array<ChangedModule>>();

        // Should be called to trigger OnGridChanged event
        public void OnModuleGridChanged()
        {
            OnGridChanged?.Invoke();
        }

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
            OnModuleGridChanged();
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
            OnModuleGridChanged();
        }

        void SaveAction(SlotStruct slot, ShipModule module, ModuleOrientation orientation, ChangeType type)
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
                            Log.Warning($"Design slot {{{x},{y}}} ({target.Restrictions}) cannot support module {module.UID} ({module.Restrictions})");
                        return false;
                    }
                }

            return true;
        }

        void PlaceModule(SlotStruct slot, ShipModule newModule, ModuleOrientation orientation)
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

        void RemoveModule(SlotStruct root, ShipModule module)
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
    }
}
