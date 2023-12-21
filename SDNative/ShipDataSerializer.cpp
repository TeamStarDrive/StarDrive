#include "ShipDataSerializer.h"
#include "NodeParser.h"
#include <rpp/sprint.h>

namespace SDNative
{
    using rapidxml::xml_document;
    ////////////////////////////////////////////////////////////////////////////////////

    struct LineColumnInfo
    {
        int line = 0, column = 0;

        LineColumnInfo(const load_buffer& data, const char* where)
        {
            rpp::line_parser parser{ data };
            for (strview prevLine, nextLine; ; ++line, prevLine = nextLine)
            {
                if (!parser.read_line(nextLine) || nextLine.str > where) {
                    column = int(where - prevLine.str) + 1;
                    break;
                }
            }
        }
    };

    // make text errors human readable
    static string visualize_bytes(const char* where, int len)
    {
        rpp::string_buffer sb;
        for (int i = 0; i < len; ++i)
        {
            char ch = where[i];
            if (ch == '\r') { sb << "\\r"; break; }
            if (ch == '\n') { sb << "\\n"; break; }
            if (isprint(ch)) sb << ch;
            else            (sb << "\\x").write_hex(ch, rpp::uppercase);
        }
        return sb.str();
    }
    // FB - this is almost a copy of the PasrPostion for ship modules. 
    // I added the Z for Thrusters and figured its better to seperate these as this is called very rarely vs. modules.
    static FINLINE void ParsePositionThruster(NodeParser& elem, float& posX, float& posY, float& posZ)
    {
        elem.parseChildren("Position", [&](NodeParser subdefs)
        {
            for (; subdefs.node; subdefs.next()) {
                subdefs.parse("X", posX);
                subdefs.parse("Y", posY);
                subdefs.parse("Z", posZ);
            }
        });
    }

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
        Data = rpp::file::read_all(filename);
        if (!Data) return Error("Failed to open ShipData xml");
        try
        {
            xml_document<> doc; doc.parse<rapidxml::parse_fastest>(Data.str);
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
                        ParsePositionThruster(thrusterZone, tz.X, tz.Y, tz.Z);
                        thrusterZone.parse("scale", tz.Scale);
                    }
                });
                elem.parse("ModelPath"         , ModelPath);
                elem.parse("ModName"           , ModName);
                elem.parse("DefaultAIState"    , DefaultAIState);
                elem.parse("ShipCategory"      , ShipCategory);
                elem.parse("HangarDesignation" , HangarDesignation);
                elem.parse("CarrierShip"       , CarrierShip);
                elem.parse("BaseStrength"      , BaseStrength);
                elem.parseList("ModuleSlotList", [this](NodeParser slotData)
                {
                    ModuleSlotList.emplace_back();
                    auto& sd = ModuleSlotList.back();

                    for (; slotData.node; slotData.next())
                    {
                        ParsePosition(slotData, sd.PositionX   , sd.PositionY);
                        slotData.parse("InstalledModuleUID"    , sd.InstalledModuleUID);
                        slotData.parse("HangarshipGuid"        , sd.HangarshipGuid);
                        slotData.parse("Health"                , sd.Health);
                        slotData.parse("Shield_Power"          , sd.ShieldPower);
                        slotData.parse("facing"                , sd.Facing);
                        slotData.parse("state"                 , sd.State);
                        slotData.parse("Restrictions"          , sd.Restrictions);
                        slotData.parse("SlotOptions"           , sd.SlotOptions);
                    }
                });
                elem.parse("hullUnlockable", HullUnlockable);
                elem.parse("allModulesUnlocakable", AllModulesUnlockable); // yes, this typo was in the original XML :D now we're stuck with it!
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
        catch (rapidxml::parse_error& e)
        {
            LineColumnInfo info { Data, e.where<char>() };
            return Error(rpp::format("XML Parsing failed: %s at line %d column %d: '%s'", 
                         e.what(), info.line, info.column, visualize_bytes(e.where<char>(), 24)));
        }
        catch (std::exception& e)
        {
            return Error(e.what());
        }
    }

    bool ShipData::Error(string err)
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
