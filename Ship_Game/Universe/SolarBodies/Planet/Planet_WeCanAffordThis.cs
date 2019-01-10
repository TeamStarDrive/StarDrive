using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;

namespace Ship_Game
{
    public partial class Planet
    {
        // @todo RedFox: This needs heavy refactoring.
        public bool WeCanAffordThis(Building building, ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            float buildingMaintenance = Owner.GetTotalBuildingMaintenance();
            float grossTaxes = Owner.GrossPlanetIncomes;

            bool itsHere = BuildingList.Contains(building);

            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
                    if (queueItem.Building == building) itsHere = true;
                }

            }
            buildingMaintenance += building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod;

            bool lowPri = buildingMaintenance / grossTaxes < .25f;
            bool medPri = buildingMaintenance / grossTaxes < .60f;
            bool highPri = buildingMaintenance / grossTaxes < .80f;
            float income = Money.NetIncome;
            float maintenance = income - building.Maintenance;
            bool incomeBuilding = maintenance > 0;

            int defensiveBuildings = BuildingList.Count(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded > 0 || combat.TheWeapon != null);
            int possibleOffensiveBuilding = BuildingsCanBuild.Count(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.TheWeapon != null);
            bool isDefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon;
            float defenseRatio = 0;
            if (defensiveBuildings + possibleOffensiveBuilding > 0)
                defenseRatio = (defensiveBuildings + 1) / (float)(defensiveBuildings + possibleOffensiveBuilding + 1);
            bool needDefense = false;

            if (Owner.data.TaxRate > .5f)
                incomeBuilding = false;
            //dont scrap buildings if we can use treasury to pay for it.
            if (building.AllowInfantry && !BuildingList.Contains(building) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SystemCommander SC))
            {
                if (incomeBuilding)
                    needDefense = SC.RankImportance >= defenseRatio * 10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }

            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == Type)
                return false;


            if (itsHere && building.Unique && (incomeBuilding || building.Maintenance < Owner.Money * .001))
                return true;

            if (income*building.PlusTaxPercentage >= building.Maintenance
                || building.CreditsProduced(this) >= building.Maintenance)
                return true;
            if (building.IsOutpost || building.WinsGame)
                return true;
            //dont build +food if you dont need to

            if (NonCybernetic && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (Food.NetIncome > 0 && Food.Percent < 0.3f || BuildingExists(building.Name))
                    return false;
                return true;

            }
            if (NonCybernetic && income > building.Maintenance)
            {
                float food = building.FoodProduced(this);
                if (food * Food.Percent > 1)
                {
                    return true;
                }
            }
            if (IsCybernetic)
            {
                if (Prod.NetIncome < 0)
                {
                    if (building.PlusFlatProductionAmount > 0 && (Prod.Percent > 0.5f || income > building.Maintenance * 2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * PopulationBillion > building.Maintenance * (2 - Prod.Percent))
                    {
                        if (income > ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * MineralRichness > building.Maintenance)
                        return true;
                }
            }
            if (building.PlusTerraformPoints > 0)
            {
                if (!incomeBuilding || IsCybernetic || BuildingList.Contains(building) || BuildingInQueue(building.Name))
                    return false;

            }
            if (!incomeBuilding || DevelopmentLevel < 3)
            {
                if (building.IsBiospheres)
                    return false;
            }

            bool iftrue = false;
            switch (governor)
            {
                case ColonyType.Agricultural:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && Prod.NetMaxPotential > 20)
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && NonCybernetic)
                            return false;
                        if (highPri)
                        {
                            if (building.PlusFlatFoodAmount > 0
                                || (building.PlusFoodPerColonist > 0 && Population > 500f)

                                //|| this.developmentLevel > 4
                                || ((building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0) && Population > MaxPopulation * .5f)
                                || building.PlusFlatFoodAmount > 0
                                || building.PlusFlatProductionAmount > 0
                                || building.StorageAdded > 0
                                // || (IsCybernetic && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount>0))
                                || (needDefense && isDefensive && DevelopmentLevel > 3)
                                )
                                return true;
                            //iftrue = true;

                        }
                        if (!iftrue && medPri && DevelopmentLevel > 2 && incomeBuilding)
                        {
                            if (
                                building.IsBiospheres ||
                                (building.PlusTerraformPoints > 0 && Fertility < 3)
                                || building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0
                                || DevelopmentLevel > 3
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                || (needDefense && isDefensive)

                                )
                                return true;
                        }
                        if (lowPri && DevelopmentLevel > 4 && incomeBuilding)
                        {
                            iftrue = true;
                        }
                        break;
                    }
                #endregion
                
                case ColonyType.Core:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && NonCybernetic)
                            return false;
                        if (highPri)
                        {

                            if (building.StorageAdded > 0
                                || (NonCybernetic && (building.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                                || (NonCybernetic && building.PlusFlatFoodAmount > 0)
                                || (NonCybernetic && building.PlusFoodPerColonist > 0)
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && (PopulationBillion) > 1)
                                //|| building.IsBiospheres

                                || (needDefense && isDefensive && DevelopmentLevel > 3)
                                || (IsCybernetic && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (medPri && DevelopmentLevel > 3 && incomeBuilding)
                        {
                            if (DevelopmentLevel > 2 && needDefense && (building.TheWeapon != null || building.Strength > 0))
                                return true;
                            iftrue = true;
                        }
                        if (!iftrue && lowPri && DevelopmentLevel > 4 && incomeBuilding && income > building.Maintenance)
                        {

                            iftrue = true;
                        }
                        break;
                    }
                #endregion
                case ColonyType.Industrial:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && Prod.NetMaxPotential > 20)
                        {
                            return true;
                        }
                        if (highPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0
                                || (NonCybernetic && Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.StorageAdded > 0
                                || (needDefense && isDefensive && DevelopmentLevel > 3)
                                )
                                return true;
                        }
                        if (medPri && DevelopmentLevel > 2 && incomeBuilding)
                        {
                            if (building.PlusResearchPerColonist * (PopulationBillion) > building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                            || (NonCybernetic && building.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation && MaxPopulation > 2000 && income > building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && Food.NetIncome < 0)
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                )

                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && lowPri && DevelopmentLevel > 3 && incomeBuilding && income > building.Maintenance)
                        {
                            if (needDefense && isDefensive && DevelopmentLevel > 2)
                                return true;

                        }
                        break;
                    }
                #endregion
                case ColonyType.Military:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && NonCybernetic)
                            return false;
                        if (highPri)
                        {
                            if (building.isWeapon
                                || building.IsSensor
                                || building.Defense > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (MineralRichness < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlanetaryShieldStrengthAdded > 0
                                || (building.AllowShipBuilding && HasProduction)
                                || (building.ShipRepair > 0 && HasProduction)
                                || building.Strength > 0
                                || (building.AllowInfantry && HasProduction)
                                || needDefense && (building.TheWeapon != null || building.Strength > 0)
                                || (IsCybernetic && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                iftrue = true;
                        }
                        if (!iftrue && medPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0)
                                iftrue = true;
                        }
                        if (!iftrue && lowPri && DevelopmentLevel > 4)
                        {
                            iftrue = true;

                        }
                        break;
                    }
                #endregion
                case ColonyType.Research:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && Prod.NetMaxPotential > 20)
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && NonCybernetic)
                            return false;

                        if (highPri)
                        {
                            if (building.PlusFlatResearchAmount > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusResearchPerColonist > 0
                                || (IsCybernetic && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0))
                                || (needDefense && isDefensive && DevelopmentLevel > 3)
                                )
                                return true;

                        }
                        if (medPri && DevelopmentLevel > 3 && incomeBuilding)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                            || NonCybernetic && ((building.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * .5f && MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && Food.NetIncome < 0))
                                )
                                return true;
                        }
                        if (lowPri && DevelopmentLevel > 4 && incomeBuilding)
                        {
                            if (needDefense && isDefensive && DevelopmentLevel > 2)

                                return true;
                        }
                        break;
                    }
                    #endregion
            }
            return iftrue;

        }

    }
}
