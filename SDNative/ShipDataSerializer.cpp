#include "ShipDataSerializer.h"
#include "node_parser.h"

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

#pragma managed(push, off)
    static FINLINE void parse_position(node_parser& elem, float& posX, float& posY)
    {
        elem.parseChildren("Position", [&](node_parser subdefs)
        {
            for (; subdefs.node; subdefs.next()) {
                subdefs.parse("X", posX);
                subdefs.parse("Y", posY);
            }
        });
    }

    bool ShipData::LoadFromFile(const wchar_t* filename)
    {
        using namespace rpp;
        data = file::read_all(filename);
        if (!data) return Error("Failed to open ShipData xml");
        try
        {
            using namespace rapidxml;
            xml_document<> doc; doc.parse<parse_fastest>(data.str);
            xml_node<>* root = doc.first_node("ShipData");
            if (!root) return Error("Invalid ShipData xml: no <ShipData> node found");

            // get a rough estimate of how many ModuleSlotLists we might parse to reduce reallocations
            constexpr int NumCharsPerSlotData = 300;
            const int estimatedSlots = data.len / NumCharsPerSlotData;
            ModuleSlotList.reserve(estimatedSlots);

            for (node_parser elem(root); elem.node; elem.next())
            {
                // try to keep this list in the same order as in the XML files,
                // this will lead to all elements being parsed in one sequence
                elem.parse("Animated",      Animated);
                elem.parse("ShipStyle",     ShipStyle);
                elem.parse("EventOnDeath",  EventOnDeath);
                elem.parse("experience",    Experience);
                elem.parse("Level",         Level);
                elem.parse("SelectionGraphic",SelectionGraphic);
                elem.parse("Name",          Name);
                elem.parse("HasFixedCost",  HasFixedCost);
                elem.parse("FixedCost",     FixedCost);
                elem.parse("HasFixedUpkeep",HasFixedUpkeep);
                elem.parse("FixedUpkeep",   FixedUpkeep);
                elem.parse("IsShipyard",    IsShipyard);
                elem.parse("IsOrbitalDefense", IsOrbitalDefense);
                elem.parse("IconPath",      IconPath);
                elem.parse("CombatState",   CombatState);
                elem.parse("MechanicalBoardingDefense", MechanicalBoardingDefense);
                elem.parse("Hull",          Hull);
                elem.parse("Role",          Role);
                elem.parseList("ThrusterList", [this](node_parser thrusterZone)
                {
                    for (; thrusterZone.node; thrusterZone.next())
                    {
                        ThrusterList.emplace_back();
                        auto& tz = ThrusterList.back();
                        parse_position(thrusterZone, tz.X, tz.Y);
                        thrusterZone.parse("scale", tz.Scale);
                    }
                });
                elem.parse("ModelPath", ModelPath);
                elem.parse("DefaultAIState",DefaultAIState);
                elem.parse("ShipCategory", ShipCategory);
                elem.parse("CarrierShip", CarrierShip);
                elem.parse("BaseStrength", BaseStrength);
                elem.parse("BaseCanWarp", BaseCanWarp);
                elem.parseList("ModuleSlotList", [this](node_parser slotData)
                {
                    for (; slotData.node; slotData.next())
                    {
                        ModuleSlotList.emplace_back();
                        auto& sd = ModuleSlotList.back();

                        parse_position(slotData, sd.PositionX, sd.PositionY);
                        slotData.parse("InstalledModuleUID", sd.InstalledModuleUID);
                        slotData.parse("HangarshipGuid", sd.HangarshipGuid);
                        slotData.parse("Health", sd.Health);
                        slotData.parse("Shield_Power", sd.ShieldPower);
                        slotData.parse("facing", sd.InstalledModuleUID);
                        slotData.parse("state", sd.State);
                        slotData.parse("Restrictions", sd.Restrictions);
                        slotData.parse("SlotOptions", sd.SlotOptions);
                    }
                });
                elem.parse("hullUnlockable", HullUnlockable);
                elem.parse("allModulesUnlocakable", AllModulesUnlocakable);
                elem.parse("unLockable", UnLockable);
                elem.parseList("techsNeeded", [this](node_parser subdefs)
                {
                    for (; subdefs.node; subdefs.next())
                    {
                        TechsNeeded.push_back(subdefs.value);
                    }
                });
                elem.parse("TechScore", TechScore);
            }
            return true;
        }
        catch (exception e)
        {
            return Error(e.what());
        }
    }

    bool ShipData::Error(const rpp::strview& err)
    {
        ErrorMessage = err;
        return false;
    }
#pragma managed(pop)

    ////////////////////////////////////////////////////////////////////////////////////

    ShipDataSerializer::ShipDataSerializer()
    {
    }

    ShipDataSerializer::~ShipDataSerializer()
    {
        delete data;
    }

    using namespace System::Runtime::InteropServices;

    bool ShipDataSerializer::LoadFromFile(String^ filename)
    {
        IntPtr uFilePtr = Marshal::StringToHGlobalUni(filename);
        bool ok = data->LoadFromFile((wchar_t*)uFilePtr.ToPointer());

        Marshal::FreeHGlobal(uFilePtr);
        return ok;
    }

    static ThrusterZone ToManaged(const _ThrusterZone& native)
    {
        auto m = ThrusterZone();
        m.X = native.X;
        m.Y = native.Y;
        m.Scale = native.Scale;
        return m;
    }
    static ModuleSlotData^ ToManaged(const _ModuleSlotData& native)
    {
        auto m = gcnew ModuleSlotData();
        m->PositionX   = native.PositionX;
        m->PositionY   = native.PositionY;
        m->Health      = native.Health;
        m->ShieldPower = native.ShieldPower;
        m->Facing      = native.Facing;
        m->InstalledModuleUID = ToStr(native.InstalledModuleUID);
        m->HangarshipGuid = native.HangarshipGuid ? Guid(ToStr(native.HangarshipGuid)) : Guid::Empty;
        m->State          = ToStr(native.State);
        m->Restrictions   = ToStr(native.Restrictions);
        m->SlotOptions    = ToStr(native.SlotOptions);
        return m;
    }
    static String^ ToManaged(const rpp::strview& native)
    {
        return ToStr(native);
    }

    template<class T, class U> static array<T>^ VectorToArray(const std::vector<U>& v) {
        int i = 0;
        auto arr = gcnew array<T>(v.size());
        for (auto& native : v)
            arr[i++] = ToManaged(native);
        return arr;
    }
    array<ThrusterZone>^   ShipDataSerializer::GetThrusterZones()   { return VectorToArray<ThrusterZone>(data->ThrusterList); }
    array<ModuleSlotData^>^ ShipDataSerializer::GetModuleSlotList() { return VectorToArray<ModuleSlotData^>(data->ModuleSlotList); }
    array<String^>^         ShipDataSerializer::GetTechsNeeded()    { return VectorToArray<String^>(data->TechsNeeded); }

    ////////////////////////////////////////////////////////////////////////////////////
}
