using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.AI.Budget
{
    public class PlanetBudget
    {
        //if not initialized then it is not safe to use. 
        public bool Initialized { get; }
        public readonly float Budget;
        public readonly float EmpireRatio;
        public float SystemRank => SysCom?.RankImportance ?? 0;
        public readonly SystemCommander SysCom;
        public readonly PlanetTracker PlanetValues;
        private Empire Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private float EmpireDefenseBudget => Owner.data.DefenseBudget;
        private SolarSystem System;
        private readonly Planet Planet;

        public readonly float Buildings;
        public readonly float Orbitals;
        public readonly float PlanetDefenseBudget;

        public PlanetBudget(Planet planet)
        {
            SysCom = planet?.Owner?.GetEmpireAI().
                DefensiveCoordinator.GetSystemCommander(planet.ParentSystem);
            if (planet != null && SysCom != null)
            {
                Planet              = planet;
                System              = planet.ParentSystem;
                Owner               = planet.Owner;
                PlanetValues        = SysCom.GetPlanetValues(planet);
                EmpireRatio         = SysCom.PercentageOfValue * PlanetValues.RatioInSystem;
                Budget              = EmpireColonizationBudget * EmpireRatio;
                Orbitals            = (EmpireDefenseBudget * EmpireRatio) - planet.OrbitalsMaintenance;
                PlanetDefenseBudget = (EmpireDefenseBudget * EmpireRatio) - planet.MilitaryBuildingsMaintenance;
                Budget             -= planet.ColonyMaintenance;
                if (Budget < 0)
                    Budget = (Budget + planet.ColonyDebtTolerance).Clamped(-float.MaxValue, 0);

                Buildings = Budget;

                Initialized = true;
            }
        }

        public void DrawBudgetInfo(UniverseScreen screen)
        {
            if (!screen.Debug) return;
            string drawText = $"<\nBudget: {Budget.String(2)}" +
                              $"\nImportance: {EmpireRatio.String(2)}" +
                              $"\nColonyBudget: {Budget.String(2)}" +
                              $"\nOrbitals: {Orbitals.String(2)}" +
                              $"\nDefenseBudget: {PlanetDefenseBudget.String(2)}" +
                              $"\nSystem Rank: {SystemRank}" +
                              $"\nIn SysTem Rank: {(int)(PlanetValues.RankInSystem * 10)}" +
                              $"\nValue: {(int)PlanetValues.Value}"; ;


            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
