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
            if (Portal.InCombat && Portal.System != null && RandomMath.RollDice(50))
                JumpToEnemy();
            else
                ReturnToSpawnPos();
        }

        void JumpToEnemy()
        {
            float desiredRange = Portal.DesiredCombatRange;
            Ship farthest      = Portal.System?.ShipList.FindMaxFiltered(s => s != null 
                                                                            && s.loyalty != empire 
                                                                            && s.GetStrength() > 100
                                                                            && !s.IsInWarp, s => s.Center.Distance(Portal.Center));

            if (farthest!= null && !farthest.Center.InRadius(Portal.Center, desiredRange))
            {
                Vector2 pos = farthest.Center + farthest.Center.DirectionToTarget(Portal.Center).Normalized() * (desiredRange + farthest.Radius);
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
            if (!Portal.InCombat)
            {
                float production = Empire.Universe.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
                production *= empire.DifficultyModifiers.RemnantResourceMod;
                production *= (int)(CurrentGame.GalaxySize + 1) * 2 * CurrentGame.StarsModifier / EmpireManager.MajorEmpires.Length;
                Remnants.TryGenerateProduction(production);
            }

            return GoalStep.TryAgain;
        }
    }
}