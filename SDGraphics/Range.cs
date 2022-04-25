using System;
using System.Diagnostics.Contracts;

namespace SDGraphics;

public struct Range : IEquatable<Range>
{
    public float Min;
    public float Max;

    public Range(float minMax)
    {
        Min = Max = minMax;
    }

    public Range(float min, float max)
    {
        Min = min; Max = max;
    }

    public bool HasValues => Min != 0f && Max != 0f;

    [Pure] public bool Equals(Range other)
    {
        return Min == other.Min && Max == other.Max;
    }

    [Pure] public bool AlmostEqual(float minMax)
    {
        return Min.AlmostEqual(minMax) && Max.AlmostEqual(minMax);
    }

    //[Pure] public float Generate()
    //{
    //    // ReSharper disable once CompareOfFloatsByEqualityOperator
    //    return Min == Max ? Min : RandomMath.Float(Min, Max);
    //}

    public override string ToString() => $"Range [{Min}, {Max}]";
}