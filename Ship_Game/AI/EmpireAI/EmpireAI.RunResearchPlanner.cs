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

        public Ship GetBestCombatShip
        {
            get { return BestCombatShip; }
        }

        private void DebugLog(string text)
        {
            Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);
        }

        private void RunResearchPlanner(string command = "CHEAPEST")
        {
            if (OwnerEmpire.ResearchTopic.NotEmpty())
                return;
            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            bool cybernetic = OwnerEmpire.data.Traits.Cybernetic > 0;
            float researchDebt = 0;
            float wars = OwnerEmpire.AllRelations
                .Where(war => !war.Key.isFaction && (war.Value.AtWar || war.Value.PreparingForWar))
                .Sum(str => str.Key.currentMilitaryStrength / OwnerEmpire.currentMilitaryStrength);

            researchDebt = 0;
            
            if (postResearchTopic.NotEmpty())
            {
                researchDebt = 50 * (1f + OwnerEmpire.Research);
                researchDebt = OwnerEmpire.GetTechEntry(postResearchTopic).TechCost / researchDebt;
            }
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
            DebugLog($"wars : {wars}");
            DebugLog($"researchDebt : {researchDebt}");
            DebugLog($"cybernetic : {cybernetic}");
            DebugLog($"needsFood : {needsFood}");
            DebugLog($"economics : {economics}");

            DebugLog($"ResearchStrategy : {res_strat.ToString()}");
            switch (res_strat)
            {
                case EmpireAI.ResearchStrategy.Random:
                    {                        
                        ChooseRandomTech(wars, shipBuildBonus, researchDebt, cybernetic, needsFood, economics, command);
                        return;
                    }
                case EmpireAI.ResearchStrategy.Scripted:
                    {
                        if (ProcessScript(wars > 0, economics > 5, researchDebt > 4, OwnerEmpire.Money < OwnerEmpire.GrossTaxes)) return;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }        
        }

        private void ChooseRandomTech(float wars, float shipBuildBonus, float researchDebt, bool cybernetic, float needsFood,
            float economics, string command = "CHEAPEST")
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
                if (max > 4)
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
            ScriptedResearch(command, "RANDOM", "TECH" + sendToScript);
            

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
                DebugLog($"index : {ScriptIndex}");
                DebugLog($"Script Command : {scriptCommand}");
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
                                GoRandomOnce();
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
                                GoRandomOnce("EXPENSIVE");
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
                            GoRandomOnce();
                            ScriptIndex++;
                            return true;
                        }
                    default:
                        {
                            DebugLog($"Hard Script : {scriptentry}");
                            if (OwnerEmpire.GetTDict().TryGetValue(scriptentry, out TechEntry defaulttech))
                            {
                                if (defaulttech.Unlocked)

                                {
                                    DebugLog($"Already Unlocked");
                                    ScriptIndex++;
                                    goto Start;
                                }
                                if (!defaulttech.Unlocked && OwnerEmpire.HavePreReq(defaulttech.UID))
                                {
                                    DebugLog($"Researching");
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
            if (OwnerEmpire.ResearchTopic.IsEmpty())
            {
                GoRandomOnce();
                if (loopcount >= OwnerEmpire.getResStrat().TechPath.Count)
                    res_strat = EmpireAI.ResearchStrategy.Random;

            }
            return false;
        }

        private bool GoRandomOnce(string command = "CHEAPEST")
        {
            DebugLog($"Go Random Once");
            res_strat = EmpireAI.ResearchStrategy.Random;
            RunResearchPlanner(command);
            res_strat = EmpireAI.ResearchStrategy.Scripted;
            return OwnerEmpire.ResearchTopic.NotEmpty();
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
            Array<TechEntry> availableTechs = AvailableTechs();

            if (availableTechs.Count <= 0) return false;

            DebugLog($"Possible Techs : {availableTechs.Count}");

            string researchtopic = "";
            TechnologyType techtype;
            float moneyNeeded = OwnerEmpire.data.ShipBudget * .2f;

            //OwnerEmpire.UpdateShipsWeCanBuild();

            if (BestCombatShip != null)
            {
                if (OwnerEmpire.ShipsWeCanBuild.Contains(GetBestCombatShip.Name))
                    BestCombatShip = null;
            }
            HashSet<string> allAvailableShipTechs = FindBestShip(modifier, availableTechs, command2);

            //now that we have a target ship to buiild filter out all the current techs that are not needed to build it.

            availableTechs = BestShiptechs(modifier, allAvailableShipTechs, availableTechs);


            float CostNormalizer = .01f;
            int previousCost = command1 == "CHEAPEST" ? int.MaxValue : int.MinValue;
            switch (command2)
            {
                case "RANDOM":
                case "TECH":
                    {

                        string[] script = modifier.Split(':');
                        for (int i = 1; i < script.Count(); i++)
                        {
                            TechEntry researchTech = GetScriptedTech(command1, script[i], availableTechs, moneyNeeded);
                            if (researchTech == null) continue;

                            string Testresearchtopic =
                                researchTech
                                    .UID;
                            int currentCost = (int)Math.Ceiling(researchTech.TechCost * CostNormalizer);


                            if (command1 == "CHEAPEST" && currentCost < previousCost)
                            {
                                DebugLog($"BetterChoice : {researchtopic.ToString()}");
                                researchtopic = Testresearchtopic;
                                previousCost = currentCost;
                                CostNormalizer += .005f;
                            }
                            else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                            {
                                DebugLog($"BetterChoice : {researchtopic.ToString()}");
                                researchtopic = Testresearchtopic;
                                previousCost = currentCost;
                                CostNormalizer *= .25f;
                            }
                            else DebugLog($"command {command1} did not choose a tech");
                        }

                        break;
                    }

                default:
                    {

                        TechEntry researchTech = GetScriptedTech(command1, command2, availableTechs, moneyNeeded);
                        if (researchTech != null)
                        {
                            researchtopic = researchTech.UID;
                            break;
                        }
                        researchtopic = null;
                        break;
                    }
            }
            OwnerEmpire.ResearchTopic = researchtopic;
            DebugLog($"Tech Choosen : {researchtopic}");

            if (string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
                return false;
            else
            {
                postResearchTopic = OwnerEmpire.ResearchTopic;
                return true;
            }
        }

        private TechEntry GetScriptedTech(string command1, string techType, Array<TechEntry> availableTechs, float moneyNeeded)
        {
            TechnologyType techtype;
            try
            {
                techtype = (TechnologyType) Enum.Parse(typeof(TechnologyType), techType);
            }
            catch
            {
                Log.Error($"techType not found : ");
                return null;
            }
            DebugLog($"\nFind : {techtype.ToString()}");
            if (OwnerEmpire.data.Traits.TechTypeRestrictions(techtype))
            {
                DebugLog($"Trait Restricted : {techtype.ToString()}");
                return null;
            }


            TechEntry researchTech = null;
            TechEntry[] filteredTechs = availableTechs.FilterBy(econ =>
            {
                if (econ.TechnologyType != techtype) return false;
                if (techtype != TechnologyType.Economic) return true;
                if (moneyNeeded > 5f) return true;
                if (econ.Tech.HullsUnlocked.Count == 0) return true;
                return false;
            });
            
            LogFinalScriptTechs(command1, techtype, filteredTechs);
            researchTech = ChooseScriptTech(command1, filteredTechs);
            if (researchTech == null)
            {
                DebugLog($"{techtype.ToString()} : No Tech found");
                return null;
            }
            return researchTech;
        }

        private TechEntry ChooseScriptTech(string command1, TechEntry[] filteredTechs)
        {
            TechEntry researchTech = null;
            if (command1 == "CHEAPEST")
                researchTech = filteredTechs.FindMin(cost => cost.TechCost);
            else if (command1 == "EXPENSIVE")
                researchTech = filteredTechs.FindMax(cost => cost.TechCost);
            DebugLog($"{command1} : {researchTech?.UID ?? "Nothing Found"}");
            return researchTech;
        }

        private void LogFinalScriptTechs(string command1, TechnologyType techtype, TechEntry[] filteredTechs)
        {
            var debugWin = Empire.Universe.DebugWin;
            if (!GlobalStats.VerboseLogging && (debugWin == null || (command1 != "CHEAPEST" && command1 != "EXPENSIVE"))) return;
            DebugLog($"possible Techs : {filteredTechs.Length}");
            foreach (var tech in filteredTechs)
            {
                DebugLog($" {tech.UID} : {tech.TechCost}");
            }
        }


        private Array<TechEntry> BestShiptechs(string modifier, HashSet<string> shipTechs, Array<TechEntry> availableTechs)
        {
            var bestShiptechs = new Array<TechEntry>();

            //use the shiptech choosers which just chooses tech in the list. 
            var repeatingTechs = new Array<TechEntry>();
            foreach (var kv in OwnerEmpire.GetTDict())
            {
                if (kv.Value.MaxLevel > 0)
                    repeatingTechs.Add(kv.Value);
            }
            foreach (string shiptech in shipTechs)
            {
                TechEntry test = OwnerEmpire.GetTechEntry(shiptech);
                if (test != null)
                {
                    bool skiprepeater = false;
                    //repeater compensator. This needs some deeper logic. I current just say if you research one level. Dont research any more.
                    if (test.MaxLevel > 0)
                    {
                        foreach (TechEntry repeater in repeatingTechs)
                        {
                            if (test == repeater && (repeater.Level > 0))
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
            return bestShiptechs;
        }


        private HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            float techcost = -1;
            float str = 0;
            int numberOfShipTechs = 0;
            HashSet<string> shipTechs = new HashSet<string>();
            HashSet<string> nonShipTechs = new HashSet<string>();
            HashSet<string> wantedShipTechs = new HashSet<string>();


            foreach (TechEntry bestshiptech in availableTechs)
            {
                switch (bestshiptech.TechnologyType)
                {
                    case TechnologyType.General:
                    case TechnologyType.Colonization:
                    case TechnologyType.Economic:
                    case TechnologyType.Industry:
                    case TechnologyType.Research:
                    case TechnologyType.GroundCombat:
                        nonShipTechs.Add(bestshiptech.UID);
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
                shipTechs.Add(bestshiptech.UID);
            }
            if (!modifier.Contains("ShipWeapons") && !modifier.Contains("ShipDefense") &&
                !modifier.Contains("ShipGeneral") && !modifier.Contains("ShipHull"))
                return nonShipTechs;

            if (BestCombatShip != null && command == "RANDOM")
            {
                foreach (var bTech in BestCombatShip.GetShipData().techsNeeded)
                    nonShipTechs.Add(bTech);
                DebugLog(
                    $"Best Ship : {GetBestCombatShip.shipData.HullRole} : {GetBestCombatShip.GetStrength()}");
                DebugLog($" : {GetBestCombatShip.Name}");
                return nonShipTechs;

            }

            //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic. 
            //RandomMath.AvgRandomBetween(200f, 500f)
            int timeToResearch = (int)((OwnerEmpire.Research + 1) *  100 * UniverseScreen.GamePaceStatic);
            timeToResearch = timeToResearch < 100 ? 100 : timeToResearch;
            techcost = timeToResearch;
            bool shipchange = false;

            foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values.OrderByDescending(tech => tech.shipData
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

                if (shortTermBest.shipData.HullData?.ShipStyle != OwnerEmpire.data.Traits.ShipType &&
                    !OwnerEmpire.IsHullUnlocked(shortTermBest.shipData.Hull))
                    continue;

                if (shortTermBest.shipData.techsNeeded.Count == 0)
                    continue;
                if (OwnerEmpire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                    continue;
                if (!shortTermBest.shipData.techsNeeded.Intersect(shipTechs).Any())
                    continue;
                var hullTechs = shortTermBest.shipData.HullData.techsNeeded.Except(shipTechs);
                if (hullTechs.Count() > 1)
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
                int currentTechCost = 0;

                float sTechCost = 0;
                foreach (var sTech in currentTechs)
                {
                    var tCost = OwnerEmpire.GetTechEntry(sTech);
                    if (tCost == null)
                        continue;
                    sTechCost += tCost.TechCost;
                    if (availableTechs.Contains(tCost))
                        if (wantedShipTechs.Add(tCost.UID))
                            numberOfShipTechs++;
                }
                currentTechCost = (int)sTechCost;

                currentTechCost -= mod;                             

                float shortStr = shortTermBest.shipData.BaseStrength;

                if (currentTechCost > techcost && currentTechCost > 0)
                {
                    float strMod = (techcost / currentTechCost) * .01f;
                    shortStr *= strMod;
                }

                if (shortStr > str)
                {
                    str = shortStr;
                    BestCombatShip = shortTermBest;
                    shipchange = true;
                }
            }

            if (shipchange && command == "RANDOM")
            {
                DebugLog(
                    $"Best Ship : {GetBestCombatShip.shipData.HullRole} : {GetBestCombatShip.GetStrength()}");
                DebugLog($" : {GetBestCombatShip.Name}");
                foreach (var tech in BestCombatShip.shipData.techsNeeded)
                    nonShipTechs.Add(tech);

                return nonShipTechs;
            }
            foreach (var tech in wantedShipTechs)
                nonShipTechs.Add(tech);
            

            //End of line focusing. 

            DebugLog($"ShipTechs Found : {numberOfShipTechs}");
            return nonShipTechs;
        }

        private int AddMatchingTechCost(Ship ship, Array<string> techList)
        {
            int techCost = 0;
            foreach (string shipTech in ship.GetShipData().techsNeeded)
            {
                if (!string.IsNullOrEmpty(techList.Find(tech => shipTech == tech)))
                {
                    techCost += (int) OwnerEmpire.GetTechEntry(shipTech).TechCost;
                }
            }
            return techCost;
        }

        private Array<TechEntry> AvailableTechs()
        {
            var availableTechs = new Array<TechEntry>();

            foreach (var kv in OwnerEmpire.GetTDict())
            {
                if (!kv.Value.Discovered || !kv.Value.shipDesignsCanuseThis || kv.Value.Unlocked ||
                    !OwnerEmpire.HavePreReq(kv.Key))
                    continue;

                availableTechs.Add(kv.Value);
            }
            if (availableTechs.Count == 0)
                DebugLog($"No Techs found to research");
            return availableTechs;
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