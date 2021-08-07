using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public partial class ShipDesignScreen
    {
        ModuleOrientation GetMirroredOrientation(ModuleOrientation orientation)
        {
            if (orientation == ModuleOrientation.Left) return ModuleOrientation.Right;
            if (orientation == ModuleOrientation.Right) return ModuleOrientation.Left;
            return orientation;
        }

        int GetMirroredTurretAngle(ModuleOrientation orientation, int turretAngle)
        {
            // TODO: Check these
            if (orientation == ModuleOrientation.Left) return turretAngle - 180;
            if (orientation == ModuleOrientation.Right) return turretAngle + 180;
            return turretAngle;
        }

        bool GetMirrorProjectedSlot(SlotStruct slot, int xSize, ModuleOrientation orientation, out SlotStruct projectedMirror)
        {
            if (GetMirrorSlot(slot, xSize, orientation, out MirrorSlot mirrored))
            {
                projectedMirror = mirrored.Slot;
                return true;
            }

            projectedMirror = default;
            return false;
        }

        Point GetMirrorGridPos(SlotStruct slot)
        {
            int offset = slot.Pos.X - slot.GridCenter.X;
            // (-offset - 1) is needed because Right starts at X:0f, Left side at X:-16f
            return new Point(slot.GridCenter.X + (-offset - 1), slot.Pos.Y);
        }

        Vector2 GetMirrorWorldPos(Vector2 moduleWorldPos)
        {
            // (-offset - 1) is needed because Right starts at X:0f, Left side at X:-16f
            return new Vector2(-moduleWorldPos.X - 16f, moduleWorldPos.Y);
        }

        bool MirroredModulesTooClose(Point mirrorPos, Point activeModulePos, int moduleSizeX)
        {
            return Math.Abs(activeModulePos.X - mirrorPos.X) <= (moduleSizeX - 1);
        }

        bool MirroredModulesTooClose(Vector2 mirrorWorldPos, Vector2 moduleWorldPos, Vector2 moduleWorldSize)
        {
            return Math.Abs(moduleWorldPos.X - mirrorWorldPos.X) <= (moduleWorldSize.X - 16f);
        }

        bool GetMirrorSlot(SlotStruct slot, int xSize, ModuleOrientation orientation, out MirrorSlot mirrored)
        {
            Point mirrorPos = GetMirrorGridPos(slot);

            if (ModuleGrid.Get(mirrorPos, out SlotStruct mirrorSS) &&
                !MirroredModulesTooClose(mirrorPos, slot.Pos, xSize) && 
                slot.Root != mirrorSS.Root) // !overlapping
            {
                int turretAngle = slot.Root.Module != null ? slot.Root.Module.TurretAngle : 0;
                mirrored = new MirrorSlot
                {
                    Slot = mirrorSS,
                    ModuleRot = GetMirroredOrientation(orientation),
                    TurretAngle = GetMirroredTurretAngle(orientation, turretAngle)
                };
                return true;
            }

            mirrored = default;
            return false;
        }

        bool GetMirrorSlotStruct(SlotStruct slot, out SlotStruct mirrored)
        {
            SlotStruct root = slot.Root;
            if (GetMirrorSlot(root, root.Module.XSIZE, root.ModuleRot, out MirrorSlot ms))
            {
                if (ms.Slot?.Module != null)
                {
                    mirrored = ms.Slot;
                    return true;
                }
            }
            mirrored = null;
            return false;
        }

        bool GetMirrorModule(SlotStruct slot, out ShipModule module)
        {
            if (GetMirrorSlotStruct(slot, out SlotStruct mirrored))
            {
                module = mirrored.Root.Module;
                if (module != null
                    && module.UID == slot.Module.UID
                    && module.XSIZE == slot.Module.XSIZE
                    && module.YSIZE == slot.Module.YSIZE)
                    return true;
            }
            module = null;
            return false;
        }
    }
}
