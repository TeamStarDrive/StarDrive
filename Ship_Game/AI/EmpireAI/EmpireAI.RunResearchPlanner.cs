using Ship_Game.AI.Research;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public ChooseTech TechChooser;
        private void DebugLog(string text) => OwnerEmpire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        private void RunResearchPlanner(string command = "CHEAPEST")
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoResearch)
                return;
            if (OwnerEmpire.Research.HasTopic)
                return;

            TechChooser.PickResearchTopic(command);
        }
    }
}