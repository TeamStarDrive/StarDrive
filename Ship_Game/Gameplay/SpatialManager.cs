using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
        private int Width;
        private int Height;
        private Vector2 UpperLeftBound;
        private PoolArrayGridU16 Buckets;
        private int CellSize;
        private bool FineDetail;
        private DynamicMemoryPool MemoryPool;
        private SolarSystem System;

        private void Setup(float sceneWidth, float sceneHeight, float cellSize, float centerX, float centerY)
        {
            UpperLeftBound.X = centerX - (sceneWidth  / 2f);
            UpperLeftBound.Y = centerY - (sceneHeight / 2f);
            Width            = (int)sceneWidth  / (int)cellSize;
            Height           = (int)sceneHeight / (int)cellSize;
            CellSize         = (int)cellSize;

            if (MemoryPool == null)
                MemoryPool = new DynamicMemoryPool();
            else
                MemoryPool.Reset();

            Buckets = MemoryPool.NewArrayGrid(Width * Height);
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
        }

        private void ClearBuckets()
        {
            MemoryPool.Reset(); // reset the pools to their default max-available state
            Buckets = MemoryPool.NewArrayGrid(Width * Height);
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

        public void SetupForDeepSpace(float universeRadiusX, float universeRadiusY)
        {
            float gameScale = Empire.Universe.GameScale;

            // assuming universe size uses radius...
            float universeWidth  = universeRadiusX * 2;
            float universeHeight = universeRadiusY * 2;
            Setup(universeWidth, universeHeight, 150000f * gameScale, 0f, 0f);
            Log.Info("SetupForDeepSpace spaceSize: {0}x{1}  grid: {2}x{3}  size: {4}", universeWidth, universeHeight, Width, Height, Buckets.Count);
        }

        public void SetupForSystem(float gameScale, SolarSystem system, float cellSize = 25000f)
        {
            System = system;
            Setup(200000f * gameScale, 200000f * gameScale, cellSize * gameScale, system.Position.X, system.Position.Y);
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
                if (Contains(obj))
                    return; // this SpatialManager already contains this object

                Log.Error("SpatialManager cannot add object {0} because it's in another SpatialManager", obj);
                return;
            }

            if (!IsSpatialType(obj))
                return; // not a supported spatial manager type. just ignore it

            if (obj is Ship ship)              Ships.Add(ship);
            else if (obj is Projectile proj)   Projectiles.Add(proj);
            else if (obj is Asteroid asteroid) Asteroids.Add(asteroid);
            else if (obj is Beam beam)         Beams.Add(beam);

            int idx = AllObjects.Count;
            if (idx >= ushort.MaxValue)
                Log.Error("SpatialManager maximum number of support objects (65536) exceeded! Fatal error!");

            obj.SpatialIndex = idx;
            AllObjects.Add(obj);

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

            if (idx != -1 && !Contains(obj))
            {
                Log.Error("SpatialManager cannot remove object {0} because it's in another SpatialManager", obj);
                return;
            }

            RemoveByIndex(obj, idx);
        }

        private void RemoveByIndex(GameplayObject obj, int index)
        {
            if (obj is Ship ship)              Ships.RemoveSwapLast(ship);
            else if (obj is Projectile proj)   Projectiles.RemoveSwapLast(proj);
            else if (obj is Asteroid asteroid) Asteroids.RemoveSwapLast(asteroid);
            else if (obj is Beam beam)         Beams.RemoveSwapLast(beam);

            AllObjects[index] = null;
            obj.SpatialIndex  = -1;
        }

        public bool Contains(GameplayObject gameObj)
        {
            return gameObj.SpatialIndex != -1 && AllObjects.ContainsRef(gameObj);
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

                if (system != null) // this is a System.spatialManager
                {
                    if (system.CombatInSystem && system.ShipList.Count > 10)
                    {
                        if (!FineDetail && Buckets.Count < 20 && Projectiles.Count > 0)
                        {
                            SetupForSystem(Empire.Universe.GameScale, system, 5000f);
                            FineDetail = true;
                        }
                    }
                    else if (FineDetail && Buckets.Count > 20 || Projectiles.Count == 0)
                    {
                        SetupForSystem(Empire.Universe.GameScale, system);
                        FineDetail = false;
                    }
                }

                RebuildBuckets();
            }

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
            int numIds = 0;
            int* ids = stackalloc int[4];

            float posX   = position.X - UpperLeftBound.X;
            float posY   = position.Y - UpperLeftBound.Y;
            int cellSize = CellSize;
            int width    = Width;

            int minX = (int)((posX - radius) / cellSize);
            int maxX = (int)((posX + radius) / cellSize);
            int minY = (int)((posY - radius) / cellSize);
            int maxY = (int)((posY + radius) / cellSize);

            PoolArrayU16** buckets = Buckets.Items;
            if (minX == maxX && minY == maxY)
            {
                int id = minY * Width + minX;
                if ((uint)id < Buckets.Count && buckets[id] != null)
                    ids[numIds++] = id;
            }
            else
            {
                int spanX = maxX - minX + 1;
                int spanY = maxY - minY + 1;
                if (spanX > 2 || spanY > 2)
                    Log.Warning("GetNearby bucket selection is larger than 2x2 !!");

                int topRowOffs = minY * width;
                int botRowOffs = maxX * width;

                int size = Buckets.Count;
                // manual loop unrolling with no bounds checking! yay! :D -- to avoid duplicate Id-s looping
                // ids[0] != id is rearranged (in a weird way) to provide statistically faster exclusion (most results give numIds=1)
                int id = topRowOffs + minX;
                if ((uint)id < size && buckets[id] != null)
                    ids[numIds++] = id;

                id = topRowOffs + maxX;
                if (ids[0] != id && (uint)id < size && buckets[id] != null)
                    ids[numIds++] = id;

                id = botRowOffs + maxX;
                if (ids[0] != id && (uint)id < size && buckets[id] != null && ids[1] != id)
                    ids[numIds++] = id;

                id = botRowOffs + maxX;
                if (ids[0] != id && (uint)id < size && buckets[id] != null && ids[1] != id && ids[2] != id)
                    ids[numIds++] = id;
            }


            if (numIds == 0) // all this work for nothing?? pffft.
                return Empty<T>.Array;

            GameplayObject[] allObjects = AllObjects.GetInternalArrayItems();
            if (numIds == 1) // fast path
            {
                PoolArrayU16* bucket = buckets[ids[0]];
                if (bucket == null) return Empty<T>.Array;

                int count = bucket->Count;
                ushort* objectIds = bucket->Items;
                int numItems = 0; // probe number of valid items first
                for (int i = 0; i < count; ++i)
                    if (allObjects[objectIds[i]] is T)
                        ++numItems;

                var objs = new T[numItems]; // we only want to allocate once, to reduce memory pressure
                numItems = 0;
                for (int i = 0; i < count; ++i) {
                    if (allObjects[objectIds[i]] is T item)
                        objs[numItems++] = item;
                }

                if (objs.Length != numItems)
                    Log.Warning("SpatialManager bucket modified during GetNearby() !!");
                if (objs.Length > 512)
                    Log.Warning("SpatialManager GetNearby returned {0} items. Seems a bit inefficient.", objs.Length);
                return objs;
            }

            // probe if selected buckets are empty to avoid unnecessary allocations
            int totalObjects = 0;
            for (int i = 0; i < numIds; ++i)
                totalObjects += buckets[ids[i]]->Count;
            if (totalObjects == 0) return Empty<T>.Array;

            // create a histogram with all the objectId frequencies
            // while filtering objects based on the requested type of T
            int numAllObjects = AllObjects.Count;
            byte* histogram = stackalloc byte[numAllObjects];
            for (int i = 0; i < numIds; ++i)
            {
                PoolArrayU16* bucket = buckets[ids[i]];
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
                Log.Warning("SpatialManager bucket modified during GetNearby() !!");
            if (unique.Length > 512)
                Log.Warning("SpatialManager GetNearby returned {0} items. Seems a bit inefficient.", unique.Length);
            return unique;
        }

        private void PlaceIntoBucket(GameplayObject obj, int objId)
        {
            // Sorry, this is an almost identicaly copy-paste from the above function "GetNearby"
            // All of this for the sole reason of extreme performance optimization. Forgive us :'(
            int numIds = 0;
            int* ids = stackalloc int[4];

            float posX   = obj.Position.X - UpperLeftBound.X;
            float posY   = obj.Position.Y - UpperLeftBound.Y;
            int   width  = Width;
            float radius = obj.Radius;
            int cellSize = CellSize;

            int leftColOffs  = (int)((posX - radius) / cellSize);
            int rightColOffs = (int)((posX + radius) / cellSize);
            int topRowOffs   = (int)((posY - radius) / cellSize) * width;
            int botRowOffs   = (int)((posY + radius) / cellSize) * width;

            if (leftColOffs == rightColOffs && topRowOffs == botRowOffs)
            {
                int id = topRowOffs + leftColOffs;
                if ((uint)id < Buckets.Count) ids[numIds++] = id;
            }
            else
            {
                int size = Buckets.Count;
                int id = topRowOffs + leftColOffs;
                if ((uint)id < size)
                    ids[numIds++] = id;

                id = topRowOffs + rightColOffs;
                if (ids[0] != id && (uint)id < size)
                    ids[numIds++] = id;

                id = botRowOffs + rightColOffs;
                if (ids[0] != id && (uint)id < size && ids[1] != id)
                    ids[numIds++] = id;

                id = botRowOffs + rightColOffs;
                if (ids[0] != id && (uint)id < size && ids[1] != id && ids[2] != id)
                    ids[numIds++] = id;
            }


            // this can happen if a ship is performing FTL, so it jumps out of the Solar system
            // best course of action is to just ignore it and not insert into buckets
            if (numIds == 0)
            {
                if (System != null && System.Position.InRadius(obj.Position, 100000f))
                    Log.Error("SpatialManager logic error: object {0} is outside of system grid {1}", obj, System);
                return;
            }

            PoolArrayU16** buckets = Buckets.Items;
            for (int i = 0; i < numIds; ++i)
            {
                int quadrantId = ids[i];
                PoolArrayU16** bucketRef = &buckets[quadrantId];
                MemoryPool.ArrayAdd(bucketRef, (ushort)objId);
            }
        }

        private void MoveAndCollide(Projectile projectile)
        {
            GameplayObject[] nearby = GetNearby<GameplayObject>(projectile.Position, 100f);
            foreach (GameplayObject otherObj in nearby)
            {
                if (CollideWith(projectile, otherObj, out GameplayObject collidedWith) && 
                    projectile.Touch(collidedWith) || collidedWith.Touch(projectile))
                {
                    return; // projectile collided (and died), no need to continue collisions
                }
            }
        }

        // @todo This method is a complete mess, like all other decompiled parts. It needs a lot of cleanup.
        private void CollideBeam(Beam beam)
        {
            beam.CollidedThisFrame = false;
            float distanceToTarget = beam.Destination.Distance(beam.Source);
            if (distanceToTarget > beam.range + 10f)
                return;
            beam.ActualHitDestination = beam.Destination;

            Ship ship1 = null;
            Vector2 actualHitDestination = Vector2.Zero;
            GameplayObject gameplayObject1 = null;
            GameplayObject beamTarget = beam.GetTarget();
            if (beamTarget != null)
            {
                if (beamTarget is Ship ship2)
                {
                    gameplayObject1 = ship2; // @todo Is this correct? Should fix a null pointer crash...
                    ship2.MoveModulesTimer = 2f;
                    beam.ActualHitDestination = beamTarget.Center;
                    if (beam.damageAmount >= 0f)
                    {
                        //beam.owner.Beams.QueuePendingRemoval(beam);
                        return;
                    }
                    foreach (ModuleSlot current in ship2.ModuleSlotList)
                    {
                        ShipModule module = current.module;
                        module.Health -= beam.damageAmount;

                        if (module.Health < 0f)
                            module.Health = 0f;
                        else if (module.Health >= module.HealthMax)
                            module.Health = module.HealthMax;
                    }
                }
                else if (beamTarget is ShipModule)
                {
                    gameplayObject1 = (beamTarget as ShipModule).GetParent();
                }
                else if (beamTarget is Asteroid)
                    gameplayObject1 = beam.GetTarget();
                else
                    Log.Info("beam null");
            }
            else gameplayObject1 = beam.owner;

            if (gameplayObject1 == null)
            {
                Log.Info("CollideBeam gameplayObject1 null");
                return;
            }

            GameplayObject[] nearby = GetNearby<GameplayObject>(gameplayObject1.Position, gameplayObject1.Radius);
            if (nearby.Length == 0)
                return;
            nearby.Sort(obj => beam.Source.SqDist(obj.Position));

            Vector2 unitV = Vector2.Normalize(beam.Destination - beam.Source);

            var beampath = new Ray(new Vector3(beam.Source, 0), new Vector3(unitV.X, unitV.Y, 0));
            float hit2 = beam.range;

            for (int i = 0; i < nearby.Length; i++)
            {
                if (!(nearby[i] is Ship ship))
                    continue;
                if (ship.loyalty == beam.owner?.loyalty)
                    continue; // dont hit allied. need to expand this to actual allies.

                if (ship == beam.owner && !beam.weapon.HitsFriendlies) //hits friendlies is in the  wrong place.
                    continue;

                ++GlobalStats.BeamTests;

                var shipShields = ship.GetShields();
                if (!beam.IgnoresShields && shipShields.Count > 0)
                {
                    var shieldhit = new BoundingSphere(new Vector3(0f, 0f, 0f), 0f); //create a bounding sphere object for shields.

                    foreach (ShipModule shield in shipShields)
                    {
                        if (!shield.Powered || shield.shield_power <= 0 || !shield.Active)
                            continue;
                        shieldhit.Center.X = shield.Center.X;
                        shieldhit.Center.Y = shield.Center.Y;
                        shieldhit.Radius = shield.Radius + 4;

                        float? hit = beampath.Intersects(shieldhit);
                        if (hit.HasValue && hit < hit2)
                        {
                            hit2 = (float)hit;
                            ship1 = ship;
                            ship1.MoveModulesTimer = 2f;

                            actualHitDestination = (beampath.Position + beampath.Direction * hit.Value).ToVec2();
                        }
                    }
                }
                {
                    float? hit = beampath.Intersects(ship.GetSO().WorldBoundingSphere);
                    if (hit.HasValue && hit < hit2)
                    {
                        hit2 = (float)hit;
                        ship1 = ship;
                        ship1.MoveModulesTimer = 2f;
                        actualHitDestination = (beampath.Position + beampath.Direction * hit.Value).ToVec2();
                    }
                }
            }

            hit2 = beam.range;
            if (ship1 != null)
            {
                ShipModule damaged = null;
                var shieldhit = new BoundingSphere(new Vector3(0f, 0f, 0f), 0f); //create a bounding sphere object for shields.
                foreach (ModuleSlot shield in ship1.ModuleSlotList)
                {
                    if (!shield.module.Active)
                        continue;
                    ShipModule test = shield.module;
                    shieldhit.Radius = 8;
                    if (shield.module.shield_power > 0)
                    {
                        shieldhit.Radius += shield.module.Radius;
                    }


                    shieldhit.Center.X = test.Center.X;
                    shieldhit.Center.Y = test.Center.Y;

                    float? hitM = beampath.Intersects(shieldhit);
                    if (hitM < hit2)
                    {
                        hit2 = (float)hitM;
                        damaged = shield.module;
                        actualHitDestination = (beampath.Position + beampath.Direction * hitM.Value).ToVec2();
                    }
                }
                if (damaged != null && beam.Touch(damaged))
                {
                    beam.CollidedThisFrame = damaged.CollidedThisFrame = true;
                    beam.ActualHitDestination = actualHitDestination;
                    return;
                }
            }

            beam.ActualHitDestination = beam.Destination;
        }

        // @return TRUE if a collision happens, false if nothing happened
        private bool CollideWith(Projectile thisProj, GameplayObject otherObj, out GameplayObject collidedWith)
        {
            collidedWith = null;
            if (thisProj == otherObj || !otherObj.Active || otherObj.CollidedThisFrame)
                return false;

            if (otherObj is Ship otherShip)
            {
                if (thisProj.loyalty == otherShip.loyalty)
                    return false;

                // if projectile travels more than 16 units (module width) per frame, we need to do ray collision
                float distPerFrame = thisProj.speed / 60.0f;
                if (thisProj.Center.OutsideRadius(otherShip.Center, otherShip.Radius + thisProj.Radius + distPerFrame))
                    return false;

                otherShip.MoveModulesTimer = 2f;

                if (distPerFrame > 16.0f) // ray collision
                {
                    Vector2 dir = thisProj.Velocity.Normalized();
                    collidedWith = otherShip.RayHitTestExternalModules(
                        thisProj.Center, dir, distPerFrame, thisProj.Radius, thisProj.IgnoresShields);
                }
                else
                {
                    collidedWith = otherShip.HitTestExternalModules(thisProj.Center, thisProj.Radius, thisProj.IgnoresShields);
                }
                return collidedWith != null;
            }
            if (otherObj is Projectile otherProj)
            {
                if (thisProj.Center.OutsideRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherProj.weapon.ProjectileRadius))
                    return false;
                collidedWith = otherObj;
                return true; // thisProj died
            }

            // finally, generic collision with any kind of 'simple' object
            if (thisProj.Center.OutsideRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherObj.Radius))
                return false;
            collidedWith = otherObj;
            return true; // thisProj died
        }

        //Added by McShooterz: New way to distribute exploding projectile damage
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;

            Ship[] ships = GetNearby<Ship>(source.Position, source.Radius).FilterBy(ship => ship.Active && !ship.dying);
            ships.Sort(ship => ship.Center.SqDist(source.Center));

            foreach (Ship ship in ships)
            {
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;
                
                // Check if valid target
                if (source.Owner?.loyalty == ship.loyalty)
                    continue;

                if (ship.loyalty?.data.ExplosiveRadiusReduction > 0f)
                    modifiedRadius *= 1f - ship.loyalty.data.ExplosiveRadiusReduction;

                Array<ShipModule> modules = ship.HitTestModules(source.Center, modifiedRadius, source.IgnoresShields);

                float damageTracker = damageAmount;
                foreach (ShipModule module in modules)
                {
                    module.Damage(source, damageTracker, ref damageTracker);
                    if (damageTracker <= 0f)
                        return;
                }
                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
            }
        }

        private static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float minFalloff = 0.4f)
        {
            return Math.Min(1.0f, explosionCenter.Distance(affectedPoint) / damageRadius + minFalloff);
        }

        // Refactored by RedFox
        public void ExplodeAtModule(GameplayObject source, ShipModule hitModule, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;
            Ship parent = hitModule.GetParent();
            if (parent.dying || !parent.Active)
                return;

            // affected modules sorted by distance
            Vector2 explosionCenter = hitModule.Center;
            var hitModules = parent.HitTestModules(explosionCenter, damageRadius, ignoreShields: false/*internal explosion*/);

            // start dishing out damage from inside out to first 8 modules
            // since damage is internal, we can't explode with radial falloff
            float damageTracker = damageAmount;
            while (damageTracker > 0.0f && hitModules.Count > 0)
            {
                float damage = damageTracker / 8;

                for (int i = 0; i < hitModules.Count; ++i)
                {
                    ShipModule module = hitModules[i];
                    float damageFalloff = DamageFalloff(explosionCenter, module.Center, damageRadius);
                    float health = module.Health;
                    module.Damage(source, damage * damageFalloff);
                    damageTracker -= health - module.Health;

                    if (module.Health <= 0.0f)
                        hitModules.RemoveAt(i--); // don't use SwapLast, 
                }
            }
        }

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
                    ShipModule nearest = otherShip.FindClosestExternalModule(explosionCenter);
                    if (nearest == null)
                        continue;

                    float reducedDamageRadius = damageRadius - explosionCenter.Distance(nearest.Center);
                    if (reducedDamageRadius <= 0.0f)
                        continue;

                    float damageFalloff = DamageFalloff(explosionCenter, nearest.Center, damageRadius);
                    ExplodeAtModule(thisShip, nearest, damageAmount * damageFalloff, reducedDamageRadius);

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
                    float damageFalloff = DamageFalloff(explosionCenter, otherObj.Center, damageRadius, 0.25f);
                    otherObj.Damage(thisShip, damageAmount * damageFalloff);
                }
            }
        }
    }
}
