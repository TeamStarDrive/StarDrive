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
        public bool IsMeagerOrBarren => Level <= (int)DevelopmentLevel.Meager;
        public bool IsVibrant        => Level >= (int)DevelopmentLevel.Vibrant;
        public bool IsCoreWorld      => Level >= (int)DevelopmentLevel.CoreWorld;
        public bool IsMegaWorld      => Level >= (int)DevelopmentLevel.MegaWorld;

        struct Afford
        {
            public readonly bool HighPri, MedPri, LowPri, MakingMoney, NeedDefense;

            public Afford(Empire owner, Planet p, Building b)
            {
                float netPlanetMoney = p.Money.NetRevenue;
                float extraMaint = b.Maintenance * owner.data.Traits.MaintMultiplier;
                foreach (QueueItem q in p.ConstructionQueue)
                {
                    if (q.isBuilding) extraMaint += owner.data.Traits.MaintMultiplier * q.Building.Maintenance;
                }

                LowPri = extraMaint / netPlanetMoney < 0.25f;
                MedPri = extraMaint / netPlanetMoney < 0.60f;
                HighPri = extraMaint / netPlanetMoney < 0.80f;
                MakingMoney = (p.Money.NetRevenue - b.Maintenance) > 0;

                int defensiveBuildings = p.BuildingList.Count(def => def.SoftAttack > 0 || def.PlanetaryShieldStrengthAdded > 0 || def.TheWeapon != null);
                int offensiveBuildings = p.BuildingsCanBuild.Count(off => off.PlanetaryShieldStrengthAdded > 0 || off.SoftAttack > 0 || off.TheWeapon != null);
                bool isDefensive = b.SoftAttack > 0 || b.PlanetaryShieldStrengthAdded > 0 || b.isWeapon;
                float defenseRatio = 0;
                if (defensiveBuildings + offensiveBuildings > 0)
                    defenseRatio = (defensiveBuildings + 1) / (float)(defensiveBuildings + offensiveBuildings + 1);

                // determine defensive needs.
                NeedDefense = false;
                if (MakingMoney && isDefensive && owner.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(p.ParentSystem, out SystemCommander commander))
                {
                    NeedDefense = commander.RankImportance >= defenseRatio * 10;
                }
            }
        }

        // @todo RedFox: This needs heavy refactoring.
        public bool WeCanAffordThis(Building b, ColonyType governor)
        {
            if (governor == ColonyType.TradeHub || b.IsPlayerAdded)
                return true;

            // don't scrap buildings if we can use treasury to pay for it.
            if (b.AllowInfantry && !BuildingList.Contains(b) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            if (b.ExcludesPlanetType.NotEmpty() && b.ExcludesPlanetType == CategoryName)
                return false;
            
            if (Money.GrossRevenue*b.PlusTaxPercentage >= b.Maintenance 
                || b.CreditsProduced(this) >= b.Maintenance
                || b.IsOutpost
                || b.WinsGame)
                return true;

            var a = new Afford(Owner, this, b);

            if (b.Unique && BuildingExists(b) && (a.MakingMoney || b.Maintenance < Owner.Money * 0.001))
                return true;

            // don't build +food if you don't need to
            if (NonCybernetic)
            {
                if (b.PlusFlatFoodAmount > 0)
                    return (Food.NetIncome <= 0 || Food.Percent >= 0.3f) && !BuildingExists(b);
                if (Money.GrossRevenue > b.Maintenance && b.FoodProduced(this) * Food.Percent > 1f)
                    return true;
            }
            else if (IsCybernetic && Prod.NetIncome < 0)
            {
                if (b.PlusFlatProductionAmount > 0
                    && (Prod.Percent > 0.5f || Money.GrossRevenue > b.Maintenance * 2))
                    return true;
                if (b.PlusProdPerColonist > 0 
                    && b.PlusProdPerColonist * PopulationBillion > b.Maintenance * (2 - Prod.Percent)
                    && Money.GrossRevenue > ShipBuildingModifier * 2)
                    return true;
                if (b.PlusProdPerRichness * MineralRichness > b.Maintenance)
                    return true;
            }

            if (b.PlusTerraformPoints > 0)
            {
                if (!a.MakingMoney || IsCybernetic || BuildingExists(b))
                    return false;
            }
            if (!a.MakingMoney || IsMeagerOrBarren)
            {
                if (b.IsBiospheres)
                    return false;
            }

            switch (governor)
            {
                case ColonyType.Agricultural: return CanAffordAgricultural(a, b);
                case ColonyType.Core:         return CanAffordCore(a, b);
                case ColonyType.Industrial:   return CanAffordIndustrial(a, b);
                case ColonyType.Military:     return CanAffordMilitary(a, b);
                case ColonyType.Research:     return CanAffordForResearch(a, b);
            }
            return false;
        }
        
        bool CanAffordAgricultural(in Afford a, Building b)
        {
            if (b.AllowShipBuilding && Prod.NetMaxPotential > 20)
                return true;
            if (Fertility > 0 && b.MinusFertilityOnBuild > 0 && NonCybernetic)
                return true;
            if (a.HighPri)
            {
                if (b.PlusFlatFoodAmount > 0
                    || (b.PlusFoodPerColonist > 0 && Population > 500f)
                    || ((b.MaxPopIncrease > 0
                    || b.PlusFlatPopulation > 0 || b.PlusTerraformPoints > 0) && Population > MaxPopulation * 0.5f)
                    || b.PlusFlatFoodAmount > 0
                    || b.PlusFlatProductionAmount > 0
                    || b.StorageAdded > 0
                    || (a.NeedDefense && IsCoreWorld))
                    return true;
            }

            if (a.MedPri && IsVibrant && a.MakingMoney)
            {
                if (b.IsBiospheres || (b.PlusTerraformPoints > 0 && Fertility < 3)
                                   || b.MaxPopIncrease > 0
                                   || b.PlusFlatPopulation > 0
                                   || IsCoreWorld
                                   || b.PlusFlatResearchAmount > 0
                                   || (b.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                   || a.NeedDefense)
                    return true;
            }

            if (a.LowPri && IsMegaWorld && a.MakingMoney)
                return true;
            return false;
        }
        
        bool CanAffordCore(in Afford a, Building b)
        {
            if (Fertility > 0 && b.MinusFertilityOnBuild > 0 && NonCybernetic)
                return false;
            if (a.HighPri)
            {
                if (b.StorageAdded > 0
                    || (NonCybernetic && (b.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                    || ((b.MaxPopIncrease > 0 || b.PlusFlatPopulation > 0)
                        && Population == MaxPopulation && Money.GrossRevenue > b.Maintenance)
                    || (NonCybernetic && b.PlusFlatFoodAmount > 0)
                    || (NonCybernetic && b.PlusFoodPerColonist > 0)
                    || b.PlusFlatProductionAmount > 0
                    || b.PlusProdPerRichness > 0
                    || b.PlusProdPerColonist > 0
                    || b.PlusFlatResearchAmount > 0
                    || (b.PlusResearchPerColonist > 0 && (PopulationBillion) > 1)
                    || (a.NeedDefense && IsCoreWorld)
                    || (IsCybernetic &&
                        (b.PlusProdPerRichness > 0 || b.PlusProdPerColonist > 0 || b.PlusFlatProductionAmount > 0))
                )
                    return true;
            }
            if (a.MedPri && IsCoreWorld && a.MakingMoney)
                return true;
            if (a.LowPri && IsMegaWorld && a.MakingMoney)
                return true;
            return false;
        }
        
        bool CanAffordIndustrial(in Afford a, Building b)
        {
            if (b.AllowShipBuilding && Prod.NetMaxPotential > 20)
                return true;
            if (a.HighPri)
            {
                if (b.PlusFlatProductionAmount > 0
                    || b.PlusProdPerRichness > 0
                    || b.PlusProdPerColonist > 0
                    || b.PlusFlatProductionAmount > 0
                    || (NonCybernetic && Fertility < 1f && b.PlusFlatFoodAmount > 0)
                    || b.StorageAdded > 0
                    || (a.NeedDefense && IsCoreWorld))
                    return true;
            }

            if (a.MedPri && IsVibrant && a.MakingMoney)
            {
                if ((b.PlusResearchPerColonist * PopulationBillion) > b.Maintenance
                    || ((b.MaxPopIncrease > 0 || b.PlusFlatPopulation > 0) && Population == MaxPopulation)
                    || (NonCybernetic && b.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation &&
                        MaxPopulation > 2000)
                    || (b.PlusFlatFoodAmount > 0 && Food.NetIncome < 0)
                    || b.PlusFlatResearchAmount > 0
                    || (b.PlusResearchPerColonist > 0 && MaxPopulation > 999))
                    return true;
            }

            return a.LowPri && IsCoreWorld
                && a.MakingMoney && a.NeedDefense && IsVibrant;
        }

        bool CanAffordMilitary(in Afford a, Building b)
        {
            if (Fertility > 0 && b.MinusFertilityOnBuild > 0 && NonCybernetic)
                return false;
            if (a.HighPri)
            {
                if (b.isWeapon
                    || b.IsSensor
                    || b.Defense > 0
                    || (Fertility < 1f && b.PlusFlatFoodAmount > 0)
                    || (MineralRichness < 1f && b.PlusFlatFoodAmount > 0)
                    || b.PlanetaryShieldStrengthAdded > 0
                    || (b.AllowShipBuilding && HasProduction)
                    || (b.ShipRepair > 0 && HasProduction)
                    || b.Strength > 0
                    || (b.AllowInfantry && HasProduction)
                    || a.NeedDefense && (b.TheWeapon != null || b.Strength > 0)
                    || (IsCybernetic &&
                        (b.PlusProdPerRichness > 0 || b.PlusProdPerColonist > 0 || b.PlusFlatProductionAmount > 0))
                )
                    return true;
            }

            if (a.MedPri)
            {
                if (b.PlusFlatProductionAmount > 0
                    || b.PlusProdPerRichness > 0
                    || b.PlusProdPerColonist > 0
                    || b.PlusFlatProductionAmount > 0)
                    return true;
            }

            return a.LowPri && IsMegaWorld;
        }

        bool CanAffordForResearch(in Afford a, Building b)
        {
            if (b.AllowShipBuilding && Prod.NetMaxPotential > 20)
                return true;

            if (Fertility > 0 && b.MinusFertilityOnBuild > 0 && NonCybernetic)
                return true;

            if (a.HighPri)
            {
                if (b.PlusFlatResearchAmount > 0
                    || (Fertility < 1f && b.PlusFlatFoodAmount > 0)
                    || (Fertility < 1f && b.PlusFlatFoodAmount > 0)
                    || b.PlusFlatProductionAmount > 0
                    || b.PlusResearchPerColonist > 0
                    || (IsCybernetic && (b.PlusFlatProductionAmount > 0 || b.PlusProdPerColonist > 0))
                    || (a.NeedDefense && IsCoreWorld))
                    return true;
            }
            if (a.MedPri && IsCoreWorld && a.MakingMoney)
            {
                if (((b.MaxPopIncrease > 0 || b.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                    || NonCybernetic && ((b.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * 0.5f && MaxPopulation > 2000)
                    || (b.PlusFlatFoodAmount > 0 && Food.NetIncome < 0)))
                    return true;
            }
            if (a.LowPri && IsMegaWorld && a.MakingMoney)
            {
                if (a.NeedDefense && IsVibrant)
                    return true;
            }
            return false;
        }
    }
}
