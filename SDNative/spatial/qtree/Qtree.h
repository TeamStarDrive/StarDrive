#pragma once
#include "QtreeNode.h"
#include "../Spatial.h"
#include <vector>

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
        int Levels;
        int FullSize;
        int WorldSize;

        // Since we're not able to modify the tree while it's being built
        // Defer the split threshold setting to `rebuild` method
        int PendingSplitThreshold = QuadDefaultLeafSplitThreshold; // pending until next `rebuild()`
        int CurrentSplitThreshold = QuadDefaultLeafSplitThreshold; // actual value used
        QtreeNode* Root = nullptr;

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        QtreeAllocator* FrontAlloc = new QtreeAllocator{};
        QtreeAllocator* BackAlloc  = new QtreeAllocator{};

        std::vector<SpatialObject> Objects;
        std::vector<SpatialObject> Pending;

    public:

        /**
         * @param worldSize The Width and Height of the simulation world
         * @param smallestCell The smallest allowed Qtree cell size to prevent qtree getting too deep
         */
        explicit Qtree(int worldSize, int smallestCell);
        ~Qtree();
        
        Qtree(Qtree&&) = delete;
        Qtree(const Qtree&) = delete;
        Qtree& operator=(Qtree&&) = delete;
        Qtree& operator=(const Qtree&) = delete;
        
        /**
         * Sets the LEAF node split threshold during next `rebuild()`
         */
        void setLeafSplitThreshold(int threshold) { PendingSplitThreshold = threshold; }

        uint32_t totalMemory() const override;
        int fullSize() const override { return FullSize; }
        int worldSize() const override { return WorldSize; }
        int count() const override { return (int)Objects.size(); }
        const SpatialObject& get(int objectId) const override { return Objects[objectId]; }
        void clear() override;
        void rebuild() override;
        int insert(const SpatialObject& o) override;
        void update(int objectId, int x, int y) override;
        void remove(int objectId) override;
        using Spatial::collideAll;
        void collideAll(float timeStep, void* user, CollisionFunc onCollide) override;
        int findNearby(int* outResults, const SearchOptions& opt) const override;
        void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const override;

    private:

        QtreeNode* createRoot() const;
        void insertAt(int level, QtreeNode& root, SpatialObject* o);
        void insertAtLeaf(int level, QtreeNode& leaf, SpatialObject* o);
        void removeAt(QtreeNode* root, int objectId);
        void markForRemoval(int objectId, SpatialObject& o);
    };

    SPATIAL_C_API Qtree* __stdcall QtreeCreate(int universeSize, int smallestCell);
    SPATIAL_C_API void __stdcall QtreeDestroy(Qtree* tree);
    SPATIAL_C_API void __stdcall QtreeClear(Qtree* tree);
    SPATIAL_C_API void __stdcall QtreeRebuild(Qtree* tree);
    SPATIAL_C_API int  __stdcall QtreeInsert(Qtree* tree, const SpatialObject& o);
    SPATIAL_C_API void __stdcall QtreeUpdate(Qtree* tree, int objectId, int x, int y);
    SPATIAL_C_API void __stdcall QtreeRemove(Qtree* tree, int objectId);
    SPATIAL_C_API void __stdcall QtreeCollideAll(Qtree* tree, float timeStep, void* user, spatial::CollisionFunc onCollide);
    SPATIAL_C_API int __stdcall QtreeFindNearby(Qtree* tree, int* outResults, const spatial::SearchOptions& opt);
    SPATIAL_C_API void __stdcall QtreeDebugVisualize(Qtree* tree, const VisualizerOptions& opt, const VisualizerBridge& vis);
}
