using SDGraphics;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
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
            if (Health <= 0f || InternalSlotsHealthPercent < GlobalStats.Defaults.ShipDestroyThreshold)
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
        public virtual void OnShipLaunched(Ship launchedShip, ShipModule hangar)
        {
            Carrier.AddToOrdnanceInSpace(launchedShip.ShipOrdLaunchCost);
            ChangeOrdnance(-launchedShip.ShipOrdLaunchCost);
            hangar.SetHangarShip(launchedShip);
            launchedShip.Mothership = this;
            hangar.HangarTimer = hangar.HangarTimerConstant;
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
            UpdateOrdnancePercentage();
            Carrier.DisposeHangarShip(ship);
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
            bool shouldCheckInvestigateInhibition = true;
            if (pSource?.Module != null)
                UpdateKillAndDefenseTasks(pSource.Module, ref shouldCheckInvestigateInhibition);
            
            if (shouldCheckInvestigateInhibition)
                AddInhibitionInvestigationIfNeeded();

            if (IsSubspaceProjector)
                Loyalty.AI.SpaceRoadsManager.RemoveProjectorFromRoadList(this);

            if (IsDysonSwarmController)
                System.DysonSwarm?.Update();

            if (Loyalty.CanBuildPlatforms)
            {
                Loyalty.AI.SpaceRoadsManager.SetupProjectorBridgeIfNeeded(this,
                    pSource != null ? ProjectorBridgeEndCondition.NoHostiles : ProjectorBridgeEndCondition.Timer);
            }

            if (IsResearchStation)
                OnResearchStationDeath();

            if (IsMiningStation)
                OnMiningStationDeath();

            DamageRelationsOnDeath(pSource);
            CreateEventOnDeath();
        }

        void UpdateKillAndDefenseTasks(ShipModule module, ref bool shouldCheckInvestigateInhibition)
        {
            Ship killerShip = module.GetParent();
            killerShip.UpdateEmpiresOnKill(this);
            killerShip.AddKill(this);
            // Defend vs lone ships, like remnants or cunning players
            if (!Loyalty.isPlayer
                && killerShip.Fleet == null
                && System?.HasPlanetsOwnedBy(Loyalty) == true
                && Loyalty.IsEmpireHostile(killerShip.Loyalty)
                && (killerShip.Loyalty.IsFaction || killerShip.Loyalty.isPlayer))
            {
                shouldCheckInvestigateInhibition = false; // We are dealing with it with defense tasks or going to deal with it now
                if (!Loyalty.HasWarTaskTargetingSystem(System))
                {
                    MilitaryTaskImportance importance = killerShip.Loyalty.isPlayer
                        ? MilitaryTaskImportance.Important
                        : MilitaryTaskImportance.Normal;

                    Loyalty.AddDefenseSystemGoal(System, Loyalty.KnownEnemyStrengthIn(System), importance);
                }
            }
        }

        void OnResearchStationDeath()
        {
            // must use the goal planet/system, since tether was removed when the ship died and no way to know
            // if it was researching  a planet or a star
            Goal researchGoal = Loyalty.AI.FindGoal(g => g is ProcessResearchStation && g.TargetShip == this);
            if (researchGoal != null)
            {
                if (researchGoal.TargetPlanet != null)
                    Universe.RemoveEmpireFromResearchableList(Loyalty, researchGoal.TargetPlanet);
                else if (System != null)
                    Universe.RemoveEmpireFromResearchableList(Loyalty, System);
                else
                    Log.Error($"On Ship die - research station {Name} System was null!");
            }
            else
            {
                Log.Error($"On Ship die - research station {Name} no goal found!");
            }

            Loyalty.AI.SpaceRoadsManager.RemoveRoadIfNeeded(System);
        }

        void OnMiningStationDeath()
        {
            Goal miningGoal = Loyalty.AI.FindGoal(g => g is MiningOps && g.TargetShip == this);
            if (miningGoal != null && miningGoal.TargetPlanet.IsMineable)
            {
                miningGoal.TargetPlanet.System.GetPotentialOpsOwner(out Empire potentialOwner);
                miningGoal.TargetPlanet.Mining.ChangeOwnershipIfNeeded(potentialOwner);
            }
            else
            {
                Log.Error($"On Ship die - mining station {Name} no goal found!");
            }

            Loyalty.AI.SpaceRoadsManager.RemoveRoadIfNeeded(System);
        }

        void AddInhibitionInvestigationIfNeeded()
        {
            if (!Loyalty.isPlayer
                && !Loyalty.IsFaction
                && !IsSubspaceProjector
                && InhibitionSource == InhibitionType.EnemyShip
                && (IsFreighter || IsConstructor)
                && (System == null || !Loyalty.HasWarTaskTargetingSystem(System))
                && !Loyalty.AI.HasGoal(g => g.IsInvsestigationHere(Position)))
            {
                float strNeeded = Loyalty.Threats.GetHostileStrengthAt(Position, 30_000).LowerBound(1000);
                Empire enemy = Loyalty.Threats.GetStrongestHostileAt(Position, 30_000);
                if (enemy == null || !enemy.WeAreRemnants)
                    Loyalty.AI.AddGoalAndEvaluate(new InhibitorInvestigate(Loyalty, enemy, strNeeded, Position));
            }
        }
    }
}
