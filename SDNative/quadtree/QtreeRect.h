#pragma once

namespace spatial
{
    struct QtreeRect
    {
        int left;
        int top;
        int right;
        int bottom;

        // NOTE: No ctor, to avoid sub-optimal code generation

        int centerX() const { return (left + right) >> 1; }
        int centerY() const { return (top + bottom) >> 1; }
        int width()  const { return (right - left); }
        int height() const { return (bottom - top); }

        static QtreeRect fromPointRadius(int x, int y, int r)
        {
            return QtreeRect{x-r, y-r, x+r, y+r};
        }
    };
}
