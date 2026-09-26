using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
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
        public IEnumerable<ShipModule> GetActiveShields() => Grid.GetActiveShields(ModuleSlotList);
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
            if (!Active) return null;
            Point gridPos = WorldToGridLocalPointClipped(worldHitPos);
            return Grid.HitTestShieldsAt(ModuleSlotList, gridPos, hitRadius);
        }

        // Gets the strongest shield currently covering internalModule
        bool IsCoveredByShield(ShipModule internalModule, out ShipModule shield)
        {
            float maxPower = 0f;
            shield = null;
            foreach (ShipModule m in GetActiveShields())
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

        IEnumerable<ShipModule> GetModulesAt(Point gridPos, bool checkShields)
        {
            return Grid.GetModulesAt(ModuleSlotList, gridPos, checkShields);
        }

        // Enumarates all Shipmodules under (worldPoint, radius) divided to quadrant.
        // starting from the center and in an order for explotion spread.
        //    NW (1) NE (2)
        //    ← ↑ ↑  ↑ ↑ →
        //    ← ↑ ↑  ↑ ↑ → 
        //    ← ← C  C → →

        //    ← ← C  C → →      
        //    ← ↓ ↓  ↓ ↓ →
        //    ← ↓ ↓  ↓ ↓ →
        //    SW (4) SE (3)

        // damage dividers (distance from explosion)
        //    3 3 3 3 3 3
        //    3 2 2 2 2 3 
        //    3 2 1 1 2 3
        //    3 2 1 1 2 3      
        //    3 2 2 2 2 3
        //    3 3 3 3 3 3
        IEnumerable<ModuleQuadrant> EnumModulesQuadrants(Vector2 worldPos, float radius, bool checkShields)
        {
            // Create an optimized integer rectangle
            // a---+
            // |   |
            // +---b
            Vector2 localPos = WorldToGridLocal(worldPos);
            // TODO: find a way to speed up this part
            Point c = GridLocalToPoint(localPos);
            Point a = Grid.GridLocalToPoint(new Vector2(localPos.X - radius, localPos.Y - radius));
            Point b = Grid.GridLocalToPoint(new Vector2(localPos.X + radius, localPos.Y + radius));
            int firstX = a.X, firstY = a.Y;
            int lastX  = b.X, lastY  = b.Y;
            int w = Grid.Width;
            int h = Grid.Height;

            // does the hit test rectangle overlap the grid at all?
            bool overlapsGrid = firstX < w && lastX >= 0 && firstY < h && lastY >= 0;
            if (!overlapsGrid && !checkShields)
                yield break;

            // clip the rectangle to grid bounds
            if (firstX < 0) firstX = 0;
            if (firstY < 0) firstY = 0;
            if (lastX >= w) lastX = w - 1;
            if (lastY >= h) lastY = h - 1;
            if (c.X < 0) c.X = 0; else if (c.X >= w) c.X = w - 1;
            if (c.Y < 0) c.Y = 0; else if (c.Y >= h) c.Y = h - 1;

            // check the first center module
            // this will keep returning shields first, and then underlying module
            foreach (ShipModule m in GetModulesAt(c, checkShields))
                yield return new ModuleQuadrant(m, DamageTransfer.Root, distance: 1, quadrant: 1);

            // special case: radius is very small and could only ever hit 1 slot
            if (firstX == lastX && firstY == lastY)
                yield break;

            int curX, curY;

            // Check Northwest quadrant
            int counter = 0;
            for (int nw = c.X; nw >= firstX; nw--)
            {
                bool diagonalModule = true;
                int distance = counter + 1 ;
                curX = c.X - counter;
                if (curX >= firstX)
                {
                    for (curY = c.Y - counter; curY >= firstY; curY--)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                        {
                            if (diagonalModule)
                            {
                                diagonalModule = false;
                                yield return new ModuleQuadrant(m, DamageTransfer.Diagonal, distance, 1);
                            }
                            else
                            {
                                yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 1);
                            }
                        }
                        diagonalModule = false;
                        distance++;
                    }
                }

                distance = counter + 2;
                curY = c.Y - counter;
                if (curY >= firstY)
                {
                    for (curX = c.X - counter - 1; curX >= firstX; curX--)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                            yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 1);

                        distance++;
                    }
                }

                counter++;
            }

            // Check Northweast quadrant
            counter = 0;
            for (int ne = c.X + 1; ne <= lastX; ne++)
            {
                bool diagonalModule = true;
                int distance = counter + 1;
                curX = c.X + 1 + counter;
                if (curX <= lastX)
                {
                    for (curY = c.Y - counter; curY >= firstY; curY--)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                        {
                            if (diagonalModule)
                            {
                                diagonalModule = false;
                                yield return new ModuleQuadrant(m, DamageTransfer.Diagonal, distance, 2);
                            }
                            else
                            {
                                yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 2);
                            }
                        }
                        diagonalModule = false;
                        distance++;
                    }
                }

                distance = counter + 2;
                curY = c.Y - counter;
                if (curY >= firstY)
                {
                    for (curX = c.X + 2 + counter; curX <= lastX; curX++)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                            yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 2);

                        distance++;
                    }
                }

                counter++;
            }

            // Check Southeast quadrant
            counter = 0;
            for (int se = c.X + 1; se <= lastX; se++)
            {
                bool diagonalModule = true;
                int distance = counter + 1;
                curX = c.X + 1 + counter;
                if (curX <= lastX)
                {
                    for (curY = c.Y + 1 + counter; curY <= lastY; curY++)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                        {
                            if (diagonalModule)
                            {
                                diagonalModule = false;
                                yield return new ModuleQuadrant(m, DamageTransfer.Diagonal, distance, 3);
                            }
                            else
                            {
                                yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 3);
                            }
                        }
                        diagonalModule = false;
                        distance++;
                    }
                }

                distance = counter + 2;
                curY = c.Y + 1 + counter;
                if (curY <= lastY)
                {
                    for (curX = c.X + 2 + counter; curX <= lastX; curX++)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                            yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 3);

                        distance++;
                    }
                }

                counter++;
            }

            // Check Southwest quadrant
            counter = 0;
            for (int sw = c.X; sw >= firstX; sw--)
            {
                bool diagonalModule = true;
                int distance = counter + 1;
                curX = c.X - counter;
                if (curX >= firstX)
                {
                    for (curY = c.Y + 1 + counter; curY <= lastY; curY++)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                        {
                            if (diagonalModule)
                            {
                                diagonalModule = false;
                                yield return new ModuleQuadrant(m, DamageTransfer.Diagonal, distance, 4);
                            }
                            else
                            {
                                yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 4);
                            }
                        }
                        diagonalModule = false;
                        distance++;
                    }
                }

                distance = counter + 2;
                curY = c.Y + 1 + counter;
                if (curY <= lastY)
                {
                    for (curX = c.X - counter - 1; curX >= firstX; curX--)
                    {
                        var p = new Point(curX, curY);
                        foreach (ShipModule m in GetModulesAt(p, checkShields))
                            yield return new ModuleQuadrant(m, DamageTransfer.Orthogonal, distance, 4);

                        distance++;
                    }
                }

                counter++;
            }
        }
        
        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        // @note Ignores shields !
        public ShipModule FindClosestModule(Vector2 worldPos)
        {
            if (!Active) return null;

            // The search rect in EnumModulesQuadrants is worldPos +/- searchRadius;
            // it must reach the ship's grid (centered at Position, extent Radius)
            // even when worldPos is far outside the hull, otherwise the overlap
            // check short-circuits and we get null.
            float searchRadius = worldPos.Distance(Position) + Radius;

            ShipModule closest = null;
            float bestDistSq = float.MaxValue;
            foreach (ModuleQuadrant mq in EnumModulesQuadrants(worldPos, searchRadius, checkShields: false))
            {
                float distSq = mq.Module.Position.SqDist(worldPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    closest = mq.Module;
                }
            }
            return closest;
        }

        /// <summary>
        /// The hull module an external blast enters through: the first one a ray from the blast
        /// towards this ship's center crosses. When the blast sits exactly on the center there is
        /// no direction to trace, so the ship's own facing is used instead.
        /// </summary>
        public ShipModule FindBlastEntryModule(Vector2 explosionCenter)
        {
            Vector2 rayFrom = explosionCenter;
            if (rayFrom.InRadius(Position, 1f))
                rayFrom = Position - Direction * (Radius + 16f);

            return RayHitTestSingle(rayFrom, Position, ignoreShields: true);
        }

        // find the first module that falls under the hit radius at given position
        public ShipModule HitTestSingle(Vector2 worldHitPos, float hitRadius, bool ignoreShields)
        {
            if (!Active) return null;
            foreach (ModuleQuadrant mq in EnumModulesQuadrants(worldHitPos, hitRadius, !ignoreShields))
                return mq.Module;
            return null;
        }

        // 1. A Projectile has hit the module and exploded
        // 2. A ShipModule like Reactor 2x2 has exploded
        // 3. A Ship has exploded and this is the closest affected module
        public void DamageExplosive(GameObject damageSource, float damageAmount,
                                    Vector2 worldHitPos, float hitRadius, bool ignoreShields)
        {
            if (!Active) return;
            // Reduces the effective explosion radius on ships with ExplosiveRadiusReduction bonus
            if (Loyalty.data.ExplosiveRadiusReduction > 0f)
                hitRadius *= 1f - Loyalty.data.ExplosiveRadiusReduction;

            float rootDamage = damageAmount; // damage to the initial module hit
            damageAmount *= 0.25f; // 1/4 damage to each quadrant
            float remainingDamage = damageAmount;
            float diagonalDamage = damageAmount;
            int currentQuadrant = 1;
            int currentDistance = 0;

            // Logic for each quadrant - example here is the nw quadrant
            //    3   3   3 
            //      D ↑   ↑   
            //    3 ← 2   2 
            //          D ↑
            //    3 ← 2 ← 1 

            // If point 1 absorbs the damage it wont spread to other points.  
            // Damage is spread from point 1 to point 3 upwards, then from point 1 to point 3 backwards.
            // Then it will start from module 2 Diagonaly and repeat the logic. 
            // Excess damage is transferred diagonally as well.
            foreach (ModuleQuadrant mq in EnumModulesQuadrants(worldHitPos, hitRadius, !ignoreShields))
            {
                if (mq.Quadrant != currentQuadrant)
                {
                    // starting a new quadrant, reset the damage to the initial damage
                    currentQuadrant = mq.Quadrant;
                    remainingDamage = damageAmount;
                    diagonalDamage = damageAmount;
                }
                else if (mq.Distance < currentDistance)
                {
                    remainingDamage = diagonalDamage; // start checking from diagonal module
                }

                if (mq.Type == DamageTransfer.Root)
                {
                    if (mq.Module.DamageExplosive(damageSource, ref rootDamage))
                        return; // Root module absorbed all the explosion
                }
                else
                {
                    mq.Module.DamageExplosive(damageSource, ref remainingDamage);
                }

                if (mq.Type is DamageTransfer.Diagonal or DamageTransfer.Root)
                    diagonalDamage = remainingDamage;

                currentDistance = mq.Distance;
            }
        }

        // Fraction of a directional blast's damage that splashes each surrounding EXTERNAL
        // armor plate (in addition to the full-strength penetration column along the ray).
        const float LateralSplashFraction = 0.5f;

        // Reused per-explosion to dedup splash targets without allocating. EnumModulesQuadrants
        // yields a module once per covered cell, so without this a wide module (or a shield bubble
        // covering many cells) would be damaged many times over for a single blast.
        [System.ThreadStatic] static HashSet<ShipModule> SplashHitScratch;

        // A projectile struck the hull and exploded. Unlike a radial blast (DamageExplosive),
        // a projectile carries momentum: the blast punches INWARD along the projectile's flight
        // path, and a wide blast also splashes the surrounding external armor plates.
        //   - Penetration: armor in the flight path absorbs up to its current health and only
        //     the EXCESS carries to the module behind it, so internal modules stay shielded
        //     until their covering armor is destroyed (no more armor bypass).
        //   - Lateral splash: external armor around the impact takes falloff damage; internals
        //     are NOT splashed here — they only ever take the penetration excess above.
        public void DamageExplosiveDirectional(GameObject damageSource, float damageAmount,
                                               ShipModule entry, float hitRadius, bool ignoreShields)
        {
            if (!Active || entry == null) return;
            if (Loyalty.data.ExplosiveRadiusReduction > 0f)
                hitRadius *= 1f - Loyalty.data.ExplosiveRadiusReduction;

            // Capture the shield state AT IMPACT: the penetration column below can drain the entry
            // shield to zero this same blast, but per the rule "a blast that hit an active shield
            // stays on the shield layer" the splash must key off whether the shield was live when
            // it was struck - not its leftover power after penetration.
            bool hitActiveShield = !ignoreShields && entry.ShieldsAreActive;

            Vector2 entryPos = entry.Position;
            // Punch along the projectile's flight path; if velocity is unavailable, fall back to
            // punching from the entry module toward the ship interior.
            Vector2 dir = damageSource is Projectile p && p.Velocity.Length() > 0.001f
                        ? p.Velocity.Normalized()
                        : entryPos.DirectionToTarget(Position);

            // 1. PENETRATION along the flight path.
            float penDamage = damageAmount;
            // When the blast struck a live shield, the overflow that breaches it disperses over the
            // depth of the bubble before reaching the hull. Attenuate the excess ONCE, as it crosses
            // from the shield(s) to the first real hull module, by the distance from the bubble's
            // outer impact point to that module (decay range = bubble radius + blast radius). A wide
            // bubble pushes the impact point far out, so little overflow survives; a tight bubble
            // barely attenuates.
            bool attenuateShieldOverflow = hitActiveShield;
            Vector2 shieldImpact = entryPos - dir * entry.ShieldHitRadius;
            foreach (ShipModule m in RayHitTestWalkModules(entryPos, dir, Radius * 2f, ignoreShields))
            {
                if (attenuateShieldOverflow && m.ShieldPowerMax <= 0f)
                {
                    penDamage *= ShipModule.DamageFalloff(shieldImpact, m.Position,
                                     entry.ShieldHitRadius + hitRadius, m.Radius);
                    attenuateShieldOverflow = false;
                }

                if (m.DamageExplosive(damageSource, ref penDamage))
                    break; // armor column absorbed it all — internals untouched
            }

            // 2. LATERAL SURFACE SPLASH for wide blasts, respecting the shield layer.
            //   - If the blast landed on an ACTIVE shield it stays on the shield layer: it spreads
            //     only to other active shields covering the blast area and NEVER leaks onto a hull
            //     module (splash never transfers from a shield to a module).
            //   - If the blast landed on the bare hull it spreads to surrounding EXTERNAL plates,
            //     but any plate an active shield covers is protected by that shield.
            //   - A shield-ignoring projectile splashes external modules directly, as before.
            //   - Each module is hit at most once (the per-cell yields used to multiply the damage
            //     and drain a multi-cell shield in a single blast).
            if (hitRadius >= 16f)
            {
                HashSet<ShipModule> splashed = SplashHitScratch ??= new HashSet<ShipModule>();
                splashed.Clear();

                foreach (ModuleQuadrant mq in EnumModulesQuadrants(entryPos, hitRadius, checkShields: !ignoreShields))
                {
                    ShipModule m = mq.Module;
                    if (m == entry || !splashed.Add(m))
                        continue; // entry took the penetration hit; damage each module at most once

                    if (!ignoreShields && m.ShieldsAreActive)
                    {
                        // an active shield always absorbs splash over the area it covers
                    }
                    else if (hitActiveShield)
                    {
                        continue; // shield-layer blast never damages a non-shield module
                    }
                    else if (!m.IsExternal || (!ignoreShields && HitTestShields(m.Position, 8f) != null))
                    {
                        continue; // hull blast: only exposed external plates — skip internals and shielded plates
                    }

                    float falloff = ShipModule.DamageFalloff(entryPos, m.Position, hitRadius, m.Radius);
                    float splash = damageAmount * LateralSplashFraction * falloff;
                    if (splash > 0f)
                        m.DamageExplosive(damageSource, ref splash);
                }
            }
        }

        // GridLocal walk from localA to localB
        IEnumerable<ShipModule> WalkModuleGrid2(Vector2 localA, Vector2 localB, bool checkShields)
        {
            (Vector2 dir, float len) = (localB - localA).GetDirectionAndLength();

            // we take steps in half-module widths, to make sure we don't jump over modules
            Vector2 step = dir * 8f;

            // reduce the total length by radius of a single module
            int n = (int)((len - 4f) / 8f);

            if (Universe.DebugMode == DebugModes.Targeting)
            {
                Universe.DebugWin?.DrawLine(DebugModes.Targeting,
                    GridLocalToWorld(localA),
                    GridLocalToWorld(localB),
                    2f, Color.IndianRed, lifeTime:0.01f);
            }

            ShipModule prevModule = null;
            var prevPos = new Point(-1000, -1000);

            for (Vector2 pos = localA; n > 0; --n, pos += step)
            {
                if (Universe.DebugMode == DebugModes.Targeting)
                {
                    Universe.DebugWin?.DrawCircle(DebugModes.Targeting, GridLocalToWorld(pos),
                                                  3f, Color.Yellow, lifeTime:0.01f);
                }

                Point p = GridLocalToPoint(pos);
                if (p == prevPos)
                    continue;

                // CONSERVATIVE DIAGONAL TRAVERSAL: half-cell sampling can step straight from a
                // cell to its diagonal neighbor, skipping the two orthogonal cells the line clips
                // at the shared corner. If one of those flanking cells holds a module, the line is
                // really blocked by it - otherwise a shot squeezes through a corner gap and hits a
                // module that is sealed on all four orthogonal sides. Visit the first solid flank so
                // it blocks the shot. One flank is enough to seal; yielding just one also avoids
                // re-emitting a module that spans both the corner and the destination cell.
                if (prevPos.X != -1000 && p.X != prevPos.X && p.Y != prevPos.Y)
                {
                    ShipModule blocker = ActiveHullModuleAt(p.X, prevPos.Y)
                                      ?? ActiveHullModuleAt(prevPos.X, p.Y);
                    if (blocker != null && blocker != prevModule)
                    {
                        prevModule = blocker;
                        if (Universe.DebugMode == DebugModes.Targeting)
                            Universe.DebugWin?.DrawRect(DebugModes.Targeting, blocker.Position,
                                                        blocker.XSize*8f+1f, Rotation, Color.Red, lifeTime:0.01f);
                        yield return blocker;
                    }
                }

                prevPos = p;

                if (Universe.DebugMode == DebugModes.Targeting)
                {
                    Universe.DebugWin?.DrawRect(DebugModes.Targeting, GridCellCenterToWorld(p.X, p.Y),
                                                8f, Rotation, Color.OrangeRed, lifeTime:0.01f);
                }

                foreach (ShipModule m in GetModulesAt(p, checkShields))
                {
                    if (prevModule != m)
                    {
                        prevModule = m;

                        if (Universe.DebugMode == DebugModes.Targeting)
                        {
                            Universe.DebugWin?.DrawRect(DebugModes.Targeting, m.Position,
                                                        m.XSize*8f+1f, Rotation, Color.GreenYellow, lifeTime:0.01f);
                        }

                        yield return m;
                    }
                }
            }
        }

        // Active hull module at a single grid cell, or null. Allocation-free single lookup (avoids
        // the yield-based GetModulesAt enumerator). Used by the conservative diagonal traversal to
        // detect a corner-sealing module. Shields are intentionally not tested here: shield bubbles
        // span many cells and are already caught at every sampled cell by the main walk, so the
        // corner-seal check only needs the hull module that the half-cell sampling skipped.
        ShipModule ActiveHullModuleAt(int gridPosX, int gridPosY)
        {
            ShipModule m = GetModuleAt(gridPosX, gridPosY);
            return m != null && m.Active ? m : null;
        }

        // guaranteed bounds safety, clips GridLocal points [a] and [b] into the local grid
        public bool ClipLineToGrid(Vector2 a, Vector2 b, ref Vector2 ca, ref Vector2 cb)
        {
            return MathExt.ClipLineWithBounds(
                (Grid.Width*16) - 0.01f, (Grid.Height*16) - 0.01f, a, b, ref ca, ref cb);
        }

        // This is used by initial hit-test in NarrowPhase
        // The hope is that most calls to this return `null`
        public ShipModule RayHitTestSingle(Vector2 startPos, Vector2 endPos, bool ignoreShields)
        {
            if (!Active) return null;
            // move [a] completely out of bounds to prevent attacking central modules
            Vector2 offset = (endPos - startPos).Normalized(Radius * 2);
            Vector2 a = WorldToGridLocal(startPos - offset);
            Vector2 b = WorldToGridLocal(endPos);
            if (ClipLineToGrid(a, b, ref a, ref b))
            {
                foreach (ShipModule m in WalkModuleGrid2(a, b, !ignoreShields))
                    return m;
            }
            return null;
        }

        // Enumerate through ModuleGrid, yielding modules
        // this is used by ArmorPiercingTouch
        public IEnumerable<ShipModule> RayHitTestWalkModules(Vector2 startPos, Vector2 direction,
                                                             float distance, bool ignoreShields)
        {
            if (!Active) yield break;
            Vector2 endPos = startPos + direction * distance;
            Vector2 a = WorldToGridLocal(startPos);
            Vector2 b = WorldToGridLocal(endPos);

            // this clips the line within grid bounds, but the line will be touching the bounds
            if (ClipLineToGrid(a, b, ref a, ref b))
            {
                foreach (ShipModule m in WalkModuleGrid2(a, b, !ignoreShields))
                    yield return m;
            }
        }

        // Refactor by RedFox: Picks a random internal module in search range (squared) of the projectile
        // -- Higher crew level means the missile will pick the most optimal target module ;) --
        ShipModule TargetRandomInternalModule(Vector2 projPos, int level, float sqSearchRange)
        {
            if (projPos.InRadius(Position, Radius+50))
                return null; // Dont shoot on top of us!

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
            return Loyalty.Random.Item(modules, limit);
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

    public struct ModuleQuadrant
    {
        public ShipModule Module;
        public DamageTransfer Type;
        public int Distance;
        public int Quadrant;
        public ModuleQuadrant(ShipModule module, DamageTransfer type, int distance, int quadrant)
        {
            Module = module;
            Type   = type;
            Distance = distance;
            Quadrant = quadrant;
        }
    }
    public enum DamageTransfer
    {
        Orthogonal,
        Diagonal,
        Root
    }
}
