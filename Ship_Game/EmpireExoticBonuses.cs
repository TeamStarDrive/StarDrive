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
    public struct EmpireExoticBonuses
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Good Good;
        [StarData] public readonly float MaxBonus;
        [StarData] public float Consumption { get; private set; }
        [StarData] public float CurrentStorage { get; private set; }
        [StarData] public float TotalRefinedPerTurn { get; private set; }
        [StarData] public float CurrentBonus { get; private set; }

        public EmpireExoticBonuses(Empire owner, Good good)
        {
            Owner = owner;
            Consumption = 0;
            CurrentStorage = 0;
            MaxBonus = good.MaxBonus;
            Good = good;
            TotalRefinedPerTurn = 0;
        }

        public void AddRefined(float amount)
        {
            TotalRefinedPerTurn += amount;
            float excessRefined = (TotalRefinedPerTurn - Consumption).LowerBound(0);
            TotalRefinedPerTurn = TotalRefinedPerTurn.UpperBound(Consumption);
            if (excessRefined > 0)
                AddToStorage(excessRefined);
        }

        void AddToStorage(float amount)
        {
            CurrentStorage = (CurrentStorage + amount).UpperBound(MaxStorage);
        }

        public void Update()
        {
            TotalRefinedPerTurn = 0;
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
            Consumption += amount * Good.ConsumptionMultiplier;
        }

        public void CalcCurrentBonus()
        {
            CurrentBonus = DynamicBonus;
        }

        public float DynamicBonus
        {
            get
            {
                return Consumption == 0
                        ? TotalRefinedPerTurn > 0 || CurrentStorage > 0 ? MaxBonus : 0f
                        : ((TotalRefinedPerTurn + CurrentStorage) / Consumption * MaxBonus).UpperBound(MaxBonus);
            }
        }

        public float CurrentBonusMultiplier => 1 + CurrentBonus;

        public float DynamicBonusMultiplier => 1 + DynamicBonus;

        public float MaxStorage => Owner.AveragePlanetStorage * Owner.GetPlanets().Count * GlobalStats.Defaults.ExoticRatioStorage;
    }
}
