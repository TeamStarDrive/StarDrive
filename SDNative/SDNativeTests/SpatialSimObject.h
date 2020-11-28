#pragma once
#include <rpp/vec.h>

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
    ObjectType_All        = 255, // collide with all types
};

struct MyGameObject
{
    int spatialId = -1;
    rpp::Vector2 pos { 0.0f, 0.0f };
    rpp::Vector2 vel { 0.0f, 0.0f };
    float radius = 0.0f;
    float mass = 1.0f;
    uint8_t loyalty = 0;
    ObjectType type = ObjectType_Any;
    bool collidedThisFrame = false;
};

