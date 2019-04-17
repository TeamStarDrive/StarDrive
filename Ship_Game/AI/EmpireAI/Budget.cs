using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.AI.Budget
{
    public struct PlanetBudget
    {
        //if not initialized then it is not safe to use. 
        public bool Initialized { get; }
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
            Initialized = false;
            if (planet == null) return;

            Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(System, out SystemCommander systemCommander);
            SysCom       = systemCommander;

            if (SysCom == null) return;

            float planetRatio = CreatePlanetRatio();
            EmpireRatio  = SysCom.PercentageOfValue * planetRatio;
            Budget       = EmpireColonizationBudget * EmpireRatio;
            SystemBudget = EmpireColonizationBudget * SysCom.PercentageOfValue;
            Initialized = true;
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
            string drawText = $"<\nBudget: {Budget.String(2)}" +
                              $"\nImportance: {EmpireRatio.String(2)}" +
                              $"\nSystemBudget: {SystemBudget.String(2)}" +
                              $"\nSysTem Rank: {SystemRank}";

            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
