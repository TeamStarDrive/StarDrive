using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics.Input;
using Ship_Game;
using Rectangle = SDGraphics.Rectangle;

namespace UnitTests.UI
{
    [TestClass]
    public class TestFloatSlider : StarDriveTest
    {
        class SliderScreen : GameScreen
        {
            public SliderScreen()
                : base(null, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), toPause: null)
            {
            }
        }

        [TestMethod]
        public void AStepDragFiresOnChangeOncePerStep()
        {
            var screen = new SliderScreen();
            var slider = new FloatSlider(SliderStyle.Percent, new Rectangle(100, 100, 232, 40), "test", 0f, 1f, 0.5f);
            screen.Add(slider);
            int calls = 0, repeats = 0;
            float last = slider.RelativeValue;
            slider.OnChange = s =>
            {
                ++calls;
                if (s.RelativeValue == last)
                    ++repeats;
                last = s.RelativeValue;
            };

            Game.Manager.AddScreenAndLoadContent(screen);
            try
            {
                MockInput.SetMouse(200, 126); // the knob, at 50% of a slider 200 wide starting at x=100
                MockInput.LeftMouse = ButtonState.Pressed;
                Game.Tick();
                Game.Tick();
                MockInput.SetMouse(240, 126);
                Game.Tick();
                MockInput.SetMouse(260, 126);
                Game.Tick();

                AssertEqual(0.001f, 0.8f, slider.RelativeValue, "setup: the drag must have moved the slider");
                Assert.AreEqual(2, calls, "one OnChange per step of the drag");
                Assert.AreEqual(0, repeats, "no OnChange repeats a value it already reported");
            }
            finally
            {
                MockInput.LeftMouse = ButtonState.Released;
                Game.Tick();
                Game.Manager.RemoveScreen(screen);
            }
        }
    }
}
