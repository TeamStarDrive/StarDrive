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
using Ship_Game.Fleets;

namespace Ship_Game
{
    public sealed class EmpirePatrolsScreen : GameScreen
    {
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu2 EMenu;

        public UniverseScreen Universe;
        public UniverseState UState => Universe.UState;
        public readonly Empire Player;

        public Planet SelectedPlanet { get; private set; }
        readonly ScrollList<EmpirePatrolsScreenListItem> PatrolsSL;

        readonly SortButton SbPatrolName;
        readonly SortButton SbNumWaypoints;
        readonly SortButton SbNumFleetsAssigned;
        readonly SortButton SbFleetsAssigned;


        RectF ERect;
        SortButton LastSorted;

        public EmpirePatrolsScreen(UniverseScreen parent, Empire player)
            : base(parent, toPause: parent)
        {
            Universe = parent;
            Player = player;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;

            Rectangle titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2((titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.EmpirePatrolsScreenTitle)).X / 2f, (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            Rectangle leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);
            EMenu = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            ERect = new(leftRect.X + 20, titleRect.Bottom + 50, ScreenWidth - 40,
                        leftRect.Bottom - (titleRect.Bottom + 46) - 31);
            RectF slRect = new(ERect.X, ERect.Y - 10, ERect.W, ERect.H + 10);
            PatrolsSL = Add(new ScrollList<EmpirePatrolsScreenListItem>(slRect));
            PatrolsSL.EnableItemHighlight = true;
            foreach (FleetPatrol patrol in player.FleetPatrols)
            {
                PatrolsSL.AddItem(new EmpirePatrolsScreenListItem(this, patrol, player));
            }

            SbPatrolName = new SortButton(Player.data.PLSort, Localizer.Token(GameText.PatrolPlanName));
            SbNumWaypoints = new SortButton(Player.data.PLSort, Localizer.Token(GameText.NumWayPoints));
            SbNumFleetsAssigned = new SortButton(Player.data.PLSort, Localizer.Token(GameText.PatrolNumAssignedFleets));
            SbFleetsAssigned = new SortButton(Player.data.PLSort, Localizer.Token(GameText.PatrolAssignedFleets));
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
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.EmpirePatrolsScreenTitle), TitlePos, Colors.Cream);
            EMenu.Draw(batch, elapsed);
            base.Draw(batch, elapsed);

            if (PatrolsSL.NumEntries > 0)
            {
                EmpirePatrolsScreenListItem e1 = PatrolsSL.ItemAtTop;
                Graphics.Font fontStyle = Fonts.Arial20Bold;

                var textCursor = GetCenteredTextOffset(e1.PatrolNameRect, GameText.PatrolPlanName);
                SbPatrolName.Update(textCursor);
                SbPatrolName.Draw(ScreenManager);

                textCursor = GetCenteredTextOffset(e1.NumWaypointsRect, GameText.NumWayPoints);
                SbNumWaypoints.Update(textCursor);
                SbNumWaypoints.Draw(ScreenManager, fontStyle);

                textCursor = GetCenteredTextOffset(e1.NumFleetsRect, GameText.PatrolNumAssignedFleets);
                SbNumFleetsAssigned.Update(textCursor);
                SbNumFleetsAssigned.Draw(ScreenManager, fontStyle);

                textCursor = GetCenteredTextOffset(e1.FleetsRect, GameText.PatrolAssignedFleets);
                SbFleetsAssigned.Update(textCursor);
                SbFleetsAssigned.Draw(ScreenManager, fontStyle);

                Color lineColor = new Color(118, 102, 67, 255);
                float columnTop = ERect.Y + 15;
                float columnBot = ERect.Y + ERect.H - 20;
                Vector2 topLeftSL = new(e1.NumWaypointsRect.X, columnTop);
                Vector2 botSL = new(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.NumFleetsRect.X), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2(e1.FleetsRect.X, columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((e1.FleetsRect.X + e1.FleetsRect.Width + 5), columnTop);
                botSL = new Vector2(topLeftSL.X, columnBot);
                batch.DrawLine(topLeftSL, botSL, lineColor);

                batch.DrawRectangle(PatrolsSL.ItemsHousing, lineColor); // items housing border
            }
            batch.SafeEnd();
        }

        void InitSortedItems(SortButton button)
        {
            LastSorted = button;
            GameAudio.BlipClick();
            button.Ascending = !button.Ascending;
            PatrolsSL.Reset();
        }

        void Sort<T>(SortButton button, Func<FleetPatrol, T> sortPredicate)
        {
            InitSortedItems(button);
            FleetPatrol[] patrols = Player.FleetPatrols.Sorted(button.Ascending, sortPredicate);
            foreach (FleetPatrol patrol in patrols)
            {
                PatrolsSL.AddItem(new EmpirePatrolsScreenListItem(this, patrol, Player));
            }
        }

        void HandleButton<T>(InputState input, SortButton button, Func<FleetPatrol, T> sortPredicate)
        {
            if (button.HandleInput(input))
                Sort(button, sortPredicate);
        }

        void ResetButton<T>(SortButton button, Func<FleetPatrol, T> sortPredicate)
        {
            if (LastSorted.Text == button.Text)
                Sort(button, sortPredicate);
        }

        public override bool HandleInput(InputState input)
        {
            if (PatrolsSL.NumEntries == 0)
                ResetList();

            HandleButton(input, SbPatrolName, p => p.Name);
            HandleButton(input, SbNumWaypoints, p => p.WayPoints.Count);
            HandleButton(input, SbNumFleetsAssigned, p => Player.AllFleets.Count(fleet => fleet.HasPatrolPlan && fleet.Patrol == p));

            if (input.KeyPressed(Keys.L) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        void ResetList()
        {
            PatrolsSL.Reset();

            if (LastSorted == null)
            {
                foreach (FleetPatrol patrol in Player.FleetPatrols)
                {
                    PatrolsSL.AddItem(new EmpirePatrolsScreenListItem(this, patrol, Player));
                }
            }
            else
            {
                ResetButton(SbPatrolName, p => p.Name);
                ResetButton(SbNumWaypoints, p => p.WayPoints.Count);
                ResetButton(SbNumFleetsAssigned, p => Player.AllFleets.Count(fleet => fleet.HasPatrolPlan && fleet.Patrol.Name == p.Name));
            }
        }

        public void DeletePatrol(FleetPatrol patrol)
        {
            lock (Player.FleetPatrols)
            {
                foreach (Fleet fleet in Player.AllFleets)
                {
                    if (fleet.HasPatrolPlan && fleet.Patrol.Name == patrol.Name)
                        fleet.ClearPatrol();
                }

                Player.FleetPatrols.Remove(patrol);
                GameAudio.EchoAffirmative();
                ResetList();
            }
        }

        public bool RenamePatrol(FleetPatrol patrol, string newName)
        {
            lock (Player.FleetPatrols)
            {
                patrol.ChangeName(newName);
                foreach (Fleet fleet in Player.AllFleets)
                {
                    if (fleet.HasPatrolPlan && fleet.Patrol.Name == newName)
                        fleet.Patrol.ChangeName(newName);
                }
                GameAudio.EchoAffirmative();
                ResetList();
                return true;
            }
        }
    }
}
