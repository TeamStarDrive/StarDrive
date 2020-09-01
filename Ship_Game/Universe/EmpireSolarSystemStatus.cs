﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    // there can be multiple races in a single solar system
    public class EmpireSolarSystemStatus
    {
        public SolarSystem System { get; }
        public Empire Owner { get; }

        public bool HostileForcesPresent { get; private set; }
        public bool DangerousForcesPresent { get; private set; }
        public float CombatTimer { get; private set; }

        public EmpireSolarSystemStatus(SolarSystem system, Empire empire)
        {
            System = system;
            Owner = empire;
        }

        public void Update(FixedSimTime timeStep)
        {
            CombatTimer -= timeStep.FixedTime;
            if (CombatTimer <= 0f)
                HostileForcesPresent = false;

            UpdateInCombat();
        }

        void UpdateInCombat()
        {
            foreach (Ship ship in System.ShipList)
            {
                if (ship.loyalty == Owner)
                    continue;

                if (ship.loyalty.isFaction || Owner.IsEmpireAttackable(ship.loyalty, ship))
                {
                    HostileForcesPresent = true;
                    DangerousForcesPresent |= ship.BaseStrength > 0;
                    CombatTimer = Owner.isPlayer ? 5f : 1f;
                    return;
                }
            }
        }
    }
}
