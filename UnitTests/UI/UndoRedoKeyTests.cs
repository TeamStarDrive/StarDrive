using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics.Input;
using Ship_Game;

namespace UnitTests.UI
{
    [TestClass]
    public class UndoRedoKeyTests : StarDriveTest
    {
        readonly MockInputProvider Keyboard = new();
        readonly InputState Input = new();

        public UndoRedoKeyTests()
        {
            Input.Provider = Keyboard;
        }

        // holds the modifiers for a frame, then presses the last key on top of them
        void Press(params Keys[] keys)
        {
            Keyboard.KeysDown.Clear();
            for (int i = 0; i < keys.Length - 1; ++i)
                Keyboard.KeysDown.Add(keys[i]);
            Input.Update(new UpdateTimes(1 / 60f, 0f));
            Keyboard.KeysDown.Add(keys[keys.Length - 1]);
            Input.Update(new UpdateTimes(1 / 60f, 0f));
        }

        [TestMethod]
        public void CtrlZUndoes()
        {
            Press(Keys.LeftControl, Keys.Z);
            Assert.IsTrue(Input.Undo, "Ctrl+Z must undo");
            Assert.IsFalse(Input.Redo, "and must not redo");
        }

        [TestMethod]
        public void CtrlShiftZRedoesAndDoesNotUndo()
        {
            Press(Keys.LeftControl, Keys.LeftShift, Keys.Z);
            Assert.IsTrue(Input.Redo, "Ctrl+Shift+Z must redo");
            Assert.IsFalse(Input.Undo, "the design screen checks undo first, so it must not undo too");
        }

        [TestMethod]
        public void CtrlYRedoes()
        {
            Press(Keys.LeftControl, Keys.Y);
            Assert.IsTrue(Input.Redo, "Ctrl+Y must redo");
            Assert.IsFalse(Input.Undo, "and must not undo");
        }
    }
}
