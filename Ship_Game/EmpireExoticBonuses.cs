using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    [StarDataType]
    public class EmpireExoticBonuses
    {
        [StarData] readonly Empire Owner;
        [StarData] float TotalRefinedPerTurn;

        [StarData] public readonly Good Good;
        [StarData] public readonly float MaxBonus;
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
            float excessRefined = (TotalRefinedPerTurn - Consumption).LowerBound(0);
            RefinedPerTurnForConsumption = TotalRefinedPerTurn.UpperBound(Consumption);
            if (excessRefined > 0)
                AddToStorage(excessRefined);
        }

        void AddToStorage(float amount)
        {
            CurrentStorage = (CurrentStorage + amount).UpperBound(MaxStorage);
        }

        public void Update()
        {
            TotalBuiltMiningOps = 0;
            InProgressMiningOps = 0;
            ActiveMiningOps = 0;
            TotalRefinedPerTurn = 0;
            MaxPotentialRefinedPerTurn = 0;
            RefinedPerTurnForConsumption = 0;
            UpdateConsumption();
        }

        void UpdateConsumption()
        {
            float consumption;
            switch (Good.ExoticBonusType)
            {
                default:
                case ExoticBonusType.RepairRate:      // consumption is calculated dynamically 
                case ExoticBonusType.ShieldRecharge:  // consumption is calculated dynamically
                case ExoticBonusType.None:            consumption = 0;                          break; 
                case ExoticBonusType.Credits:         consumption = Owner.TotalPopBillion;      break;
                case ExoticBonusType.DamageReduction: consumption = Owner.TotalShipSurfaceArea; break;
                case ExoticBonusType.Production:      consumption = Owner.GetGrossProduction(); break;
                case ExoticBonusType.WarpSpeed:       consumption = Owner.TotalShipWarpThrustK; break;
            }

            Consumption = consumption * Good.ConsumptionMultiplier;
        }

        public void AddConsumption(float amount)
        {
            Consumption = (Consumption + amount * Good.ConsumptionMultiplier).LowerBound(0);
            // TODO transfer deficit from storage ?
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
            PreviousBonus = CurrentBonus;
            float deficit = (Consumption - TotalRefinedPerTurn).LowerBound(0);
            float deficitToMove = deficit.UpperBound(CurrentStorage);
            CurrentStorage -= deficitToMove;
            RefinedPerTurnForConsumption += deficitToMove;
            OutputThisTurn = MaxPotentialRefinedPerTurn > 0 ? TotalRefinedPerTurn / MaxPotentialRefinedPerTurn : 0;
            CurrentBonus = DynamicBonus;
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
        public float MaxStorage => Owner.AveragePlanetStorage 
                                    * Owner.GetPlanets().Count 
                                    * GlobalStats.Defaults.ExoticRatioStorage
                                    * Owner.data.ExoticStorageMultiplier;
        public float RefiningNeeded => (MaxStorage + Consumption - CurrentStorage - TotalRefinedPerTurn).LowerBound(0);
        public string ActiveVsTotalOps => $"{ActiveMiningOps}/{TotalBuiltMiningOps}";
    }
}
