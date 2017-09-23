using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private EmpireAI.ResearchStrategy res_strat = EmpireAI.ResearchStrategy.Scripted;
        private int ScriptIndex = 0;
        int hullScaler = 1;
        private string postResearchTopic = "";
        Ship BestCombatShip;
        private void RunResearchPlanner()
        {
            if (!string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
                return;
            bool cybernetic = OwnerEmpire.data.Traits.Cybernetic > 0;
            bool atWar = false;
            bool highTaxes = false;
            bool lowResearch = false;
            bool lowincome = false;
            float researchDebt = 0;
            float wars = OwnerEmpire.AllRelations
                .Where(war => !war.Key.isFaction && (war.Value.AtWar || war.Value.PreparingForWar))
                .Sum(str => str.Key.currentMilitaryStrength / OwnerEmpire.currentMilitaryStrength);

            if (OwnerEmpire.data.TaxRate >= .50f)
                highTaxes = true;
            if (!string.IsNullOrEmpty(postResearchTopic))
                researchDebt = .1f + (OwnerEmpire.TechnologyDict[postResearchTopic].TechCost /
                                      (.1f + (100 * UniverseScreen.GamePaceStatic) *
                                       OwnerEmpire.GetPlanets().Sum(research => research.NetResearchPerTurn)));
            if (researchDebt > 4)
                lowResearch = true;
            float economics = (OwnerEmpire.data.TaxRate * 10); 
            float needsFood = 0;
            foreach (Planet hunger in OwnerEmpire.GetPlanets())
            {
                if ((cybernetic ? hunger.ProductionHere : hunger.FoodHere) / hunger.MAX_STORAGE < .20f)                
                    needsFood++;
                
                if (!OwnerEmpire.GetTDict()["Biospheres"].Unlocked)
                {
                    if (hunger.Fertility == 0)
                        needsFood += 2;
                }
            }
            float shipBuildBonus = 0f;
            if (OwnerEmpire.data.TechDelayTime > 0)
                OwnerEmpire.data.TechDelayTime--;
            if (OwnerEmpire.data.TechDelayTime > 0)
            {
                shipBuildBonus = -5 - OwnerEmpire.data.TechDelayTime;
            }
            else
                shipBuildBonus = 0;

            needsFood = needsFood > 0 ? needsFood / OwnerEmpire.GetPlanets().Count : 0;
            needsFood *= 10;

            switch (res_strat)
            {
                case EmpireAI.ResearchStrategy.Random:
                    {
                        ChooseRandomTech(wars, shipBuildBonus, researchDebt, cybernetic, needsFood, economics);
                        return;
                    }
                case EmpireAI.ResearchStrategy.Scripted:
                    {
                        if (ProcessScript(atWar, highTaxes, lowResearch, lowincome)) return;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
            if (!string.IsNullOrEmpty(OwnerEmpire.ResearchTopic) &&
                OwnerEmpire.ResearchTopic != postResearchTopic)
            {
                postResearchTopic = OwnerEmpire.ResearchTopic;
                return;
            }
            res_strat = ResearchStrategy.Random;
        }

        private void ChooseRandomTech(float wars, float shipBuildBonus, float researchDebt, bool cybernetic, float needsFood,
            float economics)
        {
            Map<string, float> priority = new Map<string, float>();
            var resStrat = OwnerEmpire.getResStrat();

            priority.Add("SHIPTECH", randomizer(resStrat.MilitaryPriority, wars + shipBuildBonus));

            priority.Add("Research", randomizer(resStrat.ResearchPriority, (researchDebt)));
            priority.Add("Colonization",
                randomizer(resStrat.ExpansionPriority, (!cybernetic ? needsFood : -1)));
            priority.Add("Economic", randomizer(resStrat.ExpansionPriority, (economics)));
            priority.Add("Industry",
                randomizer(resStrat.IndustryPriority, (cybernetic ? needsFood : 0)));
            priority.Add("General", randomizer(resStrat.ResearchPriority, 0));
            priority.Add("GroundCombat",
                randomizer(resStrat.MilitaryPriority, (wars + shipBuildBonus) * .5f));

            string sendToScript = string.Empty;
            int max = 0;
            foreach (var pWeighted in priority.OrderByDescending(pri => pri.Value))
            {
                if (max > priority.Count)
                    break;
                if (pWeighted.Value < 0)
                    continue;
                priority[pWeighted.Key] = -1;
                sendToScript += ":";
                if (pWeighted.Key == "SHIPTECH")
                {
                    sendToScript += "ShipWeapons:ShipDefense:ShipGeneral:ShipHull";
                    max += 4;
                }
                else
                {
                    sendToScript += pWeighted.Key;
                    max++;
                }
            }
            if (ScriptedResearch("CHEAPEST", "TECH", "TECH" + sendToScript))
                return;

            return;
        }

        private bool ProcessScript(bool atWar, bool highTaxes, bool lowResearch, bool lowincome)
        {
            int loopcount = 0;
            Start:
            if (OwnerEmpire.getResStrat() != null &&
                ScriptIndex < OwnerEmpire.getResStrat().TechPath.Count &&
                loopcount < OwnerEmpire.getResStrat().TechPath.Count)
            {
                string scriptentry = OwnerEmpire.getResStrat().TechPath[ScriptIndex].id;
                string scriptCommand = OwnerEmpire.GetTDict().ContainsKey(scriptentry)
                    ? scriptentry
                    : scriptentry.Split(':')[0];
                Log.Info($"{OwnerEmpire.PortraitName} : Script Command : {scriptCommand} ");
                switch (scriptCommand)
                {
                    case "SCRIPT":
                    {
                        string modifier = "";
                        string[] script = scriptentry.Split(':');

                        if (script.Count() > 2)
                        {
                            modifier = script[2];
                        }
                        ScriptIndex++;
                        if (ScriptedResearch("CHEAPEST", script[1], modifier))
                            return true;
                        loopcount++;
                        goto Start;
                    }
                    case "LOOP":
                    {
                        ScriptIndex =
                            int.Parse(OwnerEmpire.getResStrat().TechPath[ScriptIndex].id
                                .Split(':')[1]);
                        loopcount++;
                        goto Start;
                    }
                    case "CHEAPEST":
                    {
                        string modifier = "";
                        string[] script = scriptentry.Split(':');

                        if (script.Count() == 1)
                        {
                            res_strat = EmpireAI.ResearchStrategy.Random;
                            RunResearchPlanner();
                            res_strat = EmpireAI.ResearchStrategy.Scripted;
                            ScriptIndex++;
                            return true;
                        }
                        string[] modifiers = new string[script.Count() - 1];
                        for (int i = 1; i < script.Count(); i++)
                        {
                            modifiers[i - 1] = script[i];
                        }
                        modifier = String.Join(":", modifiers);
                        ScriptIndex++;
                        if (ScriptedResearch(scriptCommand, script[1], modifier))
                            return true;
                        loopcount++;
                        goto Start;
                    }
                    case "EXPENSIVE":
                    {
                        string modifier = "";
                        string[] script = scriptentry.Split(':');

                        if (script.Count() == 1)
                        {
                            res_strat = EmpireAI.ResearchStrategy.Random;
                            RunResearchPlanner();
                            res_strat = EmpireAI.ResearchStrategy.Scripted;
                            ScriptIndex++;
                            return true;
                        }
                        string[] modifiers = new string[script.Count() - 1];
                        for (int i = 1; i < script.Count(); i++)
                        {
                            modifiers[i - 1] = script[i];
                        }
                        modifier = String.Join(":", modifiers);
                        ScriptIndex++;
                        if (ScriptedResearch(scriptCommand, script[1], modifier))
                            return true;
                        loopcount++;
                        goto Start;
                    }
                    case "IFWAR":
                    {
                        loopcount += ScriptBump(atWar);
                        goto Start;
                    }
                    case "IFHIGHTAX":
                    {
                        loopcount += ScriptBump(highTaxes);
                        goto Start;
                    }
                    case "IFPEACE":
                    {
                        loopcount += ScriptBump(!atWar);
                        goto Start;
                    }
                    case "IFCYBERNETIC":
                    {
                        loopcount += ScriptBump(OwnerEmpire.data.Traits.Cybernetic > 0);
                        goto Start;
                    }
                    case "IFLOWRESEARCH":
                    {
                        loopcount += ScriptBump(lowResearch);
                        goto Start;
                    }
                    case "IFNOTLOWRESEARCH":
                    {
                        loopcount += ScriptBump(!lowResearch);
                        goto Start;
                    }
                    case "IFLOWINCOME":
                    {
                        loopcount += ScriptBump(lowincome);
                        goto Start;
                    }
                    case "IFNOTLOWINCOME":
                    {
                        loopcount += ScriptBump(!lowincome);
                        goto Start;
                    }
                    case "RANDOM":
                    {
                        res_strat = EmpireAI.ResearchStrategy.Random;
                        RunResearchPlanner();
                        res_strat = EmpireAI.ResearchStrategy.Scripted;
                        ScriptIndex++;
                        return true;
                    }
                    default:
                    {
                        TechEntry defaulttech;
                        if (OwnerEmpire.GetTDict().TryGetValue(scriptentry, out defaulttech))
                        {
                            if (defaulttech.Unlocked)

                            {
                                ScriptIndex++;
                                goto Start;
                            }
                            if (!defaulttech.Unlocked && OwnerEmpire.HavePreReq(defaulttech.UID))
                            {
                                OwnerEmpire.ResearchTopic = defaulttech.UID;
                                ScriptIndex++;
                                if (!string.IsNullOrEmpty(scriptentry))
                                    return true;
                            }
                        }
                        else
                        {
                            Log.Info($"TechNotFound : {scriptentry}");
                            ScriptIndex++;
                        }


                        foreach (EconomicResearchStrategy.Tech tech in OwnerEmpire.getResStrat()
                            .TechPath)
                        {
                            if (!OwnerEmpire.GetTDict().ContainsKey(tech.id) ||
                                OwnerEmpire.GetTDict()[tech.id].Unlocked ||
                                !OwnerEmpire.HavePreReq(tech.id))
                            {
                                continue;
                            }

                            OwnerEmpire.ResearchTopic = tech.id;
                            ScriptIndex++;
                            if (!string.IsNullOrEmpty(tech.id))
                                return true;
                        }
                        res_strat = EmpireAI.ResearchStrategy.Random;
                        ScriptIndex++;
                        return true;
                    }
                }
            }
            if (string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
            {
                res_strat = EmpireAI.ResearchStrategy.Random;
            }
            return false;
        }

        private int ScriptBump(bool check, int index = 1)
        {            
            if (check)
            {
                ScriptIndex =
                    int.Parse(
                        OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);                
                return 1;
            }
            ScriptIndex++;
            return 0;
        }

        private bool ScriptedResearch(string command1, string command2, string modifier)
        {
            Array<Technology> availableTechs = new Array<Technology>();

            foreach (var kv in OwnerEmpire.TechnologyDict)
            {
                if (!kv.Value.Discovered || !kv.Value.shipDesignsCanuseThis || kv.Value.Unlocked ||
                    !OwnerEmpire.HavePreReq(kv.Key))
                    continue;
                if (kv.Value.Tech.RootNode == 1)
                {
                    kv.Value.Unlocked = true;
                    continue;
                }
                ;
                availableTechs.Add(kv.Value.Tech);
            }

            if (availableTechs.Count <= 0)
            {
                return false;
            }

            List<string> useableTech = new List<string>();


            string researchtopic = "";
            TechnologyType techtype;

            #region hull checking.

            OwnerEmpire.UpdateShipsWeCanBuild();


            //Ship BestShip = null;// ""; //BestCombatShip;          //Not referenced in code, removing to save memory
            //float bestShipStrength = 0f;          //Not referenced in code, removing to save memory
            float techcost = -1;
            float str = 0;
            float moneyNeeded = OwnerEmpire.data.ShipBudget * .2f;
            //float curentBestshipStr = 0;          //Not referenced in code, removing to save memory

            if (BestCombatShip != null)
            {
                //empire.UpdateShipsWeCanBuild();
                if (OwnerEmpire.ShipsWeCanBuild.Contains(BestCombatShip.Name))
                    BestCombatShip = null;
            }
            if (BestCombatShip == null && (modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") ||
                                                modifier.Contains("ShipGeneral")
                                                || modifier.Contains("ShipHull")))
            {
                //List<string> globalShipTech = new List<string>();
                //foreach (string purgeRoots in OwnerEmpire.ShipTechs)
                //{
                //    Technology bestshiptech = null;
                //    if (!ResourceManager.TechTree.TryGetValue(purgeRoots, out bestshiptech))
                //        continue;
                //    switch (bestshiptech.TechnologyType)
                //    {
                //        case TechnologyType.General:
                //        case TechnologyType.Colonization:
                //        case TechnologyType.Economic:
                //        case TechnologyType.Industry:
                //        case TechnologyType.Research:
                //        case TechnologyType.GroundCombat:
                //            continue;
                //        case TechnologyType.ShipHull:
                //            break;
                //        case TechnologyType.ShipDefense:
                //            break;
                //        case TechnologyType.ShipWeapons:
                //            break;
                //        case TechnologyType.ShipGeneral:
                //            break;
                //        default:
                //            break;
                //    }
                //    globalShipTech.Add(bestshiptech.UID);
                //}
                
                int AddMatchingTechCost(Ship ship, Array< string> techList)
                {
                    int techCost = 0;
                    foreach (string shipTech in ship.GetShipData().techsNeeded)
                    {
                        if (!string.IsNullOrEmpty(techList.Find(tech => shipTech == tech)))
                        {
                            techCost += (int)OwnerEmpire.TechnologyDict[shipTech].TechCost;
                        }
                    }
                    return techCost;
                }
                foreach (Technology bestshiptech in availableTechs)
                {
                    switch (bestshiptech.TechnologyType)
                    {
                        case TechnologyType.General:
                        case TechnologyType.Colonization:
                        case TechnologyType.Economic:
                        case TechnologyType.Industry:
                        case TechnologyType.Research:
                        case TechnologyType.GroundCombat:
                            continue;
                        case TechnologyType.ShipHull:
                            break;
                        case TechnologyType.ShipDefense:
                            break;
                        case TechnologyType.ShipWeapons:
                            break;
                        case TechnologyType.ShipGeneral:
                            break;
                        default:
                            break;
                    }
                    useableTech.Add(bestshiptech.UID);
                }


                //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic. 

                bool shipchange = false;
                bool hullKnown = true;
                // do
                {
                    foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values.OrderBy(tech => tech.shipData
                        .TechScore)) 
                    {
                        try
                        {
                            if (shortTermBest.shipData.HullRole < ShipData.RoleName.fighter ||
                                shortTermBest.shipData.Role == ShipData.RoleName.prototype)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }

                        if (shortTermBest.shipData.ShipStyle != OwnerEmpire.data.Traits.ShipType && !OwnerEmpire.IsHullUnlocked(shortTermBest.shipData.Hull))                                                
                            continue;
                        
                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                            continue;
                        if (OwnerEmpire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                            continue;
                        if (!shortTermBest.shipData.techsNeeded.Intersect(useableTech).Any())
                            continue;
                        if (!shortTermBest.ShipGoodToBuild(OwnerEmpire))
                            continue;
                        

                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                        {
                            if (Empire.Universe.Debug)
                            {
                                Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                            }
                            continue;
                        }
                        //forget the cost of tech that provide these ships. These are defined in techentry class.
                        int mod = 0;
                        if (!OwnerEmpire.canBuildBombers && shortTermBest.DesignRole == ShipData.RoleName.bomber)                        
                            mod = AddMatchingTechCost(shortTermBest, OwnerEmpire.BomberTech);
                        
                        if (!OwnerEmpire.canBuildCarriers && shortTermBest.DesignRole == ShipData.RoleName.carrier)
                            mod = AddMatchingTechCost(shortTermBest, OwnerEmpire.CarrierTech);

                        if (!OwnerEmpire.canBuildTroopShips && shortTermBest.DesignRole == ShipData.RoleName.troopShip)
                            mod = AddMatchingTechCost(shortTermBest, OwnerEmpire.TroopShipTech);

                        if (!OwnerEmpire.canBuildTroopShips && shortTermBest.DesignRole == ShipData.RoleName.support)
                            mod = AddMatchingTechCost(shortTermBest, OwnerEmpire.SupportShipTech);


                        if (!OwnerEmpire.canBuildFrigates &&
                            shortTermBest.shipData.HullRole == ShipData.RoleName.cruiser)
                            continue;
                        //try to line focus to main goal but if we cant, line focus as best as possible by what we already have. 
                        Array<string> currentTechs =
                            new Array<string>(shortTermBest.shipData.techsNeeded.Except(OwnerEmpire.ShipTechs));
                        int currentTechCost = (int)ResourceManager.TechTree.Values
                            .Sum(tech => currentTechs.Contains(tech.UID) ? tech.Cost : 0);
                            
                        currentTechCost -= mod;

                        currentTechCost = currentTechCost / (int)(OwnerEmpire.Research * 10 + 1);

                        if ((currentTechCost < techcost && str < shortTermBest.shipData.BaseStrength) || techcost < 0)
                        {
                            if (techcost > 0)
                                str = shortTermBest.shipData.BaseStrength;
                            BestCombatShip = shortTermBest;
                            techcost = currentTechCost; // techratio;
                            shipchange = true;
                            
                        }
                    }

                    if (shipchange)
                    {
                        if (Empire.Universe.Debug)
                        {
                            Log.Info(OwnerEmpire.data.PortraitName + " : NewBestShip :" + BestCombatShip.Name +
                                     " : " + BestCombatShip.shipData.HullRole);
                        }
                    }

                    if (BestCombatShip != null && OwnerEmpire.GetHDict()
                            .TryGetValue(BestCombatShip.shipData.Hull, out hullKnown))
                    {
                        if (hullKnown)
                            hullScaler++;
                    }
                    else
                        hullScaler++;

                    //End of line focusing. 
                } ///while (BestCombatShip == null && hullScaler < 10 );
                if (!hullKnown)
                    hullScaler = 1;
            }


            //now that we have a target ship to buiild filter out all the current techs that are not needed to build it. 
            Array<Technology> bestShiptechs = new Array<Technology>();
            if ((modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") ||
                 modifier.Contains("ShipGeneral")
                 || modifier.Contains("ShipHull")))
            {
                if (BestCombatShip != null)
                {
                    //command2 = "SHIPTECH"; //use the shiptech choosers which just chooses tech in the list. 
                    foreach (string shiptech in BestCombatShip.shipData.techsNeeded)
                    {
                        Technology test = null;
                        if (ResourceManager.TechTree.TryGetValue(shiptech, out test))
                        {
                            bool skiprepeater = false;
                            //repeater compensator. This needs some deeper logic. I current just say if you research one level. Dont research any more.
                            if (test.MaxLevel > 0)
                            {
                                foreach (TechEntry repeater in OwnerEmpire.TechnologyDict.Values)
                                {
                                    if (test.UID == repeater.UID && (repeater.Level > 0))
                                    {
                                        skiprepeater = true;
                                        break;
                                    }
                                }
                                if (skiprepeater)
                                    continue;
                            }
                            bestShiptechs.Add(test);
                        }
                    }

                    bestShiptechs = availableTechs.Intersect(bestShiptechs).ToArrayList();
                }
                else
                    Log.Info(OwnerEmpire.data.PortraitName + " : NoShipFound :" + hullScaler + " : ");
            }
            HashSet<Technology> remove = new HashSet<Technology>();
            foreach (Technology test in availableTechs)
            {
                if (test.MaxLevel > 1)
                {
                    bool skiprepeater = false;
                    foreach (TechEntry repeater in OwnerEmpire.TechnologyDict.Values)
                    {
                        if (test.UID == repeater.UID && repeater.Level > 0)
                        {
                            skiprepeater = true;
                            remove.Add(test);
                            break;
                        }
                    }
                    if (skiprepeater)
                        continue;
                }
            }

            availableTechs = availableTechs.Except(remove).ToArrayList();
            Array<Technology> workingSetoftechs = availableTechs;

            #endregion

            float CostNormalizer = .01f;
            int previousCost = int.MaxValue;
            switch (command2)
            {
                case "TECH":
                {
                    string[] script = modifier.Split(':');
                    for (int i = 1; i < script.Count(); i++)
                    {
                        try
                        {
                            techtype = (TechnologyType) Enum.Parse(typeof(TechnologyType), script[i]);
                        }
                        catch
                        {
                            //techtype = (TechnologyType)Enum.Parse(typeof(TechnologyType), "General");
                            return false;
                        }
                        if (OwnerEmpire.data.Traits.Cybernetic > 0 && techtype ==
                            (TechnologyType) Enum.Parse(typeof(TechnologyType),
                                "Colonization")) //empire.GetBDict()["Biospheres"] &&
                        {
                            //techtype = TechnologyType.Industry;
                            continue;
                        }
                        if (techtype < TechnologyType.ShipHull)
                        {
                            availableTechs = workingSetoftechs;
                        }
                        else
                        {
                            availableTechs = bestShiptechs;
                        }
                        Technology ResearchTech = null;
                        if (command1 == "CHEAPEST")
                            ResearchTech = availableTechs.Where(econ => econ.TechnologyType == techtype)
                                .OrderBy(cost => cost.Cost)
                                .FirstOrDefault();
                        else if (command1 == "EXPENSIVE")
                            ResearchTech = availableTechs.Where(econ => econ.TechnologyType == techtype)
                                .OrderByDescending(cost => cost.Cost)
                                .FirstOrDefault();
                        //AvailableTechs.Where(econ => econ.TechnologyType == techtype).FirstOrDefault();
                        if (ResearchTech == null)
                            continue;
                        if (OwnerEmpire.Research > 30 && ResearchTech.Cost > OwnerEmpire.Research * 1000 &&
                            availableTechs.Count > 1)
                            continue;

                        if (techtype == TechnologyType.Economic)
                        {
                            if (ResearchTech.HullsUnlocked.Count > 0)
                            {
                                //money = empire.EstimateIncomeAtTaxRate(.25f);
                                if (moneyNeeded < 5f)
                                {
                                    if (command1 == "CHEAPEST")
                                        ResearchTech = availableTechs
                                            .Where(econ => econ.TechnologyType == techtype && econ != ResearchTech)
                                            .OrderBy(cost => cost.Cost)
                                            .FirstOrDefault();
                                    else if (command1 == "EXPENSIVE")
                                        ResearchTech = availableTechs
                                            .Where(econ => econ.TechnologyType == techtype && econ != ResearchTech)
                                            .OrderByDescending(cost => cost.Cost)
                                            .FirstOrDefault();

                                    if (ResearchTech == null)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        string Testresearchtopic =
                            ResearchTech
                                .UID; //AvailableTechs.Where(econ => econ.TechnologyType == techtype).OrderByDescending(cost => cost.Cost).FirstOrDefault().UID;
                        if (string.IsNullOrEmpty(researchtopic))
                            researchtopic = Testresearchtopic;
                        else
                        {
                            int currentCost = (int) (ResearchTech.Cost * CostNormalizer);
                            //int previousCost = (int)(ResourceManager.TechTree[researchtopic].Cost * CostNormalizer);

                            if (BestCombatShip != null &&
                                (techtype != TechnologyType.ShipHull && //techtype == TechnologyType.ShipHull ||//
                                 ResearchTech.ModulesUnlocked.Count > 0 ||
                                 ResourceManager.TechTree[researchtopic].ModulesUnlocked.Count > 0))
                            {
                                Technology PreviousTech = ResourceManager.TechTree[researchtopic];
                                //Ship 
                                Ship ship = BestCombatShip;
                                //if (ship.shipData.techsNeeded.Contains(PreviousTech.UID))
                                //    previousCost = (int)(previousCost * .5f);
                                if (ship.shipData.techsNeeded.Contains(ResearchTech.UID))
                                    currentCost = (int) (currentCost * .5f);
                            }

                            if (command1 == "CHEAPEST" && currentCost < previousCost)
                            {
                                researchtopic = Testresearchtopic;
                                previousCost = currentCost;
                                CostNormalizer += .01f;
                            }
                            else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                                researchtopic = Testresearchtopic;
                        }
                    }

                    break;
                }
                case "SHIPTECH":
                {
                    if (BestCombatShip == null)
                        return false;
                    Ship ship = BestCombatShip;
                    Technology shiptech = availableTechs
                        .Where(uid => ship.shipData.techsNeeded.Contains(uid.UID))
                        .OrderBy(techscost => techscost.Cost)
                        .FirstOrDefault();
                    if (shiptech == null)
                    {
                        //Log.Info(BestCombatShip.Name);
                        //foreach (string Bestshiptech in ship.shipData.techsNeeded) //.techsNeeded.Where(uid => !ship.shipData.techsNeeded.Contains(uid.UID))
                        //{
                        //    if (unlockedTech.Contains(Bestshiptech))
                        //        //|| AvailableTechs.Where(uid => uid.UID == Bestshiptech).Count()>0)
                        //        continue;
                        //    Log.Info("Missing Tech: " + Bestshiptech);
                        //}
                        return false;
                    }
                    researchtopic = shiptech.UID;

                    break;
                }


                default:
                {
                    try
                    {
                        techtype = (TechnologyType) Enum.Parse(typeof(TechnologyType), command2);
                        //Log.Info(EmpireName + " : " + techtype.ToString());
                    }
                    catch
                    {
                        res_strat = EmpireAI.ResearchStrategy.Random;
                        RunResearchPlanner();
                        res_strat = EmpireAI.ResearchStrategy.Scripted;
                        researchtopic = OwnerEmpire.ResearchTopic;
                        break;
                    }


                    //This should fix issue 414, but someone else will need to verify it
                    // Allium Sativum
                    Technology ResearchTech = null;
                    ResearchTech = availableTechs.OrderByDescending(econ => econ.TechnologyType == techtype)
                        .ThenBy(cost => cost.Cost)
                        .FirstOrDefault();
                    if (ResearchTech != null)
                    {
                        researchtopic = ResearchTech.UID;
                        break;
                    }
                    //float netresearch =empire.GetPlanets().Where(owner => owner.Owner == empire).Sum(research => research.NetResearchPerTurn);
                    //netresearch = netresearch == 0 ? 1 : netresearch;
                    //if (ResourceManager.TechTree[researchtopic].Cost / netresearch < 500 )
                    researchtopic = null;
                    break;
                }
            }
            {
                OwnerEmpire.ResearchTopic = researchtopic;
            }
            // else
            {
                // researchtopic = AvailableTechs.OrderBy(cost => cost.Cost).First().UID;
            }


            if (string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
                return false;
            else
            {
                //try
                //{
                //    if (ResourceManager.TechTree[empire.ResearchTopic].TechnologyType == TechnologyType.ShipHull)
                //    {
                //        BestCombatShip = "";
                //    }
                //}
                //catch(Exception e)
                //{
                //    e.Data.Add("Tech Name(UID)", empire.ResearchTopic);

                //}
                //Log.Info(EmpireName + " : " + ResourceManager.TechTree[empire.ResearchTopic].TechnologyType.ToString() + " : " + empire.ResearchTopic);
                return true;
            }
        }

        private enum ResearchStrategy
        {
            Random,
            Scripted
        }

        private float randomizer(float priority, float bonus)
        {
            float index=0;
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            return index ;
        }
    }
}