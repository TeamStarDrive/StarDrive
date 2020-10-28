#include "GridCellView.h"
#include <algorithm>

namespace spatial
{
    GridCellView::GridCellView(int desiredGridSize, int cellSize)
    {
        CellSize = cellSize;
        Width = Height = desiredGridSize / cellSize;
        GridSize = Width*cellSize;
        while (GridSize < desiredGridSize)
        {
            ++Width;
            ++Height;
            GridSize = Width*cellSize;
        }
        NumCells = Width * Height;
        setViewOffset1();
    }

    bool GridCellView::toCellRect(const Rect& worldCoords, Rect& outCellCoords) const
    {
        int cellSize = CellSize;
        int width  = Width;
        int height = Height;
        int x1 = (worldCoords.x1 - Coords.x1) / cellSize;
        int x2 = (worldCoords.x2 - Coords.x1) / cellSize;
        int y1 = (worldCoords.y1 - Coords.y1) / cellSize;
        int y2 = (worldCoords.y2 - Coords.y1) / cellSize;
        if (x2 < 0 || y2 < 0 || x1 >= width || y1 >= height)
            return false; // out of grid bounds

        x1 = std::clamp<int>(x1, 0, width - 1);
        x2 = std::clamp<int>(x2, 0, width - 1);
        y1 = std::clamp<int>(y1, 0, height - 1);
        y2 = std::clamp<int>(y2, 0, height - 1);
        outCellCoords = { x1, y1, x2, y2 };
        return true;
    }

    void GridCellView::insert(SlabAllocator& allocator, SpatialObject& o, int cellCapacity)
    {
        Rect cell;
        if (toCellRect(o.rect, cell))
        {
            GridCell* cells = Cells;
            int width  = Width;
            for (int y = cell.y1; y <= cell.y2; ++y)
            {
                for (int x = cell.x1; x <= cell.x2; ++x)
                {
                    GridCell& node = cells[x + y*width];
                    node.addObject(allocator, &o, cellCapacity);
                }
            }
        }
    }

    int GridCellView::findNodes(const SearchOptions& opt, FoundNodes& found) const
    {
        Rect cell;
        if (!toCellRect(opt.SearchRect, cell))
            return 0;
        
        uint32_t loyaltyMask = getLoyaltyMask(opt);
        int maxResults = opt.MaxResults;
        const GridCell* cells = Cells;
        int cellSize = CellSize;
        int cellRadius = cellSize/2;
        int half = GridSize / 2;

        constexpr int SearchPattern = 1;

        // standard scanline search pattern, gets slow when cell cap is reached
        if constexpr (SearchPattern == 0)
        {
            int width = Width;
            for (int y = cell.y1; y <= cell.y2; ++y)
            {
                for (int x = cell.x1; x <= cell.x2; ++x)
                {
                    const GridCell& current = cells[x + y*width];
                    if (current.loyalty.mask & loyaltyMask) // empty cell mask is 0
                    {
                        Point pt = toWorldCellCenter(x,y,half,cellSize);
                        found.add(current.objects, current.size, pt, cellRadius);
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
            int minX = cell.centerX();
            int minY = cell.centerY();
            int maxX = minX, maxY = minY;

            auto addCell = [&found,cells,half,cellSize,width,cellRadius,loyaltyMask](int x, int y)
            {
                const GridCell& current = cells[x + y*width];
                if (current.loyalty.mask & loyaltyMask) // empty cell mask is 0
                {
                    Point pt = toWorldCellCenter(x,y,half,cellSize);
                    found.add(current.objects, current.size, pt, cellRadius);
                }
            };

            addCell(minX, minY);

            while (found.count != found.MAX && found.totalObjects < maxResults)
            {
                bool didExpand = false;
                if (minX > cell.x1) { // test all cells to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        addCell(minX, y);
                }
                if (maxX < cell.x2) { // test all cells to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        addCell(maxX, y);
                }
                if (minY > cell.y1) { // test all top cells
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        addCell(x, minY);
                }
                if (maxY < cell.y2) { // test all bottom cells
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        addCell(x, maxY);
                }
                if (!didExpand)
                    break;
            }
        }
        return found.count;
    }

    void GridCellView::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        int width = Width;
        int height = Height;
        int cellSize = CellSize;
        char text[128];
        Rect visible = opt.visibleWorldRect;
        visualizer.drawRect(Coords, Yellow);

        // the grid itself can be drawn with simple lines instead of thousands of rects
        // We only draw the grid if it covers a sufficient amount of pixels on screen

        float visiblePercent = GridSize / (float)visible.width();
        if (opt.nodeBounds && visiblePercent > 0.025f)
        {
            for (int gridX = 0; gridX < width; ++gridX)
            {
                int x = Coords.x1 + gridX*cellSize;
                visualizer.drawLine({x, Coords.y1}, {x, Coords.y2}, Brown); // Vertical Lines |
            }

            for (int gridY = 0; gridY < height; ++gridY)
            {
                int y = Coords.y1 + gridY*cellSize;
                visualizer.drawLine({Coords.x1, y}, {Coords.x2, y}, Brown); // Horizontal Lines ---
            }
        }

        GridCell* nodes = Cells;
        if (nodes && (opt.nodeText || opt.objectBounds || opt.objectToLeaf || opt.objectText))
        {
            for (int gridY = 0; gridY < height; ++gridY)
            {
                for (int gridX = 0; gridX < width; ++gridX)
                {
                    Rect nodeR = toWorldRect(gridX, gridY);
                    if (!nodeR.overlaps(visible))
                        continue;

                    GridCell& cell = nodes[gridX + gridY*width];
                    Point c = nodeR.center();

                    if (opt.nodeBounds)
                    {
                        auto color = cell.loyalty.count > 1 ? Brown : BrownDim;
                        visualizer.drawRect(nodeR, color);
                    }
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
                            visualizer.drawRect(o.rect, color);
                        }
                        if (opt.objectToLeaf)
                        {
                            auto color = (o.loyalty % 2 == 0) ? VioletDim : Purple;
                            visualizer.drawLine(c, o.rect.center(), color);
                        }
                        if (opt.objectText)
                        {
                            snprintf(text, sizeof(text), "o=%d", o.objectId);
                            visualizer.drawText(o.rect.center(), o.rect.width(), text, Blue);
                        }
                    }
                }
            }
        }
    }

}
