#pragma once
#include <src/rpp/vec.h>

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

inline const char* toString(ObjectType type)
{
    switch (type)
    {
        case ObjectType_Any: return "Any";
        case ObjectType_Ship: return "Ship";
        case ObjectType_ShipModule: return "ShipModule";
        case ObjectType_Proj: return "Proj";
        case ObjectType_Beam: return "Beam";
        case ObjectType_Asteroid: return "Asteroid";
        case ObjectType_Moon: return "Moon";
        case ObjectType_All: return "All";
    }
    return "Unknown";
}

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

    float distanceTo(const MyGameObject& b) const
    {
        return (pos - b.pos).length();
    }

    std::string toString() const
    {
        char buf[512];
        int len = snprintf(buf, 512, "object %4d %s loy=%d p=%.2f,%.2f",
                           spatialId, ::toString(type), loyalty, pos.x, pos.y);
        return {buf, buf+len};
    }
};

