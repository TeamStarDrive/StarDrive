#pragma once
#include "Primitives.h"
#include <vector>
#include <unordered_map>
#include <mutex>

namespace spatial
{
    struct Visualizer;
    struct FoundNodes;

    static const Color Brown  = { 139, 69,  19, 150 };
    static const Color VioletDim = { 199, 21, 133, 100 };
    static const Color VioletBright = { 199, 21, 133, 150 };
    static const Color Aqua = { 180, 254, 231, 150 };
    static const Color Purple = { 96, 63, 139, 150 };
    static const Color Blue   = { 95, 158, 160, 200 };
    static const Color Yellow = { 255, 255,  0, 200 };
    static const Color Red    = { 255, 80, 80, 200 };

    struct DebugFindNearby
    {
        Circle Circle = Circle::Zero();
        Rect Rectangle = Rect::Zero();
        Rect TopLeft  = Rect::Zero();
        Rect BotRight = Rect::Zero();
        std::vector<Rect> FindCells;

        void addCells(const FoundNodes& found);
        void draw(Visualizer& visualizer) const;
    };

    struct SpatialDebug
    {
        // For our use case, findNearby can be called from another thread
        // so this mutable debug data must be lock guarded
        mutable std::mutex FindMutex;
        std::unordered_map<int, DebugFindNearby> FindNearby;

        void clear();
        void setFindNearby(int id, DebugFindNearby&& find);
        void draw(Visualizer& visualizer) const;
    };

}
