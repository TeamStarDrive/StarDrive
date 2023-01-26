using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        DeveloperUniverse Universe;
        TaskResult<DeveloperUniverse> CreateTask;

        public DeveloperSandbox() : base(null, toPause: null)
        {
            IsPopup = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            Mem.Dispose(ref CreateTask);
            base.Dispose(disposing);
        }

        public override void LoadContent()
        {
            Label(20, 20, "Loading Developer Sandbox . . .", Fonts.Arial20Bold);
            CreateTask = Parallel.Run(() =>
            {
                return DeveloperUniverse.Create(playerPreference:"United", numOpponents:1);
            });
        }
        
        // as a normal game screen
        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true); // no return
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            if (CreateTask != null)
            {
                Thread.Sleep(15); // @note This hugely speeds up loading
                if (CreateTask?.IsComplete == true)
                {
                    Universe = CreateTask.Result;
                    CreateTask = null;

                    // if Universe creation fails, go back to main menu
                    if (Universe == null)
                        ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true);
                    else
                        ScreenManager.GoToScreen(Universe, clear3DObjects:false);
                    return;
                }
            }
            ScreenState = ScreenState.Active;
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }
    }
}
