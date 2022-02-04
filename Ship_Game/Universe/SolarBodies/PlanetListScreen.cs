using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class PlanetListScreen : GameScreen
    {
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu2 EMenu;

        public UniverseScreen Universe;
        public EmpireUIOverlay EmpireUI;

        public Planet SelectedPlanet { get; private set; }
        readonly ScrollList2<PlanetListScreenItem> PlanetSL;

        readonly SortButton sb_Sys;
        readonly SortButton sb_Name;
        readonly SortButton sb_Fert;
        readonly SortButton sb_Rich;
        readonly SortButton sb_Pop;
        readonly SortButton sb_Owned;
        readonly SortButton sb_Distance;

        private UICheckBox cb_hideOwned;
        private UICheckBox cb_hideUninhabitable;

        bool HideOwned
        {
            get => GlobalStats.PlanetScreenHideOwned;
            set => GlobalStats.PlanetScreenHideOwned = value;
        }

        bool HideUninhab
        {
            get => GlobalStats.PlanetsScreenHideUnhabitable;
            set => GlobalStats.PlanetsScreenHideUnhabitable = value;
        }

        private int NumAvailableTroops;
        readonly Array<Planet> ExploredPlanets = new Array<Planet>();
        readonly UILabel AvailableTroops;
        Rectangle eRect;
        SortButton LastSorted;

        // FB - this will store each planet GUID and it's distance to the closest player colony. If the planet is owned
        // by the player - the distance will be 0, logically.
        readonly Map<Planet, float> PlanetDistanceToClosestColony = new Map<Planet, float>();

        public PlanetListScreen(UniverseScreen parent, EmpireUIOverlay empireUi, string audioCue = "")
            : base(parent, toPause: parent)
        {
            if(!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);

            Universe = parent;
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
            TitlePos = new Vector2((titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.PlanetArray)).X / 2f, (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            Rectangle leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);
            EMenu    = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            eRect = new Rectangle(leftRect.X + 20, titleRect.Bottom + 30,
                                  ScreenWidth - 40,
                                  leftRect.Bottom - (titleRect.Bottom + 30) - 15);

            PlanetSL = Add(new ScrollList2<PlanetListScreenItem>(eRect));
            PlanetSL.EnableItemHighlight = true;

            sb_Sys      = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.System));
            sb_Name     = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Planet));
            sb_Fert     = new SortButton(empireUi.Player.data.PLSort,Localizer.Token(GameText.Fertility) );
            sb_Rich     = new SortButton(empireUi.Player.data.PLSort,Localizer.Token(GameText.Richness));
            sb_Pop      = new SortButton(empireUi.Player.data.PLSort,Localizer.Token(GameText.MaxPopulation));
            sb_Owned    = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Owner));
            sb_Distance = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Proximity));

            foreach (SolarSystem system in Universe.UState.Systems.OrderBy(s => s.Position.Distance(Universe.Player.WeightedCenter)))
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(EmpireManager.Player))
                    {
                        p.UpdateMaxPopulation();
                        ExploredPlanets.Add(p);
                    }
                }
            }

            CalcPlanetsDistances();
            cb_hideOwned = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 15,
                () => HideOwned, 
                x => { HideOwned = x; ResetList(); }, Fonts.Arial12Bold, "Hide Owned", ""));

            cb_hideUninhabitable = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 35,
                () => HideUninhab, 
                x => { HideUninhab = x; ResetList(); }, Fonts.Arial12Bold, "Hide Uninhabitable", ""));

            Vector2 troopPos = new Vector2(TitleBar.Menu.X + TitleBar.Menu.Width + 17, TitleBar.Menu.Y + 55);
            AvailableTroops  = Add(new UILabel(troopPos, $"Available Troops: ", Fonts.Arial20Bold, Color.LightGreen));
        }

        void CalcPlanetsDistances()
        {
            var playerPlanets = EmpireManager.Player.GetPlanets();
            foreach (Planet planet in ExploredPlanets)
            {
                if (planet.Owner != EmpireManager.Player)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Center.Distance(planet.Center));
                    PlanetDistanceToClosestColony.Add(planet, shortestDistance);
                }
                else
                {
                    PlanetDistanceToClosestColony.Add(planet, 0f);
                }
            }
        }

        float GetShortestDistance(Planet p)
        {
            return PlanetDistanceToClosestColony.TryGetValue(p, out float distance) ?  distance : 0;
        }

        Vector2 GetCenteredTextOffset(Rectangle rect, GameText text)
        {
            return new Vector2(rect.X + rect.Width / 2 - Fonts.Arial20Bold.MeasureString(Localizer.Token(text)).X / 2f, 
                               eRect.Y - Fonts.Arial20Bold.LineSpacing + 16);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.PlanetArray), TitlePos, Colors.Cream);
            EMenu.Draw(batch, elapsed);
            AvailableTroops.Text = $"Available Troops: {NumAvailableTroops}";
            AvailableTroops.Color = NumAvailableTroops == 0 ? Color.Gray : Color.LightGreen;
            base.Draw(batch, elapsed);

            if (PlanetSL.NumEntries > 0)
            {
                PlanetListScreenItem e1 = PlanetSL.ItemAtTop;
                Graphics.Font fontStyle    = Fonts.Arial20Bold;

                var textCursor = GetCenteredTextOffset(e1.SysNameRect, GameText.System);
                sb_Sys.Update(textCursor);
                sb_Sys.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.PlanetNameRect, GameText.Planet);
                sb_Name.Update(textCursor);
                sb_Name.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.DistanceRect, GameText.Proximity);
                sb_Distance.Update(textCursor);
                sb_Distance.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.FertRect, GameText.Fertility);
                sb_Fert.Update(textCursor);
                sb_Fert.Draw(ScreenManager, fontStyle);

                textCursor = GetCenteredTextOffset(e1.RichRect, GameText.Richness);
                sb_Rich.Update(textCursor);
                sb_Rich.Draw(ScreenManager, fontStyle);

                textCursor = GetCenteredTextOffset(e1.PopRect, GameText.MaxPopulation);
                sb_Pop.Update(textCursor);
                sb_Pop.Draw(ScreenManager, fontStyle);

                textCursor = GetCenteredTextOffset(e1.OwnerRect, GameText.Owner);
                sb_Owned.Update(textCursor);
                sb_Owned.Draw(ScreenManager, fontStyle);
         
                Color lineColor   = new Color(118, 102, 67, 255);
                int columnTop     = eRect.Y + 35;
                int columnBot     = eRect.Y + eRect.Height - 20;
                Vector2 topLeftSL = new Vector2(e1.PlanetNameRect.X, columnTop);
                Vector2 botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.DistanceRect.X), columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.FertRect.X, columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.RichRect.X + 5), columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.PopRect.X, columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.PopRect.X + e1.PopRect.Width), columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.OwnerRect.X + e1.OwnerRect.Width), columnTop);
                botSL     = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);

                batch.DrawRectangle(PlanetSL.ItemsHousing, lineColor); // items housing border
            }
            batch.End();
        }

        void InitSortedItems(SortButton button)
        {
            LastSorted = button;
            GameAudio.BlipClick();
            button.Ascending = !button.Ascending;
            PlanetSL.Reset();
        }

        void Sort<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            InitSortedItems(button);
            Planet[] planets = ExploredPlanets.Sorted(button.Ascending, sortPredicate);
            foreach (Planet p in planets)
            {
                if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    continue;

                var e = new PlanetListScreenItem(this, p, GetShortestDistance(p), NumAvailableTroops > 0);
                PlanetSL.AddItem(e);
            }
        }

        void Sort(SortButton button, Map<Planet, float> list)
        {
            InitSortedItems(button);
            var sortedList = button.Ascending ? list.OrderBy(d => d.Value) 
                                              : list.OrderByDescending(d => d.Value);

            foreach (KeyValuePair<Planet, float> kv in sortedList)
            {
                Planet p       = kv.Key;
                float distance = kv.Value;

                if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                    continue;

                var e = new PlanetListScreenItem(this, p, distance, NumAvailableTroops > 0);
                PlanetSL.AddItem(e);
            }
        }

        void HandleButton<T>(InputState input, SortButton button, Func<Planet, T> sortPredicate)
        {
            if (button.HandleInput(input))
                Sort(button, sortPredicate);
        }

        void HandleButton(InputState input, SortButton button, Map<Planet, float> list)
        {
            if (button.HandleInput(input))
                Sort(button, list);
        }

        void ResetButton<T>(SortButton button, Func<Planet, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, sortPredicate);
        }

        void ResetButton(SortButton button, Map<Planet, float> list)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, list);
        }

        public override bool HandleInput(InputState input)
        {
            if (PlanetSL.NumEntries == 0)
                ResetList();

            HandleButton(input, sb_Sys,   p => p.ParentSystem.Name);
            HandleButton(input, sb_Name,  p => p.Name);
            HandleButton(input, sb_Fert,  p => p.FertilityFor(EmpireManager.Player));
            HandleButton(input, sb_Rich,  p => p.MineralRichness);
            HandleButton(input, sb_Pop,   p => p.MaxPopulationFor(EmpireManager.Player));
            HandleButton(input, sb_Owned, p => p.GetOwnerName());
            HandleButton(input, sb_Distance, PlanetDistanceToClosestColony);

            if (input.KeyPressed(Keys.L) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        void OnPlanetListItemClicked(PlanetListScreenItem item)
        {
            ExitScreen();
            GameAudio.AcceptClick();
            Universe.SelectedPlanet = item.Planet;
            Universe.ViewingShip = false;
            Universe.returnToShip = false;
            Universe.CamDestination = new Vector3d(item.Planet.Center, 10000);
        }

        public void ResetList()
        {
            PlanetSL.Reset();
            PlanetSL.OnClick = OnPlanetListItemClicked;
            NumAvailableTroops  = EmpireManager.Player.NumFreeTroops();

            if (LastSorted == null)
            {
                foreach (Planet p in ExploredPlanets)
                {
                    if (HideOwned && p.Owner != null || HideUninhab && !p.Habitable)
                        continue;

                    var entry = new PlanetListScreenItem(this, p, GetShortestDistance(p), NumAvailableTroops > 0);
                    PlanetSL.AddItem(entry);
                }
            }
            else
            {
                ResetButton(sb_Sys,   p => p.ParentSystem.Name);
                ResetButton(sb_Name,  p => p.Name);
                ResetButton(sb_Fert,  p => p.FertilityFor(EmpireManager.Player));
                ResetButton(sb_Rich,  p => p.MineralRichness);
                ResetButton(sb_Pop,   p => p.MaxPopulationFor(EmpireManager.Player));
                ResetButton(sb_Owned, p => p.GetOwnerName());
                ResetButton(sb_Distance, PlanetDistanceToClosestColony);
            }

            SelectedPlanet = PlanetSL.NumEntries > 0 ? PlanetSL.AllEntries[0].Planet : null;
        }

        public void RefreshSendTroopButtonsVisibility()
        {
            NumAvailableTroops = EmpireManager.Player.NumFreeTroops();
            foreach (PlanetListScreenItem item in PlanetSL.AllEntries)
            {
                item.SetCanSendTroops(NumAvailableTroops > 0);
            }
        }
    }
}
