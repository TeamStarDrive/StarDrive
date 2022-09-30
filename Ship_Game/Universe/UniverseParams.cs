using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Universe
{
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
        [StarData] public bool UsePlayerDesigns;
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
            MinAcceptableShipWarpRange = GlobalStats.MinAcceptableShipWarpRange;
            TurnTimer             = (byte)GlobalStats.TurnTimer;
            IconSize              = GlobalStats.IconSize;
            PreventFederations    = GlobalStats.PreventFederations;
            EliminationMode       = GlobalStats.EliminationMode;
            GravityWellRange      = GlobalStats.GravityWellRange;
            CustomMineralDecay    = GlobalStats.CustomMineralDecay;
            VolcanicActivity      = GlobalStats.VolcanicActivity;
            ShipMaintenanceMultiplier = GlobalStats.ShipMaintenanceMulti;
            UsePlayerDesigns      = GlobalStats.UsePlayerDesigns;
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
            GlobalStats.GravityWellRange     = GravityWellRange;
            GlobalStats.IconSize             = IconSize;
            GlobalStats.MinAcceptableShipWarpRange = MinAcceptableShipWarpRange;
            GlobalStats.ShipMaintenanceMulti = ShipMaintenanceMultiplier;
            GlobalStats.PreventFederations   = PreventFederations;
            GlobalStats.EliminationMode      = EliminationMode;
            GlobalStats.CustomMineralDecay   = CustomMineralDecay;
            GlobalStats.TurnTimer            = TurnTimer != 0 ? TurnTimer : 5;

            GlobalStats.SuppressOnBuildNotifications  = SuppressOnBuildNotifications;
            GlobalStats.PlanetScreenHideOwned         = PlanetScreenHideOwned;
            GlobalStats.PlanetsScreenHideInhospitable = PlanetsScreenHideInhospitable;
            GlobalStats.ShipListFilterPlayerShipsOnly = ShipListFilterPlayerShipsOnly;
            GlobalStats.ShipListFilterInFleetsOnly    = ShipListFilterInFleetsOnly;
            GlobalStats.ShipListFilterNotInFleets     = ShipListFilterNotInFleets;
            GlobalStats.DisableInhibitionWarning      = DisableInhibitionWarning;
            GlobalStats.DisableVolcanoWarning         = DisableVolcanoWarning;
            GlobalStats.CordrazinePlanetCaptured      = CordrazinePlanetCaptured;
            GlobalStats.UsePlayerDesigns              = UsePlayerDesigns;
            GlobalStats.UseUpkeepByHullSize           = UseUpkeepByHullSize;
        }
    }
}
