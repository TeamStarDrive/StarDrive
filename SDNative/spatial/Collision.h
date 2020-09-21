#pragma once

namespace spatial
{
    /**
     * Describes the result of the collision callback.
     * This allows the collider to ignore objects which
     * no longer participate in further collisions
     */
    enum class CollisionResult : int
    {
        NoSideEffects, // no visible side effect from collision (both objects still alive)
        ObjectAKilled, // objects collided and objectA was killed (no further collision possible)
        ObjectBKilled, // objects collided and objectB was killed (no further collision possible)
        BothKilled,    // both objects were killed during collision (no further collision possible)
    };

    /**
     * Function for handling collisions between two objects
     * @param user User defined pointer for passing context
     * @param objectA ID of the first object colliding
     * @param objectA ID of the second object colliding
     * @return CollisionResult Result of the collision
     */
    using CollisionFunc = CollisionResult (*)(void* user, int objectA, int objectB);

}
