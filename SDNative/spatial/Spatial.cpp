#include "Spatial.h"
#include "grid/Grid.h"
#include "gridL2/GridL2.h"
#include "qtree/Qtree.h"

namespace spatial
{
    std::shared_ptr<Spatial> Spatial::create(SpatialType type, int universeSize, int cellSize, int cellSize2)
    {
        if (type == SpatialType::Grid)
            return std::make_shared<Grid>(universeSize, cellSize);
        if (type == SpatialType::QuadTree)
            return std::make_shared<Qtree>(universeSize, cellSize);
        if (type == SpatialType::GridL2)
            return std::make_shared<GridL2>(universeSize, cellSize, cellSize2);
        return {};
    }

    SPATIAL_C_API Spatial* SPATIAL_CC SpatialCreate(SpatialType type, int universeSize, int cellSize, int cellSize2)
    {
        if (type == SpatialType::Grid)
            return new Grid{universeSize, cellSize};
        if (type == SpatialType::QuadTree)
            return new Qtree{universeSize, cellSize};
        if (type == SpatialType::GridL2)
            return new GridL2{universeSize, cellSize, cellSize2};
        return nullptr;
    }
    SPATIAL_C_API void SPATIAL_CC SpatialDestroy(Spatial* spatial)
    {
        delete spatial;
    }
    
    SPATIAL_C_API SpatialType SPATIAL_CC SpatialGetType(Spatial* spatial) { return spatial->type(); }
    SPATIAL_C_API int SPATIAL_CC SpatialWorldSize(Spatial* spatial) { return spatial->worldSize(); }
    SPATIAL_C_API int SPATIAL_CC SpatialFullSize(Spatial* spatial)  { return spatial->fullSize(); }
    SPATIAL_C_API int SPATIAL_CC SpatialNumActive(Spatial* spatial) { return spatial->numActive(); }
    SPATIAL_C_API int SPATIAL_CC SpatialMaxObjects(Spatial* spatial){ return spatial->maxObjects(); }
    SPATIAL_C_API void SPATIAL_CC SpatialClear(Spatial* spatial)    { spatial->clear(); }
    SPATIAL_C_API void SPATIAL_CC SpatialRebuild(Spatial* spatial)  { spatial->rebuild(); }

    SPATIAL_C_API int SPATIAL_CC SpatialInsert(Spatial* spatial, const SpatialObject* o)
    {
        return spatial->insert(*o);
    }
    SPATIAL_C_API void SPATIAL_CC SpatialUpdate(Spatial* spatial, int objectId, const Rect* rect)
    {
        spatial->update(objectId, *rect);
    }
    SPATIAL_C_API void SPATIAL_CC SpatialRemove(Spatial* spatial, int objectId)
    {
        spatial->remove(objectId);
    }

    SPATIAL_C_API void SPATIAL_CC SpatialCollideAll(Spatial* spatial, const CollisionParams* params, CollisionPairs* outResults)
    {
        *outResults = spatial->collideAll(*params);
    }
    SPATIAL_C_API int SPATIAL_CC SpatialFindNearby(Spatial* spatial, int* outResults, const SearchOptions* opt)
    {
        return spatial->findNearby(outResults, *opt);
    }
    SPATIAL_C_API void SPATIAL_CC SpatialDebugVisualize(Spatial* spatial, const VisualizerOptions* opt, const VisualizerBridge* vis)
    {
        struct CppToCBridge : Visualizer
        {
            VisualizerBridge vis;
            explicit CppToCBridge(const VisualizerBridge& visualizer) : vis{visualizer} {}
            void drawRect(Rect r, Color c) override { vis.drawRect(r, c); }
            void drawCircle(Circle ci, Color c) override { vis.drawCircle(ci, c); }
            void drawLine(Point a, Point b, Color c) override { vis.drawLine(a, b, c); }
            void drawText(Point p, int size, const char* text, Color c) override { vis.drawText(p, size, text, c); }
        };
        CppToCBridge bridge { *vis };
        spatial->debugVisualize(*opt, bridge);
    }
}
