// Type: Ship_Game.Gameplay.SpatialManager
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager : IDisposable
    {
        private readonly Array<Ship>       Ships       = new Array<Ship>();
        private readonly Array<Projectile> Projectiles = new Array<Projectile>();
        private readonly Array<Asteroid>   Asteroids   = new Array<Asteroid>();
        private readonly Array<Beam>       Beams       = new Array<Beam>();

        private float BucketUpdateTimer;
        private readonly Array<CollisionResult> CollisionResults = new Array<CollisionResult>();
        private int Width;
        private int Height;
        private int Size;
        private Vector2 UpperLeftBound;
        private Array<GameplayObject>[] Buckets;
        private int CellSize;
        private bool FineDetail;        

        public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 center)
        {
            UpperLeftBound.X = center.X - (sceneWidth  / 2f);
            UpperLeftBound.Y = center.Y - (sceneHeight / 2f);
            Width            = sceneWidth  / cellSize;
            Height           = sceneHeight / cellSize;
            Size             = Width * Height;
            Buckets          = new Array<GameplayObject>[Size];
            CellSize         = cellSize;
        }

        private static bool CheckNotNull(GameplayObject obj)
        {
            if (obj != null)
                return true;
            Log.Error("SpatialManager null object");
            return false;
        }

        public void Add(Ship ship)
        {
            if (CheckNotNull(ship) && !Contains(ship))
            {
                Ships.Add(ship);
                PlaceIntoBucket(ship);
            }
        }
        public void Add(Projectile projectile)
        {
            if (CheckNotNull(projectile) && !Contains(projectile))
            {
                Projectiles.Add(projectile);
                PlaceIntoBucket(projectile);
            }
        }
        public void Add(Asteroid asteroid)
        {
            if (CheckNotNull(asteroid) && !Contains(asteroid))
            {
                Asteroids.Add(asteroid);
                PlaceIntoBucket(asteroid);
            }
        }
        public void Add(Beam beam)
        {
            if (CheckNotNull(beam) && !Contains(beam))
            {
                Beams.Add(beam);
                PlaceIntoBucket(beam);
            }
        }

        public void Remove(Ship ship)
        {
            if (CheckNotNull(ship)) Ships.Remove(ship);
        }
        public void Remove(Projectile projectile)
        {
            if (CheckNotNull(projectile)) Projectiles.Remove(projectile);
        }
        public void Remove(Asteroid asteroid)
        {
            if (CheckNotNull(asteroid)) Asteroids.Remove(asteroid);
        }
        public void Remove(Beam beam)
        {
            if (CheckNotNull(beam)) Beams.Remove(beam);
        }

        public bool Contains(Ship ship)             => Ships.ContainsRef(ship);
        public bool Contains(Projectile projectile) => Projectiles.ContainsRef(projectile);
        public bool Contains(Asteroid asteroid)     => Asteroids.ContainsRef(asteroid);
        public bool Contains(Beam beam)             => Beams.ContainsRef(beam);

        public IReadOnlyList<Ship> ShipsList => Ships;

        public void GetDeepSpaceShips(Array<Ship> copyTo)
        {
            copyTo.Clear();
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (ship.Active && ship.isInDeepSpace && ship.System == null)
                    copyTo.Add(ship);
            }
        }

        public void Update(float elapsedTime, SolarSystem system)
        {
            BucketUpdateTimer += elapsedTime;
            if (BucketUpdateTimer >= 0.5f) // update all buckets
            {
                BucketUpdateTimer = 0.0f;
                ClearBuckets();

                if (system != null)
                {
                    if (system.CombatInSystem && system.ShipList.Count > 10)
                    {
                        if (!FineDetail || Size < 20 && Projectiles.Count > 0)
                        {
                            Setup(200000, 200000, 6000, system.Position);
                            FineDetail = true;
                        }
                    }
                    else if (FineDetail || Size > 20 || Projectiles.Count == 0)
                        Setup(200000, 200000, 50000, system.Position);
                }

                for (int i = Ships.Count - 1; i >= 0; --i)
                {
                    Ship ship = Ships[i];
                    if (!ship.Active || ship.System != null && system == null)
                        Ships.RemoveAt(i);
                    else PlaceIntoBucket(ship);
                }
                for (int i = Projectiles.Count - 1; i >= 0; --i)
                {
                    Projectile projectile = Projectiles[i];
                    if (!projectile.Active || projectile.System != null && system == null)
                        Projectiles.Remove(projectile); 
                    else PlaceIntoBucket(projectile);
                }
            }

            // run collision checks, the loops look weird because they are optimized for .NET JIT
            int numProjectiles = Projectiles.Count;
            if (numProjectiles > 0)
            {
                Projectile[] items = Projectiles.GetInternalArrayItems();
                for (int i = 0; i < numProjectiles && i < items.Length; ++i)
                {
                    Projectile projectile = items[i];
                    if (projectile.Active)
                        MoveAndCollide(projectile);
                }
            }
            int numBeams = Beams.Count;
            if (numBeams > 0)
            {
                Beam[] items = Beams.GetInternalArrayItems();
                for (int i = 0; i < numBeams && i < items.Length; ++i)
                {
                    Beam beam = items[i];
                    if (beam.Active)
                        CollideBeam(beam);
                }
            }
        }

        public void UpdateBucketsOnly(float elapsedTime)
        {
            BucketUpdateTimer += elapsedTime;
            if (BucketUpdateTimer < 0.5f)
                return;
            BucketUpdateTimer = 0.0f;
            ClearBuckets();

            for (int i = Ships.Count - 1; i >= 0; --i)
            {
                Ship ship = Ships[i];
                if (!ship.Active) Ships.RemoveAt(i);
                else              PlaceIntoBucket(ship);
            }
            for (int i = Projectiles.Count - 1; i >= 0; --i)
            {
                Projectile projectile = Projectiles[i];
                if (!projectile.Active) Projectiles.Remove(projectile);
                else                    PlaceIntoBucket(projectile);
            }
        }

        // clockwise 4 quadrants, however, values can be -1 if the quadrants were too far or nonexistant
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Length = {Length} [0]={Quad0} [1]={Quad1} [2]={Quad2} [3]={Quad3}")]
        private struct QuadrantIds
        {
            public int Length;
            private int Quad0;
            private int Quad1;
            private int Quad2;
            private int Quad3;

            public unsafe int this[int index]
            {
                get
                {
                    fixed (int* quads = &Quad0)
                        return quads[index];
                }
            }

            public unsafe void Add(int quadrantId)
            {
                fixed (int* quads = &Quad0)
                {
                    for (int i = 0; i < Length; ++i)
                        if (quads[i] == quadrantId)
                            return;
                    quads[Length++] = quadrantId;
                }
            }
        }

        private QuadrantIds GetIdForPos(Vector2 objPos, float radius = 100f)
        {
            Vector2 gridPos = objPos - UpperLeftBound;

            float left  = gridPos.X - radius;
            float right = gridPos.X + radius;
            float top   = gridPos.Y - radius;
            float bot   = gridPos.Y + radius;

            var quadrants = new QuadrantIds();
            AddBucket(ref quadrants, left,  top);
            AddBucket(ref quadrants, right, top);
            AddBucket(ref quadrants, right, bot);
            AddBucket(ref quadrants, left,  bot);
            return quadrants;
        }

        private void AddBucket(ref QuadrantIds quadrants, float posX, float posY)
        {
            int x = (int)posX / CellSize;
            int y = (int)posY / CellSize;
            int quadrantId = x + y * Width;
            if (0 <= quadrantId && quadrantId < Size)
                quadrants.Add(quadrantId);
        }

        public GameplayObject[] GetNearby(GameplayObject obj) => GetNearby(obj.Center, obj.Radius);

        public GameplayObject[] GetNearby(Vector2 position, float radius = 100f)
        {
            QuadrantIds ids = GetIdForPos(position, radius);

            int totalObjects = 0;
            for (int i = 0; i < ids.Length; ++i)
            {
                Array<GameplayObject> bucket = Buckets[ids[i]];
                if (bucket != null) totalObjects += bucket.Count;
            }

            var objects = new GameplayObject[totalObjects];
            int numObjects = 0;
            for (int i = 0; i < objects.Length; ++i)
            {
                Array<GameplayObject> bucket = Buckets[ids[i]];
                if (bucket == null) continue;
                for (int j = 0; j < bucket.Count; ++j)
                {
                    GameplayObject obj = bucket[j];
                    if (!objects.ContainsRef(numObjects, obj))
                        objects[numObjects++] = obj;
                }
            }

            if (objects.Length != numObjects)
                Array.Resize(ref objects, numObjects);
            return objects;
        }

        private void PlaceIntoBucket(GameplayObject obj)
        {
            if (CellSize == 0)
                return; // not initialized yet (probably during loading)

            QuadrantIds ids = GetIdForPos(obj.Center, obj.Radius);
            if (ids.Length == 0)
                Log.Error("GameplayObject fell outside of SpatialManager grid: {0}", obj.Position);

            for (int i = 0; i < ids.Length; ++i)
            {
                int bucketIdx = ids[i];
                if (Buckets[bucketIdx] == null)
                    Buckets[bucketIdx] = new Array<GameplayObject>();
                Buckets[bucketIdx].Add(obj);
            }
        }


        public void Destroy()
        {
            ClearBuckets();
            Ships.Clear();
            Projectiles.Clear();
            Asteroids.Clear();
            Beams.Clear();
            Buckets = null;
        }

        internal void ClearBuckets()
        {
            for (int i = 0; i < Size; ++i)
                Buckets[i]?.Clear();
        }

        private void MoveAndCollide(Projectile projectile)
        {
            CollisionResults.Clear();
            foreach (GameplayObject otherObj in GetNearby(projectile))
                if (CollideWith(projectile, otherObj))
                    break; // projectile died, no need to continue collisions

            if (CollisionResults.Count > 0)
            {
                CollisionResults.Sort(CollisionResult.Compare);
                foreach (CollisionResult cr in CollisionResults)
                {
                    if (projectile.Touch(cr.GameplayObject) || cr.GameplayObject.Touch(projectile))
                        return;
                }
            }
        }

        // @todo This method is a complete mess, like all other decompiled parts. It needs a lot of cleanup.
        private void CollideBeam(Beam beam)
        {
            CollisionResults.Clear();
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
            else if (beam.Owner != null)
                gameplayObject1 = beam.owner;

            if (gameplayObject1 == null)
            {
                Log.Info("CollideBeam gameplayObject1 null");
            }

            GameplayObject[] nearby = GetNearby(gameplayObject1).OrderBy(distance => beam.Source.SqDist(distance.Center)).ToArray();
            if (nearby.Length == 0)
                return;
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
        private bool CollideWith(Projectile thisProj, GameplayObject otherObj)
        {
            if (thisProj == otherObj || !otherObj.Active || otherObj.CollidedThisFrame)
                return false;

            if (otherObj is Projectile otherProj)
            {
                if (thisProj.Center.InRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherProj.weapon.ProjectileRadius))
                {
                    CollisionResults.Add(new CollisionResult
                    {
                        Distance = 0.0f,
                        Normal = new Vector2(),
                        GameplayObject = otherObj
                    });
                    return true; // thisProj died
                }
            }
            else if (otherObj is Asteroid)
            {
                if (thisProj.Center.InRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherObj.Radius))
                {
                    CollisionResults.Add(new CollisionResult
                    {
                        Distance = 0.0f,
                        Normal = new Vector2(),
                        GameplayObject = otherObj
                    });
                    return true; // thisProj died
                }
            }
            else 
            if (otherObj is Ship otherShip && 
                thisProj.loyalty != otherShip.loyalty && 
                thisProj.Center.InRadius(otherObj.Center, otherObj.Radius + thisProj.Radius + thisProj.speed / 60.0f))
            {
                otherShip.MoveModulesTimer = 2f;
                if (thisProj.speed / 60.0 > 16.0) // raymarch type of collision?
                {
                    Vector2 direction = thisProj.Velocity.Normalized();
                    for (int i = 0; i < otherShip.ExternalSlots.Count; ++i)
                    {
                        ++GlobalStats.DistanceCheckTotal;
                        ModuleSlot slot = otherShip.ExternalSlots[i];
                        if (slot.module != null && (slot.module.shield_power <= 0.0 || !thisProj.IgnoresShields) && slot.module.Active)
                        {
                            float radius = 8.0f + (slot.module.shield_power > 0.0f ? slot.module.shield_radius : 0.0f);

                            int num1 = 8;
                            while (num1 < thisProj.speed * 2.0 / 60.0) // raymarch again?
                            {
                                ++GlobalStats.Comparisons;
                                if (slot.module.Center.InRadius(thisProj.Center + direction * num1, radius))
                                {
                                    thisProj.Center   = thisProj.Center + direction * num1;
                                    thisProj.Position = thisProj.Center;
                                    CollisionResults.Add(new CollisionResult
                                    {
                                        Distance       = thisProj.Radius + slot.module.Radius,
                                        Normal         = new Vector2(),
                                        GameplayObject = slot.module
                                    });
                                    return true; // thisProj died
                                }
                                num1 += 8;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < otherShip.ExternalSlots.Count; ++i)
                    {
                        ++GlobalStats.Comparisons;
                        ++GlobalStats.DistanceCheckTotal;
                        ModuleSlot slot = otherShip.ExternalSlots[i];
                        if (slot.module != null && slot.module.Active && 
                            (slot.module.shield_power <= 0.0 || !thisProj.IgnoresShields) &&
                            slot.module.Center.InRadius(thisProj.Center, 10.0f + (slot.module.shield_power > 0.0f ? slot.module.shield_radius : 0.0f)))
                        {
                            CollisionResults.Add(new CollisionResult
                            {
                                Distance       = thisProj.Radius + slot.module.Radius,
                                Normal         = new Vector2(),
                                GameplayObject = slot.module
                            });
                            return true; // thisProj died
                        }
                    }
                }
            }
            return false; // no collisions
        }

        //Added by McShooterz: New way to distribute exploding projectile damage
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;

            var ships = GetNearby(source)
                .Where(go => go is Ship && go.Active && !((Ship)go).dying)
                .OrderBy(go => Vector2.Distance(source.Center, go.Center))
                .Cast<Ship>();

            foreach (Ship ship in ships)
            {
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;
                
                //Check if valid target
                //added by gremlin check that projectile owner is not null
                if (source.Owner != null && source.Owner.loyalty == ship.loyalty)
                    continue;

                if (ship.loyalty != null && ship.loyalty.data.ExplosiveRadiusReduction > 0)
                    modifiedRadius *= (1 - ship.loyalty.data.ExplosiveRadiusReduction);

                // @todo This expression is very messy, simplify and optimize
                float damageTracker = 0;
                var modules = ship.ModuleSlotList
                    .Where(slot => slot.module.Health > 0.0 && (slot.module.shield_power > 0.0 && !source.IgnoresShields)
                        ? Vector2.Distance(source.Center, slot.module.Center) <= modifiedRadius + slot.module.shield_radius
                        : Vector2.Distance(source.Center, slot.module.Center) <= modifiedRadius + 10f)
                    .OrderBy(slot => (slot.module.shield_power > 0.0 && !source.IgnoresShields)
                                ? Vector2.Distance(source.Center, slot.module.Center) - slot.module.shield_radius
                                : Vector2.Distance(source.Center, slot.module.Center));
                foreach (ModuleSlot moduleSlot in modules)
                {
                    moduleSlot.module.Damage(source, damageAmount, ref damageTracker);
                    if (damageTracker > 0)
                        damageAmount = damageTracker;
                    else return;
                }
                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
            }
        }

        //Modified by McShooterz: not used before, changed to be used for exploding modules
        public void ExplodeAtModule(GameplayObject source, ShipModule hitModule, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || hitModule.GetParent().dying || !hitModule.GetParent().Active)
                return;
            var removalCollection = new BatchRemovalCollection<ExplosionRay>(false);
            float num2 = (360f / 15);
            for (int i = 0; i < 15; ++i)
                removalCollection.Add(new ExplosionRay
                {
                    Direction = MathExt.PointOnCircle(num2 * i, 1f),
                    Damage = damageAmount / 15
                });
            var list = new Array<ShipModule>();
            list.Add(hitModule);
            foreach (ModuleSlot slot in hitModule.GetParent().ModuleSlotList.
                Where(moduleSlot => Vector2.Distance(hitModule.Center, moduleSlot.module.Center) <= damageRadius)
                .OrderBy(moduleSlot => Vector2.Distance(hitModule.Center, moduleSlot.module.Center)))
            {
                list.Add(slot.module);
            }
            int num3 = 0;
            while (num3 <damageRadius)
            {
                foreach (ExplosionRay explosionRay in removalCollection)
                {
                    if (!(explosionRay.Damage > 0.0))
                        continue;
                    foreach (ShipModule shipModule in list)
                    {
                        if (!shipModule.Active || !(shipModule.Health > 0.0))
                            continue;
                        Vector2 vector21 = hitModule.Center + explosionRay.Direction * num3;
                        Vector2 vector22 = shipModule.Center - vector21;
                        if (!(Vector2.Distance(vector21, shipModule.Center) <= 8.0) || !(explosionRay.Damage > 0.0))
                            continue;
                        float health = shipModule.Health;
                        shipModule.Damage(source, explosionRay.Damage);
                        explosionRay.Damage -= health;
                    }
                }
                num3 += 8;
            }
        }

        public void ShipExplode(Ship thisShip, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;
            float num1 = damageRadius * damageRadius;
            Vector2 explosionCenter = thisShip.Center;
            var removalCollection = new BatchRemovalCollection<ExplosionRay>(false);
            const int angle = 360 / 15;
            for (int i = 0; i < 15; ++i)
            {
                removalCollection.Add(new ExplosionRay
                {
                    Direction = MathExt.PointOnCircle(angle * i, 1f),
                    Damage = damageAmount / 15
                });
            }

            foreach (GameplayObject otherObj in GetNearby(thisShip))
            {
                if (otherObj == null || otherObj is Projectile) continue;
                if (!otherObj.Active || otherObj == thisShip)   continue;
                if (!otherObj.Center.InRadius(thisShip.Center, damageRadius + otherObj.Radius))
                    continue;

                Vector2 vector21 = Vector2.Zero;
                if (otherObj is Ship otherShip && thisShip != otherObj)
                {
                    var affectedModules = new Array<ModuleSlot>();
                    foreach (ModuleSlot slot in otherShip.ExternalSlots)
                    {
                        if (slot.module.Active && slot.module.Center.InRadius(explosionCenter, damageRadius + slot.module.Radius))
                            affectedModules.Add(slot);
                    }
                    if (affectedModules.Count == 0)
                        break;

                    affectedModules.Sort(slot => explosionCenter.SqDist(slot.module.Center));
                    int rayMarch = 0;
                    while (rayMarch < damageRadius)
                    {
                        foreach (ExplosionRay explosionRay in removalCollection)
                        {
                            if (explosionRay.Damage <= 0.0f)
                                continue;
                            foreach (ModuleSlot slot in affectedModules)
                            {
                                ShipModule module = slot.module;
                                if (!module.Active || module.Health <= 0.0f)
                                    continue;
                                if (!module.Center.InRadius(explosionCenter + explosionRay.Direction * rayMarch, 8.0f))
                                    continue;
                                vector21 += 3f * explosionRay.Damage * explosionRay.Direction;
                                if (!otherShip.dying)
                                {
                                    if (otherShip.yRotation > 0.0f)
                                    {
                                        otherShip.yRotation += explosionRay.Damage / (float)Math.Pow(otherShip.Mass, 1.3);
                                        if (otherShip.yRotation > otherShip.maxBank)
                                            otherShip.yRotation = otherShip.maxBank;
                                    }
                                    else
                                    {
                                        otherShip.yRotation -= explosionRay.Damage / (float)Math.Pow(otherShip.Mass, 1.3);
                                        if (otherShip.yRotation < -otherShip.maxBank)
                                            otherShip.yRotation = -otherShip.maxBank;
                                    }
                                }

                                if (explosionRay.Damage > 0.0f)
                                {
                                    float health = module.Health;
                                    module.Damage(thisShip, explosionRay.Damage);
                                    explosionRay.Damage -= health;
                                }
                            }
                        }
                        rayMarch += 8;
                    }
                    if (vector21.Length() > 200.0)
                        vector21 = Vector2.Normalize(vector21) * 200f;
                    if (!float.IsNaN(vector21.X))
                        otherObj.Velocity += vector21;
                }
                else
                {
                    if (otherObj is Ship && thisShip == otherObj)
                        break;
                    float sqDist = (otherObj.Center - position).LengthSquared();
                    if (sqDist > 0.0f && sqDist <= num1)
                    {
                        float num3 = (float)Math.Sqrt(sqDist);
                        float damageAmount1 = damageAmount * (damageRadius - num3) / damageRadius;
                        if (damageAmount1 > 0.0)
                            otherObj.Damage(thisShip, damageAmount1);
                    }
                }
            }
        }

        private struct CollisionResult
        {
            public float Distance;
            public Vector2 Normal;
            public GameplayObject GameplayObject;

            public static int Compare(CollisionResult a, CollisionResult b)
            {
                return a.Distance.CompareTo(b.Distance);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SpatialManager() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            Ships.Clear();
            Projectiles.Clear();
            Asteroids.Clear();
            Beams.Clear();
        }
    }
}
