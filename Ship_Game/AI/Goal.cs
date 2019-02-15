// Type: Ship_Game.Goal
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using System;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public enum GoalType
    {
        Colonize,
        DeepSpaceConstruction,
        BuildTroop,
        BuildShips,
        BuildScout,
        FleetRequisition
    }

    public enum GoalStep
    {
        GoToNextStep, // this step succeeded, go to next step
        TryAgain,     // goal step failed, so we should try this step again
        GoalComplete, // entire goal is complete and should be removed!
        RestartGoal,  // restart entire goal from scratch (step 0)
        GoalFailed    // abort, abort!!
    }

    public abstract class Goal
    {
        public Guid guid = Guid.NewGuid();
        public Empire empire;
        public GoalType type;
        public int Step { get; private set; }
        public Fleet Fleet;
        public Vector2 TetherOffset;
        public Guid TetherTarget;
        public bool Held;
        public Vector2 BuildPosition;
        public string ToBuildUID;
        public Planet PlanetBuildingAt;
        public Planet ColonizationTarget { get; set; }
        public Ship ShipToBuild;  // this is a template
        public Ship FinishedShip; // this is the actual ship that was built
        public string StepName => Steps[Step].Method.Name;
        protected bool MainGoalCompleted;
        protected Func<GoalStep>[] Steps = Empty<Func<GoalStep>>.Array;
        protected Func<bool> Holding;

        public abstract string UID { get; }
        public override string ToString() => $"{type} Goal.{UID} {ToBuildUID}";

        private static Goal CreateInstance(string uid)
        {
            switch (uid)
            {
                case BuildConstructionShip.ID:  return new BuildConstructionShip();
                case BuildDefensiveShips.ID:    return new BuildDefensiveShips();
                case BuildOffensiveShips.ID:    return new BuildOffensiveShips();
                case BuildScout.ID:             return new BuildScout();
                case BuildTroop.ID:             return new BuildTroop();
                case FleetRequisition.ID:       return new FleetRequisition();
                case IncreaseFreighters.ID:     return new IncreaseFreighters();
                case MarkForColonization.ID:    return new MarkForColonization();
                default: throw new ArgumentException($"Unrecognized Goal UID: {uid}");
            }
        }

        public static Goal Deserialize(string uid, Empire e, SavedGame.GoalSave gsave)
        {
            Goal g = CreateInstance(uid);
            g.empire        = e;
            g.ToBuildUID    = gsave.ToBuildUID;
            g.Step          = gsave.GoalStep;
            g.guid          = gsave.GoalGuid;
            g.BuildPosition = gsave.BuildPosition;
            return g;
        }

        protected Goal(GoalType type)
        {
            this.type = type;
        }

        protected GoalStep DummyStepTryAgain()     => GoalStep.TryAgain;
        protected GoalStep DummyStepGoalComplete() => GoalStep.GoalComplete;
        protected GoalStep WaitMainGoalCompletion()
        {
            if (MainGoalCompleted)
            {
                MainGoalCompleted = false;
                if (Step == Steps.Length-1)
                    return GoalStep.GoalComplete;
                return GoalStep.GoToNextStep;
            }
            return GoalStep.TryAgain;
        }

        public void NotifyMainGoalCompleted()
        {
            MainGoalCompleted = true;
        }

        // @note Goals are mainly evaluated during Empire update
        public GoalStep Evaluate()
        {
            // CG hrmm i guess this should just be part of the goal enum.
            // But that will require more cleanup of the goals. 
            if (Holding?.Invoke() == true) 
                return GoalStep.TryAgain;
            
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal(); // don't crash, just remove the step
                return GoalStep.GoalFailed;
            }

            GoalStep result = Steps[Step].Invoke();
            switch (result)
            {
                case GoalStep.GoToNextStep: ++Step; break;
                case GoalStep.TryAgain:             break;
                case GoalStep.GoalComplete:
                case GoalStep.GoalFailed:
                    RemoveThisGoal();
                    break;
                case GoalStep.RestartGoal:
                    Step = 0;
                    break;
            }
            return result;
        }

        void RemoveThisGoal()
        {
            empire?.GetEmpireAI().Goals.QueuePendingRemoval(this);
        }

        public void AdvanceToNextStep()
        {
            ++Step;
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal();
            }
        }

        public void ReportShipComplete(Ship ship)
        {
            FinishedShip = ship;
            AdvanceToNextStep();
        }

        public struct PlanetRanker
        {
            public Planet Planet;
            public float Value;
            public float Distance;
            public float JumpRange;
            public bool OutOfRange;
            public bool CantColonize;            

            public PlanetRanker(Empire empire, Planet planet, bool canColonizeBarren, Vector2 empireCenter, float enemyStr)
            {
                int commodities        = planet.Storage.CommoditiesCount;
                Distance               = empireCenter.Distance(planet.Center);                
                CantColonize           = IsBadWorld(planet, canColonizeBarren, commodities);
                
                float distanceInJumps  = Math.Max(Distance / 600000, 1);
                JumpRange              = distanceInJumps;
                Planet                 = planet;

                float baseValue        = planet.EmpireBaseValue(empire);
                Value                  = baseValue / distanceInJumps;
                OutOfRange             = PlanetToFarToColonize(planet, empire);

                if (Value < 0.3f)
                    CantColonize = true;
                
                if (enemyStr > 0)
                    Value *= (empire.currentMilitaryStrength - enemyStr) / empire.currentMilitaryStrength;
            }

            private static bool IsBadWorld(Planet planetList, bool canColonizeBarren, int commodities) =>
                planetList.IsBarrenType
                && !canColonizeBarren && commodities == 0;

            private static bool PlanetToFarToColonize(Planet planetList, Empire empire)
            {
                AO closestAO = empire.GetEmpireAI().AreasOfOperations
                    .FindMin(ao => ao.Center.SqDist(planetList.Center));
                if (closestAO != null && planetList.Center.OutsideRadius(closestAO.Center, closestAO.Radius * 1.5f))
                    return true;
                return false;
            }

        }
    }
}
