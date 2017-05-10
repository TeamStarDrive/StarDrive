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
            if (string.IsNullOrEmpty(this.OwnerEmpire.ResearchTopic))
            {
                //Added by McShooterz: random tech is less random, selects techs based on priority
                //Check for needs of empire
                // Ship ship;
                // int researchneeded = 0;

                //if( ResourceManager.ShipsDict.TryGetValue(this.BestCombatShip, out ship))
                //{
                //    ship.shipData.TechScore / this.empire.Research
                //}
                bool cybernetic = this.OwnerEmpire.data.Traits.Cybernetic > 0;
                bool atWar = false;
                bool highTaxes = false;
                bool lowResearch = false;
                bool lowincome = false;
                float researchDebt = 0;
                float wars = this.OwnerEmpire.AllRelations
                    .Where(war => !war.Key.isFaction && (war.Value.AtWar || war.Value.PreparingForWar))
                    .Sum(str => str.Key.currentMilitaryStrength / this.OwnerEmpire.currentMilitaryStrength);

                if (this.OwnerEmpire.data.TaxRate >= .50f)
                    highTaxes = true;
                if (!string.IsNullOrEmpty(this.postResearchTopic))
                    researchDebt = .1f + (this.OwnerEmpire.TechnologyDict[this.postResearchTopic].TechCost /
                                          (.1f + (100 * UniverseScreen.GamePaceStatic) *
                                           this.OwnerEmpire.GetPlanets().Sum(research => research.NetResearchPerTurn)));
                if (researchDebt > 4)
                    lowResearch = true;
                //if (this.empire.GetPlanets().Sum(research => research.NetResearchPerTurn) < this.empire.GetPlanets().Count / 3)
                //    lowResearch = true;
                float economics =
                    (this.OwnerEmpire.data.TaxRate * 10); // 10 - (int)(this.empire.Money / (this.empire.GrossTaxes + 1));
                //(int)(this.empire.data.TaxRate * 10 + this.empire.Money < this.empire.GrossTaxes?5:0);
                float needsFood = 0;
                foreach (Planet hunger in this.OwnerEmpire.GetPlanets())
                {
                    if ((cybernetic ? hunger.ProductionHere : hunger.FoodHere) / hunger.MAX_STORAGE <
                        .20f) //: hunger.MAX_STORAGE / (hunger.FoodHere+1) > 25)
                    {
                        needsFood++;
                    }
                    if (!this.OwnerEmpire.GetTDict()["Biospheres"].Unlocked)
                    {
                        if (hunger.Fertility == 0)
                            needsFood += 2;
                    }
                }
                float shipBuildBonus = 0f;
                if (this.OwnerEmpire.data.TechDelayTime > 0)
                    this.OwnerEmpire.data.TechDelayTime--;
                if (this.OwnerEmpire.data.TechDelayTime > 0)
                {
                    shipBuildBonus = -5 - this.OwnerEmpire.data.TechDelayTime;
                }
                else
                    shipBuildBonus = 0;

                needsFood = needsFood > 0 ? needsFood / this.OwnerEmpire.GetPlanets().Count : 0;
                needsFood *= 10;

                switch (this.res_strat)
                {
                    case EmpireAI.ResearchStrategy.Random:
                    case EmpireAI.ResearchStrategy.Scripted:
                    {
                        if (true)
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
                                this.randomizer(resStrat.MilitaryPriority, (wars + shipBuildBonus) * .5f));

                            string sendToScript = string.Empty;
                            int max = 0;
                            foreach (var pWeighted in priority.OrderByDescending(pri => pri.Value))
                            {
                                if (max > priority.Count)
                                    break;
                                if (pWeighted.Value < 0) //&& !string.IsNullOrEmpty(sendToScript))
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
                        }

                        return;

#if false // All of this was disabled by Crunchy gremlin:
//changed by gremlin exclude module tech that we dont have any ships that use it.
                        ConcurrentBag<Technology> AvailableTechs = new ConcurrentBag<Technology>();
                        //foreach (KeyValuePair<string, Ship_Game.Technology> Technology in ResourceManager.TechTree)

                        //System.Threading.Tasks.Parallel.ForEach(ResourceManager.TechTree, Technology =>
                        foreach(var Technology in ResourceManager.TechTree)
                        {
                            TechEntry tech = null;// new TechEntry();
                            bool techexists = this.empire.GetTDict().TryGetValue(Technology.Key, out tech);
                            if (!techexists || tech == null)
                                //continue;
                                return;
                            Technology technology = tech.Tech;
                            if (tech.Unlocked
                                || !this.empire.HavePreReq(Technology.Key)
                                || (Technology.Value.Secret && !tech.Discovered)
                                || technology.BuildingsUnlocked.Where(winsgame => ResourceManager.BuildingsDict[winsgame.Name].WinsGame == true).Count() > 0
                                || !tech.shipDesignsCanuseThis
                                || (tech.shipDesignsCanuseThis && technology.ModulesUnlocked.Count > 0 && technology.HullsUnlocked.Count == 0
                                && !this.empire.WeCanUseThisNow(tech.Tech)))
                            {
                                //continue;
                                return;
                            }
                            AvailableTechs.Add(Technology.Value);
                        }//);
                        if (AvailableTechs.Count == 0)
                            break;
                        foreach(Technology tech in AvailableTechs.OrderBy(tech => tech.Cost))
                        {
                            switch (tech.TechnologyType)
                            {
                                //case TechnologyType.ShipHull:
                                //    {
                                //        //Always research when able
                                //        this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipGeneral:
                                //    {
                                //        if (RandomMath.InRange(4) == 3)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipWeapons:
                                //    {
                                //        if(atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipDefense:
                                //    {
                                //        if (atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                case TechnologyType.GroundCombat:
                                    {
                                        if (atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Economic:
                                    {
                                        if (highTaxes || RandomMath.InRange(this.empire.getResStrat().ExpansionPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Research:
                                    {
                                        if (lowResearch || RandomMath.InRange(this.empire.getResStrat().ResearchPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Industry:
                                    {
                                        if (RandomMath.InRange(this.empire.getResStrat().IndustryPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Colonization:
                                    {
                                        if (RandomMath.InRange(this.empire.getResStrat().ExpansionPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                            if (!string.IsNullOrEmpty(this.empire.ResearchTopic))
                                break;
                        }
                        if (string.IsNullOrEmpty(this.empire.ResearchTopic))
                            this.empire.ResearchTopic = AvailableTechs.OrderBy(tech => tech.Cost).First().UID;
                        break;
#endif
                    }
                    //case GSAI.ResearchStrategy.Scripted:
                    default:
                    {
                        int loopcount = 0;
                        Start:
                        if (this.OwnerEmpire.getResStrat() != null &&
                            ScriptIndex < this.OwnerEmpire.getResStrat().TechPath.Count &&
                            loopcount < this.OwnerEmpire.getResStrat().TechPath.Count)
                        {
                            string scriptentry = this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id;
                            string scriptCommand = this.OwnerEmpire.GetTDict().ContainsKey(scriptentry)
                                ? scriptentry
                                : scriptentry.Split(':')[0];
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
                                        return;
                                    loopcount++;
                                    goto Start;
                                }
                                case "LOOP":
                                {
                                    ScriptIndex =
                                        int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                    loopcount++;
                                    goto Start;
                                }
                                case "CHEAPEST":
                                {
                                    string modifier = "";
                                    string[] script = scriptentry.Split(':');

                                    if (script.Count() == 1)
                                    {
                                        this.res_strat = EmpireAI.ResearchStrategy.Random;
                                        this.RunResearchPlanner();
                                        this.res_strat = EmpireAI.ResearchStrategy.Scripted;
                                        ScriptIndex++;
                                        return;
                                    }
                                    string[] modifiers = new string[script.Count() - 1];
                                    for (int i = 1; i < script.Count(); i++)
                                    {
                                        modifiers[i - 1] = script[i];
                                    }
                                    modifier = String.Join(":", modifiers);
                                    ScriptIndex++;
                                    if (ScriptedResearch(scriptCommand, script[1], modifier))
                                        return;
                                    loopcount++;
                                    goto Start;
                                }
                                case "EXPENSIVE":
                                {
                                    string modifier = "";
                                    string[] script = scriptentry.Split(':');

                                    if (script.Count() == 1)
                                    {
                                        this.res_strat = EmpireAI.ResearchStrategy.Random;
                                        this.RunResearchPlanner();
                                        this.res_strat = EmpireAI.ResearchStrategy.Scripted;
                                        ScriptIndex++;
                                        return;
                                    }
                                    string[] modifiers = new string[script.Count() - 1];
                                    for (int i = 1; i < script.Count(); i++)
                                    {
                                        modifiers[i - 1] = script[i];
                                    }
                                    modifier = String.Join(":", modifiers);
                                    ScriptIndex++;
                                    if (ScriptedResearch(scriptCommand, script[1], modifier))
                                        return;
                                    loopcount++;
                                    goto Start;
                                }
                                case "IFWAR":
                                {
                                    if (atWar)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;

                                    goto Start;
                                }
                                case "IFHIGHTAX":
                                {
                                    if (highTaxes)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;

                                    goto Start;
                                }
                                case "IFPEACE":
                                {
                                    if (!atWar)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;

                                    goto Start;
                                }
                                case "IFCYBERNETIC":
                                {
                                    if (this.OwnerEmpire.data.Traits.Cybernetic > 0
                                    ) //&& !this.empire.GetBDict()["Biospheres"])//==true)//this.empire.GetTDict().Where(biosphereTech=> biosphereTech.Value.GetTech().BuildingsUnlocked.Where(biosphere=> ResourceManager.BuildingsDict[biosphere.Name].Name=="Biospheres").Count() >0).Count() >0)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;
                                    goto Start;
                                }
                                case "IFLOWRESEARCH":
                                {
                                    if (lowResearch)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;
                                    goto Start;
                                }
                                case "IFNOTLOWRESEARCH":
                                {
                                    if (!lowResearch)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;
                                    goto Start;
                                }
                                case "IFLOWINCOME":
                                {
                                    if (lowincome)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;
                                    goto Start;
                                }
                                case "IFNOTLOWINCOME":
                                {
                                    if (!lowincome)
                                    {
                                        ScriptIndex =
                                            int.Parse(this.OwnerEmpire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                        loopcount++;
                                        goto Start;
                                    }
                                    ScriptIndex++;
                                    goto Start;
                                }
                                case "RANDOM":
                                {
                                    this.res_strat = EmpireAI.ResearchStrategy.Random;
                                    this.RunResearchPlanner();
                                    this.res_strat = EmpireAI.ResearchStrategy.Scripted;
                                    ScriptIndex++;
                                    return;
                                }
                                default:
                                {
                                    TechEntry defaulttech;
                                    if (this.OwnerEmpire.GetTDict().TryGetValue(scriptentry, out defaulttech))
                                    {
                                        if (defaulttech.Unlocked)

                                        {
                                            ScriptIndex++;
                                            goto Start;
                                        }
                                        if (!defaulttech.Unlocked && this.OwnerEmpire.HavePreReq(defaulttech.UID))
                                        {
                                            this.OwnerEmpire.ResearchTopic = defaulttech.UID;
                                            ScriptIndex++;
                                            if (!string.IsNullOrEmpty(scriptentry))
                                                return;
                                        }
                                    }
                                    else
                                    {
                                        Log.Info("TechNotFound : " + scriptentry);
                                        ScriptIndex++;
                                        //Log.Info(scriptentry);
                                    }


                                    foreach (EconomicResearchStrategy.Tech tech in this.OwnerEmpire.getResStrat().TechPath)
                                    {
                                        if (!this.OwnerEmpire.GetTDict().ContainsKey(tech.id) ||
                                            this.OwnerEmpire.GetTDict()[tech.id].Unlocked ||
                                            !this.OwnerEmpire.HavePreReq(tech.id))
                                        {
                                            continue;
                                        }

                                        OwnerEmpire.ResearchTopic = tech.id;
                                        ScriptIndex++;
                                        if (!string.IsNullOrEmpty(tech.id))
                                            return;
                                    }
                                    this.res_strat = EmpireAI.ResearchStrategy.Random;
                                    ScriptIndex++;
                                    return;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(this.OwnerEmpire.ResearchTopic))
                        {
                            this.res_strat = EmpireAI.ResearchStrategy.Random;
                        }
                        return;
                    }
                    //default:
                    //{
                    //    return;
                    //}
                }
            }
            if (!string.IsNullOrEmpty(this.OwnerEmpire.ResearchTopic) && this.OwnerEmpire.ResearchTopic != this.postResearchTopic)
            {
                this.postResearchTopic = this.OwnerEmpire.ResearchTopic;
            }
        }

        private bool ScriptedResearch(string command1, string command2, string modifier)
        {
            Array<Technology> AvailableTechs = new Array<Technology>();

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
                AvailableTechs.Add(kv.Value.Tech);
            }

            if (AvailableTechs.Count <= 0)
            {
                return false;
            }

            List<string> useableTech = new List<string>();


            string researchtopic = "";
            TechnologyType techtype;

            #region hull checking.

            this.OwnerEmpire.UpdateShipsWeCanBuild();


            //Ship BestShip = null;// ""; //this.BestCombatShip;          //Not referenced in code, removing to save memory
            //float bestShipStrength = 0f;          //Not referenced in code, removing to save memory
            float techcost = -1;
            float str = 0;
            float moneyNeeded = this.OwnerEmpire.data.ShipBudget * .2f;
            //float curentBestshipStr = 0;          //Not referenced in code, removing to save memory

            if (this.BestCombatShip != null)
            {
                //this.empire.UpdateShipsWeCanBuild();
                if (this.OwnerEmpire.ShipsWeCanBuild.Contains(this.BestCombatShip.Name))
                    this.BestCombatShip = null;
            }
            if (this.BestCombatShip == null && (modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") ||
                                                modifier.Contains("ShipGeneral")
                                                || modifier.Contains("ShipHull")))
            {
                List<string> globalShipTech = new List<string>();
                foreach (string purgeRoots in this.OwnerEmpire.ShipTechs)
                {
                    Technology bestshiptech = null;
                    if (!ResourceManager.TechTree.TryGetValue(purgeRoots, out bestshiptech))
                        continue;
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
                    globalShipTech.Add(bestshiptech.UID);
                }

                foreach (Technology bestshiptech in AvailableTechs)
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
                        .TechScore)) //.OrderBy(orderbytech => orderbytech.shipData.TechScore))
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

                        if (shortTermBest.shipData.ShipStyle != this.OwnerEmpire.data.Traits.ShipType
                        ) // && (!this.empire.GetHDict().TryGetValue(shortTermBest.shipData.Hull, out empirehulldict) || !empirehulldict))
                        {
                            continue;
                        }
                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                            continue;
                        if (this.OwnerEmpire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                            continue;
                        if (!this.ShipIsGoodForGoals(shortTermBest))
                            continue;
                        if (shortTermBest.shipData.techsNeeded.Intersect(useableTech).Count() == 0)
                            continue;

                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                        {
                            if (Empire.Universe.Debug)
                            {
                                Log.Info(this.OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                            }
                            continue;
                        }

                        //try to line focus to main goal but if we cant, line focus as best as possible by what we already have. 

                        //Array<string> TechsNeeded =new Array<string>(shortTermBest.shipData.techsNeeded.Except(this.empire.ShipTechs));                        
                        //int techdifference = shortTermBest.shipData.techsNeeded.Intersect(this.empire.ShipTechs).Count();
                        int mod = 0;
                        //shortTermBest.shipData.techsNeeded.Intersect(useableTech).Count();
                        if (!this.OwnerEmpire.canBuildBombers && shortTermBest.BombBays.Count > 0)
                        {
                            mod = (int) ResourceManager.TechTree.Values
                                .Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech
                                                   .ModulesUnlocked
                                                   .Where(
                                                       modu => ResourceManager.GetModuleTemplate(modu.ModuleUID)
                                                                   .ModuleType == ShipModuleType.Bomb)
                                                   .Count() > 0)
                                .Sum(tech => tech.Cost);
                        }
                        if (!this.OwnerEmpire.canBuildCarriers && shortTermBest.GetHangars().Count > 0)
                            mod = (int) ResourceManager.TechTree.Values
                                .Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech
                                                   .ModulesUnlocked
                                                   .Where(
                                                       modu => ResourceManager.GetModuleTemplate(modu.ModuleUID)
                                                                   .ModuleType == ShipModuleType.Hangar)
                                                   .Count() > 0)
                                .Sum(tech => tech.Cost);
                        if (!this.OwnerEmpire.canBuildTroopShips && shortTermBest.hasAssaultTransporter ||
                            shortTermBest.hasOrdnanceTransporter || shortTermBest.hasRepairBeam ||
                            shortTermBest.HasRepairModule || shortTermBest.HasSupplyBays ||
                            shortTermBest.hasTransporter || shortTermBest.InhibitionRadius > 0)
                            mod = (int) ResourceManager.TechTree.Values
                                .Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech
                                                   .ModulesUnlocked
                                                   .Where(modu =>
                                                       {
                                                           ShipModuleType test =
                                                               ResourceManager.GetModuleTemplate(modu.ModuleUID)
                                                                   .ModuleType;
                                                           return (test == ShipModuleType.Troop ||
                                                                   test == ShipModuleType.Transporter || test ==
                                                                   ShipModuleType.Hangar);
                                                       }
                                                   )
                                                   .Count() > 0)
                                .Sum(tech => tech.Cost);
                        if (!this.OwnerEmpire.canBuildFrigates &&
                            shortTermBest.shipData.HullRole == ShipData.RoleName.cruiser)
                            continue;
                        Array<string> currentTechs =
                            new Array<string>(shortTermBest.shipData.techsNeeded.Except(this.OwnerEmpire.ShipTechs));
                        int currentTechCost = (int) ResourceManager.TechTree.Values
                            .Where(tech => currentTechs.Contains(tech.UID))
                            .Sum(tech => tech.Cost);
                        currentTechCost -= mod;

                        currentTechCost = currentTechCost / (int) (this.OwnerEmpire.Research * 10 + 1);
                        //if (techratio < (.1f * hullScaler) + (mod * .1f) && techratio > techcost)// && realstr > .75f && realTechCost <1.25) //techratio <= .3f && 
                        if ((currentTechCost < techcost && str < shortTermBest.shipData.BaseStrength) || techcost == -1)
                        {
                            if (techcost > 0)
                                str = shortTermBest.shipData.BaseStrength;
                            this.BestCombatShip = shortTermBest;
                            techcost = currentTechCost; // techratio;
                            shipchange = true;
                            continue;
                        }
                    }

                    if (shipchange)
                    {
                        if (Empire.Universe.Debug)
                        {
                            Log.Info(this.OwnerEmpire.data.PortraitName + " : NewBestShip :" + this.BestCombatShip.Name +
                                     " : " + this.BestCombatShip.shipData.HullRole.ToString());
                        }
                    }

                    if (this.BestCombatShip != null && this.OwnerEmpire.GetHDict()
                            .TryGetValue(this.BestCombatShip.shipData.Hull, out hullKnown))
                    {
                        if (hullKnown)
                            hullScaler++;
                    }
                    else
                        hullScaler++;

                    //End of line focusing. 
                } ///while (this.BestCombatShip == null && hullScaler < 10 );
                if (!hullKnown)
                    hullScaler = 1;
            }


            //now that we have a target ship to buiild filter out all the current techs that are not needed to build it. 
            Array<Technology> bestShiptechs = new Array<Technology>();
            if ((modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") ||
                 modifier.Contains("ShipGeneral")
                 || modifier.Contains("ShipHull")))
            {
                if (this.BestCombatShip != null)
                {
                    //command2 = "SHIPTECH"; //use the shiptech choosers which just chooses tech in the list. 
                    foreach (string shiptech in this.BestCombatShip.shipData.techsNeeded)
                    {
                        Technology test = null;
                        if (ResourceManager.TechTree.TryGetValue(shiptech, out test))
                        {
                            bool skiprepeater = false;
                            //repeater compensator. This needs some deeper logic. I current just say if you research one level. Dont research any more.
                            if (test.MaxLevel > 0)
                            {
                                foreach (TechEntry repeater in this.OwnerEmpire.TechnologyDict.Values)
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

                    bestShiptechs = AvailableTechs.Intersect(bestShiptechs).ToArrayList();
                }
                else
                    Log.Info(this.OwnerEmpire.data.PortraitName + " : NoShipFound :" + hullScaler + " : ");
            }
            HashSet<Technology> remove = new HashSet<Technology>();
            foreach (Technology test in AvailableTechs)
            {
                if (test.MaxLevel > 1)
                {
                    bool skiprepeater = false;
                    foreach (TechEntry repeater in this.OwnerEmpire.TechnologyDict.Values)
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

            AvailableTechs = AvailableTechs.Except(remove).ToArrayList();
            Array<Technology> workingSetoftechs = AvailableTechs;

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
                        if (this.OwnerEmpire.data.Traits.Cybernetic > 0 && techtype ==
                            (TechnologyType) Enum.Parse(typeof(TechnologyType),
                                "Colonization")) //this.empire.GetBDict()["Biospheres"] &&
                        {
                            //techtype = TechnologyType.Industry;
                            continue;
                        }
                        if (techtype < TechnologyType.ShipHull)
                        {
                            AvailableTechs = workingSetoftechs;
                        }
                        else
                        {
                            AvailableTechs = bestShiptechs;
                        }
                        Technology ResearchTech = null;
                        if (command1 == "CHEAPEST")
                            ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype)
                                .OrderBy(cost => cost.Cost)
                                .FirstOrDefault();
                        else if (command1 == "EXPENSIVE")
                            ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype)
                                .OrderByDescending(cost => cost.Cost)
                                .FirstOrDefault();
                        //AvailableTechs.Where(econ => econ.TechnologyType == techtype).FirstOrDefault();
                        if (ResearchTech == null)
                            continue;
                        if (this.OwnerEmpire.Research > 30 && ResearchTech.Cost > this.OwnerEmpire.Research * 1000 &&
                            AvailableTechs.Count > 1)
                            continue;

                        if (techtype == TechnologyType.Economic)
                        {
                            if (ResearchTech.HullsUnlocked.Count > 0)
                            {
                                //money = this.empire.EstimateIncomeAtTaxRate(.25f);
                                if (moneyNeeded < 5f)
                                {
                                    if (command1 == "CHEAPEST")
                                        ResearchTech = AvailableTechs
                                            .Where(econ => econ.TechnologyType == techtype && econ != ResearchTech)
                                            .OrderBy(cost => cost.Cost)
                                            .FirstOrDefault();
                                    else if (command1 == "EXPENSIVE")
                                        ResearchTech = AvailableTechs
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

                            if (this.BestCombatShip != null &&
                                (techtype != TechnologyType.ShipHull && //techtype == TechnologyType.ShipHull ||//
                                 ResearchTech.ModulesUnlocked.Count > 0 ||
                                 ResourceManager.TechTree[researchtopic].ModulesUnlocked.Count > 0))
                            {
                                Technology PreviousTech = ResourceManager.TechTree[researchtopic];
                                //Ship 
                                Ship ship = this.BestCombatShip;
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
                    if (this.BestCombatShip == null)
                        return false;
                    Ship ship = this.BestCombatShip;
                    Technology shiptech = AvailableTechs
                        .Where(uid => ship.shipData.techsNeeded.Contains(uid.UID))
                        .OrderBy(techscost => techscost.Cost)
                        .FirstOrDefault();
                    if (shiptech == null)
                    {
                        //Log.Info(this.BestCombatShip.Name);
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
                        //Log.Info(this.EmpireName + " : " + techtype.ToString());
                    }
                    catch
                    {
                        this.res_strat = EmpireAI.ResearchStrategy.Random;
                        this.RunResearchPlanner();
                        this.res_strat = EmpireAI.ResearchStrategy.Scripted;
                        researchtopic = this.OwnerEmpire.ResearchTopic;
                        break;
                    }


                    //This should fix issue 414, but someone else will need to verify it
                    // Allium Sativum
                    Technology ResearchTech = null;
                    ResearchTech = AvailableTechs.OrderByDescending(econ => econ.TechnologyType == techtype)
                        .ThenBy(cost => cost.Cost)
                        .FirstOrDefault();
                    if (ResearchTech != null)
                    {
                        researchtopic = ResearchTech.UID;
                        break;
                    }
                    //float netresearch =this.empire.GetPlanets().Where(owner => owner.Owner == this.empire).Sum(research => research.NetResearchPerTurn);
                    //netresearch = netresearch == 0 ? 1 : netresearch;
                    //if (ResourceManager.TechTree[researchtopic].Cost / netresearch < 500 )
                    researchtopic = null;
                    break;
                }
            }
            {
                this.OwnerEmpire.ResearchTopic = researchtopic;
            }
            // else
            {
                // researchtopic = AvailableTechs.OrderBy(cost => cost.Cost).First().UID;
            }


            if (string.IsNullOrEmpty(this.OwnerEmpire.ResearchTopic))
                return false;
            else
            {
                //try
                //{
                //    if (ResourceManager.TechTree[this.empire.ResearchTopic].TechnologyType == TechnologyType.ShipHull)
                //    {
                //        this.BestCombatShip = "";
                //    }
                //}
                //catch(Exception e)
                //{
                //    e.Data.Add("Tech Name(UID)", this.empire.ResearchTopic);

                //}
                //Log.Info(this.EmpireName + " : " + ResourceManager.TechTree[this.empire.ResearchTopic].TechnologyType.ToString() + " : " + this.empire.ResearchTopic);
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