#include <spatial/Search.h>
#include <rpp/tests.h>
#include "spatial/CellLoyalty.h"

using spatial::getLoyaltyMask;

TestImpl(TestCellLoyalty)
{
    TestInit(TestCellLoyalty)
    {
    }

    TestCase(getLoyaltyMaskChecks)
    {
        AssertThat(getLoyaltyMask(0), spatial::MATCH_ALL);
        AssertThat(getLoyaltyMask(1), 0b0001);
        AssertThat(getLoyaltyMask(2), 0b0010);
        AssertThat(getLoyaltyMask(3), 0b0100);
        AssertThat(getLoyaltyMask(31), 0b01000000'00000000'00000000'00000000);
        AssertThat(getLoyaltyMask(32), 0b10000000'00000000'00000000'00000000);
        AssertThat(getLoyaltyMask(33), spatial::MATCH_ALL);
    }

    TestCase(getLoyaltyMaskFromSearchOptions)
    {
        auto opts = [](int only, int exclude)
        {
            spatial::SearchOptions opt;
            opt.OnlyLoyalty = only;
            opt.ExcludeLoyalty = exclude;
            return opt;
        };

        AssertThat(getLoyaltyMask(opts(/*only:*/0, /*exclude:*/0)), spatial::MATCH_ALL);
        AssertThat(getLoyaltyMask(opts(/*only:*/1, /*exclude:*/0)), 0b0001);
        AssertThat(getLoyaltyMask(opts(/*only:*/2, /*exclude:*/0)), 0b0010);
        AssertThat(getLoyaltyMask(opts(/*only:*/3, /*exclude:*/0)), 0b0100);
        AssertThat(getLoyaltyMask(opts(/*only:*/0, /*exclude:*/1)), 0b1110|0xfffffff0);
        AssertThat(getLoyaltyMask(opts(/*only:*/0, /*exclude:*/2)), 0b1101|0xfffffff0);
        AssertThat(getLoyaltyMask(opts(/*only:*/0, /*exclude:*/3)), 0b1011|0xfffffff0);
    }

    TestCase(InRangeLoyaltyReturnsProperMask)
    {
        spatial::CellLoyalty loyalty;
        AssertThat(loyalty.mask, 0);
        AssertThat(loyalty.count, 0);

        uint32_t expectedMask = 0;
        int expectedCount = 0;
        for (int i = 1; i <= 32; ++i)
        {
            expectedMask |= (1 << (i-1));
            expectedCount += 1;
            loyalty.addLoyalty(i);
            AssertThat(loyalty.mask, expectedMask);
            AssertThat(loyalty.count, expectedCount);
        }
    }

    // When loyalty is out of supported range,
    // the mask should be set to MATCH_ALL
    TestCase(OutOfRangeLoyaltyReturnsMatchAllMask)
    {
        spatial::CellLoyalty loyalty1;
        loyalty1.addLoyalty(0);
        AssertThat(loyalty1.mask, spatial::MATCH_ALL);
        AssertThat(loyalty1.count, 1);

        spatial::CellLoyalty loyalty33;
        loyalty33.addLoyalty(33);
        AssertThat(loyalty33.mask, spatial::MATCH_ALL);
        AssertThat(loyalty33.count, 1);
    }
};