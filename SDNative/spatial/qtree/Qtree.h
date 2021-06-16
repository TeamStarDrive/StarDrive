#pragma once
#include "QtreeNode.h"
#include "../Spatial.h"
#include "../ObjectCollection.h"
#include "../SpatialDebug.h"

namespace spatial
{
    /**
     * A fast QuadTree implementation
     *  -) Linear SLAB Allocators for cheap dynamic growth
     *  -) Bulk collision reaction function
     *  -) Fast search via findNearby
     */
    class SPATIAL_API Qtree final : public Spatial
    {
        QtreeNode* Root = nullptr;
        int Levels;
        int SmallestCell;

        // Since we're not able to modify the tree while it's being built
        // Defer the split threshold setting to `rebuild` method
        int PendingSplitThreshold = QuadDefaultLeafSplitThreshold; // pending until next `rebuild()`
        int CurrentSplitThreshold = QuadDefaultLeafSplitThreshold; // actual value used

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        SlabAllocator* FrontAlloc = new SlabAllocator{AllocatorSlabSize};
        SlabAllocator* BackAlloc  = new SlabAllocator{AllocatorSlabSize};
        mutable SpatialDebug Dbg;

    public:

        /**
         * @param worldSize The Width and Height of the simulation world
         * @param smallestCell The smallest allowed Qtree node size to prevent qtree getting too deep
         */
        explicit Qtree(int worldSize, int smallestCell);
        ~Qtree();

        SpatialRoot* root() const override { return reinterpret_cast<SpatialRoot*>(Root); }
        SpatialType type() const override { return SpatialType::QuadTree; }
        const char* name() const override { return "Qtree"; }
        uint32_t totalMemory() const override;
        
        int nodeCapacity() const override { return PendingSplitThreshold; }
        void nodeCapacity(int capacity) override { PendingSplitThreshold = capacity; }
        int smallestCellSize() const override { return SmallestCell; }
        void smallestCellSize(int cellSize) override;

        void clear() override;
        SpatialRoot* rebuild() override;
        CollisionPairs collideAll(SpatialRoot* root, const CollisionParams& params) override;
        int findNearby(SpatialRoot* root, int* outResults, const SearchOptions& opt) const override;
        void debugVisualize(SpatialRoot* root, const VisualizerOptions& opt, Visualizer& visualizer) const override;

    private:

        QtreeNode* createRoot() const;
        void insertAt(int level, QtreeNode& root, SpatialObject* o);
        void insertAtLeaf(int level, QtreeNode& leaf, SpatialObject* o);
    };
}
