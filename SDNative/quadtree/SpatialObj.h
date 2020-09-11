#pragma once
#include <cstdint>

namespace tree
{
    struct SpatialObj
    {
        uint8_t PendingRemove; // 1 if this item is pending removal
        uint8_t Loyalty;       // if loyalty == 0, then this is a STATIC world object !!!
        uint8_t Type;          // GameObjectType : byte
        uint8_t Reserved;

        int ObjectId; // handle to the object

        float CX, CY; // Center x y
        float Radius; // radius for collision test
        float X, Y, LastX, LastY; // bounding box of this spatial obj

        SpatialObj() = default;

        SpatialObj(uint8_t loyalty, uint8_t type, int objectId, 
                   float cx, float cy, float r, float x1, float y1, float x2, float y2)
            : PendingRemove{0}, Loyalty{loyalty}, Type{type}, Reserved{}, ObjectId{objectId}
            , CX{cx}, CY{cy}, Radius{r}, X{x1}, Y{y1}, LastX{x2}, LastY{y2}
        {
        }

        SpatialObj(float cx, float cy, float r)
            : PendingRemove{0}, Loyalty{0}, Type{0}, Reserved{}, ObjectId{-1}
            , CX{cx}, CY{cy}, Radius{r}, X{cx-r}, Y{cy-r}, LastX{cx+r}, LastY{cy+r}
        {
        }
    };
}