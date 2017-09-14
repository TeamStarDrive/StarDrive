using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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
            int numColonyGoals = 0;
            DesiredColonyGoals = (int) Empire.Universe.GameDifficulty + 3;
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
            if (numColonyGoals >= DesiredColonyGoals +
                (OwnerEmpire.data.EconomicPersonality?.ColonyGoalsPlus ?? 0)) return;
            {
                Planet toMark      = null;
                var weightedCenter = new Vector2();
                int numPlanets     = 0;
                foreach (Planet p in OwnerEmpire.GetPlanets())
                {
                    for (int i = 0; (float)i < p.Population / 1000f; i++)
                    {
                        weightedCenter = weightedCenter + p.Center;
                        numPlanets++;
                    }
                }
                weightedCenter = weightedCenter / numPlanets;
                var ranker = new Array<Goal.PlanetRanker>();
                var allPlanetsRanker = new Array<Goal.PlanetRanker>();
                bool ok;
                foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                {
                    //added by gremlin make non offensive races act like it.
                    bool systemOK = true;
                    if (!OwnerEmpire.isFaction && OwnerEmpire.data?.DiplomaticPersonality != null && !(
                            (OwnerEmpire.AllRelations.Any(war => war.Value.AtWar) &&
                             OwnerEmpire.data.DiplomaticPersonality.Name != "Honorable")
                            || OwnerEmpire.data.DiplomaticPersonality.Name == "Agressive"
                            || OwnerEmpire.data.DiplomaticPersonality.Name == "Ruthless"
                            || OwnerEmpire.data.DiplomaticPersonality.Name == "Cunning")
                    )
                    {
                        foreach (Empire enemy in s.OwnerList)
                        {
                            if (enemy == OwnerEmpire || enemy.isFaction ||
                                OwnerEmpire.GetRelations(enemy).Treaty_Alliance) continue;
                            systemOK = false;

                            break;
                        }
                    }
                    if (!systemOK)
                        continue;
                    if (!s.IsExploredBy(OwnerEmpire))
                        continue;

                    float str = ThreatMatrix.PingRadarStr(s.Position, 300000f, OwnerEmpire, true);
                    if (str > 0f)
                        continue;

                    foreach (Planet planetList in s.PlanetList)
                    {
                        ok = true;
                        foreach (Goal g in Goals)
                        {
                            if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != planetList)
                                continue;

                            ok = false;
                        }
                        if (!ok)
                            continue;

                        str = ThreatMatrix.PingRadarStr(planetList.Center, 50000f, OwnerEmpire);
                        if (str > 0)
                            continue;
                        IOrderedEnumerable<AO> sorted =
                            from ao in OwnerEmpire.GetGSAI().AreasOfOperations
                            orderby Vector2.Distance(planetList.Center, ao.Center)
                            select ao;
                        if (sorted.Any())
                        {
                            AO closestAO = sorted.First();
                            if (planetList.Center.OutsideRadius(closestAO.Center, closestAO.Radius * 2f))
                                continue;
                        }
                        int commodities = 0;
                        //Added by gremlin adding in commodities
                        foreach (Building commodity in planetList.BuildingList)
                        {
                            if (!commodity.IsCommodity) continue;
                            commodities += 1;
                        }

                        float distanceInJumps;
                        if (planetList.IsExploredBy(OwnerEmpire)
                            && planetList.habitable
                            && planetList.Owner == null)
                        {
                            var r2 = new Goal.PlanetRanker()
                            {
                                Distance = Vector2.Distance(weightedCenter, planetList.Center)
                            };
                            distanceInJumps = r2.Distance / 400000f;
                            if (distanceInJumps < 1f)
                                distanceInJumps = 1f;

                            r2.planet = planetList;
                            //Cyberbernetic planet picker
                            if (OwnerEmpire.data.Traits.Cybernetic != 0)
                                r2.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) /
                                        distanceInJumps;
                            else
                                r2.PV = (commodities + planetList.MineralRichness + planetList.Fertility +
                                         planetList.MaxPopulation / 1000f) / distanceInJumps;


                            if (commodities > 0)
                                ranker.Add(r2);

                            if (planetList.Type == "Barren"
                                && commodities > 0
                                || OwnerEmpire.GetBDict()["Biospheres"]
                                || OwnerEmpire.data.Traits.Cybernetic != 0
                            )
                                ranker.Add(r2);

                            else if (planetList.Type != "Barren"
                                     && commodities > 0 || (double)planetList.Fertility >= .5f)                            
                                ranker.Add(r2);
                            
                            else if (OwnerEmpire.data.Traits.Cybernetic != 0
                                && (double)planetList.MineralRichness >= .5f || OwnerEmpire.GetTDict()["Aeroponics"].Unlocked)                            
                                ranker.Add(r2);
                            
                            else if (planetList.Type != "Barren")
                            {
                                if (OwnerEmpire.data.Traits.Cybernetic == 0)
                                    foreach (Planet food in OwnerEmpire.GetPlanets())
                                    {
                                        if (food.FoodHere > food.MAX_STORAGE * .7f &&
                                            food.fs == Planet.GoodState.EXPORT)
                                        {
                                            ranker.Add(r2);
                                            break;
                                        }
                                    }
                                else
                                {
                                    if (planetList.MineralRichness < .5f)
                                    {
                                        foreach (Planet food in OwnerEmpire.GetPlanets())
                                        {
                                            if (!(food.ProductionHere > food.MAX_STORAGE * .7f) &&
                                                food.ps != Planet.GoodState.EXPORT) continue;
                                            ranker.Add(r2);
                                            break;
                                        }
                                    }
                                    else                                    
                                        ranker.Add(r2);                                    
                                }
                            }
                        }
                        if (!planetList.IsExploredBy(OwnerEmpire)
                            || !planetList.habitable
                            || planetList.Owner == OwnerEmpire
                            || OwnerEmpire == EmpireManager.Player
                            && ThreatMatrix.PingRadarStr(planetList.Center, 50000f, OwnerEmpire) > 0f)
                            continue;

                        var r = new Goal.PlanetRanker()
                        {
                            Distance = Vector2.Distance(weightedCenter, planetList.Center)
                        };
                        distanceInJumps = r.Distance / 400000f;
                        if (distanceInJumps < 1f)
                            distanceInJumps = 1f;

                        r.planet = planetList;
                        if (OwnerEmpire.data.Traits.Cybernetic != 0)
                            r.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) /
                                   distanceInJumps;

                        else
                            r.PV = (commodities + planetList.MineralRichness + planetList.Fertility +
                                    planetList.MaxPopulation / 1000f) / distanceInJumps;

                        if (planetList.Type == "Barren"
                            && commodities > 0
                            || OwnerEmpire.GetBDict()["Biospheres"]
                            || OwnerEmpire.data.Traits.Cybernetic != 0)

                        {
                            if (planetList.Type == "Barren"
                                || planetList.Fertility < .5f
                                && !OwnerEmpire.GetTDict()["Aeroponics"].Unlocked
                                && OwnerEmpire.data.Traits.Cybernetic == 0)
                            {
                                foreach (Planet food in OwnerEmpire.GetPlanets())
                                {
                                    if (!(food.FoodHere > food.MAX_STORAGE * .9f) ||
                                        food.fs != Planet.GoodState.EXPORT) continue;
                                    allPlanetsRanker.Add(r);
                                    break;
                                }

                                continue;
                            }

                            allPlanetsRanker.Add(r);
                        }
                        else
                            allPlanetsRanker.Add(r);

                    }
                }
                if (ranker.Count > 0)
                {
                    var winner = new Goal.PlanetRanker();
                    float highest = 0f;
                    foreach (Goal.PlanetRanker pr in ranker)
                    {
                        if (pr.PV <= highest)
                            continue;

                        ok = true;
                        foreach (Goal g in Goals)
                        {
                            if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != pr.planet)
                            {
                                if (!g.Held || g.GetMarkedPlanet() == null ||
                                    g.GetMarkedPlanet().ParentSystem != pr.planet.ParentSystem)
                                    continue;

                                ok = false;
                                break;
                            }
                            ok = false;
                            break;
                        }
                        if (!ok)
                            continue;

                        winner = pr;
                        highest = pr.PV;
                    }
                    toMark = winner.planet;
                }
                if (allPlanetsRanker.Count > 0)
                {
                    DesiredPlanets.Clear();
                    IOrderedEnumerable<Goal.PlanetRanker> sortedList =
                        from ran in allPlanetsRanker
                        orderby ran.PV descending
                        select ran;
                    foreach (Goal.PlanetRanker planetRanker in sortedList)
                        DesiredPlanets.Add(planetRanker.planet);
                }
                if (toMark == null) return;

                ok = true;
                foreach (Goal g in Goals)
                {
                    if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != toMark)                    
                        continue;
                    
                    ok = false;
                }
                if (!ok) return;

                var cgoal = new Goal(toMark, OwnerEmpire)
                {
                    GoalName = "MarkForColonization"
                };
                Goals.Add(cgoal);

            }
        }
    }
}