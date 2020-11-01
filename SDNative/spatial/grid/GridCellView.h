#pragma once
#include "../Spatial.h"
#include "../SlabAllocator.h"
#include "../SpatialDebug.h"
#include "GridCell.h"

namespace spatial
{
    /**
     * Transient view of GridCell's
     * Does not own any data, only contains necessary utilities for managing a grid
     */
    struct GridCellView
    {
        GridCell* Cells = nullptr;
        int Width = 0;
        int Height = 0;
        int GridSize = 0; // size of this particular grid array
        int CellSize = 0; // size a single cell inside the grid
        int NumCells = 0;
        Rect Coords = Rect::Zero();

        GridCellView(GridCell* cells, const GridCellView& v)
            : Cells{cells}, Width{v.Width}, Height{v.Height}
            , GridSize{v.GridSize}, CellSize{v.CellSize}
            , NumCells{v.NumCells}, Coords{v.Coords}
        {
        }

        GridCellView(GridCell* cells, int width, int height, int gridSize, int cellSize)
            : Cells{cells}, Width{width}, Height{height}
            , GridSize{gridSize}, CellSize{cellSize}
            , NumCells{width*height}
        {
            setViewOffset1();
        }

        // Sets up Width, Height, GridSize, CellSize parameters
        GridCellView(int desiredGridSize, int cellSize);

        // default offsets for first level grid
        void setViewOffset1()
        {
            int half = GridSize / 2;
            Coords = {-half, -half, +half, +half};
        }

        // offsets for a second level grid
        void setViewOffset2(GridCell* cells, int gridX, int gridY, const GridCellView& topLevel)
        {
            Cells = cells;
            Coords.x1 = topLevel.Coords.x1 + (gridX * topLevel.CellSize);
            Coords.y1 = topLevel.Coords.y1 + (gridY * topLevel.CellSize);
            Coords.x2 = Coords.x1 + GridSize;
            Coords.y2 = Coords.y1 + GridSize;
        }

        // Converts world coordinates into cell coordinates
        bool toCellRect(const Rect& worldCoords, Rect& outCellCoords) const;

        void insert(SlabAllocator& allocator, SpatialObject& o, int cellCapacity);
        int findNodes(const SearchOptions& opt, FoundNodes& found) const;
        void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const;
        
        SPATIAL_FINLINE Point toWorldPoint(int x, int y) const
        {
            int cellSize = CellSize;
            int worldX = Coords.x1 + (x * cellSize);
            int worldY = Coords.y1 + (y * cellSize);
            return { worldX, worldY };
        }

        SPATIAL_FINLINE Rect toWorldRect(int x, int y) const
        {
            int cellSize = CellSize;
            int worldX = Coords.x1 + (x * cellSize);
            int worldY = Coords.y1 + (y * cellSize);
            return { worldX, worldY, worldX + cellSize, worldY + cellSize };
        }

        SPATIAL_FINLINE Rect toWorldRect(Rect r) const
        {
            int cellSize = CellSize;
            int worldX1 = Coords.x1 + (r.x1 * cellSize);
            int worldY1 = Coords.y1 + (r.y1 * cellSize);
            int worldX2 = Coords.x1 + (r.x2 * cellSize) + cellSize;
            int worldY2 = Coords.y1 + (r.y2 * cellSize) + cellSize;
            return { worldX1, worldY1, worldX2, worldY2 };
        }
    };

    // transform a cell at index X,Y
    // to world position which falls to the center of the cell
    SPATIAL_FINLINE Point toWorldCellCenter(int x, int y, int worldHalf, int cellSize)
    {
        int offset = (cellSize >> 1) - worldHalf;
        return { (x * cellSize) + offset,
                 (y * cellSize) + offset };
    }
}