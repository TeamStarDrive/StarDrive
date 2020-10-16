#pragma once
#include "Primitives.h"

namespace spatial
{

    /**
     * Generic visualization interface to draw visualization primitives
     * @note All coordinates are World coordinates
     */
    struct Visualizer
    {
        virtual ~Visualizer() = default;
        virtual void drawRect  (Rect r,  Color c) = 0;
        virtual void drawCircle(Circle ci, Color c) = 0;
        virtual void drawLine  (Point a, Point b,  Color c) = 0;
        virtual void drawText  (Point p, int size, const char* text, Color c) = 0;
    };

    /**
     * Visualization bridge for C interface
     */
    struct VisualizerBridge
    {
        void (SPATIAL_CC*drawRect)  (Rect r,  Color c);
        void (SPATIAL_CC*drawCircle)(Circle ci, Color c);
        void (SPATIAL_CC*drawLine)  (Point a, Point b,  Color c);
        void (SPATIAL_CC*drawText)  (Point p, int size, const char* text, Color c);
    };

    /**
     * Visualization options for increasing or reducing visualized information
     */
    struct VisualizerOptions
    {
        Rect visibleWorldRect; // this visible area in world coordinates that should be drawn
        bool objectBounds = true; // show bounding box around inserted objects
        bool objectToLeaf = true; // show connections from Leaf node to object center
        bool objectText = false; // show text ontop of each object (very, very intensive)
        bool nodeText = true; // show text ontop of a leaf or branch node
        bool nodeBounds = true; // show edges of leaf and branch nodes
        bool searchDebug = true; // show the debug information for latest searches
        bool searchResults = true; // highlight search results
        bool collisions = true; // show collision flashes
    };

}
