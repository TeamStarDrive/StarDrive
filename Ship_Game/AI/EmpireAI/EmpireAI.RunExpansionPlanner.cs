using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        /// <summary>
        /// This uses difficult and empire personality to set the colonization goal count. 
        /// </summary>
        private int DesiredColonyGoals
        {
            get
            {
                int baseVal = 2;
                int difMod = (int)CurrentGame.Difficulty;
                difMod = (int)(difMod * OwnerEmpire.getResStrat().ExpansionRatio);
                int econmicPersonalityMod = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;

                //int waiting = Goals.FilterBy(g => g.type == GoalType.Colonize && (g as MarkForColonization)?.WaitingForEscort == true).Length;

                return baseVal + difMod + econmicPersonalityMod;// + waiting;
            }
        }

        private Planet[] DesiredPlanets  = new Planet[0];

        public void CheckClaim(KeyValuePair<Empire, Relationship> relKv, Planet claimedPlanet)
        {        

            if (OwnerEmpire.isPlayer || OwnerEmpire.isFaction) 
                return;

            Empire thievingEmpire        = relKv.Key;
            Relationship thiefRelationship = relKv.Value;

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
            if (markedPlanets.Length > DesiredColonyGoals)
                return;            

            var allPlanetsRanker = GatherAllPlanetRanks(markedPlanets);

            if (allPlanetsRanker.Count < 1)
                return;

            DesiredPlanets = allPlanetsRanker.Sorted(v => -(v.Value - (v.OutOfRange ? 1 :0))).Select(p => p.Planet).ToArray();

            if (DesiredPlanets.Length == 0)
                return;
            Goals.Add(new MarkForColonization(DesiredPlanets[0], OwnerEmpire));
        }

        /// <summary>
        /// Go through all known planets. filter planets by colonization rules. Rank remaining ones. 
        /// 
        /// </summary>
        /// <param name="markedPlanets"></param>
        /// <returns></returns>
        private Array<Goal.PlanetRanker>  GatherAllPlanetRanks(Planet[] markedPlanets)
        {
            //need a better way to find biosphere
            bool canColonizeBarren = OwnerEmpire.GetBDict()["Biospheres"] || OwnerEmpire.data.Traits.Cybernetic > 0;
            
            var allPlanetsRanker = new Array<Goal.PlanetRanker>();
            Vector2 weightedCenter = OwnerEmpire.GetWeightedCenter();
            //Here we should be using the building score that the governors use to determine is a planet is viable i think.
            //bool foodBonus = OwnerEmpire.GetTDict()["Aeroponics"].Unlocked || OwnerEmpire.data.Traits.Cybernetic > 0;
            
            for (int x = 0; x < UniverseScreen.SolarSystemList.Count; x++)
            {
                SolarSystem s = UniverseScreen.SolarSystemList[x];

                if (!s.IsExploredBy(OwnerEmpire))
                    continue;
               
                if (IsColonizeBlockedByMorals(s))
                    continue;

                float str = ThreatMatrix.PingRadarStr(s.Position, 150000f, OwnerEmpire, true);

                for (int y = 0; y < s.PlanetList.Count; y++)
                {
                    Planet planet = s.PlanetList[y];
                    if (!planet.Habitable)
                        continue;
                    if (planet.Owner != null)
                        continue;
                    if (markedPlanets.Contains(planet))
                        continue;

                    var r2 = new Goal.PlanetRanker(OwnerEmpire, planet, canColonizeBarren, weightedCenter, str);

                    if (r2.CantColonize)
                        continue;

                    allPlanetsRanker.Add(r2);
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

        private Planet[] GetMarkedPlanets()
        {            
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
                if (g.type == GoalType.Colonize) 
                    list.Add(g.GetMarkedPlanet());
            return list.ToArray();
        }
    }
}