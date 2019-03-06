using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private ResearchStrategy res_strat = ResearchStrategy.Scripted;
        private int ScriptIndex;
        private int HullScaler = 3;
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
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoResearch)
                return;
            if (OwnerEmpire.ResearchTopic.NotEmpty())
                return;
            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            float researchDebt = 0;
            float wars = OwnerEmpire.AllRelations.Count(war => !war.Key.isFaction && (war.Value.AtWar 
                                                             || war.Value.PreparingForWar ));

            wars += ThreatMatrix.StrengthOfAllThreats(OwnerEmpire) / (OwnerEmpire.currentMilitaryStrength + 1);
            wars += OwnerEmpire.AllRelations.Count(war => !war.Key.isFaction 
                                        && ( war.Value.TotalAnger > 10 || war.Value.FearEntries.Count > 1)) *2;

            if (postResearchTopic.NotEmpty())
            {
                researchDebt = OwnerEmpire.GetTechEntry(postResearchTopic).TechCost;
                researchDebt = researchDebt / OwnerEmpire.MaxResearchPotential;
            }
            researchDebt += OwnerEmpire.ResearchStrategy.ResearchPriority -wars;
            float economics = (OwnerEmpire.data.TaxRate * 10); 
            float needsFood = 0;

            foreach (Planet hunger in OwnerEmpire.GetPlanets())
            {
                if (hunger.Storage.RaceFoodRatio < 0.2f)                
                    ++needsFood;
                
                if (!OwnerEmpire.HasUnlocked("Biospheres"))
                {
                    if (hunger.Fertility < 0.1f)
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
            //if (hullScaler > 0) shipBuildBonus -= hullScaler;
            needsFood = needsFood > 0 ? needsFood / OwnerEmpire.GetPlanets().Count : 0;
            needsFood *= 10;

            float total = wars + researchDebt + needsFood + economics;
            total *= .1f;
            wars = wars / total;
            researchDebt = researchDebt / total;
            needsFood = needsFood / total;
            economics = economics / total;

            bool cybernetic = OwnerEmpire.IsCybernetic;
            DebugLog($"wars : {wars}");
            DebugLog($"researchDebt : {researchDebt}");
            DebugLog($"cybernetic : {cybernetic}");
            DebugLog($"needsFood : {needsFood}");
            DebugLog($"economics : {economics}");

            DebugLog($"ResearchStrategy : {res_strat.ToString()}");
            switch (res_strat)
            {
                case ResearchStrategy.Random:
                    {                        
                        ChooseRandomTech(wars, shipBuildBonus, researchDebt, cybernetic, needsFood, economics, command);
                        return;
                    }
                case ResearchStrategy.Scripted:
                    {
                        if (ProcessScript(wars > 0, economics > 4, researchDebt > 2, OwnerEmpire.Money < OwnerEmpire.NetPlanetIncomes)) return;
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
            var resStrat = OwnerEmpire.ResearchStrategy;

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
                    max += 2;
                }
                else
                {
                    sendToScript += pWeighted.Key;
                    max++;
                }
            }
            ScriptedResearch(command, "RANDOM", "TECH" + sendToScript);
        }

        private bool ProcessScript(bool atWar, bool highTaxes, bool lowResearch, bool lowincome)
        {
            int loopcount = 0;
            var strat = OwnerEmpire.ResearchStrategy;
            Start:
            if (strat != null &&
                ScriptIndex < strat.TechPath.Count &&
                loopcount < strat.TechPath.Count)
            {
                string scriptentry = strat.TechPath[ScriptIndex].id;
                string scriptCommand = OwnerEmpire.HasTechEntry(scriptentry)
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
                                int.Parse(OwnerEmpire.ResearchStrategy.TechPath[ScriptIndex].id
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
                            loopcount += ScriptBump(OwnerEmpire.IsCybernetic);
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
                    case "IFRESEARCHHIGHERTHAN":
                        bool researchPreReqMet = false;
                        string[] researchScript = scriptentry.Split(':');
                        if (float.TryParse(researchScript[2], out float researchAmount))
                            if (OwnerEmpire.GetProjectedResearchNextTurn() >= researchAmount)
                                researchPreReqMet = true;

                        loopcount += ScriptBump(researchPreReqMet);
                        goto Start;
                    case "IFTECHRESEARCHED":
                        bool techResearched = false;
                        string[] techResearchedScript = scriptentry.Split(':');
                        if (OwnerEmpire.TryGetTechEntry(techResearchedScript[2], out TechEntry checkedTech))
                        {
                            if (checkedTech.Unlocked)
                                techResearched = true;
                        }
                        loopcount += ScriptBump(techResearched);
                        goto Start;
                    default:
                        {
                            DebugLog($"Hard Script : {scriptentry}");
                            if (OwnerEmpire.TryGetTechEntry(scriptentry, out TechEntry defaulttech))
                            {
                                if (defaulttech.Unlocked)

                                {
                                    DebugLog("Already Unlocked");
                                    ScriptIndex++;
                                    goto Start;
                                }
                                if (!defaulttech.Unlocked && OwnerEmpire.HavePreReq(defaulttech.UID))
                                {
                                    DebugLog("Researching");
                                    OwnerEmpire.ResearchTopic = defaulttech.UID;
                                    ScriptIndex++;
                                    if (!string.IsNullOrEmpty(scriptentry))
                                        return true;
                                }
                                else
                                {
                                    ScriptIndex++;
                                    goto Start;
                                }
                            }
                            else
                            {
                                Log.Info($"TechNotFound : {scriptentry}");
                                ScriptIndex++;
                                goto Start;
                            }


                            foreach (EconomicResearchStrategy.Tech tech in OwnerEmpire.ResearchStrategy.TechPath)
                            {
                                if (OwnerEmpire.HasTechEntry(tech.id) && !OwnerEmpire.HasUnlocked(tech.id) &&
                                    OwnerEmpire.HavePreReq(tech.id))
                                {
                                    OwnerEmpire.ResearchTopic = tech.id;
                                    ScriptIndex++;
                                    if (tech.id.NotEmpty())
                                        return true;
                                }
                            }
                            GoRandomOnce();
                            ScriptIndex++;
                            return true;
                        }
                }
            }
            if (OwnerEmpire.ResearchTopic.IsEmpty())
            {
                GoRandomOnce();
                if (loopcount >= OwnerEmpire.ResearchStrategy.TechPath.Count)
                    res_strat = ResearchStrategy.Random;

            }
            return false;
        }

        private bool GoRandomOnce(string command = "CHEAPEST")
        {
            DebugLog($"Go Random Once");
            res_strat = ResearchStrategy.Random;
            RunResearchPlanner(command);
            res_strat = ResearchStrategy.Scripted;
            return OwnerEmpire.ResearchTopic.NotEmpty();
        }

        private int ScriptBump(bool check, int index = 1)
        {            
            if (check)
            {
                ScriptIndex =
                    int.Parse(
                        OwnerEmpire.ResearchStrategy.TechPath[ScriptIndex].id.Split(':')[1]);                
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
            float moneyNeeded = BuildCapacity * .2f;

            //OwnerEmpire.UpdateShipsWeCanBuild();

            if (BestCombatShip != null)
            {
                if (OwnerEmpire.ShipsWeCanBuild.Contains(GetBestCombatShip.Name) 
                || OwnerEmpire.structuresWeCanBuild.Contains(GetBestCombatShip.Name)) 
                    BestCombatShip = null;
                else
                if (!BestCombatShip.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any())
                    BestCombatShip = null;
            }
            HashSet<string> allAvailableShipTechs = FindBestShip(modifier, availableTechs, command2);
            DebugLog(
                $"Best Ship : {GetBestCombatShip?.shipData.HullRole} : {GetBestCombatShip?.GetStrength()}");
            DebugLog($" : {GetBestCombatShip?.Name}");

            //now that we have a target ship to buiild filter out all the current techs that are not needed to build it.

            if (!GlobalStats.HasMod || !GlobalStats.ActiveModInfo.UseManualScriptedResearch)
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
                                DebugLog($"BetterChoice : {researchtopic}");
                                researchtopic = Testresearchtopic;
                                previousCost = currentCost;
                                CostNormalizer += .005f;
                            }
                            else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                            {
                                DebugLog($"BetterChoice : {researchtopic}");
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
            postResearchTopic = OwnerEmpire.ResearchTopic;
            return true;
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

            TechEntry[] filteredTechs;
            TechEntry researchTech = null;
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseManualScriptedResearch)
                filteredTechs = availableTechs.Filter(tech => tech.TechnologyType == techtype);
            else
            {
                filteredTechs = availableTechs.Filter(econ =>
                {
                    if (econ.GetLookAheadType(techtype) > 0 &&
                     techtype != TechnologyType.Economic) return true;

                    if (econ.Tech.HullsUnlocked.Count == 0
                        || moneyNeeded < 1f
                        || availableTechs.Count == 1)
                        return true;

                    foreach (var hull in econ.Tech.HullsUnlocked)
                    {
                        if (!ResourceManager.Hull(hull.Name, out ShipData hullData) || hullData == null) continue;
                        switch (hullData.HullRole)
                        {
                            case ShipData.RoleName.station:
                            case ShipData.RoleName.platform:
                                return true;
                        }
                    }
                    return false;
                });
            }
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

            // use the shiptech choosers which just chooses tech in the list. 
            TechEntry[] repeatingTechs = OwnerEmpire.TechEntries.Filter(t => t.MaxLevel > 1);

            foreach (string shiptech in shipTechs)
            {
                if (OwnerEmpire.TryGetTechEntry(shiptech, out TechEntry test))
                {
                    bool skiprepeater = false;
                    // repeater compensator. This needs some deeper logic. I current just say if you research one level. Dont research any more.
                    if (test.MaxLevel > 1)
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
                foreach (var bTech in BestCombatShip.shipData.TechsNeeded)
                    nonShipTechs.Add(bTech);
                DebugLog(
                    $"Best Ship : {GetBestCombatShip.shipData.HullRole} : {GetBestCombatShip.GetStrength()}");
                DebugLog($" : {GetBestCombatShip.Name}");
                return nonShipTechs;

            }

            //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic.            
            
            
            
            Array<Ship> racialShips = new Array<Ship>();
            GetRacialShips(racialShips);
            Array<Ship> researchableShips = new Array<Ship>();
            var hulls = new Array<ShipData>();
            for (int x = 0 ; x< 5; x++)
            {
              
            }
            GetResearchableShips(racialShips, shipTechs, researchableShips, hulls);

            if (researchableShips.Count <= 0) return nonShipTechs;

            if (!GetLineFocusedShip(researchableShips, shipTechs))
                return nonShipTechs;
            foreach (var tech in BestCombatShip.shipData.TechsNeeded)
                nonShipTechs.Add(tech);
            return nonShipTechs;

        }

        private bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {

            var techSorter = new SortedList<int, Array<Ship>>();
            foreach (Ship shortTermBest in researchableShips)
            {                                
                //forget the cost of tech that provide these ships. These are defined in techentry class.
                if (!OwnerEmpire.canBuildCarriers && shortTermBest.shipData.CarrierShip)
                    continue;

                /*try to line focus to main goal but if we cant, line focus as best as possible. 
                 * To do this use a sorted list with a key set to the count of techs needed minus techs we already have.
                 * since i dont know which key the ship will be added to this seems the easiest without a bunch of extra steps.
                 * Now this list can be used to not just get the one with fewest techs but add a random to get a little variance. 
                 */
                Array<string> currentTechs =
                    new Array<string>(shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Except(shipTechs));

                int key = currentTechs.Count;
                
                /* this is kind of funky but the idea is to add a key and list if it doesnt already exist.
                 Because i dont know how many will be in it.                  
                 */
                if (techSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(shortTermBest);
                else
                {
                    test = new Array<Ship> {shortTermBest};
                    techSorter.Add(key, test);
                }
            }

            var hullSorter = new SortedList<int, Array<Ship>>();

            //This is part that chooses the bestShip hull
            /* takes the first entry from the least techs needed list. then sorts it the hull role needed
             */
            //try to fix sentry bug :https://sentry.io/blackboxmod/blackbox/issues/533939032/events/26436104750/
            if (techSorter.Count == 0)
                return false;
            
            int keyChosen = ChooseRole(techSorter[techSorter.Keys.First()], hullSorter ,h=> (int)h.shipData.HullRole );
            //sort roles
            var roleSorter = new SortedList<int, Array<Ship>>();
            keyChosen = ChooseRole(hullSorter[keyChosen], roleSorter,
                s => (int)s.DesignRole); // s.DesignRole < ShipData.RoleName.fighter ? (int)ShipData.RoleName.fighter -1 : (int) s.DesignRole);

            //choose Ship

            Array<Ship> ships = new Array<Ship>(roleSorter[keyChosen].
                OrderByDescending(ship => ship.shipData.TechsNeeded.Count )); //ship.GetStrength()));//  
            for (int x = 1; x <= ships.Count; x++)
            {
                var ship = ships[x-1];
                float chance = (float)x / ships.Count;
                float rand = RandomMath.RandomBetween(.01f, 1f) ;
                if (rand > chance) 
                    continue;
                return (BestCombatShip = ship) != null;
            }
            return false;
            
            
        }

        private int ChooseRole(Array<Ship> ships, SortedList<int, Array<Ship>> roleSorter, Func<Ship,int> func)
        {
            //SortRoles
            /*
             * take each ship in ships and make a sorted list based on the hull role index. 
             */
            foreach (Ship ship in ships)
            {
                int key = func(ship); // ship.DesignRole;
                if (roleSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(ship);
                else
                {
                    test = new Array<Ship> {ship};
                    roleSorter.Add(key, test);
                }
            }
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterater through the keys with an every increasing chance to choose a key. 
             */
            int keyChosen = roleSorter.Keys.First();
            

            int x = 0;
            foreach (var role in roleSorter)
            {
                float chance = (float)++x / roleSorter.Count;
                
                float rand = RandomMath.AvgRandomBetween(.01f, 1f);
                var hullRole = role.Value[0].shipData.HullRole;
                var hullUnlocked = OwnerEmpire.IsHullUnlocked(role.Value[0].shipData.Hull);
                //if (hullRole == ShipData.RoleName.platform || hullRole == ShipData.RoleName.station || hullUnlocked)
                //    chance /= 1.5f;
                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }
        
        private void GetRacialShips(Array<Ship> racialShips)
        {
            foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values.OrderBy(tech => tech.shipData
                .TechScore))
            {
                try
                {
                    //restrict to racial ships or otherwise unlocked ships. 
                    if (shortTermBest.shipData.ShipStyle==null  ||
                        shortTermBest.shipData.ShipStyle !="Platforms" && shortTermBest.shipData.ShipStyle != "Misc"
                         && shortTermBest.shipData.ShipStyle != OwnerEmpire.data.Traits.ShipType)
                        //|| !OwnerEmpire.IsHullUnlocked(shortTermBest.shipData.Hull))
                        continue;
                    
                    if (shortTermBest.shipData.TechsNeeded.Count == 0)
                    {
                        if (Empire.Universe.Debug)
                        {
                            Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                        }
                        continue;
                    }
                }
                catch
                {
                    Log.Warning($"Ship {shortTermBest.Name} has not shipData");
                    continue;
                }
                racialShips.Add(shortTermBest);
            }
        }

        private void GetResearchableShips(Array<Ship> racialShips, HashSet<string> shipTechs, Array<Ship> researchableShips,
            Array<ShipData> hulls)
        {           
            foreach (Ship shortTermBest in racialShips)
            {              
                //filter Hullroles....
                if (!IsRoleValid(shortTermBest.shipData.HullRole)) continue;
                if (!IsRoleValid(shortTermBest.DesignRole)) continue;
                if (!IsRoleValid(shortTermBest.shipData.Role)) continue;

                if (OwnerEmpire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                    continue;
                if (!shortTermBest.ShipGoodToBuild(OwnerEmpire)) continue;
                if (!shortTermBest.shipData.UnLockable) continue;                
                if (ShipHasUndiscoveredTech(shortTermBest)) continue;
                researchableShips.Add(shortTermBest);
            }
        }

        private bool ShipHasUndiscoveredTech(Ship ship)
        {
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                if (!OwnerEmpire.HasDiscovered(techName))
                    return true;
            }
            return false;
        }
        

        private static bool IsRoleValid(ShipData.RoleName role)
        {
            switch (role)
            {
                case ShipData.RoleName.disabled:
                case ShipData.RoleName.supply:
                case ShipData.RoleName.troop:
                case ShipData.RoleName.prototype:
                case ShipData.RoleName.construction:
                case ShipData.RoleName.freighter:
                case ShipData.RoleName.colony:
                    return false;
                case ShipData.RoleName.platform:
                    break;
                case ShipData.RoleName.station:
                    break;
                case ShipData.RoleName.troopShip:
                    break;
                case ShipData.RoleName.support:
                    break;
                case ShipData.RoleName.bomber:
                    break;
                case ShipData.RoleName.carrier:
                    break;
                case ShipData.RoleName.fighter:
                    break;
                case ShipData.RoleName.scout:
                    break;
                case ShipData.RoleName.gunboat:
                    break;
                case ShipData.RoleName.drone:
                    break;
                case ShipData.RoleName.corvette:
                    break;
                case ShipData.RoleName.frigate:
                    break;
                case ShipData.RoleName.destroyer:
                    break;
                case ShipData.RoleName.cruiser:
                    break;
                case ShipData.RoleName.capital:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        Array<TechEntry> AvailableTechs()
        {
            var availableTechs = new Array<TechEntry>();

            foreach (TechEntry tech in OwnerEmpire.TechEntries)
            {
                if (tech.Discovered && tech.shipDesignsCanuseThis && !tech.Unlocked &&
                    OwnerEmpire.HavePreReq(tech.UID))
                {
                    availableTechs.Add(tech);
                    tech.SetLookAhead(OwnerEmpire);
                }
            }
            if (availableTechs.Count == 0)
                DebugLog("No Techs found to research");
            return availableTechs;
        }

        private enum ResearchStrategy
        {
            Random,
            Scripted
        }

        private float randomizer(float priority, float bonus)
        {
            return RandomMath.AvgRandomBetween(0, priority + bonus);
           
        }
    }
}