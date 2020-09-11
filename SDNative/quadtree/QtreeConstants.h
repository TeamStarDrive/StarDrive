#pragma once

namespace tree
{
    /// <summary>
    /// How many objects to store per quad tree cell before subdividing
    /// </summary>
    constexpr int QuadCellThreshold = 64;
    
    /// <summary>
    /// Ratio of search radius where we switch to Linear search
    /// because Quad search would traverse entire tree
    /// </summary>
    constexpr float QuadToLinearRatio = 0.75f;

    /// <summary>
    /// Size of a single quadtree linear allocator slab
    /// </summary>
    constexpr int QuadLinearAllocatorSlabSize = 64 * 1024;
}
