using System;
namespace Ship_Game.Utils;

/// <summary>
/// An implementation of RandomBase
/// NOTE: This is not thread-safe, @see ThreadSafeRandom
/// </summary>
public class SeededRandom : RandomBase
{
    protected override Random Rand { get; }

    public SeededRandom() : this(0)
    {
    }

    public SeededRandom(int seed) : base(seed)
    {
        Rand = new(Seed);
    }
}
