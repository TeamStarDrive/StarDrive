using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.Components;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class EmpireDefense : Goal
    {
        [StarDataConstructor]
        public EmpireDefense(Empire owner) : base(GoalType.EmpireDefense, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                AddDefendSystemTasks,
                AssessDefense
            };
        }

        GoalStep AddDefendSystemTasks()
        {
            var systems = new Array<IncomingThreat>();
            foreach (IncomingThreat threatenedSystem in Owner.SystemsWithThreat)
            {
                if (!threatenedSystem.ThreatTimedOut && threatenedSystem.HighPriority)
                    systems.Add(threatenedSystem);
            }

            systems.Sort(ts => ts.TargetSystem.WarValueTo(Owner));
            for (int i = 0; i < systems.Count; i++)
            {
                var threatenedSystem = systems[i];
                if (!threatenedSystem.TargetSystem.HasPlanetsOwnedBy(Owner)
                    || Owner.HasWarTaskTargetingSystem(threatenedSystem.TargetSystem))
                {
                    continue;
                }

                float minStr = threatenedSystem.Strength.Greater(500) ? threatenedSystem.Strength : 1000;
                if (threatenedSystem.Enemies.Length > 0)
                    minStr *= Owner.GetFleetStrEmpireMultiplier(threatenedSystem.Enemies[0]).UpperBound(Owner.OffensiveStrength / 5);

                Owner.AddDefenseSystemGoal(threatenedSystem.TargetSystem, 
                    minStr, 5 - threatenedSystem.TargetSystem.DefenseTaskPriority(Owner));
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep AssessDefense()
        {
            var defendSystemTasks = Owner.AI.GetDefendSystemTasks();
            foreach (MilitaryTask defendSystem in defendSystemTasks)
            {
                if (defendSystem.Fleet != null || defendSystem.Fleet == null && defendSystem.Goal?.LifeTime > 5)
                    continue; // We have a fleet for this task or too old to prioritize

                foreach (MilitaryTask possibleTask in Owner.AI.GetPotentialTasksToCompare())
                {
                    if (possibleTask != defendSystem)
                    {
                        if (DefenseTaskHasHigherPriority(defendSystem, possibleTask))
                            possibleTask.EndTask();
                    }
                }
            }
            return GoalStep.RestartGoal;
        }

        bool DefenseTaskHasHigherPriority(MilitaryTask defenseTask, MilitaryTask possibleTask)
        {
            SolarSystem system = defenseTask.TargetSystem ?? defenseTask.TargetPlanet.ParentSystem;
            if (system.PlanetList.Any(p => p.Owner == Owner && p.HasCapital)
                && !possibleTask.TargetSystem?.PlanetList.Any(p => p.Owner == Owner && p.HasCapital) == true)
            {
                return true; // Defend our home systems at all costs (unless the other task also has a home system)!
            }

            if (possibleTask.Type == MilitaryTask.TaskType.ClearAreaOfEnemies)
                return false;

            Planet target            = possibleTask.TargetPlanet;
            SolarSystem targetSystem = target?.ParentSystem ?? possibleTask.TargetSystem;

            if (system == targetSystem)
                return false; // The checked task has the same target system, no need to cancel it

            if (possibleTask.Type == MilitaryTask.TaskType.DefendPostInvasion
                && !Owner.SystemsWithThreat.Any(t => !t.ThreatTimedOut && t.TargetSystem == targetSystem)
                && !targetSystem?.DangerousForcesPresent(Owner) == true)
            {
                return true; // Cancel idle post invasion fleets if we need to defend
            }

            float defenseValue  = system.PotentialValueFor(Owner) * Owner.PersonalityModifiers.DefenseTaskWeight;
            float possibleValue = targetSystem?.PotentialValueFor(Owner) ?? 0;

            if (possibleTask.Fleet != null) // compare fleet distances
            {
                float defenseDist = possibleTask.Fleet.AveragePosition().Distance(system.Position) / 10000;
                float expansionDist = possibleTask.Fleet.AveragePosition().Distance(target.Position) / 10000;
                defenseValue /= defenseDist.LowerBound(1);
                possibleValue /= expansionDist.LowerBound(1);
            }
            else // compare planet distances
            {
                Vector2 possiblePos = target?.Position ?? targetSystem?.Position ?? possibleTask.TargetShip.Position;
                Vector2 defensePos  = system.Position;
                defenseValue /= Owner.WeightedCenter.Distance(defensePos).LowerBound(1);
                possibleValue /= Owner.WeightedCenter.Distance(possiblePos).LowerBound(1);
            }

            return defenseValue > possibleValue;
        }
    }
}
