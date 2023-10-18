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


        UniverseState Universe => Owner.Universe;
        [StarDataConstructor] MiningOpsPlanner() { }

        public MiningOpsPlanner(Empire empire)
        {
            Owner = empire;
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
            foreach (Planet mineable in potentialPlanets)
            {
                ProcessReserchable(mineable, Influense(mineable.Position));
            }
        }

        void ProcessReserchable(ExplorableGameObject solarBody, InfluenceStatus influense)
        {
            if (influense == InfluenceStatus.Enemy)
                return; // Leave killing research stations to war logic

            SolarSystem system = solarBody.System ?? solarBody as SolarSystem;
            if (!system.HasPlanetsOwnedBy(Owner) && system.HasPlanetsOwnedByHostiles(Owner))
                return;

            Planet planet = solarBody as Planet;
            if (planet != null && !planet.System.InSafeDistanceFromRadiation(planet.Position))
                return;

            float str = Owner.KnownEnemyStrengthNoResearchStationsIn(system);
            if (str > 0)
            {
                TryClearArea(system, influense, Owner.AI.ThreatMatrix.GetStrongestHostileAt(system), str);
            }
            else
            {
                if (planet != null)
                    Owner.AI.AddGoalAndEvaluate(new ProcessResearchStation(Owner, planet));
                else
                    Owner.AI.AddGoalAndEvaluate(new ProcessResearchStation(Owner, system, system.SelectStarResearchStationPos()));
            }
        }

        void TryClearArea(SolarSystem system, InfluenceStatus influense, Empire enemy, float knownStr)
        {
            if (Owner.isPlayer || knownStr > Owner.OffensiveStrength * 0.334f)
                return;

            bool shouldClearArea = !Owner.PersonalityModifiers.ClearNeutralExoticSystems && influense == InfluenceStatus.Friendly
                || Owner.PersonalityModifiers.ClearNeutralExoticSystems && influense <= InfluenceStatus.Friendly
                || enemy.IsFaction;

            if (shouldClearArea && !Owner.HasWarTaskTargetingSystem(system))
                Owner.AddDefenseSystemGoal(system, Owner.KnownEnemyStrengthIn(system), Tasks.MilitaryTaskImportance.Normal);
        }

        bool ShouldRunMiningOpsPlanner()
        {
            if (Universe.P.DisableMiningOps
                || (Owner.Universe.StarDate % 1).Greater(0)
                || !Owner.CanBuildMiningStations
                || Owner.isPlayer && !Owner.AutoBuildMiningStations)
            {
                return false;
            }

            return true;
        }

        Planet[] GetPotentialMineablePlanets(bool ignoreDistance)
        {
            Array<Planet> mineables = new();
            float averageDist = Owner.AverageSystemsSqdistFromCenter;
            foreach (Planet planet in Universe.MineablePlanets)
            {
                if (planet.IsExploredBy(Owner)
                    && planet.Mining.Owner == null || planet.Mining.Owner == Owner
                    && (ignoreDistance || InGoodDistance(planet.System, averageDist))
                    && (Owner.Universe.Remnants == null
                        || !Owner.Universe.Remnants.AI.HasGoal(g => g is RemnantPortal && g.TargetShip.System == planet.System)))
                {
                    mineables.Add(planet);
                }
            }
            //|| Owner.GetOwnedSystems().Any(s => s.FiveClosestSystems.Any(s => s.HasPlanetsOwnedBy(Owner)))
            return mineables.ToArray();

            bool InGoodDistance(SolarSystem system, float averageDist)
            {
                return system.HasPlanetsOwnedBy(Owner)
                       || system.Position.SqDist(Owner.WeightedCenter) < averageDist * 1.5f
                       || system.FiveClosestSystems.Any(s => s.HasPlanetsOwnedBy(Owner))
                       || Influense(system.Position) == InfluenceStatus.Friendly;
            }
        }
    }
}

