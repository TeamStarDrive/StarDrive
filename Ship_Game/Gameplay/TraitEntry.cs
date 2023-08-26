using SDUtils;
using Ship_Game.Data;
using System;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Gameplay;

public sealed class TraitEntry
{
    public bool Selected;
    public RacialTraitOption Trait;
    public Rectangle rect;
    public bool Excluded => ExcludedBy.Count > 0;
    public Array<string> ExcludedBy = new(); 

    public override string ToString() => $"TraitEntry {Trait.LocalizedName.Text} Selected={Selected} Excluded={Excluded}";
}
