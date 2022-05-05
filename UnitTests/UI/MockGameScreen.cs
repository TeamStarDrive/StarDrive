using System;
using Ship_Game;
using Rectangle = SDGraphics.Rectangle;

namespace UnitTests.UI
{
    class MockGameScreen : GameScreen
    {
        public MockGameScreen()
            : base(null, new Rectangle(0,0,GameBase.ScreenWidth,GameBase.ScreenHeight), toPause: null)
        {
        }
    }
}
