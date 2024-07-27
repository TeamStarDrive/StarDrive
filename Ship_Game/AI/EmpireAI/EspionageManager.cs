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
            int total = numWars = numAllies;
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
                Relationship relations = Owner.GetRelations(empire);
                Espionage espionage = relations.Espionage;
                // implement setupweights here
                if (relations.Known && espionage.Level > 0)
                {
                    SetupInfiltrationWeights(relations, espionage, empire);
                    SetEspionageLimitLevel(relations, espionage);
                    EnableDisableEspionageOperations(relations, espionage, empire);
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
                    limitLevel = Espionage.MaxLevel;
                    break;
                case PersonalityType.Xenophobic:
                case PersonalityType.Honorable:
                    limitLevel = (byte)(relations.Treaty_Alliance && !relations.PreparingForWar ? 3 : Espionage.MaxLevel);
                    break;
                case PersonalityType.Pacifist:
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
            bool moleCoverageReached = !espionage.MoleCoverageReached;
            bool theyHaveInfiltratedUs = espionage.WeHaveInfoOnTheirInfiltration && them.GetEspionage(Owner).EffectiveLevel > 0;
            Array<InfiltrationOpsType> opsWanted = new();
            if (theyHaveInfiltratedUs)
                opsWanted.Add(InfiltrationOpsType.CounterEspionage);

            if (!moleCoverageReached)
                opsWanted.Add(InfiltrationOpsType.PlantMole);

            switch (Owner.Personality)
            {
                default:
                case PersonalityType.Aggressive:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        break;
                    }

                    if (!relations.Treaty_Alliance && relations.TotalAnger > 50)
                    {
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        opsWanted.Add(espionage.CanActivateOperation(InfiltrationOpsType.Rebellion) 
                                                ? InfiltrationOpsType.Rebellion 
                                                : InfiltrationOpsType.Uprise);
                    }

                    break;
                case PersonalityType.Ruthless:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.Rebellion);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    }

                    if (!relations.Treaty_Alliance && relations.TotalAnger > 50)
                    {
                        opsWanted.Add(InfiltrationOpsType.Rebellion);
                        opsWanted.Add(espionage.CanActivateOperation(InfiltrationOpsType.DisruptProjection)
                                                ? InfiltrationOpsType.DisruptProjection
                                                : InfiltrationOpsType.Uprise);
                    }

                    break;
                case PersonalityType.Xenophobic:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        break;
                    }

                    if (relations.TotalAnger > 25 || relations.GetStrength() < 20)
                    {
                        opsWanted.Add(InfiltrationOpsType.Rebellion);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        opsWanted.Add(espionage.CanActivateOperation(InfiltrationOpsType.DisruptProjection)
                                                ? InfiltrationOpsType.DisruptProjection
                                                : InfiltrationOpsType.Uprise);
                    }

                    break;
                case PersonalityType.Honorable:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    }

                    if (!relations.Treaty_Alliance && relations.TotalAnger > 50)
                    {
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                    }

                    break;
                case PersonalityType.Pacifist:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.Rebellion);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        opsWanted.Add(InfiltrationOpsType.SlowResearch);
                        break;
                    }

                    break;
                case PersonalityType.Cunning:
                    if (relations.AtWar || relations.PreparingForWar)
                    {
                        opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                        opsWanted.Add(InfiltrationOpsType.Sabotage);
                        opsWanted.Add(InfiltrationOpsType.Uprise);
                        break;
                    }

                    if (!relations.Treaty_Alliance || Owner.IsSafeToActivateOpsOnAllies(them))
                    {
                        if (Owner.TechScore < them.TechScore)
                            opsWanted.Add(InfiltrationOpsType.SlowResearch);

                        if (Owner.ExpansionScore < them.ExpansionScore)
                            opsWanted.Add(InfiltrationOpsType.Uprise);

                        if (Owner.IndustrialScore < them.IndustrialScore)
                            opsWanted.Add(InfiltrationOpsType.Sabotage);

                        if (Owner.MilitaryScore < them.MilitaryScore)
                            opsWanted.Add(InfiltrationOpsType.Rebellion);

                        if (Owner.TotalScore < them.TotalScore)
                            opsWanted.Add(InfiltrationOpsType.DisruptProjection);
                    }

                    break;
            }

            foreach (InfiltrationOpsType type in opsWanted)
                operations[type] = true;
        }
    }
}