#include "Grid.h"
#include "GridCellView.h"

namespace spatial
{
    Grid::Grid(int worldSize, int cellSize)
        : Spatial{worldSize}
        , View{worldSize, cellSize}
    {
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
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.totalMemory();
        return bytes;
    }

    void Grid::smallestCellSize(int cellSize)
    {
        View = { WorldSize, cellSize };
        FullSize = View.Width * cellSize;
        rebuild();
    }

    void Grid::clear()
    {
        Objects.clear();
        memset(View.Cells, 0, sizeof(GridCell) * View.Width * View.Height);
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

        GridCell* cells = front.allocArrayZeroed<GridCell>(View.NumCells);
        GridCellView view { cells, View };
        int cellCapacity = CellCapacity;

        for (SpatialObject& o : Objects)
        {
            if (o.active)
            {
                view.insert(front, o, cellCapacity);
            }
        }
        View = view;
    }

    CollisionPairs Grid::collideAll(const CollisionParams& params)
    {
        Collider collider { *FrontAlloc, Objects.maxObjects() };

        GridCell* cells = View.Cells;
        int numCells = View.NumCells;
        for (int i = 0; i < numCells; ++i)
        {
            const GridCell& cell = cells[i];
            if (cell.size > 1)
            {
                collider.collideObjects({cell.objects, cell.size}, cell.loyalty, params);
            }
        }

        CollisionPairs results = collider.getResults(params);
        if (params.showCollisions)
        {
            Dbg.setCollisions(results);
        }
        return results;
    }

    #pragma warning( disable : 6262 )
    int Grid::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;
        GridCellView view = View; // make a copy for thread safety
        int numResults = 0;
        if (view.findNodes(opt, found))
        {
            numResults = spatial::findNearby(outResults, Objects.maxObjects(), opt, found);
        }
        
        if (opt.DebugId)
        {
            DebugFindNearby dfn;
            dfn.SearchArea   = opt.SearchRect;
            dfn.RadialFilter = opt.RadialFilter;
            Rect cell;
            if (view.toCellRect(opt.SearchRect, cell))
            {
                dfn.SelectedRect = view.toWorldRect(cell);
                dfn.TopLeft      = view.toWorldRect(cell.x1, cell.y1);
                dfn.BotRight     = view.toWorldRect(cell.x2, cell.y2);
            }
            dfn.addCells(found);
            dfn.addResults(outResults, numResults);
            Dbg.setFindNearby(opt.DebugId, std::move(dfn));
        }

        return numResults;
    }

    void Grid::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        GridCellView view = View; // make a copy for thread safety
        view.debugVisualize(opt, visualizer);

        if (opt.searchDebug)
        {
            Dbg.draw(visualizer, opt, Objects.data());
        }
    }
}
