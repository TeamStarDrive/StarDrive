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
            Expansion = ExpansionRiskAssessment(us);
            Border      = BorderRiskAssessment(us);
            KnownThreat = RiskAssessment(us);
            Risk        = (Expansion + Border + KnownThreat) / 3;
            MaxRisk     = MathExt.Max3(Expansion, Border, KnownThreat);
        }

        private float ExpansionRiskAssessment(Empire us)
        {
            if (!Relation.Known  || Them == null || Them.NumPlanets == 0 || Them.data.Defeated || (!Relation.PreparingForWar && !Relation.AtWar))
                return us.Research.Strategy.ExpansionRatio;

            float radius = us.WeightedCenter.Distance(Them.WeightedCenter) / 2;
            Vector2 dir = us.WeightedCenter.DirectionToTarget(Them.WeightedCenter);
            Vector2 halfway = dir * radius;

            float totalValue = 0;
            float unownedValue = 0;

            foreach(var planet in Empire.Universe.PlanetsDict.Values)
            {
                if (planet.Center.OutsideRadius(halfway, radius)) continue;
                unownedValue += planet.Owner == null || planet.Owner == EmpireManager.Unknown ? planet.ColonyWorthTo(us) : 0;
                totalValue += planet.ColonyWorthTo(us);
            }
            float risk = (unownedValue / totalValue.LowerBound(1));

            return risk * us.Research.Strategy.ExpansionRatio.LowerBound(0.1f);
        }

        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated)
                return 0;

            var theirPlanets = Them.GetBorderSystems(us, true);
            int ourPlanets = us.GetBorderSystems(Them, true).Count + 1;
            float distance = 1 - us.WeightedCenter.Distance(Them.WeightedCenter) / (Empire.Universe.UniverseSize * 2);

            float borders = (theirPlanets.Count) / (float)ourPlanets;

            return borders * distance;
; 
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated)
                return 0;

            float risk = 0; 
            float strength = Math.Max(100, us.CurrentMilitaryStrength);
            if (!Relation.AtWar && !Relation.PreparingForWar)
                return 0;

            if (!Them.isFaction)
            {
                risk = us.GetEmpireAI().ThreatMatrix.StrengthOfEmpire(Them) / strength;
                return risk; 
            }

            var checkedSystems = new HashSet<SolarSystem>();

            var claimTasks = us.GetEmpireAI().GetClaimTasks();
            foreach (MilitaryTask task in claimTasks)
            {
                Planet p = task.TargetPlanet;
                SolarSystem ss = p.ParentSystem;
                if (checkedSystems.Add(ss))
                {
                    float test = us.GetEmpireAI().ThreatMatrix.StrengthOfEmpireInSystem(Them, ss);
                    if (test > 0 && test < risk)
                        risk = test;
                }
            }

            risk /= strength;
            return risk; 
        }

    }
}