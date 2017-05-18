using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class MissileAI
    {
        private readonly Projectile Missile;
        private GameplayObject Target;
        private readonly Array<Ship> TargetList;
        private float ThinkTimer = 0.15f;
        private bool TargetSet;
        private bool Jammed;
        private bool EcmRun;

        public MissileAI(Projectile missile)
        {
            Missile = missile;
            if (Empire.Universe == null)
                return;

            if (Missile.Owner != null)
            {
                TargetList = Missile.Owner.AI.PotentialTargets;
            }
            else if (Missile.Planet != null)
            {
                GameplayObject[] nearbyShips = UniverseScreen.SpaceManager.FindNearby(
                            Missile, Missile.Planet.GravityWellRadius, GameObjectType.Ship);
                foreach(GameplayObject go in nearbyShips)
                {
                    var nearbyShip = (Ship) go;
                    if (nearbyShip.loyalty != missile.Loyalty && missile.Weapon.TargetValid(nearbyShip.shipData.Role))
                        TargetList.Add(nearbyShip);
                }
            }
        }

        //added by gremlin deveks ChooseTarget
        public void ChooseTarget()
        {
            Ship owningShip = Missile.Owner;
            if (owningShip != null && owningShip.Active && !owningShip.dying)
            {
                if (owningShip.AI.Target is Ship targetShip)
                {
                    if (targetShip.Active && targetShip.loyalty != Missile.Loyalty && 
                        (!Missile.Loyalty.TryGetRelations(targetShip.loyalty, out Relationship targetRelations) 
                         || !targetRelations.Known || !targetRelations.Treaty_NAPact))
                    {
                        SetTarget(targetShip.GetRandomInternalModule(Missile));
                        return;
                    }
                }

                foreach (Ship ship in TargetList)
                {
                    if (!ship.Active || ship.dying || ship.engineState == Ship.MoveState.Warp)
                        continue;
                    SetTarget(ship.GetRandomInternalModule(Missile));
                    return;
                }                
            }

            if (TargetList.Count <= 0)
                return;

            Empire owner = owningShip?.loyalty ?? Missile.Planet.Owner;
            if (owner == null)
                return;

            float bestSqDist = float.MaxValue;
            Ship bestTarget = null;
            foreach (Ship sourceTargetShip in TargetList)
            {
                if (!sourceTargetShip.Active || sourceTargetShip.dying )
                    continue;

                float sqDist = Missile.Center.SqDist(sourceTargetShip.Center);
                if (sqDist > bestSqDist && 
                    Missile.Loyalty.TryGetRelations(owner, out Relationship relTarget) && relTarget.Treaty_NAPact)
                    continue;
                bestSqDist = sqDist;
                bestTarget = sourceTargetShip;                    
            }

            if (bestTarget != null && bestSqDist < 30000 * 30000)
            {
                SetTarget(bestTarget.GetRandomInternalModule(Missile));
            }
        }

        public void ClearTarget()
        {
            TargetSet = false;
            Target    = null;
        }

        private void MoveStraight(float elapsedTime)
        {
            Missile.Velocity = Missile.Rotation.RadiansToDirection() * (elapsedTime * Missile.Speed);
            Missile.Velocity = Missile.Velocity.Normalized() * Missile.VelocityMax;
        }

        private void MoveTowardsTarget(float elapsedTime)
        {
            if (Target == null)
                return;

            Vector2 forward = Missile.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 wantedForward = Missile.Center.DirectionToTarget(Target.Center);
            float angleDiff = (float)Math.Acos(wantedForward.Dot(forward));
            float facing = (wantedForward.Dot(right) > 0f ? 1f : -1f);
            // I suspect this is in radians - so 0.2f angle difference is actually about 11 degrees; can be problematic for missile AI guidance trying to hit target as it won't adjust early enough. Trying 0.1f
            if (angleDiff > 0.1f)
            {
                Missile.Rotation += Math.Min(angleDiff, facing * elapsedTime * Missile.RotationRadsPerSecond);
            }
            wantedForward = forward.Normalized();
            Missile.Velocity = wantedForward * (elapsedTime * Missile.Speed);
            Missile.Velocity = Missile.Velocity.Normalized() * Missile.VelocityMax;
        }

        private void MoveTowardsTargetJammed(float elapsedTime)
        {
            if (Target == null)
            {
                Jammed = false;
                EcmRun = false;
                return;
            }
            Vector2 forward = Missile.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 aimPos = Target.Center;
            if (!Missile.ErrorSet)
            {
                float randomdeviation = RandomMath.RandomBetween(900f, 1400f);
                float rdbothways = RandomMath.RandomBetween(0f, 1f) > 0.5f ? randomdeviation : -randomdeviation;
                aimPos.X += rdbothways;
                aimPos.Y -= rdbothways;
                Missile.FixedError = aimPos;
                Missile.ErrorSet = true;
            }
            else
            {
                aimPos = Missile.FixedError;
            }
            Vector2 wantedForward = Missile.Center.DirectionToTarget(aimPos);
            float angleDiff = (float)Math.Acos(wantedForward.Dot(forward));
            float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
            if (angleDiff > 0.1f)
                Missile.Rotation += Math.Min(angleDiff, facing * elapsedTime * Missile.RotationRadsPerSecond);
            wantedForward = Vector2.Normalize(forward);
            Missile.Velocity = wantedForward * (elapsedTime * Missile.Speed);
            Missile.Velocity = Missile.Velocity.Normalized() * Missile.VelocityMax;
            float distancetoEnd = Missile.Center.Distance(aimPos);
            if (distancetoEnd <= 300f)
                Missile.Die(Missile, false);
            Target = null;
        }

        private void MoveTowardsTargetTerminal(float elapsedTime)
        {
            if (Target == null)
                return;

            Vector2 forward = Missile.Rotation.RadiansToDirection();
            Vector2 right   = forward.RightVector();
            Vector2 wantedForward = Missile.Center.DirectionToTarget(Target.Center);
            float angleDiff = (float)Math.Acos(Vector2.Dot(wantedForward, forward));
            float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
            if (angleDiff > 0.1f)
                Missile.Rotation += Math.Min(angleDiff, facing * elapsedTime * Missile.RotationRadsPerSecond);
            wantedForward = forward.Normalized();
            Missile.Velocity = wantedForward * (elapsedTime * Missile.Speed);
            Missile.Velocity = Missile.Velocity.Normalized() * Missile.VelocityMax * Missile.Weapon.TerminalPhaseSpeedMod;
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
                || Missile.Weapon.TerminalPhaseAttack))
            {
                float distancetoTarget = Missile.Center.Distance(Target.Center);
                if (Jammed)
                {
                    MoveTowardsTargetJammed(elapsedTime);
                    return;
                }
                if (GlobalStats.ActiveModInfo.enableECM && Target is ShipModule targetModule && !EcmRun && distancetoTarget <= 4000)
                {
                    EcmRun = true;
                    float targetEcm = targetModule.GetParent().ECMValue;
                    if (RandomMath.RandomBetween(0f, 1f) + Missile.Weapon.ECMResist < targetEcm)
                    {
                        Jammed = true;
                        MoveTowardsTargetJammed(elapsedTime);
                        return;
                    }
                }
                if (Missile.Weapon.TerminalPhaseAttack && distancetoTarget <= Missile.Weapon.TerminalPhaseDistance)
                {
                    MoveTowardsTargetTerminal(elapsedTime);
                    return;
                }
            }
            ThinkTimer -= elapsedTime;
            if (ThinkTimer <= 0f)
            {
                ThinkTimer = 1f;
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