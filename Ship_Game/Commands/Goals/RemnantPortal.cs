using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantPortal : Goal
    {
        public const string ID = "RemnantPortal";
        public override string UID => ID;
        private Remnants Remnants;
        private Ship Portal;

        public RemnantPortal() : base(GoalType.RemnantPortal)
        {
            Steps = new Func<GoalStep>[]
            {
                CallGuardians,
                GenerateProduction
            };
        }

        public RemnantPortal(Empire owner, Ship portal, string systemName) : this()
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
            if (Portal.HealthPercent < 0.9f)
                LureEnemy();

            int roll = Portal.AI.Target?.System == Portal.System ? 50 : 5;
            if (Portal.AI.Target != null && Portal.System != null && RandomMath.RollDice(roll))
                JumpToEnemy();
            else
                ReturnToSpawnPos();
        }

        void LureEnemy()
        {
            if (!RandomMath.RollDice(25))
                return;

            if (Portal.System == null)
                ReturnToSpawnPos();
            else
                MoveToPos(Portal.System.Position.GenerateRandomPointOnCircle(10000));
        }

        void JumpToEnemy()
        {
            float desiredRange = Portal.DesiredCombatRange;
            Ship nearest       = Portal.AI.Target;

            if (nearest != null && !nearest.Center.InRadius(Portal.Center, desiredRange))
            {
                Vector2 pos = nearest.Center + nearest.Center.DirectionToTarget(Portal.Center).Normalized() * (desiredRange + nearest.Radius);
                MoveToPos(pos);
            }
        }

        void ReturnToSpawnPos()
        {
            if (TetherOffset == Vector2.Zero)
                return; // save support - can be removed in 2021

            Vector2 systemPos = Portal.System?.Position 
                                ?? Empire.Universe.SolarSystemDict.Values.ToArray().FindMin(s => s.Position.SqDist(Portal.Center)).Position;

            Vector2 desiredPos = Portal.Position = systemPos + TetherOffset;
            if (!Portal.Center.InRadius(desiredPos, 1000))
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
            TetherOffset = Portal.System.Position.DirectionToTarget(Portal.Center).Normalized()
                           * Portal.System.Position.Distance(Portal.Center);

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
                float production = Empire.Universe.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
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