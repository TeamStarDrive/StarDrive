using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    public class HullBonus
    {
        public string Hull;
        public float StartingCost; // additional cost to build
        public float ArmoredBonus; // % damage reduction
        public float SensorBonus; // % sensor range
        public float SpeedBonus; // % speed increase
        public float CargoBonus; // % cargo room
        public float FireRateBonus; // % fire rate
        public float RepairBonus; // % repair rate
        public float CostBonus;  // % cost reduction
        public float DamageBonus; // % weapon damage increase
        public float ShieldBonus; // % shield power increase

        public HullBonus()
        {
        }
    }
}
