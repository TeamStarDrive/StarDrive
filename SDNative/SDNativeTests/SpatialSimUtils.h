#pragma once
#include "SpatialSimObject.h"
#include <spatial/Spatial.h>

static void insertAll(spatial::Spatial& tree, std::vector<MyGameObject>& objects)
{
    tree.clear();
    for (MyGameObject& o : objects)
    {
        o.vel.x = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 5000.0f;
        o.vel.y = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 5000.0f;

        spatial::SpatialObject qto { o.loyalty, o.type, 0,
                                     (int)o.pos.x, (int)o.pos.y,
                                     (int)o.radius, (int)o.radius };
        o.spatialId = tree.insert(qto);
    }
    tree.rebuild();
}

static rpp::Vector2 getRandomOffset(float radius)
{
    return { ((rand() / static_cast<float>(RAND_MAX)) * radius * 2) - radius,
             ((rand() / static_cast<float>(RAND_MAX)) * radius * 2) - radius, };
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
        o.pos = getRandomOffset(universeRadius);
        o.radius = objectRadius;
        o.loyalty = (i % 2) == 0 ? 1 : 2;
        o.type = ObjectType_Ship;
        objects.push_back(o);
    }
    return objects;
}

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
        o.pos = getRandomOffset(universeRadius - solarRadius);
        systems.push_back(o);
    }

    for (int i = 0; i < numObjects; ++i)
    {
        const MyGameObject& sys = systems[getRandomIndex(systems.size())];

        rpp::Vector2 off = getRandomOffset(solarRadius);

        // limit offset inside the solar system radius
        float d = off.length();
        if (d > solarRadius)
            off *= (solarRadius / d);

        MyGameObject o;
        o.pos = sys.pos + off;
        o.radius = objectRadius;
        o.loyalty = (i % 2) == 0 ? 1 : 2;
        o.type = ObjectType_Ship;
        objects.push_back(o);
    }
    return objects;
}

template<class Func> static void measureEachObj(const char* what, int iterations,
                                                const std::vector<MyGameObject>& objects, Func&& func)
{
    rpp::Timer t;
    for (int x = 0; x < iterations; ++x) {
        for (const MyGameObject& o : objects) { func(o); }
    }
    double e = t.elapsed_ms();
    int total_operations = objects.size() * iterations;
    printf("QuadTree %s(%zu) total: %.2fms  avg: %.3fus\n",
        what, objects.size(), e, (e / total_operations)*1000);
}

template<class VoidFunc> static void measureIterations(const char* what, int iterations,
                                                       int objectsPerFunc, VoidFunc&& func)
{
    rpp::Timer t;
    for (int x = 0; x < iterations; ++x) { func(); }
    double e = t.elapsed_ms();
    printf("QuadTree %s(%d) total: %.2fms  avg: %.3fus\n",
        what, objectsPerFunc, e, ((e*1000)/iterations)/objectsPerFunc);
}

