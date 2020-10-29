#pragma once
#include "../Config.h"
#include "../Spatial.h"
#include "../SlabAllocator.h"
#include "../SpatialDebug.h"
#include "../grid/GridCellView.h"

namespace spatial
{
    class SPATIAL_API GridL2 final : public Spatial
    {
        GridCellView TopLevel; // cell parameters for Top level grid
        GridCellView SecondLevel; // cell parameters for Level 2 grid

        // A grid of grids, we don't store metadata inside the grid to save space
        GridCell** ArrayOfCells = nullptr;
        int CellCapacity = GridDefaultCellCapacity; // pending until next `rebuild()`

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        SlabAllocator* FrontAlloc = new SlabAllocator{AllocatorSlabSize};
        SlabAllocator* BackAlloc  = new SlabAllocator{AllocatorSlabSize};
        mutable SpatialDebug Dbg;

    public:

        /**
         * @param worldSize The Width and Height of the simulation world
         * @param cellSize Size of a single grid cell. Choose this value carefully.
         * @param cellSize2 Size of the second level grid cell
         */
        explicit GridL2(int worldSize, int cellSize, int cellSize2);
        ~GridL2();

        SpatialType type() const override { return SpatialType::GridL2; }
        const char* name() const override { return "GridL2"; }
        uint32_t totalMemory() const override;
        
        int nodeCapacity() const override { return CellCapacity; }
        void nodeCapacity(int capacity) override { CellCapacity = capacity; }
        int smallestCellSize() const override { return TopLevel.CellSize; }
        void smallestCellSize(int cellSize) override;

        void clear() override;
        void rebuild() override;

        CollisionPairs collideAll(const CollisionParams& params) override;
        int findNearby(int* outResults, const SearchOptions& opt) const override;
        void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const override;
    };
}
