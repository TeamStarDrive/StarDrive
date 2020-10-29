#pragma once
#include "SpatialObject.h"
#include <vector>
#include <mutex>

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
        std::mutex Sync;
        std::vector<SpatialObject> PendingInsert;

        // Stable vector of the objects, this capacity does not change often
        // Deleted objects are marked in a free-list so that ID-s can be reused
        // ObjectId will map directly as an Index to this array
        std::vector<SpatialObject> Objects;
        uint32_t MaxObjects = 0;
        uint32_t NumActive = 0;
        std::vector<int> FreeIds;

    public:

        ObjectCollection();
        ~ObjectCollection();

        /**
         * Resets the entire collection: clears pending, clears objects, clears free id's
         */
        void clear();
        
        /**
         * Maximum number of Objects.
         * Some of these can be inactive as they are removed.
         * Pending not included.
         */
        int maxObjects() const { return MaxObjects; }

        /**
         * Current number of active objects
         */
        int numActive() const { return NumActive; }

        /**
         * Number of objects that are pending insertion
         */
        int numPending() const { return PendingInsert.size(); }

        /**
         * Number of Free ID-s that will be reused during next inserts
         */
        int numFreeIds() const { return FreeIds.size(); }

        /** Total bytes used */
        uint32_t totalMemory() const;

        /**
         * Gets an object via its objectId
         */
        const SpatialObject& get(int objectId) const;
        const SpatialObject* data() const { return Objects.data(); }
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
        void update(int objectId, const Rect& rect);

        /**
         * Submits pending objects into the main Objects list
         * and marks them for update
         */
        void submitPending();
    };
}
