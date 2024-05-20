using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Universe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ship_Game.RaceDesignScreen;

namespace Ship_Game.GameScreens.NewGame
{
    public class FoeSelectionScreen : GameScreen
    {
        readonly MainMenuScreen MainMenu;
        readonly UniverseParams P;

        Menu2 TitleBar;
        ScrollList<RaceArchetypeListItem> ChooseRaceList;
        public Array<IEmpireData> FoeList;

        public FoeSelectionScreen(MainMenuScreen mainMenu, UniverseParams p) : base(mainMenu, toPause: null)
        {
            IsPopup = true;
            MainMenu = mainMenu;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            P = p;
            FoeList = new Array<IEmpireData>(P.NumOpponents);
        }
        public override void LoadContent()
        {
            TitleBar = Add(new Menu2(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80));
            var titlePos = new Vector2(TitleBar.CenterX - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.DesignYourRace)).X / 2f,
                                       TitleBar.CenterY - Fonts.Laserian14.LineSpacing / 4);
            Add(new UILabel(titlePos, "Select other empires", Fonts.Laserian14, Colors.Cream));

            RectF chooseRace = new(5,
                                  (int)TitleBar.Bottom + 5,
                                  (int)(ScreenWidth * 0.4f),
                                  (int)(ScreenHeight - TitleBar.Bottom - 0.18f * ScreenHeight));
            ChooseRaceList = Add(new ScrollList<RaceArchetypeListItem>(chooseRace, 135));
            ChooseRaceList.SetBackground(new Menu1(chooseRace));
            ChooseRaceList.OnClick = OnRaceItemSelected;

            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != P.PlayerData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, text: GameText.Engage, click: OnEngageClicked);

            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }
        void OnRaceItemSelected(RaceArchetypeListItem item)
        {
            if (FoeList.Contains(item.EmpireData))
            {
                FoeList.Remove(item.EmpireData);
                return;
            }

            if (FoeList.Count >= P.NumOpponents)
            {
                GameAudio.NegativeClick();
                return;
            }

            FoeList.Add(item.EmpireData);
        }

        void OnEngageClicked(UIButton b)
        {
            if (FoeList.Count <= P.NumOpponents)
            {
                var majorRacesLeft = ResourceManager.MajorRaces.Filter(
                                data => (data.ArchetypeName != P.PlayerData.ArchetypeName) && !FoeList.Select(i => i.ArchetypeName).Contains(data.ArchetypeName));

                majorRacesLeft.Shuffle();
                for (int i = 0; i <= P.NumOpponents - FoeList.Count; i++)
                {
                    FoeList.Add(majorRacesLeft[i]);
                }
            }

            P.SelectedFoes = FoeList;

            var ng = new CreatingNewGameScreen(MainMenu, P);

            ScreenManager.GoToScreen(ng, clear3DObjects: true);
        }
    }
}