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

        /**
         * @return True if Circle{p, pRadius} overlaps this Circle 
         */
        bool overlaps(Point p, int pRadius) const
        {
            // need to use floats here, otherwise some of the math breaks
            float dx = x - p.x;
            float dy = y - p.y;
            float rr = radius + pRadius;
            return (dx*dx + dy*dy) <= (rr*rr);
        }
    };
    
    static constexpr float max(float a, float b) { return a > b ? a : b; }
    static constexpr float min(float a, float b) { return a < b ? a : b; }

    struct Rect
    {
        int x1;
        int y1;
        int x2;
        int y2;

        // NOTE: No ctor, to avoid sub-optimal code generation
        static constexpr Rect Zero() { return {0,0,0,0}; }

        int centerX() const { return (x1 + x2) >> 1; }
        int centerY() const { return (y1 + y2) >> 1; }
        Point center() const { return { (x1 + x2) >> 1, (y1 + y2) >> 1 }; }
        int width()  const { return (x2 - x1); }
        int height() const { return (y2 - y1); }

        bool empty() const { return x1 == x2; }

        bool operator==(const Rect& r) const
        {
            return x1  == r.x1 && y1 == r.y1
                && x2 == r.x2 && y2 == r.y2;
        }
        bool operator!=(const Rect& r) const
        {
            return x1 != r.x1 || y1 != r.y1
                || x2 != r.x2 || y2 != r.y2;
        }

        /** @return Fixes a Rect with negative width */
        Rect normalized() const
        {
            Rect result;
            const bool swapX = x1 > x2;
            const bool swapY = y1 > y2;
            result.x1 = swapX ? x2 : x1;
            result.x2 = swapX ? x1 : x2;
            result.y1 = swapY ? y2 : y1;
            result.y2 = swapY ? y1 : y2;
            return result;
        }

        /** @returns Circle from this rectangle */
        Circle toCircle() const
        {
            return Circle{centerX(), centerY(), (width()+height()) / 4};
        }

        SPATIAL_FINLINE static Rect fromPointRadius(int x, int y, int r)
        {
            return Rect{x-r, y-r, x+r, y+r};
        }

        SPATIAL_FINLINE static Rect fromWorldCoords(float x1, float y1, float x2, float y2)
        {
            return Rect{ (int)x1, (int)y1, (int)x2, (int)y2 };
        }

        // True if two rectangles overlap or perfectly touch
        SPATIAL_FINLINE bool overlaps(const Rect& r) const
        {
            // NOTE: >= vs > determines whether there's a match if rectangles touch
            return x1 <= r.x2 && x2 >= r.x1
                && y1 <= r.y2 && y2 >= r.y1;
        }

        /**
         * @return True if any part of this Rect overlaps with the Circle
         */
        SPATIAL_FINLINE bool overlaps(const CircleF& c) const
        {
            // find the nearest point on the rectangle to the center of the circle
            float nearestX = max((float)x1, min(c.x, (float)x2));
            float nearestY = max((float)y1, min(c.y, (float)y2));
            float dx = nearestX - c.x;
            float dy = nearestY - c.y;
            float rr = c.radius;
            return (dx*dx + dy*dy) <= (rr*rr);
        }
    };
    
    /**
     * Convert rect to circle and do Circle/Circle overlap OR touch
     * @return True if Circle{r} overlaps Circle c
     *         Rectangle's radius is calculated as (width/2 + height/2)/2
     */
    SPATIAL_FINLINE bool overlapsRadius(const Rect& r, const CircleF& c)
    {
        int rw = (r.x2 - r.x1) >> 1;
        int rh = (r.y2 - r.y1) >> 1;
        // need to use floats here, otherwise some of the math breaks
        float dx = c.x - (r.x1 + rw);
        float dy = c.y - (r.y1 + rh);
        float rr = c.radius + ((rw + rh) >> 1);
        // <= vs < determines if we include touching primitives
        return (dx*dx + dy*dy) <= (rr*rr);
    }
}
