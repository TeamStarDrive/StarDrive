using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
#pragma warning disable CA2231

namespace Ship_Game.Ships;

public readonly struct TacticalIcon : IEquatable<TacticalIcon>
{
    public readonly SubTexture Primary;
    public readonly SubTexture Secondary;
    public TacticalIcon(SubTexture primary, SubTexture secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }

    public bool Equals(TacticalIcon other)
    {
        return Equals(Primary, other.Primary) && Equals(Secondary, other.Secondary);
    }

    public override bool Equals(object obj)
    {
        return obj is TacticalIcon other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Primary.Name.GetHashCode()
            + (Secondary?.Name.GetHashCode() ?? 0);
    }

    public void Draw(SpriteBatch batch, in RectF rect, Color iconColor)
    {
        batch.Draw(Primary, rect, iconColor);
        if (Secondary != null)
            batch.Draw(Secondary, rect, iconColor);
    }
}
