using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Read-Only version of ShipDesign
    /// </summary>
    public interface IShipDesign
    {
        string Name { get; } // ex: "Dodaving", just an arbitrary name
        string Hull { get; } // ID of the hull, ex: "Cordrazine/Dodaving"
        string ModName { get; } // "" if vanilla, else mod name eg "Combined Arms"
        string ShipStyle { get; } // "Terran"
        string Description { get; } // "Early Rocket fighter, great against unshielded foes, but die easily"
        string IconPath { get; } // "ShipIcons/shuttle"
        
        string EventOnDeath { get; }
        string SelectionGraphic { get; }

        float FixedUpkeep { get; }
        int FixedCost { get; }
        bool IsShipyard { get; }
        bool IsOrbitalDefense { get; }
        bool IsCarrierOnly { get; } // this ship is restricted to Carriers only

        ShipCategory ShipCategory  { get; }
        HangarOptions HangarDesignation  { get; }
        AIState DefaultAIState { get; }
        CombatState DefaultCombatState { get; }

        ShipGridInfo GridInfo { get; }

        // Complete list of all the unique module UID-s found in this design
        string[] UniqueModuleUIDs { get; }

        bool Unlockable { get; }  // unlocked=true by default
        HashSet<string> TechsNeeded { get; }

        // BaseHull is the template layout of the ship hull design
        ShipHull BaseHull { get; }
        HullBonus Bonuses { get; }
        FileInfo Source { get; }

        bool IsPlayerDesign { get; }
        bool IsReadonlyDesign { get; }
        bool Deleted { get; }

        bool IsValidForCurrentMod { get; }

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        SubTexture Icon { get; }

        // Role assigned to the Hull, such as `Cruiser`
        RoleName HullRole { get; }

        // Role expressed by this ShipDesign's modules, such as `Carrier`
        // This is saved in Shipyard, or can be updated via --fix-roles
        RoleName Role { get; }

        ShipRole ShipRole { get; }

        bool IsPlatformOrStation { get; }
        bool IsStation           { get; }
        bool IsConstructor       { get; }
        bool IsSubspaceProjector { get; }
        bool IsColonyShip        { get; }
        bool IsSupplyCarrier     { get; } // this ship launches supply ships
        bool IsSupplyShuttle     { get; }
        bool IsFreighter         { get; }
        bool IsCandidateForTradingBuild { get; }

        bool IsSingleTroopShip { get; }
        bool IsTroopShip       { get; }
        bool IsBomber          { get; }

        float BaseCost       { get; }
        float BaseWarpThrust { get; }
        bool  BaseCanWarp    { get; }

        // Hangar Templates
        ShipModule[] Hangars { get; }
        ShipModule[] AllFighterHangars { get; }

        // Weapon Templates
        Weapon[] Weapons { get; }

        // All invalid modules in this design
        // If this is not null, this ship cannot be spawned, but can still be listed and loaded in Shipyard
        string InvalidModules { get; }
    }
}
