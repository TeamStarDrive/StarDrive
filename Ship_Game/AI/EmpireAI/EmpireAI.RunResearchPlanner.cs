using Ship_Game.Ships;
using System;
using System.Linq;
using Ship_Game.AI.Research;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private ResearchStrategy res_strat = ResearchStrategy.Scripted;
        private int ScriptIndex;
        public ShipTechLineFocusing LineFocus;

        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        private void RunResearchPlanner(string command = "CHEAPEST")
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoResearch)
                return;
            if (OwnerEmpire.ResearchTopic.NotEmpty())
                return;
            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            OwnerEmpire.data.TechDelayTime++;
            var researchPriorities = new ResearchPriorities(OwnerEmpire);

            DebugLog($"ResearchStrategy : {res_strat.ToString()}");

            switch (res_strat)
            {
                case ResearchStrategy.Random:
                    {
                        ScriptedResearch(command, "RANDOM", "TECH" + researchPriorities.TechCategoryPrioritized);
                        return;
                    }
                case ResearchStrategy.Scripted:
                    {
                        if (ProcessScript(researchPriorities.Wars > 0, researchPriorities.Economics > 4
                            , researchPriorities.ResearchDebt > 2, OwnerEmpire.Money < OwnerEmpire.NetPlanetIncomes)) return;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        private bool ProcessScript(bool atWar, bool highTaxes, bool lowResearch, bool lowIncome)
        {
            int loopCount = 0;
            var strat = OwnerEmpire.ResearchStrategy;
            Start:
            if (strat != null && ScriptIndex < strat.TechPath.Count && loopCount < strat.TechPath.Count)
            {
                string scriptEntry = strat.TechPath[ScriptIndex].id;
                string scriptCommand;

                if (OwnerEmpire.HasTechEntry(scriptEntry))
                    scriptCommand = scriptEntry;
                else
                    scriptCommand = scriptEntry.Split(':')[0];

                DebugLog($"index : {ScriptIndex}");
                DebugLog($"Script Command : {scriptCommand}");
                switch (scriptCommand)
                {
                    case "SCRIPT":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

                            if (script.Count() > 2)
                            {
                                modifier = script[2];
                            }
                            ScriptIndex++;
                            if (ScriptedResearch("CHEAPEST", script[1], modifier))
                                return true;
                            loopCount++;
                            goto Start;
                        }
                    case "LOOP":
                        {
                            ScriptIndex =
                                int.Parse(OwnerEmpire.ResearchStrategy.TechPath[ScriptIndex].id
                                    .Split(':')[1]);
                            loopCount++;
                            goto Start;
                        }
                    case "CHEAPEST":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

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
                            loopCount++;
                            goto Start;
                        }
                    case "EXPENSIVE":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

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
                            loopCount++;
                            goto Start;
                        }
                    case "IFWAR":
                        {
                            loopCount += ScriptBump(atWar);
                            goto Start;
                        }
                    case "IFHIGHTAX":
                        {
                            loopCount += ScriptBump(highTaxes);
                            goto Start;
                        }
                    case "IFPEACE":
                        {
                            loopCount += ScriptBump(!atWar);
                            goto Start;
                        }
                    case "IFCYBERNETIC":
                        {
                            loopCount += ScriptBump(OwnerEmpire.IsCybernetic);
                            goto Start;
                        }
                    case "IFLOWRESEARCH":
                        {
                            loopCount += ScriptBump(lowResearch);
                            goto Start;
                        }
                    case "IFNOTLOWRESEARCH":
                        {
                            loopCount += ScriptBump(!lowResearch);
                            goto Start;
                        }
                    case "IFLOWINCOME":
                        {
                            loopCount += ScriptBump(lowIncome);
                            goto Start;
                        }
                    case "IFNOTLOWINCOME":
                        {
                            loopCount += ScriptBump(!lowIncome);
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
                        string[] researchScript = scriptEntry.Split(':');
                        if (float.TryParse(researchScript[2], out float researchAmount))
                            if (OwnerEmpire.GetProjectedResearchNextTurn() >= researchAmount)
                                researchPreReqMet = true;

                        loopCount += ScriptBump(researchPreReqMet);
                        goto Start;
                    case "IFTECHRESEARCHED":
                        bool techResearched = false;
                        string[] techResearchedScript = scriptEntry.Split(':');
                        if (OwnerEmpire.TryGetTechEntry(techResearchedScript[2], out TechEntry checkedTech))
                        {
                            if (checkedTech.Unlocked)
                                techResearched = true;
                        }
                        loopCount += ScriptBump(techResearched);
                        goto Start;
                    default:
                        {
                            DebugLog($"Hard Script : {scriptEntry}");
                            if (OwnerEmpire.TryGetTechEntry(scriptEntry, out TechEntry defaulttech))
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
                                    if (!string.IsNullOrEmpty(scriptEntry))
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
                                Log.Info($"TechNotFound : {scriptEntry}");
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
                if (loopCount >= OwnerEmpire.ResearchStrategy.TechPath.Count)
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
            Array<TechEntry> availableTechs = OwnerEmpire.CurrentTechsResearchable();

            if (availableTechs.Count <= 0) return false;

            DebugLog($"Possible Techs : {availableTechs.Count}");

            string researchTopic = "";
            float moneyNeeded = BuildCapacity * .2f;

            availableTechs = LineFocus.LineFocusShipTechs(modifier, availableTechs, command2);
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
                            var techType = ConvertTechStringTechType(script[i]);

                            TechEntry researchTech = GetScriptedTech(command1, techType, availableTechs, moneyNeeded);
                            if (researchTech == null) continue;

                            string testResearchTopic = researchTech.UID;

                            int currentCost = 0;
                            if (techType == researchTech.TechnologyType)
                                currentCost = (int)Math.Ceiling(researchTech.TechCost * CostNormalizer);
                            else
                                currentCost = (int)Math.Ceiling(researchTech.GetLookAheadType(techType) * CostNormalizer);

                            if (command1 == "CHEAPEST" && currentCost < previousCost)
                            {
                                researchTopic = testResearchTopic;
                                previousCost = currentCost;
                                CostNormalizer += .005f;
                            }
                            else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                            {
                                researchTopic = testResearchTopic;
                                previousCost = currentCost;
                                CostNormalizer *= .25f;
                            }
                        }

                        break;
                    }

                default:
                    {
                        var techType = ConvertTechStringTechType(command2);
                        TechEntry researchTech = GetScriptedTech(command1, techType, availableTechs, moneyNeeded);
                        if (researchTech != null)
                        {
                            researchTopic = researchTech.UID;
                            break;
                        }
                        researchTopic = null;
                        break;
                    }
            }
            OwnerEmpire.ResearchTopic = researchTopic;
            DebugLog($"Tech Choosen : {researchTopic}");

            if (string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
                return false;
            return true;
        }

        private TechnologyType ConvertTechStringTechType(string typeName)
        {
            TechnologyType techType = TechnologyType.General;
            try
            {
                techType = (TechnologyType)Enum.Parse(typeof(TechnologyType), typeName);
            }
            catch
            {
                Log.Error($"techType not found : ");
            }
            return techType;
        }

        private TechEntry GetScriptedTech(string command1, TechnologyType techType, Array<TechEntry> availableTechs, float moneyNeeded)
        {

            DebugLog($"\nFind : {techType.ToString()}");

            TechEntry[] filteredTechs;
            TechEntry researchTech = null;

            filteredTechs = availableTechs.Filter(tech => IncludeFreighters(tech, moneyNeeded) && tech.TechnologyType == techType);

            if (filteredTechs.Length == 0)
            {
                //this get lookahead is tricky.
                //Its trying here to see if the current tech with the wrong techType has a future tech with the right one.
                //otherwise it would be a simple tech matches techType formula.
                //its also checking economy tech types for their hulls.
                //It doesnt want to build freighters to make more money.
                //but it does want to build stations that make more money.
                filteredTechs = availableTechs.Filter(tech =>
                {
                    if (availableTechs.Count == 1) return true;
                    if (tech.GetLookAheadType(techType) > 0 && IncludeFreighters(tech, moneyNeeded))
                        return true;
                    return false;
                });
            }
            LogFinalScriptTechs(command1, techType, filteredTechs);
            researchTech = ChooseScriptTech(command1, filteredTechs, techType);
            if (researchTech == null)
            {
                DebugLog($"{techType.ToString()} : No Tech found");
                return null;
            }
            return researchTech;
        }

        bool IncludeFreighters(TechEntry tech, float moneyNeeded)
        {
            if (tech.TechnologyType != TechnologyType.Economic)
                return true;

                if (tech.Tech.HullsUnlocked.Count == 0 || moneyNeeded < 1f)
                    return true;

                foreach (var hull in tech.Tech.HullsUnlocked)
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

        }

        private TechEntry ChooseScriptTech(string command1, TechEntry[] filteredTechs, TechnologyType techType)
        {
            TechEntry researchTech = null;
            if (command1 == "CHEAPEST")
                researchTech = filteredTechs.FindMin(cost =>
                {
                    if (cost.TechnologyType == techType)
                        return cost.TechCost;
                    return cost.GetLookAheadType(techType);
                });
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
                if (tech.TechnologyType == techtype)
                    DebugLog($" {tech.UID} : {tech.TechCost}");
                else
                {
                    float lookAheadCost = tech.GetLookAheadType(techtype);
                    DebugLog($" {tech.UID} : (LA){lookAheadCost}");
                }
            }
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