using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using System;
using System.IO;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        ShipModule[] ModuleSlotList;
        ShipModule[] SparseModuleGrid;   // single dimensional grid, for performance reasons
        ShipModule[] ExternalModuleGrid; // only contains external modules
        public int NumExternalSlots { get; private set; }
        int GridWidth;
        int GridHeight;
        Vector2 GridOrigin; // local origin, eg -32, -48

        static bool EnableDebugGridExport = false;

        public bool HasModules => ModuleSlotList != null && ModuleSlotList.Length != 0;
        public bool ModuleSlotsDestroyed => ModuleSlotList.Length == 0;

        void CreateModuleGrid()
        {
            float minX  = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            SurfaceArea = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                Vector2 topLeft = module.Position;
                SurfaceArea += module.Area;
                var botRight = new Vector2(topLeft.X + module.XSIZE * 16.0f,
                                           topLeft.Y + module.YSIZE * 16.0f);
                if (topLeft.X  < minX) minX = topLeft.X;
                if (topLeft.Y  < minY) minY = topLeft.Y;
                if (botRight.X > maxX) maxX = botRight.X;
                if (botRight.Y > maxY) maxY = botRight.Y;
            }

            float spanX = maxX - minX;
            float spanY = maxY - minY;
            GridOrigin = new Vector2(minX, minY);
            GridWidth  = (int)spanX / 16;
            GridHeight = (int)spanY / 16;
            SparseModuleGrid   = new ShipModule[GridWidth * GridHeight];
            ExternalModuleGrid = new ShipModule[GridWidth * GridHeight];

            // Ship radius is half of Module Grid's Diagonal Length
            Radius = 0.5f * (float)Math.Sqrt(spanX*spanX + spanY*spanY);

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                UpdateGridSlot(SparseModuleGrid, ModuleSlotList[i], becameActive: true);
            }

            var cachedModules = new ModuleCache(ModuleSlotList);
            Shields    = cachedModules.Shields;
            Amplifiers = cachedModules.Amplifiers;

            if (EnableDebugGridExport)
            {
                DebugDumpGrid($"Debug/SparseGrid/{Name}.txt", SparseModuleGrid,
                    m => new []
                    {
                        m != null ? $"|_{m.XSIZE}x{m.YSIZE}_": "|_____",
                        m != null ? $"|{SafeSub(m.UID,0,5)}" : "|_____",
                        m != null ? $"|{SafeSub(m.UID,5,5)}" : "|_____"
                    });
            }
        }

        bool[] CreateInternalGrid(ModuleSlotData[] templateSlots)
        {
            // gather a sparse 2D grid of internal slots
            var internalSlots = new bool[GridWidth * GridHeight];
            var offset = new Vector2(264f, 264f); // @note Defined in ShipModule.Initialize
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slot = templateSlots[i];
                if (slot.Restrictions == Restrictions.I)
                {
                    ModulePosToGridPoint(slot.Position - offset, out int x, out int y);
                    internalSlots[x + y * GridWidth] = true;
                }
            }
            return internalSlots;
        }

        bool IsModuleOverlappingInternalSlot(ShipModule module, bool[] internalGrid)
        {
            ModulePosToGridPoint(module.Position, out int x, out int y);
            int endX = x + module.XSIZE;
            int endY = y + module.YSIZE;
            for (; y < endY; ++y)
            for (; x < endX; ++x)
                if (internalGrid[x + y*GridWidth])
                    return true;
            return false;
        }

        void FixLegacyInternalRestrictions(ModuleSlotData[] templateSlots)
        {
            // gather a sparse 2D grid of internal slots
            bool[] internalGrid = CreateInternalGrid(templateSlots);

            if (EnableDebugGridExport)
            {
                DebugDumpGrid($"Debug/InternalGrid/{Name}.txt", internalGrid,
                    b => new []{ b ? " I " : " - " });
            }

            // if our module is overlapping an [I] slot, then mark the whole module as internal
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.HasInternalRestrictions && IsModuleOverlappingInternalSlot(module, internalGrid))
                    module.Restrictions = Restrictions.I;
            }
        }

        static string SafeSub(string s, int start, int count)
        {
            if (start >= s.Length) return PadCentered("", count);
            if (start+count >= s.Length)
                return PadCentered(s.Substring(start), count);
            return s.Substring(start, count);
        }

        static string PadCentered(string source, int length)
        {
            int spaces = length - source.Length;
            int padLeft = spaces/2 + source.Length;
            return source.PadLeft(padLeft).PadRight(length);
        }

        void DebugDumpGrid<T>(string fileName, T[] grid, Func<T, string[]> format)
        {
            string exportDir = Path.GetDirectoryName(fileName) ?? "";
            Directory.CreateDirectory(exportDir);
            int columnWidth = 0;
            var formatted = new string[GridWidth * GridHeight][];
            for (int y = 0; y < GridHeight; ++y)
            {
                for (int x = 0; x < GridWidth; ++x)
                {
                    int index = x + y * GridWidth;
                    string[] text = format(grid[index]);
                    formatted[index] = text;
                    foreach (string s in text) columnWidth = Math.Max(columnWidth, s.Length);
                }
            }
            using (var fs = new StreamWriter(fileName))
            {
                fs.Write($"W: {GridWidth} H:{GridHeight}\n");
                fs.Write("   ");
                for (int x = 0; x < GridWidth; ++x)
                    fs.Write(PadCentered(x.ToString(), columnWidth));
                fs.Write('\n');
                for (int y = 0; y < GridHeight; ++y)
                {
                    fs.Write(PadCentered(y.ToString(), 3));
                    int numLines = formatted[0].Length;
                    for (int line = 0; line < numLines; ++line)
                    {
                        if (line != 0) fs.Write("   ");
                        for (int x = 0; x < GridWidth; ++x)
                        {
                            string[] element = formatted[x + y * GridWidth];
                            fs.Write(PadCentered(element[line], columnWidth));
                        }
                        fs.Write('\n');
                    }
                }
            }
        }

        void AddExternalModule(ShipModule module, int x, int y, int quadrant)
        {
            if (module.isExternal)
                return;
            ++NumExternalSlots;
            module.isExternal = true;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: true);
        }

        void RemoveExternalModule(ShipModule module, int x, int y)
        {
            if (!module.isExternal)
                return;
            --NumExternalSlots;
            module.isExternal = false;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: false);
        }

        bool IsModuleInactiveAt(int x, int y)
        {
            ShipModule module = (uint)x < GridWidth && (uint)y < GridHeight ? SparseModuleGrid[x + y*GridWidth] : null;
            return module == null || !module.Active;
        }

        int GetQuadrantEstimate(int x, int y)
        {
            Vector2 dir = new Vector2(x - (GridWidth / 2), y - (GridHeight / 2)).Normalized();
            if (dir.X <= 0f && dir.Y <= 0f)
                return 0;
            if (Math.Abs(dir.Y) <= 0.5f)
                return dir.X <= 0f ? 1 /*top*/ : 3 /*bottom*/;
            return dir.X <= 0f ? 4 /*left*/ : 2 /*right*/;
        }

        bool ShouldBeExternal(int x, int y, ShipModule module)
        {
            return module.Active &&
                IsModuleInactiveAt(x, y - 1) ||
                IsModuleInactiveAt(x - 1, y) ||
                IsModuleInactiveAt(x + module.XSIZE, y) ||
                IsModuleInactiveAt(x, y + module.YSIZE);
        }

        bool CheckIfShouldBeExternal(int x, int y)
        {
            if (!GetModuleAt(SparseModuleGrid, x, y, out ShipModule module))
                return false;

            ModulePosToGridPoint(module.Position, out x, out y); // now get the true topleft root coordinates of module
            if (ShouldBeExternal(x, y, module))
            {
                if (!module.isExternal)
                    AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
                return true;
            }
            if (module.isExternal)
                RemoveExternalModule(module, x, y);
            return false;
        }

        void InitExternalSlots()
        {
            NumExternalSlots = 0;
            for (int y = 0; y < GridHeight; ++y)
            {
                for (int x = 0; x < GridWidth; ++x)
                {
                    int idx = x + y*GridWidth;
                    if (ExternalModuleGrid[idx] != null || !CheckIfShouldBeExternal(x, y))
                        continue;
                    // ReSharper disable once PossibleNullReferenceException
                    x += ExternalModuleGrid[idx].XSIZE - 1; // skip slots that span this module
                }
            }
        }

        // updates the isExternal status of a module, depending on whether it died or resurrected
        public void UpdateExternalSlots(ShipModule module, bool becameActive)
        {
            ModulePosToGridPoint(module.Position, out int x, out int y);

            if (becameActive) // we resurrected, so add us to external module grid and update all surrounding slots
                AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
            else // we turned inactive, so clear self from external module grid and
                RemoveExternalModule(module, x, y);

            CheckIfShouldBeExternal(x, y - 1);
            CheckIfShouldBeExternal(x - 1, y);
            CheckIfShouldBeExternal(x + module.XSIZE, y);
            CheckIfShouldBeExternal(x, y + module.YSIZE);
        }

        void UpdateGridSlot(ShipModule[] sparseGrid, int gridX, int gridY, ShipModule module, bool becameActive)
        {
            int endX = gridX + module.XSIZE, endY = gridY + module.YSIZE;
            for (int y = gridY; y < endY; ++y)
                for (int x = gridX; x < endX; ++x)
                    sparseGrid[x + y * GridWidth] = becameActive ? module : null;
        }

        void UpdateGridSlot(ShipModule[] sparseGrid, ShipModule module, bool becameActive)
        {
            ModulePosToGridPoint(module.Position, out int x, out int y);
            UpdateGridSlot(sparseGrid, x, y, module, becameActive);
        }

        void ModulePosToGridPoint(Vector2 moduleLocalPos, out int x, out int y)
        {
            Vector2 offset = moduleLocalPos - GridOrigin;
            x = (int)Math.Floor(offset.X / 16f);
            y = (int)Math.Floor(offset.Y / 16f);
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        bool GetModuleAt(ShipModule[] sparseGrid, int x, int y, out ShipModule module)
        {
            module = (uint)x < GridWidth && (uint)y < GridHeight ? sparseGrid[x + y * GridWidth] : null;
            return module != null;
        }

        static void DebugDrawShield(ShipModule s)
        {
            var color = s.ShieldsAreActive ? Color.AliceBlue : Color.DarkBlue;
            Empire.Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Center, s.ShieldHitRadius, color, 2f);
        }

        static void DebugDrawShieldHit(ShipModule s)
        {
            Empire.Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Center, s.ShieldHitRadius, Color.BlueViolet, 2f);
        }

        static void DebugDrawShieldHit(ShipModule s, Vector2 start, Vector2 end)
        {
            Empire.Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Center, s.ShieldHitRadius, Color.BlueViolet, 2f);
            if (start != end)
                Empire.Universe.DebugWin?.DrawLine(DebugModes.SpatialManager, start, end, 2f, Color.BlueViolet, 2f);
        }

        // The simplest form of collision against shields. This is handled in all other HitTest functions
        ShipModule HitTestShields(Vector2 worldHitPos, float hitRadius)
        {
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldsAreActive && shield.HitTestShield(worldHitPos, hitRadius))
                {
                    //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                    //    DebugDrawShieldHit(shield);
                    return shield;
                }
            }
            return null;
        }

        // Slightly more complicated ray-collision against shields
        ShipModule RayHitTestShields(Vector2 worldStartPos, Vector2 worldEndPos, float rayRadius, out float hitDistance)
        {
            float minD = float.MaxValue;
            ShipModule hit = null;
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldsAreActive &&
                    shield.RayHitTestShield(worldStartPos, worldEndPos, rayRadius, out float distanceFromStart))
                {
                    if (distanceFromStart < minD)
                    {
                        minD = distanceFromStart;
                        hit = shield;
                    }
                }
            }
            if (hit != null && DebugInfoScreen.Mode == DebugModes.SpatialManager)
            {
                //DebugDrawShieldHit(hit, worldStartPos, worldEndPos);
            }
            hitDistance = minD;
            return hit;
        }

        // Gets the strongest shield currently covering internalModule
        bool IsCoveredByShield(ShipModule internalModule, out ShipModule shield)
        {
            float maxPower = 0f;
            shield = null;
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule m = Shields[i];
                float power = m.ShieldPower;
                if (power > maxPower && m.HitTestShield(internalModule.Center, internalModule.Radius))
                    shield = m;
            }
            return shield != null;
        }



        // Converts a world position to a grid local position (such as [16f,32f])
        public Vector2 WorldToGridLocal(Vector2 worldPoint)
        {
            Vector2 offset = worldPoint - Center;
            return offset.RotatePoint(-Rotation) - GridOrigin;
        }

        public Point WorldToGridLocalPoint(Vector2 worldPoint)
        {
            return GridLocalToPoint(WorldToGridLocal(worldPoint));
        }

        public Point GridLocalToPoint(Vector2 localPos)
        {
            return new Point((int)Math.Floor(localPos.X / 16f), (int)Math.Floor(localPos.Y / 16f));
        }

        public Point WorldToGridLocalPointClipped(Vector2 worldPoint)
        {
            return ClipLocalPoint(WorldToGridLocalPoint(worldPoint));
        }

        public Point ClipLocalPoint(Point pt)
        {
            if (pt.X < 0) pt.X = 0; else if (pt.X >= GridWidth)  pt.X = GridWidth  - 1;
            if (pt.Y < 0) pt.Y = 0; else if (pt.Y >= GridHeight) pt.Y = GridHeight - 1;
            return pt;
        }

        bool LocalPointInBounds(Point point)
        {
            return 0 <= point.X && point.X < GridWidth
                && 0 <= point.Y && point.Y < GridHeight;
        }

        // an out of bounds clipped point would be in any of the extreme corners.
        bool ClippedLocalPointInBounds(Point point)
        {
            return 0 <= point.X && point.X < GridWidth
                && 0 <= point.Y && point.Y < GridHeight
                && point != Point.Zero
                && (point.X < GridWidth - 1 || point.Y < GridHeight - 1)
                && (point.X > 0 || point.Y < GridHeight - 1)
                && (point.Y > 0 || point.X < GridWidth - 1);
        }

        public Vector2 GridLocalToWorld(Vector2 localPoint)
        {
            Vector2 centerLocal = GridOrigin + localPoint;
            return centerLocal.RotatePoint(Rotation) + Center;
        }

        public Vector2 GridLocalPointToWorld(Point gridLocalPoint)
        {
            return GridLocalToWorld(new Vector2(gridLocalPoint.X * 16f, gridLocalPoint.Y * 16f));
        }

        public Vector2 GridSquareToWorld(int x, int y) => GridLocalToWorld(new Vector2(x * 16f + 8f, y * 16f + 8f));



        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        // @note Ignores shields !
        public ShipModule FindClosestUnshieldedModule(Vector2 worldPoint)
        {
            if (NumExternalSlots == 0)
                return null;

            ++GlobalStats.DistanceCheckTotal;

            Point pt = WorldToGridLocalPoint(worldPoint);
            if (!LocalPointInBounds(pt))
                return null;

            ShipModule[] grid = ExternalModuleGrid;
            int width = GridWidth;
            ShipModule m;
            if ((m = grid[pt.X + pt.Y*width]) != null && m.Active) return m;

            int minX = pt.X, minY = pt.Y, maxX = pt.X, maxY = pt.Y;
            int lastX = width - 1, lastY = GridHeight - 1;
            for (;;)
            {
                bool didExpand = false;
                if (minX > 0f) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y) if ((m = grid[minX + y*width]) != null && m.Active) return m;
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y) if ((m = grid[maxX + y*width]) != null && m.Active) return m;
                }
                if (minY > 0f) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x) if ((m = grid[x + minY*width]) != null && m.Active) return m;
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x) if ((m = grid[x + maxY*width]) != null && m.Active) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }


        // find the first module that falls under the hit radius at given position
        public ShipModule HitTestSingle(Vector2 worldHitPos, float hitRadius, bool ignoreShields = false)
        {
            if (NumExternalSlots == 0) return null;
            if (!ignoreShields)
            {
                ShipModule shield = HitTestShields(worldHitPos, hitRadius);
                if (shield != null) return shield;
            }

            ++GlobalStats.DistanceCheckTotal;

            Point a  = WorldToGridLocalPoint(worldHitPos - new Vector2(hitRadius));
            Point b  = WorldToGridLocalPoint(worldHitPos + new Vector2(hitRadius));
            bool inA = LocalPointInBounds(a);
            bool inB = LocalPointInBounds(b);
            if (!inA && !inB)
                return null;
            if (!inA) a = ClipLocalPoint(a);
            if (!inB) b = ClipLocalPoint(b);

            ShipModule[] grid = SparseModuleGrid;
            int width = GridWidth;
            ShipModule m;
            if (a == b)
            {
                if ((m = grid[a.X + a.Y*width]) != null && m.Active)
                    return m;
                return null;
            }

            int firstX = Math.Min(a.X, b.X);
            int firstY = Math.Min(a.Y, b.Y);
            int lastX  = Math.Max(a.X, b.X);
            int lastY  = Math.Max(a.Y, b.Y);

            Point cx = WorldToGridLocalPointClipped(worldHitPos);
            int minX = cx.X, minY = cx.Y;
            int maxX = cx.X, maxY = cx.Y;
            if ((m = grid[minX + minY*width]) != null && m.Active) return m;

            for (;;)
            {
                bool didExpand = false;
                if (minX > firstX) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y*width]) != null && m.Active) return m;
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y*width]) != null && m.Active) return m;
                }
                if (minY > firstY) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + minY*width]) != null && m.Active) return m;
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + maxY*width]) != null && m.Active) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }

        // Code gods, I pray for thy forgiveness for this copy-paste -T_T-
        // This was done solely for performance reasons. This method gets called
        // every time an exploding projectile hits a ship. So it gets called for every missile impact
        // @note THIS IS ALWAYS AN EXPLOSION
        public void DamageModulesExplosive(GameplayObject damageSource, float damageAmount,
                                           Vector2 worldHitPos, float hitRadius, bool ignoresShields)
        {
            float damageTracker = damageAmount;
            if (!ignoresShields)
            {
                for (int i = 0; i < Shields.Length; ++i)
                {
                    ShipModule module = Shields[i];
                    if (module.ShieldsAreActive && module.HitTestShield(worldHitPos, hitRadius))
                    {
                        if (module.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker))
                            return; // no more damage to dish, exit early
                    }
                }
            }

            Point a = WorldToGridLocalPointClipped(worldHitPos - new Vector2(hitRadius));
            Point b = WorldToGridLocalPointClipped(worldHitPos + new Vector2(hitRadius));
            if (!ClippedLocalPointInBounds(a) && !ClippedLocalPointInBounds(b))
            {
                if (!LocalPointInBounds(WorldToGridLocalPoint(worldHitPos)))
                    return;
            }


            ShipModule[] grid = SparseModuleGrid;
            int width = GridWidth;
            ShipModule m;
            if (a == b)
            {
                if ((m = grid[a.X + a.Y*width]) != null && m.Active
                    && m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker))
                    return;
                return;
            }
            int firstX = Math.Min(a.X, b.X); // this is the max bounding box range of the scan
            int firstY = Math.Min(a.Y, b.Y);
            int lastX  = Math.Max(a.X, b.X);
            int lastY  = Math.Max(a.Y, b.Y);
            //damageTracker = damageAmount;
            Point cx = WorldToGridLocalPointClipped(worldHitPos); // clip the start, because it's often near an edge
            int minX = cx.X, minY = cx.Y;
            int maxX = cx.X, maxY = cx.Y;
            if ((m = grid[minX + minY*width]) != null && m.Active)
                  m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);

            // spread out the damage in 4 directions but apply a new set of full damage to external modules.

            for (;;)
            {
                bool didExpand = false;
                if (minX > firstX) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }
                if (minY > firstY) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + minY * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + maxY * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }
                if (!didExpand) return; // wellll, looks like we're done here!
            }
        }

        void DebugGridStep(Vector2 p, Color color)
        {
            Vector2 gridWorldPos = GridLocalPointToWorld(GridLocalToPoint(p)) + new Vector2(8f);
            Empire.Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, gridWorldPos, 4f, color.Alpha(0.33f), 2.0f);
        }

        void DebugGridStep(Vector2 a, Vector2 b, Color color, float width = 1f)
        {
            Vector2 worldPosA = GridLocalPointToWorld(GridLocalToPoint(a)) + new Vector2(8f);
            Vector2 worldPosB = GridLocalPointToWorld(GridLocalToPoint(b)) + new Vector2(8f);
            Empire.Universe.DebugWin?.DrawLine(DebugModes.SpatialManager, worldPosA, worldPosB, width, color.Alpha(0.75f), 2.0f);
        }

        // take one step in the module grid
        // @todo Make use of rayRadius to improve Walk precision
        ShipModule TakeOneStep(Vector2 start, Vector2 step)
        {
            Vector2 endPos = start + step;
            Point pos = GridLocalToPoint(start);
            Point end = GridLocalToPoint(endPos);
            if (!LocalPointInBounds(pos) || !LocalPointInBounds(end))
                return null; // we're walking out of bounds

            // @note We don't check grid at [pos], because we assume prev call checked it
            if (pos.IsDiagonalTo(end))
            {
                // check a module at the same Y height as final point
                // this forces us to always take an L shaped step instead of diagonal \
                var neighbor = new Point(pos.X, end.Y);
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(new Vector2(start.X, endPos.Y), Color.Yellow);

                ShipModule mb = SparseModuleGrid[neighbor.X + neighbor.Y * GridWidth];
                if (mb != null && mb.Active)
                {
                    //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                    //    DebugGridStep(start, new Vector2(start.X, endPos.Y), Color.Cyan, 4f);
                    return mb;
                }
            }

            //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
            //    DebugGridStep(endPos, Color.LightGreen);

            ShipModule mc = SparseModuleGrid[end.X + end.Y * GridWidth];
            if (mc != null && mc.Active)
            {
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(start, endPos, Color.HotPink, 4f);
                return mc;
            }
            return null;
        }

        // perform a raytrace from point a to point b, visiting all grid points between them!
        ShipModule WalkModuleGrid(Vector2 a, Vector2 b)
        {
            Vector2 pos = a;
            Vector2 delta = b - a;
            Vector2 step = delta.Normalized() * 16f;

            // sometimes we directly enter the grid and hit a module:
            Point enter = GridLocalToPoint(pos);
            ShipModule me = SparseModuleGrid[enter.X + enter.Y * GridWidth];
            if (me != null && me.Active)
            {
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(pos - step, pos, Color.DarkGoldenrod);
                return me;
            }

            int n = (int)(delta.Length() / 16f);
            for (; n >= 0; --n, pos += step)
            {
                ShipModule m = TakeOneStep(pos, step);
                if (m != null)
                {
                    //if (DebugInfoScreen.Mode == DebugModes.Targeting)
                    //    Empire.Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, m.Center, 6f, Color.IndianRed.Alpha(0.5f), 3f);
                    return m;
                }
            }
            return null;
        }

        public ShipModule RayHitTestSingle(Vector2 startPos, Vector2 endPos, float rayRadius, bool ignoreShields = false)
        {
            // first we find the shield overlap, however, a module might be overlapping just before the shield border
            float shieldHitDist = float.MaxValue;
            ShipModule shield = null;
            if (!ignoreShields)
                shield = RayHitTestShields(startPos, endPos, rayRadius, out shieldHitDist);

            ++GlobalStats.DistanceCheckTotal;

            // move [a] completely out of bounds to prevent attacking central modules
            Vector2 dir = (endPos - startPos).Normalized();
            Vector2 a = WorldToGridLocal(startPos - dir * (Radius * 2));
            Vector2 b = WorldToGridLocal(endPos);
            if (MathExt.ClipLineWithBounds(GridWidth*16f, GridHeight*16f, a, b, ref a, ref b)) // guaranteed bounds safety
            {
                ShipModule module = WalkModuleGrid(a, b);
                if (module == null)
                    return shield;

                if (shield == null || module.Center.Distance(startPos) < shieldHitDist)
                    return module; // module was closer, so should be hit first
            }
            return shield;
        }


        // find ShipModules that collide with a this wide RAY
        // direction must be normalized!!
        // results are sorted by distance
        // @warning Ignores shields!!
        // @note Don't bother optimizing this. It's only used during armour piercing, which is super rare.
        // @todo Align this with RayHitTestSingle
        public Array<ShipModule> RayHitTestModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius)
        {
            Vector2 endPos = startPos + direction * distance;
            var path = new Array<ShipModule>();

            Vector2 a = WorldToGridLocal(startPos);
            Vector2 b = WorldToGridLocal(endPos);
            if (MathExt.ClipLineWithBounds(GridWidth * 16f, GridHeight * 16f, a, b, ref a, ref b)) // guaranteed bounds safety
            {
                Vector2 pos = a;
                Vector2 delta = b - a;
                Vector2 step = delta.Normalized() * 16f;
                int n = (int)(delta.Length() / 16f);
                for (; n > 0; --n, pos += step)
                {
                    Point p = GridLocalToPoint(pos);
                    ShipModule m = SparseModuleGrid[p.X + p.Y*GridWidth];
                    if (m != null && m.Active)
                        path.AddUniqueRef(m);
                }
            }
            return path;
        }




        // Refactor by RedFox: Picks a random internal module in search range (squared) of the projectile
        // -- Higher crew level means the missile will pick the most optimal target module ;) --
        ShipModule TargetRandomInternalModule(Vector2 projPos, int level, float sqSearchRange)
        {
            ShipModule[] modules = ModuleSlotList.Filter(m => m.Active && projPos.SqDist(m.Center) < sqSearchRange);
            if (modules.Length == 0)
                return null;

            if (level > 1)
            {
                // Sort Descending (-), so first element is the module with greatest TargetingValue
                modules.Sort(m => -m.ModuleTargetingValue);
            }

            // higher levels lower the limit, which causes a better random pick
            int limit = modules.Length / (level + 1);
            return modules[RandomMath.InRange(limit)];
        }

        // This is called for guided weapons to pick a new target
        public ShipModule GetRandomInternalModule(Weapon source)
        {
            Vector2 center    = source.Owner?.Center ?? source.Origin;
            int level         = source.Owner?.Level  ?? 0;
            float searchRange = source.BaseRange + 100;
            return TargetRandomInternalModule(center, level, searchRange*searchRange);
        }

        // This is called for initial missile guidance ChooseTarget(), so range is not that important
        public ShipModule GetRandomInternalModule(Projectile source)
        {
            Vector2 projPos = source.Owner?.Center ?? source.Center;
            int level       = source.Owner?.Level  ?? 0;
            float searchRange = projPos.SqDist(Center) + 48*48; // only pick modules that are "visible" to the projectile
            return TargetRandomInternalModule(projPos, level, searchRange);
        }

    }
}
