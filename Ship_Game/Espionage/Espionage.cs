using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using System;

namespace Ship_Game
{
    // This is a new Espionage System that uses Infiltration Level instead of Agents
    [StarDataType]
    public class Espionage
    {
        public const byte MaxLevel = 5;
        public const float PercentMoneyLeech = 0.02f;
        public const float SlowResearchBy = 0.1f;
        [StarData] public byte Level;
        [StarData] readonly Empire Owner;
        [StarData] public readonly Empire Them;
        [StarData] public float LevelProgress { get; private set; }
        [StarData] int Weight;
        [StarData] Array<InfiltrationOperation> Operations = new();
        [StarData] Mole StickyMole;
        [StarData] public int SlowResearchChance { get; private set; }
        [StarData] public float TotalMoneyLeeched { get; private set; }
        [StarData] float MoneyLeechedThisTurn;

        [StarDataConstructor]
        public Espionage() { }

        public Espionage(Empire us, Empire them) 
        {
            Owner = us;
            Them  = them;
        }


        // Used for Remnants and Pirates
        public void IncreaseInfiltrationLevelTo(byte value)
        {
            for (byte i = 1; i <= value; i++)
                IncreaseInfiltrationLevel();
        }

        void IncreaseInfiltrationLevel()
        {
            if (Level == MaxLevel) 
                return;

            Level++;
            LevelProgress = 0;
            EnablePassiveEffects();
        }

        public void WipeoutInfiltration() => SetInfiltrationLevelTo(0);

        public void ReduceInfiltrationLevel() => SetInfiltrationLevelTo((byte)(Level.LowerBound(1) - 1));

        public void SetInfiltrationLevelTo(byte value)
        {
            Level = value.LowerBound(0);
            LevelProgress = 0;
            RemoveOperations();
            EnablePassiveEffects();
        }

        public void DecreaseProgress(float value)
        {
            if (Level == 0)
                return;

            LevelProgress -= value;
            if (LevelProgress < 0)
                SetInfiltrationLevelTo((byte)(Level-1));
        }

        public void Update(int totalWeight)
        {
            RemoveOperations();
            float progressToIncrease = GetProgressToIncrease(Owner.EspionagePointsPerTurn, totalWeight);
            UpdateOperations(Operations.Count > 0 ? progressToIncrease / Operations.Count : 0);

            if (AtMaxLevel)
                return;

            LevelProgress = (LevelProgress + progressToIncrease).UpperBound(LevelCost(MaxLevel));
            if (LevelProgress >= NextLevelCost)
                IncreaseInfiltrationLevel();
        }

        public string RemainingTurnsForOps(InfiltrationOpsType type)
        {
            int remainingTurns =  CalcRemainingTurnsForOps(type);
            return remainingTurns > -1 ? $"({remainingTurns} turns)" : "(INF)";
        }

        int CalcRemainingTurnsForOps(InfiltrationOpsType type)
        {
            if (Weight == 0)
                return -1;

            int totalWeight = Owner.CalcTotalEspionageWeight();
            float progressPerTurn = 0;
            InfiltrationOperation ops = Operations.Find(o => o.Type == type);
            if (ops != null)
            {
                progressPerTurn = GetProgressToIncrease(Owner.EspionagePointsPerTurn, totalWeight) / Operations.Count;
                return ops.TurnsToComplete(progressPerTurn);
            }
            else
            {
                progressPerTurn = GetProgressToIncrease(Owner.EspionagePointsPerTurn, totalWeight) / (Operations.Count+1);
                if (Operations.Count == 0)
                    progressPerTurn *= 0.5f; // We are simulating at least 1 operation, so the progress alocated is going to be halved
                return InfiltrationOperation.BaseRemainingTurns(type, LevelCost(GetOpsLevel(type)), progressPerTurn, Owner.Universe);
            }
        }

        void RemoveOperations()
        {
            for (int i = Operations.Count - 1; i >= 0; i--)
            {
                InfiltrationOperation mission = Operations[i];
                if (mission.Level > Level)
                    Operations.Remove(mission);
            }

            if (!CanPlantStickyMole && StickyMole != null)
            {
                Owner.data.MoleList.Remove(StickyMole);
                StickyMole = null;
            }

            if (!CanSlowResearch && SlowResearchChance > 0) 
                SlowResearchChance = 0;
        }

        void UpdateOperations(float progress)
        {
            for (int i = 0; i < Operations.Count; i++)
            {
                InfiltrationOperation mission = Operations[i];
                mission.Update(progress);
            }
        }

        public void DecreaseSlowResearchChance()
        {
            SlowResearchChance -= 1;
        }

        public void SetSlowResearchChance(int value)
        {
            SlowResearchChance = value;
        }

        public void SetDisruptProjectionChance(int value)
        {
            Them.SetInfluenceDisableChance(value);
        }

        void EnablePassiveEffects()
        {
            if (Level >= 1)
                Them.SetCanBeScannedByPlayer(true); // This ability cannot be lost after it was achieved.

            if (CanPlantStickyMole && StickyMole == null)
            {
                StickyMole = Mole.PlantStickyMoleAtHomeworld(Owner, Them, out Planet targetPlanet);
                if (StickyMole != null)
                {
                    string message = $"{Localizer.Token(GameText.NewSuccessfullyInfiltratedAColony)} {targetPlanet.Name}";
                    Owner.Universe.Notifications.AddAgentResult(true, message, Owner, targetPlanet);
                }
            }
        }

        public float GetProgressToIncrease(float espionagePoints, float totalWeight)
        {
            float activeMissionRatio = Operations.Count > 0 ? 0.5f : 1;
            return espionagePoints
                   * (Weight / totalWeight.LowerBound(1))
                   * (Them.TotalPopBillion / Owner.TotalPopBillion.LowerBound(0.1f))
                   * (1 - Them.EspionageDefenseRatio*0.75f)
                   * activeMissionRatio;
        }

        public void SetWeight(int value)
        {
            Weight = value;
        }

        public int GetWeight()
        {
            return !AtMaxLevel ? Weight : 0;
        }

        public int LevelCost(byte level)
        {
            // default costs
            // 1 - 50
            // 2 - 100
            // 3 - 200
            // 4 - 400
            // 5 - 800
            return level == 0 ? 0 : (int)(50 * Math.Pow(2, level-1) * Owner.Universe.SettingsResearchModifier * Owner.Universe.P.Pace);
        }

        public void AddLeechedMoney(float money)
        {
            Owner.AddMoney(money);
            TotalMoneyLeeched += money;
            MoneyLeechedThisTurn += money;
        }

        public float ExtractMoneyLeechedThisTurn()
        {
            float monetLeeched = MoneyLeechedThisTurn;
            MoneyLeechedThisTurn = 0;
            return monetLeeched;
        }

        public int NextLevelCost => LevelCost((byte)(Level+1));

        public bool CanViewPersonality   => Level >= 1;
        public bool CanViewNumPlanets    => Level >= 1;
        public bool CanViewPop           => Level >= 1;
        public bool CanViewTheirTreaties => Level >= 1;

        public bool CanViewNumShips     => Level >= 2;
        public bool CanViewTechType     => Level >= 2;
        public bool CanViewArtifacts    => Level >= 2;
        public bool CanViewRanks        => Level >= 2;
        public bool ProjectorsCanAlert  => Level >= 2;

        public bool CanViewDefenseRatio  => Level >= 3;
        public bool CanViewMoneyAndMaint => Level >= 3;
        public bool CanViewResearchTopic => Level >= 3;
        public bool CanViewBonuses       => Level >= 3;
        bool CanPlantStickyMole          => Level >= 3;

        public bool CanLeechTech    => Level >= 4;
        public bool CanSlowResearch => Level >= 4 && SlowResearchChance > 0;
        public bool CanViewTraitSet => Level >= 4;

        public bool CanLeechMoney     => Level >= 5;
        public bool CanViewTheirMoles => Level >= 5;


        public bool AtMaxLevel => Level >= MaxLevel;
        public float ProgressPercent => LevelProgress/NextLevelCost * 100;

        public string InfiltrationLevelSummary()
        {
            if (Level <= 1)
                return "Unknown";

            int theirInfiltrationLevel = Them.GetRelations(Owner).Espionage.Level;
            if (Level <= 2)
                return theirInfiltrationLevel > 0 ? "Exists" : "Probably None";

            if (Level <= 4)
                return theirInfiltrationLevel == 0 ? "None" 
                                                   : theirInfiltrationLevel > 3 ? "Deep" : "Shallow";

            return $"{theirInfiltrationLevel}";
        }

        public void AddOperation(InfiltrationOpsType type)
        {
            if (Operations.Any(m => m.Type == type))
            {
                Log.Error($"Mission type {type} already exists for {Owner}");
                return;
            }

            int levelCost = LevelCost(GetOpsLevel(type));

            switch (type) 
            {
                case InfiltrationOpsType.PlantMole:         Operations.Add(new InfiltrationOpsPlantMole(Owner, Them, levelCost));         break;
                case InfiltrationOpsType.Uprise:            Operations.Add(new InfiltrationOpsUprise(Owner, Them, levelCost));            break;
                case InfiltrationOpsType.CounterEspionage:  Operations.Add(new InfiltrationOpsCounterEspionage(Owner, Them, levelCost));  break;
                case InfiltrationOpsType.Sabotage:          Operations.Add(new InfiltrationOpsSabotage(Owner, Them, levelCost));          break;
                case InfiltrationOpsType.SlowResearch:      Operations.Add(new InfiltrationOpsDisruptResearch(Owner, Them, levelCost));   break;
                case InfiltrationOpsType.Rebellion:         Operations.Add(new InfiltrationOpsRebellion(Owner, Them, levelCost));         break;
                case InfiltrationOpsType.DisruptProjection: Operations.Add(new InfiltrationOpsDisruptProjection(Owner, Them, levelCost)); break;
                default: throw new ArgumentOutOfRangeException("InfiltrationOpsType", $"InfiltrationOpsType {type} case not defined");
            }
        }

        public void RemoveOperation(InfiltrationOpsType type) 
        {
            for (int i = Operations.Count - 1; i >= 0; i--)
            {
                InfiltrationOperation mission = Operations[i];
                if (mission.Type == type)
                    Operations.Remove(mission);
            }
        }

        public bool IsOperationActive(InfiltrationOpsType type) => Operations.Any(m => m.Type == type);

        static public byte GetOpsLevel(InfiltrationOpsType type)
        {
            switch (type)
            {
                case InfiltrationOpsType.PlantMole:         return 2;
                case InfiltrationOpsType.Uprise:            return 3;
                case InfiltrationOpsType.CounterEspionage:  return 3;
                case InfiltrationOpsType.Sabotage:          return 4;
                case InfiltrationOpsType.SlowResearch:      return 4;
                case InfiltrationOpsType.Rebellion:         return 5;
                case InfiltrationOpsType.DisruptProjection: return 5;
                default: throw new ArgumentOutOfRangeException("InfiltrationOpsType", $"InfiltrationOpsType {type} case not defined");
            }
        }
    }
}
