using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the selected derelict, laid out like the ship info panel: icons for its original owner, turns left, techs and the player's chance to recover it, a bar for its production remaining, the salvage station order for its system under the bar, the techs the player sees under that, the empire salvaging it at the bottom, and its Salvage and Recover orders in the drawer that slides out to the right while the player has a salvage station built in its system
public sealed class DerelictInfoUIElement : UIElement
{
    // the tech icons' size, and their spacing, which leaves room for the widest points under them
    const int TechWidth = 30;
    const int TechHeight = 25;
    const int TechSpacing = 34;
    static readonly Color PointsColor = new(140, 200, 255);

    readonly UniverseScreen Universe;
    readonly Rectangle OwnerRect;
    readonly Rectangle TurnsRect;
    readonly Rectangle TechsRect;
    readonly Rectangle ChanceRect;
    readonly Rectangle HullRect;
    readonly Rectangle ProductionRect;
    readonly ProgressBar ProductionBar;
    readonly Rectangle ClaimRect;
    readonly Rectangle ClaimLine;
    readonly SlidingElement Drawer;
    readonly Rectangle SalvageRect;
    readonly Rectangle RecoverRect;
    readonly Rectangle StationRect;
    readonly Rectangle StatusRect;
    readonly Rectangle FirstTechRect;
    readonly Rectangle RecoveryRect;
    readonly ProgressBar RecoveryBar;
    // the techs the player can learn from it as last drawn, empty while the player does not see them or recovers it
    readonly Array<TechEntry> Learnable = new();
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
        TechsRect = new(r.X + 13, r.Y + 162, 22, 22);
        ChanceRect = new(r.X + 13, r.Y + 187, 22, 22);
        HullRect = new(r.X + 62, r.Y + 110, 115, 115);
        ProductionRect = new(r.X + 187, r.Y + 110, 20, 20);
        ProductionBar = new(ProductionRect.X + ProductionRect.Width + 15, ProductionRect.Y, 150, 18) { color = "red" }; // as on its salvage platform's panel
        ClaimRect = new(ProductionRect.X, r.Y + 208, 20, 20); // just above the panel's lower border
        ClaimLine = new(ClaimRect.X, ClaimRect.Y, r.X + r.Width - 20 - ClaimRect.X, ClaimRect.Height);
        Drawer = new SlidingElement(new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130));
        SalvageRect = new(r.X + r.Width + 2, Drawer.Housing.Y + 15, 48, 48); // the drawer's first slot, as the ship panel's first order
        RecoverRect = new(SalvageRect.X, SalvageRect.Y + 52, 48, 48); // under it, as the ship panel stacks two orders
        int center = ProductionBar.pBar.X + ProductionBar.pBar.Width / 2;
        StationRect = new(center - 84, r.Y + 135, 168, 24); // under the bar, at the derelicts window's button size
        StatusRect = new(ClaimLine.X, StationRect.Y, ClaimLine.Width, StationRect.Height); // the claimant line's width
        FirstTechRect = new(ProductionRect.X, StationRect.Bottom + 4, TechWidth, TechHeight); // the points under them clear the claimant line
        RecoveryRect = new(ProductionRect.X, FirstTechRect.Y + 3, ProductionRect.Width, ProductionRect.Height); // in the techs' place, as the production bar
        RecoveryBar = DerelictSalvageButton.RecoveryBar(new(ProductionBar.pBar.X, RecoveryRect.Y, ProductionBar.pBar.Width, 18));
    }

    Rectangle TechRect(int i) => new(FirstTechRect.X + i * TechSpacing, FirstTechRect.Y, TechWidth, TechHeight);

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        Derelict d = Universe.SelectedDerelict;
        int turnsLeft = d?.TurnsLeft ?? 0; // read before the check, as it only runs down
        if (d is not { SelectableByPlayer: true })
            return;

        Empire player = Universe.Player;
        SalvageDerelict.Block salvageBlock = SalvageDerelict.WhyNoOrder(player, d);
        SalvageDerelict order = DerelictSalvageButton.StandingOrder(player, d, salvageBlock);
        if (DerelictSalvageButton.Shown(player, d, salvageBlock))
        {
            const int columns = 1;
            Drawer.Draw(ScreenManager, (int)(columns * 55 * (1f - TransitionPosition)) + (Drawer.Open ? 20 - columns : 0));
            if (State != ElementState.Closed) // hidden behind the panel when closed
            {
                int slide = (int)(0f.SmoothStep(1f, TransitionPosition) * 300f);
                DrawButton(batch, DerelictSalvageButton.Kind.Salvage, salvageBlock, order, SalvageRect, slide);
                DrawButton(batch, DerelictSalvageButton.Kind.Recover, DerelictSalvageButton.BlockOf(DerelictSalvageButton.Kind.Recover, salvageBlock, d), order, RecoverRect, slide);
            }
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
        DrawTechs(batch, d, order);
        if (DerelictRecovery.CanRecover(d))
        {
            batch.Draw(ResourceManager.Texture("TacticalIcons/symbol_construction"), ChanceRect, Color.White);
            DrawValue(batch, ChanceRect, DerelictSalvageButton.Percent(DerelictRecovery.Chance(player, d)), Color.White);
        }

        if (claimant != null)
            DrawClaimant(batch, claimant);
    }

    void DrawButton(SpriteBatch batch, DerelictSalvageButton.Kind kind, SalvageDerelict.Block block, SalvageDerelict order, in Rectangle rect, int slide)
    {
        var button = new Rectangle(rect.X - slide, rect.Y, rect.Width, rect.Height);
        bool hovered = State == ElementState.Open && button.HitTest(Universe.Input.CursorPosition);
        DerelictSalvageButton.Draw(batch, button, Universe.Player, kind, block, DerelictSalvageButton.Lit(kind, block, order), hovered);
    }

    // the count under the turns; once the player sees its techs, the icons of those the player can learn, with the points the player's salvage of it has given each, or while the player's order recovers it, the recovery's progress in their place
    void DrawTechs(SpriteBatch batch, Derelict d, SalvageDerelict order)
    {
        Empire player = Universe.Player;
        bool seen = SalvageDerelict.SeesTechs(player, d);
        if (seen)
            DerelictTechIcons.Learnable(player, d, Learnable);
        else
            Learnable.Clear();
        DerelictTechIcons.DrawCount(batch, TechsRect, DerelictTechIcons.Count(seen, d, Learnable));

        if (order is { Recovering: true })
        {
            Learnable.Clear();
            DerelictSalvageButton.DrawRecovery(batch, RecoveryRect, RecoveryBar, player, d, order);
            return;
        }
        for (int i = 0; i < Learnable.Count; ++i)
        {
            Rectangle rect = TechRect(i);
            DerelictTechIcons.DrawIcon(batch, rect, Learnable[i]);
            if (order != null)
            {
                string points = "+" + DerelictTechIcons.Points(DerelictTechIcons.GainedOf(order, Learnable[i]));
                DrawCentered(batch, Fonts.Arial8Bold, points, PointsColor, rect.CenterX(), rect.Bottom + DerelictTechIcons.BarHeight + 7);
            }
        }
    }

    TechEntry TechAt(Vector2 cursor)
    {
        for (int i = 0; i < Learnable.Count; ++i)
            if (TechRect(i).HitTest(cursor))
                return Learnable[i];
        return null;
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

    // the Build Salvage Station button while a station can be ordered in the derelict's system, greyed with no design to build or with other empires working every derelict there; once ordered, its status in the button's place
    void DrawStationOrder(SpriteBatch batch, Derelict d)
    {
        switch (SystemDerelictsScreen.StationOrderIn(Universe.Player, d.System))
        {
            case SystemDerelictsScreen.StationOrder.Deploy:   DrawStationButton(batch, enabled: true);                                           break;
            case SystemDerelictsScreen.StationOrder.NoDesign:
            case SystemDerelictsScreen.StationOrder.AllWorked: DrawStationButton(batch, enabled: false);                                         break;
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
        DrawCentered(batch, Fonts.TahomaBold9, Localizer.Token(GameText.BuildSalvageStation), enabled ? ButtonText : Color.Gray,
                    StationRect.CenterX(), StationRect.CenterY());
    }

    // two lines only in the smaller font, so a language's longer status stays clear of the techs under it
    readonly FittedStatus Status = new();

    void DrawStatus(SpriteBatch batch, GameText status, Color color)
    {
        Status.Fit(Localizer.Token(status), StatusRect.Width);
        Status.Draw(batch, color, StatusRect.CenterX(), StatusRect.CenterY(), centered: true);
    }

    static void DrawCentered(SpriteBatch batch, Graphics.Font font, string text, Color color, int centerX, int centerY)
    {
        var pos = new Vector2(centerX - font.TextWidth(text) / 2, centerY - font.LineSpacing / 2);
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

    // the drawer's tab opens and closes it; tooltips over the icons, the bar, the station order, the techs and the drawer's buttons; a click on the panel or the open drawer stays on it
    public override bool HandleInput(InputState input)
    {
        Derelict d = Universe.SelectedDerelict;
        if (d is not { SelectableByPlayer: true })
            return false;

        Empire player = Universe.Player;
        SalvageDerelict.Block salvageBlock = SalvageDerelict.WhyNoOrder(player, d);
        bool orders = DerelictSalvageButton.Shown(player, d, salvageBlock);
        if (orders && Drawer.HandleInput(input))
        {
            State = Drawer.Open ? ElementState.TransitionOn : ElementState.TransitionOff;
            return true;
        }

        Vector2 cursor = input.CursorPosition;
        if (StatusRect.HitTest(cursor) && HandleStationOrder(input, d))
            return true;

        bool drawerOpen = orders && State == ElementState.Open;
        if (drawerOpen && (SalvageRect.HitTest(cursor) || RecoverRect.HitTest(cursor)))
        {
            SalvageDerelict order = DerelictSalvageButton.StandingOrder(player, d, salvageBlock);
            HandleButton(input, d, DerelictSalvageButton.Kind.Salvage, salvageBlock, order, SalvageRect);
            HandleButton(input, d, DerelictSalvageButton.Kind.Recover, DerelictSalvageButton.BlockOf(DerelictSalvageButton.Kind.Recover, salvageBlock, d), order, RecoverRect);
            return true;
        }

        Empire claimant = d.Claimant;
        if (OwnerRect.HitTest(cursor))
            ToolTip.CreateTooltip(string.Format(Localizer.Token(GameText.DerelictOriginalOwner), d.Owner.Name),
                                  codexUid: Codex.CodexHooks.Find(GameText.DerelictOriginalOwner));
        else if (TurnsRect.HitTest(cursor))
            ToolTip.CreateTooltip(claimant != null ? GameText.DerelictTurnsStandTip : GameText.DerelictTurnsLeftTip);
        else if (TechsRect.HitTest(cursor))
            ToolTip.CreateTooltip(SalvageDerelict.SeesTechs(Universe.Player, d) ? GameText.DerelictTechsShownTip : GameText.DerelictTechsHeldTip);
        else if (ChanceRect.HitTest(cursor) && DerelictRecovery.CanRecover(d))
            ChanceTooltip(d);
        else if ((RecoveryRect.HitTest(cursor) || RecoveryBar.pBar.HitTest(cursor))
                 && DerelictSalvageButton.StandingOrder(player, d, salvageBlock) is { Recovering: true } recovery)
            DerelictSalvageButton.RecoveryTooltip(player, d, recovery);
        else if (TechAt(cursor) is { } tech)
            DerelictTechIcons.ShowTooltip(tech, SalvageDerelict.OrderOf(Universe.Player, d));
        else if (ProductionRect.HitTest(cursor) || ProductionBar.pBar.HitTest(cursor))
            ToolTip.CreateTooltip(GameText.DerelictProductionTip);
        else if (claimant != null && ClaimLine.HitTest(cursor))
            DerelictSalvageButton.ClaimTooltip(d, claimant);
        return ElementRect.HitTest(cursor) || drawerOpen && Drawer.ButtonHousing.HitTest(cursor);
    }

    // the drawer button's tooltip and click, where the cursor is on it
    void HandleButton(InputState input, Derelict d, DerelictSalvageButton.Kind kind, SalvageDerelict.Block block, SalvageDerelict order, in Rectangle rect)
    {
        if (!rect.HitTest(input.CursorPosition))
            return;
        bool lit = DerelictSalvageButton.Lit(kind, block, order);
        DerelictSalvageButton.Tooltip(Universe.Player, d, kind, block, lit, order);
        if (input.LeftMouseClick)
            DerelictSalvageButton.Click(Universe.Player, d, kind, block, lit);
    }

    // the player's chance, what it is made of, and the share of failures that explode
    void ChanceTooltip(Derelict d)
    {
        Empire player = Universe.Player;
        string tip = string.Format(Localizer.Token(GameText.DerelictRecoveryChanceTip),
                                   DerelictSalvageButton.Percent(DerelictRecovery.Chance(player, d)),
                                   DerelictSalvageButton.Percent(DerelictRecovery.HullChance(d.Design.HullRole)),
                                   DerelictSalvageButton.Percent(DerelictRecovery.CatastropheChanceOf(player)));
        ToolTip.CreateTooltip(tip, codexUid: Codex.CodexHooks.Find(GameText.DerelictRecoveryChanceTip));
    }

    // the button's tooltip and click over the button, or the status's tooltip over its line; false where the cursor is on neither
    bool HandleStationOrder(InputState input, Derelict d)
    {
        SystemDerelictsScreen.StationOrder order = SystemDerelictsScreen.StationOrderIn(Universe.Player, d.System);
        switch (order)
        {
            case SystemDerelictsScreen.StationOrder.Deploy:
            case SystemDerelictsScreen.StationOrder.NoDesign:
            case SystemDerelictsScreen.StationOrder.AllWorked:
                if (!StationRect.HitTest(input.CursorPosition))
                    return false;
                bool canOrder = order == SystemDerelictsScreen.StationOrder.Deploy;
                ToolTip.CreateTooltip(canOrder ? GameText.SalvageStationOrderTip
                                    : order == SystemDerelictsScreen.StationOrder.AllWorked ? GameText.SalvageStationAllWorkedTip
                                    : GameText.SalvageStationNoDesignTip);
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
