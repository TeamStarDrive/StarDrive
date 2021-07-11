using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        private readonly Planet P;

        public readonly float RemainingCivilian;
        public readonly float RemainingSpaceDef;
        public readonly float RemainingGroundDef;

        public readonly float CivilianAlloc;
        public readonly float GrdDefAlloc;
        public readonly float SpcDefAlloc;
        public readonly float TotalAlloc;

        public PlanetBudget(Planet planet)
        {
            if (planet?.Owner == null)
                return;

            P     = planet;
            Owner = P.Owner;

            EmpireRatio  = P.ColonyPotentialValue(Owner) / Owner.TotalColonyPotentialValues;
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
            switch (P.colonyType)
            {
                case Planet.ColonyType.Military: preference = 0.3f;  break;
                case Planet.ColonyType.Core:     preference = 0.2f;  break;
                default:                         preference = 0.15f; break;
            }

            return P.HabitablePercentage * preference;
        }

        float CivBudget()
        {
            var eAI = Owner.GetEmpireAI();

            float wants           = P.BuildingMaintenanceNeeded;
            float aiCivBudget     = EmpireColonizationBudget * EmpireRatio + P.ColonyDebtTolerance;
            float civMaintenance  = P.CivilianBuildingsMaintenance;
            float remainingBudget = aiCivBudget - civMaintenance;
            float empireBudget    = Owner.data.ColonyBudget - Owner.TotalBuildingMaintenance +
                                    eAI.MaintSavedByBuildingScrappedThisTurn;
            bool empireCanSupport = eAI.CreditRating > 0.25f;
            float deficit  = remainingBudget < 0 ? Math.Abs(remainingBudget) : 0;
            float deficitBudget = eAI.BuildingSupportedByEmpireLastTurn + eAI.BuildingSupportedByEmpireThisTurn;

            if (remainingBudget > 0 && wants > 0)
            {
                if (empireBudget > wants && empireCanSupport && empireBudget - deficitBudget > 0) 
                {
                    aiCivBudget += wants;
                    eAI.BuildingSupportedByEmpireThisTurn += wants;
                }
            }
            else if (deficit > 0 && empireBudget > deficit && empireCanSupport)
            {
                aiCivBudget += deficit;
                eAI.BuildingSupportedByEmpireThisTurn += deficit;
            }
            return aiCivBudget;
        }

        public void DrawBudgetInfo(UniverseScreen screen)
        {
            if (!screen.Debug)
                return;

            string drawText = $"<\nTotal Budget: {TotalRemaining.String(2)}" +
                              $"\nImportance: {EmpireRatio.String(2)}" +
                              $"\nCivilianBudget: {RemainingCivilian.String(2)}" +
                              $"\nDefenseBudge (orbitals and ground): {(RemainingSpaceDef + RemainingGroundDef).String(2)}" +
                              $"\nOrbitals: {RemainingSpaceDef.String(2)}" +
                              $"\nMilitaryBuildings: {RemainingGroundDef.String(2)}";

            screen.DrawStringProjected(P.Center + new Vector2(1000, 0), 0f, 1f, Color.LightGray, drawText);
        }
    }
}
