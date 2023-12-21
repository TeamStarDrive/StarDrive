#include <src/rpp/timer.h>
#include <src/rpp/tests.h>
#include "SpatialSimUtils.h"
#include "Simulation.h"

TestImpl(QuadTree)
{
    std::vector<int> results = { 1024, 0 };

    TestInit(QuadTree)
    {
    }

    spatial::SearchOptions makeOpts(const MyGameObject& o, int sensorRange) const
    {
        spatial::SearchOptions opt;
        opt.SearchRect   = spatial::Rect::fromPointRadius((int)o.pos.x, (int)o.pos.y, sensorRange);
        opt.RadialFilter = spatial::Circle{(int)o.pos.x, (int)o.pos.y, sensorRange};
        opt.MaxResults   = results.size();
        return opt;
    }

    std::vector<const MyGameObject*> findNearby(const SpatialWithObjects& swo, 
                                                spatial::SearchOptions& opt,
                                                ObjectType type = ObjectType_Any)
    {
        opt.Type = type;
        int n = swo.spatial->findNearby(swo.root, results.data(), opt);
        std::vector<const MyGameObject*> out(n, nullptr);
        for (int i = 0; i < n; ++i)
            out[i] = &swo.objects[results[i]];
        return out;
    }

    TestCase(findNearby_onlyloyalty)
    {
        SimParams p {};
        p.numObjects = 5'000;
        p.spawnProjectiles = true;

        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);
        
        for (const MyGameObject& o : swo.objects)
        {
            if (o.type != ObjectType_Ship)
                continue;

            spatial::SearchOptions opt = makeOpts(o, p.defaultSensorRange);
            opt.OnlyLoyalty = o.loyalty;

            std::vector<const MyGameObject*> ships = findNearby(swo, opt, ObjectType_Ship);
            AssertMsg(ships.size() > 0u, "%s findNearby(Ships, %d)", o.toString().c_str(), p.defaultSensorRange);
            for (const MyGameObject* go : ships)
            {
                AssertThat(go->loyalty, o.loyalty);
                AssertThat(go->type, ObjectType_Ship);
                AssertLessOrEqual(o.distanceTo(*go), p.defaultSensorRange + go->radius + 0.5f);
            }
            
            std::vector<const MyGameObject*> projs = findNearby(swo, opt, ObjectType_Proj);
            AssertMsg(projs.size() > 0u, "%s findNearby(Proj, %d)", o.toString().c_str(), p.defaultSensorRange);
            for (const MyGameObject* go : projs)
            {
                AssertThat(go->loyalty, o.loyalty);
                AssertThat(go->type, ObjectType_Proj);
                AssertLessOrEqual(o.distanceTo(*go), p.defaultSensorRange + go->radius + 0.5f);
            }
        }
    }

    TestCase(findNearby_excludeloyalty)
    {
        SimParams p {};
        p.numObjects = 5'000;
        p.spawnProjectiles = true;

        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);
        
        for (const MyGameObject& o : swo.objects)
        {
            if (o.type != ObjectType_Ship)
                continue;

            spatial::SearchOptions opt = makeOpts(o, p.defaultSensorRange);
            opt.ExcludeLoyalty = o.loyalty;

            std::vector<const MyGameObject*> ships = findNearby(swo, opt, ObjectType_Ship);
            // Since we are excluding, we might not find anything
            for (const MyGameObject* go : ships)
            {
                AssertNotEqual(go->loyalty, o.loyalty);
                AssertThat(go->type, ObjectType_Ship);
                AssertLessOrEqual(o.distanceTo(*go), p.defaultSensorRange + go->radius + 0.5f);
            }
            
            std::vector<const MyGameObject*> projs = findNearby(swo, opt, ObjectType_Proj);
            // Since we are excluding, we might not find anything
            for (const MyGameObject* go : projs)
            {
                AssertNotEqual(go->loyalty, o.loyalty);
                AssertThat(go->type, ObjectType_Proj);
                AssertLessOrEqual(o.distanceTo(*go), p.defaultSensorRange + go->radius + 0.5f);
            }
        }
    }

    TestCase(updateAll)
    {
        SimParams p {};
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        measureIterations("Qtree::updateAll", 100, swo.objects.size(), [&]()
        {
            swo.spatial->rebuild();
        });
    }

    TestCase(findNearby)
    {
        SimParams p {};
        p.numObjects = 5'000;
        p.spawnProjectiles = true;
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        measureEachObj("Qtree::findNearby", 100, swo.objects, [&](const MyGameObject& o)
        {
            spatial::SearchOptions opt = makeOpts(o, p.defaultSensorRange);
            opt.Type = ObjectType_Ship;
            opt.Exclude = o.spatialId;
            opt.ExcludeLoyalty = o.loyalty;
            int n = swo.spatial->findNearby(swo.root, results.data(), opt);
        });
    }

    TestCase(collideAll)
    {
        SimParams p {};
        p.numObjects = 10'000;
        p.spawnProjectiles = true;
        SpatialWithObjects swo = createSpatialWithObjects(spatial::SpatialType::QuadTree, p);

        measureIterations("Qtree::collideAll", 100, swo.objects.size(), [&]()
        {
            swo.spatial->collideAll(swo.root, {});
        });
    }
};
