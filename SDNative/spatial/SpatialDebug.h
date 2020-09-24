#pragma once
#include "Primitives.h"
#include <vector>

namespace spatial
{
    struct Visualizer;
    struct FoundNodes;

    static const Color Brown  = { 139, 69,  19, 150 };
    static const Color VioletDim = { 199, 21, 133, 100 };
    static const Color VioletBright = { 199, 21, 133, 150 };
    static const Color Blue   = { 95, 158, 160, 200 };
    static const Color Yellow = { 255, 255,  0, 200 };
    static const Color Red    = { 255, 80, 80, 200 };

    struct SpatialDebug
    {
        bool FindEnabled = false;
        Circle FindCircle = Circle::Zero();
        Rect FindRect     = Rect::Zero();
        Rect FindTopLeft  = Rect::Zero();
        Rect FindBotRight = Rect::Zero();
        std::vector<Rect> FindCells;

        void setFindCells(const FoundNodes& found);
        void draw(Visualizer& visualizer);

        // if FindDebug is not yet enabled, toggle it,
        // so the next search will record the info
        bool setIsFindEnabled(bool findEnabled)
        {
            return FindEnabled = findEnabled;
        }
    };

}
