#pragma once
#include "../Config.h"
#include "../Spatial.h"
#include "../SlabAllocator.h"
#include "../SpatialDebug.h"
#include "GridCellView.h"

namespace spatial
{
    class SPATIAL_API Grid final : public Spatial
    {
        GridCell* Root = nullptr;
        GridCellView View;
        int CellCapacity = GridDefaultCellCapacity; // pending until next `rebuild()`

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        SlabAllocator* FrontAlloc = new SlabAllocator{AllocatorSlabSize};
        SlabAllocator* BackAlloc  = new SlabAllocator{AllocatorSlabSize};
        mutable SpatialDebug Dbg;

    public:

        /**
         * @param worldSize The Width and Height of the simulation world
         * @param cellSize Size of a single grid cell. Choose this value carefully.
         */
        explicit Grid(int worldSize, int cellSize);
        ~Grid();

        SpatialRoot* root() const override { return reinterpret_cast<SpatialRoot*>(Root); }
        SpatialType type() const override { return SpatialType::Grid; }
        const char* name() const override { return "Grid"; }
        uint32_t totalMemory() const override;
        
        int nodeCapacity() const override { return CellCapacity; }
        void nodeCapacity(int capacity) override { CellCapacity = capacity; }
        int smallestCellSize() const override { return View.CellSize; }
        void smallestCellSize(int cellSize) override;

        void clear() override;
        SpatialRoot* rebuild() override;

        CollisionPairs collideAll(SpatialRoot* root, const CollisionParams& params) override;
        int findNearby(SpatialRoot* root, int* outResults, const SearchOptions& opt) const override;
        void debugVisualize(SpatialRoot* root, const VisualizerOptions& opt, Visualizer& visualizer) const override;
    };
}
