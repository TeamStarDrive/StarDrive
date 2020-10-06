#pragma once

#ifndef SPATIAL_API
#define SPATIAL_API __declspec(dllexport)
#endif

#ifndef SPATIAL_C_API
#define SPATIAL_C_API extern "C" __declspec(dllexport)
#endif

/// Calling convention of Spatial C-interface
/// By default it's set to stdcall because we mostly interface with C#
#ifndef SPATIAL_CC
#define SPATIAL_CC __stdcall
#endif

//// @note Some strong hints that some functions are merely wrappers, so should be forced inline
#ifndef SPATIAL_FINLINE
#  ifdef _MSC_VER
#    define SPATIAL_FINLINE __forceinline
#  elif __APPLE__
#    define SPATIAL_FINLINE inline __attribute__((always_inline))
#  else
#    define SPATIAL_FINLINE __attribute__((always_inline))
#  endif
#endif

namespace spatial
{
    /// <summary>
    /// Size of a single linear allocator slab
    /// </summary>
    constexpr int AllocatorSlabSize = 256 * 1024;

    /// <summary>
    /// How many objects to store per quad tree cell before subdividing
    /// </summary>
    constexpr int QuadDefaultLeafSplitThreshold = 64;
    
    /// <summary>
    /// Ratio of search radius where we switch to Linear search
    /// because Quad search would traverse entire tree
    /// </summary>
    constexpr float QuadToLinearRatio = 0.75f;

    /**
     * Default capacity for a single grid cell before reallocating
     */
    constexpr int GridDefaultCellCapacity = 16;
}
