using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        /// <summary>
        /// This uses difficulty and empire personality to set the colonization goal count. 
        /// </summary>
        private int DesiredColonyGoals
        {
            get
            {
                float baseValue = 1.0f;
                float difMod = (float)CurrentGame.Difficulty;
                difMod *= OwnerEmpire.GetResStrat().ExpansionRatio;
                int plusColonyGoals = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;

                float goals = (float)System.Math.Round(baseValue + difMod + plusColonyGoals);
                return (int)goals.Clamped(1f, 5f);
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
            if (markedPlanets.Length >= DesiredColonyGoals)
                return;            

            Array<Goal.PlanetRanker> allPlanetsRanker = GatherAllPlanetRanks(markedPlanets);
            if (allPlanetsRanker.IsEmpty)
                return;

            Goal.PlanetRanker[] ranked = allPlanetsRanker.Sorted(v => -(v.Value - (v.OutOfRange ? 1 :0)));
            DesiredPlanets = ranked.Select(p => p.Planet);

            Log.Info(System.ConsoleColor.Magenta, $"Colonize {markedPlanets.Length}/{DesiredColonyGoals} | {ranked[0]} | {OwnerEmpire}");
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

        private Planet[] GetMarkedPlanets()
        {            
            var list = new Array<Planet>();
            foreach (Goal g in Goals)
                if (g.type == GoalType.Colonize) 
                    list.Add(g.ColonizationTarget);
            return list.ToArray();
        }
    }
}