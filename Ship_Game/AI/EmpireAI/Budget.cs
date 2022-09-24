using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.Budget
{
    public class PlanetBudget
    {
        // if not initialized then it is not safe to use. 
        public bool Initialized { get; }
        public readonly float TotalRemaining;
        public readonly float EmpireRatio;
        public readonly float DefenseRatio;
        private readonly Empire Owner;
        private float EmpireColonizationBudget => Owner.data.ColonyBudget;
        private float EmpireDefenseBudget => Owner.data.DefenseBudget;
        public readonly Planet P;

        public readonly float RemainingCivilian;
        public readonly float RemainingSpaceDef;
        public readonly float RemainingGroundDef;

        public readonly float CivilianAlloc;
        public readonly float GrdDefAlloc;
        public readonly float SpcDefAlloc;
        public readonly float TotalAlloc;
        public readonly bool AboveAverage;

        public PlanetBudget(Planet planet)
        {
            if (planet?.Owner == null)
                return;

            P     = planet;
            Owner = P.Owner;

            float avgColonyValue = Owner.TotalColonyPotentialValues / Owner.GetPlanets().Count;
            var avgPlanets       = Owner.GetPlanets().Filter(p => p.ColonyPotentialValue(Owner) > avgColonyValue);
            float totalAvgValue  = avgPlanets.Sum(p => p.ColonyPotentialValue(Owner));
            float potentialValue = P.ColonyPotentialValue(Owner);
            AboveAverage         = potentialValue >= avgColonyValue;
            if (AboveAverage)
            {
                EmpireRatio = potentialValue / totalAvgValue;
            }
            else
            {
                EmpireRatio  = P.ColonyPotentialValue(Owner) / Owner.TotalColonyPotentialValues;
            }

            DefenseRatio = P.ColonyBaseValue(Owner) / Owner.TotalColonyValues;

            float defenseBudget = EmpireDefenseBudget * DefenseRatio;
            float groundRatio   = MilitaryBuildingsBudgetRatio();
            float orbitalRatio  = 1 - groundRatio;
            float aiCivBudget   = CivBudget();


            GrdDefAlloc   = P.ManualGrdDefBudget   <= 0 ? defenseBudget * groundRatio : P.ManualGrdDefBudget;
            SpcDefAlloc   = P.ManualSpcDefBudget   <= 0 ? defenseBudget * orbitalRatio : P.ManualSpcDefBudget;
            CivilianAlloc = P.ManualCivilianBudget <= 0 ? aiCivBudget : P.ManualCivilianBudget;

            RemainingGroundDef = (GrdDefAlloc - P.GroundDefMaintenance).RoundToFractionOf10();
            RemainingSpaceDef  = (SpcDefAlloc - P.SpaceDefMaintenance).RoundToFractionOf10();
            RemainingCivilian  = (CivilianAlloc - P.CivilianBuildingsMaintenance).RoundToFractionOf10();

            TotalRemaining = RemainingSpaceDef + RemainingGroundDef + RemainingCivilian; // total remaining budget for this planet
            TotalAlloc     = GrdDefAlloc + SpcDefAlloc + CivilianAlloc;

            Initialized = true;
        }

        /// <summary>
        /// This is Orbitals vs. Military Buildings ratio of budget, since Building maintenance is much less than Orbitals.
        /// </summary>
        float MilitaryBuildingsBudgetRatio()
        {
            float preference;
            switch (P.CType)
            {
                case Planet.ColonyType.Military: preference = 0.3f;  break;
                case Planet.ColonyType.Core:     preference = 0.2f;  break;
                default:                         preference = 0.15f; break;
            }

            return P.HabitablePercentage * preference;
        }

        float CivBudget()
        {
            float aiCivBudget = EmpireColonizationBudget * EmpireRatio + P.ColonyDebtTolerance;
            if (!AboveAverage)
                aiCivBudget = (float)(Math.Round(aiCivBudget * 2) / 2);
            return aiCivBudget;
        }

        public void DrawBudgetInfo(UniverseScreen screen)
        {
            string drawText = $"<\nTotal Budget: {TotalRemaining.String(2)}" +
                              $"\nImportance: {EmpireRatio.String(2)}" +
                              $"\nCivilianBudget: {RemainingCivilian.String(2)}" +
                              $"\nDefenseBudge (orbitals and ground): {(RemainingSpaceDef + RemainingGroundDef).String(2)}" +
                              $"\nOrbitals: {RemainingSpaceDef.String(2)}" +
                              $"\nMilitaryBuildings: {RemainingGroundDef.String(2)}";

            screen.DrawStringProjected(P.Position + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
