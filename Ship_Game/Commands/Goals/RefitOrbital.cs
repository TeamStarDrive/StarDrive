using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class RefitOrbital : Goal
    {
        public const string ID = "RefitOrbital";
        public override string UID => ID;

        public RefitOrbital(int id, UniverseState us)
            : base(GoalType.RefitOrbital, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                FindOrbitalAndPlanetToRefit,
                BuildNewShip,
                WaitForShipBuilt,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        public RefitOrbital(Ship oldShip, string toBuildName, Empire owner)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            OldShip    = oldShip;
            ShipLevel  = oldShip.Level;
            ToBuildUID = toBuildName;
            empire     = owner;

            if (oldShip.VanityName != oldShip.Name)
                VanityName = oldShip.VanityName;

            Evaluate();
        }

        GoalStep FindOrbitalAndPlanetToRefit()
        {
            if (ToBuildUID == null || !GetNewOrbital(out Ship newOrbital))
                return GoalStep.GoalFailed;  // No better orbital is available

            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();

            if (!empire.FindPlanetToRefitAt(empire.SafeSpacePorts, OldShip.RefitCost(newOrbital), newOrbital, out PlanetBuildingAt))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }

            OldShip.ClearFleet(returnToManagedPools: false, clearOrders: true);
            OldShip.AI.State = AIState.Refit;
            Planet targetPlanet = OldShip.GetTether();
            if (targetPlanet != null)
                TetherPlanetId = targetPlanet.Id;

            return GoalStep.GoToNextStep;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            if (ToBuildUID == null || !GetNewOrbital(out Ship newOrbital))
                return GoalStep.GoalFailed;  // No better orbital is available

            string constructorId = empire.data.ConstructorShip;
            if (!ResourceManager.Ships.GetDesign(constructorId, out ShipToBuild))
            {
                if (!ResourceManager.Ships.GetDesign(empire.data.DefaultConstructor, out ShipToBuild))
                {
                    Log.Error($"BuildOrbital: no construction ship with uid={constructorId}");
                    return GoalStep.GoalFailed;
                }
            }

            PlanetBuildingAt.Construction.Enqueue(newOrbital, ShipToBuild, OldShip.RefitCost(newOrbital), this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeployOrbital()
        {
            if (FinishedShip == null || !FinishedShip.Active)
                return GoalStep.GoalFailed; // Ship was removed or destroyed

            if (!OldShipOnPlan)
            {
                FinishedShip.AI.OrderScrapShip();
                return GoalStep.GoalFailed; // Ship was removed or destroyed
            }

            Planet targetPlanet = OldShip.GetTether();
            if (targetPlanet != null)
            {
                Vector2 dirToOrbital = targetPlanet.Position.DirectionToTarget(OldShip.Position);
                float disToOrbital   = targetPlanet.Position.Distance(OldShip.Position);
                TetherOffset         = dirToOrbital.Normalized() * disToOrbital;
            }
            else
            {
                BuildPosition = OldShip.Position;
            }

            FinishedShip.AI.OrderDeepSpaceBuild(this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (FinishedShip != null)
                return GoalStep.TryAgain;

            if (OldShip != null && OldShip.Active)
                OldShip.AI.ClearOrders(); // Constructor was maybe destroyed

            return GoalStep.GoalComplete;
        }

        bool OldShipOnPlan
        {
            get
            {
                if (OldShip == null)
                    return false; // Ship was removed from game, probably destroyed

                return OldShip.DoingRefit;
            }
        }

        void RemoveOldRefitGoal()
        {
            if (OldShip.AI.FindGoal(ShipAI.Plan.Refit, out _))
                OldShip.Loyalty.GetEmpireAI().FindAndRemoveGoal(GoalType.Refit, g => g.OldShip == OldShip);
        }

        bool GetNewOrbital(out Ship newOrbital)
        {
            newOrbital = ResourceManager.GetShipTemplate(ToBuildUID, false);
            return newOrbital != null;
        }
    }
}
