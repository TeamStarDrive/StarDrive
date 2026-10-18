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

// the selected derelict, laid out like the ship info panel: icons for its original owner and turns left, a bar for its production remaining, the salvage station order for its system under the bar, the empire salvaging it at the bottom, and its Salvage order in the drawer that slides out to the right
public sealed class DerelictInfoUIElement : UIElement
{
    readonly UniverseScreen Universe;
    readonly Rectangle OwnerRect;
    readonly Rectangle TurnsRect;
    readonly Rectangle HullRect;
    readonly Rectangle ProductionRect;
    readonly ProgressBar ProductionBar;
    readonly Rectangle ClaimRect;
    readonly Rectangle ClaimLine;
    readonly SlidingElement Drawer;
    readonly Rectangle SalvageRect;
    readonly Rectangle StationRect;
    readonly Rectangle StatusRect;
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
        ProductionBar = new(ProductionRect.X + ProductionRect.Width + 15, ProductionRect.Y, 150, 18) { color = "red" }; // as on its salvage platform's panel
        ClaimRect = new(ProductionRect.X, r.Y + 208, 20, 20); // just above the panel's lower border
        ClaimLine = new(ClaimRect.X, ClaimRect.Y, r.X + r.Width - 20 - ClaimRect.X, ClaimRect.Height);
        Drawer = new SlidingElement(new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130));
        SalvageRect = new(r.X + r.Width + 2, Drawer.Housing.Y + 15, 48, 48); // the drawer's first slot, as the ship panel's first order
        int centre = ProductionBar.pBar.X + ProductionBar.pBar.Width / 2;
        StationRect = new(centre - 84, r.Y + 145, 168, 24); // under the bar, at the derelicts window's button size
        StatusRect = new(ClaimLine.X, StationRect.Y, ClaimLine.Width, StationRect.Height); // the claimant line's width
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
            var salvage = new Rectangle(SalvageRect.X - slide, SalvageRect.Y, SalvageRect.Width, SalvageRect.Height);
            bool hovered = State == ElementState.Open && salvage.HitTest(Universe.Input.CursorPosition);
            DerelictSalvageButton.Draw(batch, salvage, SalvageDerelict.WhyNoOrder(Universe.Player, d), hovered);
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
        Empire claimant = d.Claimant;
        // grey while its timer stands, pulsing while it runs
        Color turns = claimant != null ? Color.Gray : Universe.ApplyCurrentAlphaToColor(Color.White);
        batch.Draw(ResourceManager.Texture("NewUI/icon_turns_left"), TurnsRect, turns);
        DrawValue(batch, TurnsRect, turnsLeft.ToString(), claimant != null ? Color.Gray : Color.White);

        batch.Draw(ResourceManager.Texture("NewUI/icon_production"), ProductionRect, Color.White);
        ProductionBar.Max = d.Production;
        ProductionBar.Progress = d.ProductionLeft;
        ProductionBar.Draw(batch);
        DrawStationOrder(batch, d);

        if (claimant != null)
            DrawClaimant(batch, claimant);
    }

    // its flag and name, pulsing in its colour
    void DrawClaimant(SpriteBatch batch, Empire claimant)
    {
        Color color = Universe.ApplyCurrentAlphaToColor(claimant.EmpireColor);
        batch.Draw(ResourceManager.Flag(claimant), ClaimRect, color);
        var pos = new Vector2(ClaimRect.X + ClaimRect.Width + 15, ClaimRect.Y + 3);
        FitName(claimant.Name, ClaimLine.X + ClaimLine.Width - pos.X);
        batch.DrawString(NameFont, NameText, pos, color);
    }

    // the claimant's name as last fitted, kept while the name and the fonts stay
    string NameFittedFrom;
    Graphics.Font NameFittedWith;
    Graphics.Font NameFont;
    string NameText;

    // a long name takes a smaller font, and is cut short if it still does not fit
    void FitName(string name, float space)
    {
        if (ReferenceEquals(name, NameFittedFrom) && NameFittedWith == Fonts.Arial12Bold)
            return;
        NameFittedFrom = name;
        NameFittedWith = NameFont = Fonts.Arial12Bold;
        NameText = name;
        if (NameFont.TextWidth(name) <= space)
            return;
        NameFont = Fonts.Arial10;
        for (int length = name.Length - 1; length > 0 && NameFont.TextWidth(NameText) > space; --length)
            NameText = name.Substring(0, length).TrimEnd() + "...";
    }

    // the Build Salvage Station button while a station can be ordered in the derelict's system, greyed with no design to build; once ordered, its status in the button's place
    void DrawStationOrder(SpriteBatch batch, Derelict d)
    {
        switch (SystemDerelictsScreen.StationOrderIn(Universe.Player, d.System))
        {
            case SystemDerelictsScreen.StationOrder.Deploy:   DrawStationButton(batch, enabled: true);                                           break;
            case SystemDerelictsScreen.StationOrder.NoDesign: DrawStationButton(batch, enabled: false);                                          break;
            case SystemDerelictsScreen.StationOrder.Abort:    DrawStatus(batch, GameText.SalvageStationOrdered, Color.Wheat);                    break;
            case SystemDerelictsScreen.StationOrder.Deployed: DrawStatus(batch, GameText.SalvageStationDeployed, Universe.Player.EmpireColor); break;
        }
    }

    static readonly Color ButtonText = new(255, 240, 189); // as on the game's buttons

    // greyed on the shorter button's grey plate, drawn to this one's size
    void DrawStationButton(SpriteBatch batch, bool enabled)
    {
        bool hovered = enabled && StationRect.HitTest(Universe.Input.CursorPosition);
        string back = !enabled ? "EmpireTopBar/empiretopbar_btn_132px_menu"
                    : hovered ? "EmpireTopBar/empiretopbar_btn_168px_hover"
                    : "EmpireTopBar/empiretopbar_btn_168px";
        batch.Draw(ResourceManager.Texture(back), StationRect, Color.White);
        DrawCentred(batch, Fonts.TahomaBold9, Localizer.Token(GameText.BuildSalvageStation), enabled ? ButtonText : Color.Gray,
                    StationRect.CenterX(), StationRect.CenterY());
    }

    // the status as last fitted, kept while the text and the font stay: one line, or two where the language's text is wider than the space
    string StatusFittedFrom;
    Graphics.Font StatusFittedWith;
    string StatusLine1;
    string StatusLine2;

    void DrawStatus(SpriteBatch batch, GameText status, Color color)
    {
        string text = Localizer.Token(status);
        Graphics.Font font = Fonts.Arial12Bold;
        if (!ReferenceEquals(text, StatusFittedFrom) || StatusFittedWith != font)
            FitStatus(text, font);

        int x = StatusRect.CenterX(), y = StatusRect.CenterY();
        if (StatusLine2 == null)
        {
            DrawCentred(batch, font, StatusLine1, color, x, y);
            return;
        }
        DrawCentred(batch, font, StatusLine1, color, x, y - font.LineSpacing / 2);
        DrawCentred(batch, font, StatusLine2, color, x, y + font.LineSpacing / 2);
    }

    // split at the space nearest the middle
    void FitStatus(string text, Graphics.Font font)
    {
        StatusFittedFrom = text;
        StatusFittedWith = font;
        StatusLine1 = text;
        StatusLine2 = null;
        if (font.TextWidth(text) <= StatusRect.Width)
            return;

        int middle = text.Length / 2, split = -1;
        for (int i = 0; i < text.Length; ++i)
            if (text[i] == ' ' && (split < 0 || Math.Abs(i - middle) < Math.Abs(split - middle)))
                split = i;
        if (split > 0)
        {
            StatusLine1 = text.Substring(0, split);
            StatusLine2 = text.Substring(split + 1);
        }
    }

    static void DrawCentred(SpriteBatch batch, Graphics.Font font, string text, Color color, int centreX, int centreY)
    {
        var pos = new Vector2(centreX - font.TextWidth(text) / 2, centreY - font.LineSpacing / 2);
        batch.DrawString(font, text, pos, color);
    }

    void OrderStation(Derelict d, bool canOrder)
    {
        if (canOrder && SalvageOps.OrderNearDerelicts(Universe.Player, d.System, Universe.UState.Objects.GetShips(), SiteRandom))
        {
            GameAudio.EchoAffirmative();
            return;
        }

        GameAudio.NegativeClick();
        if (canOrder) // above the panel's frame, clear of the derelict's name and of the tooltip at the cursor, even wrapped to four lines
            ToolTip.CreateFloatingText(GameText.SalvageStationNoSite, "", new Vector2(ElementRect.X + 20, ElementRect.Y - 80), 5);
    }

    // beside its icon, where the ship info panel writes a ship's defense and troops
    static void DrawValue(SpriteBatch batch, in Rectangle iconRect, string text, Color color)
    {
        var font = Fonts.Arial12Bold;
        var pos = new Vector2(iconRect.X + iconRect.Width + 2, iconRect.Y + 11 - font.LineSpacing / 2);
        batch.DrawString(font, text, pos, color);
    }

    // the drawer's tab opens and closes it; tooltips over the icons, the bar, the station order and the drawer's button; a click on the panel or the open drawer stays on it
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
        if (StatusRect.HitTest(cursor) && HandleStationOrder(input, d))
            return true;

        bool drawerOpen = State == ElementState.Open;
        if (drawerOpen && SalvageRect.HitTest(cursor))
        {
            SalvageDerelict.Block block = SalvageDerelict.WhyNoOrder(Universe.Player, d);
            LocalizedText tip = DerelictSalvageButton.Tooltip(block, d);
            if (tip.IsValid)
                ToolTip.CreateTooltip(tip);
            if (input.LeftMouseClick)
                DerelictSalvageButton.Click(Universe.Player, d, block);
            return true;
        }

        Empire claimant = d.Claimant;
        if (OwnerRect.HitTest(cursor))
            ToolTip.CreateTooltip(string.Format(Localizer.Token(GameText.DerelictOriginalOwner), d.Owner.Name));
        else if (TurnsRect.HitTest(cursor))
            ToolTip.CreateTooltip(claimant != null ? GameText.DerelictTurnsStandTip : GameText.DerelictTurnsLeftTip);
        else if (ProductionRect.HitTest(cursor) || ProductionBar.pBar.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictProductionTip);
        else if (claimant != null && ClaimLine.HitTest(cursor))
            ToolTip.CreateTooltip(string.Format(Localizer.Token(GameText.DerelictSalvagedBy), claimant.Name));
        return ElementRect.HitTest(cursor) || drawerOpen && Drawer.ButtonHousing.HitTest(cursor);
    }

    // the button's tooltip and click over the button, or the status's tooltip over its line; false where the cursor is on neither
    bool HandleStationOrder(InputState input, Derelict d)
    {
        SystemDerelictsScreen.StationOrder order = SystemDerelictsScreen.StationOrderIn(Universe.Player, d.System);
        switch (order)
        {
            case SystemDerelictsScreen.StationOrder.Deploy:
            case SystemDerelictsScreen.StationOrder.NoDesign:
                if (!StationRect.HitTest(input.CursorPosition))
                    return false;
                bool canOrder = order == SystemDerelictsScreen.StationOrder.Deploy;
                ToolTip.CreateTooltip(canOrder ? GameText.SalvageStationOrderTip : GameText.SalvageStationNoDesignTip);
                if (input.LeftMouseClick)
                    OrderStation(d, canOrder);
                return true;
            case SystemDerelictsScreen.StationOrder.Abort:
            case SystemDerelictsScreen.StationOrder.Deployed:
                ToolTip.CreateTooltip(GameText.SalvageStationOrderedTip);
                return true;
            default:
                return false;
        }
    }
}
