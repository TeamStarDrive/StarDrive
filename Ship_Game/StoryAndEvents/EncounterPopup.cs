using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class EncounterPopup : PopupWindow
    {
        readonly UniverseScreen Screen;
        public bool fade;
        public bool FromGame;
        public string UID;
        public Encounter encounter;
        private readonly SubTexture EmpireTex;
        private readonly Color EmpireColor;

        Rectangle ResponseRect;
        Rectangle BlackRect;
        ScrollList2<ResponseListItem> ResponseSL;

        EncounterPopup(UniverseScreen s, Empire playerEmpire, Empire targetEmp, Encounter e) : base(s, 600, 600)
        {
            Screen = s;
            encounter = e;
            encounter.CurrentMessageId = 0;
            encounter.SetPlayerEmpire(playerEmpire);
            encounter.SetSys(null);
            encounter.SetTarEmp(targetEmp);
            fade = true;
            IsPopup = true;
            FromGame = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0f;
            if (targetEmp != null)
            {
                EmpireTex = ResourceManager.Flag(targetEmp.data.Traits.FlagIndex);
                EmpireColor = targetEmp.EmpireColor;
            }
        }

        public static void Show(UniverseScreen s, Empire player, Empire them, Encounter e)
        {
            var screen = new EncounterPopup(s, player, them, e);
            ScreenManager.Instance.AddScreenDeferred(screen);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (fade)
            {
                ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            }

            base.Draw(batch, elapsed);

            batch.Begin();
            DrawEncounter(batch, elapsed);
            batch.End();
        }

        void DrawEncounter(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.FillRectangle(BlackRect, Color.Black);
            batch.FillRectangle(ResponseRect, Color.Black);
            Vector2 theirTextPos = new Vector2(BlackRect.X + 10, BlackRect.Y + 10);
            string theirText = encounter.ParseCurrentEncounterText(BlackRect.Width - 20, Fonts.Verdana12Bold);
            theirTextPos.X = (int)theirTextPos.X;
            theirTextPos.Y = (int)theirTextPos.Y;
            batch.DrawString(Fonts.Verdana12Bold, theirText, theirTextPos, Color.White);
            if (encounter.Current.EndTransmission)
            {
                var responsePos = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 10);
                batch.DrawString(Fonts.Arial12Bold, "Escape or Right Click to End Transmission:", responsePos, Color.White);
            }
            else
            {
                var drawCurs = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 5);
                batch.DrawString(Fonts.Arial12Bold, "Your Response:", drawCurs, Color.White);
                ResponseSL.Draw(batch, elapsed);
            }

            if (EmpireTex != null)
            {
                batch.FillRectangle(EmpireFlagRect, Color.Black);
                batch.Draw(EmpireTex, EmpireFlagRect, EmpireColor);
            }
        }

        public override bool HandleInput(InputState input)
        {
            CanEscapeFromScreen = encounter.Current.EndTransmission;
            if (encounter.Current.EndTransmission && (input.Escaped || input.RightMouseClick))
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            TitleText = encounter.Name;
            MiddleText = encounter.DescriptionText;
            base.LoadContent();
            Rectangle fitRect = new Rectangle(TitleRect.X - 4, TitleRect.Y + TitleRect.Height + MidContainer.Height + 10, 
                TitleRect.Width, 600 - (TitleRect.Height + MidContainer.Height));

            BlackRect = new Rectangle(fitRect.X, fitRect.Y, fitRect.Width, 300);
            ResponseRect = new Rectangle(fitRect.X, BlackRect.Y + BlackRect.Height + 10, fitRect.Width, 110);
            var resp = new Submenu(ResponseRect);
            ResponseSL = Add(new ScrollList2<ResponseListItem>(resp, 20));
            LoadResponseScrollList();
        }

        void LoadResponseScrollList()
        {
            ResponseSL.Reset();
            foreach (Response r in encounter.Current.ResponseOptions)
            {
                ResponseSL.AddItem(new ResponseListItem(r));
            }
            ResponseSL.OnClick = encounter.OnResponseItemClicked;
        }

    }
}