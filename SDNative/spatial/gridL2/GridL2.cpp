#include "GridL2.h"
#include "../Search.h"

namespace spatial
{
    GridL2::GridL2(int worldSize, int cellSize, int cellSize2)
        : Spatial{worldSize}
        , TopLevel{worldSize, cellSize}
        , SecondLevel{cellSize, cellSize2}
    {
        smallestCellSize(cellSize);
    }

    GridL2::~GridL2()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }
    
    uint32_t GridL2::totalMemory() const
    {
        uint32_t bytes = sizeof(GridL2);
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.totalMemory();
        return bytes;
    }

    void GridL2::smallestCellSize(int cellSize)
    {
        TopLevel = { WorldSize, cellSize };
        SecondLevel = { cellSize, SecondLevel.CellSize };
        rebuild();
    }

    void GridL2::clear()
    {
        Objects.clear();
        memset(ArrayOfCells, 0, sizeof(GridCell*) * TopLevel.Width * TopLevel.Height);
        Dbg.clear();
    }

    SpatialRoot* GridL2::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        SlabAllocator& front = *FrontAlloc;
        front.reset();
        
        Objects.submitPending();

        GridCell** arrayOfCells = front.allocArrayZeroed<GridCell*>(TopLevel.NumCells);
        int cellCapacity = CellCapacity;
        int topLevelWidth = TopLevel.Width;
        GridCellView view2 = SecondLevel;

        for (SpatialObject& o : Objects)
        {
            if (!o.active)
                continue;

            Rect topLevel;
            if (!TopLevel.toCellRect(o.rect, topLevel))
                continue;

            for (int y = topLevel.y1; y <= topLevel.y2; ++y)
            {
                for (int x = topLevel.x1; x <= topLevel.x2; ++x)
                {
                    GridCell* cells = arrayOfCells[x + y*topLevelWidth];
                    if (!cells)
                    {
                        cells = front.allocArrayZeroed<GridCell>(view2.NumCells);
                        arrayOfCells[x + y*topLevelWidth] = cells;
                    }
                    view2.setViewOffset2(x, y, TopLevel);
                    view2.insert(cells, front, o, cellCapacity);
                }
            }
        }

        ArrayOfCells = arrayOfCells;
        return reinterpret_cast<SpatialRoot*>(arrayOfCells);
    }

    CollisionPairs GridL2::collideAll(SpatialRoot* root, const CollisionParams& params)
    {
        Collider collider { *FrontAlloc, Objects.maxObjects() };
        GridCell** arrayOfCells = reinterpret_cast<GridCell**>(root);
        int numTopLevelCells = TopLevel.NumCells;
        int numSecondLevelCells = SecondLevel.NumCells;

        for (int i = 0; i < numTopLevelCells; ++i)
        {
            if (const GridCell* secondLevel = arrayOfCells[i])
            {
                for (int j = 0; j < numSecondLevelCells; ++j)
                {
                    const GridCell& cell = secondLevel[j];
                    if (cell.size > 1)
                    {
                        collider.collideObjects({cell.objects, cell.size}, cell.loyalty, params);
                    }
                }
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
    int GridL2::findNearby(SpatialRoot* root, int* outResults, const SearchOptions& opt) const
    {
        Rect topLevel;
        if (!TopLevel.toCellRect(opt.SearchRect, topLevel))
            return 0;

        FoundCells found;

        int topLevelWidth = TopLevel.Width;
        GridCellView view2 = SecondLevel;
        GridCell** arrayOfCells = reinterpret_cast<GridCell**>(root);

        for (int y = topLevel.y1; y <= topLevel.y2; ++y)
        {
            for (int x = topLevel.x1; x <= topLevel.x2; ++x)
            {
                if (GridCell* cells = arrayOfCells[x + y*topLevelWidth])
                {
                    view2.setViewOffset2(x, y, TopLevel);
                    view2.findNodes(cells, opt, found);
                }
            }
        }

        int numResults = found.filterResults(outResults, Objects, opt);

        if (opt.DebugId)
        {
            DebugFindNearby dfn;
            dfn.SearchArea   = opt.SearchRect;
            dfn.RadialFilter = opt.RadialFilter;
            dfn.SelectedRect = TopLevel.toWorldRect(topLevel);
            dfn.TopLeft      = TopLevel.toWorldRect(topLevel.x1, topLevel.y1);
            dfn.BotRight     = TopLevel.toWorldRect(topLevel.x2, topLevel.y2);
            dfn.addCells(found);
            dfn.addResults(outResults, numResults);
            Dbg.setFindNearby(opt.DebugId, std::move(dfn));
        }

        return numResults;
    }

    void GridL2::debugVisualize(SpatialRoot* root, const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        GridCell** arrayOfCells = reinterpret_cast<GridCell**>(root);
        TopLevel.debugVisualize(nullptr, opt, visualizer);

        Rect visibleTop;
        if (TopLevel.toCellRect(opt.visibleWorldRect, visibleTop))
        {
            int topLevelWidth = TopLevel.Width;
            GridCellView view2 = SecondLevel;

            for (int y = visibleTop.y1; y <= visibleTop.y2; ++y)
            {
                for (int x = visibleTop.x1; x <= visibleTop.x2; ++x)
                {
                    if (GridCell* secondLevel = arrayOfCells[x + y*topLevelWidth])
                    {
                        view2.setViewOffset2(x, y, TopLevel);
                        view2.debugVisualize(secondLevel, opt, visualizer);
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
