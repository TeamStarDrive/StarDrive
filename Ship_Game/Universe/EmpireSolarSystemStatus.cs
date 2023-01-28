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
        float CheckCombatTimer;

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

            CheckCombatTimer -= timeStep.FixedTime;
            if (CheckCombatTimer <= 0f)
            {
                CheckCombatTimer = 1f;
                UpdateInCombat();
            }
        }

        void UpdateInCombat()
        {
            DangerousForcesPresent = false;
            for (int i = 0; i < System.ShipList.Count; i++)
            {
                Ship ship = System.ShipList[i];
                if (ship == null || ship.Loyalty == Owner)
                    continue; // Todo: the null check should be removed once ShipList is safe

                if (ship.Loyalty.IsFaction || Owner.IsEmpireAttackable(ship.Loyalty, ship))
                {
                    HostileForcesPresent = true;
                    CombatTimer = Owner.isPlayer ? 5f : 1f;
                    if (ship.BaseStrength > 0 || ship.IsDefaultTroopShip)
                    {
                        DangerousForcesPresent = true;
                        return;
                    }
                }
            }
        }
    }
}
