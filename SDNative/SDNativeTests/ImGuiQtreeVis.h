#pragma once
#include <quadtree/Qtree.h>
#include <functional>

namespace spatial::vis
{
    struct SimContext
    {
        Qtree& tree;

        float zoom = 0.0001f;
        double rebuildMs = 0.0; // time spent in Qtree::rebuild()
        double collideMs = 0.0; // time spent in Qtree::collideAll()
        double findNearbyMs = 0.0; // time spent in Qtree::findNearby()
        int numCollisions = 0;
        std::vector<int> collidedObjects;
        int totalObjects = 0;

        bool isPaused = true;

        explicit SimContext(Qtree& tree) : tree{tree}
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
