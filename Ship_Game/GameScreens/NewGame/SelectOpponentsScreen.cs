using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.UI;
using Ship_Game.Universe;

namespace Ship_Game.GameScreens.NewGame
{
    public class SelectOpponnetsScreen : GameScreen
    {
        public readonly UniverseParams Params;
        ScrollList<SelectOpponentListItem> ChooseRaceList;
        readonly IEmpireData PlayerData;


        public SelectOpponnetsScreen(GameScreen parent, UniverseParams p, IEmpireData selectedData) : base(parent, toPause: null)
        {
            IsPopup = true;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            Params = p;
            PlayerData = selectedData;
        }

        public override void LoadContent()
        {
            var titleBar = new Rectangle(ScreenWidth / 2 - 203, (LowRes ? 44 : 144), 406, 80);
            RectF racesRect = new(ScreenWidth / 2 -200,
                                  titleBar.Bottom + 10,
                                  400,
                                  600);

            RectF background = new RectF(racesRect.X+10, racesRect.Y+40, racesRect.W -20, racesRect.H-50);
            Add(new Menu2(titleBar, Color.Black));
            Add(new UILabel(new Rectangle(titleBar.X+70, titleBar.Y+35, titleBar.Width, titleBar.Height),
                "Select Opponents", Fonts.Laserian14, Color.Goldenrod));
            Add(new Menu1(racesRect));
            Params.SelectedOpponents.Remove(PlayerData);
            ChooseRaceList = Add(new ScrollList<SelectOpponentListItem>(background, 135));
            Add(CloseButton(racesRect.Right - 45, racesRect.Y + 20));
            ChooseRaceList.OnClick = OnRaceItemSelected;
            ChooseRaceList.OnDoubleClick = OnRaceItemSelected;

            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != PlayerData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new SelectOpponentListItem(this, e));

            //OnExit += () =>
            //{
            //    ChooseRaceList.SlideOutToOffset(offset: new(-ChooseRaceList.Width, 0), TransitionOffTime);
            //};

            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }

        private void OnRaceItemSelected(SelectOpponentListItem item)
        {
            if (Params.SelectedOpponents.Remove(item.EmpireData))
                return;


            if (Params.SelectedOpponents.Count >= Params.NumOpponents)
                GameAudio.NegativeClick();
            else
                Params.SelectedOpponents.Add(item.EmpireData);
        }
    }
}