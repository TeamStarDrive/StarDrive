using Microsoft.Xna.Framework.Graphics;
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
            var titleBar = new Rectangle(ScreenWidth / 2 - 225, ScreenHeight / 5, 450, 80);
            RectF racesListRect = new(titleBar.X,
                                  titleBar.Bottom + 10,
                                  450,
                                  ScreenHeight * 0.6);

            RectF background = new RectF(racesListRect.X+10, racesListRect.Y+40, racesListRect.W -20, racesListRect.H-50);
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

            base.LoadContent();
        }

        public override bool HandleInput(InputState input)
        {
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
            base.Update(fixedDeltaTime);
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