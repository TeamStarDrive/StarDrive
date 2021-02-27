namespace Ship_Game
{
    public partial class Empire
    {
        public Map<int, float> FleetStrEmpireMultiplier { get; private set; } = new Map<int, float>(); // Empire IDs

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
        float DecreaseValue => DifficultyModifiers.FleetStrModifier / PersonalityModifiers.FleetStrMultiplier / 2;


        /// <summary>
        /// This will decrease the str needed vs the target empire slightly. Empire is null safe.
        /// It should be called when a fleet task succeeds
        /// </summary>
        public void DecreaseFleetStrEmpireMultiplier(Empire e) => TryUpdateFleetStrEmpireMultiplier(e, -DecreaseValue);
        public void IncreaseFleetStrEmpireMultiplier(Empire e) => TryUpdateFleetStrEmpireMultiplier(e, IncreaseValue);

        void TryUpdateFleetStrEmpireMultiplier(Empire targetEmpire, float value)
        {
            if (targetEmpire == null || isPlayer)
                return;

            if (FleetStrEmpireMultiplier.ContainsKey(targetEmpire.Id))
                FleetStrEmpireMultiplier[targetEmpire.Id] = (FleetStrEmpireMultiplier[targetEmpire.Id] + value).LowerBound(0.5f);
            else
                FleetStrEmpireMultiplier.Add(targetEmpire.Id, DifficultyModifiers.TaskForceStrength * (targetEmpire.isFaction ? 2 : 1));
        }

        public void InitFleetEmpireStrMultiplier()
        {
            if (isPlayer || isFaction)
                return;

            foreach (Empire e in EmpireManager.MajorEmpires.Filter(e => e != this))
                IncreaseFleetStrEmpireMultiplier(e);

            foreach (Empire e in EmpireManager.Factions)
                IncreaseFleetStrEmpireMultiplier(e);
        }

        public void RestoreFleetStrEmpireMultiplier(Map<int, float> empireStr)
        {
            if (empireStr != null)
                FleetStrEmpireMultiplier = empireStr;
        }
    }
}
