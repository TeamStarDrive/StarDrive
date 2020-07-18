﻿using System;
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
        private readonly Empire Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private float EmpireDefenseBudget => Owner.data.DefenseBudget;
        private readonly Planet Planet;

        public readonly float CivilianBuildings;
        public readonly float Orbitals;
        public readonly float MilitaryBuildings;

        public PlanetBudget(Planet planet)
        {
            if (planet?.Owner == null)
                return;

            SysCom = planet.Owner.GetEmpireAI().
                DefensiveCoordinator.GetSystemCommander(planet.ParentSystem);

            Planet              = planet;
            Owner               = planet.Owner;
            EmpireRatio         = planet.ColonyValue / Owner.TotalColonyValues;
            CivilianBuildings   = EmpireColonizationBudget * EmpireRatio - planet.CivilianBuildingsMaintenance;
            float defenseBudget = EmpireDefenseBudget * EmpireRatio;
            float groundRatio   = MilitaryBuildingsBudgetRatio();
            float orbitalRatio  = 1 - groundRatio;
            MilitaryBuildings   = (defenseBudget * groundRatio - planet.MilitaryBuildingsMaintenance).RoundToFractionOf10();
            Orbitals            = (defenseBudget * orbitalRatio - planet.OrbitalsMaintenance).RoundToFractionOf10();
            CivilianBuildings   = (CivilianBuildings + planet.ColonyDebtTolerance).RoundToFractionOf10();
            Budget              = Orbitals + MilitaryBuildings + CivilianBuildings; // total budget for this planet
            Initialized         = true;
        }

        // This is Orbitals vs. Military Buildings ratio of budget, since Building maintenance is much less than Orbitals.
        float MilitaryBuildingsBudgetRatio() 
        {
            float preference;
            switch (Planet.colonyType)
            {
                case Planet.ColonyType.Military: preference = 0.2f; break;
                case Planet.ColonyType.Core:     preference = 0.15f; break;
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
                              $"\nSystem Rank: {SystemRank}";

            screen.DrawStringProjected(Planet.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
