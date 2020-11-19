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
            if (!Relation.Known  || Them == null || Them.NumPlanets == 0 || Them.data.Defeated || (!Relation.PreparingForWar && !Relation.AtWar))
                return 0;

            float radius = us.WeightedCenter.Distance(Them.WeightedCenter).UpperBound(Empire.Universe.UniverseSize /4);
            Vector2 dir = us.WeightedCenter.DirectionToTarget(Them.WeightedCenter);
            Vector2 cappedDistance = dir * radius;

            float totalValue = 0;
            float unownedValue = 0;
            float theirPlanetsValue = 0;
            float ourPlanetsValue = 0;
            foreach(var planet in Empire.Universe.PlanetsDict.Values)
            {
                if (planet.Center.OutsideRadius(cappedDistance, radius)) continue;
                unownedValue += planet.Owner == null || planet.Owner == EmpireManager.Unknown ? planet.ColonyWorthTo(us) : 0;
                totalValue += planet.ColonyWorthTo(us);
                theirPlanetsValue += planet.Owner == Them ? planet.ColonyWorthTo(us) : 0;
                ourPlanetsValue += planet.Owner == us ? planet.ColonyWorthTo(us) : 0;
            }
            if (theirPlanetsValue < 1) return us.Research.Strategy.ExpansionRatio;

            float risk = 1 - (unownedValue / totalValue.LowerBound(1));
            return risk * us.Research.Strategy.ExpansionRatio.LowerBound(0.1f);
        }

        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            float ourTotalSystems = us.NumSystems;
            if (!Relation.Known || Them.data.Defeated || ourTotalSystems < 1 || Them.GetOwnedSystems().Count == 0 || Them == EmpireManager.Unknown)
                return 0;

            int ourSystems = us.GetOurBorderSystemsTo(Them, true).Count;
            float space = Empire.Universe.UniverseSize ;
            float distance = (space - us.WeightedCenter.Distance(Them.WeightedCenter)).LowerBound(1);
            distance /= space;

            float borders = (ourSystems / ourTotalSystems);

            return (borders * distance) * us.Research.Strategy.MilitaryRatio.LowerBound(0.1f);
; 
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated || Them == EmpireManager.Unknown)
                return 0;

            float risk = 0; 
            float strength = Math.Max(1000, us.CurrentMilitaryStrength);
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
            return (risk * us.Research.Strategy.ExpansionRatio.LowerBound(0.1f)).Clamped(0,1); 
        }

    }
}