using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
        private readonly Map<Vector2, ShipModule> ModulesDictionary = new Map<Vector2, ShipModule>();
        public ShipModule[] ModuleSlotList;

        private ShipModule[] SparseModuleGrid; // single dimensional grid, for performance reasons
        private int GridWidth;
        private int GridHeight;
        private Vector2 GridOrigin;

        private void CreateModuleGrid()
        {
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                Vector2 topLeft = module.Position;
                var botRight = new Vector2(topLeft.X + module.XSIZE * 16.0f,
                                           topLeft.Y + module.YSIZE * 16.0f);

                if (topLeft.X < minX) minX = topLeft.X;
                if (topLeft.Y < minY) minY = topLeft.Y;
                if (botRight.X > maxX) maxX = botRight.X;
                if (botRight.Y > maxY) maxY = botRight.Y;
            }

            GridOrigin = new Vector2(minX, minY);
            GridWidth  = (int)(maxX - minX) / 16;
            GridHeight = (int)(maxY - minY) / 16;
            SparseModuleGrid = new ShipModule[GridWidth * GridHeight];

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                Point origin = SlotPointAt(module.Position);

                for (int y = 0; y < module.YSIZE; ++y)
                {
                    for (int x = 0; x < module.XSIZE; ++x)
                    {
                        int idx = GridWidth * (origin.Y + y) + (origin.X + x);
                        SparseModuleGrid[idx] = module;
                    }
                }
            }
        }

        private Point SlotPointAt(Vector2 pos)
        {
            Vector2 offset = pos - GridOrigin;
            return new Point((int)(offset.X / 16.0f),
                             (int)(offset.Y / 16.0f));
        }

        private int SlotIndexAt(Vector2 pos)
        {
            Vector2 offset = pos - GridOrigin;
            int x = (int)(offset.X / 16.0f);
            int y = (int)(offset.Y / 16.0f);
            int idx = y * GridHeight + x;
            if ((uint)idx >= SparseModuleGrid.Length)
                return -1;
            return idx;
        }

        private void CheckIfExternalModule(Vector2 pos, ShipModule module)
        {
            if (!ModulesDictionary.TryGetValue(new Vector2(pos.X, pos.Y - 16f), out ShipModule q1) || !q1.Active)
            {
                module.isExternal = true;
                module.quadrant = 1;
            }
            else if (!ModulesDictionary.TryGetValue(new Vector2(pos.X + 16f, pos.Y), out ShipModule q4) || !q4.Active)
            {
                module.isExternal = true;
                module.quadrant = 2;
            }
            else if (!ModulesDictionary.TryGetValue(new Vector2(pos.X, pos.Y + 16f), out ShipModule q2) || !q2.Active)
            {
                module.isExternal = true;
                module.quadrant = 3;
            }
            else if (!ModulesDictionary.TryGetValue(new Vector2(pos.X - 16f, pos.Y), out ShipModule q3) || !q3.Active)
            {
                module.isExternal = true;
                module.quadrant = 4;
            }
        }

        private void FillExternalSlots()
        {
            ExternalSlots.Clear();
            ModulesDictionary.Clear();
            foreach (ShipModule slot in ModuleSlotList)
                ModulesDictionary.Add(slot.XMLPosition, slot);

            foreach (KeyValuePair<Vector2, ShipModule> kv in ModulesDictionary)
            {
                ShipModule module = kv.Value;
                if (module.Active)
                    CheckIfExternalModule(kv.Key, module);
                if (module.isExternal)
                    ExternalSlots.Add(module);
            }
        }

        // @note ExternalSlots are always Alive (Active). This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        public ShipModule FindClosestExternalModule(Vector2 point)
        {
            return ExternalSlots.FindMin(module => point.SqDist(module.Center));
        }

        public Array<ShipModule> FindExternalModules(int quadrant)
        {
            var modules = new Array<ShipModule>();
            int count = ExternalSlots.Count;
            var slots = ExternalSlots.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = slots[i];
                if (module.quadrant == quadrant && module.Health > 0f)
                    modules.Add(module);
            }
            return modules;
        }

        public ShipModule FindUnshieldedExternalModule(int quadrant)
        {
            int count = ExternalSlots.Count;
            var slots = ExternalSlots.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = slots[i];
                if (module.quadrant == quadrant && module.Health > 0f && module.ShieldPower <= 0f)
                    return module;
            }
            return null; // aargghh ;(
        }

        public ShipModule RayHitTestExternalModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius, bool ignoreShields = false)
        {
            return RayHitTestExternalModules(startPos, startPos + direction * distance, rayRadius, ignoreShields);
        }

        // @todo This needs optimization
        public ShipModule RayHitTestExternalModules(Vector2 startPos, Vector2 endPos, float rayRadius, bool ignoreShields = false)
        {
            int count = ExternalSlots.Count;
            var slots = ExternalSlots.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = slots[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                Vector2 point = module.Center.FindClosestPointOnLine(startPos, endPos);
                if (module.HitTest(point, rayRadius, ignoreShields))
                    return module;
            }
            return null;
        }

        public ShipModule HitTestExternalModules(Vector2 hitPos, float hitRadius, bool ignoreShields = false)
        {
            int count = ExternalSlots.Count;
            var slots = ExternalSlots.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = slots[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                if (module.HitTest(hitPos, hitRadius, ignoreShields))
                    return module;
            }
            return null;
        }

        // find ShipModules that collide with a this wide RAY
        // direction must be normalized!!
        // results are sorted by distance
        public Array<ShipModule> RayHitTestModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius, bool ignoreShields = false)
        {
            Vector2 endPos = startPos + direction * distance;

            var modules = new Array<ShipModule>();
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                Vector2 point = module.Position.FindClosestPointOnLine(startPos, endPos);
                if (module.HitTest(point, rayRadius, ignoreShields))
                    modules.Add(module);
            }
            modules.Sort(module => startPos.SqDist(module.Position));
            return modules;
        }

        // find ShipModules that fall into hit radius (eg an explosion)
        // results are sorted by distance
        public Array<ShipModule> HitTestModules(Vector2 hitPos, float hitRadius, bool ignoreShields = false)
        {
            var modules = new Array<ShipModule>();
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.Health <= 0f)
                    continue;

                ++GlobalStats.DistanceCheckTotal;

                if (module.HitTest(hitPos, hitRadius, ignoreShields))
                    modules.Add(module);
            }
            modules.Sort(module => hitPos.SqDist(module.Position));
            return modules;
        }

    }
}
