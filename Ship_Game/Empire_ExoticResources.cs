using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    public partial class Empire
    {
        [StarData] readonly Map<ExoticBonusType, EmpireExoticBonuses> ExoticBonuses;
        public float TotalShipSurfaceArea { get; private set; }
        public float TotalShipWarpThrustK { get; private set; }
        public float MaxExoticStorage => AveragePlanetStorage 
                                            * OwnedPlanets.Count
                                            * GlobalStats.Defaults.ExoticRatioStorage
                                            * data.ExoticStorageMultiplier;

        public Map<ExoticBonusType, EmpireExoticBonuses> GetExoticBonuses()
        {
            return ExoticBonuses;
        }

        public EmpireExoticBonuses GetExoticResource(ExoticBonusType type)
        {
            return ExoticBonuses[type];
        }

        public void AddRefinedResource(ExoticBonusType type, float amount) 
        {
            ExoticBonuses[type].AddRefined(amount);
        }

        public void AddMaxPotentialRefining(ExoticBonusType type, float amount)
        {
            ExoticBonuses[type].AddToMaxRefiningPoterntial(amount);
        }

        public void AddBuiltMiningStation(ExoticBonusType type)
        {
            ExoticBonuses[type].AddBuiltMiningsStation();
        }

        public void AddInProgressMiningsStation(ExoticBonusType type)
        {
            ExoticBonuses[type].AddInProgressMiningsStation();
        }

        public void AddActiveMiningStation(ExoticBonusType type)
        {
            ExoticBonuses[type].AddActiveMiningsStation();
        }

        public bool NeedMoreMiningOpsOfThis(ExoticBonusType type)
        {
            return ExoticBonuses[type].NeedMoreOps;
        }

        public float GetRefiningNeeded(ExoticBonusType type)
        {
            return ExoticBonuses[type].RefiningNeeded;
        }

        public void AddExoticConsumption(ExoticBonusType type, float amount)
        {
            if (ExoticBonuses.ContainsKey(type)) 
                ExoticBonuses[type].AddConsumption(amount);
        }

        public void UpdateExoticConsumpsions() // This will be done before empire goals update
        {
            foreach (EmpireExoticBonuses exoticBonus in ExoticBonuses.Values)
                exoticBonus.Update();
        }

        void CalculateExoticBonuses() // This will be done after empire goals update and DoMoney
        {
            foreach (EmpireExoticBonuses exoticBonus in ExoticBonuses.Values)
                exoticBonus.CalcCurrentBonus();
        }

        public float GetDynamicExoticBonusMuliplier(ExoticBonusType type)
        {
            // if disabled return 1
            return ExoticBonuses.Get(type, out EmpireExoticBonuses exoticBonus) ? exoticBonus.DynamicBonusMultiplier : 1;
        }

        public float GetStaticExoticBonusMuliplier(ExoticBonusType type)
        {
            // if disabled return 1
            return ExoticBonuses.Get(type, out EmpireExoticBonuses exoticBonus) ? exoticBonus.CurrentBonusMultiplier : 1;
        }

        public float GetGrossProduction()
        {
            return OwnedPlanets.Sum(p => p.Prod.GrossIncome);
        }
    }
}
