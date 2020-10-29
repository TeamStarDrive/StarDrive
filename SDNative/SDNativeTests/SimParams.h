#pragma once
#include <spatial/Spatial.h>
#include <rpp/vec.h>

struct SimParams
{
    spatial::SpatialType type {};

    int universeSize = 5'000'000;
    float solarRadius = 100'000;
    int solarSystems = 32;
    int defaultSensorRange = 30'000;

    int numObjects = 10'000;
    float objectRadius = 500;

    int qtreeCellSize = 1024;
    int gridCellSize = 20'000;

    int gridL2CellSize = 80'000;
    int gridL2CellSize2 = 5000;

    bool useRandomVelocity = true;
    rpp::Vector2 singleSystemPos; // if solarSystems == 1
    
    // debug loyalty values
    int loyaltyA = 4;
    int loyaltyB = 7;
};
