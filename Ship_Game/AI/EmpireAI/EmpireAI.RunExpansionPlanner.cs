using System;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI.Tasks;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        /// This uses difficulty and empire personality to set the colonization goal count. 
        int DesiredColonyGoals
        {
            get
            {
                float baseValue = 1.1f; // @note This value is very sensitive, don't mess around without testing
                float diffMod   = (float)CurrentGame.Difficulty * 2.5f * OwnerEmpire.Research.Strategy.ExpansionRatio;
                int plusGoals   = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;
                float goals     = (float)Math.Round(baseValue + diffMod + plusGoals, 0);
                return (int)goals.Clamped(1f, 5f);
            }
        }

        Planet[] DesiredPlanets = Empty<Planet>.Array;

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

        private void RunExpansionPlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoColonize)
                return;

            Planet[] markedPlanets = GetMarkedPlanets();
            int desired = DesiredColonyGoals;
            int difficulty = (int)CurrentGame.Difficulty * 2;
            int colonyEscorts = GetMarkedPlanetEscorts().Clamped(0, difficulty);

            if (markedPlanets.Length >= desired + colonyEscorts)
                return;

            Array<Goal.PlanetRanker> allPlanetsRanker = GatherAllPlanetRanks(markedPlanets);
            if (allPlanetsRanker.IsEmpty)
                return;

            Goal.PlanetRanker[] ranked = allPlanetsRanker.Sorted(v => -(v.Value - (v.OutOfRange ? 1 :0)));
            DesiredPlanets = ranked.Select(p => p.Planet);

            Log.Info(System.ConsoleColor.Magenta, $"Colonize {markedPlanets.Length+1}/{desired} | {ranked[0]} | {OwnerEmpire}");
            Goals.Add(new MarkForColonization(DesiredPlanets[0], OwnerEmpire));
        }

        /// Go through all known planets. filter planets by colonization rules. Rank remaining ones. 
        Array<Goal.PlanetRanker> GatherAllPlanetRanks(Planet[] markedPlanets)
        {
            //need a better way to find biosphere
            bool canColonizeBarren = OwnerEmpire.GetBDict()["Biospheres"] || OwnerEmpire.IsCybernetic;
            
            var allPlanetsRanker = new Array<Goal.PlanetRanker>();
            Vector2 weightedCenter = OwnerEmpire.GetWeightedCenter();
            // Here we should be using the building score that the governors use to determine is a planet is viable i think.
            // bool foodBonus = OwnerEmpire.GetTDict()["Aeroponics"].Unlocked || OwnerEmpire.data.Traits.Cybernetic > 0;
            
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
            {
                SolarSystem sys = UniverseScreen.SolarSystemList[i];

                if (!sys.IsExploredBy(OwnerEmpire) || IsColonizeBlockedByMorals(sys))
                    continue;

                float str = ThreatMatrix.PingRadarStr(sys.Position, sys.Radius, OwnerEmpire, true);

                for (int y = 0; y < sys.PlanetList.Count; y++)
                {
                    Planet p = sys.PlanetList[y];
                    if (p.Habitable && p.Owner == null && !markedPlanets.Contains(p))
                    {
                        var r2 = new Goal.PlanetRanker(OwnerEmpire, p, canColonizeBarren, weightedCenter, str);
                        if (!r2.CantColonize)
                            allPlanetsRanker.Add(r2);
                    }
                }
            }
            return allPlanetsRanker;
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
            bool trusting = OwnerEmpire.data.DiplomaticPersonality.IsTrusting ;
            bool careless = OwnerEmpire.data.DiplomaticPersonality.Careless ;            

            if (atWar && careless) return false;

            foreach (Empire enemy in s.OwnerList)
                if (OwnerEmpire.IsEmpireAttackable(enemy) && !trusting)
                    return false;

            return true;
            
        }

        int GetMarkedPlanetEscorts()
        {

            int taskCount = 0;
            foreach (MilitaryTask escort in OwnerEmpire.GetEmpireAI().TaskList)
            {
                foreach (Guid held in escort.HeldGoals)
                {
                    if (held != Guid.Empty && OwnerEmpire.GetEmpireAI().
                            Goals.Any(g=> g.guid == held && g is MarkForColonization) )
                        taskCount++;
                }
            }
            return taskCount;
        }

        Planet[] GetMarkedPlanets()
        {            
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
                if (g.type == GoalType.Colonize) 
                    list.Add(g.ColonizationTarget);
            return list.ToArray();
        }
    }
}