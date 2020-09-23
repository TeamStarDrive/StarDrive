#pragma once
#include "../Config.h"
#include "../Spatial.h"
#include "../SpatialObject.h"
#include "../SlabAllocator.h"
#include "../ObjectCollection.h"
#include "GridCell.h"

namespace spatial
{
    class SPATIAL_API Grid final : public Spatial
    {
        int FullSize;
        int WorldSize;
        int CellSize;
        int Width;
        int Height;
        GridCell* Cells = nullptr;
        int NodesCount = 0;
        int CellCapacity = GridDefaultCellCapacity; // pending until next `rebuild()`

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        SlabAllocator* FrontAlloc = new SlabAllocator{AllocatorSlabSize};
        SlabAllocator* BackAlloc  = new SlabAllocator{AllocatorSlabSize};

        ObjectCollection Objects;

    public:

        /**
         * @param worldSize The Width and Height of the simulation world
         * @param cellSize Size of a single grid cell. Choose this value carefully.
         */
        explicit Grid(int worldSize, int cellSize);
        ~Grid();

        SpatialType type() const override { return SpatialType::Grid; }
        const char* name() const override { return "Grid"; }
        uint32_t totalMemory() const override;
        int fullSize() const override { return FullSize; }
        int worldSize() const override { return WorldSize; }
        int count() const override { return Objects.count(); }
        const SpatialObject& get(int objectId) const override { return Objects.get(objectId); }
        
        int nodeCapacity() const override { return CellCapacity; }
        void nodeCapacity(int capacity) override { CellCapacity = capacity; }
        int smallestCellSize() const override { return CellSize; }
        void smallestCellSize(int cellSize) override;

        void clear() override;
        void rebuild() override;
        int insert(const SpatialObject& o) override { return Objects.insert(o); }
        void update(int objectId, int x, int y) override { Objects.update(objectId, x, y); }
        void remove(int objectId) override { Objects.remove(objectId); }
        using Spatial::collideAll;
        void collideAll(float timeStep, void* user, CollisionFunc onCollide) override;
        int findNearby(int* outResults, const SearchOptions& opt) const override;
        void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const override;
    };
}
