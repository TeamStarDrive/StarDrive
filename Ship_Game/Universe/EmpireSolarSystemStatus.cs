﻿using Ship_Game.Ships;

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
            Owner  = empire;
            UpdateInCombat();
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
            DangerousForcesPresent = false;
            for (int i = 0; i < System.ShipList.Count; i++)
            {
                Ship ship = System.ShipList[i];
                if (ship == null || ship.loyalty == Owner)
                    continue; // Todo: the null check should be removed once ShipList is safe

                if (ship.loyalty.isFaction || Owner.IsEmpireAttackable(ship.loyalty, ship))
                {
                    HostileForcesPresent = true;
                    CombatTimer          = Owner.isPlayer ? 5f : 1f;
                    if (ship.BaseStrength > 0)
                    {
                        DangerousForcesPresent = true;
                        return;
                    }
                }
            }
        }
    }
}
