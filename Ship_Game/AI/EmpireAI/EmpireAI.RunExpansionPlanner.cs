using System;
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
                var baseVal = 2;
                var difMod = (int)Empire.Universe.GameDifficulty;
                difMod = (int)(difMod + OwnerEmpire.getResStrat().ExpansionRatio);
                int econmicPersonalityMod = OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0;                

                return baseVal + difMod + econmicPersonalityMod;
            }
        }


        private Planet[] DesiredPlanets  = new Planet[0];

        public void CheckClaim(KeyValuePair<Empire, Relationship> them, Planet claimedPlanet)
        {
            if (OwnerEmpire == Empire.Universe.PlayerEmpire)            
                return;
            
            if (OwnerEmpire.isFaction)            
                return;
            
            if (!them.Value.Known)            
                return;
            
            if (them.Value.WarnedSystemsList.Contains(claimedPlanet.ParentSystem.guid) 
                && claimedPlanet.Owner == them.Key 
                && !them.Value.AtWar)
            {
                bool theyAreThereAlready = false;
                foreach (Planet p in claimedPlanet.ParentSystem.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)                    
                        continue;
                    
                    theyAreThereAlready = true;
                }
                if (!theyAreThereAlready || them.Key != Empire.Universe.PlayerEmpire) return;

                Relationship item = OwnerEmpire.GetRelations(them.Key);
                item.Anger_TerritorialConflict = item.Anger_TerritorialConflict +
                                                 (5f + (float) Math.Pow(5,
                                                      OwnerEmpire.GetRelations(them.Key).NumberStolenClaims));
                OwnerEmpire.GetRelations(them.Key).UpdateRelationship(OwnerEmpire, them.Key);
                Relationship numberStolenClaims = OwnerEmpire.GetRelations(them.Key);
                numberStolenClaims.NumberStolenClaims = numberStolenClaims.NumberStolenClaims + 1;
                if (OwnerEmpire.GetRelations(them.Key).NumberStolenClaims == 1 && !OwnerEmpire.GetRelations(them.Key)
                        .StolenSystems.Contains(claimedPlanet.guid))
                {
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Stole Claim", claimedPlanet.ParentSystem));
                }
                else if (OwnerEmpire.GetRelations(them.Key).NumberStolenClaims == 2 &&
                         !OwnerEmpire.GetRelations(them.Key).HaveWarnedTwice && !OwnerEmpire.GetRelations(them.Key)
                             .StolenSystems.Contains(claimedPlanet.ParentSystem.guid))
                {
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Stole Claim 2", claimedPlanet.ParentSystem));
                    OwnerEmpire.GetRelations(them.Key).HaveWarnedTwice = true;
                }
                else if (OwnerEmpire.GetRelations(them.Key).NumberStolenClaims >= 3 &&
                         !OwnerEmpire.GetRelations(them.Key).HaveWarnedThrice && !OwnerEmpire.GetRelations(them.Key)
                             .StolenSystems.Contains(claimedPlanet.ParentSystem.guid))
                {
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Stole Claim 3", claimedPlanet.ParentSystem));
                    OwnerEmpire.GetRelations(them.Key).HaveWarnedThrice = true;
                }
                OwnerEmpire.GetRelations(them.Key).StolenSystems.Add(claimedPlanet.ParentSystem.guid);
            }
        }

        private void RunExpansionPlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoColonize)
                return;
                Planet[] markedPlanets = GetMarkedPlanets();
            if (markedPlanets.Length > DesiredColonyGoals) return;            

            var allPlanetsRanker = GatherAllPlanetRanks(markedPlanets);

            if (allPlanetsRanker.Count < 1)
                return;

            DesiredPlanets = allPlanetsRanker.SortedBy(v => -(v.Value - (v.OutOfRange ? 1 :0))).Select(p => p.Planet).ToArray();

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

        private int NumColonyGoals()
        {
            int numColonyGoals = 0;
            foreach (Goal g in Goals)
            {
                if (g.type != GoalType.Colonize || g.Held)
                    continue;

                //added by Gremlin: Colony expansion changes
                Planet markedPlanet = g.GetMarkedPlanet();
                if (markedPlanet?.ParentSystem == null) continue;

                if (markedPlanet.ParentSystem.ShipList.Any(ship => ship.loyalty != null && ship.loyalty.isFaction))
                    --numColonyGoals;
                ++numColonyGoals;
            }
            return numColonyGoals;
        }
    }
}