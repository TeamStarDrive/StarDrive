using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game.Gameplay
{
	public class MissileAI
	{
		public Projectile Owner;

		private GameplayObject Target;

		private List<Ship> TargetList = new List<Ship>();

		public static UniverseScreen universeScreen;

		private float thinkTimer = 0.15f;

		private bool TargetSet;

		public MissileAI(Projectile owner)
		{
			this.Owner = owner;
			if (MissileAI.universeScreen != null)
			{
				List<GameplayObject> GPO = UniverseScreen.ShipSpatialManager.GetNearby(this.Owner);
				for (int i = 0; i < GPO.Count; i++)
				{
					if (GPO[i] is Ship)
					{
						Ship target = GPO[i] as Ship;
						if (target != null && target.loyalty != this.Owner.loyalty)
						{
							this.TargetList.Add(target);
						}
					}
				}
			}
		}

		public void ChooseTargetORIG()
		{
			this.Target = null;
			IOrderedEnumerable<Ship> sortedList = 
				from ship in this.TargetList
				orderby Vector2.Distance(this.Owner.Center, ship.Center)
				select ship;
			if (sortedList.Count<Ship>() > 0)
			{
				this.Target = sortedList.First<Ship>();
			}
		}
        //added by gremlin deveks ChooseTarget
        public void ChooseTarget()
        {
            if (this.Owner.owner != null)
            {
                bool valid = false;

                GameplayObject sourceTarget = this.Owner.owner.GetAI().Target;
                if (sourceTarget is Ship && sourceTarget != null)
                {
                    Ship shipTarget = sourceTarget as Ship;
                    valid = shipTarget.loyalty != this.Owner.loyalty;

                    if (sourceTarget.Active && valid)
                    {
                        this.SetTarget(sourceTarget); //use SetTarget function
                        return;
                    }
                }
            }
            this.Target = null;
            this.SetTarget(this.TargetList.OrderBy(ship => Vector2.Distance(this.Owner.Center, ship.Center)).FirstOrDefault<Ship>()); //use SetTarget function

        }
		public void ClearTargets()
		{
			this.Target = null;
			this.TargetList.Clear();
		}

		private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
		{
			Vector2 Vec2Target = new Vector2(0f, 0f)
			{
				X = -(OwnerPos.X - TargetPos.X),
				Y = OwnerPos.Y - TargetPos.Y
			};
			return Vec2Target;
		}

		private void MoveStraight(float elapsedTime)
		{
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 vector2 = new Vector2(-forward.Y, forward.X);
			Vector2 wantedForward = Vector2.Normalize(forward);
			wantedForward = Vector2.Normalize(forward);
			this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
			this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
		}

		private void MoveTowardsTarget(float elapsedTime)
		{
			if (this.Target == null)
			{
				return;
			}
			try
			{
				Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				Vector2 AimPosition = this.Target.Center;
				Vector2 LeftStick = this.findVectorToTarget(this.Owner.Center, AimPosition);
				LeftStick.Y = LeftStick.Y * -1f;
				Vector2 wantedForward = Vector2.Normalize(LeftStick);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.2f)
				{
					Projectile owner = this.Owner;
					owner.Rotation = owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.RotationRadsPerSecond);
				}
				wantedForward = Vector2.Normalize(forward);
				this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
				this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
			}
			catch
			{
				this.Target = null;
			}
		}

		public void SetTarget(GameplayObject target)
		{
			this.TargetSet = true;
			this.Target = target;
		}

		public void ThinkORIG(float elapsedTime)
		{
			MissileAI missileAI = this;
			missileAI.thinkTimer = missileAI.thinkTimer - elapsedTime;
			if (this.thinkTimer <= 0f && (this.Target == null || !this.Target.Active))
			{
				this.ChooseTarget();
				this.thinkTimer = 2f;
			}
			if (this.Target != null)
			{
				this.MoveTowardsTarget(elapsedTime);
				return;
			}
			this.MoveStraight(elapsedTime);
		}
        //added by gremlin Deveksmod Missilethink.
        public void Think(float elapsedTime)
        {
            MissileAI missileAI = this;
            missileAI.thinkTimer = missileAI.thinkTimer - elapsedTime;
            if (this.thinkTimer <= 0f) //check time interval
            {
                this.thinkTimer = 5 * elapsedTime;
                if (this.Target is Ship) //check if target is dying
                {
                    Ship ship = this.Target as Ship;
                    if (ship.dying)
                    {
                        TargetSet = false;
                    }
                }
                if (this.Target == null || !TargetSet || !this.Target.Active) //Check if new target is needed
                {
                    this.ChooseTarget();
                }
            }
            if (TargetSet)  //if SetTarget() was used then TargetSet=true
            {
                this.MoveTowardsTarget(elapsedTime);
                return;
            }
            this.MoveStraight(elapsedTime);
        }
	}
}