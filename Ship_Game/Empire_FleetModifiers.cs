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

        /// <summary>
        /// This will decrease the str needed vs the target empire slightly. Empire is null safe.
        /// It should be called when a fleet task succeeds
        /// </summary>
        public void DecreaseFleetStrEmpireMultiplier(Empire e) 
            => TryUpdateFleetStrEmpireMultiplier(e, -0.2f * ((int)CurrentGame.Difficulty).LowerBound(1));

        public void UpdateFleetStrEmpireMultiplier(Empire e)
            => TryUpdateFleetStrEmpireMultiplier(e, 0.2f * ((int)CurrentGame.Difficulty).LowerBound(1));

        void TryUpdateFleetStrEmpireMultiplier(Empire targetEmpire, float value)
        {
            if (targetEmpire == null || isPlayer)
                return;

            if (FleetStrEmpireMultiplier.ContainsKey(targetEmpire.Id))
                FleetStrEmpireMultiplier[targetEmpire.Id] = (FleetStrEmpireMultiplier[targetEmpire.Id] + value).LowerBound(0.5f);
            else
                FleetStrEmpireMultiplier.Add(targetEmpire.Id, DifficultyModifiers.TaskForceStrength);
        }

        public void InitFleetEmpireStrMultiplier()
        {
            if (isPlayer || isFaction)
                return;

            foreach (Empire e in EmpireManager.MajorEmpires.Filter(e => e != this))
                UpdateFleetStrEmpireMultiplier(e);

            foreach (Empire e in EmpireManager.Factions)
                UpdateFleetStrEmpireMultiplier(e);
        }

        public void RestoreFleetStrEmpireMultiplier(Map<int, float> empireStr)
        {
            FleetStrEmpireMultiplier = empireStr;
        }
    }
}
