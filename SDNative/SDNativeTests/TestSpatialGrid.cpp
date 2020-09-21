#include <rpp/timer.h>
#include <rpp/tests.h>
#include <spatial/grid/Grid.h>
#include "SpatialSim.h"

TestImpl(TestGrid)
{
    TestInit(TestGrid)
    {
    }

    TestCase(update_perf)
    {
        spatial::Grid grid { UNIVERSE_SIZE, GRID_CELL_SIZE };
        Simulation sim { grid };
        if (SHOW_SIMULATION)
            sim.show();

        measureIterations("Grid.rebuild", 1000, sim.objects.size(), [&]()
        {
            grid.rebuild();
        });
    }

    TestCase(search_perf)
    {
        spatial::Grid grid { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(grid, objects);

        std::vector<int> results(1024, 0);

        measureEachObj("findNearby", 30, objects, [&](const MyGameObject& o)
        {
            spatial::SearchOptions opt;
            opt.OriginX = (int)o.pos.x;
            opt.OriginY = (int)o.pos.y;
            opt.SearchRadius = (int)DEFAULT_SENSOR_RANGE;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.spatialId;
            opt.FilterExcludeByLoyalty = o.loyalty;
            int n = grid.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        spatial::Grid grid { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(grid, objects);

        std::vector<int> results(1024, 0);

        measureIterations("collideAll", 100, objects.size(), [&]()
        {
            grid.collideAll(1.0f/60.0f, [](int objectA, int objectB) -> spatial::CollisionResult
            {
                return spatial::CollisionResult::NoSideEffects;
            });
        });
    }
};
