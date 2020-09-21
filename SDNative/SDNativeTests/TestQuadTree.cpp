#include <ctime>
#include <spatial/qtree/Qtree.h>
#include <rpp/timer.h>
#include <rpp/tests.h>
#include <rpp/vec.h>
#include "ImGuiQtreeVis.h"

enum ObjectType : uint8_t
{
    // Can be used as a search filter to match all object types
    ObjectType_Any        = 0,
    ObjectType_Ship       = 1,
    ObjectType_ShipModule = 2,
    ObjectType_Proj       = 4, // this is a projectile, NOT a beam
    ObjectType_Beam       = 8, // this is a BEAM, not a projectile
    ObjectType_Asteroid   = 16,
    ObjectType_Moon       = 32,
};

struct MyGameObject
{
    int spatialId = -1;
    rpp::Vector2 pos { 0.0f, 0.0f };
    rpp::Vector2 vel { 0.0f, 0.0f };
    float radius = 0.0f;
    float mass = 1.0f;
    uint8_t loyalty = 0;
    ObjectType type = ObjectType_Any;
    bool collidedThisFrame = false;
};

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    static void insertAll(spatial::Spatial& tree, std::vector<MyGameObject>& objects)
    {
        tree.clear();
        for (MyGameObject& o : objects)
        {
            o.vel.x = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 5000.0f;
            o.vel.y = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 5000.0f;

            spatial::SpatialObject qto { o.loyalty, o.type, 0, (int)o.pos.x, (int)o.pos.y, (int)o.radius, (int)o.radius };
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

    static constexpr int UNIVERSE_SIZE = 5'000'000;
    static constexpr int SMALLEST_SIZE = 1024;
    static constexpr float OBJECT_RADIUS = 500;
    static constexpr int NUM_OBJECTS = 10'000;
    static constexpr float DEFAULT_SENSOR_RANGE = 10'000;

    static constexpr int SOLAR_SYSTEMS = 32;
    static constexpr int SOLAR_RADIUS = 100'000;

    struct Simulation final : spatial::vis::SimContext
    {
        std::vector<MyGameObject> objects;

        explicit Simulation(spatial::Qtree& tree) : SimContext{tree}
        {
            totalObjects = NUM_OBJECTS;
            recreateAllObjects();
        }

        void update(float timeStep) override
        {
            if (objects.size() != totalObjects)
                recreateAllObjects();

            if (isPaused)
                return;

            updateObjectPositions(timeStep);

            rpp::Timer t1;
            tree.rebuild();
            rebuildMs = t1.elapsed_ms();

            rpp::Timer t2;
            collidedObjects.clear();
            tree.collideAll(timeStep, [&](int objectA, int objectB) -> spatial::CollisionResult
            {
                collide(objectA, objectB);
                return spatial::CollisionResult::NoSideEffects;
            });
            numCollisions = (int)collidedObjects.size();
            collideMs = t2.elapsed_ms();
        }

        void updateObjectPositions(float timeStep)
        {
            float universeLo = tree.worldSize() * -0.5f;
            float universeHi = tree.worldSize() * +0.5f;
            for (MyGameObject& o : objects)
            {
                if (o.pos.x < universeLo || o.pos.x > universeHi)
                    o.vel.x = -o.vel.x;

                if (o.pos.y < universeLo || o.pos.y > universeHi)
                    o.vel.y = -o.vel.y;

                o.pos += o.vel * timeStep;
                tree.update(o.spatialId, (int)o.pos.x, (int)o.pos.y);
            }
        }

        void collide(int objectA, int objectB)
        {
            MyGameObject& a = objects[objectA];
            MyGameObject& b = objects[objectB];

            // impulse calculation
            // https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
            rpp::Vector2 collisionNormal = (b.pos - a.pos).normalized();
            rpp::Vector2 relativeVelocity = b.vel - a.vel;
            float velAlongNormal = relativeVelocity.dot(collisionNormal);
            if (velAlongNormal < 0)
            {
                float restitution = 1.0f; // perfect rigidity, all energy conserved

                // calculate impulse scalar
                float invMassA = 1.0f / a.mass;
                float invMassB = 1.0f / b.mass;
                float j = -(1 + restitution) * velAlongNormal;
                j /= invMassA + invMassB;

                // apply impulse
                rpp::Vector2 impulse = j * collisionNormal;
                a.vel -= invMassA * impulse;
                b.vel += invMassB * impulse;
            }

            collidedObjects.push_back(objectA);
            collidedObjects.push_back(objectB);
        }

        void recreateAllObjects()
        {
            tree.clear();
            objects = createObjects(totalObjects, OBJECT_RADIUS, UNIVERSE_SIZE, SOLAR_SYSTEMS, SOLAR_RADIUS);
            insertAll(tree, objects);
        }
    };

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
