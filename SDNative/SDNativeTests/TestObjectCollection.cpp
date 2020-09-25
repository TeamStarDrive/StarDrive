#include <spatial/ObjectCollection.h>
#include <rpp/tests.h>

TestImpl(TestObjectCollection)
{
    TestInit(TestObjectCollection) {}

    TestCase(insert_and_submit)
    {
        spatial::ObjectCollection objects;
        int id1 = objects.insert(spatial::SpatialObject{});
        int id2 = objects.insert(spatial::SpatialObject{});
        AssertThat(id1, 0);
        AssertThat(id2, 1);
        AssertEqual(objects.numActive(), 0);
        AssertEqual(objects.numPending(), 2);

        objects.submitPending();
        AssertEqual(objects.numActive(), 2);
        AssertEqual(objects.numPending(), 0);
    }

    TestCase(remove_pending)
    {
        spatial::ObjectCollection objects;
        int id1 = objects.insert(spatial::SpatialObject{});
        int id2 = objects.insert(spatial::SpatialObject{});
        AssertEqual(objects.numPending(), 2);

        objects.remove(id1);
        AssertEqual(objects.numPending(), 1);
        AssertEqual(objects.numFreeIds(), 1);
    }

    TestCase(remove_active)
    {
        spatial::ObjectCollection objects;
        int id1 = objects.insert(spatial::SpatialObject{});
        int id2 = objects.insert(spatial::SpatialObject{});
        objects.submitPending();
        AssertEqual(objects.numActive(), 2);

        objects.remove(id1);
        AssertEqual(objects.numActive(), 1);
        AssertEqual(objects.numFreeIds(), 1);
    }

    TestCase(reuse_pending)
    {
        spatial::ObjectCollection objects;
        int id1 = objects.insert(spatial::SpatialObject{});
        int id2 = objects.insert(spatial::SpatialObject{});
        objects.remove(id1);
        AssertEqual(objects.numPending(), 1);
        AssertEqual(objects.numFreeIds(), 1);
        
        int id3 = objects.insert(spatial::SpatialObject{});
        AssertMsg(id3 == 0, "Spatial index should be reused");
    }

    TestCase(reuse_active)
    {
        spatial::ObjectCollection objects;
        int id1 = objects.insert(spatial::SpatialObject{});
        int id2 = objects.insert(spatial::SpatialObject{});
        objects.submitPending();
        objects.remove(id1);
        AssertEqual(objects.numActive(), 1);
        AssertEqual(objects.numFreeIds(), 1);
        
        int id3 = objects.insert(spatial::SpatialObject{});
        AssertMsg(id3 == 0, "Spatial index should be reused");
    }
};