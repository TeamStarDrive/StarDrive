#pragma once
#include "util/file_io.h"

namespace SDNative
{
    using namespace std;
    using namespace rpp;
    using namespace System;
    using namespace System::Collections;

    struct ThrusterZone
    {
        float X, Y, Scale;
    };

    struct ModuleSlotData
    {
        float PositionX;
        float PositionY;
        float Health;
        float ShieldPower;
        float Facing;
        strview InstalledModuleUID;
        strview HangarshipGuid;
        strview State;
        strview Restrictions;
        strview SlotOptions;
    };

    struct ShipData
    {
        strview Name;
        strview Hull;
        strview ShipStyle;
        strview EventOnDeath;
        strview SelectionGraphic;
        strview IconPath;
        strview ModelPath;
        strview DefaultAIState;
        strview Role         = "fighter";
        strview CombatState  = "AttackRuns";
        strview ShipCategory = "Unclassified";
        int      TechScore                   = 0;
        float    BaseStrength                = 0.0f;
        float    FixedUpkeep                 = 0.0f;
        float    MechanicalBoardingDefense   = 0.0f;
        unsigned char Experience             = 0;
        unsigned char Level                  = 0;
        bool     Animated                    = false;
        bool     HasFixedCost                = false;
        bool     HasFixedUpkeep              = false;
        bool     IsShipyard                  = false;
        bool     CarrierShip                 = false;
        bool     BaseCanWarp                 = false;
        bool     IsOrbitalDefense            = false;
        bool     HullUnlockable              = false;
        bool     UnLockable                  = false;
        bool     AllModulesUnlocakable       = true;
        vector<ThrusterZone>   ThrusterList;
        vector<ModuleSlotData> ModuleSlotList;
        vector<strview>        TechsNeeded;

        strview ErrorMessage;

        // parser
        bool LoadFromFile(const wchar_t* filename);
    };

    public ref class ShipDataSerializer
    {
    public:
        ShipData* shipData = new ShipData();

        ShipDataSerializer();
        ~ShipDataSerializer();

        static inline String^ ToString(const strview& s)
        {
            return gcnew String(s.str, 0, s.len);
        }

        bool LoadFromFile(String^ filename);
    };
}

