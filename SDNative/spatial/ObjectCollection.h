#pragma once
#include "SpatialObject.h"
#include <vector>

namespace spatial
{
    /**
     * Helper class to manage Spatial collection's Objects
     * -) Objects are inserted as Pending and will be collected during Spatial rebuild
     * -) Only objects that moved will be updated
     * -) Each object receives a STABLE ObjectId which will not change while the object is active
     */
    class SPATIAL_API ObjectCollection
    {
        std::vector<SpatialObject> PendingInsert;

        // Stable vector of the objects, this capacity does not change often
        // Deleted objects are marked in a free-list so that ID-s can be reused
        // ObjectId will map directly as an Index to this array
        std::vector<SpatialObject> Objects;
        uint32_t NumObjects = 0;
        std::vector<int> FreeIds;

    public:

        ObjectCollection();
        ~ObjectCollection();

        /**
         * Resets the entire collection: clears pending, clears objects, clears free id's
         */
        void clear();
        
        /**
         * Total number of Objects
         * Some of these may be inactive
         * Pending not included
         */
        int count() const { return Objects.size(); }

        /** Total bytes used */
        uint32_t totalMemory() const;

        /**
         * Gets an object via its objectId
         */
        const SpatialObject& get(int objectId) const;
        SpatialObject* begin() { return Objects.data(); }
        SpatialObject* end()   { return Objects.data() + Objects.size(); }

        /**
         * Inserts a new object as pending
         * @return Assigned ObjectId
         */
        int insert(const SpatialObject& object);

        /**
         * Mark an object for removal.
         * The ObjectId will be reused for another object
         */
        void remove(int objectId);

        /**
         * Sets the new X and Y of an object.
         * If X and Y changed, the object is marked for update
         */
        void update(int objectId, int x, int y);

        /**
         * Submits pending objects into the main Objects list
         * and marks them for update
         */
        void submitPending();
    };
}
