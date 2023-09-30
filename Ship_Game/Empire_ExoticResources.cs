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
    using static HelperFunctions;
    public partial class Empire
    {
        [StarData] public readonly Map<ExoticBonusType, EmpireExoticBonuses> ExoticBonuses;
        public float TotalShipSurfaceArea { get; private set; }
        public float TotalShieldRechargeRate { get; private set; }

        public void AddRefinedResource(ExoticBonusType type, float amount) 
        {
            ExoticBonuses[type].AddRefined(amount);
        }

        public void AddExoticConsumption(ExoticBonusType type, float amount)
        {
            if (ExoticBonuses.ContainsKey(type)) 
                ExoticBonuses[type].AddConsumption(amount);
        }

        void UpdateExoticConsumpsions() // This will be done before empire goals update
        {
            foreach (EmpireExoticBonuses exoticBonus in ExoticBonuses.Values)
                exoticBonus.Update();
        }

        void CalculateExoticBonuses() // This will be done after empire goals update and DoMoney
        {
            foreach (EmpireExoticBonuses exoticBonus in ExoticBonuses.Values)
                exoticBonus.CalcCurrentBonus();
        }

        // This is for Exotic Bonus Data which is calculated in ship update per second (so not perturn)
        void ResetDynamicExoticData()
        {
            TotalShieldRechargeRate = 0;
        }

        public float GetExoticBonusMuliplier(ExoticBonusType type)
        {
            // if disabled return 1
            return ExoticBonuses.Get(type, out EmpireExoticBonuses exoticBonus) ? exoticBonus.DynamicBonusMultiplier : 1;
        }
    }
}
