using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class TutorialScreen : GameScreen
    {
        Array<Texture2D> TutorialSlides = new();
        Rectangle BridgeRect;
        int Index;

        public TutorialScreen(GameScreen parent) : base(parent, toPause: null)
        {
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void LoadContent()
        {
            // TODO: Implement a completely new Tutorial screen with Localized Text instead of graphics
            foreach (FileInfo info in Dir.GetFiles("Content/Tutorials/English/", "xnb"))
            {
                Texture2D tex = TransientContent.LoadTexture(info);
                TutorialSlides.Add(tex);
            }

            TutorialSlides.Sort(tex => tex.Name);
            for (int i = 0; i < TutorialSlides.Count; ++i)
                Log.Write($"Tutorial Slide {i}: {TutorialSlides[i].Name}");

            BridgeRect = new((int)ScreenCenter.X - 640, (int)ScreenCenter.Y - 360, 1280, 720);
            Add(new CloseButton(BridgeRect.Right - 38, BridgeRect.Y + 15));
            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            batch.Draw(TutorialSlides[Index], BridgeRect, Color.White);
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Right || input.InGameSelect)
            {
                if (++Index > TutorialSlides.Count - 1)
                    Index = 0;
            }
            if (input.Left || input.RightMouseClick)
            {
                if (--Index < 0)
                    Index = TutorialSlides.Count - 1;
            }
            return base.HandleInput(input);
        }
    }
}