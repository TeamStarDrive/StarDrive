using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ShipGroup: IDisposable
	{
		public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();

		public float ProjectedFacing;
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public ShipGroup()
		{
		}

		private Vector2 findPointFromAngleAndDistanceUsingRadians(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = MathHelper.ToDegrees(angle);
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		public virtual void MoveTo(Vector2 Position, float facing, Vector2 fVec)
		{
			Ships[0].Center.RadiansToTarget(Position);
			Vector2 MovePosition = Position;
			this.Ships[0].GetAI().GoTo(MovePosition, fVec);
			for (int i = 1; i < this.Ships.Count; i++)
			{
				if (i % 2 != 1)
				{
					MovePosition = this.findPointFromAngleAndDistanceUsingRadians(Position, facing - 1.57079637f, (float)(i * 500));
					this.Ships[i].GetAI().GoTo(MovePosition, fVec);
				}
				else
				{
					MovePosition = this.findPointFromAngleAndDistanceUsingRadians(Position, facing + 1.57079637f, (float)(i * 500));
					this.Ships[i].GetAI().GoTo(MovePosition, fVec);
				}
			}
		}

		public virtual void ProjectPos(Vector2 position, float facing, List<Fleet.Squad> flank)
		{
		}

		public virtual void ProjectPos(Vector2 position, float facing, Vector2 fVec)
		{
		    ProjectedFacing = facing;
			Ships[0].projectedPosition = position;
			for (int i = 1; i < Ships.Count; i++)
			{
                float facingRandomizer = (i % 2 == 0) ? -1.57079637f : +1.57079637f;
				Ships[i].projectedPosition = findPointFromAngleAndDistanceUsingRadians(position, facing + facingRandomizer, i * 500);
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipGroup() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Ships != null)
                        this.Ships.Dispose();

                }
                this.Ships = null;
                this.disposed = true;
            }
        }
	}
}