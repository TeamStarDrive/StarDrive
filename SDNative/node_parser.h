#pragma once
#pragma managed(push, off)
#include "rapidxml/rapidxml.hpp"
#include "util/strview.h"

namespace SDNative
{
    using namespace rpp;
    using namespace rapidxml;

    struct node_parser
    {
        xml_node<>* node;
        strview name, value;
        FINLINE node_parser(xml_node<>* parentNode)
        {
            if (parentNode && (node = parentNode->m_first_node)) {
                name  = { node->m_name,  node->m_name_size };
                value = { node->m_value, node->m_value_size };
            }
        }
        FINLINE node_parser(const node_parser& parser) : node_parser(parser.node) { }
        FINLINE void next()
        {
            if (node && (node = node->m_next_sibling)) {
                name  = { node->m_name,  node->m_name_size };
                value = { node->m_value, node->m_value_size };
                return;
            }
            name.clear(), value.clear();
        }
        template<int N, class T> FINLINE void parse(const char (&expectedName)[N], T& outData)
        {
            if (name.len == N - 1 && memcmp(name.str, expectedName, N - 1) == 0) {
                parseValue(outData);
                next();
            }
        }
        // usage:
        // parseSubdefs("ModuleSlotList", [](node_parser slotData) {
        //     for (; slotData.node; slotData.next()) {
        //         ...
        //     }
        // });
        template<int N, class Func>
        FINLINE void parseList(const char (&expectedName)[N], Func&& parseSubdefs)
        {
            if (name.len == N - 1 && memcmp(name.str, expectedName, N - 1) == 0)
            {
                for (node_parser list(node); list.node; list.next()) {
                    parseSubdefs(list.node);
                }
                next();
            }
        }
        template<int N, class Func>
        FINLINE void parseChildren(const char(&expectedName)[N], Func&& parseSubdefs)
        {
            if (name.len == N - 1 && memcmp(name.str, expectedName, N - 1) == 0)
            {
                parseSubdefs(node);
                next();
            }
        }
        FINLINE void parseValue(bool& out)     { out = value.to_bool();       }
        FINLINE void parseValue(byte& out)     { out = (byte)value.to_int();  }
        FINLINE void parseValue(short& out)    { out = (short)value.to_int(); }
        FINLINE void parseValue(int& out)      { out = value.to_int();        }
        FINLINE void parseValue(float& out)    { out = value.to_float();      }
        FINLINE void parseValue(strview& out)  { 
            if (value.str && value.len) out = value;
        }
    };
}
#pragma managed(pop)
