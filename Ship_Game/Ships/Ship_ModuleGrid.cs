using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using System;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        ShipModule[] ModuleSlotList;
        ShipModule[] SparseModuleGrid;   // single dimensional grid, for performance reasons
        ShipModule[] ExternalModuleGrid; // only contains external modules
        public PowerGrid PwrGrid;

        public int NumExternalSlots { get; private set; }

        /// <summary>  Ship slot (1x1 modules) width </summary>
        public int GridWidth { get; private set; }
        
        /// <summary>Ship slot (1x1 modules) height </summary>
        public int GridHeight { get; private set; }

        // center of the ship in ShipLocal world coordinates, ex [64f, 0f], always positive
        Vector2 GridLocalCenter;
        
        static bool EnableDebugGridExport = false;

        public ShipModule[] Modules => ModuleSlotList;
        public bool HasModules => ModuleSlotList != null && ModuleSlotList.Length != 0;

        void CreateModuleGrid(in ShipGridInfo gridInfo, bool isTemplate, bool shipyardDesign)
        {
            ShipGridInfo info = gridInfo;

        #if DEBUG
            if (isTemplate && !shipyardDesign)
            {
                var modulesInfo = new ShipGridInfo(ModuleSlotList);
                if (modulesInfo.SurfaceArea != gridInfo.SurfaceArea ||
                    modulesInfo.Size != gridInfo.Size)
                {
                    Log.Warning($"BaseHull mismatch: {modulesInfo} != {gridInfo}. Broken Design={Name}");
                }
            }
        #endif

            SurfaceArea = info.SurfaceArea;
            GridWidth  = info.Size.X;
            GridHeight = info.Size.Y;
            GridLocalCenter = new Vector2(info.Size.X * 8f, info.Size.Y * 8f);
            SparseModuleGrid   = new ShipModule[GridWidth * GridHeight];
            ExternalModuleGrid = new ShipModule[GridWidth * GridHeight];
            PwrGrid = new PowerGrid(SparseModuleGrid, GridWidth, GridHeight);

            // Ship's true radius is half of Module Grid's Diagonal Length
            var span = new Vector2(info.Size.X, info.Size.Y) * 16f;
            Radius = span.Length() * 0.5f;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                UpdateGridSlot(SparseModuleGrid, ModuleSlotList[i], becameActive: true);
            }

            if (GlobalStats.CountInternalModulesFromHull)
                SetModuleRestrictionsFromHull(ShipData.BaseHull);

            InitExternalSlots();

            var shields    = new Array<ShipModule>();
            var amplifiers = new Array<ShipModule>();
            InternalSlotCount = 0;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (module.ShieldPowerMax > 0f)
                    shields.Add(module);

                if (module.AmplifyShields > 0f)
                    amplifiers.Add(module);

                if (module.HasInternalRestrictions)
                    InternalSlotCount += module.XSize * module.YSize;
            }

            Shields    = shields.ToArray();
            Amplifiers = amplifiers.ToArray();

            if (EnableDebugGridExport)
            {
                ModuleGridUtils.DebugDumpGrid($"Debug/SparseGrid/{Name}.txt",
                    SparseModuleGrid, GridWidth, GridHeight, ModuleGridUtils.DumpFormat.ShipModule);
            }
        }

        // This overrides Restrictions value of modules, based on the actual BaseHull
        // So if a 2x2 IOE module overlaps an `I` hull slot, it gets set to Restrictions.I
        void SetModuleRestrictionsFromHull(ShipHull hull)
        {
            HullSlot[] hullSlots = hull.HullSlots;
            for (int i = 0; i < hullSlots.Length; ++i)
            {
                HullSlot hs = hullSlots[i];
                if (hs.R == Restrictions.I)
                {
                    ShipModule m = GetModuleAt(hs.Pos);
                    if (m != null)
                    {
                        m.Restrictions = Restrictions.I;
                    }
                }
            }
        }

        void AddExternalModule(ShipModule module)
        {
            if (module.IsExternal)
                return;
            ++NumExternalSlots;
            module.IsExternal = true;
            UpdateGridSlot(ExternalModuleGrid, module, becameActive: true);
        }

        void RemoveExternalModule(ShipModule module)
        {
            if (!module.IsExternal)
                return;
            --NumExternalSlots;
            module.IsExternal = false;
            UpdateGridSlot(ExternalModuleGrid, module, becameActive: false);
        }

        bool IsModuleInactiveAt(int x, int y)
        {
            ShipModule module = (uint)x < GridWidth && (uint)y < GridHeight ? SparseModuleGrid[x + y*GridWidth] : null;
            return module == null || !module.Active;
        }

        bool ShouldBeExternal(ShipModule module)
        {
            if (!module.Active)
                return false;
            Point p = module.Pos;
            return IsModuleInactiveAt(p.X, p.Y - 1)
                || IsModuleInactiveAt(p.X - 1, p.Y)
                || IsModuleInactiveAt(p.X + module.XSize, p.Y)
                || IsModuleInactiveAt(p.X, p.Y + module.YSize);
        }

        bool CheckIfShouldBeExternal(int x, int y)
        {
            if (!GetModuleAt(SparseModuleGrid, x, y, out ShipModule module))
                return false;

            if (ShouldBeExternal(module))
            {
                if (!module.IsExternal)
                {
                    AddExternalModule(module);
                }
                return true;
            }
            if (module.IsExternal)
                RemoveExternalModule(module);
            return false;
        }

        void InitExternalSlots()
        {
            NumExternalSlots = 0;
            for (int y = 0; y < GridHeight; ++y)
            {
                for (int x = 0; x < GridWidth; ++x)
                {
                    int idx = x + y * GridWidth;
                    if (ExternalModuleGrid[idx] == null && CheckIfShouldBeExternal(x, y))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        // NOTE about ReSharper: CheckIfShouldBeExternal modifies ExternalModuleGrid
                        x += ExternalModuleGrid[idx].XSize - 1; // skip slots that span this module
                    }
                }
            }
        }

        // updates the isExternal status of a module, depending on whether it died or resurrected
        public void UpdateExternalSlots(ShipModule module, bool becameActive)
        {
            int x = module.Pos.X;
            int y = module.Pos.Y;
            if (becameActive) // we resurrected, so add us to external module grid and update all surrounding slots
                AddExternalModule(module);
            else // we turned inactive, so clear self from external module grid and
                RemoveExternalModule(module);

            CheckIfShouldBeExternal(x, y - 1);
            CheckIfShouldBeExternal(x - 1, y);
            CheckIfShouldBeExternal(x + module.XSize, y);
            CheckIfShouldBeExternal(x, y + module.YSize);
        }

        void UpdateGridSlot(ShipModule[] sparseGrid, ShipModule module, bool becameActive)
        {
            Point p = module.Pos;
            int endX = p.X + module.XSize, endY = p.Y + module.YSize;
            for (int y = p.Y; y < endY; ++y)
                for (int x = p.X; x < endX; ++x)
                    sparseGrid[x + y * GridWidth] = becameActive ? module : null;
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        bool GetModuleAt(ShipModule[] sparseGrid, int x, int y, out ShipModule module)
        {
            module = (uint)x < GridWidth && (uint)y < GridHeight ? sparseGrid[x + y * GridWidth] : null;
            return module != null;
        }

        public ShipModule GetModuleAt(Point gridPos)
        {
            return SparseModuleGrid[gridPos.X + gridPos.Y*GridWidth];
        }

        void DebugDrawShield(ShipModule s)
        {
            var color = s.ShieldsAreActive ? Color.AliceBlue : Color.DarkBlue;
            Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Position, s.ShieldHitRadius, color, 2f);
        }

        void DebugDrawShieldHit(ShipModule s)
        {
            Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Position, s.ShieldHitRadius, Color.BlueViolet, 2f);
        }

        void DebugDrawShieldHit(ShipModule s, Vector2 start, Vector2 end)
        {
            Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, s.Position, s.ShieldHitRadius, Color.BlueViolet, 2f);
            if (start != end)
                Universe.DebugWin?.DrawLine(DebugModes.SpatialManager, start, end, 2f, Color.BlueViolet, 2f);
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

        // Gets all the shields currently covering a ship Module, starting with the outside radius first
        public ShipModule[] GetAllActiveShieldsCoveringModule(ShipModule module)
        {
            Array<ShipModule> coveringShields = new Array<ShipModule>();
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldsAreActive && shield.HitTestShield(module.Position, module.Radius))
                    coveringShields.Add(shield);
            }

            return coveringShields.Sorted(s => s.ShieldRadius - s.Position.Distance(module.Position));
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
                if (power > maxPower && m.HitTestShield(internalModule.Position, internalModule.Radius))
                    shield = m;
            }
            return shield != null;
        }

        // Converts a world position to a grid local position (such as [16f,32f])
        // TESTED in ShipModuleGridTests
        public Vector2 WorldToGridLocal(Vector2 worldPoint)
        {
            Vector2 offset = worldPoint - Position;
            return offset.RotatePoint(-Rotation) + GridLocalCenter;
        }
        
        // Converts a world position to a grid point such as [1,2]
        // TESTED in ShipModuleGridTests
        public Point WorldToGridLocalPoint(Vector2 worldPoint)
        {
            Vector2 gridLocal = WorldToGridLocal(worldPoint);
            Point gridPoint = GridLocalToPoint(gridLocal);
            return gridPoint;
        }
        
        // Converts a world position to a grid point such as [1,2]
        // CLIPS the value in range of [0, GRIDSIZE-1]
        // TESTED in ShipModuleGridTests
        public Point WorldToGridLocalPointClipped(Vector2 worldPoint)
        {
            return ClipLocalPoint(WorldToGridLocalPoint(worldPoint));
        }

        // Converts a grid-local pos to a grid point
        // TESTED via WorldToGridLocalPoint() in ShipModuleGridTests
        public Point GridLocalToPoint(Vector2 localPos)
        {
            return new Point((int)Math.Floor(localPos.X / 16f),
                             (int)Math.Floor(localPos.Y / 16f));
        }

        // Converts a grid-local pos to world pos
        // TESTED in ShipModuleGridTests
        public Vector2 GridLocalToWorld(Vector2 localPoint)
        {
            Vector2 centerLocal = localPoint - GridLocalCenter;
            return centerLocal.RotatePoint(Rotation) + Position;
        }
        
        // Converts a grid-local POINT to world pos
        // TESTED in ShipModuleGridTests
        public Vector2 GridLocalPointToWorld(Point gridLocalPoint)
        {
            return GridLocalToWorld(new Vector2(gridLocalPoint.X * 16f, gridLocalPoint.Y * 16f));
        }

        Vector2 GridSquareToWorld(int x, int y)
        {
            return GridLocalToWorld(new Vector2(x * 16f + 8f, y * 16f + 8f));
        }

        Point ClipLocalPoint(Point pt)
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



        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        // @note Ignores shields !
        public ShipModule FindClosestUnshieldedModule(Vector2 worldPoint)
        {
            if (NumExternalSlots == 0)
                return null;

            Point pt = WorldToGridLocalPoint(worldPoint);
            pt = ClipLocalPoint(pt);

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
            if (!ignoresShields)
            {
                for (int i = 0; i < Shields.Length; ++i)
                {
                    ShipModule shield = Shields[i];
                    if (shield.ShieldsAreActive && shield.HitTestShield(worldHitPos, hitRadius))
                    {
                        if (shield.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageAmount))
                            return; // No more damage to dish, shields absorbed the blast
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
                if ((m = grid[a.X + a.Y * width]) != null && m.Active)
                    m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageAmount);

                return;
            }

            int firstX = Math.Min(a.X, b.X); // this is the max bounding box range of the scan
            int firstY = Math.Min(a.Y, b.Y);
            int lastX  = Math.Max(a.X, b.X);
            int lastY  = Math.Max(a.Y, b.Y);

            Point cx = WorldToGridLocalPointClipped(worldHitPos); // clip the start, because it's often near an edge
            int hitRadiusToGrid = (int)Math.Ceiling(hitRadius / 16f);

            int minX = cx.X;
            int minY = cx.Y;
            int maxX = cx.X;
            int maxY = cx.Y;
            if ((m = grid[minX + minY * width]) != null && m.Active
                && m.TryDamageExplosiveAndContain(damageSource, worldHitPos, hitRadius, ref damageAmount))
            {
                 return; // withstood the explosion
            }
            // spread out the damage in 4 directions

            if (Loyalty.WeAreRemnants)
                Log.Info("ss");

            if (minX > firstX) // test all modules to the left
            {
                float explosiveDamage = damageAmount;
                for (int y = minY; y <= lastY; ++y)
                {
                    int x = minX;
                    while (x > firstX)
                    {
                        --x;
                        if ((m = grid[x + y * width]) != null && m.Active)
                        {
                            if (m.TryDamageExplosiveAndContain(damageSource, worldHitPos, hitRadius, ref explosiveDamage))
                                break; // withstood the explosion
                        }
                    }
                }
            }

            if (maxX < lastX) // test all modules to the right
            {
                float explosiveDamage = damageAmount;
                for (int y = minY; y <= lastY; ++y)
                {
                    int x = maxX;
                    while (x < lastX)
                    {
                        ++x;
                        if ((m = grid[x + y * width]) != null && m.Active)
                        {
                            if (m.TryDamageExplosiveAndContain(damageSource, worldHitPos, hitRadius, ref explosiveDamage))
                                break; // withstood the explosion
                        }
                    }
                }
            }

            if (minY > firstY) // test all top modules
            {
                float explosiveDamage = damageAmount;
                for (int x = minX; x <= lastX; ++x)
                {
                    int y = minY;
                    while (y > firstY)
                    {
                        --y;
                        if ((m = grid[x + y * width]) != null && m.Active)
                        {
                            if (m.TryDamageExplosiveAndContain(damageSource, worldHitPos, hitRadius, ref explosiveDamage))
                                break; // withstood the explosion
                        }
                    }
                }
            }

            if (maxY < lastY) // test all bottom modules
            {
                float explosiveDamage = damageAmount;
                for (int x = minX; x <= lastX; ++x)
                {
                    int y = maxY;
                    while (y < lastY)
                    {
                        ++y;
                        if ((m = grid[x + y * width]) != null && m.Active)
                        {
                            if (m.TryDamageExplosiveAndContain(damageSource, worldHitPos, hitRadius, ref explosiveDamage))
                                break; // withstood the explosion
                        }
                    }
                }
            }


            /*
            for (;;)
            {

                bool didExpand = false;
                if (minX > firstX) // test all modules to the left
                { 
                    --minX;
                    didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }

                if (maxX < lastX) // test all modules to the right
                { 
                    ++maxX;
                    didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }
                
                if (minY > firstY) // test all top modules
                { 
                    --minY;
                    didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + minY * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }

                if (maxY < lastY) // test all bottom modules
                { 
                    ++maxY;
                    didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + maxY * width]) != null && m.Active)
                            m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageTracker);
                }

                if (!didExpand) 
                    return; // ;looks like we're done here!
            }*/
        }

        void DebugGridStep(Vector2 p, Color color)
        {
            Vector2 gridWorldPos = GridLocalPointToWorld(GridLocalToPoint(p)) + new Vector2(8f);
            Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, gridWorldPos, 4f, color.Alpha(0.33f), 2.0f);
        }

        void DebugGridStep(Vector2 a, Vector2 b, Color color, float width = 1f)
        {
            Vector2 worldPosA = GridLocalPointToWorld(GridLocalToPoint(a)) + new Vector2(8f);
            Vector2 worldPosB = GridLocalPointToWorld(GridLocalToPoint(b)) + new Vector2(8f);
            Universe.DebugWin?.DrawLine(DebugModes.SpatialManager, worldPosA, worldPosB, width, color.Alpha(0.75f), 2.0f);
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
                    //    Universe.DebugWin?.DrawCircle(DebugModes.SpatialManager, m.Position, 6f, Color.IndianRed.Alpha(0.5f), 3f);
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

            // move [a] completely out of bounds to prevent attacking central modules
            Vector2 dir = (endPos - startPos).Normalized();
            Vector2 a = WorldToGridLocal(startPos - dir * (Radius * 2));
            Vector2 b = WorldToGridLocal(endPos);
            if (MathExt.ClipLineWithBounds(GridWidth*16f, GridHeight*16f, a, b, ref a, ref b)) // guaranteed bounds safety
            {
                ShipModule module = WalkModuleGrid(a, b);
                if (module == null)
                    return shield;

                if (shield == null || module.Position.Distance(startPos) < shieldHitDist)
                    return module; // module was closer, so should be hit first
            }
            return shield;
        }


        // Find an Active ShipModule which collide with a this wide RAY
        // Direction must be normalized!!
        // Results are sorted by distance
        // todo Align this with RayHitTestSingle
        public ShipModule RayHitTestNextModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius, bool ignoreShields)
        {
            Vector2 endPos = startPos + direction * distance;
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
                    {
                        // get covering shields to damage them first
                        if (!ignoreShields)
                        {
                            ShipModule[] shields = GetAllActiveShieldsCoveringModule(m);
                            if (shields.Length > 0)
                                return shields[0];
                        }

                        return m;
                    }
                }
            }
            return null;
        }


        // Refactor by RedFox: Picks a random internal module in search range (squared) of the projectile
        // -- Higher crew level means the missile will pick the most optimal target module ;) --
        ShipModule TargetRandomInternalModule(Vector2 projPos, int level, float sqSearchRange)
        {
            ShipModule[] modules = ModuleSlotList.Filter(m => m.Active && projPos.SqDist(m.Position) < sqSearchRange);
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
            Vector2 center    = source.Owner?.Position ?? source.Origin;
            int level         = source.Owner?.Level  ?? 0;
            float searchRange = source.BaseRange + 100;
            return TargetRandomInternalModule(center, level, searchRange*searchRange);
        }

        // This is called for initial missile guidance ChooseTarget(), so range is not that important
        public ShipModule GetRandomInternalModule(Projectile source)
        {
            Vector2 projPos = source.Owner?.Position ?? source.Position;
            int level       = source.Owner?.Level  ?? 0;
            float searchRange = projPos.SqDist(Position) + 48*48; // only pick modules that are "visible" to the projectile
            return TargetRandomInternalModule(projPos, level, searchRange);
        }

    }
}
