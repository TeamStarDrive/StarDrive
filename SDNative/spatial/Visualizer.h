#pragma once
#include "Rect.h"
#include <cstdint>

namespace spatial
{
    /**
     * Simple 4-byte RGBA color struct
     */
    struct Color
    {
        uint8_t r, g, b, a;
    };

    /**
     * Generic visualization interface to draw visualization primitives
     * @note All coordinates are World coordinates
     */
    struct Visualizer
    {
        virtual ~Visualizer() = default;
        virtual void drawRect  (int x1, int y1, int x2, int y2,  Color c) = 0;
        virtual void drawCircle(int x,  int y,  int radius,      Color c) = 0;
        virtual void drawLine  (int x1, int y1, int x2, int y2,  Color c) = 0;
        virtual void drawText  (int x,  int y, int size, const char* text, Color c) = 0;
    };

    /**
     * Visualization bridge for C interface
     */
    struct VisualizerBridge
    {
        void (*drawRect)  (int x1, int y1, int x2, int y2,  Color c);
        void (*drawCircle)(int x,  int y,  int radius,      Color c);
        void (*drawLine)  (int x1, int y1, int x2, int y2,  Color c);
        void (*drawText)  (int x,  int y, int size, const char* text, Color c);
    };

    /**
     * Visualization options for increasing or reducing visualized information
     */
    struct VisualizerOptions
    {
        Rect visibleWorldRect; // this visible area in world coordinates that should be drawn
        bool objectBounds = true; // show bounding box around inserted objects
        bool objectToLeafLines = true; // show connections from Leaf node to object center
        bool objectText = false; // show text ontop of each object (very, very intensive)
        bool nodeText = true; // show text ontop of a leaf or branch node
        bool nodeBounds = true; // show edges of leaf and branch nodes
    };

}
