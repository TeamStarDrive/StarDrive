using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
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
        public override bool HelpKeyOpensCodex => true;

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
        readonly SortButton Sb_Owner;
        // a researchable or mineable body, a star for a Dyson swarm, or a system with salvageable derelicts the player knows of
        readonly record struct ExoticEntry(ExplorableGameObject Body, bool Salvage);
        readonly Array<ExoticEntry> Rows = new();
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

            Sb_Sys      = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.System));
            Sb_Name     = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.StarOrPlanet));
            Sb_Distance = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Proximity));
            Sb_Resource = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.ResourceName));
            Sb_Richness = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Richness));
            Sb_Owner    = new SortButton(empireUi.Player.data.PLSort, Localizer.Token(GameText.Owner));

            foreach (SolarSystem system in Universe.UState.Systems.OrderBy(s => s.Position.Distance(Universe.Player.WeightedCenter)))
            {
                if (system.IsExploredBy(Player) && Player.CanBuildDysonSwarmIn(system))
                    Rows.Add(new(system, Salvage: false));
                if (system.IsResearchable && system.IsExploredBy(Player))
                    Rows.Add(new(system, Salvage: false));

                foreach (Planet p in system.PlanetList)
                {
                    if (p.IsExploredBy(Player)&& (p.IsResearchable || p.IsMineable))
                        Rows.Add(new(p, Salvage: false));
                }

                if (UState.Derelicts.AnyKnownIn(Player, system))
                    Rows.Add(new(system, Salvage: true));
            }

            CalcPlanetsDistances();
            Vector2 planetArrayPos = new Vector2(TitleBar.Menu.X + TitleBar.Menu.Width - 200, TitleBar.Menu.Y + 30);
            PlanetArrayListButton = Add(new UIButton(ButtonStyle.BigDip, planetArrayPos, GameText.PlanetArray));
            PlanetArrayListButton.OnClick = (b) => OnPlanetArrayListClick();
        }

        void CalcPlanetsDistances()
        {
            var playerPlanets = Player.GetPlanets();
            foreach (ExoticEntry row in Rows) // a system can have two rows
            {
                if (row.Body is Planet planet)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Position.Distance(planet.Position));
                    DistancesToClosestColony[planet] = shortestDistance;
                }
                else if (row.Body is SolarSystem system)
                {
                    float shortestDistance = playerPlanets.Min(p => p.Position.Distance(system.Position));
                    DistancesToClosestColony[system] = shortestDistance;
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

                textCursor = GetCenteredTextOffset(e1.OwnerRect, GameText.Owner);
                Sb_Owner.Update(textCursor);
                Sb_Owner.Draw(ScreenManager);

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
                topLeftSL = new Vector2((e1.OwnerRect.X), columnTop);
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

        ExoticSystemsListScreenItem NewItem(ExoticEntry row) => new(row.Body, GetShortestDistance(row.Body), row.Salvage);

        void Sort<T>(SortButton button, Func<ExoticEntry, T> sortPredicate)
        {
            InitSortedItems(button);
            ExoticEntry[] rows = Rows.Sorted(button.Ascending, sortPredicate);
            foreach (ExoticEntry row in rows)
                ExoticSL.AddItem(NewItem(row));
        }

        void HandleButton<T>(InputState input, SortButton button, Func<ExoticEntry, T> sortPredicate)
        {
            if (button.HandleInput(input))
                Sort(button, sortPredicate);
        }

        void ResetButton<T>(SortButton button, Func<ExoticEntry, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, sortPredicate);
        }

        static string SystemKey(ExoticEntry r) => r.Body is Planet p ? p.System.Name : r.Body is SolarSystem s ? s.Name : "";
        static string NameKey(ExoticEntry r) => r.Body is Planet p ? p.Name : "";
        float DistanceKey(ExoticEntry r) => GetShortestDistance(r.Body);
        static string ResourceKey(ExoticEntry r) => r.Salvage ? Localizer.Token(GameText.Production)
            : r.Body is Planet p ? (p.Mining?.TranslatedResourceName.Text ?? "")
            : r.Body is SolarSystem s && s.DysonSwarmType > 0 ? s.DysonSwarmType.ToString() : "";
        float RichnessKey(ExoticEntry r) => r.Body is Planet { Mining: not null } p ? p.Mining.Richness : (Sb_Richness.Ascending ? 1000 : 0);
        static string OwnerKey(ExoticEntry r) => r.Body is Planet p && p.IsMineable && p.Mining.HasOpsOwner ? p.Mining.Owner.data.Traits.Singular : "";

        public override bool HandleInput(InputState input)
        {
            if (ExoticSL.NumEntries == 0)
                ResetList();

            HandleButton(input, Sb_Sys, SystemKey);
            HandleButton(input, Sb_Name, NameKey);
            HandleButton(input, Sb_Distance, DistanceKey);
            HandleButton(input, Sb_Resource, ResourceKey);
            HandleButton(input, Sb_Richness, RichnessKey);
            HandleButton(input, Sb_Owner, OwnerKey);

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
                foreach (ExoticEntry row in Rows)
                    ExoticSL.AddItem(NewItem(row));
            }
            else
            {
                ResetButton(Sb_Sys, SystemKey);
                ResetButton(Sb_Name, NameKey);
                ResetButton(Sb_Distance, DistanceKey);
                ResetButton(Sb_Resource, ResourceKey);
                ResetButton(Sb_Richness, RichnessKey);
                ResetButton(Sb_Owner, OwnerKey);
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
