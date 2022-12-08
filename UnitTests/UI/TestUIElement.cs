using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.UI
{
    [TestClass]
    public class TestUIElement : StarDriveTest
    {
        class SimpleElement : UIElementV2
        {
            public int InputEvents, MouseOverEvents, UpdateEvents, DrawEvents;
            public int EventId, InputEventId, UpdateEventId, DrawEventId;

            public SimpleElement() {}
            public SimpleElement(Vector2 pos, Vector2 size) : base(pos, size) {}

            public override bool HandleInput(InputState input)
            {
                InputEventId = ++EventId;
                ++InputEvents;
                if (HitTest(input.CursorPosition))
                {
                    ++MouseOverEvents;
                    return true;
                }
                return false;
            }

            public override void Update(float fixedDeltaTime)
            {
                UpdateEventId = ++EventId;
                ++UpdateEvents;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                DrawEventId = ++EventId;
                ++DrawEvents;
            }
        }

        public TestUIElement()
        {
        }

        [TestMethod]
        public void UIElementEventOrder()
        {
            var screen = new MockGameScreen();
            Game.Manager.AddScreenAndLoadContent(screen);
            MockInput.MousePos = new Vector2(512, 512);

            var element = new SimpleElement(Vector2.Zero, new Vector2(100, 100));
            screen.Add(element);

            Game.Tick();
            AssertEqual(1, element.InputEvents);
            AssertEqual(0, element.MouseOverEvents);
            AssertEqual(1, element.UpdateEvents);
            AssertEqual(1, element.DrawEvents);
            AssertEqual(1, element.InputEventId, "Input Events must be before Update Events");
            AssertEqual(2, element.UpdateEventId, "Update Events must be before Draw Events");
            AssertEqual(3, element.DrawEventId, "Draw Events must be after Update Events");
            
            MockInput.MousePos = new Vector2(50, 50);
            Game.Tick();
            AssertEqual(2, element.InputEvents);
            AssertEqual(1, element.MouseOverEvents);
            AssertEqual(2, element.UpdateEvents);
            AssertEqual(2, element.DrawEvents);
        }
    }
}
