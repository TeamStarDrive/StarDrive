using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Fleet[] Fleets;
        public Fleet NearestFleet { get; private set; }
        public float ThreatDistance { get; private set; }
        public float PulseTime { get; private set; } = 5;

        public Empire[] Enemies => Fleets?.FilterSelect(f => f != null, f=>f.Owner);
        Empire Owner;

        public IncomingThreat (Empire owner, SolarSystem system, Fleet[] fleets)
        {
            TargetSystem = system;
            Owner        = owner;
            ThreatTimer  = ThreatResetTime;
            Notified     = false;
            Strength     = fleets.Sum(f => f.GetStrength() * Owner.GetFleetStrEmpireMultiplier(f.Owner));
            Fleets       = fleets;
        }

        public bool UpdateThreat(SolarSystem system, Fleet[] fleets)
        {
            if (system != TargetSystem) return false;

            Strength    = fleets.Sum(f => f.GetStrength());
            ThreatTimer = ThreatResetTime;
            Fleets      = fleets;
            return true;
        }

        public void UpdateTimer(FixedSimTime simTime)
        {
            ThreatTimer      -= simTime.FixedTime;
            if (ThreatTimedOut) {Fleets = null; return; }

            NearestFleet      = Fleets.FindMin(f => f.AveragePosition().SqDist(TargetSystem.Position));
            ThreatDistance    = NearestFleet.AveragePosition().Distance(TargetSystem.Position);
            PulseTime        -= simTime.FixedTime;
            
            if (PulseTime <= 0) PulseTime = 1;
        }
    }
}
