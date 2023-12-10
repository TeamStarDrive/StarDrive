using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Input;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;

namespace Ship_Game
{
    public sealed class PlanetListScreen : GameScreen
    {
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu2 EMenu;

        public UniverseScreen Universe;
        public UniverseState UState => Universe.UState;
        public EmpireUIOverlay EmpireUI;
        Empire Player => Universe.Player;

        public Planet SelectedPlanet { get; private set; }
        readonly ScrollList<PlanetListScreenItem> PlanetSL;

        readonly SortButton sb_Sys;
        readonly SortButton sb_Name;
        readonly SortButton sb_Fert;
        readonly SortButton sb_Rich;
        readonly SortButton sb_Pop;
        readonly SortButton sb_Owned;
        readonly SortButton sb_Distance;

        private UICheckBox cb_hideOwned;
        private UICheckBox cb_hideUninhabitable;

        UIButton ExoticSystemsButton;
        bool HideOwned
        {
            get => UState.P.PlanetScreenHideOwned;
            set => UState.P.PlanetScreenHideOwned = value;
        }

        bool HideUninhab
        {
            get => UState.P.PlanetsScreenHideInhospitable;
            set => UState.P.PlanetsScreenHideInhospitable = value;
        }

        private int NumAvailableTroops;
        readonly Array<Planet> ExploredPlanets = new Array<Planet>();
        readonly UILabel AvailableTroops;
        RectF ERect;
        SortButton LastSorted;

        // FB - this will store each planet and it's distance to the closest player colony. If the planet is owned
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
            ERect = new(leftRect.X + 20, titleRect.Bottom + 50, ScreenWidth - 40,
                        leftRect.Bottom - (titleRect.Bottom + 46) - 31);
            RectF slRect = new(ERect.X, ERect.Y-10, ERect.W, ERect.H+10);
            PlanetSL = Add(new ScrollList<PlanetListScreenItem>(slRect));
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
                    if (p.IsExploredBy(Player))
                    {
                        p.UpdateMaxPopulation();
                        ExploredPlanets.Add(p);
                    }
                }
            }

            CalcPlanetsDistances();
            cb_hideOwned = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 5,
                () => HideOwned, 
                x => { HideOwned = x; ResetList(); }, Fonts.Arial12Bold, "Hide Owned", ""));

            cb_hideUninhabitable = Add(new UICheckBox(TitleBar.Menu.X + TitleBar.Menu.Width + 15, TitleBar.Menu.Y + 25,
                () => HideUninhab, 
                x => { HideUninhab = x; ResetList(); }, Fonts.Arial12Bold, "Hide Uninhabitable", ""));

            Vector2 exoticPos = new Vector2(TitleBar.Menu.X + TitleBar.Menu.Width - 200, TitleBar.Menu.Y + 30);
            if (!Player.Universe.ExoticFeaturesDisabled)
            {
                ExoticSystemsButton = Add(new UIButton(ButtonStyle.Military, exoticPos, GameText.ExoticSystemsArray));
                ExoticSystemsButton.OnClick = (b) => OnExoticSystemsScreenClick();
                ExoticSystemsButton.Tooltip = Localizer.Token(GameText.ExoticSystemsArrayTip);
            }

            Vector2 troopPos = new Vector2(TitleBar.Menu.X + TitleBar.Menu.Width + 17, TitleBar.Menu.Y + 65);
            AvailableTroops  = Add(new UILabel(troopPos, $"Available Troops: ", Fonts.Arial20Bold, Color.LightGreen));
        }

        void CalcPlanetsDistances()
        {
            var playerPlanets = Player.GetPlanets();
            foreach (Planet planet in ExploredPlanets)
            {
                if (planet.Owner != Player)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Position.Distance(planet.Position));
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
                               ERect.Y - Fonts.Arial20Bold.LineSpacing);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
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
         
                Color lineColor = new Color(118, 102, 67, 255);
                float columnTop = ERect.Y + 15;
                float columnBot = ERect.Y + ERect.H -20;
                Vector2 topLeftSL = new(e1.PlanetNameRect.X, columnTop);
                Vector2 botSL = new(topLeftSL.X, columnBot);
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
            batch.SafeEnd();
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
                if (ShouldAddItem(p))
                {
                    var e = new PlanetListScreenItem(this, p, GetShortestDistance(p), NumAvailableTroops > 0);
                    PlanetSL.AddItem(e);
                }
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

                if (ShouldAddItem(p))
                {
                    var e = new PlanetListScreenItem(this, p, distance, NumAvailableTroops > 0);
                    PlanetSL.AddItem(e);
                }
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

            HandleButton(input, sb_Sys,   p => p.System.Name);
            HandleButton(input, sb_Name,  p => p.Name);
            HandleButton(input, sb_Fert,  p => p.FertilityFor(Player));
            HandleButton(input, sb_Rich,  p => p.MineralRichness);
            HandleButton(input, sb_Pop,   p => p.MaxPopulationFor(Player));
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
            Universe.SetSelectedPlanet(item.Planet);
            Universe.CamDestination = new Vector3d(item.Planet.Position, 10000);
        }

        public void ResetList()
        {
            PlanetSL.Reset();
            PlanetSL.OnClick = OnPlanetListItemClicked;
            NumAvailableTroops  = Player.NumFreeTroops();

            if (LastSorted == null)
            {
                foreach (Planet p in ExploredPlanets)
                {
                    if (ShouldAddItem(p))
                    {
                        var entry = new PlanetListScreenItem(this, p, GetShortestDistance(p), NumAvailableTroops > 0);
                        PlanetSL.AddItem(entry);
                    }
                }
            }
            else
            {
                ResetButton(sb_Sys,   p => p.System.Name);
                ResetButton(sb_Name,  p => p.Name);
                ResetButton(sb_Fert,  p => p.FertilityFor(Player));
                ResetButton(sb_Rich,  p => p.MineralRichness);
                ResetButton(sb_Pop,   p => p.MaxPopulationFor(Player));
                ResetButton(sb_Owned, p => p.GetOwnerName());
                ResetButton(sb_Distance, PlanetDistanceToClosestColony);
            }

            SelectedPlanet = PlanetSL.NumEntries > 0 ? PlanetSL.AllEntries[0].Planet : null;
        }

        public void RefreshSendTroopButtonsVisibility()
        {
            NumAvailableTroops = Player.NumFreeTroops();
            foreach (PlanetListScreenItem item in PlanetSL.AllEntries)
            {
                item.SetCanSendTroops(NumAvailableTroops > 0);
            }
        }

        public bool ShouldAddItem(Planet p)
        {
            if (!HideOwned && !HideUninhab)                                 return true;
            if (HideOwned && HideUninhab && p.Habitable && p.Owner == null) return true;
            if (HideOwned && !HideUninhab && p.Owner == null)               return true;
            if (!HideOwned && HideUninhab && p.Habitable)                   return true;
            return false;
        }

        void OnExoticSystemsScreenClick()
        {
            ExitScreen();
            GameAudio.AcceptClick();
            Universe.ScreenManager.AddScreen(new ExoticSystemsListScreen(Universe, Universe.EmpireUI));
        }
    }
}
