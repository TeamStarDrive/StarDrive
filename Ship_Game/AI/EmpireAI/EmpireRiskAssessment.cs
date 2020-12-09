using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public class EmpireRiskAssessment
    {
        public float Expansion   { get; private set; }
        public float Border      { get; private set; }
        public float KnownThreat { get; private set; }

        public float Risk        { get; private set; }
        public float MaxRisk     { get; private set; }
        private readonly Empire Them;
        private readonly Relationship Relation;

        public EmpireRiskAssessment(Relationship relation)
        {
            Them = EmpireManager.GetEmpireByName(relation.Name);
            Relation = relation;
        }

        public void UpdateRiskAssessment(Empire us)
        {
            Expansion   = ExpansionRiskAssessment(us);
            Border      = BorderRiskAssessment(us);
            KnownThreat = RiskAssessment(us);
            Risk        = (Expansion + Border + KnownThreat) / 3;
            MaxRisk     = MathExt.Max3(Expansion, Border, KnownThreat);
        }

        private float ExpansionRiskAssessment(Empire us)
        {
            float expansion = us.GetExpansionRatio(); 
            if (!Relation.Known  || Them == null || Them.data.Defeated)
                return expansion;

            var numberofTheirPlanets = Them.NumPlanets;
            if (Them.isFaction)
                numberofTheirPlanets = us.GetEmpireAI().ThreatMatrix.GetAllSystemsWithFactions().Sum(s => s.PlanetList.Count);

            if (us.NumPlanets > 0 && Them.NumPlanets > 0)
            {
                var planetRatio = Them.NumPlanets / (float)us.NumPlanets;

                expansion *= planetRatio;
                if (planetRatio > us.DifficultyModifiers.ExpansionMultiplier)
                    expansion /= us.DifficultyModifiers.ExpansionMultiplier;
            }

            return expansion;
        }

        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            float ourOffensiveRatio = us.GetWarOffensiveRatio().LowerBound(0.1f);
            float ourTotalSystems = us.NumSystems;
            if (!Relation.Known || Them.data.Defeated || ourTotalSystems < 1 || Them.GetOwnedSystems().Count == 0 || Them == EmpireManager.Unknown)
                return ourOffensiveRatio;

            int ourBorderSystems = us.GetOurBorderSystemsTo(Them, true).Count;
            float space = Empire.Universe.UniverseSize ;
            float distance = (space - us.WeightedCenter.Distance(Them.WeightedCenter)).LowerBound(1);
            distance /= space;

            float borders = (ourBorderSystems / ourTotalSystems);

            return (borders ) * ourOffensiveRatio;
; 
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated || Them == EmpireManager.Unknown)
                return 0;

            var riskBase = us.GetWarOffensiveRatio();
            float risk = 0; 
            float ourStrength = Math.Max(1000, us.CurrentMilitaryStrength);
            if (!Relation.AtWar && !Relation.PreparingForWar && !Them.isFaction)
                return riskBase;

            float theirStrength = us.GetEmpireAI().ThreatMatrix.KnownEmpireStrength(Them, p => p.Ship != null);
            risk = theirStrength  / ourStrength;
            
            risk = risk * riskBase;
            return risk; 
        }

    }
}