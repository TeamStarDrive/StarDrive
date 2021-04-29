#pragma once
#include <string>
#include <rpp/strview.h>
#include <rpp/file_io.h>

namespace SDNative
{
    using std::string;
    using std::vector;
    using rpp::strview;
    using rpp::load_buffer;
    ////////////////////////////////////////////////////////////////////////////////////

    struct ThrusterZone
    {
        float X, Y, Z, Scale;
    };

    static const strview Empty = "";

    struct ModuleSlotData
    {
        float PositionX;
        float PositionY;
        float Health;
        float ShieldPower;
        float Facing;
        strview InstalledModuleUID = Empty;
        strview HangarshipGuid     = Empty;
        strview State              = Empty;
        strview Restrictions       = Empty;
        strview SlotOptions        = Empty;
    };


    struct ShipData
    {
        strview Name              = Empty;
        strview Hull              = Empty;
        strview ShipStyle         = Empty;
        strview EventOnDeath      = Empty;
        strview SelectionGraphic  = Empty;
        strview IconPath          = Empty;
        strview ModelPath         = Empty;
        strview DefaultAIState    = Empty;
        strview Role              = "fighter";
        strview CombatState       = "AttackRuns";
        strview ShipCategory      = "Unclassified";
        strview HangarDesignation = "General";
		strview ModName           = Empty;
        int      TechScore             = 0;
        float    BaseStrength          = 0.0f;
        float    FixedUpkeep           = 0.0f;
        float    MechanicalBoardingDefense = 0.0f;
        unsigned char Experience       = 0;
        unsigned char Level            = 0;
        short    FixedCost             = 0;
        bool     Animated              = false;
        bool     HasFixedCost          = false;
        bool     HasFixedUpkeep        = false;
        bool     IsShipyard            = false;
        bool     CarrierShip           = false;
        bool     IsOrbitalDefense      = false;
        bool     HullUnlockable        = false;
        bool     UnLockable            = false;
        bool     AllModulesUnlockable  = true;

        // these expose raw pointers to C#, to make data conversion possible
        ThrusterZone* Thrusters     = nullptr;
        int ThrustersLen            = 0;
        ModuleSlotData* ModuleSlots = nullptr;
        int ModuleSlotsLen          = 0;
        strview* Techs              = nullptr;
        int TechsLen                = 0;

        strview ErrorMessage = Empty;

        // and this is our actual data storage, hidden from C#
        vector<ThrusterZone>   ThrusterList;
        vector<ModuleSlotData> ModuleSlotList;
        vector<strview>        TechsNeeded;
        string ErrorStr;
        load_buffer Data;

        bool LoadFromFile(const wchar_t* filename);
        bool Error(string err);
    };

    extern "C" {
        __declspec(dllexport) ShipData* __stdcall CreateShipDataParser(const wchar_t* filename);
        __declspec(dllexport) void __stdcall DisposeShipDataParser(ShipData* data);
    }


    ////////////////////////////////////////////////////////////////////////////////////
}

