using Ship_Game.AI;
using Ship_Game.Ships;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;

namespace Ship_Game;

public partial class Planet
{
    [StarData] public bool BiosphereInTheWorks { get; private set; }
    Array<Building>GetBuildingsWeCanBuildHere()
    {
        Array<Building> canBuild = [];
        if (Owner == null)
            return canBuild;

        // See if it already has a command building or not.
        bool needCommandBuilding = BuildingList.All(b => !b.IsCapitalOrOutpost);

        foreach (Building b in Owner.GetUnlockedBuildings())
        {
            // Skip adding + food buildings for cybernetic races
            if (IsCybernetic && !b.ProducesProduction && !b.ProducesResearch && b.ProducesFood)
                continue;
            // Skip adding command buildings if planet already has one
            if (!needCommandBuilding && b.IsCapitalOrOutpost)
                continue;
            // Make sure the building isn't already built on this planet
            if (b.Unique && BuildingBuiltOrQueued(b))
                continue;
            // Hide Biospheres if the entire planet is already habitable
            if (b.IsBiospheres && AllTilesHabitable())
                continue;
            // If this is a one-per-empire building, make sure it hasn't been built already elsewhere
            // Reusing fountIt bool from above
            if (b.BuildOnlyOnce && IsBuiltOrQueuedWithinEmpire(b))
                continue;
            // Terraformer Limit check
            if (b.IsTerraformer && (!Terraformable || TerraformersHere + ConstructionQueue.Count(i => i.isBuilding && i.Building.IsTerraformer) >= TerraformerLimit))
                continue;
            // If the building is still a candidate after all that, then add it to the list!
            canBuild.Add(b);
        }

        return canBuild;
    }

    public void RefreshBuildingsWeCanBuildHere()
    {
        BuildingsCanBuild = GetBuildingsWeCanBuildHere();
        Blueprints?.RefreshPlannedBuildingsWeCanBuild(BuildingsCanBuild);
    }

    public bool IsBuiltOrQueuedWithinEmpire(Building b)
    {
        // Check for this unique building across the empire
        foreach (Planet planet in Owner.GetPlanets())
            if (planet.BuildingBuiltOrQueued(b))
                return true;
        return false;
    }

    bool AllTilesHabitable()
    {
        return TilesList.All(tile => tile.Habitable);
    }
        
    public bool MilitaryBuildingInTheWorks => ConstructionQueue.Any(qi => qi.IsMilitary);
    public bool CivilianBuildingInTheWorks => ConstructionQueue.Any(qi => qi.IsCivilianBuilding && !qi.IsTerraformer);

    public bool CanBuildInfantry         => HasBuilding(b => b.AllowInfantry);
    public bool TroopsInTheWorks         => ConstructionQueue.Any(t => t.isTroop);
    public bool OrbitalsInTheWorks       => ConstructionQueue.Any(b => b.isOrbital || b.ShipData?.IsShipyard == true);
    public int NumTroopsInTheWorks       => ConstructionQueue.Count(t => t.isTroop);
    public bool SwarmSatInTheWorks       => ConstructionQueue.Any(qi => qi.isShip && qi.ShipData.IsDysonSwarmSat);

    public bool TerraformerInTheWorks    => BuildingInQueue(Building.TerraformerId);
    public bool BuildingBuilt(int bid)   => HasBuilding(existing => existing.BID == bid);
    public bool BuildingInQueue(int bid) => ConstructionQueue.Any(q => q.isBuilding && q.Building.BID == bid);

    public bool BuildingsHereCanBeBuiltAnywhere  => !HasBuilding(b => !b.CanBuildAnywhere);
    public bool PlayerAddedFirstConstructionItem => ConstructionQueue.Count > 0 && ConstructionQueue[0].IsPlayerAdded;

    // exists on planet OR in queue
    public bool BuildingBuiltOrQueued(Building b) => BuildingBuilt(b.BID) || BuildingInQueue(b.BID);
    public bool BuildingBuiltOrQueued(int bid) => BuildingBuilt(bid) || BuildingInQueue(bid);

    int TurnsUntilQueueCompleted(float priority, Array<QueueItem> newItems = null)
    {
        if (newItems == null && ConstructionQueue.Count == 0)
            return 0;

        // Turns to use all stored production with just infra, if exporting, expect only 50% will remain
        float turnsWithInfra = (ExportProd ? ProdHere/2 : ProdHere) / InfraStructure.LowerBound(0.01f);
        // Modify the number of turns that can use all production.
        turnsWithInfra *= priority.Clamped(0, 1);
        // Percentage of the pop allocated to production
        float workPercentage = IsCybernetic ? 1 : 1 - Food.GetAveragePercent();

        // Getting the queue copied and inserting the new items into it to check time to finish
        // This is needed since the planet has dynamic production allocation based on queue items
        var modifiedQueue = ConstructionQueue.ToArrayList();
        if (newItems != null)
            modifiedQueue.AddRange(newItems);

        float totalTurnsToCompleteQueue = 0;
        for (int i = 0; i < modifiedQueue.Count; i++)
        {
            QueueItem qi = modifiedQueue[i];
            // How much production will be created for this item (since some will be diverted to research)
            float productionOutputForItem = Prod.FlatBonus + workPercentage * EvaluateProductionQueue(qi) * PopulationBillion;
            // How much net production will be applied to the queue item after checking planet's trade state
            float netProdPerTurn = LimitedProductionExpenditure(turnsWithInfra <= 0 ? productionOutputForItem 
                : productionOutputForItem + InfraStructure);

            float turnsToCompleteItem = qi.ProductionNeeded / netProdPerTurn.LowerBound(0.01f);
            // Reduce the turns with infra by the turns needed to complete the item so it can be better evaluated next qi
            // We are ignoring excess turns without infra for simplicity
            turnsWithInfra            -= turnsToCompleteItem;
            totalTurnsToCompleteQueue += turnsToCompleteItem;
        }

        return (int)(totalTurnsToCompleteQueue * ModifiedTotalTurnsToComplete());

        // This is to consider food production flactuations
        float ModifiedTotalTurnsToComplete()
        {
            if (IsCybernetic || SpecializedTradeHub)   
                return 1;

            switch (CType)
            {
                default:                      
                case ColonyType.Colony:
                case ColonyType.TradeHub:
                case ColonyType.Industrial:   return 1f;
                case ColonyType.Military:     return 1.25f;
                case ColonyType.Core:         return 1.5f;
                case ColonyType.Research:     return 2f;
                case ColonyType.Agricultural: return 2.5f;
            }
        }
    }

    // @return Total numbers before ship will be finished if
    // inserted to the end of the queue.
    // if sData is null, then we want a troop
    public int TurnsUntilQueueComplete(float cost, float priority, IShipDesign sData = null)
    {
        bool forTroop = sData == null;
        if (forTroop && !HasSpacePort
            || forTroop && (!CanBuildInfantry || ConstructionQueue.Count(q => q.isTroop) >= 2))
        {
            return 9999;
        }

        int total = TurnsUntilQueueCompleted(priority, CreateItemsForTurnsCompleted(CreateQi())); // FB - this is just an estimation
        return total.UpperBound(9999);

        // Local Method
        QueueItem CreateQi()
        {
            var qi = new QueueItem(this)
            {
                isShip  = !forTroop,
                ShipData = sData,
                isTroop = forTroop,
                Cost    = forTroop ? cost : cost * ShipCostModifier,
            };

            return qi;
        }
    }

    // This creates items based on the new item we want to check completion and
    // Adding all refit goals as a new items to calculate these as well
    Array<QueueItem> CreateItemsForTurnsCompleted(QueueItem newItem)
    {
        var items = new Array<QueueItem>();
        if (TryGetQueueItemsFromRefitGoals(out Array<QueueItem> refitItems))
            items.AddRange(refitItems);

        items.Add(newItem);
        return items;

        // Local Method
        bool TryGetQueueItemsFromRefitGoals(out Array<QueueItem> refitQueue)
        {
            refitQueue = new();
            var refitGoals = Owner.AI.FindGoals(g => g.IsRefitGoalAtPlanet(this));
            if (refitGoals.Length == 0)
                return false;

            for (int i = 0; i < refitGoals.Length; i++)
            {
                Goal goal = refitGoals[i];
                if (goal.ToBuild != null)
                {
                    if (goal.OldShip != null && goal.ToBuild != null)
                    {
                        var qi = new QueueItem(this)
                        {
                            isShip = true,
                            Cost   = goal.OldShip.RefitCost(goal.ToBuild) * ShipCostModifier,
                            ShipData = goal.ToBuild
                        };
                        refitQueue.Add(qi);
                    }
                }
            }

            return refitQueue.Count > 0;
        }
    }

    public float TotalProdNeededInQueue()
    {
        return ConstructionQueue.Sum(qi => qi.ProductionNeeded);
    }

    public float MissingProdHereForScrap(Goal[] scrapGoals)
    {
        float effectiveProd         = ProdHere + IncomingProd;
        if (scrapGoals.Length > 0)
        {
            var scrapGoalsTargetingThis = scrapGoals.Filter(g => g is ScrapShip && g.PlanetBuildingAt == this);
            if (scrapGoalsTargetingThis.Length > 0)
                effectiveProd += scrapGoalsTargetingThis.Sum(g => g.OldShip?.GetScrapCost() ?? 0);
        }

        return Storage.Max - effectiveProd; // Negative means we have excess prod
    }

    public bool IsColonyShipInQueue(Goal g) => ConstructionQueue.Any(qi => qi.isShip && qi.Goal == g);

    public bool IsColonyShipInQueue(Goal g, out int queueIndex)
    {
        for (queueIndex = 0; queueIndex < ConstructionQueue.Count; queueIndex++) 
        {
            QueueItem qi = ConstructionQueue[queueIndex];
            if (qi.isShip && qi.Goal == g)
                return true;
        }

        return false;
    }
        

    public Ship FirstShipRoleInQueue(RoleName role)
    {
        foreach (var s in ConstructionQueue)
        {
            if (s.isShip)
            {
                var ship = ResourceManager.GetShipTemplate(s.ShipData.Name);
                if (ship.DesignRole == role)
                    return ship;
            }

        }
        return null;
    }

    /// <summary>
    ///  This will not Destroy Volcanoes. Use static Volcano.RemoveVolcano if you want to remove a Volcano
    /// </summary>
    public void DestroyTile(PlanetGridSquare tile) => DestroyBioSpheres(tile); // since it does the same as DestroyBioSpheres

    public void DestroyBioSpheres(PlanetGridSquare tile, bool destroyBuilding = true)
    {
        if (!tile.VolcanoHere && destroyBuilding)
            DestroyBuildingOn(tile);

        tile.SetHabitable(false);
        if (tile.QItem is { isBuilding: true } && !tile.QItem.Building.CanBuildAnywhere)
            Construction.Cancel(tile.QItem);

        if (tile.Biosphere)
            ClearBioSpheresFromList(tile);
        else
            tile.Terraformable = Random.RollDice(50);

        UpdateMaxPopulation();
    }

    public void ScrapBuilding(Building b, PlanetGridSquare tile = null)
    {
        RemoveBuildingFromPlanet(b, tile, refund:true);
        ProdHere += b.ActualCost(Owner) / 2f;
    }

    public void DestroyBuildingOn(PlanetGridSquare tile) => RemoveBuildingFromPlanet(null, tile, refund:false);
    public void DestroyBuilding(Building b) => RemoveBuildingFromPlanet(b, null, refund:false);

    // TODO: actually remove Biospheres properly
    void ClearBioSpheresFromList(PlanetGridSquare tile)
    {
        tile.Biosphere = false;

        var biosphere = FindBuilding(b => b.IsBiospheres);
        if (biosphere != null)
            BuildingList.Remove(biosphere);

        UpdatePlanetStatsFromRemovedBuilding(biosphere);
    }

    public void VerifyQueueBuildingsCanBePlacedAt(PlanetGridSquare tile)
    {
        for (int i = ConstructionQueue.Count - 1; i >= 0; i--)
        {
            QueueItem qi = ConstructionQueue[i];
            if (qi.isBuilding 
                && qi.pgs != null 
                && (!qi.pgs.BuildingOnTile || qi.Building.Name != qi.pgs.Building.Name)
                && !qi.pgs.CanPlaceBuildingHere(qi.Building))
            {
                Construction.Cancel(qi);
                return;
            }
        }
    }

    /// <summary>
    /// Places a building at `tile`. This should be the only way to add a building to the planet!
    /// </summary>
    public void PlaceBuildingAt(Building b)
    {
        if (!BuildingList.AddUniqueRef(b))
        {
            Log.Error("Building already exists and cannot be placed again");
            return;
        }

        BuildingsFertility += b.MaxFertilityOnBuild;
        MineralRichness += b.IncreaseRichness.LowerBound(0); // This must be positive. since richness cannot go below 0.

        if (b.IsCapital)
            RemoveOutpost();

        if (b.ProdCache > 0)
            b.ProdCache = (int)Random.Float(b.ProdCache * 0.5f, b.ProdCache * 1.5f);

        if (b.FoodCache > 0)
            b.FoodCache = (int)Random.Float(b.FoodCache * 0.5f, b.FoodCache * 1.5f);

        UpdatePlanetStatsFromPlacedBuilding(b);

        // TODO: call some kind of event manager instead of dealing with screenmanager here
        if (b.EventOnBuild != null && OwnerIsPlayer)
        {
            UniverseScreen u = Universe.Screen;
            ExplorationEvent e = ResourceManager.Event(b.EventOnBuild);
            u.ScreenManager.AddScreen(new EventPopup(u, u.Player, e, e.PotentialOutcomes[0], true, this));
        }
    }
    
    void RemoveBuildingFromPlanet(Building b, PlanetGridSquare tile, bool refund)
    {
        b ??= tile?.Building;
        if (b == null)
            return;

        // only trigger removal effects once -- we shouldn't touch
        // TilesList unless the building was actually in the BuildingList
        if (BuildingList.Remove(b))
        {
            tile ??= TilesList.Find(t => t.Building == b);
            if (tile != null)
            {
                tile.ClearBuilding();
                // TODO: we need a better cleanup of planetary tiles,
                //       current system with CrashSites and Volcanoes is too fragile
                tile.CrashSite = null;
            }
            else
            {
                Log.Error("Failed to find tile with building");
            }

            if (refund)
                Owner?.RefundCreditsPostRemoval(b);

            UpdatePlanetStatsFromRemovedBuilding(b);
        }
    }

    // this should update planet stats based on buildings
    // try to avoid Adding to integer stats here, and instead recalculate the value
    void UpdatePlanetStatsFromPlacedBuilding(Building b)
    {
        HasSpacePort |= b.IsSpacePort || b.AllowShipBuilding;
        // this is automatically unset
        HasLimitedResourceBuilding |= (b.ProdCache > 0 && (b.PlusProdPerColonist > 0 || b.PlusFlatProductionAmount > 0 || b.PlusProdPerRichness > 0))
                                   || (b.FoodCache > 0 && (b.PlusFlatFoodAmount > 0 || b.PlusFoodPerColonist > 0));

        UpdatePlanetStatsByRecalculation();

        TerraformingHere |= b.IsTerraformer || b.IsEventTerraformer;

        HasCapital |= b.IsCapital;
        HasOutpost |= b.IsOutpost;
        HasAnomaly |= b.EventHere;
        HasDynamicBuildings |= b.IsDynamicUpdate;
        AllowInfantry |= b.AllowInfantry;
        HasWinBuilding |= b.WinsGame;

        if (b.SensorRange > 0 && Owner != null)
            Owner.ForceUpdateSensorRadiuses = true;

        b.UpdateOffense(this);
        Blueprints?.UpdateCompletion();
    }

    void UpdatePlanetStatsFromRemovedBuilding(Building b)
    {
        if (b.IsSpacePort || b.AllowShipBuilding)
            HasSpacePort = HasBuilding(bb => bb.IsSpacePort || bb.AllowShipBuilding);

        UpdatePlanetStatsByRecalculation();

        if (b.IsTerraformer || b.IsEventTerraformer)
            TerraformingHere = HasBuilding(bb => bb.IsTerraformer || bb.IsEventTerraformer);
        
        // FB - no terraformers present, terraform effort halted
        if (b.IsTerraformer && !TerraformingHere)
            TerraformPoints = 0;

        if (b.IsCapital) HasCapital = false;
        if (b.IsOutpost) HasOutpost = false;
        if (b.EventHere) HasAnomaly = HasBuilding(bb => bb.EventHere);
        if (b.IsDynamicUpdate) HasDynamicBuildings = HasBuilding(bb => bb.IsDynamicUpdate);
        if (b.AllowInfantry) AllowInfantry = HasBuilding(bb => bb.AllowInfantry);

        if (b.SensorRange > 0 && Owner != null)
            Owner.ForceUpdateSensorRadiuses = true;

        // FB - we are reversing MaxFertilityOnBuild when scrapping even bad
        // environment buildings can be scrapped and the planet will slowly recover
        BuildingsFertility -= b.MaxFertilityOnBuild;
        MineralRichness = (MineralRichness - b.IncreaseRichness).LowerBound(0);
        Blueprints?.UpdateCompletion();
    }

    // path where full recalculation is done
    public void UpdatePlanetStatsByRecalculation()
    {
        TotalBuildings = TilesList.Count(tile => tile.BuildingOnTile);
        Storage.Max = SumBuildings(bb => bb.StorageAdded).Clamped(10, 10000000);
        TerraformersHere = CountBuildings(bb => bb.IsTerraformer || bb.IsEventTerraformer);

        TotalInvadeInjure = SumBuildings(b => b.InvadeInjurePoints);
        BuildingGeodeticOffense = SumBuildings(b => b.Offense);

        HabitablePercentage = (float)TilesList.Count(tile => tile.Habitable) / TileArea;

        FreeHabitableTiles = TilesList.Count(tile => tile.Habitable && tile.NoBuildingOnTile);
        TotalHabitableTiles = TilesList.Count(tile => tile.Habitable);
        HabiableBuiltCoverage = 1 - (float)FreeHabitableTiles / TotalHabitableTiles;
        NumFreeBiospheres = TilesList.Count(t => t.Biosphere && !t.BuildingOnTile);

        TotalMoneyBuildings = TilesList.Count(tile => tile.BuildingOnTile &&  tile.Building.IsMoneyBuilding);
        MoneyBuildingRatio = (float)TotalMoneyBuildings / TotalBuildings;

        float sensorRange = FindMaxBuilding(bb => bb.SensorRange)?.SensorRange ?? 0;
        SensorRange = sensorRange.LowerBound(Radius + 10_000) * (Owner?.data.SensorModifier ?? 1);

        float projectorRange = FindMaxBuilding(bb => bb.ProjectorRange)?.ProjectorRange ?? 0;
        ProjectorRange = projectorRange + Radius;

        InfraStructure = SumBuildings(bb => bb.Infrastructure).LowerBound(1);

        // Larger planets take more time to terraform, visa versa for smaller ones
        TerraformToAdd = SumBuildings(bb => bb.PlusTerraformPoints) / Scale;

        PlusFlatPopulationPerTurn = SumBuildings(bb => bb.PlusFlatPopulation);

        SetShieldStrengthMax(SumBuildings(bb => bb.PlanetaryShieldStrengthAdded) * (1 + (Owner?.data.ShieldPowerMod ?? 0)));
        ChangeCurrentplanetaryShield(0); // done for clamping the shields
        TotalRepair = SumBuildings(bb => bb.ShipRepair).LowerBound(0);
    }
}
