using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        DeveloperUniverse Universe;
        TaskResult<UniverseData> CreateTask;

        public DeveloperSandbox() : base(null)
        {
            IsPopup = true;
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            CreateTask = Parallel.Run(() => DeveloperUniverse.Create(
                                      playerPreference:"United",
                                      numOpponents:1));
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
                Thread.Sleep(10); // @note This hugely speeds up loading
                if (CreateTask?.IsComplete == true)
                {
                    UniverseData sandbox = CreateTask.Result;
                    CreateTask = null;
                    Universe = new DeveloperUniverse(sandbox, sandbox.EmpireList.First, paused:false);
                    ScreenManager.GoToScreen(Universe, clear3DObjects:false);
                }
            }
            ScreenState = ScreenState.Active;
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }
    }
}
