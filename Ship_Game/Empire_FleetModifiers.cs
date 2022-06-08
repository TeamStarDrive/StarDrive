using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    public partial class Empire
    {
        [StarData]
        public Map<int, float> FleetStrEmpireMultiplier { get; private set; } = new(); // Empire IDs

        /// <summary>
        /// This will get  the empire str modifier required for fleets.
        /// </summary>
        /// <returns>Will return 1 if empire is null</returns>
        public float GetFleetStrEmpireMultiplier(Empire empire)
        {
            if (empire == null)
                return 1;

            int id = empire.Id;
            return FleetStrEmpireMultiplier.ContainsKey(id) 
                ? FleetStrEmpireMultiplier[id] 
                : DifficultyModifiers.TaskForceStrength;
        }

        float IncreaseValue => DifficultyModifiers.FleetStrModifier * PersonalityModifiers.FleetStrMultiplier;
        float DecreaseValue => DifficultyModifiers.FleetStrModifier / PersonalityModifiers.FleetStrMultiplier;


        /// <summary>
        /// This will decrease the str needed vs the target empire slightly. Empire is null safe.
        /// It should be called when a fleet task succeeds
        /// There is hard limit of 5 or (more if Remnants) on the multiplier
        /// </summary>
        public void DecreaseFleetStrEmpireMultiplier(Empire e) => TryUpdateFleetStrEmpireMultiplier(e, -DecreaseValue);
        public void IncreaseFleetStrEmpireMultiplier(Empire e) => TryUpdateFleetStrEmpireMultiplier(e, IncreaseValue);

        void TryUpdateFleetStrEmpireMultiplier(Empire targetEmpire, float value)
        {
            if (targetEmpire == null || isPlayer)
                return;

            if (targetEmpire.WeAreRemnants && value > 0)
                value *= DifficultyModifiers.RemnantStrModifier;

            if (FleetStrEmpireMultiplier.ContainsKey(targetEmpire.Id))
            {
                float maxMultiplier = targetEmpire.WeAreRemnants ? 5 * DifficultyModifiers.RemnantStrModifier : 5;
                float currentValue  = FleetStrEmpireMultiplier[targetEmpire.Id];
                float newValue      = currentValue + value;
                if (newValue > maxMultiplier)
                    newValue /= 2; // reached upper limit, restart at lower value

                FleetStrEmpireMultiplier[targetEmpire.Id] = newValue.LowerBound(0.5f);
            }
            else
            {
                float startingMultiplier = targetEmpire.WeAreRemnants ? DifficultyModifiers.RemnantStrModifier 
                                                                      : DifficultyModifiers.TaskForceStrength;

                FleetStrEmpireMultiplier.Add(targetEmpire.Id, startingMultiplier);
            }
        }

        public void InitFleetEmpireStrMultiplier()
        {
            if (isPlayer || IsFaction)
                return;

            foreach (Empire e in EmpireManager.MajorEmpires.Filter(e => e != this))
                IncreaseFleetStrEmpireMultiplier(e);

            foreach (Empire e in EmpireManager.Factions)
                IncreaseFleetStrEmpireMultiplier(e);
        }
    }
}
