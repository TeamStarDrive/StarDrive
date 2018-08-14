using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.AI.Budget
{
    public class CommonValues
    {
        public static float TradeMoney(int treatyTurns) => (0.25f * treatyTurns - 3f).Clamped(-3f, 3f);
    }

    public struct PlanetBudget
    {
        public readonly float SystemBudget;
        public readonly float Budget;
        public readonly float EmpireRatio;
        public float SystemRank => SysCom?.RankImportance ?? 0;
        private readonly SystemCommander SysCom;
        private Empire Owner                   => Planet.Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private SolarSystem System             => Planet.ParentSystem;
        private readonly Planet Planet;

        public PlanetBudget(Planet planet)
        {            
            Planet       = planet;
            SysCom       = null;
            SystemBudget = 0;
            Budget       = 0;
            EmpireRatio  = 0;

            if (planet == null) return;

            Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(System, out SystemCommander systemCommander);
            SysCom       = systemCommander;

            if (SysCom == null) return;

            float planetRatio = CreatePlanetRatio();
            EmpireRatio  = SysCom.PercentageOfValue * planetRatio;
            Budget       = EmpireColonizationBudget * EmpireRatio;
            SystemBudget = EmpireColonizationBudget * SysCom.PercentageOfValue;            
        }

        private float CreatePlanetRatio()
        {
            float totalValue  = 0;
            float planetValue = 0;
            foreach (var kv in SysCom.PlanetTracker)
            {
                var planetTracker = kv.Value;
                if (planetTracker.Planet.Owner != Owner) continue;
                totalValue += planetTracker.Value;

                if (kv.Key != Planet) continue;
                planetValue = planetTracker.Value;
            }

            return planetValue / totalValue;
        }
        public void DrawBudgetInfo(UniverseScreen screen)
        {
            if (!screen.Debug) return;
            string drawText = $"<\nBudget: {(int)Budget}\nImportance: {EmpireRatio.ToString("#.00")}\nSystemBudget: {(int)SystemBudget}\nSysTem Rank: {SystemRank}";
            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);

        }
    }
}
