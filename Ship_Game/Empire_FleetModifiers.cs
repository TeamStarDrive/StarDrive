using System;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Ship_Game
{
    public partial class Empire
    {
        public Map<Guid, float> TargetsFleetStrMultiplier { get; private set; } = new Map<Guid, float>();
        public Map<int, float> FleetStrEmpireModifier { get; private set; } = new Map<int, float>(); // Empire IDs

        /// <summary>
        /// Updates the Str needed for the fleet for this guid. Empire can be null.
        /// </summary>
        public void UpdateTargetsStrMultiplier(Guid guid, Empire e)
        {
            if (TargetsFleetStrMultiplier.ContainsKey(guid))
                TargetsFleetStrMultiplier[guid] += 0.2f * ((int)CurrentGame.Difficulty).LowerBound(1);
            else
                TargetsFleetStrMultiplier.Add(guid, DifficultyModifiers.TaskForceStrength);

            // Updating the modifier for the empire, long term value
            TryUpdateFleetStrEmpireModifier(e, TargetsFleetStrMultiplier[guid]);
        }

        public void RemoveTargetsStrMultiplier(Guid guid)
        {
            TargetsFleetStrMultiplier.Remove(guid);
        }

        public float GetTargetsStrMultiplier(Guid guid, Empire targetEmpire)
        {
            float multi = TargetsFleetStrMultiplier.ContainsKey(guid) 
                ? TargetsFleetStrMultiplier[guid] 
                : DifficultyModifiers.TaskForceStrength;

            return multi + GetFleetStrEmpireModifier(targetEmpire);
        }

        public float GetTargetsStrMultiplier(Planet planet, Empire targetEmpire)
        {
            return planet == null 
                ? 1 + GetFleetStrEmpireModifier(targetEmpire) 
                : GetTargetsStrMultiplier(planet.guid, targetEmpire);
        }

        public float GetTargetsStrMultiplier(Ship ship, Empire targetEmpire)
        {
            return ship == null 
                ? 1 + GetFleetStrEmpireModifier(targetEmpire)
                : GetTargetsStrMultiplier(ship.guid, targetEmpire);
        }

        public float GetTargetsStrMultiplier(SolarSystem system, Empire targetEmpire)
        {
            return system == null 
                ? 1 + GetFleetStrEmpireModifier(targetEmpire) 
                : GetTargetsStrMultiplier(system.guid,targetEmpire);
        }

        /// <summary>
        /// This will get  the empire str modifier required for fleets.
        /// </summary>
        /// <param name="empire"></param>
        /// <returns>Will return 0 if empire is null</returns>
        float GetFleetStrEmpireModifier(Empire empire)
        {
            if (empire == null)
                return 0;

            int id = empire.Id;
            return FleetStrEmpireModifier.ContainsKey(id) ? FleetStrEmpireModifier[id] : 0;
        }

        void TryUpdateFleetStrEmpireModifier(Empire empire, float value)
        {
            if (empire == null)
                return;

            if (FleetStrEmpireModifier.ContainsKey(empire.Id))
                FleetStrEmpireModifier[empire.Id] = value.LowerBound(FleetStrEmpireModifier[empire.Id]);
            else
                FleetStrEmpireModifier.Add(empire.Id, value);
        }

        /// <summary>
        /// This will decrease the str needed vs the target empire slightly. Empire is null safe.
        /// It should be called when a fleet task suceeds
        /// </summary>
        /// <param name="empire"></param>
        public void DecreaseFleetStrEmpireModifier(Empire empire)
        {
            if (empire == null)
                return;

            int id = empire.Id;
            if (FleetStrEmpireModifier.ContainsKey(id))
                FleetStrEmpireModifier[id] = (FleetStrEmpireModifier[id] - 0.2f).LowerBound(0);
            else
                FleetStrEmpireModifier.Add(id, 0);
        }

        public void RestoreTargetsStrMultiplier(Map<Guid, float> claims)
        {
            TargetsFleetStrMultiplier = claims;
        }

        public void RestoreFleetStrEmpireModifier(Map<int, float> empireStr)
        {
            FleetStrEmpireModifier = empireStr;
        }
    }
}
