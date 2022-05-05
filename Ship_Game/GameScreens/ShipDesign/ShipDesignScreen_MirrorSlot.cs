using System;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = Microsoft.Xna.Framework.Point;

namespace Ship_Game
{
    public partial class ShipDesignScreen
    {
        static ModuleOrientation GetMirroredOrientation(ModuleOrientation orientation)
        {
            if (orientation == ModuleOrientation.Left) return ModuleOrientation.Right;
            if (orientation == ModuleOrientation.Right) return ModuleOrientation.Left;
            return orientation;
        }

        static int GetMirroredTurretAngle(int turretAngle)
        {
            if (turretAngle == 0 || turretAngle == 180)
                return turretAngle;
            return 360 - turretAngle;
        }

        static Point GetMirrorGridPos(SlotStruct slot, int xSize)
        {
            int offsetFromCenter = slot.Pos.X - slot.GridCenter.X;
            // (-offset - 1) is needed because Right starts at X:0f, Left side at X:-16f
            return new Point(slot.GridCenter.X + (-offsetFromCenter - xSize), slot.Pos.Y);
        }

        static Vector2 GetMirrorWorldPos(Vector2 moduleWorldPos, Vector2 moduleWorldSize)
        {
            // (-offset - 1) is needed because Right starts at X:0f, Left side at X:-16f
            return new Vector2(-moduleWorldPos.X - moduleWorldSize.X, moduleWorldPos.Y);
        }

        static bool MirroredModulesTooClose(Point mirrorPos, Point activeModulePos, int moduleSizeX)
        {
            int offsetX = Math.Abs(activeModulePos.X - mirrorPos.X);
            return offsetX < moduleSizeX;
        }

        static bool MirroredModulesTooClose(Vector2 mirrorWorldPos, Vector2 moduleWorldPos, Vector2 moduleWorldSize)
        {
            float offsetX = Math.Abs(moduleWorldPos.X - mirrorWorldPos.X);
            return offsetX < (moduleWorldSize.X - 16f);
        }

        bool GetMirrorSlot(SlotStruct slot, ShipModule forModule, out MirrorSlot mirrored)
        {
            Point mirrorPos = GetMirrorGridPos(slot, forModule.XSize);

            if (ModuleGrid.Get(mirrorPos, out SlotStruct mirrorSS) &&
                !MirroredModulesTooClose(mirrorPos, slot.Pos, forModule.XSize) && 
                slot.Root != mirrorSS.Root) // !overlapping
            {
                mirrored = new MirrorSlot
                {
                    Slot = mirrorSS,
                    ModuleRot = GetMirroredOrientation(forModule.ModuleRot),
                    TurretAngle = GetMirroredTurretAngle(forModule.TurretAngle)
                };
                return true;
            }

            mirrored = default;
            return false;
        }

        bool GetMirrorSlotStruct(SlotStruct slot, out SlotStruct mirrored)
        {
            SlotStruct root = slot.Root;
            if (GetMirrorSlot(root, root.Module, out MirrorSlot ms) && ms.Slot.Module != null)
            {
                mirrored = ms.Slot;
                return true;
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
                    && module.XSize == slot.Module.XSize
                    && module.YSize == slot.Module.YSize)
                    return true;
            }
            module = null;
            return false;
        }
    }
}
