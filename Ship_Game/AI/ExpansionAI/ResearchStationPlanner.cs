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
        [StarData] int ExpansionIntervalTimer = 100_000; // how often to check for expansion?
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
        /// </summary>
        /// 
        public void RunResearchStationPlanner()
        {
            if (!ShouldRunResearchMananger())
                return;

            ExplorableGameObject[] potentialExplorables = GetPotentialResearchableSolarBodies();
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

            float str = Owner.KnownEnemyStrengthIn(system);
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

        bool ShouldRunResearchMananger()
        {
            if (!Owner.CanBuildResearchStations || Owner.isPlayer && !Owner.AutoBuildResearchStations)
                return false;

            if (++ExpansionIntervalTimer < Owner.DifficultyModifiers.ExpansionCheckInterval)
                return false;

            ExpansionIntervalTimer = 0;
            return true;
        }

        ExplorableGameObject[] GetPotentialResearchableSolarBodies()
        {
            Array<ExplorableGameObject> solarBodies = new();
            foreach (ExplorableGameObject solarBody in  Universe.ResearchableSolarBodies.Keys)
            {
                if (solarBody.IsExploredBy(Owner)
                    && !solarBody.IsResearchStationDeployedBy(Owner) // this bit is for performance - faster than HasGoal
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
