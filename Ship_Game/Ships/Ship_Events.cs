using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // EVT: Called when ships health changes
        public virtual void OnHealthChange(float change, object source)
        {
            float newHealth = Health + change;

            if (newHealth > HealthMax)
                newHealth = HealthMax;
            else if (newHealth < 0.5f)
                newHealth = 0f;
            Health = newHealth;
        }

        // EVT: Called when a module dies
        public virtual void OnModuleDeath(ShipModule m)
        {
            ShipStatusChanged = true;
            if (m.PowerDraw > 0 || m.ActualPowerFlowMax > 0 || m.PowerRadius > 0)
                ShouldRecalculatePower = true;
            if (m.IsExternal)
                UpdateExternalSlots(m);
            if (m.HasInternalRestrictions)
            {
                SetActiveInternalSlotCount(ActiveInternalModuleSlots - m.Area);
            }

            // kill the ship if all modules exploded or internal slot percent is below critical
            if (Health <= 0f || InternalSlotsHealthPercent < ShipResupply.ShipDestroyThreshold)
            {
                if (Active) // TODO This is a partial work around to help several modules dying at once calling Die cause multiple xp grant and messages
                    Die(LastDamagedBy, false);
            }
        }

        // EVT: called when a module comes back alive
        public virtual void OnModuleResurrect(ShipModule m)
        {
            ShipStatusChanged = true; // update ship status sometime in the future (can be 1 second)
            if (m.PowerDraw > 0 || m.ActualPowerFlowMax > 0 || m.PowerRadius > 0)
                ShouldRecalculatePower = true;
            UpdateExternalSlots(m);
            if (m.HasInternalRestrictions)
            {
                SetActiveInternalSlotCount(ActiveInternalModuleSlots + m.Area);
            }
        }

        // EVT: when a fighter of this carrier is launched
        //      or when a boarding party shuttle launches
        public virtual void OnShipLaunched(Ship ship)
        {
            Carrier.AddToOrdnanceInSpace(ship.ShipOrdLaunchCost);
        }

        // EVT: when a fighter of this carrier returns to hangar
        public virtual void OnShipReturned(Ship ship)
        {
            Carrier.AddToOrdnanceInSpace(-ship.ShipOrdLaunchCost);
        }

        // EVT: when a fighter of this carrier is destroyed
        public virtual void OnLaunchedShipDie(Ship ship)
        {
            Carrier.AddToOrdnanceInSpace(-ship.ShipOrdLaunchCost);
        }

        // EVT: when a ShipModule installs a new weapon
        public virtual void OnWeaponInstalled(ShipModule m, Weapon w)
        {
            Weapons.Add(w);
        }

        // EVT: when a ShipModule installs a new Bomb
        public virtual void OnBombInstalled(ShipModule m)
        {
            BombBays.Add(m);
        }

        // EVT: when a ship dies
        // note that pSource can be null
        public virtual void OnShipDie(Projectile pSource)
        {
            if (IsSubspaceProjector)
                Loyalty.AI.RemoveProjectorFromRoadList(this);

            if (Loyalty.CanBuildPlatforms)
                SetupProjectorBridgeIfNeeded();

            DamageRelationsOnDeath(pSource);
            CreateEventOnDeath();
        }
    }
}
