using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using SDUtils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Windows.Forms;

namespace Ship_Game
{
    public sealed class ExoticBonusesWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        readonly UniverseScreen Screen;
        UniverseState UState => Screen.UState;
        Submenu ConstructionSubMenu;
        bool ResearchStationsEnabled;
        bool MiningOpsEnabled;
        Map<ExoticBonusType, EmpireExoticBonuses> ExoticBonuses;

        Empire Player => Screen.Player;

        public ExoticBonusesWindow(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            const int windowWidth = 650;
            int windowHeight = (ResourceManager.GetNumExoticGoods() * (Fonts.Arial12Bold.LineSpacing+18));
            Rect = new Rectangle((int)Screen.Minimap.X - 5 - windowWidth, (int)Screen.Minimap.Y + 
                (int)Screen.Minimap.Height - windowHeight - 10, windowWidth, windowHeight);
        }

        class ExoticStats : UIElementV2
        {
            readonly Empire Owner;
            readonly ExoticBonusType ExoticBonusType;
            readonly UILabel BonusInfo;
            readonly UILabel ResourceName;
            readonly UIPanel Icon;
            readonly UILabel OutputInfo;
            readonly ProgressBar ConsumptionBar;
            readonly ProgressBar StorageBar;
            readonly EmpireExoticBonuses ExoticResource;
            readonly UILabel PotentialInfo;
            readonly UILabel ActiveVsTotalOps;
            public ExoticStats(Empire owner, EmpireExoticBonuses bonus)
            {
                Owner = owner;
                ExoticBonusType = bonus.Good.ExoticBonusType;
                ExoticResource = Owner.GetExoticResource(ExoticBonusType);
                BonusInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.White);
                Icon = new UIPanel(new Rectangle(-100, -100, 20, 20), ResourceManager.Texture($"Goods/{bonus.Good.UID}"));
                ResourceName = new UILabel(new Vector2(-100, -100), new LocalizedText(bonus.Good.RefinedNameIndex), Fonts.Arial12Bold, Color.Wheat);
                OutputInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                ConsumptionBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0);
                StorageBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { color = "blue", Max = 100, DrawPercentage = true };
                StorageBar.DrawPercentage = true;
                ConsumptionBar.Fraction10Values = true;
                PotentialInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                ActiveVsTotalOps = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);

            }
            public override void PerformLayout()
            {
                BonusInfo.Pos = new Vector2(Pos.X, Pos.Y+2);
                BonusInfo.PerformLayout();
                Icon.Pos = new Vector2(Pos.X+50, Pos.Y);
                Icon.PerformLayout();
                ResourceName.Pos = new Vector2(Pos.X+ 80, Pos.Y+2);
                ResourceName.PerformLayout();
                OutputInfo.Pos = new Vector2(Pos.X + 180, Pos.Y);
                OutputInfo.PerformLayout();
                ConsumptionBar.SetRect(new Rectangle((int)Pos.X + 230, (int)Pos.Y, 150, 18));
                StorageBar.SetRect(new Rectangle((int)Pos.X + 385, (int)Pos.Y, 150, 18));
                PotentialInfo.Pos = new Vector2(Pos.X + 550, Pos.Y);
                PotentialInfo.PerformLayout();
                ActiveVsTotalOps.Pos = new Vector2(Pos.X + 600, Pos.Y);
                ActiveVsTotalOps.PerformLayout();
            }
            public override bool HandleInput(InputState input)
            {
                return false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                BonusInfo.Draw(batch, elapsed);
                Icon.Draw(batch, elapsed);
                ResourceName.Draw(batch, elapsed);
                OutputInfo.Draw(batch, elapsed);
                ConsumptionBar.Draw(batch);
                StorageBar.Draw(batch);
                PotentialInfo.Draw(batch, elapsed);
                ActiveVsTotalOps.Draw(batch, elapsed);
            }

            public override void Update(float fixedDeltaTime)
            {
                var exoticResource = ExoticResource;
                BonusInfo.Text = exoticResource.DynamicBonusString;
                OutputInfo.Text = exoticResource.CurrentPercentageOutput;
                ConsumptionBar.Max = exoticResource.Consumption;
                ConsumptionBar.Progress = exoticResource.RefinedPerTurnForConsumption;
                StorageBar.Progress = exoticResource.CurrentStorage/exoticResource.MaxStorage * 100;
                PotentialInfo.Text = exoticResource.MaxPotentialRefinedPerTurn.String(1);
                ActiveVsTotalOps.Text = exoticResource.ActiveVsTotalOps;
               // base.Update(fixedDeltaTime);
            }
        }


        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            ExoticBonuses = Player.GetExoticBonuses();

            RectF win = new(Rect);
            ConstructionSubMenu = new(win, GameText.ExoticResourcesMenu);

            UIList name = AddList(new(win.X + 5f, win.Y + 20));
            name.Padding = new(2f, 25f);

            foreach (EmpireExoticBonuses type in ExoticBonuses.Values)
            {
                name.Add(new ExoticStats(Player, type));
            }
        }

        public void ToggleVisibility()
        {
            GameAudio.AcceptClick();
            IsOpen = !IsOpen;
            if (IsOpen)
                LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            Rectangle r = ConstructionSubMenu.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch, elapsed);
            ConstructionSubMenu.Draw(batch, elapsed);
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsOpen)
                return false;

            if (input.RightMouseClick || input.Escaped)
            {
                IsOpen = false;
                return false;
            }

            return false;
        }
    }
}
