using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game.AI.ExpansionAI
{
    [StarDataType]
    public class MiningOpsPlanner
    {
        [StarData] readonly Empire Owner;
        [StarData] int TurnTimer;


        UniverseState Universe => Owner.Universe;
        [StarDataConstructor] MiningOpsPlanner() { }

        public MiningOpsPlanner(Empire empire)
        {
            Owner = empire;
            TurnTimer = empire.DifficultyModifiers.MiningOpsTurnsPerRun + empire.Id;
        }
        InfluenceStatus Influense(Vector2 pos) => Owner.Universe.Influence.GetInfluenceStatus(Owner, pos);

        /// <summary>
        /// This will check relevant mineable planets and set goals to deploy
        /// Mining Stations, based on diplomacy situation and personality
        /// ignoreDistance is used for testing
        /// </summary>
        /// 
        public void RunMiningOpsPlanner(bool ignoreDistance = false)
        {
            if (!ShouldRunMiningOpsPlanner())
                return;

            Planet[] potentialPlanets = GetPotentialMineablePlanets(ignoreDistance);
            Goal[] miningGoals = Owner.AI.FindGoals(g => g is MiningOps);
            foreach (Planet mineable in potentialPlanets)
            {
                if (ProcessMinables(mineable, Influense(mineable.Position), miningGoals))
                    return;
            }
        }

        bool ProcessMinables(Planet planet, InfluenceStatus influense, Goal[] miningGoals)
        {
            if (influense == InfluenceStatus.Enemy
                || !planet.System.InSafeDistanceFromRadiation(planet.Position)
                || planet.Mining.OpsOwnedBySomeoneElseThan(Owner))
            {
                return false;
            }

            float enemyStr = Owner.KnownEnemyStrengthNoResearchStationsIn(planet.System);
            if (enemyStr > 0)
            {
                if (Owner.isPlayer || Owner.HasWarTaskTargetingSystem(planet.System))
                    return false;

                Owner.AddDefenseSystemGoal(planet.System, Owner.KnownEnemyStrengthIn(planet.System), AI.Tasks.MilitaryTaskImportance.Normal);
                return false;
            }

            int numPlanetMiningGoals = miningGoals.Count(g => g.IsMiningOpsGoal(planet));
            if ((!planet.Mining.HasOpsOwner || planet.Mining.Owner == Owner) && numPlanetMiningGoals == 0 
                || Owner.NeedMoreMiningOpsOfThis(planet.Mining.ExoticBonusType) && numPlanetMiningGoals < Mineable.MaximumMiningStations)
            {
                Owner.AI.AddGoalAndEvaluate(new MiningOps(Owner, planet));
                return true;
            }

            return false;
        }

        bool ShouldRunMiningOpsPlanner()
        {
            if (Universe.P.DisableMiningOps
                || !Owner.CanBuildMiningStations
                || Owner.isPlayer && !Owner.AutoBuildMiningStations)
            {
                return false;
            }

            if (--TurnTimer > 0 )
                return false;

            TurnTimer = Owner.DifficultyModifiers.MiningOpsTurnsPerRun;
            return true;
        }

        Planet[] GetPotentialMineablePlanets(bool ignoreDistance)
        {
            Array<Planet> mineables = new();
            float averageDist = Owner.AverageSystemsSqdistFromCenter;
            for (int i = 0; i < Universe.MineablePlanets.Count; i++)
            {
                Planet planet = Universe.MineablePlanets[i];
                if (planet.IsExploredBy(Owner)
                    && (!planet.Mining.HasOpsOwner || planet.Mining.OpsOwnedByEmpire(Owner))
                    && (ignoreDistance || HelperFunctions.InGoodDistanceForReseachOrMiningOps(Owner, planet.System, averageDist, Influense(planet.System.Position)))
                    && (Owner.Universe.Remnants == null
                        || !Owner.Universe.Remnants.AI.HasGoal(g => g is RemnantPortal && g.TargetShip.System == planet.System)))
                {
                    mineables.Add(planet);
                }
            }

            return mineables.ToArray();
        }
    }
}

