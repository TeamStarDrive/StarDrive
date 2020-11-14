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
            if (Portal.InCombat && Portal.System != null)
                JumpToEnemy();
            else
                ReturnToSpawnPos();
        }

        void JumpToEnemy()
        {
            float desiredRange = Portal.DesiredCombatRange;
            Ship nearest       = Portal.System.ShipList.FindMinFiltered(s => s != null 
                                                                             && s.loyalty != empire 
                                                                             && s.GetStrength() > 100
                                                                             && !s.IsInWarp, s => s.Center.Distance(Portal.Center));

            if (nearest!= null && !nearest.Center.InRadius(Portal.Center, desiredRange))
            {
                Vector2 pos = nearest.Center - nearest.Center.DirectionToTarget(Portal.Center).Normalized() * desiredRange;
                MoveToPos(pos);
            }
        }

        void ReturnToSpawnPos()
        {
            if (TetherOffset == Vector2.Zero)
                return; // save support - can be removed in 2021

            Vector2 desiredPos = Portal.Position = Portal.System.Position + TetherOffset;
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
            float production = Empire.Universe.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
            production      *= empire.DifficultyModifiers.RemnantResourceMod;
            production      *= (int)(CurrentGame.GalaxySize + 1) * 2 * CurrentGame.StarsModifier / EmpireManager.MajorEmpires.Length;
            Remnants.TryGenerateProduction(production);
            UpdatePosition();
            return GoalStep.TryAgain;
        }
    }
}