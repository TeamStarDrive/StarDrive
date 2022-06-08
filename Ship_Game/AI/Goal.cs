// Type: Ship_Game.Goal
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public enum GoalType
    {
        Colonize,
        DeepSpaceConstruction,
        BuildTroop,
        BuildOffensiveShips,
        IncreaseFreighters,
        BuildScout,
        FleetRequisition,
        Refit,
        BuildOrbital,
        PirateAI,
        PirateDirectorPayment,
        PirateDirectorRaid,
        PirateBase,
        PirateRaidTransport,
        PirateRaidOrbital,
        RemnantPortal,
        PirateRaidCombatShip,
        PirateDefendBase,
        PirateProtection,
        AssaultPirateBase,
        RefitOrbital,
        DeployFleetProjector,
        ScrapShip,
        RearmShipFromPlanet,
        PirateRaidProjector,
        RemnantEngagements,
        RemnantEngageEmpire,
        RemnantInit,
        DefendVsRemnants,
        StandbyColonyShip,
        ScoutSystem,
        AssaultBombers,
        EmpireDefense,
        DefendSystem,
        WarManager,
        WarMission,
        PrepareForWar
    }

    public enum GoalStep
    {
        GoToNextStep, // this step succeeded, go to next step
        TryAgain,     // goal step failed, so we should try this step again
        GoalComplete, // entire goal is complete and should be removed!
        RestartGoal,  // restart entire goal from scratch (step 0)
        GoalFailed    // abort, abort!!
    }

    [StarDataType]
    public abstract class Goal
    {
        [StarData] public readonly int Id;
        public UniverseState UState;
        [StarData] public Empire empire;
        [StarData] public GoalType type;
        [StarData] public int Step { get; private set; }
        [StarData] public Fleet Fleet;
        [StarData] public Vector2 TetherOffset;
        [StarData] public int TetherPlanetId;
        [StarData] Vector2 StaticBuildPosition;
        [StarData] public string ToBuildUID;
        [StarData] public string VanityName;
        [StarData] public int ShipLevel;
        [StarData] public Planet PlanetBuildingAt;
        [StarData] public Planet ColonizationTarget { get; set; }

        public IShipDesign ShipToBuild;  // this is a template
        [StarData] ShipDesign Ship2Build
        {
            get => (ShipDesign)ShipToBuild;
            set => ShipToBuild = value;
        }

        [StarData] Ship ShipBuilt; // this is the actual ship that was built
        [StarData] public Ship OldShip;      // this is the ship which needs refit
        [StarData] public Ship TargetShip;      // this is targeted by this goal (raids)
        [StarData] public Empire TargetEmpire; // Empire target of this goal (for instance, pirate goals)
        [StarData] public Planet TargetPlanet;
        [StarData] public SolarSystem TargetSystem;
        [StarData] public float StarDateAdded;

        public string StepName => Steps[Step].Method.Name;

        [StarData] protected bool MainGoalCompleted;
        protected Func<GoalStep>[] Steps = Empty<Func<GoalStep>>.Array;
        protected Func<bool> Holding;

        public Vector2 MovePosition
        {
            get
            {
                Planet targetPlanet = GetTetherPlanet;
                targetPlanet = targetPlanet ?? ColonizationTarget;

                if (targetPlanet != null)
                    return targetPlanet.Position + TetherOffset;
                return BuildPosition;
            }
        }

        public Vector2 BuildPosition
        {
            get
            {
                if (GetTetherPlanet != null)
                    return GetTetherPlanet.Position + TetherOffset;
                return StaticBuildPosition;
            }
            set => StaticBuildPosition = value;
        }

        public Planet GetTetherPlanet => empire.Universum.GetPlanet(TetherPlanetId);

        public bool IsDeploymentGoal => ToBuildUID.NotEmpty() && !BuildPosition.AlmostZero();
        public abstract string UID { get; }

        public Ship FinishedShip
        {
            get
            {
                if (ShipBuilt?.Active != true)
                    ShipBuilt = null;
                return ShipBuilt;
            }
            set => ShipBuilt = value;
        }

        public override string ToString() => $"{type} Goal.{UID} {ToBuildUID}";

        protected Goal(GoalType type, int id, UniverseState us)
        {
            this.type = type;
            Id = id;
            UState = us;
        }

        [StarDataDeserialized]
        protected void OnDeserialized()
        {
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"Deserialize {type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                Step = Steps.Length - 1;
            }
        }

        // This is used to assign variables for better code readability in specific goals
        // after they are created or loaded from save
        public virtual void PostInit()
        {
        }

        public virtual bool IsRaid => false; // Is this goal a pirate raid?
        public virtual bool IsWarMission => false; // Is this goal related to war logic?

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

        protected GoalStep WaitForShipBuilt() 
        {
            // When the Ship is finished, the goal is moved externally to next step (ReportShipComplete).
            // So no need for GotoNextStep here.

            if (PlanetBuildingAt.ConstructionQueue.Filter(q => q.Goal == this).Length == 0 && FinishedShip == null)
                return GoalStep.GoalFailed;

            return GoalStep.TryAgain;
        }

        public void NotifyMainGoalCompleted()
        {
            MainGoalCompleted = true;
        }

        /// <summary>
        /// 1 is 10 turns, 5 is 50 turns
        /// </summary>
        public float LifeTime => empire.Universum.StarDate - StarDateAdded;
        public bool IsMainGoalCompleted => MainGoalCompleted;

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
            empire?.GetEmpireAI().Goals.Remove(this);
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

        public void ChangeToStep(Func<GoalStep> step)
        {
            if (!Steps.Contains(step))
            {
                Log.Error($"{type} invalid Goal.Step: {step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal();
            }
            Step = Steps.IndexOf(step);
        }

        public void ReportShipComplete(Ship ship)
        {
            FinishedShip = ship;
            AdvanceToNextStep();
        }
    }
}
