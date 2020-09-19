#pragma once
#include "QtreeRect.h"
#include <cstdint>

namespace tree
{
    enum ObjectType : uint8_t
    {
        // Can be used as a search filter to match all object types
        ObjectType_Any        = 0,
        ObjectType_Ship       = 1,
        ObjectType_ShipModule = 2,
        ObjectType_Proj       = 4, // this is a projectile, NOT a beam
        ObjectType_Beam       = 8, // this is a BEAM, not a projectile
        ObjectType_Asteroid   = 16,
        ObjectType_Moon       = 32,
    };

    struct QtreeObject
    {
        uint8_t active;  // 1 if this item is active, 0 if this item is DEAD
        uint8_t loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        uint8_t type; // GameObjectType : byte
        uint8_t reserved;
        int objectId; // handle to the object

        int x, y; // Center x y
        int rx, ry; // Radius x y

        QtreeObject() = default;

        QtreeObject(uint8_t loyalty, uint8_t type, int objectId,
                    int centerX, int centerY, int radiusX, int radiusY)
            : active{1}, loyalty{loyalty}, type{type}, reserved{}, objectId{objectId}
            , x{centerX}, y{centerY}, rx{radiusX}, ry{radiusY}
        {
        }
    };
}