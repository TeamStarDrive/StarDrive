using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.ExpansionAI
{
    public class ExpansionPlanner
    {
        readonly Empire OwnerEmpire;
        private IReadOnlyList<SolarSystem> OwnedSystems;
        private readonly Array<SolarSystem> MarkedForExploration = new Array<SolarSystem>();
        private Array<Goal> Goals => OwnerEmpire.GetEmpireAI().Goals;
        public PlanetRanker[] RankedPlanets { get; private set; }

        public Planet[] DesiredPlanets => RankedPlanets.FilterSelect(r=> r.Planet?.Owner != OwnerEmpire,
                                                                     r => r.Planet) ?? Empty<Planet>.Array;

        public Planet[] GetColonizationTargets(Planet[] markedPlanets)
        {
            return RankedPlanets.FilterSelect(ranker => !ranker.CantColonize &&
                                                        ranker.EnemyStrength < 1 &&
                                                        !markedPlanets.Contains(ranker.Planet),
                p => p.Planet);
        }

        public Planet[] GetColonizationGoalPlanets()
        {
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
                if (g.type == GoalType.Colonize)
                    list.Add(g.ColonizationTarget);
            return list.ToArray();
        }

        public bool AnyPlanetsMarkedForColonization()
        {
            var list = new Array<Planet>();
            for (int i = 0; i < Goals.Count; i++)
            {
                Goal g = Goals[i];
                if (g.type == GoalType.Colonize)
                    return true;
            }

            return false;
        }

        int DesiredColonyGoals
        {
            get
            {
                float baseColonyGoals = OwnerEmpire.DifficultyModifiers.BaseColonyGoals;
                if (OwnerEmpire.isPlayer) 
                    return (int)baseColonyGoals; // BaseColonyGoals for player

                float baseValue = 1.1f; // @note This value is very sensitive, don't mess around without testing
                int plusGoals   = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;
                float goals     = (float)Math.Round(baseValue + baseColonyGoals + plusGoals, 0);
                return (int)goals.Clamped(1f, 5f);
            }
        }

        public ExpansionPlanner(Empire empire)
        {
            OwnerEmpire = empire;
        }

        /// <summary>
        /// create a list or planets ranked by the colonization priority.
        /// colonize ones that can be.
        /// send fleets to ones that need protection. 
        /// </summary>
        public void RunExpansionPlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoColonize)
                return;

            OwnedSystems         = OwnerEmpire.GetOwnedSystems();
            float ownerStrength  = OwnerEmpire.CurrentMilitaryStrength;
            var potentialSystems = Empire.Universe.SolarSystemDict.FilterValues(s => s.IsExploredBy(OwnerEmpire)
                                                                                     && !s.IsOwnedBy(OwnerEmpire)
                                                                                     && s.PlanetList.Any(p => p.Habitable)
                                                                                     && OwnerEmpire.KnownEnemyStrengthIn(s) < ownerStrength);

            Vector2 empireCenter = OwnerEmpire.GetWeightedCenter();
            potentialSystems.Sort(s => empireCenter.Distance(s.Position));

            Array<Planet> potentialPlanets = GetPotentialPlanetsLocal(OwnedSystems);
            potentialPlanets.AddRange(GetPotentialPlanetsNonLocal(potentialSystems));

            /*
            // We are going to keep a list of wanted planets. 
            int maxDesiredPlanets = (Empire.Universe.PlanetsDict.Count / 10).LowerBound(10);
            if (maxDesiredPlanets < 1)
                return;

            */

            // Rank all known planets near the empire
            if (!GatherAllPlanetRanks(potentialPlanets, out Array <PlanetRanker> allPlanetsRanker, empireCenter))
                return;

            RankedPlanets = allPlanetsRanker.SortedDescending(pr => pr.Value);

            //take action on the found planets
            CreateColonyGoals();
            CreateClaimFleets();

            return;
            /*
            //Create a list of the top priority planets
            var planetsRanked         = new Array<PlanetRanker>();
            PlanetRanker backupPlanet = new PlanetRanker();
            bool addBackupPlanet      = OwnerEmpire.data.ColonyBudget * 0.75f > OwnerEmpire.TotalBuildingMaintenance 
                                        && !AnyPlanetsMarkedForColonization();

            for (int i = 0; i < allPlanetsRanker.Count && maxDesiredPlanets > 0; i++)
            {
                var ranker = allPlanetsRanker[i];
                if (ranker.PoorPlanet)
                {
                    if (ranker.Value > backupPlanet.Value && ranker.EnemyStrength < 1 
                                                          && ranker.Planet.Owner == null)
                    {
                        backupPlanet = ranker;
                    }

                    continue;
                }

                planetsRanked.Add(ranker);

                if (ranker.CantColonize) 
                    continue;

                maxDesiredPlanets--;
                addBackupPlanet = addBackupPlanet && ranker.EnemyStrength > 0 ;
            }

            if (addBackupPlanet && backupPlanet.Planet != null) 
                planetsRanked.Add(backupPlanet);

            RankedPlanets = planetsRanked.ToArray();

            //take action on the found planets*/
            CreateColonyGoals();
            CreateClaimFleets();
        }

        /// <summary>
        /// Send colony ships to best targets;
        /// </summary>
        void CreateColonyGoals()
        {
            Planet[] markedPlanets = GetColonizationGoalPlanets();
            int desired            = DesiredColonyGoals;
            desired               -= markedPlanets.Length;

            if (desired < 1) return;

            desired = Math.Min(desired, RankedPlanets.Length);
            var colonizationTargets = GetColonizationTargets(markedPlanets);

            for (int i = 0; i < colonizationTargets.Length && desired > 0; i++)
            {
                var planet = colonizationTargets[i];

                Log.Info(ConsoleColor.Magenta,
                    $"Colonize {markedPlanets.Length + 1}/{desired} | {planet} | {OwnerEmpire}");
                Goals.Add(new MarkForColonization(planet, OwnerEmpire));
                desired--;
            }
        }

        /// <summary>
        /// Send a claim fleet on either of these conditions.
        /// * we are sending a colony ship
        /// * the colony target has a faction force;
        /// limit the number of claim forces to the number of colony goals.
        /// i want to change this but im not sure how yet. 
        /// </summary>
        void CreateClaimFleets()
        {
            var claimTasks    = OwnerEmpire.GetEmpireAI().GetClaimTasks();
            int desiredClaims = DesiredColonyGoals - claimTasks.Length;
            var colonizing    = GetColonizationGoalPlanets();
            var taskTargets   = claimTasks.Select(t => t.TargetPlanet);
            desiredClaims    -= taskTargets.Length;

            for (int i = 0; i < RankedPlanets.Length && desiredClaims > 0; i++)
            {
                var rank = RankedPlanets[i];

                if (rank.CanColonize && rank.NeedClaimFleet && !taskTargets.Contains(rank.Planet))
                {
                    var task      = MilitaryTask.CreateClaimTask(rank.Planet, rank.EnemyStrength * 2);
                    task.Priority = 10;
                    OwnerEmpire.GetEmpireAI().AddPendingTask(task);
                    desiredClaims--;
                }
            }
        }

        Array<Planet> GetPotentialPlanetsNonLocal(SolarSystem[] systems)
        {
            Array<Planet> potentialPlanets = new Array<Planet>();
            for (int i = 0; i < systems.Length; i++)
            {
                SolarSystem system = systems[i];
                for (int j = 0; j < system.PlanetList.Count; j++)
                {
                    Planet p = system.PlanetList[j];
                    if (p.Habitable && (p.Owner == null || p.Owner.isFaction))
                        potentialPlanets.Add(p);
                }
            }

            return potentialPlanets;
        }

        Array<Planet> GetPotentialPlanetsLocal(IReadOnlyList<SolarSystem> systems)
        {
            Array<Planet> potentialPlanets = new Array<Planet>();
            for (int i = 0; i < systems.Count; i++)
            {
                SolarSystem system = systems[i];
                for (int j = 0; j < system.PlanetList.Count; j++)
                {
                    Planet p = system.PlanetList[j];
                    if (p.Habitable && (p.Owner == null || p.Owner.isFaction))
                        potentialPlanets.Add(p);
                }
            }

            return potentialPlanets;
        }

        /// Go through the filtered planet list and rank them.
        bool GatherAllPlanetRanks(Array<Planet> planetList, out Array<PlanetRanker> planetRanker, Vector2 empireCenter)
        {
            planetRanker = new Array<PlanetRanker>();
            if (planetList.Count == 0)
                return false;

            bool canColonizeBarren = OwnerEmpire.IsBuildingUnlocked(Building.BiospheresId);
            float longestDistance = planetList.Last().Center.Distance(empireCenter);
            //float totalValue       = 0;
            //int bestPlanetCount    = 0;


            SolarSystem currentSystem = planetList.First().ParentSystem;
            AO ao = OwnerEmpire.GetEmpireAI().FindClosestAOTo(currentSystem.Position);
            float systemEnemyStrength = OwnerEmpire.KnownEnemyStrengthIn(currentSystem);
            
            for (int i = 0; i < planetList.Count; i++)
            {
                Planet p = planetList[i];
                if (p.ParentSystem != currentSystem)
                {
                    currentSystem       = p.ParentSystem;
                    ao                  = OwnerEmpire.GetEmpireAI().FindClosestAOTo(currentSystem.Position);
                    systemEnemyStrength = OwnerEmpire.KnownEnemyStrengthIn(currentSystem);
                }

                // The planet ranker does the ranking
                var pr = new PlanetRanker(OwnerEmpire, p, canColonizeBarren, systemEnemyStrength, longestDistance, empireCenter );
                if (pr.CanColonize)
                    planetRanker.Add(pr);

                //totalValue += r2.Value;
                //bestPlanetCount++;
            }

            /*
            // sort and purge the list. 
            // we are taking an average of all planets ranked and saying we only want
            // above average planets only. 
            var finalPlanetsRanker = new Array<PlanetRanker>();
            if (allPlanetsRanker.Count > 0)
            {
                allPlanetsRanker.Sort(p => -p.Value);
                float avgValue = totalValue / bestPlanetCount;

                foreach (PlanetRanker rankedP in allPlanetsRanker)
                {
                    rankedP.EvaluatePoorness(avgValue);
                    finalPlanetsRanker.Add(rankedP);
                }
            }*/
            return planetRanker.Count > 0;
        }








        public void CheckClaim(Empire thievingEmpire, Relationship thiefRelationship, Planet claimedPlanet)
        {
            if (OwnerEmpire.isPlayer || OwnerEmpire.isFaction)
                return;

            if (!thiefRelationship.Known)
                return;

            if (claimedPlanet.Owner != thievingEmpire || thiefRelationship.AtWar)
                return;

            thiefRelationship.StoleOurColonyClaim(OwnerEmpire, claimedPlanet);

            if (!thievingEmpire.isPlayer)
                return;

            thiefRelationship.WarnClaimThiefPlayer(claimedPlanet, OwnerEmpire);
        }

        public bool AssignExplorationTargetSystem(Ship ship, out SolarSystem targetSystem)
        {
            targetSystem   = null;
            var potentials = new Array<SolarSystem>();
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem s = UniverseScreen.SolarSystemList[i];
                if (!s.IsExploredBy(OwnerEmpire) && !MarkedForExploration.Contains(s))
                    potentials.Add(s);
            }

            if (potentials.Count == 0)
                return false; // All systems were explored or are marked by someone else

            // Sort by distance from explorer center
            var sortedList    = potentials.Sorted(s => ship.Center.SqDist(s.Position));
            targetSystem      = sortedList.First();

            MarkedForExploration.Add(targetSystem);
            return true;
        }

        public void RemoveExplorationTargetFromList(SolarSystem system)
        {
            MarkedForExploration.Remove(system);
        }
    }
}