using Ship_Game.AI.Research;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private int ScriptIndex;
        public ChooseTech TechChooser;
        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        private void RunResearchPlanner(string command = "CHEAPEST")
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoResearch)
                return;
            if (OwnerEmpire.Research.HasTopic)
                return;

            Empire.Universe?.DebugWin?.ClearResearchLog(OwnerEmpire);
            OwnerEmpire.data.TechDelayTime++;
            var researchPriorities = new ResearchPriorities(OwnerEmpire, BuildCapacity);

            TechChooser.InitializeNewResearchRun(researchPriorities);
            DebugLog($"ResearchStrategy : {TechChooser.ScriptType.ToString()}");

            switch (TechChooser.ScriptType)
            {
                case ResearchStrategy.Random:
                    {
                        TechChooser.ScriptedResearch(command, "RANDOM", researchPriorities.TechCategoryPrioritized);
                        break;
                    }
                case ResearchStrategy.Scripted:
                    {
                        TechChooser.ProcessScript();
                        break;
                    }
                default:
                    return;
            }
        }

        public enum ResearchStrategy
        {
            Random,
            Scripted
        }
    }
}