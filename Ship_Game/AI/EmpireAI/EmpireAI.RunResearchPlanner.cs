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
        public ChooseTech TechChooser;
        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        private void RunResearchPlanner(string command = "CHEAPEST")
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoResearch)
                return;
            if (OwnerEmpire.ResearchTopic.NotEmpty())
                return;
            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            OwnerEmpire.data.TechDelayTime++;
            var researchPriorities = new ResearchPriorities(OwnerEmpire, BuildCapacity, command, res_strat.ToString());
            TechChooser.InitializeNewResearchRun(researchPriorities);
            DebugLog($"ResearchStrategy : {res_strat.ToString()}");

            switch (res_strat)
            {
                case ResearchStrategy.Random:
                    {
                        if (TechChooser.ScriptedResearch(command, "RANDOM", researchPriorities.TechCategoryPrioritized));

                        return;
                    }
                case ResearchStrategy.Scripted:
                    {

                        if (TechChooser.ProcessScript()) return;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        public enum ResearchStrategy
        {
            Random,
            Scripted
        }
    }
}