using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Rectangle = SDGraphics.Rectangle;

namespace UnitTests.UI
{
    [TestClass]
    public class TestTooltipSuppression : StarDriveTest
    {
        class TipScreen : GameScreen
        {
            public int Draws;
            readonly GameText Text;
            readonly string CodexUid;

            public TipScreen(bool popup, GameText text, string codexUid)
                : base(null, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), toPause: null)
            {
                IsPopup = popup;
                Text = text;
                CodexUid = codexUid;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                ++Draws;
                if (CodexUid != null)
                    ToolTip.CreateTooltip(Text, codexUid: CodexUid);
            }
        }

        [TestCleanup]
        public void ClearTips() => ToolTip.Clear();

        [TestMethod]
        public void ScreenReceivingInputCreatesTooltips()
        {
            var screen = new TipScreen(popup: false, GameText.Total2, "tip_owner");
            Game.Manager.AddScreenAndLoadContent(screen);
            Game.Tick();

            AssertTrue(screen.DidHandleInput);
            AssertEqual("tip_owner", ToolTip.GetActiveCodexUid());
        }

        [TestMethod]
        public void CoveredScreenCannotCreateTooltips()
        {
            var covered = new TipScreen(popup: false, GameText.Total2, "tip_owner");
            Game.Manager.AddScreenAndLoadContent(covered);
            Game.Tick();
            AssertEqual("tip_owner", ToolTip.GetActiveCodexUid());

            var popup = new TipScreen(popup: true, GameText.Income, null);
            Game.Manager.AddScreenAndLoadContent(popup);
            ToolTip.Clear();

            int drawsBefore = covered.Draws;
            Game.Tick();

            AssertTrue(popup.DidHandleInput);
            AssertFalse(covered.DidHandleInput);
            AssertTrue(covered.Draws > drawsBefore, "the covered screen must still draw, which is why its tips used to stick");
            Assert.IsNull(ToolTip.GetActiveCodexUid(), "a covered screen must not create tooltips over the screen above it");
        }
    }
}
