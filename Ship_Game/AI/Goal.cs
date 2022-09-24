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
        MarkForColonization,
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
        public UniverseState UState; // automatically set during OnDeserialize evt
        [StarData] public Empire Owner; // the empire which owns this Goal
        [StarData] public float StarDateAdded;
        [StarData] public GoalType Type;
        [StarData] public int Step { get; private set; }
        public string StepName => Steps[Step].Method.Name;

        [StarData] public Planet PlanetBuildingAt;

        [StarData] Ship ShipBuilt; // this is the actual ship that was built
        [StarData] public Ship TargetShip;      // this is targeted by this goal (raids)
        [StarData] public Empire TargetEmpire; // Empire target of this goal (for instance, pirate goals)

        [StarData] protected bool MainGoalCompleted;
        protected Func<GoalStep>[] Steps = Empty<Func<GoalStep>>.Array;
        protected Func<bool> Holding;

        public virtual Planet TargetPlanet { get; set; }
        public virtual IShipDesign ToBuild => null; // this is the ship to be built
        public virtual Ship OldShip { get; set; } // this is the ship which needs refit
        public virtual bool IsDeploymentGoal => false;
        public virtual Vector2 MovePosition => BuildPosition;
        public virtual Vector2 BuildPosition => Vector2.Zero;
        public virtual bool IsRaid => false; // Is this goal a pirate raid?

        public Ship FinishedShip
        {
            get
            {
                // in case a ship gets destroyed while en route to final deployment
                if (ShipBuilt?.Active != true)
                    ShipBuilt = null;
                return ShipBuilt;
            }
            set => ShipBuilt = value;
        }
        
        /////////////////////////////////////////////////////
        /// --- Virtual accessors for filtering Goals --- ///

        /** @return True if this goal is a type of refit goal */
        public virtual bool IsRefitGoalAtPlanet(Planet planet) => false; // Implement at relevant classes
        /** @return True if this goal is targeting the given planet for colonization */
        public virtual bool IsColonizationGoal(Planet planet) => false;
        /** @return True if this goal is Remnants targeting this planet */
        public virtual bool IsRemnantEngageAtPlanet(Planet planet) => false;

        /** @return True if this is a WarMission goal targeting this planet */
        public virtual bool IsWarMissionTarget(Planet planet) => false;
        /** @return True if this is a WarMission goal targeting this empire */
        public virtual bool IsWarMissionTarget(Empire empire) => false;

        /////////////////////////////////////////////////////

        public string TypeName => GetType().GetTypeName();
        public override string ToString() => $"{Type} Goal.{TypeName}";

        [StarDataConstructor]
        protected Goal(GoalType type, Empire owner)
        {
            Type = type;
            if (owner != null) // owner is null during initial serialization
            {
                Owner = owner;
                UState = owner?.Universum;
                StarDateAdded = owner.Universum.StarDate;
            }
        }

        [StarDataDeserialized]
        protected void OnDeserialized(UniverseState us)
        {
            UState = us;
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"Deserialize {Type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                Step = Steps.Length - 1;
            }
        }

        protected GoalStep DummyStepTryAgain() => GoalStep.TryAgain;

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
        public float LifeTime => UState.StarDate - StarDateAdded;
        public bool IsMainGoalCompleted => MainGoalCompleted;

        // @note Goals are mainly evaluated during Empire update
        public GoalStep Evaluate()
        {
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{Type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
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
            Owner.AI.RemoveGoal(this);
        }

        public void AdvanceToNextStep()
        {
            ++Step;
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{Type} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal();
            }
        }

        public void ChangeToStep(Func<GoalStep> step)
        {
            if (!Steps.Contains(step))
            {
                Log.Error($"{Type} invalid Goal.Step: {step}, Steps.Length: {Steps.Length}");
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
