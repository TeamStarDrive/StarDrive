#include "Grid.h"

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
            if (x1 < 0) x1 = 0;
            if (y1 < 0) y1 = 0;
            if (x2 >= width)  x2 = width  - 1;
            if (y2 >= height) y2 = height - 1;

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

    void Grid::collideAll(float timeStep, void* user, CollisionFunc onCollide)
    {
        GridCell* cells = Cells;
        Collider collider;
        for (int i = 0; i < NodesCount; ++i)
        {
            const GridCell& cell = cells[i];
            if (int size = cell.size)
            {
                collider.collideObjects(cell.objects, size, user, onCollide);
            }
        }
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

        Rect selection = Rect::fromPointRadius(opt.OriginX, opt.OriginY, opt.SearchRadius);
        int maxResults = opt.MaxResults;
        const GridCell* cells = Cells;
        int cellSize = CellSize;
        int cellRadius = cellSize/2;
        int half = FullSize / 2;
        int x1 = (selection.left  + half) / cellSize;
        int x2 = (selection.right + half) / cellSize;
        int y1 = (selection.top    + half) / cellSize;
        int y2 = (selection.bottom + half) / cellSize;
        if (x1 < 0) x1 = 0;
        if (y1 < 0) y1 = 0;
        if (x2 >= Width)  x2 = Width  - 1;
        if (y2 >= Height) y2 = Height - 1;

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

        if (Dbg.FindEnabled)
        {
            Dbg.FindCircle = { opt.OriginX, opt.OriginY, opt.SearchRadius };
            Dbg.FindRect     = toWorldRect(x1, y1, x2, y2, half, cellSize);
            Dbg.FindTopLeft  = toWorldRect(x1, y1, half, cellSize);
            Dbg.FindBotRight = toWorldRect(x2, y2, half, cellSize);
            Dbg.setFindCells(found);
        }

        return spatial::findNearby(outResults, opt, found);
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
                            visualizer.drawRect(o.rect(), VioletBright);
                        if (opt.objectToLeafLines)
                            visualizer.drawLine(c, {o.x, o.y}, VioletDim);
                        if (opt.objectText)
                        {
                            snprintf(text, sizeof(text), "o=%d", o.objectId);
                            visualizer.drawText({o.x, o.y}, o.rx*2, text, Blue);
                        }
                    }
                }
            }
        }

        if (Dbg.setIsFindEnabled(opt.searchDebug))
            Dbg.draw(visualizer);
    }


}
