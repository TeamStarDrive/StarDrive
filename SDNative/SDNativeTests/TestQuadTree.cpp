#include <ctime>
#include <quadtree/QuadTree.h>
#include <rpp/timer.h>
#include <rpp/tests.h>
#include "ImGuiQtreeVis.h"

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    static int getRandomOffset(int radius)
    {
        float relativeRandom = rand() / static_cast<float>(RAND_MAX);
        return (int)(relativeRandom * radius * 2) - radius;
    }

    static int getRandomIndex(size_t arraySize)
    {
        float relativeRandom = rand() / static_cast<float>(RAND_MAX);
        return (int)(relativeRandom * (arraySize - 1));
    }

    static std::vector<tree::QtreeObject> createObjects(int numObjects, int objectRadius, int universeSize)
    {
        std::vector<tree::QtreeObject> objects;
        std::vector<tree::QtreeObject> systems;
        int universeRadius = universeSize/2;
        srand(1452);

        for (int i = 0; i < numObjects; ++i)
        {
            int x = getRandomOffset(universeRadius);
            int y = getRandomOffset(universeRadius);
            uint8_t loyalty = (i % 2) == 0 ? 1 : 2;
            tree::QtreeObject o {loyalty, tree::ObjectType_Ship, i, x, y, objectRadius, objectRadius};
            objects.push_back(o);
        }
        return objects;
    }

    static float len(float x, float y) { return sqrtf(static_cast<float>(x*x + y*y)); }

    // spawn ships around limited cluster of solar systems
    static std::vector<tree::QtreeObject> createObjects(int numObjects, int objectRadius, int universeSize,
                                                        int numSolarSystems, int solarRadius)
    {
        std::vector<tree::QtreeObject> objects;
        std::vector<tree::QtreeObject> systems;
        int universeRadius = universeSize/2;
        srand(1452);

        for (int i = 0; i < numSolarSystems; ++i)
        {
            int x = getRandomOffset(universeRadius - solarRadius);
            int y = getRandomOffset(universeRadius - solarRadius);
            tree::QtreeObject o {0, tree::ObjectType_Any, 0, x, y, solarRadius, solarRadius};
            systems.push_back(o);
        }

        for (int i = 0; i < numObjects; ++i)
        {
            const tree::QtreeObject& sys = systems[getRandomIndex(systems.size())];

            int offX = getRandomOffset(solarRadius);
            int offY = getRandomOffset(solarRadius);

            // limit offset inside the solar system radius
            float d = len(offX, offY);
            if (d > solarRadius)
            {
                float multiplier = solarRadius / d;
                offX = (int)(offX * multiplier);
                offY = (int)(offY * multiplier);
            }

            int x = sys.x + offX;
            int y = sys.y + offY;

            uint8_t loyalty = (i % 2) == 0 ? 1 : 2;
            tree::QtreeObject o {loyalty, tree::ObjectType_Ship, i, x, y, objectRadius, objectRadius};
            objects.push_back(o);
        }
        return objects;
    }

    static std::vector<tree::QtreeObject> createTestSpace(tree::QuadTree& tree, int numObjects, int objectRadius)
    {
        std::vector<tree::QtreeObject> objects = createObjects(numObjects, objectRadius, tree.universeSize());
        tree.insert(objects);
        tree.rebuild();
        return objects;
    }

    template<class Func> static void measureEachObj(const char* what, int iterations,
                                                    const std::vector<tree::QtreeObject>& objects, Func&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x) {
            for (const tree::QtreeObject& o : objects) {
                func(o);
            }
        }
        double e = t.elapsed_ms();
        int total_operations = objects.size() * iterations;
        printf("QuadTree %s total: %.2fms  avg: %.2fus\n", what, e, (e / total_operations)*1000);
    }

    template<class VoidFunc> static void measureIterations(const char* what, int iterations,
                                                           int objectsPerFunc, VoidFunc&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x) {
            func();
        }
        double e = t.elapsed_ms();
        printf("QuadTree %s total: %.2fms  avg: %.2fus\n", what, e, ((e*1000)/iterations)/objectsPerFunc);
    }


    static constexpr int UNIVERSE_SIZE = 5'000'000;
    static constexpr int SMALLEST_SIZE = 1024;
    static constexpr int OBJECT_RADIUS = 120;
    static constexpr int NUM_OBJECTS = 30'000;
    static constexpr int DEFAULT_SENSOR_RANGE = 10000;

    static constexpr int SOLAR_SYSTEMS = 32;
    static constexpr int SOLAR_RADIUS = 100'000;

    TestCase(update_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };

        std::vector<tree::QtreeObject> objects = createObjects(NUM_OBJECTS, OBJECT_RADIUS, tree.universeSize(),
                                                               SOLAR_SYSTEMS, SOLAR_RADIUS);
        tree.insert(objects);
        tree.rebuild();

        tree::vis::show(0.0001f, tree);

        measureIterations("Qtree.updateAll", 1000, objects.size(), [&]()
        {
            tree.rebuild();
        });
    }

    TestCase(search_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };

        std::vector<tree::QtreeObject> objects = createTestSpace(tree, NUM_OBJECTS, OBJECT_RADIUS);
        std::vector<int> results(1024, 0);

        measureEachObj("findNearby", 200, objects, [&](const tree::QtreeObject& o)
        {
            tree::SearchOptions opt;
            opt.OriginX = o.x;
            opt.OriginY = o.y;
            opt.SearchRadius = DEFAULT_SENSOR_RANGE;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.objectId;
            opt.FilterExcludeByLoyalty = o.loyalty;
            int n = tree.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::QtreeObject> objects = createTestSpace(tree, NUM_OBJECTS, OBJECT_RADIUS);

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
