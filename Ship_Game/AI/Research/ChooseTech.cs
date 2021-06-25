﻿using Ship_Game.Ships;
using System;

namespace Ship_Game.AI.Research
{
    public class ChooseTech
    {
        readonly Empire OwnerEmpire;
        int ScriptIndex;
        public ResearchStrategy ScriptType { get; private set; }
        public readonly ShipTechLineFocusing LineFocus;
        ResearchPriorities ResearchPriorities;
        readonly EconomicResearchStrategy Strategy;
        readonly ResearchOptions ResearchMods;

        // this reduces the cost of techs so that techs that are near the same cost
        // get compared as if they are the same cost.
        float CostNormalizer;
        int NormalizeTechCost(float techCost)
        {
            return (int)Math.Ceiling(techCost * CostNormalizer);
        }

        public enum ResearchStrategy
        {
            Random,
            Scripted
        }

        public ChooseTech(Empire empire)
        {
            OwnerEmpire    = empire;
            Strategy       = OwnerEmpire.Research.Strategy;
            ResearchMods   = new ResearchOptions();
            LineFocus      = new ShipTechLineFocusing(empire, ResearchMods);
            ScriptType     = Strategy?.TechPath?.Count > 0 ? ResearchStrategy.Scripted : ResearchStrategy.Random;
        }

        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        public ResearchPriorities GetPriorities() => ResearchPriorities;

        public void PickResearchTopic(string command)
        {
            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            OwnerEmpire.data.TechDelayTime++;
            ResearchPriorities = new ResearchPriorities(OwnerEmpire, ResearchMods);
            CostNormalizer = OwnerEmpire.Research.NetResearch.LowerBound(1) / 100f;
            DebugLog($"ResearchStrategy : {ScriptType.ToString()}");

            switch (ScriptType)
            {
                case ResearchStrategy.Random:
                    {
                        ScriptedResearch(command, "RANDOM", GetPriorities().TechCategoryPrioritized);
                        break;
                    }
                case ResearchStrategy.Scripted:
                    {
                        ProcessScript();
                        break;
                    }
                default:
                    return;
            }
        }

        public bool ProcessScript()
        {
            bool atWar         = ResearchPriorities.Wars > 0.5f;
            bool highTaxes     = ResearchPriorities.Economics > 0.5f;
            bool lowResearch   = ResearchPriorities.ResearchDebt > 0.5f;
            bool lowIncome     = OwnerEmpire.Money < OwnerEmpire.NetPlanetIncomes;
            int loopCount      = 0;
            var strategy       = Strategy;

            Start:
            if (strategy != null && ScriptIndex < strategy.TechPath.Count && loopCount < strategy.TechPath.Count)
            {
                string scriptEntry   = strategy.TechPath[ScriptIndex].id;
                string scriptCommand = OwnerEmpire.HasTechEntry(scriptEntry) ? scriptEntry : scriptEntry.Split(':')[0];
                string modifier      = "";
                string[] script      = scriptEntry.Split(':');
                string[] modifiers   = new string[script.Length - 1];

                DebugLog($"index : {ScriptIndex}");
                DebugLog($"Script Command : {scriptCommand}");

                switch (scriptCommand)
                {
                    case "SCRIPT":
                        if (script.Length > 2)
                            modifier = script[2];

                        ScriptIndex++;
                        if (ScriptedResearch("CHEAPEST", script[1], modifier))
                            return true;

                        loopCount++;
                        goto Start;
                    case "LOOP":
                        ScriptIndex = int.Parse(OwnerEmpire.Research.Strategy.TechPath[ScriptIndex].id.Split(':')[1]);
                        loopCount++;
                        goto Start;
                    case "CHEAPEST":
                        if (script.Length == 1)
                        {
                            GoRandomOnce();
                            ScriptIndex++;
                            return true;
                        }

                        for (int i = 1; i < script.Length; i++)
                        {
                            modifiers[i - 1] = script[i];
                        }

                        modifier = string.Join(":", modifiers);
                        ScriptIndex++;
                        if (ScriptedResearch(scriptCommand, script[1], modifier))
                            return true;

                        loopCount++;
                        goto Start;
                    case "EXPENSIVE":
                        if (script.Length == 1)
                        {
                            GoRandomOnce("EXPENSIVE");
                            ScriptIndex++;
                            return true;
                        }

                        for (int i = 1; i < script.Length; i++)
                        {
                            modifiers[i - 1] = script[i];
                        }

                        modifier = string.Join(":", modifiers);
                        ScriptIndex++;
                        if (ScriptedResearch(scriptCommand, script[1], modifier))
                            return true;

                        loopCount++;
                        goto Start;
                    case "IFWAR":            loopCount += ScriptBump(atWar);                    goto Start;
                    case "IFHIGHTAX":        loopCount += ScriptBump(highTaxes);                goto Start;
                    case "IFPEACE":          loopCount += ScriptBump(!atWar);                   goto Start;
                    case "IFCYBERNETIC":     loopCount += ScriptBump(OwnerEmpire.IsCybernetic); goto Start;
                    case "IFLOWRESEARCH":    loopCount += ScriptBump(lowResearch);              goto Start;
                    case "IFNOTLOWRESEARCH": loopCount += ScriptBump(!lowResearch);             goto Start;
                    case "IFLOWINCOME":      loopCount += ScriptBump(lowIncome);                goto Start;
                    case "IFNOTLOWINCOME":   loopCount += ScriptBump(!lowIncome);               goto Start;
                    case "RANDOM":           GoRandomOnce(); ScriptIndex++;                     return true;
                    case "IFRESEARCHHIGHERTHAN":
                        bool researchPreReqMet  = false;
                        string[] researchScript = scriptEntry.Split(':');
                        if (float.TryParse(researchScript[2], out float researchAmount))
                            if (OwnerEmpire.Research.NetResearch >= researchAmount)
                                researchPreReqMet = true;

                        loopCount += ScriptBump(researchPreReqMet);
                        goto Start;
                    case "IFTECHRESEARCHED":
                        bool techResearched           = false;
                        string[] techResearchedScript = scriptEntry.Split(':');
                        if (OwnerEmpire.TryGetTechEntry(techResearchedScript[2], out TechEntry checkedTech))
                        {
                            if (checkedTech.Unlocked)
                                techResearched = true;
                        }
                        loopCount += ScriptBump(techResearched);
                        goto Start;
                    default:
                        DebugLog($"Hard Script : {scriptEntry}");
                        if (OwnerEmpire.TryGetTechEntry(scriptEntry, out TechEntry defaultTech))
                        {
                            if (defaultTech.Unlocked)
                            {
                                DebugLog("Already Unlocked");
                                ScriptIndex++;
                                goto Start;
                            }

                            if (!defaultTech.Unlocked && OwnerEmpire.HavePreReq(defaultTech.UID))
                            {
                                DebugLog("Researching");
                                OwnerEmpire.Research.SetTopic(defaultTech.UID);
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

                        foreach (EconomicResearchStrategy.Tech tech in OwnerEmpire.Research.Strategy.TechPath)
                        {
                            if (OwnerEmpire.HasTechEntry(tech.id) && !OwnerEmpire.HasUnlocked(tech.id) &&
                                OwnerEmpire.HavePreReq(tech.id))
                            {
                                OwnerEmpire.Research.SetTopic(tech.id);
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
            if (OwnerEmpire.Research.NoTopic)
            {
                GoRandomOnce();
                if (loopCount >= OwnerEmpire.Research.Strategy.TechPath.Count)
                    ScriptType = ResearchStrategy.Random;

            }
            return false;
        }

        bool GoRandomOnce(string command = "CHEAPEST")
        {
            DebugLog("Go Random Once");
            ScriptType = ResearchStrategy.Random;
            ScriptedResearch(command, "RANDOM", ResearchPriorities.TechCategoryPrioritized);
            ScriptType = ResearchStrategy.Scripted;
            return OwnerEmpire.Research.HasTopic;
        }

        int ScriptBump(bool check)
        {
            if (check)
            {
                ScriptIndex = int.Parse(OwnerEmpire.Research.Strategy.TechPath[ScriptIndex].id.Split(':')[1]);
                return 1;
            }
            ScriptIndex++;
            return 0;
        }

        public bool ScriptedResearch(string command1, string command2, string modifier)
        {
            Array<TechEntry> availableTechs = OwnerEmpire.CurrentTechsResearchable();

            if (availableTechs.Count <= 0)
            {
                OwnerEmpire.Research.SetNoResearchLeft(true);
                return false;
            }
            else
            {
                OwnerEmpire.Research.SetNoResearchLeft(false);
            }

            DebugLog($"Possible Techs : {availableTechs.Count}");

            string researchTopic = "";

            availableTechs = LineFocus.LineFocusShipTechs(modifier, availableTechs, command2);

            int previousCost = command1 == "CHEAPEST" ? int.MaxValue : int.MinValue;
            switch (command2)
            {
                case "RANDOM":
                case "TECH":
                    {
                        string[] script = modifier.Split(':');
                        for (int i = 1; i < script.Length; i++)
                        {
                            var techTypeName    = script[i];
                            var techType             = ConvertTechStringTechType(techTypeName);
                            TechEntry researchTech   = GetScriptedTech(command1, techType, availableTechs);
                            bool isCheaper           = command1 == "CHEAPEST";
                            string testResearchTopic = DoesCostCompare(ref previousCost, researchTech, techType, isCheaper);

                            if (testResearchTopic.NotEmpty())
                                researchTopic = testResearchTopic;
                            float priority = 0;
                            // bump priority but consider ship tech categories as more of a unit.
                            if (techTypeName.Contains("ship"))
                                priority = 0.0002f;
                            else
                                priority = 0.005f;

                            CostNormalizer += isCheaper ? priority : 0.25f;
                        }

                        break;
                    }
                default:
                    {
                        var techType = ConvertTechStringTechType(command2);
                        TechEntry researchTech = GetScriptedTech(command1, techType, availableTechs);
                        if (researchTech != null)
                        {
                            researchTopic = researchTech.UID;
                            break;
                        }
                        researchTopic = null;
                        break;
                    }
            }

            OwnerEmpire.Research.SetTopic(researchTopic);
            return OwnerEmpire.Research.HasTopic;
        }

        string DoesCostCompare(ref int previousCost, TechEntry researchTech, TechnologyType techType, bool isCheaper)
        {
            string testResearchTopic = researchTech?.UID ?? string.Empty;
            if (testResearchTopic.IsEmpty())
                return testResearchTopic;

            int currentCost = CostToResearchTechType(researchTech, techType);

            if (currentCost > 0)
            {
                if (isCheaper)
                {
                    if (currentCost < previousCost)
                    {
                        previousCost = currentCost;
                        return testResearchTopic;
                    }
                }
                else
                {
                    if (currentCost > previousCost)
                    {
                        previousCost = currentCost;
                        return testResearchTopic;
                    }
                }
            }

            return string.Empty;
        }

        public int CostToResearchTechType(TechEntry researchTech, TechnologyType techType)
        {
            float cost = 0;
            if (researchTech.IsTechnologyType(techType))
                cost = NormalizeTechCost(researchTech.TechCost);

            else if (!researchTech.IsPrimaryShipTech() && LineFocus.BestCombatShip?.shipData.TechsNeeded.Contains(researchTech.UID) == true)
                cost = NormalizeTechCost(researchTech.TechCost);

            else  if (!techType.ToString().Contains("Ship"))
                cost = NormalizeTechCost(researchTech.CostOfNextTechWithType(techType));

            return (int)Math.Ceiling(cost * (cost > 0 ? ResearchMods.CostMultiplier(researchTech) : 0));
        }

        public static TechnologyType ConvertTechStringTechType(string typeName)
        {
            TechnologyType techType = TechnologyType.General;
            try
            {
                techType = (TechnologyType)Enum.Parse(typeof(TechnologyType), typeName);
            }
            catch
            {
                Log.Error("techType not found : ");
            }
            return techType;
        }

        private TechEntry GetScriptedTech(string command1, TechnologyType techType, Array<TechEntry> availableTechs)
        {

            DebugLog($"\nFind : {techType.ToString()}");
            bool wantsShipTech = techType.ToString().Contains("Ship");
            bool generalTechs = techType == TechnologyType.GroundCombat || techType == TechnologyType.General;

            TechEntry[] techsTypeFiltered = availableTechs.Filter(tech =>
            {
                // verify that the tech has a cost. filters out techs that cant be researched
                if (CostToResearchTechType(tech, techType) <= 0)
                    return false;

                // do a tight comparison for ships
                if (wantsShipTech && !tech.IsPrimaryShipTech())
                {
                    return IsLineFocusedShipTech(tech);
                }
                // dont use primary ship techs for normal techs.
                if (!wantsShipTech && tech.IsPrimaryShipTech())
                    return false;
                // do a loose check for tech type.
                if (!tech.IsTechnologyType(techType))
                    return false;

                return true;
            });

            if (techsTypeFiltered.Length == 0)
            {
                // if not techs found do a looser check.
                // does the tech have any of the wanted types.
                // if not do any future techs have teh wanted type
                if (wantsShipTech)
                {
                    if (LineFocus.BestCombatShip != null)
                    {
                        techsTypeFiltered = availableTechs.Filter(tech =>
                        {
                            if (!tech.ContainsShipTech())
                            {
                                foreach (var shipTech in LineFocus.BestCombatShip.shipData.TechsNeeded)
                                {
                                    bool foundTech = tech.Tech.UID == shipTech;
                                    if (foundTech && tech.Tech.ActualCost > 0)
                                    {
                                        return true;
                                    }
                                }
                            }
                            return false;
                        });
                    }
                    else
                    {
                        techsTypeFiltered = availableTechs.Filter(tech => tech.IsTechnologyType(techType));
                    }
                }
                else
                {
                    techsTypeFiltered = availableTechs.Filter(tech =>
                    {
                        {
                            if (!tech.IsTechnologyType(techType) && tech.CostOfNextTechWithType(techType) <= 0)
                                return false;
                            if (tech.IsPrimaryShipTech())
                                return false;

                            if (CostToResearchTechType(tech, techType) <= 0)
                                return false;
                        }
                        return true;
                    });
                }
            }
            LogPossibleTechs(techsTypeFiltered, techType);
            TechEntry researchTech = TechWithWantedCost(command1, techsTypeFiltered, techType);

            return researchTech;
        }

        bool IsLineFocusedShipTech(TechEntry tech)
        {
            bool isShipTech = false;
            var shipTechs = LineFocus.BestCombatShip?.shipData.TechsNeeded;
            if (shipTechs != null)
                isShipTech = LineFocus.BestCombatShip?.shipData.TechsNeeded.Contains(tech.Tech.UID) == true;
            return isShipTech;
        }

        private TechEntry TechWithWantedCost(string command1, TechEntry[] filteredTechs, TechnologyType techType)
        {
            TechEntry researchTech = null;
            if (command1 == "CHEAPEST")
                researchTech = filteredTechs.FindMin(cost => CostToResearchTechType(cost, techType));
            else if (command1 == "EXPENSIVE")
                researchTech = filteredTechs.FindMax(cost => CostToResearchTechType(cost, techType));

            DebugLog($"{command1} : {researchTech?.UID ?? "Nothing Found"}");
            return researchTech;
        }

        private void LogPossibleTechs(TechEntry[] filteredTechs, TechnologyType techType)
        {
            foreach (var tech in filteredTechs)
                DebugLog($" {tech.UID} : {tech.TechCost} : ({CostToResearchTechType(tech, techType)})");
        }
    }
}
