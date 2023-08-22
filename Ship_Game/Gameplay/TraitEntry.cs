using Ship_Game.Data;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Gameplay;

public sealed class TraitEntry
{
    public bool Selected;
    public RacialTraitOption Trait;
    public Rectangle rect;
    public bool Excluded;

    public override string ToString() => $"TraitEntry {Trait.LocalizedName.Text} Selected={Selected} Excluded={Excluded}";
}
