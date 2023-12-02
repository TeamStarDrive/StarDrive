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
    public sealed class ExoticSystemsListScreen : GameScreen
    {
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu2 EMenu;

        public UniverseScreen Universe;
        public UniverseState UState => Universe.UState;
        public EmpireUIOverlay EmpireUI;
        Empire Player => Universe.Player;

        public Planet SelectedPlanet { get; private set; }
        readonly ScrollList<ExoticSystemsListScreenItem> ExoticSL;

        readonly SortButton Sb_Sys;
        readonly SortButton Sb_Name;
        readonly SortButton Sb_Distance;
        readonly SortButton Sb_Resource;
        readonly SortButton Sb_Richness;
        readonly Array<ExplorableGameObject> ExploredSolarBodies = new();
        UIButton PlanetArrayListButton;

        RectF ERect;
        SortButton LastSorted;

        // FB - this will store each planet or system and it's distance to the closest player colony. 
        readonly Map<ExplorableGameObject, float> DistancesToClosestColony = new();

        public ExoticSystemsListScreen(UniverseScreen parent, EmpireUIOverlay empireUi, string audioCue = "")
            : base(parent, toPause: parent)
        {
            if (!string.IsNullOrEmpty(audioCue))
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
            TitlePos = new Vector2((titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.ExoticSystemsArray)).X / 2f, (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            Rectangle leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);
            EMenu = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            ERect = new(leftRect.X + 20, titleRect.Bottom + 50, ScreenWidth - 40,
                        leftRect.Bottom - (titleRect.Bottom + 46) - 31);
            RectF slRect = new(ERect.X, ERect.Y - 10, ERect.W, ERect.H + 10);
            ExoticSL = Add(new ScrollList<ExoticSystemsListScreenItem>(slRect));
            ExoticSL.EnableItemHighlight = true;

            Sb_Sys = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.System));
            Sb_Name = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.StarOrPlanet));
            Sb_Distance = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Proximity));
            Sb_Resource = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.ResourceName));
            Sb_Richness = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Richness));

            foreach (SolarSystem system in Universe.UState.Systems.OrderBy(s => s.Position.Distance(Universe.Player.WeightedCenter)))
            {
                if (system.IsResearchable && system.IsExploredBy(Player))
                    ExploredSolarBodies.Add(system);

                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(Player)&& (p.IsResearchable || p.IsMineable))
                        ExploredSolarBodies.Add(p);
                }
            }

            CalcPlanetsDistances();
            Vector2 planetArrayPos = new Vector2(TitleBar.Menu.X + TitleBar.Menu.Width - 200, TitleBar.Menu.Y + 30);
            PlanetArrayListButton = Add(new UIButton(ButtonStyle.BigDip, planetArrayPos, GameText.PlanetArray));
            PlanetArrayListButton.OnClick = (b) => OnPlanetArrayListClick();
        }

        void CalcPlanetsDistances()
        {
            var playerPlanets = Player.GetPlanets();
            foreach (ExplorableGameObject solarBody in ExploredSolarBodies)
            {
                if (solarBody is Planet planet)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Position.Distance(planet.Position));
                    DistancesToClosestColony.Add(planet, shortestDistance);
                }
                else if (solarBody is SolarSystem system)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Position.Distance(system.Position));
                    DistancesToClosestColony.Add(system, shortestDistance);
                }
            }
        }

        float GetShortestDistance(ExplorableGameObject solarBody)
        {
            return DistancesToClosestColony.TryGetValue(solarBody, out float distance) ? distance : 0;
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
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.ExoticSystemsArray), TitlePos, Colors.Cream);
            EMenu.Draw(batch, elapsed);
            base.Draw(batch, elapsed);

            if (ExoticSL.NumEntries > 0)
            {
                ExoticSystemsListScreenItem e1 = ExoticSL.ItemAtTop;

                var textCursor = GetCenteredTextOffset(e1.SysNameRect, GameText.System);
                Sb_Sys.Update(textCursor);
                Sb_Sys.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.PlanetNameRect, GameText.Planet);
                Sb_Name.Update(textCursor);
                Sb_Name.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.DistanceRect, GameText.Proximity);
                Sb_Distance.Update(textCursor);
                Sb_Distance.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.ResourceRect, GameText.ResourceName);
                Sb_Resource.Update(textCursor);
                Sb_Resource.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.RichnessRect, GameText.Richness);
                Sb_Richness.Update(textCursor);
                Sb_Richness.Draw(ScreenManager);

                Color lineColor = new Color(118, 102, 67, 255);
                float columnTop = ERect.Y + 15;
                float columnBot = ERect.Y + ERect.H - 20;
                Vector2 topLeftSL = new(e1.PlanetNameRect.X, columnTop);
                Vector2 botSL = new(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.DistanceRect.X), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.ResourceRect.X), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.RichnessRect.X), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.OrdersRect.X), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);

                batch.DrawRectangle(ExoticSL.ItemsHousing, lineColor); // items housing border
            }
            batch.SafeEnd();
        }

        void InitSortedItems(SortButton button)
        {
            LastSorted = button;
            GameAudio.BlipClick();
            button.Ascending = !button.Ascending;
            ExoticSL.Reset();
        }

        void Sort<T>(SortButton button, Func<ExplorableGameObject, T> sortPredicate)
        {
            InitSortedItems(button);
            ExplorableGameObject[] solarBodies = ExploredSolarBodies.Sorted(button.Ascending, sortPredicate);
            foreach (ExplorableGameObject solarBody in solarBodies)
            {
                var e = new ExoticSystemsListScreenItem(solarBody, GetShortestDistance(solarBody));
                ExoticSL.AddItem(e);
            }
        }

        void Sort(SortButton button, Map<ExplorableGameObject, float> list)
        {
            InitSortedItems(button);
            var sortedList = button.Ascending ? list.OrderBy(d => d.Value)
                                              : list.OrderByDescending(d => d.Value);

            foreach (KeyValuePair<ExplorableGameObject, float> kv in sortedList)
            {
                ExplorableGameObject solarBody = kv.Key;
                float distance = kv.Value;
                var e = new ExoticSystemsListScreenItem(solarBody, distance);
                ExoticSL.AddItem(e);
            }
        }

        void HandleButton<T>(InputState input, SortButton button, Func<ExplorableGameObject, T> sortPredicate)
        {
            if (button.HandleInput(input))
                Sort(button, sortPredicate);
        }

        void HandleButton(InputState input, SortButton button, Map<ExplorableGameObject, float> list)
        {
            if (button.HandleInput(input))
                Sort(button, list);
        }

        void ResetButton<T>(SortButton button, Func<ExplorableGameObject, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, sortPredicate);
        }

        void ResetButton(SortButton button, Map<ExplorableGameObject, float> list)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, list);
        }

        public override bool HandleInput(InputState input)
        {
            if (ExoticSL.NumEntries == 0)
                ResetList();

            HandleButton(input, Sb_Sys, sb => sb is Planet p ? p.System.Name : sb is SolarSystem s ? s.Name : "");
            HandleButton(input, Sb_Name, sb => sb is Planet p ? p.Name : "");
            HandleButton(input, Sb_Distance, DistancesToClosestColony);
            HandleButton(input, Sb_Resource, sb => sb is Planet p ? (p?.Mining?.TranslatedResourceName.Text ?? ""): "");
            HandleButton(input, Sb_Richness, sb => sb is Planet p ? (p?.Mining?.Richness ?? (Sb_Richness.Ascending ? 1000 : 0)) : (Sb_Richness.Ascending ? 1000 : 0));

            if (input.KeyPressed(Keys.G) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        void OnExoticSystemsListItemClicked(ExoticSystemsListScreenItem item)
        {
            ExitScreen();
            GameAudio.AcceptClick();
            if (item.IsStar)
            {
                Universe.SetSelectedSystem(item.System);
                Universe.CamDestination = new Vector3d(item.System.Position, item.System.Radius);
            }
            else
            {
                Universe.SetSelectedSystem(item.System);
                Universe.CamDestination = new Vector3d(item.Planet.Position, 10000);
            }
        }

        public void ResetList()
        {
            ExoticSL.Reset();
            ExoticSL.OnClick = OnExoticSystemsListItemClicked;

            if (LastSorted == null)
            {
                foreach (ExplorableGameObject solarBody in ExploredSolarBodies)
                {
                    var entry = new ExoticSystemsListScreenItem(solarBody, GetShortestDistance(solarBody));
                    ExoticSL.AddItem(entry);
                }
            }
            else
            {
                ResetButton(Sb_Sys, sb => sb is Planet p ? p.System.Name : sb is SolarSystem s ? s.Name : "");
                ResetButton(Sb_Name, sb => sb is Planet p ? p.Name : "");
                ResetButton(Sb_Distance, DistancesToClosestColony);
                ResetButton(Sb_Resource, sb => sb is Planet p ? p?.Mining?.TranslatedResourceName ?? "Research" : "");
                ResetButton(Sb_Richness, sb => sb is Planet p ? (p?.Mining?.Richness ?? 0) : 0);
            }

            SelectedPlanet = ExoticSL.NumEntries > 0 ? ExoticSL.AllEntries[0].Planet : null;
        }

        void OnPlanetArrayListClick()
        {
            ExitScreen();
            GameAudio.AcceptClick();
            Universe.ScreenManager.AddScreen(new PlanetListScreen(Universe, Universe.EmpireUI));
        }
    }
}
