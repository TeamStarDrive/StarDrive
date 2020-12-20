using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;

namespace Ship_Game.Empires.DataPackets
{
    public class IncomingThreat
    {
        public SolarSystem TargetSystem { get; private set; }
        public float ThreatTimer { get; private set; }
        const float ThreatResetTime = 1;
        public bool ThreatTimedOut => ThreatTimer <= 0;
        bool Notified;
        public float Strength { get; private set; } = 0;
        Array<Fleet> Fleets;
        public Fleet NearestFleet { get; private set; }
        public float ThreatDistance { get; private set; }
        public float PulseTime { get; private set; } = 5;

        public Empire[] Enemies => Fleets?.FilterSelect(f => f != null, f=>f.Owner);
        Empire Owner;
        public bool HighPriority { get; private set; } = false;

        public IncomingThreat (Empire owner, SolarSystem system, Fleet[] fleets)
        {
            TargetSystem = system;
            Owner        = owner;
            ThreatTimer  = ThreatResetTime;
            Notified     = false;
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

        public void UpdateTimer(FixedSimTime simTime)
        {
            ThreatTimer      -= simTime.FixedTime;
            if (ThreatTimedOut) {Fleets = new Array<Fleet>(); return; }

            NearestFleet      = Fleets.FindMin(f => f.AveragePosition().SqDist(TargetSystem.Position));
            ThreatDistance    = NearestFleet?.AveragePosition().Distance(TargetSystem.Position) ?? float.MaxValue;
            PulseTime        -= simTime.FixedTime;
            ProcessFleetThreat();

            if (PulseTime <= 0) PulseTime = 1;
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

                if (fleet.Owner.isPlayer || fleet.FleetTask.GetTaskCategory() == MilitaryTask.TaskCategory.War)
                {
                    HighPriority = true;
                }
            }

            Fleets = newFleets;
        }
    }
}
