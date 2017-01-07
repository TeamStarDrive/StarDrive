using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class DroneAI: IDisposable
	{
		public Projectile Owner;

		private GameplayObject _target;

		public static UniverseScreen UniverseScreen;

		private float _thinkTimer;

		public Weapon W;

		//private bool TargetSet;

		private Vector2 _orbitPos;

		private float _orbitalAngle;

		public BatchRemovalCollection<Beam> Beams = new BatchRemovalCollection<Beam>();

		public DroneAI(Projectile owner)
		{
            Owner = owner;
            W = ResourceManager.GetWeapon("RepairBeam");
		}

		public void ChooseTarget()
		{
            _target = null;
			Array<Ship> potentials = new Array<Ship>();

		    for (var index = 0; index < Owner.owner.GetAI().FriendliesNearby.Count; index++)
		    {
		        Ship go = Owner.owner.GetAI().FriendliesNearby[index];

		        if (go == null || !go.Active || go.loyalty != Owner.loyalty || go.Health >= go.HealthMax)
		        {
		            continue;
		        }
		        potentials.Add(go);
		    }

		    _target =
		        potentials.FindMinFiltered(
		            filter: ship => ship.Active && ship.Health > 0 && ship.Center.InRadius(Owner.Position, 20000),
		            selector: ship => ship.Health / ship.HealthMax);
		}

		private void MoveTowardsPosition(float elapsedTime)
		{
			if (elapsedTime <= 0f) return;
            
            Vector2.Distance(Owner.Center, _orbitPos);
			var forward = new Vector2((float)Math.Sin(Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
			var right = new Vector2(-forward.Y, forward.X);
            
            
			if (_target == null)
			{
				Vector2 AimPosition = _orbitPos;
				Vector2 LeftStick = Owner.Center.FindVectorToTarget(AimPosition);
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
			Vector2 AimPosition0 = _target.Center;
			Vector2 LeftStick0 = Owner.Center.FindVectorToTarget(AimPosition0);
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
			this._orbitPos = ship.Center.PointOnCircle(this._orbitalAngle, 1500f);
			if (Vector2.Distance(this._orbitPos, this.Owner.Center) < 1500f)
			{
				DroneAI orbitalAngle = this;
				orbitalAngle._orbitalAngle = orbitalAngle._orbitalAngle + 15f;
				if (this._orbitalAngle >= 360f)
				{
					DroneAI droneAI = this;
					droneAI._orbitalAngle = droneAI._orbitalAngle - 360f;
				}
				this._orbitPos = ship.Position.PointOnCircle(this._orbitalAngle, 2500f);
			}
			this.MoveTowardsPosition(elapsedTime);
		}

		public void SetTarget(GameplayObject target)
		{
			//this.TargetSet = true;    //unused
			this._target = target;
		}

		public void Think(float elapsedTime)
		{
			if (this.W != null)
			{
				//Weapon weapon = this.w;
				this.W.timeToNextFire = this.W.timeToNextFire - elapsedTime;
			}
			this.Beams.ApplyPendingRemovals();
			//DroneAI droneAI = this;
			this._thinkTimer = this._thinkTimer - elapsedTime;
			if ((this._target == null || !this._target.Active || (this._target as Ship).Health == (this._target as Ship).HealthMax) && this._thinkTimer < 0f)
			{
				this.ChooseTarget();
				this._thinkTimer = 2.5f;
			}
			if (this._target == null)
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
			if ((this._target as Ship).Health / (this._target as Ship).HealthMax < 1f && this.W.timeToNextFire <= 0f && this._target != null && Vector2.Distance(this.Owner.Center, this._target.Center) < 15000f)
			{
				Vector2 FireDirection = Owner.Center.FindVectorToTarget(_target.Center);
				FireDirection.Y = FireDirection.Y * -1f;
				FireDirection = Vector2.Normalize(FireDirection);
				this.W.FireDroneBeam(FireDirection, this._target, this);
			}
			for (int i = 0; i < this.Beams.Count; i++)
			{
				this.Beams[i].UpdateDroneBeam(this.Owner.Center, this._target.Center, this.W.BeamThickness, DroneAI.UniverseScreen.view, DroneAI.UniverseScreen.projection, elapsedTime);
			}
			this.OrbitShip(this._target as Ship, elapsedTime);
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