using SDGraphics;
using Ship_Game.AI.Budget;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;
using System.Collections.Generic;

namespace Ship_Game
{
    public partial class Planet
    {
        public bool GovernorOn  => CType != ColonyType.Colony;
        public bool GovernorOff => CType == ColonyType.Colony;
        int NumHabitableTiles => TilesList.Count(t => t.Habitable); // Bioshperes are counted here
        public float CurrentProductionToQueue => Prod.NetIncome + InfraStructure;
        public float EstimatedAverageProduction => (Prod.NetMaxPotential / (IsCybernetic ? 2 : 3)).LowerBound(0.1f);
        float EstimatedAverageFood => (Food.NetMaxPotential / 3).LowerBound(0.1f);

        [StarData] public ColonyBlueprints Blueprints {get; private set;}
        [StarData] public bool SpecializedTradeHub { get; private set; }

        public bool HasBlueprints => Blueprints != null;
        public bool HasExclusiveBlueprints => Blueprints?.Exclusive == true;
        bool RequiredInBlueprints(Building b) => Blueprints?.IsRequired(b) == true;

        public void SetSpecializedTradeHub(bool value)
        {
            SpecializedTradeHub = value;
        }

        public void DoGoverning()
        {
            RefreshBuildingsWeCanBuildHere();
            UpdateBiospheresBeingBuilt();
            if (RecentCombat)
                return; // Cant Build stuff when there is combat on the planet

            BuildTroops();
            BuildTroopsForEvents(); // For AI to explore event in colony
            TryBuildTerraformers(TerraformBudget); // Build Terraformers if needed/enabled
            TryBuildDysonSwarmControllers();

            // If there is no Outpost or Capital, build it. This is done for non governor planets as well
            BuildOutpostOrCapitalIfAble();

            if (CType == ColonyType.Colony)
                return; // No Governor? Never mind!

            // Switch to Core for AI if there is nothing in the research queue (Does not actually change assigned Governor)
            if ((!OwnerIsPlayer || Owner.AutoResearch) && CType == ColonyType.Research && Owner.Research.NoTopic)
                CType = ColonyType.Core;

            // Change to core colony if there is only 1 planet so the AI can build stuff
            if (!OwnerIsPlayer && Owner.GetPlanets().Count == 1)
                CType = ColonyType.Core;

            if (CType != ColonyType.TradeHub)
            {
                Food.Percent = 0;
                Prod.Percent = 0;
                Res.Percent = 0;
            }

            CreateAndOrUpdateBudget();
            switch (CType) // New resource management by Gretman
            {
                case ColonyType.TradeHub:
                    DetermineFoodState(0.2f, 0.8f);
                    DetermineProdState(0.2f, 0.8f);
                    break;
                case ColonyType.Core:
                    BuildAndScrapBuildings(Budget);
                    AssignCoreWorldWorkers();
                    DetermineFoodState(0.2f, 0.5f); // Start Importing if stores drop below 20%, and stop importing once stores are above 50%.
                    DetermineProdState(0.2f, 0.5f); // Start Exporting if stores are above 50%, but dont stop exporting unless stores drop below 25%.
                    break;
                case ColonyType.Industrial:
                    BuildAndScrapBuildings(Budget);
                    // Farm to 30% storage, then devote the rest to Work, then to research when that starts to fill up
                    AssignOtherWorldsWorkers(0.33f, 1, 0, 2);
                    DetermineFoodState(0.75f, 0.99f);    // Start Importing if food drops below 75%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    if (NonCybernetic)
                        DetermineProdState(0f, 0.5f); // Never import (unless constructing something) Start exporting at 50%, and dont stop unless below 25%.
                    else
                        DetermineProdState(0.2f, 0.66f); // Start Importing if prod drops below 20%, stop importing at 40%. Start exporting at 66%, and dont stop unless below 33%.

                    break;
                case ColonyType.Research:
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    BuildAndScrapBuildings(Budget);
                    AssignOtherWorldsWorkers(0.15f, 0.15f, 0, 0);
                    DetermineFoodState(0.75f, 0.99f); // Import if either drops below 75%, and stop importing once stores reach 100%.
                    DetermineProdState(0.25f, 0.99f); // This planet will export when stores reach 100%
                    break;
                case ColonyType.Agricultural:
                    BuildAndScrapBuildings(Budget);
                    AssignOtherWorldsWorkers(1, 0.333f, Storage.Max - Storage.Food , 0);
                    DetermineFoodState(0.1f, 0.2f);  // Start Importing if food drops below 10%, export at 20% and above.
                    DetermineProdState(0.25f, 0.99f); // Start Importing if prod drops below 25%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.
                    break;
                case ColonyType.Military:
                    BuildAndScrapBuildings(Budget);
                    AssignOtherWorldsWorkers(0.3f, 0.7f, 0, 1.5f);
                    DetermineFoodState(0.75f, 1f); // Import if either drops below 75%, and stop importing once stores reach 95%.
                    DetermineProdState(0.75f, 0.99f); // This planet will only export Food or Prod due to excess FlatFood or FlatProd
                    break;
            }

            BuildPlatformsAndStations(Budget);
        }

        void UpdateBiospheresBeingBuilt()
        {
            BiosphereInTheWorks = BuildingInQueue(Building.BiospheresId);
        }

        public float CivilianBuildingsMaintenance  => Money.Maintenance - GroundDefMaintenance;

        public float GetColonyDebtTolerance()
        {
            if (Owner == null || GovernorOff || MaxPopBillionNoBuildingBonus >= 5f || PopulationBillion > 5f)
                return 0;

            float ratio = 0.1f * (5 - PopulationBillion); // bigger pop = less tolerance - between 0 and 0.5
            float fertilityBonus = IsCybernetic ? 0 : MaxFertility;
            float richnessBonus = IsCybernetic ? MineralRichness : MineralRichness * 0.5f;
            switch (CType)
            {
                case ColonyType.Agricultural: fertilityBonus *= 1.5f; break;
                case ColonyType.Industrial:   richnessBonus  *= 1.5f; break;
                case ColonyType.Core:         richnessBonus  *= 1.25f; fertilityBonus *= 1.25f; break;
            }

            float totalTolerancePerTile = (richnessBonus + fertilityBonus) * ratio;
            float lowMoneyRatio = (Owner.Money * 0.0005f).Clamped(0, 1); // money / 2000
            float total = totalTolerancePerTile * NumHabitableTiles * lowMoneyRatio; // Biospheres will increase tolerance
            return total.LowerBound(0);
        }

        //New Build Logic by Gretman, modified by Fat Bastard
        void BuildAndScrapBuildings(PlanetBudget colonyBudget)
        {
            if (OwnerIsPlayer && SpecializedTradeHub)
                return;

            BuildAndScrapCivilianBuildings(colonyBudget.RemainingCivilian);
            BuildAndScrapMilitaryBuildings(colonyBudget.RemainingGroundDef);
        }

        // returns the amount of production to spend in the build queue based on import/export state
        public float LimitedProductionExpenditure(float availableProductionToQueue)
        {
            float prodToSpend;
            float prodIncome = Prod.NetIncome > 0 ? Prod.NetIncome : InfraStructure.UpperBound(Storage.Prod);
            bool empireCanExport = Owner.TotalProdExportSlots - FreeProdExportSlots > Level.LowerBound(3);
            if (CType == ColonyType.Colony)
            {
                switch (PS)
                {
                    default: // Importing
                        prodToSpend = ProdHere; // we are manually importing, so let's spend it all
                        break;
                    case GoodState.STORE:
                        if (Storage.ProdRatio.AlmostEqual(1))
                            prodToSpend = prodIncome; // Spend all our Income since storage is full
                        else
                            prodToSpend = prodIncome * 0.5f; // Store 50% of our prod income
                        break;
                    case GoodState.EXPORT:
                        if (OutgoingProdFreighters > 0)
                            prodToSpend = prodIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        else
                            prodToSpend = ProdHere * 0.5f; // Spend 50% from our current stores and net production
                        break;
                }
            }
            else // Governor is auto managing good state
            {
                switch (PS)
                {
                    default: // Importing
                        if (!empireCanExport)
                        {
                            prodToSpend = prodIncome * 0.5f; ; 
                            break;
                        }
                        if (IncomingProdFreighters > 0)
                            prodToSpend = ProdHere + prodIncome; // We have incoming prod, so we can spend more now
                        else
                            prodToSpend = prodIncome + Storage.ProdRatio*2; // Spend less since nothing is coming
                        break;
                    case GoodState.STORE:
                        if (empireCanExport)
                        {
                            prodToSpend = prodIncome + 10; // Our empire has open export slots, so we can allow storage to dwindle
                            break;
                        }
                        if (Storage.ProdRatio.AlmostEqual(1))
                            prodToSpend = prodIncome; // Spend all our Income since storage is full
                        else
                            prodToSpend = prodIncome * 0.5f; // Store 50% of our prod income
                        break;
                    case GoodState.EXPORT:
                        if (empireCanExport)
                        {
                            prodToSpend = ProdHere; // Our empire has open export slots, so we can allow storage to dwindle
                            break;
                        }

                        if (Storage.ProdRatio > 0.8f)
                            prodToSpend = prodIncome + Storage.Prod * 0.1f; // We are actively exporting but can afford some storage spending
                        else
                            prodToSpend = prodIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        break;
                }
            }

            if (IsStarving && Construction.FirstItemCanFeedUs())
                prodToSpend = ProdHere;

            // if we have negative NetIncome (cybernetics)  - we try to take amonut of Infra (if available) to continue building the queue
            float upperBound = Prod.NetIncome <= 0 ? prodIncome : availableProductionToQueue;
            return prodToSpend.UpperBound(upperBound);
        }

        void CreateAndOrUpdateBudget()
        {
            if (Budget == null || Owner != Budget.Owner)
                CreatePlanetBudget(Owner);

            Budget.Update();
        }

        public bool BestCivilianBuildingToBuildDifferentThen(IReadOnlyList<Building> buildings, Building queuedBuilding)
        {
            CreateAndOrUpdateBudget();
            Array<Building> updatedBuildings = GetBuildingsListToChooseFrom(buildings).ToArrayList();
            updatedBuildings.Add(queuedBuilding);
            float civilianBudget = Budget.RemainingCivilian + queuedBuilding.ActualMaintenance(this);
            ChooseBestBuilding(updatedBuildings, civilianBudget, replacing: false, out Building bestBuilding);
            return bestBuilding != null && bestBuilding.Name != queuedBuilding.Name;
        }

        IReadOnlyList<Building> GetBuildingsListToChooseFrom(IReadOnlyList<Building> buildingsCanBuild)
        {
            if (!HasBlueprints)
                return buildingsCanBuild;

            if (Blueprints.Exclusive) // build only blueprints buildings, even if nothing can be built
                return Blueprints.PlannedBuildingsWeCanBuild;

            if (Blueprints.PlannedBuildingsWeCanBuild.Count == 0)
                return buildingsCanBuild; // build whatever we can if no blueprints building available
            else 
                return Blueprints.PlannedBuildingsWeCanBuild; // priorizite blueprints buildings
        }

        public void RemoveBlueprints()
        {
            Blueprints = null;
        }

        public void AddBlueprints(BlueprintsTemplate template, Empire owner)
        {
            Blueprints = new ColonyBlueprints(template, this, Owner);
        }

        public void DestroyBuildingInUprise(UpriseBuildingType type, out string buildingNameDestroyed)
        {
            buildingNameDestroyed = "";
            if (type is UpriseBuildingType.None)
                return;

            Building[] potentialBuildings = BuildingList.Filter(b => b.Scrappable);
            if (potentialBuildings.Length == 0)
                return;

            switch (type) 
            {
                case UpriseBuildingType.HighestPrice: 
                    potentialBuildings = potentialBuildings.SortedDescending(b => b.Cost).Take(5).ToArray();
                    break;
                case UpriseBuildingType.Storage:      
                    potentialBuildings = potentialBuildings.SortedDescending(b => b.StorageAdded).Take(5).ToArray();
                    break;
                case UpriseBuildingType.AllMilitary: 
                    for (int i = BuildingList.Count - 1; i >= 0; i--)
                    {
                        Building b = BuildingList[i];
                        if (b.Scrappable && b.IsMilitary)
                        {
                            DestroyBuilding(b);
                            buildingNameDestroyed = $"{Localizer.Token(GameText.UpriseAllMilitaryBuildings)}.";
                        }
                    }

                    if (buildingNameDestroyed.NotEmpty())
                        return;

                    break; // fallback to random buildings
            }

            Building toDestroy = Universe.Random.Item(potentialBuildings);
            buildingNameDestroyed = toDestroy.TranslatedName.Text;
            DestroyBuilding(toDestroy);
        }
    }
}
