using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using System.Linq;
using Ship_Game.AI.Tasks;

namespace Ship_Game
{
    public partial class Empire
    {
        public bool GetPotentialTargetPlanets(Empire enemy, WarType warType, out Array<Planet> targetPlanets)
        {
            targetPlanets = new Array<Planet>();
            switch (warType)
            {
                case WarType.GenocidalWar:
                case WarType.ImperialistWar: targetPlanets = enemy.GetPlanets().ToArrayList();          break;
                case WarType.BorderConflict: targetPlanets = PotentialPlanetTargetsBorderWar(enemy);    break;
                case WarType.DefensiveWar:   targetPlanets = PotentialPlanetTargetsDefensiveWar(enemy); break;
            }

            return targetPlanets.Count > 0;
        }

        Array<Planet> PotentialPlanetTargetsBorderWar(Empire enemy)
        {
            Array<Planet> targetPlanets = new Array<Planet>();
            var potentialPlanets = enemy.GetPlanets().Filter(p => p.ParentSystem.HasPlanetsOwnedBy(this));
            if (potentialPlanets.Length > 0)
            {
                var tasks = EmpireAI.GetTasks();
                foreach (Planet planet in potentialPlanets)
                {
                    if (!tasks.Any(t => t.TargetEmpire == enemy && t.TargetPlanet == planet))
                        targetPlanets.Add(planet);
                }
            }

            return targetPlanets;
        }

        Array<Planet> PotentialPlanetTargetsDefensiveWar(Empire enemy)
        {
            Array<SolarSystem> potentialSystems = new Array<SolarSystem>();
            var theirSystems = enemy.GetOwnedSystems();
            foreach (SolarSystem system in OwnedSolarSystems)
            {
                if (system.FiveClosestSystems.Any(s => theirSystems.Contains(s)))
                    potentialSystems.AddUnique(system);
            }

            Array<Planet> targetPlanets = new Array<Planet>();
            foreach (SolarSystem system in potentialSystems)
            {
                var potentialPlanets = system.PlanetList.Filter(p => p.Owner == enemy);
                targetPlanets.AddRange(potentialPlanets);

            }

            return targetPlanets;
        }

        public Planet[] SortPlanetTargets(Array<Planet> targets, WarType warType, Empire enemy)
        {
            switch (warType)
            {
                default:
                case WarType.BorderConflict: return targets.SortedDescending(p => p.ColonyPotentialValue(this));
                case WarType.DefensiveWar:   return targets.Sorted(p => p.Center.SqDist(WeightedCenter));
                case WarType.ImperialistWar: return targets.SortedDescending(p => p.Center.SqDist(WeightedCenter) * p.ColonyPotentialValue(this));
                case WarType.GenocidalWar:   return targets.SortedDescending(p => p.Center.SqDist(WeightedCenter) * p.ColonyPotentialValue(enemy));
            }
        }

        public void CreateWarTask(Planet targetPlanet, Empire enemy)
        {
            MilitaryTask.TaskType taskType = MilitaryTask.TaskType.StrikeForce;
            if (IsAlreadyStriking())
            {
                if (canBuildBombers
                     && !IsAlreadyGlassingPlanet(targetPlanet)
                     && (targetPlanet.Population < 1
                         || targetPlanet.ColonyPotentialValue(enemy) / targetPlanet.ColonyPotentialValue(this) > PersonalityModifiers.DoomFleetThreshold))
                {
                    taskType = MilitaryTask.TaskType.GlassPlanet;
                }
                else
                {
                    taskType = MilitaryTask.TaskType.AssaultPlanet;
                }
            }

            MilitaryTask task = (new MilitaryTask(targetPlanet, this)
            {
                Priority = 5,
                type = taskType
            });

            EmpireAI.AddPendingTask(task);
        }

        bool IsAlreadyStriking()
        {
            return EmpireAI.GetTasks().Any(t => t.type == MilitaryTask.TaskType.StrikeForce);
        }

        bool IsAlreadyGlassingPlanet(Planet planet)
        {
            return EmpireAI.GetTasks().Any(t => t.type == MilitaryTask.TaskType.GlassPlanet && t.TargetPlanet == planet);
        }

        public bool CanAddAnotherWarGoal(Empire enemy)
        {
            return EmpireAI.Goals
                .Filter(g => g.IsWarMission && g.TargetEmpire == enemy).Length <= DifficultyModifiers.NumWarTasksPerWar;
        }

        public bool TryGetMissionsVsEmpire(Empire enemy, out Goal[] goals)
        {
            goals = EmpireAI.Goals.Filter(g => g.IsWarMission && g.TargetEmpire == enemy);
            return goals.Length > 0;
        }

        public Goal[] GetDefendSystemsGoal()
        {
            return EmpireAI.Goals.Filter(g => g.type == GoalType.DefendSystem);
        }

        public bool NoEmpireDefenseGoal()
        {
            return !EmpireAI.Goals.Any(g => g.type == GoalType.EmpireDefense);
        }

        public void AddDefenseSystemGoal(SolarSystem system, int priority, float strengthWanted, int fleetCount)
        {
            EmpireAI.Goals.Add(new DefendSystem(this, system, priority, strengthWanted, fleetCount));
        }

        public bool IsAlreadyDefendingSystem(SolarSystem system)
        {
            return EmpireAI.Goals.Any(g => g.type == GoalType.DefendSystem);
        }
    }
}
