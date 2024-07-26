using Ship_Game.Gameplay;
using System.IO;
using SDGraphics;
using SDUtils;
using Ship_Game.GameScreens.Espionage;


namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void RunEspionagePlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            DetermineBudget();



        }

        void DetermineBudget()
        {
            float espionageCreditRating = (CreditRating - 0.6f).LowerBound(0); // will get 0-0.4
            float allowedBudget = SpyBudget * espionageCreditRating / 0.4f;
            OwnerEmpire.SetAiEspionageBudgetMultiplier(allowedBudget);
        }
    }
}