using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class PlanetListScreen : GameScreen
    {
        private Menu2 TitleBar;
        private Vector2 TitlePos;

        private Menu2 EMenu;

        private float ClickTimer;
        private float ClickDelay = 0.25f;

        private Planet SelectedPlanet;
        private ScrollList PlanetSL;
        public EmpireUIOverlay EmpireUI;
        private Submenu ShipSubMenu;
        private Rectangle leftRect;

        private SortButton sb_Sys;
        private SortButton sb_Name;
        private SortButton sb_Fert;
        private SortButton sb_Rich;
        private SortButton sb_Pop;
        private SortButton sb_Owned;
        private CloseButton close;

        private UICheckBox cb_hideOwned;
        private UICheckBox cb_hideUninhabitable;

        private bool HideOwned;
        private bool HideUninhab = true;

        private readonly Array<Planet> ExploredPlanets = new Array<Planet>();

        private Rectangle eRect;
        private SortButton LastSorted;
        private Rectangle AutoButton;

        //private bool AutoButtonHover;

        public PlanetListScreen(GameScreen parent, EmpireUIOverlay empireUi, string audioCue = "")
            : base(parent)
        {
            if(!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            EmpireUI = empireUi;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            IsPopup = true;
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                //LowRes = true;
            }
            Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2((titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(1402)).X / 2f, (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
            EMenu = new Menu2(leftRect);
            close = new CloseButton(this, new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
            eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 15);
            sb_Sys = new SortButton(empireUi.empire.data.PLSort, Localizer.Token(192));
            sb_Name = new SortButton(empireUi.empire.data.PLSort, Localizer.Token(389));
            sb_Fert = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(386) );
            sb_Rich = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(387));
            sb_Pop = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(1403));
            sb_Owned = new SortButton(empireUi.empire.data.PLSort, "Owner");
            

            while (eRect.Height % 40 != 0)
            {
                eRect.Height = eRect.Height - 1;
            }
            eRect.Height = eRect.Height - 20;
            ShipSubMenu = new Submenu(eRect);
            PlanetSL = new ScrollList(ShipSubMenu, 40);
           // LastSorted = empUI.empire.data.PLSort;

            foreach (SolarSystem system in UniverseScreen.SolarSystemList.OrderBy(distance => distance.Position.Distance(EmpireManager.Player.GetWeightedCenter())))
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(EmpireManager.Player))
                        ExploredPlanets.Add(p);
                }
            }

            cb_hideOwned = new UICheckBox(this, TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 15,
                () => HideOwned, 
                x => { HideOwned = x; ResetList(); }, Fonts.Arial12Bold, "Hide Owned", 0);

            cb_hideUninhabitable = new UICheckBox(this, TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 35,
                () => HideUninhab, 
                x => { HideUninhab = x; ResetList(); }, Fonts.Arial12Bold, "Hide Uninhabitable", 0);

            AutoButton = new Rectangle(0, 0, 243, 33);
            
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw();
            batch.DrawString(Fonts.Laserian14, Localizer.Token(1402), TitlePos, new Color(255, 239, 208));
            EMenu.Draw();
            var textColor = new Color(118, 102, 67, 50);
            PlanetSL.Draw(batch);
            if (PlanetSL.NumEntries > 0)
            {
                var e1 = PlanetSL.ItemAtTop<PlanetListScreenEntry>();
                var textCursor = new Vector2(e1.SysNameRect.X + e1.SysNameRect.Width / 2 - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                
                sb_Sys.Update(textCursor);
                sb_Sys.Draw(ScreenManager);
                textCursor = new Vector2(e1.PlanetNameRect.X + e1.PlanetNameRect.Width / 2 - Fonts.Arial20Bold.MeasureString(Localizer.Token(389)).X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                
                sb_Name.Update(textCursor);
                sb_Name.Draw(ScreenManager);
                textCursor = new Vector2(e1.FertRect.X + e1.FertRect.Width / 2 - Fonts.Arial20Bold.MeasureString(Localizer.Token(386)).X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    textCursor = textCursor + new Vector2(10f, 10f);
                }
                
                sb_Fert.Update(textCursor);
                sb_Fert.Draw(ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                textCursor = new Vector2((e1.RichRect.X + e1.RichRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(387)).X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    textCursor = textCursor + new Vector2(10f, 10f);
                }
                
                sb_Rich.Update(textCursor);
                sb_Rich.Draw(ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                textCursor = new Vector2((e1.PopRect.X + e1.PopRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(1403)).X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    textCursor = textCursor + new Vector2(15f, 10f);
                }
                
                sb_Pop.Update(textCursor);
                sb_Pop.Draw(ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                textCursor = new Vector2((e1.OwnerRect.X + e1.OwnerRect.Width / 2) - Fonts.Arial20Bold.MeasureString("Owner").X / 2f, (eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    textCursor = textCursor + new Vector2(10f, 10f);
                }
                
                sb_Owned.Update(textCursor);
                sb_Owned.Draw(ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                Color smallHighlight = textColor;
                smallHighlight.A = (byte)(textColor.A / 2);

                int i = PlanetSL.FirstVisibleIndex;
                foreach (ScrollList.Entry e in PlanetSL.VisibleEntries)
                {
                    var planetListEntry = (PlanetListScreenEntry)e.item;
                    if (i % 2 == 0)
                    {
                        batch.FillRectangle(planetListEntry.TotalEntrySize, smallHighlight);
                    }
                    if (planetListEntry.planet == SelectedPlanet)
                    {
                        batch.FillRectangle(planetListEntry.TotalEntrySize, textColor);
                    }
                    planetListEntry.SetNewPos(eRect.X + 22, e.Y);
                    planetListEntry.Draw(batch);
                    batch.DrawRectangle(planetListEntry.TotalEntrySize, textColor);
                    ++i;
                }                
                Color lineColor = new Color(118, 102, 67, 255);
                Vector2 topLeftSL = new Vector2(e1.SysNameRect.X, (eRect.Y + 35));
                Vector2 botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.PlanetNameRect.X, (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.FertRect.X, (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.RichRect.X + 5), (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.PopRect.X, (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.PopRect.X + e1.PopRect.Width), (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.OwnerRect.X + e1.OwnerRect.Width), (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.TotalEntrySize.X, (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height - 35));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + eRect.Height));
                batch.DrawLine(topLeftSL, botSL, lineColor);
                Vector2 leftBot = new Vector2(e1.TotalEntrySize.X, (eRect.Y + eRect.Height));
                batch.DrawLine(leftBot, botSL, lineColor);
                leftBot = new Vector2(e1.TotalEntrySize.X, (eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (eRect.Y + 35));
                batch.DrawLine(leftBot, botSL, lineColor);
            }
            cb_hideUninhabitable.Draw(batch);
            cb_hideOwned.Draw(batch);
            close.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        private void InitSortedItems<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            LastSorted = button;
            GameAudio.PlaySfxAsync("blip_click");
            button.Ascending = !button.Ascending;
            PlanetSL.Reset();

            Planet[] planets = ExploredPlanets.Sorted(button.Ascending, sortPredicate);
            foreach (Planet p in planets)
            {
                if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    continue;
                var e = new PlanetListScreenEntry(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 40, this);
                PlanetSL.AddItem(e);
            }
        }

        private void HandleButton<T>(InputState input, SortButton button, Func<Planet, T> sortPredicate)
        {
            if (button.HandleInput(input))
                InitSortedItems(button, sortPredicate);
        }

        private void ResetButton<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                InitSortedItems(button, sortPredicate);
        }


        public override bool HandleInput(InputState input)
        {
            //this.LastSorted = empUI.empire.data.PLSort;
            if (PlanetSL.NumEntries == 0)
                ResetList();

            PlanetSL.HandleInput(input);
            cb_hideOwned.HandleInput(input);
            cb_hideUninhabitable.HandleInput(input);

            HandleButton(input, sb_Sys,   p => p.ParentSystem.Name);
            HandleButton(input, sb_Name,  p => p.Name);
            HandleButton(input, sb_Fert,  p => p.Fertility);
            HandleButton(input, sb_Rich,  p => p.MineralRichness);
            HandleButton(input, sb_Pop,   p => p.MaxPopulation);
            HandleButton(input, sb_Owned, p => p.GetOwnerName());

            foreach (ScrollList.Entry e in PlanetSL.VisibleEntries)
            {
                var entry = (PlanetListScreenEntry)e.item;
                entry.HandleInput(input);
                entry.SetNewPos(eRect.X + 22, e.Y);
                if (!GlobalStats.TakingInput
                    && entry.TotalEntrySize.HitTest(input.CursorPosition)
                    && input.LeftMouseClick)
                {
                    if (ClickTimer >= ClickDelay)
                    {
                        ClickTimer = 0f;
                    }
                    else 
                    {
                        ExitScreen();
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        Empire.Universe.SelectedPlanet = entry.planet;
                        Empire.Universe.ViewingShip = false;
                        Empire.Universe.returnToShip = false;
                        Empire.Universe.CamDestination = new Vector3(entry.planet.Center.X, entry.planet.Center.Y, 10000f);
                    }
                }
            }
            if (input.KeysCurr.IsKeyDown(Keys.L) && !input.KeysPrev.IsKeyDown(Keys.L) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            if (input.Escaped || input.RightMouseClick || close.HandleInput(input) )
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }


        public void ResetList()
        {
            PlanetSL.Reset();
            if (LastSorted == null)
            {
                foreach (Planet p in ExploredPlanets)
                {
                    if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    {
                        continue;
                    }
                    var entry = new PlanetListScreenEntry(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 40, this);
                    PlanetSL.AddItem(entry);
                }
            }
            else
            {
                ResetButton(sb_Sys,   p => p.ParentSystem.Name);
                ResetButton(sb_Name,  p => p.Name);
                ResetButton(sb_Fert,  p => p.Fertility);
                ResetButton(sb_Rich,  p => p.MineralRichness);
                ResetButton(sb_Pop,   p => p.MaxPopulation);
                ResetButton(sb_Owned, p => p.GetOwnerName());
            }
            if (PlanetSL.NumEntries <= 0)
            {
                SelectedPlanet = null;
                return;
            }
            SelectedPlanet = PlanetSL.ItemAtTop<PlanetListScreenEntry>().planet;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ClickTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}