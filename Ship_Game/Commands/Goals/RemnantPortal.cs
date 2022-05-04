using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    public class RemnantPortal : Goal
    {
        public const string ID = "RemnantPortal";
        public override string UID => ID;
        private Remnants Remnants;
        private Ship Portal;

        public RemnantPortal(int id, UniverseState us)
            : base(GoalType.RemnantPortal, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                CallGuardians,
                GenerateProduction
            };
        }

        public RemnantPortal(Empire owner, Ship portal, string systemName)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire     = owner;
            TargetShip = portal;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Portal in {systemName} ----");
        }

        public sealed override void PostInit()
        {
            Remnants = empire.Remnants;
            Portal   = TargetShip;
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
            if (TetherOffset == Vector2.Zero)
                return; // save support - can be removed in 2021

            Vector2 systemPos = Portal.System?.Position 
                                ?? empire.Universum.Systems.FindMin(s => s.Position.SqDist(Portal.Position)).Position;

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
                float production = empire.Universum.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
                if (Portal.InCombat && Portal.AI.Target?.System == Portal.System)
                    production /= 2;

                production *= empire.DifficultyModifiers.RemnantResourceMod;
                production *= (int)(CurrentGame.GalaxySize + 1) * 2 * CurrentGame.StarsModifier / EmpireManager.MajorEmpires.Length;
                Remnants.TryGenerateProduction(production);
            }

            return GoalStep.TryAgain;
        }
    }
}