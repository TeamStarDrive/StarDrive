using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    public sealed class ShipSpatialManager 
    {
        private int Cols;
        private int Rows;
        private Vector2 UpperLeftBound;
        private Map<int, Array<Ship>> Buckets;
        private int SceneWidth;
        private int CellSize;
        public Array<Ship> CollidableObjects = new Array<Ship>();
        private float BucketUpdateTimer;

        private void AddBucket(Vector2 vector, float width, Array<int> buckettoaddto)
        {
            int cellPosition = (int)(Math.Floor(vector.X / CellSize) + Math.Floor(vector.Y / CellSize) * width);
            if (!buckettoaddto.Contains(cellPosition)) buckettoaddto.Add(cellPosition);     
        }

        internal void ClearBuckets()
        {
            for (int i = 0; i < Cols * Rows; i++)
            {
                Buckets[i].Clear();
            }
        }

        public void Destroy()
        {
            Buckets = null;
        }

        private Array<int> GetIdForObj(GameplayObject obj)
        {
            Array<int> bucketsObjIsIn = new Array<int>();
            Vector2 Center = obj.Center - UpperLeftBound;
            Vector2 min = new Vector2(Center.X - 500000f, Center.Y - 500000f);
            Vector2 max = new Vector2(Center.X + 5000000f, Center.Y + 500000f);
            float width = SceneWidth / CellSize;
            AddBucket(min, width, bucketsObjIsIn);
            Vector2 m1 = new Vector2(max.X, min.Y);
            AddBucket(m1, width, bucketsObjIsIn);
            Vector2 m2 = new Vector2(max.X, max.Y);
            AddBucket(m2, width, bucketsObjIsIn);
            Vector2 m3 = new Vector2(min.X, max.Y);
            AddBucket(m3, width, bucketsObjIsIn);
            return bucketsObjIsIn;
        }

        public Array<Ship> GetNearby(Ship obj)
        {
            Array<Ship> objects = new Array<Ship>();
            foreach (int item in GetIdForObj(obj))
            {
                if (!Buckets.ContainsKey(item))
                {
                    continue;
                }
                objects.AddRange(Buckets[item]);
            }
            return objects;
        }

        internal void RegisterObject(Ship obj)
        {
            foreach (int item in GetIdForObj(obj))
            {
                if (!Buckets.ContainsKey(item))
                {
                    Buckets[1].Add(obj);
                }
                else
                {
                    Buckets[item].Add(obj);
                }
            }
        }

        public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 Pos)
        {
            UpperLeftBound.X = Pos.X - sceneWidth / 2;
            UpperLeftBound.Y = Pos.Y - sceneHeight / 2;
            Cols = sceneWidth / cellSize;
            Rows = sceneHeight / cellSize;
            Buckets = new Map<int, Array<Ship>>(Cols * Rows);
            for (int i = 0; i < Cols * Rows; i++)
            {
                Buckets.Add(i, new Array<Ship>());
            }
            SceneWidth = sceneWidth;
            CellSize = cellSize;
        }

        public void Update(float elapsedTime)
        {
            BucketUpdateTimer -= elapsedTime;
            if (BucketUpdateTimer <= 0f)
            {
                ClearBuckets();

                for (int index = 0; index < CollidableObjects.Count; index++)
                {
                    Ship obj = CollidableObjects[index];
                    if (!obj.Active) CollidableObjects.Remove(obj);
                    else
                        RegisterObject(obj);
                }
                BucketUpdateTimer = 0.25f;
            }
        }


    }
}