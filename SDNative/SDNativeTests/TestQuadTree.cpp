#include <rpp/timer.h>
#include <rpp/tests.h>
#include "SpatialSimUtils.h"

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    TestCase(update_perf)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        measureIterations("Qtree::updateAll", 100, swo.objects.size(), [&]()
        {
            swo.spatial->rebuild();
        });
    }

    TestCase(search_perf)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        std::vector<int> results(1024, 0);

        measureEachObj("Qtree::findNearby", 100, swo.objects, [&](const MyGameObject& o)
        {
            spatial::SearchOptions opt;
            opt.SearchRect = spatial::Rect::fromPointRadius((int)o.pos.x, (int)o.pos.y, p.defaultSensorRange);
            opt.MaxResults = 1024;
            opt.Exclude = o.spatialId;
            opt.ExcludeLoyalty = o.loyalty;
            int n = swo.spatial->findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        SimParams p {};
        p.numObjects = 100'000;
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        std::vector<int> results(1024, 0);

        measureIterations("Qtree::collideAll", 100, swo.objects.size(), [&]()
        {
            swo.spatial->collideAll({});
        });
    }
};
