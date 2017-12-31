using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public class EmpireRiskAssessment
    {
        private readonly float[] Elements;
        public float Expansion { get => Elements[1]; private set => Elements[1] = value; }
        public float Border { get => Elements[2]; private set => Elements[2] = value; }
        public float KnownThreat { get => Elements[3]; private set => Elements[3] = value; }

        public float Risk { get; private set; }
        public float MaxRisk { get; private set; }
        private readonly Empire Them;
        private readonly Relationship Relation;

        public EmpireRiskAssessment(Relationship relation)
        {
            Elements = new float[4];
            Them = EmpireManager.GetEmpireByName(relation.Name);
            Relation = relation;
        }

        public ReadOnlyCollection<float> GetRiskElementsArray()
        {
            return Array.AsReadOnly(Elements);
        }

        public void UpdateRiskAssessment(Empire us)
        {
            Expansion = 0;
            Border = 0;
            KnownThreat = 0;
            Risk = 0;

            Expansion = ExpansionRiskAssement(us);
            Border = BorderRiskAssesment(us);
            KnownThreat = RiskAssesment(us);
            Risk = Elements.Sum();
            MaxRisk = Elements.MultiMax();

        }
        public float ExpansionRiskAssement(Empire us, float riskLimit = .5f)
        {
            if (!Relation.Known  || Them == null || Them.NumPlanets == 0) return 0;
            float themStrength = 0;
            float usStrength = 0;

            foreach (Planet p in Them.GetPlanets())
            {
                if (!p.IsExploredBy(us)) continue;
                themStrength += p.DevelopmentLevel;
            }

            foreach (Planet p in us.GetPlanets())
            {
                usStrength += p.DevelopmentLevel;
            }
            float strength = (themStrength / usStrength) * .25f;
            return strength > riskLimit ? 0 : strength;
        }
        public float BorderRiskAssesment(Empire us, float riskLimit = .5f)
        {
            if (!Relation.Known) return 0;
            float strength = 0;
            foreach (var ss in us.GetBorderSystems(Them))
            {
                strength += us.GetGSAI().ThreatMatrix.StrengthOfEmpireInSystem(Them, ss);
            }
            strength /= Math.Max(us.currentMilitaryStrength, 100);
            return strength > riskLimit ? 0 : strength;
        }
        public float RiskAssesment(Empire us, float riskLimit = 1)
        {
            if (!Relation.Known) return 0;
            float risk = float.MaxValue;
            float strength = Math.Max(100, us.currentMilitaryStrength);
            if (!Them.isFaction && !Relation.AtWar && !Relation.PreparingForWar &&
                !(Relation.TotalAnger > (us.data.DiplomaticPersonality?.Territorialism ?? 50f ))) return 0;
            if (!Them.isFaction)
                return (risk = us.GetGSAI().ThreatMatrix.StrengthOfEmpire(Them) / strength) > riskLimit ? 0 : risk;
            var s = new HashSet<SolarSystem>();
            foreach (var task in us.GetGSAI().TaskList)
            {
                if (task.type != AI.Tasks.MilitaryTask.TaskType.DefendClaim) continue;
                var p = task.GetTargetPlanet();
                var ss = p.ParentSystem;
                if (!s.Add(ss)) continue;
                float test;
                if ((test = us.GetGSAI().ThreatMatrix.StrengthOfEmpireInSystem(Them, ss)) > 0 && test < risk)
                    risk = test;
            }
            risk /= strength;
            return risk > riskLimit ? 0 : risk;
        }

    }
}