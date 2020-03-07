using System;
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
        private readonly Array<SolarSystem> MarkedForExploration = new Array<SolarSystem>();
        public Planet[] DesiredPlanets => RankedPlanets.FilterSelect(r=> r.Planet?.Owner != OwnerEmpire,
                                                                     r => r.Planet) ?? Empty<Planet>.Array;

        public Planet[] GetColonizationTargets(Planet[] markedPlanets)
        {
            return RankedPlanets.FilterSelect(ranker => !ranker.CantColonize &&
                                                        ranker.EnemyStrength < 1 &&
                                                        !markedPlanets.Contains(ranker.Planet),
                p => p.Planet);
        }

        private Array<Goal> Goals => OwnerEmpire.GetEmpireAI().Goals;
        public PlanetRanker[] RankedPlanets { get; private set; }
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

            //we are going to keep a list of wanted planets. 
            int maxDesiredPlanets = (int)(Empire.Universe.PlanetsDict.Count / 10).ClampMin(10);

            if (maxDesiredPlanets < 1)
                return;

            //rank all known planets
            Array<PlanetRanker> allPlanetsRanker = GatherAllPlanetRanks();
            if (allPlanetsRanker.IsEmpty)
                return;

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
                    if (ranker.Value > backupPlanet.Value && ranker.EnemyStrength <1 
                                                          && ranker.Planet.Owner == null)
                    {
                        backupPlanet = ranker;
                    }
                    continue;
                }
                planetsRanked.Add(ranker);

                if (ranker.CantColonize) continue;

                maxDesiredPlanets--;
                addBackupPlanet = addBackupPlanet && ranker.EnemyStrength > 0 ;
            }

            if (addBackupPlanet && backupPlanet.Planet != null) 
                planetsRanked.Add(backupPlanet);

            RankedPlanets = planetsRanked.ToArray();

            //take action on the found planets
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
            int desiredClaims = (DesiredColonyGoals * 2) - claimTasks.Length;
            var colonizing    = GetColonizationGoalPlanets();
            var taskTargets   = claimTasks.Select(t => t.TargetPlanet);

            for (int i = 0; i < RankedPlanets.Length && desiredClaims > 0; i++)
            {
                var rank = RankedPlanets[i];

                if (rank.CantColonize || rank.EnemyStrength < 1  || !colonizing.Contains(rank.Planet)
                    || rank.Planet.ParentSystem.OwnerList.Contains(OwnerEmpire))
                {
                    continue;
                }
                if (taskTargets.Contains(rank.Planet))
                    continue;
                var task = MilitaryTask.CreateClaimTask(rank.Planet, rank.EnemyStrength);
                OwnerEmpire.GetEmpireAI().AddPendingTask(task);
                desiredClaims--;
            }
        }

        /// Go through all known planets. filter planets by colonization rules. Rank remaining ones.
        Array<PlanetRanker> GatherAllPlanetRanks()
        {
            bool canColonizeBarren = OwnerEmpire.IsBuildingUnlocked(Building.BiospheresId);
            var allPlanetsRanker   = new Array<PlanetRanker>();
            float totalValue       = 0;
            int bestPlanetCount    = 0;

            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem sys = UniverseScreen.SolarSystemList[i];

                if (!sys.IsExploredBy(OwnerEmpire))
                    continue;

                AO ao = OwnerEmpire.GetEmpireAI().FindClosestAOTo(sys.Position);

                float systemEnemyStrength = OwnerEmpire.KnownEnemyStrengthIn(sys);
                
                for (int y = 0; y < sys.PlanetList.Count; y++)
                {
                    Planet p = sys.PlanetList[y];
                    if (p.Habitable)
                    {
                        //The planet ranker does the ranking
                        var r2 = new PlanetRanker(OwnerEmpire, p, canColonizeBarren, ao, systemEnemyStrength);
                        allPlanetsRanker.Add(r2);
                        totalValue += r2.Value;
                        bestPlanetCount++;
                    }
                }
            }

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
            }
            return finalPlanetsRanker;
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

        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            var potentials = new Array<SolarSystem>();
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem s = UniverseScreen.SolarSystemList[i];
                if (!s.IsExploredBy(OwnerEmpire))
                    potentials.Add(s);
                else
                    MarkedForExploration.Remove(s);
            }

            for (int i = 0; i < MarkedForExploration.Count; i++)
            {
                SolarSystem s = MarkedForExploration[i];
                potentials.Remove(s);
            }

            var empireCenter = OwnerEmpire.GetWeightedCenter();
            var sortedList = potentials.Sorted(s => empireCenter.SqDist(s.Position));

            if (sortedList.Length == 0)
            {
                queryingShip.AI.ClearOrders();
                return null;
            }
            SolarSystem nearestToHome = sortedList.Find(s=> !s.DangerousForcesPresent(OwnerEmpire));
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.DangerousForcesPresent(OwnerEmpire))
                    continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(empireCenter, nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)
                    continue;

                nearestToHome = nearest;
                break;

            }
            MarkedForExploration.Add(nearestToHome);
            return nearestToHome;
        }
    }
}