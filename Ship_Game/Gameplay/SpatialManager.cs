// Type: Ship_Game.Gameplay.SpatialManager
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager: IDisposable
    {
        public Array<GameplayObject> CollidableObjects = new Array<GameplayObject>();
        public Array<Projectile> CollidableProjectiles = new Array<Projectile>();
        public Array<Asteroid> Asteroids = new Array<Asteroid>();
        public Array<Beam> BeamList = new Array<Beam>();
        private float BucketUpdateTimer = 0.5f;
        private readonly Array<CollisionResult> CollisionResults = new Array<CollisionResult>();
        private int Cols;
        private int Rows;
        private Vector2 UpperLeftBound;
        private ConcurrentDictionary<int, BatchRemovalCollection<GameplayObject>> Buckets;
        public int SceneWidth;
        public int SceneHeight;
        public int CellSize;
        public bool FineDetail;        

        public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 Pos)
        {
            UpperLeftBound.X = Pos.X - (sceneWidth / 2f);
            UpperLeftBound.Y = Pos.Y - (sceneHeight / 2f);
            Cols = sceneWidth / cellSize;
            Rows = sceneHeight / cellSize;
            //Buckets = new Map<int, Array<GameplayObject>>(Cols * Rows);
            Buckets = new ConcurrentDictionary<int, BatchRemovalCollection<GameplayObject>>();
            for (int key = 0; key < Cols * Rows; ++key)
                Buckets.TryAdd(key, new BatchRemovalCollection<GameplayObject>());
            SceneWidth  = sceneWidth;
            SceneHeight = sceneHeight;
            CellSize    = cellSize;
        }

        public void Update(float elapsedTime, SolarSystem system)
        {
            BucketUpdateTimer -= elapsedTime;
            if (BucketUpdateTimer <= 0.0)
            {
                ClearBuckets();
                if (system != null)
                {
                    if (system.CombatInSystem && system.ShipList.Count > 10)
                    {
                        if (!FineDetail || Buckets.Count < 20 && CollidableProjectiles.Count > 0)
                        {
                            Setup(200000, 200000, 6000, system.Position);
                            FineDetail = true;
                        }
                    }
                    else if (FineDetail || Buckets.Count > 20 || CollidableProjectiles.Count == 0)
                        Setup(200000, 200000, 50000, system.Position);
                }
                for (int index = CollidableObjects.Count - 1; index >= 0; --index)
                {
                    GameplayObject gameplayObject = CollidableObjects[index];
                    if (gameplayObject != null)
                    {
                        if (gameplayObject.System != null && system == null)
                            CollidableObjects.Remove(gameplayObject);

                        if (gameplayObject.Active)
                            RegisterObject(gameplayObject);
                        else
                            CollidableObjects.Remove(gameplayObject);

                    }
                }
                for (int index = CollidableProjectiles.Count - 1; index >= 0; --index)
                {
                    Projectile gameplayObject = CollidableProjectiles[index];
                    if (gameplayObject.System!= null && system == null)
                        CollidableObjects.Remove(gameplayObject); 
                    else if (gameplayObject.Active)
                        RegisterObject(gameplayObject);
                }
                BucketUpdateTimer = 0.5f;
            }
            if (CollidableProjectiles.Count > 0)
            {
                for (int index = 0; index < CollidableObjects.Count; ++index)
                {
                    GameplayObject gameplayObject = CollidableObjects[index];
                    Ship oShip = gameplayObject as Ship;                    
                    if (gameplayObject != null && !(gameplayObject.System!= null & system == null) 
                        && (oShip == null || system != null || oShip.GetAI().BadGuysNear))
                        MoveAndCollide(gameplayObject);
                }
            }
            for (int index = 0; index < BeamList.Count; ++index)
            {
                Beam beam = BeamList[index];
                if (beam != null)
                    CollideBeam(beam);
            }
        }

        public void UpdateBucketsOnly(float elapsedTime)
        {
            BucketUpdateTimer -= elapsedTime;
            if (BucketUpdateTimer <= 0f)
            {
                ClearBuckets();
                for (int i = CollidableObjects.Count - 1; i >= 0; --i)
                {
                    GameplayObject go = CollidableObjects[i];
                    if (go.Active)
                        RegisterObject(go);
                    else
                        CollidableObjects.Remove(go);
                }
                BucketUpdateTimer = 0.5f;
            }
        }

        internal Array<GameplayObject> GetNearby(GameplayObject obj)
        {
            var nearby = new Array<GameplayObject>();
            foreach (int key in GetIdForObj(obj))
            {
                if (!Buckets.TryGetValue(key, out var list))
                    return Buckets[1];
                nearby.AddRange(list);
            }
            return nearby;
        }

        internal Array<GameplayObject> GetNearby(Vector2 position)
        {
            var list = new Array<GameplayObject>();
            foreach (int key in GetIdForPos(position))
            {
                if (Buckets.TryGetValue(key, out BatchRemovalCollection<GameplayObject> test))
                    list.AddRange(test);
            }
            return list;
        }

        internal Array<T> GetNearby<T>(Vector2 position) where T : GameplayObject
        {
            var list = new Array<T>();
            foreach (int key in GetIdForPos(position))
            {
                if (!Buckets.TryGetValue(key, out BatchRemovalCollection<GameplayObject> test))
                    continue;
                foreach (GameplayObject go in test)
                {
                    if (go is T item)
                        list.Add(item);
                }
            }
            return list;
        }

        internal void RegisterObject(GameplayObject obj)
        {
            foreach (int key in GetIdForObj(obj))
            {
                if (Buckets.TryGetValue(key, out BatchRemovalCollection<GameplayObject> test))
                    test.Add(obj);
                else
                    Buckets[1].Add(obj);
            }
        }

        public Array<int> GetIdForObj(GameplayObject obj)
        {
            Array<int> buckettoaddto = new Array<int>();
            Vector2 vector2_1 = obj.Center - UpperLeftBound;
            Vector2 vector = new Vector2(vector2_1.X - obj.Radius, vector2_1.Y - obj.Radius);
            Vector2 vector2_2 = new Vector2(vector2_1.X + obj.Radius, vector2_1.Y + obj.Radius);
            float width = (float)SceneWidth / CellSize;
            AddBucket(vector, width, buckettoaddto);
            AddBucket(new Vector2(vector2_2.X, vector.Y), width, buckettoaddto);
            AddBucket(new Vector2(vector2_2.X, vector2_2.Y), width, buckettoaddto);
            AddBucket(new Vector2(vector.X, vector2_2.Y), width, buckettoaddto);
            return buckettoaddto;
        }

        public Array<int> GetIdForPos(Vector2 Position)
        {
            Array<int> buckettoaddto = new Array<int>();
            Vector2 vector2_1 = Position - UpperLeftBound;
            Vector2 vector = new Vector2(vector2_1.X - 100f, vector2_1.Y - 100f);
            Vector2 vector2_2 = new Vector2(vector2_1.X + 100f, vector2_1.Y + 100f);
            float width = (float)SceneWidth / CellSize;
            AddBucket(vector, width, buckettoaddto);
            AddBucket(new Vector2(vector2_2.X, vector.Y), width, buckettoaddto);
            AddBucket(new Vector2(vector2_2.X, vector2_2.Y), width, buckettoaddto);
            AddBucket(new Vector2(vector.X, vector2_2.Y), width, buckettoaddto);
            return buckettoaddto;
        }


        private void AddBucket(Vector2 vector, float width, Array<int> buckettoaddto)
        {
            int num = (int)(Math.Floor(vector.X / CellSize) + Math.Floor(vector.Y / CellSize) * width);
            if (buckettoaddto.Contains(num))
                return;
            buckettoaddto.Add(num);
        }

        public void Destroy()
        {
            Buckets = null;// (Dictionary<int, Array<GameplayObject>>)null;
        }

        internal void ClearBuckets()
        {
            for (int index = 0; index < Cols * Rows; ++index)
            {
                BatchRemovalCollection<GameplayObject> test;
                if(Buckets.TryGetValue(index, out test))
                {
                    test.Clear();
                }
            }
        }

        private void MoveAndCollide(GameplayObject thisObj)
        {
            Collide(thisObj);
            if (CollisionResults.Count > 0)
            {
                CollisionResults.Sort(CollisionResult.Compare);
                foreach (CollisionResult cr in CollisionResults)
                {
                    if (thisObj.Touch(cr.GameplayObject) || cr.GameplayObject.Touch(thisObj))
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

            Ray beampath = new Ray(new Vector3(beam.Source, 0), new Vector3(unitV.X, unitV.Y, 0));
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

        // This function needs a lot more love
        public void Collide(GameplayObject thisObj)
        {
            CollisionResults.Clear();
            if (!thisObj.Active)
                return;

            foreach (GameplayObject otherObj in GetNearby(thisObj))
            {
                if (thisObj == otherObj || !otherObj.Active || otherObj.CollidedThisFrame)
                    continue;

                if (thisObj is Ship thisShip)
                {
                    if (otherObj is Projectile otherProj && thisShip.loyalty != otherProj.loyalty)
                    {
                        ++GlobalStats.Comparisons;
                        if (thisObj.Center.InRadius(otherObj.Center, otherProj.weapon.ProjectileRadius + thisShip.GetSO().WorldBoundingSphere.Radius + 100.0f))
                        {
                            thisShip.MoveModulesTimer = 2f;
                            float projVel = otherProj.Velocity.Length();
                            if (projVel / 60f > 10f) // if fast enough??
                            {
                                bool collision = false;
                                Vector2 direction = otherObj.Velocity.Normalized();

                                var shipShields = thisShip.GetShields();
                                for (int i = 0; i < shipShields.Count; ++i)
                                {
                                    ++GlobalStats.DistanceCheckTotal;
                                    ShipModule shieldModule = shipShields[i];
                                    if (shieldModule.shield_power <= 0.0f || !otherProj.IgnoresShields)
                                    {
                                        if (shieldModule.shield_power > 0.0f && shieldModule.Active)
                                        {
                                            float collisionRadius = 12.0f + shieldModule.shield_radius;
                                            int num2 = 8;
                                            while (num2 < projVel / 60.0f) // this is raymarching ?
                                            {
                                                ++GlobalStats.Comparisons;
                                                if (shieldModule.Center.InRadius(otherObj.Center + direction * num2, collisionRadius))
                                                {
                                                    otherObj.Center = otherObj.Center + direction * num2;
                                                    otherObj.Position = otherObj.Center;
                                                    otherProj.Touch(shieldModule);
                                                    otherObj.CollidedThisFrame = true;
                                                    collision = true;
                                                    break;
                                                }
                                                num2 += 8;
                                            }
                                            if (collision)
                                                break;
                                        }
                                    }
                                    else break;
                                }
                                if (collision)
                                    break;
                                for (int i = 0; i < thisShip.ExternalSlots.Count; ++i)
                                {
                                    ++GlobalStats.DistanceCheckTotal;
                                    ModuleSlot extSlot = thisShip.ExternalSlots[i];
                                    if (extSlot.module != null && (extSlot.module.shield_power <= 0.0 || !otherProj.IgnoresShields) && extSlot.module.Active)
                                    {
                                        int num2 = 8;
                                        while (num2 < projVel / 60.0)
                                        {
                                            ++GlobalStats.Comparisons;
                                            if (Vector2.Distance(otherObj.Center + direction * num2, extSlot.module.Center) <= 8.0 + otherProj.weapon.ProjectileRadius + (extSlot.module.shield_power > 0.0 ? extSlot.module.shield_radius : 0.0))
                                            {
                                                otherObj.Center = otherObj.Center + direction * num2;
                                                otherObj.Position = otherObj.Center;
                                                otherProj.Touch(extSlot.module);
                                                otherObj.CollidedThisFrame = true;
                                                collision = true;
                                                break;
                                            }
                                            num2 += 8;
                                        }
                                        if (collision) break;
                                    }
                                }
                            }
                            else
                            {
                                bool collision = false;
                                for (int i = 0; i < thisShip.GetShields().Count; ++i)
                                {
                                    ++GlobalStats.Comparisons;
                                    ++GlobalStats.DistanceCheckTotal;
                                    ShipModule shield = thisShip.GetShields()[i];
                                    if ((shield.shield_power <= 0.0 || !otherProj.IgnoresShields) && shield.Active && otherObj.Center.Distance(shield.Center) <= 10.0f + (shield.shield_power > 0.0 ? shield.shield_radius : 0.0))
                                    {
                                        collision = true;
                                        otherProj.Touch(shield);
                                        break;
                                    }
                                }
                                if (!collision)
                                {
                                    for (int index = 0; index < thisShip.ExternalSlots.Count; ++index)
                                    {
                                        ++GlobalStats.DistanceCheckTotal;
                                        ModuleSlot moduleSlot = thisShip.ExternalSlots[index];
                                        if (moduleSlot.module != null && moduleSlot.module.Active && 
                                            (moduleSlot.module.shield_power <= 0.0f || !otherProj.IgnoresShields) &&
                                            moduleSlot.module.Center.InRadius(otherObj.Center, 10.0f + (moduleSlot.module.shield_power > 0.0f ? moduleSlot.module.shield_radius : 0.0f)))
                                        {
                                            otherProj.Touch(moduleSlot.module);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisObj is Projectile thisProj)
                {
                    if (otherObj is Projectile otherProj)
                    {
                        if (thisObj.Center.InRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherProj.weapon.ProjectileRadius))
                        {
                            CollisionResults.Add(new CollisionResult
                            {
                                Distance = 0.0f,
                                Normal = new Vector2(),
                                GameplayObject = otherObj
                            });
                            break;
                        }
                    }
                    else if (otherObj is Asteroid)
                    {
                        if (thisObj.Center.InRadius(otherObj.Center, thisProj.weapon.ProjectileRadius + otherObj.Radius))
                        {
                            CollisionResults.Add(new CollisionResult
                            {
                                Distance = 0.0f,
                                Normal = new Vector2(),
                                GameplayObject = otherObj
                            });
                            break;
                        }
                    }
                    else if (otherObj is Ship otherShip && thisProj.loyalty != otherShip.loyalty && Vector2.Distance(thisObj.Center, otherObj.Center) < otherObj.Radius + thisObj.Radius + thisProj.speed / 60.0)
                    {
                        otherShip.MoveModulesTimer = 2f;
                        if (thisProj.speed / 60.0 > 16.0)
                        {
                            Vector2 direction = Vector2.Normalize(thisObj.Velocity);
                            for (int i = 0; i < otherShip.ExternalSlots.Count; ++i)
                            {
                                ++GlobalStats.DistanceCheckTotal;
                                ModuleSlot slot = otherShip.ExternalSlots[i];
                                if (slot.module != null && (slot.module.shield_power <= 0.0 || !thisProj.IgnoresShields) && slot.module.Active)
                                {
                                    bool collision = false;
                                    float radius = 8.0f + (slot.module.shield_power > 0.0f ? slot.module.shield_radius : 0.0f);

                                    int num1 = 8;
                                    while (num1 < thisProj.speed * 2.0 / 60.0) // raymarch again?
                                    {
                                        ++GlobalStats.Comparisons;
                                        if (slot.module.Center.InRadius(thisObj.Center + direction * num1, radius))
                                        {
                                            thisObj.Center   = thisObj.Center + direction * num1;
                                            thisObj.Position = thisObj.Center;
                                            CollisionResults.Add(new CollisionResult
                                            {
                                                Distance       = thisObj.Radius + slot.module.Radius,
                                                Normal         = new Vector2(),
                                                GameplayObject = slot.module
                                            });
                                            collision = true;
                                            break;
                                        }
                                        num1 += 8;
                                    }
                                    if (collision)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < otherShip.ExternalSlots.Count; ++i)
                            {
                                ++GlobalStats.Comparisons;
                                ++GlobalStats.DistanceCheckTotal;
                                ModuleSlot slot = otherShip.ExternalSlots.ElementAt(i);
                                if (slot.module != null && slot.module.Active && 
                                    (slot.module.shield_power <= 0.0 || !thisProj.IgnoresShields) &&
                                    slot.module.Center.InRadius(thisObj.Center, 10.0f + (slot.module.shield_power > 0.0f ? slot.module.shield_radius : 0.0f)))
                                {
                                    CollisionResults.Add(new CollisionResult
                                    {
                                        Distance       = thisObj.Radius + slot.module.Radius,
                                        Normal         = new Vector2(),
                                        GameplayObject = slot.module
                                    });
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Vector2 fromThisToOther = otherObj.Center - thisObj.Center;
                    float distance = fromThisToOther.Length();
                    if (distance > 0.0f)
                    {
                        float dist2 = MathHelper.Max(distance - (otherObj.Radius + thisObj.Radius), 0.0f);
                        CollisionResults.Add(new CollisionResult
                        {
                            Distance = dist2,
                            Normal = Vector2.Normalize(fromThisToOther),
                            GameplayObject = otherObj
                        });
                    }
                }
            }
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

                if (ship.loyalty != null && ship.loyalty.data.ExplosiveRadiusReduction != 0)
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
            BatchRemovalCollection<ExplosionRay> removalCollection = new BatchRemovalCollection<ExplosionRay>(false);
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
            CollidableObjects = null;
            CollidableProjectiles = null;
            Asteroids = null;
            BeamList = null;
        }
    }
}
