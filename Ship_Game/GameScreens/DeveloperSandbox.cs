using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        //private UniverseScreen Universe;

        public DeveloperSandbox(GameScreen parent) : base(parent)
        {
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);


            //var playerEmpire = new Empire()
            //{
            //    EmpireColor = currentObjectColor,
            //    data = SelectedData
            //};
            //playerEmpire.data.SpyModifier = RaceSummary.SpyMultiplier;
            //playerEmpire.data.Traits.Spiritual = RaceSummary.Spiritual;
            //RaceSummary.Adj1 = SelectedData.Traits.Adj1;
            //RaceSummary.Adj2 = SelectedData.Traits.Adj2;
            //playerEmpire.data.Traits = RaceSummary;
            //playerEmpire.EmpireColor = currentObjectColor;

            //empire.Initialize();



            //Universe = new UniverseScreen(Data)
            //{
            //    player = PlayerEmpire,
            //    CamPos = new Vector3(-playerShip.Center.X, playerShip.Center.Y, 5000f),
            //    ScreenManager = ScreenManager,
            //    GameDifficulty = Difficulty,
            //    GameScale = Scale
            //};
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Begin();
            base.Draw(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }
    }
}
