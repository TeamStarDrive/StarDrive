using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public partial class Empire
    {
        bool HasWarMissionTargeting(Planet planet)
        {
            return EmpireAI.Goals.Any(g => g.IsWarMission && g.TargetPlanet == planet);
        }

        public bool GetPotentialTargetPlanets(Empire enemy, WarType warType, out Planet[] targetPlanets)
        {
            targetPlanets = null;
            switch (warType)
            {
                case WarType.GenocidalWar:
                case WarType.ImperialistWar: targetPlanets = enemy.GetPlanets().Filter(p => !HasWarMissionTargeting(p)); break;
                case WarType.BorderConflict: targetPlanets = PotentialPlanetTargetsBorderWar(enemy);                     break;
                case WarType.DefensiveWar:   targetPlanets = PotentialPlanetTargetsDefensiveWar(enemy);                  break;
            }

            return targetPlanets?.Length > 0;
        }

        Planet[] PotentialPlanetTargetsBorderWar(Empire enemy)
        {
            var potentialPlanets = enemy.GetPlanets().Filter(p => p.ParentSystem.HasPlanetsOwnedBy(this)
                                                                  && !HasWarMissionTargeting(p));

            return potentialPlanets;
        }

        Planet[] PotentialPlanetTargetsDefensiveWar(Empire enemy)
        {
            Array<SolarSystem> potentialSystems = new Array<SolarSystem>();
            var theirSystems = enemy.GetOwnedSystems();
            foreach (SolarSystem system in OwnedSolarSystems)
            {
                if (system.FiveClosestSystems.Any(s => theirSystems.Contains(s)))
                    potentialSystems.AddUnique(system);

                if (system.HasPlanetsOwnedBy(this))
                    potentialSystems.AddUnique(system);
            }

            Array<Planet> targetPlanets = new Array<Planet>();
            foreach (SolarSystem system in potentialSystems)
            {
                var potentialPlanets = system.PlanetList.Filter(p => p.Owner == enemy && !HasWarMissionTargeting(p));
                targetPlanets.AddRange(potentialPlanets);
            }

            return targetPlanets.ToArray();
        }

        public Planet[] SortPlanetTargets(Planet[] targets, WarType warType, Empire enemy)
        {
            switch (warType)
            {
                default:
                case WarType.BorderConflict: return targets.SortedDescending(p => p.ColonyPotentialValue(this));
                case WarType.DefensiveWar:   return targets.Sorted(p => p.Position.SqDist(WeightedCenter));
                case WarType.ImperialistWar: return targets.SortedDescending(p => p.ColonyPotentialValue(this) / p.Position.Distance(WeightedCenter));
                case WarType.GenocidalWar:   return targets.SortedDescending(p => p.ColonyPotentialValue(enemy) / p.Position.Distance(WeightedCenter));
            }
        }

        public bool TryConfirmPrepareForWarType(Empire enemy, WarType warType, out WarType finalWarType)
        {
            finalWarType = warType;
            if (GetPotentialTargetPlanets(enemy, warType, out _))
                return true;

            finalWarType = GetWarEscalation(warType);
            return GetPotentialTargetPlanets(enemy, finalWarType, out _);
        }

        public WarType GetWarEscalation(WarType warType)
        {
            switch (warType)
            {
                case WarType.BorderConflict: return WarType.DefensiveWar;
                case WarType.DefensiveWar:   return WarType.ImperialistWar;
                default:                     return warType;
            }
        }

        public void CreateStageFleetTask(Planet targetPlanet, Empire enemy, Goal goal)
        {
            MilitaryTask task = new MilitaryTask(targetPlanet, this)
            {
                Type     = MilitaryTask.TaskType.StageFleet,
                Goal     = goal
            };

            EmpireAI.AddPendingTask(task);
        }

        public void CreateWarTask(Planet targetPlanet, Empire enemy, Goal goal)
        {
            // todo advanced mission types per personality or prepare for war strategy
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

            MilitaryTask task = new MilitaryTask(targetPlanet, this)
            {
                Type                 = taskType,
                GoalId             = goal.Id,
                Goal                 = goal,
                TargetPlanetWarValue = (int)(targetPlanet.ColonyBaseValue(enemy) + targetPlanet.ColonyPotentialValue(enemy))
            };

            EmpireAI.AddPendingTask(task);
        }

        public bool TryGetPrepareForWarType(Empire enemy, out WarType warType)
        {
            warType = WarType.SkirmishWar;

            if (this == enemy)
            {
                Log.Warning($"{Name}: trying to prepare for war vs itself!");
                return false;
            }

            Relationship rel = GetRelations(enemy);
            if (!rel.AtWar && rel.PreparingForWar)
                warType = rel.PreparingForWarType;
            else
                return false;

            return true;
        }

        public bool ShouldCancelPrepareForWar()
        {
            return IsAtWarWithMajorEmpire && GetAverageWarGrade() < PersonalityModifiers.WarGradeThresholdForPeace;
        }

        public bool ShouldGoToWar(Relationship rel, Empire them)
        {
            if (them.data.Defeated || !rel.PreparingForWar || rel.AtWar || IsPeaceTreaty(them))
                return false;

            var currentWarInformation = AllActiveWars.FilterSelect(w => !w.Them.IsFaction, 
                w => GetRelations(w.Them).KnownInformation);

            int minStr                = (int)(Universum.GalaxySize + 1) * 5000;
            float currentEnemyStr     = currentWarInformation.Sum(i => i.OffensiveStrength);
            float currentEnemyBuild   = currentWarInformation.Sum(i => i.EconomicStrength);
            float ourCurrentStrength  = AIManagedShips.EmpireReadyFleets.AccumulatedStrength;
            float theirKnownStrength  = (rel.KnownInformation.AllianceTotalStrength + currentEnemyStr).LowerBound(minStr);
            float theirBuildCapacity  = (rel.KnownInformation.AllianceEconomicStrength + currentEnemyBuild).LowerBound(10);
            float ourBuildCapacity    = GetEmpireAI().BuildCapacity;

            var array = EmpireManager.GetAllies(this);
            for (int i = 0; i < array.Count; i++)
            {
                var ally = array[i];
                ourBuildCapacity   += ally.GetEmpireAI().BuildCapacity;
                ourCurrentStrength += ally.OffensiveStrength;
            }

            bool weAreStronger = ourCurrentStrength > theirKnownStrength * PersonalityModifiers.GoToWarTolerance
                                 && ourBuildCapacity > theirBuildCapacity;

            return weAreStronger;
        }

        bool IsAlreadyStriking()
        {
            return EmpireAI.GetTasks().Any(t => t.Type == MilitaryTask.TaskType.StrikeForce);
        }

        bool IsAlreadyGlassingPlanet(Planet planet)
        {
            return EmpireAI.GetTasks().Any(t => t.Type == MilitaryTask.TaskType.GlassPlanet && t.TargetPlanet == planet);
        }

        public bool CanAddAnotherWarGoal(Empire enemy)
        {
            return EmpireAI.Goals.Count(g => g.IsWarMission && g.TargetEmpire == enemy) <= DifficultyModifiers.NumWarTasksPerWar;
        }

        public bool TryGetMissionsVsEmpire(Empire enemy, out Goal[] goals)
        {
            goals = EmpireAI.Goals.Filter(g => g.IsWarMission && g.TargetEmpire == enemy);
            return goals.Length > 0;
        }

        public bool NoEmpireDefenseGoal()
        {
            return !EmpireAI.Goals.Any(g => g.type == GoalType.EmpireDefense);
        }

        public void AddDefenseSystemGoal(SolarSystem system, float strengthWanted, int fleetCount)
        {
            EmpireAI.Goals.Add(new DefendSystem(this, system, strengthWanted, fleetCount));
        }

        public bool HasWarTaskTargetingSystem(SolarSystem system)
        {
            return EmpireAI.GetTasks().Any(t => t.IsWarTask && (t.TargetPlanet?.ParentSystem == system || t.TargetSystem == system));
        }
    }

    public enum WarMissionType
    {
        Standard // todo advanced types
    }
}
