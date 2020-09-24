#pragma once
#include "Config.h"
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

    struct Point
    {
        int x;
        int y;
        static constexpr Point Zero() { return { 0, 0 }; }
    };

    struct Circle
    {
        int x;
        int y;
        int radius;
        static constexpr Circle Zero() { return { 0, 0, 0 }; }
    };

    struct Rect
    {
        int left;
        int top;
        int right;
        int bottom;

        // NOTE: No ctor, to avoid sub-optimal code generation
        static constexpr Rect Zero() { return {0,0,0,0}; }

        int centerX() const { return (left + right) >> 1; }
        int centerY() const { return (top + bottom) >> 1; }
        Point center() const { return { (left + right) >> 1, (top + bottom) >> 1 }; }
        int width()  const { return (right - left); }
        int height() const { return (bottom - top); }

        SPATIAL_FINLINE static Rect fromPointRadius(int x, int y, int r)
        {
            return Rect{x-r, y-r, x+r, y+r};
        }

        SPATIAL_FINLINE bool overlaps(const Rect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
