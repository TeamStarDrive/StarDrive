using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public class EmpireExoticBonuses
    {
        [StarData] readonly Empire Owner;
        [StarData] float DynamicConsumption;
        [StarData] public readonly Good Good;
        [StarData] public readonly float MaxBonus;

        [StarData] public float TotalRefinedPerTurn { get; private set; }
        [StarData] public float Consumption { get; private set; }
        [StarData] public float CurrentStorage { get; private set; }
        [StarData] public float CurrentBonus { get; private set; }
        [StarData] public float PreviousBonus { get; private set; }
        [StarData] public float RefinedPerTurnForConsumption { get; private set; } // so it wont be over consumption for change calculations
        [StarData] public int TotalBuiltMiningOps { get; private set; }
        [StarData] public int InProgressMiningOps { get; private set; }
        [StarData] public int ActiveMiningOps { get; private set; }
        [StarData] public float MaxPotentialRefinedPerTurn { get; private set; }
        [StarData] public float OutputThisTurn { get; private set; }

        public EmpireExoticBonuses(Empire owner, Good good)
        {
            Owner = owner;
            MaxBonus = good.MaxBonus;
            Good = good;
        }

        public EmpireExoticBonuses()
        {
        }

        public void AddRefined(float amount)
        {
            TotalRefinedPerTurn += amount;
            RefinedPerTurnForConsumption = TotalRefinedPerTurn.UpperBound(Consumption);
        }

        void RemoveFromStorage(float amount)
        {
            CurrentStorage = (CurrentStorage - amount).Clamped(0, MaxStorage);
        }

        public void Update()
        {
            TotalBuiltMiningOps = 0;
            InProgressMiningOps = 0;
            ActiveMiningOps = 0;
            TotalRefinedPerTurn = 0;
            MaxPotentialRefinedPerTurn = 0;
            RefinedPerTurnForConsumption = 0;
            UpdateStaticConsumption();
        }

        void UpdateStaticConsumption()
        {
            float consumption = 0;
            switch (Good.ExoticBonusType)
            {
                case ExoticBonusType.RepairRate:      
                case ExoticBonusType.ShieldRecharge:  consumption = DynamicConsumption;                 break;
                case ExoticBonusType.Credits:         consumption = Owner.TotalPopBillion;              break;
                case ExoticBonusType.DamageReduction: consumption = Owner.TotalShipSurfaceArea;         break;
                case ExoticBonusType.Production:      consumption = Owner.GetGrossProductionNoExotic(); break;
                case ExoticBonusType.WarpSpeed:       consumption = Owner.TotalShipWarpThrustK;         break;
                case ExoticBonusType.None:
                default: break;
            }

            Consumption = consumption * Good.ConsumptionMultiplier;
        }

        public void AddDynamicConsumption(float amount)
        {
            DynamicConsumption += amount;
        }

        public void AddToMaxRefiningPoterntial(float amount) 
        {
            MaxPotentialRefinedPerTurn += amount;
        }

        public void AddBuiltMiningsStation()
        {
            TotalBuiltMiningOps += 1; 
        }

        public void AddInProgressMiningsStation()
        {
            InProgressMiningOps += 1;
        }

        public void AddActiveMiningsStation()
        {
            ActiveMiningOps += 1;
        }

        public void CalcCurrentBonus()
        {
            PreviousBonus   = CurrentBonus;
            float remainder = (Consumption - TotalRefinedPerTurn).UpperBound(CurrentStorage); // can be positive or negative
            RemoveFromStorage(remainder);
            OutputThisTurn  = MaxPotentialRefinedPerTurn > 0 ? TotalRefinedPerTurn / MaxPotentialRefinedPerTurn : 0;
            CurrentBonus    = DynamicBonus;
            DynamicConsumption = 0;
        }

        public float DynamicBonus
        {
            get
            {
                return Consumption == 0
                        ? RefinedPerTurnForConsumption > 0 || CurrentStorage > 0 ? MaxBonus : 0f
                        : ((RefinedPerTurnForConsumption + CurrentStorage) / Consumption * MaxBonus).UpperBound(MaxBonus);
            }
        }

        public bool NeedMoreOps => CurrentStorage < MaxStorage * 0.5f 
                                   && Consumption - TotalRefinedPerTurn > 0
                                   && TotalBuiltMiningOps == ActiveMiningOps
                                   && InProgressMiningOps == 0;
        public string CurrentPercentageOutput => $"{(OutputThisTurn * 100).String(0)}%";
        public string DynamicBonusString => $"{(DynamicBonus * 100).String(1)}%";
        public float CurrentBonusMultiplier => 1 + CurrentBonus;
        public float DynamicBonusMultiplier => 1 + DynamicBonus;
        public float RefiningNeeded => (MaxStorage + Consumption - CurrentStorage - TotalRefinedPerTurn).LowerBound(0);
        public string ActiveVsTotalOps => $"{ActiveMiningOps}/{TotalBuiltMiningOps}/{InProgressMiningOps}";
        float MaxStorage => Owner.MaxExoticStorage;
    }
}
