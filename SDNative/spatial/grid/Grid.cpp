#include "Grid.h"
#include "GridCellView.h"
#include "../Search.h"

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
        View = GridCellView{ WorldSize, cellSize };
        FullSize = View.Width * cellSize;
        rebuild();
    }

    void Grid::clear()
    {
        Objects.clear();
        memset(Root, 0, sizeof(GridCell) * View.Width * View.Height);
        Dbg.clear();
    }

    SpatialRoot* Grid::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        SlabAllocator& front = *FrontAlloc;
        front.reset();
        
        Objects.submitPending();
        
        GridCell* cells = front.allocArrayZeroed<GridCell>(View.NumCells);
        int cellCapacity = CellCapacity;

        for (SpatialObject& o : Objects)
        {
            if (o.active)
            {
                View.insert(cells, front, o, cellCapacity);
            }
        }
        
        Root = cells;
        return reinterpret_cast<SpatialRoot*>(cells);
    }

    CollisionPairs Grid::collideAll(SpatialRoot* root, const CollisionParams& params)
    {
        GridCell* cells = reinterpret_cast<GridCell*>(root);
        Collider collider { *FrontAlloc, Objects.maxObjects() };

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
    int Grid::findNearby(SpatialRoot* root, int* outResults, const SearchOptions& opt) const
    {
        GridCell* cells = reinterpret_cast<GridCell*>(root);
        FoundCells found;
        int numResults = 0;
        if (View.findNodes(cells, opt, found))
        {
            numResults = spatial::findNearby(outResults, Objects.data(), Objects.maxObjects(), opt, found);
        }
        
        if (opt.DebugId)
        {
            DebugFindNearby dfn;
            dfn.SearchArea   = opt.SearchRect;
            dfn.RadialFilter = opt.RadialFilter;
            Rect cell;
            if (View.toCellRect(opt.SearchRect, cell))
            {
                dfn.SelectedRect = View.toWorldRect(cell);
                dfn.TopLeft      = View.toWorldRect(cell.x1, cell.y1);
                dfn.BotRight     = View.toWorldRect(cell.x2, cell.y2);
            }
            dfn.addCells(found);
            dfn.addResults(outResults, numResults);
            Dbg.setFindNearby(opt.DebugId, std::move(dfn));
        }

        return numResults;
    }

    void Grid::debugVisualize(SpatialRoot* root, const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        GridCell* cells = reinterpret_cast<GridCell*>(root);
        GridCellView view { View };
        view.debugVisualize(cells, opt, visualizer);

        if (opt.searchDebug)
        {
            Dbg.draw(visualizer, opt, Objects.data());
        }
    }
}
