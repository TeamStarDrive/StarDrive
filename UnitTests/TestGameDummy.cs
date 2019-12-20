using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;

namespace UnitTests
{
    public class TestGameDummy : GameDummy
    {
        readonly AutoResetEvent Started;
        public KeyboardState Keys;
        bool CachedVisibility;
        public bool Visible;

        public TestGameDummy(AutoResetEvent started, int width, int height, bool show)
            : base(width, height, show)
        {
            Started = started;
            CachedVisibility = Visible = show;
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            GlobalStats.XRES = (int)ScreenSize.X; // Required for DrawLine...
            GlobalStats.YRES = (int)ScreenSize.Y;
            ScreenCenter = ScreenSize * 0.5f;
            IsFixedTimeStep = false;
        }
        
        protected override void BeginRun()
        {
            base.BeginRun();
            Fonts.LoadContent(Content);
            Started.Set();
        }

        protected override void Update(GameTime time)
        {
            if (CachedVisibility != Visible)
            {
                CachedVisibility = Visible;
                Form.Visible = Visible;
            }
            if (!Visible)
                return;

            Keys = Keyboard.GetState();
            base.Update(time);

            if (Keys.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Visible = false;
        }

        protected override void Draw(GameTime time)
        {
            if (!Visible)
                return;

            GraphicsDevice.Clear(Color.Black);
            ScreenManager.UpdateGraphicsDevice();

            try
            {
                Batch.Begin();
                base.Draw(time);
            }
            finally
            {
                Batch.End();
            }
        }

        public void AddComponent(TestGameComponent component)
        {
            component.Game = this;
            Components.Add(component);
        }

        public void RemoveComponent(TestGameComponent component)
        {
            Components.Remove(component);
        }

        public void DrawText(float x, float y, string text)
        {
            Batch.DrawString(Fonts.Arial14Bold, text, new Vector2(x,y), Color.White);
        }

        public void DrawText(float x, float y, string text, Color color)
        {
            Batch.DrawString(Fonts.Arial14Bold, text, new Vector2(x,y), color);
        }

        public void ShowAndRun(TestGameComponent component)
        {
            AddComponent(component);
            Visible = true;
            while (Visible)
            {
                RunOne();
                Tick();
            }
            RemoveComponent(component);
            Thread.Sleep(100); // ughh, some weird window related bug
        }

        static TestGameDummy Instance;

        public static TestGameDummy GetOrStartInstance(int width, int height, bool show=true)
        {
            if (Instance != null)
                return Instance;

            var started = new AutoResetEvent(false);
            new Thread(() =>
            {
                try
                {
                    Instance = new TestGameDummy(started, width, height, show);
                    Instance.Run(); // this will only return once the window is closed
                }
                finally
                {
                    Instance = null; // clean up
                }
            }) { Name = "ImpactSimThread" }.Start();

            started.WaitOne(); // wait until Instance.BeginRun() is finished
            return Instance;
        }
    }
}