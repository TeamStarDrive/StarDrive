using System;
using Ship_Game;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

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
