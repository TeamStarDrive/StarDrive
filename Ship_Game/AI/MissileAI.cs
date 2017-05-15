using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class MissileAI
    {
        public Projectile Owner;
        private GameplayObject Target;
        private readonly BatchRemovalCollection<Ship> TargetList = new BatchRemovalCollection<Ship>();
        private float thinkTimer = 0.15f;
        private bool TargetSet;
        private bool Jammed;
        private bool ECMRun;

        public MissileAI(Projectile owner)
        {
            Owner = owner;
            if (Empire.Universe == null) return;
            if (Owner.Owner == null)
            {
                GameplayObject[] nearbyShips = Owner.FindNearby(GameObjectType.Ship);
                foreach(GameplayObject go in nearbyShips)
                {
                    var nearbyShip = (Ship) go;
                    if (nearbyShip.loyalty != Owner.Loyalty && Owner.Weapon.TargetValid(nearbyShip.shipData.Role))
                        TargetList.Add(nearbyShip);
                }
            }
            else
            {
                TargetList = Owner.Owner.AI.PotentialTargets;
            }
        }

        //added by gremlin deveks ChooseTarget
        public void ChooseTarget()
        {
            Ship owningShip = Owner.Owner;
            if (owningShip != null && owningShip.Active && !owningShip.dying)
            {
                if (owningShip.AI.Target is Ship targetShip)
                {
                    if (targetShip.Active && targetShip.loyalty != Owner.Loyalty && 
                        (!Owner.Loyalty.TryGetRelations(targetShip.loyalty, out Relationship targetRelations) 
                         || !targetRelations.Known || !targetRelations.Treaty_NAPact))
                    {
                        SetTarget(targetShip.GetRandomInternalModule(Owner));
                        return;
                    }
                }

                foreach (Ship ship in TargetList)
                {
                    if (!ship.Active || ship.dying || ship.engineState == Ship.MoveState.Warp)
                        continue;
                    SetTarget(ship.GetRandomInternalModule(Owner));
                    return;
                }                
            }

            if (TargetList.Count <= 0)
                return;

            Empire owner = owningShip?.loyalty ?? Owner.Planet.Owner;
            if (owner == null)
                return;

            float bestSqDist = float.MaxValue;
            Ship bestTarget = null;
            foreach (Ship sourceTargetShip in TargetList)
            {
                if (!sourceTargetShip.Active || sourceTargetShip.dying )
                    continue;

                float sqDist = Owner.Center.SqDist(sourceTargetShip.Center);
                if (sqDist > bestSqDist && 
                    Owner.Loyalty.TryGetRelations(owner, out Relationship relTarget) && relTarget.Treaty_NAPact)
                    continue;
                bestSqDist = sqDist;
                bestTarget = sourceTargetShip;                    
            }

            if (bestTarget != null && bestSqDist < 30000 * 30000)
            {
                SetTarget(bestTarget.GetRandomInternalModule(Owner));
            }
        }

        public void ClearTarget()
        {
            TargetSet = false;
            Target    = null;
        }

        private void MoveStraight(float elapsedTime)
        {
            Owner.Velocity = Owner.Rotation.RadiansToDirection() * (elapsedTime * Owner.Speed);
            Owner.Velocity = Owner.Velocity.Normalized() * Owner.VelocityMax;
        }

        private void MoveTowardsTarget(float elapsedTime)
        {
            if (Target == null)
                return;

            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 wantedForward = Owner.Center.DirectionToTarget(Target.Center);
            float angleDiff = (float)Math.Acos(wantedForward.Dot(forward));
            float facing = (wantedForward.Dot(right) > 0f ? 1f : -1f);
            // I suspect this is in radians - so 0.2f angle difference is actually about 11 degrees; can be problematic for missile AI guidance trying to hit target as it won't adjust early enough. Trying 0.1f
            if (angleDiff > 0.1f)
            {
                Owner.Rotation += Math.Min(angleDiff, facing * elapsedTime * Owner.RotationRadsPerSecond);
            }
            wantedForward = forward.Normalized();
            Owner.Velocity = wantedForward * (elapsedTime * Owner.Speed);
            Owner.Velocity = Owner.Velocity.Normalized() * Owner.VelocityMax;
        }

        private void MoveTowardsTargetJammed(float elapsedTime)
        {
            if (Target == null)
            {
                Jammed = false;
                ECMRun = false;
                return;
            }
            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 aimPos = Target.Center;
            if (!Owner.ErrorSet)
            {
                float randomdeviation = RandomMath.RandomBetween(900f, 1400f);
                float rdbothways = RandomMath.RandomBetween(0f, 1f) > 0.5f ? randomdeviation : -randomdeviation;
                aimPos.X += rdbothways;
                aimPos.Y -= rdbothways;
                Owner.FixedError = aimPos;
                Owner.ErrorSet = true;
            }
            else
            {
                aimPos = Owner.FixedError;
            }
            Vector2 wantedForward = Owner.Center.DirectionToTarget(aimPos);
            float angleDiff = (float)Math.Acos(wantedForward.Dot(forward));
            float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
            if (angleDiff > 0.1f)
                Owner.Rotation += Math.Min(angleDiff, facing * elapsedTime * Owner.RotationRadsPerSecond);
            wantedForward = Vector2.Normalize(forward);
            Owner.Velocity = wantedForward * (elapsedTime * Owner.Speed);
            Owner.Velocity = Owner.Velocity.Normalized() * Owner.VelocityMax;
            float distancetoEnd = Owner.Center.Distance(aimPos);
            if (distancetoEnd <= 300f)
                Owner.Die(Owner, false);
            Target = null;
        }

        private void MoveTowardsTargetTerminal(float elapsedTime)
        {
            if (Target == null)
                return;

            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 wantedForward = Owner.Center.DirectionToTarget(Target.Center);
            float angleDiff = (float)Math.Acos(Vector2.Dot(wantedForward, forward));
            float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
            if (angleDiff > 0.1f)
                Owner.Rotation += Math.Min(angleDiff, facing * elapsedTime * Owner.RotationRadsPerSecond);
            wantedForward = forward.Normalized();
            Owner.Velocity = wantedForward * (elapsedTime * Owner.Speed);
            Owner.Velocity = Owner.Velocity.Normalized() * Owner.VelocityMax * Owner.Weapon.TerminalPhaseSpeedMod;
        }

        public void SetTarget(GameplayObject target)
        {
            if (target == null)
                return;
            TargetSet = true;
            Target = target;
        }

        //added by gremlin Deveksmod Missilethink.
        public void Think(float elapsedTime)
        {
            if (Target != null && GlobalStats.ActiveModInfo != null && (GlobalStats.ActiveModInfo.enableECM 
                || Owner.Weapon.TerminalPhaseAttack))
            {
                float distancetoTarget = Owner.Center.Distance(Target.Center);
                if (Jammed)
                {
                    MoveTowardsTargetJammed(elapsedTime);
                    return;
                }
                if (GlobalStats.ActiveModInfo.enableECM && Target is ShipModule targetModule && !ECMRun && distancetoTarget <= 4000)
                {
                    ECMRun = true;
                    float targetEcm = targetModule.GetParent().ECMValue;
                    if (RandomMath.RandomBetween(0f, 1f) + Owner.Weapon.ECMResist < targetEcm)
                    {
                        Jammed = true;
                        MoveTowardsTargetJammed(elapsedTime);
                        return;
                    }
                }
                if (Owner.Weapon.TerminalPhaseAttack && distancetoTarget <= Owner.Weapon.TerminalPhaseDistance)
                {
                    MoveTowardsTargetTerminal(elapsedTime);
                    return;
                }
            }
            thinkTimer -= elapsedTime;
            if (thinkTimer <= 0f)
            {
                thinkTimer = 1f;
                if (Target == null || !Target.Active || Target is ShipModule targetModule && targetModule.GetParent().dying)
                {
                    ClearTarget();
                    ChooseTarget();
                }
            }
            if (TargetSet)
            {
                MoveTowardsTarget(elapsedTime);
                return;
            }
            MoveStraight(elapsedTime);
        }
    }
}