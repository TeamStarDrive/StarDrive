using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game.AI.ExpansionAI
{
    [StarDataType]
    public class ResearchStationPlanner
    {
        [StarData] readonly Empire Owner;


        UniverseState Universe => Owner.Universe;
        [StarDataConstructor] ResearchStationPlanner() { }

        public ResearchStationPlanner(Empire empire)
        {
            Owner = empire;
        }
        InfluenceStatus Influense(Vector2 pos) => Owner.Universe.Influence.GetInfluenceStatus(Owner, pos);

        /// <summary>
        /// This will check relevant researchable planets/stars and set goals to deploy
        /// research stations, based on diplomacy situation and personality
        /// ignoreDistance is used for testing
        /// </summary>
        /// 
        public void RunResearchStationPlanner(bool ignoreDistance = false)
        {
            if (!ShouldRunResearchManager())
                return;

            ExplorableGameObject[] potentialExplorables = GetPotentialResearchableSolarBodies(ignoreDistance);
            foreach (ExplorableGameObject researchable in potentialExplorables) 
            {
                ProcessReserchable(researchable, Influense(researchable.Position));
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

        bool ShouldRunResearchManager()
        {
            if (Universe.P.DisableResearchStations
                ||(Owner.Universe.StarDate % 1).Greater(0)
                || !Owner.CanBuildResearchStations
                || Owner.isPlayer && !Owner.AutoBuildResearchStations)
            {
                return false;
            }

            return true;
        }

        ExplorableGameObject[] GetPotentialResearchableSolarBodies(bool ignoreDistance)
        {
            Array<ExplorableGameObject> solarBodies = new();
            float averageDist = Owner.AverageSystemsSqdistFromCenter;
            foreach (ExplorableGameObject solarBody in Universe.ResearchableSolarBodies.Keys)
            {
                SolarSystem system = solarBody.System ?? solarBody as SolarSystem;
                if (solarBody.IsExploredBy(Owner) 
                    && !solarBody.IsResearchStationDeployedBy(Owner) // this bit is for performance - faster than HasGoal
                    && (ignoreDistance || HelperFunctions.InGoodDistanceForReseachOrMiningOps(Owner, system, averageDist, Influense(system.Position)))
                    && !Owner.AI.HasGoal(g => g.IsResearchStationGoal(solarBody))
                    && (Owner.Universe.Remnants == null 
                        || !Owner.Universe.Remnants.AI.HasGoal(g => g is RemnantPortal && g.TargetShip.System == solarBody)))
                {
                    solarBodies.Add(solarBody);
                }
            }

            return solarBodies.ToArray();
        }
    }
}
