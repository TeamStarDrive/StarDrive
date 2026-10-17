using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.UI;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the salvageable derelicts the player knows of in one system, opened from its Exotic Systems row, with the player's salvage station order there
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

    sealed class DerelictListItem : ScrollListItem<DerelictListItem>
    {
        readonly Derelict Derelict;

        public DerelictListItem(Derelict derelict)
        {
            Derelict = derelict;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Draw(Derelict.Design.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);
            batch.DrawString(Fonts.Arial12Bold, Derelict.Design.Name, new Vector2(X + 40f, Y + 8f), Color.White);
        }
    }

    public override void LoadContent()
    {
        RectF rect = new(ScreenWidth / 2 - 200, 200, 400, 500);
        string title = string.Format(Localizer.Token(GameText.DerelictsInSystem), System.Name);
        var list = new ScrollList<DerelictListItem>(rect);
        var derelictsList = Add(new SubmenuScrollList<DerelictListItem>(rect, title, list));
        derelictsList.SetBackground(Colors.TransparentBlackFill);
        RectF area = derelictsList.ClientArea;
        list.RectF = new(area.X, area.Y, area.W, area.H - 45); // the station button sits below the list, inside the frame

        Derelict[] derelicts = System.Universe.Derelicts.Current;
        for (int i = 0; i < derelicts.Length; ++i)
            if (derelicts[i].System == System && derelicts[i].SelectableByPlayer)
                list.AddItem(new DerelictListItem(derelicts[i]));

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

    public override void Update(float fixedDeltaTime)
    {
        UpdateStationOrder();
        base.Update(fixedDeltaTime);
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
