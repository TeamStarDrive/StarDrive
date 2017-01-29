using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class DroneAI: IDisposable
	{
		public Projectile Owner;
		private GameplayObject DroneTarget;
		public static UniverseScreen UniverseScreen;
		private float ThinkTimer;
		public Weapon DroneWeapon;
		private Vector2 OrbitalPos;
		private float OrbitalAngle;
		public BatchRemovalCollection<Beam> Beams = new BatchRemovalCollection<Beam>();

        public DroneAI(Projectile owner)
		{
            Owner = owner;
            DroneWeapon = ResourceManager.GetWeapon("RepairBeam");
		}

		public void ChooseTarget()
		{
            DroneTarget = null;
			Array<Ship> potentials = new Array<Ship>();

		    for (int index = 0; index < Owner.owner.GetAI().FriendliesNearby.Count; index++)
		    {
		        Ship go = Owner.owner.GetAI().FriendliesNearby[index];

		        if (go == null || !go.Active || go.loyalty != Owner.loyalty || go.Health >= go.HealthMax)
		        {
		            continue;
		        }
		        potentials.Add(go);
		    }

		    DroneTarget =
		        potentials.FindMinFiltered(
		            filter: ship => ship.Active && ship.Health > 0 && ship.Center.InRadius(Owner.Position, 20000),
		            selector: ship => ship.Health / ship.HealthMax);
		}

		private void MoveTowardsPosition(float elapsedTime)
		{
			if (elapsedTime <= 0f) return;
            
			var forward = new Vector2((float)Math.Sin(Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
			var right = new Vector2(-forward.Y, forward.X);
            
			if (DroneTarget == null)
			{
				Vector2 AimPosition = OrbitalPos;
				Vector2 LeftStick = Owner.Center.FindVectorToTarget(AimPosition);
				LeftStick.Y = LeftStick.Y * -1f;
				Vector2 wantedForward = Vector2.Normalize(LeftStick);

				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.2f)
                    this.Owner.Rotation = this.Owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.speed / 350f);
				wantedForward = Vector2.Normalize(forward);
				this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
				this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
				return;
			}
			Vector2 wantedForward0 = Vector2.Normalize(-Owner.Center.FindVectorToTarget(DroneTarget.Center));
			float angleDiff0 = (float)Math.Acos((double)Vector2.Dot(wantedForward0, forward));
			float facing0 = (Vector2.Dot(wantedForward0, right) > 0f ? 1f : -1f);
			if (angleDiff0 > 0.2f)
                this.Owner.Rotation = this.Owner.Rotation + Math.Min(angleDiff0, facing0 * elapsedTime * this.Owner.speed / 350f);
			wantedForward0 = Vector2.Normalize(forward);
			//this.Owner.Velocity = wantedForward0 * (elapsedTime * this.Owner.speed);      //This was getting assigned a value twice in a row...?
			this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
		}

		private void OrbitShip(Ship ship, float elapsedTime)
		{
			this.OrbitalPos = ship.Center.PointOnCircle(this.OrbitalAngle, 1500f);
			if (Vector2.Distance(this.OrbitalPos, this.Owner.Center) < 1500f)
			{
				this.OrbitalAngle = this.OrbitalAngle + 15f;
				if (this.OrbitalAngle >= 360f)
					this.OrbitalAngle = this.OrbitalAngle - 360f;
				this.OrbitalPos = ship.Position.PointOnCircle(this.OrbitalAngle, 2500f);
			}
			this.MoveTowardsPosition(elapsedTime);
		}

		public void SetTarget(GameplayObject target)
		{
			this.DroneTarget = target;
		}

		public void Think(float elapsedTime)
		{
			if (this.DroneWeapon != null)
				this.DroneWeapon.timeToNextFire = this.DroneWeapon.timeToNextFire - elapsedTime;
			this.Beams.ApplyPendingRemovals();
			this.ThinkTimer -= elapsedTime;
			if (this.ThinkTimer <= 0f && (this.DroneTarget == null || !this.DroneTarget.Active || (this.DroneTarget as Ship).Health == (this.DroneTarget as Ship).HealthMax))
			{
				this.ChooseTarget();
				this.ThinkTimer = 2.5f;
			}
			if (this.DroneTarget == null)
			{
				for (int i = 0; i < this.Beams.Count; i++)
				{
					this.Beams[i].Die(null, true);
				}
				if (this.Owner.owner != null)
					this.OrbitShip(this.Owner.owner, elapsedTime);
				return;
			}
			if ((  this.DroneTarget as Ship).Health / (this.DroneTarget as Ship).HealthMax < 1f
                && this.DroneWeapon.timeToNextFire <= 0f
                && this.DroneTarget != null && Vector2.Distance(this.Owner.Center, this.DroneTarget.Center) < 15000f )
			{
				Vector2 FireDirection = Owner.Center.FindVectorToTarget(DroneTarget.Center);
				FireDirection.Y = -FireDirection.Y;
				FireDirection = Vector2.Normalize(FireDirection);
				this.DroneWeapon.FireDroneBeam(FireDirection, this.DroneTarget, this);
			}
			for (int i = 0; i < this.Beams.Count; i++)
			{
				this.Beams[i].UpdateDroneBeam(this.Owner.Center, this.DroneTarget.Center, this.DroneWeapon.BeamThickness, DroneAI.UniverseScreen.view, DroneAI.UniverseScreen.projection, elapsedTime);
			}
			this.OrbitShip(this.DroneTarget as Ship, elapsedTime);
		}

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~DroneAI() { Destroy(); }
        // simpler, faster destruction logic
        private void Destroy()
        {
            Beams?.Dispose();
            Beams = null;
        }
    }
}