#include "ShipDataSerializer.h"
#include "NodeParser.h"

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    static FINLINE void ParsePosition(NodeParser& elem, float& posX, float& posY)
    {
        elem.parseChildren("Position", [&](NodeParser subdefs)
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
        Data = file::read_all(filename);
        if (!Data) return Error("Failed to open ShipData xml");
        try
        {
            xml_document<> doc; doc.parse<parse_fastest>(Data.str);
            xml_node<>* root = doc.first_node("ShipData");
            if (!root) return Error("Invalid ShipData xml: no <ShipData> node found");

            // get a rough estimate of how many ModuleSlotLists we might parse to reduce reallocations
            constexpr int NumCharsPerSlotData = 300;
            const int estimatedSlots = Data.len / NumCharsPerSlotData;
            ModuleSlotList.reserve(estimatedSlots);

            for (NodeParser elem{root}; elem.node; elem.next())
            {
                // try to keep this list in the same order as in the XML files,
                // this will lead to all elements being parsed in one sequence
                elem.parse("Animated"                 , Animated);
                elem.parse("ShipStyle"                , ShipStyle);
                elem.parse("EventOnDeath"             , EventOnDeath);
                elem.parse("experience"               , Experience);
                elem.parse("Level"                    , Level);
                elem.parse("SelectionGraphic"         , SelectionGraphic);
                elem.parse("Name"                     , Name);
                elem.parse("HasFixedCost"             , HasFixedCost);
                elem.parse("FixedCost"                , FixedCost);
                elem.parse("HasFixedUpkeep"           , HasFixedUpkeep);
                elem.parse("FixedUpkeep"              , FixedUpkeep);
                elem.parse("IsShipyard"               , IsShipyard);
                elem.parse("IsOrbitalDefense"         , IsOrbitalDefense);
                elem.parse("IconPath"                 , IconPath);
                elem.parse("CombatState"              , CombatState);
                elem.parse("MechanicalBoardingDefense", MechanicalBoardingDefense);
                elem.parse("Hull"                     , Hull);
                elem.parse("Role"                     , Role);
                elem.parseList("ThrusterList", [this](NodeParser thrusterZone)
                {
                    for (; thrusterZone.node; thrusterZone.next())
                    {
                        ThrusterList.emplace_back();
                        auto& tz = ThrusterList.back();
                        ParsePosition(thrusterZone, tz.X, tz.Y);
                        thrusterZone.parse("scale", tz.Scale);
                    }
                });
                elem.parse("ModelPath"     , ModelPath);
                elem.parse("DefaultAIState", DefaultAIState);
                elem.parse("ShipCategory"  , ShipCategory);
                elem.parse("CarrierShip"   , CarrierShip);
                elem.parse("BaseStrength"  , BaseStrength);
                elem.parse("BaseCanWarp"   , BaseCanWarp);
                elem.parseList("ModuleSlotList", [this](NodeParser slotData)
                {
                    ModuleSlotList.emplace_back();
                    auto& sd = ModuleSlotList.back();

                    for (; slotData.node; slotData.next())
                    {
                        ParsePosition(slotData, sd.PositionX, sd.PositionY);
                        slotData.parse("InstalledModuleUID", sd.InstalledModuleUID);
                        slotData.parse("HangarshipGuid"    , sd.HangarshipGuid);
                        slotData.parse("Health"            , sd.Health);
                        slotData.parse("Shield_Power"      , sd.ShieldPower);
                        slotData.parse("facing"            , sd.Facing);
                        slotData.parse("state"             , sd.State);
                        slotData.parse("Restrictions"      , sd.Restrictions);
                        slotData.parse("SlotOptions"       , sd.SlotOptions);
                    }
                });
                elem.parse("hullUnlockable", HullUnlockable);
                elem.parse("allModulesUnlocakable", AllModulesUnlockable);
                elem.parse("unLockable", UnLockable);
                elem.parseList("techsNeeded", [this](NodeParser subdefs)
                {
                    for (; subdefs.node; subdefs.next())
                        TechsNeeded.push_back(subdefs.value);
                });
                elem.parse("TechScore", TechScore);
            }
            Thrusters      = ThrusterList.data();
            ThrustersLen   = ThrusterList.size();
            ModuleSlots    = ModuleSlotList.data();
            ModuleSlotsLen = ModuleSlotList.size();
            Techs    = TechsNeeded.data();
            TechsLen = TechsNeeded.size();
            return true;
        }
        catch (std::exception e)
        {
            return Error(e.what());
        }
    }

    bool ShipData::Error(const string & err)
    {
        ErrorMessage = ErrorStr = err;
        return false;
    }

    ////////////////////////////////////////////////////////////////////////////////////
    
    extern "C" ShipData* __stdcall CreateShipDataParser(const wchar_t * filename)
    {
        ShipData* data = new ShipData();
        data->LoadFromFile(filename);
        return data;
    }

    extern "C" void __stdcall DisposeShipDataParser(ShipData* data)
    {
        delete data;
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
