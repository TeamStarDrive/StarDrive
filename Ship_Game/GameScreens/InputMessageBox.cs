using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Ship_Game.GameScreens
{
   public class InputMessageBox : MessageBoxScreen
    {
        UITextEntry TextEntry = new UITextEntry();
        string InputText = "";
        
       public InputMessageBox(GameScreen parent) : base(parent,"Input Issue", true)
        {
            TextEntry.Text = InputText;
            TextEntry.ClickableArea = Rect;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            batch.Begin();
            TextEntry.Draw(batch, Game1.Instance.GameTime);
            batch.End();
            base.Draw(batch);
        }

        public override void Update(GameTime gameTime, bool focus, bool covered)
        {            
            base.Update(gameTime, focus, covered);
        }

        public override bool HandleInput(InputState input)
        {
            input = input ?? Input;
            TextEntry.HandleTextInput(ref InputText, input);
            return base.HandleInput(input);
        }
    }
}
