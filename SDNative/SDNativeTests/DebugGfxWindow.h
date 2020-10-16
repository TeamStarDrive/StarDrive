#pragma once
#include <functional>
#include "3rdparty/imgui/imgui.h"

class DebugGfxWindow
{
    int Width = 0;
    int Height = 0;
public:

    int width()  const { return Width; }
    int height() const { return Height; }

    /**
     * @param onFrame Frame callback, return true to continue, false to exit
     */
    void Run(const std::function<bool()>& onFrame);
};
