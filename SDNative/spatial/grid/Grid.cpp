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

    struct FoundCell
    {
        const GridCell* cell;
        int worldX, worldY;
    };
    
    struct FoundCells
    {
        static constexpr int MAX = 2048;
        int count = 0;
        int totalObjects = 0; // total number of potential objects
        FoundCell cells[MAX];
        void addCell(const GridCell* cell, int worldX, int worldY)
        {
            if (count != MAX)
            {
                cells[count++] = { cell, worldX, worldY };
                totalObjects += cell->size;
            }
        }
    };

    #pragma warning( disable : 6262 )
    int Grid::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;

        Rect selection = Rect::fromPointRadius(opt.OriginX, opt.OriginY, opt.SearchRadius);
        const GridCell* cells = Cells;
        int cellSize = CellSize;
        int half = FullSize / 2;
        int x1 = (selection.left  + half) / cellSize;
        int x2 = (selection.right + half) / cellSize;
        int y1 = (selection.top    + half) / cellSize;
        int y2 = (selection.bottom + half) / cellSize;
        if (x1 < 0) x1 = 0;
        if (y1 < 0) y1 = 0;
        if (x2 >= Width)  x2 = Width  - 1;
        if (y2 >= Height) y2 = Height - 1;

        auto addCell = [&found,cells,cellSize,width=Width](int ix, int iy)
        {
            const GridCell& cell = cells[ix + iy*width];
            if (int size = cell.size)
            {
                int worldX = (ix * cellSize) - cellSize;
                int worldY = (iy * cellSize) - cellSize;
                found.add(cell.objects, size, worldX, worldY);
            }
        };

        // cell search pattern spirals out from the center
        int minX = (x1 + x2) / 2;
        int minY = (y1 + y2) / 2;
        int maxX = minX, maxY = minY;
        addCell(minX, minY);

        for (;;)
        {
            bool didExpand = false;
            if (minX > x1) { // test all cells to the left
                --minX; didExpand = true;
                for (int y = minY; y <= maxY; ++y) addCell(minX, y);
            }
            if (maxX < x2) { // test all cells to the right
                ++maxX; didExpand = true;
                for (int y = minY; y <= maxY; ++y) addCell(maxX, y);
            }
            if (minY > y1) { // test all top cells
                --minY; didExpand = true;
                for (int x = minX; x <= maxX; ++x) addCell(x, minY);
            }
            if (maxY < y2) { // test all bottom cells
                ++maxY; didExpand = true;
                for (int x = minX; x <= maxX; ++x) addCell(x, maxY);
            }
            if (!didExpand)
                break;
        }
        return spatial::findNearby(outResults, opt, found);
    }

    static const Color Brown  = { 139, 69,  19, 150 };
    static const Color VioletDim = { 199, 21, 133, 100 };
    static const Color VioletBright = { 199, 21, 133, 150 };
    static const Color Blue   = { 95, 158, 160, 200 };
    static const Color Yellow = { 255, 255,  0, 200 };

    void Grid::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        GridCell* nodes = Cells;
        int width = Width;
        int height = Height;
        int half = FullSize / 2;
        int cellSize = CellSize;
        char text[128];
        Rect visible = opt.visibleWorldRect;
        visualizer.drawRect(-half, -half, +half, +half, Yellow);

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

                visualizer.drawRect(nodeR.left, nodeR.top, nodeR.right, nodeR.bottom, Brown);

                GridCell& cell = nodes[gridX + gridY*width];
                int cx = nodeR.centerX();
                int cy = nodeR.centerY();

                if (opt.nodeText)
                {
                    snprintf(text, sizeof(text), "CELL n=%d", cell.size);
                    visualizer.drawText(nodeR.centerX(), nodeR.centerY(), cellSize, text, Yellow);
                }

                int count = cell.size;
                SpatialObject** const items = cell.objects;
                for (int i = 0; i < count; ++i)
                {
                    const SpatialObject& o = *items[i];
                    if (opt.objectBounds)
                        visualizer.drawRect(o.x-o.rx, o.y-o.ry, o.x+o.rx, o.y+o.ry, VioletBright);
                    if (opt.objectToLeafLines)
                        visualizer.drawLine(cx, cy, o.x, o.y, VioletDim);
                    if (opt.objectText)
                    {
                        snprintf(text, sizeof(text), "o=%d", o.objectId);
                        visualizer.drawText(o.x, o.y, o.rx*2, text, Blue);
                    }
                }
            }
        }
    }


}
