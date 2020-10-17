#include <rpp/tests.h>
#include <spatial/Primitives.h>
using spatial::Rect;
using spatial::CircleF;

TestImpl(TestPrimitiveMath)
{
    TestInit(TestPrimitiveMath)
    {
    }

    TestCase(RectCircleOverlapCornersTop)
    {
        Rect r { -20, -10, +20, +10 };

        // (C)  circle 
        //  +-------+
        //  |   +   | rectangle w:40 h:20
        //  +-------+
        AssertTrue (r.overlaps(CircleF{-20, -20, 10 }));
        AssertFalse(r.overlaps(CircleF{ -20, -20, 9 }));

        //         (C)  circle 
        //  +-------+
        //  |   +   | rectangle w:40 h:20
        //  +-------+
        AssertTrue (r.overlaps(CircleF{ +20, -20, 10 }));
        AssertFalse(r.overlaps(CircleF{ +20, -20, 9 }));
    }

    TestCase(RectCircleOverlapCornersBottom)
    {
        Rect r{ -20, -10, +20, +10 };

        //  +-------+
        //  |   +   | rectangle w:40 h:20
        //  +-------+
        // (C)  circle 
        AssertTrue (r.overlaps(CircleF{ -20, +20, 10 }));
        AssertFalse(r.overlaps(CircleF{ -20, +20, 9 }));

        //  +-------+
        //  |   +   | rectangle w:40 h:20
        //  +-------+
        //         (C)  circle 
        AssertTrue (r.overlaps(CircleF{ +20, +20, 10 }));
        AssertFalse(r.overlaps(CircleF{ +20, +20, 9 }));
    }

    TestCase(RectCircleOverlapCornersLeft)
    {
        Rect r{ -20, -10, +20, +10 };

        // (C) +-------+
        //     |   +   | rectangle w:40 h:20
        //     +-------+
        AssertTrue (r.overlaps(CircleF{ -30, -10, 10 }));
        AssertFalse(r.overlaps(CircleF{ -30, -10, 9 }));

        //     +-------+
        //     |   +   | rectangle w:40 h:20
        // (C) +-------+
        AssertTrue (r.overlaps(CircleF{ -30, +10, 10 }));
        AssertFalse(r.overlaps(CircleF{ -30, +10, 9 }));
    }

    TestCase(RectCircleOverlapCornersRight)
    {
        Rect r{ -20, -10, +20, +10 };

        // +-------+ (C) 
        // |   +   | rectangle w:40 h:20
        // +-------+
        AssertTrue (r.overlaps(CircleF{ +30, -10, 10 }));
        AssertFalse(r.overlaps(CircleF{ +30, -10, 9 }));

        // +-------+
        // |   +   | rectangle w:40 h:20
        // +-------+ (C) 
        AssertTrue (r.overlaps(CircleF{ +30, +10, 10 }));
        AssertFalse(r.overlaps(CircleF{ +30, +10, 9 }));
    }

    TestCase(RectCircleOverlapMiddleTopBottom)
    {
        Rect r{ -20, -10, +20, +10 };

        //    (C)
        // +-------+
        // |   +   | rectangle w:40 h:20
        // +-------+
        AssertTrue (r.overlaps(CircleF{ 0, -20, 10 }));
        AssertFalse(r.overlaps(CircleF{ 0, -20, 9 }));

        // +-------+
        // |   +   | rectangle w:40 h:20
        // +-------+
        //    (C)
        AssertTrue (r.overlaps(CircleF{ 0, +20, 10 }));
        AssertFalse(r.overlaps(CircleF{ 0, +20, 9 }));
    }

    TestCase(RectCircleOverlapMiddleLeftRight)
    {
        Rect r{ -20, -10, +20, +10 };

        //     +-------+
        // (C) |   +   | rectangle w:40 h:20
        //     +-------+
        AssertTrue (r.overlaps(CircleF{ -30, 0, 10 }));
        AssertFalse(r.overlaps(CircleF{ -30, 0, 9 }));

        // +-------+
        // |   +   | (C)
        // +-------+
        AssertTrue (r.overlaps(CircleF{ +30, 0, 10 }));
        AssertFalse(r.overlaps(CircleF{ +30, 0, 9 }));
    }


    TestCase(RectCircleOverlapDiagonals)
    {
        Rect r{ -20, -10, +20, +10 };

        float diagonal = 10 * sqrtf(2);
        // (C) 
        //     +-------+
        //     |   +   | rectangle w:40 h:20
        //     +-------+
        AssertTrue (r.overlaps(CircleF{ -30, -20, diagonal }));
        AssertFalse(r.overlaps(CircleF{ -30, -20, diagonal - 0.1f }));
        
        //           (C) 
        // +-------+ 
        // |   +   | rectangle w:40 h:20
        // +-------+
        AssertTrue (r.overlaps(CircleF{ +30, -20, diagonal }));
        AssertFalse(r.overlaps(CircleF{ +30, -20, diagonal - 0.1f }));

        //     +-------+
        //     |   +   | rectangle w:40 h:20
        //     +-------+
        // (C) 
        AssertTrue (r.overlaps(CircleF{ -30, +20, diagonal }));
        AssertFalse(r.overlaps(CircleF{ -30, +20, diagonal - 0.1f }));

        // +-------+ 
        // |   +   | rectangle w:40 h:20
        // +-------+
        //           (C) 
        AssertTrue (r.overlaps(CircleF{ +30, -20, diagonal }));
        AssertFalse(r.overlaps(CircleF{ +30, -20, diagonal - 0.1f }));
    }
};
