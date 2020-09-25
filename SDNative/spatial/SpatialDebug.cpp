#include "SpatialDebug.h"
#include "Visualizer.h"
#include "Search.h"

namespace spatial
{
    void DebugFindNearby::addCells(const FoundNodes& found)
    {
        for (int i = 0; i < found.count; ++i)
        {
            const FoundNode& node = found.nodes[i];
            FindCells.push_back(Rect::fromPointRadius(node.world.x, node.world.y, node.radius));
        }
    }

    void DebugFindNearby::draw(Visualizer& visualizer) const
    {
        if (Circle.radius != 0)
            visualizer.drawCircle(Circle, Yellow);

        if (!Rectangle.empty())
            visualizer.drawRect(Rectangle, Yellow);

        if (!TopLeft.empty())
            visualizer.drawRect(TopLeft, Red);

        if (!BotRight.empty())
            visualizer.drawRect(BotRight, Red);

        for (const Rect& findCell : FindCells)
            visualizer.drawRect(findCell, Blue);
    }

    void SpatialDebug::clear()
    {
        std::lock_guard lock { FindMutex };
        FindNearby.clear();
    }

    void SpatialDebug::setFindNearby(int id, DebugFindNearby&& find)
    {
        std::lock_guard lock { FindMutex };

        FindNearby[id] = std::move(find);
    }

    void SpatialDebug::draw(Visualizer& visualizer) const
    {
        std::lock_guard lock { FindMutex };

        for (auto& [id, find] : FindNearby)
        {
            find.draw(visualizer);
        }
    }
}
