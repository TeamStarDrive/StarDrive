using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.ExpansionAI
{
    [StarDataType]
    public class ExpansionPlanner // Refactored by Crunchy Gremlin and Fat Bastard - Jun 22, 2020
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Array<SolarSystem> MarkedForExploration = new();
        [StarData] public int ExpandSearchTimer { get; private set; }
        [StarData] public int MaxSystemsToCheckedDiv { get; private set; }
        [StarData] int ExpansionIntervalTimer = 100_000; // how often to check for expansion?

        [StarDataConstructor] ExpansionPlanner() {}

        public ExpansionPlanner(Empire empire)
        {
            Owner = empire;
            SetMaxSystemsToCheckedDiv(Owner.IsExpansionists ? 4 : 6);
            ResetExpandSearchTimer();
        }

        public Planet[] GetColonizationGoalPlanets()
        {
            return Owner.AI.SelectFromGoals((MarkForColonization c) => c.TargetPlanet);
        }

        public int GetNumOfBlockedColonyGoals()
        {
            int count = 0;
            foreach (var g in Owner.AI.Goals)
            {
                if (g is MarkForColonization c)
                {
                    float blocker = Owner.KnownEnemyStrengthIn(c.TargetPlanet.System);
                    if (blocker > Owner.CurrentMilitaryStrength / 10)
                        count++;
                }
            }

            return count;
        }

        public MarkForColonization[] GetColonizationGoals()
        {
            return Owner.AI.FindGoals<MarkForColonization>();
        }

        int DesiredColonyGoals()
        {
            float goals = Owner.DifficultyModifiers.BaseColonyGoals;
            if (Owner.isPlayer) 
                return (int)goals; // BaseColonyGoals for player

            goals += Owner.GetExpansionRatio();

            if (Owner.IsCybernetic)
                goals += 2;

            goals += GoalsModifierByRank();
            return (int)goals;
        }

        int GoalsModifierByRank() // increase goals if we are behind other empires
        {
            if (Owner.Universe.StarDate < 1002)
                return 0;

            var empires = Owner.Universe.ActiveMajorEmpires.SortedDescending(e => e.GetPlanets().Count);
            return (int)(empires.IndexOf(Owner) * Owner.DifficultyModifiers.ColonyGoalMultiplier);
        }

        bool CanConsiderExpanding(float popRatio, int numPlanets)
        {
            // expansion check limit applies to AI only
            ++ExpansionIntervalTimer;
            if (ExpansionIntervalTimer < Owner.DifficultyModifiers.ExpansionCheckInterval)
                return false;

            ExpansionIntervalTimer = 0;

            // if we have more than enough starting colonies, we can check required pop ratio
            if (numPlanets >= Owner.DifficultyModifiers.MinStartingColonies)
            {
                // required pop ratio balances current population against MAX possible population
                // before we have enough population across all planets, the AI will not consider expanding
                float requiredPopRatio = GlobalStats.Defaults.RequiredExpansionPopRatio
                                       * Owner.DifficultyModifiers.ExpansionMultiplier;

                // expansionist empires are willing to accept a -25% lower ratio before expanding
                if (Owner.IsExpansionists)
                    requiredPopRatio -= requiredPopRatio*0.25f;

                if (popRatio < requiredPopRatio)
                    return false; // we are still below required population ratio
            }

            return true;
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

            float popRatio = Owner.TotalPopBillion / Owner.MaxPopBillion.LowerBound(1);
            int ourPlanetsNum = Owner.GetPlanets().Count;
            if (!CanConsiderExpanding(popRatio, ourPlanetsNum))
                return;

            Planet[] currentColonizationGoals = GetColonizationGoalPlanets();
            int desiredGoals = DesiredColonyGoals();

            // we are going to ignore some of the blocked colony goals based on difficulty. 
            // at brutal no blocked colony goals will be counted. 
            int blockedColonyGoals = GetNumOfBlockedColonyGoals();
            blockedColonyGoals = (int)(blockedColonyGoals * Owner.DifficultyModifiers.ColonyGoalMultiplier);

            if (currentColonizationGoals.Length - blockedColonyGoals >= desiredGoals)
                return;

            Log.Info(ConsoleColor.Magenta, $"Running Expansion for {Owner.Name}, PopRatio: {popRatio.String(2)}");

            // We are going to keep a list of wanted planets. 
            // We are limiting the number of foreign systems to check based on galaxy size and race traits
            var allSystems = Owner.Universe.Systems;
            int maxCheckedSystems = (allSystems.Count / MaxSystemsToCheckedDiv).LowerBound(3);
            Vector2 empireCenter  = Owner.WeightedCenter;

            Array<Planet> potentialPlanets = GetPotentialPlanetsFromOwnedSystems();
            var potentialSystems = allSystems.Filter(CanBeColonized);
            if (potentialSystems.Length > 0)
            {
                potentialSystems.Sort(s => empireCenter.Distance(s.Position));
                potentialSystems = potentialSystems.TakeItems(maxCheckedSystems);
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
            if (allPlanetsRanker.Count > 0)
            {
                PlanetRanker bestValuePlanet = allPlanetsRanker.FindMax(pr => pr.Value);
                    CreateColonyGoals(bestValuePlanet.Planet, currentColonizationGoals.Length);
            }
        }

        /// <summary>
        /// Send colony ships to best targets;
        /// </summary>
        void CreateColonyGoals(Planet planet, int currentColonizationGoals)
        {
                Log.Info(ConsoleColor.Magenta,
                    $"Colonize {currentColonizationGoals + 1}/{DesiredColonyGoals()} | {planet} | {Owner}");

                Owner.AI.AddGoalAndEvaluate(new MarkForColonization(planet, Owner));
        }

        bool CanBeColonized(SolarSystem s)
        {
            return s.IsExploredBy(Owner)
                && !s.HasPlanetsOwnedBy(Owner)
                && s.PlanetList.Any(p => p.Habitable)
                && Owner.KnownEnemyStrengthIn(s).LessOrEqual(Owner.OffensiveStrength/3)
                && !s.OwnerList.Any(o=> !o.IsFaction && Owner.IsAtWarWith(o));
        }

        bool CanBeColonized(Planet p)
        {
            return p.IsExploredBy(Owner) && p.Habitable && (p.Owner == null || p.Owner.IsFaction);
        }

        Array<Planet> GetPotentialPlanetsNonLocal(SolarSystem[] systems)
        {
            var potentialPlanets = new Array<Planet>();
            foreach (SolarSystem system in systems)
            {
                foreach (Planet p in system.PlanetList)
                    if (CanBeColonized(p))
                        potentialPlanets.Add(p);
            }
            return potentialPlanets;
        }

        Array<Planet> GetPotentialPlanetsFromOwnedSystems()
        {
            var ownedSystems = Owner.GetOwnedSystems();
            var potentialPlanets = new Array<Planet>();
            foreach (SolarSystem system in ownedSystems)
            {
                foreach (Planet p in system.PlanetList)
                    if (CanBeColonized(p) && Owner.KnownEnemyStrengthIn(p.System) <= Owner.OffensiveStrength)
                        potentialPlanets.Add(p);
            }
            return potentialPlanets;
        }

        /// Go through the filtered planet list and rank them.
        bool GatherAllPlanetRanks(Array<Planet> planetList, Planet[] markedPlanets, Vector2 empireCenter, out Array<PlanetRanker> planetRanker)
        {
            planetRanker = new();
            if (planetList.Count == 0)
                return false;

            float longestDistance  = planetList.Last().Position.Distance(empireCenter);
        
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
            SolarSystem system = claimedPlanet.System;
            if (!Owner.isPlayer
                && !Owner.IsFaction
                && thievingEmpire != Owner
                && thiefRelationship.Known
                && !thiefRelationship.AtWar
                && system.HasPlanetsOwnedBy(thievingEmpire))
            {
                bool warnedThem = thiefRelationship.WarnedSystemsList.Contains(claimedPlanet.System);
                float distanceToUs   = system.Position.SqDist(Owner.WeightedCenter);
                float distanceToThem = system.Position.SqDist(thievingEmpire.WeightedCenter) 
                                       * Owner.PersonalityModifiers.CloserToUsClaimWarn;

                bool closerToUs = thievingEmpire.isPlayer && distanceToUs < distanceToThem;
                if (warnedThem || closerToUs)
                {
                    thiefRelationship.StoleOurColonyClaim(Owner, claimedPlanet, out bool newTheft);
                    if (thievingEmpire.isPlayer && newTheft)
                        thiefRelationship.WarnClaimThiefPlayer(claimedPlanet, Owner);
                }
            }
        }

        public bool AssignScoutSystemTarget(Ship ship, out SolarSystem targetSystem)
        {
            targetSystem = Owner.Random.ItemFilter(
                ship.Universe.Systems,
                sys => ship.System != sys && sys.IsFullyExploredBy(Owner)
                    && Owner.KnownEnemyStrengthIn(sys) > 10 && sys.ShipList.Any(s => s.IsGuardian));

            return targetSystem != null;
        }

        public bool AssignExplorationTargetSystem(Ship ship, out SolarSystem targetSystem)
        {
            targetSystem = null;
            var potentials = new Array<SolarSystem>();
            var potentialHostiles = new Array<SolarSystem>();

            for (int i = 0; i < ship.Universe.Systems.Count; i++)
            {
                SolarSystem s = ship.Universe.Systems[i];
                if (!s.IsFullyExploredBy(Owner) && !MarkedForExploration.Contains(s))
                {
                    if (Owner.KnownEnemyStrengthIn(s) < 10)
                        potentials.Add(s);
                    else
                        potentialHostiles.Add(s);
                }
            }

            if (potentials.Count == 0 && potentialHostiles.Count == 0)
                return false; // All systems were explored or are marked by someone else

            // Sort by distance from explorer center
            potentials.Sort(s => ship.Position.SqDist(s.Position));
            potentialHostiles.Sort(s => ship.Position.SqDist(s.Position));
            potentials.AddRange(potentialHostiles); // revisit hostile not full explored lastly

            targetSystem = potentials.First();

            MarkedForExploration.Add(targetSystem);
            return true;
        }

        public void RemoveExplorationTargetFromList(SolarSystem system)
        {
            if (system != null)
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