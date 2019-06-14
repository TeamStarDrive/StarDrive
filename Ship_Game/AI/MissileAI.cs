using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;

namespace Ship_Game.AI
{
    public sealed class MissileAI
    {
        readonly Projectile Missile;
        public GameplayObject Target { get; set; }

        readonly Array<Ship> TargetList;
        float TargetUpdateTimer = 0.15f;

        public bool Jammed { get; private set; }
        bool CalculatedJamming;
        
        readonly int Level;
        Vector2 TargetError;
        float ErrorAdjustTimer;

        float RandomDirectionTimer;
        float RandomNozzleDirection;

        readonly float InitialPhaseDirection;
        float InitialPhaseTimer = 0.5f;


        public MissileAI(Projectile missile, GameplayObject target)
        {
            Missile = missile;
            Target  = target;
            if (Empire.Universe == null)
                return;

            if (Missile.Owner != null)
            {
                TargetList = Missile.Owner.AI.PotentialTargets;
                Level = Missile.Owner.Level;
            }
            else if (Missile.Planet != null)
            {
                Level = missile.Planet.Level;
                TargetList = new Array<Ship>();
                GameplayObject[] nearbyShips = UniverseScreen.SpaceManager.FindNearby(
                            Missile, Missile.Planet.GravityWellRadius, GameObjectType.Ship);
                foreach (GameplayObject go in nearbyShips)
                {
                    if (missile.Weapon.TargetValid(go))
                    {
                        var nearbyShip = (Ship) go;
                        if (missile.Loyalty.IsEmpireAttackable(nearbyShip.loyalty))
                            TargetList.Add(nearbyShip);
                    }
                }
            }

            InitialPhaseDirection = RandomMath.RollDice(50) ? -1f : +1f;
        }

        //added by gremlin deveks ChooseTarget
        public void ChooseTarget()
        {
            Ship owningShip = Missile.Owner;
            if (owningShip != null && owningShip.Active && !owningShip.dying)
            {
                if (owningShip.AI.Target is Ship targetShip)
                {
                    if (targetShip.Active && Missile.Loyalty.IsEmpireAttackable(targetShip.loyalty))
                    {
                        Target = targetShip.GetRandomInternalModule(Missile);
                        return;
                    }
                }

                foreach (Ship ship in owningShip.AI.PotentialTargets)
                {
                    if (ship.Active && !ship.dying && ship.engineState != Ship.MoveState.Warp)
                    {
                        Target = ship.GetRandomInternalModule(Missile);
                        return;
                    }
                }                
            }

            if (TargetList?.IsEmpty ?? true)
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
                if (sqDist > bestSqDist || !Missile.Loyalty.IsEmpireAttackable(owner))
                    continue;
                bestSqDist = sqDist;
                bestTarget = sourceTargetShip;                    
            }

            if (bestTarget != null && bestSqDist < 30000 * 30000)
            {
                Target = bestTarget.GetRandomInternalModule(Missile);
            }
        }

        void MoveTowardsTargetJammed(float elapsedTime)
        {
            if (Target == null)
            {
                Jammed = false;
                CalculatedJamming = false;
                return;
            }

            Vector2 targetPos = Target.Center;
            if (!Missile.ErrorSet)
            {
                float randomDeviation = RandomMath.RandomBetween(900f, 1400f);
                float randomDeviation2 = RandomMath.RandomBetween(0f, 1f) > 0.5f ? randomDeviation : -randomDeviation;
                targetPos.X += randomDeviation2;
                targetPos.Y -= randomDeviation2;
                Missile.FixedError = targetPos;
                Missile.ErrorSet   = true;
            }
            else
            {
                targetPos = Missile.FixedError;
            }

            Missile.GuidedMoveTowards(elapsedTime, targetPos, 0f);

            float distanceToEnd = Missile.Center.Distance(targetPos);
            if (distanceToEnd <= 300f)
                Missile.Die(Missile, false);
            Target = null;
        }

        // added by gremlin Deveksmod Missilethink.
        public void Think(float elapsedTime)
        {
            Vector2 targetIntercept = Vector2.Zero;
            if (Target != null)
            {                
                targetIntercept = Missile.PredictImpact(Target);
                float distanceToTarget = Missile.Center.Distance(targetIntercept);

                if (!CalculatedJamming && distanceToTarget <= 4000f && Target is ShipModule targetModule)
                {
                    float targetEcm = targetModule.GetParent().ECMValue;
                    float ecmResist = Missile.Weapon.ECMResist + RandomMath.RandomBetween(0f, 1f);
                    Jammed = (ecmResist < targetEcm);
                    CalculatedJamming = true;
                }

                if (Jammed)
                {
                    MoveTowardsTargetJammed(elapsedTime);
                    return;
                }

                if (Missile.Weapon.TerminalPhaseAttack && distanceToTarget <= Missile.Weapon.TerminalPhaseDistance)
                {
                    Missile.GuidedMoveTowards(elapsedTime, targetIntercept, 0f, terminalPhase: true);
                    return;
                }
            }

            TargetUpdateTimer -= elapsedTime;
            ErrorAdjustTimer -= elapsedTime;
            RandomDirectionTimer -= elapsedTime;
            InitialPhaseTimer -= elapsedTime;

            if (TargetUpdateTimer <= 0f)
            {
                TargetUpdateTimer = 0.15f;
                if (Target == null || !Target.Active || Target is ShipModule targetModule && targetModule.GetParent().dying)
                {
                    Target = null;
                    ChooseTarget();
                    ErrorAdjustTimer = 0f; // readjust error
                }
            }

            if (ErrorAdjustTimer <= 0f)
            {
                // with every increase in level, adjustment gets much faster
                ErrorAdjustTimer = Math.Max(0.1f, 0.8f - Level * 0.1f);

                if (Target != null)
                {
                    TargetError = Missile.Weapon.GetTargetError(Target, Level);
                }
            }

            if (RandomDirectionTimer <= 0f)
            {
                RandomDirectionTimer = 0.5f;
                RandomNozzleDirection = RandomMath.RandomBetween(-0.5f, +0.5f);
            }

            float nozzleDirection = RandomNozzleDirection;
            if (InitialPhaseTimer > 0f)
            {
                nozzleDirection += InitialPhaseDirection;
            }

            if (Target != null)
            {
                Vector2 target = targetIntercept + TargetError;
                Missile.GuidedMoveTowards(elapsedTime, target, nozzleDirection);
            }
            else
            {
                Missile.MoveStraight();
            }
        }
    }
}