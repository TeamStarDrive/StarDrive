using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162

namespace Ship_Game.Gameplay
{
    public sealed unsafe class SpatialManager : IDisposable
    {
        private readonly Array<Ship>       Ships       = new Array<Ship>();
        private readonly Array<Projectile> Projectiles = new Array<Projectile>();
        private readonly Array<Asteroid>   Asteroids   = new Array<Asteroid>();
        private readonly Array<Beam>       Beams       = new Array<Beam>();
        private readonly Array<GameplayObject> AllObjects = new Array<GameplayObject>();

        private float BucketUpdateTimer;
        private int Size;
        private Vector2 UpperLeftBound;
        private PoolArrayGridU16 Buckets;
        private int CellSize;
        private DynamicMemoryPool MemoryPool;

        private const bool UseQuadTree = false;
        private Quadtree QuadTree;
        private readonly Array<SpatialCollision> Collisions = new Array<SpatialCollision>();

        public void Setup(float universeRadius)
        {
            float universeWidth = universeRadius * 2f;
            const float cellSize = 15000f;
            UpperLeftBound.X = 0f - universeRadius;
            UpperLeftBound.Y = 0f - universeRadius;
            Size     = (int)universeWidth / (int)cellSize;
            CellSize = (int)cellSize;

            QuadTree = new Quadtree(universeWidth);

            if (MemoryPool == null)
                MemoryPool = new DynamicMemoryPool();
            else
                MemoryPool.Reset();

            Buckets = MemoryPool.NewArrayGrid(Size * Size);

            Log.Info($"SpatialManager universeWidth: {universeWidth}  grid: {Size}x{Size}  gridbuckets: {Buckets.Count}");
        }

        public void Destroy()
        {
            Ships.Clear();
            Projectiles.Clear();
            Asteroids.Clear();
            Beams.Clear();

            MemoryPool?.Dispose(ref MemoryPool);
            Buckets.Count = 0;
            Buckets.Items = null;
            Collisions.Clear();

            QuadTree?.Dispose(ref QuadTree);
        }

        private void ClearBuckets()
        {
            MemoryPool.Reset(); // reset the pools to their default max-available state
            Buckets = MemoryPool.NewArrayGrid(Size * Size);
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~SpatialManager()
        {
            Destroy();
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            QuadTree.DebugVisualize(screen);
            return;

            PoolArrayU16** allBuckets = Buckets.Items;
            if (allBuckets == null) // destroyed
                return;

            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topleft  = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botright = screen.UnprojectToWorldPosition(screenSize);

            int cellSize = CellSize;
            int size = Size;
            int minX = (int)((topleft.X  - UpperLeftBound.X) / cellSize);
            int maxX = (int)((botright.X - UpperLeftBound.X) / cellSize);
            int minY = (int)((topleft.Y  - UpperLeftBound.Y) / cellSize);
            int maxY = (int)((botright.Y - UpperLeftBound.Y) / cellSize);
            if (minX < 0) minX = 0; else if (minX >= size) minX = size - 1;
            if (maxX < 0) maxX = 0; else if (maxX >= size) maxX = size - 1;
            if (minY < 0) minY = 0; else if (minY >= size) minY = size - 1;
            if (maxY < 0) maxY = 0; else if (maxY >= size) maxY = size - 1;

            Vector2 cellrect   = new Vector2(cellSize, cellSize);
            Vector2 celloffset = UpperLeftBound + cellrect*0.5f;

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    PoolArrayU16* bucket = allBuckets[y * size + x];
                    if (bucket == null)
                        continue;

                    Vector2 cellpos = celloffset + new Vector2(x,y)*cellSize;
                    screen.DrawRectangleProjected(cellpos, cellrect, 0f, Color.SaddleBrown, 1f);

                    for (int i = 0; i < bucket->Count; ++i)
                    {
                        GameplayObject go = AllObjects[bucket->Items[i]];
                        if (go == null) // this is allowed by object removal rules
                            continue;
                        screen.DrawRectangleProjected(go.Center, new Vector2(go.Radius*2, go.Radius*2), 0f, Color.MediumVioletRed);
                    }
                }
            }
        }

        private static bool IsSpatialType(GameplayObject obj)
            => obj is Ship || obj is Projectile/*also Beam*/;

        public void Add(GameplayObject obj)
        {
            if (obj == null)
            {
                Log.Error("SpatialManager null object");
                return;
            }

            if (obj.SpatialIndex != -1) // the object is already in a SpatialManager
            {
                if (obj.SpatialIndex != -1 && AllObjects.ContainsRef(obj))
                    return; // this SpatialManager already contains this object

                Log.Error("SpatialManager cannot add object {0} because it's in another SpatialManager", obj);
                return;
            }

            if (!IsSpatialType(obj))
                return; // not a supported spatial manager type. just ignore it

            if (obj is Ship ship)              Ships.Add(ship);
            else if (obj is Beam beam)         Beams.Add(beam);
            else if (obj is Projectile proj)   Projectiles.Add(proj);
            else if (obj is Asteroid asteroid) Asteroids.Add(asteroid);

            int idx = AllObjects.Count;
            if (idx >= ushort.MaxValue)
                Log.Error("SpatialManager maximum number of support objects (65536) exceeded! Fatal error!");

            obj.SpatialIndex = idx;
            AllObjects.Add(obj);

            QuadTree.Insert(obj);
            if (Buckets.Count > 0) PlaceIntoBucket(obj, idx);
        }

        public void Remove(GameplayObject obj)
        {
            if (obj == null)
            {
                Log.Error("SpatialManager null object");
                return;
            }

            int idx = obj.SpatialIndex;
            if (idx == -1)
                return; // not in any SpatialManagers, so Remove is no-op

            QuadTree.Remove(obj);
            RemoveByIndex(obj, idx);
        }

        private void RemoveByIndex(GameplayObject obj, int index)
        {
            if (obj is Ship ship)              Ships.RemoveSwapLast(ship);
            else if (obj is Beam beam)         Beams.RemoveSwapLast(beam);
            else if (obj is Projectile proj)   Projectiles.RemoveSwapLast(proj);
            else if (obj is Asteroid asteroid) Asteroids.RemoveSwapLast(asteroid);

            AllObjects[index] = null;
            obj.SpatialIndex  = -1;
        }

        public IReadOnlyList<Ship> ShipsList => Ships;

        public void GetDeepSpaceShips(Array<Ship> copyTo)
        {
            copyTo.Clear();
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (ship.Active && ship.InDeepSpace)
                    copyTo.Add(ship);
            }
        }

        public void Update(float elapsedTime, SolarSystem system)
        {
            BucketUpdateTimer += elapsedTime;

            if (BucketUpdateTimer >= 0.5f) // update all buckets
            {
                BucketUpdateTimer = 0.0f;

                RebuildBuckets();
            }

            QuadTree.UpdateAll();

            if (UseQuadTree)
            {
                if (QuadTree.CollideAll(Collisions))
                {
                    HandleCollisions(Collisions.GetInternalArrayItems(), Collisions.Count);
                }
            }
            else
            {
                // move and collide projectiles/beams:
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile projectile = Projectiles[i];
                    if (projectile.Active)
                        MoveAndCollide(projectile);
                }
                for (int i = 0; i < Beams.Count; ++i)
                {
                    Beam beam = Beams[i];
                    if (beam.Active)
                        CollideBeam(beam);
                }
            }
        }

        public void UpdateBucketsOnly(float elapsedTime)
        {
            BucketUpdateTimer += elapsedTime;
            if (BucketUpdateTimer >= 0.5f) // update all buckets
            {
                BucketUpdateTimer = 0.0f;
                RebuildBuckets();
            }
        }

        private void RebuildBuckets()
        {
            ClearBuckets();

            // remove null values and remove inactive objects
            for (int i = 0; i < AllObjects.Count; )
            {
                GameplayObject obj = AllObjects[i];
                if (obj == null)
                {
                    AllObjects.RemoveAtSwapLast(i);
                }
                else if (!obj.Active)
                {
                    RemoveByIndex(obj, i);
                    AllObjects.RemoveAtSwapLast(i);
                }
                else ++i;
            }

            // now rebuild buckets and reassign spatial indexes
            int count = AllObjects.Count;
            GameplayObject[] allObjects = AllObjects.GetInternalArrayItems();

            for (int i = 0; i < count; ++i)
            {
                GameplayObject obj = allObjects[i];
                obj.SpatialIndex = i;
                PlaceIntoBucket(obj, (ushort)i);
            }
        }


        // @note This method is heavily optimized. If you find even more ways to optimize, then please lend a hand!
        // @note All of the code is inlined by hand to maximize performance
        public T[] GetNearby<T>(Vector2 position, float radius) where T : GameplayObject
        {
            PoolArrayU16** allBuckets = Buckets.Items;
            if (allBuckets == null) // destroyed
                return Empty<T>.Array;

            float posX   = position.X - UpperLeftBound.X;
            float posY   = position.Y - UpperLeftBound.Y;
            int cellSize = CellSize;
            int size     = Size;

            int minX = (int)((posX - radius) / cellSize);
            int maxX = (int)((posX + radius) / cellSize);
            int minY = (int)((posY - radius) / cellSize);
            int maxY = (int)((posY + radius) / cellSize);
            if (minX < 0) minX = 0; else if (minX >= size) minX = size - 1;
            if (maxX < 0) maxX = 0; else if (maxX >= size) maxX = size - 1;
            if (minY < 0) minY = 0; else if (minY >= size) minY = size - 1;
            if (maxY < 0) maxY = 0; else if (maxY >= size) maxY = size - 1;

            int spanX = maxX - minX + 1;
            int spanY = maxY - minY + 1;
            int maxSelection = spanX * spanY;

            int numBuckets = 0;
            PoolArrayU16** buckets = stackalloc PoolArrayU16*[maxSelection];
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    PoolArrayU16* bucket = allBuckets[y * size + x];
                    if (bucket != null) // null bucket means 0 objects, so we can exclude this bucket from search
                        buckets[numBuckets++] = bucket;
                }
            }

            if (numBuckets == 0) // all this work for nothing?? pffft.
                return Empty<T>.Array;

            GameplayObject[] allObjects = AllObjects.GetInternalArrayItems();
            if (numBuckets == 1) // fast path
            {
                PoolArrayU16* bucket = buckets[0];
                int count = bucket->Count;
                ushort* objectIds = bucket->Items;
                int numItems = 0; // probe number of valid items first
                for (int i = 0; i < count; ++i)
                {
                    var obj = allObjects[objectIds[i]];
                    if (obj != null && obj.Active && obj is T)
                        ++numItems;
                }

                var objs = new T[numItems]; // we only want to allocate once, to reduce memory pressure
                numItems = 0;
                for (int i = 0; i < count; ++i) {
                    {
                        var obj = allObjects[objectIds[i]];
                        if (obj != null && obj.Active && obj is T item)
                        objs[numItems++] = item;
                    }
                }

                if (objs.Length != numItems)
                    Log.Warning("SpatialManager bucket modified during GetNearby() !!");
                //if (objs.Length > 512)
                //    Log.Warning("SpatialManager GetNearby returned {0} items. Seems a bit inefficient.", objs.Length);
                return objs;
            }

            // probe if selected buckets are empty to avoid unnecessary allocations
            int totalObjects = 0;
            for (int i = 0; i < numBuckets; ++i)
                totalObjects += buckets[i]->Count;
            if (totalObjects == 0) return Empty<T>.Array;

            // create a histogram with all the objectId frequencies
            // while filtering objects based on the requested type of T
            int numAllObjects = AllObjects.Count;
            byte* histogram = stackalloc byte[numAllObjects];
            for (int i = 0; i < numBuckets; ++i)
            {
                PoolArrayU16* bucket = buckets[i];
                int count = bucket->Count;
                ushort* objectIds = bucket->Items;
                for (int j = 0; j < count; ++j)
                {
                    ushort objectId = objectIds[j];
                    if (allObjects[objectId] is T)
                        ++histogram[objectId];
                }
            }

            // how many objectId-s are unique?
            int numUnique = 0;
            for (int i = 0; i < numAllObjects; ++i)
                if (histogram[i] > 0) ++numUnique;

            // reconstruct unique sorted array:
            var unique = new T[numUnique];
            numUnique = 0;
            for (int i = 0; i < numAllObjects; ++i) {
                if (histogram[i] > 0 && allObjects[i] is T item)
                    unique[numUnique++] = item;
            }

            if (unique.Length != numUnique)
                Log.Warning("SpatialManager allObjects array modified during GetNearby() !!");
            //if (unique.Length > 64)
            //    Log.Warning("SpatialManager GetNearby returned {0} items. Seems a bit inefficient.", unique.Length);
            return unique;
        }

        private void PlaceIntoBucket(GameplayObject obj, int objId)
        {
            // Sorry, this is an almost identicaly copy-paste from the above function "GetNearby"
            // All of this for the sole reason of extreme performance optimization. Forgive us :'(
            float posX   = obj.Position.X - UpperLeftBound.X;
            float posY   = obj.Position.Y - UpperLeftBound.Y;
            int cellSize = CellSize;
            int size     = Size;
            float radius = obj.Radius;

            int minX = (int)((posX - radius) / cellSize);
            int maxX = (int)((posX + radius) / cellSize);
            int minY = (int)((posY - radius) / cellSize);
            int maxY = (int)((posY + radius) / cellSize);
            if (minX < 0) minX = 0; else if (minX >= size) minX = size - 1;
            if (maxX < 0) maxX = 0; else if (maxX >= size) maxX = size - 1;
            if (minY < 0) minY = 0; else if (minY >= size) minY = size - 1;
            if (maxY < 0) maxY = 0; else if (maxY >= size) maxY = size - 1;

            int numInsertions = 0;
            PoolArrayU16** allBuckets = Buckets.Items;
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    PoolArrayU16** bucketRef = &allBuckets[y * size + x];
                    MemoryPool.ArrayAdd(bucketRef, (ushort)objId);
                    ++numInsertions;
                }
            }

            // best course of action is to just ignore it and not insert into buckets... ?
            if (numInsertions == 0)
            {
                Log.Error("SpatialManager logic error: object {0} is outside of grid", obj);
            }
        }

        private static void HandleCollisions(SpatialCollision[] collisions, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                SpatialCollision co = collisions[i];
                GameplayObject a = co.Obj1, b = co.Obj2;
                if (b is Projectile)
                {
                    var c = a; // swap to prefer A as a projectile
                    a = b;
                    b = c;
                }

                if (a.Touch(b) || b.Touch(a))
                {
                    a.CollidedThisFrame = true;
                    b.CollidedThisFrame = true;
                }
            }
        }

        private void MoveAndCollide(Projectile projectile)
        {
            GameplayObject[] nearby = GetNearby<GameplayObject>(projectile.Position, 100f);
            foreach (GameplayObject otherObj in nearby)
            {
                if (CollideWith(projectile, otherObj, out GameplayObject collidedWith)
                    && ( projectile.Touch(collidedWith) || collidedWith.Touch(projectile) ))
                {
                    projectile.CollidedThisFrame   = true;
                    collidedWith.CollidedThisFrame = true;
                    return; // projectile collided (and died), no need to continue collisions
                }
            }
        }

        // @todo This method is a complete mess, like all other decompiled parts. It needs a lot of cleanup.
        private void CollideBeam(Beam beam)
        {
            Vector2 beamStart = beam.Source;
            Vector2 beamEnd = beam.Destination;
            float distance = beamEnd.Distance(beamStart);
            if (distance > beam.Range + 10f)
                return;

            GameplayObject beamTarget = beam.Target;
            if (beamTarget is ShipModule targetModule)
                beamTarget = targetModule.GetParent();
            else if (beamTarget is Ship targetShip)
            {
                targetShip.MoveModulesTimer = 2f;
                beam.ActualHitDestination = beamTarget.Center;
                if (beam.DamageAmount >= 0f)
                    return;

                // @todo Why is this here?? Healing stuff shuld be handled elsewhere! like target.Touch(beam)
                foreach (ShipModule module in targetShip.ModuleSlotList)
                {
                    module.Health -= beam.DamageAmount;

                    if (module.Health < 0f)
                        module.Health = 0f;
                    else if (module.Health >= module.HealthMax)
                        module.Health = module.HealthMax;
                }
            }

            Ship[] nearby = GetNearby<Ship>(beamTarget.Position, beamTarget.Radius);
            if (nearby.Length == 0)
                return;
            nearby.SortByDistance(beamStart);

            for (int i = 0; i < nearby.Length; i++)
            {
                Ship ship = nearby[i];
                if (ship.loyalty == beam.Owner?.loyalty)
                    continue; // dont hit allied. need to expand this to actual allies.

                if (ship == beam.Owner && !beam.Weapon.HitsFriendlies) //hits friendlies is in the  wrong place.
                    continue;

                ++GlobalStats.BeamTests;

                ShipModule hit = ship.RayHitTestSingle(beamStart, beamEnd, 8f, beam.IgnoresShields);
                if (!beam.Touch(hit)) // dish out damage if we can
                    continue; // we couldn't :(

                ship.MoveModulesTimer = 2f;

                Vector2 hitPos;
                float hitDistance = hit.Center.RayCircleIntersect(hit.Radius, beamStart, beamEnd);
                if (hitDistance > 0f)
                    hitPos = beamStart + (beamEnd - beamStart).Normalized()*hitDistance;
                else // the beam probably glanced the module from side, so just get the closest point:
                    hitPos = hit.Center.FindClosestPointOnLine(beamStart, beamEnd);

                beam.CollidedThisFrame = hit.CollidedThisFrame = true;
                beam.ActualHitDestination = hitPos;
                return; // beam collision happened
            }
            beam.ActualHitDestination = beamEnd;
        }

        // @return TRUE if a collision happens, false if nothing happened
        private static bool CollideWith(Projectile thisProj, GameplayObject otherObj, out GameplayObject collidedWith)
        {
            collidedWith = null;
            if (thisProj == otherObj || !otherObj.Active || otherObj.CollidedThisFrame)
                return false;

            if (otherObj is Ship otherShip)
            {
                if (thisProj.Loyalty == otherShip.loyalty)
                    return false;

                // if projectile travels more than 16 units (module width) per frame, we need to do ray collision
                if (thisProj.Center.OutsideRadius(otherShip.Center, otherShip.Radius + thisProj.Radius + 128f))
                    return false;

                otherShip.MoveModulesTimer = 2f;

                // give a lot of leeway here; if we fall short, collisions wont work right
                float maxDistPerFrame = thisProj.Velocity.Length() / 30.0f; // this actually depends on the framerate...
                if (maxDistPerFrame > 15.0f) // ray collision
                {
                    Vector2 dir = thisProj.Velocity.Normalized();
                    Vector2 prevPos = thisProj.Center - (dir*maxDistPerFrame);
                    collidedWith = otherShip.RayHitTestSingle(prevPos, thisProj.Center, thisProj.Radius, thisProj.IgnoresShields);
                }
                else
                {
                    collidedWith = otherShip.HitTestSingle(thisProj.Center, thisProj.Radius, thisProj.IgnoresShields);
                }
                return collidedWith != null;
            }
            if (otherObj is Projectile otherProj)
            {
                if (thisProj.Center.OutsideRadius(otherObj.Center, thisProj.Weapon.ProjectileRadius + otherProj.Weapon.ProjectileRadius))
                    return false;
                collidedWith = otherObj;
                return true; // thisProj died
            }

            // finally, generic collision with any kind of 'simple' object
            if (thisProj.Center.OutsideRadius(otherObj.Center, thisProj.Weapon.ProjectileRadius + otherObj.Radius))
                return false;
            collidedWith = otherObj;
            return true; // thisProj died
        }

        // @note This is called every time an exploding projectile hits a target and dies
        //       so everything nearby receives additional splash damage
        //       usually the receipient is only 1 ship, but ships can overlap and cause more results
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0f)
                return;

            Ship[] ships = GetNearby<Ship>(source.Center, damageRadius).FilterBy(ship => ship.Active && !ship.dying);
            ships.SortByDistance(source.Center);

            foreach (Ship ship in ships)
            {
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;
                
                // Check if valid target
                if (source.Owner?.loyalty == ship.loyalty)
                    continue;

                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
                if (ship.loyalty?.data.ExplosiveRadiusReduction > 0f)
                    modifiedRadius *= 1f - ship.loyalty.data.ExplosiveRadiusReduction;

                ship.DamageModulesInRange(source, damageAmount, source.Center, modifiedRadius, source.IgnoresShields);
            }
        }

        // Refactored by RedFox
        public void ExplodeAtModule(GameplayObject damageSource, ShipModule hitModule, 
                                    bool ignoreShields, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;
            Ship shipToDamage = hitModule.GetParent();
            if (shipToDamage.dying || !shipToDamage.Active)
                return;

            shipToDamage.DamageModulesInRange(damageSource, damageAmount, hitModule.Center, damageRadius, ignoreShields);
        }

        // @note This is called quite rarely, so optimization is not a priority
        public void ShipExplode(Ship thisShip, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;

            Vector2 explosionCenter = thisShip.Center;
            GameplayObject[] nearby = GetNearby<GameplayObject>(thisShip.Position, thisShip.Radius);

            for (int i = 0; i < nearby.Length; ++i)
            {
                GameplayObject otherObj = nearby[i];
                if (otherObj == thisShip || !otherObj.Active) continue;
                if (otherObj is Projectile) continue;
                if (!otherObj.Center.InRadius(thisShip.Center, damageRadius + otherObj.Radius))
                    continue;

                if (otherObj is Ship otherShip)
                {
                    ShipModule nearest = otherShip.FindClosestUnshieldedModule(explosionCenter);
                    if (nearest == null)
                        continue;

                    float reducedDamageRadius = damageRadius - explosionCenter.Distance(nearest.Center);
                    if (reducedDamageRadius <= 0.0f)
                        continue;

                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, nearest.Center, damageRadius);
                    ExplodeAtModule(thisShip, nearest, false, damageAmount * damageFalloff, reducedDamageRadius);

                    if (!otherShip.dying)
                    {
                        float rotationImpulse = damageRadius / (float)Math.Pow(otherShip.Mass, 1.3);
                        otherShip.yRotation = otherShip.yRotation > 0.0f ? rotationImpulse : -rotationImpulse;
                        otherShip.yRotation = otherShip.yRotation.Clamp(-otherShip.maxBank, otherShip.maxBank);
                    }

                    // apply some impulse from the explosion
                    Vector2 impulse = 3f * (otherShip.Center - explosionCenter);
                    if (impulse.Length() > 200.0)
                        impulse = impulse.Normalized() * 200f;
                    if (!float.IsNaN(impulse.X))
                        otherObj.Velocity += impulse;
                }
                else
                {
                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, otherObj.Center, damageRadius, 0.25f);
                    otherObj.Damage(thisShip, damageAmount * damageFalloff);
                }
            }
        }
    }
}
