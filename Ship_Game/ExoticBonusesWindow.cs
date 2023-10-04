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
            const int windowWidth = 600;
            int windowHeight = (ResourceManager.GetNumExoticGoods() * (Fonts.Arial12Bold.LineSpacing+20));
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
            readonly ProgressBar ConsumptionBar;
            readonly ProgressBar StorageBar;
            public ExoticStats(Empire owner, EmpireExoticBonuses bonus)
            {
                Owner = owner;
                ExoticBonusType = bonus.Good.ExoticBonusType;
                BonusInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.White);
                Icon = new UIPanel(new Rectangle(-100, -100, 20, 20), ResourceManager.Texture($"Goods/{bonus.Good.UID}"));
                ResourceName = new UILabel(new Vector2(-100, -100), new LocalizedText(bonus.Good.RefinedNameIndex), Fonts.Arial12Bold, Color.Wheat);
                ConsumptionBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0);
                StorageBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { color = "blue"} ;
                ConsumptionBar.Fraction10Values = true;
                StorageBar.Fraction10Values = true;
                StorageBar.DrawPercentage = true;
            }
            public override void PerformLayout()
            {
                BonusInfo.Pos = new Vector2(Pos.X, Pos.Y+2);
                BonusInfo.PerformLayout();
                Icon.Pos = new Vector2(Pos.X+50, Pos.Y);
                Icon.PerformLayout();
                ResourceName.Pos = new Vector2(Pos.X+80, Pos.Y+2);
                ResourceName.PerformLayout();
                ConsumptionBar.SetRect(new Rectangle((int)Pos.X + 180, (int)Pos.Y + 2, 150, 18));
                StorageBar.SetRect(new Rectangle((int)Pos.X + 335, (int)Pos.Y + 2, 150, 18));
            }
            public override bool HandleInput(InputState input)
            {
                /*return Check.HandleInput(input) || Options.HandleInput(input);*/
                return false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                BonusInfo.Draw(batch, elapsed);
                Icon.Draw(batch, elapsed);
                ResourceName.Draw(batch, elapsed);
                ConsumptionBar.Draw(batch);
                StorageBar.Draw(batch);
            }

            public override void Update(float fixedDeltaTime)
            {
                var exoticBonus = Owner.ExoticBonuses[ExoticBonusType];
                BonusInfo.Text = exoticBonus.DynamicBonusString;
                ConsumptionBar.Max = exoticBonus.Consumption;
                ConsumptionBar.Progress = exoticBonus.TotalRefinedPerTurn;
                StorageBar.Max = exoticBonus.MaxStorage;
                StorageBar.Progress = exoticBonus.CurrentStorage;
               // base.Update(fixedDeltaTime);
            }
        }


        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            ExoticBonuses = Player.ExoticBonuses;

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
