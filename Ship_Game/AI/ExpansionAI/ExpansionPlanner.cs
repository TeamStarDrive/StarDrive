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
        public Planet[] DesiredPlanets { get; private set; }
        private Array<Goal> Goals => OwnerEmpire.GetEmpireAI().Goals;
        Planet[] GetMarkedPlanets()
        {
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
                if (g.type == GoalType.Colonize)
                    list.Add(g.ColonizationTarget);
            return list.ToArray();
        }

        int DesiredColonyGoals
        {
            get
            {
                float baseValue = 1.1f; // @note This value is very sensitive, don't mess around without testing
                float diffMod = (float)CurrentGame.Difficulty * 2.5f * OwnerEmpire.Research.Strategy.ExpansionRatio;
                int plusGoals = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;
                float goals = (float)Math.Round(baseValue + diffMod + plusGoals, 0);
                return (int)goals.Clamped(1f, 5f);
            }
        }
        public ExpansionPlanner(Empire empire)
        {
            OwnerEmpire = empire;
            DesiredPlanets = Empty<Planet>.Array;
        }

        public void RunExpansionPlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoColonize)
                return;

            Planet[] markedPlanets = GetMarkedPlanets();
            int desired = DesiredColonyGoals;
            int difficulty = (int)CurrentGame.Difficulty * 2;
            int colonyEscorts = CountMarkedPlanetEscorts().Clamped(0, difficulty);

            if (markedPlanets.Length >= desired + colonyEscorts)
                return;

            Array<PlanetRanker> allPlanetsRanker = GatherAllPlanetRanks(markedPlanets);
            if (allPlanetsRanker.IsEmpty)
                return;

            PlanetRanker[] ranked = allPlanetsRanker.Sorted(v => -(v.Value - (v.OutOfRange ? 1 : 0)));
            DesiredPlanets = ranked.Select(p => p.Planet);

            Log.Info(ConsoleColor.Magenta, $"Colonize {markedPlanets.Length + 1}/{desired} | {ranked[0]} | {OwnerEmpire}");
            Goals.Add(new MarkForColonization(DesiredPlanets[0], OwnerEmpire));
        }


        /// Go through all known planets. filter planets by colonization rules. Rank remaining ones.
        Array<PlanetRanker> GatherAllPlanetRanks(Planet[] markedPlanets)
        {
            //need a better way to find biosphere
            bool canColonizeBarren = OwnerEmpire.GetBDict()["Biospheres"] || OwnerEmpire.IsCybernetic;
            var allPlanetsRanker   = new Array<PlanetRanker>();
            Vector2 weightedCenter = OwnerEmpire.GetWeightedCenter();
            float totalValue       = 0;
            int bestPlanetCount    = 0;
            // Here we should be using the building score that the governors use to determine is a planet is viable i think.
            // bool foodBonus      = OwnerEmpire.GetTDict()["Aeroponics"].Unlocked || OwnerEmpire.data.Traits.Cybernetic > 0;

            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem sys = UniverseScreen.SolarSystemList[i];

                if (!sys.IsExploredBy(OwnerEmpire) || IsColonizeBlockedByMorals(sys))
                    continue;

                AO ao = OwnerEmpire.GetEmpireAI().FindClosestAOTo(sys.Position);

                float systemEnemyStrength = OwnerEmpire.GetEmpireAI().ThreatMatrix.PingRadarStr(sys.Position, sys.Radius, OwnerEmpire, true);

                for (int y = 0; y < sys.PlanetList.Count; y++)
                {
                    Planet p = sys.PlanetList[y];
                    if (p.Habitable)
                    {
                        var r2 = new PlanetRanker(OwnerEmpire, p, canColonizeBarren, ao, systemEnemyStrength);
                        if (!r2.CantColonize)
                        {
                            allPlanetsRanker.Add(r2);
                            //if (!r2.OutOfRange)
                            {
                                totalValue += r2.Value;
                                bestPlanetCount++;
                            }
                        }
                    }
                }
            }

            var finalPlanetsRanker = new Array<PlanetRanker>();
            if (allPlanetsRanker.Count > 0)
            {
                allPlanetsRanker.Sort(p => -p.Value);
                float avgValue = totalValue / bestPlanetCount;
                int difficultyBonus = OwnerEmpire.isPlayer ? 0 : (int)CurrentGame.Difficulty -1;

                foreach (PlanetRanker rankedP in allPlanetsRanker)
                {
                    if (rankedP.CantColonize || markedPlanets.Contains(rankedP.Planet))
                        continue;
                    if (rankedP.Value + difficultyBonus > avgValue)
                    {
                        finalPlanetsRanker.Add(rankedP);
                    }
                }
            }
            return finalPlanetsRanker;
        }

        /// <summary>
        /// This will cause an empire to not colonize based on its personality.
        /// These values should be made common to set up common behavior types
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool IsColonizeBlockedByMorals(SolarSystem s)
        {
            if (s.OwnerList.Count == 0)
                return false;
            if (s.OwnerList.Contains(OwnerEmpire))
                return false;
            if (OwnerEmpire.isFaction)
                return false;
            if (OwnerEmpire.data?.DiplomaticPersonality == null)
                return false;
            bool atWar = OwnerEmpire.AllRelations.Any(war => war.Value.AtWar);
            bool trusting = OwnerEmpire.data.DiplomaticPersonality.IsTrusting;
            bool careless = OwnerEmpire.data.DiplomaticPersonality.Careless;

            if (atWar && careless) return false;

            foreach (Empire enemy in s.OwnerList)
                if (OwnerEmpire.IsEmpireAttackable(enemy) && !trusting)
                    return false;

            return true;

        }

        int CountMarkedPlanetEscorts()
        {

            int taskCount = 0;
            foreach (MilitaryTask escort in OwnerEmpire.GetEmpireAI().TaskList)
            {
                foreach (Guid held in escort.HeldGoals)
                {
                    if (held != Guid.Empty && OwnerEmpire.GetEmpireAI().
                            Goals.Any(g => g.guid == held && g is MarkForColonization))
                        taskCount++;
                }
            }
            return taskCount;
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
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (!s.IsExploredBy(OwnerEmpire))
                    potentials.Add(s);
            }

            foreach (SolarSystem s in MarkedForExploration)
                potentials.Remove(s);

            IOrderedEnumerable<SolarSystem> sortedList =
                from system in potentials
                orderby Vector2.Distance(OwnerEmpire.GetWeightedCenter(), system.Position)
                select system;
            if (!sortedList.Any())
            {
                queryingShip.AI.ClearOrders();
                return null;
            }
            SolarSystem nearestToHome = sortedList.OrderBy(furthest => Vector2.Distance(OwnerEmpire.GetWeightedCenter(), furthest.Position)).FirstOrDefault();
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.HostileForcesPresent(OwnerEmpire))
                    continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(OwnerEmpire.GetWeightedCenter(), nearest.Position);

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