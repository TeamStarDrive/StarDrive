#pragma once
#include "Config.h"

namespace spatial
{
    // compute the next highest power of 2 of 32-bit v
    SPATIAL_FINLINE static int upperPowerOf2(unsigned int v)
    {
        --v;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        ++v;
        return v;
    }

}