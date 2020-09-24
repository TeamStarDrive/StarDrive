#include "SpatialDebug.h"
#include "Visualizer.h"
#include "Search.h"

namespace spatial
{
    void SpatialDebug::setFindCells(const FoundNodes& found)
    {
        FindCells.clear();
        for (int i = 0; i < found.count; ++i)
        {
            const FoundNode& node = found.nodes[i];
            FindCells.push_back(Rect::fromPointRadius(node.world.x, node.world.y, node.radius));
        }
    }

    void SpatialDebug::draw(Visualizer& visualizer)
    {
        if (FindCircle.radius != 0)
            visualizer.drawCircle(FindCircle, Yellow);

        if (FindRect.width() != 0)
            visualizer.drawRect(FindRect, Yellow);

        if (FindTopLeft.width() != 0)
            visualizer.drawRect(FindTopLeft, Red);

        if (FindBotRight.width() != 0)
            visualizer.drawRect(FindBotRight, Red);

        if (!FindCells.empty())
        {
            for (size_t i = 0; i < FindCells.size(); ++i)
                visualizer.drawRect(FindCells[i], Blue);
        }
    }
}
