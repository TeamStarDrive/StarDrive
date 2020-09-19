#pragma once

namespace tree
{
    struct QtreeRect
    {
        float left;
        float top;
        float right;
        float bottom;

        // NOTE: No ctor, to avoid unnecessary code generation

        static QtreeRect fromPointRadius(float x, float y, float r)
        {
            return QtreeRect{x-r, y-r, x+r, y+r};
        }

        float centerX() const { return (left + right) * 0.5f; }
        float centerY() const { return (top + bottom) * 0.5f; }

        bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
