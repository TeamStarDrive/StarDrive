using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Universe
{
    // TODO: Use these directly, instead of updating GlobalStats which should remain readonly !
    [StarDataType]
    public class UniverseParams
    {
        [StarData] public float MinAcceptableShipWarpRange;
        [StarData] public byte TurnTimer;
        [StarData] public int IconSize;
        [StarData] public bool PreventFederations;
        [StarData] public bool EliminationMode;
        [StarData] public float GravityWellRange;
        [StarData] public float CustomMineralDecay;
        [StarData] public float VolcanicActivity;
        [StarData] public float ShipMaintenanceMultiplier;
        [StarData] public bool AIUsesPlayerDesigns;
        [StarData] public bool UseUpkeepByHullSize;

        [StarData] public bool SuppressOnBuildNotifications;
        [StarData] public bool PlanetScreenHideOwned;
        [StarData] public bool PlanetsScreenHideInhospitable;
        [StarData] public bool ShipListFilterPlayerShipsOnly;
        [StarData] public bool ShipListFilterInFleetsOnly;
        [StarData] public bool ShipListFilterNotInFleets;
        [StarData] public bool DisableInhibitionWarning;
        [StarData] public bool CordrazinePlanetCaptured;
        [StarData] public bool DisableVolcanoWarning;

        public UniverseParams()
        {
            MinAcceptableShipWarpRange = GlobalStats.Settings.MinAcceptableShipWarpRange;
            TurnTimer             = (byte)GlobalStats.TurnTimer;
            IconSize              = GlobalStats.IconSize;
            PreventFederations    = GlobalStats.PreventFederations;
            EliminationMode       = GlobalStats.EliminationMode;
            GravityWellRange      = GlobalStats.Settings.GravityWellRange;
            CustomMineralDecay    = GlobalStats.Settings.CustomMineralDecay;
            VolcanicActivity      = GlobalStats.Settings.VolcanicActivity;
            ShipMaintenanceMultiplier = GlobalStats.Settings.ShipMaintenanceMultiplier;
            AIUsesPlayerDesigns   = GlobalStats.Settings.AIUsesPlayerDesigns;
            UseUpkeepByHullSize   = GlobalStats.UseUpkeepByHullSize;

            SuppressOnBuildNotifications  = GlobalStats.SuppressOnBuildNotifications;
            PlanetScreenHideOwned         = GlobalStats.PlanetScreenHideOwned;
            PlanetsScreenHideInhospitable = GlobalStats.PlanetsScreenHideInhospitable;
            ShipListFilterPlayerShipsOnly = GlobalStats.ShipListFilterPlayerShipsOnly;
            ShipListFilterInFleetsOnly    = GlobalStats.ShipListFilterInFleetsOnly;
            ShipListFilterNotInFleets     = GlobalStats.ShipListFilterNotInFleets;
            DisableInhibitionWarning      = GlobalStats.DisableInhibitionWarning;
            CordrazinePlanetCaptured      = GlobalStats.CordrazinePlanetCaptured;
            DisableVolcanoWarning         = GlobalStats.DisableVolcanoWarning;
        }

        public void UpdateGlobalStats()
        {
            GlobalStats.Settings.GravityWellRange = GravityWellRange;
            GlobalStats.Settings.CustomMineralDecay = CustomMineralDecay;
            GlobalStats.Settings.MinAcceptableShipWarpRange = MinAcceptableShipWarpRange;
            GlobalStats.Settings.ShipMaintenanceMultiplier = ShipMaintenanceMultiplier;
            GlobalStats.PreventFederations = PreventFederations;
            GlobalStats.EliminationMode = EliminationMode;
            GlobalStats.IconSize = IconSize;
            GlobalStats.TurnTimer = TurnTimer != 0 ? TurnTimer : 5;

            GlobalStats.SuppressOnBuildNotifications  = SuppressOnBuildNotifications;
            GlobalStats.PlanetScreenHideOwned         = PlanetScreenHideOwned;
            GlobalStats.PlanetsScreenHideInhospitable = PlanetsScreenHideInhospitable;
            GlobalStats.ShipListFilterPlayerShipsOnly = ShipListFilterPlayerShipsOnly;
            GlobalStats.ShipListFilterInFleetsOnly    = ShipListFilterInFleetsOnly;
            GlobalStats.ShipListFilterNotInFleets     = ShipListFilterNotInFleets;
            GlobalStats.DisableInhibitionWarning      = DisableInhibitionWarning;
            GlobalStats.DisableVolcanoWarning         = DisableVolcanoWarning;
            GlobalStats.CordrazinePlanetCaptured      = CordrazinePlanetCaptured;
            GlobalStats.Settings.AIUsesPlayerDesigns  = AIUsesPlayerDesigns;
            GlobalStats.UseUpkeepByHullSize           = UseUpkeepByHullSize;
        }
    }
}
