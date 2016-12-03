#include "ShipDataSerializer.h"
#include "rapidxml/rapidxml.hpp"
using namespace System::Runtime::InteropServices;

namespace SDNative
{

    bool ShipData::LoadFromFile(const wchar_t* filename)
    {
        load_buffer buf = file::read_all(filename);
        if (!buf) {
            ErrorMessage = "File to open ShipData xml";
            return false;
        }

        using namespace rapidxml;
        xml_document<> doc;
        doc.parse<parse_fastest>(buf.str);

        xml_node<>* root = doc.first_node("ShipData");
        if (!root) {
            ErrorMessage = "Invalid ShipData xml: no <ShipData> node found";
            return false;
        }


        return false;
    }


    ShipDataSerializer::ShipDataSerializer()
    {
    }
    ShipDataSerializer::~ShipDataSerializer()
    {
        delete shipData;
    }

    bool ShipDataSerializer::LoadFromFile(String^ filename)
    {
        IntPtr uFilePtr = Marshal::StringToHGlobalUni(filename);
        bool ok = shipData->LoadFromFile((wchar_t*)uFilePtr.ToPointer());

        Marshal::FreeHGlobal(uFilePtr);
        return ok;
    }
}
