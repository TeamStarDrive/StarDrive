using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the selected derelict, laid out like the ship info panel: icons for its original owner and turns left, and a bar for its production remaining
public sealed class DerelictInfoUIElement : UIElement
{
    readonly UniverseScreen Universe;
    readonly Rectangle OwnerRect;
    readonly Rectangle TurnsRect;
    readonly Rectangle HullRect;
    readonly Rectangle ProductionRect;
    readonly ProgressBar ProductionBar;

    public DerelictInfoUIElement(Rectangle r, UniverseScreen universe)
    {
        Universe = universe;
        ScreenManager = universe.ScreenManager;
        ElementRect = r;
        OwnerRect = new(r.X + 13, r.Y + 112, 22, 22);
        TurnsRect = new(r.X + 13, r.Y + 137, 22, 22);
        HullRect = new(r.X + 62, r.Y + 110, 115, 115);
        ProductionRect = new(r.X + 187, r.Y + 110, 20, 20);
        ProductionBar = new(ProductionRect.X + ProductionRect.Width + 15, ProductionRect.Y, 150, 18) { color = "yellow" };
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        Derelict d = Universe.SelectedDerelict;
        int turnsLeft = d?.TurnsLeft ?? 0; // read before the check, as it only runs down
        if (d is not { SelectableByPlayer: true })
            return;

        IShipDesign design = d.Design;
        Empire owner = d.Owner;
        batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), ElementRect, Color.White);
        batch.DrawString(Fonts.Arial14Bold, design.Name, new Vector2(ElementRect.X + 30, ElementRect.Y + 63), tColor);
        string kind = $"{Localizer.Token(GameText.Derelict)} - {Localizer.GetRole(design.Role, owner)}";
        batch.DrawString(Fonts.Visitor10, kind, new Vector2(ElementRect.X + 30, ElementRect.Y + 79), Color.Orange);

        ShipHull hull = design.BaseHull;
        string icon = hull.SelectIcon.NotEmpty() ? "SelectionBox Ships/" + hull.SelectIcon : hull.IconPath;
        batch.Draw(ResourceManager.Texture(icon), HullRect, Color.Gray);

        batch.Draw(ResourceManager.Flag(owner), OwnerRect, owner.EmpireColor);
        batch.Draw(ResourceManager.Texture("NewUI/icon_turns_left"), TurnsRect, Color.White);
        DrawValue(batch, TurnsRect, turnsLeft.ToString());

        batch.Draw(ResourceManager.Texture("NewUI/icon_production"), ProductionRect, Color.White);
        ProductionBar.Max = d.Production;
        ProductionBar.Progress = d.ProductionLeft;
        ProductionBar.Draw(batch);
    }

    // beside its icon, where the ship info panel writes a ship's defense and troops
    static void DrawValue(SpriteBatch batch, in Rectangle iconRect, string text)
    {
        var font = Fonts.Arial12Bold;
        var pos = new Vector2(iconRect.X + iconRect.Width + 2, iconRect.Y + 11 - font.LineSpacing / 2);
        batch.DrawString(font, text, pos, Color.White);
    }

    // tooltips over the icons and the bar; a click on the panel stays on it
    public override bool HandleInput(InputState input)
    {
        Derelict d = Universe.SelectedDerelict;
        if (d is not { SelectableByPlayer: true })
            return false;

        Vector2 cursor = input.CursorPosition;
        if (OwnerRect.HitTest(cursor))
            ToolTip.CreateTooltip(string.Format(Localizer.Token(GameText.DerelictOriginalOwner), d.Owner.Name));
        else if (TurnsRect.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictTurnsLeftTip);
        else if (ProductionRect.HitTest(cursor) || ProductionBar.pBar.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictProductionTip);
        return ElementRect.HitTest(cursor);
    }
}
