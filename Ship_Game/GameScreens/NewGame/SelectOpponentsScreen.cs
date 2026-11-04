using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.UI;
using Ship_Game.Universe;

namespace Ship_Game.GameScreens.NewGame
{
    public class SelectOpponentsScreen : GameScreen
    {
        public readonly UniverseParams Params;
        ScrollList<SelectOpponentListItem> ChooseRaceList;
        readonly IEmpireData PlayerData;
        UILabel RandomOpponentsCount;
        UIColorPicker Picker;
        IEmpireData PickingColorFor;
        bool PickerAwaitsRelease;

        public SelectOpponentsScreen(GameScreen parent, UniverseParams p, IEmpireData selectedData) : base(parent, toPause: null)
        {
            IsPopup = true;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            Params = p;
            PlayerData = selectedData;
        }

        public override void LoadContent()
        {
            const int boxesHeight = 70, listWidth = 580, pickerWidth = 620, pickerHeight = 560, gap = 10;
            // the color picker opens right of the list, so the list moves left where the screen is too narrow for both
            int listX = Math.Min(ScreenWidth / 2 - listWidth / 2, ScreenWidth - gap - pickerWidth - gap - listWidth);
            var titleBar = new Rectangle(listX + listWidth / 2 - 225, ScreenHeight / 5, 450, 80);
            RectF racesListRect = new(listX,
                                  titleBar.Bottom + 10,
                                  listWidth,
                                  ScreenHeight * 0.6 + boxesHeight);

            RectF background = new RectF(racesListRect.X+10, racesListRect.Y+40, racesListRect.W -20, racesListRect.H-50-boxesHeight);
            Add(new Menu2(titleBar, Color.Black));
            Add(new UILabel(new Rectangle(titleBar.X+83, titleBar.Y+35, titleBar.Width, titleBar.Height),
                "Select Opponents", Fonts.Laserian14, Color.Wheat));
            Add(new Menu1(racesListRect));
            ChooseRaceList = Add(new ScrollList<SelectOpponentListItem>(background, 135));
            Add(CloseButton(racesListRect.Right - 45, racesListRect.Y + 20));
            ChooseRaceList.OnClick = OnRaceItemSelected;
            ChooseRaceList.OnDoubleClick = OnRaceItemSelected;
            RandomOpponentsCount = Add(new UILabel(
                               new Rectangle((int)racesListRect.X + 30, (int)racesListRect.Y + 20, 200, 30),
                                              "", Fonts.Arial20Bold, Color.White));
            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != PlayerData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new SelectOpponentListItem(this, e));

            UIList boxes = AddList(new Vector2(racesListRect.X + 30, background.Bottom + 15));
            boxes.Padding = new Vector2(2f, 10f);
            boxes.AddCheckbox(() => Params.PlayerHullColors, title: GameText.YourHullColor, tooltip: GameText.YourHullColorTip);
            boxes.AddCheckbox(() => Params.OpponentHullColors, title: GameText.OpponentsHullColor, tooltip: GameText.OpponentsHullColorTip);

            Picker = Add(new UIColorPicker(new Rectangle((int)racesListRect.Right + gap, (int)racesListRect.Y, pickerWidth, pickerHeight)));
            Picker.Visible = false;

            base.LoadContent();
        }

        public override bool HandleInput(InputState input)
        {
            if (Picker.Visible)
            {
                // the click that opened the picker picks no color
                if (PickerAwaitsRelease)
                {
                    PickerAwaitsRelease = input.LeftMouseDown;
                    return true;
                }
                return Picker.HandleInput(input);
            }

            if (input.RightMouseClick && ChooseRaceList.HitTest(input.CursorPosition))
                return true;

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }

        public override void Update(float fixedDeltaTime)
        {
            RandomOpponentsCount.Text = $"Random Opponents: {Params.NumOpponents - Params.SelectedOpponents.Count}";
            RandomOpponentsCount.Color = Params.SelectedOpponents.Count == Params.NumOpponents ? Color.Gray : Color.Green;
            if (Picker.Visible && PickingColorFor != null && Picker.CurrentColor != LookOf(PickingColorFor).Color)
                Params.OpponentLooks[PickingColorFor] = (Picker.CurrentColor, LookOf(PickingColorFor).Flag);
            base.Update(fixedDeltaTime);
        }

        // the color and flag a chosen opponent plays with: the ones picked for it, else its race's own
        public (Color Color, int Flag) LookOf(IEmpireData race)
            => Params.OpponentLooks.TryGetValue(race, out (Color Color, int Flag) look) ? look : (race.Traits.Color, race.Traits.FlagIndex);

        public void PickColor(IEmpireData race)
        {
            PickingColorFor = race;
            Picker.CurrentColor = LookOf(race).Color;
            Picker.Visible = true;
            PickerAwaitsRelease = true;
        }

        public void StepFlag(IEmpireData race, int step)
        {
            (Color color, int flag) = LookOf(race);
            int numFlags = ResourceManager.NumFlags;
            Params.OpponentLooks[race] = (color, ((flag + step) % numFlags + numFlags) % numFlags);
            GameAudio.BlipClick();
        }

        private void OnRaceItemSelected(SelectOpponentListItem item)
        {
            if (Params.DropOpponent(item.EmpireData))
                return;


            if (Params.SelectedOpponents.Count >= Params.NumOpponents)
                GameAudio.NegativeClick();
            else
                Params.SelectedOpponents.Add(item.EmpireData);
        }
    }
}