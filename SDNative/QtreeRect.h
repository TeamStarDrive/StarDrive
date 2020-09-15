#pragma once

namespace tree
{
    struct QtreeRect
    {
        float left;
        float top;
        float right;
        float bottom;

        float centerX() const { return (left + right) * 0.5f; }
        float centerY() const { return (top + bottom) * 0.5f; }
    };
}
