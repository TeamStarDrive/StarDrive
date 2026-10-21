using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.UI;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the salvageable derelicts the player knows of in one system, opened from its Exotic Systems row, with the player's salvage station order there and orders to salvage or recover each
public sealed class SystemDerelictsScreen : GameScreen
{
    public override bool HelpKeyOpensCodex => true;

    readonly UniverseScreen Universe;
    readonly SolarSystem System;
    readonly SeededRandom SiteRandom = new();
    public UIButton StationButton { get; private set; }
    public UILabel StationStatus { get; private set; }
    Empire Player => Universe.Player;

    public SystemDerelictsScreen(UniverseScreen universe, SolarSystem system) : base(universe, toPause: null)
    {
        Universe = universe;
        System = system;
        IsPopup = true;
        TransitionOnTime = 0.25f;
        TransitionOffTime = 0.25f;
    }

    const int WindowWidth = 840;
    // the columns from a row's left edge: the hull's icon, then its name over its production bar, the salvager's flag, the techs and the status
    const int NameX = 40;
    const int SalvagerX = 230;
    const int SalvagerWidth = 70;
    const int TechsX = 312;
    const int TechSpacing = 32;
    const int StatusX = 512;
    const int HeaderHeight = 22;

    // a derelict's hull and name, its production left, the flag of the empire salvaging it, its techs and its salvage's status, and the Salvage and Recover buttons, as on its panel
    sealed class DerelictListItem : ScrollListItem<DerelictListItem>
    {
        readonly UniverseScreen Universe;
        public readonly Derelict Derelict;
        readonly ProgressBar ProductionBar = new(0, 0, 150, 18) { color = "red" }; // as on the derelict's panel
        readonly ProgressBar RecoveryBar = DerelictSalvageButton.RecoveryBar(new(0, 0, 150, 18));
        // the techs the player can learn from it as last drawn, empty while the player does not see them or recovers it
        readonly Array<TechEntry> Learnable = new();
        readonly FittedStatus Status = new();

        public DerelictListItem(UniverseScreen universe, Derelict derelict)
        {
            Universe = universe;
            Derelict = derelict;
        }

        Rectangle ProductionRect => new((int)X + NameX, (int)Y + 21, 16, 16);
        Rectangle ClaimRect => new((int)X + SalvagerX + SalvagerWidth / 2 - 10, (int)Y + 10, 20, 20);
        Rectangle TechsRect => new((int)X + TechsX, (int)Y + 10, 19, 20);
        Rectangle TechRect(int i) => new((int)X + TechsX + i * TechSpacing, (int)Y + 6, 26, 22);
        // in the techs' place, at the production bar's sizes
        Rectangle RecoveryRect => new((int)X + TechsX, (int)Y + 12, 16, 16);
        Rectangle RecoveryBarRect => new((int)X + TechsX + 21, (int)Y + 11, 150, 18);
        Rectangle SalvageRect => new((int)(X + Width) - 80, (int)Y + 2, 36, 36);
        Rectangle RecoverRect => new((int)(X + Width) - 40, (int)Y + 2, 36, 36);
        // up to 6 short of the Salvage button
        int StatusWidth => SalvageRect.X - 6 - ((int)X + StatusX);

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Draw(Derelict.Design.Icon, new Rectangle((int)X, (int)Y + 5, 29, 30), Color.White);
            batch.DrawString(Fonts.Arial12Bold, Derelict.Design.Name, new Vector2(X + NameX, Y + 2f), Color.White);

            Rectangle production = ProductionRect;
            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), production, Color.White);
            ProductionBar.SetRect(new Rectangle(production.X + production.Width + 5, (int)Y + 20, 150, 18));
            ProductionBar.Max = Derelict.Production;
            ProductionBar.Progress = Derelict.ProductionLeft;
            ProductionBar.Draw(batch);

            if (Derelict.Claimant is { } claimant)
                batch.Draw(ResourceManager.Flag(claimant), ClaimRect, Universe.ApplyCurrentAlphaToColor(claimant.EmpireColor));

            Empire player = Universe.Player;
            SalvageDerelict.Block block = SalvageDerelict.WhyNoOrder(player, Derelict);
            SalvageDerelict order = DerelictSalvageButton.StandingOrder(player, Derelict, block);
            DrawTechs(batch, order);
            DrawStatus(batch);
            if (DerelictSalvageButton.Shown(player, Derelict, block))
            {
                DrawButton(batch, DerelictSalvageButton.Kind.Salvage, block, order, SalvageRect);
                DrawButton(batch, DerelictSalvageButton.Kind.Recover, DerelictSalvageButton.BlockOf(DerelictSalvageButton.Kind.Recover, block, Derelict), order, RecoverRect);
            }
        }

        void DrawButton(SpriteBatch batch, DerelictSalvageButton.Kind kind, SalvageDerelict.Block block, SalvageDerelict order, in Rectangle rect)
        {
            bool lit = DerelictSalvageButton.Lit(kind, block, order);
            DerelictSalvageButton.Draw(batch, rect, kind, block, lit, rect.HitTest(Universe.Input.CursorPosition));
        }

        // the button's tooltip and click, where the cursor is on it
        void HandleButton(InputState input, DerelictSalvageButton.Kind kind, SalvageDerelict.Block block, SalvageDerelict order, in Rectangle rect)
        {
            if (!rect.HitTest(input.CursorPosition))
                return;
            bool lit = DerelictSalvageButton.Lit(kind, block, order);
            DerelictSalvageButton.Tooltip(Universe.Player, Derelict, kind, block, lit, order);
            if (input.LeftMouseClick)
                DerelictSalvageButton.Click(Universe.Player, Derelict, kind, block, lit);
        }

        // the count of all it holds until the player sees its techs, then the icons of those the player can learn, or while the player's order recovers it, the recovery's progress in their place
        void DrawTechs(SpriteBatch batch, SalvageDerelict order)
        {
            Empire player = Universe.Player;
            if (order is { Recovering: true })
            {
                Learnable.Clear();
                RecoveryBar.SetRect(RecoveryBarRect);
                DerelictSalvageButton.DrawRecovery(batch, RecoveryRect, RecoveryBar, player, Derelict, order);
                return;
            }
            if (!SalvageDerelict.SeesTechs(player, Derelict))
            {
                Learnable.Clear();
                DerelictTechIcons.DrawCount(batch, TechsRect, Derelict.Techs.Length);
                return;
            }
            DerelictTechIcons.Learnable(player, Derelict, Learnable);
            for (int i = 0; i < Learnable.Count; ++i)
                DerelictTechIcons.DrawIcon(batch, TechRect(i), Learnable[i]);
        }

        // the player's salvage or recovery in wheat, with the turns left while its platform breaks in, another empire's in its colour
        void DrawStatus(SpriteBatch batch)
        {
            Empire player = Universe.Player;
            SalvageDerelict.Stage stage = SalvageDerelict.StageOf(player, Derelict, out Empire claimant, out SalvageDerelict order);
            if (stage == SalvageDerelict.Stage.None)
                return;

            Status.Fit(stage == SalvageDerelict.Stage.BreakingIn ? BreakingIn(order.BreakInTurnsLeft) : Localizer.Token(stage switch
            {
                SalvageDerelict.Stage.PlatformOrdered  => GameText.SalvagePlatformOrdered,
                SalvageDerelict.Stage.BuildingPlatform => GameText.BuildingSalvagePlatform,
                SalvageDerelict.Stage.Recovering       => GameText.DerelictRecovering,
                _                                      => GameText.DerelictSalvaging,
            }), StatusWidth);
            Status.Draw(batch, claimant != null && claimant != player ? claimant.EmpireColor : Color.Wheat, X + StatusX, Y + 20, centered: false);
        }

        // made again only when the turns left change, so the status keeps its fit
        string BreakingInText;
        int BreakingInTurns = -1;

        string BreakingIn(int turnsLeft)
        {
            if (turnsLeft != BreakingInTurns)
            {
                BreakingInTurns = turnsLeft;
                BreakingInText = string.Format(Localizer.Token(GameText.DerelictBreakingIn), turnsLeft);
            }
            return BreakingInText;
        }

        TechEntry TechAt(Vector2 cursor)
        {
            for (int i = 0; i < Learnable.Count; ++i)
                if (TechRect(i).HitTest(cursor))
                    return Learnable[i];
            return null;
        }

        public override bool HandleInput(InputState input)
        {
            Vector2 cursor = input.CursorPosition;
            if (SalvageRect.HitTest(cursor) || RecoverRect.HitTest(cursor))
            {
                Empire player = Universe.Player;
                SalvageDerelict.Block block = SalvageDerelict.WhyNoOrder(player, Derelict);
                if (DerelictSalvageButton.Shown(player, Derelict, block))
                {
                    SalvageDerelict order = DerelictSalvageButton.StandingOrder(player, Derelict, block);
                    HandleButton(input, DerelictSalvageButton.Kind.Salvage, block, order, SalvageRect);
                    HandleButton(input, DerelictSalvageButton.Kind.Recover, DerelictSalvageButton.BlockOf(DerelictSalvageButton.Kind.Recover, block, Derelict), order, RecoverRect);
                    return true;
                }
            }

            if (Derelict.Claimant is { } claimant && ClaimRect.HitTest(cursor))
                ToolTip.CreateTooltip(DerelictSalvageButton.ClaimTip(Derelict, claimant));
            else if (ProductionRect.HitTest(cursor) || ProductionBar.pBar.HitTest(cursor))
                ToolTip.CreateTooltip(GameText.DerelictProductionTip);
            else if ((RecoveryRect.HitTest(cursor) || RecoveryBarRect.HitTest(cursor))
                     && DerelictSalvageButton.StandingOrder(Universe.Player, Derelict, SalvageDerelict.WhyNoOrder(Universe.Player, Derelict)) is { Recovering: true } recovery)
                DerelictSalvageButton.RecoveryTooltip(Universe.Player, Derelict, recovery);
            else if (TechsRect.HitTest(cursor) && !SalvageDerelict.SeesTechs(Universe.Player, Derelict))
                ToolTip.CreateTooltip(GameText.DerelictTechsHeldTip);
            else if (TechAt(cursor) is { } tech)
                ToolTip.CreateTooltip(DerelictTechIcons.Tooltip(tech, SalvageDerelict.OrderOf(Universe.Player, Derelict)));
            return base.HandleInput(input);
        }
    }

    public override void LoadContent()
    {
        RectF rect = new(ScreenWidth / 2 - WindowWidth / 2, 200, WindowWidth, 500);
        string title = string.Format(Localizer.Token(GameText.DerelictsInSystem), System.Name);
        var list = Rows = new ScrollList<DerelictListItem>(rect);
        var derelictsList = Add(new SubmenuScrollList<DerelictListItem>(rect, title, list));
        derelictsList.SetBackground(Colors.TransparentBlackFill);
        RectF area = derelictsList.ClientArea;
        // the column titles above the list, and the station button below it, inside the frame
        list.RectF = new(area.X, area.Y + HeaderHeight, area.W, area.H - HeaderHeight - 45);
        AddHeaders(area);

        Derelict[] derelicts = System.Universe.Derelicts.Current;
        for (int i = 0; i < derelicts.Length; ++i)
            if (derelicts[i].System == System && derelicts[i].SelectableByPlayer)
                list.AddItem(new DerelictListItem(Universe, derelicts[i]));

        var btn = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
        var stationRect = new Rectangle((int)rect.CenterX - btn.Width / 2, (int)rect.Bottom - btn.Height - 12, btn.Width, btn.Height);
        StationButton = Button(ButtonStyle.Default, GameText.DeploySalvageStation, OnStationClicked);
        StationButton.Font = Fonts.TahomaBold9;
        StationButton.Rect = stationRect;
        StationButton.ClickSfx = null;
        StationStatus = Add(new UILabel(new Rectangle((int)rect.X, stationRect.Y, (int)rect.W, stationRect.Height),
                                        "", Fonts.Arial12Bold, Color.Gray) { TextAlign = TextAlign.Center });
        Shown = null; // a reload builds a new button and label
        UpdateStationOrder();

        base.LoadContent();
    }

    // over the rows' columns, which start at the list's left padding, with a line under them
    void AddHeaders(RectF area)
    {
        float x = area.X + ScrollListBase.PaddingLeft, y = area.Y + 6f;
        Graphics.Font font = Fonts.Arial12Bold;
        Add(new UILabel(new Vector2(x + NameX, y), GameText.Derelict, font, Color.Wheat));
        Add(new UILabel(new Rectangle((int)x + SalvagerX, (int)y, SalvagerWidth, font.LineSpacing), GameText.DerelictSalvager, font, Color.Wheat)
            { TextAlign = TextAlign.Center });
        Add(new UILabel(new Vector2(x + TechsX, y), GameText.Technologies, font, Color.Wheat));
        Add(new UILabel(new Vector2(x + StatusX, y), GameText.DerelictStatus, font, Color.Wheat));
        Add(new UIPanel(new Rectangle((int)x, (int)area.Y + HeaderHeight + 4, (int)(area.W - ScrollListBase.PaddingLeft - ScrollListBase.PaddingRight), 1),
                        new Color(60, 56, 44)));
    }

    public override void Update(float fixedDeltaTime)
    {
        RemoveGoneRows();
        UpdateStationOrder();
        base.Update(fixedDeltaTime);
    }

    ScrollList<DerelictListItem> Rows;

    public bool Lists(Derelict d) => Rows.Any(row => row.Derelict == d);

    // a row leaves once its derelict can no longer be selected: recovered as its ship appears, broken apart, blown up, stripped or sunk away
    void RemoveGoneRows()
    {
        for (int i = Rows.NumEntries - 1; i >= 0; --i)
            if (!Rows[i].Derelict.SelectableByPlayer)
                Rows.Remove(Rows[i]);
    }

    public enum StationOrder { None, Deploy, Abort, Deployed, NoDesign }
    StationOrder? Shown;

    // Deploy while a station can be ordered, Abort while the order is not built, then Deployed; grey text when no design can be built
    public static StationOrder StationOrderIn(Empire empire, SolarSystem system)
    {
        Goal order = empire.AI.FindGoal(g => g?.IsSalvageOpsGoal(system) == true);
        if (order != null)
            return order.TargetShip != null ? StationOrder.Deployed : StationOrder.Abort;

        return SalvageOps.WhyNoOrderIn(empire, system) switch
        {
            SalvageOps.OrderBlock.None => StationOrder.Deploy,
            SalvageOps.OrderBlock.NoDesign => StationOrder.NoDesign,
            _ => StationOrder.None,
        };
    }

    public void UpdateStationOrder()
    {
        StationOrder state = StationOrderIn(Player, System);
        if (Shown == state)
            return;

        Shown = state;
        bool abort = state == StationOrder.Abort;
        StationButton.Visible = state is StationOrder.Deploy or StationOrder.Abort;
        StationButton.Text = abort ? GameText.AbortDeployent : GameText.DeploySalvageStation;
        StationButton.Style = abort ? ButtonStyle.Military : ButtonStyle.Default;
        StationButton.Tooltip = abort ? LocalizedText.None : GameText.SalvageStationOrderTip;

        StationStatus.Visible = state is StationOrder.Deployed or StationOrder.NoDesign;
        StationStatus.Text = state == StationOrder.Deployed ? GameText.SalvageStationDeployed : GameText.SalvageStationNoDesignTip;
        StationStatus.Color = state == StationOrder.Deployed ? Player.EmpireColor : Color.Gray;
    }

    void OnStationClicked(UIButton b)
    {
        StationOrder state = StationOrderIn(Player, System);
        if (state == StationOrder.Abort)
        {
            GameAudio.EchoAffirmative();
            Universe.RunOnSimThread(() => Player.AI.CancelSalvageStation(System));
            return;
        }

        if (state == StationOrder.Deploy
            && SalvageOps.OrderNearDerelicts(Player, System, Universe.UState.Objects.GetShips(), SiteRandom))
        {
            GameAudio.EchoAffirmative();
            return;
        }

        GameAudio.NegativeClick();
        if (state == StationOrder.Deploy) // above the button, clear of its tooltip at the cursor
            ToolTip.CreateFloatingText(GameText.SalvageStationNoSite, "", new Vector2(b.X, b.Y - 70), 5);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
        batch.SafeBegin();
        base.Draw(batch, elapsed);
        batch.SafeEnd();
    }
}
