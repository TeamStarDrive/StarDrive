using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class PlanetListScreen : GameScreen
    {
        Menu2 TitleBar;
        Vector2 TitlePos;

        Menu2 EMenu;

        public Planet SelectedPlanet { get; private set; }
        ScrollList<PlanetListScreenItem> PlanetSL;
        public EmpireUIOverlay EmpireUI;
        Submenu ShipSubMenu;
        Rectangle leftRect;

        SortButton sb_Sys;
        SortButton sb_Name;
        SortButton sb_Fert;
        SortButton sb_Rich;
        SortButton sb_Pop;
        SortButton sb_Owned;

        UICheckBox cb_hideOwned;
        UICheckBox cb_hideUninhabitable;

        bool HideOwned;
        bool HideUninhab = true;

        readonly Array<Planet> ExploredPlanets = new Array<Planet>();

        Rectangle eRect;
        SortButton LastSorted;


        public PlanetListScreen(GameScreen parent, EmpireUIOverlay empireUi, string audioCue = "")
            : base(parent)
        {
            if(!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            EmpireUI = empireUi;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            if (ScreenWidth <= 1280)
            {
                //LowRes = true;
            }
            Rectangle titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2((titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(1402)).X / 2f, (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            EMenu = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenWidth - 40, ScreenHeight - (titleRect.Y + titleRect.Height) - 15);
            sb_Sys = new SortButton(empireUi.empire.data.PLSort, Localizer.Token(192));
            sb_Name = new SortButton(empireUi.empire.data.PLSort, Localizer.Token(389));
            sb_Fert = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(386) );
            sb_Rich = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(387));
            sb_Pop = new SortButton(empireUi.empire.data.PLSort,Localizer.Token(1403));
            sb_Owned = new SortButton(empireUi.empire.data.PLSort, "Owner");
            
            while (eRect.Height % 40 != 0)
                eRect.Height -= 1;
            eRect.Height -= 20;

            ShipSubMenu = new Submenu(eRect);
            PlanetSL = new ScrollList<PlanetListScreenItem>(ShipSubMenu, 40);

            foreach (SolarSystem system in UniverseScreen.SolarSystemList.OrderBy(distance => distance.Position.Distance(EmpireManager.Player.GetWeightedCenter())))
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(EmpireManager.Player))
                        ExploredPlanets.Add(p);
                }
            }

            cb_hideOwned = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 15,
                () => HideOwned, 
                x => { HideOwned = x; ResetList(); }, Fonts.Arial12Bold, "Hide Owned", 0));

            cb_hideUninhabitable = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 35,
                () => HideUninhab, 
                x => { HideUninhab = x; ResetList(); }, Fonts.Arial12Bold, "Hide Uninhabitable", 0));
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw(batch);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(1402), TitlePos, Colors.Cream);
            EMenu.Draw(batch);

            PlanetSL.Draw(batch);

            if (PlanetSL.NumEntries > 0)
            {
                PlanetListScreenItem e1 = PlanetSL.FirstItem;
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

            base.Draw(batch);

            batch.End();
        }

        void InitSortedItems<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            LastSorted = button;
            GameAudio.BlipClick();
            button.Ascending = !button.Ascending;
            PlanetSL.Reset();

            Planet[] planets = ExploredPlanets.Sorted(button.Ascending, sortPredicate);
            foreach (Planet p in planets)
            {
                if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    continue;
                var e = new PlanetListScreenItem(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 40, this);
                PlanetSL.AddItem(e);
            }
        }

        void HandleButton<T>(InputState input, SortButton button, Func<Planet, T> sortPredicate)
        {
            if (button.HandleInput(input))
                InitSortedItems(button, sortPredicate);
        }

        void ResetButton<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                InitSortedItems(button, sortPredicate);
        }


        public override bool HandleInput(InputState input)
        {
            if (PlanetSL.NumEntries == 0)
                ResetList();

            if (PlanetSL.HandleInput(input))
                return true;

            cb_hideOwned.HandleInput(input);
            cb_hideUninhabitable.HandleInput(input);

            HandleButton(input, sb_Sys,   p => p.ParentSystem.Name);
            HandleButton(input, sb_Name,  p => p.Name);
            HandleButton(input, sb_Fert,  p => p.FertilityFor(EmpireManager.Player));
            HandleButton(input, sb_Rich,  p => p.MineralRichness);
            HandleButton(input, sb_Pop,   p => p.MaxPopulation);
            HandleButton(input, sb_Owned, p => p.GetOwnerName());

            if (input.KeyPressed(Keys.L) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        void OnPlanetListItemClicked(PlanetListScreenItem item)
        {
            ExitScreen();
            GameAudio.AcceptClick();
            Empire.Universe.SelectedPlanet = item.planet;
            Empire.Universe.ViewingShip = false;
            Empire.Universe.returnToShip = false;
            Empire.Universe.CamDestination = new Vector3(item.planet.Center, 10000f);
        }

        public void ResetList()
        {
            PlanetSL.Reset();
            PlanetSL.OnClick = OnPlanetListItemClicked;

            if (LastSorted == null)
            {
                foreach (Planet p in ExploredPlanets)
                {
                    if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    {
                        continue;
                    }
                    var entry = new PlanetListScreenItem(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 40, this);
                    PlanetSL.AddItem(entry);
                }
            }
            else
            {
                ResetButton(sb_Sys,   p => p.ParentSystem.Name);
                ResetButton(sb_Name,  p => p.Name);
                ResetButton(sb_Fert,  p => p.FertilityFor(EmpireManager.Player));
                ResetButton(sb_Rich,  p => p.MineralRichness);
                ResetButton(sb_Pop,   p => p.MaxPopulation);
                ResetButton(sb_Owned, p => p.GetOwnerName());
            }

            SelectedPlanet = PlanetSL.NumEntries > 0 ? PlanetSL.FirstItem.planet : null;
        }
    }
}