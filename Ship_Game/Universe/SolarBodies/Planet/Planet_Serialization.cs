using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public partial class Planet
    {
        Planet() : base(0, GameObjectType.Planet)
        {
            TroopManager    = new TroopManager(this);
            GeodeticManager = new GeodeticManager(this);
            Money = new ColonyMoney(this);
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            UpdatePositionOnly();

            SetExploredBy(data.ExploredBy);

            SensorRange = data.SensorRange;
            if (data.Owner.NotEmpty())
                SetOwner(EmpireManager.GetEmpireByName(data.Owner));

            if (data.SpecialDescription.NotEmpty())
                SpecialDescription = data.SpecialDescription; 

            var type = ResourceManager.Planets.PlanetOrRandom(data.WhichPlanet); // we revert to random just in case people unload mods
            var scale = data.Scale > 0f ? data.Scale : RandomMath.Float(1f, 2f);
            InitPlanetType(type, scale, fromSave: true);
            colonyType         = data.ColonyType;
            GovOrbitals        = data.GovOrbitals;
            GovGroundDefense   = data.GovGroundDefense;
            AutoBuildTroops    = data.GovMilitia;
            GarrisonSize       = data.GarrisonSize;
            Quarantine         = data.Quarantine;
            ManualOrbitals     = data.ManualOrbitals;
            DontScrapBuildings = data.DontScrapBuildings;
            NumShipyards       = data.NumShipyards;
            FS                 = data.FoodState;
            PS                 = data.ProdState;
            Food.PercentLock   = data.FoodLock;
            Prod.PercentLock   = data.ProdLock;
            Res.PercentLock    = data.ResLock;
            BasePopPerTile     = data.BasePopPerTile;
            BombingIntensity   = data.BombingIntensity;
            
            SetBaseFertility(data.Fertility, data.MaxFertility);
            
            MineralRichness       = data.Richness;
            HasRings              = data.HasRings;
            ShieldStrengthCurrent = data.ShieldStrength;
            CrippledTurns         = data.TurnsCrippled;
            PlanetTilt            = RandomMath.Float(45f, 135f);
            
            UpdateTerraformPoints(data.TerraformPoints);

            BaseFertilityTerraformRatio = data.BaseFertilityTerraformRatio;
            SetWorkerPercentages(data.FarmerPercentage, data.WorkerPercentage, data.ResearcherPercentage);

            SetWantedPlatforms(data.WantedPlatforms);
            SetWantedStations(data.WantedStations);
            SetWantedShipyards(data.WantedShipyards);

            SetManualCivBudget(data.ManualCivilianBudget);
            SetManualGroundDefBudget(data.ManualGrdDefBudget);
            SetManualSpaceDefBudget(data.ManualSpcDefBudget);

            SetHasLimitedResourceBuilding(data.HasLimitedResourcesBuildings);

            SetManualFoodImportSlots(data.ManualFoodImportSlots);
            SetManualProdImportSlots(data.ManualProdImportSlots);
            SetManualColoImportSlots(data.ManualColoImportSlots);
            SetManualFoodExportSlots(data.ManualFoodExportSlots);
            SetManualProdExportSlots(data.ManualProdExportSlots);
            SetManualColoExportSlots(data.ManualColoExportSlots);

            AverageFoodImportTurns = data.AverageFoodImportTurns;
            AverageProdImportTurns = data.AverageProdImportTurns;
            AverageFoodExportTurns = data.AverageFoodExportTurns;
            AverageProdExportTurns = data.AverageProdExportTurns;

            SetHomeworld(data.IsHomeworld);
            if (HasRings)
            {
                // TODO: save RingTilt into PlanetSaveData
                RingTilt = RandomMath.Float(-80f, -45f).ToRadians();
            }
            
            //TODO: I'd rather have these injected already constructed into the Planet constructor but until we unwind the multiple back and forth calls it'll stay here.
            foreach (SavedGame.PGSData tileData in data.PGSList)
            {
                var tile = PlanetGridSquare.FromSaveData(tileData);

                if (tile.Biosphere)
                    BuildingList.Add(ResourceManager.CreateBuilding(this, Building.BiospheresId));

                if (tileData.CrashSiteActive)
                    tile.CrashSite.CrashShip(tileData, this, tile);

                TilesList.Add(tile);
                foreach (Troop t in tileData.TroopsHere)
                {
                    if (!ResourceManager.TroopTypes.Contains(t.Name))
                        continue;
                    var fix = ResourceManager.GetTroopTemplate(t.Name);
                    t.first_frame = fix.first_frame;
                    t.WhichFrame = fix.first_frame;
                    AddTroop(t, tile);
                }

                if (tile.Building == null || tile.CrashSite.Active)
                    continue;

                if (!ResourceManager.GetBuilding(tile.Building.Name, out Building template))
                    continue; // this can happen if savegame contains a building which no longer exists in game files

                tile.SetEventOutcomeNumFromSave(tileData.EventOutcomeNum);
                tile.Building.AssignBuildingId(template.BID);
                tile.Building.Scrappable = template.Scrappable;
                tile.Building.CalcMilitaryStrength(this);
                BuildingList.Add(tile.Building);
                AddBuildingsFertility(tile.Building.MaxFertilityOnBuild);

                if (tileData.VolcanoHere)
                    tile.CreateVolcanoFromSave(tileData, this);
            }

            ResetHasDynamicBuildings();

            foreach (PlanetGridSquare tile in TilesList)
            {
                if (tile.Building != null && !tile.IsCrashSiteActive)
                {
                    if (ResourceManager.GetBuilding(tile.Building.Name, out Building template))
                    {
                        tile.Building.AssignBuildingId(template.BID);
                        tile.Building.Scrappable = template.Scrappable;
                        tile.Building.CalcMilitaryStrength(this);
                    }
                }
            }

            UpdateIncomes();  // must be before restoring commodities since max storage is set here
        }
    }
}
