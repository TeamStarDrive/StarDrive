using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.UI;
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

        public readonly UniverseParams P;
        ScrollList<RaceArchetypeListItem> ChooseRaceList;
        readonly IEmpireData SelectedData;
        UIList SelectedRacesList;

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
            //TitleBar = Add(new Menu2(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80));
            //var titlePos = new Vector2(TitleBar.CenterX - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.DesignYourRace)).X / 2f,
            //                           TitleBar.CenterY - Fonts.Laserian14.LineSpacing / 4);
            //Add(new UILabel(titlePos, "Select other empires", Fonts.Laserian14, Colors.Cream));
            var TitleBar = new Rectangle(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80);
            RectF chooseRace = new(ScreenWidth / 6,
                                  (int)TitleBar.Bottom + 15,
                                  (int)(ScreenWidth * 0.3f),
                                  (int)(ScreenHeight - TitleBar.Bottom));

            RectF selectedRaces = new(3 * ScreenWidth / 6,
                                  (int)TitleBar.Bottom + 15,
                                  (int)(ScreenWidth * 0.3f),
                                  (int)(ScreenHeight - TitleBar.Bottom));

            if (chooseRace.H > 780 || selectedRaces.H > 780)
            {
                chooseRace.H = 780;
                selectedRaces.H = 780;
            }

            ChooseRaceList = Add(new ScrollList<RaceArchetypeListItem>(chooseRace, 135));
            ChooseRaceList.SetBackground(new Menu1(chooseRace));
            ChooseRaceList.OnClick = OnRaceItemSelected;

            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != SelectedData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            SelectedRacesList = Add(new UIList(selectedRaces, Color.TransparentBlack));
            SelectedRacesList.Visible = true;
            DrawSelectedFoesList();

            OnExit += () =>
            {
                ChooseRaceList.SlideOutToOffset(offset: new(-ChooseRaceList.Width, 0), TransitionOffTime);
                SelectedRacesList.SlideOutToOffset(offset: new(SelectedRacesList.Width, 0), TransitionOffTime);  
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
        void OnRaceItemSelected(RaceArchetypeListItem item)
        {
            if (P.SelectedFoes.Contains(item.EmpireData))
            {
                P.SelectedFoes.Remove(item.EmpireData);
                DrawSelectedFoesList();
                return;
            }

            if (P.SelectedFoes.Count >= P.NumOpponents)
            {
                GameAudio.NegativeClick();
                return;
            }

            P.SelectedFoes.Add(item.EmpireData);
            DrawSelectedFoesList();
        }

        void DrawSelectedFoesList()
        {
            SelectedRacesList.RemoveAll();
            for (int i = 0; i < P.SelectedFoes.Count; i++)
            {
                SelectedRacesList.Add(new BlueButton( P.SelectedFoes[i].ArchetypeName));
            }
        }
    }
} 