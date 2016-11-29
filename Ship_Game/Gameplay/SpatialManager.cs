// Type: Ship_Game.Gameplay.SpatialManager
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager: IDisposable
    {
        public BatchRemovalCollection<GameplayObject> CollidableObjects = new BatchRemovalCollection<GameplayObject>();
        public BatchRemovalCollection<Projectile> CollidableProjectiles = new BatchRemovalCollection<Projectile>();
        public BatchRemovalCollection<Asteroid> Asteroids = new BatchRemovalCollection<Asteroid>();
        public BatchRemovalCollection<Beam> BeamList = new BatchRemovalCollection<Beam>();
        private float bucketUpdateTimer = 0.5f;
        private List<SpatialManager.CollisionResult> collisionResults = new List<SpatialManager.CollisionResult>();
        private const float speedDamageRatio = 0.25f;
        private int Cols;
        private int Rows;
        private Vector2 UpperLeftBound;
               // private Dictionary<int, List<GameplayObject>> Buckets;
        //private ConcurrentDictionary<int, List<GameplayObject>> Buckets;
        private ConcurrentDictionary<int, BatchRemovalCollection<GameplayObject>> Buckets;
        public int SceneWidth;
        public int SceneHeight;
        public int CellSize;
        public bool FineDetail;
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 Pos)
        {
            this.UpperLeftBound.X = Pos.X - (float)(sceneWidth / 2);
            this.UpperLeftBound.Y = Pos.Y - (float)(sceneHeight / 2);
            this.Cols = sceneWidth / cellSize;
            this.Rows = sceneHeight / cellSize;
            //this.Buckets = new Dictionary<int, List<GameplayObject>>(this.Cols * this.Rows);
            this.Buckets = new ConcurrentDictionary<int, BatchRemovalCollection<GameplayObject>>();
            for (int key = 0; key < this.Cols * this.Rows; ++key)
                this.Buckets.TryAdd(key, new BatchRemovalCollection<GameplayObject>());
            this.SceneWidth = sceneWidth;
            this.SceneHeight = sceneHeight;
            this.CellSize = cellSize;
        }

        public void Update(float elapsedTime, SolarSystem system)
        {
            this.BeamList.ApplyPendingRemovals();
            this.bucketUpdateTimer -= elapsedTime;
            if (this.bucketUpdateTimer <= 0.0)
            {
                this.ClearBuckets();
                if (system != null)
                {
                    if (system.CombatInSystem && system.ShipList.Count > 10)
                    {
                        if (!this.FineDetail || this.Buckets.Count < 20 && this.CollidableProjectiles.Count > 0)
                        {
                            this.Setup(200000, 200000, 6000, system.Position);
                            this.FineDetail = true;
                        }
                    }
                    else if (this.FineDetail || this.Buckets.Count > 20 || this.CollidableProjectiles.Count == 0)
                        this.Setup(200000, 200000, 50000, system.Position);
                }
                GameplayObject gameplayObject = null;
                for (int index = 0; index < this.CollidableObjects.Count; ++index)
                {
                    gameplayObject = this.CollidableObjects[index];
                    if (gameplayObject != null)
                    {
                        if (gameplayObject.GetSystem() != null && system == null)
                            this.CollidableObjects.QueuePendingRemoval(gameplayObject);
                        else if (gameplayObject != null)
                        {
                            if (gameplayObject.Active)
                                this.RegisterObject(gameplayObject);
                            else
                                this.CollidableObjects.QueuePendingRemoval(gameplayObject);
                        }
                    }
                }
                for (int index = 0; index < this.CollidableProjectiles.Count; ++index)
                {
                    Projectile projectile = this.CollidableProjectiles[index];
                    if (projectile.GetSystem() != null && system == null)
                        this.CollidableProjectiles.QueuePendingRemoval(projectile);
                    else if (projectile != null && projectile.Active)
                        this.RegisterObject((GameplayObject)projectile);
                }
                this.bucketUpdateTimer = 0.5f;
            }
            if (this.CollidableProjectiles.Count > 0)
            {
                for (int index = 0; index < this.CollidableObjects.Count; ++index)
                {
                    GameplayObject gameplayObject = this.CollidableObjects[index];
                    Ship oShip = gameplayObject as Ship;
                    //if (gameplayObject != null &&!(gameplayObject.GetSystem() != null & system == null) &&  (!(gameplayObject is Ship) || system != null || (gameplayObject as Ship).GetAI().BadGuysNear))
                    if (gameplayObject != null && !(gameplayObject.GetSystem() != null & system == null) && (oShip == null || system != null || oShip.GetAI().BadGuysNear))
                        this.MoveAndCollide(gameplayObject);
                }
            }
            for (int index = 0; index < this.BeamList.Count; ++index)
            {
                Beam beam = this.BeamList[index];
                if (beam != null)
                    this.CollideBeam(beam);
            }
            this.CollidableObjects.ApplyPendingRemovals();
            this.CollidableProjectiles.ApplyPendingRemovals();
        }

        public void UpdateBucketsOnly(float elapsedTime)
        {
            this.bucketUpdateTimer -= elapsedTime;
            if (this.bucketUpdateTimer <= 0f)
            {
                this.ClearBuckets();
                for (int index = 0; index < this.CollidableObjects.Count; ++index)
                {
                    GameplayObject gameplayObject = this.CollidableObjects[index];
                    if (gameplayObject != null)
                    {
                        if (gameplayObject.Active)
                            this.RegisterObject(gameplayObject);
                        else
                            this.CollidableObjects.QueuePendingRemoval(gameplayObject);
                    }
                }
                this.bucketUpdateTimer = 0.5f;
            }
            this.CollidableObjects.ApplyPendingRemovals();
            this.CollidableProjectiles.ApplyPendingRemovals();
        }

        internal List<GameplayObject> GetNearby(GameplayObject obj)
        {
            BatchRemovalCollection<GameplayObject> list = new BatchRemovalCollection<GameplayObject>();
           
            {
                BatchRemovalCollection<GameplayObject> test;
                foreach (int key in this.GetIdForObj(obj))
                {

                    
                    if (!this.Buckets.TryGetValue(key, out test))
                    {
            
                        return this.Buckets[1];
                      
                    }
          
                    list.AddRange(test);
                    test = null;
          
                }
            }
       
            return list;
        }

        internal List<GameplayObject> GetNearby(Vector2 Position)
        {
            List<GameplayObject> list = new List<GameplayObject>();
            //try
            {
                foreach (int key in this.GetIdForPos(Position))
                {
                    BatchRemovalCollection<GameplayObject> test;

                    if (this.Buckets.TryGetValue(key, out test))
                        list.AddRange(test);

                    //if (this.Buckets.ContainsKey(key))
                    //    list.AddRange((IEnumerable<GameplayObject>)this.Buckets[key]);
                }
            }
            //catch
            {
            }
            return list;
        }

        internal void RegisterObject(GameplayObject obj)
        {
            //try
            {
                BatchRemovalCollection<GameplayObject> test;
                foreach (int key in this.GetIdForObj(obj))
                {
                    

                    if (this.Buckets.TryGetValue(key, out test))
                        test.Add(obj);
                    else
                        this.Buckets[1].Add(obj);

                    //if (this.Buckets.ContainsKey(key))
                    //    this.Buckets[key].Add(obj);
                    //else
                    //    this.Buckets[1].Add(obj);
                }
            }
            //catch
            {
            }
        }

        public List<int> GetIdForObj(GameplayObject obj)
        {
            List<int> buckettoaddto = new List<int>();
            Vector2 vector2_1 = obj.Center - this.UpperLeftBound;
            Vector2 vector = new Vector2(vector2_1.X - obj.Radius, vector2_1.Y - obj.Radius);
            Vector2 vector2_2 = new Vector2(vector2_1.X + obj.Radius, vector2_1.Y + obj.Radius);
            float width = (float)(this.SceneWidth / this.CellSize);
            this.AddBucket(vector, width, buckettoaddto);
            this.AddBucket(new Vector2(vector2_2.X, vector.Y), width, buckettoaddto);
            this.AddBucket(new Vector2(vector2_2.X, vector2_2.Y), width, buckettoaddto);
            this.AddBucket(new Vector2(vector.X, vector2_2.Y), width, buckettoaddto);
            return buckettoaddto;
        }

        public List<int> GetIdForPos(Vector2 Position)
        {
            List<int> buckettoaddto = new List<int>();
            Vector2 vector2_1 = Position - this.UpperLeftBound;
            Vector2 vector = new Vector2(vector2_1.X - 100f, vector2_1.Y - 100f);
            Vector2 vector2_2 = new Vector2(vector2_1.X + 100f, vector2_1.Y + 100f);
            float width = (float)(this.SceneWidth / this.CellSize);
            this.AddBucket(vector, width, buckettoaddto);
            this.AddBucket(new Vector2(vector2_2.X, vector.Y), width, buckettoaddto);
            this.AddBucket(new Vector2(vector2_2.X, vector2_2.Y), width, buckettoaddto);
            this.AddBucket(new Vector2(vector.X, vector2_2.Y), width, buckettoaddto);
            return buckettoaddto;
        }

        private void MemoryCollider()
        {
        }

        private void AddBucket(Vector2 vector, float width, List<int> buckettoaddto)
        {
            int num = (int)(Math.Floor((double)vector.X / (double)this.CellSize) + Math.Floor((double)vector.Y / (double)this.CellSize) * (double)width);
            if (buckettoaddto.Contains(num))
                return;
            buckettoaddto.Add(num);
        }

        public void Destroy()
        {
            this.Buckets = null;// (Dictionary<int, List<GameplayObject>>)null;
        }

        internal void ClearBuckets()
        {
            for (int index = 0; index < this.Cols * this.Rows; ++index)
            {
                BatchRemovalCollection<GameplayObject> test;
                if(this.Buckets.TryGetValue(index, out test))
                {
                    test.Clear();
                }
            }
        }

        private Vector2 MoveAndCollide(GameplayObject gameplayObject)
        {
            this.Collide(gameplayObject);
            if (this.collisionResults.Count > 0)
            {
                this.collisionResults.Sort(new Comparison<SpatialManager.CollisionResult>(SpatialManager.CollisionResult.Compare));
                foreach (SpatialManager.CollisionResult collisionResult in this.collisionResults)
                {
                    if (gameplayObject.Touch(collisionResult.GameplayObject) || collisionResult.GameplayObject.Touch(gameplayObject))
                        return Vector2.Zero;
                }
            }
            return Vector2.Zero;
        }

        private void CollideBeam(Beam beam)
        {
            this.collisionResults.Clear();
            beam.CollidedThisFrame = false;
            Vector2 vector2_1 = Vector2.Normalize(beam.Destination - beam.Source);
            float num1 = Vector2.Distance(beam.Destination, beam.Source);
            if (num1 > beam.range + 10f)
                return;
            List<Vector2> list1 = new List<Vector2>();
            beam.ActualHitDestination = beam.Destination;
            for (int index = 0; (index * 75) < num1; ++index)
                list1.Add(beam.Source + vector2_1 * index * 75f);
            Ship ship1 = (Ship)null;
            Vector2 vector2_2 = Vector2.Zero;
            GameplayObject gameplayObject1 = (GameplayObject)null;
            //How repair beams repair modules
            if (beam.GetTarget() != null)
            {
                Ship ship2 = beam.GetTarget() as Ship;
                if (ship2 != null)
                {

                    ship2.MoveModulesTimer = 2f;
                    Vector2 vector2_3 = beam.GetTarget().Center;
                    beam.ActualHitDestination = beam.GetTarget().Center;
                    if (beam.damageAmount >= 0f)
                    {
                        //beam.owner.Beams.QueuePendingRemoval(beam);
                        return;

                    }
                    using (LinkedList<ModuleSlot>.Enumerator enumerator = ship2.ModuleSlotList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ModuleSlot current = enumerator.Current;
                            if (current.module.Health < current.module.HealthMax)
                            {
                                ShipModule module = current.module;
                                float num2 = module.Health - beam.damageAmount;
                                module.Health = num2;
                                if (current.module.Health < current.module.HealthMax)
                                    break;
                                current.module.Health = current.module.HealthMax;
                                break;
                            }
                        }
                        return;
                    }

                }
                else if (beam.GetTarget() is ShipModule)
                {
                    gameplayObject1 = (GameplayObject)(beam.GetTarget() as ShipModule).GetParent();

                }
                else if (beam.GetTarget() is Asteroid)
                    gameplayObject1 = beam.GetTarget();
                else

                    System.Diagnostics.Debug.WriteLine("beam null");

            }
            else if (beam.Owner != null)
                gameplayObject1 = (GameplayObject)beam.owner;
            List<GameplayObject> nearby = new List<GameplayObject>(this.GetNearby(gameplayObject1).OrderBy(distance => Vector2.Distance(beam.Source, distance.Center)));
            List<GameplayObject> AlliedShips = new List<GameplayObject>();
            object locker = new object();
            //bool flag = false;          //Not referenced in code, removing to save memory
            //foreach (Vector2 vector2_3 in list1)
            {
                Vector2 unitV = Vector2.Normalize(beam.Destination - beam.Source);

                Ray beampath = new Ray(new Vector3(beam.Source, 0), new Vector3(unitV.X, unitV.Y, 0));
                // Vector3 shipsphere = Vector3.Zero;

                float hit2 = beam.range;
                //if (beam.hitLast != null)
                //    hit2 = Vector2.Distance(beam.Source, beam.ActualHitDestination)+575;
                ShipModule shieldTarget = null;

                if (nearby.Count == 0)
                    return;
                var source = Enumerable.Range(0, nearby.Count).ToArray();
                var rangePartitioner = Partitioner.Create(0, source.Length);
                //handle each weapon group in parallel
                Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {
                    for (int T = range.Item1; T < range.Item2; T++)
                    {

                        Ship shipObject2 = nearby[T] as Ship;
                        if (shipObject2 != null) //if not a ship continue
                        {
                            float? hit = 0;


                            if (shipObject2.loyalty != beam.owner.loyalty) //dont hit allied. need to expand this to actual allies.
                            {
                                if (shipObject2 != beam.owner || beam.weapon.HitsFriendlies) //hits friendlies is in the  wrong place.
                                {
                                    ++GlobalStats.BeamTests;



                                    if (!beam.IgnoresShields && shipObject2.GetShields().Count > 0)
                                        if (true)
                                        {
                                            Vector3 shieldCenter = new Vector3(0, 0, 0);
                                            BoundingSphere shieldhit = new BoundingSphere(new Vector3(0f, 0f, 0f), 0f); //create a bounding sphere object for shields.

                                            foreach (ShipModule shield in shipObject2.GetShields())
                                            {
                                                if (!shield.Powered || shield.shield_power <= 0 || !shield.Active)
                                                    continue;
                                                shieldhit.Center.X = shield.Center.X;
                                                shieldhit.Center.Y = shield.Center.Y;
                                                shieldhit.Radius = shield.Radius + 4;

                                                hit = beampath.Intersects(shieldhit);

                                                if (hit.HasValue)
                                                {
                                                    lock (locker)
                                                        if (hit < hit2)
                                                        {
                                                            hit2 = (float)hit;
                                                            ship1 = shipObject2;
                                                            ship1.MoveModulesTimer = 2f;
                                                            shieldTarget = shield;

                                                            Vector3 crap = beampath.Position + beampath.Direction * hit.Value;
                                                            vector2_2.X = crap.X;
                                                            vector2_2.Y = crap.Y;
                                                        }
                                                    //break;
                                                }

                                            }

                                        }
                                    //if(!hit.HasValue)
                                    {
                                        hit = beampath.Intersects(shipObject2.GetSO().WorldBoundingSphere);
                                        if (hit.HasValue)
                                        {
                                            lock (locker)
                                                if (hit < hit2)
                                                {
                                                    hit2 = (float)hit;
                                                    ship1 = shipObject2;
                                                    ship1.MoveModulesTimer = 2f;
                                                    shieldTarget = null;

                                                    Vector3 crap = beampath.Position + beampath.Direction * hit.Value;
                                                    vector2_2.X = crap.X;
                                                    vector2_2.Y = crap.Y;
                                                }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
                //flag = shieldTarget == null;
                //if (!flag)
                //{

                //        if (beam.Touch(shieldTarget))
                //        {
                //            beam.CollidedThisFrame = shieldTarget.CollidedThisFrame = true;
                //            beam.ActualHitDestination = vector2_2;

                //            beam.hitLast = shieldTarget;
                //            this.collisionResults.Add(new SpatialManager.CollisionResult()
                //            {
                //                Distance = shieldTarget.Radius + 8f,
                //                Normal = Vector2.Normalize(Vector2.Zero),
                //                GameplayObject = (GameplayObject)shieldTarget
                //            });
                //            //if(beam.damageAmount >0)


                //        }
                //        return;
                //}
                //else
                {
                    hit2 = beam.range;
                    float? hitM = 0;
                    if (true && ship1 != null)
                    {
                        ShipModule damaged = null;
                        Vector3 shieldCenter = new Vector3(0, 0, 0);
                        BoundingSphere shieldhit = new BoundingSphere(new Vector3(0f, 0f, 0f), 0f); //create a bounding sphere object for shields.
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


                            hitM = beampath.Intersects(shieldhit);

                            if (hitM.HasValue)
                            {
                                //lock (locker)
                                if (hitM < hit2)
                                {
                                    hit2 = (float)hitM;
                                    damaged = shield.module;
                                    Vector3 crap = beampath.Position + beampath.Direction * hitM.Value;
                                    vector2_2.X = crap.X;
                                    vector2_2.Y = crap.Y;
                                }
                                //break;
                            }

                        }
                        if (damaged != null && beam.Touch(damaged))
                        {
                            beam.CollidedThisFrame = damaged.CollidedThisFrame = true;
                            beam.ActualHitDestination = vector2_2;
                            return;
                        }
                    }
                }

                beam.ActualHitDestination = beam.Destination;

                

            }
        }
        public void Collide(GameplayObject gameplayObject)
        {
            this.collisionResults.Clear();
            if (!gameplayObject.Active)
                return;
            foreach (GameplayObject gameplayObject1 in this.GetNearby(gameplayObject))
            {
                if (gameplayObject1 != null && gameplayObject != gameplayObject1 && (gameplayObject1.Active && !gameplayObject1.CollidedThisFrame))
                {
                    if (gameplayObject is Ship)
                    {
                        if (gameplayObject1 is Projectile && (gameplayObject as Ship).loyalty != (gameplayObject1 as Projectile).loyalty)
                        {
                            ++GlobalStats.Comparisons;
                            if (Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < (gameplayObject1 as Projectile).weapon.ProjectileRadius + (double)(gameplayObject as Ship).GetSO().WorldBoundingSphere.Radius + 100.0)
                            {
                                (gameplayObject as Ship).MoveModulesTimer = 2f;
                                float num1 = (gameplayObject1 as Projectile).Velocity.Length();
                                if (num1 / 60f > 10f)
                                {
                                    bool flag = false;
                                    Vector2 vector2 = Vector2.Normalize(gameplayObject1.Velocity);
                                    for (int index = 0; index < (gameplayObject as Ship).GetShields().Count; ++index)
                                    {
                                        ++GlobalStats.DistanceCheckTotal;
                                        ShipModule shipModule = (gameplayObject as Ship).GetShields()[index];
                                        if (shipModule != null)
                                        {
                                            if (shipModule.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields)
                                            {
                                                if (shipModule.shield_power > 0.0 && shipModule.Active)
                                                {
                                                    int num2 = 8;
                                                    while (num2 < num1 / 60.0)
                                                    {
                                                        ++GlobalStats.Comparisons;
                                                        if (Vector2.Distance(gameplayObject1.Center + vector2 * num2, shipModule.Center) <= 12.0 + (shipModule.shield_power > 0.0 ? shipModule.shield_radius : 0.0))
                                                        {
                                                            gameplayObject1.Center = gameplayObject1.Center + vector2 * num2;
                                                            gameplayObject1.Position = gameplayObject1.Center;
                                                            (gameplayObject1 as Projectile).Touch((GameplayObject)shipModule);
                                                            gameplayObject1.CollidedThisFrame = true;
                                                            flag = true;
                                                            break;
                                                        }
                                                        else
                                                            num2 += 8;
                                                    }
                                                    if (flag)
                                                        break;
                                                }
                                            }
                                            else
                                                break;
                                        }
                                    }
                                    if (flag)
                                        break;
                                    for (int index = 0; index < (gameplayObject as Ship).ExternalSlots.Count; ++index)
                                    {
                                        ++GlobalStats.DistanceCheckTotal;
                                        ModuleSlot moduleSlot = (gameplayObject as Ship).ExternalSlots.ElementAt(index);
                                        if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && moduleSlot.module.Active)
                                        {
                                            int num2 = 8;
                                            while (num2 < num1 / 60.0)
                                            {
                                                ++GlobalStats.Comparisons;
                                                if (Vector2.Distance(gameplayObject1.Center + vector2 * num2, moduleSlot.module.Center) <= 8.0 + (gameplayObject1 as Projectile).weapon.ProjectileRadius + (moduleSlot.module.shield_power > 0.0 ? moduleSlot.module.shield_radius : 0.0))
                                                {
                                                    gameplayObject1.Center = gameplayObject1.Center + vector2 * num2;
                                                    gameplayObject1.Position = gameplayObject1.Center;
                                                    (gameplayObject1 as Projectile).Touch((GameplayObject)moduleSlot.module);
                                                    gameplayObject1.CollidedThisFrame = true;
                                                    flag = true;
                                                    break;
                                                }
                                                else
                                                    num2 += 8;
                                            }
                                            if (flag)
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    bool flag = false;
                                    for (int index = 0; index < (gameplayObject as Ship).GetShields().Count; ++index)
                                    {
                                        ++GlobalStats.Comparisons;
                                        ++GlobalStats.DistanceCheckTotal;
                                        ShipModule shipModule = (gameplayObject as Ship).GetShields()[index];
                                        if (shipModule != null && (shipModule.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && shipModule.Active && Vector2.Distance(gameplayObject1.Center, shipModule.Center) <= 10.0 + (shipModule.shield_power > 0.0 ? shipModule.shield_radius : 0.0))
                                        {
                                            flag = true;
                                            (gameplayObject1 as Projectile).Touch((GameplayObject)shipModule);
                                            break;
                                        }
                                    }
                                    if (!flag)
                                    {
                                        for (int index = 0; index < (gameplayObject as Ship).ExternalSlots.Count; ++index)
                                        {
                                            ++GlobalStats.DistanceCheckTotal;
                                            ModuleSlot moduleSlot = (gameplayObject as Ship).ExternalSlots.ElementAt(index);
                                            if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && moduleSlot.module.Active && Vector2.Distance(gameplayObject1.Center, moduleSlot.module.Center) <= 10.0 + (moduleSlot.module.shield_power > 0.0 ? moduleSlot.module.shield_radius : 0.0))
                                            {
                                                (gameplayObject1 as Projectile).Touch((GameplayObject)moduleSlot.module);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (gameplayObject is Projectile)
                    {
                        if (gameplayObject1 is Projectile)
                        {
                            if (Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (gameplayObject as Projectile).weapon.ProjectileRadius + (gameplayObject1 as Projectile).weapon.ProjectileRadius)
                            {
                                this.collisionResults.Add(new SpatialManager.CollisionResult()
                                {
                                    Distance = 0.0f,
                                    Normal = Vector2.Normalize(Vector2.Zero),
                                    GameplayObject = gameplayObject1
                                });
                                break;
                            }
                        }
                        else if (gameplayObject1 is Asteroid)
                        {
                            if (Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (gameplayObject as Projectile).weapon.ProjectileRadius + gameplayObject1.Radius)
                            {
                                this.collisionResults.Add(new SpatialManager.CollisionResult()
                                {
                                    Distance = 0.0f,
                                    Normal = Vector2.Normalize(Vector2.Zero),
                                    GameplayObject = gameplayObject1
                                });
                                break;
                            }
                        }
                        else if (gameplayObject1 is Ship && (gameplayObject as Projectile).loyalty != (gameplayObject1 as Ship).loyalty && Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < gameplayObject1.Radius + gameplayObject.Radius + (gameplayObject as Projectile).speed / 60.0)
                        {
                            (gameplayObject1 as Ship).MoveModulesTimer = 2f;
                            if ((gameplayObject as Projectile).speed / 60.0 > 16.0)
                            {
                                Vector2 vector2 = Vector2.Normalize(gameplayObject.Velocity);
                                for (int index = 0; index < (gameplayObject1 as Ship).ExternalSlots.Count; ++index)
                                {
                                    ++GlobalStats.DistanceCheckTotal;
                                    ModuleSlot moduleSlot = (gameplayObject1 as Ship).ExternalSlots.ElementAt(index);
                                    if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active)
                                    {
                                        bool flag = false;
                                        int num1 = 8;
                                        while (num1 < (gameplayObject as Projectile).speed * 2.0 / 60.0)
                                        {
                                            ++GlobalStats.Comparisons;
                                            //double num2 = (double)Vector2.Distance(gameplayObject.Center + vector2 * (float)num1, moduleSlot.module.Center);
                                            if (Vector2.Distance(gameplayObject.Center + vector2 * num1, moduleSlot.module.Center) <= 8.0 + (moduleSlot.module.shield_power > 0.0 ? moduleSlot.module.shield_radius : 0.0))
                                            {
                                                gameplayObject.Center = gameplayObject.Center + vector2 * num1;
                                                gameplayObject.Position = gameplayObject.Center;
                                                this.collisionResults.Add(new SpatialManager.CollisionResult()
                                                {
                                                    Distance = gameplayObject.Radius + moduleSlot.module.Radius,
                                                    Normal = Vector2.Zero,
                                                    GameplayObject = (GameplayObject)moduleSlot.module
                                                });
                                                flag = true;
                                                break;
                                            }
                                            else
                                                num1 += 8;
                                        }
                                        if (flag)
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                for (int index = 0; index < (gameplayObject1 as Ship).ExternalSlots.Count; ++index)
                                {
                                    ++GlobalStats.Comparisons;
                                    ++GlobalStats.DistanceCheckTotal;
                                    ModuleSlot moduleSlot = (gameplayObject1 as Ship).ExternalSlots.ElementAt(index);
                                    if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active && Vector2.Distance(gameplayObject.Center, moduleSlot.module.Center) <= 10.0 + (moduleSlot.module.shield_power > 0.0 ? moduleSlot.module.shield_radius : 0.0))
                                    {
                                        this.collisionResults.Add(new SpatialManager.CollisionResult()
                                        {
                                            Distance = gameplayObject.Radius + moduleSlot.module.Radius,
                                            Normal = Vector2.Normalize(Vector2.Zero),
                                            GameplayObject = (GameplayObject)moduleSlot.module
                                        });
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // double num1 = (double)gameplayObject1.Radius;
                        // double num2 = (double)gameplayObject.Radius;
                        Vector2 vector2 = gameplayObject1.Center - gameplayObject.Center;
                        float num3 = vector2.Length();
                        if (num3 > 0.0)
                        {
                            float num4 = MathHelper.Max(num3 - (gameplayObject1.Radius + gameplayObject.Radius), 0.0f);
                            this.collisionResults.Add(new SpatialManager.CollisionResult()
                            {
                                Distance = num4,
                                Normal = Vector2.Normalize(vector2),
                                GameplayObject = gameplayObject1
                            });
                        }
                    }
                }
            }
        }
        public void Collidenew(GameplayObject gameplayObject)
        {
            this.collisionResults.Clear();
            if (!gameplayObject.Active)
                return;
            object locker = new object();
            List<GameplayObject> nearbythings = this.GetNearby(gameplayObject);
            if (nearbythings.Count == 0)
                return;
            var source = Enumerable.Range(0, nearbythings.Count).ToArray();
            var rangePartitioner = Partitioner.Create(0, source.Length);
            //handle each weapon group in parallel
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                //standard for loop through each weapon group.
                for (int T = range.Item1; T < range.Item2; T++)
                {
                    GameplayObject gameplayObject1 = nearbythings[T];
                    Vector3 object1position;
                        BoundingSphere object1;

                    //float minHit = 0f;          //Not referenced in code, removing to save memory
                    // foreach (GameplayObject gameplayObject1 in this.GetNearby(gameplayObject))
                    //{
                    if (gameplayObject1 != null && gameplayObject != gameplayObject1 && (gameplayObject1.Active && !gameplayObject1.CollidedThisFrame))
                    {
                        Ship GOShip = gameplayObject as Ship;
                        Projectile GO1Projectile = gameplayObject1 as Projectile;
                        if (GOShip != null)
                        {
                            if ( GO1Projectile != null && GOShip.loyalty != GO1Projectile.loyalty)
                            {
                                if (Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < GO1Projectile.weapon.ProjectileRadius + GOShip.GetSO().WorldBoundingSphere.Radius + 575)
                                {
                                    object1position = new Vector3(gameplayObject1.Center.X, gameplayObject1.Center.Y, 0);
                                    object1 = new BoundingSphere(object1position, gameplayObject1.Radius);
                                    ++GlobalStats.Comparisons;

                                    Ship ship1 = GOShip;
                                    float hitM = 0f;
                                    float hit2 = 0f;
                                    if (true)
                                    {


                                        ShipModule damaged = null;
                                        Vector3 shieldCenter = new Vector3(0, 0, 0);
                                        BoundingSphere shieldhit = new BoundingSphere(new Vector3(0f, 0f, 0f), 0f); //create a bounding sphere object for shields.
                                        //hit shields first.
                                        if(!GO1Projectile.IgnoresShields)
                                        foreach (ShipModule shield in ship1.GetShields())
                                        {
                                            if (!shield.Active || shield.shield_power <=0)
                                                continue;
                                            ShipModule test = shield;

                                            shieldhit.Radius = test.Radius;
                                            
                                            shieldhit.Center.X = test.Center.X;
                                            shieldhit.Center.Y = test.Center.Y;

                                            if (object1.Intersects(shieldhit))
                                            {

                                                hitM = Vector2.Distance(GO1Projectile.Center, test.Center);

                                                if (hitM > hit2)
                                                {
                                                    hit2 = (float)hitM;
                                                    damaged = test;
                                                    GOShip.MoveModulesTimer = 2f;
                                                }
                                                //break;
                                            }

                                        }
                                        if (damaged != null && GO1Projectile.Touch(damaged))
                                        {
                                            GO1Projectile.CollidedThisFrame = damaged.CollidedThisFrame = true;
                                            (gameplayObject as Ship).MoveModulesTimer = 2f;
                                            loopState.Break();
                                            return;
                                        }
                                        if (GOShip.GetSO().WorldBoundingSphere.Intersects(object1))
                                        {
                                            foreach (ModuleSlot shield in ship1.ExternalSlots)
                                            {
                                                if (!shield.module.Active || shield.module.quadrant < 1)
                                                    continue;
                                                ShipModule test = shield.module;
                                                shieldhit.Radius = 8;
                                                shieldhit.Center.X = test.Center.X;
                                                shieldhit.Center.Y = test.Center.Y;

                                                if (object1.Intersects(shieldhit))
                                                {
                                                    damaged = shield.module;
                                                    GOShip.MoveModulesTimer = 2f;
                                                    if (damaged != null && GO1Projectile.Touch(damaged))
                                                    {
                                                        GO1Projectile.CollidedThisFrame = damaged.CollidedThisFrame = true;
                                                        (gameplayObject as Ship).MoveModulesTimer = 2f;
                                                        loopState.Break();
                                                        return;
                                                    }
                                                    //break;
                                                }

                                            }
                                        }
  
                                        
                                    }


                                }
                            }
                        }
                        
                        else if (gameplayObject is Projectile)
                        {
                            if (gameplayObject1 is Projectile)
                            {
                                if (gameplayObject1.Health >0 && Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (gameplayObject as Projectile).weapon.ProjectileRadius + (gameplayObject1 as Projectile).weapon.ProjectileRadius)
                                {
                                    //gameplayObject.Touch(gameplayObject1);
                                    lock (locker)
                                        this.collisionResults.Add(new SpatialManager.CollisionResult()
                                        {
                                            Distance = 0.0f,
                                            Normal = Vector2.Normalize(Vector2.Zero),
                                            GameplayObject = gameplayObject1
                                        });
                                    loopState.Break();
                                    return;
                                }
                            }
                            else if (gameplayObject1 is Asteroid)
                            {
                                if (Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (gameplayObject as Projectile).weapon.ProjectileRadius + gameplayObject1.Radius)
                                {
                                    lock (locker)
                                        this.collisionResults.Add(new SpatialManager.CollisionResult()
                                        {
                                            Distance = 0.0f,
                                            Normal = Vector2.Normalize(Vector2.Zero),
                                            GameplayObject = gameplayObject1
                                        });
                                    loopState.Break();
                                    return;
                                }
                            }
                            else if (gameplayObject1 is Ship && (gameplayObject as Projectile).loyalty != (gameplayObject1 as Ship).loyalty && Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < gameplayObject1.Radius + gameplayObject.Radius + (gameplayObject as Projectile).speed / 60.0)
                            {
                                (gameplayObject1 as Ship).MoveModulesTimer = 2f;
                                if ((gameplayObject as Projectile).speed / 60f > 16)
                                {
                                    Vector2 vector2 = Vector2.Normalize(gameplayObject.Velocity);
                                    for (int index = 0; index < (gameplayObject1 as Ship).ExternalSlots.Count; ++index)
                                    {
                                        ++GlobalStats.DistanceCheckTotal;
                                        ModuleSlot moduleSlot = (gameplayObject1 as Ship).ExternalSlots.ElementAt(index);
                                        if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active)
                                        {
                                            bool flag = false;
                                            int num1 = 8;
                                            while (num1 < (gameplayObject as Projectile).speed * 2 / 60f)
                                            {
                                                ++GlobalStats.Comparisons;
                                                //double num2 = (double)Vector2.Distance(gameplayObject.Center + vector2 * (float)num1, moduleSlot.module.Center);
                                                if (Vector2.Distance(gameplayObject.Center + vector2 * num1, moduleSlot.module.Center) <= 8f + (moduleSlot.module.shield_power > 0f ? moduleSlot.module.shield_radius : 0f))
                                                {
                                                    gameplayObject.Center = gameplayObject.Center + vector2 * num1;
                                                    gameplayObject.Position = gameplayObject.Center;
                                                    lock (locker)
                                                        this.collisionResults.Add(new SpatialManager.CollisionResult()
                                                        {
                                                            Distance = gameplayObject.Radius + moduleSlot.module.Radius,
                                                            Normal = Vector2.Zero,
                                                            GameplayObject = (GameplayObject)moduleSlot.module
                                                        });
                                                    flag = true;
                                                    break;
                                                }
                                                else
                                                    num1 += 8;
                                            }
                                            if (flag)
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int index = 0; index < (gameplayObject1 as Ship).ExternalSlots.Count; ++index)
                                    {
                                        ++GlobalStats.Comparisons;
                                        ++GlobalStats.DistanceCheckTotal;
                                        ModuleSlot moduleSlot = (gameplayObject1 as Ship).ExternalSlots.ElementAt(index);
                                        if (moduleSlot != null && moduleSlot.module != null && (moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active && Vector2.Distance(gameplayObject.Center, moduleSlot.module.Center) <= 10.0 + (moduleSlot.module.shield_power > 0.0 ? moduleSlot.module.shield_radius : 0.0))
                                        {
                                            lock (locker)
                                                this.collisionResults.Add(new SpatialManager.CollisionResult()
                                                {
                                                    Distance = gameplayObject.Radius + moduleSlot.module.Radius,
                                                    Normal = Vector2.Normalize(Vector2.Zero),
                                                    GameplayObject = (GameplayObject)moduleSlot.module
                                                });
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // double num1 = (double)gameplayObject1.Radius;
                            // double num2 = (double)gameplayObject.Radius;
                            Vector2 vector2 = gameplayObject1.Center - gameplayObject.Center;
                            float num3 = vector2.Length();
                            if (num3 > 0.0)
                            {
                                float num4 = MathHelper.Max(num3 - (gameplayObject1.Radius + gameplayObject.Radius), 0.0f);
                                lock (locker)
                                    this.collisionResults.Add(new SpatialManager.CollisionResult()
                                    {
                                        Distance = num4,
                                        Normal = Vector2.Normalize(vector2),
                                        GameplayObject = gameplayObject1
                                    });
                            }
                        }
                    }
                }
            });
        }


        public void Explode(GameplayObject source, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;
            float num1 = damageRadius * damageRadius;
            Vector2 ExplosionCenter = new Vector2();
            ExplosionCenter = source.Center;
            if (source is ShipModule)
            {
                ShipModule shipModule = source as ShipModule;
                ExplosionCenter = (int)shipModule.XSIZE != 1 || (int)shipModule.YSIZE != 3 ? ((int)shipModule.XSIZE != 2 || (int)shipModule.YSIZE != 5 ? new Vector2(shipModule.Center.X - 8f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2)) : new Vector2(shipModule.Center.X - 80f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2))) : new Vector2(shipModule.Center.X - 50f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2));
                Vector2 target = new Vector2(shipModule.Center.X - 8f, shipModule.Center.Y - 8f);
                float angleToTarget = HelperFunctions.findAngleToTarget(ExplosionCenter, target);
                Vector2 angleAndDistance = HelperFunctions.findPointFromAngleAndDistance(shipModule.Center, MathHelper.ToDegrees((source as ShipModule).Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                float num2 = (float)((int)shipModule.XSIZE * 16 / 2);
                float num3 = (float)((int)shipModule.YSIZE * 16 / 2);
                float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num2, 2.0) + (float)Math.Pow((double)num3, 2.0)));
                float radians = 3.141593f - (float)Math.Asin((double)num2 / (double)distance) + (source as ShipModule).GetParent().Rotation;
                ExplosionCenter = HelperFunctions.findPointFromAngleAndDistance(angleAndDistance, MathHelper.ToDegrees(radians), distance);
            }
            BatchRemovalCollection<ExplosionRay> removalCollection = new BatchRemovalCollection<ExplosionRay>();
            int num4 = 15;
            float num5 = (float)(360 / num4);
            for (int index = 0; index < num4; ++index)
                removalCollection.Add(new ExplosionRay()
                {
                    Direction = HelperFunctions.findPointFromAngleAndDistance(Vector2.Zero, num5 * (float)index, 1f),
                    Damage = damageAmount / (float)num4
                });
            List<ShipModule> list; //= new List<ShipModule>();
            List<ModuleSlot> list1; //= new List<ModuleSlot>();
            foreach (GameplayObject gameplayObject1 in this.GetNearby(source))
            {
                try
                {
                    if (gameplayObject1 != null)
                    {
                        if (!(gameplayObject1 is Projectile))
                        {
                            Vector2 vector2_1 = Vector2.Zero;
                            if (gameplayObject1.Active)
                            {
                                if (gameplayObject1 != source)
                                {
                                    if (Vector2.Distance(gameplayObject1.Center, source.Center) <= damageRadius + gameplayObject1.Radius)
                                    {
                                        Projectile projectile1 = source as Projectile;
                                        if (gameplayObject1 is Ship && projectile1 != null)
                                        {
                                            if (projectile1.Owner != null)
                                            {
                                                if ((gameplayObject1 as Ship).loyalty == projectile1.owner.loyalty)
                                                    continue;
                                            }
                                            if (projectile1.Planet != null)
                                            {
                                                if (projectile1.Planet.Owner == (gameplayObject1 as Ship).loyalty)
                                                    continue;
                                            }
                                        }
                                        if (gameplayObject1 is Ship && source != gameplayObject1)
                                        {
                                            if (source is Projectile && (double)(gameplayObject1 as Ship).shield_max > 0.0)
                                            {
                                                 list = new List<ShipModule>();
                                                foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                                {
                                                    if ((double)moduleSlot.module.shield_power > 0.0 && moduleSlot.module.Active)
                                                        list.Add(moduleSlot.module);
                                                }
                                                float num2 = damageAmount;
                                                foreach (ShipModule shipModule in list)
                                                {
                                                    if (Vector2.Distance(ExplosionCenter, shipModule.Center) <= damageRadius + shipModule.shield_radius && shipModule.shield_power > 0.0)
                                                    {
                                                        num2 = damageAmount - shipModule.shield_power;
                                                        
                                                        // Make sure explosions don't apply full damage when weapon is set to have penalty. Equally, applies bonus against armour from weapon.
                                                        if (shipModule.ModuleType == ShipModuleType.Armor)
                                                        {
                                                            if ((source as Projectile).isSecondary)
                                                            {
                                                                Weapon shooter = (source as Projectile).weapon;
                                                                ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                                                                damageAmount *= shooter.EffectVSShields; 
                                                                //damageAmount *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVSShields);
                                                            }
                                                            else
                                                            {
                                                                damageAmount *= (source as Projectile).weapon.EffectVSShields;
                                                            }
                                                        }
                                                        // doesn't this mean explosions hit through shields? It's not applying the 'bleed' if shields drop from damage, but half of all damage impacting...
                                                        shipModule.Damage(source, damageAmount / 2f);
                                                        break;
                                                    }
                                                }
                                                if (num2 <= 0.0)
                                                    break;
                                                damageAmount = num2;
                                            }
                                            list1 = new List<ModuleSlot>();
                                            foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                            {
                                                if (moduleSlot.module.Active && (double)Vector2.Distance(moduleSlot.module.Center, ExplosionCenter) <= damageRadius + moduleSlot.module.Radius)
                                                    list1.Add(moduleSlot);
                                            }
                                            if (list1.Count == 0)
                                                break;
                                            IOrderedEnumerable<ModuleSlot> orderedEnumerable = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)list1, (Func<ModuleSlot, float>)(moduleslot => Vector2.Distance(ExplosionCenter, moduleslot.module.Center)));
                                            int num3 = 0;
                                            while (num3 < damageRadius)
                                            {
                                                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                                                {
                                                    if (explosionRay.Damage > 0.0)
                                                    {
                                                        foreach (ModuleSlot moduleSlot in (IEnumerable<ModuleSlot>)orderedEnumerable)
                                                        {
                                                            if (moduleSlot.module.Active && moduleSlot.module.Health > 0.0)
                                                            {
                                                                GameplayObject gameplayObject2 = (GameplayObject)moduleSlot.module;
                                                                Vector2 vector2_2 = ExplosionCenter + explosionRay.Direction * num3;
                                                                Vector2 vector2_3 = gameplayObject2.Center - vector2_2;
                                                                if ((double)Vector2.Distance(vector2_2, gameplayObject2.Center) <= 8.0)
                                                                {
                                                                    Projectile projectile2 = gameplayObject1 as Projectile;
                                                                    if (gameplayObject2 != source && projectile2 == null)
                                                                    {
                                                                        if (source is Ship || source is ShipModule && (source as ShipModule).GetParent() != gameplayObject1)
                                                                            vector2_1 += 3f * explosionRay.Damage * explosionRay.Direction;
                                                                        if ((double)(gameplayObject1 as Ship).yRotation > 0.0 && !(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation += explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((gameplayObject1 as Ship).yRotation > (gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = (gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                        else if (!(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation -= explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((gameplayObject1 as Ship).yRotation < -(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = -(gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                    }
                                                                    if ((double)explosionRay.Damage > 0.0)
                                                                    {
                                                                        float health = (gameplayObject2 as ShipModule).Health;
                                                                        if ((gameplayObject2 as ShipModule).ModuleType == ShipModuleType.Armor)
                                                                        {
                                                                            if ((source as Projectile).isSecondary)
                                                                            {
                                                                                Weapon shooter = (source as Projectile).weapon;
                                                                                ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                                                                                explosionRay.Damage *= shooter.EffectVsArmor; 
                                                                                //explosionRay.Damage *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVsArmor);
                                                                            }
                                                                            else
                                                                            {
                                                                                explosionRay.Damage *= (source as Projectile).weapon.EffectVsArmor;
                                                                            }                                                                            
                                                                        }
                                                                        (gameplayObject2 as ShipModule).Damage(source, explosionRay.Damage);
                                                                        explosionRay.Damage -= health;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                num3 += 8;
                                            }
                                            if (vector2_1.Length() > 200.0)
                                                vector2_1 = Vector2.Normalize(vector2_1) * 200f;
                                            if (!float.IsNaN(vector2_1.X))
                                                gameplayObject1.Velocity += vector2_1;
                                        }
                                        else
                                        {
                                            if (gameplayObject1 is Ship && source == gameplayObject1)
                                                break;
                                            float num2 = (gameplayObject1.Center - position).LengthSquared();
                                            if (num2 > 0.0)
                                            {
                                                if (num2 <= num1)
                                                {
                                                    float num3 = (float)Math.Sqrt((double)num2);
                                                    float damageAmount1 = damageAmount * (damageRadius - num3) / damageRadius;
                                                    if (damageAmount1 > 0.0)
                                                        gameplayObject1.Damage(source, damageAmount1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        //Added by McShooterz: New way to distribute exploding projectile damage
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;

            float modifiedRadius = damageRadius;

            foreach (GameplayObject gameplayObject in this.GetNearby(source).Where(gameplayObject => gameplayObject != null && gameplayObject is Ship && gameplayObject.Active && !(gameplayObject as Ship).dying).OrderBy(gameplayObject => Vector2.Distance(source.Center, gameplayObject.Center)))
            {
                
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.

                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                modifiedRadius = damageRadius;
                
                //Check if valid target
                //added by gremlin check that projectile owner is not null
                if (source.Owner == null || source.Owner != null && source.Owner.loyalty != (gameplayObject as Ship).loyalty)
                {
                    
                    // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
                    if ((gameplayObject as Ship).loyalty != null && (gameplayObject as Ship).loyalty.data.ExplosiveRadiusReduction != 0)
                    {
                        modifiedRadius *= (1 - (gameplayObject as Ship).loyalty.data.ExplosiveRadiusReduction);
                    }

                    float DamageTracker = 0;
                    IEnumerable<ModuleSlot> modules = (gameplayObject as Ship).ModuleSlotList.Where(moduleSlot => moduleSlot.module.Health > 0.0 && (moduleSlot.module.shield_power > 0.0 && !source.IgnoresShields) ? Vector2.Distance(source.Center, moduleSlot.module.Center) <= modifiedRadius + moduleSlot.module.shield_radius : Vector2.Distance(source.Center, moduleSlot.module.Center) <= modifiedRadius + 10f).OrderBy(moduleSlot => (moduleSlot.module.shield_power > 0.0 && !source.IgnoresShields) ? Vector2.Distance(source.Center, moduleSlot.module.Center) - moduleSlot.module.shield_radius : Vector2.Distance(source.Center, moduleSlot.module.Center));
                    foreach (ModuleSlot moduleSlot in modules)
                    {
                        moduleSlot.module.Damage(source, damageAmount, ref DamageTracker);
                        if (DamageTracker > 0)
                            damageAmount = DamageTracker;
                        else return;
                    }
                }
            }
        }

        //Modified by McShooterz: not used before, changed to be used for exploding modules
        public void ExplodeAtModule(GameplayObject source, ShipModule HitModule, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0 || HitModule.GetParent().dying || !HitModule.GetParent().Active)
                return;
            BatchRemovalCollection<ExplosionRay> removalCollection = new BatchRemovalCollection<ExplosionRay>(false);
            int num1 = 15;
            float num2 = (float)(360 / num1);
            for (int index = 0; index < num1; ++index)
                removalCollection.Add(new ExplosionRay()
                {
                    Direction = HelperFunctions.findPointFromAngleAndDistance(Vector2.Zero, num2 * (float)index, 1f),
                    Damage = damageAmount / (float)num1
                });
            List<ShipModule> list = new List<ShipModule>();
            list.Add(HitModule);
            foreach (ModuleSlot ModuleSlot in HitModule.GetParent().ModuleSlotList.Where(moduleSlot => Vector2.Distance(HitModule.Center, moduleSlot.module.Center) <= damageRadius).OrderBy(moduleSlot => Vector2.Distance(HitModule.Center, moduleSlot.module.Center)))
            {
                list.Add(ModuleSlot.module);
            }
            int num3 = 0;
            while (num3 <damageRadius)
            {
                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                {
                    if (explosionRay.Damage > 0.0)
                    {
                        foreach (ShipModule shipModule in list)
                        {
                            if (shipModule.Active && shipModule.Health > 0.0)
                            {
                                Vector2 vector2_1 = HitModule.Center + explosionRay.Direction * (float)num3;
                                Vector2 vector2_2 = shipModule.Center - vector2_1;
                                if (Vector2.Distance(vector2_1, shipModule.Center) <= 8.0 && explosionRay.Damage > 0.0)
                                {
                                    float health = shipModule.Health;
                                    shipModule.Damage(source, explosionRay.Damage);
                                    explosionRay.Damage -= health;
                                }
                            }
                        }
                    }
                }
                num3 += 8;
            }
        }

        public void ShipExplode(GameplayObject source, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0)
                return;
            float num1 = damageRadius * damageRadius;
            Vector2 ExplosionCenter = new Vector2();
            ExplosionCenter = source.Center;
            BatchRemovalCollection<ExplosionRay> removalCollection = new BatchRemovalCollection<ExplosionRay>(false);
            int num4 = 15;
            float num5 = (float)(360 / num4);
            for (int index = 0; index < num4; ++index)
                removalCollection.Add(new ExplosionRay()
                {
                    Direction = HelperFunctions.findPointFromAngleAndDistance(Vector2.Zero, num5 * (float)index, 1f),
                    Damage = damageAmount / (float)num4
                });
            List<ShipModule> list;// = new List<ShipModule>();
            List<ModuleSlot> list1;// = new List<ModuleSlot>();
            foreach (GameplayObject gameplayObject1 in this.GetNearby(source))
            {
                //try
                {
                    if (gameplayObject1 != null)
                    {
                        if (!(gameplayObject1 is Projectile))
                        {
                            Vector2 vector2_1 = Vector2.Zero;
                            if (gameplayObject1.Active)
                            {
                                if (gameplayObject1 != source)
                                {
                                    if (Vector2.Distance(gameplayObject1.Center, source.Center) <= damageRadius + gameplayObject1.Radius)
                                    {
                                        Projectile projectile1 = source as Projectile;
                                        if (gameplayObject1 is Ship && projectile1 != null)
                                        {
                                            if (projectile1.Owner != null)
                                            {
                                                if ((gameplayObject1 as Ship).loyalty == projectile1.owner.loyalty)
                                                    continue;
                                            }
                                            if (projectile1.Planet != null)
                                            {
                                                if (projectile1.Planet.Owner == (gameplayObject1 as Ship).loyalty)
                                                    continue;
                                            }
                                        }
                                        if (gameplayObject1 is Ship && source != gameplayObject1)
                                        {
                                            if (source is Projectile && (gameplayObject1 as Ship).shield_max > 0.0)
                                            {
                                                list = new List<ShipModule>();
                                                foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                                {
                                                    if (moduleSlot.module.shield_power > 0.0 && moduleSlot.module.Active)
                                                        list.Add(moduleSlot.module);
                                                }
                                                float num2 = damageAmount;
                                                foreach (ShipModule shipModule in list)
                                                {
                                                    if (Vector2.Distance(ExplosionCenter, shipModule.Center) <= damageRadius + shipModule.shield_radius && shipModule.shield_power > 0.0)
                                                    {
                                                        num2 = damageAmount - shipModule.shield_power;
                                                        shipModule.Damage(source, damageAmount / 2f);
                                                        break;
                                                    }
                                                }
                                                if (num2 <= 0.0)
                                                    break;
                                                damageAmount = num2;
                                            }
                                            list1 = new List<ModuleSlot>();
                                            foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ExternalSlots)
                                            {
                                                if (moduleSlot.module.Active && Vector2.Distance(moduleSlot.module.Center, ExplosionCenter) <= damageRadius + moduleSlot.module.Radius)
                                                    list1.Add(moduleSlot);
                                            }
                                            if (list1.Count == 0)
                                                break;
                                            IOrderedEnumerable<ModuleSlot> orderedEnumerable = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)list1, (Func<ModuleSlot, float>)(moduleslot => Vector2.Distance(ExplosionCenter, moduleslot.module.Center)));
                                            int num3 = 0;
                                            while (num3 < damageRadius)
                                            {
                                                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                                                {
                                                    if (explosionRay.Damage > 0.0)
                                                    {
                                                        foreach (ModuleSlot moduleSlot in (IEnumerable<ModuleSlot>)orderedEnumerable)
                                                        {
                                                            if (moduleSlot.module.Active && moduleSlot.module.Health > 0.0)
                                                            {
                                                                GameplayObject gameplayObject2 = (GameplayObject)moduleSlot.module;
                                                                Vector2 vector2_2 = ExplosionCenter + explosionRay.Direction * (float)num3;
                                                                Vector2 vector2_3 = gameplayObject2.Center - vector2_2;
                                                                if (Vector2.Distance(vector2_2, gameplayObject2.Center) <= 8.0)
                                                                {
                                                                    Projectile projectile2 = gameplayObject1 as Projectile;
                                                                    if (gameplayObject2 != source && projectile2 == null)
                                                                    {
                                                                        if (source is Ship || source is ShipModule && (source as ShipModule).GetParent() != gameplayObject1)
                                                                            vector2_1 += 3f * explosionRay.Damage * explosionRay.Direction;
                                                                        if ((gameplayObject1 as Ship).yRotation > 0.0 && !(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation += explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((gameplayObject1 as Ship).yRotation > (gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = (gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                        else if (!(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation -= explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((gameplayObject1 as Ship).yRotation < -(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = -(gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                    }
                                                                    if (explosionRay.Damage > 0.0)
                                                                    {
                                                                        float health = (gameplayObject2 as ShipModule).Health;
                                                                        (gameplayObject2 as ShipModule).Damage(source, explosionRay.Damage);
                                                                        explosionRay.Damage -= health;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                num3 += 8;
                                            }
                                            if (vector2_1.Length() > 200.0)
                                                vector2_1 = Vector2.Normalize(vector2_1) * 200f;
                                            if (!float.IsNaN(vector2_1.X))
                                                gameplayObject1.Velocity += vector2_1;
                                        }
                                        else
                                        {
                                            if (gameplayObject1 is Ship && source == gameplayObject1)
                                                break;
                                            float num2 = (gameplayObject1.Center - position).LengthSquared();
                                            if (num2 > 0.0)
                                            {
                                                if (num2 <= num1)
                                                {
                                                    float num3 = (float)Math.Sqrt((double)num2);
                                                    float damageAmount1 = damageAmount * (damageRadius - num3) / damageRadius;
                                                    if (damageAmount1 > 0.0)
                                                        gameplayObject1.Damage(source, damageAmount1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //catch
                //{
                //}
            }
        }

        private void AdjustVelocities(GameplayObject actor1, GameplayObject actor2)
        {
            if (actor1.Mass <= 0.0 || (double)actor2.Mass <= 0.0)
                return;
            Vector2 vector2_1 = actor2.Center - actor1.Center;
            if ((double)vector2_1.LengthSquared() <= 0.0)
                return;
            vector2_1.Normalize();
            Vector2 vector2_2 = new Vector2(-vector2_1.Y, vector2_1.X);
            float num1 = Vector2.Dot(actor1.Velocity, vector2_1);
            float num2 = Vector2.Dot(actor1.Velocity, vector2_2);
            float num3 = Vector2.Dot(actor2.Velocity, vector2_1);
            float num4 = Vector2.Dot(actor2.Velocity, vector2_2);
            float f1 = (float)(((double)num1 * ((double)actor1.Mass - (double)actor2.Mass) + 2.0 * (double)actor2.Mass * (double)num3) / ((double)actor1.Mass + (double)actor2.Mass));
            float f2 = (float)(((double)num3 * ((double)actor2.Mass - (double)actor1.Mass) + 2.0 * (double)actor1.Mass * (double)num1) / ((double)actor1.Mass + (double)actor2.Mass));
            if (float.IsNaN(f1) || float.IsNaN(f2))
                return;
            actor1.Velocity = f1 * vector2_1 + num2 * vector2_2;
            actor2.Velocity = f2 * vector2_1 + num4 * vector2_2;
        }

        private struct CollisionResult
        {
            public float Distance;
            public Vector2 Normal;
            public GameplayObject GameplayObject;

            public static int Compare(SpatialManager.CollisionResult a, SpatialManager.CollisionResult b)
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
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.CollidableObjects != null)
                        this.CollidableObjects.Dispose();
                    if (this.CollidableProjectiles != null)
                        this.CollidableProjectiles.Dispose();
                    if (this.Asteroids != null)
                        this.Asteroids.Dispose();
                    if (this.BeamList != null)
                        this.BeamList.Dispose();

                }
                this.CollidableObjects = null;
                this.CollidableProjectiles = null;
                this.Asteroids = null;
                this.BeamList = null;
                this.disposed = true;
            }
        }
    }
}
