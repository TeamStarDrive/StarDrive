using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.StoryAndEvents;

namespace Ship_Game
{
    public sealed class EncounterPopup : PopupWindow
    {
        public bool fade;
        public bool FromGame;
        public string UID;
        public Encounter Encounter;
        public EncounterInstance Instance;

        Rectangle ResponseRect;

        string DialogText;
        UITextBox DialogTextBox;
        ScrollList2<ResponseListItem> ResponseSL;
        UIButton DismissButton;

        EncounterPopup(UniverseScreen s, Empire player, Empire targetEmp, Encounter e) : base(s, 600, 600)
        {
            Encounter = e;
            Instance = new EncounterInstance(e, player, targetEmp);
            fade = true;
            IsPopup = true;
            FromGame = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0f;
        }

        public static void Show(UniverseScreen s, Empire player, Empire them, Encounter e)
        {
            var screen = new EncounterPopup(s, player, them, e); 
            ScreenManager.Instance.AddScreen(screen);
        }

        public override void LoadContent()
        {
            TitleText = Encounter.Name;
            MiddleText = Encounter.DescriptionText;
            base.LoadContent();

            Rectangle fitRect = BottomBigFill.CutTop(10);
            int responseHeight = Instance.CurrentDialog.ResponseOptions.Count * 36 + 32;

            var blackRect = new Rectangle(fitRect.X, fitRect.Y, fitRect.Width, fitRect.Height - responseHeight);
            DialogTextBox = Add(new UITextBox(blackRect));
            SetDialogTextBoxContent();

            ResponseRect = new Rectangle(fitRect.X, blackRect.Bottom, fitRect.Width, responseHeight);
            ResponseSL = Add(new ScrollList2<ResponseListItem>(ResponseRect, 24));
            ResponseSL.Reset();
            foreach (Response r in Instance.CurrentDialog.ResponseOptions)
            {
                ResponseSL.AddItem(new ResponseListItem(r, Instance.OnResponseItemClicked));
            }

            DismissButton = Button(ButtonStyle.EventConfirm, GameText.Ok, b => ExitScreen());
            DismissButton.Font = Fonts.Arial14Bold;
            DismissButton.SetPosToCenterOf(this).SetDistanceFromBottomOf(this, 32);

            if (Instance.TargetEmpire != null)
            {
                Panel(EmpireFlagRect, Color.Black);
                Panel(EmpireFlagRect, Instance.TargetEmpire.EmpireColor, 
                      ResourceManager.Flag(Instance.TargetEmpire));
            }
        }

        void SetDialogTextBoxContent()
        {
            DialogText = Instance.CurrentDialogText;
            DialogTextBox.Clear();
            DialogTextBox.AddLines(DialogText, Fonts.Verdana12Bold, Color.White);
        }

        public override bool HandleInput(InputState input)
        {
            CanEscapeFromScreen = Instance.CurrentDialog.EndTransmission;
            Close.Visible = CanEscapeFromScreen;
            if (input.RightMouseClick)
                return false; // dont let this screen exit on right click

            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            DismissButton.Visible = Instance.CurrentDialog.EndTransmission;
            ResponseSL.Visible = !Instance.CurrentDialog.EndTransmission;

            if (DialogText != Instance.CurrentDialogText)
                SetDialogTextBoxContent();

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (fade)
                ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch, elapsed);
        }
    }
}