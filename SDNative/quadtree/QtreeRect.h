#pragma once

namespace tree
{
    struct QtreeRect
    {
        int left;
        int top;
        int right;
        int bottom;

        // NOTE: No ctor, to avoid unnecessary code generation

        static QtreeRect fromPointRadius(int x, int y, int r)
        {
            return QtreeRect{x-r, y-r, x+r, y+r};
        }

        int centerX() const { return (left + right) >> 1; }
        int centerY() const { return (top + bottom) >> 1; }

        bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
