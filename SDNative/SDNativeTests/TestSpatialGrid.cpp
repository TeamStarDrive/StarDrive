#include <rpp/timer.h>
#include <rpp/tests.h>
#include "SpatialSimUtils.h"

TestImpl(TestGrid)
{
    TestInit(TestGrid)
    {
    }

    TestCase(rebuild)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        measureIterations("Grid::rebuild", 100, swo.objects.size(), [&]()
        {
            swo.spatial->rebuild();
        });
    }

    TestCase(findNearby)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        std::vector<int> results(1024, 0);

        measureEachObj("Grid::findNearby", 100, swo.objects, [&](const MyGameObject& o)
        {
            spatial::SearchOptions opt;
            opt.SearchRect = spatial::Rect::fromPointRadius((int)o.pos.x, (int)o.pos.y, p.defaultSensorRange);
            opt.MaxResults = 1024;
            opt.Exclude = o.spatialId;
            opt.ExcludeLoyalty = o.loyalty;
            int n = swo.spatial->findNearby(swo.root, results.data(), opt);
        });
    }

    TestCase(collideAll)
    {
        SimParams p {};
        p.numObjects = 100'000;
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::Grid, p);

        std::vector<int> results(1024, 0);

        measureIterations("Grid::collideAll", 10, swo.objects.size(), [&]()
        {
            swo.spatial->collideAll(swo.root, {});
        });
    }
};
