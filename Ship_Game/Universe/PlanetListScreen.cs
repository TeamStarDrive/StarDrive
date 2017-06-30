using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class PlanetListScreen : GameScreen
    {
        //private bool LowRes;

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

        private Array<Planet> planets = new Array<Planet>();

        private Rectangle eRect;

        private SortButton LastSorted;

        private Rectangle AutoButton;

        //private bool AutoButtonHover;

        public PlanetListScreen(GameScreen parent, EmpireUIOverlay empireUi, string audioCue = "")
            : base(parent)
        {
            if(!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            this.EmpireUI = empireUi;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            IsPopup = true;
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                //LowRes = true;
            }
            Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(1402)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
            EMenu = new Menu2(leftRect);
            close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
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

            foreach (SolarSystem system in UniverseScreen.SolarSystemList.OrderBy(distance => Vector2.Distance(distance.Position, EmpireManager.Player.GetWeightedCenter())))
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (!p.ExploredDict[EmpireManager.Player])
                    {
                        continue;
                    }
                    planets.Add(p);
                }
            }

            cb_hideOwned = new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 15,
                () => HideOwned, 
                x => { HideOwned = x; ResetList(); }, Fonts.Arial12Bold, "Hide Owned", 0);

            cb_hideUninhabitable = new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 35,
                () => HideUninhab, 
                x => { HideUninhab = x; ResetList(); }, Fonts.Arial12Bold, "Hide Uninhabitable", 0);

            AutoButton = new Rectangle(0, 0, 243, 33);
            
        }

        protected override void Destroy()
        {
            PlanetSL?.Dispose(ref PlanetSL);
            base.Destroy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.TitleBar.Draw();
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(1402), this.TitlePos, new Color(255, 239, 208));
            this.EMenu.Draw();
            Color TextColor = new Color(118, 102, 67, 50);
            this.PlanetSL.Draw(base.ScreenManager.SpriteBatch);
            if (this.PlanetSL.Entries.Count > 0)
            {
                PlanetListScreenEntry e1 = this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry;
                PlanetListScreenEntry entry = this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry;
                Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                
                this.sb_Sys.Update(TextCursor);
                this.sb_Sys.Draw(base.ScreenManager);
                TextCursor = new Vector2((float)(entry.PlanetNameRect.X + entry.PlanetNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(389)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                
                this.sb_Name.Update(TextCursor);
                this.sb_Name.Draw(base.ScreenManager);
                TextCursor = new Vector2((float)(entry.FertRect.X + entry.FertRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(386)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    TextCursor = TextCursor + new Vector2(10f, 10f);
                }
                
                this.sb_Fert.Update(TextCursor);
                this.sb_Fert.Draw(base.ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                TextCursor = new Vector2((float)(entry.RichRect.X + entry.RichRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(387)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    TextCursor = TextCursor + new Vector2(10f, 10f);
                }
                
                this.sb_Rich.Update(TextCursor);
                this.sb_Rich.Draw(base.ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                TextCursor = new Vector2((float)(entry.PopRect.X + entry.PopRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(1403)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    TextCursor = TextCursor + new Vector2(15f, 10f);
                }
                
                this.sb_Pop.Update(TextCursor);
                this.sb_Pop.Draw(base.ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                TextCursor = new Vector2((float)(entry.OwnerRect.X + entry.OwnerRect.Width / 2) - Fonts.Arial20Bold.MeasureString("Owner").X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                if (GlobalStats.IsGermanOrPolish)
                {
                    TextCursor = TextCursor + new Vector2(10f, 10f);
                }
                
                this.sb_Owned.Update(TextCursor);
                this.sb_Owned.Draw(base.ScreenManager, (GlobalStats.IsGermanOrPolish ? Fonts.Arial12Bold : Fonts.Arial20Bold));
                Color smallHighlight = TextColor;
                smallHighlight.A = (byte)(TextColor.A / 2);

                GameTime gameTime = Game1.Instance.GameTime;
                for (int i = this.PlanetSL.indexAtTop; i < this.PlanetSL.Entries.Count && i < this.PlanetSL.indexAtTop + this.PlanetSL.entriesToDisplay; i++)
                {
                    PlanetListScreenEntry entry2 = this.PlanetSL.Entries[i].item as PlanetListScreenEntry;
                    if (i % 2 == 0)
                    {
                        base.ScreenManager.SpriteBatch.FillRectangle(entry2.TotalEntrySize, smallHighlight);
                    }
                    if (entry2.planet == this.SelectedPlanet)
                    {
                        base.ScreenManager.SpriteBatch.FillRectangle(entry2.TotalEntrySize, TextColor);
                    }
                    entry2.SetNewPos(this.eRect.X + 22, this.PlanetSL.Entries[i].clickRect.Y);
                    entry2.Draw(base.ScreenManager, gameTime);
                    base.ScreenManager.SpriteBatch.DrawRectangle(entry2.TotalEntrySize, TextColor);
                }                
                Color lineColor = new Color(118, 102, 67, 255);
                Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(this.eRect.Y + 35));
                Vector2 botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.PlanetNameRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.FertRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.RichRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.PopRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.PopRect.X + e1.PopRect.Width), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.OwnerRect.X + e1.OwnerRect.Width), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 35));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + this.eRect.Height));
                base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
                leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + 35));
                base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
            }
            this.cb_hideUninhabitable.Draw(ScreenManager.SpriteBatch);
            this.cb_hideOwned.Draw(ScreenManager.SpriteBatch);
            this.close.Draw(base.ScreenManager);
            ToolTip.Draw(ScreenManager.SpriteBatch);
            base.ScreenManager.SpriteBatch.End();
        }


        public override bool HandleInput(InputState input)
        {
            //this.LastSorted = empUI.empire.data.PLSort;
            if (this.PlanetSL.Entries.Count == 0)
                this.ResetList();
            this.PlanetSL.HandleInput(input);
            this.cb_hideOwned.HandleInput(input);
            this.cb_hideUninhabitable.HandleInput(input);
            if (this.sb_Sys.HandleInput(input))
            {
                this.LastSorted = this.sb_Sys;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Sys.Ascending = !this.sb_Sys.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Sys.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.system.Name descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.system.Name
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            if (this.sb_Name.HandleInput(input))
            {
                this.LastSorted = this.sb_Name;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Name.Ascending = !this.sb_Name.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Name.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.Name descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.Name
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            if (this.sb_Fert.HandleInput(input))
            {
                this.LastSorted = this.sb_Fert;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Fert.Ascending = !this.sb_Fert.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Fert.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.Fertility descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.Fertility
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            if (this.sb_Rich.HandleInput(input))
            {
                this.LastSorted = this.sb_Rich;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Rich.Ascending = !this.sb_Rich.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Rich.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.MineralRichness descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.MineralRichness
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            if (this.sb_Pop.HandleInput(input))
            {
                //this.LastSorted = this.sb_Pop;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Pop.Ascending = !this.sb_Pop.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Pop.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.MaxPopulation descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.MaxPopulation
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            if (this.sb_Owned.HandleInput(input))
            {
                this.LastSorted = this.sb_Owned;
                GameAudio.PlaySfxAsync("blip_click");
                this.sb_Owned.Ascending = !this.sb_Owned.Ascending;
                this.PlanetSL.Entries.Clear();
                this.PlanetSL.Copied.Clear();
                if (!this.sb_Owned.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.GetOwnerName() descending
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from planet in this.planets
                        orderby planet.GetOwnerName()
                        select planet;
                    foreach (Planet p in sortedList)
                    {
                        if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                        {
                            continue;
                        }
                        PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                        this.PlanetSL.AddItem(entry);
                    }
                }
            }
            
            for (int i = this.PlanetSL.indexAtTop; i < this.PlanetSL.Entries.Count && i < this.PlanetSL.indexAtTop + this.PlanetSL.entriesToDisplay; i++)
            {
                PlanetListScreenEntry entry = this.PlanetSL.Entries[i].item as PlanetListScreenEntry;
                entry.HandleInput(input);
                entry.SetNewPos(this.eRect.X + 22, this.PlanetSL.Entries[i].clickRect.Y);
                if (!GlobalStats.TakingInput
                    && entry.TotalEntrySize.HitTest(input.CursorPosition) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                {
                    if (this.ClickTimer >= this.ClickDelay)
                    {
                        this.ClickTimer = 0f;
                    }
                    else 
                    {
                        this.ExitScreen();
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
            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input) )
            {
                ExitScreen();
                return true;
            }
            
            return base.HandleInput(input);
        }

        public void ResetList()
        {
            
            Array<Planet> pList = new Array<Planet>();
            foreach (ScrollList.Entry entry in this.PlanetSL.Entries)
            {
                pList.Add((entry.item as PlanetListScreenEntry).planet);
            }
            this.PlanetSL.Entries.Clear();
            this.PlanetSL.Copied.Clear();
            this.PlanetSL.indexAtTop = 0;
            if (this.LastSorted == null)
            {
                foreach (Planet p in this.planets)
                {
                    if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                    {
                        continue;
                    }
                    PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                    this.PlanetSL.AddItem(entry);
                }
            }
            else
            {
                if (this.LastSorted.Text == this.sb_Sys.Text)   // (this.sb_Sys == this.LastSorted)
                {
                    if (!this.sb_Sys.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.system.Name descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.system.Name
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
                if (this.sb_Name.Text == this.LastSorted.Text)
                {
                    if (!this.sb_Name.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.Name descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.Name
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
                if (this.sb_Fert.Text == this.LastSorted.Text)
                {
                    GameAudio.PlaySfxAsync("blip_click");
                    
                    this.PlanetSL.Entries.Clear();
                    this.PlanetSL.Copied.Clear();
                    if (!this.sb_Fert.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.Fertility descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.Fertility
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
                if (this.LastSorted.Text == this.sb_Rich.Text   )//1this.sb_Rich == this.LastSorted)
                {
                    this.LastSorted = this.sb_Rich;
                    GameAudio.PlaySfxAsync("blip_click");
                    //this.sb_Rich.Ascending = !this.sb_Rich.Ascending;
                    this.PlanetSL.Entries.Clear();
                    this.PlanetSL.Copied.Clear();
                    if (!this.sb_Rich.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.MineralRichness descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.MineralRichness
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
                if (this.sb_Pop.Text == this.LastSorted.Text)
                {
                    this.LastSorted = this.sb_Pop;
                    GameAudio.PlaySfxAsync("blip_click");
                    //this.sb_Pop.Ascending = !this.sb_Pop.Ascending;
                    this.PlanetSL.Entries.Clear();
                    this.PlanetSL.Copied.Clear();
                    if (!this.sb_Pop.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.MaxPopulation descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.MaxPopulation
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
                if (this.sb_Owned.Text == this.LastSorted.Text)
                {
                    this.LastSorted = this.sb_Owned;
                    GameAudio.PlaySfxAsync("blip_click");
                    //this.sb_Owned.Ascending = !this.sb_Owned.Ascending;
                    this.PlanetSL.Entries.Clear();
                    this.PlanetSL.Copied.Clear();
                    if (!this.sb_Owned.Ascending)
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.GetOwnerName() descending
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<Planet> sortedList = 
                            from planet in this.planets
                            orderby planet.GetOwnerName()
                            select planet;
                        foreach (Planet p in sortedList)
                        {
                            if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
                            {
                                continue;
                            }
                            PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
                            this.PlanetSL.AddItem(entry);
                        }
                    }
                }
            }
            if (this.PlanetSL.Entries.Count <= 0)
            {
                this.SelectedPlanet = null;
                return;
            }
            this.SelectedPlanet = (this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry).planet;
            
            //this.empUI.empire.data.PLSort = this.LastSorted;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            PlanetListScreen clickTimer = this;
            clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
            
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}