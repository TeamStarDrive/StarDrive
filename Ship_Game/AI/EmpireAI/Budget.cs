using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.AI.Budget
{
    public struct PlanetBudget
    {
        //if not initialized then it is not safe to use. 
        public bool Initialized { get; }
        public readonly float PlanetDefenseBudget;
        public readonly float Budget;
        public readonly float EmpireRatio;
        public float SystemRank => SysCom?.RankImportance ?? 0;
        private readonly SystemCommander SysCom;
        private Empire Owner                   => Planet.Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private float EmpireDefenseBudget => Owner.data.DefenseBudget;
        private SolarSystem System             => Planet.ParentSystem;
        private readonly Planet Planet;

        public float Buildings;
        public float Orbitals;

        public PlanetBudget(Planet planet)
        {            
            Planet              = planet;
            SysCom              = null;
            PlanetDefenseBudget = 0;
            Budget              = 0;
            EmpireRatio         = 0;
            Initialized         = false;
            Buildings           = Budget;
            Orbitals            = PlanetDefenseBudget;

            if (planet == null) return;

            Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(System, out SystemCommander systemCommander);
            SysCom       = systemCommander;

            if (SysCom == null) return;

            float planetRatio    = CreatePlanetRatio();
            EmpireRatio          = SysCom.PercentageOfValue * planetRatio;
            Budget               = EmpireColonizationBudget * EmpireRatio;
            Orbitals = PlanetDefenseBudget  = EmpireDefenseBudget * EmpireRatio;
            Budget -= planet.ColonyMaintenance;
            if (Budget < 0)
                Budget = (Budget + planet.ColonyDebtTolerance).Clamped(-float.MaxValue, 0);
            Buildings = Budget;
            Initialized          = true;

        }
        private void ColonyBudget(Planet.ColonyType colonyType, Empire owner, bool govOrbitals)
        {
            float buildingsBudget;
            float totalBudget = Budget;
            if (colonyType == Planet.ColonyType.Colony || owner.isPlayer && !govOrbitals)
                buildingsBudget = totalBudget; // Governor does not manage orbitals
            else
            {
                switch (colonyType)
                {
                    case Planet.ColonyType.Industrial:
                    case Planet.ColonyType.Agricultural: buildingsBudget = totalBudget * 0.8f; break;
                    case Planet.ColonyType.Military: buildingsBudget = totalBudget * 0.6f; break;
                    case Planet.ColonyType.Research: buildingsBudget = totalBudget * 0.9f; break;
                    default: buildingsBudget = totalBudget * 0.75f; break;
                }
            }

            Buildings = (float)Math.Round(buildingsBudget, 2);
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
                              $"\nSystemBudget: {PlanetDefenseBudget.String(2)}" +
                              $"\nSysTem Rank: {SystemRank}";

            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
