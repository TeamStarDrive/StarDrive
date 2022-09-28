using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Universe;

namespace Ship_Game.AI
{
    public sealed class MissileAI
    {
        readonly Projectile Missile;
        public GameObject Target { get; set; }

        Ship[] TargetList;
        float TargetUpdateTimer = 0.15f;

        public bool Jammed { get; private set; }
        bool CalculatedJamming;
        
        readonly int Level;
        Vector2 TargetError;
        float ErrorAdjustTimer;

        float RandomDirectionTimer;
        float RandomNozzleDirection;

        readonly float MaxNozzleDirection = 0.5f;
        readonly float InitialPhaseDirection;
        float InitialPhaseTimer = 0.5f;
        float DelayedIgnitionTimer;

        public MissileAI(Projectile missile, GameObject target, Vector2 initialVelocity)
        {
            Missile = missile;
            Target = target;
            DelayedIgnitionTimer = missile.Weapon.DelayedIgnition;
            
            if (Missile.Weapon.DelayedIgnition > 0f)
            {
                float launchDir = RandomMath.RollDie(2) == 1 ? -1.5708f : 1.5708f; // 90 degrees
                float rotation = Missile.Weapon.Owner?.Rotation ?? Missile.Rotation;

                // throw the missile out sideways
                initialVelocity += (rotation + launchDir).RadiansToDirection() * (100 + RandomMath.RollDie(100));
            }

            Missile.SetInitialVelocity(initialVelocity, rotateToVelocity: false);

            if (Missile.Weapon != null && Missile.Weapon.Tag_Torpedo)
                MaxNozzleDirection = 0.02f; // Torpedoes wiggle less

            InitialPhaseDirection = RandomMath.RollDice(50) ? -1f : +1f;

            if (Missile.Owner != null)
                Level = Missile.Owner.Level;
            else if (Missile.Planet != null)
                Level = Missile.Planet.Level;
        }

        bool TargetValid(Ship ship)
        {
            return ship.Active
                   && !ship.Dying
                   && !ship.IsInWarp
                   && Missile.Weapon.TargetValid(ship.ShipData.HullRole)
                   && Missile.Loyalty.IsEmpireAttackable(ship.Loyalty);
        }

        Ship[] GetTargetList()
        {
            if (Missile.Owner != null)
            {
                return Missile.Owner.AI.PotentialTargets;
            }
            if (Missile.Planet != null)
            {
                var targets = new Array<Ship>();

                // find nearby enemy ships
                GameObject[] nearbyShips = Missile.Universe.Spatial.FindNearby(GameObjectType.Ship,
                            Missile, Missile.Planet.GravityWellRadius, maxResults:32, excludeLoyalty:Missile.Loyalty);

                foreach (GameObject go in nearbyShips)
                {
                    if (Missile.Weapon!.TargetValid(go))
                    {
                        var nearbyShip = (Ship) go;
                        if (Missile.Loyalty.IsEmpireAttackable(nearbyShip.Loyalty))
                            targets.Add(nearbyShip);
                    }
                }
                return targets.ToArray();
            }
            return Empty<Ship>.Array;
        }

        //added by gremlin deveks ChooseTarget
        GameObject ChooseTarget()
        {
            if (Missile.Weapon.Tag_Torpedo)
                return null;  // Torps will not choose new targets but continue straight.

            if (!Missile.Loyalty.data.Traits.SmartMissiles)
            {
                Missile.Die(Missile, false);
                return null;
            }

            Ship owningShip = Missile.Owner;
            if (owningShip is { Active: true, Dying: false })
            {
                if (owningShip.AI.Target is { } targetShip && TargetValid(targetShip))
                    return targetShip.GetRandomInternalModule(Missile);

                foreach (Ship ship in owningShip.AI.PotentialTargets)
                    if (TargetValid(ship))
                        return ship.GetRandomInternalModule(Missile);
            }

            TargetList ??= GetTargetList();

            Empire owner = owningShip?.Loyalty ?? Missile.Planet.Owner;
            if (owner == null)
            {
                Missile.Die(Missile, false);
                return null;
            }

            float bestSqDist = float.MaxValue;
            Ship bestTarget = null;
            foreach (Ship sourceTargetShip in TargetList)
            {
                if (TargetValid(sourceTargetShip))
                {
                    float sqDist = Missile.Position.SqDist(sourceTargetShip.Position);
                    if (sqDist <= bestSqDist)
                    {
                        bestSqDist = sqDist;
                        bestTarget = sourceTargetShip;
                    }
                }
            }

            if (bestTarget == null || bestSqDist >= 30000 * 30000)
            {
                Missile.Die(Missile, false);
                return null;
            }

            return bestTarget.GetRandomInternalModule(Missile);
        }

        void MoveTowardsTargetJammed(FixedSimTime timeStep)
        {
            if (Target == null)
            {
                Jammed = false;
                CalculatedJamming = false;
                return;
            }

            Vector2 targetPos = Target.Position;
            if (!Missile.ErrorSet)
            {
                float randomDeviation = RandomMath.Float(900f, 1400f);
                float randomDeviation2 = RandomMath.Float(0f, 1f) > 0.5f ? randomDeviation : -randomDeviation;
                targetPos.X += randomDeviation2;
                targetPos.Y -= randomDeviation2;
                Missile.FixedError = targetPos;
                Missile.ErrorSet   = true;
            }
            else
            {
                targetPos = Missile.FixedError;
            }

            Missile.GuidedMoveTowards(timeStep, targetPos, 0f);

            float distanceToEnd = Missile.Position.Distance(targetPos);
            if (distanceToEnd <= 300f)
                Missile.Die(Missile, false);
            Target = null;
        }

        // added by gremlin Deveksmod Missilethink.
        public void Think(FixedSimTime timeStep)
        {
            Vector2 targetIntercept = Vector2.Zero;
            if (Target != null)
            {                
                targetIntercept = Missile.PredictImpact(Target);
                float distanceToTarget = Missile.Position.Distance(targetIntercept);

                if (!CalculatedJamming && distanceToTarget <= 4000f && Target is ShipModule targetModule)
                {
                    float targetEcm   = targetModule.GetParent().ECMValue;
                    float ecmResist   = Missile.Weapon.ECMResist + RandomMath.Float(0f, 1f);
                    Jammed            = (ecmResist < targetEcm);
                    CalculatedJamming = true;
                }

                if (DelayedIgnitionTimer > 0f) // ignition phase for some missiles
                {
                    DelayedIgnitionTimer -= timeStep.FixedTime;
                    if (DelayedIgnitionTimer <= 0f)
                        Missile.IgniteEngine();

                    return;
                }

                if (Jammed)
                {
                    MoveTowardsTargetJammed(timeStep);
                    return;
                }

                if (Missile.Weapon.IsMirv && distanceToTarget <= Missile.Weapon.MirvSeparationDistance)
                {
                    Missile.CreateMirv(Target);
                    return;
                }

                if (Missile.Weapon.TerminalPhaseAttack && distanceToTarget <= Missile.Weapon.TerminalPhaseDistance)
                {
                    Missile.TerminalPhase = true;
                    Missile.GuidedMoveTowards(timeStep, targetIntercept, 0f);
                    return;
                }
            }

            TargetUpdateTimer    -= timeStep.FixedTime;
            ErrorAdjustTimer     -= timeStep.FixedTime;
            RandomDirectionTimer -= timeStep.FixedTime;
            InitialPhaseTimer    -= timeStep.FixedTime;

            if (TargetUpdateTimer <= 0f)
            {
                TargetUpdateTimer = 0.15f;
                if (Target == null || !Target.Active || Target is ShipModule targetModule && targetModule.GetParent().Dying)
                {
                    Target = ChooseTarget();
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
                RandomNozzleDirection = RandomMath.Float(-MaxNozzleDirection, +MaxNozzleDirection);
            }

            float nozzleDirection = RandomNozzleDirection;
            if (InitialPhaseTimer > 0f)
            {
                nozzleDirection += InitialPhaseDirection;
            }

            if (Target != null)
            {
                Vector2 target = targetIntercept + TargetError;
                Missile.GuidedMoveTowards(timeStep, target, nozzleDirection);
            }
            else
            {
                Missile.MoveStraight();
            }
        }
    }
}