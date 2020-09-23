#include <spatial/ObjectCollection.h>
#include <rpp/tests.h>

TestImpl(TestObjectCollection)
{
    TestInit(TestObjectCollection) {}

    TestCase(insert)
    {
        spatial::ObjectCollection objects;

        int id1 = objects.insert(spatial::SpatialObject{});
        AssertThat(id1, 1);
        int id2 = objects.insert(spatial::SpatialObject{});
        AssertThat(id2, 2);
    }

    TestCase(reuse)
    {
        
    }
};