using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

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
        /// <summary>
        /// This will check relevant researchable planets/stars and set goals to deploy
        /// research stations, based on diplomacy situation
        /// </summary>
        public void RunResearchStationPlanner()
        {
            if (!ShouldRunResearchMananger())
                return;

            ExplorableGameObject[] potentialExplorables = GetPotentialResearchableSolarBodies();
            foreach (ExplorableGameObject researchable in potentialExplorables) 
            {
                if (researchable is Planet planet)
                    ProcessReserchable(planet);
                else
                    ProcessReserchable(researchable as SolarSystem);
            }
        }

        void ProcessReserchable(SolarSystem system)
        {
            if (system.HostileForcesPresent(Owner))
                return; // TODO set a wargoal if at war depending on distance and borders

            Owner.AI.AddGoalAndEvaluate(new ProcessResearchStation(Owner, system, system.SelectStarResearchStationPos()));
        }

        void ProcessReserchable(Planet planet)
        {
            if (planet.System.HostileForcesPresent(Owner))
                return; // TODO set a wargoal if at war depending on distance and borders

            Owner.AI.AddGoalAndEvaluate(new ProcessResearchStation(Owner, planet));
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
                if (!solarBody.IsResearchStationDeployedBy(Owner) // this bit is for performance - faster than HasGoal
                    && !Owner.AI.HasGoal(g => g.IsResearchStationGoal(solarBody)))
                {
                    solarBodies.Add(solarBody);
                }
            }

            return solarBodies.ToArray();
        }
    }
}
