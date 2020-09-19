#include <ctime>
#include <quadtree/QuadTree.h>
#include <rpp/timer.h>
#include <rpp/tests.h>
#include "ImGuiQtreeVis.h"

struct MyGameObject
{
    float x = 0.0f;
    float y = 0.0f;
    float radius = 0.0f;
    int spatialId = -1;
    float vx = 0.0f;
    float vy = 0.0f;
    uint8_t loyalty = 0;
    uint8_t type = 0;
};

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    static void insertAll(tree::QuadTree& tree, std::vector<MyGameObject>& objects)
    {
        for (MyGameObject& o : objects)
        {
            o.vx = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 200000.0f;
            o.vy = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 200000.0f;

            tree::QtreeObject qto { o.loyalty, o.type, 0, (int)o.x, (int)o.y, (int)o.radius, (int)o.radius };
            o.spatialId = tree.insert(qto);
        }
        tree.rebuild();
    }
    
    static float getRandomOffset(float radius)
    {
        return ((rand() / static_cast<float>(RAND_MAX)) * radius * 2) - radius;
    }

    static int getRandomIndex(size_t arraySize)
    {
        return (int)((rand() / static_cast<float>(RAND_MAX)) * (arraySize - 1));
    }

    static std::vector<MyGameObject> createObjects(int numObjects, float objectRadius, float universeSize)
    {
        std::vector<MyGameObject> objects;
        float universeRadius = universeSize/2;
        srand(1452);

        for (int i = 0; i < numObjects; ++i)
        {
            MyGameObject o;
            o.x = getRandomOffset(universeRadius);
            o.y = getRandomOffset(universeRadius);
            o.radius = objectRadius;
            o.loyalty = (i % 2) == 0 ? 1 : 2;
            o.type = tree::ObjectType_Ship;
            objects.push_back(o);
        }
        return objects;
    }

    static float len(float x, float y) { return sqrtf(x*x + y*y); }

    // spawn ships around limited cluster of solar systems
    static std::vector<MyGameObject> createObjects(int numObjects, float objectRadius, float universeSize,
                                                   int numSolarSystems, float solarRadius)
    {
        std::vector<MyGameObject> objects;
        std::vector<MyGameObject> systems;
        float universeRadius = universeSize/2;
        srand(1452);

        for (int i = 0; i < numSolarSystems; ++i)
        {
            MyGameObject o;
            o.x = getRandomOffset(universeRadius - solarRadius);
            o.y = getRandomOffset(universeRadius - solarRadius);
            systems.push_back(o);
        }

        for (int i = 0; i < numObjects; ++i)
        {
            const MyGameObject& sys = systems[getRandomIndex(systems.size())];

            float offX = getRandomOffset(solarRadius);
            float offY = getRandomOffset(solarRadius);

            // limit offset inside the solar system radius
            float d = len(offX, offY);
            if (d > solarRadius)
            {
                float multiplier = solarRadius / d;
                offX = (offX * multiplier);
                offY = (offY * multiplier);
            }

            MyGameObject o;
            o.x = sys.x + offX;
            o.y = sys.y + offY;
            o.radius = objectRadius;
            o.loyalty = (i % 2) == 0 ? 1 : 2;
            o.type = tree::ObjectType_Ship;
            objects.push_back(o);
        }
        return objects;
    }

    template<class Func> static void measureEachObj(const char* what, int iterations,
                                                    const std::vector<MyGameObject>& objects, Func&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x) {
            for (const MyGameObject& o : objects) {
                func(o);
            }
        }
        double e = t.elapsed_ms();
        int total_operations = objects.size() * iterations;
        printf("QuadTree %s total: %.2fms  avg: %.3fus\n", what, e, (e / total_operations)*1000);
    }

    template<class VoidFunc> static void measureIterations(const char* what, int iterations,
                                                           int objectsPerFunc, VoidFunc&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x) {
            func();
        }
        double e = t.elapsed_ms();
        printf("QuadTree %s total: %.2fms  avg: %.3fus\n", what, e, ((e*1000)/iterations)/objectsPerFunc);
    }

    static void runSimulation(float timeStep, tree::QuadTree& tree, std::vector<MyGameObject>& objects)
    {
        float universeLo = tree.universeSize() * -0.5f;
        float universeHi = tree.universeSize() * +0.5f;

        for (MyGameObject& o : objects)
        {
            if (o.x < universeLo || o.x > universeHi)
                o.vx = -o.vx;

            if (o.y < universeLo || o.y > universeHi)
                o.vy = -o.vy;

            o.x += o.vx * timeStep;
            o.y += o.vy * timeStep;
            tree.update(o.spatialId, (int)o.x, (int)o.y);
        }
        tree.rebuild();
    }

    static constexpr int UNIVERSE_SIZE = 5'000'000;
    static constexpr int SMALLEST_SIZE = 1024;
    static constexpr float OBJECT_RADIUS = 120;
    static constexpr int NUM_OBJECTS = 30'000;
    static constexpr float DEFAULT_SENSOR_RANGE = 10'000;

    static constexpr int SOLAR_SYSTEMS = 32;
    static constexpr int SOLAR_RADIUS = 100'000;

    TestCase(update_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };

        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(tree, objects);

        //tree::vis::show(0.0001f, tree, [&](float timeStep)
        //{
        //    runSimulation(timeStep, tree, objects);
        //});

        measureIterations("Qtree.updateAll", 1000, objects.size(), [&]()
        {
            tree.rebuild();
        });
    }

    TestCase(search_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(tree, objects);

        std::vector<int> results(1024, 0);

        measureEachObj("findNearby", 200, objects, [&](const MyGameObject& o)
        {
            tree::SearchOptions opt;
            opt.OriginX = (int)o.x;
            opt.OriginY = (int)o.y;
            opt.SearchRadius = (int)DEFAULT_SENSOR_RANGE;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.spatialId;
            opt.FilterExcludeByLoyalty = o.loyalty;
            int n = tree.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<MyGameObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, UNIVERSE_SIZE,
                                                          SOLAR_SYSTEMS, SOLAR_RADIUS);
        insertAll(tree, objects);

        std::vector<int> results(1024, 0);

        measureIterations("collideAll", 100, objects.size(), [&]()
        {
            tree.collideAll(1.0f/60.0f, [](int objectA, int objectB)->int
            {
                return 0;
            });
        });
    }
};
