using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Universe;

namespace Ship_Game.GameScreens.NewGame
{
    public class FoeSelectionScreen : GameScreen
    {
        public readonly UniverseParams P;
        private ScrollList<RaceArchetypeListItem> ChooseRaceList;
        private readonly IEmpireData SelectedData;

        public FoeSelectionScreen(GameScreen parent, UniverseParams p, IEmpireData selectedData) : base(parent, toPause: null)
        {
            IsPopup = true;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            P = p;
            SelectedData = selectedData;
            p.SelectedFoes = p.SelectedFoes == null ? new Array<IEmpireData>(P.NumOpponents) : p.SelectedFoes;
        }

        public override void LoadContent()
        {
            var TitleBar = new Rectangle(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80);

            RectF chooseRace = new(2 * ScreenWidth / 6,
                                  (int)TitleBar.Bottom + 15,
                                  (int)(ScreenWidth * 0.3f),
                                  (int)(ScreenHeight - TitleBar.Bottom));

            var background = new Rectangle((int)chooseRace.X - 20, TitleBar.Y, (int)(chooseRace.W) + 100, (int)chooseRace.H + 10);

            if (chooseRace.H > 780)
            {
                chooseRace.H = 780;
            }
            Add(new Menu2(background, Color.Black));
            Add(new UILabel(new Rectangle(TitleBar.X, TitleBar.Y + 60, TitleBar.Width, TitleBar.Height),
                GameText.SelectOtherEmpires, Fonts.Laserian14, Color.Goldenrod));
            ChooseRaceList = Add(new ScrollList<RaceArchetypeListItem>(chooseRace, 135));
            Add(CloseButton(background.Right - 45, background.Y + 20));
            ChooseRaceList.SetBackground(new Menu1(chooseRace));
            ChooseRaceList.OnClick = OnRaceItemSelected;
            ChooseRaceList.OnDoubleClick = OnRaceItemSelected;

            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != SelectedData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            OnExit += () =>
            {
                ChooseRaceList.SlideOutToOffset(offset: new(-ChooseRaceList.Width, 0), TransitionOffTime);
            };

            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }

        private void OnRaceItemSelected(RaceArchetypeListItem item)
        {
            if (P.SelectedFoes.Contains(item.EmpireData))
            {
                P.SelectedFoes.Remove(item.EmpireData);
                return;
            }

            if (P.SelectedFoes.Count >= P.NumOpponents)
            {
                GameAudio.NegativeClick();
                return;
            }

            P.SelectedFoes.Add(item.EmpireData);
        }
    }
}