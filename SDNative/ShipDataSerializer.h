#pragma once
#pragma managed(push, off)
#include "util/file_io.h"
#pragma managed(pop)

namespace SDNative
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Runtime::InteropServices;

    ////////////////////////////////////////////////////////////////////////////////////

#pragma managed(push, off)
    struct _ThrusterZone
    {
        float X, Y, Scale;
    };

    struct _ModuleSlotData
    {
        float PositionX;
        float PositionY;
        float Health;
        float ShieldPower;
        float Facing;
        rpp::strview InstalledModuleUID;
        rpp::strview HangarshipGuid;
        rpp::strview State;
        rpp::strview Restrictions;
        rpp::strview SlotOptions;
    };

    struct ShipData
    {
        rpp::load_buffer data;
        rpp::strview Name;
        rpp::strview Hull;
        rpp::strview ShipStyle;
        rpp::strview EventOnDeath;
        rpp::strview SelectionGraphic;
        rpp::strview IconPath;
        rpp::strview ModelPath;
        rpp::strview DefaultAIState;
        rpp::strview Role         = "fighter";
        rpp::strview CombatState  = "AttackRuns";
        rpp::strview ShipCategory = "Unclassified";
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
        bool     BaseCanWarp           = false;
        bool     IsOrbitalDefense      = false;
        bool     HullUnlockable        = false;
        bool     UnLockable            = false;
        bool     AllModulesUnlocakable = true;
        std::vector<_ThrusterZone>   ThrusterList;
        std::vector<_ModuleSlotData> ModuleSlotList;
        std::vector<rpp::strview>    TechsNeeded;
        std::string ErrorMessage;

        // parser
        bool LoadFromFile(const wchar_t* filename);
        bool Error(const rpp::strview& err);
    };

#pragma managed(pop)

    ////////////////////////////////////////////////////////////////////////////////////

    public value struct ThrusterZone sealed
    {
        float X, Y, Scale;
    };

    public ref class ModuleSlotData sealed
    {
    public:
        float PositionX;
        float PositionY;
        float Health;
        float ShieldPower;
        float Facing;
        String^ InstalledModuleUID;
        Guid HangarshipGuid;
        String^ State;
        String^ Restrictions;
        String^ SlotOptions;
    };

    static inline String^ ToStr(const rpp::strview& s) {
        return s.str && s.len ? gcnew String(s.str, 0, s.len) : String::Empty; 
    }

    public ref class ShipDataSerializer sealed
    {
    public:
        ShipData* data = new ShipData();

        ShipDataSerializer();
        ~ShipDataSerializer();

        bool LoadFromFile(String^ filename);

        array<ThrusterZone>^   GetThrusterZones();
        array<ModuleSlotData^>^ GetModuleSlotList();
        array<String^>^         GetTechsNeeded();

#define PROPERTY_String(Name) property String^ Name { String^ get() { return ToStr(data->Name); } }
        PROPERTY_String(ErrorMessage)
        PROPERTY_String(Name)
        PROPERTY_String(Hull)
        PROPERTY_String(ShipStyle)
        PROPERTY_String(EventOnDeath)
        PROPERTY_String(SelectionGraphic)
        PROPERTY_String(IconPath)
        PROPERTY_String(ModelPath)
        PROPERTY_String(DefaultAIState)
        PROPERTY_String(Role)
        PROPERTY_String(CombatState)
        PROPERTY_String(ShipCategory)
#undef PROPERTY_String

#define PROPERTY(type, Name) property type Name { type get() { return data->Name; } }
        PROPERTY(int, TechScore)
        PROPERTY(float, BaseStrength)
        PROPERTY(float, FixedUpkeep)
        PROPERTY(float, MechanicalBoardingDefense)
        PROPERTY(Byte, Experience)
        PROPERTY(Byte, Level)
        PROPERTY(short, FixedCost)
        PROPERTY(bool, Animated)
        PROPERTY(bool, HasFixedCost)
        PROPERTY(bool, HasFixedUpkeep)
        PROPERTY(bool, IsShipyard)
        PROPERTY(bool, CarrierShip)
        PROPERTY(bool, BaseCanWarp)
        PROPERTY(bool, IsOrbitalDefense)
        PROPERTY(bool, HullUnlockable)
        PROPERTY(bool, UnLockable)
        PROPERTY(bool, AllModulesUnlocakable)
    };

    ////////////////////////////////////////////////////////////////////////////////////
}

