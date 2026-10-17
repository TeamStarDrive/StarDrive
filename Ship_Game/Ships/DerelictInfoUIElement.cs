using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the selected derelict, laid out like the ship info panel: icons for its original owner and turns left, a bar for its production remaining, and its orders in the drawer that slides out to the right
public sealed class DerelictInfoUIElement : UIElement
{
    readonly UniverseScreen Universe;
    readonly Rectangle OwnerRect;
    readonly Rectangle TurnsRect;
    readonly Rectangle HullRect;
    readonly Rectangle ProductionRect;
    readonly ProgressBar ProductionBar;
    readonly SlidingElement Drawer;
    readonly Rectangle BuildStationRect;
    readonly SeededRandom SiteRandom = new();

    public DerelictInfoUIElement(Rectangle r, UniverseScreen universe)
    {
        Universe = universe;
        ScreenManager = universe.ScreenManager;
        ElementRect = r;
        TransitionOnTime = TimeSpan.FromSeconds(0.25);
        TransitionOffTime = TimeSpan.FromSeconds(0.25);
        OwnerRect = new(r.X + 13, r.Y + 112, 22, 22);
        TurnsRect = new(r.X + 13, r.Y + 137, 22, 22);
        HullRect = new(r.X + 62, r.Y + 110, 115, 115);
        ProductionRect = new(r.X + 187, r.Y + 110, 20, 20);
        ProductionBar = new(ProductionRect.X + ProductionRect.Width + 15, ProductionRect.Y, 150, 18) { color = "yellow" };
        Drawer = new SlidingElement(new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130));
        BuildStationRect = new(r.X + r.Width + 2, Drawer.Housing.Y + 15, 48, 48); // the drawer's first slot, as the ship panel's first order
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        Derelict d = Universe.SelectedDerelict;
        int turnsLeft = d?.TurnsLeft ?? 0; // read before the check, as it only runs down
        if (d is not { SelectableByPlayer: true })
            return;

        const int columns = 1;
        Drawer.Draw(ScreenManager, (int)(columns * 55 * (1f - TransitionPosition)) + (Drawer.Open ? 20 - columns : 0));
        if (State != ElementState.Closed) // hidden behind the panel when closed
        {
            int slide = (int)(0f.SmoothStep(1f, TransitionPosition) * 300f);
            DrawBuildStationButton(batch, d, new(BuildStationRect.X - slide, BuildStationRect.Y, BuildStationRect.Width, BuildStationRect.Height));
        }

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

    // greyed when the player cannot order a station in the derelict's system
    void DrawBuildStationButton(SpriteBatch batch, Derelict d, Rectangle rect)
    {
        bool enabled = SalvageOps.WhyNoOrderIn(Universe.Player, d.System) == SalvageOps.OrderBlock.None;
        bool hovered = enabled && State == ElementState.Open && rect.HitTest(Universe.Input.CursorPosition);
        string back = !enabled ? "SelectionBox/button_action_disabled"
                    : hovered ? "SelectionBox/button_action_hover"
                    : "SelectionBox/button_action";
        batch.Draw(ResourceManager.Texture(back), rect, Color.White);

        SubTexture icon = ResourceManager.Texture("UI/icon_dsbw");
        int w = (int)(icon.Width * 1.5f), h = (int)(icon.Height * 1.5f);
        var iconRect = new Rectangle(rect.X + (rect.Width - w) / 2, rect.Y + (rect.Height - h) / 2, w, h);
        batch.Draw(icon, iconRect, enabled ? Color.White : Color.Gray);
    }

    void OrderStation(Derelict d, SalvageOps.OrderBlock block)
    {
        if (block == SalvageOps.OrderBlock.None
            && SalvageOps.OrderNearDerelicts(Universe.Player, d.System, Universe.UState.Objects.GetShips(), SiteRandom))
        {
            GameAudio.EchoAffirmative();
            return;
        }

        GameAudio.NegativeClick();
        if (block == SalvageOps.OrderBlock.None) // above the button, clear of it and of its tooltip at the cursor
            ToolTip.CreateFloatingText(GameText.SalvageStationNoSite, "", new Vector2(BuildStationRect.X, BuildStationRect.Y - 70), 5);
    }

    // beside its icon, where the ship info panel writes a ship's defense and troops
    static void DrawValue(SpriteBatch batch, in Rectangle iconRect, string text)
    {
        var font = Fonts.Arial12Bold;
        var pos = new Vector2(iconRect.X + iconRect.Width + 2, iconRect.Y + 11 - font.LineSpacing / 2);
        batch.DrawString(font, text, pos, Color.White);
    }

    // the drawer's tab opens and closes it; tooltips over the icons, the bar and the drawer's buttons; a click on the panel or the open drawer stays on it
    public override bool HandleInput(InputState input)
    {
        Derelict d = Universe.SelectedDerelict;
        if (d is not { SelectableByPlayer: true })
            return false;

        if (Drawer.HandleInput(input))
        {
            State = Drawer.Open ? ElementState.TransitionOn : ElementState.TransitionOff;
            return true;
        }

        Vector2 cursor = input.CursorPosition;
        bool drawerOpen = State == ElementState.Open;
        if (drawerOpen && BuildStationRect.HitTest(cursor))
        {
            SalvageOps.OrderBlock block = SalvageOps.WhyNoOrderIn(Universe.Player, d.System);
            ToolTip.CreateTooltip(block switch
            {
                SalvageOps.OrderBlock.StationThere => GameText.SalvageStationOrderedTip,
                SalvageOps.OrderBlock.NoDesign => GameText.SalvageStationNoDesignTip,
                _ => GameText.SalvageStationOrderTip,
            });
            if (input.LeftMouseClick)
                OrderStation(d, block);
            return true;
        }

        if (OwnerRect.HitTest(cursor))
            ToolTip.CreateTooltip(string.Format(Localizer.Token(GameText.DerelictOriginalOwner), d.Owner.Name));
        else if (TurnsRect.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictTurnsLeftTip);
        else if (ProductionRect.HitTest(cursor) || ProductionBar.pBar.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictProductionTip);
        return ElementRect.HitTest(cursor) || drawerOpen && Drawer.ButtonHousing.HitTest(cursor);
    }
}
