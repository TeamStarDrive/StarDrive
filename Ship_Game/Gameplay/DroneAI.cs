using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game.Gameplay
{
	public class DroneAI: IDisposable
	{
		public Projectile Owner;

		private GameplayObject Target;

		public static UniverseScreen universeScreen;

		private float thinkTimer;

		public Weapon w;

		//private bool TargetSet;

		private Vector2 OrbitPos = new Vector2();

		private float OrbitalAngle;

		public BatchRemovalCollection<Beam> Beams = new BatchRemovalCollection<Beam>();

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public DroneAI(Projectile owner)
		{
			this.Owner = owner;
			this.w = ResourceManager.GetWeapon("RepairBeam");
		}

		public void ChooseTarget()
		{
			this.Target = null;
			//List<GameplayObject> nearby = UniverseScreen.ShipSpatialManager.GetNearby(this.Owner);
            //List<GameplayObject> nearby = this.Owner.owner.GetAI().FriendliesNearby;
			List<Ship> Potentials = new List<Ship>();
            
			//foreach (GameplayObject go in nearby)
            foreach (Ship go in this.Owner.owner.GetAI().FriendliesNearby)
			{
                bool isShip = go is Ship;
                
                    Ship goShip = go as Ship;
                    if (!isShip || goShip.loyalty != this.Owner.loyalty || goShip.Health >= goShip.HealthMax)
				{
					continue;
				}
                    Potentials.Add(goShip);
			}
			IOrderedEnumerable<Ship> sortedList = 
				from ship in Potentials
				orderby ship.Health / ship.HealthMax
				select ship;
			for (int i = 0; i < sortedList.Count<Ship>(); i++)
			{
				if (Vector2.Distance(sortedList.ElementAt<Ship>(i).Center, this.Owner.Position) < 20000f && sortedList.ElementAt<Ship>(i).Active && sortedList.ElementAt<Ship>(i).Health > 0f)
				{
					this.Target = sortedList.ElementAt<Ship>(i);
					return;
				}
			}
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

		private void FireRepairBeam()
		{
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

		private void MoveTowardsPosition(float elapsedTime)
		{
			if (elapsedTime == 0f)
			{
				return;
			}
			Vector2.Distance(this.Owner.Center, this.OrbitPos);
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			if (this.Target == null)
			{
				Vector2 AimPosition = this.OrbitPos;
				Vector2 LeftStick = this.findVectorToTarget(this.Owner.Center, AimPosition);
				LeftStick.Y = LeftStick.Y * -1f;
				Vector2 wantedForward = Vector2.Normalize(LeftStick);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.2f)
				{
					Projectile owner = this.Owner;
					owner.Rotation = owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.speed / 350f);
				}
				wantedForward = Vector2.Normalize(forward);
				this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
				this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
				return;
			}
			Vector2 AimPosition0 = this.Target.Center;
			Vector2 LeftStick0 = this.findVectorToTarget(this.Owner.Center, AimPosition0);
			LeftStick0.Y = LeftStick0.Y * -1f;
			Vector2 wantedForward0 = Vector2.Normalize(LeftStick0);
			float angleDiff0 = (float)Math.Acos((double)Vector2.Dot(wantedForward0, forward));
			float facing0 = (Vector2.Dot(wantedForward0, right) > 0f ? 1f : -1f);
			if (angleDiff0 > 0.2f)
			{
				Projectile rotation = this.Owner;
				rotation.Rotation = rotation.Rotation + Math.Min(angleDiff0, facing0 * elapsedTime * this.Owner.speed / 350f);
			}
			wantedForward0 = Vector2.Normalize(forward);
			this.Owner.Velocity = wantedForward0 * (elapsedTime * this.Owner.speed);
			this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
		}

		private void OrbitShip(Ship ship, float elapsedTime)
		{
			this.OrbitPos = HelperFunctions.GeneratePointOnCircle(this.OrbitalAngle, ship.Center, 1500f);
			if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500f)
			{
				DroneAI orbitalAngle = this;
				orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
				if (this.OrbitalAngle >= 360f)
				{
					DroneAI droneAI = this;
					droneAI.OrbitalAngle = droneAI.OrbitalAngle - 360f;
				}
				this.OrbitPos = HelperFunctions.GeneratePointOnCircle(this.OrbitalAngle, ship.Position, 2500f);
			}
			this.MoveTowardsPosition(elapsedTime);
		}

		public void SetTarget(GameplayObject target)
		{
			//this.TargetSet = true;    //unused
			this.Target = target;
		}

		public void Think(float elapsedTime)
		{
			if (this.w != null)
			{
				Weapon weapon = this.w;
				weapon.timeToNextFire = weapon.timeToNextFire - elapsedTime;
			}
			this.Beams.ApplyPendingRemovals();
			DroneAI droneAI = this;
			droneAI.thinkTimer = droneAI.thinkTimer - elapsedTime;
			if ((this.Target == null || !this.Target.Active || (this.Target as Ship).Health == (this.Target as Ship).HealthMax) && this.thinkTimer < 0f)
			{
				this.ChooseTarget();
				this.thinkTimer = 2.5f;
			}
			if (this.Target == null)
			{
				for (int i = 0; i < this.Beams.Count; i++)
				{
					this.Beams[i].Die(null, true);
				}
				if (this.Owner.owner != null)
				{
					this.OrbitShip(this.Owner.owner, elapsedTime);
				}
				return;
			}
			if ((this.Target as Ship).Health / (this.Target as Ship).HealthMax < 1f && this.w.timeToNextFire <= 0f && this.Target != null && Vector2.Distance(this.Owner.Center, this.Target.Center) < 15000f)
			{
				Vector2 FireDirection = this.findVectorToTarget(this.Owner.Center, this.Target.Center);
				FireDirection.Y = FireDirection.Y * -1f;
				FireDirection = Vector2.Normalize(FireDirection);
				this.w.FireDroneBeam(FireDirection, this.Target, this);
			}
			for (int i = 0; i < this.Beams.Count; i++)
			{
				this.Beams[i].UpdateDroneBeam(this.Owner.Center, this.Target.Center, this.w.BeamThickness, DroneAI.universeScreen.view, DroneAI.universeScreen.projection, elapsedTime);
			}
			this.OrbitShip(this.Target as Ship, elapsedTime);
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Beams != null)
                        this.Beams.Dispose();

                }
                this.Beams = null;
                this.disposed = true;
            }
        }
	}
}