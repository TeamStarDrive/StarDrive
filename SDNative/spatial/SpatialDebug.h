#pragma once
#include "Primitives.h"
#include "Collision.h"
#include <vector>
#include <unordered_map>
#include <mutex>


namespace spatial
{
    struct SpatialObject;
    struct Visualizer;
    struct VisualizerOptions;
    struct FoundNodes;

    static const Color Brown  = { 139, 69,  19, 150 };
    static const Color BrownDim  = { 89, 39,  5, 150 };
    static const Color VioletDim = { 199, 21, 133, 100 };
    static const Color VioletBright = { 199, 21, 133, 150 };
    static const Color Aqua = { 180, 254, 231, 150 };
    static const Color Purple = { 96, 63, 139, 150 };
    static const Color Blue   = { 95, 158, 160, 200 };
    static const Color Yellow = { 255, 255,  0, 200 };
    static const Color Red    = { 255, 80, 80, 200 };
    static const Color YellowBright = { 255, 255, 0, 255 };
    static const Color Cyan   = {   0, 255, 255, 255 };

    struct DebugFindNearby
    {
        Rect SearchArea = Rect::Zero();
        Circle RadialFilter = Circle::Zero();
        Rect SelectedRect = Rect::Zero();
        Rect TopLeft  = Rect::Zero();
        Rect BotRight = Rect::Zero();
        std::vector<Rect> FindCells;
        std::vector<int> SearchResults;

        void addCells(const FoundNodes& found);
        void addResults(const int* results, int numResults);
        void draw(Visualizer& visualizer, const VisualizerOptions& opt,
                  const SpatialObject* objects) const;
    };

    struct SpatialDebug
    {
        // For our use case, findNearby can be called from another thread
        // so this mutable debug data must be lock guarded
        mutable std::mutex FindMutex;
        std::unordered_map<int, DebugFindNearby> FindNearby;
        std::vector<CollisionPair> Collisions;

        void clear();
        void setFindNearby(int id, DebugFindNearby&& find);
        void setCollisions(const CollisionPairs& collisions);
        void draw(Visualizer& visualizer, const VisualizerOptions& opt,
                  const SpatialObject* objects) const;
    };

}
