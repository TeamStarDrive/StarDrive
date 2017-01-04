using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class MissileAI
	{
		public Projectile Owner;

		private GameplayObject Target;

		private BatchRemovalCollection<Ship> TargetList = new BatchRemovalCollection<Ship>();

		public static UniverseScreen universeScreen;

		private float thinkTimer = 0.15f;

		private bool TargetSet;

        private bool Jammed = false;

        private bool ECMRun = false;

		public MissileAI(Projectile owner)
		{
			this.Owner = owner;
			if (MissileAI.universeScreen != null)
			{
				Array<GameplayObject> GPO;
                if (this.Owner.Owner == null)
                {
                    GPO = UniverseScreen.ShipSpatialManager.GetNearby(this.Owner);
                    for (int i = 0; i < GPO.Count; i++)
                    {
                        Ship target = GPO[i] as Ship;
                        if (target != null)
                        {

                            if (target != null && target.loyalty != this.Owner.loyalty && this.Owner.weapon.TargetValid(target.shipData.Role))
                                this.TargetList.Add(target);
                        }
                    }
                }
                else
                {
                    this.TargetList = this.Owner.Owner.GetAI().PotentialTargets;
                }
			}
		}

        //added by gremlin deveks ChooseTarget
        public void ChooseTarget()
        {
            if (this.Owner.owner != null && this.Owner.owner.Active && !this.Owner.owner.dying)
            {
                GameplayObject sourceTarget = this.Owner.owner.GetAI().Target;
                Relationship relTarget = null;
   
                Ship sourceTargetShip = sourceTarget as Ship;
                if (this.Owner.owner != null && sourceTargetShip!=null)
                {
                    this.Owner.loyalty.TryGetRelations(sourceTargetShip.loyalty, out relTarget);
                }
                if (sourceTarget != null && sourceTarget.Active
                    && sourceTargetShip.loyalty != this.Owner.loyalty && (relTarget == null || !relTarget.Known || !relTarget.Treaty_NAPact))
                {
                    this.SetTarget(sourceTargetShip.GetRandomInternalModule(this.Owner));
                    
                    return;
                }
                if (true)
                    foreach (Ship ship in TargetList)
                    {
                        if (!ship.Active || ship.dying || ship.engineState == Ship.MoveState.Warp)
                            continue;
                        this.SetTarget(ship
                        .GetRandomInternalModule(this.Owner));
                        return;
                    }                
            }
            if (TargetList.Count > 0)
            {
                Empire owner =null;
                if(this.Owner.owner !=null)
                owner=this.Owner.owner.loyalty;
                if(owner == null)
                    owner = this.Owner.Planet.Owner;
                if(owner == null)
                    return;
                
                Relationship relTarget = null;
                float distance = 1000000;
                float currentd =0;
                Ship test1 = null;
                foreach (Ship sourceTargetShip in this.TargetList)
                {
                    if (!sourceTargetShip.Active || sourceTargetShip.dying )
                        continue;
                    
                        this.Owner.loyalty.TryGetRelations(owner, out relTarget);
                    
                    currentd = Vector2.Distance(this.Owner.Center, sourceTargetShip.Center) ;
                    if ((relTarget != null && relTarget.Treaty_NAPact) || currentd > distance)
                    {
                        continue;
                    }
                    distance =currentd ;
                    test1 = sourceTargetShip;                    
                }

                if (test1 != null)
                    if (distance < 30000)
                        this.SetTarget(test1
                        .GetRandomInternalModule(this.Owner));
            }
        }

		public void ClearTarget()
		{
            this.TargetSet = false;
			this.Target = null;
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
                // I suspect this is in radians - so 0.2f angle difference is actually about 11 degrees; can be problematic for missile AI guidance trying to hit target as it won't adjust early enough. Trying 0.1f
				if (angleDiff > 0.1f)
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

        private void MoveTowardsTargetJammed(float elapsedTime)
        {
            if (this.Target == null)
            {
                Jammed = false;
                ECMRun = false;
                return;
            }
            try
            {
                Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
                Vector2 right = new Vector2(-forward.Y, forward.X);
                Vector2 AimPosition = this.Target.Center;
                if (!this.Owner.ErrorSet)
                {
                    float randomdeviation = RandomMath.RandomBetween(900f, 1400f);
                    float rdbothways = RandomMath.RandomBetween(0f, 1f) > 0.5f ? randomdeviation : -randomdeviation;
                    AimPosition.X += rdbothways;
                    AimPosition.Y -= rdbothways;
                    this.Owner.FixedError = AimPosition;
                    this.Owner.ErrorSet = true;
                }
                else
                {
                    AimPosition = this.Owner.FixedError;
                }
                Vector2 LeftStick = this.findVectorToTarget(this.Owner.Center, AimPosition);
                LeftStick.Y = LeftStick.Y * -1f;
                Vector2 wantedForward = Vector2.Normalize(LeftStick);
                float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
                float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
                if (angleDiff > 0.1f)
                {
                    //Projectile owner = this.Owner;
                   this.Owner.Rotation = this.Owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.RotationRadsPerSecond);
                }
                wantedForward = Vector2.Normalize(forward);
                this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
                this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
                float DistancetoEnd = Vector2.Distance(this.Owner.Center, AimPosition);
                if (DistancetoEnd <= 300f)
                {
                    this.Owner.Die((GameplayObject)this.Owner, false);
                }
            }
            catch
            {
                this.Target = null;
            }
        }

        private void MoveTowardsTargetTerminal(float elapsedTime)
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
                if (angleDiff > 0.1f)
                {
                    Projectile owner = this.Owner;
                    owner.Rotation = owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.RotationRadsPerSecond);
                }
                wantedForward = Vector2.Normalize(forward);
                this.Owner.Velocity = wantedForward * (elapsedTime * this.Owner.speed);
                this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum * this.Owner.weapon.TerminalPhaseSpeedMod;
            }
            catch
            {
                this.Target = null;
            }
        }

		public void SetTarget(GameplayObject target)
		{
            if (target == null)
                return;
			this.TargetSet = true;
			this.Target = target;
		}

        //added by gremlin Deveksmod Missilethink.
        public void Think(float elapsedTime)
        {
			if (this.Target != null && GlobalStats.ActiveModInfo != null && (GlobalStats.ActiveModInfo.enableECM || this.Owner.weapon.TerminalPhaseAttack))
            {
                float DistancetoTarget = Vector2.Distance(this.Owner.Center, this.Target.Center);
                if (this.Jammed)
                {
                    this.MoveTowardsTargetJammed(elapsedTime);
                    return;
                }
				if (GlobalStats.ActiveModInfo.enableECM && (this.Target is ShipModule) && !this.ECMRun && DistancetoTarget <= 4000)
                {
                    this.ECMRun = true;
                    float TargetECM = (this.Target as ShipModule).GetParent().ECMValue;
                    float ECMResist = this.Owner.weapon.ECMResist;
                    if (RandomMath.RandomBetween(0f, 1f) + ECMResist < TargetECM)
                    {
                        this.Jammed = true;
                        this.MoveTowardsTargetJammed(elapsedTime);
                        return;
                    }
                }
                if (this.Owner.weapon.TerminalPhaseAttack && DistancetoTarget <= this.Owner.weapon.TerminalPhaseDistance)
                {
                    this.MoveTowardsTargetTerminal(elapsedTime);
                    return;
                }
            }
            this.thinkTimer -= elapsedTime;
            if (this.thinkTimer <= 0f)
            {
                this.thinkTimer = 1f;
                if (this.Target == null || !this.Target.Active || (this.Target is ShipModule && (this.Target as ShipModule).GetParent().dying))
                {
                    this.ClearTarget();
                    this.ChooseTarget();
                }
            }
            if (TargetSet)
            {
                this.MoveTowardsTarget(elapsedTime);
                return;
            }
            this.MoveStraight(elapsedTime);
        }
	}
}