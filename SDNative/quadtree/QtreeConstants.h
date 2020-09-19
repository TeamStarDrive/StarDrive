#pragma once

#ifndef TREE_API
#define TREE_API __declspec(dllexport)
#endif

#ifndef TREE_CAPI
#define TREE_C_API extern "C" __declspec(dllexport)
#endif

//// @note Some strong hints that some functions are merely wrappers, so should be forced inline
#ifndef TREE_FINLINE
#  ifdef _MSC_VER
#    define TREE_FINLINE __forceinline
#  elif __APPLE__
#    define TREE_FINLINE inline __attribute__((always_inline))
#  else
#    define TREE_FINLINE __attribute__((always_inline))
#  endif
#endif

namespace tree
{
    /// <summary>
    /// How many objects to store per quad tree cell before subdividing
    /// </summary>
    constexpr int QuadDefaultLeafSplitThreshold = 64;
    
    /// <summary>
    /// Ratio of search radius where we switch to Linear search
    /// because Quad search would traverse entire tree
    /// </summary>
    constexpr float QuadToLinearRatio = 0.75f;

    /// <summary>
    /// Size of a single quadtree linear allocator slab
    /// </summary>
    constexpr int QuadLinearAllocatorSlabSize = 128 * 1024;
}
