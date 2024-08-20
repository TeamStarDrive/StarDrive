using Ship_Game.Gameplay;
using System.IO;
using SDGraphics;
using SDUtils;
using Ship_Game.GameScreens.Espionage;
using Ship_Game.Data.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System;


namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class EspionageManager
    {
        const int EspionageDefaultTimer = 10;
        [StarData] int EspionageUpdateTimer;
        [StarData] readonly Empire Owner;

        public EspionageManager(Empire e)
        {
            Owner = e;
        }

        public void Update(bool forceRun = false)
        {
            if (Owner.IsFaction || Owner.data.IsRebelFaction)
                return;

            DetermineBudget();
            SetupDefenseWeight();
            SetupWeightsAndOps(forceRun);
        }

        [StarDataConstructor]
        EspionageManager() { }

        public void InitEspionageManager(int id)
        {
            EspionageUpdateTimer = EspionageDefaultTimer + id; // for loadbalancing the updates per empire
        }

        void DetermineBudget()
        {
            float espionageCreditRating = (Owner.AI.CreditRating - 0.6f).Clamped(0, 0.4f);
            float allowedBudget = Owner.AI.SpyBudget * espionageCreditRating / 0.4f;
            Owner.SetAiEspionageBudgetMultiplier(allowedBudget);
        }

        void SetupDefenseWeight()
        {
            int numWars = Owner.AtWarCount;
            int numAllies = Owner.Universe.ActiveMajorEmpires.Filter(e => e != Owner && e.IsAlliedWith(Owner)).Length;
            int total = numWars + numAllies;
            int weight = (total * 10).Clamped(10, Empire.MaxEspionageDefenseWeight);
            Owner.SetEspionageDefenseWeight(weight);    
        }

        void SetupInfiltrationWeights(Relationship relations, Espionage espionage, Empire them)
        {
            if (relations.AtWar || relations.PreparingForWar) espionage.SetWeight(10);
            else if (relations.Treaty_Alliance)               espionage.SetWeight(3);
            else if (relations.TotalAnger > 50)               espionage.SetWeight(7);
            else                                              espionage.SetWeight(5);
        }

        void SetupWeightsAndOps(bool ignoreTimer)
        {
            if (!ignoreTimer && --EspionageUpdateTimer > 0)
                return;

            if (!ignoreTimer)
                EspionageUpdateTimer = EspionageDefaultTimer;

            foreach (Empire empire in Owner.Universe.ActiveMajorEmpires.Filter(e => e != Owner)) 
            {
                if (GlobalStats.RestrictAIPlayerInteraction && empire.isPlayer)
                    continue;

                Relationship relations = Owner.GetRelations(empire);
                Espionage espionage = relations.Espionage;
                if (relations.Known)
                {
                    SetupInfiltrationWeights(relations, espionage, empire);
                    if (espionage.Level > 0)
                    {
                        SetEspionageLimitLevel(relations, espionage);
                        EnableDisableEspionageOperations(relations, espionage, empire);
                    }
                }
            }
        }

        void SetEspionageLimitLevel(Relationship relations, Espionage espionage)
        {
            byte limitLevel = Espionage.MaxLevel;
            switch (Owner.Personality)
            {
                case PersonalityType.Aggressive:
                    if ((relations.Treaty_Alliance || relations.Treaty_OpenBorders) && !relations.PreparingForWar)
                        limitLevel = 4;
                    else
                        limitLevel = Espionage.MaxLevel;
                    break;
                case PersonalityType.Ruthless:
                case PersonalityType.Cunning:
                    limitLevel = (byte)(relations.Treaty_Alliance && !relations.PreparingForWar ? 4 : Espionage.MaxLevel);
                    break;
                case PersonalityType.Xenophobic:
                    if (espionage.Level < Espionage.MaxLevel)
                        break;

                    limitLevel = (byte)(relations.Treaty_Alliance && !relations.PreparingForWar ? 3 : Espionage.MaxLevel);
                    break;
                case PersonalityType.Honorable:
                    limitLevel = (byte)(relations.Treaty_Alliance && !relations.PreparingForWar ? 3 : Espionage.MaxLevel);
                    break;
                case PersonalityType.Pacifist:
                    if (espionage.Level < Espionage.MaxLevel-1)
                        break;

                    limitLevel = (byte)(!relations.AtWar && !relations.PreparingForWar ? 3 : Espionage.MaxLevel);
                    break;
            }

            espionage.SetLimitLevel(limitLevel);

        }

        void EnableDisableEspionageOperations(Relationship relations, Espionage espionage, Empire them)
        {
            Map<InfiltrationOpsType, bool> operations = InitOperationsWanted();
            UpdateOperationsByPersonality(operations, relations, espionage, them);
            foreach (var type in operations.Keys)
            {
                if (operations[type] == true)
                    espionage.ActivateOpsIfAble(type);
                else
                    espionage.RemoveOperation(type);
            }
        }

        Map<InfiltrationOpsType, bool> InitOperationsWanted()
        {
            Map<InfiltrationOpsType, bool> operations = new();
            foreach (InfiltrationOpsType type in (InfiltrationOpsType[])Enum.GetValues(typeof(InfiltrationOpsType)))
                operations[type] = false;

            return operations;
        }

        void UpdateOperationsByPersonality(Map<InfiltrationOpsType, bool> operations, Relationship relations, Espionage espionage, Empire them)
        {
            bool theyHaveInfiltratedUs = espionage.WeHaveInfoOnTheirInfiltration 
                && them.GetEspionage(Owner).EffectiveLevel > (relations.Treaty_Alliance ? 3 : 2);

            Array<InfiltrationOpsType> opsWanted = new();
            if (theyHaveInfiltratedUs)
                opsWanted.Add(InfiltrationOpsType.CounterEspionage);

            if (!espionage.MoleCoverageReached)
                opsWanted.Add(InfiltrationOpsType.PlantMole);

            bool allowMoreOps = Owner.Universe.P.Difficulty > GameDifficulty.Normal;
            if (relations.AtWar || relations.PreparingForWar && allowMoreOps)
            {
                switch (Owner.Personality)
                {
                    default:
                    case PersonalityType.Aggressive:
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        break;
                    case PersonalityType.Ruthless:
                        opsWanted.Add(InfiltrationOpsType.Rebellion);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    case PersonalityType.Xenophobic:
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        break;
                    case PersonalityType.Honorable:
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    case PersonalityType.Pacifist:
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    case PersonalityType.Cunning:
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        break;
                }
            }

            if (!relations.Treaty_Alliance || Owner.IsSafeToActivateOpsOnAllies(them) && allowMoreOps)
            {
                int allyMultiplier = relations.Treaty_Alliance ? 2 : 1;
                if (Owner.TechScore * Owner.PersonalityModifiers.EspionageTechScoreOpsMultiplier * allyMultiplier  < them.TechScore)
                    opsWanted.AddUnique(InfiltrationOpsType.SlowResearch);

                if (Owner.ExpansionScore * Owner.PersonalityModifiers.EspionageExpansionScoreOpsMultiplier  * allyMultiplier < them.ExpansionScore)
                    opsWanted.AddUnique(InfiltrationOpsType.Uprise);

                if (Owner.IndustrialScore * Owner.PersonalityModifiers.EspionageIndustryScoreOpsMultiplier * allyMultiplier < them.IndustrialScore)
                    opsWanted.AddUnique(InfiltrationOpsType.Sabotage);

                if (Owner.MilitaryScore * Owner.PersonalityModifiers.EspionageMilitaryScoreOpsMultiplier * allyMultiplier < them.MilitaryScore)
                    opsWanted.AddUnique(InfiltrationOpsType.Rebellion);

                if (Owner.TotalScore * Owner.PersonalityModifiers.EspionageTotalScoreOpsMultiplier * allyMultiplier < them.TotalScore)
                    opsWanted.AddUnique(InfiltrationOpsType.DisruptProjection);
            }

            foreach (InfiltrationOpsType type in opsWanted)
                operations[type] = true;
        }
    }
}