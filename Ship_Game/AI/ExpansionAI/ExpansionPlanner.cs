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
        readonly Empire Owner;
        private IReadOnlyList<SolarSystem> OwnedSystems;
        private readonly Array<SolarSystem> MarkedForExploration = new Array<SolarSystem>();
        private Array<Goal> Goals => Owner.GetEmpireAI().Goals;
        public PlanetRanker[] RankedPlanets { get; private set; }

        // todo - check this
        public Planet[] DesiredPlanets => RankedPlanets.FilterSelect(r=> r.Planet?.Owner != Owner,
                                                                     r => r.Planet) ?? Empty<Planet>.Array;

        public Planet[] GetColonizationGoalPlanets()
        {
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
            {
                if (g.type == GoalType.Colonize)
                    list.Add(g.ColonizationTarget);
            }

            return list.ToArray();
        }

        int DesiredColonyGoals
        {
            get
            {
                float baseColonyGoals = Owner.DifficultyModifiers.BaseColonyGoals;
                if (Owner.isPlayer) 
                    return (int)baseColonyGoals; // BaseColonyGoals for player

                float baseValue = 1.1f; // @note This value is very sensitive, don't mess around without testing
                int plusGoals   = Owner.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;
                float goals     = (float)Math.Round(baseValue + baseColonyGoals + plusGoals, 0);
                return (int)goals.Clamped(1f, 5f);
            }
        }

        float PopulationRatio    => Owner.GetTotalPop() / Owner.GetTotalPopPotential().LowerBound(1);
        bool  IsExpansionists    => Owner.data.EconomicPersonality.Name == "Expansionists";
        float ExpansionThreshold => (IsExpansionists ? 0.35f : 0.5f) + Owner.DifficultyModifiers.ExpansionModifier;

        public ExpansionPlanner(Empire empire)
        {
            Owner = empire;
        }

        /// <summary>
        /// create a list or planets ranked by the colonization priority.
        /// colonize ones that can be.
        /// send fleets to ones that need protection. 
        /// </summary>
        public void RunExpansionPlanner()
        {
            if (Owner.isPlayer && !Owner.AutoColonize)
                return;

            Planet[] currentColonizationGoals = GetColonizationGoalPlanets();
            if (currentColonizationGoals.Length >= DesiredColonyGoals)
                return;

            if (PopulationRatio < ExpansionThreshold)
                return; // We have not reached our pop capacity threshold yet

            Log.Info(ConsoleColor.Magenta,$"Running Expansion for {Owner.Name}, PopRatio: {PopulationRatio.String(2)}");
            OwnedSystems         = Owner.GetOwnedSystems();
            float ownerStrength  = Owner.CurrentMilitaryStrength;
            var potentialSystems = Empire.Universe.SolarSystemDict.FilterValues(s => s.IsExploredBy(Owner)
                                                                                     && !s.IsOwnedBy(Owner)
                                                                                     && s.PlanetList.Any(p => p.Habitable)
                                                                                     && Owner.KnownEnemyStrengthIn(s) < ownerStrength);

            // We are going to keep a list of wanted planets. 
            // We are limiting the number of foreign systems to check based on galaxy size and race traits
            int maxCheckedDiv     = IsExpansionists ? 4 : 6;
            int maxCheckedSystems = (Empire.Universe.SolarSystemDict.Count / maxCheckedDiv).LowerBound(3);
            Vector2 empireCenter  = Owner.GetWeightedCenter();

            Array<Planet> potentialPlanets = GetPotentialPlanetsLocal(OwnedSystems);
            if (potentialSystems.Length > 0)
            {
                potentialSystems.Sort(s => empireCenter.Distance(s.Position));
                potentialSystems = potentialSystems.Take(maxCheckedSystems).ToArray();
                potentialPlanets.AddRange(GetPotentialPlanetsNonLocal(potentialSystems));
            }

            // Rank all known planets near the empire
            if (!GatherAllPlanetRanks(potentialPlanets, currentColonizationGoals, empireCenter, out Array <PlanetRanker> allPlanetsRanker))
                return;

            RankedPlanets = allPlanetsRanker.SortedDescending(pr => pr.Value);

            // Take action on the found planets
            CreateColonyGoals(currentColonizationGoals);
            //CreateClaimFleets();


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
            //CreateColonyGoals();
         //   CreateClaimFleets();
        }

        /// <summary>
        /// Send colony ships to best targets;
        /// </summary>
        void CreateColonyGoals(Planet[] markedPlanets)
        {
            int desired            = DesiredColonyGoals - markedPlanets.Length;
            desired                = desired.UpperBound(RankedPlanets.Length);

            if (desired < 1) 
                return;

            for (int i = 0; i < RankedPlanets.Length && desired > 0; i++)
            {
                Planet planet = RankedPlanets[i].Planet;
                Log.Info(ConsoleColor.Magenta,
                    $"Colonize {markedPlanets.Length + 1}/{DesiredColonyGoals} | {planet} | {Owner}");

                Goals.Add(new MarkForColonization(planet, Owner));
                desired--;
            }
        }

        /*
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
        }*/

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
        bool GatherAllPlanetRanks(Array<Planet> planetList, Planet[] markedPlanets, Vector2 empireCenter, out Array<PlanetRanker> planetRanker)
        {
            planetRanker = new Array<PlanetRanker>();
            if (planetList.Count == 0)
                return false;

            bool canColonizeBarren = Owner.IsBuildingUnlocked(Building.BiospheresId);
            float longestDistance  = planetList.Last().Center.Distance(empireCenter);
            //float totalValue       = 0;
            //int bestPlanetCount    = 0;


            SolarSystem currentSystem = planetList.First().ParentSystem;
            //AO ao = OwnerEmpire.GetEmpireAI().FindClosestAOTo(currentSystem.Position);
            //float systemEnemyStrength = OwnerEmpire.KnownEnemyStrengthIn(currentSystem);
            
            for (int i = 0; i < planetList.Count; i++)
            {
                Planet p = planetList[i];
                /*
                if (p.ParentSystem != currentSystem)
                {
                    currentSystem       = p.ParentSystem;
                    ao                  = OwnerEmpire.GetEmpireAI().FindClosestAOTo(currentSystem.Position);
                    systemEnemyStrength = OwnerEmpire.KnownEnemyStrengthIn(currentSystem);
                }
                */
                // The planet ranker does the ranking

                if (!markedPlanets.Contains(p)) // Don't include planets we are already trying to colonize
                {
                    var pr = new PlanetRanker(Owner, p, canColonizeBarren, longestDistance, empireCenter);
                    if (pr.CanColonize)
                        planetRanker.Add(pr);
                }

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
            if (Owner.isPlayer || Owner.isFaction)
                return;

            if (!thiefRelationship.Known)
                return;

            if (claimedPlanet.Owner != thievingEmpire || thiefRelationship.AtWar)
                return;

            thiefRelationship.StoleOurColonyClaim(Owner, claimedPlanet);

            if (!thievingEmpire.isPlayer)
                return;

            thiefRelationship.WarnClaimThiefPlayer(claimedPlanet, Owner);
        }

        public bool AssignExplorationTargetSystem(Ship ship, out SolarSystem targetSystem)
        {
            targetSystem   = null;
            var potentials = new Array<SolarSystem>();
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem s = UniverseScreen.SolarSystemList[i];
                if (!s.IsExploredBy(Owner) && !MarkedForExploration.Contains(s))
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