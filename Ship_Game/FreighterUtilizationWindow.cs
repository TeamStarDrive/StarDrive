using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using SDUtils;
using Ship_Game.Ships;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Ship_Game.Commands.Goals;

namespace Ship_Game
{
    public sealed class FreighterUtilizationWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        public float TotalUtilizedCargo;
        readonly UniverseScreen Screen;
        Submenu ConstructionSubMenu;
        ProgressBar UtilizationBar;
        Map<Goods, GoodsUtilization> GoodsUtilizationMap = new Map<Goods, GoodsUtilization>();
        UIButton BuildFreighter;
        Empire Player => Screen.Player;
        float UpdateTimer;
        int TotalFreighters;
        int NumUtilizedFreighters;
        UILabel FreighterConstructingLabel;
        UILabel NumIdleFreightersLabel;

        public FreighterUtilizationWindow(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            const int windowWidth = 650;
            int windowHeight = 4 * (Fonts.Arial12Bold.LineSpacing + 25);
            Rect = new Rectangle((int)Screen.Minimap.X - 5 - windowWidth, (int)Screen.Minimap.Y +
                (int)Screen.Minimap.Height - windowHeight - 10, windowWidth, windowHeight);
            CanEscapeFromScreen = false;
            GoodsUtilizationMap.Add(Goods.Food, new GoodsUtilization(Goods.Food, this));
            GoodsUtilizationMap.Add(Goods.Production, new GoodsUtilization(Goods.Production, this));
            GoodsUtilizationMap.Add(Goods.Colonists, new GoodsUtilization(Goods.Colonists, this));
            UtilizationBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { DrawPercentage = true };
            BuildFreighter = Button(ButtonStyle.BigDip, GameText.BuildFrieghter, OnBuildFreighterClick);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();

            RectF win = new(Rect);
            ConstructionSubMenu = new(win, GameText.FreighterUtilization);
            float titleOffset = win.Y + 40;
            Add(new UILabel(new Vector2(win.X + 15, titleOffset), GameText.TotalFreighterUtilization, Fonts.Arial12Bold, Color.Gold, GameText.TotalUtilizationTip));
            Add(new UILabel(new Vector2(win.X + 210, titleOffset), GameText.CargoDistribution, Fonts.Arial12Bold, Color.White, GameText.CargoDistributionTip));
            Add(new UILabel(new Vector2(win.X + 370, titleOffset), GameText.Freighters, Fonts.Arial12Bold, Color.White, GameText.NumberOfFreightersTip));
            Add(new UILabel(new Vector2(win.X + 470, titleOffset), GameText.ImportingPlanets, Fonts.Arial12Bold, Color.White));
            Add(new UILabel(new Vector2(win.X + 570, titleOffset), GameText.ExportingPlanets, Fonts.Arial12Bold, Color.White));
            Add(new UILabel(new Vector2(win.X + 15, titleOffset + 50), GameText.IdleFrieghters, Fonts.Arial12Bold, Color.Wheat));
            Add(new UILabel(new Vector2(win.X + 15, titleOffset + 70), GameText.FreightersUnderConstruction, Fonts.Arial12Bold, Color.Wheat));

            NumIdleFreightersLabel     = new UILabel(new Vector2(win.X + 150, titleOffset + 50), "", Fonts.Arial12Bold, Color.White);
            FreighterConstructingLabel = new UILabel(new Vector2(win.X + 150, titleOffset + 70), "", Fonts.Arial12Bold, Color.White);

            UIList utilizationData = AddList(new(win.X + 5f, win.Y + 40));
            utilizationData.Padding = new(2f, 25f);
            foreach (GoodsUtilization gu in  GoodsUtilizationMap.Values)
                utilizationData.Add(gu);
        }

        public override void PerformLayout()
        {
            UtilizationBar.SetRect(new Rectangle((int)Pos.X + 10, (int)Pos.Y+65, 150, 18));
            BuildFreighter.Pos = new Vector2(Pos.X + 5, Pos.Y + 135);
            base.PerformLayout();
        }

        public void ToggleVisibility()
        {
            GameAudio.AcceptClick();
            IsOpen = !IsOpen;
            if (IsOpen)
            {
                Screen.ExoticBonusesWindow.CloseWindow();
                LoadContent();
            }
        }

        public void CloseWindow()
        {
            IsOpen = false;
            Visible = false;
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
            UtilizationBar.Draw(batch);
            BuildFreighter.Draw(batch, elapsed);
            FreighterConstructingLabel.Draw(batch, elapsed);
            NumIdleFreightersLabel.Draw(batch, elapsed);
            DrawLine(new Vector2(Pos.X + 180, Pos.Y + 35), new Vector2(Pos.X + 180, Pos.Y + Height - 10), Color.Wheat, 2);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsOpen)
                return false;

            if (BuildFreighter.HandleInput(input))
                return true;

            base.HandleInput(input);
            return false;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!IsOpen || Player.Universe.Paused)
                return;

            UpdateTimer -= fixedDeltaTime;
            if (UpdateTimer <= 0)
            {
                UpdateTimer = 1;
                TotalFreighters = Player.TotalFreighters;
                float totalUtilizedCargo = 0;
                foreach (GoodsUtilization goodsUtilization in GoodsUtilizationMap.Values)
                    goodsUtilization.Reset();


                foreach (Planet planet in Player.GetPlanets())
                {
                    if (planet.FoodImportSlots > 0)      GoodsUtilizationMap[Goods.Food].IncreaseNumImportingPlanets();
                    if (planet.FoodExportSlots > 0)      GoodsUtilizationMap[Goods.Food].IncreaseNumExportingPlanets();
                    if (planet.ProdImportSlots > 0)      GoodsUtilizationMap[Goods.Production].IncreaseNumImportingPlanets();
                    if (planet.ProdExportSlots > 0)      GoodsUtilizationMap[Goods.Production].IncreaseNumExportingPlanets();
                    if (planet.ColonistsImportSlots > 0) GoodsUtilizationMap[Goods.Colonists].IncreaseNumImportingPlanets();
                    if (planet.ColonistsExportSlots > 0) GoodsUtilizationMap[Goods.Colonists].IncreaseNumExportingPlanets();
                }

                var allUtilizedFreightesr = Player.OwnedShips.Filter(s => s.IsFreighter && s.AI.State == AI.AIState.SystemTrader);
                NumUtilizedFreighters = allUtilizedFreightesr.Length;
                foreach (Ship freighter in allUtilizedFreightesr)
                {
                    GoodsUtilizationMap[Goods.Food].AddGoodsTransported(freighter, ref totalUtilizedCargo);
                    GoodsUtilizationMap[Goods.Production].AddGoodsTransported(freighter, ref totalUtilizedCargo);
                    GoodsUtilizationMap[Goods.Colonists].AddGoodsTransported(freighter, ref totalUtilizedCargo);
                }

                TotalUtilizedCargo = totalUtilizedCargo;
                UtilizationBar.Progress = TotalFreighters == 0 ? 0 : (float)NumUtilizedFreighters/TotalFreighters*100;
                FreighterConstructingLabel.Text = Player.FreightersBeingBuilt.String();
                NumIdleFreightersLabel.Text = (TotalFreighters - NumUtilizedFreighters).String();
            }

            base.Update(fixedDeltaTime);
        }

        void OnBuildFreighterClick(UIButton b)
        {
            Player.AI.AddGoalAndEvaluate(new IncreaseFreighters(Player));
            FreighterConstructingLabel.Text = Player.FreightersBeingBuilt.String();
        }

        class GoodsUtilization : UIElementV2
        {
            readonly ProgressBar UtilizationBar;
            readonly UILabel NumFreightersLabel;
            readonly UILabel NumImportingLabel;
            readonly UILabel NumExportingLabel;
            readonly UIPanel IconPanel;
            readonly FreighterUtilizationWindow Window;
            readonly Goods Goods;
            public int NumImportingPlanets { get; private set; }
            public int NumExportingPlanets { get; private set; }
            public int NumFreighters { get; private set; }
            public float TotalEmpireUtilizedCargo { get; private set; }
            public float GoodsTransported { get; private set; }


            public GoodsUtilization(Goods goods, FreighterUtilizationWindow parent)
            {
                Window = parent;
                Goods  = goods;
                UtilizationBar     = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { DrawPercentage = true };
                NumFreightersLabel = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                NumImportingLabel  = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                NumExportingLabel  = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);

                SubTexture Icon = ResourceManager.Texture("Goods/Production");
                if (goods == Goods.Food)
                {
                    Icon = ResourceManager.Texture("Goods/Food");
                    UtilizationBar.color = "green";
                }
                else if (goods == Goods.Colonists)
                {
                    Icon = ResourceManager.Texture("Goods/Colonists_1000");
                    UtilizationBar.color = "blue";
                }

                IconPanel = new UIPanel(new Rectangle(-100, -100, 25, 25), Icon);
            }

            public override void PerformLayout()
            {
                IconPanel.Pos = new Vector2(Pos.X + 175, Pos.Y - 5);
                IconPanel.PerformLayout();
                UtilizationBar.SetRect(new Rectangle((int)Pos.X + 200, (int)Pos.Y, 150, 18));
                NumFreightersLabel.Pos = new Vector2(Pos.X + 390, Pos.Y);
                NumFreightersLabel.PerformLayout();
                NumImportingLabel.Pos = new Vector2(Pos.X + 490, Pos.Y);
                NumImportingLabel.PerformLayout();
                NumExportingLabel.Pos = new Vector2(Pos.X + 590, Pos.Y);
                NumExportingLabel.PerformLayout();
                base.PerformLayout();
            }

            public override bool HandleInput(InputState input)
            {
                return false;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                UtilizationBar.Draw(batch);
                IconPanel.Draw(batch, elapsed);
                NumFreightersLabel.Draw(batch, elapsed);
                NumImportingLabel.Draw(batch, elapsed);
                NumExportingLabel.Draw(batch, elapsed);
                NumExportingLabel.Color = Color.White;
                NumImportingLabel.Color = Color.White;
                if (NumExportingPlanets == 0 && NumImportingPlanets > 0 && GoodsTransported <= 0)
                {
                    NumExportingLabel.Color = Color.Red;
                    NumImportingLabel.Color = Color.Yellow;
                }
                else if (NumImportingPlanets > NumExportingPlanets)
                {
                    NumExportingLabel.Color = Color.Yellow;
                    NumImportingLabel.Color = Color.Yellow;
                }

                if (NumImportingPlanets > 0 && NumFreighters < NumImportingPlanets)
                    NumFreightersLabel.Color = NumFreighters == 0 ? Color.Red : Color.Yellow;
                else
                    NumFreightersLabel.Color = NumFreighters > 0 ? Color.White : Color.Wheat;
            }

            public override void Update(float fixedDeltaTime)
            {
                TotalEmpireUtilizedCargo = Window.TotalUtilizedCargo;
                UtilizationBar.Progress  = TotalEmpireUtilizedCargo == 0 ? 0 : GoodsTransported/TotalEmpireUtilizedCargo *100;
                NumFreightersLabel.Text  = NumFreighters.String();
                NumImportingLabel.Text   = NumImportingPlanets.String();
                NumExportingLabel.Text   = NumExportingPlanets.String();
                base.Update(fixedDeltaTime);
            }


            public void IncreaseNumImportingPlanets()
            {
                NumImportingPlanets++;
            }

            public void IncreaseNumExportingPlanets()
            {
                NumExportingPlanets++;
            }

            public void AddGoodsTransported(Ship freighter, ref float totalUtilized)
            {
                if (freighter.AI.HasTradeGoal(Goods))
                {
                    GoodsTransported += freighter.CargoSpaceMax;
                    totalUtilized    += freighter.CargoSpaceMax;
                    NumFreighters++;
                }
            }

            public void SetMaxEmpireCargo(float value)
            {
                TotalEmpireUtilizedCargo = value;
            }

            public void Reset()
            {
                NumImportingPlanets = 0;
                NumExportingPlanets = 0;
                NumFreighters       = 0;
                GoodsTransported    = 0;
                TotalEmpireUtilizedCargo      = 0;
            }
        }
    }
}
