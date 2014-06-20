using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{
	public class ShipSpatialManager
	{
		private const float speedDamageRatio = 0.5f;

		private int Cols;

		private int Rows;

		private Vector2 UpperLeftBound;

		private Dictionary<int, List<Ship>> Buckets;

		private int SceneWidth;

		private int SceneHeight;

		private int CellSize;

		public BatchRemovalCollection<Ship> CollidableObjects = new BatchRemovalCollection<Ship>();

		private float bucketUpdateTimer;

		public ShipSpatialManager()
		{
		}

		private void AddBucket(Vector2 vector, float width, List<int> buckettoaddto)
		{
			int cellPosition = (int)(Math.Floor((double)(vector.X / (float)this.CellSize)) + Math.Floor((double)(vector.Y / (float)this.CellSize)) * (double)width);
			if (!buckettoaddto.Contains(cellPosition))
			{
				buckettoaddto.Add(cellPosition);
			}
		}

		internal void ClearBuckets()
		{
			for (int i = 0; i < this.Cols * this.Rows; i++)
			{
				this.Buckets[i].Clear();
			}
		}

		public void Destroy()
		{
			this.Buckets = null;
		}

		private List<int> GetIdForObj(GameplayObject obj)
		{
			List<int> bucketsObjIsIn = new List<int>();
			Vector2 Center = obj.Center - this.UpperLeftBound;
			Vector2 min = new Vector2(Center.X - 500000f, Center.Y - 500000f);
			Vector2 max = new Vector2(Center.X + 5000000f, Center.Y + 500000f);
			float width = (float)(this.SceneWidth / this.CellSize);
			this.AddBucket(min, width, bucketsObjIsIn);
			Vector2 m1 = new Vector2(max.X, min.Y);
			this.AddBucket(m1, width, bucketsObjIsIn);
			Vector2 m2 = new Vector2(max.X, max.Y);
			this.AddBucket(m2, width, bucketsObjIsIn);
			Vector2 m3 = new Vector2(min.X, max.Y);
			this.AddBucket(m3, width, bucketsObjIsIn);
			return bucketsObjIsIn;
		}

		public List<Ship> GetNearby(Ship obj)
		{
			List<Ship> objects = new List<Ship>();
			foreach (int item in this.GetIdForObj(obj))
			{
				if (!this.Buckets.ContainsKey(item))
				{
					continue;
				}
				objects.AddRange(this.Buckets[item]);
			}
			return objects;
		}

		internal void RegisterObject(Ship obj)
		{
			foreach (int item in this.GetIdForObj(obj))
			{
				if (!this.Buckets.ContainsKey(item))
				{
					this.Buckets[1].Add(obj);
				}
				else
				{
					this.Buckets[item].Add(obj);
				}
			}
		}

		public void Setup(int sceneWidth, int sceneHeight, int cellSize, Vector2 Pos)
		{
			this.UpperLeftBound.X = Pos.X - (float)(sceneWidth / 2);
			this.UpperLeftBound.Y = Pos.Y - (float)(sceneHeight / 2);
			this.Cols = sceneWidth / cellSize;
			this.Rows = sceneHeight / cellSize;
			this.Buckets = new Dictionary<int, List<Ship>>(this.Cols * this.Rows);
			for (int i = 0; i < this.Cols * this.Rows; i++)
			{
				this.Buckets.Add(i, new List<Ship>());
			}
			this.SceneWidth = sceneWidth;
			this.SceneHeight = sceneHeight;
			this.CellSize = cellSize;
		}

		public void Update(float elapsedTime)
		{
			ShipSpatialManager shipSpatialManager = this;
			shipSpatialManager.bucketUpdateTimer = shipSpatialManager.bucketUpdateTimer - elapsedTime;
			if (this.bucketUpdateTimer <= 0f)
			{
				this.ClearBuckets();
				foreach (Ship obj in this.CollidableObjects)
				{
					if (!obj.Active)
					{
						this.CollidableObjects.QueuePendingRemoval(obj);
					}
					else
					{
						this.RegisterObject(obj);
					}
				}
				this.bucketUpdateTimer = 0.25f;
			}
			this.CollidableObjects.ApplyPendingRemovals();
		}
	}
}