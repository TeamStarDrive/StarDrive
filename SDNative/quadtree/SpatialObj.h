#pragma once
#include "QtreeRect.h"
#include <cstdint>

namespace tree
{
    struct SpatialObj
    {
        uint8_t Active;  // 1 if this item is active, 0 if this item is DEAD
        uint8_t Loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        uint8_t Type;    // GameObjectType : byte
        uint8_t Reserved;

        int ObjectId; // handle to the object

        int CX, CY; // Center x y
        int Radius; // radius for collision test

        QtreeRect Bounds; // AABB

        SpatialObj() = default;

        SpatialObj(uint8_t loyalty, uint8_t type, int objectId, 
                   int cx, int cy, int r, const QtreeRect& bounds)
            : Active{1}, Loyalty{loyalty}, Type{type}, Reserved{}, ObjectId{objectId}
            , CX{cx}, CY{cy}, Radius{r}, Bounds{bounds}
        {
        }

        SpatialObj(int cx, int cy, int r)
            : Active{1}, Loyalty{0}, Type{0}, Reserved{}, ObjectId{-1}
            , CX{cx}, CY{cy}, Radius{r}, Bounds{QtreeRect::fromPointRadius(cx, cy, r)}
        {
        }
    };
}