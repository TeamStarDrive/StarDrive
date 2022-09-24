using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantPortal : Goal
    {
        Remnants Remnants => Owner.Remnants;
        Ship Portal => TargetShip;
        [StarData] Vector2 TetherOffset;

        [StarDataConstructor]
        public RemnantPortal(Empire owner) : base(GoalType.RemnantPortal, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CallGuardians,
                GenerateProduction
            };
        }

        public RemnantPortal(Empire owner, Ship portal, string systemName) : this(owner)
        {
            TargetShip = portal;
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {Owner.Name} Portal in {systemName} ----");
        }

        void UpdatePosition()
        {
            if (Portal.HealthPercent < 0.9f && LureEnemy())
                return;

            int roll = Portal.AI.Target?.System == Portal.System ? 35 : 5;
            if (Portal.AI.Target != null && Portal.System != null && RandomMath.RollDice(roll))
                JumpToEnemy();
            else
                ReturnToSpawnPos();
        }

        bool LureEnemy()
        {
            if (RandomMath.RollDice((Portal.HealthPercent * 100).Clamped(25, 75)))
                return false;

            if (Portal.System == null)
                ReturnToSpawnPos();
            else
                MoveToPos(Portal.System.Position.GenerateRandomPointOnCircle(10000 * Portal.HealthPercent.LowerBound(0.5f)));

            return true;
        }

        void JumpToEnemy()
        {
            float desiredRange = Portal.DesiredCombatRange;
            Ship nearest       = Portal.AI.Target;

            if (nearest != null && !nearest.Position.InRadius(Portal.Position, desiredRange))
            {
                int frontOrRear = RandomMath.RollDice((Portal.HealthPercent * 100).Clamped(25, 80)) ? 1 : -1;
                Vector2 pos = nearest.Position + frontOrRear * nearest.Position.DirectionToTarget(Portal.Position).Normalized() * (desiredRange + nearest.Radius);
                MoveToPos(pos);
            }
        }

        void ReturnToSpawnPos()
        {
            Vector2 systemPos = Portal.System?.Position 
                                ?? Owner.Universum.Systems.FindMin(s => s.Position.SqDist(Portal.Position)).Position;

            Vector2 desiredPos = Portal.Position = systemPos + TetherOffset;
            if (!Portal.Position.InRadius(desiredPos, 1000))
                MoveToPos(desiredPos);
        }

        void MoveToPos(Vector2 pos)
        {
            Portal.Position = pos;
            Portal.EmergeFromPortal();
        }

        GoalStep CallGuardians()
        {
            Remnants.CallGuardians(Portal);
            TetherOffset = Portal.System.Position.DirectionToTarget(Portal.Position).Normalized()
                           * Portal.System.Position.Distance(Portal.Position);

            return GoalStep.GoToNextStep;
        }

        GoalStep GenerateProduction()
        {
            if (Portal == null || !Portal.Active)
                return GoalStep.GoalFailed;

            Remnants.OrderEscortPortal(Portal);
            UpdatePosition();
            if (Portal.System != null)
            {
                float production = Owner.Universum.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
                if (Portal.InCombat && Portal.AI.Target?.System == Portal.System)
                    production /= 2;

                production *= Owner.DifficultyModifiers.RemnantResourceMod;
                production *= (int)(UState.GalaxySize + 1) * 2 * UState.StarsModifier / UState.MajorEmpires.Length;
                Remnants.TryGenerateProduction(production);
            }

            return GoalStep.TryAgain;
        }
    }
}