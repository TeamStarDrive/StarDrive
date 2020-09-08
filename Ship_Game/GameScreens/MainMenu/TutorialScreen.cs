using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class TutorialScreen : GameScreen
    {
        Map<string, Texture2D> TexDict = new Map<string, Texture2D>();
        Rectangle BridgeRect;
        int Index;

        public TutorialScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            batch.Draw(TexDict["Slide_"+Index.ToString("00")], BridgeRect, Color.White);
            base.Draw(batch, elapsed);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Right || input.InGameSelect)
            {
                TutorialScreen index = this;
                index.Index = index.Index + 1;
                if (Index > TexDict.Count - 1)
                {
                    Index = 0;
                }
            }
            if (input.Left || input.RightMouseClick)
            {
                TutorialScreen tutorialScreen = this;
                tutorialScreen.Index = tutorialScreen.Index - 1;
                if (Index < 0)
                {
                    Index = TexDict.Count - 1;
                }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            FileInfo[] textList;
            try
            {
                 textList = Dir.GetFiles("Content/Tutorials/" + GlobalStats.Language + "/", "xnb");
            }
            catch
            {
                 textList = Dir.GetFiles("Content/Tutorials/English/", "xnb");
            }
            foreach (FileInfo info in textList)
            {
                string name = Path.GetFileNameWithoutExtension(info.Name);
                Texture2D tex = StarDriveGame.Instance.Content.Load<Texture2D>("Tutorials/"+ GlobalStats.Language+"/"+name);
                TexDict[name] = tex;
            }
            BridgeRect = new Rectangle((int)ScreenCenter.X - 640, (int)ScreenCenter.Y - 360, 1280, 720);
            Add(new CloseButton(BridgeRect.Right - 38, BridgeRect.Y + 15));
            base.LoadContent();
        }
    }
}