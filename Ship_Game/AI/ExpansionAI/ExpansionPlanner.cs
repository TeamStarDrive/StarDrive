using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.ExpansionAI
{
    public class ExpansionPlanner // Refactored by Crunchy Gremlin and Fat Bastard - Jun 22, 2020
    {
        readonly Empire Owner;
        private readonly Array<SolarSystem> MarkedForExploration = new Array<SolarSystem>();
        private Array<Goal> Goals => Owner.GetEmpireAI().Goals;
        public PlanetRanker[] RankedPlanets { get; private set; }
        public int ExpandSearchTimer { get; private set; }
        public int MaxSystemsToCheckedDiv { get; private set; }

        public Planet[] DesiredPlanets => RankedPlanets?.FilterSelect(r=> r.Planet?.Owner != Owner,
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

        public Array<Goal> GetColonizationGoals()
        {
            var list = new Array<Goal>();
            foreach (Goal g in Goals)
            {
                if (g.type == GoalType.Colonize)
                    list.Add(g);
            }

            return list;
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

        float PopulationRatio    => Owner.GetTotalPop(out float maxPop) / maxPop.LowerBound(1);
        float ExpansionThreshold => (Owner.IsExpansionists ? 0.2f : 0.33f) + Owner.DifficultyModifiers.ExpansionModifier;

        public ExpansionPlanner(Empire empire)
        {
            Owner                  = empire;
            SetMaxSystemsToCheckedDiv(Owner.IsExpansionists ? 4 : 6);
            ResetExpandSearchTimer();
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
            int colonizationGoalsWithEnemy    = currentColonizationGoals.Count(
                p => p.ParentSystem.DangerousForcesPresent(Owner));

            // Adds 1 more goal if we are stuck with all colonization goals with enemy
            int desiredGoals = colonizationGoalsWithEnemy >= DesiredColonyGoals
                ? (colonizationGoalsWithEnemy + 1).UpperBound(DesiredColonyGoals * 2)
                : DesiredColonyGoals;

            if ( currentColonizationGoals.Length >= desiredGoals)
                return;

            float ownerStrength       = Owner.CurrentMilitaryStrength;
            int ourPlanets            = Owner.GetPlanets().Count;

            if (!Owner.isPlayer && PopulationRatio < ExpansionThreshold 
                && ourPlanets >= Owner.DifficultyModifiers.MinStartingColonies)
            {
                return; // We have not reached our pop capacity threshold (for AI only) 
            }

            Log.Info(ConsoleColor.Magenta,$"Running Expansion for {Owner.Name}, PopRatio: {PopulationRatio.String(2)}");
            var ownedSystems     = Owner.GetOwnedSystems();
            var potentialSystems = UniverseScreen.SolarSystemList.Filter(s => s.IsExploredBy(Owner)
                                                                         && !s.IsOwnedBy(Owner)
                                                                         && s.PlanetList.Any(p => p.Habitable)
                                                                         && Owner.KnownEnemyStrengthIn(s).LessOrEqual(ownerStrength));

            // We are going to keep a list of wanted planets. 
            // We are limiting the number of foreign systems to check based on galaxy size and race traits
            int maxCheckedSystems = (UniverseScreen.SolarSystemList.Count / MaxSystemsToCheckedDiv).LowerBound(3);
            Vector2 empireCenter  = Owner.WeightedCenter;

            Array<Planet> potentialPlanets = GetPotentialPlanetsLocal(ownedSystems);
            if (potentialSystems.Length > 0)
            {
                potentialSystems.Sort(s => empireCenter.Distance(s.Position));
                potentialSystems = potentialSystems.Take(maxCheckedSystems).ToArray();
                potentialPlanets.AddRange(GetPotentialPlanetsNonLocal(potentialSystems));
            }

            // Rank all known planets near the empire
            if (!GatherAllPlanetRanks(potentialPlanets, currentColonizationGoals, empireCenter, out Array<PlanetRanker> allPlanetsRanker))
            {
                // Nothing found in current search area
                if (--ExpandSearchTimer <= 0 && MaxSystemsToCheckedDiv > 1) // increase search area if timer is done
                {
                    ResetExpandSearchTimer();
                    MaxSystemsToCheckedDiv = (MaxSystemsToCheckedDiv - 1).LowerBound(1);
                }

                return;
            }

            ResetExpandSearchTimer();
            RankedPlanets = allPlanetsRanker.SortedDescending(pr => pr.Value);

            // Take action on the found planets
            CreateColonyGoals(currentColonizationGoals, desiredGoals);
        }

        /// <summary>
        /// Send colony ships to best targets;
        /// </summary>
        void CreateColonyGoals(Planet[] markedPlanets, int desiredGoals)
        {
            int netDesired = (desiredGoals - markedPlanets.Length).UpperBound(RankedPlanets.Length);

            if (netDesired < 1) 
                return;

            for (int i = 0; i < RankedPlanets.Length && netDesired > 0; i++)
            {
                Planet planet = RankedPlanets[i].Planet;
                Log.Info(ConsoleColor.Magenta,
                    $"Colonize {markedPlanets.Length + 1}/{DesiredColonyGoals} | {planet} | {Owner}");

                Goals.Add(new MarkForColonization(planet, Owner));
                netDesired--;
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
                    if (Owner.KnownEnemyStrengthIn(p.ParentSystem) <= Owner.CurrentMilitaryStrength
                        && p.Habitable
                        && (p.Owner == null || p.Owner.isFaction))
                    {
                        potentialPlanets.Add(p);
                    }
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

            float longestDistance  = planetList.Last().Center.Distance(empireCenter);
        
            for (int i = 0; i < planetList.Count; i++)
            {
                Planet p = planetList[i];
                // The planet ranker does the ranking
                if (!markedPlanets.Contains(p)) // Don't include planets we are already trying to colonize
                {
                    var pr = new PlanetRanker(Owner, p, longestDistance, empireCenter);
                    if (pr.CanColonize)
                        planetRanker.Add(pr);
                }
            }

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

        public void SetExpandSearchTimer(int value)
        {
            ExpandSearchTimer = value;
        }

        public void SetMaxSystemsToCheckedDiv(int value)
        {
            MaxSystemsToCheckedDiv = value;
        }

        public void ResetExpandSearchTimer()
        {
            SetExpandSearchTimer(Owner.DifficultyModifiers.ExpandSearchTurns);
        }
    }
}