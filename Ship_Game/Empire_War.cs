using System;
using System.Collections.Generic;
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
            return AI.HasGoal(g => g.IsWarMissionTarget(planet));
        }

        public bool GetPotentialTargetPlanets(Empire enemy, WarType warType, out Planet[] targetPlanets)
        {
            targetPlanets = null;
            switch (warType)
            {
                case WarType.GenocidalWar:
                case WarType.ImperialistWar: targetPlanets = PotentialPlanetTargetsImperialistWar(enemy); break;
                case WarType.BorderConflict: targetPlanets = PotentialPlanetTargetsBorderWar(enemy);      break;
                case WarType.DefensiveWar:   targetPlanets = PotentialPlanetTargetsDefensiveWar(enemy);   break;
            }

            if (targetPlanets?.Length > 0)
                targetPlanets = targetPlanets.Filter(p => !LeavesToBombers(p));

            return targetPlanets?.Length > 0;
        }

        public int TroopShipRisk(Planet p)
        {
            if (CanBuildBombers)
                return 0;
            if (p.HasCapital)
                return 2;
            return p.BuildingGeodeticOffense > 0 ? 1 : 0;
        }

        public bool LeavesToBombers(Planet p)
        {
            if (CanBuildBombers || !p.HasCapital || p.Owner == null)
                return false;

            IReadOnlyList<Planet> theirPlanets = p.Owner.GetPlanets();
            for (int i = 0; i < theirPlanets.Count; ++i)
                if (!theirPlanets[i].HasCapital)
                    return true;

            return false;
        }

        public Planet SafestToInvade(IReadOnlyList<Planet> planets, Predicate<Planet> isTarget)
        {
            Planet safest = null;
            int lowestRisk = int.MaxValue;
            for (int i = 0; i < planets.Count; ++i)
            {
                Planet p = planets[i];
                if (!isTarget(p))
                    continue;

                int risk = TroopShipRisk(p);
                if (risk < lowestRisk)
                {
                    safest = p;
                    lowestRisk = risk;
                }
            }
            return safest;
        }

        Planet[] SafestFirst(Planet[] sorted)
        {
            var result = new Planet[sorted.Length];
            int n = 0;
            for (int risk = 0; risk <= 2; ++risk)
                for (int i = 0; i < sorted.Length; ++i)
                    if (TroopShipRisk(sorted[i]) == risk)
                        result[n++] = sorted[i];
            return result;
        }

        Planet[] PotentialPlanetTargetsImperialistWar(Empire enemy)
        {
            if (enemy.isPlayer && (IsAlliedWith(enemy) || IsOpenBordersTreaty(enemy)))
                return PotentialTargetsPlayerSurprise(enemy);

            float ratio = (float)TotalScore / enemy.TotalScore;
            int numClosestPlanetsToTake = 3;
            float finalRatio = (ratio < 1
                ? ratio * 0.3f
                : ratio * PersonalityModifiers.ImperialistWarPlanetsToTakeMult).UpperBound(1);

            numClosestPlanetsToTake = (int)(finalRatio * enemy.GetPlanets().Count).LowerBound(3);
            var potentialPlanets = enemy.GetPlanets().Filter(p => !HasWarMissionTargeting(p));
            return potentialPlanets.Sorted(p => p.System.Position.SqDist(WeightedCenter)).Take(numClosestPlanetsToTake).ToArray();
        }

        Planet[] PotentialTargetsPlayerSurprise(Empire enemy) 
        {
            var potentialPlanets = enemy.GetPlanets().Filter(p => !HasWarMissionTargeting(p));
            return potentialPlanets.SortedDescending(p => p.ColonyWarValueTo(enemy)).Take(5).ToArray();
        }

        Planet[] PotentialPlanetTargetsBorderWar(Empire enemy)
        {
            var potentialPlanets = enemy.GetPlanets().Filter(p => p.System.HasPlanetsOwnedBy(this)
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
            Planet[] sorted;
            switch (warType)
            {
                default:
                case WarType.BorderConflict: sorted = targets.SortedDescending(p => p.ColonyPotentialValue(this)); break;
                case WarType.DefensiveWar:   sorted = targets.Sorted(p => p.Position.SqDist(WeightedCenter)); break;
                case WarType.ImperialistWar: sorted = targets.SortedDescending(p => p.ColonyPotentialValue(this) / p.Position.Distance(WeightedCenter)); break;
                case WarType.GenocidalWar:   sorted = targets.SortedDescending(p => p.ColonyPotentialValue(enemy) / p.Position.Distance(WeightedCenter)); break;
            }

            return CanBuildBombers ? sorted : SafestFirst(sorted);
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
            MilitaryTask task = new MilitaryTask(targetPlanet, this, MilitaryTaskImportance.Normal)
            {
                Type     = MilitaryTask.TaskType.StageFleet,
                Goal     = goal
            };

            AI.AddPendingTask(task);
        }

        public void CreateWarTask(Planet targetPlanet, Empire enemy, Goal goal, out MilitaryTask task)
        {
            // todo advanced mission types per personality or prepare for war strategy
            MilitaryTask.TaskType taskType = MilitaryTask.TaskType.StrikeForce;
            MilitaryTaskImportance importance = MilitaryTaskImportance.Important;
            if (IsAlreadyStriking())
            {
                if (CanBuildBombers
                     && !IsAlreadyGlassingPlanet(targetPlanet)
                     && (targetPlanet.Population < 1
                         || targetPlanet.ColonyPotentialValue(enemy) / targetPlanet.ColonyPotentialValue(this) > PersonalityModifiers.DoomFleetThreshold))
                {
                    taskType = MilitaryTask.TaskType.GlassPlanet;
                    importance = MilitaryTaskImportance.Normal;
                }
                else
                {
                    taskType = MilitaryTask.TaskType.AssaultPlanet;
                    importance = MilitaryTaskImportance.Normal;
                }
            }

            task = new MilitaryTask(targetPlanet, this, importance)
            {
                Goal = goal,
                Type = taskType,
                TargetPlanetWarValue = (int)(targetPlanet.ColonyBaseValue(enemy) + targetPlanet.ColonyPotentialValue(enemy))
            };

            AI.AddPendingTask(task);
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
            if (them.IsDefeated || !rel.PreparingForWar || rel.AtWar || IsPeaceTreaty(them))
                return false;

            var currentEnemies = AllActiveWars.FilterSelect(w => !w.Them.IsFaction, w => w.Them);
            int minStr = DifficultyModifiers.MinimumThreatStr * (them.isPlayer ? 500 : 1000);
            float currentEnemyStr   = currentEnemies.Sum(e => e.isPlayer ? e.OffensiveStrength : e.ShipsReadyForFleet.AccumulatedStrength);
            float currentEnemyBuild = currentEnemies.Sum(e => e.AI.BuildCapacity);


            (float ourCurrentStrength, float ourBuildCapacity) = GetAlliedStr(this);
            (float theirKnownStrength, float theirBuildCapacity) = GetAlliedStr(them);
            theirKnownStrength = theirKnownStrength.LowerBound(minStr);
            theirBuildCapacity = theirBuildCapacity.LowerBound(10);
            bool weAreStronger = ourCurrentStrength > theirKnownStrength * PersonalityModifiers.GoToWarTolerance && ourBuildCapacity > theirBuildCapacity 
                                 || ourCurrentStrength > theirKnownStrength * PersonalityModifiers.GoToWarTolerance*1.5f;

            (float alliesStr , float alliesBuildCapacity) GetAlliedStr(Empire e)
            {
                float buildCap = e.AI.BuildCapacity;
                float currentStr = e.isPlayer ? e.OffensiveStrength : e.ShipsReadyForFleet.AccumulatedStrength;
                var array = Universe.GetAllies(e);
                for (int i = 0; i < array.Count; i++)
                {
                    var ally = array[i];
                    buildCap += ally.AI.BuildCapacity;
                    currentStr += ally.isPlayer ? ally.OffensiveStrength : ally.ShipsReadyForFleet.AccumulatedStrength;
                }

                return (currentStr, buildCap);
            }

            return weAreStronger;
        }

        bool IsAlreadyStriking()
        {
            return AI.GetTasks().Any(t => t.Type == MilitaryTask.TaskType.StrikeForce);
        }

        bool IsAlreadyGlassingPlanet(Planet planet)
        {
            return AI.GetTasks().Any(t => t.Type == MilitaryTask.TaskType.GlassPlanet && t.TargetPlanet == planet);
        }

        public bool CanAddAnotherWarGoal(Empire enemy)
        {
            return AI.CountGoals(g => g.IsWarMissionTarget(enemy)) <= DifficultyModifiers.NumWarTasksPerWar;
        }

        public bool TryGetMissionsVsEmpire(Empire enemy, out Goal[] goals)
        {
            goals = AI.FindGoals(g => g.IsWarMissionTarget(enemy));
            return goals.Length > 0;
        }

        public bool NoEmpireDefenseGoal()
        {
            return !AI.HasGoal(g => g.Type == GoalType.EmpireDefense);
        }

        public void AddDefenseSystemGoal(SolarSystem system, float strengthWanted, MilitaryTaskImportance importance, 
            int fleetCount = 1, Empire targetEmpire = null, Empire attacker = null)
        {
            AI.AddGoal(new DefendSystem(this, system, strengthWanted.LowerBound(100), fleetCount, importance, targetEmpire, attacker));
        }

        public bool HasWarTaskTargetingSystem(SolarSystem system)
        {
            return AI.GetTasks().Any(t => t.IsWarTask && (t.TargetPlanet?.System == system || t.TargetSystem == system));
        }
    }

    public enum WarMissionType
    {
        Standard // todo advanced types
    }
}
