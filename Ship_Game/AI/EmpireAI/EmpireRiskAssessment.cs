using System;
using System.Collections.Generic;
using SDGraphics;
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

            if (float.IsNaN(Risk))
                throw new Exception("Risk cannot be NaN!");
        }

        /// <summary>
        /// figure the expansion risk created by target empire.
        /// for factions we are just going to look at raw blocked colony goals.
        /// for others we are going to compare expansion scores. 
        /// </summary>
        private float ExpansionRiskAssessment(Empire us)
        {
            if (!Relation.Known  || Them == null || Them.data.Defeated)
                return 0;
            if (Relation.Treaty_OpenBorders)
                return 0;

            float expansion = us.GetExpansionRatio() / 4;

            float risk = 0;
            if (Them.IsFaction || us.IsFaction)
            {
                if (us.GetEmpireAI().CreditRating > 0.75f && (Relation.AtWar || Relation.IsHostile))
                {
                    float strengthNeeded = us.GetEmpireAI().GetAvgStrengthNeededByExpansionTasks(Them);

                    if (strengthNeeded > 0)
                    {
                        float currentStrength = us.AIManagedShips.EmpireReadyFleets?.AccumulatedStrength ?? 1;
                        float currentThreat = us.GetEmpireAI().ThreatLevel;
                        float possibleStrength = currentStrength / currentThreat;
                        if (possibleStrength > strengthNeeded)
                            return 10;
                    }
                }
                return 0;
            }

            float expansionRatio = Them.ExpansionScore / us.ExpansionScore.LowerBound(1);
            risk = (expansionRatio).LowerBound(0);

            return (risk + expansion) / 2;
        }

        /// <summary>
        /// For faction we will return 0 threat from borders.
        /// For empires we will find the closest system to any of ours.
        /// use that to determine the border pressure that they are putting on us. 
        /// </summary>
        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated || us.NumSystems < 1 || Them.GetOwnedSystems().Count == 0 || Them == EmpireManager.Unknown)
                return 0;

            // if we have an open borders treaty or they are a faction return 0
            if (Relation.Treaty_OpenBorders || Them.IsFaction)
                return 0;

            float ourOffensiveRatio = us.GetWarOffensiveRatio();
            float distanceToNearest = float.MaxValue;
            foreach (var system in us.GetOwnedSystems())
            {
                foreach(var theirSystem in Them.GetOwnedSystems())
                {
                    float distance = system.Position.Distance(theirSystem.Position);
                    distanceToNearest = Math.Min(distanceToNearest, distance);
                    if (distanceToNearest == 0) break;
                }
                if (distanceToNearest == 0) break;
            }

            // size is only the positive half of the universe. so double it.
            float space = us.Universum.Size * 2;
            float distanceThreat = (space - distanceToNearest) / space;
            float threat = (float)Them.TotalScore / us.TotalScore;
            float risk = (threat * distanceThreat - 0.5f).LowerBound(0);

            risk = Math.Max(risk, ourOffensiveRatio);
            bool reduceThreat = Relation.Treaty_NAPact;
            
            risk /= reduceThreat ? 2 : 1;

            return risk;
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated || Them == EmpireManager.Unknown)
                return 0;
            if (Them.IsFaction || Relation.Treaty_Alliance)
                return 0;

            var riskBase = us.GetWarOffensiveRatio();
            float ourScore = us.TotalScore;
            float theirScore = Them.TotalScore;
            float risk = 0;
            if (ourScore != 0f) // avoid Div by zero
                risk = theirScore / ourScore;
            risk = (riskBase + risk) / 2;
            if (!Relation.PreparingForWar && !Relation.AtWar)
            {
                risk = (risk - 0.5f).LowerBound(0);
                risk = (risk - Relation.Trust * 0.01f).LowerBound(0);
                risk /= (!Relation.PreparingForWar && Relation.Treaty_NAPact) ? 2 : 1;
            }
            else
                risk = 10;

            return risk; 
        }
    }
}