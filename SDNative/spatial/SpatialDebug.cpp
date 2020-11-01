#include "SpatialDebug.h"
#include "Visualizer.h"
#include "Search.h"

namespace spatial
{
    void DebugFindNearby::addCells(const FoundNodes& found)
    {
        FindCells.reserve(found.count);
        for (int i = 0; i < found.count; ++i)
        {
            const FoundNode& node = found.nodes[i];
            FindCells.push_back(Rect::fromPointRadius(node.world.x, node.world.y, node.radius));
        }
    }

    void DebugFindNearby::addResults(const int* results, int numResults)
    {
        SearchResults.reserve(numResults);
        for (int i = 0; i < numResults; ++i)
        {
            int objectId = results[i];
            SearchResults.push_back(results[i]);
        }
    }

    void DebugFindNearby::draw(Visualizer& visualizer, const VisualizerOptions& opt,
                               const SpatialObject* objects) const
    {
        if (!SearchArea.empty())
            visualizer.drawRect(SearchArea, Yellow);

        if (RadialFilter.radius > 0)
            visualizer.drawCircle(RadialFilter, Yellow);

        if (!SelectedRect.empty())
            visualizer.drawRect(SelectedRect, Yellow);

        if (!TopLeft.empty())
            visualizer.drawRect(TopLeft, Red);

        if (!BotRight.empty())
            visualizer.drawRect(BotRight, Red);

        for (const Rect& findCell : FindCells)
            visualizer.drawRect(findCell, Blue);

        if (opt.searchResults)
        {
            for (int objectId : SearchResults)
            {
                visualizer.drawRect(objects[objectId].rect, YellowBright);
            }
        }
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

    void SpatialDebug::setCollisions(const CollisionPairs& collisions)
    {
        std::lock_guard lock { FindMutex };
        Collisions.assign(collisions.begin(), collisions.end());
    }

    void SpatialDebug::draw(Visualizer& visualizer, const VisualizerOptions& opt,
                            const SpatialObject* objects) const
    {
        std::lock_guard lock { FindMutex };

        for (auto& [id, find] : FindNearby)
        {
            find.draw(visualizer, opt, objects);
        }

        if (opt.collisions)
        {
            for (CollisionPair collision : Collisions)
            {
                visualizer.drawRect(objects[collision.a].rect, Cyan);
                visualizer.drawRect(objects[collision.b].rect, Cyan);
            }
        }
    }
}
