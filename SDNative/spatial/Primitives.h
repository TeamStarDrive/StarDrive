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

    struct CollisionPair
    {
        int a, b;
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

    struct CircleF
    {
        float x;
        float y;
        float radius;

        CircleF() = default;
        constexpr CircleF(float x, float y, float r) : x{x}, y{y}, radius{r} {}
        constexpr CircleF(const Circle& c) : x{(float)c.x}, y{(float)c.y}, radius{(float)c.radius} {}

        static constexpr CircleF Zero() { return { 0.0f, 0.0f, 0.0f }; }
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

        bool empty() const { return left == right; }

        bool operator==(const Rect& r) const
        {
            return left  == r.left  && top == r.top
                && right == r.right && bottom == r.bottom;
        }
        bool operator!=(const Rect& r) const
        {
            return left  != r.left  || top != r.top
                || right != r.right || bottom != r.bottom;
        }

        /** @return Fixes a Rect with negative width */
        Rect normalized() const
        {
            Rect result;
            const bool swapX = left > right;
            const bool swapY = top > bottom;
            result.left   = swapX ? right : left;
            result.right  = swapX ? left : right;
            result.top    = swapY ? bottom : top;
            result.bottom = swapY ? top : bottom;
            return result;
        }

        SPATIAL_FINLINE static Rect fromPointRadius(int x, int y, int r)
        {
            return Rect{x-r, y-r, x+r, y+r};
        }

        SPATIAL_FINLINE static Rect fromWorldCoords(float x1, float y1, float x2, float y2)
        {
            return Rect{ (int)x1, (int)y1, (int)x2, (int)y2 };
        }

        SPATIAL_FINLINE bool overlaps(const Rect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }

        static constexpr float max(float a, float b) { return a > b ? a : b; }
        static constexpr float min(float a, float b) { return a < b ? a : b; }

        SPATIAL_FINLINE bool overlaps(const CircleF& c) const
        {
            // find the nearest point on the rectangle to the center of the circle
            float nearestX = max((float)left, min(c.x, (float)right));
            float nearestY = max((float)top,  min(c.y, (float)bottom));
            float dx = nearestX - c.x;
            float dy = nearestY - c.y;
            float rr = c.radius;
            return (dx*dx + dy*dy) <= (rr*rr);
        }
    };
}
