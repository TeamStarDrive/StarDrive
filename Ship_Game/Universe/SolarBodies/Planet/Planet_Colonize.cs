﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Fat Bastard - Centralized all New Colony related logic here
    {
        public void Colonize(Ship colonyShip)
        {
            Owner = colonyShip.loyalty;
            ParentSystem.OwnerList.Add(Owner);
            SetupColonyType();
            Owner.AddPlanet(this);
            SetExploredBy(Owner);
            CreateStartingEquipment(colonyShip);
            AddMaxBaseFertility(Owner.data.EmpireFertilityBonus);
            CrippledTurns = 0;
            ResetGarrisonSize();
            Owner.GetEmpireAI(). FindAndRemoveGoal(GoalType.Colonize, g => g.ColonizationTarget == this);
            NewColonyAffectPresentTroops();
            NewColonyAffectRelations();
            SetupCyberneticsWorkerAllocations();
            StatTracker.StatAddColony(Empire.Universe.StarDate, this);
        }

        void SetupColonyType()
        {
            if (Owner.isPlayer && !Owner.AutoColonize)
                colonyType = ColonyType.Colony;
            else
                colonyType = Owner.AssessColonyNeeds(this);

            if (Owner.isPlayer)
                Empire.Universe.NotificationManager.AddColonizedNotification(this, EmpireManager.Player);
        }

        void NewColonyAffectRelations()
        {
            if (ParentSystem.OwnerList.Count <= 1)
                return;

            foreach (Planet p in ParentSystem.PlanetList)
            {
                if (p.Owner == null || p.Owner == Owner)
                    continue;

                if (p.Owner.TryGetRelations(Owner, out Relationship rel) && !rel.Treaty_OpenBorders)
                    p.Owner.DamageRelationship(Owner, "Colonized Owned System", 20f, p);
            }
        }

        void NewColonyAffectPresentTroops()
        {
            bool troopsRemoved       = false;
            bool playerTroopsRemoved = false;

            for (int i = TroopsHere.Count - 1; i >= 0; i--)
            {
                Troop t      = TroopsHere[i];
                Empire owner = t?.Loyalty;

                if (owner != null && !owner.isFaction && owner.data.DefaultTroopShip != null && owner != Owner &&
                    Owner.TryGetRelations(owner, out Relationship rel) && !rel.AtWar)
                {
                    Ship troopship = t.Launch(ignoreMovement: true);
                    troopsRemoved  = true;
                    playerTroopsRemoved |= t.Loyalty.isPlayer;
                    troopship?.AI.OrderRebaseToNearest();
                }
            }

            if (troopsRemoved)
                OnTroopsRemoved(playerTroopsRemoved);
        }

        void OnTroopsRemoved(bool playerTroopsRemoved)
        {
            if (playerTroopsRemoved)
                Empire.Universe.NotificationManager.AddTroopsRemovedNotification(this);
            else if (Owner.isPlayer)
                Empire.Universe.NotificationManager.AddForeignTroopsRemovedNotification(this);
        }

        void CreateStartingEquipment(Ship colonyShip)
        {
            var startingEquipment  = colonyShip.StartingEquipment();
            Building outpost       = ResourceManager.GetBuildingTemplate(Building.OutpostId);
            
            // always spawn an outpost on a new colony
            if (!OutpostBuiltOrInQueue())
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
            Building building = ResourceManager.CreateBuilding(template);
            BuildingList.Add(building);
            building.AssignBuildingToTileOnColonize(this);
            Storage.Max = Math.Max(Storage.Max, building.StorageAdded); // so starting resources could be added
        }

        void SetupCyberneticsWorkerAllocations() 
        {
            if (Owner.IsCybernetic)
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
