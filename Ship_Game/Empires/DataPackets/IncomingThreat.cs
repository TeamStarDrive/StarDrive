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
        const float ThreatResetTime = 5;
        public bool ThreatTimedOut => ThreatTimer <= 0;
        bool Notified;
        public float Strength { get; private set; } = 0;
        Fleet[] Fleets;

        public Empire[] Enemies => Fleets?.FilterSelect(f => f != null, f=>f.Owner);

        public IncomingThreat (SolarSystem system, Fleet[] fleets)
        {
            TargetSystem = system;
            ThreatTimer  = ThreatResetTime;
            Notified     = false;
            Strength     = fleets.Sum(f => f.GetStrength());
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
            ThreatTimer -= simTime.FixedTime;
            if (ThreatTimedOut) Fleets = null;
        }
    }
}
