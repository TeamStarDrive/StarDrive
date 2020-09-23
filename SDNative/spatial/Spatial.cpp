#include "Spatial.h"
#include "grid/Grid.h"
#include "qtree/Qtree.h"

namespace spatial
{
    std::shared_ptr<Spatial> Spatial::create(SpatialType type, int universeSize, int cellSize)
    {
        if (type == SpatialType::Grid)
            return std::make_shared<Grid>(universeSize, cellSize);
        if (type == SpatialType::QuadTree)
            return std::make_shared<Qtree>(universeSize, cellSize);
        return {};
    }

    SPATIAL_C_API Spatial* SpatialCreate(SpatialType type, int universeSize, int cellSize)
    {
        if (type == SpatialType::Grid)
            return new Grid{universeSize, cellSize};
        if (type == SpatialType::QuadTree)
            return new Qtree{universeSize, cellSize};
        return nullptr;
    }
    SPATIAL_C_API void SpatialDestroy(Spatial* tree)
    {
        delete tree;
    }
    SPATIAL_C_API void SpatialClear(Spatial* tree)
    {
        tree->clear();
    }
    SPATIAL_C_API void SpatialRebuild(Spatial* tree)
    {
        tree->rebuild();
    }
    SPATIAL_C_API int SpatialInsert(Spatial* tree, const SpatialObject& o)
    {
        return tree->insert(o);
    }
    SPATIAL_C_API void SpatialUpdate(Spatial* tree, int objectId, int x, int y)
    {
        tree->update(objectId, x, y);
    }
    SPATIAL_C_API void SpatialRemove(Spatial* tree, int objectId)
    {
        tree->remove(objectId);
    }
    SPATIAL_C_API void SpatialCollideAll(Spatial* tree, float timeStep, void* user, CollisionFunc onCollide)
    {
        tree->collideAll(timeStep, user, onCollide);
    }
    SPATIAL_C_API int SpatialFindNearby(Spatial* tree, int* outResults, const SearchOptions& opt)
    {
        return tree->findNearby(outResults, opt);
    }
    SPATIAL_C_API void SpatialDebugVisualize(Spatial* tree, const VisualizerOptions& opt, const VisualizerBridge& vis)
    {
        struct CppToCBridge : Visualizer
        {
            VisualizerBridge vis;
            explicit CppToCBridge(const VisualizerBridge& visualizer) : vis{visualizer} {}
            void drawRect(int x1, int y1, int x2, int y2, Color c) override
            { vis.drawRect(x1, y1, x2, y2, c); }
            void drawCircle(int x, int y, int radius, Color c) override
            { vis.drawCircle(x, y, radius, c); }
            void drawLine(int x1, int y1, int x2, int y2, Color c) override
            { vis.drawLine(x1, y1, x2, y2, c); }
            void drawText(int x, int y, int size, const char* text, Color c) override
            { vis.drawText(x, y, size, text, c); }
        };

        CppToCBridge bridge { vis };
        tree->debugVisualize(opt, bridge);
    }
}
