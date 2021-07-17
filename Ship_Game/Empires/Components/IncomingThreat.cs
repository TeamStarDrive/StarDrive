﻿using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;

namespace Ship_Game.Empires.Components
{
    public class IncomingThreat
    {
        public SolarSystem TargetSystem { get; private set; }
        public float ThreatTimer { get; private set; }
        const float ThreatResetTime = 1;
        public bool ThreatTimedOut => ThreatTimer <= 0;
        public float Strength { get; private set; }
        Array<Fleet> Fleets;
        public Fleet NearestFleet { get; private set; }
        public float ThreatDistance { get; private set; }
        public float PulseTime { get; private set; } = 5;

        public Empire[] Enemies => Fleets?.FilterSelect(f => f != null, f=>f.Owner);
        readonly Empire Owner;
        public bool HighPriority { get; private set; } = false;

        public IncomingThreat (Empire owner, SolarSystem system, Fleet[] fleets)
        {
            TargetSystem = system;
            Owner        = owner;
            ThreatTimer  = ThreatResetTime;
            Strength     = fleets.Sum(f => f.GetStrength() * Owner.GetFleetStrEmpireMultiplier(f.Owner));
            Fleets       = new Array<Fleet>(fleets);
            ProcessFleetThreat();
        }

        public bool UpdateThreat(SolarSystem system, Fleet[] fleets)
        {
            if (system != TargetSystem) return false;

            Strength    = fleets.Sum(f => f.GetStrength() * Owner.GetFleetStrEmpireMultiplier(f.Owner));
            ThreatTimer = ThreatResetTime;

            foreach(var fleetToAdd in fleets)
            {
                Fleets.AddUnique(fleetToAdd);
            }
            return true;
        }

        public bool UpdateTimer(FixedSimTime simTime)
        {
            ThreatTimer -= simTime.FixedTime;
            if (ThreatTimedOut)
            {
                Fleets = new Array<Fleet>(); 
                return false;
            }

            NearestFleet      = Fleets.FindMin(f => f.AveragePosition().SqDist(TargetSystem.Position));
            ThreatDistance    = NearestFleet?.AveragePosition().Distance(TargetSystem.Position) ?? float.MaxValue;
            PulseTime        -= simTime.FixedTime;
            ProcessFleetThreat();

            if (PulseTime <= 0) 
                PulseTime = 1;

            return true;
        }

        void ProcessFleetThreat()
        {
            HighPriority  = false;
            var newFleets = Fleets;

            for (int i = 0; i < Fleets.Count; i++)
            {
                var fleet = Fleets[i];
                if (fleet?.Ships.IsEmpty != false)
                {
                    newFleets.RemoveAtSwapLast(i);
                    continue;
                }

                MilitaryTask.TaskCategory cat = MilitaryTask.TaskCategory.FleetNeeded | MilitaryTask.TaskCategory.War;
                if (fleet.Owner.isPlayer && fleet.GetStrength() > 0 || fleet.FleetTask?.GetTaskCategory() == cat)
                {
                    HighPriority = true;
                }
            }

            Fleets = newFleets;
        }
    }
}
