using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public ModuleGridFlyweight Grid;
        ShipModule[] ModuleSlotList;
        public ModuleGridState GetGridState() => new(Grid, ModuleSlotList);

        public PowerGrid PwrGrid;
        public ExternalSlotGrid Externals;

        // This is the total number of Slots on the ships
        // It does not depend on the number of modules, and is always a constant
        public int SurfaceArea => Grid.SurfaceArea;
        public int GridWidth => Grid.Width;
        public int GridHeight => Grid.Height;
        public Point GridSize => new(Grid.Width, Grid.Height);

        public IEnumerable<ShipModule> GetShields() => Grid.GetShields(ModuleSlotList);
        public IEnumerable<ShipModule> GetAmplifiers() => Grid.GetAmplifiers(ModuleSlotList);
        public ShipModule[] Modules => ModuleSlotList;
        public bool HasModules => ModuleSlotList != null && ModuleSlotList.Length != 0;

        void CreateModuleGrid(IShipDesign design, bool isTemplate, bool shipyardDesign)
        {
            ShipGridInfo info = design.GridInfo;

        #if DEBUG
            if (isTemplate && !shipyardDesign)
            {
                var modulesInfo = new ShipGridInfo(ModuleSlotList);
                if (modulesInfo.SurfaceArea != info.SurfaceArea ||
                    modulesInfo.Size != info.Size)
                {
                    Log.Warning($"BaseHull mismatch: {modulesInfo} != {info}. Broken Design={Name}");
                }
            }
        #endif

            Grid = design.Grid;
            PwrGrid = new PowerGrid(this, Grid);
            Radius = Grid.Radius;
            Externals = new ExternalSlotGrid(GetGridState());
        }

        // updates the isExternal status of a module,
        // depending on whether it died or resurrected
        public void UpdateExternalSlots(ShipModule module)
        {
            Externals.Update(GetGridState(), module);
        }

        public ShipModule GetModuleAt(Point gridPos)
        {
            return Grid.Get(ModuleSlotList, gridPos);
        }

        public ShipModule GetModuleAt(int gridPosX, int gridPosY)
        {
            return Grid.Get(ModuleSlotList, gridPosX, gridPosY);
        }

        public ShipModule GetModuleAt(int gridIndex)
        {
            return Grid.Get(ModuleSlotList, gridIndex);
        }

        /// <returns>First active shield which covers given grid pos</returns>
        public ShipModule GetActiveShieldAt(int gridPosX, int gridPosY)
        {
            return Grid.GetActiveShield(ModuleSlotList, gridPosX, gridPosY);
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
        // Tested in ModuleGridFlyweightTests
        public ShipModule HitTestShields(Vector2 worldHitPos, float hitRadius)
        {
            Point gridPos = WorldToGridLocalPointClipped(worldHitPos);
            return Grid.HitTestShieldsAt(ModuleSlotList, gridPos, worldHitPos, hitRadius);
        }

        public ShipModule HitTestShieldsLocal(Vector2 localHitPos, float hitRadius)
        {
            Point gridPos = Grid.GridLocalToPoint(localHitPos);
            Vector2 worldHitPos = GridLocalToWorld(localHitPos);
            return Grid.HitTestShieldsAt(ModuleSlotList, gridPos, worldHitPos, hitRadius);
        }

        // Gets the strongest shield currently covering internalModule
        bool IsCoveredByShield(ShipModule internalModule, out ShipModule shield)
        {
            float maxPower = 0f;
            shield = null;
            foreach (ShipModule m in GetShields())
            {
                float power = m.ShieldPower;
                if (power > maxPower && m.HitTestShield(internalModule.Position, internalModule.Radius))
                    shield = m;
            }
            return shield != null;
        }

        // Converts a world position to a grid local position (such as [16f,32f])
        // TESTED in ShipModuleGridTests
        public Vector2 WorldToGridLocal(in Vector2 worldPoint)
        {
            Vector2 offset = worldPoint - Position;
            return RotatePoint(offset.X, offset.Y, -Rotation) + Grid.GridLocalCenter;
        }

        // A specific variation of RadMath.RotatePoint, with additional Rounding logic
        static Vector2 RotatePoint(double x, double y, double radians)
        {
            double s = Math.Sin(radians);
            double c = Math.Cos(radians);
            double rotatedX = c*x - s*y;
            double rotatedY = s*x + c*y;
            // round 63.999997 and 64.000002 into 64
            rotatedX = Math.Round(rotatedX, 3);
            rotatedY = Math.Round(rotatedY, 3);
            return new Vector2(rotatedX, rotatedY);
        }
        
        // Converts a world position to a grid point such as [1,2]
        // TESTED in ShipModuleGridTests
        public Point WorldToGridLocalPoint(in Vector2 worldPoint)
        {
            Vector2 gridLocal = WorldToGridLocal(worldPoint);
            Point gridPoint = Grid.GridLocalToPoint(gridLocal);
            return gridPoint;
        }
        
        // Converts a world position to a grid point such as [1,2]
        // CLIPS the value in range of [0, GRIDSIZE-1]
        // TESTED in ShipModuleGridTests
        public Point WorldToGridLocalPointClipped(in Vector2 worldPoint)
        {
            return Grid.ClipLocalPoint(WorldToGridLocalPoint(worldPoint));
        }

        // Converts a grid-local pos to a grid point
        // TESTED in ShipModuleGridTests
        public Point GridLocalToPoint(in Vector2 localPos)
        {
            return Grid.GridLocalToPoint(localPos);
        }
        
        // Converts a grid-local pos to a grid point AND clips it to grid bounds
        // TESTED in ShipModuleGridTests
        public Point GridLocalToPointClipped(in Vector2 localPos)
        {
            return Grid.GridLocalToPointClipped(localPos);
        }

        // Converts a grid-local pos to world pos
        // TESTED in ShipModuleGridTests
        public Vector2 GridLocalToWorld(in Vector2 localPoint)
        {
            Vector2 centerLocal = localPoint - Grid.GridLocalCenter;
            return RotatePoint(centerLocal.X, centerLocal.Y, Rotation) + Position;
        }

        // Converts a grid-local POINT to world pos
        // TESTED in ShipModuleGridTests
        public Vector2 GridLocalPointToWorld(Point gridLocalPoint)
        {
            return GridLocalToWorld(new Vector2(gridLocalPoint.X * 16f, gridLocalPoint.Y * 16f));
        }

        Vector2 GridCellCenterToWorld(int x, int y)
        {
            return GridLocalToWorld(new Vector2(x * 16f + 8f, y * 16f + 8f));
        }

        // an out of bounds clipped point would be in any of the extreme corners.
        bool ClippedLocalPointInBounds(Point point)
        {
            return 0 <= point.X && point.X < Grid.Width
                && 0 <= point.Y && point.Y < Grid.Height
                && point != Point.Zero
                && (point.X < Grid.Width - 1 || point.Y < Grid.Height - 1)
                && (point.X > 0 || point.Y < Grid.Height - 1)
                && (point.Y > 0 || point.X < Grid.Width - 1);
        }



        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        // @note Ignores shields !
        public ShipModule FindClosestModule(Vector2 worldPoint)
        {
            if (Externals.NumModules == 0)
                return null;

            Point pt = WorldToGridLocalPoint(worldPoint);
            pt = Grid.ClipLocalPoint(pt);

            ShipModule m;
            ModuleGridState gs = GetGridState();
            if ((m = Externals.Get(gs, pt.X, pt.Y)) != null && m.Active) return m;

            int minX = pt.X, minY = pt.Y, maxX = pt.X, maxY = pt.Y;
            int lastX = Grid.Width - 1, lastY = Grid.Height - 1;
            for (;;)
            {
                bool didExpand = false;
                if (minX > 0f) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = Externals.Get(gs, minX, y)) != null && m.Active)
                            return m;
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = Externals.Get(gs, maxX, y)) != null && m.Active)
                            return m;
                }
                if (minY > 0f) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = Externals.Get(gs, x, minY)) != null && m.Active)
                            return m;
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = Externals.Get(gs, x, maxY)) != null && m.Active)
                            return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }


        // find the first module that falls under the hit radius at given position
        public ShipModule HitTestSingle(Vector2 worldHitPos, float hitRadius, bool ignoreShields = false)
        {
            if (Externals.NumModules == 0)
                return null;

            if (!ignoreShields)
            {
                ShipModule shield = HitTestShields(worldHitPos, hitRadius);
                if (shield != null) return shield;
            }

            Point a  = WorldToGridLocalPoint(worldHitPos - new Vector2(hitRadius));
            Point b  = WorldToGridLocalPoint(worldHitPos + new Vector2(hitRadius));
            bool inA = Grid.LocalPointInBounds(a);
            bool inB = Grid.LocalPointInBounds(b);
            if (!inA && !inB)
                return null;
            if (!inA) a = Grid.ClipLocalPoint(a);
            if (!inB) b = Grid.ClipLocalPoint(b);

            ShipModule m;
            if (a == b)
            {
                if ((m = GetModuleAt(a)) != null && m.Active)
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
            if ((m = GetModuleAt(minX, minY)) != null && m.Active) return m;

            for (;;)
            {
                bool didExpand = false;
                if (minX > firstX) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = GetModuleAt(minX, y)) != null && m.Active) return m;
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = GetModuleAt(maxX, y)) != null && m.Active) return m;
                }
                if (minY > firstY) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = GetModuleAt(x, minY)) != null && m.Active) return m;
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = GetModuleAt(x, maxY)) != null && m.Active) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }

        // 1. A Projectile has hit the module and exploded
        // 2. A ShipModule like Reactor 2x2 has exploded
        // 3. A Ship has exploded and this is the closest affected module
        public void DamageExplosive(GameObject damageSource, float damageAmount,
                                    Vector2 worldHitPos, float hitRadius, bool ignoresShields)
        {
            // Reduces the effective explosion radius on ships with ExplosiveRadiusReduction bonus
            if (Loyalty.data.ExplosiveRadiusReduction > 0f)
                hitRadius *= 1f - Loyalty.data.ExplosiveRadiusReduction;

            if (!ignoresShields)
            {
                ShipModule shield = HitTestShields(worldHitPos, hitRadius);
                if (shield != null && shield.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageAmount))
                    return; // No more damage to dish, shields absorbed the blast
            }

            Point a = WorldToGridLocalPointClipped(worldHitPos - new Vector2(hitRadius));
            Point b = WorldToGridLocalPointClipped(worldHitPos + new Vector2(hitRadius));
            if (!ClippedLocalPointInBounds(a) && !ClippedLocalPointInBounds(b))
            {
                if (!Grid.LocalPointInBounds(WorldToGridLocalPoint(worldHitPos)))
                    return;
            }

            ShipModule m;
            if (a == b)
            {
                if ((m = GetModuleAt(a)) != null && m.Active)
                    m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageAmount);

                return;
            }

            int firstX = Math.Min(a.X, b.X); // this is the max bounding box range of the scan
            int firstY = Math.Min(a.Y, b.Y);
            int lastX  = Math.Max(a.X, b.X);
            int lastY  = Math.Max(a.Y, b.Y);

            Point cx = WorldToGridLocalPointClipped(worldHitPos); // clip the start, because it's often near an edge

            int minX = cx.X;
            int minY = cx.Y;
            int maxX = cx.X;
            int maxY = cx.Y;
            if ((m = GetModuleAt(minX, minY)) != null && m.Active
                && m.DamageExplosive(damageSource, worldHitPos, hitRadius, ref damageAmount))
            {
                 return; // withstood the explosion
            }

            // spread out the damage in 4 directions - increasing the expansion box until reaching  the scan bounding box
            // the explosion, however, will not damage modules if an inner module survived the damage, meaning the 
            // explosion path can be contained - this is the "if (innerModule == null || !innerModule.Active)"
            // in each direction
            for (;;)
            {

                bool didExpand = false;
                if (minX > firstX) // test all modules to the left
                { 
                    --minX;
                    didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = GetModuleAt(minX, y)) != null && m.Active)
                        {
                            ShipModule innerModule = GetModuleAt(minX+1, y);
                            if (innerModule == null || !innerModule.Active)
                                m.DamageExplosive(damageSource, worldHitPos, hitRadius, damageAmount);
                        }
                }

                if (maxX < lastX) // test all modules to the right
                { 
                    ++maxX;
                    didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = GetModuleAt(maxX, y)) != null && m.Active)
                        {
                            ShipModule innerModule = GetModuleAt(maxX-1, y);
                            if (innerModule == null || !innerModule.Active)
                                m.DamageExplosive(damageSource, worldHitPos, hitRadius, damageAmount);
                        }
                }
                
                if (minY > firstY) // test all top modules
                { 
                    --minY;
                    didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = GetModuleAt(x, minY)) != null && m.Active)
                        {
                            ShipModule innerModule = GetModuleAt(x, minY+1);
                            if (innerModule == null || !innerModule.Active)
                                m.DamageExplosive(damageSource, worldHitPos, hitRadius, damageAmount);
                        }
                }

                if (maxY < lastY) // test all bottom modules
                { 
                    ++maxY;
                    didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = GetModuleAt(x, maxY)) != null && m.Active)
                        {
                            ShipModule innerModule = GetModuleAt(x, maxY-1);
                            if (innerModule == null || !innerModule.Active)
                                m.DamageExplosive(damageSource, worldHitPos, hitRadius, damageAmount);
                        }
                }

                if (!didExpand) 
                    return; // Looks like we're done here!
            }
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
            Point pos = GridLocalToPoint(start);
            Point end = GridLocalToPoint(start + step);
            if (!Grid.LocalPointInBounds(pos) || !Grid.LocalPointInBounds(end))
                return null; // we're walking out of bounds

            // @note We don't check grid at [pos], because we assume prev call checked it
            if (pos.IsDiagonalTo(end))
            {
                // check a module at the same Y height as final point
                // this forces us to always take an L shaped step instead of diagonal \
                var neighbor = new Point(pos.X, end.Y);
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(new Vector2(start.X, endPos.Y), Color.Yellow);

                ShipModule mb = GetModuleAt(neighbor.X, neighbor.Y);
                if (mb != null && mb.Active)
                {
                    //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                    //    DebugGridStep(start, new Vector2(start.X, endPos.Y), Color.Cyan, 4f);
                    return mb;
                }
            }

            //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
            //    DebugGridStep(endPos, Color.LightGreen);

            ShipModule mc = GetModuleAt(end.X, end.Y);
            if (mc != null && mc.Active)
            {
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(start, endPos, Color.HotPink, 4f);
                return mc;
            }
            return null;
        }

        // perform a raytrace from point a to point b, visiting all grid points between them!
        ShipModule WalkModuleGrid(in Vector2 a, in Vector2 b, float rayRadius, bool ignoreShields)
        {
            Vector2 pos = a;

            // sometimes we directly enter the grid and hit a module:
            Point enter = GridLocalToPoint(pos);
            if (!ignoreShields)
            {
                ShipModule se = HitTestShieldsLocal(pos, rayRadius);
                if (se != null) return se;
            }

            ShipModule me = GetModuleAt(enter.X, enter.Y);
            if (me != null && me.Active)
            {
                //if (DebugInfoScreen.Mode == DebugModes.SpatialManager)
                //    DebugGridStep(pos - step, pos, Color.DarkGoldenrod);
                return me;
            }

            Vector2 delta = b - a;
            Vector2 step = delta.Normalized(16f);

            int n = (int)(delta.Length() / 16f);
            for (; n >= 0; --n, pos += step)
            {
                if (!ignoreShields)
                {
                    ShipModule s = HitTestShieldsLocal(pos, rayRadius);
                    if (s != null) return s;
                }

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

        // guaranteed bounds safety, clips GridLocal points [a] and [b] into the local grid
        public bool ClipLineToGrid(Vector2 a, Vector2 b, ref Vector2 ca, ref Vector2 cb)
        {
            return MathExt.ClipLineWithBounds(
                (Grid.Width*16) - 0.01f, (Grid.Height*16) - 0.01f, a, b, ref ca, ref cb);
        }

        // This is used by initial hit-test in NarrowPhase
        // The hope is that most calls to this return `null`
        public ShipModule RayHitTestSingle(Vector2 startPos, Vector2 endPos,
                                           float rayRadius, bool ignoreShields)
        {
            // move [a] completely out of bounds to prevent attacking central modules
            Vector2 dir = (endPos - startPos).Normalized();
            Vector2 a = WorldToGridLocal(startPos - dir * (Radius * 2));
            Vector2 b = WorldToGridLocal(endPos);
            if (ClipLineToGrid(a, b, ref a, ref b))
            {
                // Shields take priority, then unshielded modules
                ShipModule module = WalkModuleGrid(a, b, rayRadius, ignoreShields);
                if (module != null)
                    return module;
            }
            return null;
        }

        // Enumerate through ModuleGrid, yielding modules
        // this is used by ArmorPiercingTouch
        public IEnumerable<ShipModule> RayHitTestWalkModules(Vector2 startPos, Vector2 direction,
                                                             float distance, bool ignoreShields)
        {
            Vector2 endPos = startPos + direction * distance;
            Vector2 a = WorldToGridLocal(startPos);
            Vector2 b = WorldToGridLocal(endPos);
            if (ClipLineToGrid(a, b, ref a, ref b))
            {
                ShipModule prevModule = null;
                Vector2 pos = a;
                Vector2 delta = b - a;
                Vector2 step = delta.Normalized(16f);
                int n = (int)(delta.Length() / 16f);
                for (; n > 0; --n, pos += step)
                {
                    Point p = GridLocalToPoint(pos);

                    // get covering shields to damage them first
                    // there might not be any modules under [p], but trigger shields anyways
                    if (!ignoreShields)
                    {
                        // we need world position for the shield hit-test
                        // TODO: refactor HitTestShields so that worldHitPos is not needed
                        Vector2 worldHitPos = GridLocalToWorld(pos);
                        while (true)
                        {
                            ShipModule shield = Grid.HitTestShieldsAt(ModuleSlotList, p, worldHitPos, 8f);
                            if (shield != null) // this will only return active shields
                                yield return shield;
                            else
                                break; // no more active shields under [p]
                        }
                    }

                    ShipModule m = GetModuleAt(p);
                    if (m != null && m != prevModule && m.Active)
                    {
                        yield return m;
                        prevModule = m;
                    }
                }
            }
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
