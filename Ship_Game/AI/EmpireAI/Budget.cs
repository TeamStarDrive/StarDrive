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
        private readonly Empire Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private float EmpireDefenseBudget => Owner.data.DefenseBudget;
        private SolarSystem System;
        private readonly Planet Planet;

        public readonly float CivilianBuildings;
        public readonly float Orbitals;
        public readonly float MilitaryBuildings;

        public PlanetBudget(Planet planet)
        {
            SysCom = planet?.Owner?.GetEmpireAI().
                DefensiveCoordinator.GetSystemCommander(planet.ParentSystem);

            if (planet == null || SysCom == null) 
                return;

            Planet              = planet;
            System              = planet.ParentSystem;
            Owner               = planet.Owner;
            PlanetValues        = SysCom.GetPlanetValues(planet);
            EmpireRatio         = SysCom.PercentageOfValue * PlanetValues.RatioInSystem;
            CivilianBuildings   = EmpireColonizationBudget * EmpireRatio - planet.CivilianBuildingsMaintenance;
            float defenseBudget = EmpireDefenseBudget * EmpireRatio;
            float groundRatio   = MilitaryBuildingsBudgetRatio();
            float orbitalRatio  = 1 - groundRatio;
            MilitaryBuildings   = defenseBudget * groundRatio - planet.MilitaryBuildingsMaintenance;
            Orbitals            = defenseBudget * orbitalRatio - planet.OrbitalsMaintenance;

            if (CivilianBuildings < 0)
                CivilianBuildings = (CivilianBuildings + planet.ColonyDebtTolerance).Clamped(-float.MaxValue, 0);

            Budget      = Orbitals + MilitaryBuildings + CivilianBuildings; // total budget for this planet
            Initialized = true;
        }

        // The more habitable tiles the planet has, more budget is allocated to military buildings
        float MilitaryBuildingsBudgetRatio() 
        {
            float preference;
            switch (Planet.colonyType)
            {
                case Planet.ColonyType.Military: preference = 0.3f; break;
                case Planet.ColonyType.Core:     preference = 0.25f; break;
                default:                         preference = 0.1f; break;
            }

            return Planet.HabitablePercentage * preference;
        }

        public void DrawBudgetInfo(UniverseScreen screen)
        {
            if (!screen.Debug) 
                return;

            string drawText = $"<\nTotal Budget: {Budget.String(2)}" +
                              $"\nImportance: {EmpireRatio.String(2)}" +
                              $"\nCivilianBudget: {CivilianBuildings.String(2)}" +
                              $"\nDefenseBudge (orbitals and ground): {(Orbitals + MilitaryBuildings).String(2)}" +
                              $"\nOrbitals: {Orbitals.String(2)}" +
                              $"\nMilitaryBuildings: {MilitaryBuildings.String(2)}" +
                              $"\nSystem Rank: {SystemRank}" +
                              $"\nIn SysTem Rank: {(int)(PlanetValues.RankInSystem * 10)}" +
                              $"\nValue: {(int)PlanetValues.Value}"; ;

            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
