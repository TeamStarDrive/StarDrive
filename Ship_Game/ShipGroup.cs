using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ShipGroup
	{
		public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();

		public float ProjectedFacing;

		public ShipGroup()
		{
		}

		private float findAngleToTarget(Vector2 Center, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = Center.X;
			float centerY = Center.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
			}
			else if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			return angle_to_target;
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
			MathHelper.ToRadians(this.findAngleToTarget(this.Ships[0].Center, Position));
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

		public virtual void ProjectPos(Vector2 Position, float facing, List<Fleet.Squad> Flank)
		{
		}

		public virtual void ProjectPos(Vector2 Position, float facing, Vector2 fVec)
		{
			Vector2 MovePosition;
			this.ProjectedFacing = facing;
			MathHelper.ToRadians(this.findAngleToTarget(this.Ships[0].Center, Position));
			this.Ships[0].projectedPosition = Position;
			for (int i = 1; i < this.Ships.Count; i++)
			{
				if (i % 2 != 1)
				{
					MovePosition = this.findPointFromAngleAndDistanceUsingRadians(Position, facing - 1.57079637f, (float)(i * 500));
					this.Ships[i].projectedPosition = MovePosition;
				}
				else
				{
					MovePosition = this.findPointFromAngleAndDistanceUsingRadians(Position, facing + 1.57079637f, (float)(i * 500));
					this.Ships[i].projectedPosition = MovePosition;
				}
			}
		}
	}
}