// Type: Ship_Game.Gameplay.SpatialManager
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.Gameplay
{
    public class SpatialManager
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
        private Dictionary<int, List<GameplayObject>> Buckets;
        public int SceneWidth;
        public int SceneHeight;
        public int CellSize;
        public bool FineDetail;

        public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 Pos)
        {
            this.UpperLeftBound.X = Pos.X - (float)(sceneWidth / 2);
            this.UpperLeftBound.Y = Pos.Y - (float)(sceneHeight / 2);
            this.Cols = sceneWidth / cellSize;
            this.Rows = sceneHeight / cellSize;
            this.Buckets = new Dictionary<int, List<GameplayObject>>(this.Cols * this.Rows);
            for (int key = 0; key < this.Cols * this.Rows; ++key)
                this.Buckets.Add(key, new List<GameplayObject>());
            this.SceneWidth = sceneWidth;
            this.SceneHeight = sceneHeight;
            this.CellSize = cellSize;
        }

        public void Update(float elapsedTime, SolarSystem system)
        {
            this.BeamList.ApplyPendingRemovals();
            this.bucketUpdateTimer -= elapsedTime;
            if ((double)this.bucketUpdateTimer <= 0.0)
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
                for (int index = 0; index < this.CollidableObjects.Count; ++index)
                {
                    GameplayObject gameplayObject = this.CollidableObjects[index];
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
                    if (!(gameplayObject.GetSystem() != null & system == null) && gameplayObject != null && (!(gameplayObject is Ship) || system != null || (gameplayObject as Ship).GetAI().BadGuysNear))
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
            if ((double)this.bucketUpdateTimer <= 0.0)
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
            List<GameplayObject> list = new List<GameplayObject>();
            try
            {
                foreach (int key in this.GetIdForObj(obj))
                {
                    if (!this.Buckets.ContainsKey(key))
                        return (List<GameplayObject>)this.CollidableObjects;
                    list.AddRange((IEnumerable<GameplayObject>)this.Buckets[key]);
                }
            }
            catch
            {
            }
            return list;
        }

        internal List<GameplayObject> GetNearby(Vector2 Position)
        {
            List<GameplayObject> list = new List<GameplayObject>();
            try
            {
                foreach (int key in this.GetIdForPos(Position))
                {
                    if (this.Buckets.ContainsKey(key))
                        list.AddRange((IEnumerable<GameplayObject>)this.Buckets[key]);
                }
            }
            catch
            {
            }
            return list;
        }

        internal void RegisterObject(GameplayObject obj)
        {
            try
            {
                foreach (int key in this.GetIdForObj(obj))
                {
                    if (this.Buckets.ContainsKey(key))
                        this.Buckets[key].Add(obj);
                    else
                        this.Buckets[1].Add(obj);
                }
            }
            catch
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
            this.Buckets = (Dictionary<int, List<GameplayObject>>)null;
        }

        internal void ClearBuckets()
        {
            for (int index = 0; index < this.Cols * this.Rows; ++index)
                this.Buckets[index].Clear();
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
            if ((double)num1 > (double)beam.range + 10.0)
                return;
            List<Vector2> list1 = new List<Vector2>();
            beam.ActualHitDestination = beam.Destination;
            for (int index = 0; (double)(index * 75) < (double)num1; ++index)
                list1.Add(beam.Source + vector2_1 * (float)index * 75f);
            Ship ship1 = (Ship)null;
            Vector2 vector2_2 = Vector2.Zero;
            GameplayObject gameplayObject1 = (GameplayObject)null;
            //How repair beams repair modules
            if (beam.GetTarget() != null)
            {
                if (beam.GetTarget() is Ship)
                {
                    Ship ship2 = beam.GetTarget() as Ship;
                    ship2.MoveModulesTimer = 2f;
                    Vector2 vector2_3 = beam.GetTarget().Center;
                    beam.ActualHitDestination = beam.GetTarget().Center;
                    if ((double)beam.damageAmount >= 0.0)
                        return;
                    using (LinkedList<ModuleSlot>.Enumerator enumerator = ship2.ModuleSlotList.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ModuleSlot current = enumerator.Current;
                            if ((double)current.module.Health < (double)current.module.HealthMax)
                            {
                                ShipModule module = current.module;
                                double num2 = (double)module.Health - (double)beam.damageAmount;
                                module.Health = (float)num2;
                                if ((double)current.module.Health < (double)current.module.HealthMax)
                                    break;
                                current.module.Health = current.module.HealthMax;
                                break;
                            }
                        }
                        return;
                    }
                }
                else if (beam.GetTarget() is ShipModule)
                    gameplayObject1 = (GameplayObject)(beam.GetTarget() as ShipModule).GetParent();
                else if (beam.GetTarget() is Asteroid)
                    gameplayObject1 = beam.GetTarget();
            }
            else if (beam.Owner != null)
                gameplayObject1 = (GameplayObject)beam.owner;
            List<GameplayObject> nearby = this.GetNearby(gameplayObject1);
            List<GameplayObject> AlliedShips = new List<GameplayObject>();
            foreach (Vector2 vector2_3 in list1)
            {
                foreach (GameplayObject gameplayObject2 in nearby)
                {
                    if (gameplayObject2 is Ship)
                    {
                        if ((gameplayObject2 as Ship).loyalty == beam.owner.loyalty)
                            AlliedShips.Add(gameplayObject2);
                        else if (gameplayObject2 != beam.owner || beam.weapon.HitsFriendlies)
                        {
                            ++GlobalStats.BeamTests;
                            if ((double)Vector2.Distance(gameplayObject2.Center, vector2_3) < (double)gameplayObject2.Radius)
                            {
                                ship1 = gameplayObject2 as Ship;
                                ship1.MoveModulesTimer = 2f;
                                vector2_2 = vector2_3;
                                break;
                            }
                        }
                    }
                }
                foreach (GameplayObject gameplayObject2 in AlliedShips)
                    nearby.Remove(gameplayObject2);
                AlliedShips.Clear();
                if (ship1 != null)
                    break;
            }
            if (ship1 != null)
            {
                list1.Clear();
                for (int index = 0; (double)(index * 8) < (double)ship1.Radius; ++index)
                    list1.Add(vector2_2 + vector2_1 * (float)index * 8f);
                bool flag = false;
                if (beam.hitLast != null && beam.hitLast.Active)
                {
                    List<ShipModule> list3 = new List<ShipModule>();
                    list3.Add(beam.hitLast);
                    foreach (ShipModule shipModule in beam.hitLast.LinkedModulesList)
                        list3.Add(shipModule);
                    int num2 = -48;
                    while (num2 < 48)
                    {
                        int num3 = -48;
                        while (num3 < 48)
                        {
                            if (beam.hitLast.GetParent().GetMD().ContainsKey(beam.hitLast.XMLPosition + new Vector2((float)num2, (float)num3)))
                                list3.Add(beam.hitLast.GetParent().GetMD()[beam.hitLast.XMLPosition + new Vector2((float)num2, (float)num3)].module);
                            num3 += 16;
                        }
                        num2 += 16;
                    }
                    foreach (ShipModule shipModule in list3)
                    {
                        if (shipModule != null && shipModule.isExternal)
                        {
                            float num3 = 100000f;
                            foreach (Vector2 vector2_3 in list1)
                            {
                                ++GlobalStats.BeamTests;
                                float num4 = Vector2.Distance(vector2_3, shipModule.Center);
                                if ((double)num4 <= (beam.IgnoresShields ? 16.0 : (double)shipModule.Radius + 8.0))
                                {
                                    ++GlobalStats.BeamTests;
                                    this.collisionResults.Add(new SpatialManager.CollisionResult()
                                    {
                                        Distance = (beam.IgnoresShields ? 16f : shipModule.Radius + 8f),
                                        Normal = Vector2.Normalize(Vector2.Zero),
                                        GameplayObject = (GameplayObject)shipModule
                                    });
                                    beam.ActualHitDestination = vector2_3;
                                    flag = true;
                                    beam.hitLast = shipModule;
                                    break;
                                }
                                else if ((double)num4 <= (double)num3)
                                    num3 = num4;
                                else
                                    break;
                            }
                            if (flag)
                                break;
                        }
                    }
                    if (!flag)
                        beam.hitLast = (ShipModule)null;
                    list3.Clear();
                }
                if (!flag)
                {
                    foreach (Vector2 vector2_3 in list1)
                    {
                        if (!beam.IgnoresShields)
                        {
                            for (int index = 0; index < ship1.GetShields().Count; ++index)
                            {
                                ++GlobalStats.BeamTests;
                                ShipModule shipModule = ship1.GetShields()[index];
                                if (shipModule != null && (shipModule.Active || (double)beam.damageAmount <= 0.0))
                                {
                                    if ((double)shipModule.shield_power <= 0.0)
                                        beam.hitLast = (ShipModule)null;
                                    else if ((double)Vector2.Distance(vector2_3, shipModule.Center) <= (double)shipModule.Radius + 4.0)
                                    {
                                        ++GlobalStats.BeamTests;
                                        this.collisionResults.Add(new SpatialManager.CollisionResult()
                                        {
                                            Distance = shipModule.Radius + 8f,
                                            Normal = Vector2.Normalize(Vector2.Zero),
                                            GameplayObject = (GameplayObject)shipModule
                                        });
                                        beam.ActualHitDestination = vector2_3;
                                        flag = true;
                                        beam.hitLast = shipModule;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag)
                        {
                            for (int index = 0; index < ship1.ExternalSlots.Count; ++index)
                            {
                                ++GlobalStats.BeamTests;
                                ModuleSlot moduleSlot = ship1.ExternalSlots.ElementAt(index);
                                if (moduleSlot != null && (moduleSlot.module.Active || (double)beam.damageAmount <= 0.0) && (double)Vector2.Distance(vector2_3, moduleSlot.module.Center) <= (beam.IgnoresShields ? 12.0 : (double)moduleSlot.module.Radius + 4.0))
                                {
                                    ++GlobalStats.BeamTests;
                                    this.collisionResults.Add(new SpatialManager.CollisionResult()
                                    {
                                        Distance = (beam.IgnoresShields ? 12f : moduleSlot.module.Radius + 8f),
                                        Normal = Vector2.Normalize(Vector2.Zero),
                                        GameplayObject = (GameplayObject)moduleSlot.module
                                    });
                                    beam.ActualHitDestination = vector2_3;
                                    flag = true;
                                    beam.hitLast = moduleSlot.module;
                                    break;
                                }
                            }
                            if (flag)
                                break;
                        }
                        else
                            break;
                    }
                }
            }
            if (this.collisionResults.Count <= 0)
                return;
            this.collisionResults.Sort(new Comparison<SpatialManager.CollisionResult>(SpatialManager.CollisionResult.Compare));
            foreach (SpatialManager.CollisionResult collisionResult in this.collisionResults)
            {
                if (beam.Touch(collisionResult.GameplayObject))
                    beam.CollidedThisFrame = collisionResult.GameplayObject.CollidedThisFrame = true;
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
                            if ((double)Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < (double)(gameplayObject1 as Projectile).weapon.ProjectileRadius + (double)(gameplayObject as Ship).GetSO().WorldBoundingSphere.Radius + 100.0)
                            {
                                (gameplayObject as Ship).MoveModulesTimer = 2f;
                                float num1 = (gameplayObject1 as Projectile).Velocity.Length();
                                if ((double)num1 / 60.0 > 10.0)
                                {
                                    bool flag = false;
                                    Vector2 vector2 = Vector2.Normalize(gameplayObject1.Velocity);
                                    for (int index = 0; index < (gameplayObject as Ship).GetShields().Count; ++index)
                                    {
                                        ++GlobalStats.DistanceCheckTotal;
                                        ShipModule shipModule = (gameplayObject as Ship).GetShields()[index];
                                        if (shipModule != null)
                                        {
                                            if ((double)shipModule.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields)
                                            {
                                                if ((double)shipModule.shield_power > 0.0 && shipModule.Active)
                                                {
                                                    int num2 = 8;
                                                    while ((double)num2 < (double)num1 / 60.0)
                                                    {
                                                        ++GlobalStats.Comparisons;
                                                        if ((double)Vector2.Distance(gameplayObject1.Center + vector2 * (float)num2, shipModule.Center) <= 12.0 + ((double)shipModule.shield_power > 0.0 ? (double)shipModule.shield_radius : 0.0))
                                                        {
                                                            gameplayObject1.Center = gameplayObject1.Center + vector2 * (float)num2;
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
                                        if (moduleSlot != null && moduleSlot.module != null && ((double)moduleSlot.module.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && moduleSlot.module.Active)
                                        {
                                            int num2 = 8;
                                            while ((double)num2 < (double)num1 / 60.0)
                                            {
                                                ++GlobalStats.Comparisons;
                                                if ((double)Vector2.Distance(gameplayObject1.Center + vector2 * (float)num2, moduleSlot.module.Center) <= 8.0 + (double)(gameplayObject1 as Projectile).weapon.ProjectileRadius + ((double)moduleSlot.module.shield_power > 0.0 ? (double)moduleSlot.module.shield_radius : 0.0))
                                                {
                                                    gameplayObject1.Center = gameplayObject1.Center + vector2 * (float)num2;
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
                                        if (shipModule != null && ((double)shipModule.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && shipModule.Active && (double)Vector2.Distance(gameplayObject1.Center, shipModule.Center) <= 10.0 + ((double)shipModule.shield_power > 0.0 ? (double)shipModule.shield_radius : 0.0))
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
                                            if (moduleSlot != null && moduleSlot.module != null && ((double)moduleSlot.module.shield_power <= 0.0 || !(gameplayObject1 as Projectile).IgnoresShields) && moduleSlot.module.Active && (double)Vector2.Distance(gameplayObject1.Center, moduleSlot.module.Center) <= 10.0 + ((double)moduleSlot.module.shield_power > 0.0 ? (double)moduleSlot.module.shield_radius : 0.0))
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
                            if ((double)Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (double)(gameplayObject as Projectile).weapon.ProjectileRadius + (double)(gameplayObject1 as Projectile).weapon.ProjectileRadius)
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
                            if ((double)Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) <= (double)(gameplayObject as Projectile).weapon.ProjectileRadius + (double)gameplayObject1.Radius)
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
                        else if (gameplayObject1 is Ship && (gameplayObject as Projectile).loyalty != (gameplayObject1 as Ship).loyalty && (double)Vector2.Distance(gameplayObject.Center, gameplayObject1.Center) < (double)gameplayObject1.Radius + (double)gameplayObject.Radius + (double)(gameplayObject as Projectile).speed / 60.0)
                        {
                            (gameplayObject1 as Ship).MoveModulesTimer = 2f;
                            if ((double)(gameplayObject as Projectile).speed / 60.0 > 16.0)
                            {
                                Vector2 vector2 = Vector2.Normalize(gameplayObject.Velocity);
                                for (int index = 0; index < (gameplayObject1 as Ship).ExternalSlots.Count; ++index)
                                {
                                    ++GlobalStats.DistanceCheckTotal;
                                    ModuleSlot moduleSlot = (gameplayObject1 as Ship).ExternalSlots.ElementAt(index);
                                    if (moduleSlot != null && moduleSlot.module != null && ((double)moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active)
                                    {
                                        bool flag = false;
                                        int num1 = 8;
                                        while ((double)num1 < (double)(gameplayObject as Projectile).speed * 2.0 / 60.0)
                                        {
                                            ++GlobalStats.Comparisons;
                                            double num2 = (double)Vector2.Distance(gameplayObject.Center + vector2 * (float)num1, moduleSlot.module.Center);
                                            if ((double)Vector2.Distance(gameplayObject.Center + vector2 * (float)num1, moduleSlot.module.Center) <= 8.0 + ((double)moduleSlot.module.shield_power > 0.0 ? (double)moduleSlot.module.shield_radius : 0.0))
                                            {
                                                gameplayObject.Center = gameplayObject.Center + vector2 * (float)num1;
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
                                    if (moduleSlot != null && moduleSlot.module != null && ((double)moduleSlot.module.shield_power <= 0.0 || !(gameplayObject as Projectile).IgnoresShields) && moduleSlot.module.Active && (double)Vector2.Distance(gameplayObject.Center, moduleSlot.module.Center) <= 10.0 + ((double)moduleSlot.module.shield_power > 0.0 ? (double)moduleSlot.module.shield_radius : 0.0))
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
                        double num1 = (double)gameplayObject1.Radius;
                        double num2 = (double)gameplayObject.Radius;
                        Vector2 vector2 = gameplayObject1.Center - gameplayObject.Center;
                        float num3 = vector2.Length();
                        if ((double)num3 > 0.0)
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

        public void Explode(GameplayObject source, float damageAmount, Vector2 position, float damageRadius)
        {
            if ((double)damageRadius <= 0.0)
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
                                    if ((double)Vector2.Distance(gameplayObject1.Center, source.Center) <= (double)damageRadius + (double)gameplayObject1.Radius)
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
                                                List<ShipModule> list = new List<ShipModule>();
                                                foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                                {
                                                    if ((double)moduleSlot.module.shield_power > 0.0 && moduleSlot.module.Active)
                                                        list.Add(moduleSlot.module);
                                                }
                                                float num2 = damageAmount;
                                                foreach (ShipModule shipModule in list)
                                                {
                                                    if ((double)Vector2.Distance(ExplosionCenter, shipModule.Center) <= (double)damageRadius + (double)shipModule.shield_radius && (double)shipModule.shield_power > 0.0)
                                                    {
                                                        num2 = damageAmount - shipModule.shield_power;
                                                        shipModule.Damage(source, damageAmount / 2f);
                                                        break;
                                                    }
                                                }
                                                if ((double)num2 <= 0.0)
                                                    break;
                                                damageAmount = num2;
                                            }
                                            List<ModuleSlot> list1 = new List<ModuleSlot>();
                                            foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                            {
                                                if (moduleSlot.module.Active && (double)Vector2.Distance(moduleSlot.module.Center, ExplosionCenter) <= (double)damageRadius + (double)moduleSlot.module.Radius)
                                                    list1.Add(moduleSlot);
                                            }
                                            if (list1.Count == 0)
                                                break;
                                            IOrderedEnumerable<ModuleSlot> orderedEnumerable = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)list1, (Func<ModuleSlot, float>)(moduleslot => Vector2.Distance(ExplosionCenter, moduleslot.module.Center)));
                                            int num3 = 0;
                                            while ((double)num3 < (double)damageRadius)
                                            {
                                                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                                                {
                                                    if ((double)explosionRay.Damage > 0.0)
                                                    {
                                                        foreach (ModuleSlot moduleSlot in (IEnumerable<ModuleSlot>)orderedEnumerable)
                                                        {
                                                            if (moduleSlot.module.Active && (double)moduleSlot.module.Health > 0.0)
                                                            {
                                                                GameplayObject gameplayObject2 = (GameplayObject)moduleSlot.module;
                                                                Vector2 vector2_2 = ExplosionCenter + explosionRay.Direction * (float)num3;
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
                                                                            if ((double)(gameplayObject1 as Ship).yRotation > (double)(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = (gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                        else if (!(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation -= explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((double)(gameplayObject1 as Ship).yRotation < -(double)(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = -(gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                    }
                                                                    if ((double)explosionRay.Damage > 0.0)
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
                                            if ((double)vector2_1.Length() > 200.0)
                                                vector2_1 = Vector2.Normalize(vector2_1) * 200f;
                                            if (!float.IsNaN(vector2_1.X))
                                                gameplayObject1.Velocity += vector2_1;
                                        }
                                        else
                                        {
                                            if (gameplayObject1 is Ship && source == gameplayObject1)
                                                break;
                                            float num2 = (gameplayObject1.Center - position).LengthSquared();
                                            if ((double)num2 > 0.0)
                                            {
                                                if ((double)num2 <= (double)num1)
                                                {
                                                    float num3 = (float)Math.Sqrt((double)num2);
                                                    float damageAmount1 = damageAmount * (damageRadius - num3) / damageRadius;
                                                    if ((double)damageAmount1 > 0.0)
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
            foreach (GameplayObject gameplayObject in this.GetNearby(source).Where(gameplayObject => gameplayObject != null && gameplayObject is Ship && gameplayObject.Active && !(gameplayObject as Ship).dying).OrderBy(gameplayObject => Vector2.Distance(source.Center, gameplayObject.Center)))
            {
                //Check if valid target
                //added by gremlin check that projectile owner is not null
                if (source.Owner == null || source.Owner != null && source.Owner.loyalty != (gameplayObject as Ship).loyalty)
                {
                    float DamageTracker = 0;
                    IEnumerable<ModuleSlot> modules = (gameplayObject as Ship).ModuleSlotList.Where(moduleSlot => moduleSlot.module.Health > 0.0 && (moduleSlot.module.shield_power > 0.0 && !source.IgnoresShields) ? Vector2.Distance(source.Center, moduleSlot.module.Center) <= damageRadius + moduleSlot.module.shield_radius : Vector2.Distance(source.Center, moduleSlot.module.Center) <= damageRadius + 10f).OrderBy(moduleSlot => (moduleSlot.module.shield_power > 0.0 && !source.IgnoresShields) ? Vector2.Distance(source.Center, moduleSlot.module.Center) - moduleSlot.module.shield_radius : Vector2.Distance(source.Center, moduleSlot.module.Center));
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
            if ((double)damageRadius <= 0.0 || HitModule.GetParent().dying || !HitModule.GetParent().Active)
                return;
            BatchRemovalCollection<ExplosionRay> removalCollection = new BatchRemovalCollection<ExplosionRay>();
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
            while ((double)num3 < (double)damageRadius)
            {
                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                {
                    if ((double)explosionRay.Damage > 0.0)
                    {
                        foreach (ShipModule shipModule in list)
                        {
                            if (shipModule.Active && (double)shipModule.Health > 0.0)
                            {
                                Vector2 vector2_1 = HitModule.Center + explosionRay.Direction * (float)num3;
                                Vector2 vector2_2 = shipModule.Center - vector2_1;
                                if ((double)Vector2.Distance(vector2_1, shipModule.Center) <= 8.0 && (double)explosionRay.Damage > 0.0)
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
            if ((double)damageRadius <= 0.0)
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
                                    if ((double)Vector2.Distance(gameplayObject1.Center, source.Center) <= (double)damageRadius + (double)gameplayObject1.Radius)
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
                                                List<ShipModule> list = new List<ShipModule>();
                                                foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ModuleSlotList)
                                                {
                                                    if ((double)moduleSlot.module.shield_power > 0.0 && moduleSlot.module.Active)
                                                        list.Add(moduleSlot.module);
                                                }
                                                float num2 = damageAmount;
                                                foreach (ShipModule shipModule in list)
                                                {
                                                    if ((double)Vector2.Distance(ExplosionCenter, shipModule.Center) <= (double)damageRadius + (double)shipModule.shield_radius && (double)shipModule.shield_power > 0.0)
                                                    {
                                                        num2 = damageAmount - shipModule.shield_power;
                                                        shipModule.Damage(source, damageAmount / 2f);
                                                        break;
                                                    }
                                                }
                                                if ((double)num2 <= 0.0)
                                                    break;
                                                damageAmount = num2;
                                            }
                                            List<ModuleSlot> list1 = new List<ModuleSlot>();
                                            foreach (ModuleSlot moduleSlot in (gameplayObject1 as Ship).ExternalSlots)
                                            {
                                                if (moduleSlot.module.Active && (double)Vector2.Distance(moduleSlot.module.Center, ExplosionCenter) <= (double)damageRadius + (double)moduleSlot.module.Radius)
                                                    list1.Add(moduleSlot);
                                            }
                                            if (list1.Count == 0)
                                                break;
                                            IOrderedEnumerable<ModuleSlot> orderedEnumerable = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)list1, (Func<ModuleSlot, float>)(moduleslot => Vector2.Distance(ExplosionCenter, moduleslot.module.Center)));
                                            int num3 = 0;
                                            while ((double)num3 < (double)damageRadius)
                                            {
                                                foreach (ExplosionRay explosionRay in (List<ExplosionRay>)removalCollection)
                                                {
                                                    if ((double)explosionRay.Damage > 0.0)
                                                    {
                                                        foreach (ModuleSlot moduleSlot in (IEnumerable<ModuleSlot>)orderedEnumerable)
                                                        {
                                                            if (moduleSlot.module.Active && (double)moduleSlot.module.Health > 0.0)
                                                            {
                                                                GameplayObject gameplayObject2 = (GameplayObject)moduleSlot.module;
                                                                Vector2 vector2_2 = ExplosionCenter + explosionRay.Direction * (float)num3;
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
                                                                            if ((double)(gameplayObject1 as Ship).yRotation > (double)(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = (gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                        else if (!(gameplayObject1 as Ship).dying)
                                                                        {
                                                                            (gameplayObject1 as Ship).yRotation -= explosionRay.Damage / (float)Math.Pow((double)(gameplayObject1 as Ship).Mass, 1.3);
                                                                            if ((double)(gameplayObject1 as Ship).yRotation < -(double)(gameplayObject1 as Ship).maxBank)
                                                                                (gameplayObject1 as Ship).yRotation = -(gameplayObject1 as Ship).maxBank;
                                                                        }
                                                                    }
                                                                    if ((double)explosionRay.Damage > 0.0)
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
                                            if ((double)vector2_1.Length() > 200.0)
                                                vector2_1 = Vector2.Normalize(vector2_1) * 200f;
                                            if (!float.IsNaN(vector2_1.X))
                                                gameplayObject1.Velocity += vector2_1;
                                        }
                                        else
                                        {
                                            if (gameplayObject1 is Ship && source == gameplayObject1)
                                                break;
                                            float num2 = (gameplayObject1.Center - position).LengthSquared();
                                            if ((double)num2 > 0.0)
                                            {
                                                if ((double)num2 <= (double)num1)
                                                {
                                                    float num3 = (float)Math.Sqrt((double)num2);
                                                    float damageAmount1 = damageAmount * (damageRadius - num3) / damageRadius;
                                                    if ((double)damageAmount1 > 0.0)
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

        private void AdjustVelocities(GameplayObject actor1, GameplayObject actor2)
        {
            if ((double)actor1.Mass <= 0.0 || (double)actor2.Mass <= 0.0)
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
    }
}
