#pragma once
#include "SpatialSimUtils.h"
#include "ImGuiSpatialVis.h"

static constexpr int UNIVERSE_SIZE = 5'000'000;
static constexpr int SMALLEST_SIZE = 1024;
static constexpr float OBJECT_RADIUS = 500;
static constexpr int NUM_OBJECTS = 10'000;
static constexpr float DEFAULT_SENSOR_RANGE = 10'000;

static constexpr int SOLAR_SYSTEMS = 32;
static constexpr int SOLAR_RADIUS = 100'000;

static constexpr int GRID_CELL_SIZE = 20'000;
static constexpr bool SHOW_SIMULATION = true;

struct Simulation final : spatial::vis::SimContext
{
    std::vector<MyGameObject> objects;

    explicit Simulation(spatial::Spatial& tree) : SimContext{tree}
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