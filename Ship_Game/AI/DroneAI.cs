using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class DroneAI: IDisposable
	{
		public Projectile Owner;
		private Ship DroneTarget;
		public static UniverseScreen UniverseScreen;
		private float ThinkTimer;
		public Weapon DroneWeapon;
		private float OrbitalAngle;
		public BatchRemovalCollection<Beam> Beams = new BatchRemovalCollection<Beam>();

        public DroneAI(Projectile owner)
		{
            Owner = owner;
            DroneWeapon = ResourceManager.CreateWeapon("RepairBeam");
		}

		public void ChooseTarget()
		{
			var potentials = new Array<Ship>();

		    for (int i = 0; i < Owner.Owner.AI.FriendliesNearby.Count; i++)
		    {
		        Ship go = Owner.Owner.AI.FriendliesNearby[i];
		        if (go == null || !go.Active || go.loyalty != Owner.Loyalty || go.Health >= go.HealthMax)
		            continue;
		        potentials.Add(go);
		    }

		    DroneTarget = potentials.FindMinFiltered(
		            filter:   ship => ship.Active && ship.Health > 0 && ship.Center.InRadius(Owner.Position, 20000),
		            selector: ship => ship.Health / ship.HealthMax);
		}

        // @todo Refactor this mess
		private void MoveTowardsPosition(float elapsedTime, Vector2 orbitalPos)
		{
			var forward = new Vector2((float)Math.Sin(Owner.Rotation), -(float)Math.Cos(Owner.Rotation));
			var right = new Vector2(-forward.Y, forward.X);
            
			if (DroneTarget == null)
			{
				Vector2 leftStick = Owner.Center.DirectionToTarget(orbitalPos);
				leftStick.Y = leftStick.Y * -1f;
				Vector2 wantedForward = leftStick.Normalized();

				float angleDiff = (float)Math.Acos(Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.2f)
                    Owner.Rotation = Owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * Owner.Speed / 350f);
				wantedForward = Vector2.Normalize(forward);
				Owner.Velocity = wantedForward * (elapsedTime * Owner.Speed);
				Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Owner.VelocityMax;
				return;
			}
			Vector2 wantedForward0 = Vector2.Normalize(-Owner.Center.DirectionToTarget(DroneTarget.Center));
			float angleDiff0 = (float)Math.Acos(Vector2.Dot(wantedForward0, forward));
			float facing0 = (Vector2.Dot(wantedForward0, right) > 0f ? 1f : -1f);
			if (angleDiff0 > 0.2f)
                Owner.Rotation = Owner.Rotation + Math.Min(angleDiff0, facing0 * elapsedTime * Owner.Speed / 350f);
			wantedForward0 = Vector2.Normalize(forward);
			Owner.Velocity = wantedForward0 * (elapsedTime * Owner.Speed);
			Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Owner.VelocityMax;
		}

		private void OrbitShip(Ship ship, float elapsedTime)
		{
			Vector2 orbitalPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
			if (orbitalPos.InRadius(Owner.Center, 1500f))
			{
				OrbitalAngle = OrbitalAngle + 15f;
				if (OrbitalAngle >= 360f)
					OrbitalAngle = OrbitalAngle - 360f;
			    orbitalPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
			}
            if (elapsedTime > 0f)
			    MoveTowardsPosition(elapsedTime, orbitalPos);
		}

		public void Think(float elapsedTime)
		{
			DroneWeapon.CooldownTimer -= elapsedTime;

            Beams.ApplyPendingRemovals();
			ThinkTimer -= elapsedTime;
			if (ThinkTimer <= 0f && (DroneTarget == null || !DroneTarget.Active || DroneTarget.Health >= DroneTarget.HealthMax))
			{
				ChooseTarget();
				ThinkTimer = 2.5f;
			}
			if (DroneTarget == null)
			{
				for (int i = 0; i < Beams.Count; ++i)
				{
					Beams[i].Die(null, true);
				}
				if (Owner.Owner != null)
					OrbitShip(Owner.Owner, elapsedTime);
				return;
			}
			if (DroneTarget.Health / DroneTarget.HealthMax < 1f
                && DroneWeapon.CooldownTimer <= 0f
                && DroneTarget != null && Owner.Center.Distance(DroneTarget.Center) < 15000f)
			{
				DroneWeapon.FireDroneBeam(DroneTarget, this);
			}
			for (int i = 0; i < Beams.Count; ++i)
			{
				Beams[i].UpdateDroneBeam(Owner.Center, DroneTarget.Center, DroneWeapon.BeamThickness, elapsedTime);
			}
			OrbitShip(DroneTarget, elapsedTime);
		}

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~DroneAI() { Destroy(); }

        private void Destroy()
        {
            Beams?.Dispose(ref Beams);
        }
    }
}