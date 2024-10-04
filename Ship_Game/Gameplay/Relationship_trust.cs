using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Empires.Components;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;

namespace Ship_Game.Gameplay
{
    public partial class Relationship
    {
        void UpdateTrust(Empire us, Empire them, EconomicPersonalityType eType)
            {
            foreach (TrustEntry te in TrustEntries)
                te.TurnsInExistence += 1;

            TrustUsed = TrustEntries.Sum(t => t.TurnsInExistence < t.TurnTimer ? t.TrustCost : 0);
            TrustEntries.RemoveAll(t => t.TurnsInExistence >= t.TurnTimer);

            if (AtWar || !Known)
                return;

            float trustToAdd = GetTrustGain(us, them, eType);
            Trust += trustToAdd * TrustMultiplier();
            Trust = Trust.Clamped(-50, Treaty_Alliance ? 150 : 100);
            TurnsAbove95 = Trust > 95 ? TurnsAbove95 + 1 : 0;


            float TrustMultiplier() // Based on number of planet they stole from us and pace
            {
                float trustMultiplier = 1 / us.Universe.P.Pace;
                if (NumberStolenClaims == 0 || !them.isPlayer) // AI has their internal trust gain
                    return trustMultiplier;

                return us.PersonalityModifiers.PlanetStoleTrustMultiplier / NumberStolenClaims * trustMultiplier;
            }
        }

        float GetTrustGain(Empire us, Empire them, EconomicPersonalityType eType)
        {
            float baseGain = 0.0125f;
            float trust = TotalAnger.LowerBound(0) * -0.1f * baseGain;
            switch (us.Personality)
            {
                case PersonalityType.Pacifist:   trust = PacifistTrustGain(baseGain, us, them, eType);   break;
                case PersonalityType.Honorable:  trust = HonorableTrustGain(baseGain, us, them, eType);  break;
                case PersonalityType.Cunning:    trust = CunningTrustGain(baseGain, us, them, eType);    break;
                case PersonalityType.Aggressive: trust = AggressiveTrustGain(baseGain, us, them, eType); break;
                case PersonalityType.Xenophobic: trust = XenophobicTrustGain(baseGain, us, them, eType); break;
                case PersonalityType.Ruthless:   trust = RuthlessTrustGain(baseGain, us, them, eType);   break;
            }

            trust += BaseEtraitTrustGain(baseGain, us, them, eType);

            if (them.Personality == us.Personality)
                trust += baseGain*2;
            if (!them.isPlayer && them.data.EconomicPersonality.EconomicPersonality() == eType)
                trust += baseGain*2;

            float trustDifficulty = them.isPlayer ? (-baseGain) * ((int)us.Universe.P.Difficulty) : 0;

            return trust - trustDifficulty;
        }

        float BaseEtraitTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;
            switch (eType)
            {
                case EconomicPersonalityType.Expansionists:
                    if (us.ExpansionScore * us.PersonalityModifiers.TrustChangeThreshold < them.ExpansionScore)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Industrialists:
                    if (us.IndustrialScore * us.PersonalityModifiers.TrustChangeThreshold < them.IndustrialScore)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Technologists:
                    if (us.TechScore * us.PersonalityModifiers.TrustChangeThreshold < them.TechScore)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Militarists:
                    if (us.MilitaryScore * us.PersonalityModifiers.TrustChangeThreshold < them.MilitaryScore)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Generalists:
                    if (us.TotalScore * us.PersonalityModifiers.TrustChangeThreshold < them.TotalScore)
                        gain -= baseGain;

                    break;
            }

            return gain;
        }

        float PacifistTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;

            if (Treaty_Trade)       gain += baseGain;
            if (Treaty_NAPact)      gain += baseGain;
            if (Treaty_OpenBorders) gain += baseGain;
            if (Treaty_Alliance)    gain += baseGain;
            if (them.IsAggressive)  gain -= baseGain;

            if (!them.IsAtWarWithMajorEmpire && !Treaty_Alliance) 
                gain -= baseGain * 2;

            if (!them.IsAtWarWithMajorEmpire)
                gain += baseGain;

            if (them.IsHonorable || them.IsCunning)
                gain += baseGain;

            gain -= baseGain * WarHistory.Count;
            
            return gain;
        }

        float HonorableTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;
            switch (eType)
            {
                case EconomicPersonalityType.Expansionists:
                    if (ValueWithinRange(us.ExpansionScore, them.ExpansionScore, 0.1f))
                        gain += baseGain;

                    break;
                case EconomicPersonalityType.Industrialists:
                    if (ValueWithinRange(us.IndustrialScore, them.IndustrialScore, 0.1f))
                        gain += baseGain;

                    break;
                case EconomicPersonalityType.Technologists:
                    if (ValueWithinRange(us.TechScore, them.TechScore, 0.1f))
                        gain += baseGain;

                    break;
                case EconomicPersonalityType.Militarists:
                    if (ValueWithinRange(us.MilitaryScore, them.MilitaryScore, 0.1f))
                        gain += baseGain;

                    break;
                case EconomicPersonalityType.Generalists:
                    if (ValueWithinRange(us.TotalScore, them.TotalScore, 0.2f))
                        gain += baseGain;

                    break;
            }

            if (Treaty_NAPact)   gain += baseGain;
            if (Treaty_Alliance) gain += baseGain;
            if (them.IsRuthless) gain -= baseGain;

            if (them.IsCunning || them.IsPacifist) 
                gain += baseGain;

            gain -= baseGain * SpiesKilled.UpperBound(3);

            return gain;
        }

        float CunningTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;

            if (Treaty_NAPact)      gain += baseGain;
            if (Treaty_Trade)       gain += baseGain;
            if (Treaty_OpenBorders) gain += baseGain;
            if (them.IsXenophobic)  gain -= baseGain;
            if (SpiesDetected == 0) gain -= baseGain;

            if (us.NewEspionageEnabled && Espionage.Level > 3 && them.GetEspionage(us).LimitLevel < 3)
                gain -= baseGain;

            if (them.IsHonorable || them.IsPacifist)
                gain += baseGain;

            return gain;
        }

        float AggressiveTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;
            switch (eType)
            {
                case EconomicPersonalityType.Expansionists:
                    if (us.ExpansionScore > them.ExpansionScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    if (Treaty_Trade)
                        gain += baseGain;

                    break;
                case EconomicPersonalityType.Industrialists:
                    if (us.IndustrialScore > them.IndustrialScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Technologists:
                    if (us.TechScore > them.TechScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Militarists:
                    if (us.MilitaryScore > them.MilitaryScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Generalists:
                    if (us.TotalScore > them.TotalScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
            }

            if (Treaty_NAPact)   gain += baseGain*0.5f;
            if (Treaty_Trade)    gain += baseGain*0.5f;
            if (Treaty_Alliance) gain += baseGain*0.5f;
            if (them.IsPacifist) gain -= baseGain;

            if (us.NewEspionageEnabled && Espionage.Level > 2 && them.OffensiveStrength < 100)
                gain -= baseGain;

            if (them.IsRuthless || them.IsXenophobic)
                gain += baseGain;

            return gain;
        }

        float XenophobicTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;

            if (Treaty_NAPact)      gain += baseGain*0.5f;
            if (Treaty_Trade)       gain += baseGain*0.5f;
            if (Treaty_OpenBorders) gain += baseGain*0.5f;
            if (Treaty_Alliance)    gain += baseGain;
            if (them.IsCunning)     gain -= baseGain;

            if (us.NewEspionageEnabled && Espionage.Level > 3 && them.GetEspionage(us).LimitLevel > 3)
                gain -= baseGain;

            if (them.IsXenophobic)
                gain += baseGain*2;

            return gain;
        }

        float RuthlessTrustGain(float baseGain, Empire us, Empire them, EconomicPersonalityType eType)
        {
            float gain = 0;
            switch (eType)
            {
                case EconomicPersonalityType.Expansionists:
                    if (them.GetPlanets().Count > us.GetPlanets().Count)
                        gain -= baseGain*0.5f;

                    break;
                case EconomicPersonalityType.Industrialists:
                    if (us.IndustrialScore > them.IndustrialScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Technologists:
                    if (us.CanBuildBombers && !them.CanBuildBombers)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Militarists:
                    if (us.CanBuildCruisers && !them.CanBuildCruisers)
                        gain -= baseGain;

                    break;
                case EconomicPersonalityType.Generalists:
                    if (us.TotalScore > them.TotalScore * us.PersonalityModifiers.TrustChangeThreshold)
                        gain -= baseGain;

                    break;
            }

            if (Treaty_NAPact)    gain += baseGain * 0.5f;
            if (Treaty_Alliance)  gain += baseGain * 0.5f;
            if (them.IsHonorable) gain -= baseGain;

            if (us.NewEspionageEnabled && Espionage.Level > 2 && them.OffensiveStrength < 100)
                gain -= baseGain;

            if (them.IsAggressive)
                gain += baseGain;

            return gain;
        }

        bool ValueWithinRange(float value, float valueToCompare, float range)
        {
            return valueToCompare.InRange(value * (1-range), value * (1+range));
        }
    }
}