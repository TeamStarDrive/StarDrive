namespace Ship_Game
{
    public sealed class HullBonus
    {
        public string Hull;
        public float StartingCost;  // additional cost to build
        public float ArmoredBonus;  // % damage reduction
        public float SensorBonus;   // % sensor range
        public float SpeedBonus;    // % speed increase
        public float CargoBonus;    // % cargo room
        public float FireRateBonus; // % fire rate
        public float RepairBonus;   // % repair rate
        public float CostBonus;     // % cost reduction
        public float DamageBonus;   // % weapon damage increase
        public float ShieldBonus;   // % shield power increase

        public static HullBonus Default = new HullBonus { Hull = "" };
       
        public float ArmoredModifier  => 1.0f + ArmoredBonus;
        public float SensorModifier   => 1.0f + SensorBonus;
        public float SpeedModifier    => 1.0f + SpeedBonus;
        public float CargoModifier    => 1.0f + CargoBonus;
        public float FireRateModifier => 1.0f + FireRateBonus;
        public float RepairModifier   => 1.0f + RepairBonus;
        public float CostModifier     => 1.0f + CostBonus;
        public float DamageModifier   => 1.0f + DamageBonus;
        public float ShieldModifier   => 1.0f + ShieldBonus;
    }
}
