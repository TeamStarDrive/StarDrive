#pragma once
#include "SimParams.h"
#include "SpatialSimObject.h"
#include <rpp/timer.h>

//! @return Random float between [-1.0; +1.0]
static float randFloat()
{
    return ((rand() / static_cast<float>(RAND_MAX)) - 0.5f) * 2;
}

static rpp::Vector2 getRandomOffset(float radius)
{
    // create a random direction and a random radial offset
    rpp::Vector2 dir = rpp::Vector2{ randFloat(), randFloat() }.normalized();
    float randOffset = radius * randFloat();
    return dir * randOffset;
}

static int getRandomIndex(size_t arraySize)
{
    return (int)((rand() / static_cast<float>(RAND_MAX)) * (arraySize - 1));
}

// spawn ships around limited cluster of solar systems
static std::vector<MyGameObject> createObjects(SimParams p)
{
    std::vector<MyGameObject> objects;
    std::vector<MyGameObject> systems;
    float universeRadius = p.universeSize / 2.0f;
    srand(1452);

    for (int i = 0; i < p.solarSystems; ++i)
    {
        MyGameObject o;
        o.pos = p.solarSystems == 1 ? p.singleSystemPos
              : getRandomOffset(universeRadius - p.solarRadius);
        systems.push_back(o);
    }

    for (int i = 0; i < p.numObjects; ++i)
    {
        const MyGameObject& sys = systems[getRandomIndex(systems.size())];
        MyGameObject o;
        o.pos = sys.pos + getRandomOffset(p.solarRadius);
        o.radius = p.objectRadius;
        o.loyalty = (i % 2) != 0 ? p.loyaltyA : p.loyaltyB;
        o.type = ObjectType_Ship;
        objects.push_back(o);
    }
    return objects;
}

struct SpatialWithObjects
{
    std::vector<MyGameObject> objects;
    std::shared_ptr<spatial::Spatial> spatial;

	SpatialWithObjects() = default;
	explicit SpatialWithObjects(std::vector<MyGameObject>&& objects)
		: objects{std::move(objects)}
	{}

	void createSpatial(spatial::SpatialType type, SimParams p)
	{
        int cellSize = 0;
        int cellSize2 = 0;
        switch (type)
        {
            default:
            case spatial::SpatialType::Grid:
                cellSize = p.gridCellSize;
                break;
            case spatial::SpatialType::QuadTree:
                cellSize = p.qtreeCellSize;
                break;
            case spatial::SpatialType::GridL2:
                cellSize = p.gridL2CellSize;
                cellSize2 = p.gridL2CellSize2;
                break;
        }

		spatial = spatial::Spatial::create(type, p.universeSize, cellSize, cellSize2);

	    for (MyGameObject& o : objects)
	    {
	    	if (p.useRandomVelocity)
	    	{
		        o.vel.x = randFloat() * 5000.0f;
		        o.vel.y = randFloat() * 5000.0f;
	    	}

            auto rect = spatial::Rect::fromPointRadius((int)o.pos.x, (int)o.pos.y, (int)o.radius);
	        spatial::SpatialObject qto { o.loyalty, o.type, /*collisionMask:*/ObjectType_All, /*objectId:*/-1, rect };
	        o.spatialId = spatial->insert(qto);
	    }
	    spatial->rebuild();
	}
};

static SpatialWithObjects createSpatialWithObjects(spatial::SpatialType type, SimParams p)
{
    SpatialWithObjects swo { createObjects(p) };
	swo.createSpatial(type, p);
    return swo;
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
    printf("%s(%zu) x%d total: %.2fms  avg: %.3fus\n",
        what, objects.size(), iterations, e, (e / total_operations)*1000);
}

template<class VoidFunc> static void measureIterations(const char* what, int iterations,
                                                       int objectsPerFunc, VoidFunc&& func)
{
    rpp::Timer t;
    for (int x = 0; x < iterations; ++x) { func(); }
    double e = t.elapsed_ms();
    printf("%s(%d) x%d total: %.2fms  avg: %.3fus\n",
        what, objectsPerFunc, iterations, e, ((e*1000)/iterations)/objectsPerFunc);
}

