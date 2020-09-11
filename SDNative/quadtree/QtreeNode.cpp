#include "QtreeNode.h"
#include "QtreeConstants.h"
#include "QtreeAllocator.h"

namespace tree
{
    void QtreeNode::add(QtreeAllocator& allocator, const SpatialObj& obj)
    {
        if (Capacity == Count)
        {
            Capacity = Capacity == 0 ? QuadCellThreshold : Capacity * 2;
            Items = allocator.allocArray(Items, Count, Capacity);
        }
        Items[Count++] = obj;
        ++TotalTreeDepthCount;
    }
}
