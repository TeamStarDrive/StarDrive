#include <rpp/timer.h>
#include <rpp/tests.h>
#include <spatial/qtree/Qtree.h>
#include "SpatialSim.h"

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    TestCase(update_perf)
    {
        spatial::Qtree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        Simulation sim { tree };
        //spatial::vis::show(sim);

        measureIterations("Qtree.updateAll", 1000, sim.objects.size(), [&]()
        {
            tree.rebuild();
        });
    }

    TestCase(search_perf)
    {
        spatial::Qtree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(tree, objects);

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
            int n = tree.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        spatial::Qtree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(tree, objects);

        std::vector<int> results(1024, 0);

        measureIterations("collideAll", 100, objects.size(), [&]()
        {
            tree.collideAll(1.0f/60.0f, [](int objectA, int objectB) -> spatial::CollisionResult
            {
                return spatial::CollisionResult::NoSideEffects;
            });
        });
    }
};
