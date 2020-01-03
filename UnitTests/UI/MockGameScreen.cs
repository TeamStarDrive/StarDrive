using System;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.UI
{
    class MockGameScreen : GameScreen
    {
        public MockGameScreen()
            : base(null, new Rectangle(0,0,GameBase.ScreenWidth,GameBase.ScreenHeight), false)
        {
        }
    }
}
