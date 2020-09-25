#include <rpp/timer.h>
#include <rpp/tests.h>
#include "SpatialSimUtils.h"

TestImpl(TestGrid)
{
    TestInit(TestGrid)
    {
    }

    TestCase(update_perf)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        measureIterations("Grid::rebuild", 100, swo.objects.size(), [&]()
        {
            swo.spatial->rebuild();
        });
    }

    TestCase(search_perf)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        std::vector<int> results(1024, 0);

        measureEachObj("Grid::findNearby", 100, swo.objects, [&](const MyGameObject& o)
        {
            spatial::SearchOptions opt;
            opt.OriginX = (int)o.pos.x;
            opt.OriginY = (int)o.pos.y;
            opt.SearchRadius = p.defaultSensorRange;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.spatialId;
            opt.FilterExcludeByLoyalty = o.loyalty;
            int n = swo.spatial->findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        SimParams p {};
        p.numObjects = 100'000;
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        std::vector<int> results(1024, 0);

        measureIterations("Grid::collideAll", 10, swo.objects.size(), [&]()
        {
            swo.spatial->collideAll(1.0f/60.0f, [](int objectA, int objectB) -> spatial::CollisionResult
            {
                return spatial::CollisionResult::NoSideEffects;
            });
        });
    }
};
