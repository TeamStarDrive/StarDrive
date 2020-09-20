#pragma once
#include <quadtree/QuadTree.h>
#include <functional>

namespace tree::vis
{
    struct SimContext
    {
        QuadTree& tree;

        float zoom = 0.0001f;
        double rebuildMs = 0.0; // time spent in QuadTree::rebuild()
        double collideMs = 0.0; // time spent in QuadTree::collideAll()
        double findNearbyMs = 0.0; // time spent in QuadTree::findNearby()
        int numCollisions = 0;
        std::vector<int> collidedObjects;
        int totalObjects = 10000;

        bool isPaused = true;

        explicit SimContext(QuadTree& tree) : tree{tree}
        {
        }
        virtual ~SimContext() = default;

        /**
         * Called during every simulation update
         * @param timeStep Fixed physics timeStep
         */
        virtual void update(float timeStep) = 0;
    };

    void show(SimContext& context);
}
