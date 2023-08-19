using System;
using System.Collections.Generic;
using SDGraphics;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    [StarDataType]
    public class EmpireRiskAssessment
    {
        [StarData] public float Expansion   { get; private set; }
        [StarData] public float Border      { get; private set; }
        [StarData] public float KnownThreat { get; private set; }

        [StarData] public float Risk        { get; private set; }
        [StarData] public float MaxRisk     { get; private set; }
        [StarData] readonly Empire Them;
        [StarData] readonly Relationship Relation;

        [StarDataConstructor] EmpireRiskAssessment() {}

        public EmpireRiskAssessment(Relationship relation)
        {
            Them = relation.Them;
            Relation = relation;
        }

        public void UpdateRiskAssessment(Empire us)
        {
            if (Them == us.Universe.Unknown)
                return;

            Expansion   = ExpansionRiskAssessment(us);
            Border      = BorderRiskAssessment(us);
            KnownThreat = RiskAssessment(us);
            Risk        = (Expansion + Border + KnownThreat) * 0.334f;
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
            if (!Relation.Known || Them == null || Them.IsDefeated)
                return 0;
            if (Relation.Treaty_OpenBorders)
                return 0;

            if (Them.WeArePirates)
                return Them.Pirates.Level * 0.1f;

            if (Them.WeAreRemnants)
                return Them.Remnants.ExpansionRisk;

            float expansion = us.GetExpansionRatio() * 0.25f;
            float expansionRatio = Them.ExpansionScore / us.ExpansionScore.LowerBound(1);
            float risk = expansionRatio.LowerBound(0);
            return (risk + expansion) / 2;
        }

        /// <summary>
        /// For faction we will return 0 threat from borders.
        /// For empires we will find the closest system to any of ours.
        /// use that to determine the border pressure that they are putting on us. 
        /// </summary>
        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known
                || Them.WeArePirates
                || Relation.Treaty_OpenBorders
                || Them.IsDefeated
                || us.NumSystems < 1
                || Them.GetOwnedSystems().Count == 0 || Them == us.Universe.Unknown)
            {
                return 0;
            }

            if (Them.WeAreRemnants)
                return Them.Remnants.BorderRisk(us);

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
            float space = us.Universe.Size * 2;
            float distanceThreat = (space - distanceToNearest) / space;
            float threat = (float)Them.TotalScore / us.TotalScore.LowerBound(1);
            float risk = (threat * distanceThreat - 0.5f).LowerBound(0);

            risk = Math.Max(risk, ourOffensiveRatio);
            bool reduceThreat = Relation.Treaty_NAPact;
            
            risk /= reduceThreat ? 2 : 1;

            return risk;
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.IsDefeated || Relation.Treaty_Alliance)
                return 0;

            if (Them.WeAreRemnants && Them.Remnants.Activated)
                return Them.Remnants.ThreatRisk(us);

            if (Them.IsFaction)
                return 0;

            var riskBase = us.GetWarOffensiveRatio();
            float ourScore = us.TotalScore;
            float theirScore = Them.TotalScore;
            float risk = theirScore / ourScore.LowerBound(1);
            risk = (riskBase + risk) * 0.5f;
            if (!Relation.PreparingForWar && !Relation.AtWar)
            {
                risk = (risk - 0.5f).LowerBound(0);
                risk = (risk - Relation.Trust * 0.01f).LowerBound(0);
                risk *= (!Relation.PreparingForWar && Relation.Treaty_NAPact) ? 0.5f : 1;
            }
            else
            {
                risk = 10;
            }

            return risk; 
        }
    }
}