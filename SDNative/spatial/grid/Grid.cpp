#include "Grid.h"

#include <algorithm>

namespace spatial
{
    Grid::Grid(int worldSize, int cellSize)
    {
        WorldSize = worldSize;
        smallestCellSize(cellSize);
    }

    Grid::~Grid()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }
    
    uint32_t Grid::totalMemory() const
    {
        uint32_t bytes = sizeof(Grid);
        bytes += sizeof(GridCell) * Width * Height + 16;
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.totalMemory();
        return bytes;
    }

    void Grid::smallestCellSize(int cellSize)
    {
        CellSize = cellSize;
        Width = Height = WorldSize / cellSize;
        FullSize = Width*cellSize;
        while (FullSize < WorldSize)
        {
            ++Width;
            ++Height;
            FullSize = Width*cellSize;
        }
        NodesCount = Width * Height;
        rebuild();
    }

    void Grid::clear()
    {
        Objects.clear();
        memset(Cells, 0, NodesCount * sizeof(GridCell));
        Dbg.clear();
    }

    void Grid::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        SlabAllocator& front = *FrontAlloc;
        front.reset();
        
        Objects.submitPending();

        GridCell* cells = front.allocArrayZeroed<GridCell>(NodesCount);
        int half     = FullSize / 2;
        int cellSize = CellSize;
        int width  = Width;
        int height = Height;
        int cellCapacity = CellCapacity;

        for (SpatialObject& o : Objects)
        {
            if (!o.active)
                continue;

            int ox = o.x, oy = o.y, orx = o.rx, ory = o.ry;
            int x1 = (ox-orx + half) / cellSize;
            int x2 = (ox+orx + half) / cellSize;
            int y1 = (oy-ory + half) / cellSize;
            int y2 = (oy+ory + half) / cellSize;

            if (x2 < 0 || y2 < 0 || x1 >= width || y1 >= height)
                continue; // this object is out of world bounds

            x1 = std::clamp<int>(x1, 0, width - 1);
            x2 = std::clamp<int>(x2, 0, width - 1);
            y1 = std::clamp<int>(y1, 0, height - 1);
            y2 = std::clamp<int>(y2, 0, height - 1);

            for (int y = y1; y <= y2; ++y)
            {
                for (int x = x1; x <= x2; ++x)
                {
                    GridCell& node = cells[x + y*width];
                    node.addObject(front, &o, cellCapacity);
                }
            }
        }

        Cells = cells;
    }

    CollisionPairs Grid::collideAll(const CollisionParams& params)
    {
        Collider collider { *FrontAlloc, Objects.maxObjects() };

        GridCell* cells = Cells;
        for (int i = 0; i < NodesCount; ++i)
        {
            const GridCell& cell = cells[i];
            if (int size = cell.size)
            {
                collider.collideObjects({cell.objects, size}, params);
            }
        }

        CollisionPairs results = collider.getResults(params);
        if (params.showCollisions)
        {
            Dbg.setCollisions(results);
        }
        return results;
    }

    // transform a cell at index X,Y
    // to world position which falls to the center of the cell
    SPATIAL_FINLINE Point toWorldCellCenter(int x, int y, int worldHalf, int cellSize)
    {
        int offset = (cellSize >> 1) - worldHalf;
        return { (x * cellSize) + offset,
                 (y * cellSize) + offset };
    }

    SPATIAL_FINLINE Rect toWorldRect(int x, int y, int worldHalf, int cellSize)
    {
        int worldX = (x * cellSize) - worldHalf;
        int worldY = (y * cellSize) - worldHalf;
        return { worldX, worldY, worldX + cellSize, worldY + cellSize };
    }

    SPATIAL_FINLINE Rect toWorldRect(int x1, int y1, int x2, int y2, int worldHalf, int cellSize)
    {
        int worldX1 = (x1 * cellSize) - worldHalf;
        int worldY1 = (y1 * cellSize) - worldHalf;
        int worldX2 = (x2 * cellSize) - worldHalf + cellSize;
        int worldY2 = (y2 * cellSize) - worldHalf + cellSize;
        return { worldX1, worldY1, worldX2, worldY2 };
    }

    #pragma warning( disable : 6262 )
    int Grid::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;

        int maxResults = opt.MaxResults;
        const GridCell* cells = Cells;
        int cellSize = CellSize;
        int cellRadius = cellSize/2;
        int half = FullSize / 2;

        int x1 = (opt.SearchRect.left  + half) / cellSize;
        int x2 = (opt.SearchRect.right + half) / cellSize;
        int y1 = (opt.SearchRect.top    + half) / cellSize;
        int y2 = (opt.SearchRect.bottom + half) / cellSize;

        x1 = std::clamp<int>(x1, 0, Width - 1);
        x2 = std::clamp<int>(x2, 0, Width - 1);
        y1 = std::clamp<int>(y1, 0, Height - 1);
        y2 = std::clamp<int>(y2, 0, Height - 1);

        constexpr int SearchPattern = 1;

        // standard scanline search pattern, gets slow when cell cap is reached
        if constexpr (SearchPattern == 0)
        {
            int width = Width;
            for (int y = y1; y <= y2; ++y)
            {
                for (int x = x1; x <= x2; ++x)
                {
                    const GridCell& cell = cells[x + y*width];
                    if (int size = cell.size)
                    {
                        Point pt = toWorldCellCenter(x,y,half,cellSize);
                        found.add(cell.objects, size, pt, cellRadius);
                        if (found.count == found.MAX || found.totalObjects >= maxResults)
                            break;
                    }
                }
            }
        }
        // cell search pattern spirals out from the center
        // this is 20% faster and behaves better if we reach cell limit
        else if constexpr (SearchPattern == 1)
        {
            int width = Width;
            int minX = (x1 + x2) / 2;
            int minY = (y1 + y2) / 2;
            int maxX = minX, maxY = minY;

            auto addCell = [&found,cells,half,cellSize,width,cellRadius](int x, int y)
            {
                const GridCell& cell = cells[x + y*width];
                if (int size = cell.size)
                {
                    Point pt = toWorldCellCenter(x,y,half,cellSize);
                    found.add(cell.objects, size, pt, cellRadius);
                }
            };

            addCell(minX, minY);

            while (found.count != found.MAX && found.totalObjects < maxResults)
            {
                bool didExpand = false;
                if (minX > x1) { // test all cells to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        addCell(minX, y);
                }
                if (maxX < x2) { // test all cells to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        addCell(maxX, y);
                }
                if (minY > y1) { // test all top cells
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        addCell(x, minY);
                }
                if (maxY < y2) { // test all bottom cells
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        addCell(x, maxY);
                }
                if (!didExpand)
                    break;
            }
        }

        int numResults = 0;
        if (found.count)
            numResults = spatial::findNearby(outResults, opt, found);
        
        if (opt.EnableSearchDebugId)
        {
            DebugFindNearby dfn;
            dfn.Rectangle = toWorldRect(x1, y1, x2, y2, half, cellSize);
            dfn.TopLeft  = toWorldRect(x1, y1, half, cellSize);
            dfn.BotRight = toWorldRect(x2, y2, half, cellSize);
            dfn.addCells(found);
            dfn.addResults(outResults, numResults);
            Dbg.setFindNearby(opt.EnableSearchDebugId, std::move(dfn));
        }

        return numResults;
    }

    void Grid::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        GridCell* nodes = Cells;
        int width = Width;
        int height = Height;
        int half = FullSize / 2;
        int cellSize = CellSize;
        char text[128];
        Rect visible = opt.visibleWorldRect;
        visualizer.drawRect({-half, -half, +half, +half}, Yellow);

        // the grid itself can be drawn with simple lines instead of thousands of rects
        for (int gridX = 0; gridX < width; ++gridX)
        {
            int x = gridX*cellSize - half;
            visualizer.drawLine({x, -half}, {x, +half}, Brown); // Vertical Lines |
        }

        for (int gridY = 0; gridY < height; ++gridY)
        {
            int y = gridY*cellSize - half;
            visualizer.drawLine({-half, y}, {+half, y}, Brown); // Horizontal Lines ---
        }

        if (opt.nodeText || opt.objectBounds || opt.objectToLeafLines || opt.objectText)
        {
            for (int gridY = 0; gridY < height; ++gridY)
            {
                for (int gridX = 0; gridX < width; ++gridX)
                {
                    Rect nodeR;
                    nodeR.left = gridX*cellSize - half;
                    nodeR.top  = gridY*cellSize - half;
                    nodeR.right  = nodeR.left + cellSize;
                    nodeR.bottom = nodeR.top  + cellSize;
                    if (!nodeR.overlaps(visible))
                        continue;

                    GridCell& cell = nodes[gridX + gridY*width];
                    Point c = nodeR.center();

                    if (opt.nodeText)
                    {
                        snprintf(text, sizeof(text), "CELL n=%d", cell.size);
                        visualizer.drawText(nodeR.center(), cellSize, text, Yellow);
                    }

                    int count = cell.size;
                    SpatialObject** const items = cell.objects;
                    for (int i = 0; i < count; ++i)
                    {
                        const SpatialObject& o = *items[i];
                        if (opt.objectBounds)
                        {
                            auto color = (o.loyalty % 2 == 0) ? VioletBright : Purple;
                            visualizer.drawRect(o.rect(), color);
                        }
                        if (opt.objectToLeafLines)
                        {
                            visualizer.drawLine(c, {o.x, o.y}, VioletDim);
                        }
                        if (opt.objectText)
                        {
                            snprintf(text, sizeof(text), "o=%d", o.objectId);
                            visualizer.drawText({o.x, o.y}, o.rx*2, text, Blue);
                        }
                    }
                }
            }
        }

        if (opt.searchDebug)
        {
            Dbg.draw(visualizer, opt, Objects.data());
        }
    }


}
