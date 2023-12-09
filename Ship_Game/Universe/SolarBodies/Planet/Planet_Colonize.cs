using System;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Fat Bastard - Centralized all New Colony related logic here
    {
        public void SetOwner(Empire newOwner, Empire attacker = null)
        {
            Empire oldOwner = Owner;
            Owner = newOwner;
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
                if (p.Owner == null || p.Owner == Owner)
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
