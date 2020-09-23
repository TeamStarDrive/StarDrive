#pragma once
#include <spatial/Spatial.h>

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
};
