using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Fat Bastard - Centralized all New Colony related logic here
    {
        // The empire whose colony here was wiped out, kept while it may return and after it returned in time
        [StarData] public Empire LostBy { get; private set; }
        [StarData] float LostStarDate;
        [StarData] float ReturnStarDate;

        int FullColonyGraceTurns => (int)(50 * Universe.ProductionPace);

        int TurnsSinceLost(float starDate) => (int)Math.Round((starDate - LostStarDate) * 10);

        // How many turns after the loss this empire keeps off the planet, 0 if it does not respect lostBy's grace
        int ColonyGraceTurns(Empire lostBy, Empire other)
        {
            if (lostBy == null
                || lostBy == other
                || lostBy.IsDefeated
                || other.IsFaction
                || other.IsAtWarWith(lostBy)
                || !other.isPlayer && other.PersonalityModifiers.IgnoresColonyGrace)
            {
                return 0;
            }

            for (int i = 0; i < System.PlanetList.Count; i++)
            {
                if (System.PlanetList[i].Owner == other)
                    return FullColonyGraceTurns / 2;
            }

            return FullColonyGraceTurns;
        }

        public int ColonyGraceTurnsLeft(Empire settler) => ColonyGraceTurnsLeft(settler, out _);

        public int ColonyGraceTurnsLeft(Empire settler, out Empire lostBy)
        {
            lostBy = LostBy;
            if (Owner != null)
                return 0;

            return (ColonyGraceTurns(lostBy, settler) - TurnsSinceLost(Universe.StarDate)).LowerBound(0);
        }

        // The empire which lost this colony came back to it before other's grace ran out
        public bool IsColonyGraceReturn(Empire returning, Empire other)
        {
            return Owner == returning
                && LostBy == returning
                && TurnsSinceLost(ReturnStarDate) < ColonyGraceTurns(returning, other);
        }

        public static string ColonyGraceTip(Empire lostBy, int turnsLeft)
        {
            return string.Format(Localizer.Token(GameText.ColonizeGracePeriodTip), lostBy.Name, turnsLeft);
        }

        public void SetOwner(Empire newOwner, Empire attacker = null)
        {
            Empire oldOwner = Owner;
            Owner = newOwner;
            if (newOwner != null && LostBy != null)
            {
                if (newOwner == LostBy && TurnsSinceLost(Universe.StarDate) < FullColonyGraceTurns)
                    ReturnStarDate = Universe.StarDate;
                else
                    LostBy = null;
            }

            Food.ResetAveragePercentage();
            System.UpdateOwnerList();

            if (oldOwner != null)
            {
                if (attacker != null)
                    oldOwner.RemovePlanet(this, attacker);
                else
                    oldOwner.RemovePlanet(this);
            }

            if (newOwner != null)
            {
                if (attacker != null)
                    newOwner.AddPlanet(this, loser: oldOwner);
                else
                    newOwner.AddPlanet(this);

                if (attacker != null && attacker.isPlayer && oldOwner == newOwner.Universe.Cordrazine)
                    attacker.IncrementCordrazineCapture();
            }

            if (newOwner != oldOwner)
            {
                NumBuildShipsLaunched = 0;
                SupplyShuttlesOut = 0;
            }

            UpdateShipyards();
        }

        public void Colonize(Ship colonyShip)
        {
            SetOwner(colonyShip.Loyalty);
            Quarantine     = false;
            ManualOrbitals = false;
            System.OwnerList.Add(Owner);
            SetupColonyType();
            SetExploredBy(Owner);
            CreateStartingEquipment(colonyShip);
            UnloadTroops(colonyShip);
            UnloadCargoColonists(colonyShip);
            AddMaxBaseFertility(Owner.data.EmpireFertilityBonus);
            CrippledTurns = 0;
            ResetGarrisonSize();
            LaunchNonOwnerTroops();
            NewColonyAffectRelations();
            SetupCyberneticsWorkerAllocations();
            SetInGroundCombat(Owner);
            AbortLandingPlayerFleets();
            Owner.TryTransferCapital(this);
            Universe.Stats.StatAddColony(Universe.StarDate, this);
            TriggerBadEvents();
        }

        void TriggerBadEvents()
        {
            foreach (PlanetGridSquare tile in TilesList.Filter(t => t.EventOnTile))
            {
                if (tile.IsCrashSiteActive)
                    continue;

                ExplorationEvent exEvent = ResourceManager.Event(tile.Building.EventTriggerUID);
                if (exEvent.OutcomeWillLaunchEnemyShip(this, tile.EventOutcomeNum))
                {
                    ResourceManager.Event(tile.Building.EventTriggerUID)
                        .TriggerPlanetEvent(this, Owner, tile, Universe.Screen);
                }
            }
        }

        void SetupColonyType()
        {
            if (OwnerIsPlayer && !Owner.AutoColonize)
                CType = ColonyType.Colony;
            else
                CType = Owner.AssessColonyNeeds(this);

            if (OwnerIsPlayer)
                Universe.Notifications?.AddColonizedNotification(this, Universe.Player);
        }

        void NewColonyAffectRelations()
        {
            if (System.OwnerList.Count <= 1)
                return;

            foreach (Planet p in System.PlanetList)
            {
                if (p.Owner == null || p.Owner == Owner || IsColonyGraceReturn(Owner, p.Owner))
                    continue;

                if (!p.Owner.IsOpenBordersTreaty(Owner))
                    p.Owner.DamageRelationship(Owner, "Colonized Owned System", 20f, p);
            }
        }

        public void AbortLandingPlayerFleets()
        {
            Empire player = Universe.Player;
            if (player == Owner || player.IsAtWarWith(Owner)) 
                return;

            foreach (Fleet fleet in player.ActiveFleets)
            {
                if (fleet.Ships.Any(s => s.IsTroopShipAndRebasingOrAssaulting(this)))
                {
                    fleet.OrderAbortMove();
                    Universe.Notifications.AddAbortLandNotification(this, fleet);
                }
            }
        }

        public void LaunchNonOwnerTroops()
        {
            bool troopsRemoved       = false;
            bool playerTroopsRemoved = false;

            foreach (Troop t in Troops.GetTroopsNotOf(Owner))
            {
                if (!Owner.IsAtWarWith(t.Loyalty))
                {
                    Ship troopTransport = t.Launch(forceLaunch: true);
                    troopsRemoved |= troopTransport != null;
                    playerTroopsRemoved |= t.Loyalty.isPlayer;
                    troopTransport?.AI.OrderRebaseToNearest();
                }
            }

            if (troopsRemoved)
                OnTroopsRemoved(playerTroopsRemoved);
        }

        void OnTroopsRemoved(bool playerTroopsRemoved)
        {
            if (playerTroopsRemoved)
                Universe.Notifications.AddTroopsRemovedNotification(this);
            else if (OwnerIsPlayer)
                Universe.Notifications.AddForeignTroopsRemovedNotification(this);
        }

        void UnloadTroops(Ship colonyShip)
        {
            var troops = colonyShip.GetOurTroops();
            for (int i = troops.Count - 1; i >= 0; i--)
            {
                Troop t = troops[i];
                t.TryLandTroop(this);
            }
        }

        void UnloadCargoColonists(Ship colonyShip)
        {
            Population += colonyShip.UnloadColonists();
        }

        void CreateStartingEquipment(Ship colonyShip)
        {
            var startingEquipment  = colonyShip.StartingEquipment();
            Building outpost       = ResourceManager.GetBuildingTemplate(Building.OutpostId);
            
            // always spawn an outpost on a new colony
            if (!OutpostOrCapitalBuiltOrInQueue())
                SpawnNewColonyBuilding(outpost);

            SpawnExtraBuildings(startingEquipment);
            FoodHere   += startingEquipment.AddFood;
            ProdHere   += startingEquipment.AddProd;
            Population += startingEquipment.AddColonists;
        }

        void SpawnExtraBuildings(ColonyEquipment startingEquipment)
        {
            foreach (string buildingId in startingEquipment.SpecialBuildingIDs)
            {
                Building extraBuilding = ResourceManager.GetBuildingTemplate(buildingId);
                if (!extraBuilding.Unique || !BuildingBuiltOrQueued(extraBuilding))
                    SpawnNewColonyBuilding(extraBuilding);
            }
        }

        void SpawnNewColonyBuilding(Building template)
        {
            Building building = ResourceManager.CreateBuilding(this, template);
            building.AssignBuildingToTileOnColonize(this);
        }

        void SetupCyberneticsWorkerAllocations() 
        {
            if (IsCybernetic)
            {
                Food.Percent = 0;
                Prod.Percent = 0.5f;
                Res.Percent  = 0.5f;
            }
        }
    }

    public struct ColonyEquipment
    {
        public readonly float AddFood;
        public readonly float AddProd;
        public readonly float AddColonists;
        public readonly Array<string> SpecialBuildingIDs;

        public ColonyEquipment(float addFood, float addProd, float addColonists, Array<string> specialBuildingIDs)
        {
            AddFood            = addFood;
            AddProd            = addProd;
            AddColonists       = addColonists;
            SpecialBuildingIDs = specialBuildingIDs;
        }
    }
}
