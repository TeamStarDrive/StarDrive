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
        private bool Jammed;
        private bool EcmRun;

        public MissileAI(Projectile missile, GameplayObject target)
        {
            Missile = missile;
            Target  = target;
            if (Empire.Universe == null)
                return;

            if (Missile.Owner != null)
            {
                TargetList = Missile.Owner.AI.PotentialTargets;
            }
            else if (Missile.Planet != null)
            {
                TargetList = new Array<Ship>();
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
                        Target = targetShip.GetRandomInternalModule(Missile);
                        return;
                    }
                }

                foreach (Ship ship in TargetList)
                {
                    if (!ship.Active || ship.dying || ship.engineState == Ship.MoveState.Warp)
                        continue;
                    Target = ship.GetRandomInternalModule(Missile);
                    return;
                }                
            }

            if (TargetList.IsEmpty)
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
                Target = bestTarget.GetRandomInternalModule(Missile);
            }
        }

        private void MoveStraight(float elapsedTime)
        {
            Missile.Velocity = Missile.Rotation.RadiansToDirection() * (elapsedTime * Missile.Speed);
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
            Vector2 targetPos = Target.Center;
            if (!Missile.ErrorSet)
            {
                float randomdeviation = RandomMath.RandomBetween(900f, 1400f);
                float rdbothways = RandomMath.RandomBetween(0f, 1f) > 0.5f ? randomdeviation : -randomdeviation;
                targetPos.X += rdbothways;
                targetPos.Y -= rdbothways;
                Missile.FixedError = targetPos;
                Missile.ErrorSet   = true;
            }
            else
            {
                targetPos = Missile.FixedError;
            }

            Missile.GuidedMoveTowards(elapsedTime, targetPos);

            float distancetoEnd = Missile.Center.Distance(targetPos);
            if (distancetoEnd <= 300f)
                Missile.Die(Missile, false);
            Target = null;
        }

        public void SetTarget(GameplayObject target) => Target = target;


        //added by gremlin Deveksmod Missilethink.
        public void Think(float elapsedTime)
        {
            if (Target != null && GlobalStats.HasMod 
                && (GlobalStats.ActiveModInfo.enableECM || Missile.Weapon.TerminalPhaseAttack))
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
                    Missile.GuidedMoveTowards(elapsedTime, Target.Center);
                    Missile.Velocity *= Missile.Weapon.TerminalPhaseSpeedMod;
                    return;
                }
            }
            ThinkTimer -= elapsedTime;
            if (ThinkTimer <= 0f)
            {
                ThinkTimer = 1f;
                if (Target == null || !Target.Active || Target is ShipModule targetModule && targetModule.GetParent().dying)
                {
                    Target = null;
                    ChooseTarget();
                }
            }
            if (Target != null)
            {
                Missile.GuidedMoveTowards(elapsedTime, Target.Center);
                return;
            }
            MoveStraight(elapsedTime);
        }
    }
}