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
        private int DesiredColonyGoals = 2;
        private readonly Array<Planet> DesiredPlanets = new Array<Planet>();

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
            var weightedCenter = new Vector2();

            if (FindCenterAndHungry(ref weightedCenter)) return;
            var ranker = new Array<Goal.PlanetRanker>();
            var allPlanetsRanker = new Array<Goal.PlanetRanker>();
            
            GatherAllPlanetRanks(weightedCenter, ranker, allPlanetsRanker);
            Planet toMark = MarkBestPlanet(ranker);
      
            if (toMark == null) return;

            if (allPlanetsRanker.Count > 0)
            {
                DesiredPlanets.Clear();
                IOrderedEnumerable<Goal.PlanetRanker> sortedList =
                    from ran in allPlanetsRanker
                    orderby ran.Value descending
                    select ran;
                foreach (Goal.PlanetRanker planetRanker in sortedList)
                    DesiredPlanets.Add(planetRanker.Planet);
            }

            bool ok = true;
            foreach (Goal g in Goals)
            {
                if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != toMark)
                    continue;

                ok = false;
            }
            if (!ok) return;

            Goals.Add(new MarkForColonization(toMark, OwnerEmpire));
        }

        private static Planet MarkBestPlanet(IReadOnlyCollection<Goal.PlanetRanker> ranker)
        {
            Planet toMark = null;
            if (ranker.Count > 0)
            {
                Goal.PlanetRanker winner;
                winner.Planet = null;
                float highest = float.MinValue;                
                foreach (Goal.PlanetRanker pr in ranker)
                {
                    if (pr.Value <= highest)
                        continue;                  

                    winner = pr;
                    highest = pr.Value;
                }
                toMark = winner.Planet;
            }
            return toMark;
        }

        private void GatherAllPlanetRanks(Vector2 weightedCenter, Array<Goal.PlanetRanker> ranker, Array<Goal.PlanetRanker> allPlanetsRanker)
        {
            bool canColonizeBarren = OwnerEmpire.GetBDict()["Biospheres"] || OwnerEmpire.data.Traits.Cybernetic > 0;
            bool foodBonus = OwnerEmpire.GetTDict()["Aeroponics"].Unlocked || OwnerEmpire.data.Traits.Cybernetic > 0;
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                //added by gremlin make non offensive races act like it.
                if (!s.IsExploredBy(OwnerEmpire))
                    continue;
                if (ColonizeBlockedByMorals(s)) continue;

                float str = ThreatMatrix.PingRadarStr(s.Position, 300000f, OwnerEmpire, true);
                if (str > 0) continue;
                foreach (Planet planetList in s.PlanetList)
                {
                    if (!planetList.Habitable)
                        continue;
                    if (planetList.Owner != null) continue;
                    if (IsAlreadyMarked(planetList, str)) continue;
                    int commodities = planetList.CommoditiesPresent.Count;
                    
                    Goal.PlanetRanker r2 = PlanetRank(weightedCenter, planetList, commodities);
                    if (r2.Value < .3f )
                        continue;
                    bool hasCommodities = commodities > 0;                  

                    if (IsBadWorld(planetList, canColonizeBarren, hasCommodities, foodBonus)) continue;
                    r2.OutOfRange = PlanetToFarToColonize(planetList);

                    allPlanetsRanker.Add(r2);

                    
                    

                    if (str >0 && ThreatMatrix.PingRadarStr(planetList.Center, 50000f, OwnerEmpire, false, any: true) >0 )
                        continue;
                    ranker.Add(r2);
                    
                }
            }
        }

        private bool PlanetToFarToColonize(Planet planetList)
        {
            var closestAO = OwnerEmpire.GetGSAI().AreasOfOperations
                .FindMin(ao => ao.Center.SqDist(planetList.Center));
            if (closestAO != null && planetList.Center.OutsideRadius(closestAO.Center, closestAO.Radius * 2f))
                return true;
            return false;
        }

        private Goal.PlanetRanker PlanetRank(Vector2 weightedCenter, Planet planetList, int commodities)
        {
            var r2 = new Goal.PlanetRanker()
            {
                Distance = Vector2.Distance(weightedCenter, planetList.Center)
            };
            float distanceInJumps = Math.Max(r2.Distance / 600000, 1);
            
            r2.JumpRange = distanceInJumps;
            r2.Planet = planetList;            
            
            float baseValue = planetList.EmpireBaseValue(OwnerEmpire);

            r2.Value = baseValue / distanceInJumps;

            return r2;
        }

        private static bool IsBadWorld(Planet planetList, bool canColonizeBarren, bool hasCommodities, bool foodBonus)
        {
            if (planetList.Type == "Barren"
                && !canColonizeBarren && !hasCommodities)
                return true;

            //if (!foodBonus && planetList.Fertility < 1 && !hasCommodities)
            //    return true;
            return false;
        }

        private bool ColonizeBlockedByMorals(SolarSystem s)
        {
            if (s.OwnerList.Count == 0) return false;
            if (s.OwnerList.Contains(OwnerEmpire)) return false;
            if (OwnerEmpire.isFaction) return false;
            if (OwnerEmpire.data?.DiplomaticPersonality == null) return false;
            bool atWar = OwnerEmpire.AllRelations.Any(war => war.Value.AtWar);
            bool trusting = OwnerEmpire.data?.DiplomaticPersonality.Trustworthiness >= 80;
            bool careless = OwnerEmpire.data?.DiplomaticPersonality.Trustworthiness <= 60;
            string personality = OwnerEmpire.data.DiplomaticPersonality.Name;

            if (atWar && careless) return false;

            bool systemOK = true;

            foreach (Empire enemy in s.OwnerList)
            {
                if (OwnerEmpire.IsEmpireAttackable(enemy)  && !trusting) return false;
                systemOK = enemy.isFaction;
            }
            
            return true;
            
        }

        private bool IsAlreadyMarked(Planet planetList,float str)
        {
            bool ok = true;
            Goal remove = null;
            foreach (Goal g in Goals)
            {
                if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != planetList)
                    continue;

                ok = false;
                if (str > 0) remove = g;
                break;
            }
            if (!ok)
            {
                if (remove != null)
                    Goals.Remove(remove);
                return true;
            }
            return false;
        }

        private bool FindCenterAndHungry(ref Vector2 weightedCenter)
        {
            DesiredColonyGoals = (int) Empire.Universe.GameDifficulty + 3 +
                                 (OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0);
            int numColonyGoals = NumColonyGoals();
            if (numColonyGoals >= DesiredColonyGoals) return true;


            int numPlanets = 0;
            foreach (Planet p in OwnerEmpire.GetPlanets())
            {
                //if (p.NeedsFood())
                //    numColonyGoals++;
                for (int i = 0; (float) i < p.Population / 1000f; i++)
                {
                    weightedCenter = weightedCenter + p.Center;
                    numPlanets++;
                }
                if (numColonyGoals <= 0) break;
            }
            if (numColonyGoals >= DesiredColonyGoals)
                return true;

            weightedCenter = weightedCenter / numPlanets;
            return false;
        }

        private int NumColonyGoals()
        {
            int numColonyGoals = 0;
            foreach (Goal g in Goals)
            {
                if (g.type != GoalType.Colonize)
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